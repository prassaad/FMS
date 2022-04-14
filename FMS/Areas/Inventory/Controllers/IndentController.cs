using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;
using System.Web.Security;
namespace FMS.Areas.Inventory.Controllers
{
    [CustomAuthorizationFilter]
    public class IndentController: Controller
    {
        private FMSDBEntities db;
        private List<tbl_inv_indent_details> IndentItemDet = new List<tbl_inv_indent_details>();
        public core objcore = new core();
        List<SelectListItem> PurchasesFor = new List<SelectListItem>();
        List<SelectListItem> Priorities = new List<SelectListItem>();
        List<SelectListItem> IndentTypes = new List<SelectListItem>();
        List<SelectListItem> DeliveryMethods = new List<SelectListItem>();
        StoresController objStore = new StoresController();
        public IndentController()
        {
            db = new FMSDBEntities();
            PurchasesFor.Add(new SelectListItem { Value = "Stock", Text = "Stock" });
            PurchasesFor.Add(new SelectListItem { Value = "Capital", Text = "Capital" });
            Priorities.Add(new SelectListItem { Value = "Low", Text = "Low" });
            Priorities.Add(new SelectListItem { Value = "Medium", Text = "Medium" });
            Priorities.Add(new SelectListItem { Value = "High", Text = "High" });
            IndentTypes.Add(new SelectListItem { Value = "Purchase", Text = "Purchase" });
            IndentTypes.Add(new SelectListItem { Value = "Repair", Text = "Repair" });
            DeliveryMethods.Add(new SelectListItem { Value = "Once", Text = "Once" });
            DeliveryMethods.Add(new SelectListItem { Value = "Partial", Text = "Partial" });
        }
        public ActionResult Index()
        {
            List<tbl_inv_indents> indents = db.tbl_inv_indents.OrderBy(a=>a.DtOfInd).ToList();
            return View(indents);
        }
        [HttpGet]
        public ActionResult Create()
        {
            ViewBag.DeptID = new SelectList(db.tbl_departments.Where(a => a.Status == true), "ID", "DisplayName");
            ViewBag.VehicleID = db.tbl_vehicles.Where(a => a.Status == true).Select(a => new { a.ID, a.VehicleRegNum }).ToDictionary(a => a.ID, a => a.VehicleRegNum.ToUpper());
            ViewBag.ItemsList = new SelectList(db.InventoryItemsMasters.Where(a => a.Active == true), "ID", "ItemText");
            ViewBag.PurchasesFor = new SelectList(PurchasesFor, "Value", "Text");
            ViewBag.Priority = new SelectList(Priorities, "Value", "Text");
            ViewBag.IndentType = new SelectList(IndentTypes, "Value", "Text");
            ViewBag.Delivery = new SelectList(DeliveryMethods, "Value", "Text");
            ViewBag.IndRefNo = DateTime.Now.ToString("ddMMyyhhmmss").ToString();
            return View();
        }
        [HttpPost]
        public ActionResult Create(FormCollection frm,tbl_inv_indents indent)
        {
           try
           {
              
               List<tbl_inv_indent_details> IndentRegDetList = (List<tbl_inv_indent_details>)Session["List"];
               if (IndentRegDetList == null)
                   return Json(new { success = false, IndentId = 0, msg = "Please check items are added.\n suggestion do logoff and login then try again." });

               long VehicleId = frm["VehicleId"] == null ? 0 : Convert.ToInt64(frm["VehicleId"]); 
               int DepartId = frm["DepartId"] == null ? 0 : Convert.ToInt32(frm["DepartId"]);
               DateTime DtOfInd = Convert.ToDateTime(frm["DtOfInd"].ToString());
               DateTime RequiredDt =  Convert.ToDateTime(frm["RequiredDt"].ToString());
               string IndRefNo = frm["IndRefNo"] == null ? "" : frm["IndRefNo"].ToString();
               string PurchasesFor = frm["PurchasesFor"] == null ? "" : frm["PurchasesFor"].ToString();
               string Priority = frm["Priority"] == null ? "" : frm["Priority"].ToString();
               string IndentType = frm["IndentType"] == null ? "" : frm["IndentType"].ToString();
               string AddInfo = frm["AddInfo"] == null ? "" : frm["AddInfo"].ToString();
               string Delivery = frm["Delivery"] == null ? "" : frm["Delivery"].ToString();
              // int Status =Convert.ToInt32(frm["status"]);
               MembershipUser userInfo = Membership.GetUser(User.Identity.Name);
                Guid UserId = new Guid();
                if (userInfo != null)
                {
                     UserId= objcore.GetUserIdByUsername(User.Identity.Name);
                }
                else
                    return Json(new { success = false, IndentId = 0, msg = "Your session has been expired.\n suggestion do logoff and login then try again." });
                  // Insert Indent Master 
               tbl_inv_indents objIndentReg = new tbl_inv_indents();
               objIndentReg.VehicleId = VehicleId;
               objIndentReg.DepartId = DepartId;
               objIndentReg.IndUserId = UserId;
               objIndentReg.DtOfInd = DtOfInd;
               objIndentReg.RequiredDt = RequiredDt;
               objIndentReg.PurchasesFor = PurchasesFor;
               objIndentReg.Priority = Priority;
               objIndentReg.IndentType = IndentType;
               objIndentReg.Delivery = Delivery;
               objIndentReg.AddInfo = AddInfo;
               objIndentReg.IndRefNo = IndRefNo;
               objIndentReg.Status = IndentItemStatus.Edit.ToString();
             
               db.AddTotbl_inv_indents(objIndentReg);
               db.SaveChanges();
               // Insert Indent Item details 
               foreach (tbl_inv_indent_details _RegDet in IndentRegDetList)
               {
                   tbl_inv_indent_details objIndentDet = new tbl_inv_indent_details();
                   objIndentDet.IndentId = objIndentReg.Id;
                   objIndentDet.ItemId = _RegDet.ItemId;
                   objIndentDet.QtyReq = _RegDet.QtyReq;
                   objIndentDet.ReorderLevel = _RegDet.ReorderLevel;
                   objIndentDet.LeadTime = _RegDet.LeadTime;
                   objIndentDet.Procure = _RegDet.Procure;   
                   objIndentDet.PurposeFor = _RegDet.PurposeFor;
                   objIndentDet.Specification = _RegDet.Specification;
                   db.tbl_inv_indent_details.AddObject(objIndentDet);
                   db.SaveChanges();
               }
               return Json(new { success = true,IndentId=objIndentReg.Id, msg = "Indent has been created sucessfully." });
           }
           catch (Exception ex)
           {
               return Json(new { success = false, IndentId =0, msg = "Please check the details and try again." });
           }
        }

