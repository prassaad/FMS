using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;
using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Web.Configuration;
using System.Drawing;
using System.Web.Util;
using System.ComponentModel;
using System.IO;
using FMS.Helpers;
using ClosedXML.Excel;
using System.Data.Objects;
using System.Reflection;

namespace FMS.Controllers
{
    [CustomAuthorizationFilter]
    public class VehicleController : Controller
    {
        #region ctors
        private FMSDBEntities db;
        private core objCore = new core();
        public VehicleController()
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
        public JsonResult GetVehiclesList(int iDisplayStart, int iDisplayLength, string sSearch, int iColumns,
   int iSortingCols, int iSortCol_0, string sSortDir_0,
   int sEcho, string mDataProp_Key)
        {
            var IList = GetEnumerableList(iDisplayStart, iDisplayLength);
            var filteredVehicles = IList
                .Select(l => new[]{
                    l.Id.ToString(),
                    l.Text1.ToString(),
                    l.Text2.ToString(),
                    l.Text3.ToString(),
                    l.Text4.ToString(),
                    l.Text5.ToString(),
                    l.Text6.ToString(),
                    l.Text7.ToString(),
                    l.Status1.ToString(),
                    l.Text8.ToString(),
                    l.Value1.ToString(),
                    l.Value2.ToString(),
                    l.Status2.ToString(),
                    l.Status3.ToString(),
                    l.Status.ToString()
                    
                }).Where(i => string.IsNullOrEmpty(sSearch)
                     || i.Any(y => y.IndexOf(sSearch, StringComparison.InvariantCultureIgnoreCase) >= 0));
            var orderedlist = filteredVehicles
                   .OrderByWithDirection(i => (i[iSortCol_0]).Parse(), sSortDir_0 == "asc").Skip(iDisplayStart).Take(iDisplayLength);
            // db.tbl_vehicles.Where(a => a.Status == true && (a.IsProxy == null || a.IsProxy == false) && (a.Isown == null || a.Isown == false)).Count();
            var model = new
            {
                aaData = orderedlist,
                iTotalDisplayRecords = filteredVehicles.Count(),
                iTotalRecords = filteredVehicles.Count(),
                sEcho = sEcho.ToString()
            };
            return Json(model, JsonRequestBehavior.AllowGet);
        }
        private IEnumerable<CommonFields> GetEnumerableList(int d1, int d2)
        {
            int take = d2 + d1;
            var list = new List<CommonFields>();
            var tempList = (from l in db.tbl_vehicles
                            where l.Status == true && (l.IsProxy == null || l.IsProxy == false) && (l.Isown == null || l.Isown == false)
                            select l).OrderByDescending(a => a.ID).ToList();
            foreach (tbl_vehicles ds in tempList)
            {
                list.Add(new CommonFields
                {
                    Id = ds.ID,
                    Text1 = ds.VehicleRegNum,
                    Text2 = ds.VendorID == null ? "" : ds.tbl_vendor_details.FirstName + " " + ds.tbl_vendor_details.LastName,
                    Text3 = ds.VendorID == null ? "" : ds.tbl_vendor_details.PanCardNumber == null ? "" : ds.tbl_vendor_details.PanCardNumber,
                    Text4 = ds.VehicleModelID == null ? "" : ds.tbl_vehicle_models.VehicleModelName,
                    Text5 = ds.SeaterId == null ? "" : ds.tbl_seaters.Seater,
                    Text6 = ds.tbl_vehicle_types.VehicleType,
                    Text7 = ds.ManufactureYearMonth == null ? "" : ds.ManufactureYearMonth,
                    Status1 = ds.OwnerCumDriver == null ? false : (bool)ds.OwnerCumDriver,
                    Text8 = ds.SingleDoubleDriver == null ? "" : ds.SingleDoubleDriver,
                    Value1 = ds.VendorRate == null ? 0 : (double)ds.VendorRate,
                    Value2 = ds.VendorAcRate == null ? 0 : (double)ds.VendorAcRate,
                    Status2 = IsAuditByVehicleDocs(ds.ID),
                    Status3 = ds.ProfileComplete == null ? false : (bool)ds.ProfileComplete,
                    Status = (bool)ds.Status
                });
            }
            objCore.ConvertToUppercase<CommonFields>(list);
            return list.OrderByDescending(a => a.Id);
        }

        #endregion

        #region Add Vehicle
        [HttpGet]
        public ActionResult AddVehicle()
        {
            // Populate fields into Dropdown from database using viewbag 
            ViewBag.VehicleModelID = new SelectList(db.tbl_vehicle_models.Where(a => a.Status == true), "ID", "VehicleModelName");
            ViewBag.VehicleTypeID = new SelectList(db.tbl_vehicle_types.Where(a => a.Status == true), "ID", "VehicleType");
            ViewBag.SeaterID = new SelectList(db.tbl_seaters.Where(a => a.Status == true), "ID", "Seater");
            ViewBag.VendorID = 0;
            return View();
        }
        [HttpPost]
        public ActionResult AddVehicle([Bind(Exclude = "ID")]FormCollection frm, tbl_vehicles vehicle)
        {
            ViewBag.VehicleModelID = new SelectList(db.tbl_vehicle_models.Where(a => a.Status == true), "ID", "VehicleModelName");
            ViewBag.VehicleTypeID = new SelectList(db.tbl_vehicle_types.Where(a => a.Status == true), "ID", "VehicleType");
            ViewBag.SeaterID = new SelectList(db.tbl_seaters.Where(a => a.Status == true), "ID", "Seater");
            if (vehicle.VendorID == null)
            {
                vehicle.VendorID = frm["VID"] == null ? 0 : Convert.ToInt64(frm["VID"]);
                ViewBag.VendorID = vehicle.VendorID;
            }

            try
            {
                if (ValidateForm(vehicle, true))
                {
                    vehicle.ProfileComplete = IsComplete(vehicle);
                    vehicle.Status = true;
                    objCore.ConvertToUppercase(vehicle);
                    db.tbl_vehicles.AddObject(vehicle);
                    db.SaveChanges();

                    // Assign service schedules to the vehicle if it is own vehicle
                    if ((vehicle.Isown == null ? false : (bool)vehicle.Isown))
                        AutoAssignServiceScheduleToVehicles(vehicle.ID);

                    objCore.LoggingEntries("Vehicle Mgmt.", "vehicle has created with reg number " + vehicle.VehicleRegNum + "", User.Identity.Name);
                    return Content("<script language='javascript' type='text/javascript'>var r=confirm('Are you want to assign driver to this Vehicle?'); if(r==true) {window.location='/Vehicle/EditVehicle/" + vehicle.ID + "';} if(r==false) {window.location='/Vehicle/Index';}</script>");
                }
            }
            catch (Exception)
            {
                return View();
            }
            return View();
        }
        #endregion

        #region Edit Vehicle
        [HttpGet]
        public ActionResult EditVehicle(long Id)
        {
            tbl_vehicles vehicle = db.tbl_vehicles.Where(a => a.Status == true && a.ID == Id).SingleOrDefault();
            // Populate fields into Dropdown from database using viewbag 
            ViewBag.SeaterID = new SelectList(db.tbl_seaters.Where(a => a.Status == true), "ID", "Seater", vehicle.SeaterId);
            ViewBag.VehicleModelID = new SelectList(db.tbl_vehicle_models.Where(a => a.Status == true), "ID", "VehicleModelName", vehicle.VehicleModelID);
            ViewBag.VehicleTypeID = new SelectList(db.tbl_vehicle_types.Where(a => a.Status == true), "ID", "VehicleType", vehicle.VehicleTypeID);
            ViewBag.VendorID = vehicle.VendorID;
            return View(vehicle);
        }
        [HttpPost]
        public ActionResult EditVehicle(long Id, FormCollection frm, tbl_vehicles vehicleDet)
        {
            tbl_vehicles vehicle = db.tbl_vehicles.Where(a => a.Status == true && a.ID == Id).SingleOrDefault();
            // Populate fields into Dropdown from database using viewbag 
            ViewBag.VehicleModelID = new SelectList(db.tbl_vehicle_models.Where(a => a.Status == true), "ID", "VehicleModelName", vehicle.VehicleModelID);
            ViewBag.VehicleTypeID = new SelectList(db.tbl_vehicle_types.Where(a => a.Status == true), "ID", "VehicleType", vehicle.VehicleTypeID);
            ViewBag.SeaterID = new SelectList(db.tbl_seaters.Where(a => a.Status == true), "ID", "Seater", vehicle.SeaterId);
            try
            {
                if (vehicleDet.VendorID == 0 || vehicleDet.VendorID == null)
                {
                    vehicleDet.VendorID = frm["VID"] == null ? 0 : Convert.ToInt64(frm["VID"]);
                    ViewBag.VendorID = vehicleDet.VendorID;
                    vehicle.VendorID = vehicleDet.VendorID;
                }
                if (ValidateForm(vehicleDet, false))
                {
                    // Update Vehicle Details 
                    vehicle.Status = true;
                    vehicle.ProfileComplete = IsComplete(vehicleDet);
                    TryUpdateModel(vehicle);
                    vehicle.VendorID = vehicleDet.VendorID;
                    objCore.ConvertToUppercase(vehicle);
                    db.SaveChanges();
                    objCore.LoggingEntries("Vehicle Mgmt.", "vehicle has edited with reg number " + vehicle.VehicleRegNum + "", User.Identity.Name);
                    return RedirectToAction("Index");
                }
            }
            catch (Exception)
            {
                return View(vehicle);
            }
            return View(vehicle);
        }
        #endregion

        #region Delete Vehicle
        [HttpGet]
        public ActionResult DeleteVehicle(long Id)
        {
            tbl_vehicles vehicle = db.tbl_vehicles.Where(a => a.Status == true && a.ID == Id).SingleOrDefault();
            return View(vehicle);
        }
        [HttpPost]
        public ActionResult DeleteVehicle(long Id, FormCollection frm)
        {
            tbl_vehicles vehicle = db.tbl_vehicles.Where(a => a.Status == true && a.ID == Id).SingleOrDefault();
            try
            {
                vehicle.Status = false;
                TryUpdateModel(vehicle);

                //if (vehicle.IsProxy == true)
                //{
                tbl_vendor_details vendor = db.tbl_vendor_details.Where(a => a.Status == true && a.ID == vehicle.VendorID).FirstOrDefault();
                if (vendor != null)
                    vendor.Status = false;
                tbl_drivers driver = db.tbl_drivers.Where(a => a.Status == true && a.CurrentVehicleID == Id).FirstOrDefault();
                if (driver != null)
                {
                    driver.Status = false;
                    tbl_vehicle_driver vdriver = db.tbl_vehicle_driver.Where(a => a.Status == true && a.VehicleID == Id && a.DriverID == driver.ID).SingleOrDefault();
                    //When ever we delete driver lets delete VehicleDriver Details this time. what happens is driver delete then particular driver Vehicle Relieved from that driver
                    if (vdriver != null)
                    {
                        vdriver.Status = false;
                        vdriver.RelievedOn = DateTime.Now;
                    }
                }
                //}

                List<tbl_vehicle_clients> vehclientList = GetVehicleClientList((vehicle.ID));
                foreach (tbl_vehicle_clients _vehclient in vehclientList)
                    db.DeleteObject(_vehclient);
                db.SaveChanges();
                objCore.LoggingEntries("Vehicle Mgmt.", "vehicle has deleted with reg number " + vehicle.VehicleRegNum + "", User.Identity.Name);
                if (vehicle.IsProxy == false || vehicle.IsProxy == null)
                    return RedirectToAction("Index");
                else
                    return RedirectToAction("ProxyVehicles", "Vehicle");
            }
            catch (Exception)
            {
                return View();
            }
        }
        #endregion

