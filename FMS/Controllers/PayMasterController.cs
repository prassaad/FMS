using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;

namespace FMS.Controllers
{
    [CustomAuthorizationFilter]
    public class PayMasterController : Controller
    {

        #region ctors
        private FMSDBEntities db;
        private core objCore = new core();

        public PayMasterController()
        {
            db = new FMSDBEntities();
        } 
        #endregion

        // GET: /PayMaster/
        public ActionResult Index()
        {
            return View();
        }

        #region Add Pay Master
        // Add Pay Master 
        [Authorize]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(int payFieldId, FormCollection frm, tbl_pay_master payMaster)
        {
            tbl_pay_master objPayMaster = new tbl_pay_master();
            objPayMaster.PayFieldID = payFieldId;
            objPayMaster.EmpID = payMaster.EmpID;
            objPayMaster.Formula = db.tbl_pay_fields.Where(a => a.PayID == payFieldId && a.Status == true).FirstOrDefault().Formula;
            objPayMaster.Status = true;
            try
            {
                db.tbl_pay_master.AddObject(objPayMaster);
                db.SaveChanges();
                objCore.LoggingEntries("Masters", "Pay Master " + payMaster.tbl_pay_fields.PayName + " for employee " + objPayMaster.tbl_employees.FirstName + " " + objPayMaster.tbl_employees.LastName + " has assigned.", User.Identity.Name);
                return View();
            }
            catch (Exception)
            {
                return View();
            }
        }
        // Add Pay fields to employee
        public ActionResult AddPayFieldsToPayMaster(long EmpId)
        {
            List<GeneralClassFields> list = GetPayFields(EmpId);
            return PartialView("_AddPayFieldsToPayMaster", list);
        } 
        #endregion

        #region Edit PayMaster
        [HttpGet]
        public ActionResult Edit(long Id)
        {
            tbl_pay_master PayMasterDet = db.tbl_pay_master.Where(a => a.ID == Id).FirstOrDefault();
            return PartialView("_Edit", PayMasterDet);
        }
        [HttpPost]
        public ActionResult Edit(long Id, FormCollection frm, tbl_pay_master payMaster)
        {
            tbl_pay_master payMasterDet = db.tbl_pay_master.Where(a => a.ID == Id).FirstOrDefault();
            try
            {
                payMasterDet.Formula = frm["Formula"] == null ? "" : frm["Formula"];
                TryUpdateModel(payMasterDet);
                objCore.LoggingEntries("Masters", "Pay Master " + payMaster.tbl_pay_fields.PayName + " for employee " + payMasterDet.tbl_employees.FirstName + " " + payMasterDet.tbl_employees.LastName + " has updated.", User.Identity.Name);
                db.SaveChanges();
            }
            catch (Exception)
            {
                return PartialView("_Edit", payMasterDet);
            }
            return Content("<script language='javascript' type='text/javascript'> $.modal.close(); alert('Updated successfully'); GetPayMasterListByEmp('" + payMasterDet.EmpID + "') </script>");
        } 
        #endregion

        #region Delete
        [HttpPost]
        public ActionResult Delete(long Id)
        {
            tbl_pay_master payMasterDet = db.tbl_pay_master.Where(a => a.ID == Id).FirstOrDefault();
            try
            {
                TryUpdateModel(payMasterDet);
                payMasterDet.Status = false;
                db.SaveChanges();
                objCore.LoggingEntries("Masters", "Pay Master " + payMasterDet.tbl_pay_fields.PayName + " for employee " + payMasterDet.tbl_employees.FirstName + " " + payMasterDet.tbl_employees.LastName + " has deleted.", User.Identity.Name);
                return Json(new { success = true, msg = "Deleted successfully." });
            }
            catch (Exception)
            {
                return Json(new { success = false, msg = "An error occured while processing " });
            }
        } 
        #endregion

        // Pay Master List by empid
        public ActionResult GetPayMasterListByEmp(long EmpId)
        {
            EmployeesController objEmp = new EmployeesController();
            if (!objEmp.ValidateBasicPay(EmpId))
            {
                tbl_pay_fields payDet = db.tbl_pay_fields.Where(a => a.PayName.ToUpper() == "BP" && a.Status == true).FirstOrDefault();
                tbl_pay_master objPayMaster = new tbl_pay_master();
                objPayMaster.PayFieldID = payDet.PayID;
                objPayMaster.EmpID = EmpId;
                objPayMaster.Formula = payDet.Formula == null ? "" : payDet.Formula;
                objPayMaster.Status = true;
                db.tbl_pay_master.AddObject(objPayMaster);
                db.SaveChanges();
            }
            List<tbl_pay_master> PayMasterList = GetPayMaster(EmpId);
            return PartialView("_GetPayMasterListByEmp", PayMasterList);
        }

        #region CustomMethods

        public List<tbl_pay_master> GetPayMaster(long EmpId)
        {
            return db.tbl_pay_master.Where(a => a.EmpID == EmpId && a.Status == true).ToList().Count == 0 ? null : db.tbl_pay_master.Where(a => a.EmpID == EmpId && a.Status == true).OrderBy(a => a.tbl_pay_fields.OrderNo).ToList();
        }

        public List<GeneralClassFields> GetPayFields(long EmpId)
        {
            string qry = " select PayID as Value1,PayName as Text1,Formula as Text2  from tbl_pay_fields where Status =1 and PayID not in (select PayFieldID as PayID from tbl_pay_master pm inner join tbl_pay_fields pf on pf.PayID = pm.PayFieldID where EmpID = " + EmpId + " and pf.Status =1 and pm.Status =1) ";
            var list = db.ExecuteStoreQuery<GeneralClassFields>(qry).ToList();
            if (list.Count == 0)
                return null;
            return list;
        }

        #endregion

        

    }
}
