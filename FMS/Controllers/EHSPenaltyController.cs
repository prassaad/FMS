using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;
using FMS.Helpers;

namespace FMS.Controllers
{
    [CustomAuthorizationFilter]
    public class EHSPenaltyController : Controller
    {

        #region ctors
        private FMSDBEntities db;
        private List<tbl_ehs_details> _EHSPenaltyList;
        core objCore = new core();
        public EHSPenaltyController()
        {
            db = new FMSDBEntities();
        } 
        #endregion

        #region Index List
        // GET: /EHSPenalty/
        [Authorize]
        public ActionResult Index()
        {
            return View();
        }
        public JsonResult GetEHSPenaltyList(int iDisplayStart, int iDisplayLength, string sSearch, int iColumns,
      int iSortingCols, int iSortCol_0, string sSortDir_0,
      int sEcho, string mDataProp_Key)
        {
            var IList = GetEnumerableList();
            var filteredLogSheet = IList
                .Select(l => new[]{
                    l.Id.ToString(),
                    l.Date1.ToShortDateString(),
                    l.Text1.ToString(),
                    l.Text2.ToString(),
                    l.Text3.ToString(),
                    l.Text4.ToString(),
                    l.Amount.ToString(),
                    l.Text10.ToString(),
                    l.Status.ToString()
                    
                }).Where(i => string.IsNullOrEmpty(sSearch)
                     || i.Any(y => y.IndexOf(sSearch, StringComparison.InvariantCultureIgnoreCase) >= 0));
            var orderedlist = filteredLogSheet
                   .OrderByWithDirection(i => (i[iSortCol_0]).Parse(), sSortDir_0 == "asc")
                   .Skip(iDisplayStart)
                   .Take(iDisplayLength);
            var model = new
            {
                aaData = orderedlist,
                iTotalDisplayRecords = filteredLogSheet.Count(),
                iTotalRecords = filteredLogSheet.Count(),
                sEcho = sEcho.ToString()
            };
            return Json(model, JsonRequestBehavior.AllowGet);
        }
        private IEnumerable<CommonFields> GetEnumerableList()
        {
            var list = new List<CommonFields>();
            var tempList = (from l in db.tbl_ehs_details
                            where l.Status == true && (l.ClosedFlag == false || l.ClosedFlag == null)
                            select l).ToList();
            foreach (tbl_ehs_details ds in tempList)
            {
                list.Add(new CommonFields
                {
                    Id = ds.ID,
                    Date1 = Convert.ToDateTime(ds.CreatedDate),
                    Text1 = ds.tbl_clients.CompanyName,
                    Text2 = ds.tbl_vehicles.tbl_vendor_details.FirstName + " " + ds.tbl_vehicles.tbl_vendor_details.LastName,
                    Text3 = ds.tbl_vehicles.VehicleRegNum,
                    Text4 = ds.tbl_ehs_codes.EHSCode,
                    Amount = ds.EHSAmt == null ? 0 : (decimal)ds.EHSAmt,
                    Text10 = ds.Remark == null ? "" : ds.Remark,
                    Status = (bool)ds.Status
                });
            }
            objCore.ConvertToUppercase<CommonFields>(list);
            return list.OrderBy(a => a.Id);
        }  
        #endregion

        #region Add EHS Penalty
        [Authorize]
        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(FormCollection frm)
        {
            try
            {
                // List of Penalty 
                List<tbl_ehs_details> _EHSpenaltyList = (List<tbl_ehs_details>)Session["EHSPenaltyList"];
                objCore.ConvertToUppercase<tbl_ehs_details>(_EHSpenaltyList);
                foreach (tbl_ehs_details obj in _EHSpenaltyList)
                    db.tbl_ehs_details.AddObject(obj);
                db.SaveChanges();
                Session["EHSPenaltyList"] = null;
                objCore.LoggingEntries("EHS Penalty", "EHS penalty has created.", User.Identity.Name);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Message = "An Error occured :" + ex.Message.ToString();
                return View();
            }
        }

