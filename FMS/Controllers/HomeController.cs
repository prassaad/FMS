using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using FMS.Models;
using System.Web.Helpers;
using System.Drawing;
using System.Web.UI.DataVisualization.Charting;
using System.Text;
using System.Globalization;
using System.Data.Objects;
using ClosedXML.Excel;

namespace FMS.Controllers
{
    [CustomAuthorizationFilter]
    public class HomeController : Controller
    {
        #region ctors
        private FMSDBEntities db;
        core objCore = new core();
        private List<string> titleList = new List<string>();
        public HomeController()
        {
            db = new FMSDBEntities();
        } 
        #endregion

        #region Actions
        [Authorize]
        public ActionResult Index()
        {
            DateTime thisMonth = DateTime.Now;

            // For Vehicle button
            int vehCount = 0, prxyVehCount = 0;
            vehCount = db.tbl_vehicles.Where(a => a.Status == true && (a.IsProxy == null || a.IsProxy == false) && (a.Isown == null || a.Isown == false)).ToList().Count;
            ViewBag.VehicleCount = vehCount;
            prxyVehCount = db.tbl_vehicles.Where(a => a.Status == true && a.IsProxy == true).ToList().Count;
            ViewBag.ProxyVehicle = prxyVehCount;
            ViewBag.TotalVehCount = vehCount + prxyVehCount;
            //Client Button
            ViewBag.ClientCount = db.tbl_clients.Where(a => a.Status == true).ToList().Count;

            //LogSheet 
            ViewBag.LogSheetCount = db.tbl_log_sheets.Where(a => a.Status == true && a.LogDate.Value.Month == thisMonth.Month && a.LogDate.Value.Year == thisMonth.Year && (a.ClosedFlag == null || a.ClosedFlag == false)).ToList().Count;

            // Client Invoice Amount for this month
            decimal ClientInvAmt = db.tbl_client_invoice.Where(a => a.Status == true && a.Paid == false).Sum(a => a.NetTotal - (a.PaidAmount == null ? 0 : a.PaidAmount)) ?? 0;
            ViewBag.ClientInvAmt = string.Format("{0:c}", Math.Round(ClientInvAmt, 0)).Replace(".00", "");

            //Vendor Invoice Amount for this month
            decimal VenInvAmt = (from a in db.tbl_vendor_invoices
                                 where (a.Status == true && EntityFunctions.TruncateTime(a.InvoiceDate).Value.Month == thisMonth.Month
                                     && EntityFunctions.TruncateTime(a.InvoiceDate).Value.Year == thisMonth.Year)
                                 select (decimal?)a.InvoiceAmt.Value).Sum() ?? 0;
            //(decimal)db.tbl_vendor_invoices.Where(a => a.Status == true && a.InvoiceDate.Value.Month == thisMonth.Month && a.InvoiceDate.Value.Month == thisMonth.Year).Sum(a => a.InvoiceAmt);

            ViewBag.VenInvoiceAmt = string.Format("{0:c}", Math.Round(VenInvAmt, 0)).Replace(".00", ""); ;

            // Vendor 
            ViewBag.VendorCount = db.tbl_vendor_details.Where(a => a.Status == true && (a.SubVendor == null || a.SubVendor == false)).ToList().Count;

            ViewBag.DriverCount = db.tbl_drivers.Where(a => a.Status == true).ToList().Count;

            //Diesel Tracking Amount for this month
            decimal DieselTrackTokenAmt = db.tbl_diesel_tracking.Where(a => a.Status == true && a.Date.Value.Month == thisMonth.Month && a.Date.Value.Year == thisMonth.Year && !string.IsNullOrEmpty(a.DieselTokenNum)).Sum(a => a.TotalAmount) ?? 0;
            decimal DieselTrackCashAmt = db.tbl_diesel_tracking.Where(a => a.Status == true && a.Date.Value.Month == thisMonth.Month && a.Date.Value.Year == thisMonth.Year && string.IsNullOrEmpty(a.DieselTokenNum)).Sum(a => a.TotalAmount) ?? 0;
            ViewBag.DieselTrackTokenAmt = string.Format("{0:c}", Math.Round(DieselTrackTokenAmt, 0)).Replace(".00", "");
            ViewBag.DieselTrackCashAmt = string.Format("{0:c}", Math.Round(DieselTrackCashAmt, 0)).Replace(".00", "");
            // Advances Amount for this Month
            decimal AdvanceAmt = db.tbl_advances.Where(a => a.Status == true).Sum(a => a.Amount) ?? 0;
            ViewBag.AdvanceAmt = string.Format("{0:c}", Math.Round(AdvanceAmt, 0)).Replace(".00", ""); ;

            // Penalty Amount for this Month
            decimal PenaltyAmt = db.tbl_penalties.Where(a => a.Status == true && a.CreatedDate.Value.Month == thisMonth.Month && a.CreatedDate.Value.Year == thisMonth.Year).Sum(a => a.PenalityAmt) ?? 0;
            ViewBag.PenaltyAmt = string.Format("{0:c}", Math.Round(PenaltyAmt, 0)).Replace(".00", ""); ;

            // EHS Penalty Amount for this month
            decimal EhsPenaltyAmt = db.tbl_ehs_details.Where(a => a.Status == true && a.CreatedDate.Value.Month == thisMonth.Month && a.CreatedDate.Value.Year == thisMonth.Year).Sum(a => a.EHSAmt) ?? 0;
            ViewBag.EHSPenaltyAmt = string.Format("{0:c}", Math.Round(EhsPenaltyAmt, 0)).Replace(".00", ""); ;

            //Employees 
            ViewBag.EmployeeCount = db.tbl_employees.Where(a => a.Status == true).ToList().Count;

            // Booking
            ViewBag.BookingCount = (from m in db.tbl_bookings 
                                    where(EntityFunctions.TruncateTime(m.BookingDt).Value.Month == thisMonth.Month
                                    && EntityFunctions.TruncateTime(m.BookingDt).Value.Year == thisMonth.Year) select m).ToList().Count();

            return View();
        }
        [Authorize]
        public ActionResult Transactions()
        {
            return View();
        }
        [Authorize]
        public ActionResult Reports()
        {
            return View();
        }

