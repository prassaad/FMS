using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models; 

namespace FMS.Controllers
{
    [CustomAuthorizationFilter]
    public class EHSCodesController : Controller
    {
        #region ctors
        private FMSDBEntities db;
        private core core = new core();
        public EHSCodesController()
        {
            db = new FMSDBEntities();
        } 
        #endregion

        #region Index Methods
        // GET: /EHSCodes/
        [Authorize]
        public ActionResult Index()
        {
            List<tbl_ehs_codes> _lstEHSCodes = db.tbl_ehs_codes.Where(a => a.Status == true).ToList();
            core.ConvertToUppercase<tbl_ehs_codes>(_lstEHSCodes);
            return View(_lstEHSCodes);
        } 
        #endregion

        #region Create
        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Create([Bind(Exclude = "ID")]FormCollection frm, tbl_ehs_codes ehsCodeDet)
        {
            try
            {
                ehsCodeDet = null;
                if (Validation(ehsCodeDet))
                {
                    core.ConvertToUppercase(ehsCodeDet);
                    ehsCodeDet.Status = true;
                    db.tbl_ehs_codes.AddObject(ehsCodeDet);
                    db.SaveChanges();
                    core.LoggingEntries("Masters", "EHS codes has created", User.Identity.Name);
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Message = "An Error occured ==> " + ex.Message.ToString();
                return View();
            }
            return View();
        } 
        #endregion

        #region Edit 
        [HttpGet]
        public ActionResult Edit(long Id)
        {
            tbl_ehs_codes EHSDet = db.tbl_ehs_codes.Where(a => a.Status == true && a.ID == Id).SingleOrDefault();
            return View(EHSDet);
        }
        [HttpPost]
        public ActionResult Edit(long Id, FormCollection frm, tbl_ehs_codes ehsCodeDet)
        {
            tbl_ehs_codes EHSDet = db.tbl_ehs_codes.Where(a => a.Status == true && a.ID == Id).SingleOrDefault();
            try
            {
                if (Validation(ehsCodeDet))
                {
                    TryUpdateModel(EHSDet);
                    core.ConvertToUppercase(EHSDet);
                    EHSDet.Status = true;
                    db.SaveChanges();
                    core.LoggingEntries("Masters", "EHS codes has updated", User.Identity.Name);
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Message = "An Error Occured ==> " + ex.Message.ToString();
                return View(EHSDet);
            }
            return View();
        } 
        #endregion

        #region Delete
        [HttpGet]
        public ActionResult Delete(long Id)
        {
            tbl_ehs_codes ehsDet = db.tbl_ehs_codes.Where(a => a.Status == true && a.ID == Id).SingleOrDefault();
            return View(ehsDet);
        }
        [HttpPost]
        public ActionResult Delete(long Id, FormCollection frm)
        {
            tbl_ehs_codes ehsDet = db.tbl_ehs_codes.Where(a => a.Status == true && a.ID == Id).SingleOrDefault();
            try
            {
                TryUpdateModel(ehsDet);
                ehsDet.Status = false;
                db.SaveChanges();
                core.LoggingEntries("Masters", "EHS codes has deleted", User.Identity.Name);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Message = "An Error occured ==> " + ex.Message.ToString();
                return View(ehsDet);
            }
        } 
        #endregion

        public bool Validation(tbl_ehs_codes ehs)
        {
            if (string.IsNullOrEmpty(ehs.EHSCode))
                ModelState.AddModelError("EHSCode", "Please enter EHS code");
            return ModelState.IsValid;
        }
    }
}