        public ActionResult AddIndentItem(long VehicleId, int IsEdit)
        {
            ViewBag.Vehicle = db.tbl_vehicles.Where(a => a.ID == VehicleId).FirstOrDefault().VehicleRegNum;
            ViewBag.VehicleId = VehicleId;
            ViewBag.ItemId = new SelectList(db.InventoryItemsMasters.Where(a => a.Active == true), "ID", "ItemText");
            ViewBag.IsEdit = IsEdit;
            return PartialView("_AddIndentItem",new InventoryModel.IndentManagement(null, new tbl_inv_indent_details()));
        }
        // Add to Item list post Method
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult IndentItemListAdd(int ItemId, double Qty, int ReorderLevel, int LeadTime, string PurposeFor, string Specification,int AvalQty,long VehicleId, long? IndentId, FormCollection frm)
        {
            InventoryItemsMaster _itemMaster = db.InventoryItemsMasters.Single(a => a.ID == ItemId);
            string Procure = string.Empty;
            IndentItemDet = (List<tbl_inv_indent_details>)Session["List"];
            if (IndentItemDet == null)
                IndentItemDet = new List<tbl_inv_indent_details>();
            else if (IndentItemDet.Count > 0)
            {
                // check items if already have on ItemId and Campus  if not Canceled or Transfer


                if (IndentId == 0 || IndentId == null)
                {
                    if(IndentItemDet.Where(a=>a.ItemId == ItemId).Any())
                    return Json(new { success = false, IndentId = 0, msg = "Indent items are already added for this Campus" });
                }
                else
                {
                    List<tbl_inv_indent_details> _CurrentIndenteDet = GetIndentDetailsByItemAndCampus(ItemId, VehicleId);
                    if (_CurrentIndenteDet.Count > 0)
                        return Json(new { success = false, IndentId = IndentId, msg = "Indent items are already added for this vehicle" });
                }
                if (IndentId == 0 || IndentId == null)
                {
                    double CurrentIssuedQty = 0, CurrentAvaiQty = 0;
                    foreach (var Item in IndentItemDet)
                    {
                        if (Item.ItemId == ItemId)
                        {
                            //Issued Quantity
                            CurrentIssuedQty += (double)Item.QtyReq;
                            List<GeneralClassFeilds> _StockList =objStore.GetStockRegList("", ItemId, VehicleId);
                            double AvaiQty = Convert.ToDouble(_StockList.Sum(a => a.ReceQty) - _StockList.Sum(a => a.IssuedSlod));
                            CurrentAvaiQty = (AvaiQty - CurrentIssuedQty);
                        }
                    }
                    double CurrentQty = CurrentIssuedQty + Qty;
                    //if (Qty > CurrentAvaiQty)
                    //{
                    //    ViewBag.ErrorMessage = "Stock is not available for the quantity " + Qty + "";
                    //    ViewData["IsEdit"] = "0";
                    //   //return PartialView("_IndentItemListAdd", IndentItemDet);
                    //    return Json(new { success = false, IndentId = 0, msg = "Stock is not available for the quantity " + Qty + "" });
                    //}
                    // Compare all sessions Avail Qty with Current Avail Qty.
                    double AvailableQuantity = objStore.GetAvailQty("", ItemId, VehicleId);
                    if (CurrentQty > AvailableQuantity)
                    {
                        ViewBag.ErrorMessage = "Stock is not available for the quantity " + CurrentQty + "";
                        ViewData["IsEdit"] = "0";
                       // return PartialView("_IndentItemListAdd", IndentItemDet);
                       // return Json(new { success = false, IndentId = 0, msg = "Stock is not available for the quantity " + Qty + "" });
                    }
                }
            }
            // Get Stock List 
            List<GeneralClassFeilds> _StockRegList =objStore.GetStockRegList("", ItemId, VehicleId);
            double AvailQty = Convert.ToDouble(_StockRegList.Sum(a => a.ReceQty) - _StockRegList.Sum(a => a.IssuedSlod));

            if (_StockRegList != null)
            {
                if (Qty > AvailQty)
                {
                    if (IndentId == 0 || IndentId == null)
                    {
                        ViewData["IsEdit"] = "0";
                      //  return Json(new { success = false, IndentId = 0, msg = "Stock is not available for the quantity " + Qty + "" });
                    }
                    else
                    {
                        ViewData["IsEdit"] = "1";
                        ViewBag.ErrorMessage = "Stock is not available for the quantity " + Qty + "";
                        // return PartialView("_IndentItemListAdd", IndentItemDet);
                       // return Json(new { success = false, IndentId = IndentId, msg = "Stock is not available for the quantity " + Qty + "" });
                    }
                }
            }
            else
            {
                ViewBag.ErrorMessage = "Please check the details.";
                if (IndentId == 0 || IndentId == null)
                {
                    ViewData["IsEdit"] = "0";
                    //return Json(new { success = false, IndentId = 0, msg = "Please check the details." });
                }
                else
                {
                    ViewData["IsEdit"] = "1";
                    //return PartialView("_IndentItemListAdd", IndentItemDet);
                   // return Json(new { success = false, IndentId = IndentId, msg = "Please check the details." });
                }
            }
            int Id = 0;
            if (IndentItemDet.Count() == 0)
                Id = 1;
            else
                Id = (int)(IndentItemDet.LastOrDefault().Id + 1);

            if (AvalQty > 0)
                Procure = "Stock";
            else
                Procure = "Purchase";
            long Indent_Reg_Id = IndentId == null ? 0 : Convert.ToInt64(IndentId);
            IndentItemDet.Add(new tbl_inv_indent_details 
                             { 
                                 Id=Id,
                                 ItemId = ItemId, 
                                 IndentId = Indent_Reg_Id,
                                 ReorderLevel = ReorderLevel, 
                                 QtyReq = Qty, 
                                 LeadTime = LeadTime,
                                 Procure = Procure,
                                 PurposeFor = PurposeFor,
                                 Specification = Specification
                             });
            Session["List"] = IndentItemDet;
            ViewData["IsEdit"] = "0";
            if (Indent_Reg_Id != 0)
            {
                tbl_inv_indent_details objRegDet = new tbl_inv_indent_details();
                objRegDet.ItemId = ItemId;
                objRegDet.IndentId = Indent_Reg_Id;
                objRegDet.QtyReq = Qty;
                objRegDet.ReorderLevel = ReorderLevel;
                objRegDet.LeadTime = LeadTime;
                objRegDet.Procure = Procure;
                objRegDet.PurposeFor = PurposeFor;
                objRegDet.Specification = Specification;
                db.tbl_inv_indent_details.AddObject(objRegDet);
                db.SaveChanges();
                ViewData["IsEdit"] = "1";
            }
           // return PartialView("_IndentItemListAdd", IndentItemDet);
            return Json(new { success = true, IndentId = Indent_Reg_Id, msg = "Indent item insert successfully." });
        }

       

