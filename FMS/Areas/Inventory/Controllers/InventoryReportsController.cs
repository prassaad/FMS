using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Globalization;
using FMS.Models;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Web.UI.HtmlControls;
using System.IO;
using ClosedXML.Excel;
namespace FMS.Areas.Inventory.Controllers
{
     [CustomAuthorizationFilter]
    public class InventoryReportsController : Controller
    {
        CultureInfo info = CultureInfo.GetCultureInfo("hi-IN");
        FMSDBEntities db = new FMSDBEntities();
        core objCore = new core();
        private static int rep_stock_pos_flag = 0;
        private static List<System.Nullable<decimal>> cust_id_list = new List<System.Nullable<decimal>>();
        private static List<System.Nullable<int>> prod_id_list = new List<System.Nullable<int>>();
        private static string day_date = "";
        private static string to_date = "";
        #region Address
        //public ActionResult GetAddress()
        //{
        //    var address = db.OrganizationMasters.ToList();
        //    return PartialView("~/Accounts/Views/Shared/Address.cshtml", address);
        //}
        #endregion
        public ActionResult Index()
        {
            return View();
        }
        #region Reports
        public ActionResult GetReport(FormCollection form)
        {

            cust_id_list.Clear();
            prod_id_list.Clear();

            string ReportTitles = Request.Form["ReportTitles"];
            string rptType = form["hdnReportType"].ToString();
            string ReportType = String.IsNullOrEmpty(Request.Form["ReportType"]) ? rptType : Request.Form["ReportType"];

            day_date = Request.Form["day_date"];
            to_date = Request.Form["to_date"];
            string month = Request.Form["month"];
            bool show_nil_stock = bool.Parse(Request.Form.GetValues("show_nil_stock")[0].ToString());

            //Customers
            if (Request.Form["chk_cust"] != null)
            {
                string[] cust_ids = Request.Form["chk_cust"].ToString().Split(',');

                foreach (string item in cust_ids)
                {
                    cust_id_list.Add(Convert.ToDecimal(item));
                }
            }
            //Products
            if (Request.Form["chk_prod"] != null)
            {
                string[] prod_ids = Request.Form["chk_prod"].ToString().Split(',');
                if (prod_ids.Contains("0"))
                {
                    List<int> itemids = db.InventoryItemsMasters.Where(a => a.Active == true).Select(a => a.ID).ToList();
                    foreach (int item in itemids)
                    {
                        prod_id_list.Add(item);
                    }
                }
                else
                {
                    foreach (string item in prod_ids)
                    {
                        prod_id_list.Add(int.Parse(item));
                    }
                }
            }

            /* Report Selection */

            if (int.Parse(ReportTitles) == 1)
            {
                if (int.Parse(ReportType) == 1)
                {
                    ViewData["title"] = "DELIVERY REGISTER REPORT";
                    ViewData["subtitle"] = "Day Delivery Register on " + day_date;
                    DateTime dt = Convert.ToDateTime(day_date);
                    var report = GetStockIssueReportListByDate(dt);
                    //var report = isalesReportRepository.GetSalesReportByDate(long.Parse(branch_id), DateTime.Parse(day_date));
                    return PartialView("GetStockIssueReport", report);
                }
                if (int.Parse(ReportType) == 2 || int.Parse(ReportType) == 3)
                {
                    ViewData["title"] = "DELIVERY REGISTER REPORT";
                    ViewData["subtitle"] = "Delivery Register from " + day_date + " to " + to_date;
                    //ViewData["day_date"] = day_date;
                    //var report = isalesReportRepository.GetSalesReportBetweenDates(long.Parse(branch_id), DateTime.Parse(day_date), DateTime.Parse(to_date));
                    DateTime fromDt = Convert.ToDateTime(day_date);
                    DateTime toDt = Convert.ToDateTime(to_date);
                    var report = GetStockIssueReportListBetweenDates(fromDt, toDt);
                    return PartialView("GetStockIssueReport", report);
                }

            }
            else if (int.Parse(ReportTitles) == 2)
            {
                if (int.Parse(ReportType) == 1)
                {
                    ViewData["title"] = "GOODS RECEIVED REGISTER REPORT";
                    ViewData["subtitle"] = "Day Goods Received Register on " + day_date;
                    DateTime dt = Convert.ToDateTime(day_date);
                    var report = GetStockInwardReportListByDate(dt);
                    //var report = isalesReportRepository.GetSalesReportByDate(long.Parse(branch_id), DateTime.Parse(day_date));
                    return PartialView("GetStockInwardReport", report);
                }
                if (int.Parse(ReportType) == 2 || int.Parse(ReportType) == 3)
                {
                    ViewData["title"] = "GOODS RECEIVED REGISTER REPORT";
                    ViewData["subtitle"] = "Goods Received Register from " + day_date + " to " + to_date;
                    //ViewData["day_date"] = day_date;
                    //var report = isalesReportRepository.GetSalesReportBetweenDates(long.Parse(branch_id), DateTime.Parse(day_date), DateTime.Parse(to_date));
                    DateTime fromDt = Convert.ToDateTime(day_date);
                    DateTime toDt = Convert.ToDateTime(to_date);
                    var report = GetStockInwardReportListBetweenDates(fromDt, toDt);
                    return PartialView("GetStockInwardReport", report);
                }

            }
            else if (int.Parse(ReportTitles) == 3)
            {
                if (int.Parse(ReportType) == 1)
                {
                    ViewData["title"] = "PRODUCT WISE CONSOLIDATED REGISTER REPORT";
                    ViewData["subtitle"] = "Product Wise Consolidated Register on " + day_date;
                    DateTime dt = Convert.ToDateTime(day_date);
                    var report = GetStockProdConReportListByDate(dt);
                    //var report = isalesReportRepository.GetSalesReportByDate(long.Parse(branch_id), DateTime.Parse(day_date));
                    return PartialView("GetStockProdConReport", report);
                }
                if (int.Parse(ReportType) == 2 || int.Parse(ReportType) == 3)
                {
                    ViewData["title"] = "PRODUCT WISE CONSOLIDATED REGISTER REPORT";
                    ViewData["subtitle"] = "Product Wise Consolidated report from " + day_date + " to " + to_date;
                    //ViewData["day_date"] = day_date;
                    //var report = isalesReportRepository.GetSalesReportBetweenDates(long.Parse(branch_id), DateTime.Parse(day_date), DateTime.Parse(to_date));
                    DateTime fromDt = Convert.ToDateTime(day_date);
                    DateTime toDt = Convert.ToDateTime(to_date);
                    var report = GetStockProdConReportListBetweenDates(fromDt, toDt);
                    return PartialView("GetStockProdConReport", report);
                }

            }
            else if (int.Parse(ReportTitles) == 7)
            {
                rep_stock_pos_flag = 7;
                ViewData["title"] = "Branch WISE STOCK POSITIONS REGISTER REPORT";
                ViewData["subtitle"] = "Branch wise Stock Positions Register from " + day_date + " to " + to_date;
                ViewBag.date1 = Convert.ToDateTime(day_date); //GlobalHelper.Tommddyy(day_date);
                ViewBag.date2 = Convert.ToDateTime(to_date);  // GlobalHelper.Tommddyy(to_date);

                var report = GetStockPositionsReportListByCustomers(cust_id_list, show_nil_stock);

                return PartialView("GetStockPositionsReport", report);


            }
            else if (int.Parse(ReportTitles) == 8)
            {
                rep_stock_pos_flag = 8;
                ViewData["title"] = "PRODUCT WISE STOCK POSITIONS REGISTER REPORT";
                ViewData["subtitle"] = "Product wise Stock Positions Register from " + day_date + " to " + to_date;
                ViewBag.date1 = Convert.ToDateTime(day_date);
                ViewBag.date2 = Convert.ToDateTime(to_date); //GlobalHelper.Tommddyy(to_date);
                var report = GetStockPositionsReportListByProducts(prod_id_list, show_nil_stock);
                return PartialView("GetStockPositionsReport", report);


            }
            else if (int.Parse(ReportTitles) == 9)
            {
                rep_stock_pos_flag = 9;
                ViewData["title"] = "STOCK POSITIONS REGISTER REPORT";
                ViewData["subtitle"] = "Stock Positions Positions Register from " + day_date + " to " + to_date;
                ViewBag.date1 = Convert.ToDateTime(day_date);
                ViewBag.date2 = Convert.ToDateTime(to_date);
                var report = GetStockPositionsReportList(show_nil_stock);
                return PartialView("GetStockPositionsReport", report);


            }
            return View();

        }

