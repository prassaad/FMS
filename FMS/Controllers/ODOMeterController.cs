using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;
using System.Reflection;
using System.Text;
using FMS.Helpers;

namespace FMS.Controllers
{
    public class ODOMeterController : Controller
    {

        #region ctors
        private FMSDBEntities db = new FMSDBEntities();
        private core objCr = new core();
        private List<tbl_odometer_readings> _readingList = new List<tbl_odometer_readings>(); 
        #endregion

        public ActionResult Index()
        {
            return View(db.tbl_odometer_readings.ToList());
        }

        #region CREATE OdodmeterReading
        public ActionResult Create()
        {
            return View();
        }
        public ActionResult AddNewOdometerReading()
        {
            var lastReading = db.tbl_odometer_readings.OrderByDescending(r => r.ODMDate).FirstOrDefault();
            DateTime? dt = null;
            dt = lastReading == null ? dt : Convert.ToDateTime(lastReading.ODMDate);
            ViewBag.lastreadingDate = lastReading == null ? "" : String.Format("{0:s}", dt);
            ViewBag.lastUpdated = lastReading == null ? "" : String.Format("{0:d/M/yyyy HH:mm:ss}", lastReading.CreatedDt);
            return PartialView("_AddNewOdometerRange");
        }
        [HttpPost]
        public ActionResult AddOdometerReadingEntry(FormCollection frm, long VID, tbl_odometer_readings odometerReadings)
        {

            tbl_vehicles _vehicle = db.tbl_vehicles.Where(c => c.Status == true && c.ID == VID).FirstOrDefault();
            long ID = 0;
            _readingList = (List<tbl_odometer_readings>)Session["OdometerReadingList"];
            if (_readingList == null)
                _readingList = new List<tbl_odometer_readings>();

            //this is for delete list item based on this id
            if (_readingList.Count() == 0)
                ID = 1;
            else
                ID = _readingList.Count() + 1;
            
            // Add object to list 
            _readingList.Add(new tbl_odometer_readings
            {
                Id = ID,
                VehicleId = VID,
                ModeOfEntry = odometerReadings.ModeOfEntry,
                ODMDate = odometerReadings.ODMDate,
                CreatedDt = DateTime.Now,
                CreatedBy = User.Identity.Name,
                Status = true,
                ReadingValue = odometerReadings.ReadingValue,
            });

            Session["OdometerReadingList"] = _readingList;
            ViewBag.message = "";
            return PartialView("_AddOdometerRangeEntry", _readingList);
        }
        [HttpPost]
        public ActionResult SaveOdometerReadingEntry(FormCollection frm)
        {
            try
            {
                //List of Readings structure 
                List<tbl_odometer_readings> _listofReadings = (List<tbl_odometer_readings>)Session["OdometerReadingList"];
                objCr.ConvertToUppercase<tbl_odometer_readings>(_listofReadings);
                foreach (tbl_odometer_readings _list in _listofReadings)
                {
                    var odoMeterDet = _list;
                    db.tbl_odometer_readings.AddObject(odoMeterDet);
                    db.SaveChanges();
                }
                Session["OdometerReadingList"] = null;
                objCr.LoggingEntries("Odometer reading", "Odometer reading were created ", User.Identity.Name);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Errormessage = ex.Message.ToString();
                return View();
            }
        }

        // Save Reading Entries 

        public int  SaveUnSavedReadingEntries()
        {
            try
            {
                List<tbl_odometer_readings> _listofReadings = (List<tbl_odometer_readings>)Session["OdometerReadingList"];
                objCr.ConvertToUppercase<tbl_odometer_readings>(_listofReadings);
                foreach (tbl_odometer_readings _list in _listofReadings)
                {
                    var odoMeterDet = _list;
                    db.tbl_odometer_readings.AddObject(odoMeterDet);
                    db.SaveChanges();
                }
                Session["OdometerReadingList"] = null;
                objCr.LoggingEntries("Odometer reading", "Odometer reading were created ", User.Identity.Name);
                return 1;
            }
            catch
            {
                return 0;
            }
        }
        public ActionResult RemoveItem(long ID)
        {
            _readingList = (List<tbl_odometer_readings>)Session["OdometerReadingList"];
            foreach (tbl_odometer_readings obj in _readingList)
            {
                if (obj.Id == ID) // Will match once
                {
                    //Delete the Row
                    _readingList.Remove(obj);
                    ReShuffleSlno();
                    break;
                }
            }
            return PartialView("_AddLogSheetEntry", _readingList);
        }

