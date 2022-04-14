using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;
using ClosedXML.Excel;
using System.Data.SqlClient;
using AsyncUploaderDemo.Models;
using System.Globalization;
using System.IO;
using System.Data.OleDb;
using System.Web.Configuration;
using System.Data.Objects;


namespace FMS.Controllers
{

    public class CardAssignmentController : Controller
    {

        #region ctors
        private FMSDBEntities db;
        private core objCore;
        private List<tbl_card_assignments> _CardList;
        public CardAssignmentController()
        {
            db = new FMSDBEntities();
            objCore = new core();
            _CardList = new List<tbl_card_assignments>();



        }
        #endregion

        public ActionResult Index()
        {
            var CardAssignments = db.tbl_card_assignments.Where(a => a.IsActive == true).ToList();
            return View(CardAssignments);
        }

        public ActionResult GetCardComments(Int64 CardId)
        {
            tbl_card_assignments cardDet = db.tbl_card_assignments.Where(a => a.Id == CardId).FirstOrDefault();
            return PartialView("_CardComments", cardDet);
        }

        #region Add  CardAssignments
        [HttpGet]
        public ActionResult Create()
        {
            ViewBag.ClientId = objCore.LoadClients();
            ViewBag.ProjectId = objCore.LoadProjects();
            ViewBag.VehicleId = objCore.LoadVehicles();

            return View(new tbl_card_assignments());
        }
        [HttpPost]
        public ActionResult Create(tbl_card_assignments tbl_card_assignments)
        {
            ViewBag.ClientID = new SelectList(db.tbl_clients.Where(a => a.Status == true), "ID", "CompanyName");
            ViewBag.VehicleID = new SelectList(db.tbl_vehicles.Where(a => a.Status == true), "ID", "VehicleRegNum");
            ViewBag.ProjectID = new SelectList(db.tbl_projects.Where(a => a.IsActive == true), "ID", "ProjectName");

            try
            {
                if (ValidateForm(tbl_card_assignments, true))
                {
                    tbl_card_assignments.IsActive = true;
                    tbl_card_assignments.IsClosed = false;
                    db.tbl_card_assignments.AddObject(tbl_card_assignments);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                return View(tbl_card_assignments);
            }
            return View(tbl_card_assignments);
        }
        #endregion

        #region Edit CardAssignmnets
        [HttpGet]

        public ActionResult Edit(long id)
        {
            tbl_card_assignments tbl_card_assignment = db.tbl_card_assignments.Single(t => t.Id == id);
            ViewBag.ClientId = new SelectList(objCore.LoadClients(), "Value", "Text", tbl_card_assignment.ClientId);
            ViewBag.VehicleId = new SelectList(objCore.LoadVehicles(), "Value", "Text", tbl_card_assignment.tbl_vehicles.ID);
            ViewBag.ProjectId = new SelectList(objCore.LoadProjects(), "Value", "Text", tbl_card_assignment.tbl_projects.Id);
            ViewBag.PackageId = new SelectList(objCore.LoadPackages(), "Value", "Text", tbl_card_assignment.PackageId);

            return View(tbl_card_assignment);
        }

        [HttpPost]
        public ActionResult Edit(long ID, tbl_card_assignments tbl_card_assignments)
        {

            ViewBag.ClientId = objCore.LoadClients();
            ViewBag.VehicleId = objCore.LoadVehicles();
            ViewBag.ProjectId = objCore.LoadProjects();
            var updatetbl_card_Assingment = db.tbl_card_assignments.SingleOrDefault(s => s.Id == ID);
            try
            {
                if (ValidateForm(tbl_card_assignments, false))
                {


                    if (updatetbl_card_Assingment.CardNo.Trim().ToUpper() != tbl_card_assignments.CardNo.Trim().ToUpper())
                    {

                        var isduplicate = db.tbl_card_assignments.Where(a => a.CardNo.Trim().ToUpper() == tbl_card_assignments.CardNo.Trim().ToUpper() && a.IsActive == true).Any();
                        if (isduplicate == true)
                            ModelState.AddModelError("CardNo", "Card No already exists.");
                        else
                        {
                            UpdateModel(updatetbl_card_Assingment);
                            db.SaveChanges();
                            return RedirectToAction("Index");
                        }

                    }
                    else
                    {
                        UpdateModel(updatetbl_card_Assingment);
                        db.SaveChanges();
                        return RedirectToAction("Index");
                    }
                }
            }
            //tbl_card_assignment.IsActive = true;
            // db.tbl_card_assignments.Attach(tbl_card_assignment);
            // db.ObjectStateManager.ChangeObjectState(tbl_card_assignment, EntityState.Modified);
            // db.SaveChanges();
            // return RedirectToAction("Index");
            catch (Exception ex)
            {
                return View(tbl_card_assignments);
            }
            return View(tbl_card_assignments);
        }

        #endregion
        public bool ValidateForm(tbl_card_assignments card)
        {

            if (card.ClientId == null || card.ClientId.ToString().Trim().Length == 0)
                ModelState.AddModelError("ClientId", "Please enter Client.");

            if (card.VehicleId == null || card.VehicleId.ToString().Trim().Length == 0)
                ModelState.AddModelError("VehicleId", "Please enter Vehicle.");

            if (card.ProjectId == null || card.ProjectId.ToString().Trim().Length == 0)
                ModelState.AddModelError("ProjectId", "Please enter Project.");

            if (card.PackageId == null || card.PackageId.ToString().Trim().Length == 0)
                ModelState.AddModelError("PackageId", "Please Enter Package");

            if (card.CardNo == null || card.CardNo.ToString().Trim().Length == 0)
                ModelState.AddModelError("CardNo", "Please Enter CardNo");

            return ModelState.IsValid;
        }

        ////Validation For Edit
        //public bool ValidateForm(tbl_card_assignments card, bool isEdit)
        //{

        //    if (card.ClientId == null || card.ClientId.ToString().Trim().Length == 0)
        //        ModelState.AddModelError("ClientId", "Please enter Client.");

        //    if (card.VehicleId == null || card.VehicleId.ToString().Trim().Length == 0)
        //        ModelState.AddModelError("VehicleId", "Please enter Vehicle.");

        //    if (card.ProjectId == null || card.ProjectId.ToString().Trim().Length == 0)
        //        ModelState.AddModelError("ProjectId", "Please enter Project.");

        //    if (card.PackageId == null || card.PackageId.ToString().Trim().Length == 0)
        //        ModelState.AddModelError("PackageId", "Please Enter Package");

        //    if (card.CardNo == null || card.CardNo.ToString().Trim().Length == 0)
        //        ModelState.AddModelError("CardNo", "Please Enter CardNo");

        //    if (isEdit == true)
        //    {
        //        var isduplicate = db.tbl_card_assignments.Where(a => a.CardNo.Trim().ToUpper() == card.CardNo.Trim().ToUpper() && a.IsActive == true).Any();

        //        if (isduplicate == true)

        //            ModelState.AddModelError("CardNo", "CardNo has already exists.");


        //    }

        //    if (card.CreatedBy == null || card.CreatedBy.ToString().Trim().Length == 0)
        //        ModelState.AddModelError("CreatedBy", "Please Enter CreatedBy");



        //    return ModelState.IsValid;
        //}

        #region Delete CardAssignments
        public ActionResult Delete(long id)
        {
            tbl_card_assignments card_assignmnets = db.tbl_card_assignments.Single(t => t.Id == id);
            return View(card_assignmnets);
        }
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(long id)
        {
            try
            {
                tbl_card_assignments tbl_card_assignments = db.tbl_card_assignments.Single(t => t.Id == id);
                tbl_card_assignments.IsActive = false;
                TryUpdateModel(tbl_card_assignments);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return View();
            }
        }
        #endregion


        public bool ValidateForm(tbl_card_assignments card, bool isCreate)
        {

            if (card.ClientId == null || card.ClientId.ToString().Trim().Length == 0)
                ModelState.AddModelError("ClientId", "Please enter Client.");

            if (card.VehicleId == null || card.VehicleId.ToString().Trim().Length == 0)
                ModelState.AddModelError("VehicleId", "Please enter Vehicle.");

            if (card.ProjectId == null || card.ProjectId.ToString().Trim().Length == 0)
                ModelState.AddModelError("ProjectId", "Please enter Project.");

            if (card.PackageId == null || card.PackageId.ToString().Trim().Length == 0)
                ModelState.AddModelError("PackageId", "Please Enter Package");

            if (card.CardNo == null || card.CardNo.ToString().Trim().Length == 0)
                ModelState.AddModelError("CardNo", "Please Enter CardNo");

            if (isCreate == true)
            {
                var isduplicate = db.tbl_card_assignments.Where(a => a.CardNo.Trim().ToUpper() == card.CardNo.Trim().ToUpper() && a.IsActive == true).Any();

                if (isduplicate == true)

                    ModelState.AddModelError("CardNo", "CardNo has already exists.");


            }

            if (card.CreatedBy == null || card.CreatedBy.ToString().Trim().Length == 0)
                ModelState.AddModelError("CreatedBy", "Please Enter CreatedBy");



            return ModelState.IsValid;
        }

        #region Bulk Card Assignment

        [HttpGet]
        public ActionResult BulkCardAssignments()
        {
            ViewBag.ClientId = objCore.LoadClients();
            ViewBag.ProjectId = objCore.LoadProjects();
            return View();
        }

        [HttpGet]
        public ActionResult GetCardsList(Int64 ClientId, Int64 ProjectId)
        {
            try
            {
                DateTime prevMonth = DateTime.Now.AddMonths(-1);
                DateTime currMonth = DateTime.Now;
                ViewBag.VehicleId = objCore.LoadVehicles();
                var packDet = (from p in db.tbl_package_client_rates.AsEnumerable()
                               where p.ClientId == ClientId && p.ProjectId == ProjectId && p.IsActive == true
                               && p.Approved == true && ((p.EffectiveDt.Value.Month == prevMonth.Month && p.EffectiveDt.Value.Year == prevMonth.Year)
                               || (p.EffectiveDt.Value.Month == currMonth.Month && p.EffectiveDt.Value.Year == currMonth.Year))
                               select new tbl_package_client_rates
                               {
                                   Id = p.Id,
                                   PackModel = p.tbl_clients.CompanyName + "-" + p.tbl_projects.ProjectName + "-" + p.tbl_projects.ProjectCode + "-" + p.tbl_vehicle_types.VehicleType + "-" +
                                   p.tbl_vehicle_models.VehicleModelName + "-" + Convert.ToString(p.WorkingDays.Value) + (p.TimeUnit == 1 ? "Days" : "Months"),
                                   EffectiveDt = p.EffectiveDt,
                                   ExpiredDt = p.ExpiredDt
                               }).ToList();

                return PartialView("_GetCardsList", packDet);
            }
            catch (Exception)
            {
                throw;
            }
        }


        [HttpPost]
        public ActionResult SaveBulkCardAssignments(FormCollection frm)
        {
            try
            {
                List<string> rowIds = new List<string>();
                foreach (string Key in frm.AllKeys)
                {
                    if (Key.Contains("PackageId_"))
                        rowIds.Add(frm.Get(Key).ToString());
                }

                Int64 ClientId, ProjectId, VehicleId, PackageId;
                DateTime? MonthFor;
                string CardNumber = string.Empty;

                ClientId = string.IsNullOrEmpty(frm["ClientId"]) ? 0 : Convert.ToInt64(frm["ClientId"]);
                ProjectId = string.IsNullOrEmpty(frm["ProjectId"]) ? 0 : Convert.ToInt64(frm["ProjectId"]);

                foreach (string formKey in rowIds)
                {
                    VehicleId = string.IsNullOrEmpty(frm["VehicleTypeId_" + formKey]) ? 0 : Convert.ToInt64(frm["VehicleTypeId_" + formKey]);
                    PackageId = string.IsNullOrEmpty(frm["PackageId_" + formKey]) ? 0 : Convert.ToInt64(frm["PackageId_" + formKey]);
                    MonthFor = string.IsNullOrEmpty(frm["MonthDate_" + formKey]) ? (DateTime?)null : Convert.ToDateTime(frm["MonthDate_" + formKey]);
                    CardNumber = frm["CardNo_" + formKey];

                    if (PackageId != 0 && VehicleId != 0 && CardNumber != "")
                    {
                        tbl_card_assignments objCard = new tbl_card_assignments();
                        objCard.ClientId = ClientId;
                        objCard.CardNo = CardNumber;
                        objCard.ProjectId = ProjectId;
                        objCard.PackageId = PackageId;
                        objCard.MonthDate = MonthFor;
                        objCard.IsActive = true;
                        objCard.VehicleId = VehicleId;
                        objCard.CreatedBy = User.Identity.Name;
                        objCard.CreatedOn = DateTime.Now;
                        objCard.IsClosed = false;

                        db.tbl_card_assignments.AddObject(objCard);
                    }

                }
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                core.ExceptionLogging(ex.Message.ToString(), User.Identity.Name, ex.StackTrace);
                return Json(new { success = false, msg = "An error occured while processing your request.Please try again" });
            }
            return Json(new { success = true, msg = "Card Assignment has done successfully" });
        }

        public JsonResult GetPackages(long ClientId, long ProjectId)
        {
            try
            {
                var packDet = (from p in db.tbl_package_client_rates.AsEnumerable()
                               where p.ClientId == ClientId && p.ProjectId == ProjectId && p.IsActive == true
                               && p.Approved == true
                               select new
                               {
                                   Id = p.Id,
                                   Package = p.tbl_clients.CompanyName + "-" + p.tbl_projects.ProjectCode + "-" + p.tbl_vehicle_types.VehicleType + "-" +
                                       p.tbl_vehicle_models.VehicleModelName + "-" + Convert.ToString(p.WorkingDays.Value) + (p.TimeUnit == 1 ? "Days" : "Months"),
                                   p.EffectiveDt,
                                   p.ExpiredDt
                               }).ToList();

                return Json(packDet, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json((new { Id = 0, Package = "No records" }), JsonRequestBehavior.AllowGet);
            }

        }

        public JsonResult GetMonthlyPackages(long ClientId, long ProjectId, string SelectedMonth, Int64? PackageId)
        {
            try
            {
                DateTime CurrentMonth = string.IsNullOrEmpty(SelectedMonth) ? DateTime.Now : Convert.ToDateTime(SelectedMonth);
                var packDet = (from p in db.tbl_package_client_rates.AsEnumerable()
                               where p.ClientId == ClientId && p.ProjectId == ProjectId && p.IsActive == true
                               && p.Approved == true
                               select new
                               {
                                   Id = p.Id,
                                   Package = p.tbl_clients.CompanyName + "-" + p.tbl_projects.ProjectCode + "-" + p.tbl_vehicle_types.VehicleType + "-" +
                                       p.tbl_vehicle_models.VehicleModelName + "-" + Convert.ToString(p.WorkingDays.Value) + (p.TimeUnit == 1 ? "Days" : "Months"),
                                   p.EffectiveDt,
                                   p.ExpiredDt
                               }).ToList();
                if (PackageId.HasValue)
                {
                    packDet = packDet.Where(a => a.Id == PackageId).ToList(); // filter by package id
                }
                else if (string.IsNullOrEmpty(SelectedMonth))
                {
                    packDet = packDet.Where(p => (p.ExpiredDt.Value < CurrentMonth.Date)).ToList(); // filter expired packages
                }
                else if (!string.IsNullOrEmpty(SelectedMonth))
                {
                    packDet = packDet.Where(p => (p.EffectiveDt.Value.Month == CurrentMonth.Month && p.EffectiveDt.Value.Year == CurrentMonth.Year)).ToList(); // filter selected month packages
                }

                return Json(packDet, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json((new { Id = 0, Package = "No records" }), JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetPackageDetailsbyPackage(long PackageId)
        {
            try
            {
                var PackDet = (from p in db.tbl_package_client_rates.AsEnumerable()
                               where p.Id == PackageId
                               select new
                               {
                                   RouteId = p.tbl_locations.RouteId,
                                   RouteName = p.tbl_locations.Location,
                                   PackageAmt = p.PackageAmt,
                                   LogInTime = p.LoginTime,
                                   LogOutTime = p.LogoutTime
                               }).FirstOrDefault();
                return Json(PackDet, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json((new { Id = 0, Package = "No records" }), JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult VerifyCardNumber(string cardNumber)
        {
            bool IsExist = false;
            try
            {
                _CardList = (List<tbl_card_assignments>)Session["CardsList"];

                if (_CardList != null && _CardList.Count() != 0)
                {
                    IsExist = _CardList.Where(a => a.CardNo.Trim().ToUpper() == cardNumber.Trim().ToUpper() && a.IsClosed == false).Any();
                }
                if (!IsExist)
                {
                    IsExist = objCore.VerifyCardNumber(cardNumber);
                }

                return Json(new { IsExist = IsExist }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { IsExist = IsExist }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetEffectedDate(Int64 PackageId)
        {
            try
            {
                var packDet = (from m in db.tbl_package_client_rates
                               where m.IsActive == true && m.Id == PackageId
                               select m).FirstOrDefault();

                if (packDet != null && packDet.EffectiveDt.HasValue)
                {
                    return Json(new { PackDate = Convert.ToDateTime(packDet.EffectiveDt.Value).ToShortDateString() }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { PackDate = DateTime.Now.Date.ToShortDateString() }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { PackDate = "" }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult VerifyPackageAssignToOtherVeh(Int64 PackageId)
        {
            var IsExistPackageAssignToVeh = (from m in db.tbl_card_assignments
                                             where m.IsActive == true && m.PackageId == PackageId
                                             && m.IsClosed == false
                                             select m).Any();
            return Json(IsExistPackageAssignToVeh, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult CloseCardWithComments(Int64 PackageId)
        {
            tbl_card_assignments cardDet = db.tbl_card_assignments.Where(a => a.PackageId == PackageId && a.IsActive == true && a.IsClosed == false).FirstOrDefault();
            return PartialView("_CloseCardWithComments", cardDet);
        }


        [HttpPost]
        public ActionResult CloseCardWithComments(FormCollection frm)
        {
            try
            {
                Int64 cardId = !string.IsNullOrEmpty(frm["CardId"]) ? Convert.ToInt64(frm["CardId"]) : 0;
                string comment = frm["Comments"];
                if (cardId != 0)
                {
                    var CardDet = db.tbl_card_assignments.Where(a => a.Id == cardId).SingleOrDefault();
                    CardDet.Comment = comment;
                    CardDet.IsClosed = true;
                    db.SaveChanges();
                }
                return Json(new { success = true, msg = "Thank you for your comment, card has been released successfully" });
            }
            catch (Exception ex)
            {
                core.ExceptionLogging(ex.Message.ToString(), User.Identity.Name, ex.StackTrace);
                return Json(new { success = false, msg = "An error occured while processing your request.Please try again" });
            }
        }

        public Int32 CloseCardFromVehicle(String CardNumber)
        {
            Int64 CardId = objCore.GetCardId(CardNumber);
            if (CardId != 0)
            {
                var CardDet = db.tbl_card_assignments.Where(a => a.Id == CardId).SingleOrDefault();
                CardDet.Comment = "User select that NoCAB option so auto closed by " + User.Identity;
                CardDet.IsClosed = true;
                return db.SaveChanges();
            }
            return 0;
        }
        public Int32 AddCardNumberToVehicle(Int64? ClientId, Int64? VehicleId, Int64? ProjectId, Int64? PackageId, string CardNo, DateTime? MonthFor)
        {
            tbl_card_assignments objCardAssign = new tbl_card_assignments();
            objCardAssign.ClientId = ClientId;
            objCardAssign.VehicleId = VehicleId;
            objCardAssign.ProjectId = ProjectId;
            objCardAssign.PackageId = PackageId;
            objCardAssign.CardNo = CardNo;
            objCardAssign.MonthDate = MonthFor;
            objCardAssign.CreatedBy = User.Identity.Name;
            objCardAssign.CreatedOn = DateTime.Now;
            objCardAssign.IsClosed = false;
            objCardAssign.IsActive = true;

            db.tbl_card_assignments.AddObject(objCardAssign);

            return db.SaveChanges();
        }

        #endregion

        #region Card Entry

        [HttpGet]
        public ActionResult CardEntry()
        {
            ViewBag.CardNo = objCore.BindCardNumbers();
            return View();
        }
        [HttpGet]
        public ActionResult GetCardDetails(Int64 cardId, string logDate)
        {
            try
            {
                tbl_card_assignments _cardAssignementDet = db.tbl_card_assignments.Where(a => a.Id == cardId).FirstOrDefault();
                DateTime _logdate = string.IsNullOrEmpty(logDate) ? DateTime.Now.Date : Convert.ToDateTime(logDate);

                // validate package by logdate 
                if (!objCore.ValidatePackageByLogDate(_logdate, Convert.ToInt64(_cardAssignementDet.PackageId)))
                {
                    ViewBag.Status = 1;
                    ViewBag.Message = "Package doesn't exist with given logdate.Please enter valid logdate.";
                }

                if (_cardAssignementDet != null)
                {

                    ViewBag.ClientId = new SelectList(objCore.LoadClients(), "Value", "Text", _cardAssignementDet.ClientId);
                    ViewBag.ProjectId = new SelectList(db.tbl_projects.Where(a => a.ClientId == _cardAssignementDet.ClientId && a.IsActive == true), "Id", "ProjectName", _cardAssignementDet.ProjectId);
                    ViewBag.VehicleId = new SelectList(objCore.LoadVehicles(), "Value", "Text", _cardAssignementDet.VehicleId);
                    ViewBag.VehicleTypeID = new SelectList(objCore.LoadVehicleTypes(), "Value", "Text", _cardAssignementDet.tbl_vehicles.VehicleTypeID);
                    ViewBag.PackageId = _cardAssignementDet.PackageId;

                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return PartialView("_GetCardDetails");
        }
        [HttpPost]
        public ActionResult CardEntry(FormCollection frm)
        {
            try
            {
                DateTime? logdate = string.IsNullOrEmpty(frm["LogDate"]) ? (DateTime?)null : Convert.ToDateTime(frm["LogDate"]);
                Int64 cardId = string.IsNullOrEmpty(frm["CardNo"]) ? 0 : Convert.ToInt64(frm["CardNo"]);
                string cardNumber = string.Empty;
                string TripeType = Convert.ToString(frm["TripeType"]);
                if (cardId != 0)
                {
                    tbl_card_assignments _cardAssignementDet = db.tbl_card_assignments.Where(a => a.Id == cardId).FirstOrDefault();
                    if (_cardAssignementDet != null && _cardAssignementDet.CardNo != null)
                    {
                        cardNumber = _cardAssignementDet.CardNo;
                    }
                }


                // verify duplicate log entries 
                if (objCore.VerifyExistLogSheetByCardNumber(logdate.Value, cardNumber, TripeType))
                {
                    return Json(new { success = false, msg = "LogSheet has already exists with the card number " + cardNumber + " , date " + logdate.Value.ToShortDateString() + " and trip " + TripeType });
                }

                Int64 ClientId = string.IsNullOrEmpty(frm["ClientId"]) ? 0 : Convert.ToInt64(frm["ClientId"]);
                Int64 ProjectId = string.IsNullOrEmpty(frm["ProjectId"]) ? 0 : Convert.ToInt64(frm["ProjectId"]);
                Int64 PackageId = string.IsNullOrEmpty(frm["PackageId"]) ? 0 : Convert.ToInt64(frm["PackageId"]);
                Int64 VehicleId = string.IsNullOrEmpty(frm["VehicleId"]) ? 0 : Convert.ToInt64(frm["VehicleId"]);
                Int64? SeaterId = null;

                string Locations = (string.IsNullOrEmpty(frm["RouteId"]) ? "" : Convert.ToString(frm["RouteId"])) + "-" +
                                   (string.IsNullOrEmpty(frm["RouteName"]) ? "" : Convert.ToString(frm["RouteName"]));
                Int64 DriverID = string.IsNullOrEmpty(frm["DriverID"]) ? 0 : Convert.ToInt64(frm["DriverID"]);
                long VehicleModelID = string.IsNullOrEmpty(frm["VehicleModelID"]) ? 0 : Convert.ToInt64(frm["VehicleModelID"]);
                long VehicleTypeID = string.IsNullOrEmpty(frm["VehicleTypeID"]) ? 0 : Convert.ToInt64(frm["VehicleTypeID"]);
                decimal PackageAmt = string.IsNullOrEmpty(frm["PackageAmt"]) ? 0 : Convert.ToDecimal(frm["PackageAmt"]);
                string LogInTime = Convert.ToString(frm["LogInTime"]);
                string LogOutTime = Convert.ToString(frm["LogOutTime"]);
                decimal NetAmount = string.IsNullOrEmpty(frm["NetAmount"]) ? 0 : Convert.ToDecimal(frm["NetAmount"]);
                string AlterVehNumber = frm["AlternateVehNumber"];
                string ContactNumber = frm["ContactNumber"];

                if (VehicleId != 0)
                {
                    tbl_vehicles vehDet = db.tbl_vehicles.Where(a => a.Status == true && a.ID == VehicleId).FirstOrDefault();
                    SeaterId = vehDet.SeaterId;
                }

                // Create Object for LogSheet
                tbl_log_sheets objLogSheet = new tbl_log_sheets();
                objLogSheet.LogDate = logdate;
                objLogSheet.CardNumber = cardNumber;
                objLogSheet.LogSheetNum = DateTime.Now.ToString("yyMMff") + objCore.GetLogSheetCount(Convert.ToDateTime(logdate)).ToString("D" + 4);
                objLogSheet.ClientID = ClientId;
                objLogSheet.ProjectId = ProjectId;
                objLogSheet.PackId = PackageId;
                objLogSheet.VehicleID = VehicleId;
                objLogSheet.SeaterID = SeaterId;
                objLogSheet.TripeType = TripeType;
                objLogSheet.Location = Locations;
                objLogSheet.DriverID = DriverID;
                objLogSheet.VehicleModelID = VehicleModelID;
                objLogSheet.VehicleTypeID = VehicleTypeID;
                objLogSheet.NetAmount = PackageAmt;
                objLogSheet.ShiftTime = LogInTime;
                objLogSheet.ReachTime = LogOutTime;
                objLogSheet.LogSheetType = "PACKAGE";
                if (!string.IsNullOrEmpty(AlterVehNumber))
                {
                    objLogSheet.AlterVehNum = AlterVehNumber;
                    objLogSheet.ContactNumber = ContactNumber;
                    objLogSheet.IsAdhoc = true;
                }
                objLogSheet.CreatedDt = DateTime.Now;
                objLogSheet.UserName = User.Identity.Name;
                objLogSheet.LogEndDate = objLogSheet.LogDate;
                objLogSheet.Status = true;
                objLogSheet.NetAmount = NetAmount;

                db.tbl_log_sheets.AddObject(objLogSheet);
                db.SaveChanges();

            }
            catch (Exception)
            {
                return Json(new { success = false, msg = "An error occured while proccessing your request." });
            }
            return Json(new { success = true, msg = "Card entry has been done successfully" });
        }

        #endregion

        #region User Card Entry

        [HttpGet]
        public ActionResult UserCardEntry()
        {
            ViewBag.CardNo = objCore.BindCardNumbers();
            return View();
        }
        [HttpGet]
        public ActionResult GetUserCardDetails(Int64 cardId, string logDate)
        {
            try
            {
                tbl_card_assignments _cardAssignementDet = db.tbl_card_assignments.Where(a => a.Id == cardId).FirstOrDefault();
                DateTime _logdate = string.IsNullOrEmpty(logDate) ? DateTime.Now.Date : Convert.ToDateTime(logDate);
                ViewBag.Status = 0;

                // validate package by logdate 
                if (!objCore.ValidatePackageByLogDate(_logdate, Convert.ToInt64(_cardAssignementDet.PackageId)))
                {
                    ViewBag.Status = 1;
                    ViewBag.Message = "Package doesn't exist with given logdate.Please enter valid logdate.";
                }

                if (_cardAssignementDet != null)
                {

                    ViewBag.ClientId = new SelectList(objCore.LoadClients(), "Value", "Text", _cardAssignementDet.ClientId);
                    ViewBag.ProjectId = new SelectList(db.tbl_projects.Where(a => a.ClientId == _cardAssignementDet.ClientId && a.IsActive == true), "Id", "ProjectName", _cardAssignementDet.ProjectId);
                    ViewBag.VehicleId = new SelectList(objCore.LoadVehicles(), "Value", "Text", _cardAssignementDet.VehicleId);
                    ViewBag.VehicleTypeID = new SelectList(objCore.LoadVehicleTypes(), "Value", "Text", _cardAssignementDet.tbl_vehicles.VehicleTypeID);
                    ViewBag.PackageId = _cardAssignementDet.PackageId;

                    UserCardEntryModel objCardModel = new UserCardEntryModel();
                    objCardModel.ClientId = _cardAssignementDet.ClientId;
                    objCardModel.VehicleId = _cardAssignementDet.VehicleId;
                    objCardModel.PackageId = _cardAssignementDet.PackageId;
                    objCardModel.VehicleTypeId = _cardAssignementDet.ClientId;
                    objCardModel.VehicleTypeId = _cardAssignementDet.tbl_vehicles.VehicleTypeID;
                    objCardModel.VehicleModelId = _cardAssignementDet.tbl_vehicles.VehicleModelID;
                    objCardModel.ProjectId = _cardAssignementDet.ProjectId;
                    objCardModel.DriverId = objCore.GetDriverIdByVehicle(Convert.ToInt64(_cardAssignementDet.VehicleId));

                    ViewBag.CustomModel = objCardModel;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return PartialView("_GetUserCardDetails");
        }
        [HttpPost]
        public ActionResult UserCardEntry(FormCollection frm)
        {
            try
            {
                DateTime? logdate = string.IsNullOrEmpty(frm["LogDate"]) ? (DateTime?)null : Convert.ToDateTime(frm["LogDate"]);
                Int64 cardId = string.IsNullOrEmpty(frm["CardNo"]) ? 0 : Convert.ToInt64(frm["CardNo"]);
                string cardNumber = string.Empty;
                string TripeType = Convert.ToString(frm["TripeType"]);
                if (cardId != 0)
                {
                    tbl_card_assignments _cardAssignementDet = db.tbl_card_assignments.Where(a => a.Id == cardId).FirstOrDefault();
                    if (_cardAssignementDet != null && _cardAssignementDet.CardNo != null)
                    {
                        cardNumber = _cardAssignementDet.CardNo;
                    }
                }
                // verify duplicate log entries 
                if (objCore.VerifyExistLogSheetByCardNumber(logdate.Value, cardNumber, TripeType))
                {
                    return Json(new { success = false, msg = "LogSheet has already exists with the card number " + cardNumber + " , date " + logdate.Value.ToShortDateString() + " and trip " + TripeType });
                }

                Int64 ClientId = string.IsNullOrEmpty(frm["ClientId"]) ? 0 : Convert.ToInt64(frm["ClientId"]);
                Int64 ProjectId = string.IsNullOrEmpty(frm["ProjectId"]) ? 0 : Convert.ToInt64(frm["ProjectId"]);
                Int64 PackageId = string.IsNullOrEmpty(frm["PackageId"]) ? 0 : Convert.ToInt64(frm["PackageId"]);
                Int64 VehicleId = string.IsNullOrEmpty(frm["VehicleId"]) ? 0 : Convert.ToInt64(frm["VehicleId"]);
                Int64? SeaterId = null;
                string Locations = (string.IsNullOrEmpty(frm["RouteId"]) ? "" : Convert.ToString(frm["RouteId"])) + "-" +
                                   (string.IsNullOrEmpty(frm["RouteName"]) ? "" : Convert.ToString(frm["RouteName"]));
                Int64 DriverID = string.IsNullOrEmpty(frm["DriverID"]) ? 0 : Convert.ToInt64(frm["DriverID"]);
                long VehicleModelID = string.IsNullOrEmpty(frm["VehicleModelID"]) ? 0 : Convert.ToInt64(frm["VehicleModelID"]);
                long VehicleTypeID = string.IsNullOrEmpty(frm["VehicleTypeID"]) ? 0 : Convert.ToInt64(frm["VehicleTypeID"]);
                decimal PackageAmt = string.IsNullOrEmpty(frm["PackageAmt"]) ? 0 : Convert.ToDecimal(frm["PackageAmt"]);
                string LogInTime = Convert.ToString(frm["LogInTime"]);
                string LogOutTime = Convert.ToString(frm["LogOutTime"]);
                decimal NetAmount = string.IsNullOrEmpty(frm["NetAmount"]) ? 0 : Convert.ToDecimal(frm["NetAmount"]);
                string AlterVehNumber = frm["AlternateVehNumber"];
                string ContactNumber = frm["ContactNumber"];

                if (VehicleId != 0)
                {
                    tbl_vehicles vehDet = db.tbl_vehicles.Where(a => a.Status == true && a.ID == VehicleId).FirstOrDefault();
                    SeaterId = vehDet.SeaterId;
                }

                // Create Object for LogSheet
                tbl_log_sheets objLogSheet = new tbl_log_sheets();
                objLogSheet.LogDate = logdate;
                objLogSheet.CardNumber = cardNumber;
                objLogSheet.LogSheetNum = DateTime.Now.ToString("yyMMff") + objCore.GetLogSheetCount(Convert.ToDateTime(logdate)).ToString("D" + 4);
                objLogSheet.ClientID = ClientId;
                objLogSheet.ProjectId = ProjectId;
                objLogSheet.PackId = PackageId;
                objLogSheet.VehicleID = VehicleId;
                objLogSheet.SeaterID = SeaterId;
                objLogSheet.TripeType = TripeType;
                objLogSheet.Location = Locations;
                objLogSheet.DriverID = DriverID;
                objLogSheet.VehicleModelID = VehicleModelID;
                objLogSheet.VehicleTypeID = VehicleTypeID;
                objLogSheet.ShiftTime = LogInTime;
                objLogSheet.ReachTime = LogOutTime;
                objLogSheet.LogSheetType = "PACKAGE";
                if (!string.IsNullOrEmpty(AlterVehNumber))
                {
                    objLogSheet.AlterVehNum = AlterVehNumber;
                    objLogSheet.ContactNumber = ContactNumber;
                    objLogSheet.IsAdhoc = true;
                }
                objLogSheet.CreatedDt = DateTime.Now;
                objLogSheet.UserName = User.Identity.Name;
                objLogSheet.LogEndDate = objLogSheet.LogDate;
                objLogSheet.Status = true;
                objLogSheet.NetAmount = NetAmount;

                db.tbl_log_sheets.AddObject(objLogSheet);
                db.SaveChanges();

            }
            catch (Exception)
            {
                return Json(new { success = false, msg = "An error occured while proccessing your request." });
            }
            return Json(new { success = true, msg = "Card entry has been done successfully" });
        }

        [HttpGet]
        public ActionResult GetLogbyCardNumber(string cardNumber)
        {
            List<tbl_log_sheets> _logList = (from m in db.tbl_log_sheets
                                             where m.Status == true && m.CardNumber.Trim() == cardNumber.Trim()
                                             select m).ToList();
            return PartialView("_LogListByCard", _logList);
        }

        #endregion

        #region Bulk Card Entry
        [HttpGet]
        public ActionResult BulkCardEntry()
        {
            ViewBag.ClientId = objCore.LoadClients();
            return View();
        }
        [HttpGet]
        public ActionResult BulkCardEntryList(long ClientId, long ProjectId, string logDate)
        {
            DateTime _logdate = string.IsNullOrEmpty(logDate) ? DateTime.Now.Date : Convert.ToDateTime(logDate);
            ViewBag.NoVehicleId = objCore.LoadVehicles();
            ViewBag.LogDate = _logdate;
            List<UserCardEntryModel> _userCardList = objCore.GetCardEntryDetailsListByClientProjectAndDate(ClientId, ProjectId, _logdate);
            return PartialView("_BulkCardEntryList", _userCardList);
        }
        [HttpPost]
        public JsonResult BulkCardEntry(FormCollection frm, tbl_log_sheets logsheets)
        {
            string Msg = string.Empty;
            try
            {
                List<string> rowIds = new List<string>();
                foreach (string Key in frm.AllKeys)
                {
                    if (Key.Contains("UniqueId_"))
                        rowIds.Add(frm.Get(Key).ToString());
                }
                Int64? ClientId, ProjectId, VehicleId, PackageId, VehicleTypeId, VehicleModelId, SeaterId, LocationId, DriverId;
                DateTime? LogDate;
                string CardNumber, LogInTime, LogOutTime, AlterVehNum, Mobile, Locations, RouteId = string.Empty;
                bool? PickUp, Drop, IsNoCab = false;
                decimal? NetAmount = 0;
                string[] TripType = new string[0];
                ClientId = string.IsNullOrEmpty(frm["ClientId"]) ? 0 : Convert.ToInt64(frm["ClientId"]);
                ProjectId = string.IsNullOrEmpty(frm["ProjectId"]) ? 0 : Convert.ToInt64(frm["ProjectId"]);
                LogDate = string.IsNullOrEmpty(frm["LogDate"]) ? (DateTime?)null : Convert.ToDateTime(frm["LogDate"]);

                // Set Fields Values to Null
                VehicleId = SeaterId = VehicleModelId = DriverId = (long?)null;
                Locations = AlterVehNum = string.Empty;

                foreach (string formKey in rowIds)
                {
                    TripType = new string[1];
                    IsNoCab = string.IsNullOrEmpty(frm["NoCab_" + formKey]) ? false : frm.GetValues("NoCab_" + formKey).Contains("true");
                    PackageId = string.IsNullOrEmpty(frm["PackageId_" + formKey]) ? 0 : Convert.ToInt64(frm["PackageId_" + formKey]);
                    VehicleTypeId = string.IsNullOrEmpty(frm["VehicleTypeId_" + formKey]) ? 0 : Convert.ToInt64(frm["VehicleTypeId_" + formKey]);
                    LogInTime = Convert.ToString(frm["LogInTime_" + formKey]);
                    LogOutTime = Convert.ToString(frm["LogOutTime_" + formKey]);
                    NetAmount = string.IsNullOrEmpty(frm["NetAmount_" + formKey]) ? 0 : Convert.ToDecimal(frm["NetAmount_" + formKey]);

                    if ((bool)IsNoCab)  // If User Select NoCab Option
                    {
                        CardNumber = frm["CardNo_" + formKey];

                        // First Close Card Number 
                        Int32 Result = CloseCardFromVehicle(frm["CardNumber_" + formKey]);
                        // end of close card 

                        RouteId = frm["Location_" + formKey];
                        RouteId = RouteId.Contains("-") ? RouteId.Split('-')[1].ToString() : RouteId;
                        LocationId = string.IsNullOrEmpty(RouteId) ? 0 : Convert.ToInt64(objCore.GetLocationId(RouteId));
                        Locations = objCore.GetLocation(Convert.ToInt64(LocationId));
                        VehicleId = string.IsNullOrEmpty(frm["AlterVehId_" + formKey]) ? 0 : Convert.ToInt64(frm["AlterVehId_" + formKey]);

                        if (VehicleId != 0)
                        {
                            tbl_vehicles VehDet = db.tbl_vehicles.Where(a => a.ID == VehicleId).FirstOrDefault();
                            SeaterId = Convert.ToInt64(VehDet.SeaterId);
                            DriverId = objCore.GetDriverIdByVehicle(Convert.ToInt64(VehicleId));
                            VehicleModelId = Convert.ToInt64(VehDet.VehicleModelID);

                            if (!objCore.VerifyCardNumber(CardNumber)) // Verify Whether Card number is closed or not 
                            {
                                Result = AddCardNumberToVehicle(ClientId, VehicleId, ProjectId, PackageId, CardNumber, LogDate);
                            }
                        }
                    }
                    else
                    {
                        VehicleId = string.IsNullOrEmpty(frm["VehicleId_" + formKey]) ? 0 : Convert.ToInt64(frm["VehicleId_" + formKey]);
                        SeaterId = string.IsNullOrEmpty(frm["SeaterId_" + formKey]) ? 0 : Convert.ToInt64(frm["SeaterId_" + formKey]);
                        DriverId = string.IsNullOrEmpty(frm["DriverId_" + formKey]) ? 0 : Convert.ToInt64(frm["DriverId_" + formKey]);
                        VehicleModelId = string.IsNullOrEmpty(frm["VehicleModelId_" + formKey]) ? 0 : Convert.ToInt64(frm["VehicleModelId_" + formKey]);
                        CardNumber = frm["CardNumber_" + formKey];
                        LocationId = string.IsNullOrEmpty(frm["LocationId_" + formKey]) ? 0 : Convert.ToInt64(frm["LocationId_" + formKey]);
                        Locations = objCore.GetLocation(Convert.ToInt64(LocationId));
                        AlterVehNum = frm["AlterVehNum_" + formKey];
                    }
                    Mobile = frm["ContactNumber_" + formKey];
                    PickUp = string.IsNullOrEmpty(frm["PickUp_" + formKey]) ? false : frm.GetValues("PickUp_" + formKey).Contains("true");
                    Drop = string.IsNullOrEmpty(frm["Drop_" + formKey]) ? false : frm.GetValues("Drop_" + formKey).Contains("true");

                    if (PickUp.Value && Drop.Value) // If User Select Pickup & Drop Checkbox then Add Values in String Array
                    {
                        TripType = new string[2];
                        TripType[0] = "PICKUP";
                        TripType[1] = "DROP";
                    }
                    else if (PickUp.Value)
                    {
                        TripType = new string[1];
                        TripType[0] = "PICKUP";
                    }
                    else if (Drop.Value)
                    {
                        TripType = new string[1];
                        TripType[0] = "DROP";
                    }


                    if (string.IsNullOrEmpty(TripType[0])) // Verify Trip is null or blank
                    {

                        continue;  // If user doesn't select any of the trip then skip this
                    }




                    foreach (string Trip in TripType)
                    {
                        if (!string.IsNullOrEmpty(Trip))  // Verify Trip not null or not blank
                        {
                            // Verify duplicate log entries 
                            if (objCore.VerifyExistLogSheetByCardNumber(LogDate.Value, CardNumber, Trip))
                            {

                                continue;  // If LogEntry has exist with the cardnumber , date , trip
                            }
                            // Add object here 
                            tbl_log_sheets objLogDet = new tbl_log_sheets();
                            objLogDet.ClientID = ClientId;
                            objLogDet.VehicleID = VehicleId;
                            objLogDet.ProjectId = ProjectId;
                            objLogDet.CardNumber = CardNumber;
                            objLogDet.VehicleTypeID = VehicleTypeId;
                            objLogDet.VehicleModelID = VehicleModelId == 0 ? (Int64?)null : VehicleModelId;
                            objLogDet.Location = Locations;
                            objLogDet.DriverID = DriverId == 0 ? (Int64?)null : DriverId;
                            objLogDet.PackId = PackageId;
                            objLogDet.SeaterID = SeaterId == 0 ? (Int64?)null : SeaterId;
                            objLogDet.LogDate = LogDate;
                            objLogDet.LogSheetNum = DateTime.Now.ToString("yyMMff") + objCore.GetLogSheetCount(Convert.ToDateTime(LogDate)).ToString("D" + 4);
                            objLogDet.NetAmount = NetAmount;
                            objLogDet.ShiftTime = LogInTime;
                            objLogDet.ReachTime = LogOutTime;
                            objLogDet.Pax = 0;
                            objLogDet.LogSheetType = "PACKAGE";

                            if (!(bool)IsNoCab)
                            {
                                if (!string.IsNullOrEmpty(AlterVehNum))
                                {
                                    objLogDet.AlterVehNum = AlterVehNum;
                                    objLogDet.ContactNumber = Mobile;
                                    objLogDet.IsAdhoc = true;
                                }
                            }

                            objLogDet.CreatedDt = DateTime.Now;
                            objLogDet.UserName = User.Identity.Name;
                            objLogDet.LogEndDate = objLogDet.LogDate;
                            objLogDet.Status = true;
                            objLogDet.TripeType = Trip;
                            db.tbl_log_sheets.AddObject(objLogDet);
                            db.SaveChanges();
                            Msg = "Bulk Log entry has done successfully";
                        }

                    }
                }
                if (string.IsNullOrEmpty(Msg))//if record not save in database then change  Msg text.
                {
                    Msg = "Please select any Record\n\t\t (OR)\nRecord(s) already exist";
                }

            }

            catch (Exception ex)
            {
                core.ExceptionLogging(ex.Message.ToString(), User.Identity.Name, ex.StackTrace);
                return Json(new { success = false, msg = "An error occured while processing your request.Please try again" });
            }

            return Json(new { success = true, msg = Msg });
        }

        private bool LogEntryValidate1(tbl_log_sheets logsheets, bool p)
        {
            throw new NotImplementedException();
        }



        #region Export To Excel For Logsheet Mgmt.
        public ActionResult ExportToExcel()
        {
            var LogSheets = (from l in db.tbl_log_sheets
                             where l.Status == true
                             select l).OrderBy(a => a.ID).ToList();
            string Filename = "Logsheets_" + DateTime.Now.ToString("hhmmss") + ".xlsx";
            if (LogSheets == null)
            {
                MemoryStream ms1 = new MemoryStream();
                ms1.Seek(0, SeekOrigin.Begin);
                return File(ms1, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml", Filename);
            }
            MemoryStream ms = LogsheetsExportToExcel(LogSheets);
            if (ms != null)
            {
                ms.Seek(0, SeekOrigin.Begin);
                return File(ms, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml", Filename);
            }
            return View();
        }

        //By sarath on 01-02-2016 to 02-02-2016
        public MemoryStream LogsheetsExportToExcel(List<tbl_log_sheets> list)
        {
            string sheetName = "Logsheetreport";
            var wb = new XLWorkbook();
            try
            {
                core objCore = new core();
                var ws = wb.Worksheets.Add(sheetName);
                // ws.Cell("A2").Style.Font.FontColor = XLColor.FromHtml("#FF0040");
                ws.Cell("A2").Style.Font.Bold = true;
                //ws.Cell("B2").Style.Font.FontColor = XLColor.FromHtml("#FF0040");
                ws.Cell("B2").Style.Font.Bold = true;
                //ws.Cell("C2").Style.Font.FontColor = XLColor.FromHtml("#FF0040");
                ws.Cell("C2").Style.Font.Bold = true;
                // ws.Cell("D2").Style.Font.FontColor = XLColor.FromHtml("#FF0040");
                ws.Cell("D2").Style.Font.Bold = true;
                // ws.Cell("E2").Style.Font.FontColor = XLColor.FromHtml("#FF0040");
                ws.Cell("E2").Style.Font.Bold = true;
                // ws.Cell("F2").Style.Font.FontColor = XLColor.FromHtml("#FF0040");
                ws.Cell("F2").Style.Font.Bold = true;
                // ws.Cell("G2").Style.Font.FontColor = XLColor.FromHtml("#FF0040");
                ws.Cell("G2").Style.Font.Bold = true;
                //ws.Cell("H2").Style.Font.FontColor = XLColor.FromHtml("#FF0040");
                ws.Cell("H2").Style.Font.Bold = true;
                //ws.Cell("I2").Style.Font.FontColor = XLColor.FromHtml("#FF0040");
                ws.Cell("I2").Style.Font.Bold = true;
                // ws.Cell("J2").Style.Font.FontColor = XLColor.FromHtml("#FF0040");
                ws.Cell("J2").Style.Font.Bold = true;
                //ws.Cell("K2").Style.Font.FontColor = XLColor.FromHtml("#FF0040");
                ws.Cell("K2").Style.Font.Bold = true;
                ws.Cell("L2").Style.Font.Bold = true;
                ws.Cell("M2").Style.Font.Bold = true;
                ws.Cell("N2").Style.Font.Bold = true;
                ws.Cell("O2").Style.Font.Bold = true;
                ws.Cell("P2").Style.Font.Bold = true;
                ws.Cell("Q2").Style.Font.Bold = true;
                ws.Cell("R2").Style.Font.Bold = true;
                ws.Cell("S2").Style.Font.Bold = true;
                ws.Cell("T2").Style.Font.Bold = true;
                ws.Cell("U2").Style.Font.Bold = true;
                ws.Cell("V2").Style.Font.Bold = true;
                ws.Cell("W2").Style.Font.Bold = true;
                ws.Cell("X2").Style.Font.Bold = true;
                ws.Cell("Y2").Style.Font.Bold = true;
                ws.Cell("Z2").Style.Font.Bold = true;
                ws.Cell("AA2").Style.Font.Bold = true;

                ws.Cell("A2").Value = "Client Name";
                //ws.Cell("B2").Value = "Vehicle Reg#";
                ws.Cell("C2").Value = "Vehicle Type";
                ws.Cell("D2").Value = "Driver Name";
                ws.Cell("E2").Value = "Seater Capacity";
                ws.Cell("F2").Value = "LogDate";
                ws.Cell("G2").Value = "LogSheet Number";
                ws.Cell("H2").Value = "Tripe Type";
                ws.Cell("I2").Value = "Shift Time";
                ws.Cell("J2").Value = "Reach Time";
                ws.Cell("K2").Value = "Location";
                ws.Cell("L2").Value = "StartKM";
                ws.Cell("M2").Value = "EndKM";
                ws.Cell("N2").Value = "TotalKMs";
                ws.Cell("O2").Value = "Approved";
                ws.Cell("P2").Value = "Passenger Name";
                ws.Cell("Q2").Value = "Remarks";
                ws.Cell("R2").Value = "LogSheet Type";
                ws.Cell("S2").Value = "LogEndDate";
                ws.Cell("T2").Value = "Toll Charges";
                ws.Cell("U2").Value = "NetAmount";
                ws.Cell("V2").Value = "GuestName";
                ws.Cell("W2").Value = "TotalHrs";
                ws.Cell("X2").Value = "Project Name";
                ws.Cell("Y2").Value = "Card Number";
                ws.Cell("Z2").Value = "Alternate Vehicle Number";
                ws.Cell("AA2").Value = "Contact Number";

                int i = 4;
                foreach (var item in list)
                {
                    ws.Cell("A" + i).SetValue(item.tbl_clients.Address);
                    //ws.Cell("B" + i).SetValue((item.tbl_vehicles.VehicleRegNum == null ? "" : item.tbl_vehicles.VehicleRegNum));
                    ws.Cell(("C" + i)).SetValue((item.tbl_vehicle_types == null ? "" : item.tbl_vehicle_types.VehicleType));
                    ws.Cell(("D" + i)).SetValue((item.tbl_drivers.FirstName == null ? "" : item.tbl_drivers.FirstName));
                    ws.Cell(("E" + i)).SetValue((item.tbl_seaters.Seater == null ? "" : item.tbl_seaters.Seater));
                    ws.Cell(("F" + i)).SetValue(item.LogDate);
                    ws.Cell(("G" + i)).SetValue(item.LogSheetNum);
                    ws.Cell(("H" + i)).SetValue((item.TripeType == null ? "" : item.TripeType));
                    ws.Cell(("I" + i)).SetValue((item.ShiftTime == null ? "" : item.ShiftTime));
                    ws.Cell(("J" + i)).SetValue((item.ReachTime == null ? "" : item.ReachTime));
                    ws.Cell(("K" + i)).SetValue((item.Location == null ? "" : item.Location));
                    ws.Cell(("L" + i)).SetValue((item.StartKM == null ? 0 : item.StartKM));
                    ws.Cell(("M" + i)).SetValue((item.EndKM == null ? 0 : item.EndKM));
                    ws.Cell(("N" + i)).SetValue((item.TotalKM == null ? 0 : item.TotalKM));
                    ws.Cell(("O" + i)).SetValue((item.Approved == null ? 0 : item.Approved));
                    ws.Cell(("P" + i)).SetValue((item.PassengerEmpID == null ? "" : item.PassengerEmpID));
                    ws.Cell(("Q" + i)).SetValue((item.Remark == null ? "" : item.Remark));
                    ws.Cell(("R" + i)).SetValue((item.LogSheetType == null ? "" : item.LogSheetType));
                    ws.Cell(("S" + i)).SetValue(item.LogEndDate);
                    ws.Cell(("T" + i)).SetValue((item.TollChrg == null ? 0 : item.TollChrg));
                    ws.Cell(("U" + i)).SetValue((item.NetAmount == null ? 0 : item.NetAmount));
                    ws.Cell(("V" + i)).SetValue(item.GuestName == null ? "" : item.GuestName);
                    ws.Cell(("W" + i)).SetValue((item.TotalHrs == null ? 0 : item.TotalHrs));
                    ws.Cell(("X" + i)).SetValue(objCore.GetProject((long)item.ProjectId));
                    ws.Cell(("Y" + i)).SetValue((item.CardNumber == null ? "" : item.CardNumber));
                    ws.Cell(("Z" + i)).SetValue((item.AlterVehNum == null ? "" : item.AlterVehNum));
                    ws.Cell(("AA" + i)).SetValue((item.ContactNumber == null ? "" : item.ContactNumber));
                    i++;
                }
                ws.Columns().AdjustToContents();
            }
            catch (Exception ex)
            {

            }
            MemoryStream ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms;
        }
        #endregion

        //By Sarath on 27-02-2016
        public bool LogEntryValidate(tbl_log_sheets log, string logDate, long PackId, long CardNumber, bool isExist)
        {



            if (isExist == true)
            {
                var isduplicate = db.tbl_log_sheets.Where(a => a.CardNumber.Trim().ToUpper() == log.CardNumber.Trim().ToUpper() && a.PackId == PackId).Any();

                if (isduplicate == true)

                    ModelState.AddModelError("CardNo", "LogEntry has already exists.");


            }

            return ModelState.IsValid;
        }


        // 22-2-2016 by vinod NoCab Entry List
        #region Nocab Entry
        [HttpGet]
        public ActionResult NocabEntry(long ClientId, long ProjectId, string logDate)
        {
            DateTime _logdate = string.IsNullOrEmpty(logDate) ? DateTime.Now.Date : Convert.ToDateTime(logDate);
            tbl_log_sheets objLogSheet = new tbl_log_sheets();
            objLogSheet.ClientID = ClientId;
            objLogSheet.ProjectId = ProjectId;
            objLogSheet.LogDate = _logdate;
            ViewBag.Location = new SelectList(objCore.LoadLocations(), "Value", "Text");
            ViewBag.VehicleId = new SelectList(objCore.LoadVehicles(), "Value", "Text");
            ViewBag.VehicleTypeId = new SelectList(objCore.LoadVehicleTypes(), "Value", "Text");
            ViewBag.PackageId = new SelectList(objCore.NOCabLoadPackages(ClientId, ProjectId,logDate), "Value", "Text");
            return PartialView("_NocabEntry", objLogSheet);
        }

        [HttpGet]
        public ActionResult NoCabList(long ClientId, long ProjectId, string logDate)
        {
            DateTime _logdate = string.IsNullOrEmpty(logDate) ? DateTime.Now.Date : Convert.ToDateTime(logDate);
            tbl_log_sheets objLogSheet = new tbl_log_sheets();
            objLogSheet.ProjectId = ProjectId;
            objLogSheet.ClientID = ClientId;
            objLogSheet.LogDate = _logdate;
            List<NocabListModel> _Nocab = objCore.GetNocabList(ClientId, ProjectId, _logdate);
            return PartialView("_NoCabList", _Nocab);
        }

        [HttpPost]
        public ActionResult NoCabEntry(FormCollection frm)
        {
            Int32 IErrorCount;
            string FailedErrorMsg = string.Empty;
            try
            {
                Int64 ClientId, ProjectId, VehicleTypeId, PackageId;
                Int32 TotalHrs;
                DateTime? LogDate;
                string EmpName, Location, AlterVehNum, ContactNumber, CardNumber, ShiftTime, ReachTime = string.Empty;
                bool? PickUp, Drop = false;
                decimal? NetAmount = 0;
                string[] TripType = new string[0];
                ClientId = string.IsNullOrEmpty(frm["ClientId"]) ? 0 : Convert.ToInt64(frm["ClientId"]);
                ProjectId = string.IsNullOrEmpty(frm["ProjectId"]) ? 0 : Convert.ToInt64(frm["ProjectId"]);
                VehicleTypeId = string.IsNullOrEmpty(frm["VehicleTypeId"]) ? 0 : Convert.ToInt64(frm["VehicleTypeId"]);
                LogDate = string.IsNullOrEmpty(frm["LogDate"]) ? (DateTime?)null : Convert.ToDateTime(frm["LogDate"]);
                EmpName = string.IsNullOrEmpty(frm["EmpName"]) ? "" : frm["EmpName"].ToString().ToUpper();
                Location = string.IsNullOrEmpty(frm["Location"]) ? "" : frm["Location"].ToString().ToUpper();
                AlterVehNum = string.IsNullOrEmpty(frm["AlterVehNum"]) ? "" : frm["AlterVehNum"].ToString().ToUpper();
                TotalHrs = string.IsNullOrEmpty(frm["TotalHrs"]) ? 0 : Convert.ToInt32(frm["TotalHrs"]);
                PickUp = string.IsNullOrEmpty(frm["PickUp"]) ? false : frm.GetValues("PickUp").Contains("true");
                Drop = string.IsNullOrEmpty(frm["Drop"]) ? false : frm.GetValues("Drop").Contains("true");
                NetAmount = string.IsNullOrEmpty(frm["NetAmount"]) ? 0 : Convert.ToDecimal(frm["NetAmount"]);
                ContactNumber = string.IsNullOrEmpty(frm["AlterMobNum"]) ? "" : frm["AlterMobNum"].ToString().ToUpper();
                CardNumber = string.IsNullOrEmpty(frm["Card"]) ? "" : frm["Card"].ToString().ToUpper();
                ShiftTime = string.IsNullOrEmpty(frm["LogIn"]) ? "" : frm["LogIn"].ToString().ToUpper();
                ReachTime = string.IsNullOrEmpty(frm["LogOut"]) ? "" : frm["LogOut"].ToString().ToUpper();
                PackageId = string.IsNullOrEmpty(frm["PackageId"]) ? 0 : Convert.ToInt64(frm["PackageId"]);
                TripType = new string[1];
                if (PickUp.Value && Drop.Value)
                {
                    TripType = new string[2];
                    TripType[0] = "PICKUP";
                    TripType[1] = "DROP";
                }
                else if (PickUp.Value)
                {
                    TripType = new string[1];
                    TripType[0] = "PICKUP";
                }
                else if (Drop.Value)
                {
                    TripType = new string[1];
                    TripType[0] = "DROP";
                }
                IErrorCount = 0;
                foreach (string Trip in TripType)
                {
                    if (!string.IsNullOrEmpty(Trip))  // Verify Trip not null or not blank
                    {
                        if (objCore.VerifyExistingNoCabLogSheet(ClientId, ProjectId, PackageId, Convert.ToDateTime(LogDate), Trip))
                        {
                            IErrorCount = IErrorCount + 1;
                            FailedErrorMsg = Trip;
                            continue;  // To restrict multiple entries and if it is exist in the logentry
                        }
                        // Add object here 
                        tbl_log_sheets objLogDet = new tbl_log_sheets();
                        objLogDet.ClientID = ClientId;
                        objLogDet.ProjectId = ProjectId;
                        objLogDet.VehicleTypeID = VehicleTypeId;
                        objLogDet.Location = Location;
                        objLogDet.LogDate = LogDate;
                        objLogDet.LogSheetNum = DateTime.Now.ToString("yyMMff") + objCore.GetLogSheetCount(Convert.ToDateTime(LogDate)).ToString("D" + 4);
                        objLogDet.NetAmount = NetAmount;
                        objLogDet.TotalHrs = TotalHrs;
                        objLogDet.Pax = 0;
                        objLogDet.LogSheetType = "PACKAGE";
                        objLogDet.AlterVehNum = AlterVehNum;
                        objLogDet.IsNoCab = true;
                        objLogDet.CreatedDt = DateTime.Now;
                        objLogDet.UserName = User.Identity.Name;
                        objLogDet.LogEndDate = objLogDet.LogDate;
                        objLogDet.Status = true;
                        objLogDet.TripeType = Trip;
                        objLogDet.EmpName = EmpName;
                        objLogDet.ContactNumber = ContactNumber;
                        objLogDet.ShiftTime = ShiftTime;
                        objLogDet.ReachTime = ReachTime;
                        objLogDet.CardNumber = CardNumber;
                        objLogDet.PackId = PackageId;
                        db.tbl_log_sheets.AddObject(objLogDet);
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                core.ExceptionLogging(ex.Message.ToString(), User.Identity.Name, ex.StackTrace);
                return Json(new { success = false, msg = "An error occured while processing your request.Please try again" });
            }
            if (IErrorCount == 2)
            {
                return Json(new { success = false, msg = "Trip type has already exists with the entry fields" });
            }
            else if (IErrorCount == 1)
            {
                return Json(new
                {
                    success = true,
                    msg = (string.IsNullOrEmpty(FailedErrorMsg) ? "Nocab entry has submitted successfully" :
                    "Nocab entry was Unsuccessfull\nBecause" + FailedErrorMsg + " Trip Type has already exists.")
                });
            }
            return Json(new { success = true, msg = "Nocab entry has submitted successfully" });
        }

        #endregion


        #region AdHoc Entry

        [HttpGet]
        public ActionResult AdhocLogEntry(long ClientId, long ProjectId, string logDate)
        {

            tbl_card_assignments _cardAssignementDet = db.tbl_card_assignments.Where(a => a.Id == ClientId).FirstOrDefault();
            DateTime _logdate = string.IsNullOrEmpty(logDate) ? DateTime.Now.Date : Convert.ToDateTime(logDate);
            tbl_log_sheets objLogSheet = new tbl_log_sheets();
            objLogSheet.ClientID = ClientId;
            objLogSheet.ProjectId = ProjectId;
            objLogSheet.LogDate = _logdate;
            ViewBag.VehicleId = new SelectList(objCore.LoadVehicles(), "Value", "Text");
            ViewBag.VehicleTypeId = new SelectList(objCore.LoadVehicleTypes(), "Value", "Text");
            ViewBag.PackageId = new SelectList(objCore.LoadPackages(), "Value", "Text");
            return PartialView("_AdhocLogEntry", objLogSheet);
        }


        [HttpGet]
        public ActionResult AdhocList(long ClientId, long ProjectId, string logDate)
        {
            DateTime _logdate = string.IsNullOrEmpty(logDate) ? DateTime.Now.Date : Convert.ToDateTime(logDate);
            tbl_log_sheets objLogSheet = new tbl_log_sheets();
            objLogSheet.ProjectId = ProjectId;
            objLogSheet.ClientID = ClientId;
            objLogSheet.LogDate = _logdate;
            List<AdhocListModel> _AdhocList = objCore.GetAdhocListByLogDate(ClientId, ProjectId, _logdate);
            //var Adhoc = db.tbl_log_sheets.Where(a => a.IsAdhoc == true).ToList();
            return PartialView("_AdhocList", _AdhocList);


        }


        [HttpPost]
        public ActionResult AdhocLogEntry(FormCollection frm)
        {
            try
            {
                Int64 ClientId, ProjectId, VehicleTypeId, PackageId;
                Int32 TotalHrs;
                DateTime? LogDate;
                string EmpName, Location, AlterVehNum, ContactNumber = string.Empty;
                bool? PickUp, Drop = false;
                decimal? NetAmount = 0;
                string[] TripType = new string[0];
                ClientId = string.IsNullOrEmpty(frm["ClientId"]) ? 0 : Convert.ToInt64(frm["ClientId"]);
                ProjectId = string.IsNullOrEmpty(frm["ProjectId"]) ? 0 : Convert.ToInt64(frm["ProjectId"]);
                LogDate = string.IsNullOrEmpty(frm["LogDate"]) ? (DateTime?)null : Convert.ToDateTime(frm["LogDate"]);
                VehicleTypeId = string.IsNullOrEmpty(frm["VehicleTypeId"]) ? 0 : Convert.ToInt64(frm["VehicleTypeId"]);
                EmpName = string.IsNullOrEmpty(frm["EmpName"]) ? "" : frm["EmpName"].ToString().ToUpper();
                Location = string.IsNullOrEmpty(frm["Location"]) ? "" : frm["Location"].ToString().ToUpper();
                AlterVehNum = string.IsNullOrEmpty(frm["AlterVehNum"]) ? "" : frm["AlterVehNum"].ToString().ToUpper();
                TotalHrs = string.IsNullOrEmpty(frm["TotalHrs"]) ? 0 : Convert.ToInt32(frm["TotalHrs"]);
                PickUp = string.IsNullOrEmpty(frm["PickUp"]) ? false : frm.GetValues("PickUp").Contains("true");
                Drop = string.IsNullOrEmpty(frm["Drop"]) ? false : frm.GetValues("Drop").Contains("true");
                NetAmount = string.IsNullOrEmpty(frm["NetAmount"]) ? 0 : Convert.ToDecimal(frm["NetAmount"]);
                PackageId = string.IsNullOrEmpty(frm["PackageId"]) ? 0 : Convert.ToInt64(frm["PackageId"]);
                ContactNumber = string.IsNullOrEmpty(frm["Mobile"]) ? "" : frm["Mobile"].ToString().ToUpper();
                TripType = new string[1];
                if (PickUp.Value && Drop.Value)
                {
                    TripType = new string[2];
                    TripType[0] = "PICKUP";
                    TripType[1] = "DROP";
                }
                else if (PickUp.Value)
                {
                    TripType = new string[1];
                    TripType[0] = "PICKUP";
                }
                else if (Drop.Value)
                {
                    TripType = new string[1];
                    TripType[0] = "DROP";
                }

                foreach (string Trip in TripType)
                {
                    if (!string.IsNullOrEmpty(Trip))  // Verify Trip not null or not blank
                    {
                        // Add object here 
                        tbl_log_sheets objLogDet = new tbl_log_sheets();
                        objLogDet.ClientID = ClientId;
                        objLogDet.ProjectId = ProjectId;
                        objLogDet.VehicleTypeID = VehicleTypeId;
                        objLogDet.Location = Location;
                        objLogDet.LogDate = LogDate;
                        objLogDet.LogSheetNum = DateTime.Now.ToString("yyMMff") + objCore.GetLogSheetCount(Convert.ToDateTime(LogDate)).ToString("D" + 4);
                        objLogDet.NetAmount = NetAmount;
                        objLogDet.TotalHrs = TotalHrs;
                        objLogDet.Pax = 0;
                        objLogDet.LogSheetType = "PACKAGE";
                        objLogDet.AlterVehNum = AlterVehNum;
                        objLogDet.IsAdhoc = true;
                        objLogDet.CreatedDt = DateTime.Now;
                        objLogDet.UserName = User.Identity.Name;
                        objLogDet.LogEndDate = objLogDet.LogDate;
                        objLogDet.Status = true;
                        objLogDet.TripeType = Trip;
                        objLogDet.EmpName = EmpName;
                        objLogDet.ContactNumber = ContactNumber;
                        objLogDet.PackId = PackageId;
                        db.tbl_log_sheets.AddObject(objLogDet);
                        db.SaveChanges();
                    }
                }

            }
            catch (Exception ex)
            {
                core.ExceptionLogging(ex.Message.ToString(), User.Identity.Name, ex.StackTrace);
                return Json(new { success = false, msg = "An error occured while processing your request.Please try again" });
            }
            return Json(new { success = true, msg = "Adhoc Log entry has done successfully" });
        }


        #endregion

        #endregion

        public object Adhoc { get; set; }
    }
}
