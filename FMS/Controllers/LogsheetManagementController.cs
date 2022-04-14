using System;
using System.Text;
using System.Data;
using System.Data.Entity;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;
using FMS.Helpers;
using System.Globalization;
using System.Drawing;
using System.IO;
using System.Data.OleDb;
using System.Web.Configuration;
using System.Data.SqlClient;
using AsyncUploaderDemo.Models;
using ClosedXML.Excel;
using System.Web.Script.Serialization;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.html.simpleparser;
using System.Collections;

namespace FMS.Controllers
{
    [CustomAuthorizationFilter]
    public class LogsheetManagementController : Controller
    {
        #region ctors
        private FMSDBEntities db;
        private List<tbl_log_sheets> _logSheetList = new List<tbl_log_sheets>();
        private core core = new core();
        public LogsheetManagementController()
        {
            db = new FMSDBEntities();
        }
        #endregion

        #region Index Methods
        [Authorize]
        public ActionResult Index()
        {
           // List<tbl_log_sheets> _logsheetDets = db.tbl_log_sheets.Where(l => l.Status == true && (l.ClosedFlag == false || l.ClosedFlag == null)).ToList();
            //core.ConvertToUppercase<tbl_log_sheets>(_logsheetDets);
            return View();
        }
        public JsonResult GetDutyLogSheets(int iDisplayStart, int iDisplayLength, string sSearch, int iColumns,
       int iSortingCols, int iSortCol_0, string sSortDir_0,
       int sEcho, string mDataProp_Key, string StartDate, string EndDate, string LogSheetType)
        {
            var logSheetList = GetDutyLogSheetList(Convert.ToDateTime(StartDate), Convert.ToDateTime(EndDate), LogSheetType);
            var filteredLogSheet = logSheetList
                .Select(l => new[]{
                    l.ID.ToString(),
                    l.LogSheetType.ToString(),
                    l.LogDate.ToShortDateString(),
                    l.Client.ToString(),
                    l.VehicleReg.ToString(),
                    l.VehicleType.ToString(),
                    l.VehicleModel.ToString(),
                    l.Driver.ToString(),
                    l.LogSheetNum.ToString(),
                    l.TripType.ToString(),
                    l.Pax.ToString(),
                    l.ShiftTime.ToString(),
                    l.ReachTime.ToString(),
                    l.Location.ToString(),
                    l.TotalKM.ToString(),
                    l.Approved.ToString(),
                    l.Status.ToString()

                }).Where(i => string.IsNullOrEmpty(sSearch)
                     || i.Any(y => y.IndexOf(sSearch, StringComparison.InvariantCultureIgnoreCase) >= 0));
            var orderedlist = filteredLogSheet
                   .OrderByWithDirection(i => (i[iSortCol_0]).Parse(), sSortDir_0 == "asc")
                   .Skip(iDisplayStart)
                   .Take(iDisplayLength);
            var model = new
            {
                aaData = orderedlist,
                iTotalDisplayRecords = filteredLogSheet.Count(),
                iTotalRecords = filteredLogSheet.Count(),
                sEcho = sEcho.ToString()
            };
            return Json(model, JsonRequestBehavior.AllowGet);
        }

        private IEnumerable<DutylogSheet> GetDutyLogSheetList(DateTime StartDt, DateTime EndDt, string LogSheetType)
        {
            var list = new List<DutylogSheet>();
            DateTime TodayDt = DateTime.Now.Date;
            var tempLogList = (from l in db.tbl_log_sheets
                               where l.Status == true && (l.ClosedFlag == false || l.ClosedFlag == null)
                               && (l.LogDate.Value >= StartDt && l.LogDate.Value <= EndDt)
                               && (LogSheetType.Contains("All") ? true : l.LogSheetType == LogSheetType)
                               select l).AsEnumerable().ToList();
            foreach (tbl_log_sheets ls in tempLogList)
            {
                list.Add(new DutylogSheet
                {
                    ID = ls.ID,
                    LogSheetType = ls.LogSheetType,
                    LogDate = Convert.ToDateTime(ls.LogDate),
                    Client = ls.tbl_clients.CompanyName,
                    VehicleReg = (ls.VehicleID == null ? ls.AlterVehNum : ls.tbl_vehicles.VehicleRegNum),
                    VehicleType = ls.tbl_vehicle_types.VehicleType,
                    VehicleModel = (ls.VehicleModelID == null ? "" : ls.tbl_vehicle_models.VehicleModelName),
                    Driver = (ls.DriverID == null ? "" : (ls.tbl_drivers.FirstName + " " + ls.tbl_drivers.LastName)),
                    Seater = ls.SeaterID == null ? "" : ls.tbl_seaters.Seater,
                    LogSheetNum = ls.LogSheetNum,
                    TripType = ls.TripeType == null ? "" : ls.TripeType,
                    Pax = ls.Pax == null ? 0 : (int)ls.Pax,
                    ShiftTime = ls.ShiftTime == null ? "" : ls.ShiftTime,
                    ReachTime = ls.ReachTime == null ? "" : ls.ReachTime,
                    Location = ls.Location == null ? "" : ls.Location,
                    TotalKM = ls.TotalKM == null ? 0 : (int)ls.TotalKM,
                    Approved = ls.Approved == null ? 0 : (int)ls.Approved,
                    SlabRate = ls.SlabRate == null ? 0 : (decimal)ls.SlabRate,
                    FinalApprovedKm = ls.FinalApprovedKM == null ? 0 : (int)ls.FinalApprovedKM,
                    FinalSlabRate = ls.FinalSlabRate == null ? 0 : (decimal)ls.FinalSlabRate,
                    Status = (bool)ls.Status

                });
            }
            core.ConvertToUppercase<DutylogSheet>(list);
            return list.OrderBy(a => a.ID).OrderBy(a => a.FinalApprovedKm);
        }

        private IEnumerable<DutylogSheet> GetDutyLogSheetList()
        {
            var list = new List<DutylogSheet>();
            DateTime TodayDt = DateTime.Now.Date;
            var tempLogList = (from l in db.tbl_log_sheets
                               where l.Status == true
                               select l).AsEnumerable().ToList();
            foreach (tbl_log_sheets ls in tempLogList)
            {
                list.Add(new DutylogSheet
                {
                    ID = ls.ID,
                    LogSheetType = ls.LogSheetType,
                    LogDate = Convert.ToDateTime(ls.LogDate),
                    Client = ls.tbl_clients.CompanyName,
                    VehicleReg = (ls.VehicleID == null ? ls.AlterVehNum : ls.tbl_vehicles.VehicleRegNum),
                    VehicleType = ls.tbl_vehicle_types.VehicleType,
                    VehicleModel = (ls.VehicleModelID == null ? "" : ls.tbl_vehicle_models.VehicleModelName),
                    Driver = (ls.DriverID == null ? "" : (ls.tbl_drivers.FirstName + " " + ls.tbl_drivers.LastName)),
                    Seater = ls.tbl_seaters.Seater == null ? "" : ls.tbl_seaters.Seater,
                    LogSheetNum = ls.LogSheetNum,
                    TripType = ls.TripeType == null ? "" : ls.TripeType,
                    Pax = ls.Pax == null ? 0 : (int)ls.Pax,
                    ShiftTime = ls.ShiftTime == null ? "" : ls.ShiftTime,
                    Location = ls.Location == null ? "" : ls.Location,
                    TotalKM = ls.TotalKM == null ? 0 : (int)ls.TotalKM,
                    Approved = ls.Approved == null ? 0 : (int)ls.Approved,
                    PassengerEmpID = ls.PassengerEmpID == null ? "" : ls.PassengerEmpID,
                    SlabRate = ls.SlabRate == null ? 0 : (decimal)ls.SlabRate,
                    FinalApprovedKm = ls.FinalApprovedKM == null ? 0 : (int)ls.FinalApprovedKM,
                    FinalSlabRate = ls.FinalSlabRate == null ? 0 : (decimal)ls.FinalSlabRate,
                    Status = (bool)ls.Status,
                    Remark = ls.Remark,
                    AuditRemark = ls.AuditRemark,
                    CreatedDate = ls.CreatedDt == null ? "" : ls.CreatedDt.Value.ToShortDateString(),
                    UserName = ls.UserName,
                    ModifiedDt = ls.ModifiedDt == null ? "" : ls.ModifiedDt.Value.ToShortDateString(),
                    ModifiedBy = ls.ModifiedBy,
                    AuditDt = ls.AuditDt == null ? "" : ls.AuditDt.Value.ToShortDateString(),
                    AuditedBy = ls.AuditedBy,
                    Paid = (ls.ClosedFlag == null || ls.ClosedFlag == false) ? "Un-Paid" : "Paid"
                });
            }
            core.ConvertToUppercase<DutylogSheet>(list);
            return list.OrderBy(a => a.ID).OrderBy(a => a.FinalApprovedKm);
        }
        #endregion

