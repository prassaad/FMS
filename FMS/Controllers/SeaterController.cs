using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;

namespace FMS.Controllers
{
    [CustomAuthorizationFilter]
    public class SeaterController : Controller
    {
        #region ctors
        private FMSDBEntities db;
        private core core = new core();
        public SeaterController()
        {
            db = new FMSDBEntities();
        } 
        #endregion

        #region Index Methods
        [Authorize]
        public ActionResult Index()
        {
            var seaterList = db.tbl_seaters.Where(s => s.Status == true).ToList();
            core.ConvertToUppercase<tbl_seaters>(seaterList);
            return View(seaterList);
        } 
        #endregion

        #region Add Seater
        [HttpGet]
        public ActionResult Create()
        {
            return View(new tbl_seaters());
        }
        [HttpPost]
        public ActionResult Create([Bind(Exclude = "ID")]FormCollection frm, tbl_seaters tbl_seater)
        {
            core.ConvertToUppercase(tbl_seater);
            try
            {
                if (ValidateForm(tbl_seater))
                {
                    string str = tbl_seater.Seater.Trim().Substring(0, 2);
                    var seaterList = db.tbl_seaters.Where(s => s.Status == true).ToList();
                    if (seaterList.Count > 0)
                    {
                        foreach (var slist in seaterList)
                        {
                            string seater = slist.Seater.Trim().Substring(0, 2);
                            if (seater == str)
                            {
                                ViewBag.message = "Unable to add entry to list as Seater already added. Please remove from the list and continue again.";
                                return View();
                            }
                        }
                    }
                    tbl_seater.Status = true;
                    db.tbl_seaters.AddObject(tbl_seater);
                    db.SaveChanges();
                    core.LoggingEntries("Masters", "Seater has created with name " + tbl_seater.Seater + "", User.Identity.Name);
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

        #region Edit Seater
        public ActionResult Edit(int id)
        {
            tbl_seaters seater = db.tbl_seaters.Where(s => s.Status == true && s.ID == id).SingleOrDefault();
            return View(seater);
        }
        [HttpPost]
        public ActionResult Edit(int id, FormCollection frm, tbl_seaters tbl_seater)
        {
            tbl_seaters seater = db.tbl_seaters.Where(s => s.Status == true && s.ID == id).SingleOrDefault();
            try
            {
                if (ValidateForm(tbl_seater))
                {
                    string str = tbl_seater.Seater.Trim().Substring(0, 2);
                    var seaterList = db.tbl_seaters.Where(s => s.Status == true).ToList();
                    if (seaterList.Count > 0)
                    {
                        foreach (var slist in seaterList)
                        {
                            string seaterStr = slist.Seater.Trim().Substring(0, 2);
                            if (seaterStr == str)
                            {
                                ViewBag.message = "Unable to add entry to list as Seater already added. Please remove from the list and continue again.";
                                return View(seater);
                            }
                        }
                    }
                    seater.Status = true;
                    TryUpdateModel(seater);
                    core.ConvertToUppercase(seater);
                    db.SaveChanges();
                    core.LoggingEntries("Masters", "Seater has Updated with name " + tbl_seater.Seater + "", User.Identity.Name);
                    return RedirectToAction("Index");
                }
            }
            catch
            {
                return View(seater);
            }
            return View(seater);
        } 
        #endregion

        #region Delete
        public ActionResult Delete(int id)
        {
            tbl_seaters seater = db.tbl_seaters.Where(s => s.Status == true && s.ID == id).SingleOrDefault();
            return View(seater);
        }
        [HttpPost]
        public ActionResult Delete(int id, FormCollection frm)
        {
            tbl_seaters seater = db.tbl_seaters.Where(s => s.Status == true && s.ID == id).SingleOrDefault();
            try
            {
                seater.Status = false;
                TryUpdateModel(seater);
                db.SaveChanges();
                core.LoggingEntries("Masters", "Seater has deleted with name " + seater.Seater + "", User.Identity.Name);
                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        } 
        #endregion

        public bool ValidateForm(tbl_seaters seater)
        {
            if (seater.Seater == null || seater.Seater.ToString().Trim().Length == 0)
                ModelState.AddModelError("Seater", "Please enter seater.");
            return ModelState.IsValid;
        }
    }
}
