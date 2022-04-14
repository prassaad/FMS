using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;
using System.Text;
using FMS.Helpers;

namespace FMS.Controllers
{
    [CustomAuthorizationFilter]
    public class PenaltyController : Controller
    {
        #region ctors
        private FMSDBEntities db;
        private List<tbl_penalties> _penaltyList;
        private core core = new core();
        public PenaltyController()
        {
            db = new FMSDBEntities();
        } 
        #endregion

        #region Index Methods
        [Authorize]
        public ActionResult Index()
        {
            return View();
        }
        public JsonResult GetPenaltyList(int iDisplayStart, int iDisplayLength, string sSearch, int iColumns,
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
                    l.Text5.ToString(),
                    l.Text6.ToString(),
                    l.Text7.ToString(),
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
            var tempList = (from l in db.tbl_penalties
                            where l.Status == true && (l.ClosedFlag == false || l.ClosedFlag == null)
                            select l).ToList();
            foreach (tbl_penalties ds in tempList)
            {
                list.Add(new CommonFields
                {
                    Id = ds.ID,
                    Date1 = Convert.ToDateTime(ds.CreatedDate),
                    Text1 = ds.tbl_log_sheets.LogSheetNum,
                    Text2 = ds.tbl_log_sheets.ShiftTime,
                    Text3 = ds.tbl_log_sheets.TripeType,
                    Text4 = ds.tbl_log_sheets.tbl_vehicles.VehicleRegNum,
                    Text5 = ds.tbl_log_sheets.tbl_seaters.Seater,
                    Text6 = ds.tbl_log_sheets.Pax == null ? "0" : ds.tbl_log_sheets.Pax.ToString(),
                    Text7 = ds.tbl_log_sheets.Location == null ? "" : ds.tbl_log_sheets.Location,
                    Amount = ds.PenalityAmt == null ? 0 : (decimal)ds.PenalityAmt,
                    Text10 = ds.Remark == null ? "" : ds.Remark,
                    Status = (bool)ds.Status
                });
            }
            core.ConvertToUppercase<CommonFields>(list);
            return list.OrderBy(a => a.Id);
        }  
        #endregion

        #region Add Penalties
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
                List<tbl_penalties> _penaltyList = (List<tbl_penalties>)Session["PenaltyList"];
                core.ConvertToUppercase<tbl_penalties>(_penaltyList);
                foreach (tbl_penalties obj in _penaltyList)
                    db.tbl_penalties.AddObject(obj);
                db.SaveChanges();
                Session["PenaltyList"] = null;
                core.LoggingEntries("Penalty Mgmt.", "Penalty(s) has created", User.Identity.Name);
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
        public ActionResult AddNewPenaltyDetails()
        {
            return PartialView("_AddNewPenaltyDetails");
        }
        [HttpPost]
        public ActionResult AddNewPenaltyDetails(FormCollection frm, long LogID, tbl_penalties penalty)
        {
            long ID = 0;
            _penaltyList = (List<tbl_penalties>)Session["PenaltyList"];
            if (_penaltyList == null)
                _penaltyList = new List<tbl_penalties>();
            if (_penaltyList.Count() == 0)
                ID = 1;
            else
                ID = _penaltyList.Count() + 1;
            penalty.LogSheetNum = db.tbl_log_sheets.Where(a => a.ID == LogID).SingleOrDefault().LogSheetNum;
            _penaltyList.Add(new tbl_penalties { ID = ID, LogSheetID = LogID, LogSheetNum = penalty.LogSheetNum, CreatedDate = penalty.CreatedDate, PenalityAmt = penalty.PenalityAmt, Remark = penalty.Remark, Status = true });
            Session["PenaltyList"] = _penaltyList;
            return PartialView("_PenaltyList", _penaltyList);
        } 
        #endregion

