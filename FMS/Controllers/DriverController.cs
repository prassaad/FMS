using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;
using System.Globalization;
using System.Drawing;
using System.IO;
using System.Data;
using System.Data.Entity;
using System.Drawing.Imaging;
using System.Web.Configuration;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Web.Util;
using System.ComponentModel;
using System.Text;
using FMS.Helpers;

namespace FMS.Controllers
{
    [CustomAuthorizationFilter]
    public class DriverController : Controller
    {
        #region ctors
        private FMSDBEntities db;
        public core core = new core();
        public DriverController()
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
        public JsonResult GetDriversList(int iDisplayStart, int iDisplayLength, string sSearch, int iColumns,
      int iSortingCols, int iSortCol_0, string sSortDir_0,
      int sEcho, string mDataProp_Key)
        {
            var IList = GetEnumerableList();
            var filteredLogSheet = IList
                .Select(l => new[]{
                    l.Id.ToString(),
                    l.Text1.ToString(),
                    l.Text2.ToString(),
                    l.Text3.ToString(),
                    l.Text4.ToString(),
                    l.Text5.ToString(),
                    l.Text6.ToString(),
                    l.Text7.ToString(),
                    l.Text8.ToString(),
                    l.Text9.ToString(),
                    l.Text10.ToString(),
                    l.Status1.ToString(),
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
            var tempList = (from l in db.tbl_drivers
                            where l.Status == true
                            select l).ToList();
            foreach (tbl_drivers ds in tempList)
            {
                list.Add(new CommonFields
                {
                    Id = ds.ID,
                    Text1 = ds.FirstName + " " + ds.LastName,
                    Text2 = ds.CurrentVehicleID == null ? "" : ds.tbl_vehicles.VehicleRegNum,
                    Text3 = ds.Address1 == null ? "" : ds.Address1,
                    Text4 = ds.ContactNumber == null ? "" : ds.ContactNumber.ToString(),
                    Text5 = ds.LicenceNumber == null ? "" : ds.LicenceNumber,
                    Text6 = ds.LicenceValidity == null ? "" : ds.LicenceValidity.Value.ToShortDateString(),
                    Text7 = ds.BadgeNumber == null ? "" : ds.BadgeNumber,
                    Text8 = ds.BadgeValidity == null ? "" : ds.BadgeValidity.Value.ToShortDateString(),
                    Text9 = ds.ReferenceName == null ? "" : ds.ReferenceName,
                    Text10 = ds.ReferenceNumber == null ? "" : ds.ReferenceNumber.ToString(),
                    Status1 = ds.ProfileComplete == null ? false : (bool)ds.ProfileComplete,
                    Status = (bool)ds.Status
                });
            }
            core.ConvertToUppercase<CommonFields>(list);
            return list.OrderByDescending(a => a.Id);
        }
        #endregion

        #region Add Driver
        [HttpGet]
        public ActionResult Create()
        {
            // Populate fields into Dropdown from database using viewbag 
            ViewBag.VendorID = new SelectList(db.tbl_vendor_details.Where(a => a.Status == true), "ID", "FirstName");
            return View(new tbl_drivers());
        }
        [HttpGet]
        public ActionResult CreatePartial(long VendorId, long? VehicleID)
        {
            ViewBag.VendorID = VendorId;
            ViewBag.CurrentVehicleID = (VehicleID == null || VehicleID == 0) ? null : VehicleID;
            tbl_vehicles _vehicleDet = db.tbl_vehicles.Where(a => a.Status == true && a.ID == VehicleID).FirstOrDefault();

            if (VehicleID != null)
            {
                if (_vehicleDet.OwnerCumDriver == true)
                {
                    tbl_vendor_details _vDet = db.tbl_vendor_details.Where(a => a.Status == true && a.ID == VendorId).FirstOrDefault();
                    tbl_drivers DR = new tbl_drivers();
                    DR.FirstName = _vDet.FirstName;
                    DR.LastName = _vDet.LastName;
                    DR.Address1 = _vDet.Address;
                    DR.ContactNumber = _vDet.ContactNumber;
                    DR.CurrentVehicleID = VehicleID;
                    DR.VendorID = _vDet.ID;
                    DR.PhotoUrl = _vDet.PhotoUrl;
                    return PartialView("_CreateDriverPartial", DR);
                }
                else
                    return PartialView("_CreateDriverPartial");
            }
            else
                return PartialView("_CreateDriverPartial");
        }
        [HttpPost]
        public ActionResult Create([Bind(Exclude = "ID")]FormCollection frm, tbl_drivers driver, HttpPostedFileBase PhotoUrl, HttpPostedFileBase IDProof)
        {
            ViewBag.VendorID = driver.VendorID; //new SelectList(db.tbl_vendor_details.Where(a => a.Status == true), "ID", "FirstName");
            ViewBag.CurrentVehicleID = (frm["CurrentVehicleID"] == null || frm["CurrentVehicleID"] == "") ? 0 : Convert.ToInt64(frm["CurrentVehicleID"]);
            core.ConvertToUppercase(driver);
            try
            {
                if (ValidateForm(driver, false))
                {
                    //if (ValidatePhoto(driver,PhotoUrl,IDProof))
                    //{

                    if (PhotoUrl != null && PhotoUrl.ContentLength > 0)
                    {
                        string ext = Path.GetExtension(PhotoUrl.FileName);
                        driver.PhotoUrl = "Photo" + "_" + driver.FirstName + "_" + driver.ID + ext;
                        var path = Path.Combine(Server.MapPath("../Content/uploadimages/"), driver.PhotoUrl);
                        PhotoUrl.SaveAs(path);
                    }
                    if (IDProof != null && IDProof.ContentLength > 0)
                    {
                        string ext = Path.GetExtension(IDProof.FileName);
                        driver.IDProof = "IDProof" + "_" + driver.FirstName + "_" + driver.ID + ext;
                        var path = Path.Combine(Server.MapPath("../Content/uploadimages/"), driver.IDProof);
                        IDProof.SaveAs(path);
                    }
                    long CurrentVehicleID = (long)driver.CurrentVehicleID;
                    if (CurrentVehicleID == -1)
                        driver.CurrentVehicleID = null;
                    driver.Status = true;
                    driver.ProfileComplete = IsComplete(driver);
                    core.ConvertToUppercase(driver);
                    db.tbl_drivers.AddObject(driver);
                    db.SaveChanges();
                    //Add VehicleDriver Details
                    tbl_vehicle_driver Vdriver = new tbl_vehicle_driver();
                    if (CurrentVehicleID == -1)
                    {
                        Vdriver.VehicleID = null;
                        Vdriver.AssignedOn = null;
                        Vdriver.IsProxy = true;
                    }
                    else
                    {
                        Vdriver.VehicleID = driver.CurrentVehicleID;
                        Vdriver.AssignedOn = DateTime.Now;
                        Vdriver.IsProxy = false;
                    }
                    Vdriver.VehicleID = driver.CurrentVehicleID;
                    Vdriver.DriverID = driver.ID;
                    Vdriver.AssignedOn = DateTime.Now;
                    Vdriver.Status = true;
                    db.tbl_vehicle_driver.AddObject(Vdriver);
                    db.SaveChanges();
                    core.LoggingEntries("Driver Mgmt.", "Added new driver with the name " + driver.FirstName + " " + driver.LastName + "", User.Identity.Name);
                    return Content("<script language='javascript' type='text/javascript'>$.modal.close();alert('Driver added suceessfully with the name " + driver.FirstName + " " + driver.LastName + "');DriversList('" + ((ViewBag.CurrentVehicleID == 0) ? driver.VendorID : ViewBag.CurrentVehicleID) + "');</script>");
                    //return Json(new { success = true, msg = "Driver added successfully with the name " + driver.FirstName + " "+ driver.LastName + " " });
                    //}
                }

            }
            catch (Exception)
            {
                return PartialView("_CreateDriverPartial", driver);
                // return Json(new { success = true, msg = "Fail to add Driver. Error Message is " + ex.Message + "" });
            }
            return PartialView("_CreateDriverPartial", driver);
            //return Json(new { success = false, msg = "Fail to add driver.Please Check entered fields sholud be in correct format.", model = driver });
        }
        #endregion

        #region Edit
        [HttpGet]
        public ActionResult Edit(long ID)
        {
            tbl_drivers driverDet = db.tbl_drivers.Where(d => d.Status == true && d.ID == ID).SingleOrDefault();
            ViewBag.VendorID = new SelectList(db.tbl_vendor_details.Where(a => a.Status == true), "ID", "FirstName", driverDet.VendorID);
            return View(driverDet);
        }

        [HttpPost]
        public ActionResult Edit(long ID, FormCollection frm, tbl_drivers driver, HttpPostedFileBase PhotoUrl, HttpPostedFileBase IDProof)
        {
            
            tbl_drivers driverDet = db.tbl_drivers.Where(d => d.Status == true && d.ID == ID).SingleOrDefault();
            long VehicleId = Convert.ToInt64(driverDet.CurrentVehicleID);
            long SelectedVehicleID = Convert.ToInt64(driver.CurrentVehicleID);
            long CurrentVehicleID = frm["CurrentVehicleID"] == null ? 0 : Convert.ToInt64(frm["CurrentVehicleID"]);
            ViewBag.VendorID = new SelectList(db.tbl_vendor_details.Where(a => a.Status == true), "ID", "FirstName", driverDet.VendorID);
            ViewBag.CurrentVehicleID = (frm["CurrentVehicleID"] == null || frm["CurrentVehicleID"] == "") ? 0 : Convert.ToInt64(frm["CurrentVehicleID"]);
            core.ConvertToUppercase(driver);
            string PhotoStr = driverDet.PhotoUrl;
            string IDPhotoStr = driverDet.IDProof;
            try
            {
                if (ValidateForm(driver, true))
                {

                    if (CurrentVehicleID == -1)
                        driverDet.CurrentVehicleID = null;
                    else
                        driverDet.CurrentVehicleID = SelectedVehicleID;

                    if (VehicleId != SelectedVehicleID)
                    {
                        //Add RelievedOn date when driver change Vechile and Add new Vehicle set AssignedOn
                        tbl_vehicle_driver Vdriver = null;
                        // Set VehicleID to -1 for checking whether the proxy driver or normal driver
                        Vdriver = db.tbl_vehicle_driver.Where(vd => vd.DriverID == ID && vd.Status == true).SingleOrDefault();

                        if (CurrentVehicleID != -1)  // user selected vehicle 
                        {
                            Vdriver.RelievedOn = DateTime.Now;
                            Vdriver.Status = false;
                        }
                        if (VehicleId == 0)
                        {
                            Vdriver.RelievedOn = null;
                            Vdriver.Status = false;
                        }
                        else
                        {
                            Vdriver.RelievedOn = DateTime.Now;
                            Vdriver.Status = false;
                        }
                        tbl_vehicle_driver vehicDriver = new tbl_vehicle_driver();
                        if (CurrentVehicleID == -1)
                        {
                            vehicDriver.VehicleID = null;
                            vehicDriver.IsProxy = true;
                        }
                        else
                        {
                            vehicDriver.VehicleID = driver.CurrentVehicleID;
                            vehicDriver.IsProxy = false;
                            vehicDriver.AssignedOn = DateTime.Now;
                        }
                        vehicDriver.DriverID = driver.ID;
                        vehicDriver.Status = true;
                        db.tbl_vehicle_driver.AddObject(vehicDriver);
                    }
                    // Update changes here
                    driverDet.Status = true;
                    driverDet.AadharNum = driver.AadharNum;
                    TryUpdateModel(driverDet);
                    core.ConvertToUppercase(driverDet);
                    if (driverDet.CurrentVehicleID == -1)
                        driverDet.CurrentVehicleID = null;
                    if (PhotoUrl != null && PhotoUrl.ContentLength > 0)
                    {
                        if (ValidPhoto(PhotoUrl, 1))
                        {
                            string ext = Path.GetExtension(PhotoUrl.FileName);
                            driverDet.PhotoUrl = "Photo" + "_" + driverDet.FirstName + "_" + ID + ext;
                            var path = Path.Combine(Server.MapPath("../Content/uploadimages/"), driverDet.PhotoUrl);
                            PhotoUrl.SaveAs(path);
                        }
                    }
                    else
                        driverDet.PhotoUrl = PhotoStr;
                    if (IDProof != null && IDProof.ContentLength > 0)
                    {
                        if (ValidPhoto(IDProof, 2))
                        {
                            string ext = Path.GetExtension(IDProof.FileName);
                            driverDet.IDProof = "IDProof" + "_" + driverDet.FirstName + "_" + ID + ext;
                            var path = Path.Combine(Server.MapPath("../Content/uploadimages/"), driverDet.IDProof);
                            IDProof.SaveAs(path);
                        }
                    }
                    else
                        driverDet.IDProof = IDPhotoStr;

                    db.SaveChanges();
                    core.LoggingEntries("Driver Mgmt.", "Updated driver with the name " + driver.FirstName + " " + driver.LastName + "", User.Identity.Name);
                    return Content("<script language='javascript' type='text/javascript'>$.modal.close();alert('Updated successfully');DriversList('" + ((ViewBag.CurrentVehicleID == 0) ? driver.VendorID : ViewBag.CurrentVehicleID) + "');</script>");
                }
            }
            catch (Exception)
            {
                //return Json(new { success = false, msg = "Fail to update : Error message " + ex.Message + "" });
                return PartialView("_ViewDriverDetailsPartial", driverDet);
            }
            //return Json(new { success = false, msg = "Fail to update.Please Check entered fields sholud be in correct format." });
            return PartialView("_ViewDriverDetailsPartial", driverDet);
        }
        #endregion

        #region Delete
        [HttpGet]
        public ActionResult Delete(long ID)
        {
            tbl_drivers driverDet = db.tbl_drivers.Where(d => d.Status == true && d.ID == ID).SingleOrDefault();
            return View(driverDet);
        }
        [HttpPost]
        public ActionResult Delete(long ID, FormCollection frm)
        {
            tbl_drivers driverDet = db.tbl_drivers.Where(d => d.Status == true && d.ID == ID).SingleOrDefault();
            tbl_vehicle_driver vehdriverDet = db.tbl_vehicle_driver.Where(d => d.Status == true && d.DriverID == ID).SingleOrDefault();
            ViewBag.CurrentVehicleID = (frm["CurrentVehicleID"] == null || frm["CurrentVehicleID"] == "" || frm["CurrentVehicleID"] == "-1") ? 0 : Convert.ToInt64(frm["CurrentVehicleID"]);
            try
            {
                driverDet.Status = false;
                TryUpdateModel(driverDet);
                if (driverDet.CurrentVehicleID == -1)
                    driverDet.CurrentVehicleID = null;
                db.SaveChanges();
                //When ever we delete driver lets delete VehicleDriver Details this time
                //what happens is driver delete then particular driver Vehicle Relieved from that driver
                if (vehdriverDet != null)
                {
                    vehdriverDet.Status = false;
                    vehdriverDet.RelievedOn = DateTime.Now;
                    db.SaveChanges();
                }
                core.LoggingEntries("Driver Mgmt.", "Deleted driver with the name " + driverDet.FirstName + " " + driverDet.LastName + "", User.Identity.Name);
                return Content("<script language='javascript' type='text/javascript'>$.modal.close();alert('Driver deleted successfully');DriversList('" + ((ViewBag.CurrentVehicleID == 0) ? driverDet.VendorID : ViewBag.CurrentVehicleID) + "');</script>");

            }
            catch (Exception ex)
            {

                return PartialView("_ViewDriverDetailsPartial", driverDet);
                //return Json(new { success = false, msg = "Fail to Delete : Error message" + ex.Message + "" });
            }
        }
        #endregion

        #region View Driver Details
        // View Driver Details 
        [HttpGet]
        public ActionResult ViewDriverDetails(long Id, long? VehicleID)
        {
            tbl_drivers driverDet = db.tbl_drivers.Where(d => d.Status == true && d.ID == Id).SingleOrDefault();
            ViewBag.VendorID = driverDet.VendorID;
            ViewBag.CurrentVehicleID = (VehicleID == null || VehicleID == 0) ? null : VehicleID;
            return PartialView("_ViewDriverDetailsPartial", driverDet);
        }
        [HttpGet]
        public ActionResult Details(long ID)
        {
            tbl_drivers driverDet = db.tbl_drivers.Where(a => a.Status == true && a.ID == ID).SingleOrDefault();
            return View(driverDet);
        }
        // load Vehicles by Vendor selction 
        public JsonResult LoadVehiclesByVendor(string Vendor)
        {
            long VendorID = Convert.ToInt64(Vendor);
            var vehicles = db.tbl_vehicles.Where(a => a.VendorID == VendorID && a.Status == true).Select(a => new { a.VehicleRegNum, a.ID }).ToList();
            return Json(vehicles, JsonRequestBehavior.AllowGet);
        }
        // Get List of Drivers By Vendor
        public ActionResult GetDriverListByVendor(long VendorId)
        {
            core objCore = new core();
            List<tbl_drivers> list = objCore.GetDriversList(VendorId);
            ViewBag.VehicleID = 0;
            if (list.Count <= 2)
                return PartialView("_GetDriverList", list);
            else
                return PartialView("_GetDriverListPartial", list);

        }

        // Get Driver list by Vehicle ID 

        public ActionResult GetDriverListByVehicle(long VehicleID)
        {
            List<tbl_drivers> list = GetDriverList(VehicleID);
            ViewBag.VehicleID = VehicleID;
            if (list.Count <= 2)
                return PartialView("_GetDriverList", list);
            else
                return PartialView("_GetDriverListPartial", list);
        }

        // Get Driver list by Vehicle partially 
        [HttpGet]
        public ActionResult LoadDriversListByVehicle(long vehicleID)
        {
            List<tbl_drivers> list = GetDriverList(vehicleID);
            return PartialView("_GetDriversListByVehicle", list);
        }
        #endregion

        #region Driver Documents
        [HttpGet]
        public ActionResult Documents()
        {
            ViewBag.DocTypeID = new SelectList(db.tbl_documents.Where(a => a.Status == true), "ID", "DocumentType");
            return View();
        }
        [HttpPost]
        public ActionResult Documents(FormCollection frm, HttpPostedFileBase DocPath, tbl_doc_driver_nodes DriverDocs)
        {
            ViewBag.DocTypeID = new SelectList(db.tbl_documents.Where(a => a.Status == true), "ID", "DocumentType", DriverDocs.DocTypeID);
            long DriverID = Convert.ToInt64(frm["did"]);
            DriverDocs.DriverID = DriverID;
            ViewBag.DriverID = DriverID;

            tbl_documents docs = db.tbl_documents.Where(d => d.Status == true && d.ID == DriverDocs.DocTypeID).FirstOrDefault();
            try
            {
                if (DocPath != null && DocPath.ContentLength > 0)
                {
                    DirectoryInfo thisFolder = new DirectoryInfo(Server.MapPath("../Content/Documents/Driver/"));
                    if (!thisFolder.Exists)
                    {
                        thisFolder.Create();
                    }
                    string ext = Path.GetExtension(DocPath.FileName);
                    string filename = Path.GetFileNameWithoutExtension(DocPath.FileName);
                    DriverDocs.DocPath = "DriverDoc" + "_" + docs.DocCode.ToUpper() + "_" + +DriverDocs.DriverID + ext;
                    var path = Path.Combine(Server.MapPath("../Content/Documents/Driver/"), DriverDocs.DocPath);
                    DocPath.SaveAs(path);
                }
                DriverDocs.Status = true;
                db.tbl_doc_driver_nodes.AddObject(DriverDocs);
                db.SaveChanges();
                core.LoggingEntries("Driver Mgmt.", "driver document has uploaded with the name " + DriverDocs.tbl_drivers.FirstName + " " + DriverDocs.tbl_drivers.LastName + "", User.Identity.Name);
                return Content("<script language='javascript' type='text/javascript'>var r=confirm('Are you want to add one more document?'); if(r==true) {window.location='/Driver/Documents';} if(r==false) {window.location='/Driver/Index';}</script>");

            }
            catch (Exception ex)
            {
                return View("Documents", DriverDocs);
            }
        }
        [HttpGet]
        public ActionResult LoadDocumentsListByDriver(long DriverID)
        {
            List<tbl_doc_driver_nodes> _lstdriverNodes = db.tbl_doc_driver_nodes.Where(v => v.Status == true && v.DriverID == DriverID).ToList();
            core.ConvertToUppercase<tbl_doc_driver_nodes>(_lstdriverNodes);
            ViewBag.DriverID = DriverID;
            if (_lstdriverNodes.Count > 0)
                return PartialView("_DocumentsListByDriver", _lstdriverNodes);
            else
                return PartialView("_NoDocuments");
        }
        //For send mail with multiple uploads
        [HttpPost]
        public ActionResult SendMail(FormCollection frm, long DriverID)
        {
            List<tbl_doc_driver_nodes> _lstDriverNodes = db.tbl_doc_driver_nodes.Where(v => v.Status == true && v.DriverID == DriverID).ToList();

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
                mail.From = new MailAddress(EmailAddress, "Driver Documents");
                mail.Subject = subject;
                //Multiple File Attachment
                int docCnt = 0;
                for (int i = 0; i < _lstDriverNodes.Count; i++)
                {
                    bool isChecked = frm.GetValues("chkdoc_" + _lstDriverNodes[i].ID).Contains("true"); //Getting all checkboxes values
                    long DocID = Convert.ToInt64(_lstDriverNodes[i].ID);
                    if (isChecked == true)
                    {
                        tbl_doc_driver_nodes _vdocs = db.tbl_doc_driver_nodes.Where(d => d.Status == true && d.ID == DocID).FirstOrDefault();
                        string Uplodefile = Request.PhysicalApplicationPath + "Content\\Documents\\Driver\\" + _vdocs.DocPath;
                        mail.Attachments.Add(new Attachment(Uplodefile));
                        docCnt += 1;
                    }
                }
                if (docCnt == 0)
                {
                    return Content("<script language='javascript' type='text/javascript'>alert('Please select atleast on document.');ViewDriverDocuments('" + DriverID + "'); $('#divEMailDet').show();</script>");
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
                return Content("<script language='javascript' type='text/javascript'>alert('Mail send successfully');ViewDriverDocuments('" + DriverID + "');</script>");

            }
            catch (Exception ex)
            {
                return Content("<script language='javascript' type='text/javascript'>alert('Delivery to the following recipient failed permanently'" + sendto + "'); ViewDriverDocuments('" + DriverID + "');</script>");
            }
        }
        [HttpGet]
        public ActionResult DeleteDriverDocuments(long ID)
        {
            tbl_doc_driver_nodes _cdocs = db.tbl_doc_driver_nodes.Where(a => a.Status == true && a.ID == ID).SingleOrDefault();
            long DriverID = (long)_cdocs.DriverID;
            try
            {
                _cdocs.Status = false;
                TryUpdateModel(_cdocs);
                db.SaveChanges();
                core.LoggingEntries("Driver Mgmt.", "driver document has deleted with the name " + _cdocs.tbl_drivers.FirstName + " " + _cdocs.tbl_drivers.LastName + "", User.Identity.Name);
                return Content("<script language='javascript' type='text/javascript'>alert('Document delete successfully');ViewDriverDocuments('" + DriverID + "');</script>");

            }
            catch (Exception ex)
            {
                return View();
            }

        }
        [HttpPost]
        public ActionResult AuditDocments(FormCollection frm, long DriverID)
        {
            List<tbl_doc_driver_nodes> _lstDriverNodes = db.tbl_doc_driver_nodes.Where(v => v.Status == true && v.DriverID == DriverID).ToList();
            int docCnt = 0;
            for (int i = 0; i < _lstDriverNodes.Count; i++)
            {
                bool isChecked = frm.GetValues("chkdoc_" + _lstDriverNodes[i].ID).Contains("true"); //Getting all checkboxes values
                long DocID = Convert.ToInt64(_lstDriverNodes[i].ID);
                if (isChecked == true)
                {
                    tbl_doc_driver_nodes _vdocs = db.tbl_doc_driver_nodes.Where(d => d.Status == true && d.ID == DocID).FirstOrDefault();
                    _vdocs.AuditedBy = User.Identity.Name.ToUpper();
                    _vdocs.Audited = true;
                    db.SaveChanges();
                    docCnt += 1;
                }
            }
            if (docCnt == 0)
            {
                return Content("<script language='javascript' type='text/javascript'>alert('Please select atleast one document.');ViewDriverDocuments('" + DriverID + "');</script>");
            }
            core.LoggingEntries("Driver Mgmt.", "driver document has audited with the name " + _lstDriverNodes.FirstOrDefault().tbl_drivers.FirstName + " " + _lstDriverNodes.FirstOrDefault().tbl_drivers.LastName + "", User.Identity.Name);
            return Content("<script language='javascript' type='text/javascript'>alert('Selected Documents are Audited successfully.');ViewDriverDocuments('" + DriverID + "');</script>");

        }
        #endregion

        #region Custom Methods and Validations

        public List<tbl_drivers> GetDriverList(long VehicleID)
        {
            List<tbl_drivers> driverList = db.tbl_drivers.Where(a => a.Status == true && a.CurrentVehicleID == VehicleID).ToList();
            return driverList;
        }

        public bool ValidateForm(tbl_drivers driver, bool IsEdit)
        {
            if (driver.FirstName == null || driver.FirstName.ToString().Trim().Length == 0)
                ModelState.AddModelError("FirstName", "Please enter firstname.");
            //if (driver.LastName == null || driver.LastName.ToString().Trim().Length == 0)
            //    ModelState.AddModelError("LastName", "Please enter lastname.");
            //if (driver.ContactNumber.ToString().Trim().Length == 0)
            //    ModelState.AddModelError("ContactNumber", "Please enter contact number.");
            //if (driver.ContactNumber.ToString().Trim().Length > 10)
            //    ModelState.AddModelError("ContactNumber", "Contact number should not be greater than 10 digits.");
            //if (driver.ContactNumber.ToString().Trim().Length < 10)
            //    ModelState.AddModelError("ContactNumber", "Contact number should not be less than 10 digits.");
            //if (driver.LicenceNumber == null || driver.LicenceNumber.ToString().Trim().Length == 0)
            //    ModelState.AddModelError("LicenceNumber", "Please enter Licence number.");
            //if (String.IsNullOrEmpty(driver.LicenceValidity.ToString()))
            //    ModelState.AddModelError("LicenceValidity", "Please enter Licence validity date.");
            //if (driver.BadgeNumber == null || driver.BadgeNumber.ToString().Trim().Length == 0)
            //    ModelState.AddModelError("BadgeNumber", "Please enter Badge number.");
            //if (String.IsNullOrEmpty(driver.BadgeValidity.ToString()))
            //    ModelState.AddModelError("BadgeValidity", "Please enter Badge validity date.");
            //if (driver.NameonLicence == null || driver.NameonLicence.ToString().Trim().Length == 0)
            //    ModelState.AddModelError("NameonLicence", "Please enter Name on licence.");
            //if (driver.VendorID == null || driver.VendorID.Value.ToString() == "")
                //ModelState.AddModelError("VendorID", "Please select Vendor");
            if (driver.CurrentVehicleID == null || driver.CurrentVehicleID.Value.ToString() == "0")
                ModelState.AddModelError("CurrentVehicleID", "Please select Vehicle");
            if (driver.CurrentVehicleID != null)
            {
                if (!IsEdit)
                {
                    if (!ValidDrivers((long)driver.CurrentVehicleID))
                        ModelState.AddModelError("Status", "Drivers exceeded for this vehicle");
                }
            }
            return ModelState.IsValid;
        }

        public bool ValidatePhoto(tbl_drivers driver, HttpPostedFileBase PhotoUrl, HttpPostedFileBase IDProof)
        {
            if (driver.PhotoUrl == null || driver.PhotoUrl.ToString().Trim().Length == 0)
                ModelState.AddModelError("PhotoUrl", "Please upload driver photo.");
            if (driver.IDProof == null || driver.IDProof.ToString().Trim().Length == 0)
                ModelState.AddModelError("IDProof", "Please upload driver ID Proof.");
            if (IsValid(PhotoUrl) == false)
                ModelState.AddModelError("PhotoUrl", "Only image files are accepted, please browse a image file");
            if (IsValid(IDProof) == false)
                ModelState.AddModelError("IDProof", "Only image files are accepted, please browse a image file");
            return ModelState.IsValid;
        }

        public bool ValidDrivers(long VehicleID)
        {
            tbl_vehicles vehicleDet = db.tbl_vehicles.Where(a => a.ID == VehicleID && a.Status == true).SingleOrDefault();
            List<tbl_drivers> driverDet = db.tbl_drivers.Where(a => a.CurrentVehicleID == VehicleID && a.Status == true).ToList();

            if (vehicleDet.SingleDoubleDriver.ToUpper().ToString() == "SINGLE")
            {
                if (driverDet == null)
                    return true;
                else if (driverDet.Count() == 1)
                    return false;
                else
                    return true;
            }
            if (vehicleDet.SingleDoubleDriver.ToUpper().ToString() == "DOUBLE")
            {
                if (driverDet == null)
                    return true;
                else if (driverDet.Count() == 2)
                    return false;
                else
                    return true;
            }

            return true;
        }

        public JsonResult ValidateDriver(long VehicleID)
        {
            tbl_vehicles vehicleDet = db.tbl_vehicles.Where(a => a.ID == VehicleID && a.Status == true).SingleOrDefault();
            List<tbl_drivers> driverDet = db.tbl_drivers.Where(a => a.CurrentVehicleID == VehicleID && a.Status == true).ToList();
            bool result = true;
            if (vehicleDet.SingleDoubleDriver.ToUpper().ToString() == "SINGLE")
            {
                if (driverDet == null)
                    result = true;
                else if (driverDet.Count() == 1)
                    result = false;
                else
                    result = true;
            }
            if (vehicleDet.SingleDoubleDriver.ToUpper().ToString() == "DOUBLE")
            {
                if (driverDet == null)
                    result = true;
                else if (driverDet.Count() == 2)
                    result = false;
                else
                    result = true;
            }
            return Json(new { success = true, HasDriver = result }, JsonRequestBehavior.AllowGet);
        }

        public bool ValidPhoto(HttpPostedFileBase Proof, int no)
        {
            if (no == 1)
            {
                if (IsValid(Proof) == false)
                    ModelState.AddModelError("PhotoUrl", "Only image files are accepted, please browse a image file");
            }
            else
            {
                if (IsValid(Proof) == false)
                    ModelState.AddModelError("IDProof", "Only image files are accepted, please browse a image file");
            }
            return ModelState.IsValid;
        }

        public bool IsComplete(tbl_drivers driver)
        {
            if (driver.LicenceValidity == null)
                return false;
            else if (driver.BadgeValidity == null)
                return false;
            else
                return true;
        }

        public bool IsValid(HttpPostedFileBase file)
        {
            bool isValid = false;
            //  var file = value as HttpPostedFileBase;
            if (file == null)
                return isValid;
            if (IsFileTypeValid(file))
            {
                isValid = true;
            }

            return isValid;
        }

        private bool IsFileTypeValid(HttpPostedFileBase file)
        {
            bool isValid = false;

            try
            {
                if (file == null)
                    return isValid;
                using (var img = Image.FromStream(file.InputStream))
                {
                    if (IsOneOfValidFormats(img.RawFormat))
                    {
                        isValid = true;
                    }
                }
            }
            catch
            {
                //Image is invalid
            }
            return isValid;
        }

        private bool IsOneOfValidFormats(ImageFormat rawFormat)
        {
            List<ImageFormat> formats = getValidFormats();

            foreach (ImageFormat format in formats)
            {
                if (rawFormat.Equals(format))
                {
                    return true;
                }
            }
            return false;
        }

        private List<ImageFormat> getValidFormats()
        {
            List<ImageFormat> formats = new List<ImageFormat>();
            formats.Add(ImageFormat.Png);
            formats.Add(ImageFormat.Jpeg);
            formats.Add(ImageFormat.Gif);
            //add types here
            return formats;
        }

        public string getAjaxResult(string f, string q)
        {
            f = f == "undefined" ? "FirstName" : f;
            StringBuilder str = new StringBuilder();
            var driver = from a in db.tbl_drivers
                                    .Where(a => a.Status.Value == true)
                                    .Where<tbl_drivers>(f, q, WhereOperation.Contains)
                         select new { a.FirstName, a.LastName, a.ID };


            foreach (var a in driver)
            {
                str.Append(string.Format("{0}  {1} |{2}\r\n", a.FirstName, a.LastName, a.ID)).ToString();
            }

            return str.ToString();
        }

        #endregion

        #region Driver Salary
       
        #region Index
        [HttpGet]
        public ActionResult DriverSalary()
        {
            return View();
        }
        public JsonResult GetDriversSalaryList(int iDisplayStart, int iDisplayLength, string sSearch, int iColumns,
     int iSortingCols, int iSortCol_0, string sSortDir_0,
     int sEcho, string mDataProp_Key)
        {
            var IList = GetDriverSalEnumerableList();
            var filteredLogSheet = IList
                .Select(l => new[]{
                    l.Id.ToString(),
                    l.Text1.ToString(),
                    l.Text2.ToString(),
                    l.Text3.ToString(),
                    l.Amount.ToString(),
                    l.Id1.ToString(),
                    l.Id2.ToString(),
                    l.Amount1.ToString(),
                    l.Amount2.ToString(),
                    l.Amount3.ToString(),
                    l.Amount4.ToString(),
                    l.Amount5.ToString(),
                    l.Amount6.ToString(),
                    l.Status1.ToString(),
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
        private IEnumerable<CommonFields> GetDriverSalEnumerableList()
        {
            var list = new List<CommonFields>();
            var tempList = (from l in db.tbl_driver_salaries
                            where l.Status == true
                            select l).ToList();
            foreach (tbl_driver_salaries ds in tempList)
            {
                list.Add(new CommonFields
                {
                    Id = ds.Id,
                    Text1 = (new System.Globalization.CultureInfo("en-GB").DateTimeFormat.MonthNames[ds.MonthYear.Value.Month - 1]) + " " + ds.MonthYear.Value.Year,
                    Text2 = ds.VehicleId == null ? "" : ds.tbl_vehicles.VehicleRegNum,
                    Text3 = ds.DriverName == null ? "" : ds.DriverName,
                    Amount = ds.SalaryAmt == null ? 0 : (decimal)ds.SalaryAmt,
                    Id1 = ds.WorkDays == null ? 0 : (int)ds.WorkDays,
                    Id2 = ds.PresentDays == null ? 0 : (int)ds.PresentDays,
                    Amount1 = ds.NetSalary == null ? 0 : (decimal)ds.NetSalary,
                    Amount2 = ds.Incentive == null ? 0 : (decimal)ds.Incentive,
                    Amount3 = ds.AdvAmt == null ? 0 : (decimal)ds.AdvAmt,
                    Amount4 = ds.Deductions == null ? 0 : (decimal)ds.Deductions,
                    Amount5 = ds.TotalAmt == null ? 0 : (decimal)ds.TotalAmt,
                    Amount6 = ds.AdvAmt ?? 0 + ds.Deductions ?? 0 + ds.TotalAmt ?? 0,
                    Status1 = (bool)ds.Approve,
                    Status = (bool)ds.Status
                });
            }
            core.ConvertToUppercase<CommonFields>(list);
            return list.OrderByDescending(a => a.Id);
        }
        #endregion

        #region Approve Driver Salary
        [HttpGet]
        public ActionResult ApproveDriverSalary(long Id)
        {
            try
            {
                tbl_driver_salaries driverSal = db.tbl_driver_salaries.Where(a => a.Id == Id).SingleOrDefault();
                driverSal.Approve = true;
                TryUpdateModel(driverSal);
                db.SaveChanges();
                return RedirectToAction("DriverSalary");
            }
            catch (Exception ex)
            {
                core.ExceptionLogging(ex.Message.ToString(), User.Identity.Name, ex.StackTrace.ToString());
                return View("Error");
            }
        }
        #endregion

        #region Add Driver Salary
        [HttpGet]
        public ActionResult AddDriverSalary()
        {
            return View();
        }
        [HttpPost]
        public ActionResult AddDriverSalary([Bind(Exclude = "Id")]FormCollection frm, tbl_driver_salaries driverSal)
        {
            try
            {
                driverSal.CreatedDt = DateTime.Now;
                driverSal.CreatedBy = User.Identity.Name;
                driverSal.Approve = false;
                driverSal.Status = true;
                driverSal.ClosedFlag = false;
                db.tbl_driver_salaries.AddObject(driverSal);
                db.SaveChanges();
                core.LoggingEntries("Driver Salary", "Driver salary has added to the driver " + driverSal.DriverName, User.Identity.Name);
                return Json(new { success = true, msg = "Driver salary has been added succesfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, msg = "An error occured , the error is " + ex.Message.ToString() });
            }
        } 
        #endregion

        #region Edit Driver Salary
        [HttpGet]
        public ActionResult EditDriverSalary(long Id)
        {
            tbl_driver_salaries driverSal = db.tbl_driver_salaries.Where(a => a.Id == Id).SingleOrDefault();
            return View(driverSal);
        }
        [HttpPost]
        public ActionResult EditDriverSalary(long Id, FormCollection frm, tbl_driver_salaries driverSal)
        {
            try
            {
                bool IsApprove = false;
                tbl_driver_salaries DriverSal = db.tbl_driver_salaries.Where(a => a.Id == Id).SingleOrDefault();
                DriverSal.ModifiedBy = User.Identity.Name;
                DriverSal.ModifiedDt = DateTime.Now;
                IsApprove = frm["Approve"].Contains("true") ? true : false;
                DriverSal.Approve = IsApprove;
                TryUpdateModel(DriverSal);
                db.SaveChanges();
                core.LoggingEntries("Driver Salary", "Driver salary has updated to the driver " + DriverSal.DriverName, User.Identity.Name);
                return Json(new { success = true, msg = "Driver salary has been updated succesfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, msg = "An error occured , the error is " + ex.Message.ToString() });
            }
        } 
        #endregion

        #region Delete Driver Salary
        [HttpGet]
        public ActionResult DeleteDriverSalary(long Id)
        {
            tbl_driver_salaries DriverSal = db.tbl_driver_salaries.Where(a => a.Id == Id).SingleOrDefault();
            try
            {
                TryUpdateModel(DriverSal);
                DriverSal.Status = false;
                db.SaveChanges();
                core.LoggingEntries("Driver Salary", "Driver salary has deleted to the driver " + DriverSal.DriverName, User.Identity.Name);
                return RedirectToAction("DriverSalary");
            }
            catch (Exception)
            {
                throw;
            }
        } 
        #endregion

        [HttpGet]
        public bool VerifyDriverSalary(string MonthYear, long VehicleId)
        {
            DateTime MnYear = Convert.ToDateTime(MonthYear);
            return db.tbl_driver_salaries.Where(a => a.Status == true && a.VehicleId == VehicleId && a.MonthYear.Value.Year == MnYear.Year && a.MonthYear.Value.Month == MnYear.Month && a.ClosedFlag == false).Any();
        }

        public bool VerifyDriverSalary(DateTime MonthYear, string Driver, long VehicleId)
        {
            return db.tbl_driver_salaries.Where(a => a.Status == true && a.DriverName.Trim() == Driver.Trim() && a.VehicleId == VehicleId && a.MonthYear.Value.Year == MonthYear.Year && a.MonthYear.Value.Month == MonthYear.Month).Any();
        }

        [HttpGet]
        public JsonResult GetDriverDeductionsByVehicle(DateTime MonthYear, long VehicleId)
        {
            return Json(new { success = true, AdvAmt = core.GetAdvanceAmountByVehicleId(MonthYear, VehicleId), DedAmt = core.GetTotalDeductionsByVehicleId(MonthYear, VehicleId) }, JsonRequestBehavior.AllowGet);
        }
        #endregion
    }
}
