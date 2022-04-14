using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;

namespace FMS.Areas.Inventory.Controllers
{
    public class ServiceStationController : Controller
    {
        //
        // GET: /ServiceStation/ 
        private FMSDBEntities db;
        public ServiceStationController()
        {
            db = new FMSDBEntities();
        }
        public ActionResult Index()
        {
            return View(db.tbl_service_stations.Where(a=>a.Status==true).ToList());
        }
        public ActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Create(tbl_service_stations servstn)
        {
            try
            {
                if (validateForm(servstn,true))
                {
                    servstn.Status = true;
                    db.tbl_service_stations.AddObject(servstn);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
               
            }
            catch(Exception ex)
            {
                ViewBag.Errormessage = "An Error Occured While Processing Your Request" + ex.ToString();
                return View(servstn);
            }

            return View(servstn);
        }
        public ActionResult Edit(int ? id)
        {
            var ServiceStation = db.tbl_service_stations.Where(a => a.Id == id).SingleOrDefault();
            return View(ServiceStation);
        }
        [HttpPost]
        public ActionResult Edit(tbl_service_stations servstn)
        {
            var _ServiceStation = db.tbl_service_stations.Where(a => a.Id == servstn.Id).SingleOrDefault();
            try
            {
                if (validateForm(servstn, false))
                {
                    if (_ServiceStation.ServiceStation.Trim().ToUpper() != servstn.ServiceStation.Trim().ToUpper())
                    {
                        var duplicate = db.tbl_service_stations.Where(a => a.ServiceStation.Trim().ToUpper() == servstn.ServiceStation.Trim().ToUpper() && a.Status == true).Any();
                        if (duplicate == true)
                        {
                            ModelState.AddModelError("ServiceStation", " '" + servstn.ServiceStation + " ' Service Station has already exists");

                        }
                        else
                        {
                            UpdateModel(_ServiceStation);
                            db.SaveChanges();
                            return RedirectToAction("Index");
                        }
                    }
                    else
                    {
                        UpdateModel(_ServiceStation);
                        db.SaveChanges();
                        return RedirectToAction("Index");
                    }
                }
               
            }
            catch (Exception ex)
            {
                ViewBag.Errormessage = "An Error Occured While Processing Your Request" + ex.ToString();
                return View(servstn);
            }
            return View(servstn);
           
        }
        public ActionResult Delete(int id)
        {
            var _ServiceStation = db.tbl_service_stations.Where(a => a.Id == id).SingleOrDefault();
            _ServiceStation.Status = false;        
    db.SaveChanges();
            return RedirectToAction("Index");
        }
        private bool validateForm(tbl_service_stations servstn,bool isCreate)
        {
            if (servstn.ServiceStation == null || servstn.ServiceStation == "")
            {
                ModelState.AddModelError("ServiceStation", "Service Station is required");
            }
            if (isCreate == true)
            {
                var duplicate = db.tbl_service_stations.Where(a => a.ServiceStation.Trim().ToUpper()== servstn.ServiceStation.Trim().ToUpper() && a.Status == true).Any();
                if (duplicate == true)
                {
                    ModelState.AddModelError("ServiceStation", " '" + servstn.ServiceStation + " ' Service Station has already exists");

                }
            }

            return ModelState.IsValid;
        }


    }
}
