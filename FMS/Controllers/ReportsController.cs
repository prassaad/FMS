using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;
using System.Data.Objects;
using System.IO;
using ClosedXML.Excel;
using System.Data.SqlClient;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.html.simpleparser;
using System.Collections;
using System.Data;
using System.Data.OleDb;
using System.Web.Configuration;
using System.ComponentModel;

namespace FMS.Controllers
{
    [CustomAuthorizationFilter]
    public class ReportsController : Controller
    {
        #region ctors
        private FMSDBEntities db;

        private core objCore = new core();

        public ReportsController()
        {
            db = new FMSDBEntities();
        }
        #endregion

        public ActionResult Index()
        {
            return View();
        }

        #region Vehicle Attendance Report

        // Vehicle Attendance Report
        [Authorize]
        [HttpGet]
        public ActionResult VehicleAttendanceReport()
        {
            ViewBag.VehicleID = db.tbl_vehicles.Where(a => a.Status == true).Select(a => new { a.ID, a.VehicleRegNum }).ToDictionary(a => a.ID, a => a.VehicleRegNum.ToUpper());
            return View();
        }
        [HttpPost]
        public ActionResult VehicleAttendanceReport(string vehicleId, FormCollection frm)
        {
            string StartDate = Convert.ToDateTime(frm["StartDate"]).ToString("yyyy-MM-dd");
            string EndDate = Convert.ToDateTime(frm["EndDate"]).ToString("yyyy-MM-dd");
            List<object> MyParam = new List<object>();
            MyParam.Add(vehicleId); MyParam.Add(StartDate); MyParam.Add(EndDate);
            var VehAttendanceReportList = db.ExecuteStoreQuery<GeneralClassFields>("exec Sp_VehicleAttendanceReport {0},{1},{2}", MyParam.ToArray()).ToList();
            objCore.ConvertToUppercase<GeneralClassFields>(VehAttendanceReportList);
            if (VehAttendanceReportList == null || VehAttendanceReportList.Count() == 0)
                return PartialView("_NoData");
            ViewBag.ListCount = VehAttendanceReportList.Count;
            TempData["VehAttendReportList"] = VehAttendanceReportList;
            return PartialView("_VehicleAttendanceReportList", VehAttendanceReportList);
        }

        // Export to excel for vehicle attendance Report

