using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;

namespace FMS.Controllers
{
    [CustomAuthorizationFilter]
    public class PayFieldsController : Controller
    {
        #region ctors
        private FMSDBEntities db;
        private core objCore = new core();
        public PayFieldsController()
        {
            db = new FMSDBEntities();
        } 
        #endregion

        // GET: /PayFields/
        [Authorize]
        public ActionResult Index()
        {
            var payFields = db.tbl_pay_fields.Where(a=>a.Status == true).ToList();
            objCore.ConvertToUppercase<tbl_pay_fields>(payFields);
            return View(payFields);
        }

        #region Add Pay Fields
        // Add PayFields
        [HttpGet]
        public ActionResult Create()
        {
            ViewBag.Type = new SelectList(new[] { "EARNINGS", "DEDUCTIONS" }).ToList();
            ViewBag.EntryMode = new SelectList(new[] { "TO BE ENTERED", "TO BE CALCULATED" }).ToList();
            ViewBag.CalcType = new SelectList(new[] { "FIXED", "VARIES WITH PRESENTS" }).ToList();
            return View();
        }
        [HttpPost]
        public ActionResult Create([Bind(Exclude = "PayID")]FormCollection frm, tbl_pay_fields payfields)
        {
            ViewBag.Type = new SelectList(new[] { "EARNINGS", "DEDUCTIONS" }).ToList();
            ViewBag.EntryMode = new SelectList(new[] { "TO BE ENTERED", "TO BE CALCULATED" }).ToList();
            ViewBag.CalcType = new SelectList(new[] { "FIXED", "VARIES WITH PRESENTS" }).ToList();
            try
            {
                if (Validation(payfields))
                {
                    payfields.Status = true;
                    payfields.CreatedOn = DateTime.Now;
                    payfields.CreatedBy = User.Identity.Name;
                    objCore.ConvertToUppercase(payfields);
                    db.tbl_pay_fields.AddObject(payfields);
                    db.SaveChanges();
                    objCore.LoggingEntries("Masters", "PayFields was created", User.Identity.Name);
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Message = "An error occured while processing ==> " + ex.Message.ToString();
                return View();
            }
            return View();
        } 
        #endregion

        #region Edit Pay Fields
        // Edit PayFields
        [HttpGet]
        public ActionResult Edit(long Id)
        {
            tbl_pay_fields payFields = db.tbl_pay_fields.Where(a => a.Status == true && a.PayID == Id).FirstOrDefault();
            ViewBag.Type = new SelectList(new[] { "EARNINGS", "DEDUCTIONS" }, (payFields.Type == null ? null : payFields.Type)).ToList();
            ViewBag.EntryMode = new SelectList(new[] { "TO BE ENTERED", "TO BE CALCULATED" }, (payFields.EntryMode == null ? null : payFields.EntryMode)).ToList();
            ViewBag.CalcType = new SelectList(new[] { "FIXED", "VARIES WITH PRESENTS" }, (payFields.CalcType == null ? null : payFields.CalcType)).ToList();
            return View(payFields);
        }
        [HttpPost]
        public ActionResult Edit(long Id, FormCollection frm, tbl_pay_fields payfields)
        {
            tbl_pay_fields payFieldDet = db.tbl_pay_fields.Where(a => a.Status == true && a.PayID == Id).FirstOrDefault();
            ViewBag.Type = new SelectList(new[] { "EARNINGS", "DEDUCTIONS" }, (payFieldDet.Type == null ? null : payFieldDet.Type)).ToList();
            ViewBag.EntryMode = new SelectList(new[] { "TO BE ENTERED", "TO BE CALCULATED" }, (payFieldDet.EntryMode == null ? null : payFieldDet.EntryMode)).ToList();
            ViewBag.CalcType = new SelectList(new[] { "FIXED", "VARIES WITH PRESENTS" }, (payFieldDet.CalcType == null ? null : payFieldDet.CalcType)).ToList();
            try
            {
                if (Validation(payfields))
                {
                    payfields.Status = true;
                    objCore.ConvertToUppercase(payfields);
                    TryUpdateModel(payFieldDet);
                    db.SaveChanges();
                    objCore.LoggingEntries("Masters", "PayFields was Edited", User.Identity.Name);
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Message = "An error occured while processing ==> " + ex.Message.ToString();
                return View(payFieldDet);
            }
            return View(payFieldDet);
        } 
        #endregion

        #region Delete Pay Fields
        // Delete 
        public ActionResult Delete(long Id)
        {
            tbl_pay_fields payFields = db.tbl_pay_fields.Where(a => a.PayID == Id && a.Status == true).FirstOrDefault();
            try
            {
                TryUpdateModel(payFields);
                payFields.Status = false;
                db.SaveChanges();
                objCore.LoggingEntries("Masters", "PayFields was Deleted", User.Identity.Name);
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                return View("Error");
            }
        } 
        #endregion

        public bool Validation(tbl_pay_fields payfields)
        {
            if (payfields.PayName == null || payfields.PayName == "")
                ModelState.AddModelError("PayName", "Please enter pay name.");
            if (payfields.Type == null || payfields.Type == "")
                ModelState.AddModelError("Type", "Please select Type");
            if (payfields.EntryMode == null || payfields.EntryMode == "")
                ModelState.AddModelError("EntryMode", "Please select entry mode");
            if (payfields.CalcType == null || payfields.EntryMode == "")
                ModelState.AddModelError("CalcType", "Please select calculation type");
            if (payfields.OrderNo == null || payfields.OrderNo == 0)
                ModelState.AddModelError("OrderNo", "Please enter order number");
            return ModelState.IsValid;
        }
    }
}
