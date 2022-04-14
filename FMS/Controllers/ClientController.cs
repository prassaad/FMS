using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.Data.OleDb;
using System.Globalization;
using System.Collections;
using System.Configuration;
using System.Web.Configuration;
using ClosedXML.Excel;
using AsyncUploaderDemo.Models;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Drawing;
using System.Web.Util;
using System.ComponentModel;
using FMS.Helpers;


namespace FMS.Controllers
{
    [CustomAuthorizationFilter]
    public class ClientController : Controller
    {
        #region ctors
        private FMSDBEntities db;
        private List<tbl_km_client_rate> _kmClientList;
        private List<tbl_slab_client_rate> _sbClientList;
        private List<SelectListItem> _fieldTextList;
        private List<SelectListItem> _availColsList;
        private List<tbl_client_log_sheet_cols> _clientLogMapColsList;
        private core core = new core();

        public ClientController()
        {
            db = new FMSDBEntities();
        } 
        #endregion

        #region ctors
        [Authorize]
        public ActionResult Index()
        {
            return View();
        }
        public JsonResult GetClients(int iDisplayStart, int iDisplayLength, string sSearch, int iColumns,
     int iSortingCols, int iSortCol_0, string sSortDir_0,
     int sEcho, string mDataProp_Key)
        {
            var List = GetIEnumerableList();
            var filteredlist = List
                .Select(l => new[]{
                        l.Id.ToString(),
                        l.Text1.ToString(),
                        l.Text2.ToString(),
                        l.Text3.ToString(),
                        l.Text4.ToString(),
                        l.Date1.ToShortDateString(),
                        l.Status1.ToString(), 
                        l.Status.ToString()
                    
                }).Where(i => string.IsNullOrEmpty(sSearch)
                     || i.Any(y => y.IndexOf(sSearch, StringComparison.InvariantCultureIgnoreCase) >= 0));
            var orderedlist = filteredlist
                   .OrderByWithDirection(i => (i[iSortCol_0]).Parse(), sSortDir_0 == "asc")
                   .Skip(iDisplayStart)
                   .Take(iDisplayLength);
            var model = new
            {
                aaData = orderedlist,
                iTotalDisplayRecords = filteredlist.Count(),
                iTotalRecords = filteredlist.Count(),
                sEcho = sEcho.ToString()
            };
            return Json(model, JsonRequestBehavior.AllowGet);
        }
        private IEnumerable<CommonFields> GetIEnumerableList()
        {
            var list = new List<CommonFields>();
            var ClinetList = db.tbl_clients.Where(a => a.Status == true).ToList();
            foreach (tbl_clients ls in ClinetList)
            {
                list.Add(new CommonFields
                {
                    Id = ls.ID,
                    Text1 = ls.CompanyName,
                    Text2 = ls.Address == null ? "" : ls.Address,
                    Text3 = ls.PhoneNumber == null ? "" : ls.PhoneNumber.ToString(),
                    Text4 = ls.BillingTypeID == null ? "" : ls.tbl_billing_types.BillingType,
                    Date1 = Convert.ToDateTime(ls.CreatedOn),
                    Status1 = VerifyRateStrucutre((long)ls.ID),
                    Status = (bool)ls.Status
                });
            }
            core.ConvertToUppercase<CommonFields>(list);
            return list.OrderBy(a => a.Id);
        } 
        #endregion

