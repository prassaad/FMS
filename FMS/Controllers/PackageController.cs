using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;
using ClosedXML.Excel;
using System.Data.SqlClient;
using AsyncUploaderDemo.Models;
using System.Globalization;
using System.IO;
using System.Data.OleDb;
using System.Web.Configuration;
using System.Text;
using FMS.Helpers;
using System.Web.Script.Serialization;

namespace FMS.Controllers
{
    public class PackageController : Controller
    {
        public PackageController()
        {
            db = new FMSDBEntities();
            objCore = new core();
        }
        #region ctors
        private FMSDBEntities db;
        private core objCore;
        private List<tbl_package_client_rates> _PackageList;

        #endregion
        //
        // GET: /Package/

        public ActionResult Index()
        {
            var Package = db.tbl_package_client_rates.Where(a => a.IsActive == true).ToList();
            ViewBag.ClientId = objCore.LoadClients();
            ViewBag.ProjectId = objCore.LoadProjects();
            List<SelectListItem> MonthsList = new List<SelectListItem>();
            for (int i = 0; i < 12; i++)
                MonthsList.Add(new SelectListItem { Text = CultureInfo.GetCultureInfo("en-GB").DateTimeFormat.MonthNames[i], Value = (i >= 9) ? (i + 1).ToString() : "0" + (i + 1).ToString() });
            ViewBag.Package = new SelectList(MonthsList, "Value", "Text", DateTime.Now.Month);
            return View();

        }


        #region Delete Package
        public ActionResult Delete(long id)
        {
            tbl_package_client_rates Package = db.tbl_package_client_rates.Single(t => t.Id == id);
            return View(Package);
        }
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(long id)
        {
            try
            {
                tbl_package_client_rates tbl_Package = db.tbl_package_client_rates.Single(t => t.Id == id);
                tbl_Package.IsActive = false;
                TryUpdateModel(tbl_Package);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                return View();
            }
        }
        #endregion

        public ActionResult _GetPackageList(Int64 ClientId, Int64 ProjectId, int PackageMonth)
        {
            // Get Only Previous Months Packages list by selection
            List<tbl_package_client_rates> packList = objCore.GetPreviouMonthPackageList(ClientId, ProjectId, "", PackageMonth);
            ViewBag.LocationId = objCore.LoadLocations();
            ViewBag.VehicleTypeId = objCore.LoadVehicleTypes();
            ViewBag.VehicleID = objCore.LoadVehicleRegNum();
            ViewBag.TimeUnit = objCore.LoadTimeUnit();
            ViewBag.PackageMonth = PackageMonth;
            return PartialView("_GetPackagelist", packList);
        }
         [HttpGet]
        public ActionResult Create(string logDate)
        {
            DateTime _logdate = string.IsNullOrEmpty(logDate) ? DateTime.Now.Date : Convert.ToDateTime(logDate);
            ViewBag.ClientId = new SelectList(db.tbl_clients.Where(a => a.Status == true), "ID", "CompanyName");
            ViewBag.VehicleId = new SelectList(db.tbl_vehicles.Where(a => a.Status == true), "ID", "VehicleRegNum");
            ViewBag.ProjectId = new SelectList(db.tbl_projects.Where(a => a.IsActive == true), "ID", "ProjectName");
            ViewBag.VehicleTypeId = new SelectList(db.tbl_vehicle_types.Where(a => a.Status == true), "ID", "VehicleType");
            ViewBag.VehicleModelID = new SelectList(db.tbl_vehicle_models.Where(a => a.Status == true), "ID", "VehicleModelName");
            ViewBag.LocationId = objCore.LoadLocations();
            ViewBag.TimeUnit = objCore.LoadTimeUnit();
            return View(new tbl_package_client_rates());
        }

        [HttpPost]
        public ActionResult Create(tbl_package_client_rates tbl_package_client_rates, string logDate)
        {
            DateTime _logdate = string.IsNullOrEmpty(logDate) ? DateTime.Now.Date : Convert.ToDateTime(logDate);
            ViewBag.ClientId = new SelectList(db.tbl_clients.Where(a => a.Status == true), "ID", "CompanyName");
            ViewBag.VehicleId = new SelectList(db.tbl_vehicles.Where(a => a.Status == true), "ID", "VehicleRegNum");
            ViewBag.ProjectId = new SelectList(db.tbl_projects.Where(a => a.IsActive == true), "ID", "ProjectName");
            ViewBag.VehicleTypeId = new SelectList(db.tbl_vehicle_types.Where(a => a.Status == true), "ID", "VehicleType");
            ViewBag.VehicleModelID = new SelectList(db.tbl_vehicle_models.Where(a => a.Status == true), "ID", "VehicleModelName");
            ViewBag.LocationId = objCore.LoadLocations();
            ViewBag.TimeUnit = objCore.LoadTimeUnit();
            try
            {
                if (ValidateForm(tbl_package_client_rates, true))
                {
                    tbl_package_client_rates.IsActive = true;
                    db.tbl_package_client_rates.AddObject(tbl_package_client_rates);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                return View(tbl_package_client_rates);
            }
            return View(tbl_package_client_rates);
        }

        private bool ValidateForm(tbl_package_client_rates tbl_package_client_rates, bool p)
        {
            throw new NotImplementedException();
        }


        public bool ValidateForm(tbl_package_client_rates tbl_package_client_rates, tbl_vehicles Vehicle, bool isCreate)
        {
            if (tbl_package_client_rates.EffectiveDt == null || tbl_package_client_rates.EffectiveDt.ToString().Trim().Length == 0)
                ModelState.AddModelError("EffectiveDt", "Please Enter Date");

            if (tbl_package_client_rates.ClientId == null || tbl_package_client_rates.ClientId.ToString().Trim().Length == 0)
                ModelState.AddModelError("ClientId", "Please select Client.");

            if (tbl_package_client_rates.ProjectId == null || tbl_package_client_rates.ProjectId.ToString().Trim().Length == 0)
                ModelState.AddModelError("ProjectId", "Please select Project.");

            if (Vehicle.VehicleRegNum == null || Vehicle.VehicleRegNum.ToString().Trim().Length == 0)
                ModelState.AddModelError("VehicleRegNum", "Please select VehicleReg Num.");
            
            if (Vehicle.VehicleTypeID == null || Vehicle.VehicleTypeID.ToString().Trim().Length == 0)
                ModelState.AddModelError("VehicleTypeID", "Please select Vehicle ");

            if (Vehicle.VehicleModelID == null || Vehicle.VehicleModelID.ToString().Trim().Length == 0)
                ModelState.AddModelError("VehicleModelID", "Please Enter CardNo");

            if (tbl_package_client_rates.LocationId == null || tbl_package_client_rates.LocationId.ToString().Trim().Length == 0)
                ModelState.AddModelError("LocationId", " Please select location");


            return ModelState.IsValid;
        }
    }
}