        #region Add LogSheets 
        [Authorize]
        public ActionResult Create()
        {
            ViewBag.LocationId = new SelectList(db.tbl_locations.Where(a => a.Status == true), "ID", "Location");
            return View();
        }
        public ActionResult AddNewDutyLogsheet()
        {
            ViewBag.VehicleTypeID = new SelectList(db.tbl_vehicle_types.Where(a => a.Status == true), "ID", "VehicleType");
            ViewBag.VehicleModelID = new SelectList(db.tbl_vehicle_models.Where(a => a.Status == true), "ID", "VehicleModelName");
            return PartialView("_AddDutyLogsheet");
        }
        [HttpPost]
        public ActionResult AddLogSheetEntry(FormCollection frm, long CID, long VID, tbl_log_sheets logsheets)
        {
            ViewBag.VehicleTypeID = new SelectList(db.tbl_vehicle_types.Where(a => a.Status == true), "ID", "VehicleType");
            ViewBag.VehicleModelID = new SelectList(db.tbl_vehicle_models.Where(a => a.Status == true), "ID", "VehicleModelName");
            tbl_vehicles _vehicle = db.tbl_vehicles.Where(c => c.Status == true && c.ID == VID).FirstOrDefault();
            long ID = 0;
            _logSheetList = (List<tbl_log_sheets>)Session["LogsheetEntryList"];
            if (_logSheetList == null)
                _logSheetList = new List<tbl_log_sheets>();

            //this is for delete list item based on this id
            if (_logSheetList.Count() == 0)
                ID = 1;
            else
                ID = _logSheetList.Count() + 1;
            // Add object to list 
            _logSheetList.Add(new tbl_log_sheets
            {
                ID = ID,
                ClientID = CID,
                VehicleID = VID,
                VehicleTypeID = logsheets.VehicleTypeID,
                VehicleModelID = logsheets.VehicleModelID,
                DriverID = logsheets.DriverID,
                SeaterID = _vehicle.SeaterId,
                LogDate = logsheets.LogDate,
                CreatedDt = DateTime.Now,
                LogSheetNum = logsheets.LogSheetNum,
                TripeType = logsheets.TripeType,
                Pax = logsheets.Pax == null ? null : logsheets.Pax,
                TravelPax = logsheets.TravelPax == null ? null : logsheets.TravelPax,
                ShiftTime = logsheets.ShiftTime,
                Location = logsheets.Location.ToUpper(),
                FullLocation = logsheets.FullLocation == null ? "" : logsheets.FullLocation.ToUpper(),
                StartKM = logsheets.StartKM == null ? null : logsheets.StartKM,
                EndKM = logsheets.EndKM == null ? null : logsheets.EndKM,
                TotalKM = logsheets.TotalKM == null ? null : logsheets.TotalKM,
                Approved = logsheets.Approved == null ? null : logsheets.Approved,
                FinalApprovedKM = logsheets.FinalApprovedKM == null ? null : logsheets.FinalApprovedKM,
                PassengerEmpID = logsheets.PassengerEmpID == null ? null : logsheets.PassengerEmpID,
                SlabRate = logsheets.SlabRate == null ? null : logsheets.SlabRate,
                LogSheetType = "TRIP",
                FinalSlabRate = logsheets.FinalSlabRate == null ? null : logsheets.FinalSlabRate,
                Remark = logsheets.Remark == null ? null : logsheets.Remark,
                IsCash = logsheets.IsCash,
                UserName = User.Identity.Name.ToUpper(),
                Status = true
            });
            Session["LogsheetEntryList"] = _logSheetList;
            ViewBag.message = "";
            return PartialView("_AddLogSheetEntry", _logSheetList);
        }

        [HttpPost]
        public ActionResult SaveLogSheetEntry(FormCollection frm)
        {
            try
            {
                // List of billing structure 
                List<tbl_log_sheets> _logList = (List<tbl_log_sheets>)Session["LogsheetEntryList"];
                core.ConvertToUppercase<tbl_log_sheets>(_logList);
                foreach (tbl_log_sheets _log in _logList)
                { // check log sheet number
                    if (!db.tbl_log_sheets.Where(a => a.LogSheetNum.ToUpper().Trim() == _log.LogSheetNum.ToUpper().Trim() && a.Status == true).Any())
                    {
                        db.tbl_log_sheets.AddObject(_log);
                        db.SaveChanges();
                    }
                }
                Session["LogsheetEntryList"] = null;
                core.LoggingEntries("LogSheet Mgmt.", "LogSheets were created ", User.Identity.Name);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return View();
            }
        }

        // Save LogSheet Entries 

        public int SaveUnSavedLogSheetEntries()
        {
            try
            {
                List<tbl_log_sheets> _logList = (List<tbl_log_sheets>)Session["LogsheetEntryList"];
                core.ConvertToUppercase<tbl_log_sheets>(_logList);
                foreach (tbl_log_sheets _log in _logList)
                { // check log sheet number
                    if (!db.tbl_log_sheets.Where(a => a.LogSheetNum.ToUpper().Trim() == _log.LogSheetNum.ToUpper().Trim() && a.Status == true).Any())
                    {
                        db.tbl_log_sheets.AddObject(_log);
                        db.SaveChanges();
                    }
                }
                Session["LogsheetEntryList"] = null;
                core.LoggingEntries("LogSheet Mgmt.", "LogSheets were created ", User.Identity.Name);
                return 1;
            }
            catch
            {
                return 0;
            }
        }

        public ActionResult RemoveItem(long ID)
        {
            _logSheetList = (List<tbl_log_sheets>)Session["LogsheetEntryList"];
            foreach (tbl_log_sheets obj in _logSheetList)
            {
                if (obj.ID == ID) // Will match once
                {
                    //Delete the Row
                    _logSheetList.Remove(obj);
                    ReShuffleSlno();
                    break;
                }
            }
            return PartialView("_AddLogSheetEntry", _logSheetList);
        }

        public void ReShuffleSlno()
        {
            var i = 1;
            foreach (tbl_log_sheets obj in _logSheetList)
            {
                obj.ID = i;
                i++;
            }
        }
        #endregion

        #region Edit LogSheet
        public ActionResult Edit(long ID, int? viewId)
        {
            ViewBag.LocationId = new SelectList(db.tbl_locations.Where(a => a.Status == true), "ID", "Location");

            tbl_log_sheets _logEntry = db.tbl_log_sheets.Where(a => a.Status == true && a.ID == ID).SingleOrDefault();
            ViewBag.VehicleRegNum = db.tbl_vehicles.Where(a => a.Status == true && a.ID == _logEntry.VehicleID).SingleOrDefault().VehicleRegNum.ToString();
            ViewBag.VehicleTypeID = new SelectList(db.tbl_vehicle_types.Where(a => a.Status == true), "ID", "VehicleType", _logEntry.VehicleTypeID);
            ViewBag.VehicleModelID = new SelectList(db.tbl_vehicle_models.Where(a => a.Status == true), "ID", "VehicleModelName", _logEntry.VehicleModelID);
            ViewBag.ClientName = core.GetClient((long)_logEntry.ClientID);
            ViewBag.ClientID = _logEntry.ClientID;
            ViewBag.VehicleID = _logEntry.VehicleID;
            if (viewId == 1)
                TempData["ViewId"] = viewId;

            // Verify If LogSheet Type is PACKAGE
            if (_logEntry.LogSheetType == "PACKAGE")
            {
                ViewBag.PackModel = new SelectList(core.LoadPackModel(), "Value", "Text", _logEntry.PackModel);
                return PartialView("_EditPackageLogSheet", _logEntry);
            }

            return PartialView("_EditLogentry", _logEntry);
        }