        public ActionResult DownloadExcel()
        {
            MemoryStream ms = CreateExcelFile();
            if (ms != null)
            {
                // return the filestream
                // Rewind the memory stream to the beginning
                ms.Seek(0, SeekOrigin.Begin);
                string filename = "StockPositionsExcel.xlsx";
                return File(ms, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml", filename);
            }
            // No Excel file, reshow the same view
            return View();
        }
        public MemoryStream CreateExcelFile()
        {
            var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Stock Position");


            IEnumerable<ReportHelper> report = null;
            try
            {
                switch (rep_stock_pos_flag)
                {
                    case 7:
                        report = GetStockPositionsReportListByCustomers(cust_id_list, false);
                        //Title
                        ws.Cell("B2").Value = " WISE STOCK POSITIONS REGISTER REPORT";
                        ws.Cell("B3").Value = "Customer wise Stock Positions Register from " + day_date + " to " + to_date;


                        break;
                    case 8:
                        report = GetStockPositionsReportListByProducts(prod_id_list, false);
                        //Title

                        ws.Cell("B2").Value = "PRODUCT WISE STOCK POSITIONS REGISTER REPORT";
                        ws.Cell("B3").Value = "Product wise Stock Positions Register from " + day_date + " to " + to_date;
                        break;
                    case 9:
                        report = GetStockPositionsReportList(false);
                        //Title
                        ws.Cell("B2").Value = "STOCK POSITIONS REGISTER REPORT";
                        ws.Cell("B3").Value = "Stock Positions Positions Register from " + day_date + " to " + to_date;
                        break;
                    default:
                        break;
                }

                string lot_no = String.Empty; double sum_total = 0; double issued_qty = 0; double total_weight = 0;
                //First Names
                ws.Cell("B5").Value = "Date";
                ws.Cell("C5").Value = "Receipt.No.";
                //ws.Cell("D5").Value = "GRN.No.";
                //ws.Cell("D5").Value = "Branch";
                ws.Cell("E5").Value = "Product Name";
                ws.Cell("F5").Value = "Dt.Issued";
                ws.Cell("G5").Value = "Qty Received";
                ws.Cell("H5").Value = "Issued";
                ws.Cell("I5").Value = "Total QOH";
                // ws.Cell("J5").Value = "Total Weight";
                ws.Cell("J5").Value = "Units";
                int i = 6; int ItemId = 0;
                foreach (var item in report)
                {
                    string FromDt = string.Format("{0:dd\\/MM\\/yyyy}", item.StockRegister.EntryDt);
                    ws.Cell(("B" + i)).SetValue(FromDt);
                    ws.Cell(("C" + i)).SetValue(item.StockRegister.ReceiptNO);
                    // ws.Cell(("D" + i)).SetValue("");
                    //ws.Cell(("D" + i)).SetValue(item.StockRegister.CampusMaster.CampusName);
                    ws.Cell(("D" + i)).SetValue(item.StockRegister_Det.ItemText);
                    ws.Cell(("E" + i)).SetValue("");
                    ws.Cell(("F" + i)).SetValue(item.StockRegister_Det.ReceQty);
                    ws.Cell(("G" + i)).SetValue("");
                    ws.Cell(("H" + i)).SetValue(item.StockRegister_Det.ReceQty);
                    //sum_total += (double)item.StockRegister_Det.QOH;
                    if (ItemId == 0)
                    {
                        sum_total += item.StockRegister_Det.QOH == null ? 0 : (double)item.StockRegister_Det.QOH;
                    }
                    else if (ItemId == item.StockRegister_Det.ItemId)
                    {
                        sum_total += item.StockRegister_Det.QOH == null ? 0 : (double)item.StockRegister_Det.QOH;
                    }
                    else
                    {
                        sum_total = item.StockRegister_Det.QOH == null ? 0 : (double)item.StockRegister_Det.QOH;
                    }
                    ItemId = (int)item.StockRegister_Det.ItemId;
                    // ws.Cell(("J" + i)).SetValue(item.StockRegister_Det.Weight == null ? 0 : item.StockRegister_Det.Weight);
                    total_weight += item.StockRegister_Det.Weight == null ? 0 : (double)item.StockRegister_Det.Weight;
                    ws.Cell(("I" + i)).SetValue(item.StockRegister_Det.InventoryUnitsMaster.UnitsShortText);
                    DateTime dt1 = Convert.ToDateTime(day_date);
                    DateTime dt2 = Convert.ToDateTime(to_date);
                    issued_qty = 0;

                    var q =
                        (from n in db.InventoryIssuedMasters
                         join s in db.InventoryIssuedDetails on n.IssueNo equals s.IssueNo
                         where s.ReceiptNo == item.StockRegister.ReceiptNO && n.IssueNo == s.IssueNo
                         && item.StockRegister_Det.ItemId == s.ItemId
                         && n.IssueDt >= dt1 && n.IssueDt <= dt2
                         select new ReportHelper
                         {
                             Dc = n,
                             Dc_Det = s
                         }).ToList<ReportHelper>();
                    int j = (i + 1);
                    foreach (var itm in q)
                    {
                        string Dt = string.Format("{0:dd\\/MM\\/yyyy}", itm.Dc_Det.IssuedDt);
                        ws.Cell(("B" + j)).SetValue("");
                        ws.Cell(("C" + j)).SetValue("");
                        //ws.Cell(("D" + j)).SetValue("");
                        //ws.Cell(("D" + j)).SetValue("");
                        ws.Cell(("D" + j)).SetValue(item.StockRegister_Det.ItemText);
                        ws.Cell(("E" + j)).SetValue(Dt); //GlobalHelper.Toddmmyy(@itm.Dc_Det.IssuedDt.Value.ToString()));
                        ws.Cell(("F" + j)).SetValue("");
                        ws.Cell(("G" + j)).SetValue(itm.Dc_Det.IssuedQty);
                        issued_qty += (double)itm.Dc_Det.IssuedQty;
                        ws.Cell(("H" + j)).SetValue((item.StockRegister_Det.ReceQty - issued_qty));
                        sum_total = item.StockRegister_Det.QOH == null ? 0 : (double)item.StockRegister_Det.QOH;
                        //ws.Cell(("J" + j)).SetValue("");
                        ws.Cell(("I" + j)).SetValue(item.StockRegister_Det.InventoryUnitsMaster.UnitsShortText);

                        j++;
                    }

                    ws.Cell(("B" + j)).SetValue("");
                    ws.Cell(("C" + j)).SetValue("");
                    // ws.Cell(("D" + j)).SetValue("");
                    //ws.Cell(("D" + j)).SetValue("");
                    ws.Cell(("D" + j)).SetValue("");
                    ws.Cell(("E" + j)).SetValue("");
                    ws.Cell(("F" + j)).SetValue("");
                    ws.Cell(("G" + j)).SetValue("Total:");
                    ws.Cell(("H" + j)).SetValue(sum_total);
                    // ws.Cell(("J" + j)).SetValue("");
                    ws.Cell(("I" + j)).SetValue(item.StockRegister_Det.InventoryUnitsMaster.UnitsShortText);
                    i = (j + 1);
                }
                MemoryStream ms = new MemoryStream();
                wb.SaveAs(ms);
                return ms;
            }
            catch (Exception e)
            {
                string errmsg = String.Format("Failed to create Excel file: {0}", e.Message);
                throw new Exception(errmsg, e);
            }
        }
        #endregion
        #region Custom Methods
        //public JsonResult GetCampuses()
        //{
        //    List<String> auth_campus_id = objCore.GetCampusIdsByUserName(User.Identity.Name);
        //    var customers_list =
        //             (from cust in db.CampusMasters.AsEnumerable()
        //              where auth_campus_id.Contains(cust.campusId.ToString())
        //              select new
        //              {
        //                  id = cust.campusId,
        //                  cust_text = cust.CampusName
        //              });

        //    return Json(customers_list, JsonRequestBehavior.AllowGet);
        //}

        public JsonResult GetProducts()
        {
            var products_list =
                    (from prod in db.InventoryItemsMasters
                     where prod.Active == true
                     select new
                     {
                         id = prod.ID,
                         prod_text = prod.ItemText
                     });


            return Json(products_list, JsonRequestBehavior.AllowGet);
        }

        public List<ReportHelper> GetStockIssueReportListByDate(DateTime date)
        {

            var q =
                     (from n in db.InventoryIssuedMasters
                      join s in db.InventoryIssuedDetails on n.IssueNo equals s.IssueNo
                      where n.IssueNo == s.IssueNo && n.IssueDt == date
                      select new ReportHelper
                      {
                          Dc = n,
                          Dc_Det = s
                      }).ToList<ReportHelper>();
            return q;


        }
        public List<ReportHelper> GetStockIssueReportListBetweenDates(DateTime date1, DateTime date2)
        {

            var q =
                     (from n in db.InventoryIssuedMasters
                      join s in db.InventoryIssuedDetails on n.IssueNo equals s.IssueNo
                      where n.IssueNo == s.IssueNo && n.IssueDt >= date1 && n.IssueDt <= date2
                      select new ReportHelper
                      {
                          Dc = n,
                          Dc_Det = s
                      }).ToList<ReportHelper>();
            return q;


        }
        public List<ReportHelper> GetStockInwardReportListByDate(DateTime date)
        {

            var q =
                     (from n in db.InventoryStockRegisters
                      join s in db.InventoryStockRegDetails on n.ID equals s.StockRegId
                      where n.ID == s.StockRegId && n.ReceivedDt == date
                      select new ReportHelper
                      {
                          StockRegister = n,
                          StockRegister_Det = s
                      }).ToList<ReportHelper>();
            return q;

        }
        public List<ReportHelper> GetStockInwardReportListBetweenDates(DateTime date1, DateTime date2)
        {

            var q = (from n in db.InventoryStockRegisters
                     join s in db.InventoryStockRegDetails on n.ID equals s.StockRegId
                     where n.ID == s.StockRegId && n.ReceivedDt >= date1 && n.ReceivedDt <= date2
                     select new ReportHelper
                     {
                         StockRegister = n,
                         StockRegister_Det = s
                     }).ToList<ReportHelper>();
            return q;

        }
        public List<ReportHelper> GetStockProdConReportListByDate(DateTime date)
        {

            var q = (from n in db.InventoryStockRegisters
                     join s in db.InventoryStockRegDetails on n.ID equals s.StockRegId
                     where n.ID == s.StockRegId && n.ReceivedDt == date
                     select new ReportHelper
                     {
                         StockRegister = n,
                         StockRegister_Det = s
                     }).OrderBy(p => p.StockRegister_Det.ItemId).ToList<ReportHelper>();
            return q;

        }
        public List<ReportHelper> GetStockProdConReportListBetweenDates(DateTime date1, DateTime date2)
        {

            var q =
                     (from n in db.InventoryStockRegisters
                      join s in db.InventoryStockRegDetails on n.ID equals s.StockRegId
                      where n.ID == s.StockRegId && n.ReceivedDt >= date1 && n.ReceivedDt <= date2
                      select new ReportHelper
                      {
                          StockRegister = n,
                          StockRegister_Det = s
                      }).OrderBy(p => p.StockRegister_Det.ItemId).ToList<ReportHelper>();
            return q;

        }

        public List<ReportHelper> GetStockPositionsReportList(bool show_nil_stock)
        {
            //  List<System.Nullable<int>> auth_cust_id = GlobalHelper.GetCustIdsByUserName(User.Identity.Name);
            List<ReportHelper> q = null;
            if (!show_nil_stock)
            {
                q =
                     (from n in db.InventoryStockRegisters
                      join s in db.InventoryStockRegDetails on n.ID equals s.StockRegId
                      where n.ID == s.StockRegId && s.QOH != 0
                      select new ReportHelper
                      {
                          StockRegister = n,
                          StockRegister_Det = s,


                      }).OrderBy(c => c.StockRegister.EntryDt.Value).ThenBy(p => p.StockRegister_Det.ItemId).ToList<ReportHelper>();
            }
            else
            {
                q =
                   (from n in db.InventoryStockRegisters
                    join s in db.InventoryStockRegDetails on n.ID equals s.StockRegId
                    where n.ID == s.StockRegId
                    select new ReportHelper
                    {
                        StockRegister = n,
                        StockRegister_Det = s,


                    }).OrderBy(c => c.StockRegister.EntryDt.Value).ThenBy(p => p.StockRegister_Det.ItemId).ToList<ReportHelper>();
            }
            //var exceptlist = from r in db.user_cust join m in db.customers on r.aspnet_Users.UserName equals User.Identity.Name where m.id == r.cid select new { m.id, m.cust_text };

            return q;

        }
        public List<ReportHelper> GetStockPositionsReportListByCustomers(List<System.Nullable<decimal>> custids, bool show_nil_stock)
        {
            List<ReportHelper> q = null;
            if (!show_nil_stock)
            {
                q =
                         (from n in db.InventoryStockRegisters
                          join s in db.InventoryStockRegDetails on n.ID equals s.StockRegId
                          where n.ID == s.StockRegId && s.QOH != 0
                          select new ReportHelper
                          {
                              StockRegister = n,
                              StockRegister_Det = s,


                          }).OrderBy(c => c.StockRegister.EntryDt.Value).ThenBy(p => p.StockRegister_Det.ItemId).ToList<ReportHelper>();
            }
            else
            {
                q =
                      (from n in db.InventoryStockRegisters
                       join s in db.InventoryStockRegDetails on n.ID equals s.StockRegId
                       where n.ID == s.StockRegId 
                       select new ReportHelper
                       {
                           StockRegister = n,
                           StockRegister_Det = s,

                       }).OrderBy(c => c.StockRegister.EntryDt.Value).ThenBy(p => p.StockRegister_Det.ItemId).ToList<ReportHelper>();
            }

            //var exceptlist = from r in db.user_cust join m in db.customers on r.aspnet_Users.UserName equals User.Identity.Name where m.id == r.cid select new { m.id, m.cust_text };

            return q;

        }
        public List<ReportHelper> GetStockPositionsReportListByProducts(List<System.Nullable<int>> prodids, bool show_nil_stock)
        {
            List<ReportHelper> q = null;
            if (!show_nil_stock)
            {
                q =
                     (from n in db.InventoryStockRegisters
                      join s in db.InventoryStockRegDetails on n.ID equals s.StockRegId
                      where n.ID == s.StockRegId && prodids.Contains(s.ItemId) && s.QOH != 0
                      select new ReportHelper
                      {
                          StockRegister = n,
                          StockRegister_Det = s,


                      }).OrderBy(c => c.StockRegister.EntryDt.Value).ThenBy(p => p.StockRegister_Det.ItemId).ToList<ReportHelper>();
                //var exceptlist = from r in db.user_cust join m in db.customers on r.aspnet_Users.UserName equals User.Identity.Name where m.id == r.cid select new { m.id, m.cust_text };
            }
            else
            {
                q =
                (from n in db.InventoryStockRegisters
                 join s in db.InventoryStockRegDetails on n.ID equals s.StockRegId
                 where n.ID == s.StockRegId && prodids.Contains(s.ItemId)
                 select new ReportHelper
                 {
                     StockRegister = n,
                     StockRegister_Det = s,


                 }).OrderBy(c => c.StockRegister.EntryDt.Value).ThenBy(p => p.StockRegister_Det.ItemId).ToList<ReportHelper>();

            }
            return q;

        }
        #endregion
    }
}
