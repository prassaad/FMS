using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;
using ClosedXML.Excel;
using System.Data.SqlClient;
using AsyncUploaderDemo.Models;
using System.Globalization;
using System.IO;
using System.Data.OleDb;
using System.Web.Configuration;
using System.Text;
using FMS.Helpers;
using System.Web.Script.Serialization;


namespace FMS.Controllers
{
    public class ProjectsController : Controller
    {
        #region ctors
        private FMSDBEntities db;
        private core objCore;
        public ProjectsController()
        {
            db = new FMSDBEntities();
            objCore = new core(); 
        }
        #endregion
        //
        // GET: /CardAssignment/
        //  [Authorize]

        public ActionResult Index()
        {
            List<tbl_projects> Projects = db.tbl_projects.Where(a => a.IsActive == true).ToList();
            return View(Projects);
        }

        #region Add  Project
        [HttpGet]
        public ActionResult Create()
        {
            ViewBag.ClientId = objCore.LoadClients();
            ViewBag.BillingTypeId = objCore.LoadBillTypes();
            return View(new tbl_projects());
        }
        [HttpPost]
        public ActionResult Create(tbl_projects tbl_projects,FormCollection frm)
        {
            try
            {
                tbl_projects.IsActive = true;
                tbl_projects.CreatedBy = User.Identity.Name;
                tbl_projects.CreatedOn = DateTime.Now;
                db.tbl_projects.AddObject(tbl_projects);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                core.ExceptionLogging(ex.Message.ToString(), User.Identity.Name, ex.StackTrace);
                return Json(new { success = false, msg = "An error occured while processing your request.Please try again" });
 
            }
            return Json(new { success = true, msg = "Project has been added successfully" });
        }
        #endregion

        #region Edit Project
        public ActionResult Edit(long Id)
        {
            tbl_projects tbl_projects = db.tbl_projects.Where(t => t.Id == Id).SingleOrDefault();
            ViewBag.ClientId = new SelectList(objCore.LoadClients(), "Value", "Text", tbl_projects.ClientId);
            ViewBag.BillingTypeId = new SelectList(objCore.LoadBillTypes(), "Value", "Text", tbl_projects.BillingTypeId); 
            return View(tbl_projects);
        }

        [HttpPost]
        public ActionResult Edit(long ID, tbl_projects tbl_projects,FormCollection frm)
        {
            tbl_projects ProjectDet = db.tbl_projects.Where(t => t.Id == ID).SingleOrDefault(); 
            try
            {
                ProjectDet.IsActive = true;
                TryUpdateModel(ProjectDet);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                core.ExceptionLogging(ex.Message.ToString(), User.Identity.Name, ex.StackTrace);
                return Json(new { success = false, msg = "An error occured while processing your request.Please try again" });
            }
            return Json(new { success = true, msg = "Project has been updated successfully" });
        }
        #endregion

        #region Delete Project
        public ActionResult Delete(long id)
        {
            tbl_projects Projects = db.tbl_projects.Single(t => t.Id == id);
            return View(Projects);
        }
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(long id)
        {
            try
            {
                tbl_projects tbl_projects = db.tbl_projects.Single(t => t.Id == id);
                tbl_projects.IsActive = false;
                TryUpdateModel(tbl_projects);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                return View();
            }
        }
        #endregion

        #region Bulk Package Approvals

        public bool VerifyProjectName(string ProjectName)
        {
            try
            {
                return db.tbl_projects.Where(a => a.ProjectName == ProjectName && a.ProjectName.Trim() == ProjectName.Trim()).Any();
            }
            catch
            {
                return false;
            }
        }
        public bool VerifyProjectCode(string ProjectCode)
        {
            try
            {
                return db.tbl_projects.Where(a => a.ProjectCode == ProjectCode && a.ProjectCode.Trim() == ProjectCode.Trim()).Any();
            }
            catch
            {
                return false;
            }
        }

        [HttpGet]
        public ActionResult BulkPackageApprovals()
        {
            ViewBag.ClientId = objCore.LoadClients();
            int currMonth = DateTime.Now.Month;
            ViewBag.Package = new SelectList(core.GetAllMonths(), "Value", "Text", (currMonth >= 9 ? currMonth.ToString() : "0" + currMonth.ToString()));
            return View();
        }
       
        [HttpGet]
        