        // Edit Indent
        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult Edit(long Id)
        {
            tbl_inv_indents IndentReg = db.tbl_inv_indents.Where(a => a.Id == Id).FirstOrDefault();
            ViewBag.ItemsList = new SelectList(db.InventoryItemsMasters.Where(a => a.Active == true), "ID", "ItemText");
            ViewBag.VehicleID = db.tbl_vehicles.Where(a => a.Status == true).Select(a => new { a.ID, a.VehicleRegNum }).ToDictionary(a => a.ID, a => a.VehicleRegNum.ToUpper());
            ViewBag.DeptID = new SelectList(db.tbl_departments.Where(a => a.Status == true), "ID", "DisplayName", IndentReg.DepartId);
            ViewBag.PurchasesFor = new SelectList(PurchasesFor, "Value", "Text",IndentReg.PurchasesFor);
            ViewBag.Priority = new SelectList(Priorities, "Value", "Text",IndentReg.Priority);
            ViewBag.IndentType = new SelectList(IndentTypes, "Value", "Text",IndentReg.IndentType);
            ViewBag.Delivery = new SelectList(DeliveryMethods, "Value", "Text", IndentReg.Delivery);
        
            List<tbl_inv_indent_details> _IndDet = GetIndentDetailsByIndentId(Id);
            Session["List"] = (List<tbl_inv_indent_details>)_IndDet;
            return View("Edit", new InventoryModel.IndentManagement(IndentReg, _IndDet));
        }
        [HttpPost]
        public ActionResult Edit(FormCollection frm)
        {
            try
            {
                long VehicleId = frm["VehicleId"] == null ? 0 : Convert.ToInt64(frm["VehicleId"]);
                int DepartId = frm["DepartId"] == null ? 0 : Convert.ToInt32(frm["DepartId"]);
                DateTime DtOfInd = Convert.ToDateTime(frm["DtOfInd"].ToString());
                DateTime RequiredDt = Convert.ToDateTime(frm["RequiredDt"].ToString());
                string IndRefNo = frm["IndRefNo"] == null ? "" : frm["IndRefNo"].ToString();
                string PurchasesFor = frm["PurchasesFor"] == null ? "" : frm["PurchasesFor"].ToString();
                string Priority = frm["Priority"] == null ? "" : frm["Priority"].ToString();
                string IndentType = frm["IndentType"] == null ? "" : frm["IndentType"].ToString();
                string Status = frm["status"] == null ? "" : frm["status"].ToString();
                string AddInfo = frm["AddInfo"] == null ? "" : frm["AddInfo"].ToString();
                string Delivery = frm["Delivery"] == null ? "" : frm["Delivery"].ToString();
                long Id = frm["Id"] == null ? 0 : Convert.ToInt64(frm["Id"].ToString());
                MembershipUser userInfo = Membership.GetUser(User.Identity.Name);
                Guid UserId = new Guid();
                if (userInfo != null)
                {
                    UserId = objcore.GetUserIdByUsername(User.Identity.Name);
                }
                else
                    return Json(new { success = false, IndentId = 0, msg = "Your session has been expired.\n suggestion do logoff and login then try again." });
                // Update Indent Master 
                tbl_inv_indents objIndentReg = db.tbl_inv_indents.Where(a => a.Id == Id).FirstOrDefault();

                objIndentReg.VehicleId = VehicleId;
                objIndentReg.DepartId = DepartId;
                objIndentReg.IndUserId = UserId;
                objIndentReg.DtOfInd = DtOfInd;
                objIndentReg.RequiredDt = RequiredDt;
                objIndentReg.PurchasesFor = PurchasesFor;
                objIndentReg.Priority = Priority;
                objIndentReg.IndentType = IndentType;
                objIndentReg.Delivery = Delivery;
                objIndentReg.AddInfo = AddInfo;
                objIndentReg.IndRefNo = IndRefNo;
                objIndentReg.Status = Status;
                db.SaveChanges();
                return Json(new { success = true, IndentId = objIndentReg.Id, msg = "Indent has been saved sucessfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, IndentId = 0, msg = "Please check the details and try again." });
            }
        }

        public ActionResult Approve(long Id)
        {
            tbl_inv_indents _indent = db.tbl_inv_indents.Single(a => a.Id == Id);
            try
            {
                tbl_departments _dept = db.tbl_departments.Single(a => a.ID == _indent.DepartId);
                 string IndentManager = objcore.GetUserNameByUserId((Guid)_dept.IndentMgrUserId);
                 if (IndentManager.Trim().ToUpper() != User.Identity.Name.Trim().ToUpper())
                 {
                     return Json(new { success = false, IndentId = _indent.Id, msg = "You don't have permissions to approve this indent." });
                 }
                _indent.Status = IndentItemStatus.InProgress.ToString();
                _indent.IndAppDt = DateTime.Now;
                _indent.IndAppUserId = objcore.GetUserIdByUsername(User.Identity.Name);
                UpdateModel(_indent);
                db.SaveChanges();
                return Json(new { success = true,IndentId= _indent.Id, msg = "Indent has been approved successfully." });
            }
            catch
            {
                return Json(new { success = false, IndentId = _indent.Id, msg = "Indent approved failed." });
            }
        }

        public ActionResult CancelIndent(long Id)
        {
            tbl_inv_indents _indent = db.tbl_inv_indents.Single(a => a.Id == Id);
            try
            {
                _indent.Status = IndentItemStatus.Canceled.ToString();
                _indent.CancelDt = DateTime.Now;
                _indent.CancelUserId = objcore.GetUserIdByUsername(User.Identity.Name);
                UpdateModel(_indent);
                db.SaveChanges();
                return Json(new { success = true, msg = "Indent has been canceled successfully." });
            }
            catch
            {
                return Json(new { success = false, msg = "Indent canceled failed." });
            }
        }
        public ActionResult RejectIndent(long Id)
        {
            tbl_inv_indents _indent = db.tbl_inv_indents.Single(a => a.Id == Id);
            try
            {
                tbl_departments _dept = db.tbl_departments.Single(a => a.ID == _indent.DepartId);
                string IndentManager = objcore.GetUserNameByUserId((Guid)_dept.IndentMgrUserId);
                if (IndentManager.Trim().ToUpper() != User.Identity.Name.Trim().ToUpper())
                {
                    return Json(new { success = false, IndentId = _indent.Id, msg = "You don't have permissions to reject this indent." });
                }
                _indent.Status = IndentItemStatus.Rejected.ToString();
                _indent.IndRejDt = DateTime.Now;
                _indent.IndRejUserId = objcore.GetUserIdByUsername(User.Identity.Name);
                UpdateModel(_indent);
                db.SaveChanges();
                return Json(new { success = true, IndentId = _indent.Id, msg = "Indent has been rejected successfully." });
            }
            catch
            {
                return Json(new { success = false, IndentId = _indent.Id, msg = "Indent rejection failed." });
            }
        }
        public ActionResult ConfirmIndent(long Id)
        {
            tbl_inv_indents _indent = db.tbl_inv_indents.Single(a => a.Id == Id);
            try
            {
                _indent.Status = IndentItemStatus.WaitingForApproval.ToString();
                UpdateModel(_indent);
                db.SaveChanges();
                return Json(new { success = true, msg = "Indent has been confirmed successfully." });
            }
            catch
            {
                return Json(new { success = false, msg = "Indent confirmation failed." });
            }
        }
        // Edit Indent Details 
        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult EditIndentDetails(long? Id)
        {
            var IndentDet = db.tbl_inv_indent_details.Where(a => a.Id == Id).FirstOrDefault();
            var Indent = db.tbl_inv_indents.Where(a => a.Id == IndentDet.IndentId).FirstOrDefault();
            ViewBag.Vehicle = Indent.tbl_vehicles.VehicleRegNum;
            ViewBag.VehicleId = Indent.VehicleId;
            ViewBag.ItemId = new SelectList(db.InventoryItemsMasters.Where(a => a.Active == true), "ID", "ItemText", IndentDet.ItemId);
            decimal AvailQty = (decimal)objStore.GetAvailQty("",(int)IndentDet.ItemId, (long)Indent.VehicleId);
            ViewBag.AvailQty = AvailQty;
            InventoryItemsMaster _itemMaster = db.InventoryItemsMasters.Single(a => a.ID == IndentDet.ItemId);
            ViewBag.Price = _itemMaster.Amount;
            return PartialView("_EditIndentDetails", new InventoryModel.IndentManagement(null, IndentDet));
        }
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult EditIndentDetails(long Id, int ItemId, double QtyReq, int ReorderLevel, int LeadTime, string PurposeFor, string Specification)
        {
            string Procure = string.Empty;
            var IndentDet = db.tbl_inv_indent_details.Where(a => a.Id == Id).FirstOrDefault();
            var Indent = db.tbl_inv_indents.FirstOrDefault(a => a.Id == IndentDet.IndentId);
          
            try
            {
                decimal AvailQty = (decimal)objStore.GetAvailQty("",ItemId, (long)Indent.VehicleId);
                if (AvailQty > 0)
                    Procure = "Stock";
                else
                    Procure = "Purchase";
                // update here 
                IndentDet.ItemId = ItemId;
                IndentDet.QtyReq = QtyReq;
                IndentDet.ReorderLevel = ReorderLevel;
                IndentDet.LeadTime = LeadTime;
                IndentDet.PurposeFor = PurposeFor;
                IndentDet.Specification = Specification;
                IndentDet.Procure = Procure;
                db.SaveChanges();
                return Json(new { success = true, Id = IndentDet.IndentId, msg = "Indent item details has been updated  successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, Id = IndentDet.IndentId, msg = "Indent item updation failed."+ ex });
            }
           // return RedirectToAction("Edit", "indent", new { Area = "Inventory", Id = IndentDet.IndentId });
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult DeleteStockDetails(string Id)
        {
            string IsEdit = Id.Split('_')[1];
            long IndentDetId = Convert.ToInt64(Id.Split('_')[0]);
            if (IsEdit == "1")
            {
                tbl_inv_indent_details IndentRegDet = db.tbl_inv_indent_details.Where(a => a.Id == IndentDetId).FirstOrDefault();
                long IndentId = (long)IndentRegDet.IndentId;
                //if (StockRegDet.IssuedSlod != null)
                //{
                //    //ViewData["ErrorMessage"] = "Sorry! Stock has been issued from this invoice no. Deletion has cancelled.\n suggestion remove the issued stock from invoice to delete.";
                //    return RedirectToAction("InwardEdit", "Stores", new { Area = "Inventory", Id = RegId });
                //}
                db.tbl_inv_indent_details.DeleteObject(IndentRegDet);
                db.SaveChanges();
                return RedirectToAction("Edit", "Indent", new { Area = "Inventory", Id = IndentId });
            }
            else
            {
                IndentItemDet = (List<tbl_inv_indent_details>)Session["List"];
                IndentItemDet.RemoveAll(a => a.Id == Convert.ToInt32(Id.Split('_')[0]));

                List<tbl_inv_indent_details> _TempList = new List<tbl_inv_indent_details>();
                int IndDetId = 1;
                foreach (tbl_inv_indent_details _ind in IndentItemDet)
                {
                    _TempList.Add(new tbl_inv_indent_details 
                                 {
                                     Id = _ind.Id,
                                     ItemId = _ind.ItemId,
                                     ReorderLevel = _ind.ReorderLevel,
                                     QtyReq = _ind.QtyReq,
                                     LeadTime = _ind.LeadTime,
                                     Procure = _ind.Procure,
                                     PurposeFor = _ind.PurposeFor,
                                     Specification = _ind.Specification
                                 });
                    IndDetId += 1;
                }
                IndentItemDet = _TempList;
                ViewData["IsEdit"] = "0";
                return PartialView("_IndentItemListAdd", IndentItemDet);
            }

        }
        public ActionResult GetItemDetails(int ItemId, long VehicleId)
        {
            InventoryItemsMaster _itemMaster = db.InventoryItemsMasters.Single(a => a.ID == ItemId);
            IndentItemDet = (List<tbl_inv_indent_details>)Session["List"];

            decimal CurrentIssuedQty = 0, CurrentAvailQty = 0;
            if (IndentItemDet != null)
            {
                foreach (var Item in IndentItemDet)
                {
                    if (Item.ItemId == ItemId)
                    {
                        //Issued Quantity
                        CurrentIssuedQty += (decimal)Item.QtyReq;
                        List<GeneralClassFeilds> _StockList = objStore.GetStockRegList("", ItemId, VehicleId);
                        decimal AvaiQty = Convert.ToDecimal(_StockList.Sum(a => a.ReceQty) - _StockList.Sum(a => a.IssuedSlod));
                        CurrentAvailQty = (AvaiQty - CurrentIssuedQty);
                    }
                }
            }
            //else
            //{
            //    var indentDet = (from a in db.tbl_inv_indents
            //                     join b in db.tbl_inv_indent_details on a.Id equals b.IndentId
            //                     where a.CampusId == CampusId && b.ItemId == ItemId
            //                     select b).ToList();
            //    decimal AQty = (decimal)objStore.GetAvailQty(ItemId, CampusId);
            //    decimal ReqQty =(decimal)indentDet.Sum(a => a.QtyReq);
            //    CurrentAvailQty = (AQty - ReqQty);
            //}
            if (CurrentAvailQty <= 0)
            {
                CurrentAvailQty =(decimal)objStore.GetAvailQty("",ItemId, VehicleId);
            }
            if (_itemMaster != null)
                return Json(new { success = true, ReorderLevel = _itemMaster.ReorderLevel,
                                  Specification = _itemMaster.Specification,
                                  Amount = _itemMaster.Amount,
                                  AvailQty = CurrentAvailQty,
                }, JsonRequestBehavior.AllowGet);
            else
                return Json(new { success = true, ReorderLevel = 0, Specification = "", Amount = 0 ,AvailQty = 0}, JsonRequestBehavior.AllowGet);
        }
        public List<tbl_inv_indent_details> GetIndentDetailsByIndentId(long Id)
        {
            List<tbl_inv_indent_details> IndentDetList = db.tbl_inv_indent_details.Where(a => a.IndentId == Id).ToList();
            return IndentDetList;
        }
        public int VerifyIndentRefNo(string IndRefNo)
        {
           
            try
            {
                var IndentDet = db.tbl_inv_indents.Where(a => a.IndRefNo == IndRefNo).ToList();
                if (IndentDet.Count != 0)
                    return 1;
                else
                    return 0;
            }
            catch
            {
                return 0;
            }
        }
        public ActionResult ViewIndent(int Id)
        {
            tbl_inv_indents _indent = db.tbl_inv_indents.Single(a => a.Id == Id);
            tbl_departments _dept = db.tbl_departments.Single(a => a.ID == _indent.DepartId);
            ViewBag.Status = _indent.Status;
            ViewBag.IndentId = Id;
            ViewBag.RefferNo = _indent.IndRefNo;
            if (_dept.IndentMgrUserId != null)
            {
                ViewBag.IndentManager = objcore.GetUserNameByUserId((Guid)_dept.IndentMgrUserId);
            }
            else
                ViewBag.IndentManager = "";
            ViewBag.IndentCreatedBy = objcore.GetUserNameByUserId((Guid)_indent.IndUserId);
            ViewBag.AlreadyIssue = 0;
            if (db.InventoryIssuedMasters.Where(a => a.IndentRefNo == _indent.IndRefNo).Any())
            {
                ViewBag.AlreadyIssue = 1;
            }
            //bool isCorrect = checkIndentPurchaseOrder(Id);
            //if (isCorrect == true)
            //    ViewBag.AlreadyInPurchaseOrder = true;
            //else
             //   ViewBag.AlreadyInPurchaseOrder = false;
            return View();
        }
        public ActionResult GetIndentDetails(int IndentId)
        {
            tbl_inv_indents _itemMaster = db.tbl_inv_indents.Single(a => a.Id == IndentId);
            List<tbl_inv_indent_details> _IndDet = GetIndentDetailsByIndentId(IndentId);
            return PartialView("_GetIndentDetails", new InventoryModel.IndentManagement(_itemMaster, _IndDet));
        }
        public ActionResult IssuedProducts(int Id)
        {
            tbl_inv_indents Indent = db.tbl_inv_indents.FirstOrDefault(a => a.Id == Id);
            List<tbl_inv_indent_details> _IndDet = GetIndentDetailsByIndentId(Indent.Id);
            ViewBag.IndentId = Indent.Id;
            string IssueNo = DateTime.Now.ToString("ddMMyyhhmmss").ToString();
            //==> if Indent already issued 
            if (db.InventoryIssuedMasters.Where(a => a.IndentRefNo == Indent.IndRefNo).Any())
            {
                return Json(new { success = false, Id = 0, msg = "Indent has already issued." });
            }
            // insert into Inventory Issued Master
            InventoryIssuedMaster objIssuedMaster = new InventoryIssuedMaster();
            objIssuedMaster.IssueNo = IssueNo;
            objIssuedMaster.IssueDt = DateTime.Now;
            objIssuedMaster.VehicleId =(long)Indent.VehicleId;
            objIssuedMaster.OtherDetails = Indent.AddInfo;
            objIssuedMaster.IssueQty = _IndDet.Sum(a => a.QtyReq);
            objIssuedMaster.Active = true;
            objIssuedMaster.Status = IssuedItemStatus.WaitingForAvailability.ToString();
            objIssuedMaster.IndentRefNo = Indent.IndRefNo;
            objIssuedMaster.IssueUserId = Indent.IndUserId;
            db.InventoryIssuedMasters.AddObject(objIssuedMaster);
            db.SaveChanges();
            // insert into Inventory Issued Details
            int Slno = 0; string ProductStatus = string.Empty;
            foreach(tbl_inv_indent_details item in _IndDet)
            {
                InventoryIssuedDetail objIssueDet = new InventoryIssuedDetail();
                if (_IndDet.Count() == 1) Slno = 1;
                else                      Slno = Slno + 1;
                string IssuedNo = objIssuedMaster.IssueNo;
                string UserName = objcore.GetUserNameByUserId((Guid)Indent.IndAppUserId);
                InventoryItemsMaster _ItemMaster = db.InventoryItemsMasters.Where(a => a.ID == item.ItemId).FirstOrDefault();
                int ItemId = Convert.ToInt32(item.ItemId);
                long VehicleId = Convert.ToInt64(Indent.VehicleId);
                double AvailableQuantity = objStore.GetAvailQty("", ItemId, VehicleId);
                double IssuedQty =(double)item.QtyReq;
                if (AvailableQuantity < IssuedQty)
                    ProductStatus = "WaitingForAvailability";
                else
                    ProductStatus = "Available";
                objIssueDet.Slno = Slno;
                objIssueDet.IssueNo = IssuedNo;
                objIssueDet.ReceiptNo = "";
                objIssueDet.UserName = UserName;
                objIssueDet.ItemId = _ItemMaster.ID;
                objIssueDet.ItemText = _ItemMaster.ItemText;
                objIssueDet.IssuedDt =null;
                objIssueDet.IssuedQty = IssuedQty;
                objIssueDet.Total = null;
                objIssueDet.Status = ProductStatus;
                db.InventoryIssuedDetails.AddObject(objIssueDet);
                db.SaveChanges();
            }
            // set Indent Status to In progress
            Indent.Status = IndentItemStatus.InProgress.ToString();
            UpdateModel<tbl_inv_indents>(Indent);
            db.SaveChanges();
            return Json(new { success = true, Id = objIssuedMaster.Id, msg = "Indent Issued has been done successfully." });
        }
        public ActionResult CheckIndentProcurement(int Id)
        {
            int isCorrect = checkIndentPurchaseOrder(Id);
            if (isCorrect == 0)
                return Json(new { success = false, Id = 0, msg = "Indent has already issued for purchase order." });
            if (isCorrect == 2)
                return Json(new { success = false, Id = 0, msg = "Purchase order has already exists for Issue item(s) details" });
            else
                return Json(new { success = true, Id = 0, msg = "" });
        }
        public int checkIndentPurchaseOrder(int Id)
        {
            InventoryIssuedMaster _issueMaster = db.InventoryIssuedMasters.FirstOrDefault(a => a.Id == Id);
            string IndentRefNo = string.Empty;
            if (!string.IsNullOrEmpty(_issueMaster.IndentRefNo))
                IndentRefNo = db.tbl_inv_indents.FirstOrDefault(a => a.IndRefNo.Trim().ToUpper() == _issueMaster.IndentRefNo.Trim().ToUpper()).IndRefNo;
            else
                IndentRefNo = _issueMaster.IssueNo;
            List<InventoryIssuedDetail> _IssuedList = objStore.GetIssuedDetailsByIssuedNo(_issueMaster.IssueNo);
            int hasWaitForAvail = 0, hasAvailable = 0;
            //==> check for item availability in InventoryIssuedDetail
             //1)==> if all are "AVAILABLE" for transfer then "Ristrict" purchase order.
            //2)==> if any one of item is in "WAITING FOR AVAILABILITY" then go for purchase order.
            foreach (InventoryIssuedDetail item in _IssuedList)
            {
                if (item.Status.ToUpper() == "WAITINGFORAVAILABILITY")
                    hasWaitForAvail += 1;
                if (item.Status.ToUpper() == "AVAILABLE")
                    hasAvailable += 1;
               

            } 
            if (db.tbl_PurchaseOrders.Where(a => a.Source == IndentRefNo  && hasWaitForAvail == 0).Any())
                return 0;
            else
                return 1;
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
        public ActionResult LoadIndentItems(long IndentId)
        {
            if (IndentId == 0)
            {
                IndentItemDet = (List<tbl_inv_indent_details>)Session["List"];
                ViewData["IsEdit"] = "0";
                    return PartialView("_IndentItemListAdd", IndentItemDet);
            }
            else
            {
                List<tbl_inv_indent_details> _IndDet = GetIndentDetailsByIndentId(IndentId);
                ViewData["IsEdit"] = "1";
                return PartialView("_IndentItemListAdd", _IndDet);
            }
        }
        private List<tbl_inv_indent_details> GetIndentDetailsByItemAndCampus(int ItemId, long VehicleId)
        {
            string stat = IndentItemStatus.Received.ToString().ToUpper();
            var x = (from im in db.tbl_inv_indents
                     join id in db.tbl_inv_indent_details on im.Id equals id.IndentId
                     where id.ItemId == ItemId && im.VehicleId == VehicleId && im.Status.ToUpper() != stat
                     select id).ToList();
            return x.ToList<tbl_inv_indent_details>();
             
        }
    }
}