        [HttpPost]
        public ActionResult Edit(long Id, long ClientID, FormCollection frm, tbl_log_sheets logentry)
        {
            //long ClientID = Convert.ToInt64(frm["cid"]);
            long VehicleID = Convert.ToInt64(frm["vid"]);
            tbl_vehicles _vehicle = db.tbl_vehicles.Where(c => c.Status == true && c.ID == VehicleID).FirstOrDefault();


            //==========================================
            ViewBag.VehicleRegNum = db.tbl_vehicles.Where(a => a.Status == true && a.ID == VehicleID).FirstOrDefault().VehicleRegNum.ToString();
            ViewBag.VehicleTypeID = new SelectList(db.tbl_vehicle_types.Where(a => a.Status == true), "ID", "VehicleType", logentry.VehicleTypeID);
            ViewBag.VehicleModelID = new SelectList(db.tbl_vehicle_models.Where(a => a.Status == true), "ID", "VehicleModelName", logentry.VehicleModelID);
            ViewBag.ClientName = core.GetClient(ClientID);
            tbl_log_sheets _logSheet = db.tbl_log_sheets.Where(a => a.Status == true && a.ID == logentry.ID).FirstOrDefault();
            core.ConvertToUppercase(logentry);
            try
            {
                // Verify LogSheet Cash or not
                if (!(_logSheet.IsCash == null ? false : (bool)_logSheet.IsCash))
                {
                    // Verify LogSheet Final Slab Rate or Final Approved KM
                    if (_logSheet.FinalApprovedKM != null && _logSheet.FinalApprovedKM != 0)
                        if (_logSheet.FinalApprovedKM != logentry.FinalApprovedKM)
                        {
                            if (!User.IsInRole("superadmin"))
                            {
                                ViewBag.Message = "User access is denied to modify final approved KM.Please contact superadmin.";
                                return PartialView("_EditLogentry", logentry);
                            }
                            _logSheet.AuditedBy = User.Identity.Name;
                            _logSheet.AuditDt = DateTime.Now;
                        }

                    if (_logSheet.FinalSlabRate != null && _logSheet.FinalSlabRate != 0)
                        if (_logSheet.FinalSlabRate != logentry.FinalSlabRate)
                        {
                            if (!User.IsInRole("superadmin"))
                            {
                                ViewBag.Message = "User access is denied to modify final slab rate.Please contact superadmin.";
                                return PartialView("_EditLogentry", logentry);
                            }
                            _logSheet.AuditedBy = User.Identity.Name;
                            _logSheet.AuditDt = DateTime.Now;
                        }
                }
                else
                {
                    if (!((logentry.FinalApprovedKM ?? 0) == 0))
                    {
                        if (!User.IsInRole("superadmin"))
                        {
                            ViewBag.Message = "User access is denied to modify final approved KM for cash logsheets.Please contact superadmin.";
                            return PartialView("_EditLogentry", logentry);
                        }
                        _logSheet.AuditedBy = User.Identity.Name;
                        _logSheet.AuditDt = DateTime.Now;
                    }

                    if (!((logentry.FinalSlabRate ?? 0) == 0))
                    {
                        if (!User.IsInRole("superadmin"))
                        {
                            ViewBag.Message = "User access is denied to modify final slab rate for cash logsheets.Please contact superadmin.";
                            return PartialView("_EditLogentry", logentry);
                        }
                        _logSheet.AuditedBy = User.Identity.Name;
                        _logSheet.AuditDt = DateTime.Now;
                    }
                }

                TryUpdateModel(_logSheet);
                _logSheet.ClientID = ClientID;
                _logSheet.VehicleID = VehicleID;
                _logSheet.VehicleModelID = _vehicle.VehicleModelID;
                _logSheet.SeaterID = _vehicle.SeaterId;
                _logSheet.Status = true;
                _logSheet.AuditedBy = User.Identity.Name;
                _logSheet.AuditDt = DateTime.Now;
                _logSheet.ModifiedBy = User.Identity.Name;
                _logSheet.ModifiedDt = DateTime.Now;
                db.SaveChanges();
                core.LoggingEntries("LogSheet Mgmt.", "LogSheets was edited with logsheet number " + logentry.LogSheetNum + "", User.Identity.Name);
                if (TempData["ViewId"] != null)
                    return Content("<script language='javascript' type='text/javascript'>$.modal.close();alert('Log sheet entry saved successfully');window.location.href = '/LogsheetManagement/SearchLogSheetReport'</script>");
                else
                    return Content("<script language='javascript' type='text/javascript'>$.modal.close();alert('Log sheet entry saved successfully');window.location.href = '/LogsheetManagement/Index'</script>");
            }
            catch (Exception ex)
            {
                ViewBag.Message = "An Error occured :" + ex.Message.ToString();
                return PartialView("_EditLogentry", logentry);
            }
        }

        // Edit LogSheet at MIS Report
        [HttpGet]
        public ActionResult EditLogSheetDet(long ID)
        {
            tbl_log_sheets logSheetDet = db.tbl_log_sheets.Where(a => a.ID == ID).FirstOrDefault();
            ViewBag.BillType = logSheetDet.tbl_clients.tbl_billing_types.BillingType;
            return PartialView("_EditLogSheetDet", logSheetDet);
        }
        [HttpPost]
        public ActionResult EditLogSheetDet(long ID, FormCollection frm)
        {
            decimal finalSlabRate = (frm["FinalSlabRate"] == null || frm["FinalSlabRate"] == "") ? 0 : Convert.ToDecimal(frm["FinalSlabRate"]);
            int finalApprovedKM = (frm["FinalApprovedKM"] == null || frm["FinalApprovedKM"] == "") ? 0 : Convert.ToInt32(frm["FinalApprovedKM"]);
            decimal finalNetAmt = (frm["FinalNetAmt"] == null || frm["FinalNetAmt"] == "" ? 0 : Convert.ToDecimal(frm["FinalNetAmt"]));
            string remark = frm["AuditRemark"];
            string BillType = frm["BillType"];
            tbl_log_sheets logSheetDet = db.tbl_log_sheets.Where(a => a.ID == ID).FirstOrDefault();
            bool IsCash = logSheetDet.IsCash == null ? false : (bool)logSheetDet.IsCash;

            if (IsCash)
                if (finalApprovedKM != 0 || finalSlabRate != 0 || finalNetAmt != 0)
                    if (!User.IsInRole("superadmin"))
                        return Json(new { success = true, msg = "User access is denied to modify final rate for the cash loghseets.Please contact superadmin." });


            // Verify LogSheet Final Slab Rate or Final Approved KM

            if (logSheetDet.FinalApprovedKM != null && logSheetDet.FinalApprovedKM != 0)
                if (!User.IsInRole("superadmin"))
                    return Json(new { success = true, msg = "User access is denied to modify final approved KM.Please contact superadmin." });

            if (logSheetDet.FinalSlabRate != null && logSheetDet.FinalSlabRate != 0)
                if (!User.IsInRole("superadmin"))
                    return Json(new { success = true, msg = "User access is denied to modify final slab rate.Please contact superadmin." });

            if (logSheetDet.FinalNetAmt != null && logSheetDet.FinalNetAmt != 0)
                if (!User.IsInRole("superadmin"))
                    return Json(new { success = true, msg = "User access is denied to modify final net amount.Please contact superadmin." });

            if (BillType == "TRIPS")
                logSheetDet.FinalSlabRate = finalSlabRate;
            if (BillType == "KILO METER")
                logSheetDet.FinalApprovedKM = finalApprovedKM;
            if (BillType == "PACKAGE")
                logSheetDet.FinalNetAmt = finalNetAmt;
            try
            {
                TryUpdateModel(logSheetDet);
                logSheetDet.AuditRemark = remark;
                logSheetDet.AuditDt = DateTime.Now;
                logSheetDet.AuditedBy = User.Identity.Name;
                db.SaveChanges();
                core.LoggingEntries("LogSheet Mgmt.", "LogSheets was edited with finalSlabrate/finalApprovedKM/finalNetAmont with logsheet " + logSheetDet.LogSheetNum + "  ", User.Identity.Name);
                return Json(new { success = true, msg = "LogSheet has been updated successfully." });
            }
            catch (Exception)
            {
                return Json(new { success = false, msg = "Sorry! An error occured while processing your request.\n Please try again." });
            }
        }
        #endregion

        #region Delete LogSheet
        [HttpGet]
        public ActionResult Delete(long ID, int? viewId)
        {
            tbl_log_sheets _logEntry = db.tbl_log_sheets.Where(a => a.Status == true && a.ID == ID).SingleOrDefault();
            if (viewId == 1)
                TempData["ViewDeleteId"] = viewId;
            return View(_logEntry);
        }
        [HttpPost]
        public ActionResult Delete(long ID, FormCollection frm)
        {
            tbl_log_sheets _logEntry = db.tbl_log_sheets.Where(a => a.Status == true && a.ID == ID).SingleOrDefault();
            try
            {
                if (_logEntry.FinalApprovedKM != null && _logEntry.FinalApprovedKM != 0)
                {
                    if (!User.IsInRole("superadmin"))
                    {
                        ViewBag.Message = "User access is denied to modify final approved KM.Please contact superadmin.";
                        return View("ShowMessage", _logEntry);
                    }
                }

                if (_logEntry.FinalSlabRate != null && _logEntry.FinalSlabRate != 0)
                {
                    if (!User.IsInRole("superadmin"))
                    {
                        ViewBag.Message = "User access is denied to modify final slab rate.Please contact superadmin.";
                        return View("ShowMessage", _logEntry);
                    }
                }
                _logEntry.Status = false;
                TryUpdateModel(_logEntry);
                db.SaveChanges();
                core.LoggingEntries("LogSheet Mgmt.", "LogSheets was deleted with the logsheet number " + _logEntry.LogSheetNum + "", User.Identity.Name);
                if (TempData["ViewDeleteId"] != null)
                    return RedirectToAction("SearchLogSheetReport");
                else
                    return RedirectToAction("Index");
            }
            catch (Exception)
            {
                return View(_logEntry);
            }
        }
        #endregion

