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
    public class DepositsController : Controller
    {
        #region ctors
        private FMSDBEntities db;
        private core objCore = new core();
        private List<tbl_deposits_deductions> _depositList = new List<tbl_deposits_deductions>();
        public DepositsController()
        {
            db = new FMSDBEntities();
        } 
        #endregion

        #region Index Methods
        public ActionResult Index()
        {
            var deposits = db.tbl_deposits_deductions.Where(a => a.Status == true).ToList();
            return View(deposits);
        }
        public JsonResult GetDeposits(int iDisplayStart, int iDisplayLength, string sSearch, int iColumns,
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
            var tempList = (from l in db.tbl_deposits_deductions
                            where l.Status == true
                            select l).ToList();
            foreach (tbl_deposits_deductions ds in tempList)
            {
                list.Add(new CommonFields
                {
                    Id = ds.Id,
                    Date1 = Convert.ToDateTime(ds.CreatedDt),
                    Text1 = ds.tbl_vendor_details.FirstName + " " + ds.tbl_vendor_details.LastName,
                    Text2 = ds.tbl_vehicles.VehicleRegNum,
                    Text3 = ds.tbl_drivers.FirstName + " " + ds.tbl_drivers.LastName,
                    Text5 = ds.DepositMode,
                    Text6 = ds.ChequeNo == null ? "" : ds.ChequeNo,
                    Text7 = ds.ChequeDt == null ? "" : ds.ChequeDt.Value.ToShortDateString(),
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

        #region Add Deposits
        [HttpGet]
        [Authorize]
        public ActionResult Create()
        {
            return View();
        }

        [HttpGet]
        public ActionResult AddNewDepositDetails()
        {
            ViewBag.VehicleID = db.tbl_vehicles.Where(a => a.Status == true).Select(a => new { a.ID, a.VehicleRegNum }).ToDictionary(a => a.ID, a => a.VehicleRegNum.ToUpper());
            return PartialView("_AddNewDepositDetails");
        }
        [HttpPost]
        public ActionResult AddNewDepositDetails(FormCollection frm, long VID, tbl_deposits_deductions deposits)
        {
            long ID = 0; decimal fixedAmt = 0, availDeposit = 0;
            _depositList = (List<tbl_deposits_deductions>)Session["DepositsList"];
            if (_depositList == null)
                _depositList = new List<tbl_deposits_deductions>();
            if (_depositList.Count() == 0)
                ID = 1;
            else
                ID = _depositList.Count() + 1;
            // Verify Deposit amount with total fixed deposit in vendor master
            tbl_vendor_details venDet = db.tbl_vendor_details.Where(a => a.ID == deposits.VendorId).FirstOrDefault();
            fixedAmt = venDet.FixedDeposit == null ? 0 : Convert.ToDecimal(venDet.FixedDeposit);
            if (fixedAmt != 0)
            {
                var _availDeposits = _depositList.Where(a => a.VehicleId == VID && a.VendorId == deposits.VendorId && a.DriverId == deposits.DriverId).ToList();

                if (decimal.Compare(fixedAmt, Convert.ToDecimal(deposits.Amount) + _availDeposits.Sum(a => a.Amount) ?? 0) == decimal.MinusOne)
                    return Json(new { success = false, msg = "Deposited amount is greater than the available deposit amount in the owner master", model = _depositList });

                var _existedDeposits = db.tbl_deposits_deductions.Where(a => a.VehicleId == deposits.VehicleId && a.VendorId == deposits.VendorId && a.DriverId == deposits.DriverId).ToList();
                availDeposit = _existedDeposits.Sum(a => a.Amount) ?? 0;

                if (decimal.Compare(fixedAmt, availDeposit) == decimal.MinusOne)
                    return Json(new { success = false, msg = "Deposited amount is greater than the available deposit amount in the owner master", model = _depositList });

                availDeposit += _availDeposits.Sum(a => a.Amount) ?? 0;

                if (decimal.Compare(fixedAmt, availDeposit) == decimal.MinusOne)
                    return Json(new { success = false, msg = "Deposited amount is exceeded than the available deposit amount in the owner master", model = _depositList });
            }
            deposits.DepositMode = frm["DepositMode"].ToString().ToUpper().Trim();
            if (deposits.DepositMode.ToString().ToUpper() == "CHEQUE")
                _depositList.Add(new tbl_deposits_deductions { Id = ID, VehicleId = VID, DriverId = deposits.DriverId, VendorId = deposits.VendorId, CreatedDt = deposits.CreatedDt, DepositMode = deposits.DepositMode.ToUpper(), Amount = deposits.Amount, ChequeNo = deposits.ChequeNo, ChequeDt = deposits.ChequeDt, BankName = deposits.BankName, Branch = deposits.Branch, Status = true, CreatedBy = User.Identity.Name });
            else if (deposits.DepositMode.ToString().ToUpper() == "TRANSFER")
                _depositList.Add(new tbl_deposits_deductions { Id = ID, VehicleId = VID, DriverId = deposits.DriverId, VendorId = deposits.VendorId, CreatedDt = deposits.CreatedDt, DepositMode = deposits.DepositMode.ToUpper(), Amount = deposits.Amount, TransactionNum = deposits.TransactionNum, Status = true, CreatedBy = User.Identity.Name });
            else
                _depositList.Add(new tbl_deposits_deductions { Id = ID, VehicleId = VID, DriverId = deposits.DriverId, VendorId = deposits.VendorId, CreatedDt = deposits.CreatedDt, DepositMode = deposits.DepositMode.ToUpper(), Amount = deposits.Amount, Status = true, CreatedBy = User.Identity.Name });
            Session["DepositsList"] = _depositList;
            return Json(new { success = true, msg = "", model = _depositList });
        }

        [HttpGet]
        public ActionResult GetDepositList()
        {
            List<tbl_deposits_deductions> depositList = new List<tbl_deposits_deductions>();
            depositList = (List<tbl_deposits_deductions>)Session["DepositsList"];
            return PartialView("_DepositList", depositList);
        }

        [HttpPost]
        public ActionResult Create(FormCollection frm)
        {
            try
            {
                // List of Advances 
                List<tbl_deposits_deductions> _depositList = (List<tbl_deposits_deductions>)Session["DepositsList"];
                objCore.ConvertToUppercase<tbl_deposits_deductions>(_depositList);
                foreach (tbl_deposits_deductions obj in _depositList)
                    db.tbl_deposits_deductions.AddObject(obj);
                db.SaveChanges();
                Session["DepositsList"] = null;
                objCore.LoggingEntries("Deposit", "Deposit(s) has been created ", User.Identity.Name);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Message = "An error occured while proccessing your request " + ex.Message.ToString();
                return View();
            }
        }  
        #endregion

        #region Edit Deposits
        [HttpGet]
        public ActionResult EditDeposit(long Id)
        {
            tbl_deposits_deductions _deposits = db.tbl_deposits_deductions.Where(a => a.Id == Id).SingleOrDefault();
            ViewBag.VehicleID = db.tbl_vehicles.Where(a => a.Status == true).Select(a => new { a.ID, a.VehicleRegNum }).ToDictionary(a => a.ID, a => a.VehicleRegNum.ToUpper());
            return PartialView("_EditDeposit", _deposits);
        }
        [HttpPost]
        public ActionResult EditDeposit(FormCollection frm, long VID, tbl_deposits_deductions deposits)
        {
            tbl_deposits_deductions _deposits = db.tbl_deposits_deductions.Where(a => a.Id == deposits.Id).SingleOrDefault();
            try
            {
                deposits.VehicleId = VID;
                deposits.DepositMode = deposits.DepositMode.ToUpper();
                TryUpdateModel(_deposits);
                objCore.ConvertToUppercase(_deposits);
                if (deposits.DepositMode.ToString().ToUpper() == "CASH" || deposits.DepositMode.ToString().ToUpper() == "TRANSFER")
                {
                    _deposits.ChequeDt = null;
                    _deposits.ChequeNo = null;
                    _deposits.BankName = null;
                    _deposits.Branch = null;
                }
                if (deposits.DepositMode.ToString().ToUpper() == "CASH" || deposits.DepositMode.ToString().ToUpper() == "CHEQUE")
                    _deposits.TransactionNum = null;
                db.SaveChanges();
                objCore.LoggingEntries("Deposits", "Deposit has Updated with driver " + _deposits.tbl_drivers.FirstName + " " + _deposits.tbl_drivers.LastName + " ", User.Identity.Name);
                return Json(new { success = true, msg = "Deposit has been saved successfully." });
            }
            catch
            {
                return Json(new { success = false, msg = "An error occured while processing your request." });
            }

        } 
        #endregion

        #region Delete Deposits
        // Delete Advance 
        public ActionResult Delete(long Id)
        {
            tbl_deposits_deductions depositDet = db.tbl_deposits_deductions.Where(a => a.Id == Id).SingleOrDefault();
            return View(depositDet);
        }
        [HttpPost]
        public ActionResult Delete(long Id, FormatException frm)
        {
            tbl_deposits_deductions depositDet = db.tbl_deposits_deductions.Where(a => a.Id == Id).SingleOrDefault();
            try
            {
                TryUpdateModel(depositDet);
                depositDet.Status = false;
                db.SaveChanges();
                objCore.LoggingEntries("Deposits", "Deposit has deleted with driver name is " + depositDet.tbl_drivers.FirstName + " " + depositDet.tbl_drivers.LastName + " ", User.Identity.Name);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Message = "An Error occured ==> " + ex.Message.ToString();
                return View(depositDet);
            }
        } 
        #endregion

        #region Custom Methods
        public int UnSavedDepositsEntries()
        {
            try
            {
                // List of Advances 
                List<tbl_deposits_deductions> _depositList = (List<tbl_deposits_deductions>)Session["DepositsList"];
                objCore.ConvertToUppercase<tbl_deposits_deductions>(_depositList);
                foreach (tbl_deposits_deductions obj in _depositList)
                    db.tbl_deposits_deductions.AddObject(obj);
                db.SaveChanges();
                Session["DepositsList"] = null;
                objCore.LoggingEntries("Deposit", "Deposit(s) has been created ", User.Identity.Name);
                return 1;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }
        [HttpGet]
        public decimal GetFixedDepositByVendor(long vendorId)
        {
            try
            {
                tbl_vendor_details vendorDet = db.tbl_vendor_details.Where(a => a.ID == vendorId).FirstOrDefault();
                return vendorDet.FixedDeposit ?? 0;
            }
            catch
            {
                return 0;
            }

        } 
        #endregion
    }
}
