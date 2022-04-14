using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;
using System.Globalization;
using System.Data.Common;
using System.Data.Objects;
using FMS.Helpers;
using System.Data;
using System.IO;
using ClosedXML.Excel;

namespace FMS.Controllers
{
    [CustomAuthorizationFilter]
    public class VendorInvoiceController : Controller
    {
        #region ctors
        private FMSDBEntities db;
        private core objCore = new core();
        public VendorInvoiceController()
        {
            db = new FMSDBEntities();
        }
        #endregion

        #region InvoiceList
        /// <summary>
        /// Generated Invoices list in datatable
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public ActionResult Index()
        {
            return View();
        }
        public JsonResult GetVendorInvoices(int iDisplayStart, int iDisplayLength, string sSearch, int iColumns,
     int iSortingCols, int iSortCol_0, string sSortDir_0,
     int sEcho, string mDataProp_Key)
        {
            var vendorsList = GetVendorInvoiceList();
            var filteredvendors = vendorsList
                .Select(l => new[]{
                    l.Id.ToString(),
                    l.Text1.ToString(),
                    l.Text2.ToString(),
                    l.Text3.ToString(),
                    l.Amount.ToString(),
                    l.Date1.ToShortDateString(),
                    l.Amount1.ToString(),
                    l.Text6.ToString(),
                    l.Status1.ToString(),
                    l.Text4.ToString(),
                    l.Text5.ToString(),
                    l.Status.ToString()
                    
                }).Where(i => string.IsNullOrEmpty(sSearch)
                     || i.Any(y => y.IndexOf(sSearch, StringComparison.InvariantCultureIgnoreCase) >= 0));
            var orderedlist = filteredvendors
                   .OrderByWithDirection(i => (i[iSortCol_0]).Parse(), sSortDir_0 == "asc")
                   .Skip(iDisplayStart)
                   .Take(iDisplayLength);
            var model = new
            {
                aaData = orderedlist,
                iTotalDisplayRecords = filteredvendors.Count(),
                iTotalRecords = filteredvendors.Count(),
                sEcho = sEcho.ToString()
            };
            return Json(model, JsonRequestBehavior.AllowGet);
        }

        private IEnumerable<CommonFields> GetVendorInvoiceList()
        {
            var list = new List<CommonFields>();
            var vendorInvoiceList = db.tbl_vendor_invoices.Where(a => a.Status == true).ToList();
            foreach (tbl_vendor_invoices ls in vendorInvoiceList)
            {
                string VendorName = ls.tbl_vendor_details.FirstName + " " + ls.tbl_vendor_details.LastName;
                list.Add(new CommonFields
                {
                    Id = ls.ID,
                    Text1 = ls.InvoiceNum,
                    Text2 = VendorName,
                    Text3 = ls.tbl_vehicles.VehicleRegNum,
                    Amount = (decimal)ls.InvoiceAmt,
                    Date1 = Convert.ToDateTime(ls.InvoiceDate),
                    Amount1 = (decimal)ls.PaidAmount,
                    Text6 = ls.PaidDate == null ? "" : Convert.ToDateTime(ls.PaidDate).Date.ToShortDateString(),
                    Status1 = (bool)ls.Paid,
                    Text4 = (ls.AuthorizedSign == null || ls.AuthorizedSign == "") ? "" : ls.AuthorizedSign,
                    Text5 = (ls.ReceiverName == null || ls.ReceiverName == "") ? "" : ls.ReceiverName,
                    Status = (bool)ls.Status
                });
            }
            objCore.ConvertToUppercase<CommonFields>(list);
            return list.OrderBy(a => a.Id);
        }

        public ActionResult Invoice(string StartDate, string EndDate, long? vehicleId)
        {
            if (StartDate != null || EndDate != null)
            {
                ViewBag.FromDate = DateTime.Parse(StartDate).ToString("dd/MM/yyyy");
                ViewBag.ToDate = DateTime.Parse(EndDate).ToString("dd/MM/yyyy");
            }
            ViewBag.vehID = vehicleId;
            if (vehicleId != null)
                ViewBag.VehicleRegNum = db.tbl_vehicles.Where(a => a.Status == true && a.ID == vehicleId).FirstOrDefault().VehicleRegNum;
            else
                ViewBag.VehicleRegNum = "";
            return View("Invoice");
        }

        public ActionResult GenerateInvoice(string StartDate, string EndDate, long vehicleId, bool hasDiesel, bool hasAdv)
        {
            ReportsController objReports = new ReportsController();
            tbl_vehicles vehicleDet = db.tbl_vehicles.Where(a => a.ID == vehicleId).SingleOrDefault();
            //string startdate = Convert.ToDateTime(StartDate).ToString("yyyy-MM-dd");
            //string enddate = Convert.ToDateTime(EndDate).ToString("yyyy-MM-dd");

            string startdate = DateTime.Parse(StartDate).ToString("yyyy-MM-dd");
            string enddate = DateTime.Parse(EndDate).ToString("yyyy-MM-dd");

            // get last month 
            string lastmnthStartdate = DateTime.Parse(StartDate).AddMonths(-1).ToString("yyyy-MM-dd");
            string lastmnthEnddate = DateTime.Parse(StartDate).AddDays(-1).ToString("yyyy-MM-dd");
            ViewBag.hasDiesel = hasDiesel;
            ViewBag.IsAdHoc = 0;
            try
            {
                List<InvoiceList> _InvoiceList = GenerateInvoiceList(vehicleId, startdate, enddate, false, "", null);
                ViewBag.ServiceTax = System.Configuration.ConfigurationManager.AppSettings["ServiceTax"];
                ViewBag.DieselTax = System.Configuration.ConfigurationManager.AppSettings["DieselTax"];
                List<DieselTracking> dieselList = new List<DieselTracking>(); List<DieselTracking> PreviousMnthdieselList = new List<DieselTracking>();
                if (hasDiesel == true)
                {
                    dieselList = GetMonthlyDieselByVehicle(vehicleId, startdate, enddate, "", false); // vehicle details by selected dates
                    PreviousMnthdieselList = GetMonthlyDieselByVehicle(vehicleId, lastmnthStartdate, lastmnthEnddate, "", false);
                }

                var fromdt = DateTime.Parse(startdate);
                var todt = DateTime.Parse(enddate);
                // Advances,EHSPenalty,Penalty details by selected dates
                //decimal AdvanceTobeReceive = (from a in db.tbl_advances 
                //                      where (a.VehicleID == vehicleId
                //                      && a.Status == true)
                //                      select (decimal?)a.Amount.Value).Sum() ?? 0;

                ViewBag.AdvanceAmt = hasAdv ? GetAdvanceAmountByVehicle(vehicleId, "", false) : 0;
                ViewBag.EHSPenalty = (from a in db.tbl_ehs_details
                                      where (EntityFunctions.TruncateTime(a.CreatedDate) >= fromdt
                                      && EntityFunctions.TruncateTime(a.CreatedDate) <= todt
                                      && a.VehicleID == vehicleId
                                      && a.Status == true
                                      && (a.ClosedFlag == null || a.ClosedFlag == false))
                                      select (decimal?)a.EHSAmt).Sum() ?? 0;
                ViewBag.Penalty = (from a in db.tbl_penalties
                                   where (EntityFunctions.TruncateTime(a.CreatedDate) >= fromdt
                                   && EntityFunctions.TruncateTime(a.CreatedDate) <= todt
                                   && a.tbl_log_sheets.VehicleID == vehicleId
                                   && a.Status == true
                                   && (a.ClosedFlag == null || a.ClosedFlag == false))
                                   select (decimal?)a.PenalityAmt).Sum() ?? 0;
                //..........................................................................
                // Advances,EHSPenalty,Penalty details by previous month
                var lastfromdt = DateTime.Parse(lastmnthStartdate);
                var lasttodt = DateTime.Parse(lastmnthEnddate);
                decimal AdvEHSPenaltyAmt = objReports.GetAdvenceEHSPenaltyAmt(vehicleId, lastfromdt, lasttodt);
                // caliculate last month amount
                decimal dieselAmt = 0;
                if (PreviousMnthdieselList.Count != 0)
                {
                    decimal DieselTaxAmt = ((3 * (decimal)PreviousMnthdieselList.Select(s => s.TotalAmt).Sum()) / 100);
                    dieselAmt = ((decimal)PreviousMnthdieselList.Select(s => s.TotalAmt).Sum() + DieselTaxAmt);
                    ViewBag.PreviousMnthAmt = dieselAmt + AdvEHSPenaltyAmt;
                }
                else
                    ViewBag.PreviousMnthAmt = dieselAmt + AdvEHSPenaltyAmt;
                //.......................................................

                ViewBag.startDate = startdate;
                ViewBag.endDate = enddate;


                tbl_sequences seq = db.tbl_sequences.Where(a => a.Auto == true && a.Status == true && a.Type == "INVOICE").FirstOrDefault();
                string startformat = string.Format("{0} {1}", objCore.Ordinal(DateTime.Parse(startdate).Day), Convert.ToDateTime(startdate).ToString("MMM", CultureInfo.CreateSpecificCulture("en-US")));
                string endformat = string.Format("{0} {1}", objCore.Ordinal(DateTime.Parse(enddate).Day), Convert.ToDateTime(enddate).ToString("MMM", CultureInfo.CreateSpecificCulture("en-US")));
                ViewBag.InvoiceNumber = seq.Number + "/" + seq.Prefix + "/" + startformat + "-" + endformat;
                objCore.ConvertToUppercase<InvoiceList>(_InvoiceList);
                objCore.ConvertToUppercase(vehicleDet);

                // Get Previous Invoice Number and Total Amount

                tbl_vendor_invoices venInvoice = db.tbl_vendor_invoices.Where(a => a.Status == true && a.VehicleID == vehicleId).AsEnumerable().OrderBy(a => a.ID).LastOrDefault();

                if (venInvoice != null)
                    ViewBag.PrevInvoice = " Invoice# : " + venInvoice.InvoiceNum.ToString() + ", Invoice Amount : " + venInvoice.InvoiceAmt;
                return PartialView("_GenerateInvoice", new GenerateInvoice(_InvoiceList, vehicleDet, dieselList));
            }
            catch (Exception)
            {
                return PartialView("_GenerateInvoice", new GenerateInvoice(null, vehicleDet, null));
            }
        }
        #endregion

        #region AD-HOC Invoice
        /// <summary>
        /// AD-HOC Invoice generation by selecting logsheets,diesel tracking and advances 
        /// </summary>
        /// <param name="StartDate"></param>
        /// <param name="EndDate"></param>
        /// <param name="vehicleId"></param>
        /// <returns></returns>
        public ActionResult SelectLogSheet(string StartDate, string EndDate, long? vehicleId)
        {
            TempData["PaidLogSheets"] = null;
            TempData["PaidDieselTrack"] = null;
            TempData["PaidAdvanceDet"] = null;
            if (StartDate != null || EndDate != null)
            {
                ViewBag.FromDate = Convert.ToDateTime(StartDate).ToString("dd/MM/yyyy");
                ViewBag.ToDate = Convert.ToDateTime(EndDate).ToString("dd/MM/yyyy");
            }
            ViewBag.vehID = vehicleId;
            if (vehicleId != null)
                ViewBag.VehicleRegNum = db.tbl_vehicles.Where(a => a.Status == true && a.ID == vehicleId).FirstOrDefault().VehicleRegNum;
            else
                ViewBag.VehicleRegNum = "";
            return View();
        }

        public ActionResult GetLogSheets(string StartDate, string EndDate, long vehicleId, int? IsBooking)
        {
            try
            {
                string startdate = Convert.ToDateTime(StartDate).ToString("yyyy-MM-dd");
                string enddate = Convert.ToDateTime(EndDate).ToString("yyyy-MM-dd");
                string qry = "exec Sp_GetLogSheetsByVehicle {0},{1},{2},{3}";
                List<object> MyParam = new List<object>();
                MyParam.Add(vehicleId);
                MyParam.Add(startdate);
                MyParam.Add(enddate);
                MyParam.Add(IsBooking.HasValue ? Convert.ToInt32(IsBooking) : 0);
                var logSheetList = db.ExecuteStoreQuery<GeneralClassFields>(qry, MyParam.ToArray()).ToList();
                ViewBag.StartDate = startdate;
                ViewBag.EndDate = enddate;
                ViewBag.VehId = vehicleId;
                ViewBag.BookingId = IsBooking.HasValue ? IsBooking : 0;
                objCore.ConvertToUppercase<GeneralClassFields>(logSheetList);
                return PartialView("_GetLogSheets", logSheetList);
            }
            catch (Exception)
            {
                return PartialView("Error");
            }
        }

