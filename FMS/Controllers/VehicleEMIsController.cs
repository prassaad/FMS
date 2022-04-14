using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;
using System.Text;
using FMS.Helpers;
using System.Globalization;
namespace FMS.Controllers
{
    [CustomAuthorizationFilter]
    public class VehicleEMIsController : Controller
    {
        #region ctors
        private FMSDBEntities db = new FMSDBEntities();
        private core objCr = new core();
        private List<tbl_veh_emis> _vehEmiList = new List<tbl_veh_emis>();
        #endregion ctors

        #region Index
        //List of VehicleEMIs
        public ActionResult Index()
        {
            ViewBag.MonthId = new SelectList(core.GetAllMonths(), "Value", "Text");
            ViewBag.YearId = new SelectList(core.GetAllYears(), "Value", "Text");
            return View();
        }
        public JsonResult GetVehiclesEMIsList(int iDisplayStart, int iDisplayLength, string sSearch, int iColumns,
        int iSortingCols, int iSortCol_0, string sSortDir_0,
        int sEcho, string mDataProp_Key, int? MonthId, int? YearId)
        {
            var IList = GetEnumerableList(MonthId, YearId);
            var filteredVehEMIs = IList
                .Select(l => new[]{
                    l.Id.ToString(),
                    l.Text1.ToString(),
                    l.Id1.ToString(),
                    l.Text2.ToString(),
                    l.Amount.ToString(),
                    l.Amount1.ToString(),
                    l.Text3.ToString(),
                    l.Text4.ToString()
                    
                }).Where(i => string.IsNullOrEmpty(sSearch)
                     || i.Any(y => y.IndexOf(sSearch, StringComparison.InvariantCultureIgnoreCase) >= 0));
            var orderedlist = filteredVehEMIs
                   .OrderByWithDirection(i => (i[iSortCol_0]).Parse(), sSortDir_0 == "asc").Take(iDisplayLength);
            var model = new
            {
                aaData = orderedlist,
                iTotalDisplayRecords = filteredVehEMIs.Count(),
                iTotalRecords = filteredVehEMIs.Count(),
                sEcho = sEcho.ToString()
            };
            return Json(model, JsonRequestBehavior.AllowGet);
        }

        private IEnumerable<CommonFields> GetEnumerableList(int? MonthId, int? YearId)
        {
            var list = new List<CommonFields>();
            List<tbl_veh_emis> vehEMIsList = null;
            if (MonthId.HasValue && YearId.HasValue)
            {
                vehEMIsList = (from m in db.tbl_veh_emis
                               where m.MonthYear.Value.Month == MonthId
                               && m.MonthYear.Value.Year == YearId
                               select m).ToList();
            }
            else
            {
                vehEMIsList = (from m in db.tbl_veh_emis
                               select m).ToList();
            }

            foreach (tbl_veh_emis ds in vehEMIsList)
            {
                list.Add(new CommonFields
                {
                    Id = ds.Id,
                    Text1 = ds.tbl_vehicles.VehicleRegNum,
                    Id1 = ds.EMINo.HasValue ? Convert.ToInt64(ds.EMINo) : 0,
                    Text2 = String.Format("{0:MMMM,yyyy}", ds.MonthYear.Value.ToString("Y", new System.Globalization.CultureInfo("en-Us"))),
                    Amount = ds.Amount.HasValue ? Convert.ToDecimal(ds.Amount) : 0,
                    Amount1 = ds.FineAmt.HasValue ? Convert.ToDecimal(ds.FineAmt) : 0,
                    Text3 = ds.Paymode,
                    Text4 = ""
                });
            }
            objCr.ConvertToUppercase<CommonFields>(list);
            return list.OrderByDescending(a => a.Id);
        } 
        #endregion