        #region View Vehicle Details

        // View Vehicle and Vendor Details
        [HttpGet]
        public ActionResult Details(long Id)
        {
            tbl_vehicles _vehicleDet = db.tbl_vehicles.Where(a => a.ID == Id && a.Status == true).SingleOrDefault();
            return PartialView("Details", _vehicleDet);
        }

        // Get Vehicle Details By Vehicle 
        public ActionResult GetVehicleDetailsByVehicle(long VehicleId)
        {
            string DriverName = string.Empty, MobileNums = string.Empty;
            tbl_vehicles vehDet = db.tbl_vehicles.Where(a => a.Status == true && a.ID == VehicleId).FirstOrDefault();
            var driverList = db.tbl_drivers.Where(a => a.Status == true && a.CurrentVehicleID == VehicleId).ToList();
            foreach (tbl_drivers objDriver in driverList)
            {
                DriverName += "," + objDriver.FirstName + " " + objDriver.LastName;
                MobileNums += "," + objDriver.ContactNumber;
            }
            if (DriverName != "")
                DriverName = DriverName.Substring(1);
            if (MobileNums != "")
                MobileNums = MobileNums.Substring(1);
            GeneralClassFields obGen = new GeneralClassFields();
            obGen.ID = VehicleId;
            obGen.Text1 = vehDet.VehicleRegNum;
            obGen.Text2 = "[ " + vehDet.tbl_vehicle_models.VehicleModelName + " ] - [ " + vehDet.tbl_vehicle_types.VehicleType + " ] - [ " + (vehDet.SeaterId == null ? "" : vehDet.tbl_seaters.Seater) + " ] ";
            obGen.Text3 = vehDet.tbl_vendor_details.FirstName + " " + vehDet.tbl_vendor_details.LastName;
            obGen.Text4 = DriverName;
            obGen.Text5 = MobileNums;
            return PartialView("_GetVehicleDetailsPartialView", obGen);
        }

        #endregion

