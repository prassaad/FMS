using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;
using FMS.Areas.Inventory.Controllers;
using System.Globalization;
namespace FMS.Areas.Inventory.Controllers
{
    [CustomAuthorizationFilter]
    public class PurchaseOrdersController : Controller
    {
        private List<tbl_PurchaseOrderItemDetails> invPurOrder_det = new List<tbl_PurchaseOrderItemDetails>();
        private FMSDBEntities db = new FMSDBEntities();
        private StoresController objStore = new StoresController();
        private IndentController objIndent = new IndentController();
        public core objcore = new core();
        public ActionResult Index()
        {
            var PurchaseOrders = db.tbl_PurchaseOrders.Where(a=>a.Active == true).ToList();
            return View(PurchaseOrders);
        }
        [HttpGet]
        public ActionResult PurchaseOrderCreate()
        {
            ViewBag.SupplierId = new SelectList(db.tbl_SuppliersMaster, "SupplierId", "SupplierName");
            return View();
        }
        [HttpPost]
        public ActionResult PurchaseOrderCreate(FormCollection frm)
        {
            try
            {
                List<tbl_PurchaseOrderItemDetails> PurchaseOrderDetList = (List<tbl_PurchaseOrderItemDetails>)Session["PurchaseOrdersList"];
                if (PurchaseOrderDetList == null)
                    return Json(new { success = false, POId = 0, msg = "Please check order items are added.\n suggestion do logoff and login then try again." });

                string RefNo = frm["RefNo"] == null ? "" : frm["RefNo"];
                string OrderDt = frm["OrderDt"].ToString();
                string IsFromIndent = frm["hdnIsFromIndent"].ToString();
               
                DateTime OrderDate = DateTime.ParseExact(OrderDt.ToString(), "dd\\/MM\\/yyyy", CultureInfo.InvariantCulture);
                string ExpectedDt = frm["ExpectedDt"].ToString();
                DateTime ExpectedDate = DateTime.ParseExact(ExpectedDt.ToString(), "dd\\/MM\\/yyyy", CultureInfo.InvariantCulture);
                string TermsAndConditions = frm["TermsAndConditions"] == null ? "" : frm["TermsAndConditions"].ToString();
                //string Cust_Name = frm["employee"] == null ? "" : frm["employee"];
                
                decimal UnTaxedAmt = frm["UnTaxed"].ToString() == "" ? 0 : Convert.ToDecimal(frm["UnTaxed"]); 
                long SupplierId = (frm["SupplierId"] == null || frm["SupplierId"] == "") ? 0 : Convert.ToInt64(frm["SupplierId"]);
                // check duplicate ref. number
                bool isduplicate = db.tbl_PurchaseOrders.Where(a => a.Active == true && a.RefNo.Trim().ToUpper() == RefNo.Trim().ToUpper()).Any();
                if (isduplicate == true)
                {
                    return Json(new { success = false, POId = 0, msg = "Reference Number has already exists." });
                }
                // Insert Purchase Order
                tbl_PurchaseOrders objPurchaseOrder = new tbl_PurchaseOrders();
                objPurchaseOrder.SupplierId = SupplierId;
                objPurchaseOrder.RefNo = RefNo;
                objPurchaseOrder.OrderDt = OrderDate;
                objPurchaseOrder.CreatedBy = User.Identity.Name;
                objPurchaseOrder.ExpectedDt = ExpectedDate;
                objPurchaseOrder.Terms_Conditions = TermsAndConditions;
                objPurchaseOrder.UnTaxed = UnTaxedAmt;
                objPurchaseOrder.Total =(decimal)PurchaseOrderDetList.Sum(a => a.Qty);  
                objPurchaseOrder.Status = PurchaseOrderStatus.Edit.ToString();
                objPurchaseOrder.Active = true;
                if (IsFromIndent == "1")
                {
                    string SourceRefNo = frm["hdnSource"].ToString();
                    objPurchaseOrder.Source = SourceRefNo;
                }
                db.tbl_PurchaseOrders.AddObject(objPurchaseOrder);
                db.SaveChanges();

                // Insert issued Details list 

                List<tbl_PurchaseOrderItemDetails> OrderItemsList = new List<tbl_PurchaseOrderItemDetails>();
                foreach (tbl_PurchaseOrderItemDetails _OrderDet in PurchaseOrderDetList)
                {
                    tbl_PurchaseOrderItemDetails objPOrderDetails = new tbl_PurchaseOrderItemDetails();
                    // get Item Unit Price
                    double _itemAmt = (double)db.InventoryItemsMasters.SingleOrDefault(a => a.ID == _OrderDet.ItemId).Amount;
                    objPOrderDetails.POId = objPurchaseOrder.Id;
                    objPOrderDetails.ItemId = _OrderDet.ItemId;
                    objPOrderDetails.Description = _OrderDet.Description;
                    objPOrderDetails.ScheduleDt = _OrderDet.ScheduleDt;
                    objPOrderDetails.Qty = Convert.ToDouble(_OrderDet.Qty);
                    objPOrderDetails.UnitPrice = _itemAmt;
                    objPOrderDetails.Taxes = _OrderDet.Taxes;
                    objPOrderDetails.SubTotal = (_OrderDet.Qty * _itemAmt);
                    OrderItemsList.Add(objPOrderDetails);
                }
                foreach (var objorderDet in OrderItemsList)
                {
                    db.tbl_PurchaseOrderItemDetails.AddObject(objorderDet);
                    db.SaveChanges();
                }
                return Json(new { success = true, POId = objPurchaseOrder.Id, msg = "Purchase order has been added successfully with reference no " + RefNo + "." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, POId = 0, msg = "Please check the details and try again." });
            }
        }
        [HttpGet]
        public ActionResult PurchaseOrderEdit(long Id)
        {
            tbl_PurchaseOrders _POrder = db.tbl_PurchaseOrders.Where(a => a.Active == true && a.Id == Id).FirstOrDefault();
            // ==> check Quantity Avalability  for Each Order Details
           // CheckAvailableQuantity(_Issued.Id);
            ViewBag.SupplierId = new SelectList(db.tbl_SuppliersMaster, "SupplierId", "SupplierName",_POrder.SupplierId);
            List<tbl_PurchaseOrderItemDetails> _POrderDetailsList = GetPurchaseOrderDetailsByPOId(_POrder.Id);
            Session["PurchaseOrdersList"] = _POrderDetailsList;
            return View(new InventoryModel.PurchaseOrder(_POrder, _POrderDetailsList));
        }
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult PurchaseOrderEdit(FormCollection frm)
        {
            try
            {
                string RefNo = frm["RefNo"] == null ? "" : frm["RefNo"];
                string OrderDt = frm["OrderDt"].ToString();
                DateTime OrderDate = DateTime.ParseExact(OrderDt.ToString(), "dd\\/MM\\/yyyy", CultureInfo.InvariantCulture);
                string ExpectedDt = frm["ExpectedDt"].ToString();
                DateTime ExpectedDate = DateTime.ParseExact(ExpectedDt.ToString(), "dd\\/MM\\/yyyy", CultureInfo.InvariantCulture);
                string TermsAndConditions = frm["TermsAndConditions"] == null ? "" : frm["TermsAndConditions"].ToString();
                //string Cust_Name = frm["employee"] == null ? "" : frm["employee"];
                decimal UnTaxedAmt = frm["UnTaxed"].ToString() == "" ? 0 : Convert.ToDecimal(frm["UnTaxed"]);
                long SupplierId = (frm["SupplierId"] == null || frm["SupplierId"] == "") ? 0 : Convert.ToInt64(frm["SupplierId"]);
                long Id = frm["hdnPOId"] == null ? 0 : Convert.ToInt64(frm["hdnPOId"].ToString());
                List<tbl_PurchaseOrderItemDetails> _POrderDetailsList = GetPurchaseOrderDetailsByPOId(Id);
                // Update Purchase Orders Masters  
                tbl_PurchaseOrders _POrder = db.tbl_PurchaseOrders.Where(a => a.Active == true && a.Id == Id).FirstOrDefault();
                if (_POrder.RefNo.Trim().ToUpper() != RefNo.Trim().ToUpper())
                {
                    bool isduplicate = db.tbl_PurchaseOrders.Where(a => a.Active == true && a.RefNo.Trim().ToUpper() == RefNo.Trim().ToUpper()).Any();
                    if (isduplicate == true)
                    {
                        return Json(new { success = false, POId = 0, msg = "Reference Number has already exists." });
                    }
                }
                _POrder.SupplierId = SupplierId;
                _POrder.RefNo = RefNo;
                _POrder.OrderDt = OrderDate;
                _POrder.CreatedBy = User.Identity.Name;
                _POrder.ExpectedDt = ExpectedDate;
                _POrder.Terms_Conditions = TermsAndConditions;
                _POrder.UnTaxed = UnTaxedAmt;
                _POrder.Total = (decimal)_POrderDetailsList.Sum(a => a.Qty);
                _POrder.InvoiceReceived = false;
                _POrder.Status = PurchaseOrderStatus.Edit.ToString();
                db.SaveChanges();
                return Json(new { success = true, POId = _POrder.Id, msg = "Purchase Order has been saved sucessfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, POId = 0, msg = "Please check the details and try again." });
            }
        }
        public ActionResult AddPurchaseOrderItem(long SupplierId, int IsEdit)
        {
            ViewBag.SupplierName = db.tbl_SuppliersMaster.Where(a => a.SupplierId == SupplierId).FirstOrDefault().Title1;
            ViewBag.SupplierId = SupplierId;
            ViewBag.ItemId = new SelectList(db.InventoryItemsMasters.Where(a => a.Active == true), "ID", "ItemText");
            ViewBag.IsEdit = IsEdit;

            return PartialView("_AddPurchaseOrderItem", new tbl_PurchaseOrderItemDetails());
        }
        public ActionResult PurchaseOrderItemListAdd(int ItemId, double Qty,double Tax, string Description,string ScheduleDt, long? POId,long SupplierId, FormCollection frm)
        {
            InventoryItemsMaster _itemMaster = db.InventoryItemsMasters.Single(a => a.ID == ItemId);
            DateTime ScheduleDate = DateTime.ParseExact(ScheduleDt, "dd\\/MM\\/yyyy", CultureInfo.InvariantCulture);
            string ItemName = _itemMaster.ItemText;
            invPurOrder_det = (List<tbl_PurchaseOrderItemDetails>)Session["PurchaseOrdersList"];
            if (invPurOrder_det != null)
            {
                // check items if already have on ItemId and Campus  if not Canceled or Transfer
                if (POId == 0 || POId == null)
                {
                    if (invPurOrder_det.Where(a => a.ItemId == ItemId).Any())
                        return Json(new { success = false, POId = 0, msg = "Order items are already added for this Campus" });
                }
                else
                {
                    List<tbl_PurchaseOrderItemDetails> _CurrentIssueDet = GetPurchaseOrderDetailsByItemAndCampus(ItemId,POId,SupplierId);
                    if (_CurrentIssueDet.Count > 0)
                        return Json(new { success = false, POId = POId, msg = "Order items are already added for this Campus" });
                }
            }
            else
                invPurOrder_det = new List<tbl_PurchaseOrderItemDetails>();
            long PurchaseOrderId = POId == null ? 0 : Convert.ToInt64(POId);
            int Id = 0;
            if (invPurOrder_det.Count() ==0)
                Id = 1;
            else
                Id = (int)(invPurOrder_det.LastOrDefault().Id + 1);
            if (PurchaseOrderId == 0 || PurchaseOrderId == null)
            {
                invPurOrder_det.Add(new tbl_PurchaseOrderItemDetails
                {
                    Id = Id,
                    ItemId = ItemId,
                    Qty = Convert.ToDouble(Qty),
                    Description = Description,
                    ScheduleDt = ScheduleDate,
                    //UnitPrice = UnitPrice,
                    Taxes = Tax
                });
            }
            else
            {
                invPurOrder_det.Add(new tbl_PurchaseOrderItemDetails
                {
                    Id = Id,
                    ItemId = ItemId,
                    POId = (long)POId,
                    Qty = Convert.ToDouble(Qty),
                    Description = Description,
                    ScheduleDt = ScheduleDate,
                    //UnitPrice = UnitPrice,
                    Taxes = Tax
                });
            }
            Session["PurchaseOrdersList"] = invPurOrder_det;
            ViewData["IsEdit"] = "0";
            if (PurchaseOrderId != 0)
            {
                //tbl_PurchaseOrders _POrder = db.tbl_PurchaseOrders.Where(a => a.Active == true && a.Id == PurchaseOrderId).FirstOrDefault();
                //if (_POrder.RefNo.Trim().ToUpper() != RefNo.Trim().ToUpper())
                //{
                //    bool isduplicate = db.tbl_PurchaseOrders.Where(a => a.Active == true && a.RefNo.Trim().ToUpper() == RefNo.Trim().ToUpper()).Any();
                //    if (isduplicate == true)
                //    {
                //        return Json(new { success = false, POId = 0, msg = "Reference Number has already exists." });
                //    }
                //}
                double _itemAmt = (double)db.InventoryItemsMasters.SingleOrDefault(a => a.ID == ItemId).Amount;
                tbl_PurchaseOrderItemDetails objPurchaseOrderDet = new tbl_PurchaseOrderItemDetails();
                objPurchaseOrderDet.Description = Description;
                objPurchaseOrderDet.POId = PurchaseOrderId;
                objPurchaseOrderDet.ScheduleDt = ScheduleDate;
                objPurchaseOrderDet.Qty = Convert.ToDouble(Qty);
                objPurchaseOrderDet.ItemId = ItemId;
                objPurchaseOrderDet.UnitPrice = _itemAmt;
                objPurchaseOrderDet.Taxes = Tax;
                objPurchaseOrderDet.SubTotal = (Qty * _itemAmt);
                db.tbl_PurchaseOrderItemDetails.AddObject(objPurchaseOrderDet);
                db.SaveChanges(); 
                ViewData["IsEdit"] = "1";
            }
            return Json(new { success = true, POId = PurchaseOrderId, msg = "Order item insert successfully." });
        }
        public ActionResult LoadPurchaseOrderItems(long POId)
        {
            if (POId == 0 || POId == null)
            {
                invPurOrder_det = (List<tbl_PurchaseOrderItemDetails>)Session["PurchaseOrdersList"];
                ViewData["IsEdit"] = "0";
                return PartialView("_OrderItemsListAdd", invPurOrder_det);
            }
            else
            {
                List<tbl_PurchaseOrderItemDetails> _OrderDet = GetPurchaseOrderDetailsByPOId(POId);
                ViewData["IsEdit"] = "1";
                return PartialView("_OrderItemsListAdd", _OrderDet);
            }
        }
        public ActionResult ConfirmRFQEntry(long Id)
        {
            tbl_PurchaseOrders _PurchaseOrders = db.tbl_PurchaseOrders.Where(a => a.Id == Id).FirstOrDefault();
            try
            {
                _PurchaseOrders.Status = PurchaseOrderStatus.RFQ.ToString();
                UpdateModel(_PurchaseOrders);
                db.SaveChanges();
                return Json(new { success = true, msg = "Request for Quatation has been confirmed successfully." });
            }
            catch
            {
                return Json(new { success = false, msg = "Request for Quatation confirmation failed." });
            }
        }
        public ActionResult SavePOrderBid(long Id)
        {
            tbl_PurchaseOrders _PurchaseOrders = db.tbl_PurchaseOrders.Where(a => a.Id == Id).FirstOrDefault();
            try
            {
                _PurchaseOrders.Status = PurchaseOrderStatus.BidReceived.ToString();
                _PurchaseOrders.BidReceivedDt = DateTime.Now;
                UpdateModel(_PurchaseOrders);
                db.SaveChanges();
                return Json(new { success = true, msg = "Bid has been received successfully." });
            }
            catch
            {
                return Json(new { success = false, msg = "Bid received failed." });
            }
        }
       public ActionResult ConfirmPurchaseOrder(long Id)
        {
            tbl_PurchaseOrders _PurchaseOrders = db.tbl_PurchaseOrders.Where(a => a.Id == Id).FirstOrDefault();
            try
            {
                _PurchaseOrders.Status = PurchaseOrderStatus.ConfirmOrder.ToString();
                UpdateModel(_PurchaseOrders);
                db.SaveChanges();
                return Json(new { success = true, msg = "Purchase order has been confirmed successfully." });
            }
            catch
            {
                return Json(new { success = false, msg = "Purchase order confirmation failed." });
            }
        }
        [HttpGet]
        public ActionResult ViewPurchaseOrders(long Id)
        {
            tbl_PurchaseOrders _PurchaseOrders = db.tbl_PurchaseOrders.Where(a => a.Id == Id).FirstOrDefault();
            ViewBag.POId = Id;
            ViewBag.Status = _PurchaseOrders.Status;
            ViewBag.RefNo = _PurchaseOrders.RefNo;
            return View();
        }
        public ActionResult GetOrderItemDetails(long Id)
        {
            tbl_PurchaseOrders _PurchaseOrders = db.tbl_PurchaseOrders.Where(a => a.Id == Id).FirstOrDefault();
            List<tbl_PurchaseOrderItemDetails> _POrderDetailsList = GetPurchaseOrderDetailsByPOId(_PurchaseOrders.Id);
            return PartialView("_GetOrderItemDetails", new InventoryModel.PurchaseOrder(_PurchaseOrders, _POrderDetailsList));
        }
        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult EditOrderDetails(long Id)
        {
            tbl_PurchaseOrderItemDetails _POrderDets = db.tbl_PurchaseOrderItemDetails.Where(a => a.Id == Id).FirstOrDefault();
            tbl_PurchaseOrders _PurchaseOrders = db.tbl_PurchaseOrders.Where(a => a.Id == _POrderDets.POId).FirstOrDefault();
            InventoryItemsMaster _itemMaster = db.InventoryItemsMasters.Single(a => a.ID == _POrderDets.ItemId);
            decimal AvailQty = (decimal)objStore.GetAvailQty("",(int)_POrderDets.ItemId, 0);
            ViewBag.AvailQty = AvailQty;
            ViewBag.UnitPrice = _itemMaster.Amount;
            return PartialView("_EditOrderDetails", _POrderDets);
        }
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult EditOrderDetails(long Id, int Qty, double Tax, string Description, string ScheduleDt)
        {
            tbl_PurchaseOrderItemDetails _POrderDets = db.tbl_PurchaseOrderItemDetails.Where(a => a.Id == Id).FirstOrDefault();
            tbl_PurchaseOrders _PurchaseOrders = db.tbl_PurchaseOrders.Where(a => a.Id == _POrderDets.POId).FirstOrDefault();
            InventoryItemsMaster _itemMaster = db.InventoryItemsMasters.Single(a => a.ID == _POrderDets.ItemId);
            DateTime ScheduleDate = DateTime.ParseExact(ScheduleDt.ToString(), "dd\\/MM\\/yyyy", CultureInfo.InvariantCulture);
            _POrderDets.Qty = Qty;
            _POrderDets.Taxes = Tax;
            _POrderDets.Description = Description;
            _POrderDets.ScheduleDt = ScheduleDate;
            _POrderDets.SubTotal = (_POrderDets.Qty *(double)_itemMaster.Amount);
            db.SaveChanges();
            return Json(new { success = true, Id = _PurchaseOrders.Id, msg = "Purchase order details has been updated  successfully." });
        }
        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult DeleteOrderDetails(string Id)
        {
            string IsEdit = Id.Split('_')[1];
            long OrderDetId = Convert.ToInt64(Id.Split('_')[0]);
            if (IsEdit == "1")
            {
                tbl_PurchaseOrderItemDetails _POrderDets = db.tbl_PurchaseOrderItemDetails.Where(a => a.Id == OrderDetId).FirstOrDefault();
                db.tbl_PurchaseOrderItemDetails.DeleteObject(_POrderDets);
                db.SaveChanges();
                return RedirectToAction("PurchaseOrderEdit", "PurchaseOrders", new { Area = "Inventory", Id = _POrderDets.POId });
            }
            else
            {
                invPurOrder_det = (List<tbl_PurchaseOrderItemDetails>)Session["PurchaseOrdersList"];
                invPurOrder_det.RemoveAll(a => a.Id == OrderDetId);

                List<tbl_PurchaseOrderItemDetails> _TempList = new List<tbl_PurchaseOrderItemDetails>();
                int OrderId = 1;
                foreach (tbl_PurchaseOrderItemDetails _OrderDet in invPurOrder_det)
                {
                    _TempList.Add(new tbl_PurchaseOrderItemDetails
                    {
                        Id = OrderId,
                        ItemId = _OrderDet.ItemId,
                        POId = _OrderDet.POId,
                        Qty = _OrderDet.Qty,
                        Taxes = _OrderDet.Taxes,
                        Description = _OrderDet.Description,
                        ScheduleDt = _OrderDet.ScheduleDt,
                        UnitPrice = _OrderDet.UnitPrice,
                        SubTotal = _OrderDet.SubTotal
                    });
                    Id += 1;
                }
                invPurOrder_det = _TempList;

                ViewData["IsEdit"] = "0";
                return PartialView("_OrderItemsListAdd", invPurOrder_det);
            }

        }
        [HttpGet]
        public ActionResult GetRFQBidDetails(long Id)
        {
            tbl_PurchaseOrders _PurchaseOrders = db.tbl_PurchaseOrders.Where(a => a.Id == Id).FirstOrDefault();
            return PartialView("_RFQBidDetails",_PurchaseOrders);
        }
        [HttpPost]
        public ActionResult SaveBidDetails(long Id, string BidValidity, string BidTerms)
        {
            tbl_PurchaseOrders _PurchaseOrders = db.tbl_PurchaseOrders.Where(a => a.Id == Id).FirstOrDefault();
            try
            {
                DateTime BidValidityDt = DateTime.ParseExact(BidValidity.ToString(), "dd\\/MM\\/yyyy", CultureInfo.InvariantCulture);
                _PurchaseOrders.BidTerms = BidTerms;
                _PurchaseOrders.BidValidity = BidValidityDt;
                // UpdateModel(_PurchaseOrders);
                db.SaveChanges();
                return Json(new { success = true, msg = "Bid details has been saved successfully." });
            }
            catch
            {
                return Json(new { success = false, msg = "Bid details updation failed." });
            }
        }
        [HttpGet]
        public ActionResult GetDeliveryInvoices(long Id)
        {
            tbl_PurchaseOrders _PurchaseOrders = db.tbl_PurchaseOrders.Where(a => a.Id == Id).FirstOrDefault();
            //ViewBag.CampusId = new SelectList(db.CampusMasters.Where(a => a.Active == true), "campusId", "CampusName");
            return PartialView("_DeliveryInvoiceDetails", _PurchaseOrders);
        }
        [HttpPost]
        public ActionResult SaveDeliveryInvoiceDetails(long Id, string DelExpectedDt, string InvoiceTerms)
        {
            tbl_PurchaseOrders _PurchaseOrders = db.tbl_PurchaseOrders.Where(a => a.Id == Id).FirstOrDefault();
            try
            {
                //var campusId = Session["Campus_Id"] == null ? 0 : Convert.ToDecimal(Session["Campus_Id"]);
                DateTime DeliveryExpectedDt = DateTime.ParseExact(DelExpectedDt.ToString(), "dd\\/MM\\/yyyy", CultureInfo.InvariantCulture);
                _PurchaseOrders.InvoiceTerms = InvoiceTerms;
                _PurchaseOrders.DeliveryExpectedDt = DeliveryExpectedDt;
                _PurchaseOrders.InvoiceReceived = true;
                //_PurchaseOrders.InvoiceCampusId = CampusId;
                // UpdateModel(_PurchaseOrders);
                db.SaveChanges();
                return Json(new { success = true, msg = "Delivery invoice details has been saved successfully." });
            }
            catch
            {
                return Json(new { success = false, msg = "Delivery invoice details updation failed." });
            }
        }

        //generate purchase order for issue
        public ActionResult IndentProcurement(int IssueId,int? IsJobCard)
        {
            InventoryIssuedMaster _issueMaster = db.InventoryIssuedMasters.FirstOrDefault(a => a.Id == IssueId);
            List<InventoryIssuedDetail> _IndDet =objStore.GetIssueDetailsByIssueId(_issueMaster.Id);
            //==> Get Indent Ref Number
            string IndentRefNo = string.Empty;

            if (IsJobCard.HasValue)
            {
                IndentRefNo = db.tbl_jobcard.FirstOrDefault(a => a.JobCardNo.Trim().ToUpper() == _issueMaster.IndentRefNo.Trim().ToUpper()).JobCardNo;
            }
            else if (!string.IsNullOrEmpty(_issueMaster.IndentRefNo))
                IndentRefNo = db.tbl_inv_indents.FirstOrDefault(a => a.IndRefNo.Trim().ToUpper() == _issueMaster.IndentRefNo.Trim().ToUpper()).IndRefNo;
            else
                IndentRefNo = _issueMaster.IssueNo;
            
            // add indent to purchase order master
            tbl_PurchaseOrders objPOrders = new tbl_PurchaseOrders();
            //objPOrders.CampusId = (decimal)_issueMaster.CampusId;
            objPOrders.OrderDt = DateTime.Now;
            objPOrders.ExpectedDt = DateTime.Now;
            objPOrders.Source = IndentRefNo;
            objPOrders.Total = (decimal)_IndDet.Sum(a => a.IssuedQty);
            objPOrders.CreatedBy = objcore.GetUserNameByUserId((Guid)_issueMaster.IssueUserId);
            ViewBag.IsEdit = "0";

            // add indent details to purchase order details 
            int PODetId = 0;
            List<tbl_PurchaseOrderItemDetails> _POrderDetailsList = new List<tbl_PurchaseOrderItemDetails>();
            foreach (InventoryIssuedDetail item in _IndDet)
            {
                if (item.Status.ToUpper() == "AVAILABLE" || item.Status.ToUpper() == "TRANSFER")
                {
                    continue;
                }
                PODetId++;
                double _itemAmt = (double)db.InventoryItemsMasters.SingleOrDefault(a => a.ID == item.ItemId).Amount;
                _POrderDetailsList.Add(new tbl_PurchaseOrderItemDetails
                {
                    Id = PODetId,
                    ItemId = (int)item.ItemId,
                    Qty = Convert.ToDouble(item.IssuedQty),
                    Description = "",
                    ScheduleDt = DateTime.Now,
                    UnitPrice = _itemAmt,
                    Taxes = 0,
                    SubTotal = (item.IssuedQty * _itemAmt)
                });
            }
            ViewBag.SupplierId = new SelectList(db.tbl_SuppliersMaster, "SupplierId", "SupplierName");
            Session["PurchaseOrdersList"] = _POrderDetailsList;
            return View(new InventoryModel.PurchaseOrder(objPOrders, _POrderDetailsList));
        }
     
        public ActionResult GetIndentDetails(int Id)
        {
            tbl_PurchaseOrders _PurchaseOrders = db.tbl_PurchaseOrders.Where(a => a.Id == Id).FirstOrDefault();
            tbl_inv_indents _indent = db.tbl_inv_indents.SingleOrDefault(a => a.IndRefNo == _PurchaseOrders.Source);
            if (_indent != null)
            {
                return RedirectToAction("ViewIndent", "Indent", new { Area = "Inventory", Id = _indent.Id });
            }
            return RedirectToAction("Index", "PurchaseOrders", new { Area = "Inventory" });
        }
        public ActionResult GetIndentDetailsByIndentRefNo(string IndentRefNo)
        {
            tbl_inv_indents _indent = db.tbl_inv_indents.SingleOrDefault(a => a.IndRefNo.Trim().ToUpper() == IndentRefNo.Trim().ToUpper());
            if (_indent != null)
            {
                return RedirectToAction("ViewIndent", "Indent", new { Area = "Inventory", Id = _indent.Id });
            }
            return RedirectToAction("Index", "PurchaseOrders", new { Area = "Inventory" });
        }
        public ActionResult CheckReceiveStockFromPurchaseOrder(long Id)
        {
            bool isCorrect = checkPurchaseOrderStock(Id);
            if (isCorrect == true)
                return Json(new { success = false, Id = 0, msg = "Purchase order has already issued for stock." });
            else
                return Json(new { success = true, Id = 0, msg = "" });
        }

        private bool checkPurchaseOrderStock(long Id)
        {
            tbl_PurchaseOrders _PurchaseOrders = db.tbl_PurchaseOrders.Where(a => a.Id == Id).FirstOrDefault();
            if (db.InventoryStockRegisters.Where(a => a.SourceDoc.Trim().ToUpper() == _PurchaseOrders.RefNo.Trim().ToUpper()).Any())
                return true;
            else
                return false;
        }
        public List<tbl_PurchaseOrderItemDetails> GetPurchaseOrderDetailsByPOId(long POId)
        {
            try
            {
                List<tbl_PurchaseOrderItemDetails> _OrderList = db.tbl_PurchaseOrderItemDetails.Where(a => a.POId == POId).ToList();
                if (_OrderList.Count() != 0 || _OrderList != null)
                    return _OrderList;
                else
                    return null;
            }
            catch (Exception)
            {

                return null;
            }
        }  
        private List<tbl_PurchaseOrderItemDetails> GetPurchaseOrderDetailsByItemAndCampus(int ItemId, long? POId, long SupplierId)
        {
            var x = (from po in db.tbl_PurchaseOrders
                     join pod in db.tbl_PurchaseOrderItemDetails on po.Id equals pod.POId
                     where pod.ItemId == ItemId && po.SupplierId == SupplierId
                     && po.Id == POId
                     select pod).ToList();
            return x.ToList<tbl_PurchaseOrderItemDetails>();
        }
    }
}