        public ActionResult GetPackageApproveAndRenewalList(Int64 ClientId, Int64 ProjectId,string rdFilter,int PackageMonth) 
        {
            // Get Only Previous Months Packages list by selection
            List<tbl_package_client_rates> packList = objCore.GetPreviouMonthPackageList(ClientId, ProjectId, rdFilter, PackageMonth);
            ViewBag.LocationId = objCore.LoadLocations();
            ViewBag.VehicleTypeId = objCore.LoadVehicleTypes();
            ViewBag.VehicleModelId = objCore.LoadVehicleModels();
            ViewBag.TimeUnit = objCore.LoadTimeUnit();
            ViewBag.rdFilter = rdFilter;
            ViewBag.PackageMonth = PackageMonth;
            return PartialView("_GetPackageApproveAndRenewalList", packList);
        }
        
        [HttpPost]
        public ActionResult SaveBulkApprovedPackages(Int64 ClientId,Int64 ProjectId,FormCollection frm)
        {
            string rdFilter = Convert.ToString(frm["rdFilter"]);
            Int32 PackageMonth = Convert.ToInt32(frm["PackageMonth"]);
            List<tbl_package_client_rates> packList = objCore.GetPreviouMonthPackageList(ClientId, ProjectId, rdFilter, PackageMonth);
            List<Int64> Ids = packList.Select(a=>a.Id).ToList();
            try
            {
                Int64 LocationId, VehTypeId, VehModelId;
                Int32 TimeUnit, WorkingDays;
                string LogIn, LogOut;
                decimal PackageAmt;
                bool Approve;
                DateTime? EffectiveDt;

                foreach (var item in Ids)
                {
                    tbl_package_client_rates _clientPackrate = db.tbl_package_client_rates.Where(a => a.Id == item).FirstOrDefault();
                    LocationId = Convert.ToInt64(frm["LocationId_" + item]);
                    VehTypeId = Convert.ToInt64(frm["VehicleTypeId_" + item]);
                    VehModelId = Convert.ToInt64(frm["VehicleModelId_" + item]);
                    TimeUnit = Convert.ToInt32(frm["TimeUnit_" + item]);
                    LogIn = frm["LoginTime_" + item];
                    LogOut = frm["LogoutTime_" + item];
                    PackageAmt = Convert.ToDecimal(frm["PackageAmt_" + item]);
                    WorkingDays = Convert.ToInt32(frm["WorkingDays_" + item]);
                    Approve = string.IsNullOrEmpty(frm["Approve_" + item]) ? false : frm.GetValues("Approve_" + item).Contains("true");
                    EffectiveDt = string.IsNullOrEmpty(frm["EffectiveDt_" + item]) ? (DateTime?)null : Convert.ToDateTime(frm["EffectiveDt_" + item]);
                    bool IsExpired = false;
                    IsExpired = DateTime.Now.Date < Convert.ToDateTime(_clientPackrate.ExpiredDt.Value).Date ? false : true;

                    if (Approve) // If approve is true add package client rate
                    {
                        tbl_package_client_rates ObjPackage = new tbl_package_client_rates();
                        ObjPackage.ClientId = ClientId;
                        ObjPackage.LocationId = LocationId;
                        ObjPackage.VehicleTypeId = VehTypeId;
                        ObjPackage.VehicleModelId = VehModelId;
                        ObjPackage.EffectiveDt = EffectiveDt;
                        ObjPackage.CreatedDt = DateTime.Now;
                        ObjPackage.CreatedBy = User.Identity.Name;
                        ObjPackage.PackageAmt = PackageAmt;
                        ObjPackage.ProjectId = ProjectId;
                        ObjPackage.LoginTime = LogIn;
                        ObjPackage.LogoutTime = LogOut;
                        ObjPackage.LocationId = LocationId;
                        ObjPackage.WorkingHrs = 0;
                        ObjPackage.TimeUnit = TimeUnit;
                        ObjPackage.ExtHr = 0;
                        ObjPackage.ExtKM = 0;
                        ObjPackage.PackageKM = 0;
                        ObjPackage.PackModel = "";
                        ObjPackage.Approved = Approve;
                        ObjPackage.IsActive = true;

                        if (TimeUnit == 1) // 1 = Days 
                        {
                            ObjPackage.ExpiredDt = EffectiveDt.HasValue ? EffectiveDt.Value.AddDays(WorkingDays) : (DateTime?)null;
                            ObjPackage.WorkingDays = WorkingDays;
                        }
                        else if (TimeUnit == 2) // 2 = Months 
                        {
                            ObjPackage.ExpiredDt = EffectiveDt.HasValue ? EffectiveDt.Value.AddMonths(WorkingDays) : (DateTime?)null;
                            ObjPackage.WorkingDays = WorkingDays;
                        }

                        db.tbl_package_client_rates.AddObject(ObjPackage);
                    }
                    else if (Approve == false)  //  && IsExpired == false
                    {
                        TryUpdateModel(_clientPackrate);

                        if (_clientPackrate.TimeUnit != TimeUnit || _clientPackrate.WorkingDays != WorkingDays)
                        {
                            _clientPackrate.ExpiredDt = (TimeUnit == 1 ? _clientPackrate.EffectiveDt.Value.AddDays(WorkingDays) : _clientPackrate.EffectiveDt.Value.AddMonths(WorkingDays));
                        }
                        _clientPackrate.LoginTime = LogIn;
                        _clientPackrate.LogoutTime = LogOut;
                        _clientPackrate.VehicleTypeId = VehTypeId;
                        _clientPackrate.VehicleModelId = VehModelId;
                        _clientPackrate.TimeUnit = TimeUnit;
                        _clientPackrate.WorkingDays = WorkingDays;
                        _clientPackrate.PackageAmt = PackageAmt;
                        _clientPackrate.LocationId = LocationId;
                        _clientPackrate.EffectiveDt = EffectiveDt;

                        

                        // Add Log here 

                        tbl_package_client_rates_log objPackageLog = new tbl_package_client_rates_log();
                        objPackageLog.PackId = item;
                        objPackageLog.VehicleTypeId = VehTypeId;
                        objPackageLog.VehicleModelId = VehModelId;
                        objPackageLog.LocationId = LocationId;
                        objPackageLog.PackageAmt = PackageAmt;
                        objPackageLog.TimeUnit = TimeUnit;
                        objPackageLog.WorkingDays = WorkingDays;
                        objPackageLog.ModifiedBy = User.Identity.Name;
                        objPackageLog.ModifiedDt = DateTime.Now;
                        objPackageLog.EffectiveDt = EffectiveDt;
                        objPackageLog.LoginTime = LogIn;
                        objPackageLog.LogoutTime = LogOut;
                        objPackageLog.IsActive = true;
                        objPackageLog.ExpiredDt = _clientPackrate.ExpiredDt;

                        db.tbl_package_client_rates_log.AddObject(objPackageLog);
                    }
                }

                db.SaveChanges();
            }
            catch (Exception ex)
            {
                core.ExceptionLogging(ex.Message.ToString(), User.Identity.Name, ex.StackTrace);
                return Json(new { success = false, msg = "An error occured while processing your request.Please try again" });
            }
            return Json(new { success = true, msg = "Package(s) has been approved successfully" });
        }                                                                                        