        public ActionResult AccessRestrict()
        {
            ViewBag.Message = "ACCESS DENIED: The Requested URL could not be retrived as you are not allowed to request.";
            return View();
        }

        public ActionResult About()
        {
            return View();
        } 
        #endregion

        #region DashBoard

        // DashBoard
        public ActionResult DashBoard()
        {
            ViewBag.VehicleID = db.tbl_vehicles.Where(a => a.Status == true).Select(a => new { a.ID, a.VehicleRegNum }).ToDictionary(a => a.ID, a => a.VehicleRegNum);
            ViewBag.EmpID = db.tbl_employees.Where(a => a.Status == true).Select(a => new { a.ID, a.FirstName, a.LastName, a.EmpNo }).ToDictionary(a => a.ID, a => a.EmpNo + " - " + a.FirstName + " " + a.LastName);
            return View("DashBoard");
        }

        // DashBoard -- > Document alerts
        #region Alerts
        // DashBoard -- > Alerts
        public ActionResult Alerts()
        {
            DataTable dt;
            dt = GetAlerts();
            Session["DtAlerts"] = dt;
            return PartialView();
        }

        public ActionResult AlertsExportToExcel()
        {
            DataTable dt = (System.Data.DataTable)Session["DtAlerts"];
            string Filename = "Alerts_" + DateTime.Now.ToString("hhmmss") + ".xlsx";
            if (dt.Rows.Count == 0)
            {
                MemoryStream ms1 = new MemoryStream();
                ms1.Seek(0, SeekOrigin.Begin);
                return File(ms1, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml", Filename);
            }
            ReportsController objrep = new ReportsController();
            MemoryStream ms = objrep.ExportToExcel(dt, 1);
            if (ms != null)
            {
                ms.Seek(0, SeekOrigin.Begin);
                return File(ms, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml", Filename);
            }
            return View();
        } 
        #endregion

        // DashBoard -- > Driver Utilization
        #region Driver Utilization
        [HttpGet]
        public ActionResult DriverUtilizationReport()
        {
            ViewBag.DriverID = new core().LoadDrivers();
            return PartialView("_DriverUtilizationReport");
        }
        [HttpPost]
        public ActionResult DriverUtilizationReport(FormCollection frm)
        {
            long DriverID = (frm["DriverID"] == null || frm["DriverID"] == "") ? 0 : Convert.ToInt64(frm["DriverID"]);
            string StartDate = Convert.ToDateTime(frm["StartDate"]).ToString("yyyy-MM-dd");
            string EndDate = Convert.ToDateTime(frm["EndDate"]).ToString("yyyy-MM-dd");
            List<object> MyParam = new List<object>();
            MyParam.Add(DriverID); MyParam.Add(StartDate); MyParam.Add(EndDate);
            var DriverUtiliReportList = db.ExecuteStoreQuery<GeneralClassFields>("exec Sp_Driverutilizationreport {0},{1},{2}", MyParam.ToArray()).ToList();
            objCore.ConvertToUppercase<GeneralClassFields>(DriverUtiliReportList);
            if (DriverUtiliReportList == null || DriverUtiliReportList.Count() == 0)
                return PartialView("_NoData");
            ViewBag.ListCount = DriverUtiliReportList.Count;
            return PartialView("_DriverUtilizationReportList", DriverUtiliReportList);
        }  
        #endregion

        // Dashboard -- > Client wise trip report 
        #region Client Wise Trip
        [HttpGet]
        public ActionResult ClientWiseTripView(String fromDate, String toDate)
        {
            if (fromDate == null)
            {
                DateTime TodayDate = DateTime.Now.Date;
                string month = string.Empty;
                var mm = TodayDate.Month;
                var yy = TodayDate.Year;
                month = mm.ToString();
                if (mm < 10) { month = "0" + Convert.ToString(mm); }
                ViewBag.fromDate = "01" + "-" + month + "-" + yy;
                ViewBag.fromlogDateStatus = "1";
            }
            else
            {
                ViewBag.fromlogDateStatus = "0";
                DateTime dt = Convert.ToDateTime(fromDate);
                string s = dt.ToString("dd-MM-yyyy");
                ViewBag.fromDate = s;
            }
            if (toDate == null)
            {
                ViewBag.toDate = DateTime.Now.Date.ToShortDateString();
                ViewBag.tologDateStatus = "1";
            }
            else
            {
                ViewBag.tologDateStatus = "0";
                DateTime dt = Convert.ToDateTime(toDate);
                string s = dt.ToString("dd-MM-yyyy");
                ViewBag.toDate = s;
            }
            return PartialView("ClientWiseTripView");

        }
        [HttpGet]
        public ActionResult CW_TripsByClient(long ClientId, string fromDate, string toDate)
        {
            List<GeneralClassFields> lsttripDet = new List<GeneralClassFields>();
            lsttripDet = GetTripsByClient(ClientId, fromDate, toDate);
            objCore.ConvertToUppercase<GeneralClassFields>(lsttripDet);
            return PartialView("_CW_TripsByClient", lsttripDet);
        }
        [HttpGet]
        public ActionResult CW_TripsByLogs(long ClientId, string fromDate, string toDate)
        {
            List<GeneralClassFields> lstlogsDet = new List<GeneralClassFields>();
            string FromDate = fromDate == null ? DateTime.Now.Date.ToString("yyyy-MM-dd") : Convert.ToDateTime(fromDate).ToString("yyyy-MM-dd");
            string ToDate = toDate == null ? DateTime.Now.Date.ToString("yyyy-MM-dd") : Convert.ToDateTime(toDate).ToString("yyyy-MM-dd");
            ViewBag.fromDate = FromDate;
            ViewBag.toDate = ToDate;
            string query = "select  (CASE WHEN TripeType ='PICKUP' THEN 'LOGIN'   ELSE  'LOGOUT'  END) AS Text1,  COUNT(TripeType) AS Value1,ClientID as ID from tbl_log_sheets where Status=1 AND CAST(FLOOR(CAST(LogDate AS FLOAT)) AS DATETIME) between'" + FromDate + "' and '" + ToDate + "' AND ClientID in(" + ClientId + ") group by TripeType,ClientID ORDER BY TripeType DESC ";
            lstlogsDet = db.ExecuteStoreQuery<GeneralClassFields>(query).ToList();
            if (lstlogsDet.Count == 0)
                lstlogsDet.Add(new GeneralClassFields { ID = ClientId, Text1 = "No Logins/Logouts ", Value1 = 0 });
            objCore.ConvertToUppercase<GeneralClassFields>(lstlogsDet);
            return PartialView("_CW_TripsByLogs", lstlogsDet);
        }
        [HttpGet]
        public ActionResult CW_TripsByTime(long ClientId, string Logs, string fromDate, string toDate)
        {
            List<GeneralClassFields> lstlogtimeDet = new List<GeneralClassFields>();
            string FromDate = fromDate == null ? DateTime.Now.Date.ToString("yyyy-MM-dd") : Convert.ToDateTime(fromDate).ToString("yyyy-MM-dd");
            string ToDate = toDate == null ? DateTime.Now.Date.ToString("yyyy-MM-dd") : Convert.ToDateTime(toDate).ToString("yyyy-MM-dd");
            ViewBag.fromDate = FromDate;
            ViewBag.toDate = ToDate;
            string TripType = Logs == "LOGOUT" ? "DROP" : "PICKUP";
            string query = "SELECT ShiftTime AS Text1,COUNT(*) AS Value1,TripeType as Text2,ClientID AS ID FROM tbl_log_sheets WHERE    Status = 1  AND CAST (FLOOR(CAST (LogDate AS FLOAT)) AS DATETIME)  between'" + FromDate + "' and '" + ToDate + "' AND ClientID IN (" + ClientId + ") AND TripeType = '" + TripType + "' GROUP BY ShiftTime,TripeType,ClientID ORDER BY ShiftTime";
            lstlogtimeDet = db.ExecuteStoreQuery<GeneralClassFields>(query).ToList();
            if (lstlogtimeDet.Count == 0)
                lstlogtimeDet.Add(new GeneralClassFields { ID = ClientId, Text1 = "No Logins/Logouts ", Value1 = 0 });
            objCore.ConvertToUppercase<GeneralClassFields>(lstlogtimeDet);
            return PartialView("_CW_TripsByTime", lstlogtimeDet);
        }
        public List<GeneralClassFields> GetTripsByClient(long ClientId, string fromDate, string toDate)
        {
            List<GeneralClassFields> lstlogsDet = new List<GeneralClassFields>();
            string FromDate = fromDate == null ? DateTime.Now.Date.ToString("yyyy-MM-dd") : Convert.ToDateTime(fromDate).ToString("yyyy-MM-dd");
            string ToDate = toDate == null ? DateTime.Now.Date.ToString("yyyy-MM-dd") : Convert.ToDateTime(toDate).ToString("yyyy-MM-dd");
            ViewBag.fromDate = FromDate;
            ViewBag.toDate = ToDate;
            string query = "select ClientID AS ID,COUNT(TripeType) AS Value1 from tbl_log_sheets where Status=1 AND CAST(FLOOR(CAST(LogDate AS FLOAT)) AS DATETIME) between'" + FromDate + "' and '" + ToDate + "'";
            if (ClientId != 0) { query = query + " AND ClientID in(" + ClientId + ")"; }
            query = query + "  group by ClientID ";
            lstlogsDet = db.ExecuteStoreQuery<GeneralClassFields>(query).ToList();
            if (lstlogsDet.Count == 0)
                lstlogsDet.Add(new GeneralClassFields { ID = ClientId, Value1 = 0 });
            objCore.ConvertToUppercase<GeneralClassFields>(lstlogsDet);
            return lstlogsDet;
        }

        // Modal PopUp in Detail view 
        public ActionResult CW_TripsByTimeList(long ClientId, string Logs, string shiftTime, string fromDate, string toDate)
        {
            string FromDate = fromDate == null ? DateTime.Now.Date.ToString("yyyy-MM-dd") : Convert.ToDateTime(fromDate).ToString("yyyy-MM-dd");
            string ToDate = toDate == null ? DateTime.Now.Date.ToString("yyyy-MM-dd") : Convert.ToDateTime(toDate).ToString("yyyy-MM-dd");
            string query = "SELECT ls.ID,c.CompanyName as Client,v.VehicleRegNum as VehicleReg,vt.VehicleType as VehicleType,vm.VehicleModelName as VehicleModel,d.FirstName + ' ' + d.LastName as Driver,ls.LogDate as LogDate,ls.LogSheetNum as LogSheetNum,ls.TripeType as TripType," +
                           "ISNULL(ls.Pax,0) as Pax,ISNULL(ls.ShiftTime,'') as ShiftTime,ISNULL( ls.Location,'') as Location,ISNULL(ls.PassengerEmpID,'') as PassengerEmpID	  " +
                           "FROM tbl_log_sheets ls inner join tbl_clients c on ls.ClientID = c.ID inner join tbl_vehicles v on ls.VehicleID = v.ID  inner join tbl_vehicle_types vt on ls.VehicleTypeID = vt.ID inner join tbl_vehicle_models vm on ls.VehicleModelID = vm.ID " +
                           "inner join tbl_drivers d on ls.DriverID = d.ID WHERE ls.ClientID = " + ClientId + " and  CAST (FLOOR(CAST (ls.LogDate AS FLOAT)) AS DATETIME)  " +
                           "BETWEEN '" + FromDate + "' and '" + ToDate + "' AND ls.Status = 1 AND TripeType = '" + Logs + "' AND ShiftTime = '" + shiftTime + "' ORDER BY ls.LogDate";
            var list = db.ExecuteStoreQuery<DutyLogSheetReportList>(query).ToList();
            if (list.Count == 0 || list == null)
                return PartialView("_NoData");
            ViewBag.ListCount = list.Count;
            ViewBag.FromDate = Convert.ToDateTime(fromDate).ToShortDateString(); ViewBag.ToDate = Convert.ToDateTime(toDate).ToShortDateString();
            ViewBag.ShiftTime = shiftTime; ViewBag.Logs = Logs;
            return PartialView("_CW_TripsByTimeList", list);
        }

        #endregion

        // Dashboard -- > Monthly client wise Profit and loss
        #region Monthly Client wise profit and loss Report
        public ActionResult GetMonthlyClientWiseProfitNLoss()
        {
            ViewBag.Client = db.tbl_clients.Where(a => a.Status == true).Select(a => new { a.ID, a.CompanyName }).ToDictionary(a => a.ID, a => a.CompanyName.ToUpper());
            return PartialView("_ClientWiseProfitNLoss");
        }
        [HttpPost]
        public ActionResult GetMonthlyClientWiseProfitNLoss(FormCollection frm)
        {
            string ClientID = (frm["ClientID"] == null || frm["ClientID"] == "") ? "null" : frm["ClientID"].ToString();
            string StartDate = Convert.ToDateTime(frm["StartDate"]).ToString("yyyy-MM-dd");
            string EndDate = Convert.ToDateTime(frm["EndDate"]).ToString("yyyy-MM-dd");
            List<object> MyParam = new List<object>();
            MyParam.Add(ClientID); MyParam.Add(StartDate); MyParam.Add(EndDate);
            var MonthlyClientWiseProfitNLossList = db.ExecuteStoreQuery<GeneralClassFields>("exec Sp_MonthlyClientWiseProfitNLoss {0},{1},{2}", MyParam.ToArray()).ToList();
            objCore.ConvertToUppercase<GeneralClassFields>(MonthlyClientWiseProfitNLossList);
            if (MonthlyClientWiseProfitNLossList == null || MonthlyClientWiseProfitNLossList.Count() == 0)
                return PartialView("_NoData");
            ViewBag.ListCount = MonthlyClientWiseProfitNLossList.Count;
            return PartialView("_ClientWiseProfitNLossList", MonthlyClientWiseProfitNLossList);
        } 
        #endregion
 
        // DashBoard -- > InActive Duty logsheet

        public ActionResult InActiveDutyLogSheet()
        {
            var InActiveDutyLogList = db.ExecuteStoreQuery<tbl_vehicles>("exec Sp_GetInActiveDutyLogSheet").ToList();
            ViewBag.ListCount = InActiveDutyLogList.Count;
            TempData["InActiveList"] = InActiveDutyLogList;
            return PartialView("_InActiveDutyLogSheet", InActiveDutyLogList);
        }

        public ActionResult InActiveDutyExportToExcel()
        {
            var dailyduty = TempData["InActiveList"];
            string Filename = "DailyDutyLogSheet_" + DateTime.Now.ToString("hhmmss") + ".xlsx";
            if (dailyduty == null)
            {
                MemoryStream ms1 = new MemoryStream();
                ms1.Seek(0, SeekOrigin.Begin);
                return File(ms1, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml", Filename);
            }
            MemoryStream ms = DailyDutyLogSheetExportToExcel((List<tbl_vehicles>)TempData["InActiveList"]);
            if (ms != null)
            {
                ms.Seek(0, SeekOrigin.Begin);
                return File(ms, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml", Filename);
            }
            return View();
        }

        public MemoryStream DailyDutyLogSheetExportToExcel(List<tbl_vehicles> list)
        {
            string sheetName = "Sheet1";
            var wb = new XLWorkbook();
            try
            {
                var ws = wb.Worksheets.Add(sheetName);

                ws.Cell("B1").Value = " In Active LogSheet(s) Report ";
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
                ws.Cell("D3").Value = "Model";
                ws.Cell("E3").Value = "Type";
                ws.Cell("F3").Value = "Seater";
                ws.Cell("G3").Value = "IsProxy";

                core objCore = new core();      

                int i = 4, k = list.Count;
                foreach (var item in list)
                {
                    ws.Cell(("B" + i)).SetValue(item.VehicleRegNum);
                    ws.Cell(("C" + i)).SetValue(objCore.GetDriverByVehicle((long)item.ID));
                    ws.Cell(("D" + i)).SetValue(objCore.GetVehicleModel((long)item.VehicleModelID));
                    ws.Cell(("E" + i)).SetValue(objCore.GetVehicleType((long)item.VehicleTypeID));
                    ws.Cell(("F" + i)).SetValue(objCore.GetSeater((long)item.SeaterId));
                    ws.Cell(("G" + i)).SetValue((item.IsProxy == null ? "No" : (item.IsProxy == true ? "Yes" : "No")));
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

        public ActionResult ViewReport(string linkid)
        {
            ViewBag.linkId = linkid;
            return View();
        }

        #region Dashboard Graph
        public ActionResult Chart()
        {
            var chart = buildChart();
            StringBuilder result = new StringBuilder();
            result.Append(getChartImage(chart, titleList));
            result.Append(chart.GetHtmlImageMap("ImageMap"));
            return Content(result.ToString());
        }
        private System.Web.UI.DataVisualization.Charting.Chart buildChart()
        {
            // Build Chart
            var chart = new System.Web.UI.DataVisualization.Charting.Chart();

            chart.Width = 415;
            chart.Height = 336;

            decimal ClientInvAmt = GetMonthlyClientInvoiceAmount();
            decimal VenInvAmt = GetMonthlyVendorInvoiceAmount();
            titleList = new List<string>();

            if (ClientInvAmt != 0)
                titleList.Add("Client_" + ClientInvAmt);
            if (VenInvAmt != 0)
                titleList.Add("Vendor_" + VenInvAmt);

            // Create chart here
            chart.Titles.Add(CreateTitle());
            chart.Legends.Add(CreateLegend(titleList, chart));
            chart.ChartAreas.Add(CreateChartArea());
            chart.Series.Add(CreateSeries(titleList));
            chart.Palette = ChartColorPalette.BrightPastel;
            chart.AntiAliasing = AntiAliasingStyles.All;
            chart.TextAntiAliasingQuality = TextAntiAliasingQuality.High;

            return chart;
        }

        private string getChartImage(System.Web.UI.DataVisualization.Charting.Chart chart, List<string> titleList)
        {
            if (titleList.Count != 0)
            {
                using (var stream = new MemoryStream())
                {
                    string img = "<img src='data:image/png;base64,{0}' alt='' usemap='#ImageMap' class='maphighlight'>";
                    chart.SaveImage(stream, ChartImageFormat.Png);
                    string encoded = Convert.ToBase64String(stream.ToArray());
                    return String.Format(img, encoded);
                }
            }
            else
            {
                var dir = Server.MapPath("/Content/images");
                var path = Path.Combine(dir, "NoData.png");
                using (Image image = Image.FromFile(path))
                {
                    using (MemoryStream m = new MemoryStream())
                    {
                        string img = "<img src='data:image/png;base64,{0}' alt='' usemap='#ImageMap'>";
                        image.Save(m, image.RawFormat);
                        chart.SaveImage(m, ChartImageFormat.Png);
                        byte[] imageBytes = m.ToArray();
                        string base64String = Convert.ToBase64String(imageBytes);
                        return String.Format(img, base64String);
                    }
                }
            }
        }

        private Title CreateTitle()
        {
            Title title = new Title("Client Vs Vendor Invoices", Docking.Top, new System.Drawing.Font("Trebuchet MS", 12, System.Drawing.FontStyle.Bold), System.Drawing.Color.FromArgb(26, 59, 105));
            return title;
        }

        private Legend CreateLegend(List<string> titleList, System.Web.UI.DataVisualization.Charting.Chart Chart2)
        {
            Legend legend = new Legend();
            foreach (string value in titleList)
            {
                string[] split = value.Split('_');
                string LegendText = split[0].ToString();
                Chart2.Legends.Add(LegendText);
                Chart2.Legends[LegendText].HeaderSeparator = LegendSeparatorStyle.Line;
                Chart2.Legends[LegendText].HeaderSeparatorColor = Color.Gray;

                // Add Color column      
                LegendCellColumn firstColumn = new LegendCellColumn();
                firstColumn.ColumnType = LegendCellColumnType.SeriesSymbol;
                // firstColumn.HeaderText = "Color";
                firstColumn.HeaderBackColor = Color.WhiteSmoke;
                firstColumn.Font = new Font("Verdana", 7.00f, FontStyle.Bold);

                if (LegendText == "Vendor")
                    firstColumn.Url = "/VendorInvoice/Index";
                else
                    firstColumn.Url = "/ClientInvoice/Index";
                Chart2.Legends[LegendText].CellColumns.Add(firstColumn);

                // Add Legend Text column      
                LegendCellColumn secondColumn = new LegendCellColumn();
                secondColumn.ColumnType = LegendCellColumnType.Text;
                secondColumn.HeaderText = "Name";
                secondColumn.HeaderFont = new Font("Verdana", 7.00f, FontStyle.Bold);
                secondColumn.Text = "#LEGENDTEXT";
                secondColumn.HeaderBackColor = Color.WhiteSmoke;
                secondColumn.Font = new Font("Verdana", 6.00f, FontStyle.Bold);
                Chart2.Legends[LegendText].CellColumns.Add(secondColumn);

                // Add AVG cell column      
                LegendCellColumn avgColumn = new LegendCellColumn();
                avgColumn.Text = "#VALY";
                avgColumn.HeaderText = "Value";
                avgColumn.HeaderFont = new Font("Verdana", 7.00f, FontStyle.Bold);
                avgColumn.Name = "ValColumn";
                avgColumn.HeaderBackColor = Color.WhiteSmoke;
                avgColumn.Font = new Font("Verdana", 6.00f, FontStyle.Bold);
                Chart2.Legends[LegendText].CellColumns.Add(avgColumn);

                // Add Total cell column      
                LegendCellColumn percentColumn = new LegendCellColumn();
                percentColumn.Text = "#PERCENT";
                percentColumn.HeaderText = "%";
                percentColumn.HeaderFont = new Font("Verdana", 7.00f, FontStyle.Bold);
                percentColumn.Name = "PercentColumn";
                percentColumn.HeaderBackColor = Color.WhiteSmoke;
                percentColumn.Font = new Font("Verdana", 6.00f, FontStyle.Bold);
                Chart2.Legends[LegendText].CellColumns.Add(percentColumn);
                //  Chart2.Legends[LegendText].Docking = Docking.Bottom;

            }

            return legend;
        }

        private ChartArea CreateChartArea()
        {
            ChartArea area = new ChartArea("Result Chart");
            area.Area3DStyle.Enable3D = false;
            area.AxisX.Interval = 1;
            area.AxisX.LabelStyle.Font = new Font("Verdana", 8.25f, FontStyle.Underline);//Why does it recognize the style but not the font!!!???
            return area;
        }

        public Series CreateSeries(List<string> titleList)
        {
            Series seriesDetail = new Series();
            seriesDetail.Name = "Result Chart";
            seriesDetail.IsValueShownAsLabel = false;
            seriesDetail.Color = Color.FromArgb(198, 99, 99);
            seriesDetail.ChartType = SeriesChartType.Pie;

            foreach (string value in titleList)
            {
                string[] split = value.Split('_');
                string Name = split[0].ToString();
                double Amount = Convert.ToDouble(split[1]);
                var p = seriesDetail.Points.Add(Amount);
                //p.Label = Name + " (" + Amount.ToString() + ")";
                p.Label = "#PERCENT";
                p.LegendText = Name;
                p.MapAreaAttributes = "onclick=\"GraphNavigation(" + Amount + ",'" + Name + "')\"";
                p.Name = "pie_" + Name;
                // p.MapAreaAttributes = "onclick=\"myfunction('" + Amount + "')\" onmouseover=\"DisplayTooltip('" + Amount + "');\"";
                p.ToolTip = Name + " (" + Amount.ToString() + ")";
                p.LabelForeColor = Color.White;
                p.Font = new Font("Trebuchet MS", 8.25f, FontStyle.Bold);
            }
            seriesDetail.ChartArea = "Result Chart";

            return seriesDetail;
        }

        #endregion

        // Quick Links Count 
        #region Quick Links Count Methods

        public int GetClientTripCount()
        {
            try
            {
                string TodayDate = DateTime.Now.ToString("dd/MM/yyyy");
                List<GeneralClassFields> list = GetTripsByClient(0, TodayDate, TodayDate);
                return list.Sum(a => a.Value1);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public int GetTodayExpiredAlerts()
        {
            try
            {
                var AlertsCnt = db.ExecuteStoreQuery<GeneralClassFields>("exec Sp_GetTodayExpiredAlerts").FirstOrDefault();
                return AlertsCnt.Value1;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public int GetDriverUtilizationCount()
        {
            try
            {
                string TodayDate = DateTime.Now.ToString("yyyy-MM-dd");
                List<object> MyParam = new List<object>();
                MyParam.Add(0); MyParam.Add(TodayDate); MyParam.Add(TodayDate);
                var DriverUtiliReportList = db.ExecuteStoreQuery<GeneralClassFields>("exec Sp_Driverutilizationreport {0},{1},{2}", MyParam.ToArray()).ToList();
                return DriverUtiliReportList.Count();
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public int GetTodayDieselPendingListCount()
        {
            try
            {
                string TodayDate = DateTime.Now.ToString("yyyy-MM-dd");
                // Input Parameters for Stored Procedures 
                List<object> MyParam = new List<object>();
                MyParam.Add(TodayDate); MyParam.Add(TodayDate);
                MyParam.Add("null");
                var DieselTrackingReportList = db.ExecuteStoreQuery<GeneralClassFields>("exec Sp_DieselAmountsPendingReport {0},{1},{2}", MyParam.ToArray()).ToList();
                return DieselTrackingReportList.Count;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public int GetTodayClientWiseProfitORLoss()
        {
            try
            {
                string TodayDate = DateTime.Now.ToString("yyyy-MM-dd");
                // Input Parameters for Stored Procedures 
                List<object> MyParam = new List<object>();
                MyParam.Add("null");
                MyParam.Add(TodayDate);
                MyParam.Add(TodayDate);
                var TodayClientWiseProfitNLossList = db.ExecuteStoreQuery<GeneralClassFields>("exec Sp_MonthlyClientWiseProfitNLoss {0},{1},{2}", MyParam.ToArray()).ToList();
                return TodayClientWiseProfitNLossList.Count;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public int GetInActiveLogSheetListCount()
        {
            try
            {
                var InActiveDutyLogList = db.ExecuteStoreQuery<tbl_vehicles>("exec Sp_GetInActiveDutyLogSheet").ToList();
                return InActiveDutyLogList.Count;
            }
            catch
            {
                return 0;
            }
        } 
        #endregion

        #endregion

        #region CustomMethods

        public decimal GetMonthlyClientInvoiceAmount()
        {
            DateTime thisMonth = DateTime.Now;
            decimal clientInvoiceAmt = 0;
            try
            {
                string MonthName = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(thisMonth.Month);
                string MonthYear = MonthName.ToUpper() + " " + thisMonth.Year.ToString();
                //clientInvoiceAmt = db.tbl_client_invoice.Where(a => a.InvoiceDate.Value.Month == thisMonth.Month && a.InvoiceDate.Value.Year == thisMonth.Year && a.Status == true).Sum(a => a.NetTotal).Value;
                clientInvoiceAmt = (from m in db.tbl_client_invoice
                                    where m.MonthYear == MonthYear
                                    && m.Status == true
                                    select (decimal)m.InvoiceAmount).Sum();
            }
            catch (Exception)
            {
                return clientInvoiceAmt;
            }
            return clientInvoiceAmt;
        }
        public decimal GetMonthlyVendorInvoiceAmount()
        {
            DateTime thisMonth = DateTime.Now;
            decimal venInvoiceAmt = 0;
            try
            {
                //db.tbl_vendor_invoices.Where(a => a.InvoiceDate.Value.Month == thisMonth.Month && a.InvoiceDate.Value.Year == thisMonth.Year && a.Status == true).Sum(a => a.InvoiceAmt).Value;
                string query = "select isnull(SUM(InvoiceAmt),0) as Value2 from tbl_vendor_invoices where Status = 1 and DATEPART(MM, CONVERT(datetime,Substring(FilterDates, 1,Charindex(',', FilterDates)-1))) =  " + thisMonth.Month + " and   DATEPART(YYYY, CONVERT(datetime,Substring(FilterDates, 1,Charindex(',', FilterDates)-1))) =" + thisMonth.Year;
                var VendorInv = db.ExecuteStoreQuery<GeneralClassFields>(query).FirstOrDefault();
                venInvoiceAmt = VendorInv.Value2;
            }
            catch (Exception)
            {
                return venInvoiceAmt;
            }
            return venInvoiceAmt;
        }
        public DateTime GetDateByString(string s)
        {
            return Convert.ToDateTime(s.Split(',')[0]);
        }
        // load all Clients
        public IDictionary<string, long> LoadClients()
        {
            IDictionary<string, long> _Clients = (IDictionary<string, long>)(from m in db.tbl_log_sheets join c in db.tbl_clients on m.ClientID equals c.ID select c).Select(a => new { a.CompanyName, a.ID }).OrderByDescending(a => a.CompanyName).Distinct().ToDictionary(s => s.CompanyName, s => s.ID);
            System.Collections.Generic.SortedDictionary<string, long> sortedList = null;
            if (_Clients != null)
            {
                sortedList = new SortedDictionary<string, long>(_Clients);
            }
            return sortedList;
        }
        public ActionResult ViewDocument(string foldername,string filename) 
        {
            string path = string.Empty;
            string pdfPath = string.Empty;
            string FolderName = string.Empty;
            string FullText = string.Empty;

            Response.Buffer = true;
            Response.ExpiresAbsolute = DateTime.Now.AddDays(-1d);
            Response.Expires = -1500;
            Response.CacheControl = "no-cache";
            Response.Cache.SetNoStore();
            if (!User.Identity.IsAuthenticated)
                //Your Session Expired. Please Login Again
                return RedirectToAction("LogOn", "Account");

            // PDF Viewer
            pdfPath = filename;// filename
            ViewBag.SwfFile = execcmd(Server.MapPath("~/Content/Documents/"+ foldername +"/") + pdfPath);
            ViewBag.IsPrint = "false";
            return PartialView("_ViewDocument");
        }
        string execcmd(string filename)
        {

            string swflocation = Server.MapPath("~/SWFConverter/");
            FileInfo fo = new FileInfo(filename);
            string strFile = "," + fo.Extension.ToLower().Replace('.', ' ').Trim() + ",";


            string destFile = Server.MapPath("~/FlashFiles/");
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.WorkingDirectory = "";
            p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.FileName = swflocation + "swf\\pdf2swf.exe";
            p.StartInfo.Arguments = filename + " -o " + destFile + getPlainFileName(Path.GetFileNameWithoutExtension(filename)) + ".swf -f -T 9 -t -O1";//-s storeallcharacters 

            p.Start();
            p.WaitForExit();

            //Session["FlashFile"] = Server.MapPath("~/FlashFiles/") + getPlainFileName(Path.GetFileNameWithoutExtension(filename)) + ".swf";
            return getPlainFileName(Path.GetFileNameWithoutExtension(filename)) + ".swf";


        }
        private string getPlainFileName(string FileName)
        {
            FileName = FileName.Replace("%20", "");
            FileName = FileName.Replace("%", "");
            FileName = FileName.Replace("@", "");
            FileName = FileName.Replace("+", "");
            FileName = FileName.Replace("*", "");
            FileName = FileName.Replace("/", "");
            FileName = FileName.Replace("\\", "");

            FileName = FileName.Replace(":", "");
            FileName = FileName.Replace("?", "");
            FileName = FileName.Replace("=", "");
            FileName = FileName.Replace("\"", "");
            FileName = FileName.Replace("\\", "");
            FileName = FileName.Replace(">", "");
            FileName = FileName.Replace("<", "");

            FileName = FileName.Replace("~", "");
            FileName = FileName.Replace("`", "");
            FileName = FileName.Replace("$", "");
            FileName = FileName.Replace("^", "");
            FileName = FileName.Replace("!", "");
            return FileName;
        }

        public string GetConsiderDocList()
        {
            string[] Arry = (from m in db.tbl_documents
                             where m.IsConsider == true
                             && m.Status == true
                             select m.DocumentType + "VALIDITY").ToArray();
            return string.Join(",", Arry);
            //CultureInfo.CurrentCulture.TextInfo.ToTitleCase(m.DocumentType)
        }

        public DataTable GetAlerts()
        {
            DataTable dt;
            using (SqlConnection sqlcon = new SqlConnection(ConfigurationManager.ConnectionStrings["ApplicationServices"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("Sp_DashboardAlerts", sqlcon))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    using (System.Data.SqlClient.SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        dt = new DataTable();
                        da.Fill(dt);
                    }
                }
            }
            return dt;
        }
        #endregion

        public ActionResult Configurations()
        {
            return View();
        }
    }

}
