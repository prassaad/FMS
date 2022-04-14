using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;
using ClosedXML.Excel;
using System.IO;
using System.Data;
using System.Data.SqlClient;

namespace FMS.Controllers
{
    [CustomAuthorizationFilter]
    public class AuditController : Controller
    {
        #region ctors
        private FMSDBEntities db;
        private core objCore = new core();
        // GET: /Audit/
        public AuditController()
        {
            db = new FMSDBEntities();
        } 
        #endregion

        public ActionResult Index()
        {
            return View();
        }

        #region Audit MIS Report
        [Authorize]
        [HttpGet]
        public ActionResult AuditMISReport()
        {
            ViewBag.ClientID = db.tbl_clients.Where(a => a.Status == true).Select(a => new { a.ID, a.CompanyName }).ToDictionary(a => a.ID, a => a.CompanyName.ToUpper());
            return View("MISReport");
        }

        public ActionResult AuditMISReport(string ClientID, FormCollection frm)
        {
            string StartDate = DateTime.Parse(frm["StartDate"]).ToString("yyyy-MM-dd");
            string EndDate = DateTime.Parse(frm["EndDate"]).ToString("yyyy-MM-dd");
            string Selection = frm["Selection"] == null ? "" : frm["Selection"];
            int ReportType = frm["ReportType"] == null ? 0 : Convert.ToInt32(frm["ReportType"]);

            List<object> Params = new List<object>();
            Params.Add(ClientID);
            Params.Add(StartDate); Params.Add(EndDate); Params.Add(ReportType);

            if (Selection.ToUpper() == "LOGSHEET")
            {
                var lstDutyLogDets = db.ExecuteStoreQuery<DutyLogSheetReportList>("exec Sp_GetLogSheetList {0},{1},{2},{3}", Params.ToArray()).ToList();
                if (lstDutyLogDets.Count == 0 || lstDutyLogDets == null)
                    return PartialView("_NoData");
                ViewBag.ListCount = lstDutyLogDets.Count;
                TempData["LogSheetList"] = lstDutyLogDets;
                return PartialView("_LogSheetList", lstDutyLogDets);
            }
            else if (Selection.ToUpper() == "DIESEL")
            {
                var lstDieselDets = db.ExecuteStoreQuery<GeneralClassFields>("exec Sp_GetDieselTrackingList {0},{1},{2},{3}", Params.ToArray()).ToList();
                if (lstDieselDets.Count == 0 || lstDieselDets == null)
                    return PartialView("_NoData");
                ViewBag.ListCount = lstDieselDets.Count;
                TempData["DieselList"] = lstDieselDets;
                return PartialView("_DieselList", lstDieselDets);
            }

            return PartialView("_NoData");
        }

        public ActionResult ExportExcel(int selectType)
        {
            if (selectType == 1) // LogSheet Excel
            {
                var DutyLogDets = (List<DutyLogSheetReportList>)TempData["LogSheetList"];
                string Filename = "DutyLogSheet_" + DateTime.Now.ToString("hhmmss") + ".xlsx";
                if (DutyLogDets == null)
                {
                    MemoryStream ms1 = new MemoryStream();
                    ms1.Seek(0, SeekOrigin.Begin);
                    return File(ms1, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml", Filename);
                }
                DataTable dt = objCore.ConvertToDataTable(DutyLogDets);
                MemoryStream ms = ExportToExcel(dt, 1);
                if (ms != null)
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    return File(ms, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml", Filename);
                }
            }
            else if (selectType == 2)  // Diesel Excel
            {
                var DieselDets = (List<GeneralClassFields>)TempData["DieselList"];
                string Filename = "DieselList_" + DateTime.Now.ToString("hhmmss") + ".xlsx";
                if (DieselDets == null)
                {
                    MemoryStream ms1 = new MemoryStream();
                    ms1.Seek(0, SeekOrigin.Begin);
                    return File(ms1, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml", Filename);
                }
                ReportsController objRep = new ReportsController();
                MemoryStream ms = objRep.DieselTrackingExportToExcel(DieselDets);
                if (ms != null)
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    return File(ms, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml", Filename);
                }
            }

            return View();
        }

        public MemoryStream ExportToExcel(DataTable table, int reportType)
        {

            string sheetName = "Sheet1";
            var wb = new XLWorkbook();
            try
            {
                var ws = wb.Worksheets.Add(sheetName);
                string reportHeader = string.Empty;
                if (reportType == 1)
                    reportHeader = " Duty LogSheet Report";
                else if (reportType == 2)
                    reportHeader = " Diesel Report";

                ws.Cell("B1").Value = reportHeader;
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
        #endregion

    }
}