        #region Add Vehicle EMI
        public ActionResult Create()
        {
            return View();
        }
        public ActionResult AddNewVehicleEMI()
        {
            return PartialView("_AddNewVehicleEMI");
        }
        [HttpPost]
        public ActionResult AddVehicleEMIEntry(FormCollection frm, long VID, tbl_veh_emis vehEmis)
        {

            tbl_vehicles _vehicle = db.tbl_vehicles.Where(c => c.Status == true && c.ID == VID).FirstOrDefault();
            long ID = 0;
            _vehEmiList = (List<tbl_veh_emis>)Session["VehicleEMIList"];
            if (_vehEmiList == null)
                _vehEmiList = new List<tbl_veh_emis>();

            //this is for delete list item based on this id
            if (_vehEmiList.Count() == 0)
                ID = 1;
            else
                ID = _vehEmiList.Count() + 1;

            // Add object to list 
            _vehEmiList.Add(new tbl_veh_emis
            {

                Id = ID,
                EMINo = GetInstNo(_vehicle.ID),
                MonthYear = vehEmis.MonthYear,
                Amount = vehEmis.Amount,
                FineAmt = vehEmis.FineAmt,
                Paymode = vehEmis.Paymode,
                DDChequeNo = vehEmis.DDChequeNo,
                ChequeDt = vehEmis.ChequeDt,
                BankName = vehEmis.BankName,
                RefNumber = vehEmis.RefNumber,
                ClosedFlag = false,
                VehicleId = VID
            });

            Session["VehicleEMIList"] = _vehEmiList;
            ViewBag.message = "";
            return PartialView("_AddVehicleEMIEntry", _vehEmiList);
        }
        public string getVehicleAjaxResult(string f, string q)
        {
            f = f == "undefined" ? "VehicleRegNum" : f;
            StringBuilder str = new StringBuilder();
            var vehicleId = from a in db.tbl_vehicles
                                   .Where(a => a.Status.Value == true && (a.Isown == true))
                                   .Where<tbl_vehicles>(f, q, WhereOperation.Contains)
                            select new { a.VehicleRegNum, a.ID };

            foreach (var a in vehicleId)
            {
                str.Append(string.Format("{0} |{1}\r\n", a.VehicleRegNum.ToUpper(), a.ID)).ToString();
                ViewBag.ClientID = a.ID;
            }

            return str.ToString();
        }
        public JsonResult GetVehicleEMIDetails(long VehicleId)
        {
            var HypEMIDet = db.tbl_veh_hypothicated.Where(a => a.VehicleId == VehicleId).SingleOrDefault();
            List<tbl_veh_emis> emis = db.tbl_veh_emis.Where(a => a.VehicleId == VehicleId).ToList();
            List<SelectListItem> EMIMonthList = new List<SelectListItem>();
            if (HypEMIDet != null)
            {
                int TotEMIs = Convert.ToInt32(HypEMIDet.RepaymentTenure);
                var TotEMIAmt = HypEMIDet.RepaidAmt;
                var MonthlyEMIAmt = HypEMIDet.MonthlyEMIAmt;
                EMIMonthList = GetEMIMonthListForVehicle(VehicleId, HypEMIDet.MonthlyEMIFrom, HypEMIDet.MonthlyEMITo, TotEMIs);
                decimal? RemainEMIAmt = TotEMIAmt;
                int emiNo = 1;
                if (emis.Count() > 0)
                {
                    emiNo = emis.Count() + 1;
                    RemainEMIAmt = TotEMIAmt - emis.Sum(a => a.Amount);
                }

                return Json(new { success = true, EMIMonthList, TotEMIs, TotEMIAmt, MonthlyEMIAmt, HypEMIDet.MonthlyEMIFrom, HypEMIDet.MonthlyEMITo, emiNo, RemainEMIAmt }, JsonRequestBehavior.AllowGet);
            }
            else
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);

        }
        //Get EMI Monthly Details
        public List<SelectListItem> GetEMIMonthListForVehicle(long VehId, string EMIFrmMonth, string EMIToMonth, int TotEMIs)
        {
            List<SelectListItem> EMIMonthList = new List<SelectListItem>();
            var FromSplit = Convert.ToDateTime(EMIFrmMonth); // EMIFrmMonth.Split('/');
            var ToSplit = Convert.ToDateTime(EMIToMonth); //EMIToMonth.Split('/');
            int FM = FromSplit.Month;
            int Fy = FromSplit.Year;
            int TM = ToSplit.Month;
            int Ty = ToSplit.Year;
            TotEMIs = 0;
            VehicleController objVeh = new VehicleController();
            TotEMIs = objVeh.GetTotalMonthsBetweenDates(EMIFrmMonth, EMIToMonth);
            for (long i = 1; i <= TotEMIs; i++)
            {
                if (FM > 12)
                {
                    FM = 1;
                    Fy += 1;
                }
                string date = "";
                if (FM < 10)
                    date = Fy + "-0" + FM + "-01 00:00:00.000";
                else
                    date = Fy + "-" + FM + "-01 00:00:00.000";
                DateTime monyear = Convert.ToDateTime(date);
                EMIMonthList.Add(new SelectListItem { Text = CultureInfo.GetCultureInfo("en-GB").DateTimeFormat.MonthNames[FM - 1] + "-" + Fy, Value = "01-" + FM + "-" + Fy, Selected = db.tbl_veh_emis.Where(a => a.VehicleId == VehId && a.MonthYear == monyear).Any() });
                FM += 1;
            }
            return EMIMonthList;
        }

