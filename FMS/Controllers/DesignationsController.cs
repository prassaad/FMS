using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;

namespace FMS.Controllers
{
    [CustomAuthorizationFilter]
    public class DesignationsController : Controller
    {

        #region ctors
        private FMSDBEntities db;
        private core objCore = new core();
        public DesignationsController()
        {
            db = new FMSDBEntities();
        } 
        #endregion

        #region Index Methods
        [Authorize]
        public ActionResult Index()
        {
            var designation = db.tbl_designations.Where(d => d.Status == true);
            objCore.ConvertToUppercase<tbl_designations>(designation);
            return View(designation);
        } 
        #endregion

        #region Add Designation
        // GET: /Designation/Create
        public ActionResult Create()
        {
            tbl_designations designation = new tbl_designations();
            return View(designation);
        }

        //
        // POST: /Designation/Create

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Create([Bind(Exclude = "ID")] tbl_designations designation)
        {
            try
            {
                if (ValidateForm(designation))
                {
                    objCore.ConvertToUppercase(designation);
                    db.tbl_designations.AddObject(designation);
                    designation.Status = true;
                    db.SaveChanges();
                    objCore.LoggingEntries("Masters ", "Designation has created with the name " + designation.DisplayName + ".", User.Identity.Name);
                    return RedirectToAction("Index");
                }
            }
            catch
            {
                return View();
            }

            return View(designation);
        } 
        #endregion

        #region Validation Method
        private bool ValidateForm(tbl_designations profile)
        {
            if (profile.DisplayName.ToString().Length == 0)
                ModelState.AddModelError("DisplayName", "Designation is required.");
            return ModelState.IsValid;
        } 
        #endregion

        #region Edit Designation
        //
        // GET: /Designation/Edit/5

        public ActionResult Edit(int id)
        {
            var designation = db.tbl_designations.SingleOrDefault(d => d.ID == id);
            return View(designation);
        }

        //
        // POST: /Designation/Edit/5

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Edit(int id, FormCollection collection, tbl_designations designation)
        {
            var updatedesignation = db.tbl_designations.SingleOrDefault(d => d.ID == id);
            try
            {
                if (ValidateForm(designation))
                {
                    UpdateModel(updatedesignation);
                    designation.Status = true;
                    objCore.ConvertToUppercase(designation);
                    db.SaveChanges();
                    objCore.LoggingEntries("Masters ", "Designation has updated with the name " + designation.DisplayName + ".", User.Identity.Name);
                    return RedirectToAction("Index");
                }
            }
            catch
            {
                return View();
            }
            return View(designation);
        } 
        #endregion

        #region Delete Designation
        public ActionResult Delete(int id)
        {

            tbl_designations designation = db.tbl_designations.SingleOrDefault(d => d.ID == id);
            return View(designation);
        }
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Delete(int id, string confirmButton)
        {

            tbl_designations designation = db.tbl_designations.SingleOrDefault(d => d.ID == id);
            designation.Status = false;
            if (designation == null)
                return View("Error");
            db.SaveChanges();
            objCore.LoggingEntries("Masters ", "Designation has deleted with the name " + designation.DisplayName + ".", User.Identity.Name);
            return RedirectToAction("Index");
        } 
        #endregion

    }
}
