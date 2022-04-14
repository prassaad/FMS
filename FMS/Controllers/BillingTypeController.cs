using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;

namespace FMS.Controllers
{
    public class BillingTypeController : Controller
    {
        #region ctors
        private FMSDBEntities db;
        private core objCore = new core();
        public BillingTypeController()
        {
            db = new FMSDBEntities();
        } 
        #endregion

        #region Index Methods
        [Authorize]
        public ActionResult Index()
        {
            var billingtypes = db.tbl_billing_types.Where(b => b.Status == true).ToList();
            objCore.ConvertToUppercase<tbl_billing_types>(billingtypes);
            return View(billingtypes);
        } 
        #endregion

        #region Add Billing Type
        [HttpGet]
        public ActionResult Create()
        {
            return View(new tbl_billing_types());
        }
        [HttpPost]
        public ActionResult Create([Bind(Exclude = "ID")]FormCollection frm, tbl_billing_types billtypes)
        {
            try
            {
                if (ValidateForm(billtypes))
                {
                    billtypes.Status = true;
                    objCore.ConvertToUppercase(billtypes);
                    db.tbl_billing_types.AddObject(billtypes);
                    db.SaveChanges();
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

        #region Edit Billing Type
        [HttpGet]
        public ActionResult Edit(int id)
        {
            tbl_billing_types billtypes = db.tbl_billing_types.Where(b => b.ID == id && b.Status == true).SingleOrDefault();
            return View(billtypes);
        }
        [HttpPost]
        public ActionResult Edit(int id, FormCollection frm, tbl_billing_types billtypes)
        {
            tbl_billing_types updatebilltype = db.tbl_billing_types.Where(b => b.ID == id && b.Status == true).SingleOrDefault();
            try
            {
                if (ValidateForm(billtypes))
                {
                    updatebilltype.Status = true;
                    TryUpdateModel(updatebilltype);
                    objCore.ConvertToUppercase(billtypes);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            catch
            {
                return View(updatebilltype);
            }
            return View(updatebilltype);
        } 
        #endregion

        #region Delete
        [HttpGet]
        public ActionResult Delete(int id)
        {
            tbl_billing_types billtypes = db.tbl_billing_types.Where(b => b.ID == id && b.Status == true).SingleOrDefault();
            return View(billtypes);
        }
        [HttpPost]
        public ActionResult Delete(int id, FormCollection frm)
        {
            tbl_billing_types billtypes = db.tbl_billing_types.Where(b => b.ID == id && b.Status == true).SingleOrDefault();
            try
            {
                billtypes.Status = false;
                TryUpdateModel(billtypes);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch
            {
                return View(billtypes);
            }
        } 
        #endregion

        #region Validation Method
        public bool ValidateForm(tbl_billing_types billtypes)
        {
            if (billtypes.BillingType == null || billtypes.BillingType.ToString().Trim().Length == 0)
                ModelState.AddModelError("BillingType", "Please enter Billing Type.");
            return ModelState.IsValid;
        } 
        #endregion
        
    }
}
