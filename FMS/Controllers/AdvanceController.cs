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
    public class AdvanceController : Controller
    {

        #region ctors
        // GET: /Advance/
        private FMSDBEntities db;
        private core objCore = new core();
        private List<tbl_advances> _advanceList = new List<tbl_advances>();
        public AdvanceController()
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
        public JsonResult GetAdvances(int iDisplayStart, int iDisplayLength, string sSearch, int iColumns,
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
                    l.Text8.ToString(),
                    l.Text9.ToString(),
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
            var tempList = (from l in db.tbl_advances
                            where l.Status == true && (l.ClosedFlag == false || l.ClosedFlag == null)
                            select l).ToList();
            foreach (tbl_advances ds in tempList)
            {
                list.Add(new CommonFields
                {
                    Id = ds.ID,
                    Date1 = Convert.ToDateTime(ds.CreatedDate),
                    Text1 = ds.tbl_vendor_details.FirstName + " " + ds.tbl_vendor_details.LastName,
                    Text2 = ds.tbl_vehicles.VehicleRegNum,
                    Text3 = ds.tbl_drivers.FirstName + " " + ds.tbl_drivers.LastName,
                    Text4 = ds.Towards == null ? "" : ds.Towards,
                    Text5 = ds.PayMode,
                    Text6 = ds.ChequeDDNo == null ? "" : ds.ChequeDDNo,
                    Text7 = ds.ChequeDate == null ? "" : ds.ChequeDate.Value.ToShortDateString(),
                    Text8 = ds.BankName == null ? "" : ds.BankName,
                    Text9 = ds.Branch == null ? "" : ds.Branch,
                    Amount = ds.Amount == null ? 0 : (decimal)ds.Amount,
                    Text10 = ds.TransactionNum == null ? "" : ds.TransactionNum,
                    Status = (bool)ds.Status
                });
            }
            objCore.ConvertToUppercase<CommonFields>(list);
            return list.OrderBy(a => a.Id);
        }  
        #endregion

        #region Add Advance
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
                // List of Advances 
                List<tbl_advances> _advanceList = (List<tbl_advances>)Session["AdvancesList"];
                objCore.ConvertToUppercase<tbl_advances>(_advanceList);
                foreach (tbl_advances obj in _advanceList)
                    db.tbl_advances.AddObject(obj);
                db.SaveChanges();
                Session["AdvancesList"] = null;
                objCore.LoggingEntries("Advances", "Advances has been created ", User.Identity.Name);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Message = "An Error occured :" + ex.Message.ToString();
                return View();
            }
        }
        [HttpGet]
        public ActionResult AddNewAdvanceDetails()
        {
            ViewBag.VehicleID = db.tbl_vehicles.Where(a => a.Status == true).Select(a => new { a.ID, a.VehicleRegNum }).ToDictionary(a => a.ID, a => a.VehicleRegNum.ToUpper());
            return PartialView("_AddNewAdvanceDetails");
        }
        [HttpPost]
        public ActionResult AddNewAdvanceDetails(FormCollection frm, long VID, tbl_advances advances)
        {
            long ID = 0;
            _advanceList = (List<tbl_advances>)Session["AdvancesList"];
            if (_advanceList == null)
                _advanceList = new List<tbl_advances>();
            if (_advanceList.Count() == 0)
                ID = 1;
            else
                ID = _advanceList.Count() + 1;
            advances.VehicleRegNum = db.tbl_vehicles.Where(a => a.Status == true && a.ID == VID).SingleOrDefault().VehicleRegNum.ToUpper().Trim();
            advances.PayMode = frm["PayMode"].ToString().ToUpper().Trim();
            if (advances.PayMode.ToString().ToUpper() == "CHEQUE")
                _advanceList.Add(new tbl_advances { ID = ID, VehicleID = VID, DriverID = advances.DriverID, VendorID = advances.VendorID, VehicleRegNum = advances.VehicleRegNum, CreatedDate = advances.CreatedDate, PayMode = advances.PayMode, Narration = advances.Narration, Amount = advances.Amount, Status = true, Towards = advances.Towards, ChequeDDNo = advances.ChequeDDNo, ChequeDate = advances.ChequeDate, BankName = advances.BankName, Branch = advances.Branch });
            else if (advances.PayMode.ToString().ToUpper() == "TRANSFER")
                _advanceList.Add(new tbl_advances { ID = ID, VehicleID = VID, DriverID = advances.DriverID, VendorID = advances.VendorID, VehicleRegNum = advances.VehicleRegNum, CreatedDate = advances.CreatedDate, PayMode = advances.PayMode, Narration = advances.Narration, Amount = advances.Amount, Status = true, Towards = advances.Towards, TransactionNum = advances.TransactionNum });
            else
                _advanceList.Add(new tbl_advances { ID = ID, VehicleID = VID, DriverID = advances.DriverID, VendorID = advances.VendorID, VehicleRegNum = advances.VehicleRegNum, CreatedDate = advances.CreatedDate, PayMode = advances.PayMode, Narration = advances.Narration, Amount = advances.Amount, Status = true, Towards = advances.Towards });
            Session["AdvancesList"] = _advanceList;
            return PartialView("_AdvancesList", _advanceList);
        }
        #endregion

        #region Edit Advance
        [HttpGet]
        public ActionResult EditAdvance(long Id)
        {
            tbl_advances _advances = db.tbl_advances.Where(a => a.ID == Id).SingleOrDefault();
            return PartialView("_EditAdvance", _advances);
        }
        [HttpPost]
        public ActionResult EditAdvance(long Id, FormCollection frm, tbl_advances advanceDetails)
        {
            tbl_advances advanceDet = db.tbl_advances.Where(a => a.ID == Id && a.Status == true).SingleOrDefault();
            try
            {
                TryUpdateModel(advanceDet);
                objCore.ConvertToUppercase(advanceDet);
                if (advanceDetails.PayMode.ToString().ToUpper() == "CASH" || advanceDetails.PayMode.ToString().ToUpper() == "TRANSFER")
                {
                    advanceDet.ChequeDate = null;
                    advanceDet.ChequeDDNo = null;
                    advanceDet.BankName = null;
                    advanceDet.Branch = null;
                }
                if (advanceDetails.PayMode.ToString().ToUpper() == "CASH" || advanceDetails.PayMode.ToString().ToUpper() == "CHEQUE")
                    advanceDet.TransactionNum = null;
                advanceDet.VendorID = advanceDetails.VendorID;
                advanceDet.DriverID = advanceDetails.DriverID;
                advanceDet.VehicleID = frm["cid"] == null ? 0 : Convert.ToInt64(frm["cid"]);
                advanceDet.VehicleRegNum = db.tbl_vehicles.Where(a => a.ID == advanceDet.VehicleID).SingleOrDefault().VehicleRegNum.ToUpper();
                db.SaveChanges();
                objCore.LoggingEntries("Advances", "Advance has Updated with driver " + advanceDet.tbl_drivers.FirstName + " " + advanceDet.tbl_drivers.LastName + " ", User.Identity.Name);
                return Content("<script language='javascript' type='text/javascript'>$.modal.close();alert('Advance saved successfully');window.location.href = '/Advance/Index'</script>");
            }
            catch (Exception ex)
            {
                ViewBag.Message = "An Error occured ==>" + ex.Message.ToString();
                return PartialView("_EditAdvance", advanceDet);
            }
        } 
        #endregion

        #region Delete Advance
        // Delete Advance 
        public ActionResult Delete(long Id)
        {
            tbl_advances advanceDet = db.tbl_advances.Where(a => a.ID == Id).SingleOrDefault();
            return View(advanceDet);
        }
        [HttpPost]
        public ActionResult Delete(long Id, FormatException frm)
        {
            tbl_advances advanceDet = db.tbl_advances.Where(a => a.ID == Id).SingleOrDefault();
            try
            {
                TryUpdateModel(advanceDet);
                advanceDet.Status = false;
                db.SaveChanges();
                objCore.LoggingEntries("Advances", "Advance Deleted with driver name is " + advanceDet.tbl_drivers.FirstName + " " + advanceDet.tbl_drivers.LastName + " ", User.Identity.Name);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Message = "An Error occured ==> " + ex.Message.ToString();
                return View(advanceDet);
            }
        }
        public ActionResult RemoveItem(long ID)
        {
            _advanceList = (List<tbl_advances>)Session["AdvancesList"];
            foreach (tbl_advances obj in _advanceList)
            {
                if (obj.ID == ID) // Will match once
                {
                    //Delete the Row
                    _advanceList.Remove(obj);
                    ReShuffleSlno();
                    break;
                }
            }
            return PartialView("_AdvancesList", _advanceList);
        }
        public void ReShuffleSlno()
        {
            var i = 1;
            foreach (tbl_advances obj in _advanceList)
            {
                obj.ID = i;
                i++;
            }

        } 
        #endregion

        #region CustomMethods
        public string getVehicleAjaxResult(string f, string q)
        {
            f = f == "undefined" ? "VehicleRegNum" : f;
            StringBuilder str = new StringBuilder();
            var vehicle = from a in db.tbl_vehicles
                                   .Where(a => a.Status.Value == true)
                                   .Where<tbl_vehicles>(f, q, WhereOperation.Contains)
                          select new { a.VehicleRegNum, a.ID };
            foreach (var a in vehicle)
            {
                str.Append(string.Format("{0} |{1}\r\n", a.VehicleRegNum.ToUpper(), a.ID)).ToString();
                ViewBag.ClientID = a.ID;
            }
            return str.ToString();
        }

        public JsonResult GetCurrentVendor(long VendorID, long ID)
        {
            var log = db.tbl_advances.Where(a => a.Status == true && a.ID == ID).SingleOrDefault();
            var vendor = db.tbl_vendor_details.Where(s => s.Status == true && s.ID == log.VendorID).Select(a => new { a.FirstName, a.LastName, a.ID }).ToList();
            return Json(vendor, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetCurrentVehicleDriver(long VehicleID, long ID)
        {
            var log = db.tbl_advances.Where(a => a.Status == true && a.ID == ID).SingleOrDefault();
            var vDrivers = db.tbl_drivers.Where(s => s.Status == true && s.ID == log.DriverID).Select(a => new { a.FirstName, a.LastName, a.ID }).ToList();
            return Json(vDrivers, JsonRequestBehavior.AllowGet);
        }
        #endregion
    }
}