        [HttpPost]
        public ActionResult SaveVehicleEMIEntry(FormCollection frm)
        {
            try
            {
                //List of Vehicle EMI structure 
                List<tbl_veh_emis> _listofVehEMIs = (List<tbl_veh_emis>)Session["VehicleEMIList"];
                //objCr.ConvertToUppercase<tbl_odometer_readings>(_listofReadings);
                foreach (tbl_veh_emis _list in _listofVehEMIs)
                {
                    var VehEMIDet = _list;
                    db.tbl_veh_emis.AddObject(VehEMIDet);
                    db.SaveChanges();
                }
                Session["VehicleEMIList"] = null;
                objCr.LoggingEntries("Vehicle EMIs", "Vehicle EMI were created ", User.Identity.Name);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Errormessage = ex.Message.ToString();
                return View();
            }
        }
        // Save Vehicle EMI Entries 

        public int SaveUnSavedEMIEntries()
        {
            try
            {
                List<tbl_veh_emis> _listofVehEMIs = (List<tbl_veh_emis>)Session["VehicleEMIList"];
                // objCr.ConvertToUppercase<tbl_odometer_readings>(_listofReadings);
                foreach (tbl_veh_emis _list in _listofVehEMIs)
                {
                    var VehEMIDet = _list;
                    db.tbl_veh_emis.AddObject(VehEMIDet);
                    db.SaveChanges();
                }
                Session["VehicleEMIList"] = null;
                objCr.LoggingEntries("Vehicle EMIs", "Vehicle EMI were created ", User.Identity.Name);
                return 1;
            }
            catch
            {
                return 0;
            }
        }
        #endregion

        #region Edit Vehicle EMI

        [HttpGet]
        public ActionResult Edit(long id)
        {
            tbl_veh_emis vehEMIs = db.tbl_veh_emis.Where(a => a.Id == id).SingleOrDefault();
            return PartialView("_Edit", vehEMIs);
        }
        [HttpPost]
        public ActionResult Edit(long Id,FormCollection frm, tbl_veh_emis vehEmis)
        {
            try
            {
                tbl_veh_emis emisDet = db.tbl_veh_emis.Where(a => a.Id == Id).SingleOrDefault();
                TryUpdateModel(emisDet);
                db.SaveChanges();
                objCr.LoggingEntries("Vehicle EMIs", "Vehicle EMI has updated to the vehicle " + emisDet.tbl_vehicles.VehicleRegNum, User.Identity.Name);
            }
            catch
            {
                return Json(new { success = false, msg = "An error occured while processing your request." });
            }
            return Json(new { success = true, msg = "Vehicle EMI has been updated successfully." });
        }

        public List<SelectListItem> GetSelectedMonthListForVehicle(long VehId, DateTime Date)
        {
            List<SelectListItem> EMIMonthList = new List<SelectListItem>();
            int FM = Convert.ToInt32(Date.Month);
            int Fy = Convert.ToInt32(Date.Year);
            EMIMonthList.Add(new SelectListItem { Text = CultureInfo.GetCultureInfo("en-GB").DateTimeFormat.MonthNames[FM - 1] + "-" + Fy, Value = "01-" + (FM <= 9 ? "0" + FM : FM.ToString()) + "-" + Fy, Selected = true });
            return EMIMonthList;
        }

