using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;
using FMS.Helpers;
using System.Text;
using System.Data.OleDb;
using System.Web.Configuration;
using System.Data;
using System.Data.SqlClient;
using AsyncUploaderDemo.Models;
using System.Globalization;
using System.IO;
using ClosedXML.Excel;

namespace FMS.Controllers
{
    [CustomAuthorizationFilter]
    public class DieselTrackingController : Controller
    {
        #region ctors
        private FMSDBEntities db;
        private core core = new core();
        private List<tbl_diesel_tracking> _DieselTrackList = new List<tbl_diesel_tracking>();
        public DieselTrackingController()
        {
            db = new FMSDBEntities();
        } 
        #endregion

        #region Index Methods
        [Authorize]
        public ActionResult Index()
        {
            return View();
        }

        public JsonResult GetDieselTracking(int iDisplayStart, int iDisplayLength, string sSearch, int iColumns,
       int iSortingCols, int iSortCol_0, string sSortDir_0,
       int sEcho, string mDataProp_Key)
        {
            var DieselTrackingList = GetEnumerableList(null);
            var filteredLogSheet = DieselTrackingList
                .Select(l => new[]{
                    l.Id.ToString(),
                    l.Date1.ToShortDateString(),
                    l.Text3.ToString(),
                    l.Text4.ToString(),
                    l.Text6.ToString(),
                    l.Value1.ToString(),
                    l.Amount.ToString(),
                    l.Amount1.ToString(),
                    l.Text2.ToString(),
                    l.Text1.ToString(),
                    l.Text5.ToString(),
                    l.Text7.ToString(),
                    l.Status1.ToString(),
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

        private IEnumerable<CommonFields> GetEnumerableList(bool ? IsDet)
        {
            var list = new List<CommonFields>();
            if (IsDet.HasValue)
            {
                var tempDieselList = (from l in db.tbl_diesel_tracking
                                      where l.Status == true
                                      select l).ToList();
                foreach (tbl_diesel_tracking ds in tempDieselList)
                {
                    list.Add(new CommonFields
                    {
                        Id = ds.ID,
                        Text1 = ds.tbl_vehicles.VehicleRegNum,
                        Text2 = ds.tbl_clients.CompanyName,
                        Date1 = Convert.ToDateTime(ds.Date),
                        Text3 = ds.FuelStationName == null ? "" : ds.FuelStationName,
                        Value1 = ds.TokenValue == null ? 0 : (double)ds.TokenValue,
                        Text4 = ds.DieselTokenNum == null ? "" : ds.DieselTokenNum,
                        Text5 = ds.IssuedTo == null ? "" : ds.IssuedTo,
                        Amount = ds.PricePerLiter == null ? 0 : (decimal)ds.PricePerLiter,
                        Text6 = ds.CardNum == null ? "" : ds.CardNum,
                        Text7 = ds.IssuedBy == null ? "" : ds.IssuedBy,
                        Amount1 = ds.TotalAmount == null ? 0 : (decimal)ds.TotalAmount,
                        Status1 = ds.Audited == null ? false : (bool)ds.Audited,
                        Status = (bool)ds.Status,
                        Text8 = (ds.ClosedFlag == null || ds.ClosedFlag == false) ? "Un-Deducted" : "Deducted"
                    });
                }
            }
            else
            {
                var tempDieselList = (from l in db.tbl_diesel_tracking
                                      where l.Status == true && (l.ClosedFlag == false || l.ClosedFlag == null)
                                      select l).ToList();
                foreach (tbl_diesel_tracking ds in tempDieselList)
                {
                    list.Add(new CommonFields
                    {
                        Id = ds.ID,
                        Text1 = ds.tbl_vehicles.VehicleRegNum,
                        Text2 = ds.tbl_clients.CompanyName,
                        Date1 = Convert.ToDateTime(ds.Date),
                        Text3 = ds.FuelStationName == null ? "" : ds.FuelStationName,
                        Value1 = ds.TokenValue == null ? 0 : (double)ds.TokenValue,
                        Text4 = ds.DieselTokenNum == null ? "" : ds.DieselTokenNum,
                        Text5 = ds.IssuedTo == null ? "" : ds.IssuedTo,
                        Amount = ds.PricePerLiter == null ? 0 : (decimal)ds.PricePerLiter,
                        Text6 = ds.CardNum == null ? "" : ds.CardNum,
                        Text7 = ds.IssuedBy == null ? "" : ds.IssuedBy,
                        Amount1 = ds.TotalAmount == null ? 0 : (decimal)ds.TotalAmount,
                        Status1 = ds.Audited == null ? false : (bool)ds.Audited,
                        Status = (bool)ds.Status,
                        Text8 = (ds.ClosedFlag == null || ds.ClosedFlag == false) ? "Un-Deducted" : "Deducted"
                    });
                }
            }
            core.ConvertToUppercase<CommonFields>(list);
            return list.OrderBy(a => a.Id);
        }  
        #endregion

        #region Add Diesel Track
        [Authorize]
        public ActionResult Create()
        {
            return View(new tbl_diesel_tracking());
        }
        public ActionResult AddNewDieselTrack()
        {
            return PartialView("_AddNewDieselTrack");
        }
        [HttpPost]
        public ActionResult Create(FormCollection frm)
        {
            try
            {
                // List of Diesel Book 
                List<tbl_diesel_tracking> _dTrackingList = (List<tbl_diesel_tracking>)Session["DieselTrackingEntryList"];
                foreach (tbl_diesel_tracking _dtrack in _dTrackingList)
                {
                    db.tbl_diesel_tracking.AddObject(_dtrack);

                    // Get Diesel Book Details by token number
                    tbl_diesel_books dieselBook = GetDieselBook(_dtrack.DieselTokenNum);
                    if (dieselBook != null)
                        UpdateClosedFlagInDieselBook(dieselBook.ID);
                }
                db.SaveChanges();
                core.LoggingEntries("Diesel tracking ", "Diesel tracking has created for client " + _dTrackingList.FirstOrDefault().tbl_clients.CompanyName + "", User.Identity.Name);
                Session["DieselTrackingEntryList"] = null;
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                return View();
            }
        }
        [HttpPost]
        public ActionResult AddDieselTrackingEntry(FormCollection frm, long CID, long VID, tbl_diesel_tracking diesel)
        {
            long ID = 0;
            diesel.IssuedTo = new core().GetDriver(Convert.ToInt64(diesel.IssuedTo));
            _DieselTrackList = (List<tbl_diesel_tracking>)Session["DieselTrackingEntryList"];
            if (_DieselTrackList == null)
                _DieselTrackList = new List<tbl_diesel_tracking>();
            if (_DieselTrackList.Count() == 0)
                ID = 1;
            else
                ID = _DieselTrackList.Count() + 1;

            // Verify Diesel Tracking Token Numbers

            if ((diesel.FuelStationName != null && diesel.CardNum != null && diesel.DieselTokenNum != null) || (diesel.FuelStationName != "" && diesel.CardNum != "" && diesel.DieselTokenNum != ""))
            {
                var dtList = db.tbl_diesel_tracking.Where(a => a.FuelStationName == diesel.FuelStationName && a.CardNum == diesel.CardNum
                    && a.DieselTokenNum == diesel.DieselTokenNum && (a.ClosedFlag == false || a.ClosedFlag == null) && a.Status == true).ToList();
                if (dtList.Count >= 1)
                {
                    return Content("Msg_Record already exists with Fuel Station name:" + diesel.FuelStationName + ",book No:" + diesel.CardNum + ",Token Number:" + diesel.DieselTokenNum);
                }
                var tempDtList = _DieselTrackList.Where(a => a.FuelStationName == diesel.FuelStationName
                    && a.CardNum == diesel.CardNum && a.DieselTokenNum == diesel.DieselTokenNum).ToList();
                if (tempDtList.Count >= 1)
                {
                    return Content("Msg_Record already exists with Fuel Station name:" + diesel.FuelStationName + ",book No:" + diesel.CardNum + ",Token Number:" + diesel.DieselTokenNum);
                }
            }

            // Add object to list 
            _DieselTrackList.Add(new tbl_diesel_tracking
            {
                ID = ID,
                ClientID = CID,
                VehicleID = VID,
                Date = diesel.Date,
                FuelStationName = diesel.FuelStationName,
                DieselTokenNum = diesel.DieselTokenNum,
                TokenValue = diesel.TokenValue,
                CreatedDt = DateTime.Now,
                CreatedBy = User.Identity.Name,
                CardNum = diesel.CardNum,
                PricePerLiter = diesel.PricePerLiter,
                IssuedTo = diesel.IssuedTo,
                IssuedBy = diesel.IssuedBy,
                TotalAmount = diesel.TotalAmount,
                Remark = diesel.Remark,
                OdoMeterReading = diesel.OdoMeterReading,
                Status = true
            });
            Session["DieselTrackingEntryList"] = _DieselTrackList;
            ViewBag.message = "";
            return PartialView("_AddDieselTrackingEntry", _DieselTrackList);
        } 
        #endregion

        #region Edit
        public ActionResult Edit(int id)
        {
            tbl_diesel_tracking _dtrack = db.tbl_diesel_tracking.Where(d => d.Status == true && d.ID == id).SingleOrDefault();
            ViewBag.VehicleRegNum = db.tbl_vehicles.Where(a => a.Status == true && a.ID == _dtrack.VehicleID).SingleOrDefault().VehicleRegNum.ToString();
            ViewBag.ClientName = core.GetClient((long)_dtrack.ClientID);
            ViewBag.ClientID = _dtrack.ClientID;
            ViewBag.VehicleID = _dtrack.VehicleID;
            return View(_dtrack);
        }
        [HttpPost]
        public ActionResult Edit(int id, FormCollection frm, tbl_diesel_tracking diesel)
        {
            long ClientID = Convert.ToInt64(frm["cid"]);
            long VehicleID = Convert.ToInt64(frm["vid"]);
            diesel.ClientID = ClientID;
            diesel.VehicleID = VehicleID;
            diesel.Status = true;
            diesel.IssuedTo = new core().GetDriver(Convert.ToInt64(diesel.IssuedTo));
            ViewBag.VehicleRegNum = db.tbl_vehicles.Where(a => a.Status == true && a.ID == VehicleID).SingleOrDefault().VehicleRegNum.ToString();
            ViewBag.ClientName = core.GetClient(ClientID);

            tbl_diesel_tracking _dtrack = db.tbl_diesel_tracking.Where(d => d.Status == true && d.ID == diesel.ID).SingleOrDefault();
            try
            {
                if (diesel.DieselTokenNum != null && diesel.DieselTokenNum != "")
                {
                    if (!ValidationTokenNumber(diesel))
                    {
                        return View(_dtrack);
                    }
                    //Verify whether previous diesel token number is different from current diesel token number
                    if (_dtrack.DieselTokenNum != diesel.DieselTokenNum)
                    {
                        if (_dtrack.DieselTokenNum != null && _dtrack.DieselTokenNum != "")
                        {
                            tbl_diesel_books prevDieselBook = GetDieselBook(_dtrack.DieselTokenNum); // Get Previous diesel books details by token number
                            if (prevDieselBook != null)
                                UpdateClosedFlagSetToZeroInDieselBook(prevDieselBook.ID); // Upddate closed flag to false in diesel book
                        }
                        tbl_diesel_books dieselBook = GetDieselBook(diesel.DieselTokenNum);
                        if (dieselBook != null)                          // Get Current diesel books details by token number
                            UpdateClosedFlagInDieselBook(dieselBook.ID); // Update closed flag to true in diesel book
                    }
                }
                else
                {
                    if (_dtrack.DieselTokenNum != null && _dtrack.DieselTokenNum != "")
                    {
                        tbl_diesel_books prevDieselBook = GetDieselBook(_dtrack.DieselTokenNum);
                        if (prevDieselBook != null)
                            UpdateClosedFlagSetToZeroInDieselBook(prevDieselBook.ID);
                    }
                }
                db.tbl_diesel_tracking.DeleteObject(_dtrack); //Delete existing object
                this.db.SaveChanges();

                db.tbl_diesel_tracking.AddObject(diesel); //Re-insert log entry
                db.SaveChanges();
                core.LoggingEntries("Diesel Tracking ", "Diesel tracking has updated.", User.Identity.Name);
                return RedirectToAction("Index");
            }
            catch
            {
                return View(_dtrack);
            }

        } 
        #endregion

        #region Delete
        public ActionResult Delete(int id)
        {
            tbl_diesel_tracking _dtrack = db.tbl_diesel_tracking.Where(d => d.Status == true && d.ID == id).SingleOrDefault();
            return View(_dtrack);
        }
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            tbl_diesel_tracking _dtrack = db.tbl_diesel_tracking.Where(d => d.Status == true && d.ID == id).SingleOrDefault();
            try
            {
                _dtrack.Status = false;
                TryUpdateModel(_dtrack);
                //Set Closed Flag to false in Diesel Book for selected token number
                tbl_diesel_books dieselBook = GetDieselBook(_dtrack.DieselTokenNum);
                if (dieselBook != null)
                    UpdateClosedFlagSetToZeroInDieselBook(dieselBook.ID);
                db.SaveChanges();
                core.LoggingEntries("Diesel Tracking ", "Diesel tracking deletion has done.", User.Identity.Name);
                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        } 
        #endregion

        #region Audit Diesel
        [HttpGet]
        public ActionResult AuditDiesel(long Id)
        {
            try
            {
                tbl_diesel_tracking dieselTrack = db.tbl_diesel_tracking.Where(a => a.ID == Id && a.Status == true).FirstOrDefault();
                dieselTrack.Audited = true;
                dieselTrack.AuditedBy = User.Identity.Name;
                dieselTrack.AuditedDt = DateTime.Now;
                TryUpdateModel(dieselTrack);
                db.SaveChanges();
                core.LoggingEntries("Diesel Mgmt.", "Diesel Audit has done to the token number " + dieselTrack.DieselTokenNum, User.Identity.Name);
                return Content("<script language='javascript' type='text/javascript'>alert('Diesel Audit has done sucessfully');window.location='/DieselTracking/Index';</script>");
            }
            catch (Exception Ex)
            {
                return Content("<script language='javascript' type='text/javascript'>alert('An error occured while processing your request."+ Ex.Message +"');</script>");
            }
        } 
        #endregion

        #region Export to Excel
        public ActionResult DieselTrackingExportToExcel()
        {
            string Filename = "DieselTrack_" + DateTime.Now.ToString("hhmmss") + ".xlsx";
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
            string sheetName = "DieselTracking";
            var wb = new XLWorkbook();
            try
            {
                var ws = wb.Worksheets.Add(sheetName);
                var dieseltrackList = GetEnumerableList(true).OrderBy(X => X.Id).ToList();
                if (dieseltrackList.Count > 0)
                {
                    ws.Cell("B1").Value = "Diesel Tracking Details";
                    ws.Cell("B2").Value = " Generated on " + DateTime.Now;
                }
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
                ws.Cell("D3").Value = "Diesel Token#";
                ws.Cell("E3").Value = "Book No.";
                ws.Cell("F3").Value = "Litre(s)";

                ws.Cell("G3").Value = "Price/liter";
                ws.Cell("H3").Value = "Total Amount";
                ws.Cell("I3").Value = "Client Name";
                ws.Cell("J3").Value = "Vehicle Reg#";
                ws.Cell("K3").Value = "Issued To";
                ws.Cell("L3").Value = "Issued By";
                ws.Cell("M3").Value = "Status";

                int i = 4;
                foreach (var item in dieseltrackList)
                {
                    string PricePerlts = item.Amount.ToString("N", CultureInfo.CurrentCulture);
                    string TotAmount = item.Amount1.ToString("N", CultureInfo.CurrentCulture);
                    ws.Cell("B" + i).SetValue(item.Date1.ToShortDateString());
                    ws.Cell(("C" + i)).SetValue(item.Text3.ToString());
                    ws.Cell(("D" + i)).SetValue(item.Text4.ToString());
                    ws.Cell(("E" + i)).SetValue(item.Text6.ToString());
                    ws.Cell(("F" + i)).SetValue(item.Value1);
                    ws.Cell(("G" + i)).SetValue(PricePerlts);
                    ws.Cell(("H" + i)).SetValue(TotAmount);
                    ws.Cell(("I" + i)).SetValue(item.Text2.ToString());
                    ws.Cell(("J" + i)).SetValue(item.Text1.ToString());
                    ws.Cell(("K" + i)).SetValue(item.Text5.ToString());
                    ws.Cell(("L" + i)).SetValue(item.Text7.ToString());
                    ws.Cell(("M" + i)).SetValue(item.Text8.ToString());
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

        #region Methods
        // UnSaved Diesel Track
        public int SaveUnSavedDieselEntries()
        {
            try
            {
                // List of Diesel Book 
                List<tbl_diesel_tracking> _dTrackingList = (List<tbl_diesel_tracking>)Session["DieselTrackingEntryList"];
                foreach (tbl_diesel_tracking _dtrack in _dTrackingList)
                {
                    db.tbl_diesel_tracking.AddObject(_dtrack);

                    // Get Diesel Book Details by token number
                    tbl_diesel_books dieselBook = GetDieselBook(_dtrack.DieselTokenNum);
                    if (dieselBook != null)
                        UpdateClosedFlagInDieselBook(dieselBook.ID);
                }
                db.SaveChanges();
                core.LoggingEntries("Diesel tracking ", "Diesel tracking has created for client " + _dTrackingList.FirstOrDefault().tbl_clients.CompanyName + "", User.Identity.Name);
                Session["DieselTrackingEntryList"] = null;
                return 1;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public ActionResult RemoveItem(long ID)
        {
            _DieselTrackList = (List<tbl_diesel_tracking>)Session["DieselTrackingEntryList"];
            foreach (tbl_diesel_tracking obj in _DieselTrackList)
            {
                if (obj.ID == ID) // Will match once
                {
                    //Delete the Row
                    _DieselTrackList.Remove(obj);
                    ReShuffleSlno();
                    break;
                }
            }
            return PartialView("_AddDieselTrackingEntry", _DieselTrackList);
        }

        public void ReShuffleSlno()
        {
            var i = 1;
            foreach (tbl_diesel_tracking obj in _DieselTrackList)
            {
                obj.ID = i;
                i++;
            }

        }

        public bool ValidateForm(tbl_diesel_tracking diesel)
        {
            if (diesel.ClientID == null || diesel.ClientID.ToString().Trim().Length == 0)
                ModelState.AddModelError("ClientID", "Please enter Client Name.");
            if (diesel.VehicleID == null || diesel.VehicleID.ToString().Trim().Length == 0)
                ModelState.AddModelError("VehicleID", "Please enter Vehicle Reg No.");
            if (diesel.Date == null || diesel.Date.ToString().Trim().Length == 0)
                ModelState.AddModelError("Date", "Please enter date.");
            if (diesel.FuelStationName == null || diesel.FuelStationName.ToString().Trim().Length == 0)
                ModelState.AddModelError("FuelStationName", "Please enter Fuel Station Name.");

            if (diesel.DieselTokenNum == null || diesel.DieselTokenNum.ToString().Trim().Length == 0)
                ModelState.AddModelError("DieselTokenNum", "Please enter Diesel token No.");
            if (diesel.TokenValue == null || diesel.TokenValue.ToString().Trim().Length == 0)
                ModelState.AddModelError("TokenValue", "Please enter Diesel token value.");

            return ModelState.IsValid;
        }
        public bool ValidationTokenNumber(tbl_diesel_tracking diesel) 
        {
            var dtList = db.tbl_diesel_tracking.Where(a => a.FuelStationName == diesel.FuelStationName && a.CardNum == diesel.CardNum
                    && a.DieselTokenNum == diesel.DieselTokenNum && (a.ClosedFlag == false || a.ClosedFlag == null) && a.Status == true).ToList();
            if (!dtList.Where(a => a.ID == diesel.ID).Any())
            {
                ModelState.AddModelError("DieselTokenNum", "Token Number has already exists.");
                return false;
            }
            return true;
        }
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
                str.Append(string.Format("{0} | {1}\r\n", a.CompanyName.ToUpper().Trim(), a.ID)).ToString();
                ViewBag.ClientID = a.ID;
            }

            return str.ToString();
        }
        public string getVehicleAjaxResult(string f, string q)
        {
            f = f == "undefined" ? "VehicleRegNum" : f;
            StringBuilder str = new StringBuilder();
            var vehicle = from a in db.tbl_vehicles
                                   .Where(a => a.Status.Value == true)
                                   .Where<tbl_vehicles>(f, q, WhereOperation.Contains)
                         select new { a.VehicleRegNum, a.ID };


            foreach (var a in vehicle)
            {
                str.Append(string.Format("{0} |{1}\r\n", a.VehicleRegNum.ToUpper().Trim(), a.ID)).ToString();
            }

            return str.ToString();
        }
        public string GetTokenNumber(string f, string q) 
        {
            f = f == "undefined" ? "TokenNumber" : f;
            StringBuilder str = new StringBuilder();
            var dieselBook = from a in db.tbl_diesel_books
                                    .Where(a => a.Status.Value == true)
                                    .Where<tbl_diesel_books>(f, q, WhereOperation.Contains)
                         select new { a.TokenNumber, a.ID };

            foreach (var a in dieselBook)
            {
                str.Append(string.Format("{0}|{1}\r\n", a.TokenNumber.ToUpper(), a.ID)).ToString();
            }
            return str.ToString();
        }

        public JsonResult GetDieselBookDetailsByTokenNum(string TokenNumber)
        {
            var _diesel = db.tbl_diesel_books.Where(a => a.TokenNumber.Trim() == TokenNumber.Trim() && a.Status == true).FirstOrDefault();
            object[] dieselBook = new object[4];
            dieselBook[0] = _diesel.BookNo;
            dieselBook[1] = _diesel.tbl_fuel_stations.FuelStation;
            dieselBook[2] = _diesel.TokenValue;
            dieselBook[3] = _diesel.PricePerLiter;
            return Json(dieselBook, JsonRequestBehavior.AllowGet);
        }

        private void UpdateClosedFlagInDieselBook(long Id)
        {
            tbl_diesel_books diesel = db.tbl_diesel_books.Where(a => a.ID == Id && a.Status == true).FirstOrDefault();
            diesel.ClosedFlag = true;
            TryUpdateModel(diesel);
            db.SaveChanges();
        }

        private void UpdateClosedFlagSetToZeroInDieselBook(long Id) 
        {
            tbl_diesel_books diesel = db.tbl_diesel_books.Where(a => a.ID == Id && a.Status == true).FirstOrDefault();
            double tokenValue = (double)diesel.TokenValue;
            decimal pricePerLiter = (decimal)diesel.PricePerLiter;
            diesel.ClosedFlag = false;
            TryUpdateModel(diesel);
            diesel.TokenValue = tokenValue;
            diesel.PricePerLiter = pricePerLiter;
            db.SaveChanges();
        }

        private tbl_diesel_books GetDieselBook(string TokenNum) 
        {
            tbl_diesel_books diesel = db.tbl_diesel_books.Where(a => a.TokenNumber.ToUpper().Trim() == TokenNum.Trim().ToUpper() && a.Status == true).FirstOrDefault();
            return diesel;
        }
 
        #endregion

    }


    internal class CommonFields
    {
        public long Id { get; set; }
        public long Id1 { get; set; }
        public int Id2 { get; set; }
        public string Text1 { get; set; }
        public string Text2 { get; set; }
        public string Text3 { get; set; }
        public string Text4 { get; set; } 
        public string Text5 { get; set; }
        public string Text6 { get; set; }
        public string Text7 { get; set; }
        public string Text8 { get; set; }
        public string Text9 { get; set; }
        public string Text10 { get; set; }  

        public DateTime Date1 { get; set; }
        public DateTime Date2 { get; set; } 

        public double Value1 { get; set; }
        public double Value2 { get;set; }

        public bool Status { get; set; }
        public bool Status1 { get; set; }
        public bool Status2 { get; set; }
        public bool Status3 { get; set; }

        public decimal Amount { get; set; }
        public decimal Amount1 { get; set; }
        public decimal Amount2 { get; set; }
        public decimal Amount3 { get; set; }
        public decimal Amount4 { get; set; }
        public decimal Amount5 { get; set; }
        public decimal Amount6 { get; set; }

    }
}