        public void ReShuffleSlno()
        {
            var i = 1;
            foreach (tbl_odometer_readings obj in _readingList)
            {
                obj.Id = i;
                i++;
            }
        }
        public ActionResult checkleastRange(int range, long vehId,long? Id)
        {
            decimal lstVal = 0;
            var odoMeterList = db.tbl_odometer_readings.Where(a => a.VehicleId == vehId).ToList();
            lstVal = Convert.ToDecimal(odoMeterList.Max(a => a.ReadingValue));

            if (Id.HasValue)
            {
                decimal lstMaxVal = 0;
                if (odoMeterList.Where(a => a.Id == Id).Any())
                {
                    var lstodometer = odoMeterList.Except(odoMeterList.Where(a => a.Id == Id)).ToList();
                    lstMaxVal = Convert.ToDecimal(lstodometer.Max(a => a.ReadingValue));
                    if (range < lstMaxVal)
                        return Json(new { success = false, msg = range + " reading value should be above the " + lstMaxVal + " reading" }, JsonRequestBehavior.AllowGet);
                }
            }
            
            if (range < lstVal)
                return Json(new { success = false, msg = range + " reading value should be above the " + lstVal + " reading" }, JsonRequestBehavior.AllowGet);

            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }
        public string getVehicleAjaxResult(string f, string q)
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

        public decimal MaxOdoMeterReading(long VehicleId)
        {
            try
            {
                var odoReading = db.tbl_odometer_readings.Where(a => a.VehicleId == VehicleId).ToList();
                return Convert.ToDecimal(odoReading.Max(a => a.ReadingValue));
            }
            catch
            {
                return 0;
            }
        }
        #endregion CREATE Odometerreading

        #region EDIT OdometerReading
        [HttpGet]
        public ActionResult Edit(int? id)
        {
            try
            {
                var reading = db.tbl_odometer_readings.SingleOrDefault(a => a.Id == id);
                ViewBag.ModeOfEntry = new SelectList(objCr.GetModeOfEntryList(), "Value", "Text", reading.ModeOfEntry);
                return PartialView("_EditPartial",reading);
            }
            catch (Exception ex)
            {

                ViewBag.Errormessage = "An error occured while processing your request" + ex.Message.ToString();
                return View();
            }
           
        }
        [HttpPost]
        public ActionResult Edit(int id,FormCollection frm,tbl_odometer_readings odometerreading)
        {
            var updatereading = db.tbl_odometer_readings.SingleOrDefault(a => a.Id == id);
            try
            {
                TryUpdateModel(updatereading);
                updatereading.Status = true;
                db.SaveChanges();
                return Json(new { success = true , msg = "Odo Meter reading has updated successfully." });
            }
            catch (Exception ex)
            {
                ViewBag.Errormessage = "An error occured while proccessing your request" + ex.Message.ToString();
                return Json(new { success = false, msg = ViewBag.Errormessage });
            }
            
        }
        #endregion
                 
        #region DELETE OdometerReading
        public ActionResult Delete(int id)
        {
            try
            {
                tbl_odometer_readings odometerReading = db.tbl_odometer_readings.SingleOrDefault(a => a.Id == id);
                db.DeleteObject(odometerReading);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {

                ViewBag.Errormessage = "An Error Occured While Processing Your Request" + ex.Message.ToString();
                return View();
            }
           
        }
        #endregion DELETE OdometerReading


        #region Odometer Mobile Entry

        public void SaveOdometerByVehicle(long vid, decimal readingval, string username)
        {
            try
            {
                if (db.tbl_vehicles.Where(a => a.ID == vid && a.Status == true).Any())
                {
                    tbl_odometer_readings odometerDet = new tbl_odometer_readings();
                    odometerDet.VehicleId = vid;
                    odometerDet.ReadingValue = readingval;
                    odometerDet.CreatedBy = username;
                    odometerDet.CreatedDt = DateTime.Now;
                    odometerDet.ModeOfEntry = "MOBILE";
                    odometerDet.ODMDate = DateTime.Now;
                    odometerDet.Status = true;
                    // Insert ODO Meter reading
                    db.tbl_odometer_readings.AddObject(odometerDet);
                    db.SaveChanges();
                }
                Response.Write("Success");
            }
            catch
            {
                Response.Write("Error");
            }
        }
        #endregion

    }
}