        public ActionResult VehAttendReportExportExcel()
        {
            var ReportList = (List<GeneralClassFields>)TempData["VehAttendReportList"];
            string Filename = "VehicleAttendReport_" + DateTime.Now.ToString("hhmmss") + ".xlsx";
            if (ReportList == null)
            {
                MemoryStream ms1 = new MemoryStream();
                ms1.Seek(0, SeekOrigin.Begin);
                return File(ms1, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml", Filename);
            }
            MemoryStream ms = VehAttendanceExportToExcel(ReportList);
            if (ms != null)
            {
                ms.Seek(0, SeekOrigin.Begin);
                return File(ms, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml", Filename);
            }
            return View();
        }

        public MemoryStream VehAttendanceExportToExcel(List<GeneralClassFields> list)
        {
            string sheetName = "VehAttendReport";
            var wb = new XLWorkbook();
            try
            {
                var ws = wb.Worksheets.Add(sheetName);

                ws.Cell("B3").Value = "Date";
                ws.Cell("C3").Value = "Client";
                ws.Cell("D3").Value = "Vehicle Reg#";
                ws.Cell("E3").Value = "Trips";

                int i = 4;
                foreach (var item in list)
                {
                    ws.Cell("B" + i).SetValue(item.Date1.ToShortDateString());
                    ws.Cell(("C" + i)).SetValue(item.Text1.ToString());
                    ws.Cell(("D" + i)).SetValue(item.Text2.ToString());
                    ws.Cell(("E" + i)).SetValue(item.Value1);
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

        #region Vendor Invoice Report
        // Paid Invoice Report 
        [Authorize]
        [HttpGet]
        public ActionResult VendorInvoiceReport()
        {
            ViewBag.VehicleID = db.tbl_vehicles.Where(a => a.Status == true).Select(a => new { a.ID, a.VehicleRegNum }).ToDictionary(a => a.ID, a => a.VehicleRegNum);
            return View();
        }
        [HttpPost]
        public ActionResult VendorInvoiceReport(string vehicleId, FormCollection frm)
        {
            string StartDate = Convert.ToDateTime(frm["StartDate"]).ToString("yyyy-MM-dd");
            string EndDate = Convert.ToDateTime(frm["EndDate"]).ToString("yyyy-MM-dd");
            string InvStatus = frm["InvStatus"];
            List<object> MyParam = new List<object>();
            MyParam.Add(vehicleId); MyParam.Add(StartDate); MyParam.Add(EndDate); MyParam.Add(Convert.ToBoolean(InvStatus));
            var VenInvoiceReportList = db.ExecuteStoreQuery<GeneralClassFields>("exec Sp_VendorInvoiceReport {0},{1},{2},{3}", MyParam.ToArray()).ToList();
            objCore.ConvertToUppercase<GeneralClassFields>(VenInvoiceReportList);
            if (VenInvoiceReportList == null || VenInvoiceReportList.Count() == 0)
                return PartialView("_NoData");
            ViewBag.ListCount = VenInvoiceReportList.Count;
            TempData["VenInvoiceReportList"] = VenInvoiceReportList;
            return PartialView("_VendorInvoiceReportList", VenInvoiceReportList);
        }

        // Export to excel for Vendor Invoice Report

        public ActionResult VendorInvoiceReportExportExcel()
        {
            var ReportList = (List<GeneralClassFields>)TempData["VenInvoiceReportList"];
            string Filename = "VenInvoiceReport_" + DateTime.Now.ToString("hhmmss") + ".xlsx";
            if (ReportList == null)
            {
                MemoryStream ms1 = new MemoryStream();
                ms1.Seek(0, SeekOrigin.Begin);
                return File(ms1, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml", Filename);
            }
            MemoryStream ms = VenInvoiceExportToExcel(ReportList);
            if (ms != null)
            {
                ms.Seek(0, SeekOrigin.Begin);
                return File(ms, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml", Filename);
            }
            return View();
        }
        public MemoryStream VenInvoiceExportToExcel(List<GeneralClassFields> list)
        {
            string sheetName = "VenInvoiceReport";
            var wb = new XLWorkbook();
            try
            {
                var ws = wb.Worksheets.Add(sheetName);

                ws.Cell("B3").Value = "Vendor";
                ws.Cell("C3").Value = "Vehicle Reg#";
                ws.Cell("D3").Value = "Dates From";
                ws.Cell("E3").Value = "Invoice#";
                ws.Cell("F3").Value = "Date";
                ws.Cell("G3").Value = "Invoice Amount";
                ws.Cell("H3").Value = "Pay Mode";
                ws.Cell("I3").Value = "Transaction#";
                ws.Cell("J3").Value = "Payment Status";
                ws.Cell("K3").Value = "Log. Qty";
                ws.Cell("L3").Value = "Log. Amt.";
                ws.Cell("M3").Value = "Diesel Qty.";
                ws.Cell("N3").Value = "Diesel Amt.";
                ws.Cell("O3").Value = "Advance Amt.";

                int i = 4, k = list.Count; decimal TotAmt = 0;
                GeneralClassFields objGen = new GeneralClassFields();
                foreach (var item in list)
                {
                    tbl_vendor_invoices venInv = db.tbl_vendor_invoices.Where(a => a.ID == item.ID1).FirstOrDefault();
                    //ws.Row(i).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFFF00");
                    ws.Cell("B" + i).SetValue(item.Text1.ToString());
                    ws.Cell("C" + i).SetValue(item.Text4.ToString());
                    ws.Cell(("D" + i)).SetValue(item.StartDt.ToShortDateString() + " " + item.EndDt.ToShortDateString());
                    ws.Cell(("E" + i)).SetValue(item.Text3.ToString());
                    ws.Cell(("F" + i)).SetValue(item.Date1.ToShortDateString());
                    ws.Cell(("G" + i)).SetValue(item.Value2);
                    ws.Cell(("H" + i)).SetValue(item.Text2);
                    ws.Cell(("I" + i)).SetValue(item.Text5);
                    ws.Cell(("J" + i)).SetValue(item.Status == false ? "Un-Paid" : "Paid");
                    objGen = GetInvioceDetails(item.ID, item.StartDt.ToShortDateString(), item.EndDt.ToShortDateString(), venInv.LogIds, venInv.DieslIds, venInv.AdvIds, item.ID1);
                    ws.Cell(("K" + i)).SetValue(objGen.Value1);
                    ws.Cell(("L" + i)).SetValue(objGen.Value2);
                    ws.Cell(("M" + i)).SetValue(objGen.Value3);
                    ws.Cell(("N" + i)).SetValue(objGen.Value4);
                    ws.Cell(("O" + i)).SetValue(objGen.Value5);
                    TotAmt += item.Value2;
                    i++;
                }
                ws.Cell(("F" + (k + 4))).SetValue("Total");
                ws.Cell(("G" + (k + 4))).SetValue(TotAmt);
                ws.Columns().AdjustToContents();
            }
            catch (Exception ex)
            {

            }
            MemoryStream ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms;
        }
        public GeneralClassFields GetInvioceDetails(long vehId, string startDt, string endDt, string logIds, string dieslIds, string advIds, long InvoiceId)
        {
            string startdate = Convert.ToDateTime(startDt).ToString("yyyy-MM-dd");
            string enddate = Convert.ToDateTime(endDt).ToString("yyyy-MM-dd");
            string qry = "exec Sp_VendorInvoiceDetailsReport {0},{1},{2},{3},{4},{5}";
            List<object> MyParam = new List<object>();
            MyParam.Add(vehId);
            MyParam.Add(startdate);
            MyParam.Add(enddate);
            MyParam.Add(string.IsNullOrEmpty(logIds) ? "" : logIds);
            MyParam.Add(string.IsNullOrEmpty(dieslIds) ? "" : dieslIds);
            MyParam.Add(InvoiceId);
            var objGen = db.ExecuteStoreQuery<GeneralClassFields>(qry, MyParam.ToArray()).FirstOrDefault();
            return objGen;
        }
        public decimal TotalAmountByClient(long ClientID, long VehicleID, string startDate, string endDate, string logIds)
        {
            string qry = string.Empty;
            if (string.IsNullOrEmpty(logIds))
                qry = "select isnull(SUM(l.FinalSlabRate),0) as TotalAmt from tbl_log_sheets l inner join tbl_clients c on c.ID = l.ClientID inner join tbl_billing_types b on b.ID = c.BillingTypeID where VehicleID = " + VehicleID + " and CAST(FLOOR(CAST(l.LogDate AS FLOAT)) AS DATETIME) between '" + startDate + "' and '" + endDate + "' and b.BillingType = 'TRIPS' and isnull(l.ClosedFlag,0) = 1 and  l.status = 1 and l.ClientID=" + ClientID;
            else
                qry = "select isnull(SUM(l.FinalSlabRate),0) as TotalAmt from tbl_log_sheets l inner join tbl_clients c on c.ID = l.ClientID inner join tbl_billing_types b on b.ID = c.BillingTypeID where CONVERT(nvarchar,l.ID) in (select Keyword  from dbo.Split('" + logIds + "',','))  and b.BillingType = 'TRIPS' and isnull(l.ClosedFlag,0) = 1 and  l.status = 1 and l.ClientID=" + ClientID;
            var list = db.ExecuteStoreQuery<InvoiceList>(qry).ToList();
            if (list.Count() == 0)
                return 0;
            return list.FirstOrDefault().TotalAmt;
        }
        #endregion

        #region To be Tracked Diesel Tokens Report
        // To be Tracked Diesel Tracking Report
        [Authorize]
        [HttpGet]
        public ActionResult ToBeTrackedTokenReport()
        {
            return View();
        }
        [HttpPost]
        public ActionResult ToBeTrackedTokenReport(FormCollection frm)
        {
            string StartDate = Convert.ToDateTime(frm["StartDate"]).ToString("yyyy-MM-dd");
            string EndDate = Convert.ToDateTime(frm["EndDate"]).ToString("yyyy-MM-dd");
            // Input Parameters for Stored Procedures 
            List<object> MyParam = new List<object>();
            MyParam.Add(StartDate); MyParam.Add(EndDate);
            var TobeTrackedTokenReportList = db.ExecuteStoreQuery<GeneralClassFields>("exec Sp_ToBeTrackedTokensReport {0},{1}", MyParam.ToArray()).ToList();
            objCore.ConvertToUppercase<GeneralClassFields>(TobeTrackedTokenReportList);
            if (TobeTrackedTokenReportList == null || TobeTrackedTokenReportList.Count() == 0)
                return PartialView("_NoData");
            TempData["lstTobeTrackedTokenDets"] = TobeTrackedTokenReportList;
            ViewBag.ListCount = TobeTrackedTokenReportList.Count;
            return PartialView("_ToBeTrackedTokenReport", TobeTrackedTokenReportList);
        }
        public ActionResult TrackedTokenReportExportExcel()
        {
            string Filename = "TobeTrackedTokens_" + DateTime.Now.ToString("hhmmss") + ".xlsx";
            MemoryStream ms = ExportToExcel();
            if (ms != null)
            {
                ms.Seek(0, SeekOrigin.Begin);
                return File(ms, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml", Filename);
            }
            return View();
        }
        public MemoryStream ExportToExcel()
        {
            string sheetName = "ToBeTrackedTokens";
            var wb = new XLWorkbook();
            try
            {
                var ws = wb.Worksheets.Add(sheetName);
                var dieseltobetrackedList = (List<GeneralClassFields>)TempData["lstTobeTrackedTokenDets"];
                ws.Cell("B1").Value = "To Be Tracked Tokens Details";
                ws.Cell("B2").Value = " Generated on " + DateTime.Now;
                ws.Range("B1:E1").Merge();
                ws.Cell("B1").Style.Font.FontSize = 16;
                ws.Cell("B1").Style.Font.Bold = true;
                ws.Cell("B1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ws.Cell("B1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell("B1").Style.Font.FontColor = XLColor.FromHtml("#41AADF");
                ws.Range("B2:E2").Merge();
                ws.Cell("B2").Style.Font.FontSize = 14;
                ws.Cell("B2").Style.Font.Bold = true;
                ws.Cell("B2").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ws.Cell("B2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell("B3").Value = "Date";
                ws.Cell("C3").Value = "Fuel Station Name";
                ws.Cell("D3").Value = "Book No.";
                ws.Cell("E3").Value = "Diesel Token#";
                ws.Cell("F3").Value = "Litre(s)";
                ws.Cell("G3").Value = "Price/liter";
                ws.Cell("H3").Value = "Site";
                int i = 4;
                foreach (var item in dieseltobetrackedList)
                {
                    ws.Cell("B" + i).SetValue(item.Date1.ToShortDateString());
                    ws.Cell(("C" + i)).SetValue(item.Text1.ToString());
                    ws.Cell(("D" + i)).SetValue(item.Text2.ToString());
                    ws.Cell(("E" + i)).SetValue(item.Text3.ToString());
                    ws.Cell(("F" + i)).SetValue(item.Value3);
                    ws.Cell(("G" + i)).SetValue(item.Value2);
                    ws.Cell(("H" + i)).SetValue(item.Text4.ToString());

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

        #region Diesel Tracking Report
        [HttpGet]
        [Authorize]
        public ActionResult DieselTrackingReport()
        {
            return View();
        }
        [HttpPost]
        public ActionResult DieselTrackingReport(FormCollection frm)
        {
            string StartDate = Convert.ToDateTime(frm["StartDate"]).ToString("yyyy-MM-dd");
            string EndDate = Convert.ToDateTime(frm["EndDate"]).ToString("yyyy-MM-dd");
            // Input Parameters for Stored Procedures 
            List<object> MyParam = new List<object>();
            MyParam.Add(StartDate); MyParam.Add(EndDate);
            var DieselTrackingReportList = db.ExecuteStoreQuery<GeneralClassFields>("exec Sp_GetDieselTrackingReport {0},{1}", MyParam.ToArray()).ToList();
            objCore.ConvertToUppercase<GeneralClassFields>(DieselTrackingReportList);
            if (DieselTrackingReportList == null || DieselTrackingReportList.Count() == 0)
                return PartialView("_NoData");
            ViewBag.ListCount = DieselTrackingReportList.Count;
            TempData["DieselTrackingList"] = DieselTrackingReportList;
            return PartialView("_DieselTrackingReport", DieselTrackingReportList);
        }

        // Export to excel for Diesel Report

        public ActionResult DieselTrackingReportExportExcel()
        {
            var DieselTrackingDets = (List<GeneralClassFields>)TempData["DieselTrackingList"];
            string Filename = "DieselTrackingDets_" + DateTime.Now.ToString("hhmmss") + ".xlsx";
            if (DieselTrackingDets == null)
            {
                MemoryStream ms1 = new MemoryStream();
                ms1.Seek(0, SeekOrigin.Begin);
                return File(ms1, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml", Filename);
            }
            MemoryStream ms = DieselTrackingExportToExcel(DieselTrackingDets);
            if (ms != null)
            {
                ms.Seek(0, SeekOrigin.Begin);
                return File(ms, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml", Filename);
            }
            return View();
        }

        public MemoryStream DieselTrackingExportToExcel(List<GeneralClassFields> list)
        {
            string sheetName = "DieselTracking";
            var wb = new XLWorkbook();
            try
            {
                var ws = wb.Worksheets.Add(sheetName);

                ws.Cell("B1").Value = " Diesel Tracking Report ";
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
                ws.Cell("D3").Value = "Vehicle Reg#";
                ws.Cell("E3").Value = "Book#";
                ws.Cell("F3").Value = "Token#";
                ws.Cell("G3").Value = "Liter(s)";
                ws.Cell("H3").Value = "Price/liter";
                ws.Cell("I3").Value = "Total Amount";
                ws.Cell("J3").Value = "Status";
                ws.Cell("K3").Value = "Invoice Number";
                ws.Cell("L3").Value = "Created On";
                ws.Cell("M3").Value = "Created By";
                ws.Cell("N3").Value = "Audited On";
                ws.Cell("O3").Value = "Audited By";

                int i = 4, k = list.Count;
                decimal TotAmt = 0;
                foreach (var item in list)
                {
                    ws.Cell("B" + i).SetValue(item.Date1.ToShortDateString());
                    ws.Cell(("C" + i)).SetValue(item.Text1.ToString());
                    ws.Cell(("D" + i)).SetValue(item.Text2.ToString());
                    ws.Cell(("E" + i)).SetValue(item.Text3.ToString());
                    ws.Cell(("F" + i)).SetValue(item.Text4.ToString());
                    ws.Cell(("G" + i)).SetValue(item.Value3);
                    ws.Cell(("H" + i)).SetValue(item.Value2);
                    ws.Cell(("I" + i)).SetValue(item.Value4);
                    ws.Cell(("H" + (k + 4))).SetValue("Total Amount");
                    TotAmt += (decimal)item.Value4;
                    ws.Cell(("J" + i)).SetValue(item.Text5);
                    ws.Cell(("K" + i)).SetValue(item.Text6);
                    ws.Cell(("L" + i)).SetValue(string.IsNullOrEmpty(item.Text7) ? "" : item.Text7);
                    ws.Cell(("M" + i)).SetValue(item.Text8);
                    ws.Cell(("N" + i)).SetValue(string.IsNullOrEmpty(item.Text9) ? "" : item.Text9);
                    ws.Cell(("O" + i)).SetValue(item.Text10);
                    i++;

                }
                ws.Cell(("I" + (k + 4))).SetValue(TotAmt);
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

        #region EHS  & PENALITY REPORT
        //EHS  & PENALITY REPORT
        [Authorize]
        [HttpGet]
        public ActionResult EhsAndPenalityReport()
        {
            return View();
        }
        [HttpPost]
        public ActionResult EhsAndPenalityReport(FormCollection frm)
        {
            DateTime StartDate = Convert.ToDateTime(frm["StartDate"]);
            DateTime EndDate = Convert.ToDateTime(frm["EndDate"]);

            List<DateTime> lstDates = GetDatesBetween(StartDate, EndDate);
            decimal EhsTotAmt = EhsTotAmountByDates(StartDate, EndDate);
            decimal PenTotAmt = PenalityTotAmountByDates(StartDate, EndDate);
            ViewBag.TotAmount = (EhsTotAmt + PenTotAmt);
            return PartialView("_EhsAndPenality", lstDates);
        }
        [HttpGet]
        public ActionResult EHS_VehiclesByDate(string Date)
        {
            string date = Convert.ToDateTime(Date).ToString("yyyy-MM-dd");
            ViewBag.Date = Convert.ToDateTime(Date).ToShortDateString();
            List<object> MyParam = new List<object>();
            MyParam.Add(date);
            var VehicleByDateList = db.ExecuteStoreQuery<GeneralClassFields>("exec Sp_EHS_VehiclesByDate {0} ", MyParam.ToArray()).ToList();
            objCore.ConvertToUppercase<GeneralClassFields>(VehicleByDateList);
            return PartialView("_EHS_VehiclesByDate", VehicleByDateList);
        }
        [HttpGet]
        public ActionResult EHS_PenalityByDateAndVehilce(long VehicleId, string Date)
        {
            string date = Convert.ToDateTime(Date).ToString("yyyy-MM-dd");
            ViewBag.Date = Convert.ToDateTime(Date).ToShortDateString();
            List<object> MyParam = new List<object>();
            MyParam.Add(VehicleId); MyParam.Add(date);
            var EHSPenalityByDateAndVehicleList = db.ExecuteStoreQuery<GeneralClassFields>("exec Sp_EHS_PenalityByDateAndVehicle {0},{1} ", MyParam.ToArray()).ToList();
            objCore.ConvertToUppercase<GeneralClassFields>(EHSPenalityByDateAndVehicleList);
            return PartialView("_EHS_PenalityByDateAndVehilce", EHSPenalityByDateAndVehicleList);
        }
        public List<DateTime> GetDatesBetween(DateTime startDate, DateTime endDate)
        {
            List<DateTime> allDates = new List<DateTime>();
            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
                allDates.Add(date);
            return allDates;

        }
        public decimal EhsTotAmountByDates(DateTime start, DateTime End)
        {
            decimal TotAmt = 0;
            List<GeneralClassFields> lstEhsAmt = new List<GeneralClassFields>();
            string query = "select ISNULL(SUM(EHSAmt),0) AS Value2 from tbl_ehs_details where CAST(FLOOR(CAST(CreatedDate AS FLOAT)) AS DATETIME) between '" + start.ToString("yyyy-MM-dd") + "' and '" + End.ToString("yyyy-MM-dd") + "' AND Status=1 ";
            lstEhsAmt = db.ExecuteStoreQuery<GeneralClassFields>(query).ToList();
            if (lstEhsAmt.Count != 0)
                TotAmt = lstEhsAmt[0].Value2;
            return TotAmt;
        }
        public decimal PenalityTotAmountByDates(DateTime start, DateTime End)
        {
            decimal TotAmt = 0;
            List<GeneralClassFields> lstPenAmt = new List<GeneralClassFields>();
            string query = "select ISNULL(SUM(PenalityAmt),0) AS Value2 from tbl_penalties where CAST(FLOOR(CAST(CreatedDate AS FLOAT)) AS DATETIME) between '" + start.ToString("yyyy-MM-dd") + "' and '" + End.ToString("yyyy-MM-dd") + "' AND Status=1 ";
            lstPenAmt = db.ExecuteStoreQuery<GeneralClassFields>(query).ToList();
            if (lstPenAmt.Count != 0)
                TotAmt = lstPenAmt[0].Value2;
            return TotAmt;
        }
        public List<GeneralClassFields> GetEHSPenaltyTotalAmtByEachDate(DateTime Date)
        {
            List<object> MyParam = new List<object>();
            MyParam.Add(Date.ToString("yyyy-MM-dd"));
            List<GeneralClassFields> lstEHSPenaltyAmt = db.ExecuteStoreQuery<GeneralClassFields>("exec Sp_EHS_TotalAmountByDate {0} ", MyParam.ToArray()).ToList();
            return lstEHSPenaltyAmt;
        }
        #endregion

        #region To Be Generated Vendor INVOICES REPORT
        //TO BE GENERATED VENDOR INVOICE REPORT
        [Authorize]
        [HttpGet]
        public ActionResult InvoiceReport()
        {
            return View();
        }

        [HttpPost]
        public ActionResult InvoiceReport(FormCollection frm)
        {
            VendorInvoiceController objVendor = new VendorInvoiceController();
            string startdate = Convert.ToDateTime(frm["StartDate"]).ToString("yyyy-MM-dd");
            string enddate = Convert.ToDateTime(frm["EndDate"]).ToString("yyyy-MM-dd");
            ViewBag.FromDate = startdate;
            ViewBag.ToDate = enddate;
            decimal ServiceTax = Convert.ToDecimal(System.Configuration.ConfigurationManager.AppSettings["ServiceTax"]);
            int DieselTax = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["DieselTax"]);
            var fromdt = DateTime.Parse(startdate);
            var todt = DateTime.Parse(enddate);
            List<long> _logDet = (from a in db.tbl_log_sheets
                                  where (a.LogDate >= fromdt
                                    && a.LogDate <= todt
                                    && a.Status == true
                                    && a.AuditedBy != null
                                    && (a.ClosedFlag == null || a.ClosedFlag == false))
                                  select (long)a.VehicleID).Distinct().ToList();

            List<GeneralClassFields> _lstInvoiceDet = new List<GeneralClassFields>();
            foreach (long VehicleID in _logDet)
            {
                decimal dieselAmt = 0, InvoiceAmt = 0, TotAmt = 0, PreviousMnthAmt = 0;
                tbl_vehicles vehDet = db.tbl_vehicles.Where(a => a.ID == VehicleID).FirstOrDefault();
                long vehicleId = vehDet.ID;
                List<InvoiceList> _InvoiceList = objVendor.GenerateInvoiceList(vehicleId, startdate, enddate, false, "", null);
                if (_InvoiceList.Count != 0)
                {
                    decimal totAmt = _InvoiceList.Select(s => s.TotalAmt).Sum();
                    decimal ServiceTaxAmt = ((ServiceTax * totAmt) / 100);
                    InvoiceAmt = (totAmt - ServiceTaxAmt);
                }
                List<DieselTracking> dieselList = objVendor.GetMonthlyDieselByVehicle(vehicleId, startdate, enddate, "", false); // vehicle details by selected dates
                if (dieselList.Count != 0) // deisel deduction amount caliculation
                {
                    decimal DieselTaxAmt = ((DieselTax * (decimal)dieselList.Select(s => s.TotalAmt).Sum()) / 100);
                    dieselAmt = ((decimal)dieselList.Select(s => s.TotalAmt).Sum() + DieselTaxAmt);
                }
                decimal AdvEHSPenaltyAmt = GetAdvenceEHSPenaltyAmt(vehicleId, fromdt, todt);
                TotAmt = (InvoiceAmt) - (dieselAmt + AdvEHSPenaltyAmt);
                var name = db.tbl_vendor_details.Where(s => s.ID == vehDet.VendorID).FirstOrDefault();
                //// get last month  Details
                string lastmnthStartdate = Convert.ToDateTime(startdate).AddMonths(-1).ToString("yyyy-MM-dd");
                string lastmnthEnddate = Convert.ToDateTime(startdate).AddDays(-1).ToString("yyyy-MM-dd");
                List<DieselTracking> PreviousMnthdieselList = objVendor.GetMonthlyDieselByVehicle(vehicleId, lastmnthStartdate, lastmnthEnddate, "", false);
                //..........................................................................
                // Advances,EHSPenalty,Penalty details by previous month
                var lastfromdt = DateTime.Parse(lastmnthStartdate);
                var lasttodt = DateTime.Parse(lastmnthEnddate);
                // caliculate last month amount
                decimal lastMnthAdvEHSPenaltyAmt = GetAdvenceEHSPenaltyAmt(vehicleId, lastfromdt, lasttodt);
                decimal lmnthdieselAmt = 0;
                if (PreviousMnthdieselList.Count != 0)
                {
                    decimal lastmnthDieselTaxAmt = ((3 * (decimal)PreviousMnthdieselList.Select(s => s.TotalAmt).Sum()) / 100);
                    lmnthdieselAmt = ((decimal)PreviousMnthdieselList.Select(s => s.TotalAmt).Sum() + lastmnthDieselTaxAmt);
                    PreviousMnthAmt = lmnthdieselAmt + lastMnthAdvEHSPenaltyAmt;
                }
                else
                    PreviousMnthAmt = lmnthdieselAmt + lastMnthAdvEHSPenaltyAmt;

                //.......................................................
                _lstInvoiceDet.Add(new GeneralClassFields
                {
                    Text1 = name.FirstName.ToUpper() + " " + name.LastName.ToUpper(),
                    Text2 = vehDet.VehicleRegNum.ToUpper(),
                    Value2 = Math.Round(TotAmt - PreviousMnthAmt),
                    Date1 = DateTime.Now,
                    ID = vehicleId
                });
            }
            objCore.ConvertToUppercase<GeneralClassFields>(_lstInvoiceDet);
            ViewBag.ListCount = _lstInvoiceDet.Count;
            TempData["VendorInvoiceReport"] = _lstInvoiceDet;
            return PartialView("_InvoiceReport", _lstInvoiceDet);
        }

        public decimal GetAdvenceEHSPenaltyAmt(long vehicleId, DateTime fromdt, DateTime todt)
        {
            decimal Amount = 0;
            decimal AdvanceAmt = (from a in db.tbl_advances
                                  where (EntityFunctions.TruncateTime(a.CreatedDate) >= fromdt
                                  && EntityFunctions.TruncateTime(a.CreatedDate) <= todt
                                  && a.VehicleID == vehicleId
                                  && a.Status == true
                                  && (a.ClosedFlag == null || a.ClosedFlag == false))
                                  select (decimal?)a.Amount.Value).Sum() ?? 0;
            decimal EHSPenaltyAmt = (from a in db.tbl_ehs_details
                                     where (EntityFunctions.TruncateTime(a.CreatedDate) >= fromdt
                                     && EntityFunctions.TruncateTime(a.CreatedDate) <= todt
                                     && a.VehicleID == vehicleId
                                     && a.Status == true
                                     && (a.ClosedFlag == null || a.ClosedFlag == false))
                                     select (decimal?)a.EHSAmt).Sum() ?? 0;
            decimal PenaltyAmt = (from a in db.tbl_penalties
                                  where (EntityFunctions.TruncateTime(a.CreatedDate) >= fromdt
                                  && EntityFunctions.TruncateTime(a.CreatedDate) <= todt
                                  && a.tbl_log_sheets.VehicleID == vehicleId
                                  && a.Status == true
                                  && (a.ClosedFlag == null || a.ClosedFlag == false))
                                  select (decimal?)a.PenalityAmt).Sum() ?? 0;
            Amount = (AdvanceAmt + EHSPenaltyAmt + PenaltyAmt);
            return Amount;
        }

        public ActionResult ToBeGenVenInvoiceReportExportExcel()
        {
            var ReportList = (List<GeneralClassFields>)TempData["VendorInvoiceReport"];
            string Filename = "ToBeGenVenInvoiceReport_" + DateTime.Now.ToString("hhmmss") + ".xlsx";
            if (ReportList == null)
            {
                MemoryStream ms1 = new MemoryStream();
                ms1.Seek(0, SeekOrigin.Begin);
                return File(ms1, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml", Filename);
            }
            MemoryStream ms = ToBeGeneratedVenInvoiceExportToExcel(ReportList);
            if (ms != null)
            {
                ms.Seek(0, SeekOrigin.Begin);
                return File(ms, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml", Filename);
            }
            return View();
        }
        public MemoryStream ToBeGeneratedVenInvoiceExportToExcel(List<GeneralClassFields> list)
        {
            string sheetName = "ToBeGenVenInvoiceReport";
            var wb = new XLWorkbook();
            try
            {
                var ws = wb.Worksheets.Add(sheetName);

                ws.Cell("B3").Value = "Invoice Date";
                ws.Cell("C3").Value = "Owner";
                ws.Cell("D3").Value = "Vehicle Reg No#";
                ws.Cell("E3").Value = "Invoice Amount";

                int i = 4, k = list.Count;
                decimal TotAmt = 0;
                foreach (var item in list)
                {
                    ws.Cell("B" + i).SetValue(item.Date1.ToShortDateString());
                    ws.Cell(("C" + i)).SetValue(item.Text1.ToString());
                    ws.Cell(("D" + i)).SetValue(item.Text2.ToString());
                    ws.Cell(("E" + i)).SetValue(item.Value2);
                    TotAmt += item.Value2;
                    i++;

                }
                ws.Cell(("D" + (k + 4))).SetValue("Total");
                ws.Cell(("E" + (k + 4))).SetValue(TotAmt);
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

        #region ADVANCES REPORT
        // ADVANCES REPORT
        [Authorize]
        [HttpGet]
        public ActionResult AdvancesReport()
        {
            return View();
        }
        [HttpPost]
        public ActionResult AdvancesReport(FormCollection frm)
        {
            DateTime StartDate = Convert.ToDateTime(frm["StartDate"]);
            DateTime EndDate = Convert.ToDateTime(frm["EndDate"]);

            List<DateTime> lstDates = GetDatesBetween(StartDate, EndDate);
            decimal AdvTotAmt = AdvancesTotAmountByDates(StartDate, EndDate);
            ViewBag.TotAmount = AdvTotAmt;
            return PartialView("_Advances", lstDates);
        }
        [HttpGet]
        public ActionResult ADV_VendorsByDate(string Date)
        {
            string date = Convert.ToDateTime(Date).ToString("yyyy-MM-dd");
            ViewBag.Date = Convert.ToDateTime(Date).ToShortDateString();
            List<object> MyParam = new List<object>();
            MyParam.Add(date);
            var VendorsByDateList = db.ExecuteStoreQuery<GeneralClassFields>("exec Sp_ADV_VendorsByDate {0} ", MyParam.ToArray()).ToList();
            objCore.ConvertToUppercase<GeneralClassFields>(VendorsByDateList);
            return PartialView("_ADV_VendorsByDate", VendorsByDateList);
        }
        [HttpGet]
        public ActionResult ADV_AdvancesByDateAndVendor(long VendorID, string Date)
        {
            List<GeneralClassFields> AdvancesByDateAndVehicleList = new List<GeneralClassFields>();
            string date = Convert.ToDateTime(Date).ToString("yyyy-MM-dd");
            ViewBag.Date = Convert.ToDateTime(Date).ToShortDateString();
            ViewBag.VendorID = VendorID;
            string query = "SELECT DISTINCT VehicleID AS ID, VehicleRegNum AS Text1,ISNULL(SUM(Amount),0) AS Value2  FROM tbl_advances WHERE  CAST (FLOOR(CAST (CreatedDate AS FLOAT)) AS DATETIME) ='" + date + "' AND VendorID ='" + VendorID + "'  AND Status = 1 GROUP BY VehicleID,VehicleRegNum";
            AdvancesByDateAndVehicleList = db.ExecuteStoreQuery<GeneralClassFields>(query).ToList();
            objCore.ConvertToUppercase<GeneralClassFields>(AdvancesByDateAndVehicleList);
            return PartialView("_ADV_AdvancesByDateAndVendor", AdvancesByDateAndVehicleList);
        }
        public decimal AdvancesTotAmountByDates(DateTime StartDate, DateTime EndDate)
        {
            decimal TotAmt = 0;
            List<GeneralClassFields> lstAdvAmt = new List<GeneralClassFields>();
            string query = "select ISNULL(SUM(Amount),0) AS Value2 from tbl_advances where CAST(FLOOR(CAST(CreatedDate AS FLOAT)) AS DATETIME) between '" + StartDate.ToString("yyyy-MM-dd") + "' and '" + EndDate.ToString("yyyy-MM-dd") + "' AND Status=1 ";
            lstAdvAmt = db.ExecuteStoreQuery<GeneralClassFields>(query).ToList();
            if (lstAdvAmt.Count != 0)
                TotAmt = lstAdvAmt[0].Value2;
            return TotAmt;
        }

        public List<GeneralClassFields> GetAdvenceAmtByEachDate(DateTime Date)
        {
            List<GeneralClassFields> lstAdvAmt = new List<GeneralClassFields>();
            string query = "SELECT ISNULL(SUM(Amount),0) AS Value2 FROM   tbl_advances WHERE  CAST (FLOOR(CAST (CreatedDate AS FLOAT)) AS DATETIME) = '" + Date.ToString("yyyy-MM-dd") + "' AND Status = 1 ";
            lstAdvAmt = db.ExecuteStoreQuery<GeneralClassFields>(query).ToList();
            objCore.ConvertToUppercase<GeneralClassFields>(lstAdvAmt);
            return lstAdvAmt;
        }
        #endregion

        #region Advance MIS Report
        [HttpGet]
        [Authorize]
        public ActionResult MISAdvanceReport()
        {
            ViewBag.VehicleID = db.tbl_vehicles.Where(a => a.Status == true).Select(a => new { a.ID, a.VehicleRegNum }).ToDictionary(a => a.ID, a => a.VehicleRegNum.ToUpper());
            return View();
        }
        [HttpPost]
        public ActionResult MISAdvanceReport(string VehicleID, FormCollection frm)
        {
            string StartDate = Convert.ToDateTime(frm["StartDate"]).ToString("yyyy-MM-dd");
            string EndDate = Convert.ToDateTime(frm["EndDate"]).ToString("yyyy-MM-dd");

            List<object> Params = new List<object>();
            Params.Add(StartDate); Params.Add(EndDate);
            Params.Add(VehicleID);
            var lstAdvDets = db.ExecuteStoreQuery<GeneralClassFields>("exec Sp_AdvanceMISReport {0},{1},{2}", Params.ToArray()).ToList();
            if (lstAdvDets.Count == 0 || lstAdvDets == null)
                return PartialView("_NoData");
            TempData["lstAdvDets"] = lstAdvDets;
            ViewBag.ListCount = lstAdvDets.Count;
            return PartialView("_MISAdvList", lstAdvDets);
        }

        public ActionResult MISAdvanceExportExcel()
        {
            var lstAdvDets = (List<GeneralClassFields>)TempData["lstAdvDets"];
            string Filename = "AdvanceReport_" + DateTime.Now.ToString("hhmmss") + ".xlsx";
            if (lstAdvDets == null)
            {
                MemoryStream ms1 = new MemoryStream();
                ms1.Seek(0, SeekOrigin.Begin);
                return File(ms1, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml", Filename);
            }
            MemoryStream ms = AdvanceExportToExcel(lstAdvDets);
            if (ms != null)
            {
                ms.Seek(0, SeekOrigin.Begin);
                return File(ms, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml", Filename);
            }
            return View();
        }

        public MemoryStream AdvanceExportToExcel(List<GeneralClassFields> list)
        {
            string sheetName = "Sheet1";
            var wb = new XLWorkbook();
            try
            {
                var ws = wb.Worksheets.Add(sheetName);

                ws.Cell("B1").Value = " Advance Report ";
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
                ws.Cell("B3").Value = "Vehicle Reg#";
                ws.Cell("C3").Value = "Driver";
                ws.Cell("D3").Value = "Date";
                ws.Cell("E3").Value = "Advance Amount";
                ws.Cell("F3").Value = "Towards";
                ws.Cell("G3").Value = "PayMode";
                ws.Cell("H3").Value = "Transaction#";
                ws.Cell("I3").Value = "Received Amount";
                ws.Cell("J3").Value = "Invoice#";
                ws.Cell("K3").Value = "Status";

                int i = 4, k = list.Count;
                decimal TotAmt = 0, TotPaid = 0;
                foreach (var item in list)
                {
                    ws.Cell(("B" + i)).SetValue(item.Text1.ToString());
                    ws.Cell(("C" + i)).SetValue(item.Text2);
                    ws.Cell(("D" + i)).SetValue(item.Date1.ToShortDateString());
                    ws.Cell(("E" + i)).SetValue(item.Value2);
                    ws.Cell(("F" + i)).SetValue(item.Text3);
                    ws.Cell(("G" + i)).SetValue(item.Text4);
                    ws.Cell(("H" + i)).SetValue(item.Text5);
                    ws.Cell(("I" + i)).SetValue(item.Value4);
                    ws.Cell(("J" + i)).SetValue(item.Text6);
                    ws.Cell(("K" + i)).SetValue(item.Text7);
                    TotAmt += (decimal)item.Value2;
                    TotPaid += (decimal)item.Value4;
                    i++;

                }
                ws.Cell(("D" + (k + 4))).SetValue("Total Amount");
                ws.Cell(("E" + (k + 4))).SetValue(TotAmt);
                ws.Cell(("H" + (k + 4))).SetValue("Total Received");
                ws.Cell(("I" + (k + 4))).SetValue(TotPaid);
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

        #region Client Invoice Report
        // Client Invoice Report 
        [Authorize]
        public ActionResult ClientInvoiceReport()
        {
            ViewBag.ClientID = db.tbl_clients.Where(a => a.Status == true).Select(a => new { a.ID, a.CompanyName }).ToDictionary(a => a.ID, a => a.CompanyName);
            return View();
        }
        [HttpPost]
        public ActionResult ClientInvoiceReport(string ClientID, FormCollection frm)
        {
            string StartDate = Convert.ToDateTime(frm["StartDate"]).ToString("yyyy-MM-dd");
            string EndDate = Convert.ToDateTime(frm["EndDate"]).ToString("yyyy-MM-dd");
            string InvStatus = frm["InvStatus"];
            List<object> Params = new List<object>();
            Params.Add(ClientID); Params.Add(StartDate); Params.Add(EndDate); Params.Add(InvStatus);
            var clientInvReportList = db.ExecuteStoreQuery<GeneralClassFields>("exec Sp_ClientInvoiceReport {0},{1},{2},{3}", Params.ToArray()).ToList();
            if (clientInvReportList.Count == 0 || clientInvReportList == null)
                return PartialView("_NoData");
            TempData["ClientInvoiceReport"] = clientInvReportList;
            return PartialView("_ClientInvoiceReportList", clientInvReportList);
        }

        // Export Excel

        public ActionResult ClientInvReportExportExcel()
        {
            var reportList = (List<GeneralClassFields>)TempData["ClientInvoiceReport"];
            string Filename = "ClientInvReport_" + DateTime.Now.ToString("hhmmss") + ".xlsx";
            if (reportList == null)
            {
                MemoryStream ms1 = new MemoryStream();
                ms1.Seek(0, SeekOrigin.Begin);
                return File(ms1, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml", Filename);
            }
            MemoryStream ms = ClientInvoiceExportToExcel(reportList);
            if (ms != null)
            {
                ms.Seek(0, SeekOrigin.Begin);
                return File(ms, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml", Filename);
            }
            return View();
        }

        public MemoryStream ClientInvoiceExportToExcel(List<GeneralClassFields> list)
        {
            string sheetName = "ClientInvReport";
            var wb = new XLWorkbook();
            try
            {
                var ws = wb.Worksheets.Add(sheetName);

                ws.Cell("B3").Value = "Client";
                ws.Cell("C3").Value = "Month Year";
                ws.Cell("D3").Value = "Invoice#";
                ws.Cell("E3").Value = "Inv.Particular";
                ws.Cell("F3").Value = "Date";
                ws.Cell("G3").Value = "Invoice Amount";
                ws.Cell("H3").Value = "Payment Status";

                int i = 4, k = list.Count;
                decimal TotAmt = 0;
                foreach (var item in list)
                {
                    ws.Cell("B" + i).SetValue(item.Text1.ToString());
                    ws.Cell(("C" + i)).SetValue(item.Text2.ToString());
                    ws.Cell(("D" + i)).SetValue(item.Text3.ToString());
                    ws.Cell(("E" + i)).SetValue(item.Text4.ToString());
                    ws.Cell(("F" + i)).SetValue(item.Date1.ToString());
                    ws.Cell(("G" + i)).SetValue(item.Value2);
                    ws.Cell(("H" + i)).SetValue(item.Status == true ? "Payment Received" : "Receivable");
                    TotAmt += item.Value2;
                    i++;

                }
                ws.Cell(("F" + (k + 4))).SetValue("Total");
                ws.Cell(("G" + (k + 4))).SetValue(TotAmt);
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

        #region MIS Report
        // MIS Report
        [HttpGet]
        [Authorize]
        public ActionResult MISReport(int? reportType)
        {
            ViewBag.ClientID = db.tbl_clients.Where(a => a.Status == true).Select(a => new { a.ID, a.CompanyName }).ToDictionary(a => a.ID, a => a.CompanyName.ToUpper());
            ViewBag.VehicleID = db.tbl_vehicles.Where(a => a.Status == true).Select(a => new { a.ID, a.VehicleRegNum }).ToDictionary(a => a.ID, a => a.VehicleRegNum.ToUpper());
            ViewBag.ReportType = reportType.HasValue ? reportType : 0;
            return View();
        }
        [HttpPost]
        public ActionResult MISReport(bool IsPack, string ClientID, string VehicleID, FormCollection frm)
        {
            string StartDate = Convert.ToDateTime(frm["StartDate"]).ToString("yyyy-MM-dd");
            string EndDate = Convert.ToDateTime(frm["EndDate"]).ToString("yyyy-MM-dd");
            string Selection = frm["Selection"] == null ? "" : frm["Selection"];
            string LogSheetNum = frm["LogSheetNum"] == null ? "" : frm["LogSheetNum"];
            long LogId = (frm["logId"] == null || frm["logId"] == "") ? 0 : Convert.ToInt64(frm["logId"]);
            if (LogSheetNum != "")
                if (LogId == 0)
                    LogId = db.tbl_log_sheets.Where(a => a.LogSheetNum == LogSheetNum.Trim() && a.Status == true).FirstOrDefault().ID;

            if (Selection != "LogSheet" || LogSheetNum == "")
                LogId = 0;
            List<object> Params = new List<object>();
            Params.Add(StartDate); Params.Add(EndDate);
            Params.Add(VehicleID); Params.Add(ClientID); Params.Add(LogId);
            var lstDutyLogDets = db.ExecuteStoreQuery<DutyLogSheetReportList>("exec Sp_GetLogSheetDetailsByVehicleorClient {0},{1},{2},{3},{4}", Params.ToArray()).ToList();
            if (lstDutyLogDets.Count == 0 || lstDutyLogDets == null)
                return PartialView("_NoData");
            TempData["lstDutyLogDets"] = lstDutyLogDets;
            ViewBag.ListCount = lstDutyLogDets.Count;
            TempData["PackCols"] = IsPack;
            return PartialView("_MISReportList", lstDutyLogDets);
        }

        public ActionResult MISReportExportExcel(int reportType)
        {
            var DutyLogDets = (List<DutyLogSheetReportList>)TempData["lstDutyLogDets"];
            bool IsPack = Convert.ToBoolean(TempData["PackCols"]);
            string Filename = "DutyLogSheet_" + DateTime.Now.ToString("hhmmss") + ".xlsx";
            if (DutyLogDets == null)
            {
                MemoryStream ms1 = new MemoryStream();
                ms1.Seek(0, SeekOrigin.Begin);
                return File(ms1, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml", Filename);
            }
            DataTable dt = objCore.ConvertToDataTable(DutyLogDets);
            MemoryStream ms;
            if (reportType == 1) // For Report Type is 1 for Booking 
            {
                DataTable packDt = GetPackageDataTable(dt, 3); // <=== Status 3 is for Booking  
                ms = ExportToExcel(packDt, 0);
            }
            else
            {
                if (IsPack)
                {
                    DataTable packDt = GetPackageDataTable(dt, 1); // <=== Status 2 is for Trip
                    ms = ExportToExcel(packDt, 0);
                }
                else
                {
                    DataTable packDt = GetPackageDataTable(dt, 2); // <=== Status 2 is for package
                    ms = ExportToExcel(packDt, 0);
                }
            }

            if (ms != null)
            {
                ms.Seek(0, SeekOrigin.Begin);
                return File(ms, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml", Filename);
            }
            return View();
        }

        private IEnumerable<DutyLogSheetReportList> GetDutyLogSheetList(List<tbl_log_sheets> lstlogDets)
        {
            var list = new List<DutyLogSheetReportList>();
            foreach (tbl_log_sheets ls in lstlogDets)
            {
                list.Add(new DutyLogSheetReportList
                {
                    ID = ls.ID,
                    LogDate = Convert.ToDateTime(ls.LogDate),
                    Client = ls.ClientID == null ? "" : objCore.GetClient((long)ls.ClientID),
                    VehicleReg = objCore.GetVehicleRegNumber((long)ls.VehicleID),
                    VehicleType = objCore.GetVehicleType((long)ls.VehicleTypeID),
                    VehicleModel = objCore.GetVehicleModel((long)ls.VehicleModelID),
                    Driver = ls.DriverID == null ? "" : objCore.GetDriver((long)ls.DriverID),
                    LogSheetNum = ls.LogSheetNum,
                    TripType = ls.TripeType,
                    Pax = ls.Pax == null ? 0 : (int)ls.Pax,
                    ShiftTime = ls.ShiftTime,
                    Location = ls.Location,
                    TotalKM = ls.TotalKM == null ? 0 : (int)ls.TotalKM,
                    Approved = ls.Approved == null ? 0 : (int)ls.Approved,
                    PassengerEmpID = ls.PassengerEmpID == null ? "" : ls.PassengerEmpID,
                    SlabRate = ls.SlabRate == null ? 0 : (decimal)ls.SlabRate,
                    FinalApprovedKm = ls.FinalApprovedKM == null ? 0 : (int)ls.FinalApprovedKM,
                    FinalSlabRate = ls.FinalSlabRate == null ? 0 : (decimal)ls.FinalSlabRate,
                    Status = (bool)ls.Status

                });
            }
            return list.OrderBy(a => a.ID);
        }

        public MemoryStream ExportToExcel(DataTable table, int reportType)
        {

            string sheetName = "Sheet1";
            var wb = new XLWorkbook();
            try
            {
                var ws = wb.Worksheets.Add(sheetName);
                if (reportType == 1)
                {

                    ws.Cell("B1").Value = "Document Alerts";
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
                }

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
                        if (c.ColumnName == "Paid")
                            ws.Cell(iRow, iCol).SetValue((bool)r[c.ColumnName] == true ? "Paid" : "UnPaid");
                        else
                            ws.Cell(iRow, iCol).SetValue(r[c.ColumnName] == null ? "" : r[c.ColumnName]);
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

        public MemoryStream ExportToExcel(DataTable table, string reportType)
        {

            string sheetName = "Sheet1";
            var wb = new XLWorkbook();
            try
            {
                var ws = wb.Worksheets.Add(sheetName);
                if (ws != null)
                {

                    ws.Cell("B1").Value = reportType;
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
                }

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
                        ws.Cell(iRow, iCol).SetValue(r[c.ColumnName] == null ? "" : r[c.ColumnName]);
                    }
                }
                ws.Columns().AdjustToContents();
            }
            catch (SqlException)
            {
                throw;
            }
            MemoryStream ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms;
        }

        public DataTable GetPackageDataTable(DataTable dt, int Flag)
        {
            DataTable tempDt = new DataTable();
            foreach (DataColumn dc in dt.Columns)
            {
                if (Flag == 1) // Pack Columns
                {
                    if (GetTripColumns(dc.ColumnName.Trim()))
                        tempDt.Columns.Add(dc.ColumnName, dc.DataType);
                }
                else if (Flag == 2) // Trip Columns
                {
                    if (GetPackageColumns(dc.ColumnName.Trim()))
                        tempDt.Columns.Add(dc.ColumnName, dc.DataType);
                }
                else if (Flag == 3) // Booking Columns
                {
                    if (GetBookingColumns(dc.ColumnName.Trim()))
                        tempDt.Columns.Add(dc.ColumnName, dc.DataType);
                }
            }
            foreach (DataRow dr in dt.Rows)
            {
                DataRow drNew = tempDt.NewRow();
                foreach (DataColumn dc in dt.Columns)
                {
                    if (Flag == 1) // Package columns
                    {
                        if (GetTripColumns(dc.ColumnName.Trim()))
                            drNew[dc.ColumnName] = dr[dc.ColumnName];

                    }
                    else if (Flag == 2) // Trip
                    {
                        if (GetPackageColumns(dc.ColumnName.Trim()))
                            drNew[dc.ColumnName] = dr[dc.ColumnName];
                    }
                    else if (Flag == 3) // Booking
                    {
                        if (GetBookingColumns(dc.ColumnName.Trim()))
                            drNew[dc.ColumnName] = dr[dc.ColumnName];
                    }
                }
                tempDt.Rows.Add(drNew);
            }
            return tempDt;
        }

        public bool GetTripColumns(string colNames)
        {
            string[] arry = { "TripType", "Pax", "ShiftTime", "ReachTime", "FullLocation", "TravelPax", "TotalHrs", "Location", "StartKM", "EndKM", "TotalKM", "ExtraKMAmt", "ExtraHrAmt", "Approved", "PassengerEmpID", "SlabRate", "FinalApprovedKm", "FinalSlabRate", "BookRefNum", "GuestName" };
            if (arry.Contains(colNames))
                return false;
            else
                return true;
        }
        public bool GetPackageColumns(string ColNames)
        {
            string[] arry = { "LogEndDt", "WorkingDays", "ReachTime", "FullLocation", "WorkingHrs", "TotalHrs", "ExtraKM", "ExtraKMRate", "ExtraKMAmt", "ExtraHr", "ExtraHrRate", "ExtraHrAmt", "PackModel", "GrossAmt", "TollChrg", "ParkingChrg", "FuelHike", "NetAmount", "BookRefNum", "GuestName" };
            if (arry.Contains(ColNames))
                return false;
            else
                return true;
        }
        public bool GetBookingColumns(string colNames)
        {
            string[] arry = { "Pax", "TravelPax", "Location", "WorkingDays", "WorkingHrs", "PackModel", "PassengerEmpID", "SlabRate", "FinalApprovedKm", "FinalSlabRate", "AuditRemark", "Seater" };
            if (arry.Contains(colNames))
                return false;
            else
                return true;
        }

        #endregion

        #region Booking MIS Report 
        [HttpPost]
        public ActionResult BookingMISReport(bool IsPack, string ClientID, string VehicleID, FormCollection frm)
        {
            string StartDate = Convert.ToDateTime(frm["StartDate"]).ToString("yyyy-MM-dd");
            string EndDate = Convert.ToDateTime(frm["EndDate"]).ToString("yyyy-MM-dd");
            string Selection = frm["Selection"] == null ? "" : frm["Selection"];
            string LogSheetNum = frm["LogSheetNum"] == null ? "" : frm["LogSheetNum"];
            long LogId = (frm["logId"] == null || frm["logId"] == "") ? 0 : Convert.ToInt64(frm["logId"]);
            if (LogSheetNum != "")
                if (LogId == 0)
                    LogId = db.tbl_log_sheets.Where(a => a.LogSheetNum == LogSheetNum.Trim() && a.Status == true).FirstOrDefault().ID;

            if (Selection != "LogSheet" || LogSheetNum == "")
                LogId = 0;
            List<object> Params = new List<object>();
            Params.Add(StartDate); Params.Add(EndDate);
            Params.Add(VehicleID); Params.Add(ClientID); Params.Add(LogId);
            var lstDutyLogDets = db.ExecuteStoreQuery<DutyLogSheetReportList>("exec Sp_MISBookingLogSheets {0},{1},{2},{3},{4}", Params.ToArray()).ToList();
            if (lstDutyLogDets.Count == 0 || lstDutyLogDets == null)
                return PartialView("_NoData");
            TempData["lstDutyLogDets"] = lstDutyLogDets;
            ViewBag.ListCount = lstDutyLogDets.Count;
            TempData["PackCols"] = IsPack;
            return PartialView("_BookMISPartail", lstDutyLogDets);
        }
        #endregion

        #region User Activities Report
        [HttpGet]
        [Authorize]
        public ActionResult UserActivityReport()
        {
            ViewBag.Users = db.aspnet_Users.Select(a => new { a.UserName }).ToDictionary(a => a.UserName, a => a.UserName);
            return View();
        }
        [HttpPost]
        public ActionResult UserActivityReport(string UserName, FormCollection frm)
        {
            string StartDate = Convert.ToDateTime(frm["StartDate"]).ToString("yyyy-MM-dd");
            string EndDate = Convert.ToDateTime(frm["EndDate"]).ToString("yyyy-MM-dd");
            List<object> Params = new List<object>();
            Params.Add(StartDate); Params.Add(EndDate);
            Params.Add(UserName);
            var UserActivitiesList = db.ExecuteStoreQuery<GeneralClassFields>("exec Sp_GetUserActivitiesByUser {0},{1},{2}", Params.ToArray()).ToList();
            ViewBag.ListCount = UserActivitiesList.Count;
            if (UserActivitiesList.Count == 0 || UserActivitiesList == null)
                return PartialView("_NoData");
            return PartialView("_UserActivityReportList", UserActivitiesList);
        }

        #endregion

        #region AuditLogSheetReport
        // MIS Report
        [HttpGet]
        [Authorize]
        public ActionResult AuditLogSheetReport()
        {
            ViewBag.ClientID = db.tbl_clients.Where(a => a.Status == true).Select(a => new { a.ID, a.CompanyName }).ToDictionary(a => a.ID, a => a.CompanyName.ToUpper());
            ViewBag.VehicleID = db.tbl_vehicles.Where(a => a.Status == true).Select(a => new { a.ID, a.VehicleRegNum }).ToDictionary(a => a.ID, a => a.VehicleRegNum.ToUpper());
            return View();
        }
        [HttpPost]
        public ActionResult AuditLogSheetReport(string ClientID, string VehicleID, FormCollection frm)
        {
            string StartDate = Convert.ToDateTime(frm["StartDate"]).ToString("yyyy-MM-dd");
            string EndDate = Convert.ToDateTime(frm["EndDate"]).ToString("yyyy-MM-dd");
            string Selection = frm["Selection"] == null ? "" : frm["Selection"];
            string LogSheetNum = frm["LogSheetNum"] == null ? "" : frm["LogSheetNum"];
            long LogId = (frm["logId"] == null || frm["logId"] == "") ? 0 : Convert.ToInt64(frm["logId"]);
            if (LogSheetNum != "")
                if (LogId == 0)
                    LogId = db.tbl_log_sheets.Where(a => a.LogSheetNum == LogSheetNum.Trim() && a.Status == true).FirstOrDefault().ID;

            if (Selection != "LogSheet" || LogSheetNum == "")
                LogId = 0;
            List<object> Params = new List<object>();
            Params.Add(StartDate); Params.Add(EndDate);
            Params.Add(VehicleID); Params.Add(ClientID); Params.Add(LogId);
            var lstDutyLogDets = db.ExecuteStoreQuery<DutyLogSheetReportList>("exec Sp_AuditLogSheetDetailsByVehicleorClient {0},{1},{2},{3},{4}", Params.ToArray()).ToList();
            if (lstDutyLogDets.Count == 0 || lstDutyLogDets == null)
                return PartialView("_NoData");
            TempData["lstAuditDutyLogDets"] = lstDutyLogDets;
            ViewBag.ListCount = lstDutyLogDets.Count;
            return PartialView("_AuditedLogSheetList", lstDutyLogDets);
        }

        #endregion

        #region Diesel Pending Amount Reports
        [HttpGet]
        [Authorize]
        public ActionResult DieselAmountsPendingReport()
        {
            ViewBag.VehicleID = db.tbl_vehicles.Where(a => a.Status == true).Select(a => new { a.ID, a.VehicleRegNum }).ToDictionary(a => a.ID, a => a.VehicleRegNum.ToUpper());
            return PartialView("_DieselAmountsPendingReport");
        }
        [HttpPost]
        public ActionResult DieselAmountsPendingReport(string VehicleId, FormCollection frm)
        {
            string StartDate = Convert.ToDateTime(frm["StartDate"]).ToString("yyyy-MM-dd");
            string EndDate = Convert.ToDateTime(frm["EndDate"]).ToString("yyyy-MM-dd");
            List<object> MyParam = new List<object>();
            MyParam.Add(StartDate); MyParam.Add(EndDate); MyParam.Add(VehicleId);
            var ReportList = db.ExecuteStoreQuery<GeneralClassFields>("exec Sp_DieselAmountsPendingReport {0},{1},{2}", MyParam.ToArray()).ToList();
            objCore.ConvertToUppercase<GeneralClassFields>(ReportList);
            if (ReportList == null || ReportList.Count() == 0)
                return PartialView("_NoData");
            ViewBag.ListCount = ReportList.Count;
            TempData["DieselTrackingList"] = ReportList;
            return PartialView("_DieselTrackingReport", ReportList);
        }
        #endregion

        #region Daily Duty Log Sheets Report at Dashboard
        [HttpGet]
        [Authorize]
        public ActionResult DailyLogSheetsReport()
        {
            return View();
        }
        public ActionResult DailyLogSheetsReport(FormCollection frm)
        {
            string StartDate = Convert.ToDateTime(frm["StartDate"]).ToString("yyyy-MM-dd");
            string EndDate = Convert.ToDateTime(frm["EndDate"]).ToString("yyyy-MM-dd");
            List<object> MyParam = new List<object>();
            MyParam.Add(StartDate); MyParam.Add(EndDate);
            var DailyDutyLogSheetList = db.ExecuteStoreQuery<GeneralClassFields>("exec Sp_DailyDutyLogSheetReport {0},{1}", MyParam.ToArray()).ToList();
            objCore.ConvertToUppercase<GeneralClassFields>(DailyDutyLogSheetList);
            if (DailyDutyLogSheetList == null || DailyDutyLogSheetList.Count() == 0)
                return PartialView("_NoData");
            ViewBag.ListCount = DailyDutyLogSheetList.Count;
            TempData["dailydutydetails"] = DailyDutyLogSheetList;
            return PartialView("_DailyLogSheetsReportList", DailyDutyLogSheetList);
        }

        public ActionResult DailyDutyReportExportExcel()
        {
            var dailyduty = TempData["dailydutydetails"];
            string Filename = "DailyDutyLogSheet_" + DateTime.Now.ToString("hhmmss") + ".xlsx";
            if (dailyduty == null)
            {
                MemoryStream ms1 = new MemoryStream();
                ms1.Seek(0, SeekOrigin.Begin);
                return File(ms1, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml", Filename);
            }
            MemoryStream ms = DailyDutyLogSheetExportToExcel((List<GeneralClassFields>)TempData["dailydutydetails"]);
            if (ms != null)
            {
                ms.Seek(0, SeekOrigin.Begin);
                return File(ms, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml", Filename);
            }
            return View();
        }

        public MemoryStream DailyDutyLogSheetExportToExcel(List<GeneralClassFields> list)
        {
            string sheetName = "DieselTracking";
            var wb = new XLWorkbook();
            try
            {
                var ws = wb.Worksheets.Add(sheetName);

                ws.Cell("B1").Value = " Daily Duty LogSheet(s) Report ";
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
                ws.Cell("B3").Value = "Vehicle Reg#";
                ws.Cell("C3").Value = "Trip(s)";
                ws.Cell("D3").Value = "Diesel Amount";

                int i = 4, k = list.Count;
                decimal TotAmt = 0;
                foreach (var item in list)
                {
                    ws.Cell(("B" + i)).SetValue(item.Text1.ToString());
                    ws.Cell(("C" + i)).SetValue(item.Value1);
                    ws.Cell(("D" + i)).SetValue(item.Value2);
                    ws.Cell(("C" + (k + 4))).SetValue("Total Amount");
                    TotAmt += (decimal)item.Value2;
                    ws.Cell(("D" + (k + 4))).SetValue(TotAmt);
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

        #region UserLogReports
        [HttpGet]
        [Authorize]
        public ActionResult UserLogReport()
        {
            ViewBag.Users = db.aspnet_Users.Select(a => new { a.UserName }).ToDictionary(a => a.UserName, a => a.UserName);
            return View();
        }
        [HttpPost]
        public ActionResult UserLogReport(string UserName, FormCollection frm)
        {
            string StartDate = Convert.ToDateTime(frm["StartDate"]).ToString("dd/MM/yyyy");
            string EndDate = Convert.ToDateTime(frm["EndDate"]).ToString("dd/MM/yyyy");
            List<object> Params = new List<object>();
            Params.Add(StartDate); Params.Add(EndDate);
            Params.Add(UserName);
            var UserLogList = db.ExecuteStoreQuery<UserLogReport>("exec Sp_UserLogReportBwDates {0},{1},{2}", Params.ToArray()).ToList();
            ViewBag.ListCount = UserLogList.Count;
            if (UserLogList.Count == 0 || UserLogList == null)
                return PartialView("_NoData");
            return PartialView("_UserLogReportList", UserLogList);
        }

        [HttpGet]
        public ActionResult UserLogReportDetails(string userName, string date, bool isLogSheet, string type)
        {
            string StartDate = String.Format("{0:dd/MM/yyyy}", Convert.ToDateTime(date));
            string ModelValue = string.Empty;
            List<object> Params = new List<object>();
            Params.Add(userName); Params.Add(StartDate);
            Params.Add(isLogSheet); Params.Add(type);
            ViewBag.IsLogSheet = isLogSheet;
            if (isLogSheet)
            {
                var UserLogList = db.ExecuteStoreQuery<tbl_log_sheets>("exec Sp_UserLogReportBwDatesDetails {0},{1},{2},{3}", Params.ToArray()).ToList();
                ViewBag.ListCount = UserLogList.Count;
                //if (UserLogList.Count == 0 || UserLogList == null)
                //    return PartialView("_NoData");
                ViewBag.List = UserLogList;
                return PartialView("_UserLogReportDetails");
            }
            else
            {
                var UserLogList = db.ExecuteStoreQuery<tbl_diesel_tracking>("exec Sp_UserLogReportBwDatesDetails {0},{1},{2},{3}", Params.ToArray()).ToList();
                ViewBag.ListCount = UserLogList.Count;
                //if (UserLogList.Count == 0 || UserLogList == null)
                //    return PartialView("_NoData");
                ViewBag.List = UserLogList;
                return PartialView("_UserLogReportDetails");
            }
        }

        #endregion UserLogReports

        #region TaxDeductions Report
        [HttpGet]
        public ActionResult TaxDeductionReport()
        {
            ViewBag.Years = new SelectList(core.GetAllYears(), "Value", "Text");
            return PartialView();
        }
        [HttpPost]
        public ActionResult TaxDeductionReport(FormCollection frm)
        {
            string YearsId = frm["YearsId"];
            List<object> Params = new List<object>();
            Params.Add(YearsId);
            var TaxSummary = db.ExecuteStoreQuery<GeneralClassFields>("exec Sp_GetAllDeductionYearly {0}", Params.ToArray()).ToList();
            ViewBag.ListCount = TaxSummary.Count;
            if (TaxSummary.Count == 0 || TaxSummary == null)
                return PartialView("_NoData");
            return PartialView("_TaxDeductionSummaryRep", TaxSummary);
        }
        #endregion

        #region Service Alerts
        [HttpGet]
        public ActionResult VehServiceAlerts()
        {
            var vehServiceAlerts = db.ExecuteStoreQuery<GeneralClassFields>("exec Sp_VehServiceAlerts").ToList();
            return PartialView(vehServiceAlerts);
        }
        [HttpGet]
        public ActionResult VehServicePendingAlerts()
        {
            var vehServiceAlerts = db.ExecuteStoreQuery<GeneralClassFields>("exec Sp_VehServicePendings").ToList();
            return PartialView("VehServiceAlerts", vehServiceAlerts);
        }
        #endregion

        #region Vehicle MileAge Report

        [HttpGet]
        public ActionResult VehicleMileAge()
        {
            string Mac = string.Empty;
            Mac = core.GetMacAddress().ToString();
            ViewBag.VehicleID = db.tbl_vehicles.Where(a => a.Status == true && a.Isown == true).Select(a => new { a.ID, a.VehicleRegNum }).ToDictionary(a => a.ID, a => a.VehicleRegNum.ToUpper());
            return View();
        }
        [HttpPost]
        public ActionResult VehicleMileAge(string VehicleID, FormCollection frm)
        {
            DateTime StartDate = Convert.ToDateTime(frm["StartDate"]);
            DateTime EndDate = Convert.ToDateTime(frm["EndDate"]);
            List<long?> _VehicleIds = new List<long?>();
            char[] delimiters = new char[] { ',' };
            if (!string.IsNullOrEmpty(VehicleID) && VehicleID != "null")
            {
                if (VehicleID.Contains(","))
                    _VehicleIds = VehicleID.Split(delimiters).ToList().Select(a => Generic.GetValueOrNull<long>(a)).ToList();
                else
                    _VehicleIds.Add(Convert.ToInt64(VehicleID));
            }

            List<tbl_diesel_tracking> _dieselTrack = null;
            List<GeneralClassFields> _reportList = new List<GeneralClassFields>();

            var dieselList = db.tbl_diesel_tracking.Where(a => a.Status == true).GroupBy(a => new { a.VehicleID, a.Date.Value }).Select(b => new { ID = b.Key.VehicleID, Date = b.Key.Value }).AsEnumerable().ToList();

            if (_VehicleIds.Count > 0)
                dieselList = (from m in dieselList
                              where _VehicleIds.Contains(m.ID)
                              select m).ToList();

            dieselList = (from m in dieselList
                          where m.Date >= StartDate.Date && m.Date <= EndDate.Date
                          select m).AsEnumerable().ToList();

            for (int i = 0; i < dieselList.Count(); i++)
            {
                // Skip First Take Two

                //if (_dieselTrack.Count() == 2) 
                //{
                //    _reportList.Add(new GeneralClassFields
                //    {
                //        ID = i + 1,
                //        Text1 = dieselList[i].tbl_vehicles.VehicleRegNum,
                //        Text3 = dieselList[i].tbl_vehicles.tbl_vehicle_models.VehicleModelName + "-" + dieselList[i].tbl_vehicles.tbl_vehicle_types.VehicleType,
                //        Text2 = _dieselTrack.FirstOrDefault().Date.Value.ToShortDateString() + "-" + _dieselTrack.LastOrDefault().Date.Value.ToShortDateString(),
                //        Value3 = _dieselTrack.Sum(a => a.TokenValue) ?? 0,
                //        Value2 = (_dieselTrack.LastOrDefault().OdoMeterReading ?? 0) - (_dieselTrack.FirstOrDefault().OdoMeterReading ?? 0)
                //    });
                //}
            }

            if (_reportList.Count == 0)
                return PartialView("_NoData");

            ViewBag.ListCount = _reportList.Count;
            return PartialView("_VehMailagePartial", _reportList);
        }

        #endregion

        #region LogSheet By Client

        [HttpGet]
        public ActionResult LogSheetReportByClient()
        {
            ViewBag.ClientId = new SelectList(objCore.LoadClients(), "Value", "Text");
            ViewBag.Month = new SelectList(core.GetAllMonths(), "Value", "Text", DateTime.Now.Month);
            ViewBag.YearId = new SelectList(core.GetAllYears(), "Value", "Text", DateTime.Now.Year);
            return View();
        }

        [HttpPost]
        public ActionResult LogSheetReportByClient(FormCollection frm)
        {
            try
            {
                Int64 ClientId = Convert.ToInt64(frm["ClientId"]);
                Int32 MonthId = Convert.ToInt32(frm["Month"]);
                Int32 YearId = Convert.ToInt32(frm["YearId"]);
                var _logList = objCore.GetLogSheetListByClient(ClientId, MonthId, YearId);
                if (_logList.Count() == 0)
                    return PartialView("_NoData");
                return PartialView("_GetLogSheetListByClient", _logList);
            }
            catch (Exception)
            {
                return PartialView("_NoData");
            }
        }

        #endregion

        #region Garbage
        public ActionResult Demo()
        {
            return PartialView();
        }
        [HttpPost]
        public ActionResult Demo(FormCollection data)
        {
            if (Request.Files["files"] != null)
            {
                HttpPostedFileBase file = Request.Files["files"];
            }
            return Content("<script language='javascript' type='text/javascript'> alert('Updated Successfully.');</script>");
        }

        public JsonResult GetInvoiceGraph()
        {
            List<SelectListItem> list = GetInvoice();
            return Json(new { success = true, d = list }, JsonRequestBehavior.AllowGet);
        }

        public List<SelectListItem> GetInvoice()
        {
            List<SelectListItem> list = new List<SelectListItem>();
            HomeController objHome = new HomeController();
            decimal ClientInvoice = objHome.GetMonthlyClientInvoiceAmount();
            decimal VendorInvoice = objHome.GetMonthlyVendorInvoiceAmount();
            list.Add(new SelectListItem { Text = "Client", Value = "165" });
            list.Add(new SelectListItem { Text = "Vendor", Value = "435" });
            return list;
        }

        //public GeneralClassFields GetInvioceDetails(long vehId, string startDt, string endDt, string logIds, string dieslIds, string advIds, long InvoiceId)
        //{
        //    GeneralClassFields objGen = new GeneralClassFields();
        //    List<tbl_log_sheets> _logList = objCore.GetClosedLogSheetListByVehicle(startDt, endDt, vehId, logIds);
        //    int _logQty = _logList.Count();
        //    decimal _totalAmt = 0;
        //    tbl_vehicles vehicleDet = db.tbl_vehicles.Where(a => a.ID == vehId).FirstOrDefault();
        //    foreach (var item in _logList)
        //    {
        //        if (item.tbl_clients.tbl_billing_types.BillingType == "TRIPS")
        //            item.FinalSlabRate = TotalAmountByClient((long)item.ClientID, vehId, startDt, endDt, item.ID.ToString());
        //        else if (item.tbl_clients.tbl_billing_types.BillingType == "KILO METER")
        //        {
        //            if (item.VehicleTypeID == (int)VehicleType.AC)  // 
        //                item.SlabRate = (vehicleDet.VendorAcRate == null ? 0 : (decimal)vehicleDet.VendorAcRate);
        //            else if (item.VehicleTypeID == (int)VehicleType.NONAC)
        //                item.SlabRate = (vehicleDet.VendorRate == null ? 0 : (decimal)vehicleDet.VendorRate);
        //            item.FinalSlabRate = Convert.ToDecimal(1 * item.SlabRate);
        //        }
        //        else if (item.tbl_clients.tbl_billing_types.BillingType == "PACKAGE")
        //            item.FinalSlabRate = Convert.ToDecimal(item.FinalNetAmt);
        //    }
        //    _totalAmt = _logList.Sum(a => a.FinalSlabRate) ?? 0;
        //    // Find Diesel Deductions
        //    List<tbl_diesel_tracking> _dieselList = objCore.GetClosedDieselListByVehicle(startDt, endDt, vehId, null, dieslIds);
        //    // Fin Advance Dedutions
        //    //List<tbl_advances> _advList = objCore.GetClosedAdvancesListByVehicle(startDt, endDt, vehId, null, advIds);
        //    List<tbl_advance_payments> _advPayList = db.tbl_advance_payments.Where(a => a.InvoiceId == InvoiceId).ToList();
        //    objGen.Value1 = _logQty;
        //    objGen.Value2 = _totalAmt;
        //    objGen.Value3 = _dieselList == null ? 0 : (_dieselList.Sum(a => a.TokenValue) ?? 0);
        //    objGen.Value4 = _dieselList == null ? 0 : (_dieselList.Sum(a => a.TotalAmount) ?? 0);
        //    objGen.Value5 = _advPayList == null ? 0 : (_advPayList.Sum(a => a.Amount) ?? 0);
        //    return objGen;
        //}

        #endregion
    }

    public class DutyLogSheetReportList
    {
        public long ID { get; set; }
        public DateTime LogDate { get; set; }
        public string LogEndDt { get; set; }
        //public Nullable<System.DateTime> CreatedDate { get; set; }
        public string Client { get; set; }
        public string VehicleReg { get; set; }
        public string VehicleType { get; set; }
        public string VehicleModel { get; set; }
        public string Driver { get; set; }
        public string Seater { get; set; }
        public string GuestName { get; set; }
        public string LogSheetNum { get; set; }
        public string TripType { get; set; }
        public int Pax { get; set; }
        public string ShiftTime { get; set; }
        public string ReachTime { get; set; }
        public int TravelPax { get; set; }
        public string Location { get; set; }
        public string FullLocation { get; set; }
        public int StartKM { get; set; }
        public int EndKM { get; set; }
        public int TotalKM { get; set; }
        public int Approved { get; set; }
        public string PassengerEmpID { get; set; }
        public decimal SlabRate { get; set; }
        public int FinalApprovedKm { get; set; }
        public decimal FinalSlabRate { get; set; }
        public int WorkingDays { get; set; }
        public int WorkingHrs { get; set; }
        public int TotalHrs { get; set; }
        public int ExtraKM { get; set; }
        public decimal ExtraKMRate { get; set; }
        public decimal ExtraKMAmt { get; set; }
        public int ExtraHr { get; set; }
        public decimal ExtraHrRate { get; set; }
        public decimal ExtraHrAmt { get; set; }
        public string PackModel { get; set; }
        public decimal GrossAmt { get; set; }
        public decimal TollChrg { get; set; }
        public decimal ParkingChrg { get; set; }
        public decimal FuelHike { get; set; }
        public decimal NetAmount { get; set; }
        public decimal FinalNetAmt { get; set; }
        public bool Status { get; set; }
        public string Remark { get; set; }
        public string AuditRemark { get; set; }
        public string CreatedDate { get; set; }
        public string UserName { get; set; }
        public string ModifiedDt { get; set; }
        public string ModifiedBy { get; set; }
        public string AuditDt { get; set; }
        public string AuditedBy { get; set; }
        public bool Paid { get; set; }
        public string InvoiceNum { get; set; }
        public string BookRefNum { get; set; }

    }
}