        #region PackLogSheet Entry 
        // Package LogSheet Entry 
        [HttpGet]
        [Authorize]
        public ActionResult PackageEntry()
        {
            return View();
        }

        [Authorize]
        [HttpGet]
        public ActionResult AddPackageLogSheet()
        {
            ViewBag.VehicleTypeID = new SelectList(db.tbl_vehicle_types.Where(a => a.Status == true), "ID", "VehicleType");
            ViewBag.VehicleModelID = new SelectList(db.tbl_vehicle_models.Where(a => a.Status == true), "ID", "VehicleModelName");
            ViewBag.PackModel = core.LoadPackModel();
            return PartialView("_AddPackageLogSheet");
        }
        [HttpPost]
        public ActionResult AddPackageLogSheet(FormCollection frm, long CID, long VID, tbl_log_sheets logDet)
        {
            try
            {
                string logSheetNum = "P" + DateTime.Now.ToString("yyddmmhhss");
                while (!VerifyLogSheetNumber(logSheetNum))
                    logSheetNum = "P" + DateTime.Now.ToString("yyddmmhhss");

                tbl_vehicles vehDet = db.tbl_vehicles.Where(a => a.ID == VID).FirstOrDefault();
                long ID = 0;
                _logSheetList = (List<tbl_log_sheets>)Session["LogsheetEntryList"];
                if (_logSheetList == null)
                    _logSheetList = new List<tbl_log_sheets>();

                //this is for delete list item based on this id
                if (_logSheetList.Count() == 0)
                    ID = 1;
                else
                    ID = _logSheetList.Count() + 1;

                long PackId = frm["Package"] == null ? 0 : Convert.ToInt64(frm["Package"]);

                // LogSheet Entry 
                logDet.ID = ID;
                logDet.CreatedDt = DateTime.Now;
                logDet.UserName = User.Identity.Name;
                logDet.VehicleID = VID;
                logDet.ClientID = CID;
                logDet.Status = true;
                logDet.PackId = PackId;
                logDet.LogSheetType = "PACKAGE";
                logDet.LogSheetNum = logSheetNum;
                logDet.SeaterID = vehDet.SeaterId;

                core.ConvertToUppercase(logDet);

                _logSheetList.Add(logDet);
                Session["LogsheetEntryList"] = _logSheetList;
                ViewBag.message = "";
                return PartialView("_AddLogSheetEntry", _logSheetList);
            }
            catch
            {
                return PartialView("_AddLogSheetEntry", _logSheetList);
            }
        }

        [HttpPost]
        public ActionResult EditPackageLogSheet(long Id, long ClientID, FormCollection frm, tbl_log_sheets logentry)
        {
            long VehicleID = Convert.ToInt64(frm["vid"]);
            tbl_vehicles _vehicle = db.tbl_vehicles.Where(c => c.Status == true && c.ID == VehicleID).FirstOrDefault();


            //=================== Drop Down List ======================= //
            ViewBag.VehicleRegNum = db.tbl_vehicles.Where(a => a.Status == true && a.ID == VehicleID).FirstOrDefault().VehicleRegNum.ToString();
            ViewBag.VehicleTypeID = new SelectList(db.tbl_vehicle_types.Where(a => a.Status == true), "ID", "VehicleType", logentry.VehicleTypeID);
            ViewBag.VehicleModelID = new SelectList(db.tbl_vehicle_models.Where(a => a.Status == true), "ID", "VehicleModelName", logentry.VehicleModelID);
            ViewBag.PackModel = new SelectList(core.LoadPackModel(), "Value", "Text", logentry.PackModel);
            ViewBag.ClientName = core.GetClient(ClientID);
            tbl_log_sheets _logSheet = db.tbl_log_sheets.Where(a => a.Status == true && a.ID == logentry.ID).FirstOrDefault();
            //=================== Convert to Upper case ======================= //
            core.ConvertToUppercase(logentry);
            try
            {
                long PackId = frm["Package"] == null ? 0 : Convert.ToInt64(frm["Package"]);

                TryUpdateModel(_logSheet);
                _logSheet.ClientID = ClientID;
                _logSheet.VehicleID = VehicleID;
                _logSheet.VehicleModelID = _vehicle.VehicleModelID;
                _logSheet.SeaterID = _vehicle.SeaterId;
                _logSheet.PackId = PackId;
                _logSheet.Status = true;
                _logSheet.ModifiedBy = User.Identity.Name;
                _logSheet.ModifiedDt = DateTime.Now;
                db.SaveChanges();
                core.LoggingEntries("LogSheet Mgmt.", "LogSheets was edited with logsheet number " + logentry.LogSheetNum + "", User.Identity.Name);
                if (TempData["ViewId"] != null)
                    return Content("<script language='javascript' type='text/javascript'>$.modal.close();alert('Log sheet entry has been saved successfully');window.location.href = '/LogsheetManagement/SearchLogSheetReport'</script>");
                else
                    return Content("<script language='javascript' type='text/javascript'>$.modal.close();alert('Log sheet entry has been saved successfully');window.location.href = '/LogsheetManagement/Index'</script>");
            }
            catch (Exception ex)
            {
                ViewBag.Message = "An Error occured :" + ex.Message.ToString();
                return PartialView("_EditPackageLogSheet", logentry);
            }
        }
        public decimal CalculatePackGrossAmount(long PackId, int WrkDays, int WrkHrs, int extKM, int extHr)
        {
            tbl_package_client_rates packRate = db.tbl_package_client_rates.Where(a => a.Id == PackId && a.IsActive == true).FirstOrDefault();
            decimal OneDayAmt = 0, GrossAmt = 0, ExtKMAmt = 0, ExtHrAmt = 0;
            if (packRate != null)
            {
                OneDayAmt = Convert.ToDecimal(packRate.PackageAmt) / Convert.ToDecimal(packRate.WorkingDays);
                GrossAmt = (WrkDays * OneDayAmt);
                ExtHrAmt = (extHr * (packRate.ExtHr == null ? 0 : Convert.ToDecimal(packRate.ExtHr)));
                ExtKMAmt = (extKM * (packRate.ExtKM == null ? 0 : Convert.ToDecimal(packRate.ExtKM)));
                GrossAmt = GrossAmt + (ExtHrAmt + ExtKMAmt);
            }
            return GrossAmt;
        }
        #endregion

        #region LogSheet Search by LogSheet Number Report
        [Authorize]
        [HttpGet]
        public ActionResult SearchLogSheetReport()
        {
            return View();
        }
        [HttpPost]
        public ActionResult SearchLogSheetReport(FormCollection frm)
        {
            string LogSheetNum = frm["LogSheetNum"] == null ? "" : frm["LogSheetNum"];
            long LogId = (frm["logId"] == null || frm["logId"] == "") ? 0 : Convert.ToInt64(frm["logId"]);
            if (LogSheetNum != "")
                if (LogId == 0)
                    LogId = db.tbl_log_sheets.Where(a => a.LogSheetNum == LogSheetNum.Trim() && a.Status == true).FirstOrDefault().ID;
            List<object> Params = new List<object>();
            Params.Add(LogId);
            var lstDutyLogDets = db.ExecuteStoreQuery<DutyLogSheetReportList>("exec Sp_SearchLogSheet {0}", Params.ToArray()).ToList();
            if (lstDutyLogDets.Count == 0 || lstDutyLogDets == null)
                return PartialView("_NoData");
            ViewBag.ListCount = lstDutyLogDets.Count;
            return PartialView("_LogSheetList", lstDutyLogDets);
        }
        #endregion