        #region Edit Penalty
        [HttpGet]
        public ActionResult EditPenalty(long Id)
        {
            tbl_penalties _penalty = db.tbl_penalties.Where(a => a.ID == Id).SingleOrDefault();
            return PartialView("_EditPenalty", _penalty);
        }
        [HttpPost]
        public ActionResult EditPenalty(long Id, long logId, FormCollection frm, tbl_penalties penaltyDetails)
        {
            tbl_penalties penaltyDet = db.tbl_penalties.Where(a => a.ID == Id && a.Status == true).SingleOrDefault();
            try
            {
                TryUpdateModel(penaltyDet);
                core.ConvertToUppercase(penaltyDet);
                penaltyDet.LogSheetID = logId;
                penaltyDet.LogSheetNum = db.tbl_log_sheets.Where(a => a.ID == logId && a.Status == true).SingleOrDefault().LogSheetNum.ToUpper().Trim();
                db.SaveChanges();
                core.LoggingEntries("Penalty Mgmt.", "Penalty(s) has Updated for the vehicle " + penaltyDetails.tbl_log_sheets.tbl_vehicles.VehicleRegNum + "", User.Identity.Name);
                return Content("<script language='javascript' type='text/javascript'>$.modal.close();alert('Penalty saved successfully');window.location.href = '/Penalty/Index'</script>");
            }
            catch (Exception ex)
            {
                ViewBag.Message = "An Error occured ==>" + ex.Message.ToString();
                return PartialView("_EditPenalty", penaltyDet);
            }
        } 
        #endregion

        #region Delete Penalty
        // Delete Penalty 
        public ActionResult Delete(long Id)
        {
            tbl_penalties PenaltyDet = db.tbl_penalties.Where(a => a.ID == Id).SingleOrDefault();
            return View(PenaltyDet);
        }
        [HttpPost]
        public ActionResult Delete(long Id, FormatException frm)
        {
            tbl_penalties penaltyDet = db.tbl_penalties.Where(a => a.ID == Id).SingleOrDefault();
            try
            {
                TryUpdateModel(penaltyDet);
                penaltyDet.Status = false;
                db.SaveChanges();
                core.LoggingEntries("Penalty Mgmt.", "Penalty has deleted for the vehicle " + penaltyDet.tbl_log_sheets.tbl_vehicles.VehicleRegNum + "", User.Identity.Name);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Message = "An Error occured ==> " + ex.Message.ToString();
                return View(penaltyDet);
            }
        } 
        #endregion

        #region CustomMethods 
            // getLogSheetNumber Ajax Call for Auto Complete 

        public string getLogSheetNumber(string f, string q)
        {
            f = f == "undefined" ? "LogSheetNum" : f;
            StringBuilder str = new StringBuilder();
            var vehicle = from a in db.tbl_log_sheets
                                   .Where(a => a.Status.Value == true)
                                   .Where<tbl_log_sheets>(f, q, WhereOperation.Contains)
                          select new { a.LogSheetNum, a.ID };
            foreach (var a in vehicle)
            {
                str.Append(string.Format("{0} |{1}\r\n", a.LogSheetNum, a.ID)).ToString();
            }
            return str.ToString();
        }

        // LogSheet Details By LogSheet Id 

        public JsonResult GetLogSheetDetails(long LogId)
        {
            var LogSheetDet = db.tbl_log_sheets.Where(s => s.Status == true && s.ID == LogId).Select(a => new { a.tbl_vehicles.VehicleRegNum, a.ShiftTime, a.TripeType, a.tbl_seaters.Seater, a.Location , a.Pax}).SingleOrDefault();
            LogSheetDetail _logSheet = new LogSheetDetail();
            _logSheet.VehicleRegNum = LogSheetDet.VehicleRegNum;
            _logSheet.Seater = LogSheetDet.Seater;
            _logSheet.Location = LogSheetDet.Location;
            _logSheet.LogTime = LogSheetDet.ShiftTime.ToString();
            _logSheet.TripType = LogSheetDet.TripeType.ToString();
            _logSheet.ActualPax = LogSheetDet.Pax.ToString();
            core.ConvertToUppercase(_logSheet);
            return Json(_logSheet, JsonRequestBehavior.AllowGet);
        }

        #endregion
    }
}
