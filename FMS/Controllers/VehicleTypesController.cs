using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;

namespace FMS.Controllers
{
    [CustomAuthorizationFilter]
    public class VehicleTypesController : Controller
    {
        #region ctors
        private FMSDBEntities db;
        private core core = new core();
        public VehicleTypesController()
        {
            db = new FMSDBEntities();
        } 
        #endregion
        
        [Authorize]
        public ViewResult Index()
        {
            var VehTypeList = db.tbl_vehicle_types.Where(a => a.Status == true).ToList();
            core.ConvertToUppercase<tbl_vehicle_types>(VehTypeList);
            return View(VehTypeList);
        }

        #region View Vehicle Type

        // GET: /VehicleTypes/Details/5

        public ViewResult Details(long id)
        {
            tbl_vehicle_types tbl_vehicle_types = db.tbl_vehicle_types.Single(t => t.ID == id);
            return View(tbl_vehicle_types);
        } 
        #endregion

        #region Add Vehicle Type
        // GET: /VehicleTypes/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: /VehicleTypes/Create
        [HttpPost]
        public ActionResult Create(tbl_vehicle_types vt)
        {
            core.ConvertToUppercase(vt);
            try
            {
                if (ModelState.IsValid)
                {
                    vt.Status = true;
                    db.tbl_vehicle_types.AddObject(vt);
                    db.SaveChanges();
                    core.LoggingEntries("Masters", "Vehicle Type has created with the name " + vt.VehicleType, User.Identity.Name);
                    return RedirectToAction("Index");
                }
                return View();
            }
            catch (Exception ex)
            {
                return View();
            }
        } 
        #endregion

        #region Edit Vehicle Type
        // GET: /VehicleTypes/Edit/5
        public ActionResult Edit(long id)
        {
            tbl_vehicle_types tbl_vehicle_types = db.tbl_vehicle_types.Single(t => t.ID == id);
            return View(tbl_vehicle_types);
        }

        // POST: /VehicleTypes/Edit/5
        [HttpPost]
        public ActionResult Edit(tbl_vehicle_types vt)
        {
            core.ConvertToUppercase(vt);
            try
            {
                if (ModelState.IsValid)
                {
                    vt.Status = true;
                    db.tbl_vehicle_types.Attach(vt);
                    db.ObjectStateManager.ChangeObjectState(vt, EntityState.Modified);
                    db.SaveChanges();
                    core.LoggingEntries("Masters", "Vehicle Type has Edited with the name " + vt.VehicleType, User.Identity.Name);
                    return RedirectToAction("Index");
                }
                return View(vt);
            }
            catch (Exception ex)
            {
                return View(vt);
            }
        } 
        #endregion

        #region Delete Vehicle Type
        //
        // GET: /VehicleTypes/Delete/5

        public ActionResult Delete(long id)
        {
            tbl_vehicle_types tbl_vehicle_types = db.tbl_vehicle_types.Single(t => t.ID == id);
            return View(tbl_vehicle_types);
        }

        //
        // POST: /VehicleTypes/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(long id)
        {
            tbl_vehicle_types tbl_vehicle_types = db.tbl_vehicle_types.Single(t => t.ID == id);
            tbl_vehicle_types.Status = false;
            db.SaveChanges();
            core.LoggingEntries("Masters", "Vehicle Type has deleted with the name " + tbl_vehicle_types.VehicleType, User.Identity.Name);
            return RedirectToAction("Index");
        } 
        #endregion
        
        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}