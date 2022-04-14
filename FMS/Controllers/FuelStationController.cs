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
    public class FuelStationController : Controller
    {
        #region ctors
        private FMSDBEntities db;
        public FuelStationController()
        {
            db = new FMSDBEntities();
        }
        private core objCore = new core(); 
        #endregion 
        
        // GET: /FuelStation/
        [Authorize]
        public ViewResult Index()
        {
            var list = db.tbl_fuel_stations.Where(a => a.Status == true).ToList();
            objCore.ConvertToUppercase<tbl_fuel_stations>(list);
            return View(list);
        }


        #region Add Fuel Station
        // GET: /FuelStation/Create

        public ActionResult Create()
        {
            return View();
        }

        // POST: /FuelStation/Create

        [HttpPost]
        public ActionResult Create([Bind(Exclude = "ID")]tbl_fuel_stations tbl_fuel_stations)
        {
            if (ModelState.IsValid)
            {
                tbl_fuel_stations.Status = true;
                objCore.ConvertToUppercase(tbl_fuel_stations);
                db.tbl_fuel_stations.AddObject(tbl_fuel_stations);
                db.SaveChanges();
                objCore.LoggingEntries("Masters", "Fuel Station Name was created.", User.Identity.Name);
                return RedirectToAction("Index");
            }

            return View(tbl_fuel_stations);
        } 
        #endregion

        #region Edit Fuel Station
        // GET: /FuelStation/Edit/5
 
        public ActionResult Edit(long id)
        {
            tbl_fuel_stations tbl_fuel_stations = db.tbl_fuel_stations.Single(t => t.ID == id && t.Status == true);
            return View(tbl_fuel_stations);
        }

         
        // POST: /FuelStation/Edit/5

        [HttpPost]
        public ActionResult Edit(tbl_fuel_stations tbl_fuel_stations)
        {
            if (ModelState.IsValid)
            {
                db.tbl_fuel_stations.Attach(tbl_fuel_stations);
                tbl_fuel_stations.Status = true;
                objCore.ConvertToUppercase(tbl_fuel_stations);
                db.ObjectStateManager.ChangeObjectState(tbl_fuel_stations, EntityState.Modified);
                db.SaveChanges();
                objCore.LoggingEntries("Masters", "Fuel Station has edited with the name " + tbl_fuel_stations.FuelStation + " ", User.Identity.Name);
                return RedirectToAction("Index");
            }
            return View(tbl_fuel_stations);
        }

        #endregion

        #region Delete Fuel Station
        // GET: /FuelStation/Delete/5
        public ActionResult Delete(long id)
        {
            tbl_fuel_stations tbl_fuel_stations = db.tbl_fuel_stations.Single(t => t.ID == id);
            return View(tbl_fuel_stations);
        }

        // POST: /FuelStation/Delete/5
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(long id)
        {
            tbl_fuel_stations tbl_fuel_stations = db.tbl_fuel_stations.Single(t => t.ID == id);
            db.tbl_fuel_stations.Attach(tbl_fuel_stations);
            tbl_fuel_stations.Status = false;
            db.ObjectStateManager.ChangeObjectState(tbl_fuel_stations, EntityState.Modified);
            db.SaveChanges();
            objCore.LoggingEntries("Masters", "Fuel Station has deleted with the name " + tbl_fuel_stations.FuelStation + " ", User.Identity.Name);
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