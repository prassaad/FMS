using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;
using System.Globalization;
using System.Text;


namespace FMS.Areas.Inventory.Controllers
{
    [CustomAuthorizationFilter]
    public class StoresController : Controller
    {
        private List<InventoryIssuedDetail> invIssue_det = new List<InventoryIssuedDetail>();
        private FMSDBEntities _dataModel = new FMSDBEntities();
        private List<InventoryStockRegDetail> Reg_det = new List<InventoryStockRegDetail>();
        private List<InventoryIssuedDetail> Issued_Reg_det = new List<InventoryIssuedDetail>();
        public core objcore = new core();
        public ActionResult Index()
        {
            return View();
        }

        #region Inward Stock
        public ActionResult Inward()
        {
            var inwardList = _dataModel.InventoryStockRegisters.Where(a => a.Active == true).OrderByDescending(a => a.ID).ToList();
            var _invStockRegisters = (from iim in _dataModel.InventoryStockRegisters
                                      join iid in _dataModel.InventoryStockRegDetails on iim.ID equals iid.StockRegId
                                      join item in _dataModel.InventoryItemsMasters on iid.ItemId equals item.ID
                                      where iim.Active == true
                                      select iim).Distinct().ToList();
            return View(_invStockRegisters);
        }
        // Inward Entry Get method
        public ActionResult InwardCreate()
        {
            ViewData["ItemsList"] = new core().LoadInvItems();
            ViewData["Units"] = new SelectList(_dataModel.InventoryUnitsMasters.Where(a => a.Active == true), "Id", "UnitsText");
            ViewData["ReceiptNo"] = DateTime.Now.ToString("ddMMyyhhmmss").ToString();
            return View("InwardCreate");
        }
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult InwardCreate(FormCollection frm)
        {
            try
            {
                List<InventoryStockRegDetail> RegDetList = (List<InventoryStockRegDetail>)Session["List"];
                if (RegDetList == null)
                    return Json(new { success = false, msg = "Please check items are added.\n suggestion do logoff and login then try again." });
                string ReceiptNo = frm["txtreceiptno"] == null ? "" : frm["txtreceiptno"].ToString();
                DateTime EntryDt = DateTime.Now;
                string recdate = frm["entry_dt"].ToString();
                EntryDt = DateTime.Parse(recdate.ToString());
                string Remark = frm["txtremark"] == null ? "" : frm["txtremark"].ToString();
                string IsFromPOrder = frm["hdnIsFromPOrder"].ToString();
                // Insert Stock Register Master 
                InventoryStockRegister objStockReg = new InventoryStockRegister();

                objStockReg.EntryDt = EntryDt;
                objStockReg.ReceiptNO = ReceiptNo;
                objStockReg.Remarks = Remark;
                // objStockReg.CampusId = CampusId;
                objStockReg.ReceivedBy = User.Identity.Name;
                objStockReg.ReceivedDt = EntryDt;
                objStockReg.Active = true;
                if (IsFromPOrder == "1")
                {
                    string SourceRefNo = frm["hdnSource"].ToString();
                    objStockReg.SourceDoc = SourceRefNo;

                    // Get Purchase order Id by Reference number

                    tbl_PurchaseOrders purchaseOrder = _dataModel.tbl_PurchaseOrders.Where(a => a.RefNo.Trim() == SourceRefNo.Trim() && a.Active == true).FirstOrDefault();
                    purchaseOrder.Status = PurchaseOrderStatus.Done.ToString();
                    TryUpdateModel(purchaseOrder);
                }
                _dataModel.AddToInventoryStockRegisters(objStockReg);
                _dataModel.SaveChanges();
                // Insert Stock register details 
                foreach (InventoryStockRegDetail _RegDet in RegDetList)
                {
                    InventoryStockRegDetail objRegDet = new InventoryStockRegDetail();
                    objRegDet.StockRegId = objStockReg.ID;
                    objRegDet.Slno = _RegDet.Slno;
                    objRegDet.ItemId = _RegDet.ItemId;
                    objRegDet.ItemText = _RegDet.ItemText;
                    objRegDet.ReceQty = _RegDet.ReceQty;
                    objRegDet.Weight = _RegDet.Weight;
                    objRegDet.UnitsId = _RegDet.UnitsId;
                    objRegDet.TotalWeight = _RegDet.TotalWeight;
                    objRegDet.Rate = _RegDet.Rate;
                    objRegDet.IssuedSlod = 0;
                    objRegDet.QOH = _RegDet.ReceQty;
                    _dataModel.InventoryStockRegDetails.AddObject(objRegDet);
                    _dataModel.SaveChanges();
                }
                return Json(new { success = true, msg = "Stock has been saved sucessfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, msg = "An error occured while proccessing your request and try again." });
            }
        }
        // Add to Item list post Method
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult ItemListAdd(string ItemName, int ItemId, double Qty, int Size, double Amount, int UnitsId, long? StockId, FormCollection frm)
        {
            Reg_det = (List<InventoryStockRegDetail>)Session["List"];
            if (Reg_det == null)
                Reg_det = new List<InventoryStockRegDetail>();

            int Slno = 0;
            if (Reg_det.Count() == 0)
                Slno = 1;
            else
                Slno = (int)(Reg_det.LastOrDefault().Slno + 1);
            //Reg_det.Add(new InventoryStockRegDetail { Slno = Slno, ItemId = ItemId, ItemText = ItemName, UnitsId = UnitsId, ReceQty = Qty });
            Session["List"] = Reg_det;
            long stock_reg_id = StockId == null ? 0 : Convert.ToInt64(StockId);
            ViewData["IsEdit"] = "0";
            if (stock_reg_id != 0)
            {
                InventoryStockRegDetail objRegDet = new InventoryStockRegDetail();
                objRegDet.ItemId = ItemId;
                objRegDet.ItemText = ItemName;
                objRegDet.StockRegId = stock_reg_id;
                objRegDet.ReceQty = Qty;
                objRegDet.UnitsId = UnitsId;
                objRegDet.Weight = Size;
                objRegDet.Rate = Amount;
                objRegDet.TotalWeight = 0;
                objRegDet.Slno = Slno;
                objRegDet.IssuedSlod = 0;
                objRegDet.QOH = Qty;
                _dataModel.InventoryStockRegDetails.AddObject(objRegDet);
                _dataModel.SaveChanges();
                long id = objRegDet.ID;
                Reg_det.Add(new InventoryStockRegDetail { Slno = Slno, ItemId = ItemId, ItemText = ItemName, UnitsId = UnitsId, ReceQty = Qty, ID = id , Weight = Size , Rate = Amount  });
                ViewData["IsEdit"] = "1";
            }
            else
                Reg_det.Add(new InventoryStockRegDetail { Slno = Slno, ItemId = ItemId, ItemText = ItemName, UnitsId = UnitsId, ReceQty = Qty, Weight = Size, Rate = Amount });
            return PartialView("_ItemListAdd", Reg_det);
        }
        // View Stock details 
        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult GetStockDetails(long Id)
        {
            List<InventoryStockRegDetail> _RegDetail = GetStockRegListByStockId(Id);
            ViewBag.ReceiptNo = _dataModel.InventoryStockRegisters.Where(a => a.Active == true && a.ID == Id).FirstOrDefault() == null ? "" : _dataModel.InventoryStockRegisters.Where(a => a.Active == true && a.ID == Id).FirstOrDefault().ReceiptNO;
            return PartialView("_GetStockDetails", new InventoryModel.StockManagement(_RegDetail, null));
        }
        // Edit Inward Stock 
        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult InwardEdit(long Id)
        {
            InventoryStockRegister StockReg = _dataModel.InventoryStockRegisters.Where(a => a.Active == true && a.ID == Id).FirstOrDefault();
            ViewData["ItemsList"] = new core().LoadInvItems(); 
            ViewData["Units"] = new SelectList(_dataModel.InventoryUnitsMasters.Where(a => a.Active == true), "Id", "UnitsText");
            List<InventoryStockRegDetail> _StockDet = GetStockRegListByStockId(Id);
            Session["List"] = (List<InventoryStockRegDetail>)_StockDet;
            return View("InwardEdit", new InventoryModel.StockManagement(StockReg, _StockDet));
        }
        // Edit Inward Post
        public ActionResult InwardEdit(FormCollection frm)
        {
            try
            {
                string ReceiptNo = frm["txtreceiptno"] == null ? "" : frm["txtreceiptno"].ToString();
                DateTime EntryDt = DateTime.Now;
                string recdate = frm["entry_dt"].ToString();
                EntryDt = Convert.ToDateTime(recdate);
                string Remark = frm["txtremark"] == null ? "" : frm["txtremark"].ToString();
                long Id = frm["hdnStockId"] == null ? 0 : Convert.ToInt64(frm["hdnStockId"].ToString());
                // Update Stock Register Master 
                InventoryStockRegister objStockReg = _dataModel.InventoryStockRegisters.Where(a => a.Active == true && a.ID == Id).FirstOrDefault();
                objStockReg.EntryDt = EntryDt;
                objStockReg.ReceiptNO = ReceiptNo;
                objStockReg.Remarks = Remark;
                _dataModel.SaveChanges();
                return Json(new { success = true, msg = "Stock has been saved sucessfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, msg = "An error occured while processing your request." });
            }
        }
        // Delete Inward Stock 
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult InwardDelete(long Id)
        {
            try
            {
                InventoryStockRegister StockReg = _dataModel.InventoryStockRegisters.Where(a => a.Active == true && a.ID == Id).FirstOrDefault();
                StockReg.Active = false;
                // Get list of stock reg details
                List<InventoryStockRegDetail> StockRegDetList = GetStockRegListByStockId(Id);

                foreach (InventoryStockRegDetail _StockReg in StockRegDetList)
                {
                    if (_StockReg.IssuedSlod != null)
                        return Json(new { success = false, msg = "Sorry! Stock has been issued from this receipt no. Deletion has been cancelled.\n suggestion remove the issued stock from received stock to delete." });
                    // Delete here 
                    _dataModel.InventoryStockRegDetails.DeleteObject(_StockReg);
                }
                // Save changes
                _dataModel.SaveChanges();
                return Json(new { success = true, msg = "Stock has been deleted sucessfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, msg = "Please select the item to delete from list" });
            }
        }

        // Edit Stock Details 
        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult EditStockDetails(long Id)
        {
            var StockDet = _dataModel.InventoryStockRegDetails.Where(a => a.ID == Id).ToList();
            return PartialView("_EditStockDetails", new InventoryModel.StockManagement(StockDet, null));
        }
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult EditStockDetails(long Id, FormCollection frm)
        {
            var StockDet = _dataModel.InventoryStockRegDetails.Where(a => a.ID == Id).FirstOrDefault();
            double Weight = (frm["weight"] == "" || frm["weight"] == null) ? 0 : Convert.ToDouble(frm["weight"].ToString());
            double Rec_Qty = (frm["rece_qty"] == null || frm["rece_qty"] == "") ? 0 : Convert.ToDouble(frm["rece_qty"].ToString());
            double Rate = (frm["rate"] == "" || frm["rate"] == null) ? 0 : Convert.ToDouble(frm["rate"].ToString());
            //double IssueSold = frm["issued_slod"] == null ? 0 : Convert.ToDouble(frm["issued_slod"]);
            // update here 
            StockDet.Weight = Weight;
            StockDet.ReceQty = Rec_Qty + (StockDet.IssuedSlod == null ? 0 : StockDet.IssuedSlod);
            StockDet.QOH = StockDet.ReceQty - (StockDet.IssuedSlod == null ? 0 : StockDet.IssuedSlod);
            StockDet.Rate = Rate;
            //StockDet.issued_slod = IssueSold;
            StockDet.TotalWeight = (Weight * StockDet.ReceQty);
            _dataModel.SaveChanges();

            return RedirectToAction("InwardEdit", "Stores", new { Area = "Inventory", Id = StockDet.StockRegId });
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult DeleteStockDetails(string Id)
        {
            string IsEdit = Id.Split('_')[1];
            long StockDetId = Convert.ToInt64(Id.Split('_')[0]);
            if (IsEdit == "1")
            {
                InventoryStockRegDetail StockRegDet = _dataModel.InventoryStockRegDetails.Where(a => a.ID == StockDetId).FirstOrDefault();
                long RegId = (long)StockRegDet.StockRegId;
                if (StockRegDet.IssuedSlod != null && StockRegDet.IssuedSlod != 0)
                {
                    ViewBag.ErrorMessage = "Sorry! Stock has been issued from this receipt number. Deletion has cancelled.suggestion remove the issued stock from this inward to delete.";
                    return RedirectToAction("InwardEdit", "Stores", new { Area = "Inventory", Id = RegId });
                }
                _dataModel.InventoryStockRegDetails.DeleteObject(StockRegDet);
                _dataModel.SaveChanges();
                return RedirectToAction("InwardEdit", "Stores", new { Area = "Inventory", Id = RegId });
            }
            else
            {
                Reg_det = (List<InventoryStockRegDetail>)Session["List"];
                Reg_det.RemoveAll(a => a.Slno == Convert.ToInt32(Id.Split('_')[0]));

                List<InventoryStockRegDetail> _TempList = new List<InventoryStockRegDetail>();
                int Slno = 1;
                foreach (InventoryStockRegDetail _Stock in Reg_det)
                {
                    _TempList.Add(new InventoryStockRegDetail { Slno = Slno, ItemId = _Stock.ItemId, ItemText = _Stock.ItemText, ReceQty = _Stock.ReceQty, UnitsId = _Stock.ItemId });
                    Slno += 1;
                }
                Reg_det = _TempList;
                ViewData["IsEdit"] = "0";
                return PartialView("_ItemListAdd", Reg_det);
            }

        }
        // Receive products from purchase orders
        public ActionResult ReceiveProducts(int Id)
        {
            tbl_PurchaseOrders _PurchaseOrders = _dataModel.tbl_PurchaseOrders.Where(a => a.Id == Id).FirstOrDefault();
            List<tbl_PurchaseOrderItemDetails> _POrderDetailsList = GetPurchaseOrderDetailsByPOId(_PurchaseOrders.Id);
            string ReceiptNo = DateTime.Now.ToString("ddMMyyhhmmss").ToString();
            // insert into Inventory Stock Register Master
            InventoryStockRegister objStockReg = new InventoryStockRegister();
            objStockReg.EntryDt = DateTime.Now;
            objStockReg.ReceiptNO = ReceiptNo;
            objStockReg.Remarks = _PurchaseOrders.InvoiceTerms;
            // objStockReg.CampusId = (decimal)_PurchaseOrders.CampusId;
            objStockReg.ReceivedBy = User.Identity.Name;
            objStockReg.ReceivedDt = DateTime.Now;
            objStockReg.Active = true;
            objStockReg.SourceDoc = _PurchaseOrders.RefNo;
            // Insert Stock register details
            int Slno = 0;
            List<InventoryStockRegDetail> _StockRegDetailsList = new List<InventoryStockRegDetail>();
            foreach (tbl_PurchaseOrderItemDetails item in _POrderDetailsList)
            {
                if (_POrderDetailsList.Count() == 1) Slno = 1;
                else Slno = Slno + 1;
                InventoryItemsMaster _ItemMaster = _dataModel.InventoryItemsMasters.Where(a => a.ID == item.ItemId).FirstOrDefault();
                double IssuedQty = (double)item.Qty;
                InventoryStockRegDetail objRegDet = new InventoryStockRegDetail();
                objRegDet.StockRegId = objStockReg.ID;
                objRegDet.Slno = Slno;
                objRegDet.ItemId = _ItemMaster.ID;
                objRegDet.ItemText = _ItemMaster.ItemText;
                objRegDet.ReceQty = IssuedQty;
                objRegDet.Weight = 0;
                objRegDet.UnitsId = _ItemMaster.UnitId;
                objRegDet.TotalWeight = 0;
                objRegDet.Rate = 0;
                _StockRegDetailsList.Add(objRegDet);
            }
            // set Indent Status to In progress
            // Indent.Status = IndentItemStatus.InProgress.ToString();
            //  _dataModel.SaveChanges();
            ViewData["ItemsList"] = new SelectList(_dataModel.InventoryItemsMasters.Where(a => a.Active == true), "ID", "ItemText");
            ViewData["Units"] = new SelectList(_dataModel.InventoryUnitsMasters.Where(a => a.Active == true), "Id", "UnitsText");
            //ViewData["CampusId"] = new SelectList(_dataModel.CampusMasters.Where(a => a.Active == true), "campusId", "CampusName", objStockReg.CampusId);
            Session["List"] = (List<InventoryStockRegDetail>)_StockRegDetailsList;
            return View("ReceiveProducts", new InventoryModel.StockManagement(objStockReg, _StockRegDetailsList));
        }
        public ActionResult GetPurchaseOrderDetailsByRefNo(string RefNo)
        {
            tbl_PurchaseOrders _purchaseOrder = _dataModel.tbl_PurchaseOrders.SingleOrDefault(a => a.RefNo.Trim().ToUpper() == RefNo.Trim().ToUpper());
            if (_purchaseOrder != null)
            {
                return RedirectToAction("ViewPurchaseOrders", "PurchaseOrders", new { Area = "Inventory", Id = _purchaseOrder.Id });
            }
            return RedirectToAction("Inward", "Stores", new { Area = "Inventory" });
        }
        #region Inward Stock - Custom Methods
        // Auto complete for Item Search
        //public string getAjaxResult(string f, string q)
        //{
        //    f = f == "undefined" ? "item_text" : f;
        //    StringBuilder str = new StringBuilder();


        //    var ItemsList = (from a in _dataModel.InventoryItemsMasters
        //                        .Where(a => a.active == true)
        //                        .Where<InventoryItemsMaster>(f, q, WhereOperation.Contains)
        //                     select new { a.item_text, a.id });


        //    foreach (var t in ItemsList)
        //    {
        //        str.Append(string.Format("{0}| {1} \r\n", t.item_text, t.id)).ToString();
        //    }
        //    return str.ToString();
        //}
        // Get Inward Stock Count by StockId
        public double InwardStockDetailsByStockId(long Id)
        {
            double TotalStock = 0;
            TotalStock = (double)_dataModel.InventoryStockRegDetails.Where(a => a.StockRegId == Id).ToList().Sum(a => a.ReceQty);
            return TotalStock;
        }
        // Get Inward Stock Count by StockId
        public double AvailStockByStockId(long Id)
        {
            double TotalStock = 0;
            TotalStock = (double)_dataModel.InventoryStockRegDetails.Where(a => a.StockRegId == Id).ToList().Sum(a => a.ReceQty - (a.IssuedSlod == null ? 0 : a.IssuedSlod));
            return TotalStock;
        }
        // Get Inward Stock Details list  by StockId
        public List<InventoryStockRegDetail> GetStockRegListByStockId(long StockRegId)
        {
            List<InventoryStockRegDetail> RegList = _dataModel.InventoryStockRegDetails.Where(a => a.StockRegId == StockRegId).ToList();
            return RegList;
        }

        //check Invoice no if already exists
        public int CheckInvoiceNo(string id)
        {
            try
            {
                InventoryStockRegister _Stock = _dataModel.InventoryStockRegisters.Where(a => a.Active == true && a.ReceiptNO == id.Trim().ToString()).FirstOrDefault();
                if (_Stock != null)
                    return 1;
                else
                    return 0;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }
        public string GetStockRegisterList(long Id)
        {
            List<InventoryStockRegDetail> list = null;
            string ItemList = string.Empty;
            try
            {
                list = _dataModel.InventoryStockRegDetails.Where(a => a.StockRegId == Id).ToList();
                foreach (InventoryStockRegDetail _RegList in list)
                {
                    ItemList = ItemList + "," + _RegList.ItemText;
                }
                ItemList = ItemList.Substring(1);
                return ItemList;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public List<tbl_PurchaseOrderItemDetails> GetPurchaseOrderDetailsByPOId(long POId)
        {
            try
            {
                List<tbl_PurchaseOrderItemDetails> _OrderList = _dataModel.tbl_PurchaseOrderItemDetails.Where(a => a.POId == POId).ToList();
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
        public JsonResult LoadUOMByItem(long ItemId)
        {
            var UOMDet = _dataModel.InventoryItemsMasters.Where(a => a.Active == true && a.ID == ItemId).Select(a => new { a.InventoryUnitsMaster.Id, a.InventoryUnitsMaster.UnitsText }).ToList();
            return Json(UOMDet, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetItemDetailsByItem(long ItemId)
        {
            var ItemDet = _dataModel.InventoryItemsMasters.Where(a => a.Active == true && a.ID == ItemId).Select(a => new { a.ID, a.Size, a.Amount, a.ItemText, a.OpeningBalance }).ToList();
            return Json(ItemDet, JsonRequestBehavior.AllowGet);
        }

        #endregion
        #endregion

        #region Outward Stock
        public ActionResult Outward()
        {
            var list = _dataModel.InventoryIssuedMasters.Where(a => a.Active == true).OrderBy(a => a.Id).ToList();
            var _invIssueMasters = (from iim in _dataModel.InventoryIssuedMasters
                                    join iid in _dataModel.InventoryIssuedDetails on iim.IssueNo equals iid.IssueNo
                                    join item in _dataModel.InventoryItemsMasters on iid.ItemId equals item.ID
                                    where iim.Active == true
                                    select iim).Distinct().ToList();
            return View(_invIssueMasters);
        }

        public ActionResult OutwardCreate()
        {
            ViewData["ItemsList"] = new core().LoadInvItems();
            ViewData["Units"] = new SelectList(_dataModel.InventoryUnitsMasters.Where(a => a.Active == true), "Id", "UnitsText");
            List<SelectListItem> empList = new List<SelectListItem>();
            foreach (var item in _dataModel.tbl_employees.Where(a => a.Status == true).ToList())
                empList.Add(new SelectListItem { Text = item.FirstName + " " + item.LastName, Value = item.ID.ToString() });
            ViewData["Employees"] = new SelectList(empList, "Value", "Text");

            ViewBag.VehicleID = _dataModel.tbl_vehicles.Where(a => a.Status == true).Select(a => new { a.ID, a.VehicleRegNum }).ToDictionary(a => a.ID, a => a.VehicleRegNum.ToUpper());
            ViewData["IssueNo"] = DateTime.Now.ToString("ddMMyyhhmmss").ToString();
            return View();
        }
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult OutwardCreate(string VehicleId, FormCollection frm)
        {
            try
            {
                List<InventoryIssuedDetail> IssuedDetList = (List<InventoryIssuedDetail>)Session["IssuedList"];
                if (IssuedDetList == null)
                    return Json(new { success = false, IssuedId = 0, msg = "Please check items are added.\n suggestion do logoff and login then try again." });

                string IssueNo = frm["txtIssueNo"] == null ? "" : frm["txtIssueNo"];
                DateTime IssuedDt = DateTime.Now;
                string Issdate = frm["issued_dt"].ToString();
                IssuedDt = DateTime.ParseExact(Issdate.ToString(), "dd\\/MM\\/yyyy", CultureInfo.InvariantCulture);
                string Remark = frm["txtremark"] == null ? "" : frm["txtremark"].ToString();
                long VId = string.IsNullOrEmpty(VehicleId) ? 0 : Convert.ToInt64(VehicleId);
                long emp_id = (frm["employee"] == null || frm["employee"] == "") ? 0 : Convert.ToInt64(frm["employee"]);

                // Insert Issued Master
                InventoryIssuedMaster objIssuedMaster = new InventoryIssuedMaster();
                objIssuedMaster.IssueNo = IssueNo;
                objIssuedMaster.IssueDt = IssuedDt;
                objIssuedMaster.IssueUserId = objcore.GetUserIdByUsername(User.Identity.Name);
                if (VId != 0)
                    objIssuedMaster.VehicleId = VId;
                objIssuedMaster.OtherDetails = Remark;

                if (emp_id != 0)
                    objIssuedMaster.IssuedEmpId = int.Parse(emp_id.ToString());

                objIssuedMaster.IssueQty = IssuedDetList.Sum(a => a.IssuedQty);
                objIssuedMaster.Active = true;
                objIssuedMaster.Status = IssuedItemStatus.Edit.ToString();
                _dataModel.InventoryIssuedMasters.AddObject(objIssuedMaster);
                _dataModel.SaveChanges();

                // Insert issued Details list 
                List<InventoryIssuedDetail> IssuedItemsList = new List<InventoryIssuedDetail>();
                foreach (InventoryIssuedDetail _IssuedDet in IssuedDetList)
                {
                    InventoryIssuedDetail objIssuedDetails = new InventoryIssuedDetail();
                    objIssuedDetails.ReceiptNo = _IssuedDet.ReceiptNo;
                    objIssuedDetails.IssueNo = objIssuedMaster.IssueNo;
                    objIssuedDetails.IssuedDt = DateTime.Now;
                    objIssuedDetails.IssuedQty = _IssuedDet.IssuedQty;
                    objIssuedDetails.ItemId = _IssuedDet.ItemId;
                    objIssuedDetails.ItemText = _IssuedDet.ItemText;
                    objIssuedDetails.Slno = _IssuedDet.Slno;
                    objIssuedDetails.UserName = User.Identity.Name;
                    objIssuedDetails.Status = _IssuedDet.Status;
                    IssuedItemsList.Add(objIssuedDetails);

                    // Stock detection from Issued stock  
                    List<InventoryStockRegDetail> _StockDetList = GetStockRegList(_IssuedDet.ReceiptNo.ToString(), (int)_IssuedDet.ItemId).Where(a => a.ID == _IssuedDet.Id).ToList();
                    foreach (InventoryStockRegDetail _StockRegDet in _StockDetList)
                    {
                        if (_StockRegDet.QOH != 0)
                        {
                            var StockRegDetails = _dataModel.InventoryStockRegDetails.Where(a => a.ID == _StockRegDet.ID).FirstOrDefault();
                            StockRegDetails.IssuedSlod = (StockRegDetails.IssuedSlod == null ? 0 : StockRegDetails.IssuedSlod) + _IssuedDet.IssuedQty;
                            StockRegDetails.QOH = StockRegDetails.ReceQty - StockRegDetails.IssuedSlod;
                            TryUpdateModel(StockRegDetails);
                            _dataModel.SaveChanges();
                        }
                    }
                }
                foreach (var objInvIssueDet in IssuedItemsList)
                {
                    _dataModel.InventoryIssuedDetails.AddObject(objInvIssueDet);
                    _dataModel.SaveChanges();
                }
                var emp = _dataModel.tbl_drivers.FirstOrDefault(e => e.ID == emp_id);
                return Json(new { success = true, IssuedId = objIssuedMaster.Id, msg = "Issued stock to the employee " + emp.FirstName + " " + emp.LastName + " and Issued No " + IssueNo + "." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, IssuedId = 0, msg = "An error occured while processing your request, Error is " + ex.Message.ToString() });
            }
        }
        public ActionResult OutwardEdit(long Id)
        {
            InventoryIssuedMaster _Issued = _dataModel.InventoryIssuedMasters.Where(a => a.Active == true && a.Id == Id).FirstOrDefault();
            // ==> check Quantity Avalability  for Each Issue Details
            CheckAvailableQuantity(_Issued.Id);
            ViewBag.VehicleID = _dataModel.tbl_vehicles.Where(a => a.Status == true).Select(a => new { a.ID, a.VehicleRegNum }).ToDictionary(a => a.ID, a => a.VehicleRegNum.ToUpper());
            ViewData["ItemsList"] = new core().LoadInvItems();
            List<SelectListItem> empList = new List<SelectListItem>();
            foreach (var item in _dataModel.tbl_employees.Where(a => a.Status == true).ToList())
            {
                empList.Add(new SelectListItem { Text = item.FirstName + " " + item.LastName, Value = item.ID.ToString() });
            }
            ViewBag.IssuedEmpId = new SelectList(empList, "Value", "Text", _Issued.IssuedEmpId);
            List<InventoryIssuedDetail> _IssuedList = GetIssuedDetailsByIssuedNo(_Issued.IssueNo);
            Session["IssuedList"] = _IssuedList;
            return View(new InventoryModel.IssuedStock(_Issued, _IssuedList));
        }
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult OutwardEdit(FormCollection frm)
        {
            try
            {
                DateTime IssuedDt = DateTime.Now;
                string IssuedDate = frm["issued_dt"].ToString();
                IssuedDt = DateTime.Parse(IssuedDate.ToString());
                string Remark = frm["txtremark"] == null ? "" : frm["txtremark"].ToString();
                long Id = frm["hdnIssuedId"] == null ? 0 : Convert.ToInt64(frm["hdnIssuedId"].ToString());
                int emp_id = (frm["IssuedEmpId"] == null || frm["IssuedEmpId"] == "") ? 0 : Convert.ToInt32(frm["IssuedEmpId"]);
                string Status = frm["status"] == null ? "" : frm["status"].ToString();
                // Update Issued master 
                InventoryIssuedMaster objIssued = _dataModel.InventoryIssuedMasters.Where(a => a.Id == Id).FirstOrDefault();
                objIssued.OtherDetails = Remark;
                objIssued.IssueDt = IssuedDt;
                if (emp_id != 0)
                    objIssued.IssuedEmpId = emp_id;
                objIssued.Status = Status;
                _dataModel.SaveChanges();
                return Json(new { success = true, IssuedId = objIssued.Id, msg = "Issued Stock has been updated sucessfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, IssuedId = 0, msg = "An error occured while processing your request, Error is " + ex.Message.ToString() });
            }
        }

        public ActionResult AddIssuedItem(long VehicleId, int IsEdit)
        {
            ViewBag.Vehicle = _dataModel.tbl_vehicles.Where(a => a.ID == VehicleId).FirstOrDefault().VehicleRegNum;
            ViewBag.VehicleId = VehicleId;
            ViewBag.ItemId = new core().LoadInvItems();
            ViewBag.IsEdit = IsEdit;
            return PartialView("_AddIssuedItem", new InventoryIssuedDetail());
        }

        public ActionResult OutwardItemListAdd(int ItemId, double Qty, long? Emp_Id, long? Issued_Id, long VehicleId, FormCollection frm)
        {
            InventoryItemsMaster _itemMaster = _dataModel.InventoryItemsMasters.Single(a => a.ID == ItemId);
            string ItemName = _itemMaster.ItemText;
            invIssue_det = (List<InventoryIssuedDetail>)Session["IssuedList"];
            string ProductStatus = string.Empty;
            if (invIssue_det == null)
            {
                invIssue_det = new List<InventoryIssuedDetail>();
                List<GeneralClassFeilds> _StockRegList = GetStockRegList("", ItemId, VehicleId);
                double AvailQty = Convert.ToDouble(_StockRegList.Sum(a => a.ReceQty) - _StockRegList.Sum(a => a.IssuedSlod));
                if (Issued_Id == 0 || Issued_Id == null)
                {
                    if (_StockRegList != null)
                    {
                        if (Qty > AvailQty)
                            ProductStatus = "WaitingForAvailability";
                        else
                            ProductStatus = "Available";
                    }
                }
                else
                {
                    if (_StockRegList != null)
                    {
                        if (Qty > AvailQty)
                            ProductStatus = "WaitingForAvailability";
                        else
                            ProductStatus = "Available";
                    }
                }
            }
            else if (invIssue_det.Count > 0)
            {
                // check items if already have on ItemId and Campus  if not Canceled or Transfer

                if (Issued_Id == 0 || Issued_Id == null)
                {
                    if (invIssue_det.Where(a => a.ItemId == ItemId).Any())
                        return Json(new { success = false, IssueId = 0, msg = "Issues items are already added for this vehicle" });
                }
                else
                {
                    List<InventoryIssuedDetail> _CurrentIssueDet = GetIssueDetailsByItemAndCampus(ItemId, VehicleId, (long)Issued_Id);
                    if (_CurrentIssueDet.Count > 0)
                        return Json(new { success = false, IssueId = Issued_Id, msg = "Issues items are already added for this vehicle" });
                }


                if (Issued_Id == 0 || Issued_Id == null)
                {
                    double CurrentIssuedQty = 0, CurrentAvaiQty = 0;
                    foreach (var Item in invIssue_det)
                    {
                        if (Item.ItemId == ItemId)
                        {
                            //Issued Quantity
                            CurrentIssuedQty += (double)Item.IssuedQty;
                            List<GeneralClassFeilds> _StockList = GetStockRegList(Item.ReceiptNo, ItemId, VehicleId);
                            double AvaiQty = Convert.ToDouble(_StockList.Sum(a => a.ReceQty) - _StockList.Sum(a => a.IssuedSlod));
                            CurrentAvaiQty = (AvaiQty - CurrentIssuedQty);
                        }
                    }
                    double CurrentQty = CurrentIssuedQty + Qty;
                    double AvailableQuantity = (double)GetAvailQty("", ItemId, VehicleId);
                    if (CurrentQty > AvailableQuantity) // Compare all sessions Avail Qty with Current Avail Qty.
                        ProductStatus = "WaitingForAvailability";
                    else
                        ProductStatus = "Available";
                }

            }
            long EmpId = Emp_Id == null ? 0 : Convert.ToInt64(Emp_Id);
            if (EmpId != 0)
            {
                tbl_employees EmpDet = _dataModel.tbl_employees.Where(e => e.ID == EmpId).FirstOrDefault();
            }
            // Invoice No  and divided quantity list
            List<InventoryModel.InventoryViewModel> InvoiceNoList = GetInvoiceListByItem(VehicleId, ItemId, Qty);
            long IssuedId = Issued_Id == null ? 0 : Convert.ToInt64(Issued_Id);

            int Slno = 0;
            if (invIssue_det.Count() == 0)
                Slno = 1;
            else
                Slno = (int)(invIssue_det.LastOrDefault().Slno + 1);
            if (IssuedId != 0)
            {
                string IssueNo = _dataModel.InventoryIssuedMasters.Where(a => a.Id == IssuedId).FirstOrDefault().IssueNo.ToString();
                if (InvoiceNoList.Count > 1)
                {
                    foreach (var Item in InvoiceNoList)
                    {
                        invIssue_det.Add(new InventoryIssuedDetail
                        {
                            Slno = Slno,
                            ItemId = ItemId,
                            ItemText = ItemName,
                            IssueNo = IssueNo,
                            ReceiptNo = Item._Receipt,
                            IssuedQty = Convert.ToDouble(Item._Qty),
                            Id = Item._Id,
                            Status = ProductStatus
                        });
                        if (invIssue_det.Count() == 0)
                            Slno = 1;
                        else
                            Slno = (int)(invIssue_det.LastOrDefault().Slno + 1);
                    }
                }
                else
                {
                    foreach (var Item in InvoiceNoList)
                    {
                        invIssue_det.Add(new InventoryIssuedDetail
                        {
                            Slno = Slno,
                            ItemId = ItemId,
                            ItemText = ItemName,
                            IssueNo = IssueNo,
                            ReceiptNo = Item._Receipt,
                            IssuedQty = Convert.ToDouble(Item._Qty),
                            Id = Item._Id,
                            Status = ProductStatus
                        });
                    }
                }
            }
            else
            {
                if (InvoiceNoList.Count > 1)
                {
                    foreach (var Item in InvoiceNoList)
                    {
                        invIssue_det.Add(new InventoryIssuedDetail
                        {
                            Slno = Slno,
                            ItemId = ItemId,
                            ItemText = ItemName,
                            ReceiptNo = Item._Receipt,
                            IssuedQty = Convert.ToDouble(Item._Qty),
                            Id = Item._Id,
                            Status = ProductStatus
                        });
                        if (invIssue_det.Count() == 0)
                            Slno = 1;
                        else
                            Slno = (int)(invIssue_det.LastOrDefault().Slno + 1);
                    }
                }
                else
                {
                    foreach (var Item in InvoiceNoList)
                    {
                        invIssue_det.Add(new InventoryIssuedDetail
                        {
                            Slno = Slno,
                            ItemId = ItemId,
                            ItemText = ItemName,
                            ReceiptNo = Item._Receipt,
                            IssuedQty = Convert.ToDouble(Item._Qty),
                            Id = Item._Id,
                            Status = ProductStatus
                        });
                    }
                }
            }
           
            Session["IssuedList"] = invIssue_det;
            ViewData["IsEdit"] = "0";
            if (IssuedId != 0)
            {
                // Get all Previous Inserted Quantity  and check with Available Quantity
                string IssueNo = _dataModel.InventoryIssuedMasters.Where(a => a.Id == IssuedId).FirstOrDefault().IssueNo;
                foreach (var InvItem in InvoiceNoList)
                {

                    List<InventoryIssuedDetail> _IssuedList = GetIssuedDetailsByIssuedNo(IssueNo);
                    double AlreadyIssuedQty = 0, CurrentAvailQty = 0;
                    foreach (var Item in _IssuedList)
                    {
                        if (Item.ItemId == ItemId)
                        {
                            AlreadyIssuedQty += (double)Item.IssuedQty;
                            List<GeneralClassFeilds> _StockList = GetStockRegList("", ItemId, VehicleId);
                            double AvaiQty = Convert.ToDouble(_StockList.Sum(a => a.ReceQty) - _StockList.Sum(a => a.IssuedSlod));
                            CurrentAvailQty = (AvaiQty - AlreadyIssuedQty);
                        }
                    }
                    double CurrentQty = AlreadyIssuedQty + Qty;
                    double AvailableQuantity = (double)GetAvailQty("", ItemId, VehicleId);
                    if (CurrentQty > AvailableQuantity) // Compare all sessions Avail Qty with Current Avail Qty.
                        ProductStatus = "WaitingForAvailability";
                    else
                        ProductStatus = "Available";
                    // Insert Issued details 

                    InventoryIssuedDetail objIssuedDetails = new InventoryIssuedDetail();
                    objIssuedDetails.IssueNo = IssueNo;
                    objIssuedDetails.IssuedDt = DateTime.Now;
                    objIssuedDetails.ReceiptNo = InvItem._Receipt;
                    objIssuedDetails.IssuedQty = Convert.ToDouble(Qty);
                    objIssuedDetails.ItemId = ItemId;
                    objIssuedDetails.ItemText = ItemName;
                    objIssuedDetails.Slno = Slno;
                    objIssuedDetails.UserName = User.Identity.Name;
                    objIssuedDetails.Status = ProductStatus;
                    _dataModel.InventoryIssuedDetails.AddObject(objIssuedDetails);

                    // Stock detection from Issued stock  
                    List<InventoryStockRegDetail> _StockDetList = GetStockRegList(InvItem._Receipt.ToString(), (int)ItemId).Where(a => a.ID == InvItem._Id).ToList();
                    foreach (InventoryStockRegDetail _StockRegDet in _StockDetList)
                    {
                        if (_StockRegDet.QOH != 0)
                        {
                            // _StockRegDet.IssuedSlod = (_StockRegDet.IssuedSlod == null ? 0 : _StockRegDet.IssuedSlod) + Qty;
                            // _StockRegDet.QOH = _StockRegDet.ReceQty - _StockRegDet.IssuedSlod;
                            var StockRegDetails = _dataModel.InventoryStockRegDetails.Where(a => a.ID == _StockRegDet.ID).FirstOrDefault();
                            StockRegDetails.IssuedSlod = (StockRegDetails.IssuedSlod == null ? 0 : StockRegDetails.IssuedSlod) + Qty;
                            StockRegDetails.QOH = StockRegDetails.ReceQty - StockRegDetails.IssuedSlod;
                            TryUpdateModel(StockRegDetails);
                            _dataModel.SaveChanges();
                        }
                    }
                    InventoryIssuedMaster objIssued = _dataModel.InventoryIssuedMasters.Where(a => a.Id == IssuedId).FirstOrDefault();
                    objIssued.IssueQty = invIssue_det.Sum(a => a.IssuedQty);
                }
                _dataModel.SaveChanges(); // save changes 
                ViewData["IsEdit"] = "1";
            }
            //return PartialView("_OutwardItemListAdd", invIssue_det);
            return Json(new { success = true, IssueId = IssuedId, msg = "Issue item insert successfully." });
        }

        public ActionResult LoadInvoiceNoByItem(string ItemId)
        {
            List<SelectListItem> _InvoiceNoList = GetInvoiceList(Convert.ToInt32(ItemId));
            return Json(new { success = true, msg = _InvoiceNoList });
        }

        // Edit Issued Details 
        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult EditIssuedDetails(long Id)
        {
            InventoryIssuedDetail _IssuedDet = _dataModel.InventoryIssuedDetails.Where(a => a.Id == Id).FirstOrDefault();
            InventoryIssuedMaster _Issued = _dataModel.InventoryIssuedMasters.Where(a => a.IssueNo == _IssuedDet.IssueNo).FirstOrDefault();
            decimal AvailQty = (decimal)GetAvailQty(_IssuedDet.ReceiptNo, (int)_IssuedDet.ItemId, (long)_Issued.VehicleId);
            ViewBag.AvailQty = AvailQty;
            InventoryItemsMaster _itemMaster = _dataModel.InventoryItemsMasters.Single(a => a.ID == _IssuedDet.ItemId);
            ViewBag.ItemAmount = _itemMaster.Amount;
            return PartialView("_EditIssuedDetails", _IssuedDet);
        }
        // Edit Issued details Post 
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult EditIssuedDetails(long Id, int Qty)
        {
            var IssuedDet = _dataModel.InventoryIssuedDetails.Where(a => a.Id == Id).FirstOrDefault();
            InventoryIssuedMaster _Issued = _dataModel.InventoryIssuedMasters.Where(a => a.IssueNo == IssuedDet.IssueNo).FirstOrDefault();
            double Issued_Qty = Qty;


            // update stock reg details 
            List<InventoryStockRegDetail> _StockDetList = GetStockRegList(IssuedDet.ReceiptNo, (int)IssuedDet.ItemId);
            double AvailQty = (double)_StockDetList.Sum(a => a.ReceQty - (a.IssuedSlod == null ? 0 : a.IssuedSlod));
            if (Issued_Qty > AvailQty)
            {
                long EditId = _Issued.Id;
                //return Content("<script> alert('Stock is not available for " + Issued_Qty + "');window.location.href = '/Inventory/Stores/OutwardEdit/" + EditId + "'</script>");
                return Json(new { success = false, Id = _Issued.Id, msg = "Stock is not available for " + Issued_Qty });
            }
            foreach (InventoryStockRegDetail _StockDet in _StockDetList)  // QoH Caliculation part
            {
                var StockRegDetails = _dataModel.InventoryStockRegDetails.Where(a => a.ID == _StockDet.ID).FirstOrDefault();
                double Issued_sold = StockRegDetails.IssuedSlod == null ? 0 : (double)StockRegDetails.IssuedSlod;
                StockRegDetails.IssuedSlod = (Issued_sold - IssuedDet.IssuedQty) + Issued_Qty;
                StockRegDetails.QOH = StockRegDetails.ReceQty - StockRegDetails.IssuedSlod;
                TryUpdateModel(StockRegDetails);
                _dataModel.SaveChanges();

            }
            // update here  
            IssuedDet.IssuedQty = Issued_Qty;
            // Issued Master 
            List<InventoryIssuedDetail> IssuedDetList = GetIssuedDetailsByIssuedNo(_Issued.IssueNo);
            _Issued.IssueQty = IssuedDetList.Sum(a => a.IssuedQty) /
            _dataModel.SaveChanges();
            //return RedirectToAction("OutwardEdit","Stores", new { Area="Inventory", Id = _Issued.Id });
            return Json(new { success = true, Id = _Issued.Id, msg = "Issued item details has been updated  successfully." });
        }
        // View Stock details 
        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult GetIssueDetails(long Id)
        {
            ViewBag.IssuedNo = _dataModel.InventoryIssuedMasters.Where(a => a.Active == true && a.Id == Id).FirstOrDefault() == null ? "" : _dataModel.InventoryIssuedMasters.Where(a => a.Active == true && a.Id == Id).FirstOrDefault().IssueNo;
            InventoryIssuedMaster _Issued = _dataModel.InventoryIssuedMasters.Where(a => a.Active == true && a.Id == Id).FirstOrDefault();
            string IssuedNo = _Issued.IssueNo;
            List<InventoryIssuedDetail> _IssuedDetList = GetIssuedDetailsByIssuedNo(IssuedNo);

            return PartialView("_GetIssueDetails", _IssuedDetList.OrderBy(a => a.Slno));
        }
        // Delete Inward Stock 
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult OutwardDelete(long Id)
        {
            try
            {
                InventoryIssuedMaster _Issued = _dataModel.InventoryIssuedMasters.Where(a => a.Id == Id && a.Active == true).FirstOrDefault();
                _Issued.Active = false;
                // Get list of Issued Details
                List<InventoryIssuedDetail> IssuedDetList = GetIssuedDetailsByIssuedNo(_Issued.IssueNo);
                foreach (InventoryIssuedDetail _IssuedDet in IssuedDetList)
                {
                    // update Inward stock reg details
                    List<InventoryStockRegDetail> _StockDetList = GetStockRegList(_IssuedDet.ReceiptNo.ToString(), (int)_IssuedDet.ItemId);
                    foreach (InventoryStockRegDetail _StockReg in _StockDetList)
                    {
                        // _StockReg.IssuedSlod = (_StockReg.IssuedSlod == null ? 0 : _StockReg.IssuedSlod) - _IssuedDet.IssuedQty;
                        // _StockReg.QOH = _StockReg.ReceQty - _StockReg.IssuedSlod;
                        var StockRegDetails = _dataModel.InventoryStockRegDetails.Where(a => a.ID == _StockReg.ID).FirstOrDefault();
                        StockRegDetails.IssuedSlod = (StockRegDetails.IssuedSlod == null ? 0 : StockRegDetails.IssuedSlod) - _IssuedDet.IssuedQty;
                        StockRegDetails.QOH = StockRegDetails.ReceQty - StockRegDetails.IssuedSlod;
                        TryUpdateModel(StockRegDetails);
                        _dataModel.SaveChanges();
                    }
                }
                // Delete here 
                // _dataModel.InventoryIssuedDetails.DeleteObject(IssuedDetList);
                _dataModel.SaveChanges();
                return Json(new { success = true, msg = "stock has been sucessfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, msg = "Please select the item to delete from list" });
            }
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult DeleteIssuedDetails(string Id)
        {
            string IsEdit = Id.Split('_')[1];
            long IssueDetId = Convert.ToInt64(Id.Split('_')[0]);
            if (IsEdit == "1")
            {
                InventoryIssuedDetail IssuedDet = _dataModel.InventoryIssuedDetails.Where(a => a.Id == IssueDetId).FirstOrDefault();
                string IssueNo = IssuedDet.IssueNo;
                // update Inward stock  Remove QoH Caliculation part  ---------------------------------- from  here
                List<InventoryStockRegDetail> _StockDetList = GetStockRegList(IssuedDet.ReceiptNo.ToString(), (int)IssuedDet.ItemId);
                foreach (InventoryStockRegDetail _StockReg in _StockDetList)
                {
                    var StockRegDetails = _dataModel.InventoryStockRegDetails.Where(a => a.ID == _StockReg.ID).FirstOrDefault();
                    StockRegDetails.IssuedSlod = (StockRegDetails.IssuedSlod == null ? 0 : StockRegDetails.IssuedSlod) - IssuedDet.IssuedQty;
                    StockRegDetails.QOH = StockRegDetails.ReceQty - StockRegDetails.IssuedSlod;
                    TryUpdateModel(StockRegDetails);
                    _dataModel.SaveChanges();
                }
                _dataModel.InventoryIssuedDetails.DeleteObject(IssuedDet);
                _dataModel.SaveChanges();
                // Issued Master 
                InventoryIssuedMaster _Issued = _dataModel.InventoryIssuedMasters.Where(a => a.IssueNo == IssuedDet.IssueNo).FirstOrDefault();
                List<InventoryIssuedDetail> IssuedDetList = GetIssuedDetailsByIssuedNo(_Issued.IssueNo);
                _Issued.IssueQty = IssuedDetList.Sum(a => a.IssuedQty);
                _dataModel.SaveChanges();

                long IssuedId = _dataModel.InventoryIssuedMasters.Where(a => a.IssueNo == IssueNo).FirstOrDefault().Id;
                return RedirectToAction("OutwardEdit", "Stores", new { Area = "Inventory", Id = IssuedId });
            }
            else
            {
                invIssue_det = (List<InventoryIssuedDetail>)Session["IssuedList"];

                invIssue_det.RemoveAll(a => a.Slno == IssueDetId);

                List<InventoryIssuedDetail> _TempList = new List<InventoryIssuedDetail>();
                int Slno = 1;
                foreach (InventoryIssuedDetail _Issued in invIssue_det)
                {
                    _TempList.Add(new InventoryIssuedDetail { Slno = Slno, ItemId = _Issued.ItemId, ItemText = _Issued.ItemText, ReceiptNo = _Issued.ReceiptNo, IssuedQty = _Issued.IssuedQty, Total = _Issued.Total });
                    Slno += 1;
                }
                invIssue_det = _TempList;

                ViewData["IsEdit"] = "0";
                return PartialView("_OutwardItemListAdd", invIssue_det);
            }

        }
        public ActionResult ConfirmIssuedItem(long Id)
        {
            InventoryIssuedMaster _Issued = _dataModel.InventoryIssuedMasters.Where(a => a.Id == Id).FirstOrDefault();
            try
            {
                List<InventoryIssuedDetail> _IssuedList = GetIssuedDetailsByIssuedNo(_Issued.IssueNo);
                int AvailCnt = 0; int WaitingCnt = 0;
                foreach (InventoryIssuedDetail item in _IssuedList)
                {
                    if (item.Status.ToUpper() == "AVAILABLE")
                        AvailCnt += 1;
                    else
                        WaitingCnt += 1;
                }
                if (_IssuedList.Count == AvailCnt)
                    _Issued.Status = IssuedItemStatus.ReadyToTransfer.ToString();
                else if (_IssuedList.Count == WaitingCnt)
                    _Issued.Status = IssuedItemStatus.WaitingForAvailability.ToString();
                UpdateModel(_Issued);
                _dataModel.SaveChanges();
                return Json(new { success = true, msg = "Issued Item has been confirmed successfully." });
            }
            catch
            {
                return Json(new { success = false, msg = "Issued Item confirmation failed." });
            }
        }
        public ActionResult GetItemDetails(int ItemId, long VehicleId)
        {
            InventoryItemsMaster _itemMaster = _dataModel.InventoryItemsMasters.Single(a => a.ID == ItemId);
            invIssue_det = (List<InventoryIssuedDetail>)Session["IssuedList"];

            decimal CurrentIssuedQty = 0, CurrentAvailQty = 0;
            if (invIssue_det != null)
            {
                foreach (var Item in invIssue_det)
                {
                    if (Item.ItemId == ItemId)
                    {
                        //Issued Quantity
                        CurrentIssuedQty += (decimal)Item.IssuedQty;
                        List<GeneralClassFeilds> _StockList = GetStockRegList("", ItemId, VehicleId);
                        decimal AvaiQty = Convert.ToDecimal(_StockList.Sum(a => a.ReceQty) - _StockList.Sum(a => a.IssuedSlod));
                        CurrentAvailQty = (AvaiQty - CurrentIssuedQty);
                    }
                }
            }
            if (CurrentAvailQty <= 0)
            {
                CurrentAvailQty = (decimal)GetAvailQty("", ItemId, VehicleId);
            }
            if (_itemMaster != null)
                return Json(new
                {
                    success = true,
                    ReorderLevel = _itemMaster.ReorderLevel,
                    Specification = _itemMaster.Specification,
                    Amount = _itemMaster.Amount,
                    AvailQty = CurrentAvailQty,
                }, JsonRequestBehavior.AllowGet);
            else
                return Json(new { success = true, ReorderLevel = 0, Specification = "", Amount = 0, AvailQty = 0 }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult ViewIssuedItem(int IssueId, int? IsJobCard)
        {
            InventoryIssuedMaster _Issued = _dataModel.InventoryIssuedMasters.Where(a => a.Id == IssueId).FirstOrDefault();
            // ==> check Quantity Avalability  for Each Issue Details
            CheckAvailableQuantity(IssueId);
            ViewBag.IssueId = IssueId;
            ViewBag.IssueNo = _Issued.IssueNo;
            if (_Issued.IssueUserId != null)
            {
                ViewBag.IssueCreatedBy = objcore.GetUserNameByUserId((Guid)_Issued.IssueUserId);
            }
            else
            {
                InventoryIssuedDetail _IssuedDet = _dataModel.InventoryIssuedDetails.Where(a => a.IssueNo == _Issued.IssueNo).FirstOrDefault();
                ViewBag.IssueCreatedBy = _IssuedDet.UserName;
            }
            List<InventoryIssuedDetail> _IssuedList = GetIssuedDetailsByIssuedNo(_Issued.IssueNo);
            int hasWaitForAvail = 0, hasAvailable = 0;
            foreach (InventoryIssuedDetail item in _IssuedList)
            {
                if (item.Status.ToUpper() == "WAITINGFORAVAILABILITY")
                    hasWaitForAvail += 1;
                if (item.Status.ToUpper() == "AVAILABLE")
                    hasAvailable += 1;
            }
            if (hasAvailable == _IssuedList.Count() && _Issued.Status.ToUpper() == "WAITINGFORAVAILABILITY") // if All area available change status to ready to transfer
            {
                _Issued.Status = IssuedItemStatus.ReadyToTransfer.ToString();
                _dataModel.SaveChanges();
            }
            ViewBag.hasWaitForAvail = false;
            if (hasWaitForAvail != 0 && hasAvailable != 0)
            {
                ViewBag.IssueItemStatus = 3;//==>In Ready to Transfer 
                if (_Issued.Status.ToUpper() == "WAITINGFORAVAILABILITY")
                {
                    _Issued.Status = IssuedItemStatus.ReadyToTransfer.ToString();
                    _dataModel.SaveChanges();
                }
                ViewBag.hasWaitForAvail = true;
            }
            else if (hasWaitForAvail == 0 && hasAvailable != 0)
            {
                ViewBag.IssueItemStatus = 3;//==>In Ready to Transfer 
                ViewBag.hasWaitForAvail = false;
                if (_Issued.Status.ToUpper() == "WAITINGFORAVAILABILITY")
                {
                    _Issued.Status = IssuedItemStatus.ReadyToTransfer.ToString();
                    _dataModel.SaveChanges();
                }
            }
            else if (hasAvailable == 0 && hasWaitForAvail != 0)
            {
                ViewBag.IssueItemStatus = 2;//==>In Waiting for Availability
                if (_Issued.Status.ToUpper() == "READYTOTRANSFER")
                {
                    _Issued.Status = IssuedItemStatus.WaitingForAvailability.ToString();
                    _dataModel.SaveChanges();
                }
                ViewBag.hasWaitForAvail = true;
            }
            else if (hasWaitForAvail == 0 && hasAvailable == 0 && _Issued.Status.ToUpper() == "TRANSFER")
                ViewBag.IssueItemStatus = 4;//==>In Transfer
            else if (hasWaitForAvail == 0 && hasAvailable == 0 && _Issued.Status.ToUpper() == "WAITINGFORAVAILABILITY")
            {
                ViewBag.IssueItemStatus = 2;//==>In Waiting for Availability
                ViewBag.hasWaitForAvail = true;
            }
            else if (hasWaitForAvail == 0 && hasAvailable == 0 && _Issued.Status.ToUpper() == "READYTOTRANSFER")
            {
                ViewBag.IssueItemStatus = 3;//==>In Ready to Transfer 
                ViewBag.hasWaitForAvail = false;
            }
            ViewBag.Status = _Issued.Status;
            ViewBag.IsJobCard = IsJobCard.HasValue ? IsJobCard : 0;
            return View();
        }
        public ActionResult GetIssuedItemDetails(int IssueId)
        {
            InventoryIssuedMaster _Issued = _dataModel.InventoryIssuedMasters.Where(a => a.Active == true && a.Id == IssueId).FirstOrDefault();
            List<InventoryIssuedDetail> _IssuedList = GetIssuedDetailsByIssuedNo(_Issued.IssueNo);
            return PartialView("_GetIssuedItemDetails", new InventoryModel.IssuedStock(_Issued, _IssuedList));
        }

        public ActionResult RejectIssuedItem(long IssueId)
        {
            InventoryIssuedMaster _Issued = _dataModel.InventoryIssuedMasters.Where(a => a.Id == IssueId).FirstOrDefault();
            try
            {
                _Issued.Status = IssuedItemStatus.Canceled.ToString();
                _Issued.IssueRejDt = DateTime.Now;
                _Issued.IssueRejUserId = objcore.GetUserIdByUsername(User.Identity.Name);
                UpdateModel(_Issued);
                // List<InventoryIssuedDetail> _IssuedList = GetIssuedDetailsByIssuedNo(_Issued.IssueNo);

                _dataModel.SaveChanges();
                return Json(new { success = true, IssueId = _Issued.Id, msg = "Issue item transfer has been canceled successfully." });
            }
            catch
            {
                return Json(new { success = false, IssueId = _Issued.Id, msg = "Issue item transfer cancellation failed." });
            }
        }
        public ActionResult GetIndentDetails(int IssueId)
        {
            InventoryIssuedMaster _Issued = _dataModel.InventoryIssuedMasters.Where(a => a.Id == IssueId).FirstOrDefault();
            tbl_inv_indents _indent = _dataModel.tbl_inv_indents.SingleOrDefault(a => a.IndRefNo == _Issued.IndentRefNo);
            if (_indent != null)
            {
                return RedirectToAction("ViewIndent", "Indent", new { Area = "Inventory", Id = _indent.Id });
            }
            return RedirectToAction("Outward", "Stores", new { Area = "Inventory" });
        }
        public ActionResult LoadIssueItems(long IssueId)
        {
            if (IssueId == 0 || IssueId == null)
            {
                invIssue_det = (List<InventoryIssuedDetail>)Session["IssuedList"];
                ViewData["IsEdit"] = "0";
                return PartialView("_OutwardItemListAdd", invIssue_det);
            }
            else
            {
                List<InventoryIssuedDetail> _IndDet = GetIssueDetailsByIssueId(IssueId);
                ViewData["IsEdit"] = "1";
                return PartialView("_OutwardItemListAdd", _IndDet);
            }
        }

        public List<InventoryIssuedDetail> GetIssueDetailsByIssueId(long IssueId)
        {
            InventoryIssuedMaster _Issued = _dataModel.InventoryIssuedMasters.Where(a => a.Active == true && a.Id == IssueId).FirstOrDefault();
            List<InventoryIssuedDetail> _IssuedList = GetIssuedDetailsByIssuedNo(_Issued.IssueNo);
            return _IssuedList;
        }
        public ActionResult ModifyIssuedItems(int IssueId)
        {
            InventoryIssuedMaster _Issued = _dataModel.InventoryIssuedMasters.Where(a => a.Active == true && a.Id == IssueId).FirstOrDefault();
            List<InventoryIssuedDetail> _IssuedList = GetIssuedDetailsByIssuedNo(_Issued.IssueNo);
            _IssuedList.RemoveAll(a => a.Status.ToUpper() == "TRANSFER");
            int hasAvailable = 0, hasWaitForAvail = 0;
            foreach (InventoryIssuedDetail item in _IssuedList)
            {
                if (item.Status.ToUpper() == "AVAILABLE")
                    hasAvailable += 1;
                if (item.Status.ToUpper() == "WAITINGFORAVAILABILITY")
                    hasWaitForAvail += 1;
            }
            if (hasAvailable == _IssuedList.Count() && _Issued.Status.ToUpper() == "WAITINGFORAVAILABILITY") // if All area available change status to ready to transfer
            {
                _Issued.Status = "ReadyToTransfer";
                _dataModel.SaveChanges();
            }
            if (hasAvailable == 0 && hasWaitForAvail != 0)
            {
                if (_Issued.Status.ToUpper() == "READYTOTRANSFER")
                {
                    _Issued.Status = "WaitingForAvailability";
                    _dataModel.SaveChanges();
                }
            }
            ViewBag.hasAvailable = hasAvailable;
            return PartialView("_ModifyIssuedItems", new InventoryModel.IssuedStock(_Issued, _IssuedList));
        }
        public ActionResult DCPrintPreview(int IssueId)
        {
            InventoryIssuedMaster _Issued = _dataModel.InventoryIssuedMasters.Where(a => a.Active == true && a.Id == IssueId).FirstOrDefault();
            List<InventoryIssuedDetail> _IssuedList = GetIssuedDetailsByIssuedNo(_Issued.IssueNo);
            return PartialView("_DCPrintPreview", new InventoryModel.IssuedStock(_Issued, _IssuedList));
        }
        public ActionResult AddIssueItems(long VehicleId, int IsEdit)
        {
            ViewBag.Vehicle = _dataModel.tbl_vehicles.Where(a => a.ID == VehicleId).FirstOrDefault().VehicleRegNum;
            ViewBag.hdnVehicleId = VehicleId;
            ViewBag.ItemId = new SelectList(_dataModel.InventoryItemsMasters.Where(a => a.Active == true), "ID", "ItemText");
            ViewBag.IsEdit = IsEdit;
            return PartialView("_AddIssueItems", new InventoryIssuedDetail());
        }
        [HttpPost]
        public ActionResult AddIssueItems(int ItemId, double Qty, long? Emp_Id, long? Issued_Id, long CampusId, FormCollection frm)
        {
            try
            {
                // check items if already have on ItemId and Campus  if not Canceled or Transfer
                List<InventoryIssuedDetail> _CurrentIssueDet = GetIssueDetailsByItemAndCampus(ItemId, CampusId, (long)Issued_Id);
                if (_CurrentIssueDet.Count > 0)
                {
                    if (Issued_Id == 0 || Issued_Id == null)
                        return Json(new { success = false, IssueId = 0, msg = "Issues items are already added for this Campus" });
                    else
                        return Json(new { success = false, IssueId = Issued_Id, msg = "Issues items are already added for this Campus" });

                }
                long IssuedId = Issued_Id == null ? 0 : Convert.ToInt64(Issued_Id);
                InventoryIssuedMaster _IssueMaster = _dataModel.InventoryIssuedMasters.Where(a => a.Id == IssuedId).FirstOrDefault();
                string IssuedNo = _IssueMaster.IssueNo;
                double IssueQty = (double)_IssueMaster.IssueQty;

                List<InventoryIssuedDetail> _IssuedList = GetIssuedDetailsByIssuedNo(IssuedNo);
                InventoryItemsMaster _itemMaster = _dataModel.InventoryItemsMasters.Single(a => a.ID == ItemId);
                string ProductStatus = string.Empty;
                double CurrentIssuedQty = 0, CurrentAvaiQty = 0;
                foreach (var Item in _IssuedList)
                {
                    if (Item.ItemId == ItemId)
                    {
                        CurrentIssuedQty += (double)Item.IssuedQty;
                        List<GeneralClassFeilds> _StockList = GetStockRegList(Item.ReceiptNo, ItemId, CampusId);
                        double AvaiQty = Convert.ToDouble(_StockList.Sum(a => a.ReceQty) - _StockList.Sum(a => a.IssuedSlod));
                        CurrentAvaiQty = (AvaiQty - CurrentIssuedQty);
                    }
                }
                double CurrentQty = CurrentIssuedQty + Qty;
                double AvailableQuantity = (double)GetAvailQty("", ItemId, CampusId);
                if (CurrentQty > AvailableQuantity) // Compare all sessions Avail Qty with Current Avail Qty.
                    ProductStatus = "WaitingForAvailability";
                else
                    ProductStatus = "Available";

                if (CurrentAvaiQty < Qty)
                    ProductStatus = "WaitingForAvailability";
                else
                    ProductStatus = "Available";
                string ItemName = _itemMaster.ItemText;
                // Invoice No  and divided quantity list
                List<InventoryModel.InventoryViewModel> InvoiceNoList = GetInvoiceListByItem(CampusId, ItemId, Qty);
                int Slno = 0;
                if (_IssuedList.Count() == 0)
                    Slno = 1;
                else
                    Slno = (int)(_IssuedList.LastOrDefault().Slno + 1);
                foreach (var InvItem in InvoiceNoList)
                {
                    // Insert Issued details 
                    InventoryIssuedDetail objIssuedDetails = new InventoryIssuedDetail();
                    objIssuedDetails.ReceiptNo = InvItem._Receipt;
                    objIssuedDetails.IssueNo = IssuedNo;
                    objIssuedDetails.IssuedDt = DateTime.Now;
                    objIssuedDetails.IssuedQty = Convert.ToDouble(InvItem._Qty);
                    objIssuedDetails.ItemId = ItemId;
                    objIssuedDetails.ItemText = ItemName;
                    objIssuedDetails.Slno = Slno;
                    objIssuedDetails.UserName = User.Identity.Name;
                    objIssuedDetails.Status = ProductStatus;
                    _dataModel.InventoryIssuedDetails.AddObject(objIssuedDetails);
                    // Stock detection from Issued stock  
                    List<InventoryStockRegDetail> _StockDetList = GetStockRegList(InvItem._Receipt, (int)ItemId).Where(a => a.ID == InvItem._Id).ToList();
                    foreach (InventoryStockRegDetail _StockRegDet in _StockDetList)
                    {
                        if (_StockRegDet.QOH != 0)
                        {
                            // _StockRegDet.IssuedSlod = (_StockRegDet.IssuedSlod == null ? 0 : _StockRegDet.IssuedSlod) + Qty;
                            // _StockRegDet.QOH = _StockRegDet.ReceQty - _StockRegDet.IssuedSlod;
                            var StockRegDetails = _dataModel.InventoryStockRegDetails.Where(a => a.ID == _StockRegDet.ID).FirstOrDefault();
                            StockRegDetails.IssuedSlod = (StockRegDetails.IssuedSlod == null ? 0 : StockRegDetails.IssuedSlod) + Qty;
                            StockRegDetails.QOH = StockRegDetails.ReceQty - StockRegDetails.IssuedSlod;
                            TryUpdateModel(StockRegDetails);
                            _dataModel.SaveChanges();
                        }
                    }
                    InventoryIssuedMaster objIssued = _dataModel.InventoryIssuedMasters.Where(a => a.Id == IssuedId).FirstOrDefault();
                    objIssued.IssueQty = invIssue_det.Sum(a => a.IssuedQty);
                }
                _dataModel.SaveChanges();
                return Json(new { success = true, IssueId = Issued_Id, msg = "Issued Item has been added successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, IssueId = 0, msg = "An error occured while adding issue item! please try again." + ex.Message });
            }
        }
        [HttpPost]
        public ActionResult DeleteIssueItem(long Id, int ItemId, long IssueId, long VehicleId)
        {
            try
            {
                //==> delete Issued Details 
                InventoryIssuedDetail IssuedDet = _dataModel.InventoryIssuedDetails.Where(a => a.Id == Id).FirstOrDefault();
                _dataModel.InventoryIssuedDetails.DeleteObject(IssuedDet);

                //==> update in Issued Master 
                double IssueQty = (double)IssuedDet.IssuedQty;
                InventoryIssuedMaster _IssueMaster = _dataModel.InventoryIssuedMasters.Where(a => a.Id == IssueId).FirstOrDefault();
                double IssuedQty = (double)_IssueMaster.IssueQty;
                _IssueMaster.IssueQty = (IssuedQty - IssueQty);
                if (_IssueMaster.IssueQty <= 0)
                    _IssueMaster.Status = "WaitingForAvailability";
                _dataModel.SaveChanges();
                return Json(new { success = true, IssueId = IssueId, msg = "Issued Item has been deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, IssueId = IssueId, msg = "An error occured while deleteing issue item! please try again." + ex.Message });
            }
        }
        [HttpPost]
        public ActionResult SaveIssueItem(long Id, int txtReqQty, long VehicleId)
        {
            try
            {
                InventoryIssuedDetail IssuedDet = _dataModel.InventoryIssuedDetails.Where(a => a.Id == Id).FirstOrDefault();
                InventoryIssuedMaster _IssueMaster = _dataModel.InventoryIssuedMasters.Where(a => a.IssueNo == IssuedDet.IssueNo).FirstOrDefault();
                List<GeneralClassFeilds> _StockDetList = GetStockRegList("", (int)IssuedDet.ItemId, (long)_IssueMaster.VehicleId);
                double AvailQty = Convert.ToDouble(_StockDetList.Sum(a => a.ReceQty) - _StockDetList.Sum(a => a.IssuedSlod));
                //==> update in Issued Master 
                double IssueQty = (double)IssuedDet.IssuedQty;
                double IssuedQty = (double)_IssueMaster.IssueQty;
                _IssueMaster.IssueQty = (IssuedQty - IssueQty) + txtReqQty;
                if (txtReqQty > AvailQty)
                {
                    IssuedDet.Status = IssuedItemStatus.WaitingForAvailability.ToString();
                }
                IssuedDet.IssuedQty = txtReqQty;
                UpdateModel<InventoryIssuedDetail>(IssuedDet);
                _dataModel.SaveChanges();
                // update Issued master table
                List<InventoryIssuedDetail> _IssuedList = GetIssuedDetailsByIssuedNo(_IssueMaster.IssueNo);
                int hasWaitForAvail = 0, hasAvailable = 0;
                foreach (InventoryIssuedDetail item in _IssuedList)      // ==> check Quantity Avalability  for Each Issue Details
                {
                    if (item.Status.ToUpper() == "WAITINGFORAVAILABILITY")
                        hasWaitForAvail += 1;
                    if (item.Status.ToUpper() == "AVAILABLE")
                        hasAvailable += 1;
                }
                if (hasAvailable == _IssuedList.Count() && _IssueMaster.Status.ToUpper() == "WAITINGFORAVAILABILITY") // if All area available change status to ready to transfer
                {
                    _IssueMaster.Status = IssuedItemStatus.ReadyToTransfer.ToString();
                    _dataModel.SaveChanges();
                }
                else if (hasWaitForAvail == 0 && hasAvailable != 0)
                {
                    if (_IssueMaster.Status.ToUpper() == "WAITINGFORAVAILABILITY")
                    {
                        _IssueMaster.Status = IssuedItemStatus.ReadyToTransfer.ToString();
                        _dataModel.SaveChanges();
                    }
                }
                else if (hasAvailable == 0 && hasWaitForAvail != 0)
                {
                    if (_IssueMaster.Status.ToUpper() == "READYTOTRANSFER")
                    {
                        _IssueMaster.Status = IssuedItemStatus.WaitingForAvailability.ToString();
                        _dataModel.SaveChanges();
                    }
                }
                return Json(new { success = true, msg = "Issued Item has been updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, msg = "An error occured while updating issue item! please try again." + ex.Message });
            }
        }
        [HttpPost]
        public ActionResult TransferIssueItems(long IssueId, long VehicleId)
        {
            try
            {
                InventoryIssuedMaster _Issued = _dataModel.InventoryIssuedMasters.Where(a => a.Active == true && a.Id == IssueId).FirstOrDefault();
                List<InventoryIssuedDetail> _IssuedList = GetIssuedDetailsByIssuedNo(_Issued.IssueNo);
                _IssuedList.RemoveAll(a => a.Status.ToUpper() == "TRANSFER");
                double IssuedQty = 0;
                foreach (InventoryIssuedDetail item in _IssuedList)
                {
                    if (item.Status.ToUpper() == "AVAILABLE")
                    {
                        List<GeneralClassFeilds> _StockDetList = null;
                        List<InventoryModel.InventoryViewModel> InvoiceNoList = GetInvoiceListByItem(0, (int)item.ItemId, (double)item.IssuedQty);

                        // Split Receipt Numbers into multiple
                        if (InvoiceNoList.Count > 1) //===> Receipt number count is greater than one
                        {
                            item.Status = "Transfer";
                            item.IssuedDt = DateTime.Now;
                            item.IssuedQty = InvoiceNoList[0]._Qty;
                            item.ReceiptNo = InvoiceNoList[0]._Receipt;
                            IssuedQty += (double)item.IssuedQty;
                            UpdateModel(item);

                            for (int i = 0; i < InvoiceNoList.Count; i++)
                            {
                                _StockDetList = GetStockRegList(InvoiceNoList[i]._Receipt, (int)item.ItemId, VehicleId);
                                if (i != 0)
                                {
                                    InventoryIssuedDetail _IssuedDet = new InventoryIssuedDetail();
                                    _IssuedDet.Status = "Transfer";
                                    _IssuedDet.IssuedDt = DateTime.Now;
                                    _IssuedDet.IssuedQty = InvoiceNoList[i]._Qty;
                                    _IssuedDet.ReceiptNo = InvoiceNoList[i]._Receipt;
                                    _IssuedDet.IssueNo = item.IssueNo;
                                    _IssuedDet.UserName = item.UserName;
                                    _IssuedDet.ItemId = item.ItemId;
                                    _IssuedDet.ItemText = item.ItemText;
                                    _IssuedDet.IssuedDt = item.IssuedDt;
                                    _IssuedDet.IssuedFor = item.IssuedFor;
                                    _IssuedDet.Status = item.Status;
                                    _IssuedDet.Slno = item.Slno;
                                    _IssuedDet.Total = item.Total;
                                    _IssuedDet.Pair = item.Pair;
                                    _dataModel.InventoryIssuedDetails.AddObject(_IssuedDet);
                                }

                                // ==> Update QOH in Stock Register details
                                foreach (GeneralClassFeilds _StockRegDet in _StockDetList)
                                {
                                    var StockRegDetails = _dataModel.InventoryStockRegDetails.Where(a => a.ID == _StockRegDet.invSrdId).FirstOrDefault();
                                    StockRegDetails.IssuedSlod = (StockRegDetails.IssuedSlod == null ? 0 : StockRegDetails.IssuedSlod) + InvoiceNoList[i]._Qty;
                                    StockRegDetails.QOH = StockRegDetails.ReceQty - StockRegDetails.IssuedSlod;
                                    TryUpdateModel(StockRegDetails);
                                    _dataModel.SaveChanges();
                                }
                            }
                        }
                        else  //===> Receipt number count is one 
                        {
                            item.Status = "Transfer";
                            item.IssuedDt = DateTime.Now;
                            item.ReceiptNo = InvoiceNoList[0]._Receipt;
                            IssuedQty += (double)item.IssuedQty;
                            UpdateModel(item);

                            _StockDetList = GetStockRegList(item.ReceiptNo, (int)item.ItemId, VehicleId);
                            // ==> Update QOH in Stock Register details
                            foreach (GeneralClassFeilds _StockRegDet in _StockDetList)
                            {
                                var StockRegDetails = _dataModel.InventoryStockRegDetails.Where(a => a.ID == _StockRegDet.invSrdId).FirstOrDefault();
                                StockRegDetails.IssuedSlod = (StockRegDetails.IssuedSlod == null ? 0 : StockRegDetails.IssuedSlod) + item.IssuedQty;
                                StockRegDetails.QOH = StockRegDetails.ReceQty - StockRegDetails.IssuedSlod;
                                TryUpdateModel(StockRegDetails);
                                _dataModel.SaveChanges();
                            }
                        }
                        _dataModel.SaveChanges();


                    }
                    else
                    {
                        item.Pair = _Issued.IssueNo;
                        UpdateModel(item);
                        _dataModel.SaveChanges();
                    }
                }
                // ==> update Inventory Master Issued Quantity 
                _Issued.IssueQty = IssuedQty;
                // check All Items are transfered or not 
                List<InventoryIssuedDetail> _IssueItemsList = GetIssuedDetailsByIssuedNo(_Issued.IssueNo);
                _IssueItemsList.RemoveAll(a => a.Status.ToUpper() == "TRANSFER");
                if (_IssueItemsList.Count == 0)//==> if  all are  transfered  1) inserts Transfer user Id and Date 2) change status to transfer in Inventory Issue Master
                {
                    _Issued.Status = IssuedItemStatus.Transfer.ToString();
                    _Issued.IssueTransUserId = objcore.GetUserIdByUsername(User.Identity.Name);
                    _Issued.IssueTransDt = DateTime.Now;

                    if (!string.IsNullOrEmpty(_Issued.IndentRefNo))
                    {
                        tbl_inv_indents indent = _dataModel.tbl_inv_indents.Where(a => a.IndRefNo.Trim() == _Issued.IndentRefNo.Trim()).SingleOrDefault();
                        indent.Status = IndentItemStatus.Received.ToString();
                    }
                }
                _dataModel.SaveChanges();

                return Json(new { success = true, msg = "Issued Item has been transfered successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, msg = "An error occured while trasfering issue items! please try again." + ex.Message });
            }
        }

        #region OutWard Issued Details
        // Auto complete for Item Search
        //public string SearchIssuedItem(string f, string q,long ? c)  
        //{
        //    f = f == "undefined" ? "item_text" : f;
        //    StringBuilder str = new StringBuilder();

        //    var ItemsList = (from a in _dataModel.InventoryItemsMaster
        //                        .Where(a => a.Active == true)
        //                        .Where<InventoryItemsMaster>(f, q, WhereOperation.Contains)
        //                     select new { a.item_text, a.id });

        //    //string Nasid = string.Empty;
        //   // if (c != 0)
        //   // {
        //   //     var customer = _dataModel.Employees.Where(a=>a.EmpID == c).FirstOrDefault();
        //    //    Nasid = customer.NASID;
        //    //}
        //    var StockItemList = (from sr in _dataModel.InventoryStockRegister
        //                         join srd in
        //                             _dataModel.InventoryStockRegDetails on sr.ID equals srd.StockRegId
        //                         where (sr.Active == true)
        //                         select srd
        //    ).Where<InventoryStockRegDetails>(f, q, WhereOperation.Contains).Distinct();

        //     //var StockItemList = _dataContext.InventoryStockRegDetails.Where(StList.Contains((long)srd.stock_reg_id)).ToList();

        //    foreach (var t in StockItemList)
        //    {
        //        str.Append(string.Format("{0}| {1} \r\n", t.item_text, t.item_id)).ToString();
        //    }
        //    //foreach (var t in ItemsList)
        //    //{
        //    //    str.Append(string.Format("{0}| {1} \r\n", t.item_text, t.id)).ToString();
        //    //}
        //    return str.ToString();
        //}

        public List<SelectListItem> GetInvoiceList(int ItemId)
        {
            List<SelectListItem> list = (from sr in _dataModel.InventoryStockRegisters
                                         join srd in _dataModel.InventoryStockRegDetails on sr.ID equals srd.StockRegId
                                         where srd.ItemId == ItemId
                                         select sr).Distinct().Select(a => new SelectListItem { Text = a.ReceiptNO, Value = a.ReceiptNO.ToString() }).ToList();
            //  string Query = "select distinct sr.invoice_no,sr.id from inventorystockregister sr inner join inventorystockregdetails srd on sr.id = srd.stock_reg_id 
            //where srd.item_id = " + ItemId + "";
            // list = _dataContext.ExecuteQuery<InventoryStockRegister>(Query).Select(a => new SelectListItem { Text = a.invoice_no, Value = a.invoice_no.ToString() }).ToList();
            return list;
        }

        public List<InventoryStockRegDetail> GetStockRegList(string ReceiptNo, int ItemId)
        {
            List<InventoryStockRegDetail> list = null;
            try
            {
                string Query = "select srd.* from inventorystockregister sr inner join inventorystockregdetails srd on sr.id = srd.StockRegId where sr.active=1 and srd.itemid = " + ItemId + " ";
                if (ReceiptNo != "")
                {
                    Query = Query + "and sr.receiptno = '" + ReceiptNo + "' ";
                }
                list = _dataModel.ExecuteStoreQuery<InventoryStockRegDetail>(Query).ToList();
                return list;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public List<GeneralClassFeilds> GetStockRegList(string ReceiptNo, int ItemId, long VehicleId)
        {
            List<GeneralClassFeilds> list = null;
            try
            {
                string Query = "select srd.ID invSrdId, isnull(srd.IssuedSlod,0) IssuedSlod,  srd.Slno, srd.ReceQty, srd.UnitsId,srd.StockRegId, sr.ReceiptNO ReceiptNo,isnull(srd.QOH,0) AS QOH from inventorystockregister sr " +
                    " inner join inventorystockregdetails srd on sr.id = srd.stockregid where sr.active=1 and srd.receqty-isnull(srd.issuedslod,0)>0 and srd.itemid = " + ItemId + " ";

                //var query = (from invSr in _dataModel.InventoryStockRegister
                //             join invSrd in _dataModel.InventoryStockRegDetails on invSr.ID equals invSrd.StockRegId
                //             where invSr.Active == true && (invSrd.ReceQty - (invSrd.IssuedSlod != null ? invSrd.IssuedSlod : 0)) > 0 && invSrd.ItemId == ItemId
                //             select new { invSrd.Slno, invSrd.ReceQty,invSrd.UnitsId, invSr.ReceiptNO, invSr.Location }).ToList();

                if (ReceiptNo != "")
                {
                    Query = Query + "and sr.receiptNo = '" + ReceiptNo + "' ";
                    //query = query.Where(s => s.ReceiptNO == ReceiptNo).ToList();
                }
                //if (VehicleId != null && VehicleId !=0)
                //{
                //    Query = Query + "and sr.vehicleId = " + VehicleId + " ";
                //    //query = query.Where(s => s.Location == Location).ToList();
                //}

                // list = query
                list = _dataModel.ExecuteStoreQuery<GeneralClassFeilds>(Query).ToList();
                return list;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        // Get issued Details list  by Issued No
        public List<InventoryIssuedDetail> GetIssuedDetailsByIssuedNo(string IssuedNo)
        {
            List<InventoryIssuedDetail> RegList = _dataModel.InventoryIssuedDetails.Where(a => a.IssueNo == IssuedNo).ToList();
            return RegList;
        }

        public List<InventoryIssuedDetail> GetIssueddetailsByInvoiceNo(string InvoiceNo)
        {
            List<InventoryIssuedDetail> IssuedDet = _dataModel.InventoryIssuedDetails.Where(a => a.ReceiptNo == InvoiceNo).ToList();
            return IssuedDet;
        }

        public Dictionary<string, double> GetInvoiceList(long CampusId, int ItemId, double Qty)
        {
            //List<string> _InvoiceList = new List<string>();
            Dictionary<string, double> _InvoiceList = new Dictionary<string, double>();
            try
            {
                List<GeneralClassFeilds> _RegDetList = new List<GeneralClassFeilds>();
                _RegDetList = GetStockRegList("", ItemId, CampusId);
                if (_RegDetList.Count() > 1)
                {
                    double TempQty = Qty;
                    foreach (GeneralClassFeilds _StockRegDet in _RegDetList)
                    {
                        string InvoiceNo = string.Empty;
                        double DifQty = 0;
                        double IssueSold = _StockRegDet.IssuedSlod == null ? 0 : (double)_StockRegDet.IssuedSlod;
                        if (TempQty != 0)
                        {
                            if (_StockRegDet.ReceQty - IssueSold >= TempQty)
                            {
                                DifQty = TempQty;
                                TempQty = (double)(Qty - Qty);
                                InvoiceNo = _StockRegDet.ReceiptNo;
                                _InvoiceList.Add(InvoiceNo, DifQty);
                            }
                            else if (_StockRegDet.ReceQty - IssueSold <= TempQty)
                            {
                                DifQty = (double)(_StockRegDet.ReceQty - IssueSold);
                                TempQty = (double)(TempQty - (_StockRegDet.ReceQty - IssueSold));
                                InvoiceNo = _StockRegDet.ReceiptNo;
                                _InvoiceList.Add(InvoiceNo, DifQty);
                            }
                        }
                    }
                    return _InvoiceList;
                }
                else
                {
                    _InvoiceList.Add(_RegDetList.FirstOrDefault().ReceiptNo, Qty);
                    return _InvoiceList;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        // Get Invoice list by Nasid and ItemId
        public List<InventoryModel.InventoryViewModel> GetInvoiceListByItem(long CampusId, int ItemId, double Qty)
        {
            List<InventoryModel.InventoryViewModel> _InvoiceList = new List<InventoryModel.InventoryViewModel>();
            try
            {
                List<GeneralClassFeilds> _RegDetList = new List<GeneralClassFeilds>();
                _RegDetList = GetStockRegList("", ItemId, CampusId);
                if (_RegDetList.Count() > 1)
                {
                    double TempQty = Qty;
                    foreach (GeneralClassFeilds _StockRegDet in _RegDetList)
                    {
                        string InvoiceNo = string.Empty;
                        double DifQty = 0;
                        double IssueSold = _StockRegDet.IssuedSlod == null ? 0 : (double)_StockRegDet.IssuedSlod;
                        if (TempQty != 0)
                        {
                            if (_StockRegDet.ReceQty - IssueSold >= TempQty)
                            {
                                DifQty = TempQty;
                                TempQty = (double)(Qty - Qty);
                                InvoiceNo = _StockRegDet.ReceiptNo;
                                _InvoiceList.Add(new InventoryModel.InventoryViewModel { _Id = _StockRegDet.invSrdId, _Receipt = InvoiceNo, _Qty = DifQty });
                            }
                            else if (_StockRegDet.ReceQty - IssueSold <= TempQty)
                            {
                                DifQty = (double)(_StockRegDet.ReceQty - IssueSold);
                                TempQty = (double)(TempQty - (_StockRegDet.ReceQty - IssueSold));
                                InvoiceNo = _StockRegDet.ReceiptNo;
                                _InvoiceList.Add(new InventoryModel.InventoryViewModel { _Id = _StockRegDet.invSrdId, _Receipt = InvoiceNo, _Qty = DifQty });
                            }
                        }
                    }
                    return _InvoiceList;
                }
                else
                {
                    _InvoiceList.Add(new InventoryModel.InventoryViewModel { _Id = _RegDetList.Count() == 0 ? 0 : _RegDetList.FirstOrDefault().invSrdId, _Receipt = _RegDetList.Count() == 0 ? "" : _RegDetList.FirstOrDefault().ReceiptNo, _Qty = Qty });
                    return _InvoiceList;
                }
            }
            catch (Exception ex)
            {
                return null;
            }

        }

        public double GetAvailQty(string receiptNo, int ItemId, long VehicleId)
        {
            double Qty = 0;
            try
            {
                if (VehicleId != 0 || VehicleId != null)
                {
                    List<GeneralClassFeilds> _StockRegList = GetStockRegList(receiptNo, ItemId, VehicleId);
                    Qty = Convert.ToDouble(_StockRegList.Sum(a => a.ReceQty) - _StockRegList.Sum(a => a.IssuedSlod));
                }
                return Qty;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }
        public string GetIssuedDetList(string IssuedNo)
        {
            List<string> list = null;
            string ItemList = string.Empty;
            try
            {
                list = _dataModel.InventoryIssuedDetails.Where(a => a.IssueNo == IssuedNo).Select(a => a.ItemText).Distinct().ToList();
                string UnqItemText = string.Empty;
                foreach (string _IssuedItem in list)
                {

                    if (string.IsNullOrEmpty(UnqItemText))
                    {
                        ItemList = ItemList + "," + _IssuedItem;
                    }
                    else
                    {
                        if (UnqItemText.Trim().ToUpper() != _IssuedItem.Trim().ToUpper())
                            ItemList = ItemList + "," + _IssuedItem;
                    }
                    UnqItemText = _IssuedItem;
                }
                ItemList = ItemList.Substring(1);
                return ItemList;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public List<InventoryIssuedDetail> GetIssueDetailsByItemAndCampus(int ItemId, long CampusId, long IssueId)
        {
            var x = (from im in _dataModel.InventoryIssuedMasters
                     join id in _dataModel.InventoryIssuedDetails on im.IssueNo equals id.IssueNo
                     where id.ItemId == ItemId && im.VehicleId == CampusId && im.Id == IssueId
                     && (id.Status.ToUpper() == "WAITINGFORAVAILABILITY" || id.Status.ToUpper() == "AVAILABLE")
                     select id).ToList();
            return x.ToList<InventoryIssuedDetail>();

        }

        public List<InventoryIssuedMaster> GetIssuedMaster(long CustId)
        {
            List<InventoryIssuedMaster> _IssuedList = null;
            try
            {
                _IssuedList = _dataModel.InventoryIssuedMasters.Where(a => a.IssuedEmpId == CustId).ToList();
                return _IssuedList;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        // ==> check Quantity Avalability  for Each Issue Details or If new Stock is available
        public void CheckAvailableQuantity(int IssueId)
        {
            InventoryIssuedMaster _Issued = _dataModel.InventoryIssuedMasters.Where(a => a.Active == true && a.Id == IssueId).FirstOrDefault();
            List<InventoryIssuedDetail> _IssuedList = GetIssuedDetailsByIssuedNo(_Issued.IssueNo);
            // ==> check Quantity Avalability  for Each Issue Details
            foreach (InventoryIssuedDetail item in _IssuedList)
            {
                double CurrentAvailQty = (double)GetAvailQty("", (int)item.ItemId, (long)_Issued.VehicleId);
                double CurrentIssueQty = (double)item.IssuedQty;
                if (item.Status.ToUpper() == "WAITINGFORAVAILABILITY" && (_Issued.Status.ToUpper() != "TRANSFER" && _Issued.Status.ToUpper() != "CANCELED"))
                {
                    if (CurrentIssueQty <= CurrentAvailQty)
                    {
                        item.Status = "Available";
                        // UpdateModel(item);
                        _dataModel.SaveChanges();
                    }
                }
            }
        }

        #endregion

        #endregion

    }
}
