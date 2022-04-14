using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;

namespace FMS.Controllers
{
    [CustomAuthorizationFilter]
    public class DepartmentsController : Controller
    {
        #region ctors
        private FMSDBEntities db;
        private core objCore = new core();
        public DepartmentsController()
        {
            db = new FMSDBEntities();
        } 
        #endregion

        #region Index Methods
        // GET: /Departments/
        [Authorize]
        public ActionResult Index()
        {
            var departments = db.tbl_departments.Where(d => d.Status == true);
            objCore.ConvertToUppercase<tbl_departments>(departments);
            return View(departments);
        } 
        #endregion

        #region Add Departments
        //
        // GET: /Departments/Create

        public ActionResult Create()
        {
            tbl_departments tbl_departments = new tbl_departments();
            ViewBag.IndentMgrUserId = new SelectList(db.aspnet_Users, "UserId", "UserName");
            return View(tbl_departments);
        }

        //
        // POST: /Departments/Create

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Create([Bind(Exclude = "ID")] tbl_departments department)
        {
            ViewBag.IndentMgrUserId = new SelectList(db.aspnet_Users, "UserId", "UserName", department.IndentMgrUserId);
            try
            {
                if (ValidateForm(department))
                {
                    objCore.ConvertToUppercase(department);
                    db.tbl_departments.AddObject(department);
                    department.Status = true;
                    db.SaveChanges();
                    objCore.LoggingEntries("Masters ", "Department has created with the name " + department.DisplayName + ".", User.Identity.Name);
                    return RedirectToAction("Index");
                }
            }
            catch
            {
                return View();
            }
            return View();
        } 
        #endregion

        #region Validation Methods
        private bool ValidateForm(tbl_departments profile)
        {
            if (string.IsNullOrEmpty(profile.DisplayName))
                ModelState.AddModelError("DisplayName", "Department is required.");
            if (profile.ReqItem == true)
            {
                if (profile.IndentMgrUserId.ToString() == "" || profile.IndentMgrUserId.Value.ToString().Length == 0)
                    ModelState.AddModelError("IndentMgrUserId", "Please Select Indent Manager.");
            }
            return ModelState.IsValid;
        }
        #endregion

        #region Edit Department
        // GET: /Departments/Edit/5

        public ActionResult Edit(int id)
        {
            var department = db.tbl_departments.SingleOrDefault(d => d.ID == id);
            ViewBag.IndentMgrUserId = new SelectList(db.aspnet_Users, "UserId", "UserName", department.IndentMgrUserId);
            return View(department);
        }

        //
        // POST: /Departments/Edit/5

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Edit(int id, FormCollection collection, tbl_departments department)
        {
            var updatedepartment = db.tbl_departments.SingleOrDefault(d => d.ID == id);
            ViewBag.IndentMgrUserId = new SelectList(db.aspnet_Users, "UserId", "UserName", department.IndentMgrUserId);
            try
            {
                if (ValidateForm(department))
                {
                    UpdateModel(updatedepartment);
                    department.Status = true;
                    objCore.ConvertToUppercase(department);
                    db.SaveChanges();
                    objCore.LoggingEntries("Masters ", "Department has updated with the name " + department.DisplayName + ".", User.Identity.Name);
                    return RedirectToAction("Index");
                }
            }
            catch
            {
                return View();
            }
            return View(department);
        } 
        #endregion

        #region Delete Department
        public ActionResult Delete(int id)
        {
            tbl_departments department = db.tbl_departments.SingleOrDefault(d => d.ID == id);
            return View(department);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Delete(int id, string confirmButton)
        {
            tbl_departments department = db.tbl_departments.SingleOrDefault(d => d.ID == id);
            department.Status = false;
            if (department == null)
                return View("error");
            db.SaveChanges();
            objCore.LoggingEntries("Masters ", "Department has deleted with the name " + department.DisplayName + ".", User.Identity.Name);
            return RedirectToAction("Index");
        } 
        #endregion

    }
}
