using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;
using System.Text;
using FMS.Helpers;
using System.Web.Security;
using FMS.Controllers;

namespace FMS.Areas.Inventory.Controllers
{
    [CustomAuthorizationFilter]
    public class JobCardController : Controller
    {
        #region ctors
        // GET: /JobCard/
        public FMSDBEntities db;
        private List<tbl_jobcard_details> jbCrdDetails;
        private StoresController objStore;
        private core objcore;
        public JobCardController()
        {
            db = new FMSDBEntities();
            jbCrdDetails = new List<tbl_jobcard_details>();
            objStore = new StoresController();
            objcore = new core();
        } 
        #endregion

        #region Index

        public ActionResult Index()
        {
            return View();
        }

        public JsonResult GetJobCardList(int iDisplayStart, int iDisplayLength, string sSearch, int iColumns,
        int iSortingCols, int iSortCol_0, string sSortDir_0,
        int sEcho, string mDataProp_Key)
        {
            var IList = GetEnumerableList();
            var filteredLogSheet = IList
                .Select(l => new[]{
                    l.Id.ToString(),
                    l.Text1.ToString(),
                    l.Text2.ToString(),
                    l.Text3.ToString(),
                    l.Text4.ToString(),
                    l.Text5.ToString(),
                    l.Amount.ToString(),
                    l.Text6.ToString(),
                    l.Amount1.ToString(),
                    l.Amount2.ToString(),
                    l.Amount3.ToString(),
                    l.Amount4.ToString(),
                    l.Text7.ToString(),
                    l.Status.ToString()
                    
                }).Where(i => string.IsNullOrEmpty(sSearch)
                     || i.Any(y => y.IndexOf(sSearch, StringComparison.InvariantCultureIgnoreCase) >= 0));
            var orderedlist = filteredLogSheet
                   .OrderByWithDirection(i => (i[iSortCol_0]).Parse(), sSortDir_0 == "asc").Take(iDisplayLength);
            var model = new
            {
                aaData = orderedlist,
                iTotalDisplayRecords = filteredLogSheet.Count(),
                iTotalRecords = filteredLogSheet.Count(),
                sEcho = sEcho.ToString()
            };
            return Json(model, JsonRequestBehavior.AllowGet);
        }

        private IEnumerable<CommonFields> GetEnumerableList()
        {
            var list = new List<CommonFields>();
            var tempList = (from l in db.tbl_jobcard
                            select l).OrderByDescending(a => a.Id).ToList();

            foreach (tbl_jobcard ds in tempList)
            {
                list.Add(new CommonFields
                {
                    Id = ds.Id,
                    Text1 = ds.JobCardNo,
                    Text2 = ds.VehicleId == null ? "" : ds.tbl_vehicles.VehicleRegNum,
                    Text3 = ds.JobCardDt.HasValue ? ds.JobCardDt.Value.ToShortDateString() : "",
                    Text4 = ds.ServiceType,
                    Text5 = ds.ServiceId == null ? "-" : ds.tbl_veh_service_schedules.ServiceNo.ToString(),
                    Amount = ds.OdometerReading.HasValue ? ds.OdometerReading.Value : 0,
                    Text6 = ds.tbl_service_stations.ServiceStation,
                    Amount1 = ds.ServiceCharges ?? 0,
                    Amount2 = ds.TotalAmount ?? 0,
                    Amount3 = ds.DisCountAmt ?? 0,
                    Amount4 = ds.NetAmount ?? 0,
                    Text7 = ds.Status,
                    Status = true
                });
            }
            //objcore.ConvertToUppercase<CommonFields>(list);
            return list.OrderByDescending(a => a.Id);
        }

        #endregion

