using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using FMS.Models;
using System.Web.Configuration;
using FMS.Helpers;
using System.IO;
using ActiveUp.Net.Mail;
namespace FMS.Controllers
{
    [CustomAuthorizationFilter]
    public class BookingsController : Controller
    {
        private FMSDBEntities db;
        private core objCr;
        public BookingsController()
        {
            db = new FMSDBEntities();
            objCr = new core(); 
        }

        #region Index Method
        public ActionResult Index()
        {
            ViewBag.MonthId = new SelectList(core.GetAllMonths(), "Value", "Text");
            ViewBag.YearId = new SelectList(core.GetAllYears(), "Value", "Text");
            return View();
        }

        public JsonResult GetBookingList(int iDisplayStart, int iDisplayLength, string sSearch, int iColumns,
       int iSortingCols, int iSortCol_0, string sSortDir_0,
       int sEcho, string mDataProp_Key, string Status, int? MonthId, int? YearId)
        {
            var bookingList = GetBookingList(Status, MonthId, YearId);
            var filteredLogSheet = bookingList
                .Select(l => new[]{
                    l.Id.ToString(),
                    l.BookingRef.ToString(),
                    l.Client.ToString(),
                    l.FromMail.ToString(),
                    l.GuestName.ToString(),
                    l.BookingDt.ToString(),
                    l.ReportDt.ToString(),
                    l.ReportTime.ToString(),
                    l.VehModel.ToString(),
                    l.VehType.ToString(),
                    l.Priority.ToString(),
                    l.TripType.ToString(),
                    l.Status.ToString(),
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

        private IEnumerable<BookingClass> GetBookingList(string status, int? MonthId, int? YearId)
        {
            var list = new List<BookingClass>();
            List<tbl_bookings> tempBookList = null;
            if (MonthId.HasValue && YearId.HasValue)
            {
                tempBookList = (from l in db.tbl_bookings
                                where l.Active == true
                                && l.BookingDt.Value.Month == MonthId
                                && l.BookingDt.Value.Year == YearId
                                && l.Status == status
                                select l).AsEnumerable().ToList();
            }
            else
            {
                tempBookList = (from l in db.tbl_bookings
                                where l.Active == true
                                && l.Status == status
                                select l).AsEnumerable().ToList();
            }
            foreach (tbl_bookings bk in tempBookList)
            {
                list.Add(new BookingClass
                {
                    Id = bk.Id,
                    BookingRef = bk.BookRefNo,
                    Client = bk.tbl_clients.CompanyName,
                    FromMail = bk.FromMail,
                    GuestName = bk.GuestName,
                    BookingDt = bk.BookingDt.HasValue ? bk.BookingDt.Value.ToShortDateString() : "",
                    ReportDt = bk.DateofReport.HasValue ? bk.DateofReport.Value.ToShortDateString() : "",
                    ReportTime = bk.ReportingTime,
                    VehModel = bk.tbl_vehicle_models.VehicleModelName,
                    VehType = bk.tbl_vehicle_types.VehicleType,
                    Priority = bk.Priority,
                    TripType = bk.TripType,
                    Status = bk.Status,
                    Active = bk.Active.HasValue ? (bool)bk.Active : false
                });
            }

            return list.OrderBy(a => a.Id).OrderBy(a => a.Status);
        }

        #endregion

        #region Booking Generation
        [HttpGet]
        public ActionResult GenerateBooking(string EmailAddress = null, string DateOfMail = null, string CC = null, string BCC = null)
        {
            ViewBag.VehicleTypeId = new SelectList(db.tbl_vehicle_types.Where(a => a.Status == true), "ID", "VehicleType");
            ViewBag.VehicleModelId = new SelectList(db.tbl_vehicle_models.Where(a => a.Status == true), "ID", "VehicleModelName");
            ViewBag.TripType = new SelectList(objCr.GetTripTypeList(), "Value", "Text");
            ViewBag.Priority = new SelectList(objCr.GetPriorityList(), "Value", "Text");
            ViewBag.EmailAddress = string.IsNullOrEmpty(EmailAddress) ? "" : EmailAddress;
            ViewBag.DateOfMail = string.IsNullOrEmpty(DateOfMail) ? "" : DateOfMail;
            ViewBag.CC = string.IsNullOrEmpty(CC) ? "" : CC;
            ViewBag.BCC = string.IsNullOrEmpty(BCC) ? "" : BCC; 
            return View();
        }
        [HttpPost]
        public ActionResult GenerateBooking(FormCollection frm, tbl_bookings bookingDet)
        {
            try
            {
                string messageText = string.Empty;
                long ClientId = frm["cid"] == null ? 0 : Convert.ToInt64(frm["cid"]);
                string CC = frm["CC"];
                string BCC = frm["BCC"];
                bookingDet.Active = true;
                bookingDet.ClientId = ClientId;
                bookingDet.BookingBy = User.Identity.Name;
                bookingDet.BookRefNo = GetBookingRefNo(Convert.ToDateTime(bookingDet.DateofReport), 4); //DateTime.Now.ToString("yyhhddmmMMss");
                bookingDet.BookAckDt = DateTime.Now;
                bookingDet.BookAckBy = User.Identity.Name;
                bookingDet.Status = BookingStatus.Acknowledgement.ToString();
                bool IsTrigger = string.IsNullOrEmpty(frm["IsTrigger"]) ? false : frm.GetValues("IsTrigger").Contains("on");
                //objCr.ConvertToUppercase(bookingDet);
                if (db.tbl_bookings.Where(a => a.BookRefNo.Trim() == bookingDet.BookRefNo && a.Active == true).Any())
                {
                    return Json(new { success = false, msg = "Booking reference number has already exists", id = 0 });
                }
                db.tbl_bookings.AddObject(bookingDet);
                db.SaveChanges();

                messageText = "Dear Sir / Madam, <br/> <br/> Greeting’s from " + ApplicationSettings.OrgName + "!!! <br/> <br/> " +
                    "Thanks for choosing " + ApplicationSettings.OrgName + " ! Your Booking is confirmed <br/> <br/>" +
                    "We Will Revert with the Vehicle details at the earliest! " +
                    " <br/> <br/> <br/> <br/>  " +
                    " <span style='color:#41AADF;'> Regards, </span> <br/> " +
                    " <span style='color:#41AADF;'> " + ApplicationSettings.OrgName + " </span> <span style='color:Red;'> | </span> <span style='color:#41AADF;'> " + ApplicationSettings.Phone + " </span> <span style='color:Red;'> | </span>  <span style='color:#41AADF;'> Rakesh: 9246111211 </span> <span style='color:Red;'> | </span> <span style='color:#41AADF;'> Ramjan: 8886668940 </span> <span style='color:Red;'> | </span> ";
                // Send Acknowledgement mail
                if (IsTrigger)
                {
                    string ccAdress = string.Empty, bccAdress = string.Empty;
                    if (objCr.VerifySettingsCode("CC"))
                        ccAdress = objCr.GetSettingsValueByCode("CC");
                    if (objCr.VerifySettingsCode("BCC"))
                        bccAdress = objCr.GetSettingsValueByCode("BCC");

                    ccAdress = string.IsNullOrEmpty(CC) ? ccAdress : (string.IsNullOrEmpty(ccAdress) ? ccAdress + CC : ccAdress + "," + CC);
                    bccAdress = string.IsNullOrEmpty(BCC) ? bccAdress : (string.IsNullOrEmpty(bccAdress) ? bccAdress + BCC : bccAdress + "," + BCC);
                    //ccAdress = WebConfigurationManager.AppSettings["CCAddress"].ToString();
                    //bccAdress = WebConfigurationManager.AppSettings["BCCAddress"].ToString();
                    objCr.Sendmail(bookingDet.FromMail, messageText, ccAdress, bccAdress);
                }
                objCr.LoggingEntries("Booking", "Booking has been generated for the booking reference number is " + bookingDet.BookRefNo, User.Identity.Name);
            }
            catch
            {
                return Json(new { success = false, msg = "An error occured while processing your request.", id = 0 });
            }
            return Json(new { success = true, msg = "Booking has been created successfully.", id = bookingDet.Id });
        } 
        #endregion

        #region Edit

        [HttpGet]
        public ActionResult Edit(long Id)
        {
            tbl_bookings bookDet = db.tbl_bookings.Where(a => a.Id == Id).SingleOrDefault();
            tbl_booking_cab_details cabDetails = db.tbl_booking_cab_details.Where(a => a.BookingId == Id).SingleOrDefault();
            tbl_log_sheets logDet = db.tbl_log_sheets.Where(a => a.BoookRefNum.Trim() == bookDet.BookRefNo.Trim()).FirstOrDefault();
            ViewBag.CabDetId = cabDetails == null ? 0 : Convert.ToInt64(cabDetails.Id);
            ViewBag.logId = logDet == null ? 0 : Convert.ToInt64(logDet.ID);
            return View(bookDet);
        }
        // Edit booking details
        [HttpGet]
        public ActionResult EditPartial(long Id)
        {
            tbl_bookings bookDet = db.tbl_bookings.Where(a => a.Id == Id).SingleOrDefault();
            ViewBag.VehicleTypeId = new SelectList(db.tbl_vehicle_types.Where(a => a.Status == true), "ID", "VehicleType", bookDet.VehicleTypeId);
            ViewBag.VehicleModelId = new SelectList(db.tbl_vehicle_models.Where(a => a.Status == true), "ID", "VehicleModelName", bookDet.VehicleModelId);
            ViewBag.TripType = new SelectList(objCr.GetTripTypeList(), "Value", "Text", bookDet.TripType);
            ViewBag.Priority = new SelectList(objCr.GetPriorityList(), "Value", "Text", bookDet.Priority);
            return PartialView("_EditPartial", bookDet);
        }
        [HttpPost]
        public ActionResult Edit(long Id, FormCollection frm, tbl_bookings bookingDet)
        {
            tbl_bookings bookDet = db.tbl_bookings.Where(a => a.Id == Id).SingleOrDefault();
            try
            {
                long ClientId = frm["ClientId"] == null ? 0 : Convert.ToInt64(frm["ClientId"]);
                bookDet.ClientId = ClientId;
                if (bookDet.Status.ToLower() == "edit")
                    bookDet.Status = BookingStatus.Edit.ToString();
                bookDet.Active = true;
                //objCr.ConvertToUppercase(bookingDet);
                TryUpdateModel(bookDet);
                db.SaveChanges(); 
                objCr.LoggingEntries("Booking", "Booking has been updated for the booking reference number is " + bookDet.BookRefNo, User.Identity.Name);
            }
            catch
            {
                return Json(new { success = false, msg = "An error occured while processing your request.", id = 0 });
            }
            return Json(new { success = true, msg = "Booking has been saved successfully.", id = bookDet.Id });
        }

        // Vehicle Cab details
        [HttpGet]
        public ActionResult EditCabDetails(long BookingId, long Id)
        {
            tbl_booking_cab_details cabDetails = db.tbl_booking_cab_details.Where(a => a.Id == Id).SingleOrDefault();
            tbl_bookings bookingDet = db.tbl_bookings.Where(a => a.Id == BookingId).SingleOrDefault();
            ViewBag.VehicleTypeId = new SelectList(db.tbl_vehicle_types.Where(a => a.Status == true), "ID", "VehicleType", bookingDet.VehicleTypeId);
            ViewBag.BookingDet = bookingDet;
            return PartialView("_EditCabDetails", cabDetails);
        }
        [HttpPost]
        public ActionResult EditCabDetails(long Id, FormCollection frm,tbl_booking_cab_details cabDet)
        {
            try
            {
                tbl_booking_cab_details cabDetails = db.tbl_booking_cab_details.Where(a => a.Id == Id).SingleOrDefault();
                TryUpdateModel(cabDetails);
                cabDetails.VehicleId = Convert.ToInt64(frm["vid"]);
                cabDetails.DriverId = Convert.ToInt64(frm["DriverId"]);
                cabDetails.VehicleTypeId = Convert.ToInt64(frm["VehicleTypeId"]);
                cabDetails.ContactNo = frm["ContactNo"];
                db.SaveChanges();
            }
            catch
            {
                return Json(new { success = false , msg = "An error occured while processing your request." });
            }
            return Json(new { success = true , msg = "Cab details has updated successfully" });
        }

        // Edit LogSheet Details

        [HttpGet]
        public ActionResult EditLogSheetDetails(long BookingId, long Id)
        {
            tbl_bookings bookingDet = db.tbl_bookings.Where(a => a.Id == BookingId).SingleOrDefault();
            tbl_log_sheets logDet = db.tbl_log_sheets.Where(a => a.ID == Id).SingleOrDefault();
            ViewBag.VehicleTypeId = new SelectList(db.tbl_vehicle_types.Where(a => a.Status == true), "ID", "VehicleType", logDet.VehicleTypeID);
            ViewBag.TripType = new SelectList(objCr.GetTripTypeList(), "Value", "Text", logDet.TripeType);
            return PartialView("_EditLogSheetDetails", logDet);
        }

        [HttpPost]
        public ActionResult EditLogSheetDetails(long Id,FormCollection frm, tbl_log_sheets _logSheet)
        {
            tbl_log_sheets logDet = db.tbl_log_sheets.Where(a => a.ID == Id).SingleOrDefault();
            try
            {
                tbl_vehicles vehDet = db.tbl_vehicles.Where(a => a.ID == _logSheet.VehicleID).SingleOrDefault();
                string fromLocation = frm["FromLocation"];
                string ToLocation = frm["ToLocation"];
                string Trip = frm["TripType"];
                TimeSpan Time = TimeSpan.Parse(frm["ReportTime"]);
                DateTime ReportDt = Convert.ToDateTime(frm["CreatedDt"]).Add(Time);

                TryUpdateModel(logDet);
                logDet.ModifiedBy = User.Identity.Name;
                logDet.ModifiedDt = DateTime.Now;
                logDet.Status = true;
                logDet.FullLocation = fromLocation + "," + ToLocation;
                logDet.CreatedDt = ReportDt;
                logDet.SeaterID = vehDet.SeaterId;
                logDet.Pax = 1;
                logDet.TripeType = Trip;
                logDet.TravelPax = 1;
                db.SaveChanges();
                objCr.LoggingEntries("LogSheet Mgmt.", "LogSheets was edited with logsheet number " + logDet.LogSheetNum + "", User.Identity.Name);
            }
            catch
            {
                return Json(new { success = false , msg = "An error occured while proccessing your request." });
            }
            return Json(new { success = true , msg = "Logsheet has been saved successfully" });
        }
        
        // Aduit LogSheet
        [HttpGet]
        public ActionResult EditAuditLogSheet(long BookingId, long Id)
        {
            tbl_log_sheets _logDet = db.tbl_log_sheets.Where(a => a.ID == Id).SingleOrDefault();
            tbl_bookings bookingDet = db.tbl_bookings.Where(a => a.Id == BookingId).SingleOrDefault();
            objCr.ConvertToUppercase(_logDet);
            ViewBag.BookingDet = bookingDet;
            return PartialView("_EditAuditLogSheet", _logDet);
        }
        [HttpPost]
        public ActionResult EditAuditLogSheet(long Id,FormCollection frm)
        {
            try
            {
                long logId = Id;
                decimal GrossAmt = Convert.ToDecimal(frm["GrossAmt"]);
                int ExtraKM = string.IsNullOrEmpty(frm["ExtraKM"]) ? 0 : Convert.ToInt32(frm["ExtraKM"]);
                decimal ExtraRate = string.IsNullOrEmpty(frm["ExtraKMRate"]) ? 0 : Convert.ToDecimal(frm["ExtraKMRate"]);
                int ExtraHr = string.IsNullOrEmpty(frm["ExtraHr"]) ? 0 : Convert.ToInt32(frm["ExtraHr"]);
                decimal ExtraHrRate = string.IsNullOrEmpty(frm["ExtraHrRate"]) ? 0 : Convert.ToDecimal(frm["ExtraHrRate"]);
                decimal TollChrg = string.IsNullOrEmpty(frm["TollChrg"]) ? 0 : Convert.ToDecimal(frm["TollChrg"]);
                decimal ParkingChrg = string.IsNullOrEmpty(frm["ParkingChrg"]) ? 0 : Convert.ToDecimal(frm["ParkingChrg"]);
                decimal FuelHike = string.IsNullOrEmpty(frm["FuelHike"]) ? 0 : Convert.ToDecimal(frm["FuelHike"]);
                decimal NetAmount = string.IsNullOrEmpty(frm["NetAmount"]) ? 0 : Convert.ToDecimal(frm["NetAmount"]);
                decimal FinalAmt = string.IsNullOrEmpty(frm["FinalNetAmt"]) ? 0 : Convert.ToDecimal(frm["FinalNetAmt"]);

                tbl_log_sheets _logDet = db.tbl_log_sheets.Where(a => a.ID == logId).SingleOrDefault();

                TryUpdateModel(_logDet);
                _logDet.GrossAmt = GrossAmt;
                _logDet.ExtraHr = ExtraHr;
                _logDet.ExtraKM = ExtraKM;
                _logDet.ExtraHrRate = ExtraHrRate;
                _logDet.ExtraKMRate = ExtraRate;
                _logDet.TollChrg = TollChrg;
                _logDet.ParkingChrg = ParkingChrg;
                _logDet.FuelHike = FuelHike;
                _logDet.NetAmount = NetAmount;
                _logDet.FinalNetAmt = FinalAmt;
                _logDet.AuditDt = DateTime.Now;
                _logDet.AuditedBy = User.Identity.Name;
                _logDet.Status = true;
                db.SaveChanges();
                objCr.LoggingEntries("Booking", "Logsheet has been audited with a logsheet number " + _logDet.LogSheetNum, User.Identity.Name);
            }
            catch
            {
                return Json(new { success = false, msg = "An error occured while proccessing your request." });
            }
            return Json(new { success = true, msg = "Logsheet has been audited successfully." });
        }

        #endregion

        #region Delete
        public ActionResult Delete(long Id)
        {
            try
            {
                tbl_bookings bookDet = db.tbl_bookings.Where(a => a.Id == Id).SingleOrDefault();

                var cabDet = db.tbl_booking_cab_details.Where(a => a.BookingId == Id);
                if (cabDet.Any())
                {
                    var vehDet = db.tbl_booking_cab_details.Where(a => a.BookingId == Id).SingleOrDefault();
                    db.DeleteObject(vehDet);
                }

                var logSheetDet = db.tbl_log_sheets.Where(a => a.Status == true && a.BoookRefNum.Trim() == bookDet.BookRefNo.Trim());
                if (logSheetDet.Any())
                {
                    var logDet = db.tbl_log_sheets.Where(a => a.Status == true && a.BoookRefNum.Trim() == bookDet.BookRefNo.Trim()).FirstOrDefault();
                    db.DeleteObject(logDet);
                }
                db.DeleteObject(bookDet);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch(Exception ex)
            {
                core.ExceptionLogging(ex.Message.ToString(), User.Identity.Name, ex.StackTrace.ToString());
                return RedirectToAction("Index");
            }
        }
        #endregion

        #region Acknowledge Booking
        [HttpPost]
        public ActionResult Acknowledge(long Id)
        {
            tbl_bookings bookDet = db.tbl_bookings.Where(a => a.Id == Id).SingleOrDefault();
            
            try
            {
                bookDet.BookAckDt = DateTime.Now;
                bookDet.BookAckBy = User.Identity.Name;
                bookDet.Status = BookingStatus.Acknowledgement.ToString();
                TryUpdateModel(bookDet);
                db.SaveChanges();
                objCr.LoggingEntries("Booking", "Booking has been acknowledged for the booking reference number is " + bookDet.BookRefNo, User.Identity.Name);
            }
            catch
            {
                return Json(new { success = false, msg = "An error occured while processing your request.", id = 0 });
            }
            return Json(new { success = true, msg = "Booking has acknowledged successfully.", id = bookDet.Id });
        } 
        #endregion
         
        #region Booking Details
        [HttpGet]
        public ActionResult ViewBookingDetails(long Id)
        {
            tbl_bookings bookDet = db.tbl_bookings.Where(a => a.Id == Id).SingleOrDefault();
            ViewBag.LogId = db.tbl_log_sheets.Where(a => a.Status == true && a.BoookRefNum.Trim() == bookDet.BookRefNo.Trim()).FirstOrDefault() == null ? 0 :
                db.tbl_log_sheets.Where(a => a.Status == true && a.BoookRefNum.Trim() == bookDet.BookRefNo.Trim()).FirstOrDefault().ID;
            objCr.ConvertToUppercase(bookDet);
            return View(bookDet);
        }

        public ActionResult VehicleBookDetails(long BookingId)
        {
            tbl_booking_cab_details vehDet = new tbl_booking_cab_details();
            vehDet.BookingId = BookingId;
            tbl_bookings bookDet = db.tbl_bookings.Where(a => a.Id == BookingId).SingleOrDefault();
            ViewBag.VehicleTypeId = new SelectList(db.tbl_vehicle_types.Where(a => a.Status == true), "ID", "VehicleType", bookDet.VehicleTypeId);
            return PartialView("_VehicleBookingDetails", vehDet);
        } 
        #endregion

        #region Confirm Booking
        [HttpPost]
        public ActionResult ConfirmBooking(FormCollection frm)
        {
            long BookingId = frm["BookingId"] == null ? 0 : Convert.ToInt64(frm["BookingId"]);
            try
            {
                string messageText = string.Empty;
                long VehicleId = frm["vid"] == null ? 0 : Convert.ToInt64(frm["vid"]);
                long DriverId = frm["DriverId"] == null ? 0 : Convert.ToInt64(frm["DriverId"]);
                long VehicleTypeId = frm["VehicleTypeId"] == null ? 0 : Convert.ToInt64(frm["VehicleTypeId"]);
                string ContactNo = frm["ContactNo"];
                bool IsTrigger = string.IsNullOrEmpty(frm["IsTrigger"]) ? false : frm.GetValues("IsTrigger").Contains("on");
                // Update cab details
                tbl_booking_cab_details objCab = new tbl_booking_cab_details();
                objCab.VehicleId = VehicleId;
                objCab.BookingId = BookingId;
                objCab.VehicleTypeId = VehicleTypeId;
                objCab.DriverId = DriverId;
                objCab.ContactNo = ContactNo;
                objCab.Date = DateTime.Now;
                objCab.CreatedBy = User.Identity.Name;
                db.tbl_booking_cab_details.AddObject(objCab);
                // Update bookings
                tbl_bookings bookDet = db.tbl_bookings.Where(a => a.Id == BookingId).SingleOrDefault();
                bookDet.BookingMngrDt = DateTime.Now;
                bookDet.BookingMngrBy = User.Identity.Name;
                bookDet.Status = BookingStatus.ConfirmBooking.ToString();
                TryUpdateModel(bookDet);
                db.SaveChanges();

                messageText = "Dear Sir / Madam, <br/> <br/> Greeting’s from " + ApplicationSettings.OrgName + "!!! <br/> <br/> " +
                    " <p style='color:Red;'> PLEASE FIND THE BELOW CAB DETAILS FOR YOUR INFORMATION </p> " +
                    " CAB NUMBER : " + objCab.tbl_vehicles.VehicleRegNum + " <br/> " +
                    " DRIVER NAME : " + objCab.tbl_drivers.FirstName + " " + objCab.tbl_drivers.LastName + " <br/>" +
                    " DRIVER CONTACT NO :" + objCab.ContactNo + " <br/> <br/>" +
                    " <br/> <br/> <br/> <br/>  " +
                    " <span style='color:#41AADF;'> Regards, </span> <br/> " +
                    " <span style='color:#41AADF;'> " + ApplicationSettings.OrgName + " </span> <span style='color:Red;'> | </span> <span style='color:#41AADF;'> " + ApplicationSettings.Phone + " </span> <span style='color:Red;'> | </span>  <span style='color:#41AADF;'> Rakesh: 9246111211 </span> <span style='color:Red;'> | </span> <span style='color:#41AADF;'> Ramjan: 8886668940 </span> <span style='color:Red;'> | </span> ";
                // Send Confirm booking mail
                if (IsTrigger)
                {
                    string ccAdress = string.Empty, bccAdress = string.Empty;
                    if (objCr.VerifySettingsCode("CC"))
                        ccAdress = objCr.GetSettingsValueByCode("CC");
                    if (objCr.VerifySettingsCode("BCC"))
                        bccAdress = objCr.GetSettingsValueByCode("BCC");
                    //ccAdress = WebConfigurationManager.AppSettings["CCAddress"].ToString();
                    //bccAdress = WebConfigurationManager.AppSettings["BCCAddress"].ToString();
                    objCr.Sendmail(bookDet.FromMail, messageText, ccAdress, bccAdress);
                }
                objCr.LoggingEntries("Booking", "Booking has been confirm for the booking reference number is " + bookDet.BookRefNo, User.Identity.Name);

            }
            catch
            {
                return Json(new { success = false, msg = "", id = BookingId });
            }
            return Json(new { success = true, msg = "Booking has been confirmed successfully", id = BookingId });
        } 
        #endregion

        #region Generate Booking LogSheet
        [HttpGet]
        public ActionResult GenerateBookingLogSheet(long BookingId)
        {
            tbl_bookings bookDet = db.tbl_bookings.Where(a => a.Id == BookingId).SingleOrDefault();
            tbl_log_sheets _logSheet = new tbl_log_sheets();
            _logSheet.ClientID = bookDet.ClientId;
            _logSheet.VehicleID = bookDet.tbl_booking_cab_details.FirstOrDefault().VehicleId;
            _logSheet.CreatedDt = bookDet.DateofReport;
            ViewBag.Client = bookDet.tbl_clients.CompanyName;
            ViewBag.ReportTime = bookDet.ReportingTime;
            ViewBag.Vehicle = bookDet.tbl_booking_cab_details.FirstOrDefault().tbl_vehicles.VehicleRegNum;
            _logSheet.GuestName = bookDet.GuestName;
            _logSheet.BoookRefNum = bookDet.BookRefNo;
            ViewBag.FromLocation = bookDet.FromLocation;
            ViewBag.ToLocation = bookDet.ToLocation;
            ViewBag.BookingId = BookingId;
            ViewBag.VehicleTypeId = new SelectList(db.tbl_vehicle_types.Where(a => a.Status == true), "ID", "VehicleType", bookDet.VehicleTypeId);
            ViewBag.TripType = new SelectList(objCr.GetTripTypeList(), "Value", "Text", bookDet.TripType);
            return PartialView("_BookingLogSheet", _logSheet);
        }
        [HttpPost]
        public ActionResult GenerateBookingLogSheet(long BookingId, FormCollection frm, tbl_log_sheets _logSheet)
        {
            try
            {
                tbl_vehicles vehDet = db.tbl_vehicles.Where(a => a.ID == _logSheet.VehicleID).SingleOrDefault();
                string fromLocation = frm["FromLocation"];
                string ToLocation = frm["ToLocation"];
                string Trip = frm["TripType"];
                TimeSpan Time = TimeSpan.Parse(frm["ReportTime"]);
                DateTime ReportDt = Convert.ToDateTime(frm["CreatedDt"]).Add(Time);

                _logSheet.UserName = User.Identity.Name;
                _logSheet.Status = true;
                _logSheet.FullLocation = fromLocation + "," + ToLocation;
                _logSheet.CreatedDt = ReportDt;
                _logSheet.SeaterID = vehDet.SeaterId;
                _logSheet.Pax = 1;
                _logSheet.TripeType = Trip;
                _logSheet.TravelPax = 1;
                _logSheet.LogSheetType = "PACKAGE";
                db.tbl_log_sheets.AddObject(_logSheet);

                // Update Booking 
                tbl_bookings bookDet = db.tbl_bookings.Where(a => a.Id == BookingId).SingleOrDefault();
                bookDet.Status = BookingStatus.LogSheet.ToString();
                TryUpdateModel(bookDet);
                db.SaveChanges();
                objCr.LoggingEntries("Booking", "Logsheet has been generated for the booking reference number is " + bookDet.BookRefNo, User.Identity.Name);
                return Json(new { success = true, msg = "Logsheet has been generated successfully", id = BookingId });
            }
            catch
            {
                return Json(new { success = false, msg = "An error occured while proccessing your request", id = BookingId });
            }
        }
        #endregion

        #region Audit LogSheet
        [HttpGet]
        public ActionResult AuditLogSheet(long BookingId, long logId)
        {
            tbl_log_sheets _logDet = db.tbl_log_sheets.Where(a => a.ID == logId).SingleOrDefault();
            ViewBag.BookingId = BookingId;
            objCr.ConvertToUppercase(_logDet);
            return PartialView("_AuditLogSheet", _logDet);
        }
        [HttpPost]
        public ActionResult AuditLogSheet(FormCollection frm)
        {
            try
            {
                long BookingId = frm["BookingId"] == null ? 0 : Convert.ToInt64(frm["BookingId"]);
                long logId = Convert.ToInt64(frm["Id"]);
                decimal GrossAmt = Convert.ToDecimal(frm["GrossAmt"]);
                int ExtraKM = string.IsNullOrEmpty(frm["ExtraKM"]) ? 0 : Convert.ToInt32(frm["ExtraKM"]);
                decimal ExtraRate = string.IsNullOrEmpty(frm["ExtraKMRate"]) ? 0 : Convert.ToDecimal(frm["ExtraKMRate"]);
                int ExtraHr = string.IsNullOrEmpty(frm["ExtraHr"]) ? 0 : Convert.ToInt32(frm["ExtraHr"]);
                decimal ExtraHrRate = string.IsNullOrEmpty(frm["ExtraHrRate"]) ? 0 : Convert.ToDecimal(frm["ExtraHrRate"]);
                decimal TollChrg = string.IsNullOrEmpty(frm["TollChrg"]) ? 0 : Convert.ToDecimal(frm["TollChrg"]);
                decimal ParkingChrg = string.IsNullOrEmpty(frm["ParkingChrg"]) ? 0 : Convert.ToDecimal(frm["ParkingChrg"]);
                decimal FuelHike = string.IsNullOrEmpty(frm["FuelHike"]) ? 0 : Convert.ToDecimal(frm["FuelHike"]);
                decimal NetAmount = string.IsNullOrEmpty(frm["NetAmount"]) ? 0 : Convert.ToDecimal(frm["NetAmount"]);
                decimal FinalAmt = string.IsNullOrEmpty(frm["FinalNetAmt"]) ? 0 : Convert.ToDecimal(frm["FinalNetAmt"]);
                tbl_log_sheets _logDet = db.tbl_log_sheets.Where(a => a.ID == logId).SingleOrDefault();
                TryUpdateModel(_logDet);
                _logDet.GrossAmt = GrossAmt;
                _logDet.ExtraHr = ExtraHr;
                _logDet.ExtraKM = ExtraKM;
                _logDet.ExtraHrRate = ExtraHrRate;
                _logDet.ExtraKMRate = ExtraRate;
                _logDet.TollChrg = TollChrg;
                _logDet.ParkingChrg = ParkingChrg;
                _logDet.FuelHike = FuelHike;
                _logDet.NetAmount = NetAmount;
                _logDet.FinalNetAmt = FinalAmt;
                _logDet.AuditDt = DateTime.Now;
                _logDet.AuditedBy = User.Identity.Name;
                _logDet.Status = true;
                db.SaveChanges();

                // Update Booking
                tbl_bookings bookDet = db.tbl_bookings.Where(a => a.Id == BookingId).SingleOrDefault();
                TryUpdateModel(bookDet);
                bookDet.Status = BookingStatus.BookingDone.ToString();
                db.SaveChanges();
                objCr.LoggingEntries("Booking", "Logsheet has been audited for the booking reference number is " + bookDet.BookRefNo, User.Identity.Name);
            }
            catch
            {
                return Json(new { success = false, msg = "An error occured while proccessing your request." });
            }
            return Json(new { success = true, msg = "Logsheet has been audited successfully." });
        }
        #endregion

        #region Custom Methods

        public void UpdateBookingReferenceNumber()
        {
            var bookingList = db.tbl_bookings.Where(a => a.Active == true).ToList();
            int i = 0;
            int month = 0;
            string refNo = string.Empty;
            foreach (tbl_bookings bookingDet in bookingList)
            {
                tbl_bookings bookDet = db.tbl_bookings.Where(a => a.Id == bookingDet.Id).SingleOrDefault();
                if (month != bookDet.DateofReport.Value.Month)
                    i = 1;
                refNo = bookDet.DateofReport.Value.ToString("yyMM") + i.ToString("D" + 4);
                i++;
                month = bookDet.DateofReport.Value.Month;
                
                bookDet.BookRefNo = refNo;
                TryUpdateModel(bookDet);
                db.SaveChanges();
            }
        }

        public string GetDriverNumberByDriver(long Id)
        {
            var driverDet = db.tbl_drivers.Where(a => a.ID == Id).SingleOrDefault();
            try
            {
                return driverDet.ContactNumber.Value.ToString();
            }
            catch
            {
                return "";
            }
        }

        public string GetBookingRefNo(DateTime DateofReport, int Prefix)
        {
            string bookingRefNo = string.Empty;
            int number = GetLastBookingSerialNumber(DateofReport);
            string seqNo = number.ToString("D" + Prefix);
            bookingRefNo = DateofReport.Date.ToString("yyMM") + seqNo;
            return bookingRefNo;
        }

        public int GetLastBookingSerialNumber(DateTime DateofReport)
        {
            int i = 0;
            string num = string.Empty;
            try
            {
                var bookingList = db.tbl_bookings.Where(a => a.Active == true && a.DateofReport.Value.Month == DateofReport.Month && a.DateofReport.Value.Year == DateofReport.Date.Year).ToList();
                num = bookingList.LastOrDefault().BookRefNo.Trim().ToString();
                i = num.Length == 8 ? Convert.ToInt32(num.Substring(num.Length - 4)) : i;
                return i + 1;
            }
            catch
            {
                return i;
            }
        }

        #endregion

        #region Booking Mail Configurations

        public ActionResult InboxMails()
        {
            string EmailAddress = string.Empty;
            List<WebEmail> list = ReadMails();
            if (objCr.VerifySettingsCode("SenderId") && objCr.VerifySettingsCode("Password"))
                EmailAddress = objCr.GetSettingsValueByCode("SenderId");
            else
                EmailAddress = WebConfigurationManager.AppSettings["EmailAddress"].ToString();
            ViewBag.EmailAddress = EmailAddress;
            return View(list);
        }

        public ActionResult Refresh()
        {
            Session["ImapClient"] = null;
            return RedirectToAction("InboxMails");
        }

        [ValidateInput(false)]
        public ActionResult Message(int id)
        {
            Mailbox inbox = null;
            if (Session["ImapClient"] == null)
            {
                Imap4Client imap = new Imap4Client();
                imap.ConnectSsl("imap.gmail.com", 993);
                string EmailAddress = string.Empty;
                string PassWord = string.Empty;
                if (objCr.VerifySettingsCode("SenderId") && objCr.VerifySettingsCode("Password"))
                {
                    EmailAddress = objCr.GetSettingsValueByCode("SenderId");
                    PassWord = objCr.GetSettingsValueByCode("Password");
                }
                else
                {
                    EmailAddress = WebConfigurationManager.AppSettings["EmailAddress"].ToString();
                    PassWord = WebConfigurationManager.AppSettings["PassWord"].ToString();
                }
                imap.Login(EmailAddress, PassWord);
                imap.Command("capability");
                inbox = imap.SelectMailbox("inbox");
                Session["ImapClient"] = inbox;
            }
            else
                inbox = (Mailbox)Session["ImapClient"];

            Message message = inbox.Fetch.MessageObject(id);

            WebEmail email = new WebEmail()
                    {
                        MessageNumber = id,
                        Subject = message.Subject,
                        DateSent = message.Date,
                        From = message.From.ToString(),
                        To = message.To.ToString(),
                        CC = string.Join(",", message.Cc.Select(a=>a.Email).ToList()),
                        BCC = string.Join(",", message.Bcc.Select(a=>a.Email).ToList()),
                        Body = message.BodyHtml.Text,
                        SenderId = message.From.Email.ToString()
                    };
            if (message.Attachments.Count > 0)
            {
                foreach (MimePart attachment in message.Attachments)
                {
                    email.FileAttachments.Add(new FileAttachments { Content = attachment.BinaryContent, FileName = attachment.ContentName, ContentType = attachment.ContentType.MimeType });
                }
            }
            var flags = new FlagCollection();
            flags.Add("Seen");
            inbox.RemoveFlagsSilent(id, flags);
            return PartialView("ViewMessage", email);
        }

        public ActionResult MarkAsRead(int id)
        {
            Mailbox inbox = null;
            if (Session["ImapClient"] == null)
            {
                Imap4Client imap = new Imap4Client();
                imap.ConnectSsl("imap.gmail.com", 993);
                string EmailAddress = string.Empty;
                string PassWord = string.Empty;
                if (objCr.VerifySettingsCode("SenderId") && objCr.VerifySettingsCode("Password"))
                {
                    EmailAddress = objCr.GetSettingsValueByCode("SenderId");
                    PassWord = objCr.GetSettingsValueByCode("Password");
                }
                else
                {
                    EmailAddress = WebConfigurationManager.AppSettings["EmailAddress"].ToString();
                    PassWord = WebConfigurationManager.AppSettings["PassWord"].ToString();
                }
                imap.Login(EmailAddress, PassWord);
                imap.Command("capability");
                inbox = imap.SelectMailbox("inbox");
                Session["ImapClient"] = inbox;
            }
            else
                inbox = (Mailbox)Session["ImapClient"];
            
            Message message = inbox.Fetch.MessageObject(id);

            return RedirectToAction("InBoxMails");
        }

        public FileContentResult DownloadFile(string fileName, string fileType)
        {
            string filePath = string.Empty;
            filePath = Path.Combine(Server.MapPath(@"~/InBoxMails/" + fileName));
            var fileContents = System.IO.File.ReadAllBytes(filePath);
            return new FileContentResult(fileContents, fileType) { FileDownloadName = Path.GetFileName(filePath) };
        }

        public List<WebEmail> ReadMails()
        {
            Mailbox inbox = null;
            if (Session["ImapClient"] == null)
            {
                Imap4Client imap = new Imap4Client();
                imap.ConnectSsl("imap.gmail.com", 993);
                string EmailAddress = string.Empty;
                string PassWord = string.Empty;
                if (objCr.VerifySettingsCode("SenderId") && objCr.VerifySettingsCode("Password"))
                {
                    EmailAddress = objCr.GetSettingsValueByCode("SenderId");
                    PassWord = objCr.GetSettingsValueByCode("Password");
                }
                else
                {
                    EmailAddress = WebConfigurationManager.AppSettings["EmailAddress"].ToString();
                    PassWord = WebConfigurationManager.AppSettings["PassWord"].ToString();
                }
                imap.Login(EmailAddress, PassWord);
                imap.Command("capability");
                inbox = imap.SelectMailbox("inbox");
                Session["ImapClient"] = inbox;
            }
            else
                inbox = (Mailbox)Session["ImapClient"];

            List<WebEmail> Emails = new List<WebEmail>();

            int[] ids = inbox.Search("UNSEEN");

            if (ids.Length > 0)
            {
                foreach (int i in ids)
                {
                    Message message = inbox.Fetch.MessageObject(i);
                    
                    WebEmail email = new WebEmail()
                    {
                        MessageNumber = i,
                        Subject = message.Subject,
                        DateSent = message.Date,
                        From = message.From.ToString(),
                        CC = string.Join(",", message.Cc.Select(a => a.Email).ToList()),
                        BCC = string.Join(",", message.Bcc.Select(a => a.Email).ToList()),
                        SenderId = message.From.Email.ToString()
                    };
                    var flags = new FlagCollection();
                    flags.Add("Seen");
                    inbox.RemoveFlagsSilent(i, flags);
                    Emails.Add(email);
                }
            }
            return Emails;
        }

        #endregion

    }

    internal class BookingClass
    {
        public long Id { get; set; }
        public string BookingRef { get; set; }
        public string Client { get; set; }
        public string FromMail { get; set; }
        public string GuestName { get; set; }
        public string BookingDt { get; set; }
        public string ReportDt { get; set; }
        public string ReportTime { get; set; }
        public string VehModel { get; set; }
        public string VehType { get; set; }
        public string Priority { get; set; }
        public string TripType { get; set; }
        public string Status { get; set; }
        public bool Active { get; set; }
    }
}