        // Add New Penalty as Item wise 
        [HttpGet]
        public ActionResult AddNewEHSPenaltyDetails()
        {
            ViewBag.EHSCodeID = objCore.LoadEHSCodes();
            return PartialView("_AddNewEHSPenaltyDetails");
        }
        [HttpPost]
        public ActionResult AddNewEHSPenaltyDetails(FormCollection frm, long cid, long vid, tbl_ehs_details EHSPenalty)
        {
            long ID = 0;
            _EHSPenaltyList = (List<tbl_ehs_details>)Session["EHSPenaltyList"];
            if (_EHSPenaltyList == null)
                _EHSPenaltyList = new List<tbl_ehs_details>();
            if (_EHSPenaltyList.Count() == 0)
                ID = 1;
            else
                ID = _EHSPenaltyList.Count() + 1;
            EHSPenalty.VehicleRegNum = db.tbl_vehicles.Where(a => a.ID == vid && a.Status == true).SingleOrDefault().VehicleRegNum;
            _EHSPenaltyList.Add(new tbl_ehs_details { ID = ID, CreatedDate = EHSPenalty.CreatedDate, ClientID = cid, VehicleID = vid, VehicleRegNum = EHSPenalty.VehicleRegNum, EHSCodeID = EHSPenalty.EHSCodeID, EHSAmt = EHSPenalty.EHSAmt, Remark = EHSPenalty.Remark, Status = true });
            Session["EHSPenaltyList"] = _EHSPenaltyList;
            return PartialView("_EHSPenaltyList", _EHSPenaltyList);
        } 
        #endregion

        #region Edit
        // Edit EHS Penalty

        [HttpGet]
        public ActionResult EditEHSPenalty(long Id)
        {
            tbl_ehs_details _EHSpenalty = db.tbl_ehs_details.Where(a => a.ID == Id).SingleOrDefault();
            ViewBag.ClientID = _EHSpenalty.tbl_clients.CompanyName;
            ViewBag.EHSCodeID = new SelectList(db.tbl_ehs_codes.Where(a => a.Status == true), "ID", "EHSCode", _EHSpenalty.EHSCodeID);
            return PartialView("_EditEHSPenalty", _EHSpenalty);
        }
        [HttpPost]
        public ActionResult EditEHSPenalty(long Id, long cid, long vid, FormCollection frm, tbl_ehs_details penaltyDetails)
        {
            tbl_ehs_details penaltyDet = db.tbl_ehs_details.Where(a => a.ID == Id && a.Status == true).SingleOrDefault();
            try
            {
                TryUpdateModel(penaltyDet);
                objCore.ConvertToUppercase(penaltyDet);
                penaltyDet.ClientID = cid;
                penaltyDet.VehicleID = vid;
                penaltyDet.VehicleRegNum = db.tbl_vehicles.Where(a => a.ID == vid && a.Status == true).SingleOrDefault().VehicleRegNum;
                db.SaveChanges();
                objCore.LoggingEntries("EHS Penalty", "EHS penalty has updated.", User.Identity.Name);
                return Content("<script language='javascript' type='text/javascript'>$.modal.close();alert('EHS Penalty saved successfully');window.location.href = '/EHSPenalty/Index'</script>");
            }
            catch (Exception ex)
            {
                ViewBag.Message = "An Error occured ==>" + ex.Message.ToString();
                return PartialView("_EditEHSPenalty", penaltyDet);
            }
        } 
        #endregion

        #region Delete
        // Delete EHS Penalty 
        public ActionResult Delete(long Id)
        {
            tbl_ehs_details PenaltyDet = db.tbl_ehs_details.Where(a => a.ID == Id).SingleOrDefault();
            return View(PenaltyDet);
        }
        [HttpPost]
        public ActionResult Delete(long Id, FormatException frm)
        {
            tbl_ehs_details penaltyDet = db.tbl_ehs_details.Where(a => a.ID == Id).SingleOrDefault();
            try
            {
                TryUpdateModel(penaltyDet);
                penaltyDet.Status = false;
                db.SaveChanges();
                objCore.LoggingEntries("EHS Penalty", "EHS penalty has Deleted.", User.Identity.Name);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Message = "An Error occured ==> " + ex.Message.ToString();
                return View(penaltyDet);
            }
        } 
        #endregion

    }
}