        [HttpPost]
        public ActionResult GetLogSheets(FormCollection frm)
        {
            decimal DieselAmount = 0;
            string logIds = string.Empty;
            string startdate = frm["hdnfromdate"];
            string enddate = frm["hdntodate"];
            int IsBooking = Convert.ToInt32(frm["hdnBooking"]);
            string qry = "exec Sp_GetLogSheetsByVehicle {0},{1},{2},{3}";
            List<object> MyParam = new List<object>();
            MyParam.Add(Convert.ToInt64(frm["hdnVehicleID"]));
            MyParam.Add(startdate);
            MyParam.Add(enddate);
            MyParam.Add(IsBooking);
            var logSheetList = db.ExecuteStoreQuery<GeneralClassFields>(qry, MyParam.ToArray()).ToList();
            List<DieselTracking> dieselList = GetMonthlyDieselByVehicle(Convert.ToInt64(frm["hdnVehicleID"]), startdate, enddate, "", false); // vehicle details by selected dates
            decimal AdvAmt = GetAdvanceAmountByVehicle(Convert.ToInt64(frm["hdnVehicleID"]), "", false);
            if (dieselList != null)
                DieselAmount = dieselList.Sum(a => a.TotalAmt);
            bool chkLog = false;
            if (IsBooking == 1)
            {
                DieselAmount = 0;
                AdvAmt = 0;
            }
            List<GeneralClassFields> AdhocLogSheets = new List<GeneralClassFields>();
            foreach (var item in logSheetList) // loop through for LogSheet Det list
            {
                chkLog = (bool)frm["chkLogId_" + item.ID].Contains("true");
                if (chkLog)
                    AdhocLogSheets.Add(item);
            }
            try
            {
                logIds = string.Join(",", AdhocLogSheets.Select(a => a.ID).ToArray());
                TempData["PaidLogSheets"] = logIds;
                Session["AdhocLogSheets"] = AdhocLogSheets;
            }
            catch (Exception)
            {
                return Json(new { success = false, msg = "An Error occured while processing your request." });
            }
            if (dieselList.Count == 0)
            {
                if (AdvAmt == 0)
                    return Json(new { success = true, msg = "Updated successfully" });
                else
                    return Json(new { success = true, msg = "", advAmt = AdvAmt, dieselAmt = 0 });
            }
            else
                return Json(new { success = true, msg = "", dieselAmt = DieselAmount, advAmt = AdvAmt });
        }

        // Get Diesel Amount and list 
        public ActionResult GetDieselDetailsByVehicle(string StartDate, string EndDate, long VehicleID)
        {
            try
            {
                string startdate = Convert.ToDateTime(StartDate).ToString("yyyy-MM-dd");
                string enddate = Convert.ToDateTime(EndDate).ToString("yyyy-MM-dd");
                string dieselquery = "exec Sp_GetDieselTrackDetailsByVehicle {0},{1},{2}";
                List<object> MyParam = new List<object>();
                MyParam.Add(VehicleID);
                MyParam.Add(startdate);
                MyParam.Add(enddate);
                var logSheetList = db.ExecuteStoreQuery<GeneralClassFields>(dieselquery, MyParam.ToArray()).ToList();

                ViewBag.StartDate = startdate;
                ViewBag.EndDate = enddate;
                ViewBag.VehId = VehicleID;

                return PartialView("_GetDieselDetailsByVehicle", logSheetList);
            }
            catch (Exception)
            {
                return PartialView("Error");
            }
        }
        [HttpPost]
        public ActionResult GetDieselDetailsByVehicle(FormCollection frm)
        {
            string dieselIds = string.Empty;
            string startdate = frm["hdnfromdate"];
            string enddate = frm["hdntodate"];
            string qry = "exec Sp_GetDieselTrackDetailsByVehicle {0},{1},{2}";
            List<object> MyParam = new List<object>();
            MyParam.Add(Convert.ToInt64(frm["hdnVehicleID"]));
            MyParam.Add(startdate);
            MyParam.Add(enddate);
            var dieselList = db.ExecuteStoreQuery<GeneralClassFields>(qry, MyParam.ToArray()).ToList();
            bool chkLog = false;
            List<GeneralClassFields> adHocDieselTracks = new List<GeneralClassFields>();
            decimal AdvAmt = GetAdvanceAmountByVehicle(Convert.ToInt64(frm["hdnVehicleID"]), "", false);
            foreach (var item in dieselList) // loop through for LogSheet Det list
            {
                chkLog = (bool)frm["chkDieselId_" + item.ID].Contains("true");
                if (chkLog) // if checked
                    adHocDieselTracks.Add(item);
            }
            try
            {
                dieselIds = string.Join(",", adHocDieselTracks.Select(a => a.ID).ToArray());
                TempData["PaidDieselTrack"] = dieselIds;
                Session["AdHocDieselTracks"] = adHocDieselTracks;
            }
            catch (Exception)
            {
                return Json(new { success = false, msg = "An error occured while processing your request." });
            }
            if (AdvAmt == 0)
                return Json(new { success = true, msg = "Updated Succesfully" });
            else
                return Json(new { success = true, msg = "", advDet = AdvAmt });
        }

        [HttpGet]
        public ActionResult GetAdvanceDetailsByVehicle(long VehicleId, bool hasDiesel)
        {
            List<tbl_advances> _advList = GetAdvanceListByVehicle(VehicleId);
            ViewBag.VehId = VehicleId;
            ViewBag.hasDiesel = hasDiesel;
            return PartialView("_GetAdvanceDetailsByVehicle", _advList);
        }
        [HttpPost]
        public ActionResult GetAdvanceDetailsByVehicle(FormCollection frm)
        {
            string advIds = string.Empty;
            List<tbl_advances> _advList = GetAdvanceListByVehicle(Convert.ToInt64(frm["hdnVehicleID"]));
            List<tbl_advances> _paidAdvList = new List<tbl_advances>();
            bool chkAdv = false;
            foreach (var item in _advList)
            {
                chkAdv = (bool)frm["chkAdvanceId_" + item.ID].Contains("true");
                if (chkAdv)
                    _paidAdvList.Add(item);
            }
            try
            {
                advIds = string.Join(",", _paidAdvList.Select(a => a.ID).ToArray());
                TempData["PaidAdvanceDet"] = advIds;
            }
            catch (Exception)
            {
                return Json(new { success = false, msg = "An error occured while processing your request." });
            }
            return Json(new { success = true, msg = "" });
        }

        [HttpGet]
        public ActionResult GetEMIAmountByVehicle(long VehicleId, string MonthYear, bool hasDiesel, bool hasAdv)
        {
            DateTime MnYear = Convert.ToDateTime(MonthYear);
            List<tbl_veh_emis> list = objCore.GetVehicleEMIsList(VehicleId, MnYear);
            ViewBag.VehId = VehicleId;
            ViewBag.MonthYear = MonthYear;
            ViewBag.hasDiesel = hasDiesel;
            ViewBag.hasAdv = hasAdv;
            return PartialView("_VehicleEMIList", list);
        }

        public ActionResult GetEMIAmountByVehicle(FormCollection frm)
        {
            string EMIsIds = string.Empty;
            DriverController objDriver = new DriverController();
            List<tbl_veh_emis> _vehEMIsList = objCore.GetVehicleEMIsList(Convert.ToInt64(frm["hdnVehicleID"]), Convert.ToDateTime(frm["hdnMonthYear"]));
            List<tbl_veh_emis> _paidEMIsList = new List<tbl_veh_emis>();
            bool chkEMI = false;
            foreach (var item in _vehEMIsList)
            {
                chkEMI = (bool)frm["chkVehEMI_" + item.Id].Contains("true");
                if (chkEMI)
                    _paidEMIsList.Add(item);
            }
            try
            {
                EMIsIds = string.Join(",", _paidEMIsList.Select(a => a.Id).ToArray());
                TempData["PaidEMIDet"] = EMIsIds;
            }
            catch (Exception)
            {
                return Json(new { success = false, msg = "An error occured while processing your request." });
            }
            return Json(new { success = true, msg = "", IsDriverAmt = objDriver.VerifyDriverSalary(frm["hdnMonthYear"], Convert.ToInt64(frm["hdnVehicleID"])) });
        }

        [HttpGet]
        public ActionResult GetDriverSalaryByVehicle(long VehicleId, string MonthYear, bool hasDiesel, bool hasAdv, bool hasEMIAmt)
        {
            DateTime MnYear = Convert.ToDateTime(MonthYear);
            List<tbl_driver_salaries> list = objCore.GetDriverSalaryList(VehicleId, MnYear);
            ViewBag.VehId = VehicleId;
            ViewBag.MonthYear = MonthYear;
            ViewBag.hasDiesel = hasDiesel;
            ViewBag.hasAdv = hasAdv;
            ViewBag.hasEMIAmt = hasEMIAmt;
            return PartialView("_DriverSalaryList", list);
        }

        [HttpPost]
        public ActionResult GetDriverSalaryByVehicle(FormCollection frm)
        {
            string DriverSalIds = string.Empty;
            List<tbl_driver_salaries> _driverSalList = objCore.GetDriverSalaryList(Convert.ToInt64(frm["hdnVehicleID"]), Convert.ToDateTime(frm["hdnMonthYear"]));
            List<tbl_driver_salaries> _paidSalList = new List<tbl_driver_salaries>();
            bool chkEMI = false;
            foreach (var item in _driverSalList)
            {
                chkEMI = (bool)frm["chkDriverSal_" + item.Id].Contains("true");
                if (chkEMI)
                    _paidSalList.Add(item);
            }
            try
            {
                DriverSalIds = string.Join(",", _paidSalList.Select(a => a.Id).ToArray());
                TempData["PaidDriverSalDet"] = DriverSalIds;
            }
            catch (Exception)
            {
                return Json(new { success = false, msg = "An error occured while processing your request." });
            }
            return Json(new { success = true, msg = "" });
        }

