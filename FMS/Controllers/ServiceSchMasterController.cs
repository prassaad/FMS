using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;

namespace FMS.Controllers
{
    [CustomAuthorizationFilter]
    public class ServiceSchMasterController : Controller
    {
        #region ctors
        private FMSDBEntities db;
        private List<tbl_service_sch_master> _serviceSchList;
        private core objCr = new core();
        public ServiceSchMasterController()
        {
            db = new FMSDBEntities();
        }
        #endregion

        #region Index Method
        public ActionResult Index()
        {
            var _serSchList = db.tbl_service_sch_master.Where(a => a.Status == true).ToList();
            return View(_serSchList);
        }
        #endregion

        #region Add Service
        [HttpGet]
        public ActionResult AddNewService()
        {
            ViewBag.SSNO = new SelectList(objCr.LoadServiceNumber(), "Value", "Text");
            ViewBag.VehModelId = new SelectList(objCr.LoadVehicleModels(), "Value", "Text");
            return PartialView("_AddNewService");
        }
        [HttpPost]
        public ActionResult AddNewService(decimal MinVal, decimal MaxVal, int Days, int SSNo, long ModelId)
        {
            _serviceSchList = (List<tbl_service_sch_master>)Session["ServiceList"];
            if (_serviceSchList == null)
                _serviceSchList = new List<tbl_service_sch_master>();

            List<tbl_service_sch_master> prevSerSchList = GetServiceList();
            if (prevSerSchList.Where(a => a.SSNO == SSNo && a.VehModelId == ModelId).Any())
                return Json(new { success = false, msg = "Service data has already exist with same SSNO & Vehicle Model", model = _serviceSchList });
            else if (prevSerSchList.Where(a => a.SSNO == SSNo && a.MinKM == MinVal && a.MaxKM == MaxVal && a.VehModelId == ModelId).Any())
                return Json(new { success = false, msg = "Service data has already exist with same SSNO : " + SSNo + ",Min KM :" + MinVal + ",Max KM :" + MaxVal, model = _serviceSchList });
            if (_serviceSchList.Where(a => a.SSNO == SSNo && a.VehModelId == ModelId).Any())
                return Json(new { success = false, msg = "Service data has already exist with same SSNO & Vehicle Model", model = _serviceSchList });
            else if (_serviceSchList.Where(a => a.SSNO == SSNo && a.MinKM == MinVal && a.MaxKM == MaxVal && a.VehModelId == ModelId).Any())
                return Json(new { success = false, msg = "Service data has already exist with same SSNO : " + SSNo + ",Min KM :" + MinVal + ",Max KM :" + MaxVal, model = _serviceSchList });
            else if (_serviceSchList.Where(a => a.MaxKM > MinVal && a.VehModelId == ModelId).Any())
                return Json(new { success = false, msg = "Service data has already exist with same SSNO : " + SSNo + ",Min KM :" + MinVal + ",Max KM :" + MaxVal, model = _serviceSchList });

            _serviceSchList.Add(new tbl_service_sch_master
            {
                SSNO = SSNo,
                MaxKM = MaxVal,
                MinKM = MinVal,
                Days = Days,
                VehModelId = ModelId,
                Status = true
            });
            Session["ServiceList"] = _serviceSchList;
            return Json(new { success = true, msg = "", model = _serviceSchList });
        }

        public ActionResult SaveService()
        {
            try
            {
                List<tbl_service_sch_master> _serList = (List<tbl_service_sch_master>)Session["ServiceList"];
                objCr.ConvertToUppercase<tbl_service_sch_master>(_serList);
                foreach (tbl_service_sch_master _ser in _serList)
                {
                    db.tbl_service_sch_master.AddObject(_ser);
                    db.SaveChanges();
                }
                Session["ServiceList"] = null;
                objCr.LoggingEntries("Service Schedule.", "Service schedules were created ", User.Identity.Name);
                return RedirectToAction("Index");
            }
            catch
            {
                return RedirectToAction("Index");
            }
        }

        public ActionResult GetServiceSchList()
        {
            _serviceSchList = (List<tbl_service_sch_master>)Session["ServiceList"];
            return PartialView("_ServiceList", _serviceSchList);
        }



        public List<tbl_service_sch_master> GetServiceList()
        {
            try
            {
                var _lstServiceSch = db.tbl_service_sch_master.Where(a => a.Status == true).ToList();
                return _lstServiceSch;
            }
            catch
            {
                return null;
            }
        }

        public List<tbl_service_sch_master> GetServiceList(long ModelId)
        {
            try
            {
                var _lstServiceSch = db.tbl_service_sch_master.Where(a => a.Status == true && a.VehModelId == ModelId).ToList();
                return _lstServiceSch;
            }
            catch
            {
                return null;
            }
        }

        #endregion

        [HttpGet]
        public ActionResult EditService(long Id)
        {
            var _serSchDet = db.tbl_service_sch_master.Where(a => a.Status == true && a.Id == Id).SingleOrDefault();
            ViewBag.SSNO = new SelectList(objCr.LoadServiceNumber(), "Value", "Text", _serSchDet.SSNO);
            ViewBag.VehModelId = new SelectList(objCr.LoadVehicleModels(), "Value", "Text", _serSchDet.VehModelId);
            return PartialView("_EditService", _serSchDet);
        }

        [HttpPost]
        public ActionResult EditService(long Id, tbl_service_sch_master serSchMaster, FormCollection frm)
        {
            tbl_service_sch_master serSchDet = db.tbl_service_sch_master.Where(a => a.Id == Id).FirstOrDefault();
            ViewBag.SSNO = new SelectList(objCr.LoadServiceNumber(), "Value", "Text", serSchDet.SSNO);
            ViewBag.VehModelId = new SelectList(objCr.LoadVehicleModels(), "Value", "Text", serSchMaster.VehModelId);
            try
            {
                TryUpdateModel(serSchDet);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                return Json(new { success = false, msg = "An error occured while processing your request." + ex.Message.ToString() });
            }
            return Json(new { success = true, msg = "Service schedule has been updated successfully." });
        }

        public ActionResult Delete(long Id)
        {
            try
            {
                var serviceDet = db.tbl_service_sch_master.Where(a => a.Id == Id).SingleOrDefault();
                TryUpdateModel(serviceDet);
                serviceDet.Status = false;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch
            {

            }
            return RedirectToAction("Index");
        }

        public ActionResult GetAllExistServices(long VehicleId)
        {
            List<tbl_service_sch_master> _serviceList = GetServiceList(objCr.GetVehicleModelId(VehicleId));
            ViewBag.VehicleId = VehicleId;
            return PartialView("_ExistedServiceList", _serviceList);
        }
    }
}