        #region Add New Client
        [HttpGet]
        public ActionResult Create()
        {
            core objCore = new core();
            ViewBag.BillingTypeID = objCore.LoadBillTypes();
            ViewBag.message = "";
            return View(new tbl_clients());
        }
        [HttpPost]
        public ActionResult Create(FormCollection frm, tbl_clients client, HttpPostedFileBase AgreementDocument)
        {
            ViewBag.BillingTypeID = core.LoadBillTypes();
            core.ConvertToUppercase(client);
            try
            {
                if (ValidateForm(frm, client, "Create"))
                {
                    if (AgreementDocument != null && AgreementDocument.ContentLength > 0)
                    {
                        string ext = Path.GetExtension(AgreementDocument.FileName);
                        client.AgreementDocument = client.CompanyName + "_" + client.ID + ext;
                        var path = Path.Combine(Server.MapPath("../Content/uploadimages/"), client.AgreementDocument);
                        AgreementDocument.SaveAs(path);
                    }
                    client.Status = true;
                    client.CreatedOn = DateTime.Now.Date;
                    client.CreatedBy = User.Identity.Name.ToUpper();
                    db.tbl_clients.AddObject(client);
                    db.SaveChanges();
                    core.LoggingEntries("Client Mgmt.", "New client is created.", User.Identity.Name);
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                ViewBag.message = "Error Message :" + ex.Message;
                return View();
            }
            ViewBag.message = "";
            return View();
        } 
        #endregion

        #region Edit Client 
        [HttpGet]
        public ActionResult Edit(int ID)
        {
            tbl_clients client = db.tbl_clients.Where(d => d.Status == true && d.ID == ID).SingleOrDefault();
            ViewBag.BillingTypeID = new SelectList(db.tbl_billing_types.Where(a => a.Status == true), "ID", "BillingType", client.BillingTypeID);
            return View(client);
        }
        [HttpPost]
        public ActionResult Edit(int ID, FormCollection frm, tbl_clients client, HttpPostedFileBase Agreement)
        {
            ViewBag.BillingTypeID = new SelectList(db.tbl_billing_types.Where(a => a.Status == true), "ID", "BillingType", client.BillingTypeID);
            tbl_clients clientDet = db.tbl_clients.Where(d => d.Status == true && d.ID == ID).SingleOrDefault();
            try
            {
                if (ValidateForm(frm, client, "Edit"))
                {
                    if (Agreement != null && Agreement.ContentLength > 0)
                    {
                        string ext = Path.GetExtension(Agreement.FileName);
                        clientDet.AgreementDocument = clientDet.CompanyName + "_" + clientDet.ID + ext;
                        var path = Path.Combine(Server.MapPath("../../Content/uploadimages/"), clientDet.AgreementDocument);
                        Agreement.SaveAs(path);
                    }
                    if (clientDet.BillingTypeID != client.BillingTypeID)
                    {
                        UpdateClientRateChart(clientDet);
                    }
                    clientDet.Status = true;
                    TryUpdateModel(clientDet);
                    core.ConvertToUppercase(clientDet);
                    db.SaveChanges();
                    core.LoggingEntries("Client Mgmt.", "client has updated with company is " + client.CompanyName + ".", User.Identity.Name);
                    return RedirectToAction("Index");
                }
            }
            catch (Exception)
            {
                return View(clientDet);
            }
            return View(clientDet);
        } 
        #endregion

        #region View Client
        [HttpGet]
        public ActionResult Details(long ID)
        {
            tbl_clients ClientDet = db.tbl_clients.Where(a => a.Status == true && a.ID == ID).SingleOrDefault();
            core.ConvertToUppercase(ClientDet);
            return View(ClientDet);
        } 
        #endregion

        #region Delete Client
        [HttpGet]
        public ActionResult Delete(long ID)
        {
            tbl_clients clientDet = db.tbl_clients.Where(d => d.Status == true && d.ID == ID).SingleOrDefault();
            return View(clientDet);
        }

        [HttpPost]
        public ActionResult Delete(long ID, FormCollection frm, tbl_clients client)
        {
            tbl_clients clientDet = db.tbl_clients.Where(d => d.Status == true && d.ID == ID).SingleOrDefault();
            core objCore = new core();
            try
            {
                clientDet.Status = false;
                TryUpdateModel(clientDet);
                UpdateClientRateChart(clientDet);
                db.SaveChanges();
                core.LoggingEntries("Client Mgmt.", "client has deleted with company is " + client.CompanyName + ".", User.Identity.Name);
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                return View();
            }
        } 
        #endregion

        #region Client Rates
        // Client Rate
        [HttpGet]
        public ActionResult AddClientRateChart(long ClientID)
        {
            core objCore = new core();
            ViewBag.LocationID = objCore.LoadLocations();
            ViewBag.SeaterID = objCore.LoadSeaters();
            ViewBag.VehicleTypeID = objCore.LoadVehicleTypes();
            ViewBag.VehicleModelId = objCore.LoadVehicleModels();
            ViewBag.TimeUnit = objCore.LoadTimeUnit();
            ViewBag.PackModel = objCore.LoadPackModel();
            ViewBag.LocationId = objCore.LoadLocations();
            ViewBag.ClientID = ClientID;
            tbl_clients clientDet = db.tbl_clients.Where(a => a.ID == ClientID && a.Status == true).SingleOrDefault();
            ViewBag.IsEmpBased = clientDet.IsEmpBased;
            if (clientDet.tbl_billing_types.BillingType.Trim().Contains("KILO METER"))
                return PartialView("_AddClientRateKMChartPartial");
            else if (clientDet.tbl_billing_types.BillingType.Trim().Contains("TRIPS"))
                return PartialView("_AddClientRateSBChartPartial");
            else if (clientDet.tbl_billing_types.BillingType.Trim().Contains("PACKAGE"))
                return PartialView("_AddClientPackageRatePartial");
            else
                return PartialView("_AddClientPackageRatePartial");
        }
        [HttpGet]
        public ActionResult GetClientRateList(long ClientID)
        {
            tbl_clients clientDet = db.tbl_clients.Where(a => a.ID == ClientID && a.Status == true).SingleOrDefault();
            core objCore = new core();
            if (clientDet.tbl_billing_types.BillingType.Trim().Contains("KILO METER"))
            {
                ViewBag.ClientID = ClientID;
                return PartialView("_SavedClientKMRateList");

            }
            else if (clientDet.tbl_billing_types.BillingType.Trim().Contains("TRIPS"))
            {
                ViewBag.ClientID = ClientID;
                return PartialView("_SavedClientSLABRateList");
            }
            else if (clientDet.tbl_billing_types.BillingType.Trim().Contains("PACKAGE"))
            {
                var packageList = db.tbl_package_client_rates.Where(a => a.ClientId == ClientID && a.IsActive == true).ToList();
                return PartialView("_ClientPackageRateList", packageList);
            }
            else
                return PartialView("_NoData");
        }

        // Client Slab Rate Ienumerable list 
        public JsonResult GetClientSlabRateList(long ClientID, int iDisplayStart, int iDisplayLength, string sSearch, int iColumns,
      int iSortingCols, int iSortCol_0, string sSortDir_0,
      int sEcho, string mDataProp_Key)
        {
            var IList = GetEnumerableSlabList(ClientID);
            var filteredLogSheet = IList
                .Select(l => new[]{
                    l.Id.ToString(),
                    l.Text1.ToString(),
                    l.Text2.ToString(),
                    l.Text3.ToString(),
                    l.Text4.ToString(),
                    l.Value1.ToString(),
                    l.Text5.ToString(),
                    l.Value2.ToString(),
                    l.Text6.ToString(),
                    l.Text7.ToString(),
                    l.Text8.ToString(),
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
        private IEnumerable<CommonFields> GetEnumerableSlabList(long ClientID)
        {
            var list = new List<CommonFields>();
            var tempList = core.GetSlabClientRateList(ClientID);
            if (tempList != null)
            {
                foreach (tbl_slab_client_rate ds in tempList)
                {
                    list.Add(new CommonFields
                    {
                        Id = ds.SLABID,
                        Text1 = ds.Location == null ? "" : ds.Location,
                        Text2 = ds.SeaterID == null ? "" : ds.tbl_seaters.Seater,
                        Text3 = ds.VehicleTypeID == null ? "" : ds.tbl_vehicle_types.VehicleType,
                        Text4 = ds.ClientSlab == null ? "" : ds.ClientSlab,
                        Value1 = ds.ClientRate == null ? 0 : (double)ds.ClientRate,
                        Text5 = ds.VendorSlab == null ? "" : ds.VendorSlab,
                        Value2 = ds.VendorRate == null ? 0 : (double)ds.VendorRate,
                        Text6 = ds.EmpId == null ? "" : ds.EmpId,
                        Text7 = ds.EmpName == null ? "" : ds.EmpName,
                        Text8 = ds.EmpGender == null ? "" : ds.EmpGender,
                        Status = (bool)ds.Status
                    });
                }
            }
            core.ConvertToUppercase<CommonFields>(list);
            return list.OrderBy(a => a.Id);
        }

        // Client KiloMeter Ienumerable List
        public JsonResult GetClientKMRateList(long ClientID, int iDisplayStart, int iDisplayLength, string sSearch, int iColumns,
      int iSortingCols, int iSortCol_0, string sSortDir_0,
      int sEcho, string mDataProp_Key)
        {
            var IList = GetEnumerableKMList(ClientID);
            var filteredLogSheet = IList
                .Select(l => new[]{
                        l.Id.ToString(),
                        l.Text1.ToString(),
                        l.Id2.ToString(),
                        l.Value1.ToString(),
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
        private IEnumerable<CommonFields> GetEnumerableKMList(long ClientID)
        {
            var list = new List<CommonFields>();
            var tempList = core.GetKMClientRateList(ClientID);
            if (tempList != null)
            {
                foreach (tbl_km_client_rate ds in tempList)
                {
                    list.Add(new CommonFields
                    {
                        Id = ds.KMID,
                        Text1 = ds.Location == null ? "" : ds.Location,
                        Id2 = ds.ApprovedKM == null ? 0 : (int)ds.ApprovedKM,
                        Value1 = ds.ClientRate == null ? 0 : (double)ds.ClientRate,
                        Status = (bool)ds.Status
                    });
                }
            }
            core.ConvertToUppercase<CommonFields>(list);
            return list.OrderBy(a => a.Id);
        }  
        #endregion

        #region KMClientRate
        
        [HttpPost]
        public ActionResult AddClientKMRateChart(FormCollection frm)
        {
            string BillType = frm["BillingType"] == null ? "" : frm["BillingType"];
            long ID = 0; 
            long ClientID = frm["ClientID"] == null ? 0 : Convert.ToInt64(frm["ClientID"]);
            tbl_clients client =db.tbl_clients.Where(c=>c.Status==true && c.ID == ClientID).SingleOrDefault();
            
            string location = frm["Location"] == null ? "" : frm["Location"].ToString().ToUpper().Trim();
             
            tbl_km_client_rate _checkLoc = db.tbl_km_client_rate.Where(s => s.Status == true && s.Location.ToUpper().Trim() == location
                                                                              && s.ClientID == ClientID).SingleOrDefault();
            if (_checkLoc != null) //if location details are  found on particular Location Name. based on ClientID,SeaterID
            {
                ViewBag.ErrorMsg = "KILO METER Rate is available on this Location: " + location + ", Client: " + client.CompanyName.ToUpper();
                return PartialView("_ClientKMRateChartList", _kmClientList);
            }
            int ApprovedKM = frm["ApprovedKM"] == null ? 0 : Convert.ToInt32(frm["ApprovedKM"]);
            double ClientRate = (frm["ClientRate"] == null || frm["ClientRate"] == "") ? 0 : Convert.ToDouble(frm["ClientRate"]);
            
            // Get list of saved rate list
            List<tbl_km_client_rate> prevRateList = (List<tbl_km_client_rate>)Session["PrevKMClientList"];
            
            prevRateList = (prevRateList == null ? new List<tbl_km_client_rate>() : new core().GetKMClientRateList(ClientID));
            
            _kmClientList = (List<tbl_km_client_rate>)Session["KMClientList"];
            // Verify existed list with current 
            if (_kmClientList != null)
            {
                bool _checkSessionData = _kmClientList.Where(s => s.Status == true && s.Location.ToUpper().Trim() == location
                                                                               && s.ClientID == ClientID).Any();
                if (_checkSessionData == true) //if location details are  found on particular Location Name. based on ClientID,SeaterID in Already added data(session data)
                {
                    ViewBag.ErrorMsg = "KILO METER Rate is available on this Location: " + location + ", Client: " + client.CompanyName.ToUpper();
                    return PartialView("_ClientKMRateChartList", _kmClientList);
                }
            }
            // If session has no data 
            if (_kmClientList == null)
                _kmClientList = new List<tbl_km_client_rate>();
            if (_kmClientList.Count() == 0)
                ID = 1;
            else
                ID = _kmClientList.Count() + 1;

            // Add to prevoius list
            prevRateList.Add(new tbl_km_client_rate { KMID = ID, ClientID = ClientID, Location = location, ApprovedKM = ApprovedKM, ClientRate = ClientRate, Status = true });
            // Add to current list
            _kmClientList.Add(new tbl_km_client_rate { KMID = ID, ClientID = ClientID, Location = location , ApprovedKM = ApprovedKM, ClientRate = ClientRate, Status = true });
            Session["KMClientList"] = _kmClientList;
            Session["PrevKMClientList"] = prevRateList;
            return PartialView("_ClientKMRateChartList", prevRateList);
        }
        [HttpPost]
        public ActionResult SaveClientKMRateChart(FormCollection frm)
        {
            long ClientID = frm["ClientID"] == null ? 0 : Convert.ToInt64(frm["ClientID"]);
            // Get List of saved KM rate chart 
            List<tbl_km_client_rate> savedRateList = new core().GetKMClientRateList(ClientID);
            List<tbl_km_client_rate> _KMList = (List<tbl_km_client_rate>)Session["KMClientList"];
            if (_KMList != null)
            {
                //if (savedRateList != null)
                //{
                //    foreach (tbl_km_client_rate km in savedRateList)
                //    {
                //        tbl_km_client_rate kmRate = db.tbl_km_client_rate.Where(a => a.KMID == km.KMID).SingleOrDefault();
                //        db.DeleteObject(kmRate);
                //    }
                //    db.SaveChanges();
                //}
                foreach (tbl_km_client_rate km in _KMList)
                    db.tbl_km_client_rate.AddObject(km);
                db.SaveChanges();
                core.LoggingEntries("Client Mgmt.", "KM rate has added to the client " + core.GetClient(ClientID) + ".", User.Identity.Name);
                Session["KMClientList"] = null; Session["PrevKMClientList"] = null;
                ViewBag.message = "Kilo meter chart list added successfully.";
                ViewBag.ClientID = ClientID;
                return PartialView("_SavedClientKMRateList", _KMList);
            }
            else
            {
                ViewBag.ClientID = ClientID;
                return PartialView("_SavedClientKMRateList", savedRateList);
            }

        }
        public ActionResult DeleteKMItem(long ID)
        {
            tbl_km_client_rate KMRate = db.tbl_km_client_rate.Where(a => a.KMID == ID).SingleOrDefault();
            KMRate.Status = false;
            TryUpdateModel(KMRate);
            db.SaveChanges();
            core objCore = new core(); 
            List<tbl_km_client_rate> list = objCore.GetKMClientRateList((long)KMRate.ClientID);
            ViewBag.message = "Item Deleted successfully";
            core.LoggingEntries("Client Mgmt.", "KM rate has deleted to the client " + KMRate.tbl_clients.CompanyName + ".", User.Identity.Name);
            ViewBag.ClientID = KMRate.ClientID;
            return PartialView("_SavedClientKMRateList", list);
        }
        public ActionResult RemoveKMItem(long ID)
        {
            _kmClientList = (List<tbl_km_client_rate>)Session["KMClientList"];
            foreach (tbl_km_client_rate obj in _kmClientList)
            {
                if (obj.KMID == ID) // Will match once
                {
                    //Delete the Row
                    _kmClientList.Remove(obj);
                    ReKMShuffleSlno();
                    break;
                }
            }
            ViewBag.message = "Item removed successfully";
            return PartialView("_ClientKMRateChartList", _kmClientList);
        }
        public void ReKMShuffleSlno() 
        {
            var i = 1;
            foreach (tbl_km_client_rate obj in _kmClientList)
            {
                obj.KMID = i;
                i++;
            }

        }

        [HttpGet]
        public ActionResult EditKMDetails(long ID)
        {
            tbl_km_client_rate KMRate = db.tbl_km_client_rate.Where(a => a.KMID == ID && a.Status == true).SingleOrDefault(); 
            ViewBag.ClientID = KMRate.ClientID;
            return PartialView("_EditKMDetails", KMRate);
        }
        [HttpPost]
        public ActionResult EditKMDetails(long ID, FormCollection frm)
        {
            tbl_km_client_rate KMRate = db.tbl_km_client_rate.Where(a => a.KMID == ID && a.Status == true).SingleOrDefault(); 
            try
            {
                long ClientID = frm["hdnClientID"] == null ? 0 : Convert.ToInt64(frm["hdnClientID"]);
                tbl_clients client = db.tbl_clients.Where(c => c.Status == true && c.ID == ClientID).SingleOrDefault();
                string Location = frm["EditLocation"] == null ? "" : frm["EditLocation"].ToString().ToUpper().Trim();
                double ClientRate = (frm["EditClientRate"] == null || frm["EditClientRate"] == "") ? 0 : Convert.ToDouble(frm["EditClientRate"]);
                int ApprovedKM = frm["EditApprovedKM"] == null ? 0 : Convert.ToInt32(frm["EditApprovedKM"]);

                KMRate.ClientID = ClientID;
                KMRate.Location = Location;
                KMRate.ClientRate = ClientRate;
                KMRate.ApprovedKM = ApprovedKM;
                KMRate.Status = true;
                TryUpdateModel<tbl_km_client_rate>(KMRate);
                db.SaveChanges();
                core.LoggingEntries("Client Mgmt.", "KM rate has updated to the client " + KMRate.tbl_clients.CompanyName + ".", User.Identity.Name);
                return Content("<script language='javascript' type='text/javascript'>$.modal.close();alert('Updated successfully');AddClientRateChart('" + ClientID + "');ClientRateChartList('" + ClientID + "')</script>");
            }
            catch (Exception)
            {
                return PartialView("_EditKMDetails", KMRate);
            }
        }
        #endregion

        #region SLABClientRate
        
        [HttpPost]
        public ActionResult AddClientSLABRateChart(FormCollection frm) 
        {
            string BillType = frm["BillingType"] == null ? "" : frm["BillingType"];
            long ID = 0, SeaterID = 0, VehicleTypeID = 0;
            long ClientID = frm["ClientID"] == null ? 0 : Convert.ToInt64(frm["ClientID"]);
            tbl_clients client = db.tbl_clients.Where(c => c.Status == true && c.ID == ClientID).SingleOrDefault();
            string Location = frm["Location"] == null ? "" : frm["Location"].ToString().ToUpper().Trim();
            string Seater = frm["Seater"] == null ? "" : frm["Seater"].ToString().ToUpper().Trim();
            string VehicleType = frm["VehicleType"] == null ? "" : frm["VehicleType"].ToString().ToUpper().Trim();

            string EmpId = frm["EmpId"] == null ? "" : frm["EmpId"];
            string EmpName = frm["EmpName"] == null ? "" : frm["EmpName"];
            string EmpGender = frm["EmpGender"] == null ? "" : frm["EmpGender"];
            bool LocationBased = frm["LocationBased"] == null ? false : true;
            bool EmpBased = frm["EmpIdBased"] == null ? false : true;
            
            if (Seater != ""){
                if (!core.VerifySeater(Seater)){
                    tbl_seaters _seater = db.tbl_seaters.Where(s => s.Status == true && s.Seater.ToUpper().Trim() == Seater.ToUpper().Trim()).SingleOrDefault();
                    SeaterID = _seater.ID;
                }
                else{
                    //tbl_seaters st = new tbl_seaters();
                    //st.Seater = Seater;
                    //st.Status = true;
                    //db.tbl_seaters.AddObject(st);
                    //db.SaveChanges();
                    //SeaterID = st.ID;
                    ViewBag.ErrorMsg = "Please check seater or please check with entered spelling here available in the seater master or not";
                    return PartialView("_ClientSLABRateChartList", _sbClientList);
                }
            }
            if (VehicleType != "")
            {
                if (!core.VerifyVehicleType(VehicleType))
                {
                    tbl_vehicle_types _types = db.tbl_vehicle_types.Where(s => s.Status == true && s.VehicleType.ToUpper().Trim() == VehicleType.ToUpper().Trim()).SingleOrDefault();
                    VehicleTypeID = _types.ID;
                }
                else
                {
                    //tbl_vehicle_types VT = new tbl_vehicle_types();
                    //VT.VehicleType = VehicleType;
                    //VT.Status = true;
                    //db.tbl_vehicle_types.AddObject(VT);
                    //db.SaveChanges();
                    //VehicleTypeID = VT.ID;
                    ViewBag.ErrorMsg = "Please check vehicle type or please check with entered spelling here available keywords are AC,NON-AC";
                    return PartialView("_ClientSLABRateChartList", _sbClientList);
                }
              
            }
            string seaterName = core.GetSeater(SeaterID);
            string Type = core.GetVehicleType(VehicleTypeID);
            if (LocationBased == true)
            {
                tbl_slab_client_rate _checkLoc = db.tbl_slab_client_rate.Where(s => s.Status == true && s.Location.ToUpper().Trim() == Location
                                                                                  && s.ClientID == ClientID && s.SeaterID == SeaterID && s.VehicleTypeID == VehicleTypeID).FirstOrDefault();
                if (_checkLoc != null) //if location details are  found on particular Location Name. based on ClientID,SeaterID
                {
                    ViewBag.ErrorMsg = "TRIP Rate is available on this Location: " + Location + ", Client: " + client.CompanyName.ToUpper() + " , Seater: " + seaterName.ToUpper() + ", Vehicle Type: " + Type.ToUpper() + "";
                    return PartialView("_ClientSLABRateChartList", _sbClientList);
                }
            }
            else
            {
                tbl_slab_client_rate _checkLoc = db.tbl_slab_client_rate.Where(s => s.Status == true && s.Location.ToUpper().Trim() == Location
                                                                                && s.ClientID == ClientID && s.SeaterID == SeaterID && s.VehicleTypeID == VehicleTypeID && s.EmpId.ToUpper().Trim() == EmpId.ToUpper().Trim()).FirstOrDefault();
                if (_checkLoc != null) //if location details are  found on particular Location Name. based on ClientID,SeaterID
                {
                    ViewBag.ErrorMsg = "TRIP Rate is available on this Location: " + Location + ", Client: " + client.CompanyName.ToUpper() + " , Seater: " + seaterName.ToUpper() + ", Vehicle Type: " + Type.ToUpper() + ", and Employee No: " + EmpId + "";
                    return PartialView("_ClientSLABRateChartList", _sbClientList);
                }
            }
          
            string ClientSlab = frm["ClientSlab"] == null ? "" : frm["ClientSlab"].ToUpper().Trim();
            double ClientRate = frm["ClientRate"] == null ? 0 : Convert.ToDouble(frm["ClientRate"]);
            string VendorSlab = frm["VendorSlab"] == null ? "" : frm["VendorSlab"].ToUpper().Trim();
            double VenderRate = frm["VendorRate"] == null ? 0 : Convert.ToDouble(frm["VendorRate"]);

            // Get list of saved rate list
            List<tbl_slab_client_rate> prevRateList = (List<tbl_slab_client_rate>)Session["SBPrevClientList"];
            if (prevRateList == null)
            {
                prevRateList = new core().GetSlabClientRateList(ClientID);
            }
            // Current New Slab rates to be added
            _sbClientList = (List<tbl_slab_client_rate>)Session["SBClientList"];

            // verify whether the slab rate or not
            if (_sbClientList != null)
            {
               bool _checkSessionData = _sbClientList.Where(s => s.Status == true && s.Location.ToUpper().Trim() == Location
                                                                              && s.ClientID == ClientID && s.SeaterID == SeaterID && s.VehicleTypeID == VehicleTypeID).Any();
                if (_checkSessionData == true) //if location details are  found on particular Location Name. based on ClientID,SeaterID in Already added data(session data)
                {
                    ViewBag.ErrorMsg = "TRIP Rate is available on this Location: " + Location + ", Client: " + client.CompanyName.ToUpper() + " , Seater: " + seaterName.ToUpper() + " and Vehicle Type: " + Type.ToUpper() + "";
                    return PartialView("_ClientSLABRateChartList", _sbClientList);
                }
                
            }
            // If session has no data 
            if (_sbClientList == null)
                _sbClientList = new List<tbl_slab_client_rate>();
            if (_sbClientList.Count() == 0)
                ID = 1;
            else
                ID = _sbClientList.Count() + 1;

            tbl_slab_client_rate tbl_Slab_Client_Rate = new tbl_slab_client_rate
            {
                SLABID = ID,
                ClientID = ClientID,
                Location = Location,
                SeaterID = SeaterID,
                VehicleTypeID = VehicleTypeID,
                ClientSlab = ClientSlab,
                ClientRate = ClientRate,
                VendorSlab = VendorSlab,
                VendorRate = VenderRate,
                EmpId = (EmpId == null || EmpId == "") ? null : EmpId,
                EmpName = EmpName,
                EmpGender = EmpGender,
                Status = true
            };

            db.tbl_slab_client_rate.AddObject(tbl_Slab_Client_Rate);
            db.SaveChanges();

            if (prevRateList == null)
            {
                // Add to current records to be added
                _sbClientList.Add(new tbl_slab_client_rate
                {
                    SLABID = ID,
                    ClientID = ClientID,
                    Location = Location,
                    SeaterID = SeaterID,
                    VehicleTypeID = VehicleTypeID,
                    ClientSlab = ClientSlab,
                    ClientRate = ClientRate,
                    VendorSlab = VendorSlab,
                    VendorRate = VenderRate,
                    EmpId = (EmpId == null || EmpId == "") ? null : EmpId,
                    EmpName = EmpName,
                    EmpGender = EmpGender,
                    Status = true
                });
                Session["SBClientList"] = _sbClientList;
                return PartialView("_ClientSLABRateChartList", _sbClientList);

            }
            else {
                // Add to existed List of records 
                prevRateList.Add(new tbl_slab_client_rate
                {
                    SLABID = ID,
                    ClientID = ClientID,
                    Location = Location,
                    SeaterID = SeaterID,
                    VehicleTypeID = VehicleTypeID,
                    ClientSlab = ClientSlab,
                    ClientRate = ClientRate,
                    VendorSlab = VendorSlab,
                    VendorRate = VenderRate,
                    EmpId = (EmpId == null || EmpId == "") ? null : EmpId,
                    EmpName = EmpName,
                    EmpGender = EmpGender,
                    Status = true
                });
                Session["SBPrevClientList"] = prevRateList;
                return PartialView("_ClientSLABRateChartList", prevRateList);
            }

           

               
            
            //Session["SBPrevClientList"] = prevRateList;
            //Session["SBClientList"] = _sbClientList;
            //return PartialView("_ClientSLABRateChartList", prevRateList);
        }
        [HttpPost]
        public ActionResult SaveClientSLABRateChart(FormCollection frm) 
        {
            long ClientID = frm["ClientID"] == null ? 0 : Convert.ToInt64(frm["ClientID"]);
            List<tbl_slab_client_rate> savedRateList = new core().GetSlabClientRateList(ClientID);
            List<tbl_slab_client_rate> _SBList = (List<tbl_slab_client_rate>)Session["SBClientList"];
            if (_SBList != null)
            {
                //if (savedRateList != null)
                //{
                //    foreach (tbl_slab_client_rate obj in savedRateList)
                //    {
                //        _SBList.Add(new tbl_slab_client_rate
                //        {
                //            //SLABID = ID,
                //            ClientID = obj.ClientID,
                //            Location = obj.Location,
                //            SeaterID = obj.SeaterID,
                //            VehicleTypeID = obj.VehicleTypeID,
                //            ClientSlab = obj.ClientSlab,
                //            ClientRate = obj.ClientRate,
                //            VendorSlab = obj.VendorSlab,
                //            VendorRate = obj.VendorRate,
                //            EmpId = (obj.EmpId == null || obj.EmpId == "") ? null : obj.EmpId,
                //            EmpName = obj.EmpName,
                //            EmpGender = obj.EmpGender,
                //            Status = true
                //        });
                //        //ID++;
                //    }
                //}
                //if (savedRateList != null)
                //{
                //    foreach (tbl_slab_client_rate sb in savedRateList)
                //    {
                //        tbl_slab_client_rate objslab = db.tbl_slab_client_rate.Where(a => a.SLABID == sb.SLABID).SingleOrDefault();
                //        db.tbl_slab_client_rate.DeleteObject(objslab);
                //    }
                //    db.SaveChanges();
                //}

                foreach (tbl_slab_client_rate sb in _SBList)
                    db.tbl_slab_client_rate.AddObject(sb);
                db.SaveChanges();
                Session["SBClientList"] = null;
                Session["SBPrevClientList"] = null;
                ViewBag.message = "Slab chart item added successfully.";
                core.LoggingEntries("Client Mgmt.", "Slab rate has added to the client " + core.GetClient(ClientID) + ".", User.Identity.Name);
                ViewBag.ClientID = ClientID;
                return PartialView("_SavedClientSLABRateList", _SBList);
            }
            else
            {
                ViewBag.ClientID = ClientID;
                return PartialView("_SavedClientSLABRateList", savedRateList);
            }
        }
        public ActionResult DeleteSBItem(long ID) 
        {
            tbl_slab_client_rate SBRate = db.tbl_slab_client_rate.Where(a => a.SLABID == ID).SingleOrDefault();
            SBRate.Status = false;
            TryUpdateModel(SBRate);
            db.SaveChanges();
            core objCore = new core();
            List<tbl_slab_client_rate> list = objCore.GetSlabClientRateList((long)SBRate.ClientID);
            ViewBag.message = "Item deleted successfully";
            core.LoggingEntries("Client Mgmt.", "Slab rate has deleted to the client " + SBRate.tbl_clients.CompanyName + ".", User.Identity.Name);
            ViewBag.ClientID = SBRate.ClientID;
            return PartialView("_SavedClientSLABRateList", list);
        }
        public ActionResult RemoveSBItem(long ID) 
        {
            _sbClientList = (List<tbl_slab_client_rate>)Session["SBClientList"];
            foreach (tbl_slab_client_rate obj in _sbClientList)
            {
                if (obj.SLABID == ID) // Will match once
                {
                    //Delete the Row
                    _sbClientList.Remove(obj); 
                    ReSBShuffleSlno();
                    break;
                }
            }
            ViewBag.message = "Item removed successfully";
            return PartialView("_ClientSLABRateChartList", _sbClientList);
        }
        public void ReSBShuffleSlno()
        {
            var i = 1;
            foreach (tbl_slab_client_rate obj in _sbClientList)
            {
                obj.SLABID = i;
                i++;
            }
        }

        [HttpGet]
        public ActionResult EditSLABDetails(long ID)
        {
            tbl_slab_client_rate SBRate = db.tbl_slab_client_rate.Where(a => a.SLABID == ID && a.Status == true).SingleOrDefault();
            ViewBag.SeaterID = SBRate.SeaterID;
            ViewBag.VehicleTypeID = SBRate.VehicleTypeID;
            ViewBag.ClientID = SBRate.ClientID;
            return PartialView("_EditSLABDetails", SBRate);
        }
        [HttpPost]
        public ActionResult EditSLABDetails(long ID , FormCollection frm)
        {
            tbl_slab_client_rate SBRate = db.tbl_slab_client_rate.Where(a => a.SLABID == ID && a.Status == true).SingleOrDefault();
            try
            {
                long SeaterID = 0, VehicleTypeID = 0;
                long ClientID = frm["hdnClientID"] == null ? 0 : Convert.ToInt64(frm["hdnClientID"]);
                tbl_clients client = db.tbl_clients.Where(c => c.Status == true && c.ID == ClientID).SingleOrDefault();
                string Location = frm["EditLocation"] == null ? "" : frm["EditLocation"].ToString().ToUpper().Trim();
                string Seater = frm["EditSeaterID"] == null ? "" : frm["EditSeaterID"].ToString().ToUpper().Trim();
                string VehicleType = frm["EditVehicleTypeID"] == null ? "" : frm["EditVehicleTypeID"].ToString().ToUpper().Trim();
                string EmpId = frm["EditEmpId"] == null ? "" : frm["EditEmpId"];
                string EmpName = frm["EditEmpName"] == null ? "" : frm["EditEmpName"];
                string EmpGender = frm["EditEmpGender"] == null ? "" : frm["EditEmpGender"];

                string ClientSlab = frm["EditClientSlab"] == null ? "" : frm["EditClientSlab"].ToUpper().Trim();
                double ClientRate = frm["EditClientRate"] == null ? 0 : Convert.ToDouble(frm["EditClientRate"]);
                string VendorSlab = frm["EditVendorSlab"] == null ? "" : frm["EditVendorSlab"].ToUpper().Trim();
                double VendorRate = frm["EditVendorRate"] == null ? 0 : Convert.ToDouble(frm["EditVendorRate"]);
                if (Seater != "")
                {
                    if (!core.VerifySeater(Seater))
                    {
                        tbl_seaters _seater = db.tbl_seaters.Where(s => s.Status == true && s.Seater.ToUpper().Trim() == Seater.ToUpper().Trim()).SingleOrDefault();
                        SeaterID = _seater.ID;
                    }
                    else
                    {
                        tbl_seaters st = new tbl_seaters();
                        st.Seater = Seater;
                        st.Status = true;
                        db.tbl_seaters.AddObject(st);
                        db.SaveChanges();
                        SeaterID = st.ID;
                    }
                }
                if (VehicleType != "")
                {
                    if (!core.VerifyVehicleType(VehicleType))
                    {
                        tbl_vehicle_types _types = db.tbl_vehicle_types.Where(s => s.Status == true && s.VehicleType.ToUpper().Trim() == VehicleType.ToUpper().Trim()).SingleOrDefault();
                        VehicleTypeID = _types.ID;
                    }
                    else
                    {
                        tbl_vehicle_types VT = new tbl_vehicle_types();
                        VT.VehicleType = VehicleType;
                        VT.Status = true;
                        db.tbl_vehicle_types.AddObject(VT);
                        db.SaveChanges();
                        VehicleTypeID = VT.ID;
                    }
                }
                string seaterName = core.GetSeater(SeaterID);
                string Type = core.GetVehicleType(VehicleTypeID);
                tbl_slab_client_rate _checkLoc = db.tbl_slab_client_rate.Where(s => s.Status == true && s.Location.ToUpper().Trim() == Location && s.ClientID == ClientID
                                                             && s.SeaterID == SeaterID && s.VehicleTypeID == VehicleTypeID && s.EmpId.ToUpper().Trim() == EmpId.ToUpper().Trim() && s.ClientRate == ClientRate && s.VendorRate == VendorRate).SingleOrDefault();
                if (_checkLoc != null) //if location details are  found on particular Location Name. based on ClientID,SeaterID
                {
                  //  return Content("<script language='javascript' type='text/javascript'>$.modal.close();alert('TRIP Rate is available on this Location: '" + Location + "', Client: '" + client.CompanyName.ToUpper() + "' , Seater: '" + seaterName.ToUpper() + "', Vehicle Type: '" + Type.ToUpper() + "', and Employee No: '" + EmpId + "'');</script>");
                    return Content("<script language='javascript' type='text/javascript'>$.modal.close();alert('TRIP Rate is available on this Details');</script>");
                }
               
              

               SBRate.ClientID = ClientID;
               SBRate.Location = Location;
               SBRate.SeaterID = SeaterID;
               SBRate.VehicleTypeID = VehicleTypeID;
               SBRate.ClientSlab = ClientSlab;
               SBRate.ClientRate = ClientRate;
               SBRate.VendorSlab = VendorSlab;
               SBRate.VendorRate = VendorRate;
               SBRate.EmpId = (EmpId == null || EmpId == "") ? null : EmpId;
               SBRate.EmpName = EmpName;
               SBRate.EmpGender = EmpGender;
               SBRate.Status = true;
               TryUpdateModel<tbl_slab_client_rate>(SBRate);
               db.SaveChanges();
               core.LoggingEntries("Client Mgmt.", "Slab rate has updated to the client " + SBRate.tbl_clients.CompanyName + ".", User.Identity.Name);
               return Content("<script language='javascript' type='text/javascript'>$.modal.close();alert('Updated successfully');AddClientRateChart('" + ClientID + "');ClientRateChartList('" + ClientID + "')</script>");

            }
            catch (Exception)
            {
                return PartialView("_EditSLABDetails", SBRate);
            }
        }
        #endregion

        #region PackageRates
        [HttpPost]            
        public ActionResult AddClientPackageRateChart(FormCollection frm,tbl_package_client_rates packDet)
        {
            try
            {
                DateTime? EffectedDt = string.IsNullOrEmpty(frm["EffectiveDt"]) ? (DateTime?)null : Convert.ToDateTime(frm["EffectiveDt"]);
                Int16 TimeUnit = Convert.ToInt16(frm["TimeUnit"]);

                if (db.tbl_package_client_rates.Where(a => a.VehicleTypeId == packDet.VehicleTypeId && a.VehicleModelId == packDet.VehicleModelId && a.PackModel == packDet.PackModel && a.WorkingDays == packDet.WorkingDays && a.ClientId == packDet.ClientId && a.PackageAmt == packDet.PackageAmt && a.IsActive == true).Any())
                {
                    return Json(new { success = false, msg = "Package rate has already exist with this structure.suggestion please do edit with existed structure", clientid = packDet.ClientId });
                }
                packDet.CreatedDt = DateTime.Now;
                packDet.CreatedBy = User.Identity.Name;
                packDet.PackageKM = 0;
                packDet.ExtHr = 0;
                packDet.ExtKM = 0;
                packDet.PackModel = "";
                packDet.IsActive = true;
                packDet.TimeUnit = TimeUnit;
                packDet.EffectiveDt = EffectedDt;

                if (packDet.TimeUnit == 1) // 1 = Days
                {
                    packDet.ExpiredDt = packDet.EffectiveDt.HasValue ? packDet.EffectiveDt.Value.AddDays(Convert.ToDouble(packDet.WorkingDays)) : (DateTime?)null;
                }
                else if (packDet.TimeUnit == 2)
                {
                    packDet.ExpiredDt = packDet.EffectiveDt.HasValue ? packDet.EffectiveDt.Value.AddMonths(Convert.ToInt32(packDet.WorkingDays)) : (DateTime?)null;
                    packDet.WorkingDays = packDet.WorkingDays;  // 30 days per month
                }
                     
                 

                db.tbl_package_client_rates.AddObject(packDet);
                db.SaveChanges();
                core.LoggingEntries("Client Mgmt.", "PACKAGE rate has added to the client " + core.GetClient((long)packDet.ClientId) + ".", User.Identity.Name);
                return Json(new { success = true, msg = "Package rate has been added successfully.", clientid = packDet.ClientId });
            }
            catch
            {
                return Json(new { success = false , msg = "An error occured while processing your request.Please try again." , clientid = packDet.ClientId });
            }
        }
        // Edit Package Structure 
        [HttpGet]
        public ActionResult EditPackStructure(long Id)
        {
            var PacStruct = db.tbl_package_client_rates.Where(a=>a.Id == Id && a.IsActive == true).FirstOrDefault();
            ViewBag.VehicleTypeID = new SelectList(core.LoadVehicleTypes(), "Value", "Text", PacStruct.VehicleTypeId);
            ViewBag.VehicleModelId = new SelectList(core.LoadVehicleModels(), "Value", "Text", PacStruct.VehicleModelId == null ? 0 : PacStruct.VehicleModelId);
            ViewBag.LocationId = new SelectList(core.LoadLocations(), "Value", "Text", PacStruct.LocationId == null ? 0 : PacStruct.LocationId);
            ViewBag.TimeUnit = new SelectList(core.LoadTimeUnit(), "Value", "Text", PacStruct.TimeUnit == null ? 0 : PacStruct.TimeUnit);
            ViewBag.ClientID = PacStruct.ClientId;
            return PartialView("_EditPackStructure", PacStruct);
        }

        [HttpPost]
        public ActionResult EditPackStructure(long Id,FormCollection frm,tbl_package_client_rates packDet)
        {
            tbl_package_client_rates _PackRate = db.tbl_package_client_rates.Where(a => a.Id == Id && a.IsActive == true).FirstOrDefault();
            ViewBag.VehicleTypeID = new SelectList(core.LoadVehicleTypes(), "Value", "Text", _PackRate.VehicleTypeId);
            ViewBag.VehicleModelId = new SelectList(core.LoadVehicleModels(), "Value", "Text", _PackRate.VehicleModelId == null ? 0 : _PackRate.VehicleModelId);
            ViewBag.PackModel = new SelectList(core.LoadPackModel(), "Value", "Text", _PackRate.PackModel == null ? "" : _PackRate.PackModel);
            ViewBag.TimeUnit = new SelectList(core.LoadTimeUnit(), "Value", "Text", _PackRate.TimeUnit == null ? 0 : _PackRate.TimeUnit);
            try
            {
                
                        if (packDet.TimeUnit == 1) // 1 = Days
                        {
                            packDet.ExpiredDt = packDet.EffectiveDt.HasValue ? packDet.EffectiveDt.Value.AddDays(Convert.ToDouble(packDet.WorkingDays)) : (DateTime?)null;
                        }
                        else if (packDet.TimeUnit == 2)
                        {
                            packDet.ExpiredDt = packDet.EffectiveDt.HasValue ? packDet.EffectiveDt.Value.AddMonths(Convert.ToInt32(packDet.WorkingDays)) : (DateTime?)null;
                            packDet.WorkingDays = packDet.WorkingDays * 30;  // 30 days per month
                        }
                     

                TryUpdateModel(_PackRate);
                db.SaveChanges();

                return Json(new { success = true, msg = "Package rate has been updated successfully.", clientid = packDet.ClientId });
            }
            catch
            {
                return Json(new { success = false, msg = "An error occured while processing your request.Please try again.", clientid = packDet.ClientId });
            }
        }

        // Delete Package Rate
        [HttpPost]
        public JsonResult DeletePackStructure(long Id)
        {
            tbl_package_client_rates packRate = db.tbl_package_client_rates.Where(a => a.Id == Id && a.IsActive == true).FirstOrDefault();
            long ClientId = Convert.ToInt64(packRate.ClientId);
            try
            {
                packRate.IsActive = false;
                TryUpdateModel(packRate);
                db.SaveChanges();
                return Json(new
                {
                    success = true,
                    msg = "Package rate has been deleted successfully.",
                    clientid = ClientId
                });
            }
            catch
            {
                return Json(new
                {
                    success = false,
                    msg = "An error occured while processing your request.Please try again.",
                    clientid = ClientId
                });
            }
        }

        public ActionResult ClientPackageRateList(long ClientId)
        {
            var packageList = db.tbl_package_client_rates.Where(a=>a.ClientId == ClientId && a.IsActive == true).ToList();
            return PartialView("_ClientPackageRateList", packageList);
        }

        #endregion

        #region LogSheetMapColumns
        public ActionResult AddColumnsToBeMapped(long ClientID)
        {
            ViewBag.clientID = ClientID;
            List<tbl_client_log_sheet_cols> _mapColsList = new core().GetMappedColsList(ClientID);
            if (_mapColsList != null)
            {
                ViewBag.ClientID = ClientID;
                return PartialView("_ColsMappingStructureList", _mapColsList);
            }
            return PartialView("_AddColumnsToBeMapped");
        }
        [HttpGet]
        public ActionResult AddMappedList(string fieldText, long ClientID)
        {
            int slno = 0;
            _fieldTextList = (List<SelectListItem>)Session["FieldTextList"];
            if (_fieldTextList == null)
                _fieldTextList = new List<SelectListItem>();
            // Verify whether client log sheet map columns exists or not 
            List<tbl_client_log_sheet_cols> _clientLogList = new core().GetMappedColsList(ClientID);
            if (_clientLogList != null)  // when it is edit
            {
                _availColsList = new core().GetTableFieldsByTableName("tbl_log_sheets");
                foreach (var item in _clientLogList)
                    _availColsList.Remove(_availColsList.Where(a => a.Value == item.FID.ToString()).SingleOrDefault());
                Session["AvailColsList"] = _availColsList;
            }
            else // when it is create
            {
                if (_fieldTextList.Count() == 0)
                {
                    if (Session["AvailColsList"] == null)
                    {
                        _availColsList = new core().GetTableFieldsByTableName("tbl_log_sheets");
                        Session["AvailColsList"] = _availColsList;
                    }
                }
            }
            if (_fieldTextList.Count() == 0)
                slno = 1;
            else
                slno = _fieldTextList.Count() + 1;
            _fieldTextList.Add(new SelectListItem { Value = slno.ToString(), Text = fieldText });
            ViewBag.FieldTextList = _fieldTextList;
            Session["FieldTextList"] = _fieldTextList;
            ViewBag.AvailColsList = (List<SelectListItem>)Session["AvailColsList"];
            ViewBag.ClientID = ClientID;
            return PartialView("_AddedMappedList");
        }
        [HttpGet]
        public ActionResult AddColsMappingStructure(int slNo, string fieldText, int fieldID, long ClientID)
        {
            int ID = 0;
            _clientLogMapColsList = (List<tbl_client_log_sheet_cols>)Session["ClientColsList"];
            if (_clientLogMapColsList == null)
                _clientLogMapColsList = new List<tbl_client_log_sheet_cols>();
            if (_clientLogMapColsList.Count() == 0)
                ID = 1;
            else
                ID = _clientLogMapColsList.Count() + 1;
            // Verify whether Columns Mapped Structure exists or not 
            List<tbl_client_log_sheet_cols> _clientLogList = new core().GetMappedColsList(ClientID);
            if (_clientLogMapColsList.Count() == 0)
            {
                if (_clientLogList != null)
                {
                    foreach (tbl_client_log_sheet_cols obj in _clientLogList)
                    {
                        _clientLogMapColsList.Add(new tbl_client_log_sheet_cols { ID = ID, SlNo = ID, FID = obj.FID, FieldText = obj.FieldText, ClientID = obj.ClientID });
                        ID++;
                    }
                    ViewBag.Status = "IsEdit";
                }
            }
            _clientLogMapColsList.Add(new tbl_client_log_sheet_cols { ID = ID, SlNo = ID, FID = fieldID, FieldText = fieldText, ClientID = ClientID });
            // Verify whether listbox columns exists or not and remove from listbox selected mapped items 
            if (_clientLogMapColsList.Count() > 0)
            {
                _fieldTextList = (List<SelectListItem>)Session["FieldTextList"];
                _fieldTextList.Remove((SelectListItem)_fieldTextList.Where(a => a.Value == slNo.ToString()).SingleOrDefault());
                Session["FieldTextList"] = _fieldTextList;
                _availColsList = (List<SelectListItem>)Session["AvailColsList"];
                _availColsList.Remove((SelectListItem)_availColsList.Where(a => a.Value == fieldID.ToString()).SingleOrDefault());
                Session["AvailColsList"] = _availColsList;
            }
            Session["ClientColsList"] = _clientLogMapColsList;
            return PartialView("_AddColsMappingStructure", _clientLogMapColsList);
        }
        // Get Mapped Structure List 
        public ActionResult GetMappedStructList(long ? ClientID)
        {
            _clientLogMapColsList = (List<tbl_client_log_sheet_cols>)Session["ClientColsList"];
            if (_clientLogMapColsList != null)
                return PartialView("_AddColsMappingStructure", _clientLogMapColsList);
            else if (ClientID != null)// Verify whether client log sheet map columns exists or not 
            {
                ViewBag.Status = "IsEdit";
                List<tbl_client_log_sheet_cols> _clientLogList = new core().GetMappedColsList(ClientID == null ? 0 : (long)ClientID);
                if (_clientLogList != null)
                    return PartialView("_AddColsMappingStructure", _clientLogList);
            }
            _clientLogMapColsList = new List<tbl_client_log_sheet_cols>();
            return PartialView("_AddColsMappingStructure", _clientLogMapColsList);
        }
        // Reload List Box items 
        public ActionResult ReLoadListBoxItems(long ClientID) 
        {
            List<tbl_client_log_sheet_cols> _savedClientLogList = new core().GetMappedColsList(ClientID);
            if (_savedClientLogList != null)
                ViewBag.Status = "IsEdit";
            _fieldTextList = (List<SelectListItem>)Session["FieldTextList"];
            if (_fieldTextList == null)
                _fieldTextList = new List<SelectListItem>();
            ViewBag.FieldTextList = _fieldTextList;
            _availColsList = (List<SelectListItem>)Session["AvailColsList"];
            ViewBag.AvailColsList = _availColsList;
            ViewBag.ClientID = ClientID; 
            return PartialView("_AddedMappedList");
        }
        // Save All Mapped Structure by client 
        [HttpPost]
        public ActionResult SaveColsMapStructure(FormCollection frm)
        {
            long ClientID = frm["ClientID"] == null ? 0 : Convert.ToInt64(frm["ClientID"]);
            // Verify whether Columns Mapped Structure exists or not 
            List<tbl_client_log_sheet_cols> _clientLogList = new core().GetMappedColsList(ClientID);
            if (_clientLogList != null)
            {
                foreach (tbl_client_log_sheet_cols obj in _clientLogList)
                {
                    tbl_client_log_sheet_cols clientLog = db.tbl_client_log_sheet_cols.Where(a => a.ID == obj.ID).SingleOrDefault();
                    db.tbl_client_log_sheet_cols.DeleteObject(clientLog);
                }
                db.SaveChanges();
            }
            List<tbl_client_log_sheet_cols> _colsMapList = (List<tbl_client_log_sheet_cols>)Session["ClientColsList"];
            foreach (tbl_client_log_sheet_cols obj in _colsMapList)
                db.tbl_client_log_sheet_cols.AddObject(obj);
            db.SaveChanges();
            Session["ClientColsList"] = null;
            return RedirectToAction("Index");
        }
        // Edit Mapped Columns
        public ActionResult EditLogSheetMappedCols(long ClientID)
        {
            ViewBag.ClientID = ClientID;
            ViewBag.Status = "IsEdit";
            return PartialView("_AddColumnsToBeMapped");
        }
        public ActionResult EditLogSheetListBoxCols(long ClientID)
        {
            List<tbl_client_log_sheet_cols> _clientLogList = new core().GetMappedColsList(ClientID);
            if (_clientLogList != null)  // when it is edit
            {
                _availColsList = new core().GetTableFieldsByTableName("tbl_log_sheets");
                foreach (var item in _clientLogList)
                    _availColsList.Remove(_availColsList.Where(a => a.Value == item.FID.ToString()).SingleOrDefault());
                Session["AvailColsList"] = _availColsList;
                ViewBag.Status = "IsEdit";
            }
            _fieldTextList = (List<SelectListItem>)Session["FieldTextList"];
            if (_fieldTextList == null)
                _fieldTextList = new List<SelectListItem>();  
            ViewBag.FieldTextList = _fieldTextList;
            ViewBag.AvailColsList = (List<SelectListItem>)Session["AvailColsList"];
            ViewBag.ClientID = ClientID;
            return PartialView("_AddedMappedList");
        }
        // Remove from the session list of client logsheet Mapped columns 
        public ActionResult RemoveMappedColsItem(long ID,long FID,long ClientID)
        {
            // First add Coulmns to listbox and keep it in session
            _availColsList = (List<SelectListItem>)Session["AvailColsList"];
            _availColsList.Add(new SelectListItem { Value = FID.ToString(), Text = new core().GetFieldName("tbl_log_sheets", FID) });
            Session["AvailColsList"] = _availColsList;
            // Verify whether Columns Mapped Structure exists or not 
            List<tbl_client_log_sheet_cols> _savedClientLogList = new core().GetMappedColsList(ClientID);
            if (_savedClientLogList != null) // If it is Edit Action
            {
                tbl_client_log_sheet_cols _Clientlog = db.tbl_client_log_sheet_cols.Where(a => a.ID == ID).SingleOrDefault();
                db.tbl_client_log_sheet_cols.DeleteObject(_Clientlog);
                db.SaveChanges();
                ViewBag.Status = "IsEdit";
                return PartialView("_AddColsMappingStructure", _savedClientLogList);
            }
            _clientLogMapColsList = (List<tbl_client_log_sheet_cols>)Session["ClientColsList"];
            _clientLogMapColsList.Remove(_clientLogMapColsList.Where(a => a.ID == ID).SingleOrDefault());
            ReShuffleColsSlno();
            return PartialView("_AddColsMappingStructure", _clientLogMapColsList);
        }
        public void ReShuffleColsSlno() 
        {
            var i = 1;
            foreach (tbl_client_log_sheet_cols obj in _clientLogMapColsList)
            {
                obj.ID = i;
                i++;
            }
        }
        #endregion

        #region LogSheetMappedFormat
        public ActionResult LogSheetFormat(long ClientID)
        {
            core objCore = new core();
            List<tbl_client_log_sheet_cols> _mapColsList = objCore.GetMappedColsList(ClientID);
            if (_mapColsList == null)
            {
                return PartialView("_LogSheetFormat", new CustomModel(null, null));
            }
            List<SelectListItem> _list = objCore.GetColsList(ClientID);
            string ColsNames = objCore.SeparateByComma(_list);
            // Headers List 
            List<SelectListItem> _headerList = objCore.GetHeaderColsList(ClientID);
            DataTable dt;
            using (SqlConnection sqlcon = new SqlConnection(ConfigurationManager.ConnectionStrings["ApplicationServices"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_logSheetFormat", sqlcon))
                {
                    cmd.Parameters.AddWithValue("@cols", ColsNames);
                    cmd.Parameters.AddWithValue("@clientID", ClientID.ToString());
                    cmd.CommandType = CommandType.StoredProcedure;
                    using (System.Data.SqlClient.SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        dt = new DataTable();
                        da.Fill(dt);
                    }
                }
            }
            DataTable LogSheetFormatDt = objCore.GetLogSheetFormatView(dt);
            ViewBag.LogSheetFormatDt = LogSheetFormatDt;
            return PartialView("_LogSheetFormat", new CustomModel(_headerList, null));
        }
        #endregion

        #region Client Documents
        [HttpGet]
        public ActionResult Documents()
        {
            ViewBag.DocTypeID = new SelectList(db.tbl_documents.Where(a => a.Status == true), "ID", "DocumentType");
            return View();
        }
        [HttpPost]
        public ActionResult Documents(FormCollection frm, HttpPostedFileBase DocPath, tbl_doc_client_nodes ClientDocs)
        {
            ViewBag.DocTypeID = new SelectList(db.tbl_documents.Where(a => a.Status == true), "ID", "DocumentType", ClientDocs.DocTypeID);
            long ClientID = Convert.ToInt64(frm["cid"]);
            ClientDocs.ClientID = ClientID;
            ViewBag.ClientID = ClientID;
            // ViewBag.VehicleRegNum = db.tbl_vehicles.Where(a => a.Status == true && a.ID == VehicleID).SingleOrDefault().VehicleRegNum.ToString();
            tbl_documents docs = db.tbl_documents.Where(d => d.Status == true && d.ID == ClientDocs.DocTypeID).FirstOrDefault();
            try
            {
                if (DocPath != null && DocPath.ContentLength > 0)
                {
                    DirectoryInfo thisFolder = new DirectoryInfo(Server.MapPath("../Content/Documents/Client/"));
                    if (!thisFolder.Exists)
                    {
                        thisFolder.Create();
                    }
                    string ext = Path.GetExtension(DocPath.FileName);
                    string filename = Path.GetFileNameWithoutExtension(DocPath.FileName);
                    ClientDocs.DocPath = "ClientDoc" + "_" + docs.DocCode + "_" + +ClientDocs.ClientID + ext;
                    var path = Path.Combine(Server.MapPath("../Content/Documents/Client/"), ClientDocs.DocPath);
                    DocPath.SaveAs(path);
                }
                ClientDocs.Status = true;
                db.tbl_doc_client_nodes.AddObject(ClientDocs);
                db.SaveChanges();
                return Content("<script language='javascript' type='text/javascript'>var r=confirm('Are you want to add one more document?'); if(r==true) {window.location='/Client/Documents';} if(r==false) {window.location='/Client/Index';}</script>");

            }
            catch (Exception)
            {
                return View("Documents", ClientDocs);
            }
        }
        [HttpGet]
        public ActionResult LoadDocumentsListByClient(long ClientID)
        {
            List<tbl_doc_client_nodes> _lstClientNodes = db.tbl_doc_client_nodes.Where(v => v.Status == true && v.ClientID == ClientID).ToList();
            core.ConvertToUppercase<tbl_doc_client_nodes>(_lstClientNodes);
            ViewBag.ClientID = ClientID;
            if (_lstClientNodes.Count > 0)
                return PartialView("_DocumentsListByClient", _lstClientNodes);
            else
                return PartialView("_NoDocuments");
        }
        //For send mail with multiple uploads
        [HttpPost]
        public ActionResult SendMail(FormCollection frm, long ClientID)
        {
            List<tbl_doc_client_nodes> _lstClientNodes = db.tbl_doc_client_nodes.Where(v => v.Status == true && v.ClientID == ClientID).ToList();

            string EmailAddress = WebConfigurationManager.AppSettings["EmailAddress"].ToString();
            string PassWord = WebConfigurationManager.AppSettings["PassWord"].ToString();

            String str = String.Empty;
            System.Net.Mail.MailMessage mail = new System.Net.Mail.MailMessage();
            SmtpClient smtp = new SmtpClient();
            string sendto = frm["mail"].ToString().Trim();
            string subject = frm["txtSubject"].ToString().Trim();
            string body = frm["txtBody"].ToString().Trim();
            try
            {
                mail.To.Add(sendto);
                mail.From = new MailAddress(EmailAddress, "Client Documents");
                mail.Subject = subject;
                //Multiple File Attachment
                int docCnt = 0;
                for (int i = 0; i < _lstClientNodes.Count; i++)
                {
                    bool isChecked = frm.GetValues("chkdoc_" + _lstClientNodes[i].ID).Contains("true"); //Getting all checkboxes values
                    long DocID = Convert.ToInt64(_lstClientNodes[i].ID);
                    if (isChecked == true)
                    {
                        tbl_doc_client_nodes _vdocs = db.tbl_doc_client_nodes.Where(d => d.Status == true && d.ID == DocID).FirstOrDefault();
                        string Uplodefile = Request.PhysicalApplicationPath + "Content\\Documents\\Client\\" + _vdocs.DocPath;
                        mail.Attachments.Add(new Attachment(Uplodefile));
                        docCnt += 1;
                    }
                }
                if (docCnt == 0)
                {
                    return Content("<script language='javascript' type='text/javascript'>alert('Please select atleast on document.');ViewClientDocuments('" + ClientID + "'); $('#divEMailDet').show();</script>");
                }
                mail.Body = body;
                mail.IsBodyHtml = true;
                mail.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;
                smtp.EnableSsl = true;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential(EmailAddress, PassWord);
                smtp.Host = "smtp.gmail.com";
                smtp.Port = 587;
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                mail.Headers.Add("Disposition-Notification-To", EmailAddress);
                smtp.Send(mail);
                return Content("<script language='javascript' type='text/javascript'>alert('Mail send successfully');ViewClientDocuments('" + ClientID + "');</script>");

            }
            catch (Exception ex)
            {
                return Content("<script language='javascript' type='text/javascript'>alert('Delivery to the following recipient failed permanently'" + sendto + "'); ViewClientDocuments('" + ClientID + "');</script>");
            }
        }
        [HttpGet]
        public ActionResult DeleteClientDocuments(long ID)
        {
            tbl_doc_client_nodes _cdocs = db.tbl_doc_client_nodes.Where(a => a.Status == true && a.ID == ID).SingleOrDefault();
            long ClientID = (long)_cdocs.ClientID;
            try
            {
                _cdocs.Status = false;
                TryUpdateModel(_cdocs);
                db.SaveChanges();
                return Content("<script language='javascript' type='text/javascript'>alert('Document delete successfully');ViewClientDocuments('" + ClientID + "');</script>");

            }
            catch (Exception ex)
            {
                return View();
            }

        }
        [HttpPost]
        public ActionResult AuditDocments(FormCollection frm, long ClientID)
        {
            List<tbl_doc_client_nodes> _lstClientNodes = db.tbl_doc_client_nodes.Where(v => v.Status == true && v.ClientID == ClientID).ToList();
            int docCnt = 0;
            for (int i = 0; i < _lstClientNodes.Count; i++)
            {
                bool isChecked = frm.GetValues("chkdoc_" + _lstClientNodes[i].ID).Contains("true"); //Getting all checkboxes values
                long DocID = Convert.ToInt64(_lstClientNodes[i].ID);
                if (isChecked == true)
                {
                    tbl_doc_client_nodes _vdocs = db.tbl_doc_client_nodes.Where(d => d.Status == true && d.ID == DocID).FirstOrDefault();
                    _vdocs.AuditedBy = User.Identity.Name.ToUpper();
                    _vdocs.Audited = true;
                    db.SaveChanges();
                    docCnt += 1;
                }
            }
            if (docCnt == 0)
            {
                return Content("<script language='javascript' type='text/javascript'>alert('Please select atleast one document.');ViewClientDocuments('" + ClientID + "');</script>");
            }
            return Content("<script language='javascript' type='text/javascript'>alert('Selected Documents are Audited successfully.');ViewClientDocuments('" + ClientID + "');</script>");

        }
        #endregion

        #region Custom Methods
        public bool ValidateForm(FormCollection frm, tbl_clients client, string Action)
        {
            if (frm["CompanyName"] == null || frm["CompanyName"].ToString().Trim().Length == 0)
                ModelState.AddModelError("CompanyName", "Please enter company name.");
            //if (frm["PhoneNumber"].ToString().Trim().Length > 10)
            //    ModelState.AddModelError("PhoneNumber", "Contact number should not be greater than 10 digits.");
            //if (frm["PhoneNumber"].ToString().Trim().Length < 10)
            //    ModelState.AddModelError("PhoneNumber", "Contact number should not be less than 10 digits.");
            if (frm["BillingTypeID"].ToString().Trim().Length == 0)
                ModelState.AddModelError("BillingTypeID", "Please select billing type.");
            //if (Action == "Create")
            //{
            //    if (client.AgreementDocument == null || client.AgreementDocument.ToString().Trim().Length == 0)
            //        ModelState.AddModelError("AgreementDocument", "Please upload Agreement Document.");
            //}
            return ModelState.IsValid;
        }
        // Verify whether the client has rate strucutre or not 
        public bool VerifyRateStrucutre(long ClientID)
        {
            tbl_clients clientDet = db.tbl_clients.Where(a => a.ID == ClientID && a.Status == true).SingleOrDefault();
            core objCore = new core();
            if (clientDet.tbl_billing_types.BillingType.Trim().Contains("KILO METER"))
            {
                List<tbl_km_client_rate> list = objCore.GetKMClientRateList(ClientID);
                if (list != null)
                    return true;
                else
                    return false;
            }
            else if (clientDet.tbl_billing_types.BillingType.Trim().Contains("TRIPS"))
            {
                List<tbl_slab_client_rate> list = objCore.GetSlabClientRateList(ClientID);
                if (list != null)
                    return true;
                else
                    return false;
            }
            else if (clientDet.tbl_billing_types.BillingType.Trim().Contains("PACKAGE"))
            {
                List<tbl_package_client_rates> list = objCore.GetPackageClientRateList(ClientID);
                if (list != null)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }
        public void UpdateClientRateChart(tbl_clients clientDet)
        {
            core objCore = new core();
            if (clientDet.tbl_billing_types.BillingType == "KILO METER")
            {
                List<tbl_km_client_rate> _kmList = objCore.GetKMClientRateList(clientDet.ID);
                if (_kmList != null)
                {
                    foreach (tbl_km_client_rate km in _kmList)
                    {
                        tbl_km_client_rate objkm = db.tbl_km_client_rate.Where(a => a.KMID == km.KMID).SingleOrDefault();
                        db.tbl_km_client_rate.DeleteObject(objkm);
                    }
                    db.SaveChanges();
                }
            }
            else if (clientDet.tbl_billing_types.BillingType == "TRIPS")
            {
                List<tbl_slab_client_rate> _sbList = objCore.GetSlabClientRateList(clientDet.ID);
                if (_sbList != null)
                {
                    foreach (tbl_slab_client_rate sb in _sbList)
                    {
                        tbl_slab_client_rate objslab = db.tbl_slab_client_rate.Where(a => a.SLABID == sb.SLABID).SingleOrDefault();
                        db.tbl_slab_client_rate.DeleteObject(objslab);
                    }
                    db.SaveChanges();
                }
            }
            else if (clientDet.tbl_billing_types.BillingType == "PACKAGE")
            {
                List<tbl_package_client_rates> _pkList = objCore.GetPackageClientRateList(clientDet.ID);
                if (_pkList != null)
                {
                    foreach (tbl_package_client_rates pk in _pkList)
                    {
                        tbl_package_client_rates objPack = db.tbl_package_client_rates.Where(a => a.Id == pk.Id && a.IsActive == true).SingleOrDefault();
                        db.tbl_package_client_rates.DeleteObject(objPack);
                    }
                    db.SaveChanges();
                }
            }
        }
        public JsonResult GetCurrentVehicleType(long VehicleTypeID, long ID)
        {
            var vTypes = db.tbl_vehicle_types.Where(s => s.Status == true && s.ID == VehicleTypeID).FirstOrDefault();
            return Json(new { success = true, VehicleType = vTypes.VehicleType }, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetCurrentSeater(long SeaterID, long ID)
        {
            var vSeaters = db.tbl_seaters.Where(s => s.Status == true && s.ID == SeaterID).FirstOrDefault();
            return Json(new { success = true, Seater = vSeaters.Seater }, JsonRequestBehavior.AllowGet);
        }

        public void UpdateClientSlabRate()
        {
            var slabRateList = db.tbl_slab_client_rate.Where(a => a.ClientID == 8 && a.Status == true).ToList();

            foreach (var item in slabRateList)
            {
                var sbRt = slabRateList.Where(a => a.Location.Trim() == item.Location.Trim()).ToList();

                if (sbRt.Count() == 2)
                {
                    foreach (var item1 in sbRt)
                    {
                        if (item1.VehicleTypeID != null)
                        {
                            var sbVehType = sbRt.Where(a => a.VehicleTypeID == item1.VehicleTypeID).ToList();
                            if (sbVehType.Count() == 2)
                            {
                                var finalRate = sbVehType.LastOrDefault();
                                // do action 
                                db.tbl_slab_client_rate.DeleteObject(finalRate);
                                db.SaveChanges();
                                continue;
                            }
                            else
                                continue;
                        }
                    }
                }
                else
                    continue;
            }
        }

        public void UpdateClientSlabRate2()
        {
            int[] str = { 10, 11, 12, 13 };

            foreach (int i in str)
            {
                var kmRateList = db.tbl_km_client_rate.Where(a => a.Status == true && a.ClientID == i).ToList();

                foreach (var item in kmRateList)
                {
                    var kmRt = kmRateList.Where(a => a.Location.Trim() == item.Location.Trim()).ToList();

                    if (kmRt.Count() == 2)
                    {
                        var finalRate = kmRt.LastOrDefault();
                        // do action 
                        db.tbl_km_client_rate.DeleteObject(finalRate);
                        db.SaveChanges();
                        continue;
                    }
                    else
                        continue;
                }
            }
        }

        public void UpdateClientSlabRate3()
        {
            var slabRateList = db.tbl_slab_client_rate.Where(a => a.ClientID == 5 && a.Status == true).ToList();

            foreach (var item in slabRateList)
            {
                var sbRt = slabRateList.Where(a => a.Location.Trim() == item.Location.Trim()).ToList();

                if (sbRt.Count() == 3)
                {
                    foreach (var item1 in sbRt)
                    {
                        if (item1.VehicleTypeID != null)
                        {
                            var sbVehType = sbRt.Where(a => a.VehicleTypeID == item1.VehicleTypeID).ToList();
                            if (sbVehType.Count() == 2 || sbVehType.Count() == 3)
                            {
                                var finalRate = sbVehType.LastOrDefault();
                                // do action 
                                db.tbl_slab_client_rate.DeleteObject(finalRate);
                                db.SaveChanges();
                                continue;
                            }
                            else
                                continue;
                        }
                    }
                }
                else
                    continue;
            }
        } 
        #endregion

    }
}