        #region Add Driver from Duty Log sheet
        public ActionResult AddDriver(long VehicleID)
        {
            tbl_vehicles VehicleDet = db.tbl_vehicles.Where(v => v.Status == true && v.ID == VehicleID).SingleOrDefault();
            ViewBag.VendorID = new SelectList(db.tbl_vendor_details.Where(a => a.Status == true), "ID", "FirstName", VehicleDet.VendorID);
            ViewBag.CurrentVehicleID = VehicleID;
            ViewBag.VehicleRegNum = VehicleDet.VehicleRegNum;
            return PartialView("_AddDriver", new tbl_drivers());
        }
        public bool IsComplete(tbl_drivers driver)
        {
            if (driver.LicenceValidity == null)
                return false;
            else if (driver.BadgeValidity == null)
                return false;
            else
                return true;
        }
        public bool ValidateForm(tbl_drivers driver)
        {
            if (driver.FirstName == null || driver.FirstName.ToString().Trim().Length == 0)
                ModelState.AddModelError("FirstName", "Please enter firstname.");
            if (driver.LastName == null || driver.LastName.ToString().Trim().Length == 0)
                ModelState.AddModelError("LastName", "Please enter lastname.");
            if (driver.ContactNumber.ToString().Trim().Length == 0)
                ModelState.AddModelError("ContactNumber", "Please enter contact number.");
            if (driver.ContactNumber.ToString().Trim().Length > 10)
                ModelState.AddModelError("ContactNumber", "Contact number should not be greater than 10 digits.");
            if (driver.ContactNumber.ToString().Trim().Length < 10)
                ModelState.AddModelError("ContactNumber", "Contact number should not be less than 10 digits.");
            if (driver.LicenceNumber == null || driver.LicenceNumber.ToString().Trim().Length == 0)
                ModelState.AddModelError("LicenceNumber", "Please enter Licence number.");
            if (String.IsNullOrEmpty(driver.LicenceValidity.ToString()))
                ModelState.AddModelError("LicenceValidity", "Please enter Licence validity date.");
            if (driver.BadgeNumber == null || driver.BadgeNumber.ToString().Trim().Length == 0)
                ModelState.AddModelError("BadgeNumber", "Please enter Badge number.");
            if (String.IsNullOrEmpty(driver.BadgeValidity.ToString()))
                ModelState.AddModelError("BadgeValidity", "Please enter Badge validity date.");
            if (driver.ReferenceName == null || driver.ReferenceName.ToString().Trim().Length == 0)
                ModelState.AddModelError("ReferenceName", "Please enter Reference name.");
            if (driver.ReferenceNumber == null || driver.ReferenceNumber.ToString().Trim().Length == 0)
                ModelState.AddModelError("ReferenceNumber", "Please enter Reference number.");
            if (driver.NameonLicence == null || driver.NameonLicence.ToString().Trim().Length == 0)
                ModelState.AddModelError("NameonLicence", "Please enter Name on licence.");

            if (driver.VendorID == null || driver.VendorID.Value.ToString() == "")
                ModelState.AddModelError("VendorID", "Please select Vendor");
            if (driver.CurrentVehicleID == null || driver.CurrentVehicleID.Value.ToString() == "0")
                ModelState.AddModelError("CurrentVehicleID", "Please select Vehicle");
            return ModelState.IsValid;
        }
        public bool ValidatePhoto(tbl_drivers driver, HttpPostedFileBase PhotoUrl, HttpPostedFileBase IDProof)
        {
            if (driver.PhotoUrl == null || driver.PhotoUrl.ToString().Trim().Length == 0)
                ModelState.AddModelError("PhotoUrl", "Please upload driver photo.");
            if (driver.IDProof == null || driver.IDProof.ToString().Trim().Length == 0)
                ModelState.AddModelError("IDProof", "Please upload driver ID Proof.");
            if (core.IsValid(PhotoUrl) == false)
                ModelState.AddModelError("PhotoUrl", "Only image files are accepted, please browse a image file");
            if (core.IsValid(IDProof) == false)
                ModelState.AddModelError("IDProof", "Only image files are accepted, please browse a image file");
            return ModelState.IsValid;
        }
        #endregion

        #region Custom Methods
        public string getAjaxResult(string f, string q)
        {
            f = f == "undefined" ? "CompanyName" : f;
            StringBuilder str = new StringBuilder();
            var client = from a in db.tbl_clients
                                    .Where(a => a.Status.Value == true)
                                    .Where<tbl_clients>(f, q, WhereOperation.Contains)
                         select new { a.CompanyName, a.ID };


            foreach (var a in client)
            {
                str.Append(string.Format("{0}|{1}\r\n", a.CompanyName.ToUpper(), a.ID)).ToString();
                ViewBag.ClientID = a.ID;
            }

            return str.ToString();
        }
        public string getVehicleAjaxResult(string f, string q)
        {
            f = f == "undefined" ? "VehicleRegNum" : f;
            StringBuilder str = new StringBuilder();
            var client = from a in db.tbl_vehicles
                                   .Where(a => a.Status.Value == true)
                                   .Where<tbl_vehicles>(f, q, WhereOperation.Contains)
                         select new { a.VehicleRegNum, a.ID };


            foreach (var a in client)
            {
                str.Append(string.Format("{0} |{1}\r\n", a.VehicleRegNum.ToUpper(), a.ID)).ToString();
                ViewBag.ClientID = a.ID;
            }

            return str.ToString();
        }

        // Verify logSheet Number 
        public bool VerifyLogSheetNumber(string logsheetnum)
        {
            if (db.tbl_log_sheets.Where(a => a.LogSheetNum.ToUpper().Trim() == logsheetnum.ToUpper().Trim() && a.Status == true).Any())
                return false;
            _logSheetList = (List<tbl_log_sheets>)Session["LogsheetEntryList"];
            if (_logSheetList != null)
            {
                if (_logSheetList.Where(a => a.LogSheetNum.ToUpper().Trim() == logsheetnum.ToUpper().Trim()).Any())
                    return false;
            }
            return true;
        }