        #region Create
        [HttpGet]
        public ActionResult Create(long? VehicleId, long? ServiceId)
        {
            string serviceType = string.Empty;
            ViewBag.jobCardNo = GetJobCardRefNo(DateTime.Now, 4);
            ViewBag.jobDoneBy = new SelectList(db.tbl_service_stations.Where(a => a.Status == true), "Id", "ServiceStation");
            List<SelectListItem> _serviceTypeList = new List<SelectListItem>();
            _serviceTypeList.Add(new SelectListItem { Text = "General Service", Value = "General Service" });
            _serviceTypeList.Add(new SelectListItem { Text = "Maintenance", Value = "Maintenance" });
            if (VehicleId.HasValue && ServiceId.HasValue)
            {
                tbl_vehicles vehDet = db.tbl_vehicles.Where(a => a.ID == VehicleId).SingleOrDefault();
                serviceType = "General Service";
                ViewBag.VehicleId = VehicleId;
                ViewBag.ServiceId = ServiceId;
                ViewBag.VehRegNum = vehDet.VehicleRegNum;
            }
            ViewBag.ServiceType = new SelectList(_serviceTypeList, "Value", "Text", serviceType);
            return View();
        }
        [HttpPost]
        public JsonResult Create(FormCollection frm, tbl_jobcard jbCard)
        {
            try
            {
                List<tbl_jobcard_details> JobCardRegDetList = (List<tbl_jobcard_details>)Session["List"];
                
                string jbCrdNo = frm["JobCardNo"];
                DateTime jbCrdDt = Convert.ToDateTime(frm["JobCardDt"].ToString());
                decimal odoMtrRdng = string.IsNullOrEmpty(frm["OdometerReading"]) ? 0 : Convert.ToDecimal(frm["OdometerReading"]);
                decimal srvcChrg = string.IsNullOrEmpty(frm["ServiceCharges"]) ? 0 : Convert.ToDecimal(frm["ServiceCharges"]);
                decimal totalAmt = string.IsNullOrEmpty(frm["TotalAmount"]) ? 0 :  Convert.ToDecimal(frm["TotalAmount"]);
                decimal discountAmt = string.IsNullOrEmpty(frm["DisCountAmt"]) ? 0 : Convert.ToDecimal(frm["DisCountAmt"]);
                decimal VatTax = string.IsNullOrEmpty(frm["VatTax"]) ? 0 : Convert.ToDecimal(frm["VatTax"]);
                decimal netAmount = string.IsNullOrEmpty(frm["NetAmount"]) ? 0 : Convert.ToDecimal(frm["NetAmount"]);
                long vehId = Convert.ToInt64(frm["vehId"] == "" ? "" : frm["vehId"]);
                long? serviceId = frm["ServiceId"] == null ? 0 : Convert.ToInt64(frm["ServiceId"]);
                string serviceType = frm["ServiceType"];
                long jbDoneBy = Convert.ToInt64(frm["JobDoneBy"] == "" ? "" : frm["JobDoneBy"].ToString());
                decimal Fuel = frm["Fuel"] == "" ? 0 : Convert.ToDecimal(frm["Fuel"].ToString());
                string jbCrdDescr = frm["JobCardDescr"] == null ? "" : frm["JobCardDescr"].ToString();
                string Status = JobCardStatus.Open.ToString();
                
                // Verify JobCard Number while inserting
                if (VerifyJobCardRefNumber(jbCrdNo))
                    jbCrdNo = GetJobCardRefNo(jbCrdDt, 4);
                
                //Insert jobCard Master 
                tbl_jobcard objjobcard = new tbl_jobcard();
                objjobcard.JobCardNo = jbCrdNo;
                objjobcard.JobCardDt = jbCrdDt;
                objjobcard.TotalAmount = totalAmt;
                objjobcard.OdometerReading = odoMtrRdng;
                objjobcard.ServiceCharges = srvcChrg;
                objjobcard.VatTax = VatTax;
                objjobcard.VehicleId = vehId;
                objjobcard.ServiceId = serviceId == 0 ? null : serviceId;
                objjobcard.ServiceType = serviceType;
                objjobcard.DisCountAmt = discountAmt;
                objjobcard.NetAmount = netAmount;
                objjobcard.JobDoneBy = jbDoneBy;
                objjobcard.Fuel = Fuel;
                objjobcard.JobCardDescr = jbCrdDescr;
                objjobcard.DtTimeActual = null;
                objjobcard.CreatedBy = User.Identity.Name;
                objjobcard.CreatedDt = DateTime.Now;
                objjobcard.Status = Status;
                db.AddTotbl_jobcard(objjobcard);
                db.SaveChanges();

                // Insert Indent Item details 
                if (JobCardRegDetList != null)
                {
                    foreach (tbl_jobcard_details _RegDet in JobCardRegDetList)
                    {
                        tbl_jobcard_details objJobCardDet = new tbl_jobcard_details();
                        objJobCardDet.JobcardId = objjobcard.Id;
                        objJobCardDet.ItemId = _RegDet.ItemId;
                        objJobCardDet.Qty = _RegDet.Qty;
                        objJobCardDet.Amount = _RegDet.Amount;
                        objJobCardDet.ReorderLevel = _RegDet.ReorderLevel;
                        objJobCardDet.LeadTime = _RegDet.LeadTime;
                        objJobCardDet.Procure = _RegDet.Procure;
                        objJobCardDet.PurposeFor = _RegDet.PurposeFor;
                        objJobCardDet.Specifications = _RegDet.Specifications;
                        objJobCardDet.VatTax = _RegDet.VatTax;
                        objJobCardDet.ServiceTax = _RegDet.ServiceTax;
                        db.tbl_jobcard_details.AddObject(objJobCardDet);
                        db.SaveChanges();
                    }
                }
                return Json(new { success = true, IndentId = objjobcard.Id, msg = "Job Card has been created sucessfully." });
            }
             catch (Exception ex)
            {
                return Json(new { success = false, IndentId = 0, msg = "Please check the details and try again." });
            }
        }
        public ActionResult AddJobCard(int IsEdit)
        {
            ViewBag.IsEdit = IsEdit;
            return PartialView("_AddJobCard");
        }
        public JsonResult LoadItmsByItemType(string itemType)
        {
            if (itemType == "Item")
            {
                var items = db.InventoryItemsMasters.Where(t => t.IsItem == 1 && t.MessItem == true && t.Active == true).Select(a => new { a.ID, a.ItemText }).ToList();
                return Json(items, JsonRequestBehavior.AllowGet);
            }
            else
            {
                var srvcItm = db.InventoryItemsMasters.Where(t => t.IsItem == 0 && t.MessItem == true && t.Active == true).Select(a => new { a.ID, a.ItemText }).ToList();
                return Json(srvcItm, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult GetItemDetails(int ItemId)
        {
            InventoryItemsMaster _itemMaster = db.InventoryItemsMasters.Single(a => a.ID == ItemId);
            jbCrdDetails = (List<tbl_jobcard_details>)Session["List"];

            decimal CurrentIssuedQty = 0, CurrentAvailQty = 0;
            if (jbCrdDetails != null)
            {
                foreach (var Item in jbCrdDetails)
                {
                    if (Item.ItemId == ItemId)
                    {
                        //Issued Quantity
                        CurrentIssuedQty += (decimal)Item.Qty;
                        List<GeneralClassFeilds> _StockList = GetStockRegList("", ItemId);
                        decimal AvaiQty = Convert.ToDecimal(_StockList.Sum(a => a.ReceQty) - _StockList.Sum(a => a.IssuedSlod));
                        CurrentAvailQty = (AvaiQty - CurrentIssuedQty);
                    }
                }
            }

            if (CurrentAvailQty <= 0)
            {
                CurrentAvailQty = (decimal)GetAvailQty(ItemId);
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
        public List<GeneralClassFeilds> GetStockRegList(string ReceiptNo, int ItemId)
        {
            List<GeneralClassFeilds> list = null;
            try
            {
                string Query = "select srd.ID invSrdId, isnull(srd.IssuedSlod,0) IssuedSlod,  srd.Slno, srd.ReceQty, srd.UnitsId,srd.StockRegId, sr.ReceiptNO ReceiptNo,isnull(srd.QOH,0) AS QOH from inventorystockregister sr " +
                    " inner join inventorystockregdetails srd on sr.id = srd.stockregid where sr.active=1 and srd.receqty-isnull(srd.issuedslod,0)>0 and srd.itemid = " + ItemId + " ";

                if (ReceiptNo != "")
                {
                    Query = Query + "and sr.receiptNo = '" + ReceiptNo + "' ";
                    //query = query.Where(s => s.ReceiptNO == ReceiptNo).ToList();
                }


                // list = query
                list = db.ExecuteStoreQuery<GeneralClassFeilds>(Query).ToList();
                return list;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public double GetAvailQty(int ItemId)
        {
            double Qty = 0;
            try
            {
                if (ItemId != 0)
                {
                    List<GeneralClassFeilds> _StockRegList = GetStockRegList("", ItemId);
                    Qty = Convert.ToDouble(_StockRegList.Sum(a => a.ReceQty) - _StockRegList.Sum(a => a.IssuedSlod));
                }
                return Qty;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        public ActionResult JobCardItemListAdd(int ItemId, double Qty, decimal Amount, long? JobcardId, FormCollection frm)
        {
            InventoryItemsMaster _itemMaster = db.InventoryItemsMasters.Single(a => a.ID == ItemId);
            string Procure = string.Empty;
            jbCrdDetails = (List<tbl_jobcard_details>)Session["List"];
            if (jbCrdDetails == null)
                jbCrdDetails = new List<tbl_jobcard_details>();
            else if (jbCrdDetails.Count > 0)
            {
                // check items if already have on ItemId and Campus  if not Canceled or Transfer
                if (JobcardId == 0 || JobcardId == null)
                {
                    if (jbCrdDetails.Where(a => a.ItemId == ItemId).Any())
                        return Json(new { success = false, IndentId = 0, msg = "Job card items has already added for this vehicle" });
                }
            }
            
            if (JobcardId == 0 || JobcardId == null)
                ViewData["IsEdit"] = "0";
            else
                ViewData["IsEdit"] = "1";
            
            int Id = 0;
            if (jbCrdDetails.Count() == 0)
                Id = 1;
            else
                Id = (int)(jbCrdDetails.LastOrDefault().Id + 1);

            Procure = "Purchase";
            long Indent_Reg_Id = JobcardId == null ? 0 : Convert.ToInt64(JobcardId);

            jbCrdDetails.Add(new tbl_jobcard_details
            {
                Id = Id,
                ItemId = ItemId,
                JobcardId = ItemId,
                ReorderLevel = null,
                Qty = Qty,
                LeadTime = null,
                Amount = Amount,
                Procure = Procure,
                
            });
            Session["List"] = jbCrdDetails;
            ViewData["IsEdit"] = "0";
            if (Indent_Reg_Id != 0)
            {
                tbl_jobcard_details objDet = new tbl_jobcard_details();
                objDet.ItemId = ItemId;
                objDet.JobcardId = Indent_Reg_Id;
                objDet.Qty = Qty;
                objDet.Amount = Amount;
                objDet.Procure = Procure;
                db.tbl_jobcard_details.AddObject(objDet);
                db.SaveChanges();
            }
            ViewBag.IsEdit = 1;
            return Json(new { success = true, JobCardId = Indent_Reg_Id, msg = "Job card item has been added successfully." });
        }

        public List<tbl_jobcard_details> GetIndentDetailsByIndentId(long Id)
        {
            List<tbl_jobcard_details> IndentDetList = db.tbl_jobcard_details.Where(a => a.JobcardId == Id).ToList();
            return IndentDetList;
        }
        public ActionResult LoadJobCardItems(long JobcardId, string Status)
        {
            if (JobcardId == 0)
            {
                jbCrdDetails = (List<tbl_jobcard_details>)Session["List"];
                ViewData["IsEdit"] = "0";
                return PartialView("_JobCardDetailsList", jbCrdDetails);
            }
            else
            {
                jbCrdDetails = GetIndentDetailsByIndentId(JobcardId);
                ViewData["IsEdit"] = "1";
                ViewData["Status"] = Status;
                ViewBag.JobCardDet = db.tbl_jobcard.Where(a => a.Id == JobcardId).SingleOrDefault(); 
                return PartialView("_JobCardDetailsList", jbCrdDetails);
            }
        }

        public string getAjaxResult(string f, string q)
        {
            f = f == "undefined" ? "VehicleRegNum" : f;
            StringBuilder str = new StringBuilder();
            var vehicleId = from a in db.tbl_vehicles
                                   .Where(a => a.Status.Value == true)
                                   .Where<tbl_vehicles>(f, q, WhereOperation.Contains)
                            select new { a.VehicleRegNum, a.ID };


            foreach (var a in vehicleId)
            {
                str.Append(string.Format("{0} |{1}\r\n", a.VehicleRegNum.ToUpper(), a.ID)).ToString();
                ViewBag.ClientID = a.ID;
            }

            return str.ToString();
        }

        #endregion Create

        #region Edit
        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult Edit(long Id)
        {
            tbl_jobcard jobCard = db.tbl_jobcard.Where(a => a.Id == Id).FirstOrDefault();
            ViewBag.ItemsList = new SelectList(db.InventoryItemsMasters.Where(a => a.Active == true), "ID", "ItemText");
            ViewBag.VehId = new SelectList(db.tbl_vehicles.Where(a => a.Status == true), "ID", "DisplayName", jobCard.VehicleId);
            ViewBag.JobDoneBy = new SelectList(db.tbl_service_stations.Where(a => a.Status == true), "Id", "ServiceStation", jobCard.JobDoneBy);
            ViewBag.ServiceNumber = new SelectList(db.tbl_service_sch_master.Where(a => a.Status == true), "Id", "SSNO", jobCard.ServiceId);
            List<SelectListItem> _serviceNumList = new List<SelectListItem>();
            _serviceNumList.Add(new SelectListItem { Text = "General Service", Value = "General Service" });
            _serviceNumList.Add(new SelectListItem { Text = "Maintenance", Value = "Maintenance" });
            ViewBag.ServiceType = new SelectList(_serviceNumList, "Value", "Text", jobCard.ServiceType);
            List<tbl_jobcard_details> _ItemDet = GetIndentDetailsByIndentId(Id);
            Session["List"] = (List<tbl_jobcard_details>)_ItemDet;
            return View("Edit", new JobCardManagement(jobCard, _ItemDet));
        }
        [HttpPost]
        public ActionResult Edit(FormCollection frm)
        {
            try
            {
                DateTime? billingInvDt = null; 
                string jbCrdNo = frm["JobCardNo"];
                DateTime jbCrdDt = Convert.ToDateTime(frm["JobCardDt"].ToString());
                decimal odoMtrRdng = Convert.ToDecimal(frm["OdometerReading"] == "" ? null : frm["OdometerReading"]);
                decimal srvcChrg = Convert.ToDecimal(frm["ServiceCharges"] == "" ? null : frm["ServiceCharges"].ToString());
                decimal totalAmt = Convert.ToDecimal(frm["TotalAmount"] == "" ? null : frm["TotalAmount"].ToString());
                decimal discountAmt = Convert.ToDecimal(frm["DisCountAmt"] == "" ? null : frm["DisCountAmt"].ToString());
                decimal VatTax = string.IsNullOrEmpty(frm["VatTax"]) ? 0 : Convert.ToDecimal(frm["VatTax"]);
                decimal netAmount = Convert.ToDecimal(frm["NetAmount"] == "" ? null : frm["NetAmount"].ToString());
                long vehId = Convert.ToInt64(frm["vehId"] == "" ? "" : frm["vehId"]);
                long? serviceId = frm["ServiceId"] == null ? 0 : Convert.ToInt64(frm["ServiceId"]);
                string serviceType = frm["ServiceType"];
                string billingInvNo = frm["BillingInvNO"];
                billingInvDt = string.IsNullOrEmpty(frm["BillingInvDt"]) ? billingInvDt : Convert.ToDateTime(frm["BillingInvDt"]);
                long jbDoneBy = Convert.ToInt64(frm["JobDoneBy"] == "" ? "" : frm["JobDoneBy"].ToString());
                decimal Fuel = Convert.ToInt64(frm["Fuel"] == null ? "" : frm["Fuel"].ToString());
                string jbCrdDescr = frm["JobCardDescr"] == null ? "" : frm["JobCardDescr"].ToString();
                long Id = frm["Id"] == null ? 0 : Convert.ToInt64(frm["Id"].ToString());
                string Status = (frm["Status"]);
                // Update Indent Master 
                tbl_jobcard objjobcard = db.tbl_jobcard.Where(a => a.Id == Id).FirstOrDefault();

                objjobcard.JobCardNo = jbCrdNo;
                objjobcard.JobCardDt = jbCrdDt;
                objjobcard.TotalAmount = totalAmt;
                objjobcard.OdometerReading = odoMtrRdng;
                objjobcard.DisCountAmt = discountAmt;
                objjobcard.ServiceCharges = srvcChrg;
                objjobcard.VehicleId = vehId;
                objjobcard.VatTax = VatTax;
                objjobcard.NetAmount = netAmount;
                objjobcard.ServiceId = serviceId == 0 ? null : serviceId;
                objjobcard.ServiceType = serviceType;
                objjobcard.JobDoneBy = jbDoneBy;
                objjobcard.Fuel = Fuel;
                objjobcard.JobCardDescr = jbCrdDescr;
                objjobcard.BillingInvNO = billingInvNo;
                objjobcard.BillingInvDt = billingInvDt;
                objjobcard.Status = Status;
                db.SaveChanges();

                if (Status == "Closed" && objjobcard.ServiceId != null)
                {
                    tbl_veh_service_schedules vehSch = db.tbl_veh_service_schedules.Where(a => a.Id == serviceId).SingleOrDefault();
                    if (vehSch != null)
                    {
                        vehSch.IsDone = true;
                        vehSch.DoneDt = DateTime.Now;
                        vehSch.DoneBy = User.Identity.Name;
                        vehSch.ServiceStation = objjobcard.JobDoneBy;
                        db.SaveChanges();
                    }
                }

                return Json(new { success = true, JobcardId = objjobcard.Id, msg = "Jobcard has been saved sucessfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, JobcardId = 0, msg = "Please check the details and try again." });
            }
        }

        public ActionResult ConfirmJobCard(long Id)
        {
            tbl_jobcard _jobCard = db.tbl_jobcard.Single(a => a.Id == Id);
            try
            {
                _jobCard.Status = JobCardItemStatus.WaitingForApproval.ToString();
                UpdateModel(_jobCard);
                db.SaveChanges();
                return Json(new { success = true, msg = "JobCard has been confirmed successfully." });
            }
            catch
            {
                return Json(new { success = false, msg = "JobCard confirmation failed." });
            }
        }
        public ActionResult ViewJobCard(long Id)
        {
            tbl_jobcard _jobCard = db.tbl_jobcard.Single(a => a.Id == Id);
            ViewBag.Status = _jobCard.Status;
            ViewBag.IndentId = Id;
            ViewBag.RefferNo = _jobCard.JobCardNo;
            ViewBag.IndentManager = "";
            ViewBag.AlreadyIssue = 0;
            if (db.InventoryIssuedMasters.Where(a => a.IndentRefNo == _jobCard.JobCardNo).Any())
                ViewBag.AlreadyIssue = 1;
            return View();
        }
        public ActionResult GetJobCardDetails(int IndentId)
        {
            tbl_jobcard _itemMaster = db.tbl_jobcard.Single(a => a.Id == IndentId);
            List<tbl_jobcard_details> _IndDet = GetIndentDetailsByIndentId(IndentId);
            return PartialView("_GetJobCardDetailsPartial", new JobCardManagement(_itemMaster, _IndDet));
        }

        public ActionResult Approve(long Id)
        {
            tbl_jobcard _jobCard = db.tbl_jobcard.Single(a => a.Id == Id);
            try
            {
                //tbl_departments _dept = db.tbl_departments.Single(a => a.ID == _indent.DepartId);
                //string IndentManager = objcore.GetUserNameByUserId((Guid)_dept.IndentMgrUserId);
                //if (IndentManager.Trim().ToUpper() != User.Identity.Name.Trim().ToUpper())
                //{
                //    return Json(new { success = false, IndentId = _indent.Id, msg = "You don't have permissions to approve this indent." });
                //}
                _jobCard.Status = JobCardItemStatus.InProgress.ToString();
                //_jobCard.IndAppDt = DateTime.Now;
                //_indent.IndAppUserId = objcore.GetUserIdByUsername(User.Identity.Name);
                UpdateModel(_jobCard);
                db.SaveChanges();
                return Json(new { success = true, IndentId = _jobCard.Id, msg = "JobCard has been approved successfully." });
            }
            catch
            {
                return Json(new { success = false, IndentId = _jobCard.Id, msg = "JobCard approved failed." });
            }
        }

        public ActionResult RejectJobCard(long Id)
        {
            tbl_jobcard _jobCard = db.tbl_jobcard.Single(a => a.Id == Id);
            try
            {
                //tbl_departments _dept = db.tbl_departments.Single(a => a.ID == _indent.DepartId);
                //string IndentManager = objcore.GetUserNameByUserId((Guid)_dept.IndentMgrUserId);
                //if (IndentManager.Trim().ToUpper() != User.Identity.Name.Trim().ToUpper())
                //{
                //    return Json(new { success = false, IndentId = _indent.Id, msg = "You don't have permissions to reject this indent." });
                //}
                _jobCard.Status = JobCardItemStatus.Rejected.ToString();
                // _indent.IndRejDt = DateTime.Now;
                //_indent.IndRejUserId = objcore.GetUserIdByUsername(User.Identity.Name);
                UpdateModel(_jobCard);
                db.SaveChanges();
                return Json(new { success = true, IndentId = _jobCard.Id, msg = "JobCard has been rejected successfully." });
            }
            catch
            {
                return Json(new { success = false, IndentId = _jobCard.Id, msg = "JobCard rejection failed." });
            }
        }
        public ActionResult IssuedProducts(int Id)
        {
            tbl_jobcard _jobCard = db.tbl_jobcard.FirstOrDefault(a => a.Id == Id);
            List<tbl_jobcard_details> _itemDet = GetIndentDetailsByIndentId(_jobCard.Id);
            ViewBag.IndentId = _jobCard.Id;
            string IssueNo = DateTime.Now.ToString("ddMMyyhhmmss").ToString();
            //==> if Indent already issued 
            if (db.InventoryIssuedMasters.Where(a => a.IndentRefNo == _jobCard.JobCardNo).Any())
            {
                return Json(new { success = false, Id = 0, msg = "JobCard has already issued." });
            }
            // insert into Inventory Issued Master
            InventoryIssuedMaster objIssuedMaster = new InventoryIssuedMaster();
            objIssuedMaster.IssueNo = IssueNo;
            objIssuedMaster.IssueDt = DateTime.Now;
            objIssuedMaster.OtherDetails = _jobCard.JobCardDescr;
            objIssuedMaster.IssueQty = _itemDet.Sum(a => a.Qty);
            objIssuedMaster.Active = true;
            objIssuedMaster.Status = IssuedItemStatus.WaitingForAvailability.ToString();
            objIssuedMaster.IndentRefNo = _jobCard.JobCardNo;
            objIssuedMaster.VehicleId = Convert.ToInt64(_jobCard.VehicleId);
            objIssuedMaster.IssueUserId = objcore.GetUserIdByUsername(User.Identity.Name);
            db.InventoryIssuedMasters.AddObject(objIssuedMaster);
            db.SaveChanges();
            // insert into Inventory Issued Details
            int Slno = 0; string ProductStatus = string.Empty;
            foreach (tbl_jobcard_details item in _itemDet)
            {
                InventoryIssuedDetail objIssueDet = new InventoryIssuedDetail();
                if (_itemDet.Count() == 1) Slno = 1;
                else Slno = Slno + 1;
                string IssuedNo = objIssuedMaster.IssueNo;
                //string UserName = objcore.GetUserNameByUserId((Guid)_jobCard.IndAppUserId);
                InventoryItemsMaster _ItemMaster = db.InventoryItemsMasters.Where(a => a.ID == item.ItemId).FirstOrDefault();
                int ItemId = Convert.ToInt32(item.ItemId);
                // long CampusId = Convert.ToInt64(_jobCard.CampusId);
                double AvailableQuantity = GetAvailQty(ItemId);
                double IssuedQty = (double)item.Qty;
                if (AvailableQuantity < IssuedQty)
                    ProductStatus = "WaitingForAvailability";
                else
                    ProductStatus = "Available";
                objIssueDet.Slno = Slno;
                objIssueDet.IssueNo = IssuedNo;
                objIssueDet.ReceiptNo = "";
                objIssueDet.UserName = User.Identity.Name;
                objIssueDet.ItemId = _ItemMaster.ID;
                objIssueDet.ItemText = _ItemMaster.ItemText;
                objIssueDet.IssuedDt = null;
                objIssueDet.IssuedQty = IssuedQty;
                objIssueDet.Total = null;
                objIssueDet.Status = ProductStatus;
                db.InventoryIssuedDetails.AddObject(objIssueDet);
                db.SaveChanges();
            }
            // set Indent Status to In progress
            _jobCard.Status = JobCardItemStatus.InProgress.ToString();
            UpdateModel<tbl_jobcard>(_jobCard);
            db.SaveChanges();
            return Json(new { success = true, Id = objIssuedMaster.Id, msg = "JobCard Issued has been done successfully." });
        }
        [HttpGet]
        public ActionResult EditJobCardDetails(long? Id)
        {
            var jbCardDet = db.tbl_jobcard_details.Where(a => a.Id == Id).FirstOrDefault();
            var _JbCard = db.tbl_jobcard.Where(a => a.Id == jbCardDet.JobcardId).FirstOrDefault();
            ViewBag.Campus = _JbCard.tbl_vehicles.VehicleRegNum;
            ViewBag.CampusId = jbCardDet.ItemId;
            ViewBag.VatTax = jbCardDet.VatTax;
            ViewBag.ServiceTax = jbCardDet.ServiceTax;
            ViewBag.ItemId = new SelectList(db.InventoryItemsMasters.Where(a => a.Active == true), "ID", "ItemText", jbCardDet.ItemId);
            decimal AvailQty = (decimal)GetAvailQty((int)jbCardDet.ItemId);
            ViewBag.AvailQty = AvailQty;
            InventoryItemsMaster _itemMaster = db.InventoryItemsMasters.Single(a => a.ID == jbCardDet.ItemId);
            ViewBag.Price = _itemMaster.Amount;
            return PartialView("_EditJobCardDetails", new JobCardManagement(null, jbCardDet));
        }
        [HttpPost]
        public ActionResult EditIndentDetails(long Id, int ItemId, double QtyReq, int ReorderLevel, int LeadTime, string PurposeFor, string Specifications, double? VatTax, double? SrvcTax)
        {
            string Procure = string.Empty;
            var IndentDet = db.tbl_jobcard_details.Where(a => a.Id == Id).FirstOrDefault();
            var Indent = db.tbl_jobcard.FirstOrDefault(a => a.Id == IndentDet.JobcardId);

            try
            {
                decimal AvailQty = (decimal)GetAvailQty(ItemId);
                if (AvailQty > 0)
                    Procure = "Stock";
                else
                    Procure = "Purchase";
                // update here 
                IndentDet.ItemId = ItemId;
                IndentDet.Qty = QtyReq;
                IndentDet.ReorderLevel = ReorderLevel;
                IndentDet.LeadTime = LeadTime;
                IndentDet.PurposeFor = PurposeFor;
                IndentDet.Specifications = Specifications;
                IndentDet.VatTax = VatTax;
                IndentDet.ServiceTax = Convert.ToDouble(SrvcTax);
                IndentDet.Procure = Procure;
                db.SaveChanges();
                return Json(new { success = true, Id = IndentDet.JobcardId, msg = "JobCard item details has been updated  successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, Id = IndentDet.JobcardId, msg = "Jobcard item updation failed." + ex });
            }
            // return RedirectToAction("Edit", "indent", new { Area = "Inventory", Id = IndentDet.IndentId });
        }

        #endregion Edit

        #region Delete
        public ActionResult Delete(long id)
        {
            try
            {
                tbl_jobcard _jbCard = db.tbl_jobcard.Where(a => a.Id == id).SingleOrDefault();
                if (_jbCard.Status == "Closed" && _jbCard.ServiceId != null)
                {
                    tbl_veh_service_schedules vehSch = db.tbl_veh_service_schedules.Where(a => a.Id == _jbCard.ServiceId).SingleOrDefault();
                    if (vehSch != null)
                    {
                        vehSch.IsDone = false;
                        vehSch.DoneDt = null;
                        vehSch.DoneBy = null;
                        vehSch.ServiceStation = null;
                        db.SaveChanges();
                    }
                }
                db.tbl_jobcard.DeleteObject(_jbCard);
                db.SaveChanges();
                return RedirectToAction("Index", "JobCard");
            }
            catch (Exception)
            {

                throw;
            }
        }
        public ActionResult DeleteStockDetails(string Id)
        {
            string IsEdit = Id.Split('_')[1];
            long IndentDetId = Convert.ToInt64(Id.Split('_')[0]);
            if (IsEdit == "1")
            {
                tbl_jobcard_details IndentRegDet = db.tbl_jobcard_details.Where(a => a.Id == IndentDetId).FirstOrDefault();
                long IndentId = (long)IndentRegDet.JobcardId;
                db.tbl_jobcard_details.DeleteObject(IndentRegDet);
                db.SaveChanges();
                return RedirectToAction("Edit", "JobCard", new { Id = IndentId });
            }
            else
            {
                jbCrdDetails = (List<tbl_jobcard_details>)Session["List"];
                jbCrdDetails.RemoveAll(a => a.Id == Convert.ToInt32(Id.Split('_')[0]));

                List<tbl_jobcard_details> _TempList = new List<tbl_jobcard_details>();
                int IndDetId = 1;
                foreach (tbl_jobcard_details _ind in jbCrdDetails)
                {
                    _TempList.Add(new tbl_jobcard_details
                    {
                        Id = _ind.Id,
                        ItemId = _ind.ItemId,
                        ReorderLevel = _ind.ReorderLevel,
                        Qty = _ind.Qty,
                        LeadTime = _ind.LeadTime,
                        Procure = _ind.Procure,
                        PurposeFor = _ind.PurposeFor,
                        Specifications = _ind.Specifications
                    });
                    IndDetId += 1;
                }
                jbCrdDetails = _TempList;
                ViewData["IsEdit"] = "0";
                return PartialView("_JobCardDetailsList", jbCrdDetails);
            }

        }
        #endregion Delete

        #region View

        [HttpGet]
        public ActionResult Details(long Id)
        {
            tbl_jobcard jobCardDet = db.tbl_jobcard.Where(a => a.Id == Id).SingleOrDefault();
            List<tbl_jobcard_details> _jobCardList = db.tbl_jobcard_details.Where(a => a.JobcardId == jobCardDet.Id).ToList();
            return View(new JobCardManagement(jobCardDet, _jobCardList));
        }

        #endregion

        #region Custom Methods
        [HttpGet]
        public JsonResult GetVehicleServiceNumbers(long VehicleId, long? Id)
        {
            return Json(new { success = true, serviceList = objcore.GetVehicleServiceNumbers(VehicleId, Id) }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult CheckJobCardProcurement(int Id)
        {
            int isCorrect = checkJobCardPurchaseOrder(Id);
            if (isCorrect == 0)
                return Json(new { success = false, Id = 0, msg = "Jobcard has already issued for purchase order." });
            if (isCorrect == 2)
                return Json(new { success = false, Id = 0, msg = "Purchase order has already exists for Issue item(s) details" });
            else
                return Json(new { success = true, Id = 0, msg = "" });
        }

        public int checkJobCardPurchaseOrder(int Id)
        {
            InventoryIssuedMaster _issueMaster = db.InventoryIssuedMasters.FirstOrDefault(a => a.Id == Id);
            string JobCardNo = string.Empty;
            if (!string.IsNullOrEmpty(_issueMaster.IndentRefNo))
                JobCardNo = db.tbl_jobcard.FirstOrDefault(a => a.JobCardNo.Trim().ToUpper() == _issueMaster.IndentRefNo.Trim().ToUpper()).JobCardNo;
            else
                JobCardNo = _issueMaster.IssueNo;
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
            if (db.tbl_PurchaseOrders.Where(a => a.Source == JobCardNo && hasWaitForAvail == 0).Any())
                return 0;
            else
                return 1;
        }

        public string GetJobCardRefNo(DateTime JobcardDt, int Prefix)
        {
            string jobcardRefNo = string.Empty;
            int number = GetLastJobCardSerialNumber(JobcardDt);
            string seqNo = number.ToString("D" + Prefix);
            jobcardRefNo = JobcardDt.Date.ToString("yyMM") + seqNo;
            return jobcardRefNo;
        }

        public int GetLastJobCardSerialNumber(DateTime JobcardDt)
        {
            int i = 0;
            string num = string.Empty;
            try
            {
                var jobCardList = db.tbl_jobcard.Where(a => a.JobCardDt.Value.Year == JobcardDt.Date.Year).ToList();
                num = jobCardList.LastOrDefault().JobCardNo.Trim().ToString();
                i = num.Length == 8 ? Convert.ToInt32(num.Substring(num.Length - 4)) : i;
                return i + 1;
            }
            catch
            {
                return i;
            }
        }

        public bool VerifyJobCardRefNumber(string JobCardNo)
        {
            return db.tbl_jobcard.Where(a => a.JobCardNo.Trim() == JobCardNo.Trim()).Any();
        }

        [HttpGet]
        public JsonResult CurrentOdoMeterReadByVehicle(long VehicleId)
        {
            return Json(new { success = true, _currentOdoMeter = objcore.GetCurrentOdoMeterReadByVehicle(VehicleId) }, JsonRequestBehavior.AllowGet);
        } 
        #endregion

        
    }
}