        public JsonResult GetProjectsByClient(long ClientId)
        {
            try
            {
                var ProjectDet = db.tbl_projects.Where(a => a.ClientId == ClientId && a.IsActive == true).Select(a => new { Id = a.Id, ProjectName = a.ProjectName }).ToList();
                return Json(ProjectDet, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json((new { Id = 0, ProjectName = "No records" }), JsonRequestBehavior.AllowGet);
            }
        }

        //public JsonResult GetTimeUnitByProject(Int64 ProjectId)
        //{
        //    try
        //    {
        //        var ProjectDet = db.tbl_projects.Where(a => a.Id == ProjectId && a.IsActive == true).FirstOrDefault();
        //        return Json(new { TimeUnit = (ProjectDet.TimeUnit == 1 ? "Days" : "Months") }, JsonRequestBehavior.AllowGet);
        //    }
        //    catch
        //    {
        //        return Json((new { TimeUnit = "" }), JsonRequestBehavior.AllowGet);
        //    }
        //}


        #region Quick Package Entry 

        public ActionResult AddQuickPackage(Int64 ClientId,Int64 ProjectId)
        {
            ViewBag.VehicleTypeID = objCore.LoadVehicleTypes();
            ViewBag.VehicleModelId = objCore.LoadVehicleModels();
            ViewBag.TimeUnit = objCore.LoadTimeUnit();
            ViewBag.ProjectId = ProjectId;
            ViewBag.ClientId = ClientId;
            return PartialView("_AddQuickPackage");
        }

        public JsonResult SavePackageDetails(Int64 ClientId, Int64 ProjectId, FormCollection frm)
        {
            try
            {

                // Get Paramters 
                string RouteId = string.IsNullOrEmpty(frm["RouteId"]) ? string.Empty : Convert.ToString(frm["RouteId"]);
                string Location = string.IsNullOrEmpty(frm["Location"]) ? string.Empty : Convert.ToString(frm["Location"]);
                Int64 LocationId = string.IsNullOrEmpty(frm["LocationId"]) ? 0 : Convert.ToInt64(frm["LocationId"]);
                Int64 VehicleTypeId = Convert.ToInt64(frm["VehicleTypeId"]);
                Int64 VehicleModelId = Convert.ToInt64(frm["VehicleModelId"]);
                decimal PackRate = Convert.ToDecimal(frm["PackageAmt"]);
                Int16 TimeUnit = Convert.ToInt16(frm["TimeUnit"]);
                Int32 WorkingDays = Convert.ToInt32(frm["WorkingDays"]);
                DateTime? EffectedDt = string.IsNullOrEmpty(frm["EffectiveDt"]) ? (DateTime?)null : Convert.ToDateTime(frm["EffectiveDt"]);

                // If No Location Create New
                if (LocationId == 0 && RouteId.Trim().Length > 0 && Location.Trim().Length > 0)
                {
                    tbl_locations locationDet = new tbl_locations();
                    locationDet.Location = Location;
                    locationDet.RouteId = RouteId;
                    locationDet.Status = true;
                    db.tbl_locations.AddObject(locationDet);
                    db.SaveChanges();
                    
                    // Assign New  Location Id 
                    LocationId = locationDet.ID;
                }

                if (LocationId != 0)  // Verifying form field values
                {

                    tbl_package_client_rates packDet = new tbl_package_client_rates();
                    packDet.ClientId = ClientId;
                    packDet.ProjectId = ProjectId;
                    packDet.LocationId = LocationId;
                    packDet.VehicleModelId = VehicleModelId;
                    packDet.VehicleTypeId = VehicleTypeId;
                    packDet.TimeUnit = TimeUnit;
                    packDet.PackageAmt = PackRate;
                    packDet.EffectiveDt = EffectedDt;
                    packDet.CreatedDt = DateTime.Now;
                    packDet.CreatedBy = User.Identity.Name;
                    packDet.Approved = true;
                    packDet.PackageKM = 0;
                    packDet.ExtHr = 0;
                    packDet.ExtKM = 0;
                    packDet.PackModel = "";
                    packDet.WorkingHrs = 0;
                    packDet.IsActive = true;

                    if (packDet.TimeUnit == 1) // 1 = Days
                    {
                        packDet.ExpiredDt = packDet.EffectiveDt.HasValue ? packDet.EffectiveDt.Value.AddDays(Convert.ToDouble(WorkingDays)) : (DateTime?)null;
                        packDet.WorkingDays = WorkingDays;
                    }
                    else if (packDet.TimeUnit == 2)
                    {
                        packDet.ExpiredDt = packDet.EffectiveDt.HasValue ? packDet.EffectiveDt.Value.AddMonths(Convert.ToInt32(WorkingDays)) : (DateTime?)null;
                        packDet.WorkingDays = WorkingDays;
                    }

                    db.tbl_package_client_rates.AddObject(packDet);
                    db.SaveChanges();
                }
            }
            catch (Exception)
            {
                return Json(new { success = false, msg = "An error occured while proccessing your request" }); //  failure
            }
            return Json(new { success = true, msg = "Package rate has been added successfully" });  // Success
        }

        public string getAjaxLocationSearch(string f, string q)
        {
            f = f == "undefined" ? "Location" : f;
            StringBuilder str = new StringBuilder();
            var client = from a in db.tbl_locations
                                    .Where(a => a.Status.Value == true)
                                    .Where<tbl_locations>(f, q, WhereOperation.Contains)
                         select new { a.Location, a.ID };

            foreach (var a in client)
            {
                str.Append(string.Format("{0}|{1}\r\n", a.Location.ToUpper(), a.ID)).ToString();
                ViewBag.LocatioId = a.ID;
            }
            return str.ToString();
        }


        public string GetRouteIdbyLocationId(Int64 LocationId)
        {
            tbl_locations locationDet = db.tbl_locations.Where(a => a.Status == true && a.ID == LocationId).FirstOrDefault();
            return (string.IsNullOrEmpty(locationDet.RouteId) ? "" : locationDet.RouteId);
        }

        #endregion

        #endregion

    }

}