        [HttpGet]
        public ActionResult AdHocInvoiceGeneration(string StartDate, string EndDate, long vehicleId, bool hasDiesel, bool hasAdv, bool hasEMIAmt, bool hasDriverSal, int? IsBooking, string MonthYear)
        {
            ReportsController objReports = new ReportsController();
            tbl_vehicles vehicleDet = db.tbl_vehicles.Where(a => a.ID == vehicleId).SingleOrDefault();
            string startdate = Convert.ToDateTime(StartDate).ToString("yyyy-MM-dd");
            string enddate = Convert.ToDateTime(EndDate).ToString("yyyy-MM-dd");
            int Booking = IsBooking.HasValue ? Convert.ToInt32(IsBooking) : 0;

            // GET LAST MONTH 
            string lastmnthStartdate = Convert.ToDateTime(StartDate).AddMonths(-1).ToString("yyyy-MM-dd");
            string lastmnthEnddate = Convert.ToDateTime(StartDate).AddDays(-1).ToString("yyyy-MM-dd");
            bool IsOwn = vehicleDet.Isown == null ? false : (bool)vehicleDet.Isown;
            ViewBag.hasDiesel = hasDiesel;
            ViewBag.hasAdv = hasAdv;
            ViewBag.IsAdHoc = 1;
            try
            {
                List<InvoiceList> _InvoiceList = GenerateInvoiceList(vehicleId, startdate, enddate, true, TempData["PaidLogSheets"].ToString(), IsBooking);
                ViewBag.ServiceTax = System.Configuration.ConfigurationManager.AppSettings["ServiceTax"];
                ViewBag.DieselTax = System.Configuration.ConfigurationManager.AppSettings["DieselTax"];
                List<DieselTracking> dieselList = new List<DieselTracking>(); List<DieselTracking> PreviousMnthdieselList = new List<DieselTracking>();
                if (hasDiesel == true)
                {
                    dieselList = GetMonthlyDieselByVehicle(vehicleId, startdate, enddate, (TempData["PaidDieselTrack"] == null ? "" : TempData["PaidDieselTrack"].ToString()), true); // vehicle details by selected dates
                    PreviousMnthdieselList = GetMonthlyDieselByVehicle(vehicleId, lastmnthStartdate, lastmnthEnddate, TempData["PaidDieselTrack"].ToString(), true);
                }

                var fromdt = DateTime.Parse(startdate);
                var todt = DateTime.Parse(enddate);

                // Advances,EHSPenalty,Penalty details by selected dates

                ViewBag.AdvanceAmt = hasAdv ? (GetAdvanceAmountByVehicle(vehicleId, (TempData["PaidAdvanceDet"] == null ? "" : TempData["PaidAdvanceDet"].ToString()), true)) : 0;
                ViewBag.EHSPenalty = (from a in db.tbl_ehs_details
                                      where (EntityFunctions.TruncateTime(a.CreatedDate) >= fromdt
                                      && EntityFunctions.TruncateTime(a.CreatedDate) <= todt
                                      && a.VehicleID == vehicleId
                                      && a.Status == true
                                      && (a.ClosedFlag == null || a.ClosedFlag == false))
                                      select (decimal?)a.EHSAmt).Sum() ?? 0;
                ViewBag.Penalty = (from a in db.tbl_penalties
                                   where (EntityFunctions.TruncateTime(a.CreatedDate) >= fromdt
                                   && EntityFunctions.TruncateTime(a.CreatedDate) <= todt
                                   && a.tbl_log_sheets.VehicleID == vehicleId
                                   && a.Status == true
                                   && (a.ClosedFlag == null || a.ClosedFlag == false))
                                   select (decimal?)a.PenalityAmt).Sum() ?? 0;

                //.......................................................
                DateTime MnYear = Convert.ToDateTime(MonthYear);
                ViewBag.PreviousMnthAmt = 0;
                ViewBag.startDate = startdate;
                ViewBag.endDate = enddate;
                ViewBag.MonthYear = MonthYear;
                ViewBag.IsOwn = IsOwn;
                ViewBag.EMIAmt = hasEMIAmt ? objCore.GetMonthlyVehicleEMIByVehicle(MnYear, vehicleId, TempData["PaidEMIDet"].ToString()) : 0;
                ViewBag.DriverSal = hasDriverSal ? objCore.GetMonthlyDriverSalary(MnYear, vehicleId, TempData["PaidDriverSalDet"].ToString()) : 0;

                tbl_sequences seq = db.tbl_sequences.Where(a => a.Auto == true && a.Status == true && a.Type == "INVOICE").FirstOrDefault();
                string startformat = string.Format("{0} {1}", objCore.Ordinal(Convert.ToDateTime(startdate).Day), Convert.ToDateTime(startdate).ToString("MMM", CultureInfo.CreateSpecificCulture("en-US")));
                string endformat = string.Format("{0} {1}", objCore.Ordinal(Convert.ToDateTime(enddate).Day), Convert.ToDateTime(enddate).ToString("MMM", CultureInfo.CreateSpecificCulture("en-US")));
                ViewBag.InvoiceNumber = seq.Number + "/" + seq.Prefix + "/" + startformat + "-" + endformat;
                objCore.ConvertToUppercase<InvoiceList>(_InvoiceList);
                objCore.ConvertToUppercase(vehicleDet);

                // Get Previous Invoice Number and Total Amount

                tbl_vendor_invoices venInvoice = db.tbl_vendor_invoices.Where(a => a.Status == true && a.VehicleID == vehicleId).AsEnumerable().OrderBy(a => a.ID).LastOrDefault();

                if (venInvoice != null)
                    ViewBag.PrevInvoice = " Invoice# : " + venInvoice.InvoiceNum.ToString() + ", Invoice Amount : " + venInvoice.InvoiceAmt;
                return PartialView("_GenerateInvoice", new GenerateInvoice(_InvoiceList, vehicleDet, dieselList));
            }
            catch (Exception)
            {
                return PartialView("_GenerateInvoice", new GenerateInvoice(null, vehicleDet, null));
            }
        }

        // Make Invoice
        [HttpPost]
        public ActionResult GenerateInvoice(FormCollection frm)
        {

            string startdate = Convert.ToDateTime(frm["startDate"]).ToString("yyyy-MM-dd");
            string enddate = Convert.ToDateTime(frm["endDate"]).ToString("yyyy-MM-dd");
            string MnYe = frm["MonthYear"];
            DateTime MonthYear = Convert.ToDateTime(frm["MonthYear"]);
            long vendorId = Convert.ToInt64(frm["VendorID"]);
            long vehicleId = Convert.ToInt64(frm["VehicleID"]);
            decimal DueAmt = frm["hdnGrossDue"] == null ? 0 : Convert.ToDecimal(frm["hdnGrossDue"]);
            DateTime InvoiceDt = Convert.ToDateTime(frm["DueDate"]);
            string InvoiceNum = frm["InvoiceNum"];
            string Remark = frm["Remark"] == null ? "" : frm["Remark"].ToString();
            string ReceiverName = frm["RecSignature"] == null ? "" : frm["RecSignature"];
            string AuthorizedSign = frm["AuthorizedSign"] == null ? "" : frm["AuthorizedSign"];
            bool hasDiesel = Convert.ToBoolean(frm["hasDiesel"]);
            bool hasAdv = Convert.ToBoolean(frm["hasAdv"]);
            int hasAdhoc = (string.IsNullOrEmpty(frm["IsAdhoc"]) ? 0 : (frm["IsAdhoc"] == "0" ? 0 : 1));
            string filterdates = startdate + "," + enddate;
            string[] IncNumsplit = InvoiceNum.Split('/');
            int Number = Convert.ToInt32(IncNumsplit[0]);
            string Prefix = IncNumsplit[1];
            string PaidLogSheetIds = TempData["PaidLogSheets"] == null ? "" : TempData["PaidLogSheets"].ToString();
            string PaidDieselIds = TempData["PaidDieselTrack"] == null ? "" : TempData["PaidDieselTrack"].ToString();
            string PaidAdvIds = TempData["PaidAdvanceDet"] == null ? "" : TempData["PaidAdvanceDet"].ToString();
            string PaidEMIIds = TempData["PaidEMIDet"] == null ? "" : TempData["PaidEMIDet"].ToString();
            string PaidDriverSalIds = TempData["PaidDriverSalDet"] == null ? "" : TempData["PaidDriverSalDet"].ToString();
            bool isAdhoc = hasAdhoc == 1 ? true : false;

            // Apply Values to object of vendor invoice 
            tbl_vendor_invoices objVenInv = new tbl_vendor_invoices();
            objVenInv.VendorID = vendorId;
            objVenInv.VehicleID = vehicleId;
            objVenInv.InvoiceAmt = DueAmt;
            objVenInv.CreatedDate = DateTime.Now;
            objVenInv.InvoiceDate = InvoiceDt;
            objVenInv.Remark = Remark;
            objVenInv.InvoiceNum = InvoiceNum;
            objVenInv.Paid = false;
            objVenInv.PaidAmount = 0;
            objVenInv.Status = true;
            objVenInv.UserName = User.Identity.Name;
            objVenInv.AuthorizedSign = AuthorizedSign;
            objVenInv.ReceiverName = ReceiverName;
            objVenInv.LogIds = PaidLogSheetIds;
            objVenInv.DieslIds = PaidDieselIds;
            objVenInv.AdvIds = PaidAdvIds;
            objVenInv.EMIds = PaidEMIIds;
            objVenInv.DriverSalIds = PaidDriverSalIds;
            objVenInv.MonthYear = MonthYear;
            objVenInv.FilterDates = filterdates;

            db.Connection.Open();
            using (var transaction = db.Connection.BeginTransaction())
            {
                try
                {
                    tbl_vehicles vehDet = db.tbl_vehicles.Where(a => a.ID == vehicleId).SingleOrDefault();
                    // Insert object into vendor invoice table 

                    db.AddTotbl_vendor_invoices(objVenInv);
                    db.SaveChanges();

                    // DO Get Invoice Details list
                    List<tbl_vendor_invoice_details> _InvoiceDetails = GetMonthlyInvoiceDetailsByVehicle(frm, vehicleId, startdate, enddate, objVenInv.ID, (decimal)objVenInv.InvoiceAmt, hasDiesel, isAdhoc, PaidLogSheetIds, PaidDieselIds, (vehDet.Isown == null ? false : (bool)vehDet.Isown));

                    // DO : Insert Invoice Details here 
                    foreach (tbl_vendor_invoice_details obj in _InvoiceDetails)
                        db.AddTotbl_vendor_invoice_details(obj);

                    db.SaveChanges();

                    // Update LogSheet , Diesel & Advance Details as set to closedflag = 1 

                    UpdateClosedFlagByVehicle(startdate, enddate, vehicleId, objVenInv.InvoiceDate, hasDiesel, hasAdv, objVenInv.ID, PaidLogSheetIds, PaidDieselIds, PaidAdvIds, isAdhoc);

                    // Update vehicle emis and driver salary details as set to closedflag = 1 if it is own vehicle

                    if (!string.IsNullOrEmpty(PaidEMIIds) || !string.IsNullOrEmpty(PaidDriverSalIds))
                        UpdateClosedFlagForOwnVehicle(vehicleId, PaidEMIIds, PaidDriverSalIds,  MonthYear);


                    // Verify Whether Payment do or not 

                    int status = 0;

                    // Set to Paid LogSheet 
                    TempData["PaidLogSheets"] = null;
                    TempData["PaidDieselTrack"] = null;
                    TempData["PaidAdvanceDet"] = null;
                    TempData["PaidEMIDet"] = null;
                    TempData["PaidDriverSalDet"] = null;
                    UpdateSequence("INVOICE", Prefix, Number);

                    objCore.LoggingEntries("Vendor Invoice Mgmt.", " Vendor Invoice has generated for the vendor " + objVenInv.tbl_vendor_details.FirstName + " " + objVenInv.tbl_vendor_details.LastName, User.Identity.Name);
                    // Return JSON Result
                    transaction.Commit();
                    db.Connection.Dispose();
                    return Json(new { success = true, msg = "Invoice has been generated successfully and kept in due.", status = status, hasAdhoc = hasAdhoc });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    db.Connection.Dispose();
                    return Json(new { success = false, msg = "An error Occured while submitting : " + ex.Message.ToString(), status = 0, hasAdhoc = hasAdhoc });
                }
            }
        }