        public JsonResult GetSelectedVehicleEMIDetails(long VehicleId, DateTime date)
        {
            var HypEMIDet = db.tbl_veh_hypothicated.Where(a => a.VehicleId == VehicleId).SingleOrDefault();
            List<tbl_veh_emis> emis = db.tbl_veh_emis.Where(a => a.VehicleId == VehicleId).ToList();
            List<SelectListItem> EMIMonthList = new List<SelectListItem>();
            if (HypEMIDet != null)
            {
                int TotEMIs = Convert.ToInt32(HypEMIDet.RepaymentTenure);
                var TotEMIAmt = HypEMIDet.HypothicatedAmt;
                var MonthlyEMIAmt = HypEMIDet.MonthlyEMIAmt;
                EMIMonthList = GetSelectedMonthListForVehicle(VehicleId, date);
                decimal? RemainEMIAmt = TotEMIAmt;
                int emiNo = GetInstNo(VehicleId);
                if (emis.Count() > 0)
                    //emiNo = emis.Count() + 1;
                    RemainEMIAmt = TotEMIAmt - emis.Sum(a => a.Amount);
                return Json(new { success = true, EMIMonthList, TotEMIs, TotEMIAmt, MonthlyEMIAmt, HypEMIDet.MonthlyEMIFrom, HypEMIDet.MonthlyEMITo, emiNo, RemainEMIAmt }, JsonRequestBehavior.AllowGet);
            }
            else
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);

        }

        #endregion

        #region Delete
        [HttpGet]
        public ActionResult Delete(long Id)
        {
            tbl_veh_emis vehEmis = db.tbl_veh_emis.Where(a => a.Id == Id).SingleOrDefault();
            db.DeleteObject(vehEmis);
            db.SaveChanges();
            objCr.LoggingEntries("Vehicle EMIs", "Vehicle EMI has deleted to the vehicle ", User.Identity.Name);
            return RedirectToAction("Index");
        } 
        #endregion

        #region Custom Methods
        public int GetInstNo(long VehicleId)
        {
            int InstNo = 0;
            InstNo += db.tbl_veh_emis.Where(a => a.VehicleId == VehicleId).Count();
            InstNo += _vehEmiList.Where(a => a.VehicleId == VehicleId).Count();
            InstNo++;
            return InstNo;
        }

        public bool VerifyVehicleEMIs(long VehicleId, string MonthYear)
        {
            DateTime MnYear = Convert.ToDateTime(MonthYear);
            return db.tbl_veh_emis.Where(a => a.VehicleId == VehicleId && a.MonthYear.Value.Month == MnYear.Month && a.MonthYear.Value.Year == MnYear.Year && a.ClosedFlag == false).Any();
        }
        #endregion

        #region Auto Renewal Vehicle EMIs
        [HttpGet]
        public ActionResult DoAutoRenewalVehicleEMIs()
        {
            DateTime MonthYear = DateTime.Now;
            objCr.LoggingEntries("Vehicle EMIs", "Vehicle EMIs has been initiated for the month of " + MonthYear.Month + "," + MonthYear.Year, "");
            try
            {
                List<GeneralClassFields> financeList = GetFinanceVehicles(true);
                if (financeList != null)
                {
                    List<tbl_veh_emis> _vehEmisList = null;
                    foreach (var finance in financeList)
                    {
                        long VehicleId = (long)finance.ID;

                        _vehEmisList = GetVehicleEMIsByVehicle(VehicleId);

                        // Verify Total Actual Emis count with Paid Vehicle Emis count

                        if (_vehEmisList.Count == Convert.ToInt32(finance.Text1))
                            continue;

                        // Verify Whether EMI Payment has done or not for the current Month & Year

                        if (_vehEmisList.Where(a => a.MonthYear.Value.Month == MonthYear.Month && a.MonthYear.Value.Year == MonthYear.Year).Any())
                            continue;

                        // Add Vehicle EMIs

                        tbl_veh_emis vehEMI = new tbl_veh_emis();
                        vehEMI.VehicleId = VehicleId;
                        vehEMI.MonthYear = new DateTime(MonthYear.Year, MonthYear.Month, 01);
                        vehEMI.EMINo = GetInstNo(VehicleId);
                        vehEMI.Paymode = "Cash";
                        vehEMI.Amount = finance.Value2;
                        vehEMI.ClosedFlag = false;
                        db.tbl_veh_emis.AddObject(vehEMI);
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception)
            {
                return Json(new { success = false, msg = "An error occured while proccessing your request." });
            }
            objCr.LoggingEntries("Vehicle EMIs", "Vehicle EMIs has been renewed for the month of " + MonthYear.Month + "," + MonthYear.Year, User.Identity.Name);
            return Json(new { success = true, msg = "Vehicle EMIs has been renewal successfully." }, JsonRequestBehavior.AllowGet);
        }

        public List<GeneralClassFields> GetFinanceVehicles(bool IsOwn) 
        {
            var financeList = (from v in db.tbl_vehicles
                               join
                                   f in db.tbl_veh_hypothicated on v.ID equals f.VehicleId
                               where v.Status == true
                               && v.Isown == IsOwn
                               select new GeneralClassFields { ID = v.ID, Text1 = f.RepaymentTenure, Value2 = (decimal)f.MonthlyEMIAmt, Text2 = f.MonthlyEMIFrom, Text3 = f.MonthlyEMITo }).ToList();
            return financeList;
        }

        public List<tbl_veh_emis> GetVehicleEMIsByVehicle(long VehicleId)
        {
            List<tbl_veh_emis> vehEMIsList = (from m in db.tbl_veh_emis
                                              where m.VehicleId == VehicleId
                                              select m).ToList();
            return vehEMIsList;
        }

        public JsonResult UpdateVehicleEMIs()
        {
            DateTime MonthYear = DateTime.Now;
            try
            {
                List<GeneralClassFields> financeList = GetFinanceVehicles(true);
                if (financeList != null)
                {
                    List<tbl_veh_emis> _vehEmisList = null;
                    List<SelectListItem> _pendingEMIsList = null;
                    foreach (var finance in financeList)
                    {
                        long VehicleId = (long)finance.ID;

                        _pendingEMIsList = GetUnPaidEMIMonthListForVehicle(VehicleId, finance.Text2, finance.Text3, 0);

                        _vehEmisList = GetVehicleEMIsByVehicle(VehicleId);

                        foreach (var item in _pendingEMIsList)
                        {
                            MonthYear = Convert.ToDateTime(item.Value);
                            // Verify Total Actual Emis count with Paid Vehicle Emis count

                            if (_vehEmisList.Count == Convert.ToInt32(finance.Text1))
                                continue;

                            // Verify Whether EMI Payment has done or not for the current Month & Year

                            if (_vehEmisList.Where(a => a.MonthYear.Value.Month == MonthYear.Month && a.MonthYear.Value.Year == MonthYear.Year).Any())
                                continue;

                            // Add Vehicle EMIs

                            tbl_veh_emis vehEMI = new tbl_veh_emis();
                            vehEMI.VehicleId = VehicleId;
                            vehEMI.MonthYear = new DateTime(MonthYear.Year, MonthYear.Month, 01);
                            vehEMI.EMINo = GetInstNo(VehicleId);
                            vehEMI.Paymode = "Cash";
                            vehEMI.Amount = finance.Value2;
                            vehEMI.ClosedFlag = false;
                            db.tbl_veh_emis.AddObject(vehEMI);
                            db.SaveChanges();
                        }
                    }
                }
            }
            catch (Exception)
            {
                return Json(new { success = false, msg = "An error occured while proccessing your request." });
            }
            objCr.LoggingEntries("Vehicle EMIs", "Vehicle EMIs has been renewed for the month of " + MonthYear.Month + "," + MonthYear.Year, User.Identity.Name);
            return Json(new { success = true, msg = "Vehicle EMIs has been renewal successfully." }, JsonRequestBehavior.AllowGet);
        }

        public List<SelectListItem> GetUnPaidEMIMonthListForVehicle(long VehId, string EMIFrmMonth, string EMIToMonth, int TotEMIs) 
        {
            List<SelectListItem> EMIMonthList = new List<SelectListItem>();
            var FromSplit = Convert.ToDateTime(EMIFrmMonth); // EMIFrmMonth.Split('/');
            var ToSplit = Convert.ToDateTime(EMIToMonth); //EMIToMonth.Split('/');
            int FM = FromSplit.Month;
            int Fy = FromSplit.Year;
            int TM = ToSplit.Month;
            int Ty = ToSplit.Year;
            TotEMIs = 0;
            DateTime currentMonth = DateTime.Now;
            VehicleController objVeh = new VehicleController();
            TotEMIs = objVeh.GetTotalMonthsBetweenDates(EMIFrmMonth, EMIToMonth);
            for (long i = 1; i <= TotEMIs; i++)
            {
                if (FM > 12)
                {
                    FM = 1;
                    Fy += 1;
                }
                string date = "";
                if (FM < 10)
                    date = Fy + "-0" + FM + "-01 00:00:00.000";
                else
                    date = Fy + "-" + FM + "-01 00:00:00.000";
                DateTime monyear = Convert.ToDateTime(date);
                if (monyear.Year >= currentMonth.Year)
                {
                    if (monyear.Month > currentMonth.Month || monyear.Year > currentMonth.Year)
                        break;
                    else
                        if (!db.tbl_veh_emis.Where(a => a.VehicleId == VehId && a.MonthYear == monyear).Any())
                            EMIMonthList.Add(new SelectListItem { Text = CultureInfo.GetCultureInfo("en-GB").DateTimeFormat.MonthNames[FM - 1] + "-" + Fy, Value = "01-" + FM + "-" + Fy });
                }
                else
                    if (!db.tbl_veh_emis.Where(a => a.VehicleId == VehId && a.MonthYear == monyear).Any())
                        EMIMonthList.Add(new SelectListItem { Text = CultureInfo.GetCultureInfo("en-GB").DateTimeFormat.MonthNames[FM - 1] + "-" + Fy, Value = "01-" + FM + "-" + Fy });

                FM += 1;
            }
            return EMIMonthList;
        }

        public ActionResult GetUserTrack()
        {
            objCr.LoggingEntries("Vehicle EMIs", "Vehicle EMIs has updated", "");
            return Json(new { success = true , msg = "User has Tracked" }, JsonRequestBehavior.AllowGet);
        }

        #endregion
    }
}