        #region ProxyVehicle
        [HttpGet]
        public ActionResult ProxyVehicles()
        {
            return View();
        }
        public JsonResult GetProxyVehiclesList(int iDisplayStart, int iDisplayLength, string sSearch, int iColumns,
       int iSortingCols, int iSortCol_0, string sSortDir_0,
       int sEcho, string mDataProp_Key)
        {
            var IList = GetProxyVehiclesList();
            var filteredLogSheet = IList
                .Select(l => new[]{
                    l.Id.ToString(),
                    l.Text1.ToString(),
                    l.Text2.ToString(),
                    l.Text3.ToString(),
                    l.Text4.ToString(),
                    l.Text5.ToString(),
                    l.Text6.ToString(),
                    l.Text7.ToString(),
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

        private IEnumerable<CommonFields> GetProxyVehiclesList()
        {
            var list = new List<CommonFields>();
            var tbl_proxy_vehicles_list = db.tbl_vehicles.Where(a => a.Status == true && a.IsProxy == true).ToList();
            foreach (tbl_vehicles ds in tbl_proxy_vehicles_list)
            {
                string drivername = "";
                tbl_drivers driverDet = db.tbl_drivers.Where(a => a.Status == true && a.CurrentVehicleID == ds.ID).FirstOrDefault();
                if (driverDet != null)
                {
                    drivername = driverDet.FirstName + " " + driverDet.LastName;
                }

                list.Add(new CommonFields
                {
                    Id = ds.ID,
                    Text1 = ds.VehicleRegNum,
                    Text2 = ds.VendorID == null ? "" : ds.tbl_vendor_details.FirstName + " " + ds.tbl_vendor_details.LastName,
                    Text3 = drivername,
                    Text4 = ds.VendorID == null ? "" : ds.tbl_vendor_details.ContactNumber.ToString(),
                    Text5 = ds.VehicleModelID == null ? "" : ds.tbl_vehicle_models.VehicleModelName,
                    Text6 = ds.SeaterId == null ? "" : ds.tbl_seaters.Seater,
                    Text7 = ds.tbl_vehicle_types.VehicleType,
                    Status = (bool)ds.Status
                });

            }
            objCore.ConvertToUppercase<CommonFields>(list);
            return list.OrderBy(a => a.Id);
        }
        [HttpGet]
        public ActionResult AddProxyVehicle()
        {
            ViewBag.VehicleModelID = new SelectList(db.tbl_vehicle_models.Where(a => a.Status == true), "ID", "VehicleModelName");
            ViewBag.VehicleTypeID = new SelectList(db.tbl_vehicle_types.Where(a => a.Status == true), "ID", "VehicleType");
            ViewBag.SeaterID = new SelectList(db.tbl_seaters.Where(a => a.Status == true), "ID", "Seater");
            return PartialView("AddProxyVehicle");
        }
        [HttpPost]
        public ActionResult AddProxyVehicle(FormCollection frm)
        {
            // Get Fields from Form

            string vehicleReg = frm["VehicleRegNum"] == null ? "" : frm["VehicleRegNum"];
            string vendor = frm["vendor"] == null ? "" : frm["vendor"];
            long VehicleTypeID = frm["VehicleTypeID"] == null ? 0 : Convert.ToInt64(frm["VehicleTypeID"]);
            long VehicleModelID = frm["VehicleModelID"] == null ? 0 : Convert.ToInt64(frm["VehicleModelID"]);
            long SeaterID = frm["SeaterID"] == null ? 0 : Convert.ToInt64(frm["SeaterID"]);
            string Driver = frm["Driver"] == null ? "" : frm["Driver"];
            long Phone = frm["Phone"] == null ? 0 : Convert.ToInt64(frm["Phone"]);

            if (!VerifyVehicleRegNumber(vehicleReg))
            {
                return Json(new { success = false, msg = "Vehicle register number already exists." });
            }
            // Add vendor 

            tbl_vendor_details vendorDet = new tbl_vendor_details();
            vendorDet.FirstName = vendor.ToString().ToUpper();
            vendorDet.LastName = string.Empty;
            vendorDet.Address = string.Empty;
            vendorDet.ContactNumber = Phone;
            vendorDet.SubVendor = false;
            vendorDet.Status = true;

            db.tbl_vendor_details.AddObject(vendorDet);
            db.SaveChanges();

            // Add Vehicle  

            tbl_vehicles vehicleDet = new tbl_vehicles();
            vehicleDet.VehicleRegNum = vehicleReg.ToUpper();
            vehicleDet.VendorID = vendorDet.ID;
            vehicleDet.VehicleTypeID = VehicleTypeID;
            vehicleDet.VehicleModelID = VehicleModelID;
            vehicleDet.SeaterId = SeaterID;
            vehicleDet.IsProxy = true;
            vehicleDet.Status = true;

            db.tbl_vehicles.AddObject(vehicleDet);
            db.SaveChanges();
            long CurrentVehicleID = vehicleDet.ID;

            // Add Driver

            tbl_drivers DriverDet = new tbl_drivers();
            DriverDet.FirstName = Driver.ToString().ToUpper();
            DriverDet.LastName = string.Empty;
            DriverDet.ContactNumber = Phone;
            DriverDet.Address1 = string.Empty;
            DriverDet.CurrentVehicleID = CurrentVehicleID;
            DriverDet.VendorID = vendorDet.ID;
            DriverDet.Status = true;

            db.tbl_drivers.AddObject(DriverDet);
            db.SaveChanges();
            // Assign Driver to vehicle 

            tbl_vehicle_driver vDriver = new tbl_vehicle_driver();
            vDriver.DriverID = DriverDet.ID;
            vDriver.VehicleID = vehicleDet.ID;
            vDriver.IsProxy = true;
            vDriver.AssignedOn = DateTime.Now;
            vDriver.Status = true;
            db.tbl_vehicle_driver.AddObject(vDriver);

            db.SaveChanges();
            objCore.LoggingEntries("Vehicle Mgmt.", "Proxy vehicle has created with reg number " + vehicleDet.VehicleRegNum + "", User.Identity.Name);
            return Json(new { success = true, msg = "Vehicle Added successfully." });
        }
        [HttpGet]
        public ActionResult EditProxyVehicle(long ID)
        {
            tbl_vehicles vehicle = db.tbl_vehicles.Where(a => a.ID == ID && a.Status == true && a.IsProxy == true).SingleOrDefault();
            ViewBag.VendorName = db.tbl_vendor_details.Where(a => a.Status == true && a.ID == vehicle.VendorID).FirstOrDefault().FirstName.ToUpper();
            ViewBag.VehRegNo = vehicle.VehicleRegNum;
            ViewBag.VehicleModelID = new SelectList(db.tbl_vehicle_models.Where(a => a.Status == true), "ID", "VehicleModelName", vehicle.VehicleModelID);
            ViewBag.VehicleTypeID = new SelectList(db.tbl_vehicle_types.Where(a => a.Status == true), "ID", "VehicleType", vehicle.VehicleTypeID);
            ViewBag.SeaterID = new SelectList(db.tbl_seaters.Where(a => a.Status == true), "ID", "Seater", vehicle.SeaterId);
            ViewBag.DriverName = db.tbl_drivers.Where(a => a.Status == true && a.CurrentVehicleID == ID).FirstOrDefault().FirstName.ToUpper();
            ViewBag.PhoneNumber = db.tbl_drivers.Where(a => a.Status == true && a.CurrentVehicleID == ID).FirstOrDefault().ContactNumber;
            ViewBag.VendorID = vehicle.VendorID;
            ViewBag.DriverID = db.tbl_drivers.Where(a => a.Status == true && a.CurrentVehicleID == ID).FirstOrDefault().ID;
            return PartialView("EditProxyVehicle", vehicle);
        }
        [HttpPost]
        public ActionResult EditProxyVehicle(long ID, FormCollection frm)
        {
            // Get Fields from Form 
            // long ID = frm["VehicleID"] == null ? 0 : Convert.ToInt64(frm["VehicleID"]);
            string vehicleReg = frm["VehicleRegNum"] == null ? "" : frm["VehicleRegNum"];
            string vendor = frm["vendor"] == null ? "" : frm["vendor"];
            long VehicleTypeID = frm["VehicleTypeID"] == null ? 0 : Convert.ToInt64(frm["VehicleTypeID"]);
            long VehicleModelID = frm["VehicleModelID"] == null ? 0 : Convert.ToInt64(frm["VehicleModelID"]);
            long SeaterID = frm["SeaterID"] == null ? 0 : Convert.ToInt64(frm["SeaterID"]);
            string Driver = frm["Driver"] == null ? "" : frm["Driver"];
            long Phone = frm["Phone"] == null ? 0 : Convert.ToInt64(frm["Phone"]);
            long DriverID = frm["DriverID"] == null ? 0 : Convert.ToInt64(frm["DriverID"]);
            long VendorID = frm["VendorID"] == null ? 0 : Convert.ToInt64(frm["VendorID"]);

            //Update vendor details

            tbl_vendor_details _vendorDets = db.tbl_vendor_details.Where(a => a.Status == true && a.ID == VendorID).FirstOrDefault();
            _vendorDets.FirstName = vendor.ToString().ToUpper();
            _vendorDets.ContactNumber = Phone;
            _vendorDets.Status = true;
            // TryUpdateModel<tbl_vendor_details>(_vendorDets);
            db.SaveChanges();

            // Update Vehicle Details

            tbl_vehicles _vehicleDets = db.tbl_vehicles.Where(a => a.Status == true && a.ID == ID).FirstOrDefault();
            _vehicleDets.VehicleRegNum = vehicleReg.ToUpper();
            _vehicleDets.VendorID = _vehicleDets.VendorID;
            _vehicleDets.VehicleTypeID = VehicleTypeID;
            _vehicleDets.VehicleModelID = VehicleModelID;
            _vehicleDets.SeaterId = SeaterID;
            _vehicleDets.Status = true;
            // TryUpdateModel<tbl_vehicles>(_vehicleDets);
            db.SaveChanges();


            // Update Driver Details

            tbl_drivers _driverDets = db.tbl_drivers.Where(a => a.Status == true && a.ID == DriverID).FirstOrDefault();
            _driverDets.FirstName = Driver.ToString().ToUpper();
            _driverDets.ContactNumber = Phone;
            _driverDets.CurrentVehicleID = ID;
            _driverDets.VendorID = _driverDets.VendorID;
            _driverDets.Status = true;
            // TryUpdateModel<tbl_drivers>(_driverDets);
            db.SaveChanges();
            objCore.LoggingEntries("Vehicle Mgmt.", "Proxy vehicle has edited with reg number " + _vehicleDets.VehicleRegNum + "", User.Identity.Name);
            //return Json(new { success = true, msg = "Vehicle update successfully." });
            return Json(new { success = true, msg = "Vehicle update successfully" });
        }

        [HttpGet]
        public ActionResult ModifytoPermenentVehicle(long ID)
        {
            tbl_vehicles vehicle = db.tbl_vehicles.Where(a => a.Status == true && a.ID == ID).SingleOrDefault();
            // Populate fields into Dropdown from database using viewbag 
            ViewBag.SeaterID = new SelectList(db.tbl_seaters.Where(a => a.Status == true), "ID", "Seater", vehicle.SeaterId);
            ViewBag.VehicleModelID = new SelectList(db.tbl_vehicle_models.Where(a => a.Status == true), "ID", "VehicleModelName", vehicle.VehicleModelID);
            ViewBag.VehicleTypeID = new SelectList(db.tbl_vehicle_types.Where(a => a.Status == true), "ID", "VehicleType", vehicle.VehicleTypeID);
            ViewBag.VendorID = vehicle.VendorID;
            return View(vehicle);
        }
        [HttpPost]
        public ActionResult ModifytoPermenentVehicle(long ID, FormCollection frm, tbl_vehicles vehicleDet)
        {
            tbl_vehicles vehicle = db.tbl_vehicles.Where(a => a.Status == true && a.ID == ID).SingleOrDefault();
            long CurrentVendorID = (long)vehicle.VendorID;
            // Populate fields into Dropdown from database using viewbag 
            ViewBag.VehicleModelID = new SelectList(db.tbl_vehicle_models.Where(a => a.Status == true), "ID", "VehicleModelName", vehicle.VehicleModelID);
            ViewBag.VehicleTypeID = new SelectList(db.tbl_vehicle_types.Where(a => a.Status == true), "ID", "VehicleType", vehicle.VehicleTypeID);
            ViewBag.SeaterID = new SelectList(db.tbl_seaters.Where(a => a.Status == true), "ID", "Seater", vehicle.SeaterId);
            try
            {
                if (vehicleDet.VendorID == 0 || vehicleDet.VendorID == null)
                {
                    vehicleDet.VendorID = frm["VID"] == null ? 0 : Convert.ToInt64(frm["VID"]);
                    ViewBag.VendorID = vehicleDet.VendorID;
                    vehicle.VendorID = vehicleDet.VendorID;
                }
                else // if we change vendor delete vendor
                {
                    tbl_vendor_details _vendors = db.tbl_vendor_details.Where(a => a.Status == true && a.ID == CurrentVendorID).SingleOrDefault();
                    _vendors.Status = false;
                    ViewBag.VendorID = vehicleDet.VendorID;
                }
                if (ValidateForm(vehicleDet, false))
                {
                    // Update Vehicle Details 
                    vehicle.ProfileComplete = IsComplete(vehicleDet);
                    TryUpdateModel(vehicle);
                    vehicle.IsProxy = false;
                    vehicle.VendorID = vehicleDet.VendorID;
                    objCore.ConvertToUppercase(vehicle);
                    db.SaveChanges();
                    objCore.LoggingEntries("Vehicle Mgmt.", "Proxy vehicle has changed to permenent vehicle with reg number " + vehicle.VehicleRegNum + "", User.Identity.Name);
                    return RedirectToAction("EditVehicle", "Vehicle", new { id = vehicle.ID });
                }
            }
            catch (Exception ex)
            {
                return View(vehicle);
            }
            return View(vehicle);
        }
        #endregion

        #region InactiveVehicle
        public ActionResult InactiveVehicles()
        {
            var lstVeh = db.tbl_vehicles.Where(a => a.Status == false && a.DeactivateDt != null).ToList();
            return View(lstVeh);
        }

        [HttpGet]
        public ActionResult ActivateVehicle(long VehicleId)
        {
            tbl_vehicles vehDet = db.tbl_vehicles.Where(a => a.ID == VehicleId).SingleOrDefault();
            return PartialView("_ActivateVehicle", vehDet);
        }
        [HttpPost]
        public ActionResult ActivateVehicle(long Id, FormCollection frm)
        {
            tbl_vehicles vehDet = db.tbl_vehicles.Where(a => a.ID == Id).SingleOrDefault();
            string Reason = frm["ReasonForActive"];
            try
            {
                vehDet.ActivateDt = DateTime.Now;
                vehDet.ActivateReason = Reason;
                vehDet.ActivatedBy = User.Identity.Name;
                vehDet.Status = true;
                TryUpdateModel(vehDet);
                tbl_vendor_details vendor = db.tbl_vendor_details.Where(a => a.Status == false && a.ID == vehDet.VendorID).SingleOrDefault();
                if (vendor != null)
                    vendor.Status = true;
                tbl_drivers driver = db.tbl_drivers.Where(a => a.Status == false && a.CurrentVehicleID == Id).SingleOrDefault();
                if (driver != null)
                {
                    driver.Status = true;

                    tbl_vehicle_driver vDriver = new tbl_vehicle_driver();
                    vDriver.DriverID = driver.ID;
                    vDriver.VehicleID = vehDet.ID;
                    vDriver.IsProxy = true;
                    vDriver.AssignedOn = DateTime.Now;
                    vDriver.Status = true;
                    db.tbl_vehicle_driver.AddObject(vDriver);
                }
                db.SaveChanges();
            }
            catch
            {
                return Json(new { success = false, msg = "An error occured while processing your request." });
            }
            return Json(new { success = true, msg = "Vehicle has been activated successfully." });
        }

        [HttpGet]
        public ActionResult DeactivateVehicle(long VehicleId)
        {
            tbl_vehicles vehDet = db.tbl_vehicles.Where(a => a.ID == VehicleId).SingleOrDefault();
            return PartialView("_DeactivateVehicle", vehDet);
        }
        [HttpPost]
        public ActionResult DeactivateVehicle(long Id, FormCollection frm)
        {
            tbl_vehicles vehDet = db.tbl_vehicles.Where(a => a.ID == Id).SingleOrDefault();
            string Reason = frm["ReasonForDeactive"];
            try
            {
                vehDet.DeactivateDt = DateTime.Now;
                vehDet.ReasonForDeactive = Reason;
                vehDet.DeactivatedBy = User.Identity.Name;
                vehDet.Status = false;
                TryUpdateModel(vehDet);
                tbl_vendor_details vendor = db.tbl_vendor_details.Where(a => a.Status == true && a.ID == vehDet.VendorID).SingleOrDefault();
                if (vendor != null)
                    vendor.Status = false;
                tbl_drivers driver = db.tbl_drivers.Where(a => a.Status == true && a.CurrentVehicleID == Id).SingleOrDefault();
                if (driver != null)
                {
                    driver.Status = false;
                    tbl_vehicle_driver vdriver = db.tbl_vehicle_driver.Where(a => a.Status == true && a.VehicleID == Id && a.DriverID == driver.ID).SingleOrDefault();
                    ///  When ever we delete driver lets delete VehicleDriver Details this time.
                    ///  what happens is driver delete then particular driver Vehicle Relieved from that driver
                    if (vdriver != null)
                    {
                        vdriver.Status = false;
                        vdriver.RelievedOn = DateTime.Now;
                    }
                }
                db.SaveChanges();
            }
            catch
            {
                return Json(new { success = false, msg = "An error occured while processing your request." });
            }
            return Json(new { success = true, msg = "Vehicle has been deactivated successfully." });
        }
        #endregion

        #region Assigned Vehicle Client Tab
        [HttpGet]
        public ActionResult AssignedVehicleToClient(long VehicleId)
        {
            ViewBag.VehicleId = VehicleId;
            return PartialView("_AssignedVehicleToClient");
        }
        [HttpPost]
        public ActionResult AddClientToList(FormCollection frm)
        {
            long VehicleID = frm["VehicleID"] == null ? 0 : Convert.ToInt64(frm["VehicleID"]);
            long ClientID = frm["cid"] == null ? 0 : Convert.ToInt64(frm["cid"]);
            if (VerifyClientVehicle(VehicleID, ClientID))
            {
                ViewBag.ExceptionMsg = "Client is already exists with this vehicle.Please check and try again";
                return PartialView("_AddedClientToList", GetVehicleClientList(VehicleID));
            }
            tbl_vehicle_clients objvehClient = new tbl_vehicle_clients();
            objvehClient.VehicleID = VehicleID;
            objvehClient.ClientID = ClientID;
            objvehClient.Status = true;
            db.AddTotbl_vehicle_clients(objvehClient);
            db.SaveChanges();
            ViewBag.ExceptionMsg = "Client is successfully added to list";
            objCore.LoggingEntries("Vehicle Mgmt.", "Assigned vehicle reg with " + objCore.GetVehicleRegNumber(VehicleID) + " to the client " + objCore.GetClient(ClientID), User.Identity.Name);
            return PartialView("_AddedClientToList", GetVehicleClientList(VehicleID));
        }
        [HttpGet]
        public ActionResult GetVehicleClient(long VehicleID)
        {
            ViewBag.ExceptionMsg = "";
            return PartialView("_AddedClientToList", GetVehicleClientList(VehicleID));
        }
        // View client vehicle assigned details
        [HttpGet]
        public ActionResult LoadVehicleClient(long VehicleID)
        {
            return PartialView("_LoadVehicleClient", GetVehicleClientList(VehicleID));
        }
        [HttpPost]
        public ActionResult DeleteClientVehicle(long ID)
        {
            tbl_vehicle_clients vehClientDet = db.tbl_vehicle_clients.Where(a => a.ID == ID && a.Status == true).SingleOrDefault();
            long VehicleID = (long)vehClientDet.VehicleID;
            try
            {
                db.tbl_vehicle_clients.DeleteObject(vehClientDet);
                db.SaveChanges();
                ViewBag.ExceptionMsg = "Client is successfully deleted from list";
                objCore.LoggingEntries("Vehicle Mgmt.", "Assigned vehicle reg with " + objCore.GetVehicleRegNumber(VehicleID) + " to the client has deleted.", User.Identity.Name);
                return PartialView("_AddedClientToList", GetVehicleClientList(VehicleID));
            }
            catch (Exception ex)
            {
                ViewBag.ExceptionMsg = "Fail to delete : error message is " + ex.Message.ToString();
                return PartialView("_AddedClientToList", GetVehicleClientList(VehicleID));
            }
        }
        public List<tbl_vehicle_clients> GetVehicleClientList(long VehicleID)
        {
            return db.tbl_vehicle_clients.Where(a => a.Status == true && a.VehicleID == VehicleID).ToList();
        }
        public bool VerifyClientVehicle(long vehicleId, long clientId)
        {
            return db.tbl_vehicle_clients.Where(a => a.VehicleID == vehicleId && a.ClientID == clientId && a.Status == true).Any();
        }
        #endregion

        #region OwnVehicle
        [HttpGet]
        public ActionResult OwnVehicles()
        {
            return View();
        }
        public JsonResult GetOwnVehiclesList(int iDisplayStart, int iDisplayLength, string sSearch, int iColumns,
       int iSortingCols, int iSortCol_0, string sSortDir_0,
       int sEcho, string mDataProp_Key)
        {
            var IList = GetOwnVehiclesList();
            var filteredLogSheet = IList
                .Select(l => new[]{
                    l.Id.ToString(),
                    l.Text1.ToString(),
                    l.Text2.ToString(),
                    l.Text3.ToString(),
                    l.Text4.ToString(),
                    l.Text5.ToString(),
                    l.Text6.ToString(),
                    l.Text7.ToString(),
                    l.Status1.ToString(),
                    l.Text8.ToString(),
                    l.Value1.ToString(),
                    l.Value2.ToString(),
                    l.Status2.ToString(),
                    l.Status3.ToString(),
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
        private IEnumerable<CommonFields> GetOwnVehiclesList()
        {
            var list = new List<CommonFields>();
            var tbl_own_vehicles_list = db.tbl_vehicles.Where(a => a.Status == true && a.Isown == true).ToList();
            foreach (tbl_vehicles ds in tbl_own_vehicles_list)
            {
                string drivername = "";
                tbl_drivers driverDet = db.tbl_drivers.Where(a => a.Status == true && a.CurrentVehicleID == ds.ID).FirstOrDefault();
                if (driverDet != null)
                {
                    drivername = driverDet.FirstName + " " + driverDet.LastName;
                }

                list.Add(new CommonFields
                {
                    Id = ds.ID,
                    Text1 = ds.VehicleRegNum,
                    Text2 = ds.VendorID == null ? "" : ds.tbl_vendor_details.FirstName + " " + ds.tbl_vendor_details.LastName,
                    Text3 = ds.VendorID == null ? "" : ds.tbl_vendor_details.PanCardNumber == null ? "" : ds.tbl_vendor_details.PanCardNumber,
                    Text4 = ds.VehicleModelID == null ? "" : ds.tbl_vehicle_models.VehicleModelName,
                    Text5 = ds.SeaterId == null ? "" : ds.tbl_seaters.Seater,
                    Text6 = ds.tbl_vehicle_types.VehicleType,
                    Text7 = ds.ManufactureYearMonth == null ? "" : ds.ManufactureYearMonth,
                    Status1 = ds.OwnerCumDriver == null ? false : (bool)ds.OwnerCumDriver,
                    Text8 = ds.SingleDoubleDriver == null ? "" : ds.SingleDoubleDriver,
                    Value1 = ds.VendorRate == null ? 0 : (double)ds.VendorRate,
                    Value2 = ds.VendorAcRate == null ? 0 : (double)ds.VendorAcRate,
                    Status2 = IsAuditByVehicleDocs(ds.ID),
                    Status3 = ds.ProfileComplete == null ? false : (bool)ds.ProfileComplete,
                    Status = (bool)ds.Status
                });

            }
            objCore.ConvertToUppercase<CommonFields>(list);
            return list.OrderBy(a => a.Id);
        }
        public ActionResult AddOwnVehicleFinanceView(long id, string VehRegNum)
        {
            ViewBag.VehicleId = id;
            ViewBag.VehRegNum = VehRegNum;
            ViewBag.VehDet = db.tbl_vehicles.Where(a => a.ID == id).SingleOrDefault();
            var hypothicatedDet = db.tbl_veh_hypothicated.Where(a => a.VehicleId == id).FirstOrDefault();
            return PartialView("_AddOwnVehicleFinanceView", hypothicatedDet);
        }
        [HttpPost]
        public ActionResult AddOwnVehicleFinanceView(tbl_veh_hypothicated hypothicatedDet)
        {
            try
            {
                if (!db.tbl_veh_hypothicated.Where(a => a.VehicleId == hypothicatedDet.VehicleId).Any())
                {
                    tbl_vehicles vehDet = db.tbl_vehicles.Where(a => a.ID == hypothicatedDet.VehicleId).SingleOrDefault();
                    //var TotAmt = hypothicatedDet.DownPayment == null ? 0 : hypothicatedDet.DownPayment + hypothicatedDet.GPSCost == null ? 0 : hypothicatedDet.GPSCost + hypothicatedDet.AccressCost == null ? 0 : hypothicatedDet.AccressCost;
                    //hypothicatedDet.TotalAmt = TotAmt;
                    hypothicatedDet.CreatedBy = User.Identity.Name;
                    hypothicatedDet.CreatedOn = DateTime.Now;
                    db.tbl_veh_hypothicated.AddObject(hypothicatedDet);
                    db.SaveChanges();
                    objCore.LoggingEntries("Vehicle Mgmt.", "vehicle finance has added to the vehicle " + vehDet.VehicleRegNum + "", User.Identity.Name);
                }
            }
            catch
            {
                return Json(new { success = false, msg = "An error occured while processing your request." });
            }
            return Json(new { success = true, msg = "Vehicle finance has been added successfully." });
        }
        public ActionResult EditOwnVehicleFinanceView(long VehicleID)
        {
            var hypothicatedDet = db.tbl_veh_hypothicated.Where(a => a.VehicleId == VehicleID).SingleOrDefault();
            return PartialView("_EditOwnVehicleFinanceView", hypothicatedDet);
        }
        [HttpPost]
        public ActionResult EditOwnVehicleFinanceView(tbl_veh_hypothicated hypothicatedDet)
        {
            try
            {
                var OldHypDet = db.tbl_veh_hypothicated.Where(a => a.HId == hypothicatedDet.HId).Single();
                tbl_vehicles vehDet = db.tbl_vehicles.Where(a => a.ID == OldHypDet.VehicleId).SingleOrDefault();
                TryUpdateModel(OldHypDet);
                OldHypDet.TotalAmt = hypothicatedDet.TotalAmt;
                db.SaveChanges();
                objCore.LoggingEntries("Vehicle Mgmt.", "vehicle finance has updated to the vehicle " + vehDet.VehicleRegNum + "", User.Identity.Name);
            }
            catch
            {
                return Json(new { success = false, msg = "An error occured while processing your request." });
            }
            return Json(new { success = true, msg = "Vehicle finance has been updated successfully." });
        }
        [HttpGet]
        public ActionResult DeleteOwnVehicle(long Id)
        {
            tbl_vehicles vehicle = db.tbl_vehicles.Where(a => a.Isown == true && a.ID == Id).SingleOrDefault();
            return View(vehicle);
        }
        [HttpPost]
        public ActionResult DeleteOwnVehicle(long Id, FormCollection frm)
        {
            tbl_vehicles vehicle = db.tbl_vehicles.Where(a => a.Isown == true && a.ID == Id).SingleOrDefault();
            try
            {
                vehicle.Status = false;
                TryUpdateModel(vehicle);

                //if (vehicle.IsOwn == true)
                //{
                tbl_vendor_details vendor = db.tbl_vendor_details.Where(a => a.Status == true && a.ID == vehicle.VendorID).SingleOrDefault();
                if (vendor != null)
                    vendor.Status = false;
                tbl_drivers driver = db.tbl_drivers.Where(a => a.Status == true && a.CurrentVehicleID == Id).SingleOrDefault();
                if (driver != null)
                {
                    driver.Status = false;
                    tbl_vehicle_driver vdriver = db.tbl_vehicle_driver.Where(a => a.Status == true && a.VehicleID == Id && a.DriverID == driver.ID).SingleOrDefault();
                    //When ever we delete driver lets delete VehicleDriver Details this time. what happens is driver delete then particular driver Vehicle Relieved from that driver
                    if (vdriver != null)
                    {
                        vdriver.Status = false;
                        vdriver.RelievedOn = DateTime.Now;
                    }
                }
                //}

                List<tbl_vehicle_clients> vehclientList = GetVehicleClientList((vehicle.ID));
                foreach (tbl_vehicle_clients _vehclient in vehclientList)
                    db.DeleteObject(_vehclient);
                db.SaveChanges();
                objCore.LoggingEntries("Vehicle Mgmt.", "vehicle has deleted with reg number " + vehicle.VehicleRegNum + "", User.Identity.Name);
                if (vehicle.IsProxy == false || vehicle.IsProxy == null)
                    return RedirectToAction("Index");
                else
                    return RedirectToAction("OwnVehicles", "Vehicle");
            }
            catch (Exception)
            {
                return View();
            }
        }

        public int GetTotalMonthsBetweenDates(string start, string end)
        {
            int Months = 0;
            try
            {
                DateTime? startDt = null; DateTime? endDt = null;
                startDt = string.IsNullOrEmpty(start) ? startDt : Convert.ToDateTime(start);
                endDt = string.IsNullOrEmpty(end) ? endDt : Convert.ToDateTime(end);
                Months = (endDt.Value.Month - startDt.Value.Month) + 12 * (endDt.Value.Year - startDt.Value.Year);
                Months++;
            }
            catch
            {
                return 0;
            }
            return Months;
        }
        #endregion OwnVehicle

        #region Partially called actions in vendor management
        [HttpGet]
        public ActionResult CreatePartial(long VendorId)
        {
            ViewBag.VendorID = VendorId; //new SelectList(db.tbl_vendor_details.Where(a => a.Status == true), "ID", "FirstName", VendorId);
            ViewBag.VehicleModelID = new SelectList(db.tbl_vehicle_models.Where(a => a.Status == true), "ID", "VehicleModelName");
            ViewBag.VehicleTypeID = new SelectList(db.tbl_vehicle_types.Where(a => a.Status == true), "ID", "VehicleType");
            ViewBag.SeaterID = new SelectList(db.tbl_seaters.Where(a => a.Status == true), "ID", "Seater");
            return PartialView("_CreateVehiclePartial");
        }
        [HttpPost]
        public ActionResult Create([Bind(Exclude = "ID")]FormCollection frm, tbl_vehicles vehicle)
        {
            // Populate fields into Dropdown from database using viewbag 
            ViewBag.VendorID = frm["VendorID"] == null ? 0 : Convert.ToInt64(frm["VendorID"]); //new SelectList(db.tbl_vendor_details.Where(a => a.Status == true), "ID", "FirstName");
            ViewBag.VehicleModelID = new SelectList(db.tbl_vehicle_models.Where(a => a.Status == true), "ID", "VehicleModelName");
            ViewBag.VehicleTypeID = new SelectList(db.tbl_vehicle_types.Where(a => a.Status == true), "ID", "VehicleType");
            ViewBag.SeaterID = new SelectList(db.tbl_seaters.Where(a => a.Status == true), "ID", "Seater");
            try
            {
                if (ValidateForm(vehicle, true))
                {
                    vehicle.ProfileComplete = IsComplete(vehicle);
                    vehicle.Status = true;
                    db.tbl_vehicles.AddObject(vehicle);
                    db.SaveChanges();
                    objCore.LoggingEntries("Vehicle Mgmt.", "Added new vehicle with the reg number " + vehicle.VehicleRegNum, User.Identity.Name);
                    return Content("<script language='javascript' type='text/javascript'>$.modal.close();alert('Vehicle added successfully with a registration number " + vehicle.VehicleRegNum + "');VehiclesList('" + vehicle.VendorID + "');</script>");
                    //return Json(new { success = true, msg = "Vehicle added successfully with a registration number " + vehicle.VehicleRegNum + " " });
                }
            }
            catch (Exception)
            {
                return PartialView("_CreateVehiclePartial", vehicle);
            }
            return PartialView("_CreateVehiclePartial", vehicle);
        }
        // View Vehicle Details
        [HttpGet]
        public ActionResult ViewVehicleDetails(long Id)
        {
            tbl_vehicles vehicle = db.tbl_vehicles.Where(a => a.ID == Id && a.Status == true).SingleOrDefault();
            ViewBag.VendorID = new SelectList(db.tbl_vendor_details.Where(a => a.Status == true), "ID", "FirstName", vehicle.VendorID);
            ViewBag.VehicleModelID = new SelectList(db.tbl_vehicle_models.Where(a => a.Status == true), "ID", "VehicleModelName", vehicle.VehicleModelID);
            ViewBag.VehicleTypeID = new SelectList(db.tbl_vehicle_types.Where(a => a.Status == true), "ID", "VehicleType", vehicle.VehicleTypeID);
            ViewBag.SeaterID = new SelectList(db.tbl_seaters.Where(a => a.Status == true), "ID", "Seater", vehicle.SeaterId);
            return PartialView("_ViewVehicleDetails", vehicle);
        }
        [HttpPost]
        public ActionResult Edit(long Id, FormCollection frm, tbl_vehicles vehicleDet)
        {
            tbl_vehicles vehicle = db.tbl_vehicles.Where(a => a.Status == true && a.ID == Id).SingleOrDefault();
            // Populate fields into Dropdown from database using viewbag 
            ViewBag.VendorID = new SelectList(db.tbl_vendor_details.Where(a => a.Status == true), "ID", "FirstName", vehicle.VendorID);
            ViewBag.VehicleModelID = new SelectList(db.tbl_vehicle_models.Where(a => a.Status == true), "ID", "VehicleModelName", vehicle.VehicleModelID);
            ViewBag.VehicleTypeID = new SelectList(db.tbl_vehicle_types.Where(a => a.Status == true), "ID", "VehicleType", vehicle.VehicleTypeID);
            ViewBag.SeaterID = new SelectList(db.tbl_seaters.Where(a => a.Status == true), "ID", "Seater", vehicle.SeaterId);

            try
            {
                if (ValidateForm(vehicleDet, false))
                {
                    // Update Vehicle Details 
                    vehicle.Status = true;
                    vehicle.ProfileComplete = IsComplete(vehicleDet);
                    TryUpdateModel(vehicle);
                    db.SaveChanges();
                    objCore.LoggingEntries("Vehicle Mgmt.", "Updated vehicle with the reg number " + vehicle.VehicleRegNum, User.Identity.Name);
                    return Content("<script language='javascript' type='text/javascript'>$.modal.close();alert('Vehicle Updated successfully.');VehiclesList('" + vehicle.VendorID + "');</script>");
                    //return Json(new { success = true, msg = "Updated successfully." });
                }
            }
            catch (Exception)
            {
                return PartialView("_ViewVehicleDetails", vehicleDet);
            }
            return PartialView("_ViewVehicleDetails", vehicleDet);
        }
        [HttpPost]
        public ActionResult Delete(long Id, FormCollection frm)
        {
            tbl_vehicles vehicle = db.tbl_vehicles.Where(a => a.Status == true && a.ID == Id).SingleOrDefault();
            try
            {
                vehicle.Status = false;
                TryUpdateModel(vehicle);
                db.SaveChanges();
                objCore.LoggingEntries("Vehicle Mgmt.", "Deleted vehicle with the reg number " + vehicle.VehicleRegNum, User.Identity.Name);
                return Json(new { success = true, msg = "Vehicle was deleted Successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, msg = "Fail to delete vehicle.Error message is " + ex.Message + "" });
            }
        }
        public ActionResult GetVehicleListByVendor(long VendorId)
        {
            core objCore = new core();
            List<tbl_vehicles> vehicleList = objCore.GetVehiclesList(VendorId);
            return PartialView("_GetVehicleListPartialView", vehicleList);
        }
        #endregion

        #region Vehicle Documents
        [HttpGet]
        public ActionResult Documents()
        {
            ViewBag.DocTypeID = new SelectList(db.tbl_documents.Where(a => a.Status == true), "ID", "DocumentType");
            return View();
        }
        [HttpPost]
        public ActionResult Documents(FormCollection frm, HttpPostedFileBase DocPath, tbl_doc_vehicle_nodes vehDocs)
        {
            ViewBag.DocTypeID = new SelectList(db.tbl_documents.Where(a => a.Status == true), "ID", "DocumentType", vehDocs.DocTypeID);
            long VehicleID = Convert.ToInt64(frm["vid"]);
            vehDocs.VehicleID = VehicleID;
            ViewBag.VehicleID = VehicleID;
            ViewBag.VehicleRegNum = db.tbl_vehicles.Where(a => a.Status == true && a.ID == VehicleID).SingleOrDefault().VehicleRegNum.ToString();
            tbl_documents docs = db.tbl_documents.Where(d => d.Status == true && d.ID == vehDocs.DocTypeID).FirstOrDefault();
            try
            {
                if (DocPath != null && DocPath.ContentLength > 0)
                {
                    DirectoryInfo thisFolder = new DirectoryInfo(Server.MapPath("../Content/Documents/Vehicle/"));
                    if (!thisFolder.Exists)
                    {
                        thisFolder.Create();
                    }
                    string ext = Path.GetExtension(DocPath.FileName);
                    string filename = Path.GetFileNameWithoutExtension(DocPath.FileName);
                    vehDocs.DocPath = "VehDoc" + "_" + docs.DocCode + "_" + +vehDocs.VehicleID + ext;
                    var path = Path.Combine(Server.MapPath("../Content/Documents/Vehicle/"), vehDocs.DocPath);
                    DocPath.SaveAs(path);
                }
                vehDocs.Status = true;
                db.tbl_doc_vehicle_nodes.AddObject(vehDocs);
                db.SaveChanges();
                objCore.LoggingEntries("Vehicle Mgmt.", "New document(s) has uploaded to the vehicle " + vehDocs.tbl_vehicles.VehicleRegNum, User.Identity.Name);
                return Content("<script language='javascript' type='text/javascript'>var r=confirm('Are you want to add one more document?'); if(r==true) {window.location='/Vehicle/Documents';} if(r==false) {window.location='/Vehicle/Index';}</script>");

            }
            catch (Exception ex)
            {
                return View("Documents", vehDocs);
            }
        }

        [HttpGet]
        public ActionResult LoadDocumentsListByVehicle(long vehicleID)
        {
            List<tbl_doc_vehicle_nodes> _lstvehNodes = db.tbl_doc_vehicle_nodes.Where(v => v.Status == true && v.VehicleID == vehicleID).ToList();
            ViewBag.VehicleID = vehicleID;
            if (_lstvehNodes.Count > 0)
                return PartialView("_DocumentsListByVehicle", _lstvehNodes);
            else
                return PartialView("_NoDocuments");
        }

        //For send mail with multiple uploads
        [HttpPost]
        public ActionResult SendMail(FormCollection frm, long VehicleID)
        {
            List<tbl_doc_vehicle_nodes> _lstvehNodes = db.tbl_doc_vehicle_nodes.Where(v => v.Status == true && v.VehicleID == VehicleID).ToList();

            string EmailAddress = WebConfigurationManager.AppSettings["EmailAddress"].ToString();
            string PassWord = WebConfigurationManager.AppSettings["PassWord"].ToString();

            String str = String.Empty;
            System.Net.Mail.MailMessage mail = new System.Net.Mail.MailMessage();
            SmtpClient smtp = new SmtpClient();
            string sendto = frm["mail"].ToString().Trim();
            string subject = frm["txtSubject"].ToString().Trim();
            string body = frm["txtBody"].ToString().Trim();
            try
            {
                mail.To.Add(sendto);
                mail.From = new MailAddress(EmailAddress, "Vehicle Documents");
                mail.Subject = subject;
                //Multiple File Attachment
                int docCnt = 0;
                for (int i = 0; i < _lstvehNodes.Count; i++)
                {
                    bool isChecked = frm.GetValues("chkdoc_" + _lstvehNodes[i].ID).Contains("true"); //Getting all checkboxes values
                    long DocID = Convert.ToInt64(_lstvehNodes[i].ID);
                    if (isChecked == true)
                    {
                        tbl_doc_vehicle_nodes _vdocs = db.tbl_doc_vehicle_nodes.Where(d => d.Status == true && d.ID == DocID).FirstOrDefault();
                        string Uplodefile = Request.PhysicalApplicationPath + "Content\\Documents\\Vehicle\\" + _vdocs.DocPath;
                        mail.Attachments.Add(new Attachment(Uplodefile));
                        docCnt += 1;
                        //string[] S1 = Directory.GetFiles(Uplodefile);
                        //foreach (string fileName in S1)
                        //{
                        //    mail.Attachments.Add(new Attachment(fileName));
                        //}
                    }
                }
                if (docCnt == 0)
                {
                    return Content("<script language='javascript' type='text/javascript'>alert('Please select atleast on document.');ViewVehicleDocuments('" + VehicleID + "'); $('#divEMailDet').show();</script>");
                }
                mail.Body = body;
                mail.IsBodyHtml = true;
                mail.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;
                smtp.EnableSsl = true;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential(EmailAddress, PassWord);
                smtp.Host = "smtp.gmail.com";
                smtp.Port = 587;
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                mail.Headers.Add("Disposition-Notification-To", EmailAddress);
                //smtp.SendCompleted += new SendCompletedEventHandler(SendCompletedCallback);
                smtp.Send(mail);
                return Content("<script language='javascript' type='text/javascript'>alert('Mail send successfully');ViewVehicleDocuments('" + VehicleID + "');</script>");

            }
            catch (Exception)
            {
                return Content("<script language='javascript' type='text/javascript'>alert('Delivery to the following recipient failed permanently'" + sendto + "'); ViewVehicleDocuments('" + VehicleID + "');</script>");
            }
            //return Content("<script language='javascript' type='text/javascript'>ViewVehicleDocuments('" + VehicleID + "');</script>");

        }
        [HttpGet]
        public ActionResult DeleteVehicleDocuments(long ID)
        {
            tbl_doc_vehicle_nodes _vdocs = db.tbl_doc_vehicle_nodes.Where(a => a.Status == true && a.ID == ID).SingleOrDefault();
            long VehicleID = (long)_vdocs.VehicleID;
            try
            {
                _vdocs.Status = false;
                TryUpdateModel(_vdocs);
                db.SaveChanges();
                return Content("<script language='javascript' type='text/javascript'>alert('Document delete successfully');ViewVehicleDocuments('" + VehicleID + "');</script>");

            }
            catch (Exception ex)
            {
                return View();
            }

        }
        [HttpPost]
        public ActionResult AuditDocments(FormCollection frm, long VehicleID)
        {
            List<tbl_doc_vehicle_nodes> _lstvehNodes = db.tbl_doc_vehicle_nodes.Where(v => v.Status == true && v.VehicleID == VehicleID).ToList();
            int docCnt = 0;
            for (int i = 0; i < _lstvehNodes.Count; i++)
            {
                bool isChecked = frm.GetValues("chkdoc_" + _lstvehNodes[i].ID).Contains("true"); //Getting all checkboxes values
                long DocID = Convert.ToInt64(_lstvehNodes[i].ID);
                if (isChecked == true)
                {
                    tbl_doc_vehicle_nodes _vdocs = db.tbl_doc_vehicle_nodes.Where(d => d.Status == true && d.ID == DocID).FirstOrDefault();
                    _vdocs.AuditedBy = User.Identity.Name.ToUpper();
                    _vdocs.Audited = true;
                    db.SaveChanges();
                    docCnt += 1;
                }
            }
            if (docCnt == 0)
            {
                return Content("<script language='javascript' type='text/javascript'>alert('Please select atleast one document.');ViewVehicleDocuments('" + VehicleID + "');</script>");
            }
            objCore.LoggingEntries("Vehicle Mgmt.", "Document(s) has Approved to the vehicle " + objCore.GetVehicleRegNumber(VehicleID), User.Identity.Name);
            return Content("<script language='javascript' type='text/javascript'>alert('Selected Documents are Audited successfully.');ViewVehicleDocuments('" + VehicleID + "');</script>");

        }
        [HttpGet]
        public ActionResult AddDocumentstoVehicle(long VehicleID)
        {
            List<tbl_documents> _lstdocs = db.tbl_documents.Where(a => a.Status == true).ToList();
            ViewBag.VehicleID = VehicleID;
            return PartialView("_AddDocuments", _lstdocs);
        }
        [HttpPost]
        public ActionResult AddDocumentstoVehicle(FormCollection frm)
        {
            List<tbl_documents> _lstvehdocs = db.tbl_documents.Where(a => a.Status == true).ToList();
            long VehicleID = Convert.ToInt64(frm["hdnVehicleID"]);

            for (int i = 0; i < _lstvehdocs.Count; i++)
            {
                bool isChecked = frm.GetValues("chkdoc_" + _lstvehdocs[i].ID).Contains("true"); //Getting all checkboxes values
                long DocID = Convert.ToInt64(_lstvehdocs[i].ID);
                DateTime? Validity = null;
                Validity = (frm["validty_" + _lstvehdocs[i].ID] == "" || frm["validty_" + _lstvehdocs[i].ID] == null) ? Validity : Convert.ToDateTime(frm["validty_" + _lstvehdocs[i].ID]);

                tbl_documents _docs = db.tbl_documents.Where(a => a.Status == true && a.ID == DocID).FirstOrDefault();
                tbl_doc_vehicle_nodes vehDocs = new tbl_doc_vehicle_nodes();
                if (isChecked == true)
                {
                    // Check Document if already added in list
                    tbl_doc_vehicle_nodes _vdocs = db.tbl_doc_vehicle_nodes.Where(a => a.Status == true && a.DocTypeID == DocID && a.VehicleID == VehicleID).FirstOrDefault();
                    if (_vdocs != null)
                    {
                        HttpPostedFileBase file = Request.Files["file_" + DocID];
                        if (file != null && file.ContentLength > 0)
                        {
                            DirectoryInfo thisFolder = new DirectoryInfo(Server.MapPath("../Content/Documents/Vehicle/"));
                            if (!thisFolder.Exists)
                            {
                                thisFolder.Create();
                            }
                            string ext = Path.GetExtension(file.FileName);
                            string filename = Path.GetFileNameWithoutExtension(file.FileName);
                            _vdocs.DocPath = "VehDoc" + "_" + _docs.DocCode + "_" + VehicleID + ext;
                            var path = Path.Combine(Server.MapPath("../Content/Documents/Vehicle/"), _vdocs.DocPath);
                            file.SaveAs(path);
                        }

                        TryUpdateModel<tbl_doc_vehicle_nodes>(_vdocs);
                        _vdocs.Validity = Validity;
                        db.SaveChanges();

                        // Update into vehicle Table 
                        UpdateValidtyField(VehicleID, _lstvehdocs[i].DocumentType.ToUpper(), Validity);
                    }
                    else
                    {
                        HttpPostedFileBase file = Request.Files["file_" + DocID];
                        if (file != null && file.ContentLength > 0)
                        {
                            DirectoryInfo thisFolder = new DirectoryInfo(Server.MapPath("../Content/Documents/Vehicle/"));
                            if (!thisFolder.Exists)
                            {
                                thisFolder.Create();
                            }
                            string ext = Path.GetExtension(file.FileName);
                            string filename = Path.GetFileNameWithoutExtension(file.FileName);
                            vehDocs.DocPath = "VehDoc" + "_" + _docs.DocCode + "_" + VehicleID + ext;
                            var path = Path.Combine(Server.MapPath("../Content/Documents/Vehicle/"), vehDocs.DocPath);
                            file.SaveAs(path);
                        }
                        vehDocs.Validity = Validity;
                        vehDocs.DocTypeID = DocID;
                        vehDocs.VehicleID = VehicleID;
                        vehDocs.Status = true;
                        db.tbl_doc_vehicle_nodes.AddObject(vehDocs);
                        db.SaveChanges();
                        // Update into vehicle Table 
                        UpdateValidtyField(VehicleID, _lstvehdocs[i].DocumentType.ToUpper(), Validity);
                    }

                }
            }
            objCore.LoggingEntries("Vehicle Mgmt.", "New Document(s) has Uploaded to the vehicle " + objCore.GetVehicleRegNumber(VehicleID), User.Identity.Name);
            return RedirectToAction("EditVehicle", "Vehicle", new { ID = VehicleID });
        }

        [HttpGet]
        public ActionResult DeleteDocument(long ID, long VehicleID)
        {
            tbl_doc_vehicle_nodes _vdocs = db.tbl_doc_vehicle_nodes.Where(a => a.Status == true && a.DocTypeID == ID && a.VehicleID == VehicleID).FirstOrDefault();
            _vdocs.Status = false;
            // TryUpdateModel(_vdocs);
            db.SaveChanges();
            objCore.LoggingEntries("Vehicle Mgmt.", "Document(s) has Audited to the vehicle " + objCore.GetVehicleRegNumber(VehicleID), User.Identity.Name);
            return Content("<script language='javascript' type='text/javascript'>alert('Document Delete successfully');AddDocumentstoVehicle('" + VehicleID + "');</script>");
        }
        [HttpGet]
        public ActionResult AuditDocument(long ID, long VehicleID)
        {
            tbl_doc_vehicle_nodes _vdocs = db.tbl_doc_vehicle_nodes.Where(a => a.Status == true && a.DocTypeID == ID && a.VehicleID == VehicleID).FirstOrDefault();
            _vdocs.AuditedBy = User.Identity.Name.ToUpper();
            _vdocs.Audited = true;
            db.SaveChanges();
            objCore.LoggingEntries("Vehicle Mgmt.", "Document(s) has Audited to the vehicle " + objCore.GetVehicleRegNumber(VehicleID), User.Identity.Name);
            return Content("<script language='javascript' type='text/javascript'>alert('Document Audit successfully');AddDocumentstoVehicle('" + VehicleID + "');</script>");
        }
        [HttpPost]
        public ActionResult AuditAllDocments(FormCollection frm)
        {
            long VehicleID = Convert.ToInt64(frm["hdnVehicleID"]);
            List<tbl_documents> _lstvehdocs = db.tbl_documents.Where(a => a.Status == true).ToList();
            int docCnt = 0;
            for (int i = 0; i < _lstvehdocs.Count; i++)
            {
                bool isChecked = frm.GetValues("chkdoc_" + _lstvehdocs[i].ID).Contains("true"); //Getting all checkboxes values
                long DocID = Convert.ToInt64(_lstvehdocs[i].ID);
                if (isChecked == true)
                {
                    tbl_doc_vehicle_nodes _vdocs = db.tbl_doc_vehicle_nodes.Where(d => d.Status == true && d.DocTypeID == DocID && d.VehicleID == VehicleID).FirstOrDefault();
                    if (_vdocs != null)
                    {
                        _vdocs.AuditedBy = User.Identity.Name.ToUpper();
                        _vdocs.Audited = true;
                        db.SaveChanges();
                        docCnt += 1;
                    }
                }
            }
            if (docCnt == 0)
            {
                return Content("<script language='javascript' type='text/javascript'>alert('Please select atleast one document.');AddDocumentstoVehicle('" + VehicleID + "');</script>");
            }
            objCore.LoggingEntries("Vehicle Mgmt.", "Document(s) has Audited to the vehicle " + objCore.GetVehicleRegNumber(VehicleID), User.Identity.Name);
            return Content("<script language='javascript' type='text/javascript'>alert('Selected Documents are Audited successfully.');AddDocumentstoVehicle('" + VehicleID + "');</script>");

        }



        // Update TextField
        public void UpdateValidtyField(long VehicleId, string ValidtyField, DateTime? Validty)
        {
            string FieldText = (ValidtyField + "Validity").ToUpper();
            if (objCore.GetValidtyFromVeh().Contains(FieldText))
            {
                DateTime? DateVal = Validty;
                if (DateVal != null)
                {
                    string Qry = "Update tbl_vehicles set " + FieldText + "= '" + Validty.Value.ToString("yyyy-MM-dd") + "' where status=1 and id =" + VehicleId + " and status=1 ";
                    db.ExecuteStoreCommand(Qry);
                    db.SaveChanges();
                }
            }
        }

        #endregion

        #region Vehicle Service Schedules

        public ActionResult VehServiceSchedule(long VehId)
        {
            ViewBag.VehicleId = VehId;
            return PartialView("_ServiceSchedule");
        }

        [HttpGet]
        public ActionResult AddNewServiceToVeh(long VehicleId)
        {
            List<tbl_veh_service_schedules> _vehSerList = db.tbl_veh_service_schedules.Where(a => a.VehicleId == VehicleId).ToList();
            ViewBag.SSNO = new SelectList(objCore.LoadServiceNumber(), "Value", "Text");
            ViewBag.ServiceStation = new SelectList(objCore.LoadServiceStations(), "Value", "Text");
            ViewBag.VehicleId = VehicleId;
            return PartialView("_AddNewServiceToVeh", _vehSerList);
        }
        [HttpPost]
        public ActionResult AddNewServiceToVeh(FormCollection frm, tbl_veh_service_schedules serviceSch)
        {
            try
            {
                List<string> rowIds = new List<string>();
                foreach (var item in frm.AllKeys)
                    if (item.Contains("rowId_"))
                        rowIds.Add(frm.Get(item).ToString());

                long VehicleId = Convert.ToInt64(frm["VehicleId"]);
                int _serviceNo = 0; decimal _odoMeterReading = 0; decimal _odoMeterMaxReading = 0;
                int _serviceDays = 0; DateTime? _dueDt = null; long _id = 0;
                long? _serStation = 0;
                foreach (var item in rowIds)
                {
                    _id = Convert.ToInt64(frm["rowId_" + item]);
                    _serviceNo = Convert.ToInt32(frm["SS_NO_" + item]);
                    _odoMeterReading = string.IsNullOrEmpty(frm["txt_odo_meter_" + item]) ? 0 : Convert.ToDecimal(frm["txt_odo_meter_" + item]);
                    _odoMeterMaxReading = string.IsNullOrEmpty(frm["txt_odo_meter_max_" + item]) ? 0 : Convert.ToDecimal(frm["txt_odo_meter_max_" + item]);
                    _serviceDays = string.IsNullOrEmpty(frm["txt_ser_days_" + item]) ? 0 : Convert.ToInt32(frm["txt_ser_days_" + item]);
                    //_dueDt = string.IsNullOrEmpty(frm["txt_date_" + item]) ? _dueDt : Convert.ToDateTime(frm["txt_date_" + item]);
                    _serStation = string.IsNullOrEmpty(frm["txt_station_" + item]) ? 0 : Convert.ToInt64(frm["txt_station_" + item]);

                    using (FMSDBEntities context = new FMSDBEntities())
                    {
                        // Verify If Exist Delete and Insert
                        if (db.tbl_veh_service_schedules.Where(a => a.Id == _id && a.VehicleId == VehicleId && a.ServiceNo == _serviceNo).Any())
                        {
                            var previousSerStruct = context.tbl_veh_service_schedules.Where(a => a.Id == _id).SingleOrDefault();
                            if (previousSerStruct != null)
                                context.tbl_veh_service_schedules.DeleteObject(previousSerStruct);
                            context.SaveChanges();
                        }
                        if (!VerifyExistedServiceStructure(VehicleId, _serviceNo, _odoMeterReading, _serviceDays, Convert.ToDateTime(_dueDt)))
                        {
                            if (db.tbl_veh_service_schedules.Where(a => a.VehicleId == VehicleId && a.ServiceNo == _serviceNo).Any())
                                continue;

                            tbl_veh_service_schedules vs = new tbl_veh_service_schedules();
                            vs.VehicleId = VehicleId;
                            vs.ServiceNo = _serviceNo;
                            vs.ServiceDays = _serviceDays;
                            vs.ODMeterReading = _odoMeterReading;
                            vs.ODMeterMax = _odoMeterMaxReading;
                            vs.DueDt = _dueDt;
                            vs.CreatedBy = User.Identity.Name;
                            vs.CreatedDt = DateTime.Now.Date;
                            vs.ServiceStation = (_serStation == 0 ? null : _serStation);
                            vs.IsDone = false;
                            context.AddTotbl_veh_service_schedules(vs);
                            context.SaveChanges();
                        }
                    }
                }
                return Json(new { success = true, msg = "service schedules has been assigned successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, msg = "An Error occured while processing your request." });
            }
        }

        public ActionResult AddServiceRow()
        {
            ViewBag.SSNO = new SelectList(objCore.LoadServiceNumber(), "Value", "Text");
            ViewBag.ServiceStation = new SelectList(objCore.LoadServiceStations(), "Value", "Text");
            return PartialView("_AddTrService", new tbl_veh_service_schedules());
        }

        [HttpPost]
        public ActionResult DeleteServiceRow(long Id)
        {
            if (db.tbl_veh_service_schedules.Where(a => a.Id == Id).Any())
            {
                tbl_veh_service_schedules servStruct = db.tbl_veh_service_schedules.Where(a => a.Id == Id).FirstOrDefault();
                db.tbl_veh_service_schedules.DeleteObject(servStruct);
                db.SaveChanges();
            }
            return Json(new { success = true, msg = "" });
        }

        [HttpPost]
        public ActionResult ApplyServiceToVeh(FormCollection frm)
        {
            try
            {
                long vehicleId = Convert.ToInt64(frm["vid"]);
                ServiceSchMasterController objService = new ServiceSchMasterController();
                var serviceList = objService.GetServiceList(objCore.GetVehicleModelId(vehicleId));
                tbl_vehicles vehDet = db.tbl_vehicles.Where(a => a.ID == vehicleId).SingleOrDefault();
                DateTime? DueDt = null;
                //DueDt = (vehDet.AcquisitionDt == null ? (odMeterVeh.ODMDate == null ? DateTime.Now.Date : odMeterVeh.ODMDate.Value.Date) : vehDet.AcquisitionDt.Value.Date);
                foreach (var item in serviceList)
                {
                    if (db.tbl_veh_service_schedules.Where(a => a.VehicleId == vehicleId && a.ServiceNo == item.SSNO).Any())
                        continue;

                    // Insert here
                    tbl_veh_service_schedules vs = new tbl_veh_service_schedules();
                    vs.VehicleId = vehicleId;
                    vs.ODMeterReading = item.MinKM;
                    vs.ODMeterMax = item.MaxKM;
                    vs.IsDone = false;
                    vs.ServiceNo = item.SSNO;
                    vs.ServiceDays = item.Days;
                    vs.DueDt = DueDt;
                    vs.CreatedBy = User.Identity.Name;
                    vs.CreatedDt = DateTime.Now;
                    db.AddTotbl_veh_service_schedules(vs);
                    db.SaveChanges();

                }
                return Json(new { success = true, msg = "Service schedule has been successfully assigned to vehicle." });
            }
            catch
            {
                return Json(new { success = false, msg = "An error occured while proccessing your request." });
            }
        }

        [HttpGet]
        public ActionResult GetVehServiceList(long vehicleId)
        {
            List<tbl_veh_service_schedules> _vehSerList = db.tbl_veh_service_schedules.Where(a => a.VehicleId == vehicleId).ToList();
            ViewBag.ServiceStation = new SelectList(objCore.LoadServiceStations(), "Value", "Text");
            return PartialView("_VehServiceList", _vehSerList);
        }

        [HttpPost]
        public ActionResult DoneVehService(long Id, string date, long serviceStatId)
        {
            try
            {
                tbl_veh_service_schedules vehSrvice = db.tbl_veh_service_schedules.Where(a => a.Id == Id).SingleOrDefault();
                vehSrvice.ServiceStation = serviceStatId;
                vehSrvice.DoneBy = User.Identity.Name;
                vehSrvice.DoneDt = Convert.ToDateTime(date);
                vehSrvice.IsDone = true;
                TryUpdateModel(vehSrvice);
                db.SaveChanges();
            }
            catch
            {
                return Json(new { success = false, msg = "An error occured while processing your request." });
            }
            return Json(new { success = true, msg = "Vehicle servicing has done successfully." });
        }


        [HttpGet]
        public ActionResult GetVehServiceDetailList(long vehicleId)
        {
            List<tbl_veh_service_schedules> _vehSerList = db.tbl_veh_service_schedules.Where(a => a.VehicleId == vehicleId).ToList();
            return PartialView("_VehServiceDetailList", _vehSerList);
        }


        public int AutoAssignServiceScheduleToVehicles(long? VehicleId)
        {
            int result = 0;
            ServiceSchMasterController objService = new ServiceSchMasterController();
            List<tbl_vehicles> _vehList = null;
            List<tbl_service_sch_master> _serviceList = null;

            db.Connection.Open();
            using (var transaction = db.Connection.BeginTransaction())
            {
                try
                {
                    _vehList = objCore.GetVehiclesList(true);

                    // For a single vehicle
                    if (VehicleId.HasValue)
                        _vehList = _vehList.Where(a => a.ID == VehicleId).ToList();

                    foreach (var _vehicle in _vehList)
                    {
                        _serviceList = objService.GetServiceList(objCore.GetVehicleModelId(_vehicle.ID));
                        // Assign Service to Vehicle
                        foreach (var item in _serviceList)
                        {
                            if (db.tbl_veh_service_schedules.Where(a => a.VehicleId == _vehicle.ID && a.ServiceNo == item.SSNO).Any())
                                continue;

                            // Insert here
                            tbl_veh_service_schedules vs = new tbl_veh_service_schedules();
                            vs.VehicleId = _vehicle.ID;
                            vs.ODMeterReading = item.MinKM;
                            vs.ODMeterMax = item.MaxKM;
                            vs.IsDone = false;
                            vs.ServiceNo = item.SSNO;
                            vs.ServiceDays = item.Days;
                            //vs.DueDt = DueDt;
                            vs.CreatedBy = User.Identity.Name;
                            vs.CreatedDt = DateTime.Now;
                            db.AddTotbl_veh_service_schedules(vs);
                        }
                    }
                    db.SaveChanges();
                    transaction.Commit();
                    result = 1;
                }
                catch
                {
                    transaction.Rollback();
                    return result;
                }
            }
            db.Connection.Dispose();
            return result;
        }

        #endregion

        #region Vehicle Condition History

        [HttpGet]
        public ActionResult VehicleConditionHistory(Int64 VehicleId)
        {
            var _vehConHistory = (from m in db.tbl_veh_condition_history
                                  where m.IsActive == true && m.VehicleId == VehicleId

                                  select m).AsEnumerable().OrderByDescending(a => a.InspectionDt.Value).FirstOrDefault();
           
            tbl_veh_condition_history _vehConHistoryObjTemp = new tbl_veh_condition_history();
           
                ViewBag.Percentage = new SelectList(objCore.LoadAvgPercentage(), "Value", "Text", (_vehConHistory == null ? null : _vehConHistory.Percentage));
                ViewBag.BreakesPercent = new SelectList(objCore.LoadAvgPercentage(), "Value", "Text", (_vehConHistory == null ? null : _vehConHistory.BreakesPercent));
                ViewBag.OutSidePercent = new SelectList(objCore.LoadAvgPercentage(), "Value", "Text",  (_vehConHistory == null ? null : _vehConHistory.OutSidePercent));
                ViewBag.InSidePercent = new SelectList(objCore.LoadAvgPercentage(), "Value", "Text",  (_vehConHistory == null ? null :_vehConHistory.InSidePercent));
                ViewBag.SeatingPercent = new SelectList(objCore.LoadAvgPercentage(), "Value", "Text",  (_vehConHistory == null ? null :_vehConHistory.SeatingPercent));
                ViewBag.StepneyPercent = new SelectList(objCore.LoadAvgPercentage(), "Value", "Text",  (_vehConHistory == null ? null :_vehConHistory.StepneyPercent));
                ViewBag.AcCondPercent = new SelectList(objCore.LoadAvgPercentage(), "Value", "Text",  (_vehConHistory == null ? null :_vehConHistory.AcCondPercent));
                ViewBag.TyresWheelsPercent = new SelectList(objCore.LoadAvgPercentage(), "Value", "Text",  (_vehConHistory == null ? null :_vehConHistory.TyresWheelsPercent));
           return PartialView("_VehicleConditionHistory", _vehConHistory ?? SETValuesTOConditionHistory(_vehConHistoryObjTemp, VehicleId));

        }
        [HttpPost]
        public ActionResult VehicleConditionHistory(FormCollection frm, tbl_veh_condition_history _vehCondHistoryDet)
        {
            try
            {

                _vehCondHistoryDet.Percentage = frm["Percentage"].ToString();
                _vehCondHistoryDet.BreakesPercent = frm["BreakesPercent"].ToString();
                _vehCondHistoryDet.OutSidePercent = frm["OutSidePercent"].ToString();
                _vehCondHistoryDet.InSidePercent = frm["InSidePercent"].ToString();
                _vehCondHistoryDet.SeatingPercent = frm["SeatingPercent"].ToString();
                _vehCondHistoryDet.StepneyPercent = frm["StepneyPercent"].ToString();
                _vehCondHistoryDet.AcCondPercent = frm["AcCondPercent"].ToString();
                _vehCondHistoryDet.TyresWheelsPercent = frm["TyresWheelsPercent"].ToString();
                _vehCondHistoryDet.Comment = frm["Comments"].ToString();
                _vehCondHistoryDet.CreatedBy = User.Identity.Name;
                _vehCondHistoryDet.InspectionDt = DateTime.Now;
                _vehCondHistoryDet.IsActive = true;
                db.tbl_veh_condition_history.AddObject(_vehCondHistoryDet);
                db.SaveChanges();
                return Json(new { success = true, msg = "Vehicle condition history has updated successfully", VehicleId = _vehCondHistoryDet.VehicleId });
            }
            catch (Exception)
            {
                return Json(new { success = false, msg = "An error occured while processing your request", VehicleId = _vehCondHistoryDet.VehicleId });
            }
        }


        public tbl_veh_condition_history SETValuesTOConditionHistory(tbl_veh_condition_history _vehConHistoryObj, Int64 VehicleId)
        {
            _vehConHistoryObj.BodyConditionAndPainting = false;
            _vehConHistoryObj.BothSideMirrors = false;
            _vehConHistoryObj.AcCond = false;
            _vehConHistoryObj.Brakes = false;
            _vehConHistoryObj.BreakLights = false;
            _vehConHistoryObj.ConditionOfCabInSide = false;
            _vehConHistoryObj.ConditionOfCabOutSide = false;
            _vehConHistoryObj.FirstAidKit = false;
            _vehConHistoryObj.FuelIndicator = false;
            _vehConHistoryObj.GlassClear = false;
            _vehConHistoryObj.HeadLights = false;
            _vehConHistoryObj.Horn = false;
            _vehConHistoryObj.IsActive = true;
            _vehConHistoryObj.ParkingLights = false;
            _vehConHistoryObj.Prefume = false;
            _vehConHistoryObj.ReverseLamps = false;
            _vehConHistoryObj.SeatBelts = false;
            _vehConHistoryObj.SeateCovers = false;
            _vehConHistoryObj.SeatingComfort = false;
            _vehConHistoryObj.SpeedoMeterCond = false;
            _vehConHistoryObj.Stepney = false;
            _vehConHistoryObj.ToolKit = false;
            _vehConHistoryObj.Tourch = false;
            _vehConHistoryObj.TrackTyresWheels = false;
            _vehConHistoryObj.TurnSignallights = false;
            _vehConHistoryObj.Umbrella = false;
            _vehConHistoryObj.VehicleId = VehicleId;
            _vehConHistoryObj.WindShieldWipers = false;
            return _vehConHistoryObj;
        }

        #endregion

        #region Export To Excel For Vehicle Mgmt.
        public ActionResult ExportToExcel()
        {
            var ReportList = (from l in db.tbl_vehicles
                              where l.Status == true
                              select l).OrderBy(a => a.IsProxy).ToList();
            string Filename = "Vehicles_" + DateTime.Now.ToString("hhmmss") + ".xlsx";
            if (ReportList == null)
            {
                MemoryStream ms1 = new MemoryStream();
                ms1.Seek(0, SeekOrigin.Begin);
                return File(ms1, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml", Filename);
            }
            MemoryStream ms = VehicleExportToExcel(ReportList);

            if (ms != null)
            {
                ms.Seek(0, SeekOrigin.Begin);
                return File(ms, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml", Filename);
            }
            return View();
        }

        public MemoryStream VehicleExportToExcel(List<tbl_vehicles> list)
        {
            string sheetName = "VehicleMgmtReport";
            var wb = new XLWorkbook();
            try
            {
                core objCore = new core();

                var ws = wb.Worksheets.Add(sheetName);

                ws.Cell("B3").Value = "Vehicle Reg";
                ws.Cell("C3").Value = "Vendor";
                ws.Cell("D3").Value = "PAN No.";
                ws.Cell("E3").Value = "Model";
                ws.Cell("F3").Value = "Seater";
                ws.Cell("G3").Value = "Type";
                ws.Cell("H3").Value = "Manuf. Year Month";
                ws.Cell("I3").Value = "Owner Cum Driver";
                ws.Cell("J3").Value = "Driver";
                ws.Cell("K3").Value = "NON-AC Rate";
                ws.Cell("L3").Value = "AC Rate";
                ws.Cell("M3").Value = "Driver(s)";
                ws.Cell("N3").Value = "Tax Validity";
                ws.Cell("O3").Value = "Permit Validity";
                ws.Cell("P3").Value = "Fitness Validity";
                ws.Cell("Q3").Value = "Insurance Validity";
                ws.Cell("R3").Value = "Pollution Validity";
                ws.Cell("S3").Value = "Proxy";

                int i = 4;
                foreach (var item in list)
                {
                    ws.Cell("B" + i).SetValue(item.VehicleRegNum);
                    ws.Cell(("C" + i)).SetValue(item.VendorID == null ? "" : item.tbl_vendor_details.FirstName + " " + item.tbl_vendor_details.LastName);
                    ws.Cell(("D" + i)).SetValue(item.VendorID == null ? "" : (item.tbl_vendor_details.PanCardNumber == null ? "" : item.tbl_vendor_details.PanCardNumber));
                    ws.Cell(("E" + i)).SetValue(item.VehicleModelID == null ? "" : item.tbl_vehicle_models.VehicleModelName);
                    ws.Cell(("F" + i)).SetValue(item.SeaterId == null ? "" : item.tbl_seaters.Seater);
                    ws.Cell(("G" + i)).SetValue(item.VehicleTypeID == null ? "" : item.tbl_vehicle_types.VehicleType);
                    ws.Cell(("H" + i)).SetValue(item.ManufactureYearMonth == null ? "" : item.ManufactureYearMonth);
                    ws.Cell(("I" + i)).SetValue((item.OwnerCumDriver == null || item.OwnerCumDriver == false) ? "No" : "Yes");
                    ws.Cell(("J" + i)).SetValue(item.SingleDoubleDriver == null ? "" : item.SingleDoubleDriver);
                    ws.Cell(("K" + i)).SetValue(item.VendorRate == null ? 0 : (double)item.VendorRate);
                    ws.Cell(("L" + i)).SetValue(item.VendorAcRate == null ? 0 : (double)item.VendorAcRate);
                    ws.Cell(("M" + i)).SetValue(objCore.GetDriverByVehicle(item.ID));
                    ws.Cell(("N" + i)).SetValue(item.TaxValidity == null ? "" : item.TaxValidity.Value.ToShortDateString());
                    ws.Cell(("O" + i)).SetValue(item.PermitValidity == null ? "" : item.PermitValidity.Value.ToShortDateString());
                    ws.Cell(("P" + i)).SetValue(item.FitnessValidity == null ? "" : item.FitnessValidity.Value.ToShortDateString());
                    ws.Cell(("Q" + i)).SetValue(item.InsuranceValidity == null ? "" : item.InsuranceValidity.Value.ToShortDateString());
                    ws.Cell(("R" + i)).SetValue(item.PollutionValidity == null ? "" : item.PollutionValidity.Value.ToShortDateString());
                    ws.Cell(("S" + i)).SetValue(item.IsProxy == null ? "Not Proxy" : (bool)(item.IsProxy) ? "Proxy" : "Not Proxy");
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

        #region Custom Methods

        public bool VerifyOdoMeterReading(decimal OdoMeter, decimal CurrentReading, bool Iscurrent)
        {
            try
            {
                if (OdoMeter >= CurrentReading)
                {
                    if (Iscurrent)
                        return false;
                    else
                        return true;
                }
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }

        public bool VerifyExistedServiceStructure(long VehicleId, int ServiceNo, decimal OdoMeter, int ServiceDays, DateTime DueDt)
        {
            try
            {
                var _isServiceStruct = db.tbl_veh_service_schedules.Where(a => a.VehicleId == VehicleId && a.ServiceNo == ServiceNo && a.ServiceDays == ServiceDays
                    && a.ODMeterReading == OdoMeter).Any();
                return _isServiceStruct;
            }
            catch
            {
                return false;
            }

        }

        public bool IsComplete(tbl_vehicles vehicle)
        {
            if (vehicle.TaxValidity == null)
                return false;
            else if (vehicle.PermitValidity == null)
                return false;
            else if (vehicle.PollutionValidity == null)
                return false;
            else if (vehicle.InsuranceValidity == null)
                return false;
            else
                return true;
        }

        public bool ValidateForm(tbl_vehicles vehicle, bool IsCreate)
        {
            if (vehicle.VehicleRegNum == null || vehicle.VehicleRegNum.ToString().Trim().Length == 0)
                ModelState.AddModelError("VehicleRegNum", "Please enter vehicle reg. number.");
            if (IsCreate)
            {
                if (vehicle.VehicleRegNum != null)
                {
                    if (!VerifyVehicleRegNumber(vehicle.VehicleRegNum.ToString()))
                        ModelState.AddModelError("VehicleRegNum", "Vehicle register number already exists");
                }
            }
            if (vehicle.VendorID == 0 || vehicle.VendorID.ToString().Trim().Length == 0)
                ModelState.AddModelError("VendorID", "Please select vendor.");
            if (vehicle.VehicleModelID.ToString().Trim().Length == 0)
                ModelState.AddModelError("VehicleModelID", "Please select vehicle model.");
            if (vehicle.ManufactureYearMonth == null || vehicle.ManufactureYearMonth.Trim().Length == 0)
                ModelState.AddModelError("ManafactureYearMonth", "Please select manufacture month and year.");
            if (vehicle.VehicleTypeID.ToString().Trim().Length == 0)
                ModelState.AddModelError("VehicleTypeID", "Please select vehicle type.");
            if (vehicle.SeaterId.ToString().Trim().Length == 0)
                ModelState.AddModelError("SeaterID", "Please select seater.");
            //if (vehicle.TaxValidity == null || vehicle.TaxValidity.Value.ToString().Trim().Length == 0)
            //    ModelState.AddModelError("TaxValidity", "Please select Tax validity.");
            //if (vehicle.PermitValidity == null || vehicle.PermitValidity.Value.ToString().Trim().Length == 0)
            //    ModelState.AddModelError("PermitValidy", "Please select permit validity.");
            //if (vehicle.PollutionValidity == null || vehicle.PollutionValidity.Value.ToString().Trim().Length == 0)
            //    ModelState.AddModelError("PollutionValidiy", "Please select pollution validty.");
            //if (vehicle.InsuranceValidity == null || vehicle.InsuranceValidity.Value.ToString().Trim().Length == 0)
            //    ModelState.AddModelError("InsuranceValidty", "Please select insurance validty.");
            if (vehicle.OwnerCumDriver == null || vehicle.OwnerCumDriver.Value.ToString().Length == 0)
                ModelState.AddModelError("OwnerCumDriver", "Please select owner or driver.");
            if (vehicle.SingleDoubleDriver == null || vehicle.SingleDoubleDriver.ToString().Trim().Length == 0)
                ModelState.AddModelError("SingleDoubleDriver", "Please select single or double.");
            //if (vehicle.VendorRate == null || vehicle.VendorRate.ToString().Trim().Length == 0)
            //    ModelState.AddModelError("VendorRate", "Please enter vendor rate.");
            return ModelState.IsValid;
        }

        public bool VerifyVehicleRegNumber(string vehicleRegNum)
        {
            if (db.tbl_vehicles.Where(a => a.Status == true && a.VehicleRegNum.Trim().ToUpper() == vehicleRegNum.Trim().ToUpper()).Any())
                return false;
            else
                return true;
        }

        public bool IsAuditByVehicleDocs(long VehicleId)
        {
            List<tbl_doc_vehicle_nodes> vehDocList = db.tbl_doc_vehicle_nodes.Where(a => a.Status == true && a.VehicleID == VehicleId).ToList();
            int i = 0, j = 0;
            bool IsAudit = false;
            foreach (var objdoc in vehDocList)
            {
                IsAudit = objdoc.Audited == null ? false : (bool)objdoc.Audited;
                if (IsAudit)
                {
                    i = 1; j = 0;
                }
                else
                {
                    j = 1; i = 0;
                }

            }
            if (i == 1)
                return true;
            else
                return false;
        }

        #endregion

    }
}