        // Excel Generation while generating AD-HOC invoice
        public ActionResult AdhocExportExcel()
        {
            var DutyLogDets = (List<GeneralClassFields>)Session["AdhocLogSheets"];
            var DiselDets = (List<GeneralClassFields>)Session["AdHocDieselTracks"];
            string Filename = "AdhocLogSheet_" + DateTime.Now.ToString("hhmmss") + ".xlsx";
            if (DutyLogDets == null)
            {
                MemoryStream ms1 = new MemoryStream();
                ms1.Seek(0, SeekOrigin.Begin);
                return File(ms1, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml", Filename);
            }
            MemoryStream ms = AdhocExportToExcel(DutyLogDets, DiselDets);
            Session["AdhocLogSheets"] = null;
            Session["AdHocDieselTracks"] = null;
            if (ms != null)
            {
                ms.Seek(0, SeekOrigin.Begin);
                return File(ms, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml", Filename);
            }
            return View();
        }

        public MemoryStream AdhocExportToExcel(List<GeneralClassFields> _logList, List<GeneralClassFields> _dieselList)
        {
            string sheetName = "Sheet1";
            var wb = new XLWorkbook();
            try
            {
                var ws = wb.Worksheets.Add(sheetName);

                ws.Cell("B1").Value = " Adhoc LogSheet ";
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
                ws.Cell("B3").Value = "Date";
                ws.Cell("C3").Value = "Client";
                ws.Cell("D3").Value = "Log Sheet#";
                ws.Cell("E3").Value = "Vehicle Reg#";
                ws.Cell("F3").Value = "Location";
                ws.Cell("G3").Value = "Rate";
                ws.Cell("H3").Value = "Approved KM";

                int i = 4, k = _logList.Count;
                decimal TotAmt = 0;
                foreach (var item in _logList)
                {
                    ws.Cell("B" + i).SetValue(item.Date1.ToShortDateString());
                    ws.Cell(("C" + i)).SetValue(item.Text1.ToString());
                    ws.Cell(("D" + i)).SetValue(item.Text2.ToString());
                    ws.Cell(("E" + i)).SetValue(item.Text3.ToString());
                    ws.Cell(("F" + i)).SetValue(item.Text4.ToString());
                    ws.Cell(("G" + i)).SetValue(item.Value2);
                    ws.Cell(("F" + (k + 4))).SetValue("Total Amount");
                    ws.Cell(("H" + i)).SetValue(item.Value7);
                    TotAmt += (decimal)item.Value2;
                    ws.Cell(("G" + (k + 4))).SetValue(TotAmt);

                    i++;
                }
                // Diesel List 

                if (_dieselList != null)
                {
                    int j, l = 0;
                    j = k + 8;
                    l = j + _dieselList.Count;
                    decimal TotalAmt = 0;
                    ws.Cell("B" + j).Value = "Date";
                    ws.Cell("C" + j).Value = "Client";
                    ws.Cell("D" + j).Value = "Diesel Token#";
                    ws.Cell("E" + j).Value = "Vehicle Reg#";
                    ws.Cell("F" + j).Value = "Litres";
                    ws.Cell("G" + j).Value = "Rate";

                    foreach (var item in _dieselList)
                    {
                        j++;
                        ws.Cell("B" + j).SetValue(item.Date1.ToShortDateString());
                        ws.Cell(("C" + j)).SetValue(item.Text1.ToString());
                        ws.Cell(("D" + j)).SetValue(item.Text2.ToString());
                        ws.Cell(("E" + j)).SetValue(item.Text3.ToString());
                        ws.Cell(("F" + j)).SetValue(item.Value3);
                        ws.Cell(("G" + j)).SetValue(item.Value2);

                        TotalAmt += (decimal)item.Value2;
                    }
                    ws.Cell(("F" + (l + 1))).SetValue("Total Amount");
                    ws.Cell(("G" + (l + 1))).SetValue(TotalAmt);
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

        #region Booking Invoice
        /// <summary>
        /// Booking Invoice generation by start date and end date by vehicle
        /// </summary>
        /// <param name="StartDate"></param>
        /// <param name="EndDate"></param>
        /// <param name="vehicleId"></param>
        /// <returns></returns>
        public ActionResult BookingInvoice(string StartDate, string EndDate, long? vehicleId)
        {
            TempData["PaidLogSheets"] = null;
            TempData["PaidDieselTrack"] = null;
            TempData["PaidAdvanceDet"] = null;
            if (StartDate != null || EndDate != null)
            {
                ViewBag.FromDate = Convert.ToDateTime(StartDate).ToString("dd/MM/yyyy");
                ViewBag.ToDate = Convert.ToDateTime(EndDate).ToString("dd/MM/yyyy");
            }
            ViewBag.vehID = vehicleId;
            if (vehicleId != null)
                ViewBag.VehicleRegNum = db.tbl_vehicles.Where(a => a.Status == true && a.ID == vehicleId).FirstOrDefault().VehicleRegNum;
            else
                ViewBag.VehicleRegNum = "";
            return View();
        }
        #endregion

        #region InvoicePayment
        /// <summary>
        ///  Make Invoice Payment
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult DoInvoicePayment(long Id)
        {
            tbl_vendor_invoices venInvDet = db.tbl_vendor_invoices.Where(a => a.ID == Id && a.Status == true).FirstOrDefault();

            // Get Invoice Details 

            List<tbl_vendor_invoice_details> venInvDetList = GetInvoiceDetails(venInvDet.ID);

            // Get Vehicle Details

            tbl_vehicles vehicleDet = db.tbl_vehicles.Where(a => a.ID == venInvDet.VehicleID).FirstOrDefault();



            return View(new GenerateInvoice(venInvDetList, venInvDet, vehicleDet, null));
        }

        [HttpPost]
        public ActionResult DoInvoicePayment(long Id, FormCollection frm)
        {
            // Get Invoice Detail

            tbl_vendor_invoices venInvDet = db.tbl_vendor_invoices.Where(a => a.ID == Id).FirstOrDefault();

            //string Remark = frm["Remark"] == null ? "" : frm["Remark"].ToString();
            string ReceiverName = frm["RecSignature"] == null ? "" : frm["RecSignature"];
            string AuthorizedSign = frm["AuthorizedSign"] == null ? "" : frm["AuthorizedSign"];
            string PayMode = frm["PayMode"] == null ? "" : frm["PayMode"];
            string ReceptNo = DateTime.Now.ToString("ddMMyyyyhhmmss");

            try
            {
                tbl_vendor_payments objPayment = new tbl_vendor_payments();
                objPayment.VendorID = venInvDet.VendorID;
                objPayment.VehicleID = venInvDet.VehicleID;
                objPayment.InvoiceId = Id;
                objPayment.ReceptNo = ReceptNo;
                objPayment.Amount = venInvDet.InvoiceAmt;
                objPayment.PaidDate = DateTime.Now;
                objPayment.PayMode = PayMode;
                if (PayMode.ToUpper().Trim() == "CHEQUE")
                {
                    string DDChequeNum = frm["ChequeDDNo"] == null ? "" : frm["ChequeDDNo"];
                    objPayment.DDChequeDate = Convert.ToDateTime(frm["ChequeDate"]);
                    objPayment.DDChequeNo = DDChequeNum;
                    objPayment.Branch = frm["BankName"] == null ? "" : frm["BankName"];
                    objPayment.BankName = frm["Branch"] == null ? "" : frm["Branch"];
                }
                if (PayMode.ToUpper().ToString() == "TRANSFER")
                    objPayment.TransactionNum = frm["TransactionNum"] == null ? "" : frm["TransactionNum"];
                objPayment.Status = true;
                objPayment.UserName = User.Identity.Name;

                db.AddTotbl_vendor_payments(objPayment);

                //  Update Invoice Details here 

                TryUpdateModel(venInvDet);

                //venInvDet.Remark = Remark;
                venInvDet.ReceiverName = ReceiverName;
                venInvDet.AuthorizedSign = AuthorizedSign;

                db.SaveChanges();

                // Make Invoice Payment

                MakeInvoicePayment((decimal)venInvDet.InvoiceAmt, Id);

                string[] dates = venInvDet.FilterDates.Split(',').ToArray();

                // Update LogSheets to Paid 
                UpdateLogSheetsToPaid(dates[0].ToString(), dates[1].ToString(), (long)venInvDet.VehicleID);

                objCore.LoggingEntries("Vendor Invoice Mgmt.", " Vendor Invoice payment has done for the vendor " + objPayment.tbl_vendor_details.FirstName + "" + objPayment.tbl_vendor_details.LastName, User.Identity.Name);

            }
            catch (Exception ex)
            {
                return Json(new { success = false, msg = "An Error Occured while submitting : " + ex.Message.ToString() });
            }
            return Json(new { success = true, msg = "Invoice payment has done successfully." });
        }
        #endregion

        #region Invoice Edit
        /// <summary>
        /// Edit Invoice 
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult Edit(long Id)
        {
            tbl_vendor_invoices venDet = db.tbl_vendor_invoices.Where(a => a.ID == Id && a.Status == true).FirstOrDefault();
            return PartialView("_Edit", venDet);
        }
        [HttpPost]
        public ActionResult Edit(long Id, FormCollection frm, tbl_vendor_invoices vendorInvoiceDet)
        {
            tbl_vendor_invoices venDet = db.tbl_vendor_invoices.Where(a => a.ID == Id && a.Status == true).FirstOrDefault();
            try
            {
                venDet.InvoiceAmt = vendorInvoiceDet.InvoiceAmt;
                venDet.InvoiceDate = vendorInvoiceDet.InvoiceDate;
                TryUpdateModel(venDet);
                db.SaveChanges();
                objCore.LoggingEntries("Vendor Invoice", "Vendor Invoice was edited for the vendor " + objCore.GetOwner(venDet.ID) + ".", User.Identity.Name);
                return Json(new { success = true, msg = "Vendor Invoice was updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, msg = "An error occured while processing your request ==>" + ex.Message.ToString() });
            }
        }
        #endregion

        #region View Invoice
        /// <summary>
        ///  View Invoice details and Print 
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public ActionResult Details(long Id)
        {
            var venInvDet = db.tbl_vendor_invoices.Where(a => a.Status == true && a.ID == Id).FirstOrDefault();

            // Get Invoice Details 

            List<tbl_vendor_invoice_details> venInvDetList = GetInvoiceDetails(venInvDet.ID);

            // Get Vehicle Details

            tbl_vehicles vehicleDet = db.tbl_vehicles.Where(a => a.ID == venInvDet.VehicleID).FirstOrDefault();

            // Get Invoice Payment Details 

            tbl_vendor_payments venInvPayment = db.tbl_vendor_payments.Where(a => a.InvoiceId == venInvDet.ID && a.Status == true).FirstOrDefault();

            // Get Previous Invoice Number and Total Amount

            //tbl_vendor_invoices venInvoice = (from v in db.tbl_vendor_invoices
            //                                  where v.Status == true
            //                                  && v.VehicleID == venInvDet.FirstOrDefault().VehicleID
            //                                  && EntityFunctions.TruncateTime(v.CreatedDate) < curInvDate
            //                                  select v).OrderByDescending(a => a.ID).First();

            //if (venInvoice != null)
            //{
            //    if (venInvoice.ID != venInvDet.FirstOrDefault().ID)
            //        ViewBag.PrevInvoice = " Invoice# : " + venInvoice.InvoiceNum.ToString() + ", Invoice Amount : " + venInvoice.InvoiceAmt;
            //}

            return View(new GenerateInvoice(venInvDetList, venInvDet, vehicleDet, venInvPayment));
        }
        #endregion

        #region Delete Invoice
        /// <summary>
        /// Delete Invoice 
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public ActionResult Delete(long Id)
        {

            tbl_vendor_invoices vendorInvoice = db.tbl_vendor_invoices.Where(a => a.ID == Id).SingleOrDefault();

            List<tbl_vendor_invoice_details> InvoiceList = db.tbl_vendor_invoice_details.Where(a => a.InvoiceID == vendorInvoice.ID && a.Status == true).ToList();

            TryUpdateModel(vendorInvoice);
            vendorInvoice.Status = false;

            foreach (tbl_vendor_invoice_details obj in InvoiceList)
            {
                tbl_vendor_invoice_details InvoiceDet = db.tbl_vendor_invoice_details.Where(a => a.Status == true && a.ID == obj.ID).SingleOrDefault();
                db.DeleteObject(InvoiceDet);
            }

            //Get Dates
            List<string> dates = vendorInvoice.FilterDates.Split(',').ToList();
            string startdate = Convert.ToDateTime(dates[0]).ToString("yyyy-MM-dd");
            string enddate = Convert.ToDateTime(dates[1]).ToString("yyyy-MM-dd");
            bool isAdhoc = string.IsNullOrEmpty(vendorInvoice.LogIds) ? false : true;
            
            // Set ClosedFlag to false
            RevertClosedFlagToFalseByVehicle(startdate, enddate, (long)vendorInvoice.VehicleID, vendorInvoice.InvoiceDate, vendorInvoice.ID, vendorInvoice.LogIds, vendorInvoice.DieslIds, vendorInvoice.AdvIds, isAdhoc);

            RevertClosedFlagForOwnVehicle((long)vendorInvoice.VehicleID, vendorInvoice.EMIds, vendorInvoice.DriverSalIds, Convert.ToDateTime(vendorInvoice.MonthYear));

            // If Invoice has made payment 

            List<tbl_vendor_payments> PaymentList = db.tbl_vendor_payments.Where(a => a.InvoiceId == vendorInvoice.ID).ToList();

            foreach (tbl_vendor_payments obj in PaymentList)
            {
                tbl_vendor_payments venPayment = db.tbl_vendor_payments.Where(a => a.ID == obj.ID).FirstOrDefault();
                venPayment.Status = false;
            }

            // Get Transction history 

            List<tbl_vendor_transactions> transList = db.tbl_vendor_transactions.Where(a => a.InvoiceID == vendorInvoice.ID).ToList();

            foreach (tbl_vendor_transactions obj in transList)
            {
                tbl_vendor_transactions transDet = db.tbl_vendor_transactions.Where(a => a.ID == obj.ID).FirstOrDefault();
                db.DeleteObject(transDet);
            }

            db.SaveChanges();
            objCore.LoggingEntries("Vendor Invoice Mgmt.", " Vendor Invoice has deleted for the vendor " + vendorInvoice.tbl_vendor_details.FirstName + "" + vendorInvoice.tbl_vendor_details.LastName, User.Identity.Name);
            return RedirectToAction("Index");
        }
        #endregion

        #region Diesel Transfer
        /// <summary>
        /// Transfer Diesel Tracking
        /// </summary>
        /// <param name="StartDate"></param>
        /// <param name="EndDate"></param>
        /// <param name="vehicleId"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult TransferDieselTracking(string StartDate, string EndDate, long vehicleId)
        {
            string startdate = Convert.ToDateTime(StartDate).ToString("yyyy-MM-dd");
            string enddate = Convert.ToDateTime(EndDate).ToString("yyyy-MM-dd");
            string dieselquery = "exec Sp_GetDieselTrackDetailsByVehicle {0},{1},{2}";
            List<object> MyParam = new List<object>();
            MyParam.Add(vehicleId);
            MyParam.Add(startdate);
            MyParam.Add(enddate);
            var dieselList = db.ExecuteStoreQuery<GeneralClassFields>(dieselquery, MyParam.ToArray()).ToList();

            tbl_vehicles vehDet = db.tbl_vehicles.Where(a => a.ID == vehicleId).FirstOrDefault();

            ViewBag.StartDate = startdate;
            ViewBag.EndDate = enddate;
            ViewBag.VehId = vehicleId;
            ViewBag.VehRegNum = vehDet.VehicleRegNum;
            // To Vehicle DropDown
            ViewBag.VehicleID = db.tbl_vehicles.Where(a => a.Status == true).Select(a => new { a.ID, a.VehicleRegNum }).ToDictionary(a => a.ID, a => a.VehicleRegNum.ToUpper());

            return PartialView("_TransferDieselTracking", dieselList);
        }

        // Post Method 
        [HttpPost]
        public ActionResult TransferDieselTracking(FormCollection frm)
        {
            string startdate = frm["hdnfrmdt"];
            string enddate = frm["hdntodt"];
            string qry = "exec Sp_GetDieselTrackDetailsByVehicle {0},{1},{2}";
            List<object> MyParam = new List<object>();
            MyParam.Add(Convert.ToInt64(frm["hdnVID"]));
            MyParam.Add(startdate);
            MyParam.Add(enddate);
            var dieselList = db.ExecuteStoreQuery<GeneralClassFields>(qry, MyParam.ToArray()).ToList();
            bool chkLog = false;
            long TrnsVehId = Convert.ToInt64(frm["VehicleID"]);
            try
            {
                foreach (var item in dieselList) // loop through for LogSheet Det list
                {
                    chkLog = (bool)frm["chkDieselId_" + item.ID].Contains("true");
                    if (chkLog) // if not checked
                    {
                        // Update Diesel Tracking Details
                        tbl_diesel_tracking dieselTrack = db.tbl_diesel_tracking.Where(a => a.ID == item.ID).FirstOrDefault();
                        dieselTrack.VehicleID = TrnsVehId;
                        TryUpdateModel(dieselTrack);

                        // insert transfer history
                        tbl_diesel_transfer_details objTrans = new tbl_diesel_transfer_details();
                        objTrans.DieselId = dieselTrack.ID;
                        objTrans.ToVehId = TrnsVehId;
                        objTrans.FromVehId = Convert.ToInt64(frm["hdnVID"]);
                        objTrans.CreatedDt = DateTime.Now;
                        objTrans.CreatedBy = User.Identity.Name;

                        db.tbl_diesel_transfer_details.AddObject(objTrans);
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception)
            {
                return Json(new { success = false, msg = "An error occured while processing your request.Please try again." });
            }
            return Json(new { success = true, msg = "Diesel tracking details has been transfered successfully.", startDt = startdate, endDt = enddate, vehId = Convert.ToInt64(frm["hdnVID"]) });
        }
        #endregion

        #region Methods

        public bool VerifyDieselAudit(string StartDt, string EndDt, long vehicleId)
        {
            DateTime fromDt = Convert.ToDateTime(StartDt);
            DateTime toDt = Convert.ToDateTime(EndDt);
            var DiselList = db.tbl_diesel_tracking.Where(a => a.Date.Value >= fromDt.Date && a.Date.Value <= toDt.Date && a.VehicleID == vehicleId && a.Status == true).ToList();
            // Diesel loop through
            foreach (var item in DiselList)
            {
                if (!(item.Audited == null ? false : (bool)item.Audited))
                    return false;
            }
            return true;
        }

        public bool VerifyDocumentAlerts(string VehicleReg)
        {
            HomeController objHome = new HomeController();
            DataTable dt = objHome.GetAlerts();
            string ToConsider = objHome.GetConsiderDocList().Replace(",", "'',''");
            //DataRow[] result = dt.Select("VehicleRegNum='" + VehicleReg + "' AND Priority='Validity Expired'");
            var result = (from row in dt.AsEnumerable()
                          where row.Field<string>("VehicleRegNum") == VehicleReg
                          && row.Field<string>("Priority") == "Validity Expired"
                          && ToConsider.Contains(row.Field<string>("Document"))
                          select row);
            if (result.Count() > 0)
                return true;
            else
                return false;
        }

        public decimal GetAdvanceAmountByVehicle(long VehicleId, string advIds, bool IsUpdate)
        {
            string qry = string.Empty;
            if (IsUpdate == true && string.IsNullOrEmpty(advIds))
                return 0;
            if (string.IsNullOrEmpty(advIds))
                qry = "select convert(decimal,SUM(ISNULL(a.Amount,0) - ISNULL(adv.amount,0))) as value2 from tbl_advances a left join tbl_advance_payments adv on a.id = adv.advId where Status=1 and isnull(closedflag,0)=0 and VehicleID = " + VehicleId;
            else
                qry = "select convert(decimal,SUM(ISNULL(a.Amount,0) - ISNULL(adv.amount,0))) as value2 from tbl_advances a left join tbl_advance_payments adv on a.id = adv.advId where Status=1 and isnull(closedflag,0)=0 and CONVERT(nvarchar,a.ID) in (select Keyword  from dbo.Split('" + advIds + "',','))";
            try
            {
                var list = db.ExecuteStoreQuery<GeneralClassFields>(qry).ToList().FirstOrDefault();
                if (list != null)
                    return list.Value2;
                else
                    return 0;
            }
            catch
            {
                return 0;
            }

        }

        public List<tbl_advances> GetAdvanceListByVehicle(long VehicleId)
        {
            try
            {
                List<tbl_advances> advDetList = (from a in db.tbl_advances
                                                 where a.Status == true
                                                 && (a.ClosedFlag == null || a.ClosedFlag == false)
                                                 && a.VehicleID == VehicleId
                                                 select a).ToList();
                return advDetList;
            }
            catch
            {
                return null;
            }
        }

        public List<InvoiceList> GenerateInvoiceList(long vehicleId, string startDate, string endDate, bool IsAdHoc, string logIds, int? IsBooking)
        {
            try
            {
                string qry = "exec Sp_GetVendorParticulars {0},{1},{2},{3},{4}";
                List<object> MyParam = new List<object>();
                MyParam.Add(vehicleId);
                MyParam.Add(startDate);
                MyParam.Add(endDate);
                MyParam.Add(IsAdHoc);
                MyParam.Add(logIds);
                var particularList = db.ExecuteStoreQuery<InvoiceList>(qry, MyParam.ToArray()).ToList();
                List<InvoiceList> InvoiceList = new List<InvoiceList>();
                // Get Vehicle Details 
                tbl_vehicles vehicleDet = db.tbl_vehicles.Where(a => a.ID == vehicleId).SingleOrDefault();
                long ClientId = 0;
                foreach (var item in particularList)
                {
                    if (IsBooking.HasValue)
                    {
                        item.TotalAmt = Convert.ToDecimal(item.Rate);
                        InvoiceList.Add(item);
                    }
                    else
                    {
                        if (item.IsBooking == 1)
                        {
                            item.TotalAmt = Convert.ToDecimal(item.Rate);
                            InvoiceList.Add(item);
                        }
                        else if (item.BillingType == "TRIPS")
                        {
                            if (ClientId != item.ClientID)  // If BillType is Trip then rate 
                            {
                                item.TotalAmt = TotalAmountByClient(item.ClientID, vehicleId, startDate, endDate, logIds);
                                item.Trips = TotalTripsByClient(item.ClientID, vehicleId, startDate, endDate, logIds);
                                InvoiceList.Add(item);
                            }
                        }
                        else if (item.BillingType == "KILO METER")
                        {
                            if (item.VehicleTypeID == (int)VehicleType.AC)  // 
                                item.Rate = (vehicleDet.VendorAcRate == null ? 0 : (double)vehicleDet.VendorAcRate);
                            else if (item.VehicleTypeID == (int)VehicleType.NONAC)
                                item.Rate = (vehicleDet.VendorRate == null ? 0 : (double)vehicleDet.VendorRate);
                            item.TotalAmt = Convert.ToDecimal(item.Qty * item.Rate);
                            InvoiceList.Add(item);
                        }
                        else if (item.BillingType == "PACKAGE")
                        {
                            item.TotalAmt = Convert.ToDecimal(item.Rate);
                            InvoiceList.Add(item);
                        }
                    }
                    ClientId = item.ClientID;
                }
                return InvoiceList;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public decimal TotalAmountByClient(long ClientID, long VehicleID, string startDate, string endDate, string logIds)
        {
            string qry = string.Empty;
            if (string.IsNullOrEmpty(logIds))
                return 0; //qry = "select isnull(SUM(l.FinalSlabRate),0) as TotalAmt from tbl_log_sheets l inner join tbl_clients c on c.ID = l.ClientID inner join tbl_billing_types b on b.ID = c.BillingTypeID where VehicleID = " + VehicleID + " and CAST(FLOOR(CAST(l.LogDate AS FLOAT)) AS DATETIME) between '" + startDate + "' and '" + endDate + "' and b.BillingType = 'TRIPS' and isnull(l.ClosedFlag,0) = 0 and  l.status = 1 and l.ClientID=" + ClientID;
            else
                qry = "select isnull(SUM(l.FinalSlabRate),0) as TotalAmt from tbl_log_sheets l inner join tbl_clients c on c.ID = l.ClientID inner join tbl_billing_types b on b.ID = c.BillingTypeID where CONVERT(nvarchar,l.ID) in (select Keyword  from dbo.Split('" + logIds + "',','))  and b.BillingType = 'TRIPS' and isnull(l.ClosedFlag,0) = 0 and  l.status = 1 and l.ClientID=" + ClientID;
            var list = db.ExecuteStoreQuery<InvoiceList>(qry).ToList();
            if (list.Count() == 0)
                return 0;
            return list.FirstOrDefault().TotalAmt;
        }
        
        public decimal TotalAmountByClient(long ClientID, long VehicleID, string logIds)
        {
            string qry = string.Empty;
            if (string.IsNullOrEmpty(logIds))
                return 0; 
            else
                qry = "select isnull(SUM(l.FinalNetAmt),0) as TotalAmt from tbl_log_sheets l inner join tbl_clients c on c.ID = l.ClientID inner join tbl_billing_types b on b.ID = c.BillingTypeID where CONVERT(nvarchar,l.ID) in (select Keyword  from dbo.Split('" + logIds + "',','))  and l.BoookRefNum is not null and isnull(l.ClosedFlag,0) = 0 and  l.status = 1 and l.ClientID=" + ClientID;
            var list = db.ExecuteStoreQuery<InvoiceList>(qry).ToList();
            if (list.Count() == 0)
                return 0;
            return list.FirstOrDefault().TotalAmt;
        }

        public int TotalTripsByClient(long ClientID, long VehicleID, string startDate, string endDate, string logIds)
        {
            string qry = string.Empty;
            if (string.IsNullOrEmpty(logIds))
                return 0;//qry = "select isnull(count(TripeType),0) as Trips from tbl_log_sheets l inner join tbl_clients c on c.ID = l.ClientID inner join tbl_billing_types b on b.ID = c.BillingTypeID where l.status = 1 and VehicleID = " + VehicleID + "  and CAST(FLOOR(CAST(l.LogDate AS FLOAT)) AS DATETIME) between '" + startDate + "' and '" + endDate + "' and b.BillingType = 'TRIPS' and isnull(l.ClosedFlag,0) = 0 and l.ClientID=" + ClientID;
            else
                qry = "select isnull(count(TripeType),0) as Trips from tbl_log_sheets l inner join tbl_clients c on c.ID = l.ClientID inner join tbl_billing_types b on b.ID = c.BillingTypeID where l.status = 1 and CONVERT(nvarchar,l.ID) in (select Keyword  from dbo.Split('" + logIds + "',',')) and b.BillingType = 'TRIPS' and isnull(l.ClosedFlag,0) = 0 and l.ClientID=" + ClientID;
            var list = db.ExecuteStoreQuery<InvoiceList>(qry).ToList();
            if (list.Count() == 0)
                return 0;
            return list.FirstOrDefault().Trips;
        }

        public List<DieselTracking> GetMonthlyDieselByVehicle(long vehicleId, string startDate, string endDate, string dieselIds, bool IsUpdate)
        {
            string qry = "";
            List<DieselTracking> dieselDet = new List<DieselTracking>();
            if (IsUpdate == true && string.IsNullOrEmpty(dieselIds))
                return dieselDet;
            if (string.IsNullOrEmpty(dieselIds))
                qry = " select isnull(SUM(TokenValue),0) as TokenValue,isnull(PricePerLiter,0) as PricePerLiter, isnull(sum(TotalAmount),isnull((SUM(TokenValue)*PricePerLiter),0)) as TotalAmt  from tbl_diesel_tracking where status = 1 and VehicleID = " + vehicleId + "  and CAST(FLOOR(CAST(Date AS FLOAT)) AS DATETIME) between '" + startDate + "' and '" + endDate + "' and isnull(ClosedFlag,0) = 0 and isnull(Audited,0) = 1 group by PricePerLiter ";
            else
                qry = " select isnull(SUM(TokenValue),0) as TokenValue,isnull(PricePerLiter,0) as PricePerLiter, isnull(sum(TotalAmount),isnull((SUM(TokenValue)*PricePerLiter),0)) as TotalAmt  from tbl_diesel_tracking where status = 1 and CONVERT(nvarchar,ID) in (select Keyword  from dbo.Split('" + dieselIds + "',',')) and isnull(ClosedFlag,0) = 0 and isnull(Audited,0) = 1 group by PricePerLiter ";
            dieselDet = db.ExecuteStoreQuery<DieselTracking>(qry).ToList();
            return dieselDet;
        }

        public void UpdateClosedFlagByVehicle(string startDate, string endDate, long vehicleId, DateTime? closedDate, bool hasDiesel, bool hasAdv, long? InvoiceId, string logIds, string dieslIds, string advIds, bool isAdhoc)
        {
            core objCore = new core();
            string lastmnthStartdate = Convert.ToDateTime(startDate).AddMonths(-1).ToString("yyyy-MM-dd");
            string lastmnthEnddate = Convert.ToDateTime(startDate).AddDays(-1).ToString("yyyy-MM-dd");
            DateTime frmDt = Convert.ToDateTime(startDate);
            DateTime toDt = Convert.ToDateTime(endDate);
            List<tbl_log_sheets> _logList = objCore.GetLogSheetListByVehicle(startDate, endDate, vehicleId, logIds, isAdhoc);
            List<tbl_penalties> _penaltyList = objCore.GetPenaltiesListByVehicle(startDate, endDate, vehicleId, null);
            List<tbl_advances> _advanceList = null;
            List<tbl_ehs_details> _ehsList = objCore.GetEHSListByVehicle(startDate, endDate, vehicleId, null);
            List<tbl_diesel_tracking> _dieselList = null;

            if (hasDiesel == true)
                _dieselList = objCore.GetDieselListByVehicle(startDate, endDate, vehicleId, null, dieslIds, isAdhoc);
            if (hasAdv)
                _advanceList = objCore.GetAdvancesListByVehicle(startDate, endDate, vehicleId, null, advIds, isAdhoc);


            if (_logList != null)
            {
                foreach (tbl_log_sheets l in _logList)
                {
                    tbl_log_sheets logDet = db.tbl_log_sheets.Where(a => a.ID == l.ID).FirstOrDefault();
                    // Verify LogSheet Type - PACKAGE
                    if (logDet.LogSheetType == "PACKAGE")
                    {
                        tbl_log_sheets logPacDet = null;
                        if (string.IsNullOrEmpty(logIds))
                        {
                            logPacDet = (from a in db.tbl_log_sheets
                                         where a.LogSheetType == "PACKAGE"
                                         && EntityFunctions.TruncateTime(a.LogDate) >= frmDt.Date
                                         && EntityFunctions.TruncateTime(a.LogDate) <= toDt.Date
                                         && EntityFunctions.TruncateTime(a.LogEndDate) >= frmDt.Date
                                         && EntityFunctions.TruncateTime(a.LogEndDate) <= toDt.Date
                                         && a.Status == true
                                         select a).SingleOrDefault();
                        }
                        else
                        {
                            List<long> lstIds = logIds.Split(',').ToList().Select(a => Convert.ToInt64(a)).ToList();
                            logPacDet = (from a in db.tbl_log_sheets
                                         where a.LogSheetType == "PACKAGE"
                                             //&& lstIds.Contains(a.ID)
                                         && a.ID == l.ID
                                         && a.Status == true
                                         select a).SingleOrDefault();
                        }
                        if (logPacDet != null)
                            logPacDet.ClosedFlag = true;
                    }
                    else
                        logDet.ClosedFlag = true;

                    //db.SaveChanges();
                }
            }
            if (_penaltyList != null)
            {
                foreach (tbl_penalties p in _penaltyList)
                {
                    tbl_penalties penDet = db.tbl_penalties.Where(a => a.ID == p.ID).FirstOrDefault();
                    penDet.ClosedFlag = true;
                }
            }
            if (_advanceList != null)
            {
                foreach (tbl_advances a in _advanceList)
                {
                    tbl_advances advDet = db.tbl_advances.Where(s => s.ID == a.ID).FirstOrDefault();
                    advDet.ClosedFlag = true;

                    tbl_advance_payments advPayDet = db.tbl_advance_payments.Where(m => m.AdvId == advDet.ID).FirstOrDefault();

                    if (advDet.Amount == (advPayDet == null ? 0 : advPayDet.Amount))
                        continue;

                    // Do Payment Here for Advance
                    tbl_advance_payments advPayment = new tbl_advance_payments();
                    advPayment.AdvId = a.ID;
                    advPayment.Amount = a.Amount;
                    advPayment.InvoiceId = InvoiceId;
                    advPayment.ReceivedDt = DateTime.Now.Date;
                    advPayment.Remark = "Advance payment has done for Rs /- " + advPayment.Amount;
                    db.tbl_advance_payments.AddObject(advPayment);
                    db.SaveChanges();
                }
            }
            if (_ehsList != null)
            {
                foreach (tbl_ehs_details e in _ehsList)
                {
                    tbl_ehs_details ehsDet = db.tbl_ehs_details.Where(a => a.ID == e.ID).FirstOrDefault();
                    ehsDet.ClosedFlag = true;
                }
            }
            if (_dieselList != null)
            {
                foreach (tbl_diesel_tracking d in _dieselList)
                {
                    tbl_diesel_tracking dieselDet = db.tbl_diesel_tracking.Where(a => a.ID == d.ID).SingleOrDefault();
                    dieselDet.ClosedFlag = true;
                }
            }
            if (!isAdhoc)  // ======> If Not Adhoc
            {
                // Advances,EHSPenalty,Penalty details by previous month 
                List<tbl_penalties> _LastMnthpenaltyList = objCore.GetPenaltiesListByVehicle(lastmnthStartdate, lastmnthEnddate, vehicleId, null);
                List<tbl_advances> _LastMnthadvanceList = objCore.GetAdvancesListByVehicle(lastmnthStartdate, lastmnthEnddate, vehicleId, null, advIds, isAdhoc);
                List<tbl_ehs_details> _LastMnthehsList = objCore.GetEHSListByVehicle(lastmnthStartdate, lastmnthEnddate, vehicleId, null);
                List<tbl_diesel_tracking> _LastMnthdieselList = null;
                if (hasDiesel == true)
                {
                    _LastMnthdieselList = objCore.GetDieselListByVehicle(lastmnthStartdate, lastmnthEnddate, vehicleId, null, dieslIds, isAdhoc);
                }

                if (_LastMnthpenaltyList != null)
                {
                    foreach (tbl_penalties p in _LastMnthpenaltyList)
                    {
                        tbl_penalties penDet = db.tbl_penalties.Where(a => a.ID == p.ID).FirstOrDefault();
                        penDet.ClosedFlag = true;
                        penDet.PrevClosedDate = closedDate;
                    }
                }
                if (_LastMnthadvanceList != null)
                {
                    foreach (tbl_advances a in _LastMnthadvanceList)
                    {
                        tbl_advances advDet = db.tbl_advances.Where(s => s.ID == a.ID).FirstOrDefault();
                        advDet.ClosedFlag = true;
                        advDet.PrevClosedDate = closedDate;
                    }
                }
                if (_LastMnthehsList != null)
                {
                    foreach (tbl_ehs_details e in _LastMnthehsList)
                    {
                        tbl_ehs_details ehsDet = db.tbl_ehs_details.Where(a => a.ID == e.ID).FirstOrDefault();
                        ehsDet.ClosedFlag = true;
                        ehsDet.PrevClosedDate = closedDate;
                    }
                }
                if (_LastMnthdieselList != null)
                {
                    foreach (tbl_diesel_tracking d in _LastMnthdieselList)
                    {
                        tbl_diesel_tracking dieselDet = db.tbl_diesel_tracking.Where(a => a.ID == d.ID).FirstOrDefault();
                        dieselDet.ClosedFlag = true;
                        dieselDet.PrevClosedDate = closedDate;
                    }
                }
            }
            db.SaveChanges();
        }

        public void RevertClosedFlagToFalseByVehicle(string startDate, string endDate, long vehicleId, DateTime? closedDate, long? InvoiceId, string logIds, string dieslIds, string advIds, bool IsAdhoc)
        {
            core objCore = new core();
            List<tbl_log_sheets> _logList = objCore.GetClosedLogSheetListByVehicle(startDate, endDate, vehicleId, logIds);
            List<tbl_penalties> _penaltyList = objCore.GetPenaltiesListByVehicle(startDate, endDate, vehicleId, null);
            List<tbl_advances> _advanceList = objCore.GetClosedAdvancesListByVehicle(startDate, endDate, vehicleId, null, advIds);
            List<tbl_ehs_details> _ehsList = objCore.GetEHSListByVehicle(startDate, endDate, vehicleId, null);
            List<tbl_diesel_tracking> _dieselList = objCore.GetClosedDieselListByVehicle(startDate, endDate, vehicleId, null, dieslIds);
            DateTime frmDt = Convert.ToDateTime(startDate);
            DateTime toDt = Convert.ToDateTime(endDate);
            if (_logList != null)
            {
                foreach (tbl_log_sheets l in _logList)
                {
                    tbl_log_sheets logDet = db.tbl_log_sheets.Where(a => a.ID == l.ID).FirstOrDefault();
                    // Verify LogSheet Type - PACKAGE
                    if (logDet.LogSheetType == "PACKAGE")
                    {
                        tbl_log_sheets logPacDet = null;
                        if (string.IsNullOrEmpty(logIds))
                        {
                            logPacDet = (from a in db.tbl_log_sheets
                                         where a.LogSheetType == "PACKAGE"
                                         && EntityFunctions.TruncateTime(a.LogDate) >= frmDt.Date
                                         && EntityFunctions.TruncateTime(a.LogDate) <= toDt.Date
                                         && EntityFunctions.TruncateTime(a.LogEndDate) >= frmDt.Date
                                         && EntityFunctions.TruncateTime(a.LogEndDate) <= toDt.Date
                                         && a.Status == true
                                         select a).FirstOrDefault();
                        }
                        else
                        {
                            List<long> lstIds = logIds.Split(',').ToList().Select(a => Convert.ToInt64(a)).ToList();
                            logPacDet = (from a in db.tbl_log_sheets
                                         where a.LogSheetType == "PACKAGE"
                                             //&& lstIds.Contains(a.ID)
                                         && a.ID == l.ID
                                         && a.Status == true
                                         select a).SingleOrDefault();
                        }
                        if (logPacDet != null)
                        {
                            logPacDet.ClosedFlag = false; logPacDet.Paid = false;
                        }
                    }
                    else
                    {
                        logDet.ClosedFlag = false;
                        logDet.Paid = false;
                    }

                }
            }
            if (_penaltyList != null)
            {
                foreach (tbl_penalties p in _penaltyList)
                {
                    tbl_penalties penDet = db.tbl_penalties.Where(a => a.ID == p.ID).FirstOrDefault();
                    penDet.ClosedFlag = false;
                }
            }
            if (_advanceList != null)
            {
                // Delete Payment Here 
                var advPayment = db.tbl_advance_payments.Where(ad => ad.InvoiceId == InvoiceId).ToList();
                foreach (tbl_advance_payments advPay in advPayment)
                {
                    // Update Advance Det
                    tbl_advances advDet = db.tbl_advances.Where(s => s.ID == advPay.AdvId).SingleOrDefault();
                    advDet.ClosedFlag = false;
                    tbl_advance_payments advPayDet = db.tbl_advance_payments.Where(ad => ad.Id == advPay.Id).FirstOrDefault();
                    db.tbl_advance_payments.DeleteObject(advPayDet);

                    db.SaveChanges();
                }
            }
            if (_ehsList != null)
            {
                foreach (tbl_ehs_details e in _ehsList)
                {
                    tbl_ehs_details ehsDet = db.tbl_ehs_details.Where(a => a.ID == e.ID).FirstOrDefault();
                    ehsDet.ClosedFlag = false;
                }
            }
            if (_dieselList != null)
            {
                foreach (tbl_diesel_tracking d in _dieselList)
                {
                    tbl_diesel_tracking dieselDet = db.tbl_diesel_tracking.Where(a => a.ID == d.ID).FirstOrDefault();
                    dieselDet.ClosedFlag = false;
                }
            }

            // Advances,EHSPenalty,Penalty details by previous month 
            if (!IsAdhoc)  // -----------> If Not AdHoc 
            {
                string lastmnthStartdate = Convert.ToDateTime(startDate).AddMonths(-1).ToString("yyyy-MM-dd");
                string lastmnthEnddate = Convert.ToDateTime(startDate).AddDays(-1).ToString("yyyy-MM-dd");
                List<tbl_penalties> _LastMnthpenaltyList = objCore.GetPenaltiesListByVehicle(lastmnthStartdate, lastmnthEnddate, vehicleId, closedDate);
                List<tbl_advances> _LastMnthadvanceList = objCore.GetAdvancesListByVehicle(lastmnthStartdate, lastmnthEnddate, vehicleId, closedDate, advIds, IsAdhoc);
                List<tbl_ehs_details> _LastMnthehsList = objCore.GetEHSListByVehicle(lastmnthStartdate, lastmnthEnddate, vehicleId, closedDate);
                List<tbl_diesel_tracking> _LastMnthdieselList = objCore.GetDieselListByVehicle(lastmnthStartdate, lastmnthEnddate, vehicleId, closedDate, dieslIds, IsAdhoc);

                if (_LastMnthpenaltyList != null)
                {
                    foreach (tbl_penalties p in _LastMnthpenaltyList)
                    {
                        tbl_penalties penDet = db.tbl_penalties.Where(a => a.ID == p.ID).FirstOrDefault();
                        penDet.ClosedFlag = false;
                        penDet.PrevClosedDate = null;
                    }
                }
                if (_LastMnthadvanceList != null)
                {
                    foreach (tbl_advances a in _LastMnthadvanceList)
                    {
                        tbl_advances advDet = db.tbl_advances.Where(s => s.ID == a.ID).FirstOrDefault();
                        advDet.ClosedFlag = false;
                        advDet.PrevClosedDate = null;
                    }
                }
                if (_LastMnthehsList != null)
                {
                    foreach (tbl_ehs_details e in _LastMnthehsList)
                    {
                        tbl_ehs_details ehsDet = db.tbl_ehs_details.Where(a => a.ID == e.ID).FirstOrDefault();
                        ehsDet.ClosedFlag = false;
                        ehsDet.PrevClosedDate = null;
                    }
                }
                if (_LastMnthdieselList != null)
                {
                    foreach (tbl_diesel_tracking d in _LastMnthdieselList)
                    {
                        tbl_diesel_tracking dieselDet = db.tbl_diesel_tracking.Where(a => a.ID == d.ID).FirstOrDefault();
                        dieselDet.ClosedFlag = false;
                        dieselDet.PrevClosedDate = null;
                    }
                }
            }
            db.SaveChanges();
        }

        public void UpdateClosedFlagForOwnVehicle(long vehicleId, string EMIIds, string DriverSalIds, DateTime MonthYear)
        {
            List<tbl_veh_emis> _vehEMIList = new List<tbl_veh_emis>() ;
            List<tbl_driver_salaries> _driverSalList = new List<tbl_driver_salaries>();

            if (!string.IsNullOrEmpty(EMIIds))
                _vehEMIList = objCore.GetVehicleEMIsList(vehicleId, MonthYear, EMIIds, false);

            if (!string.IsNullOrEmpty(DriverSalIds))
                _driverSalList = objCore.GetDriverSalaryList(vehicleId, MonthYear, DriverSalIds, false);
            try
            {
                foreach (var _emiDet in _vehEMIList)
                {
                    tbl_veh_emis vehEMI = db.tbl_veh_emis.Where(a => a.Id == _emiDet.Id).SingleOrDefault();
                    TryUpdateModel(vehEMI);
                    vehEMI.ClosedFlag = true;
                }

                foreach (var _driverSalDet in _driverSalList)
                {
                    tbl_driver_salaries _driverSal = db.tbl_driver_salaries.Where(a => a.Id == _driverSalDet.Id).SingleOrDefault();
                    TryUpdateModel(_driverSal);
                    _driverSal.ClosedFlag = true;
                }

                db.SaveChanges();

            }
            catch
            {
                throw;
            }
        }

        public void RevertClosedFlagForOwnVehicle(long vehicleId, string EMIIds, string DriverSalIds, DateTime MonthYear)
        {
            List<tbl_veh_emis> _vehEMIList = new List<tbl_veh_emis>();
            List<tbl_driver_salaries> _driverSalList = new List<tbl_driver_salaries>();

            if (!string.IsNullOrEmpty(EMIIds))
                _vehEMIList = objCore.GetVehicleEMIsList(vehicleId, MonthYear, EMIIds, true);

            if (!string.IsNullOrEmpty(DriverSalIds))
                _driverSalList = objCore.GetDriverSalaryList(vehicleId, MonthYear, DriverSalIds, true);

                try
                {
                    foreach (var _emiDet in _vehEMIList)
                    {
                        tbl_veh_emis vehEMI = db.tbl_veh_emis.Where(a => a.Id == _emiDet.Id).SingleOrDefault();
                        TryUpdateModel(vehEMI);
                        vehEMI.ClosedFlag = false;
                    }

                    foreach (var _driverSalDet in _driverSalList)
                    {
                        tbl_driver_salaries _driverSal = db.tbl_driver_salaries.Where(a => a.Id == _driverSalDet.Id).SingleOrDefault();
                        TryUpdateModel(_driverSal);
                        _driverSal.ClosedFlag = false;
                    }

                    db.SaveChanges();

                }
                catch
                {
                    throw;
                }
        }

        // Method should be called while doing Invoice Payment 
        public void UpdateLogSheetsToPaid(string startDate, string endDate, long vehicleId)
        {
            core objCr = new core();
            List<tbl_log_sheets> _logList = objCr.GetClosedLogSheetListByVehicle(startDate, endDate, vehicleId, "");

            if (_logList != null)
            {
                foreach (tbl_log_sheets l in _logList)
                {
                    tbl_log_sheets logDet = db.tbl_log_sheets.Where(a => a.ID == l.ID).FirstOrDefault();
                    logDet.Paid = true;
                }
            }
            db.SaveChanges();
        }

        public List<tbl_vendor_invoice_details> GetMonthlyInvoiceDetailsByVehicle(FormCollection frm, long VehicleId, string startDate, string endDate, long InvoiceID, decimal NetAmount, bool hasDiesel, bool isAdHoc, string logIds, string dieslIds, bool IsOwn)
        {
            List<InvoiceList> _ClientIds = GenerateInvoiceList(VehicleId, startDate, endDate, isAdHoc, logIds, null);
            List<tbl_vendor_invoice_details> _InvoiceDetails = new List<tbl_vendor_invoice_details>();

            // Add Invoice List Here
            decimal ClientAmt = 0;
            decimal TotalAmt = 0;
            for (int i = 0; i < _ClientIds.Count; i++)
            {
                ClientAmt = frm["hdnTotalAmt_" + _ClientIds[i].ClientID + "_" + _ClientIds[i].VehicleTypeID + ""] == null ? 0 : Convert.ToDecimal(frm["hdnTotalAmt_" + _ClientIds[i].ClientID + "_" + _ClientIds[i].VehicleTypeID + ""]);
                _InvoiceDetails.Add(new tbl_vendor_invoice_details
                {
                    InvoiceID = InvoiceID,
                    Particulars = _ClientIds[i].Client.ToUpper() + "-" + _ClientIds[i].BillingType + "-Rate",
                    Qty = (_ClientIds[i].Rate == 0) ? _ClientIds[i].Trips : _ClientIds[i].Qty,
                    Rate = (_ClientIds[i].Rate == 0) ? " - " : _ClientIds[i].Rate.ToString(),
                    TotalAmt = ClientAmt,
                    Status = true,
                    Type = (int)ParticularType.Earnings

                });
                TotalAmt += ClientAmt;
            }
            // Adding Total Kilo Meters Travel by Vehicle for the month

            //_InvoiceDetails.Add(new tbl_vendor_invoice_details
            //{
            //    InvoiceID = InvoiceID,
            //    Particulars = "Total KM(s)",
            //    Qty = _ClientIds.Sum(a => a.Qty),
            //    Status = true,
            //    Type = (int)ParticularType.Earnings
            //});

            // Adding Total Gross Amount 

            _InvoiceDetails.Add(new tbl_vendor_invoice_details
            {
                InvoiceID = InvoiceID,
                Particulars = "Gross Total Amount",
                NetTotalAmt = TotalAmt,
                Status = true,
                Type = (int)ParticularType.Earnings
            });

            // Adding Dedution @ 1.33 % from NetAmount 

            _InvoiceDetails.Add(new tbl_vendor_invoice_details
            {
                InvoiceID = InvoiceID,
                Particulars = "Less : Deduction @ " + System.Configuration.ConfigurationManager.AppSettings["ServiceTax"] + " % ",
                TotalAmt = Convert.ToDecimal(frm["hdnNetTaxAmt"]),
                Status = true,
                Type = (int)ParticularType.Deductions
            });

            // Nettotal After TDS 

            //_InvoiceDetails.Add(new tbl_vendor_invoice_details
            //{
            //    InvoiceID = InvoiceID,
            //    Particulars = "Gross Amount Deduction ",
            //    NetTotalAmt = Convert.ToDecimal(frm["hdnGrossAmt"]),
            //    Status = true,
            //    Type = (int)ParticularType.Deductions
            //});

            // Diesel Section
            List<DieselTracking> dieselList = new List<DieselTracking>();
            decimal TotaldieselAmt = 0;
            if (hasDiesel == true)
            {
                dieselList = GetMonthlyDieselByVehicle(VehicleId, startDate, endDate, dieslIds, true);
            }

            foreach (var obj in dieselList)
            {
                if (Convert.ToDecimal(obj.TokenValue * (double)obj.PricePerLiter) > 0)
                {
                    _InvoiceDetails.Add(new tbl_vendor_invoice_details
                    {
                        InvoiceID = InvoiceID,
                        Particulars = "Less: Diesel ",
                        Qty = obj.TokenValue,
                        Rate = obj.PricePerLiter.ToString(),
                        TotalAmt = Convert.ToDecimal(obj.TokenValue * (double)obj.PricePerLiter),
                        Status = true,
                        Type = (int)ParticularType.Deductions
                    });
                }
                TotaldieselAmt += Convert.ToDecimal(obj.TokenValue * (double)obj.PricePerLiter) == 0 ? Convert.ToDecimal(frm["NetDieselTotal"]) : Convert.ToDecimal(obj.TokenValue * (double)obj.PricePerLiter);
            }

            if (TotaldieselAmt != 0)
            {
                // Add Total Diesel Amount 
                _InvoiceDetails.Add(new tbl_vendor_invoice_details
                {
                    InvoiceID = InvoiceID,
                    Particulars = "Total Diesel Amount ",
                    TotalAmt = TotaldieselAmt,
                    Status = true,
                    Type = (int)ParticularType.Deductions
                });

                // Adding Diesel Tax 3 %

                _InvoiceDetails.Add(new tbl_vendor_invoice_details
                {
                    InvoiceID = InvoiceID,
                    Particulars = "Less : Diesel @ " + System.Configuration.ConfigurationManager.AppSettings["DieselTax"] + " %",
                    TotalAmt = Convert.ToDecimal(frm["hdnDieselTaxAmt"]),
                    Status = true,
                    Type = (int)ParticularType.Deductions
                });

                //_InvoiceDetails.Add(new tbl_vendor_invoice_details
                //{
                //    InvoiceID = InvoiceID,
                //    Particulars = "Gross Diesel Amount ",
                //    NetTotalAmt = TotaldieselAmt + Convert.ToDecimal(frm["hdnDieselTaxAmt"]),
                //    Status = true,
                //    Type = (int)ParticularType.Deductions
                //});
            }

            // Gross Amount After Diesel and TDS

            _InvoiceDetails.Add(new tbl_vendor_invoice_details
            {
                InvoiceID = InvoiceID,
                Particulars = "Gross Amount After Deduction & Diesel  ",
                TotalAmt = Convert.ToDecimal(frm["hdnGrossDeduction"]),
                NetTotalAmt = Convert.ToDecimal(frm["NetTotal"]) - (Convert.ToDecimal(frm["hdnGrossDeduction"])),
                Status = true,
                Type = (int)ParticularType.Deductions
            });

            // Add Maintenance 
            if ((frm["MaintenanceAmt"] == null ? 0 : Convert.ToDecimal(frm["MaintenanceAmt"])) > 0)
            {
                _InvoiceDetails.Add(new tbl_vendor_invoice_details
                {
                    InvoiceID = InvoiceID,
                    Particulars = "Maintenance Amt.",
                    NetTotalAmt = frm["MaintenanceAmt"] == null ? 0 : Convert.ToDecimal(frm["MaintenanceAmt"]),
                    Status = true,
                    Type = (int)ParticularType.Deductions
                });
            }

            if (IsOwn)
            {
                if ((frm["hdnEMIsAmt"] == null ? 0 : Convert.ToDecimal(frm["hdnEMIsAmt"])) > 0)
                {
                    _InvoiceDetails.Add(new tbl_vendor_invoice_details
                    {
                        InvoiceID = InvoiceID,
                        Particulars = "Vehicle EMIs",
                        NetTotalAmt = frm["hdnEMIsAmt"] == null ? 0 : Convert.ToDecimal(frm["hdnEMIsAmt"]),
                        Status = true,
                        Type = (int)ParticularType.Deductions
                    });
                }
                if ((frm["hdnDriverSal"] == null ? 0 : Convert.ToDecimal(frm["hdnDriverSal"])) > 0)
                {
                    _InvoiceDetails.Add(new tbl_vendor_invoice_details
                    {
                        InvoiceID = InvoiceID,
                        Particulars = "Driver Salary",
                        NetTotalAmt = frm["hdnDriverSal"] == null ? 0 : Convert.ToDecimal(frm["hdnDriverSal"]),
                        Status = true,
                        Type = (int)ParticularType.Deductions
                    });
                }
            }

            // Advances 

            if (Convert.ToDecimal(frm["hdnAdvances"]) > 0)
            {

                _InvoiceDetails.Add(new tbl_vendor_invoice_details
                {
                    InvoiceID = InvoiceID,
                    Particulars = "Less: Advances ",
                    NetTotalAmt = Convert.ToDecimal(frm["hdnAdvances"]),
                    Status = true,
                    Type = (int)ParticularType.Deductions
                });
            }

            // EHS Penalties  
            if (Convert.ToDecimal(frm["hdnEHSPenalties"]) > 0)
            {
                _InvoiceDetails.Add(new tbl_vendor_invoice_details
                {
                    InvoiceID = InvoiceID,
                    Particulars = "Less: EHS Penalties ",
                    NetTotalAmt = Convert.ToDecimal(frm["hdnEHSPenalties"]),
                    Status = true,
                    Type = (int)ParticularType.Deductions
                });
            }
            // Penalties 
            if (Convert.ToDecimal(frm["hdnPenalties"]) > 0)
            {
                _InvoiceDetails.Add(new tbl_vendor_invoice_details
                {
                    InvoiceID = InvoiceID,
                    Particulars = "Less: Penalties ",
                    NetTotalAmt = Convert.ToDecimal(frm["hdnPenalties"]),
                    Status = true,
                    Type = (int)ParticularType.Deductions
                });
            }

            if ((frm["hdnLastMnthDet"] == null ? 0 : Convert.ToDecimal(frm["hdnLastMnthDet"])) > 0)
            {

                // Last month Advances,Diesel ,EHSPenality,Penalty Total Amount

                _InvoiceDetails.Add(new tbl_vendor_invoice_details
                {
                    InvoiceID = InvoiceID,
                    Particulars = "Less: Last Month ",
                    NetTotalAmt = Convert.ToDecimal(frm["hdnLastMnthDet"]),
                    Status = true,
                    Type = (int)ParticularType.Deductions
                });
            }

            // Net Amount

            _InvoiceDetails.Add(new tbl_vendor_invoice_details
            {
                InvoiceID = InvoiceID,
                Particulars = "Net Amount =(Total Amount – Deductions)",
                NetTotalAmt = NetAmount,
                Status = true,
                Type = (int)ParticularType.Deductions
            });
            return _InvoiceDetails;
        }

        public int MakeInvoicePayment(decimal Amount, long InvoiceId)
        {
            decimal fedAmt = 0; // Received Amt
            decimal curInvAmt = 0; // Actual Due
            decimal InvoiceAmt = 0;
            fedAmt = Amount;

            try
            {
                if (Amount != 0)
                {
                    List<tbl_vendor_invoices> Invoices = GetUnPaidInvoice(InvoiceId);
                    if (Invoices != null)
                    {
                        // loop through make invoice payment
                        foreach (tbl_vendor_invoices obj in Invoices)
                        {
                            InvoiceAmt = (decimal)obj.InvoiceAmt;

                            if ((InvoiceAmt - obj.PaidAmount) != 0)
                            {
                                curInvAmt = InvoiceAmt - (decimal)obj.PaidAmount;
                                if (curInvAmt > fedAmt)
                                {
                                    obj.PaidAmount = fedAmt + obj.PaidAmount;
                                    obj.Paid = false;
                                    obj.PaidDate = DateTime.Now;

                                    // Record history while do Transactions

                                    tbl_vendor_transactions vendorTrans = new tbl_vendor_transactions();
                                    vendorTrans.InvoiceID = obj.ID;
                                    vendorTrans.Amount = fedAmt;
                                    vendorTrans.TransactionDt = DateTime.Now;
                                    vendorTrans.VendorID = obj.VendorID;
                                    vendorTrans.Descr = "Actual Invoice Amount " + InvoiceAmt + " accounted partial amount " + fedAmt + " to invoice # " + obj.InvoiceNum;
                                    db.tbl_vendor_transactions.AddObject(vendorTrans);
                                    db.SaveChanges();
                                    fedAmt = 0;
                                }
                                else if (curInvAmt <= fedAmt)
                                {
                                    obj.Paid = true;
                                    obj.PaidDate = DateTime.Now;
                                    obj.PaidAmount = curInvAmt + obj.PaidAmount;

                                    // Record history while do Transactions

                                    tbl_vendor_transactions vendorTrans = new tbl_vendor_transactions();
                                    vendorTrans.InvoiceID = obj.ID;
                                    vendorTrans.Amount = curInvAmt;
                                    vendorTrans.TransactionDt = DateTime.Now;
                                    vendorTrans.VendorID = obj.VendorID;
                                    vendorTrans.Descr = "Accounted due amount " + curInvAmt + " to invoice# " + obj.InvoiceNum;
                                    db.tbl_vendor_transactions.AddObject(vendorTrans);
                                    db.SaveChanges();

                                    fedAmt = fedAmt - curInvAmt;
                                }
                            }
                        } // END Foreach Loop
                    } // END IF 
                } // END IF Amount ! =0 
                return 1;
            } // END TRY BLOCK
            catch (Exception)
            {
                return 0;
            }
        }

        public List<tbl_vendor_invoices> GetUnPaidInvoice(long InvoiceId)
        {
            List<tbl_vendor_invoices> list = db.tbl_vendor_invoices.Where(a => a.ID == InvoiceId && a.Status == true).ToList();
            return list;
        }

        public List<tbl_vendor_invoice_details> GetInvoiceDetails(long InvoiceId)
        {
            return db.tbl_vendor_invoice_details.Where(a => a.InvoiceID == InvoiceId).ToList().Count() == 0 ? null : db.tbl_vendor_invoice_details.Where(a => a.InvoiceID == InvoiceId).ToList();
        }

        // For Advance Details
        public void UpdateAdvanceToPaid(List<tbl_advances> advanceList)
        {
            if (advanceList != null)
            {
                foreach (var item in advanceList)
                {
                    tbl_advances advDet = db.tbl_advances.Where(a => a.ID == item.ID).FirstOrDefault();
                    TryUpdateModel(advDet);
                    advDet.ClosedFlag = true;
                }
                db.SaveChanges();
            }
        }

        public void RevertAdvanceToPaid(List<tbl_advances> advanceList)
        {
            if (advanceList != null)
            {
                foreach (var item in advanceList)
                {
                    tbl_advances advDet = db.tbl_advances.Where(a => a.ID == item.ID).FirstOrDefault();
                    TryUpdateModel(advDet);
                    advDet.ClosedFlag = false;
                }
                db.SaveChanges();
            }
        }
        // For Diesel Tracking Details             
        public void UpdateDieselToPaid(List<GeneralClassFields> dieselList)
        {
            if (dieselList != null)
            {
                foreach (var item in dieselList)
                {
                    tbl_diesel_tracking dieselDet = db.tbl_diesel_tracking.Where(a => a.ID == item.ID).FirstOrDefault();
                    TryUpdateModel(dieselDet);
                    dieselDet.ClosedFlag = true;
                }
                db.SaveChanges();
            }
        }

        public void RevertDieselToUnPaid(List<GeneralClassFields> dieselList)
        {
            if (dieselList != null)
            {
                foreach (var item in dieselList)
                {
                    tbl_diesel_tracking dieselDet = db.tbl_diesel_tracking.Where(a => a.ID == item.ID).FirstOrDefault();
                    TryUpdateModel(dieselDet);
                    dieselDet.ClosedFlag = false;
                }
                db.SaveChanges();
            }
        }
        // Method should be called while generating invoice for AD-HOC
        public void UpdateLogSheetToPaid(List<GeneralClassFields> logSheetList)
        {
            if (logSheetList != null)
            {
                foreach (var item in logSheetList)
                {
                    tbl_log_sheets logSheetDet = db.tbl_log_sheets.Where(a => a.ID == item.ID).FirstOrDefault();
                    TryUpdateModel(logSheetDet);
                    //logSheetDet.PayByCash = true;
                    logSheetDet.ClosedFlag = true;
                }
                db.SaveChanges();
            }
        }

        public void RevertLogSheetToUnPaid(List<GeneralClassFields> logSheetList)
        {
            if (logSheetList != null)
            {
                foreach (var item in logSheetList)
                {
                    tbl_log_sheets logSheetDet = db.tbl_log_sheets.Where(a => a.ID == item.ID).FirstOrDefault();
                    TryUpdateModel(logSheetDet);
                    logSheetDet.Paid = false;
                    logSheetDet.ClosedFlag = false;
                }
                db.SaveChanges();
            }
        }

        public void UpdateSequence(string Type, string Prefix, int Number)
        {
            var Sequence = db.tbl_sequences.Where(a => a.Prefix == Prefix && a.Type == Type && a.Number == Number).SingleOrDefault();
            Sequence.Number = Sequence.Number + 1;
            db.SaveChanges();
        }
        #endregion

    }

    public enum VehicleType
    {
        AC = 1, NONAC = 5
    }
}
