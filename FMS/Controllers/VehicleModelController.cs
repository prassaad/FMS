using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models; 

namespace FMS.Controllers
{
    [CustomAuthorizationFilter]
    public class VehicleModelController : Controller
    {
        #region ctors
        private FMSDBEntities db;
        private core core = new core();
        public VehicleModelController()
        {
            db = new FMSDBEntities();
        } 
        #endregion
         
        // GET: /VehicleModel/
        [Authorize]
        public ActionResult Index()
        {
            var vehicleModelList = db.tbl_vehicle_models.Where(a => a.Status == true).ToList();
            core.ConvertToUppercase<tbl_vehicle_models>(vehicleModelList);
            return View(vehicleModelList);
        }

        #region Add Vehicle Model
        // URL: /VehicleModel/Create 
        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Create([Bind(Exclude = "id")]FormCollection frm, tbl_vehicle_models vm)
        {
            core.ConvertToUppercase(vm);
            try
            {
                if (ValidateForm(vm))
                {
                    vm.Status = true;
                    db.tbl_vehicle_models.AddObject(vm);
                    db.SaveChanges();
                    core.LoggingEntries("Masters", "Vehicle Model has created with the name " + vm.VehicleModelName, User.Identity.Name);
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                return View();
            }
            return View();
        } 
        #endregion

        #region Edit Vehicle Model
        // URL : VehicleModels/Edit
        [HttpGet]
        public ActionResult Edit(long Id)
        {
            var vehicleModel = db.tbl_vehicle_models.Where(a => a.ID == Id && a.Status == true).SingleOrDefault();
            return View(vehicleModel);
        }
        [HttpPost]
        public ActionResult Edit(long Id, FormCollection frm, tbl_vehicle_models vm)
        {
            var vehicleModel = db.tbl_vehicle_models.Where(a => a.ID == Id && a.Status == true).SingleOrDefault();
            try
            {
                if (ValidateForm(vm))
                {
                    vehicleModel.VehicleModelName = vm.VehicleModelName.ToUpper().Trim();
                    db.SaveChanges();
                    core.LoggingEntries("Masters", "Vehicle Model has Edited with the name " + vm.VehicleModelName, User.Identity.Name);
                    return RedirectToAction("Index");
                }
                return View(vehicleModel);
            }
            catch (Exception ex)
            {
                return View(vehicleModel);
            }
        } 
        #endregion

        #region Delete Vehicle Model
        [HttpGet]
        public ActionResult Delete(long Id)
        {
            var vehicleModel = db.tbl_vehicle_models.Where(a => a.ID == Id && a.Status == true).SingleOrDefault();
            return View(vehicleModel);
        }
        [HttpPost]
        public ActionResult Delete(long Id, FormCollection frm, tbl_vehicle_models vm)
        {
            try
            {
                var vehicleModel = db.tbl_vehicle_models.Where(a => a.ID == Id && a.Status == true).SingleOrDefault();
                vehicleModel.Status = false;
                db.SaveChanges();
                core.LoggingEntries("Masters", "Vehicle Model has deleted with the name " + vm.VehicleModelName, User.Identity.Name);
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                return View();
            }
        } 
        #endregion

        public bool ValidateForm(tbl_vehicle_models vm)
        {
            if (vm.VehicleModelName.Trim().Length == 0)
                ModelState.AddModelError("VehicleModelName", "Please enter Vehicle Model Name."); 
            return ModelState.IsValid; 
        }
    }
}