        public JsonResult GetVendor(long VehicleID)
        {
            var vehicles = db.tbl_vehicles.Where(a => a.Status == true && a.ID == VehicleID).SingleOrDefault();
            var vendor = db.tbl_vendor_details.Where(s => s.Status == true && s.ID == vehicles.VendorID).Select(a => new { a.FirstName, a.LastName, a.ID }).ToList();
            return Json(vendor, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetVehicleModel(long VehicleID)
        {
            var vehicles = db.tbl_vehicles.Where(a => a.Status == true && a.ID == VehicleID).SingleOrDefault();
            var vModels = db.tbl_vehicle_models.Where(s => s.Status == true && s.ID == vehicles.VehicleModelID).Select(a => new { a.VehicleModelName, a.ID }).ToList();
            return Json(vModels, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetVehicleDriver(long VehicleID)
        {
            var vDrivers = db.tbl_drivers.Where(s => s.Status == true && s.CurrentVehicleID == VehicleID).Select(a => new { a.FirstName, a.LastName, a.ID }).ToList();
            return Json(vDrivers, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetVehicleType(long VehicleID)
        {
            var vehicles = db.tbl_vehicles.Where(a => a.Status == true && a.ID == VehicleID).SingleOrDefault();
            var vTypes = db.tbl_vehicle_types.Where(s => s.Status == true && s.ID == vehicles.VehicleTypeID).Select(a => new { a.VehicleType, a.ID }).ToList();
            return Json(vTypes, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetSeater(long VehicleID)
        {
            var vehicles = db.tbl_vehicles.Where(a => a.Status == true && a.ID == VehicleID).SingleOrDefault();
            var vSeaters = db.tbl_seaters.Where(s => s.Status == true && s.ID == vehicles.SeaterId).Select(a => new { a.Seater, a.ID }).ToList();
            return Json(vSeaters, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetLoaction(long ClientID)
        {
            var Clients = db.tbl_clients.Where(b => b.Status == true && b.ID == ClientID).SingleOrDefault();
            var billtype = db.tbl_billing_types.Where(b => b.Status == true && b.ID == Clients.BillingTypeID).SingleOrDefault().BillingType;
            List<GeneralClassFields> cLocations = new List<GeneralClassFields>();
            if (billtype == "KILO METER")
            {
                var lstKmLocations = (from o in db.tbl_km_client_rate
                                      where o.Status == true && o.ClientID == ClientID
                                      group o by new { Location = o.Location } into g
                                      select new { Location = g.Key.Location }).ToList();

                foreach (var Cloc in lstKmLocations)
                {
                    cLocations.Add(new GeneralClassFields
                    {
                        Text1 = Cloc.Location,
                        Text2 = Cloc.Location
                    });
                }
            }
            else if (billtype == "TRIPS")
            {
                var lstSlabLocations = (from o in db.tbl_slab_client_rate
                                        where o.Status == true && o.ClientID == ClientID
                                        group o by new { Location = o.Location } into g
                                        select new { Location = g.Key.Location }).ToList();

                foreach (var Cloc in lstSlabLocations)
                {
                    cLocations.Add(new GeneralClassFields
                    {
                        Text1 = Cloc.Location,
                        Text2 = Cloc.Location
                    });
                }
            }
            return Json((from obj in cLocations select new { Text1 = obj.Text1, Text2 = obj.Text2 }), JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetLoactionByEmployee(long ClientID, string EmployeeID)
        {
            var Clients = db.tbl_clients.Where(b => b.Status == true && b.ID == ClientID).SingleOrDefault();
            var billtype = db.tbl_billing_types.Where(b => b.Status == true && b.ID == Clients.BillingTypeID).SingleOrDefault().BillingType;
            List<GeneralClassFields> cLocations = new List<GeneralClassFields>();
            if (billtype == "KILO METER")
            {
                var lstKmLocations = (from o in db.tbl_km_client_rate
                                      where o.Status == true && o.ClientID == ClientID
                                      group o by new { Location = o.Location } into g
                                      select new { Location = g.Key.Location }).ToList();

                foreach (var Cloc in lstKmLocations)
                {
                    cLocations.Add(new GeneralClassFields
                    {
                        Text1 = Cloc.Location,
                        Text2 = Cloc.Location
                    });
                }
            }
            else if (billtype == "TRIPS")
            {
                var lstSlabLocations = (from o in db.tbl_slab_client_rate
                                        where o.Status == true && o.ClientID == ClientID && o.EmpId.ToUpper().Trim() == EmployeeID.ToUpper().Trim()
                                        group o by new { Location = o.Location } into g
                                        select new { Location = g.Key.Location }).ToList();

                foreach (var Cloc in lstSlabLocations)
                {
                    cLocations.Add(new GeneralClassFields
                    {
                        Text1 = Cloc.Location,
                        Text2 = Cloc.Location
                    });
                }
            }
            return Json((from obj in cLocations select new { Text1 = obj.Text1, Text2 = obj.Text2 }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetBillTypeByClient(long ClientID)
        {
            tbl_clients _client = db.tbl_clients.Where(c => c.Status == true && c.ID == ClientID).SingleOrDefault();
            return Json(new { success = true, BillTypeID = _client.tbl_billing_types.BillingType }, JsonRequestBehavior.AllowGet);
        }
        public JsonResult LoadApprovedKMByLocation(string Location, long ClientID, long VehicleID)
        {
            tbl_vehicles _vehicle = db.tbl_vehicles.Where(c => c.Status == true && c.ID == VehicleID).FirstOrDefault();
            tbl_km_client_rate _clientrate = db.tbl_km_client_rate.Where(s => s.Status == true && s.ClientID == ClientID && s.Location == Location.ToUpper().Trim()).FirstOrDefault();
            if (_clientrate != null)
                return Json(new { success = true, ApprovedKM = _clientrate.ApprovedKM }, JsonRequestBehavior.AllowGet);
            else
                return Json(new { success = true, ApprovedKM = 0 }, JsonRequestBehavior.AllowGet);

        }
        public JsonResult LoadSlabRateByLocation(string Location, long ClientID, long VehicleID, long VehicleTypeID)
        {
            tbl_vehicles _vehicle = db.tbl_vehicles.Where(c => c.Status == true && c.ID == VehicleID).FirstOrDefault();
            tbl_slab_client_rate _clientrate = db.tbl_slab_client_rate.Where(s => s.Status == true && s.ClientID == ClientID && s.Location == Location.ToUpper().Trim()
                                                                       && s.SeaterID == _vehicle.SeaterId && s.VehicleTypeID == VehicleTypeID).FirstOrDefault();
            if (_clientrate != null)
                return Json(new { success = true, VendorRate = _clientrate.VendorRate }, JsonRequestBehavior.AllowGet);
            else
                return Json(new { success = true, VendorRate = 0 }, JsonRequestBehavior.AllowGet);

        }
        public JsonResult LoadSlabRateByEmployee(string Location, long ClientID, long VehicleID, string EmployeeID, long VehicleTypeID)
        {
            tbl_vehicles _vehicle = db.tbl_vehicles.Where(c => c.Status == true && c.ID == VehicleID).FirstOrDefault();
            tbl_slab_client_rate _clientrate = db.tbl_slab_client_rate.Where(s => s.Status == true && s.ClientID == ClientID && s.Location == Location.ToUpper().Trim()
                                                                            && s.SeaterID == _vehicle.SeaterId && s.VehicleTypeID == VehicleTypeID
                                                                            && s.EmpId.ToUpper().Trim() == EmployeeID.ToUpper().Trim()).FirstOrDefault();
            if (_clientrate != null)
                return Json(new { success = true, VendorRate = _clientrate.VendorRate }, JsonRequestBehavior.AllowGet);
            else
                return Json(new { success = true, VendorRate = 0 }, JsonRequestBehavior.AllowGet);

        }
        public JsonResult LoadSlabRateByEmpID(string Location, long ClientID, string EmpId)
        {
            decimal VendorRate = 0;
            tbl_slab_client_rate _clientrate = db.tbl_slab_client_rate.Where(s => s.Status == true && s.ClientID == ClientID && s.Location == Location.ToUpper().Trim() && s.EmpId == EmpId).FirstOrDefault();
            if (_clientrate != null)
                VendorRate = (decimal)_clientrate.VendorRate;
            return Json(new { success = true, VendorRate = VendorRate }, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetCurrentSLABRate(long ID)
        {
            decimal SLABRate = 0;
            tbl_log_sheets _logDet = db.tbl_log_sheets.Where(l => l.Status == true && l.ID == ID).SingleOrDefault();
            if (_logDet != null)
            {

                SLABRate = _logDet.SlabRate == null ? 0 : (decimal)_logDet.SlabRate;
            }
            return Json(new { success = true, SLABRate = _logDet.SlabRate }, JsonRequestBehavior.AllowGet);
        }
        //These Methods for Editing load model ,type,seater from log_sheets table
        public JsonResult GetCurrentVehicleModel(long VehicleID, long ID)
        {
            var log = db.tbl_log_sheets.Where(a => a.Status == true && a.ID == ID).SingleOrDefault();
            var vModels = db.tbl_vehicle_models.Where(s => s.Status == true && s.ID == log.VehicleModelID).Select(a => new { a.VehicleModelName, a.ID }).ToList();
            return Json(vModels, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetCurrentVehicleType(long VehicleID, long ID)
        {
            var log = db.tbl_log_sheets.Where(a => a.Status == true && a.ID == ID).SingleOrDefault();
            var vTypes = db.tbl_vehicle_types.Where(s => s.Status == true && s.ID == log.VehicleTypeID).Select(a => new { a.VehicleType, a.ID }).ToList();
            return Json(vTypes, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetCurrentSeater(long VehicleID, long ID)
        {
            var log = db.tbl_log_sheets.Where(a => a.Status == true && a.ID == ID).SingleOrDefault();
            var vSeaters = db.tbl_seaters.Where(s => s.Status == true && s.ID == log.SeaterID).Select(a => new { a.Seater, a.ID }).ToList();
            return Json(vSeaters, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetCurrentVehicleDriver(long VehicleID, long ID)
        {

            var log = db.tbl_log_sheets.Where(a => a.Status == true && a.ID == ID).SingleOrDefault();
            var vDrivers = db.tbl_drivers.Where(s => s.Status == true && s.ID == log.DriverID).Select(a => new { a.FirstName, a.LastName, a.ID }).ToList();
            return Json(vDrivers, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetCurrentClientLocation(long ClientID, long ID)
        {
            var log = db.tbl_log_sheets.Where(a => a.Status == true && a.ID == ID).Select(a => new { a.Location }).ToList();
            //var cLocations = db.tbl_locations.Where(b => b.Status == true && b.ID == log.LocationId).Select(b => new { b.Location, b.ID }).ToList();
            return Json(log, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetPackageRateByVehType(long ClientID, long VehTypeId, long VehModelId, string PackModel)
        {
            var Clients = db.tbl_clients.Where(b => b.Status == true && b.ID == ClientID).SingleOrDefault();
            var billtype = db.tbl_billing_types.Where(b => b.Status == true && b.ID == Clients.BillingTypeID).SingleOrDefault().BillingType;
            List<GeneralClassFields> cLocations = new List<GeneralClassFields>();
            if (billtype == "PACKAGE")
            {
                var lstPackages = (from m in db.tbl_package_client_rates
                                   where m.ClientId == ClientID
                                   && m.VehicleTypeId == VehTypeId
                                   && m.VehicleModelId == VehModelId
                                   && m.PackModel == PackModel
                                   select m).ToList();

                foreach (var cPK in lstPackages)
                {
                    cLocations.Add(new GeneralClassFields
                    {
                        ID = cPK.Id,
                        Text2 = cPK.PackageAmt + " RS -" + cPK.PackageKM + " KM -" + cPK.WorkingDays + " Day(s)"
                    });
                }
            }

            return Json((from obj in cLocations select new { Id = obj.ID, Text2 = obj.Text2 }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetPackageDet(long Id)
        {
            tbl_package_client_rates PkRate = db.tbl_package_client_rates.Where(a => a.Id == Id && a.IsActive == true).FirstOrDefault();
            if (PkRate != null)
                return Json(new { PackDet = PkRate.WorkingDays, PackWorkHr = PkRate.WorkingHrs, ExtKM = PkRate.ExtKM, ExtHr = PkRate.ExtHr }, JsonRequestBehavior.AllowGet);
            else
                return Json(new { PackDet = 0 }, JsonRequestBehavior.AllowGet);
        }

        public string getEmployeeAjaxResult(string f, string q, long cid)
        {
            f = f == "undefined" ? "EmpId" : f;
            StringBuilder str = new StringBuilder();
            var EmpIDs = from a in db.tbl_slab_client_rate
                                   .Where(a => a.Status.Value == true && a.EmpId != "")
                                   .Where(a => a.ClientID == cid)
                                   .Where<tbl_slab_client_rate>(f, q, WhereOperation.Contains)
                         select new { a.EmpId };
            var emps = EmpIDs.Distinct();
            foreach (var a in emps)
            {
                str.Append(string.Format("{0} |{1}\r\n", a.EmpId, a.EmpId)).ToString();
            }
            return str.ToString();
        }

        public void UpdatePackageId(long ClientId)
        {
            var ls = db.tbl_log_sheets.Where(a => a.ClientID == ClientId
                && a.LogSheetType == "PACKAGE"
                && a.VehicleTypeID == 1
                && a.VehicleModelID == 1
                && a.PackId == null
                && a.PackModel == "NON-WAITING").ToList();

            var RateList = db.tbl_package_client_rates.Where(a => a.ClientId == ClientId
                && a.VehicleModelId == 1
                && a.VehicleTypeId == 1 && a.IsActive == true
                && a.PackModel == "NON-WAITING").ToList();

            int rateWrDays = 0, logWrkDays = 0, logExtraHr = 0, logExtraKM = 0;
            decimal perDayRate = 0, grssRate = 0, rateExtraHr = 0, rateExtraKM = 0, netGrssAmt = 0;
            long PackId = 0;
            foreach (var logsheet in ls) // logsheet
            {
                foreach (var rate in RateList)  // rate
                {
                    rateWrDays = Convert.ToInt32(rate.WorkingDays);
                    logWrkDays = Convert.ToInt32(logsheet.WorkingDays);
                    perDayRate = Convert.ToDecimal(rate.PackageAmt / rateWrDays);

                    grssRate = Math.Round((logWrkDays * perDayRate), 0);

                    if (logsheet.ExtraHr == 0 && logsheet.ExtraKM == 0)
                    {
                        if (grssRate == Math.Round((decimal)logsheet.GrossAmt, 0))
                        {
                            // Get PACKID AND UPDATE LOGSHEET

                            PackId = rate.Id;

                            tbl_log_sheets logDet = db.tbl_log_sheets.Where(a => a.ID == logsheet.ID).SingleOrDefault();
                            logDet.PackId = PackId;
                            TryUpdateModel(logDet);
                            db.SaveChanges();
                            continue;
                        }
                        else
                            continue;
                    }
                    else
                    {
                        // Verify Extra 
                        rateExtraHr = Convert.ToDecimal(rate.ExtHr);
                        rateExtraKM = Convert.ToDecimal(rate.ExtKM);
                        logExtraHr = Convert.ToInt32(logsheet.ExtraHr);
                        logExtraKM = Convert.ToInt32(logsheet.ExtraKM);

                        netGrssAmt = Math.Round(((rateExtraHr * logExtraHr) + (rateExtraKM * logExtraKM) + grssRate), 0);

                        if (netGrssAmt == Math.Round((decimal)logsheet.GrossAmt, 0))
                        {
                            PackId = rate.Id;

                            tbl_log_sheets logDet = db.tbl_log_sheets.Where(a => a.ID == logsheet.ID).SingleOrDefault();
                            logDet.PackId = PackId;
                            TryUpdateModel(logDet);
                            db.SaveChanges();
                            continue;
                        }
                        else
                            continue;

                    }

                }
            }
        }

        public void UpdatePackageId1(long ClientId)
        {
            var ls = db.tbl_log_sheets.Where(a => a.ClientID == ClientId
                && a.LogSheetType == "PACKAGE"
                && a.VehicleTypeID == 1
                && a.VehicleModelID == 1
                && a.PackId == null
                && a.PackModel == "WAITING").ToList();

            var RateList = db.tbl_package_client_rates.Where(a => a.ClientId == ClientId
                && a.VehicleModelId == 1 && a.IsActive == true
                && a.VehicleTypeId == 1
                && a.PackModel == "WAITING").ToList();

            int rateWrDays = 0, logWrkDays = 0, logExtraHr = 0, logExtraKM = 0;
            decimal perDayRate = 0, grssRate = 0, rateExtraHr = 0, rateExtraKM = 0, netGrssAmt = 0;
            long PackId = 0;
            foreach (var logsheet in ls) // logsheet
            {
                foreach (var rate in RateList)  // rate
                {
                    rateWrDays = Convert.ToInt32(rate.WorkingDays);
                    logWrkDays = Convert.ToInt32(logsheet.WorkingDays);
                    perDayRate = Convert.ToDecimal(rate.PackageAmt / rateWrDays);

                    grssRate = Math.Round((logWrkDays * perDayRate), 0);

                    if (logsheet.ExtraHr == 0 && logsheet.ExtraKM == 0)
                    {
                        if (grssRate == Math.Round((decimal)logsheet.GrossAmt, 0))
                        {
                            // Get PACKID AND UPDATE LOGSHEET

                            PackId = rate.Id;

                            tbl_log_sheets logDet = db.tbl_log_sheets.Where(a => a.ID == logsheet.ID).SingleOrDefault();
                            logDet.PackId = PackId;
                            TryUpdateModel(logDet);
                            db.SaveChanges();
                            continue;
                        }
                        else
                            continue;
                    }
                    else
                    {
                        // Verify Extra 
                        rateExtraHr = Convert.ToDecimal(rate.ExtHr);
                        rateExtraKM = Convert.ToDecimal(rate.ExtKM);
                        logExtraHr = Convert.ToInt32(logsheet.ExtraHr);
                        logExtraKM = Convert.ToInt32(logsheet.ExtraKM);

                        netGrssAmt = Math.Round(((rateExtraHr * logExtraHr) + (rateExtraKM * logExtraKM) + grssRate), 0);

                        if (netGrssAmt == Math.Round((decimal)logsheet.GrossAmt, 0))
                        {
                            PackId = rate.Id;

                            tbl_log_sheets logDet = db.tbl_log_sheets.Where(a => a.ID == logsheet.ID).SingleOrDefault();
                            logDet.PackId = PackId;
                            TryUpdateModel(logDet);
                            db.SaveChanges();
                            continue;
                        }
                        else
                            continue;

                    }

                }
            }
        }

        public void UpdatePackageId2(long ClientId)
        {
            var ls = db.tbl_log_sheets.Where(a => a.ClientID == ClientId
                && a.LogSheetType == "PACKAGE"
                && a.VehicleTypeID == 5
                && a.VehicleModelID == 1
                && a.PackId == null
                && a.PackModel == "NON-WAITING").ToList();

            var RateList = db.tbl_package_client_rates.Where(a => a.ClientId == ClientId
                && a.VehicleModelId == 1 && a.IsActive == true
                && a.VehicleTypeId == 5
                && a.PackModel == "NON-WAITING").ToList();

            int rateWrDays = 0, logWrkDays = 0, logExtraHr = 0, logExtraKM = 0;
            decimal perDayRate = 0, grssRate = 0, rateExtraHr = 0, rateExtraKM = 0, netGrssAmt = 0;
            long PackId = 0;
            foreach (var logsheet in ls) // logsheet
            {
                foreach (var rate in RateList)  // rate
                {
                    rateWrDays = Convert.ToInt32(rate.WorkingDays);
                    logWrkDays = Convert.ToInt32(logsheet.WorkingDays);
                    perDayRate = Convert.ToDecimal(rate.PackageAmt / rateWrDays);

                    grssRate = Math.Round((logWrkDays * perDayRate), 0);

                    if (logsheet.ExtraHr == 0 && logsheet.ExtraKM == 0)
                    {
                        if (grssRate == Math.Round((decimal)logsheet.GrossAmt, 0))
                        {
                            // Get PACKID AND UPDATE LOGSHEET

                            PackId = rate.Id;

                            tbl_log_sheets logDet = db.tbl_log_sheets.Where(a => a.ID == logsheet.ID).SingleOrDefault();
                            logDet.PackId = PackId;
                            TryUpdateModel(logDet);
                            db.SaveChanges();
                            continue;
                        }
                        else
                            continue;
                    }
                    else
                    {
                        // Verify Extra 
                        rateExtraHr = Convert.ToDecimal(rate.ExtHr);
                        rateExtraKM = Convert.ToDecimal(rate.ExtKM);
                        logExtraHr = Convert.ToInt32(logsheet.ExtraHr);
                        logExtraKM = Convert.ToInt32(logsheet.ExtraKM);

                        netGrssAmt = Math.Round(((rateExtraHr * logExtraHr) + (rateExtraKM * logExtraKM) + grssRate), 0);

                        if (netGrssAmt == Math.Round((decimal)logsheet.GrossAmt, 0))
                        {
                            PackId = rate.Id;

                            tbl_log_sheets logDet = db.tbl_log_sheets.Where(a => a.ID == logsheet.ID).SingleOrDefault();
                            logDet.PackId = PackId;
                            TryUpdateModel(logDet);
                            db.SaveChanges();
                            continue;
                        }
                        else
                            continue;

                    }

                }
            }
        }
        #endregion

        #region export to excel,pdf
        public ActionResult LogSheetExportToExcel()
        {
            var logSheetList = GetDutyLogSheetList();
            DataTable dt = core.ConvertToDataTable(logSheetList.OrderBy(a => a.ID));
            string Filename = "DutyLogSheet_" + DateTime.Now.ToString("hhmmss") + ".xlsx";
            MemoryStream ms = ExportToExcel(dt);
            if (ms != null)
            {
                ms.Seek(0, SeekOrigin.Begin);
                return File(ms, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml", Filename);
            }
            return View();
        }
        //public ActionResult LogSheetExportToPdf()
        //{
        //    var logSheetList = GetDutyLogSheetList();
        //    DataTable dt = core.ConvertToDataTable(logSheetList.OrderBy(a => a.ID));
        //    PDFExport(dt);
        //    return RedirectToAction("Index");
        //}
        public MemoryStream ExportToExcel(DataTable table)
        {

            string sheetName = "LogSheetDetails";
            var wb = new XLWorkbook();
            try
            {
                var ws = wb.Worksheets.Add(sheetName);
                ws.Cell("B1").Value = "Duty Log Sheet Details";
                ws.Range("B1:E1").Merge();
                ws.Cell("B1").Style.Font.FontSize = 16;
                ws.Cell("B1").Style.Font.Bold = true;
                ws.Cell("B1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ws.Cell("B1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell("B1").Style.Font.FontColor = XLColor.FromHtml("#41AADF");

                ws.Cell("B2").Value = " Generated on " + DateTime.Now;
                ws.Range("B2:E2").Merge();
                ws.Cell("B2").Style.Font.FontSize = 14;
                ws.Cell("B2").Style.Font.Bold = true;
                ws.Cell("B2").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ws.Cell("B2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                int iCol = 0;
                // Add column headings...
                foreach (DataColumn Col in table.Columns)
                {
                    iCol++;
                    if (Col.ColumnName == "ID" || Col.ColumnName == "Status")
                    {
                        iCol--;
                        continue;
                    }
                    ws.Cell(3, iCol).SetValue(Col.ColumnName);
                    ws.Cell(3, iCol).Style.Font.Bold = true;
                    ws.Cell(3, iCol).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    ws.Cell(3, iCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                }
                // for each row of data...
                int iRow = 3;
                foreach (DataRow r in table.Rows)
                {
                    iRow++;
                    // add each row's cell data...
                    iCol = 0;
                    foreach (DataColumn c in table.Columns)
                    {
                        iCol++;
                        if (c.ColumnName == "ID" || c.ColumnName == "Status")
                        {
                            iCol--;
                            continue;
                        }
                        ws.Cell(iRow, iCol).SetValue(r[c.ColumnName]);
                    }
                }
                ws.Columns().AdjustToContents();
            }
            catch (SqlException ex)
            {

            }
            MemoryStream ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms;
        }
        public void PDFExport(DataTable dt)
        {
            iTextSharp.text.Table table = new iTextSharp.text.Table(dt.Columns.Count - 3);
            iTextSharp.text.Font titleFont = FontFactory.GetFont(BaseFont.TIMES_ROMAN, 12, iTextSharp.text.Font.BOLD, new iTextSharp.text.Color(System.Drawing.ColorTranslator.FromHtml("#41AADF")));
            iTextSharp.text.Font ColumnheaderFont = FontFactory.GetFont(BaseFont.TIMES_ROMAN, 8, iTextSharp.text.Font.BOLD, iTextSharp.text.Color.BLACK);
            iTextSharp.text.Font fonts = FontFactory.GetFont(BaseFont.TIMES_ROMAN, 8, iTextSharp.text.Font.NORMAL, iTextSharp.text.Color.BLACK);

            // table.Cellpadding = 2;
            table.Width = 100;
            ArrayList columnsWidth = new ArrayList();
            int cnt = dt.Columns.Count;
            foreach (DataColumn Col in dt.Columns)
            {
                if (Col.ColumnName == "ID" || Col.ColumnName == "Status" || Col.ColumnName == "Seater")
                    continue;

                string cellText = Server.HtmlDecode(Col.ColumnName);
                iTextSharp.text.Cell cell = new iTextSharp.text.Cell();
                cell.Add(new Phrase(cellText, ColumnheaderFont));
                columnsWidth.Add(20);
                table.AddCell(cell);
            }
            table.SetWidths(columnsWidth.OfType<int>().Select(w => (int)w).ToArray());
            foreach (DataRow r in dt.Rows)
            {
                foreach (DataColumn c in dt.Columns)
                {
                    if (c.ColumnName == "ID" || c.ColumnName == "Status" || c.ColumnName == "Seater")
                        continue;

                    string cellText = Server.HtmlDecode(r[c.ColumnName].ToString());
                    iTextSharp.text.Cell cell = new iTextSharp.text.Cell();
                    cell.Add(new Phrase(cellText, fonts));
                    table.AddCell(cell);
                }
            }

            string Filename = "DutyLogSheet_" + DateTime.Now.ToString("hhmmss") + ".pdf";
            Document pdfDoc = new Document(PageSize.A4, 10f, 10f, 10f, 0f);
            PdfWriter.GetInstance(pdfDoc, Response.OutputStream);
            pdfDoc.Open();
            Paragraph p = new Paragraph("Duty log sheet details", titleFont);
            p.Alignment = Element.ALIGN_CENTER;
            p.SpacingAfter = 2f;
            pdfDoc.Add(p);
            Paragraph p1 = new Paragraph(" Generated on " + DateTime.Now, titleFont);
            p1.Alignment = Element.ALIGN_CENTER;
            p1.SpacingAfter = 4f;
            pdfDoc.Add(p1);
            pdfDoc.Add(table);
            pdfDoc.Close();
            Response.ContentType = "application/pdf";
            Response.AddHeader("content-disposition", "attachment;" + "filename=" + Filename);
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Write(pdfDoc);
            Response.End();
        }
        #endregion
    }

    internal class DutylogSheet
    {
        public long ID { get; set; }
        public DateTime LogDate { get; set; }
        public string LogSheetType { get; set; }
        public string Client { get; set; }
        public string VehicleReg { get; set; }
        public string VehicleType { get; set; }
        public string VehicleModel { get; set; }
        public string Driver { get; set; }
        public string Seater { get; set; }
        public string LogSheetNum { get; set; }
        public string TripType { get; set; }
        public int Pax { get; set; }
        public string ShiftTime { get; set; }
        public string ReachTime { get; set; }
        public string Location { get; set; }
        public int StartKM { get; set; }
        public int EndKM { get; set; }
        public int TotalKM { get; set; }
        public int Approved { get; set; }
        public string PassengerEmpID { get; set; }
        public decimal SlabRate { get; set; }
        public int FinalApprovedKm { get; set; }
        public decimal FinalSlabRate { get; set; }
        public bool Status { get; set; }
        public string Remark { get; set; }
        public string AuditRemark { get; set; }
        public string CreatedDate { get; set; }
        public string UserName { get; set; }
        public string ModifiedDt { get; set; }
        public string ModifiedBy { get; set; }
        public string AuditDt { get; set; }
        public string AuditedBy { get; set; }
        public string Paid { get; set; }

    }
}
