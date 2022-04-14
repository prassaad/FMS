using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;
using System.Drawing;
using System.IO;
using System.Data;
using AsyncUploaderDemo.Models;
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
    public class VendorController : Controller
    {
        #region ctors
        private FMSDBEntities db;
        private IFileStore _fileStore = new DiskFileStore();
        private core core = new core();
        public string AsyncUpload()
        {
            return _fileStore.SaveUploadedFile(Request.Files[0]);
        }
        public VendorController()
        {
            db = new FMSDBEntities();
        } 
        #endregion

        #region List of vendors
        /// <summary>
        /// vendors list action
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public ActionResult Index()
        {
            return View();
        }
        public JsonResult GetVendors(int iDisplayStart, int iDisplayLength, string sSearch, int iColumns,
      int iSortingCols, int iSortCol_0, string sSortDir_0,
      int sEcho, string mDataProp_Key)
        {
            var vendorsList = GetVendorsList();
            var filteredvendors = vendorsList
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
                    l.Status.ToString()
                    
                }).Where(i => string.IsNullOrEmpty(sSearch)
                     || i.Any(y => y.IndexOf(sSearch, StringComparison.InvariantCultureIgnoreCase) >= 0));
            var orderedlist = filteredvendors
                   .OrderByWithDirection(i => (i[iSortCol_0]).Parse(), sSortDir_0 == "asc")
                   .Skip(iDisplayStart)
                   .Take(iDisplayLength);
            var model = new
            {
                aaData = orderedlist,
                iTotalDisplayRecords = filteredvendors.Count(),
                iTotalRecords = filteredvendors.Count(),
                sEcho = sEcho.ToString()
            };
            return Json(model, JsonRequestBehavior.AllowGet);
        }
        private IEnumerable<CommonFields> GetVendorsList()
        {
            var list = new List<CommonFields>();
            var vendorList = db.tbl_vendor_details.Where(a => a.Status == true && (a.SubVendor == false || a.SubVendor == null)).ToList();
            foreach (tbl_vendor_details ls in vendorList)
            {
                list.Add(new CommonFields
                {
                    Id = ls.ID,
                    Text1 = ls.FirstName + "" + ls.LastName,
                    Text2 = ls.ContactNumber.ToString() == null ? "" : ls.ContactNumber.ToString(),
                    Text3 = ls.PanCardNumber == null ? "" : ls.PanCardNumber,
                    Text4 = ls.AccountHolderName == null ? "" : ls.AccountHolderName,
                    Text5 = ls.AccountNumber == null ? "" : ls.AccountNumber,
                    Text6 = ls.AccountType == null ? "" : ls.AccountType,
                    Text7 = ls.BankName == null ? "" : ls.BankName,
                    Text8 = ls.Branch == null ? "" : ls.Branch,
                    Text9 = ls.IFSCCode == null ? "" : ls.IFSCCode,
                    Status = (bool)ls.Status
                });
            }
            core.ConvertToUppercase<CommonFields>(list);
            return list.OrderBy(a => a.Id);
        } 
        #endregion

        #region Add vendor
        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Create([Bind(Exclude = "ID")]FormCollection frm, tbl_vendor_details vd, HttpPostedFileBase PhotoUrl)
        {
            core.ConvertToUppercase(vd);
            try
            {
                if (ValidateForm(vd))
                {
                    if (PhotoUrl != null && PhotoUrl.ContentLength > 0)
                    {
                        vd.PhotoUrl = vd.FirstName + "_" + vd.ID;
                        var path = Path.Combine(Server.MapPath("../Content/uploadimages/"), vd.PhotoUrl);
                        PhotoUrl.SaveAs(path);
                    }
                    vd.Status = true;
                    vd.SubVendor = false;
                    db.tbl_vendor_details.AddObject(vd);
                    db.SaveChanges();
                    core.LoggingEntries("Vendor Mgmt.", "Vendor has created with the name " + vd.FirstName + " " + vd.LastName, User.Identity.Name);
                    return RedirectToAction("Index");
                }
                return View();
            }
            catch (Exception)
            {
                return View();
            }

        }

        // Add New vendor details partially through vehicle management 
        [HttpGet]
        public ActionResult AddNewVendorDetails(bool IsSubVendor)
        {
            ViewBag.IsSubVendor = IsSubVendor;
            return PartialView("_AddNewVendorDetails");
        }
        [HttpPost]
        public ActionResult AddNewVendorDetails([Bind(Exclude = "ID")]FormCollection frm, tbl_vendor_details vd, HttpPostedFileBase PhotoPath)
        {
            bool IsSubVendor = (frm["SubVendor"] == null) ? false : Convert.ToBoolean(frm["SubVendor"]);
            ViewBag.IsSubVendor = IsSubVendor;
            core.ConvertToUppercase(vd);
            try
            {
                if (ValidateForm(vd))
                {
                    if (PhotoPath != null && PhotoPath.ContentLength > 0)
                    {
                        vd.PhotoUrl = vd.FirstName + "_" + vd.ID;
                        var path = Path.Combine(Server.MapPath("../Content/uploadimages/"), vd.PhotoUrl);
                        PhotoPath.SaveAs(path);
                    }
                    vd.Status = true;
                    vd.SubVendor = IsSubVendor;
                    db.tbl_vendor_details.AddObject(vd);
                    db.SaveChanges();
                    core.LoggingEntries("Vendor Mgmt.", "Vendor has created with the name " + vd.FirstName + " " + vd.LastName, User.Identity.Name);
                    return Content("<script language='javascript' type='text/javascript'>$.modal.close();alert('Vendor added successfully');$('#VID').val('" + vd.ID + "');LoadVendors('" + IsSubVendor + "');</script>");
                }
                return PartialView("_AddNewVendorDetails");
            }
            catch (Exception Ex)
            {
                return PartialView("_AddNewVendorDetails");
            }
        }
        #endregion

        #region Edit Vendor
        [HttpGet]
        public ActionResult Edit(long Id)
        {
            tbl_vendor_details vendorDet = db.tbl_vendor_details.Where(a => a.Status == true && a.ID == Id).SingleOrDefault();
            return View(vendorDet);
        }
        [HttpPost]
        public ActionResult Edit(long Id, FormCollection frm, tbl_vendor_details vd, HttpPostedFileBase Photo)
        {
            tbl_vendor_details vendorDet = db.tbl_vendor_details.Where(a => a.Status == true && a.ID == Id).SingleOrDefault();

            try
            {
                if (ValidateForm(vd))
                {
                    if (Photo != null && Photo.ContentLength > 0)
                    {
                        vendorDet.PhotoUrl = vendorDet.FirstName + "_" + Id + ".jpg";
                        var path = Path.Combine(Server.MapPath("../../Content/uploadimages/"), vendorDet.PhotoUrl);
                        Photo.SaveAs(path);
                    }
                    vendorDet.Status = true;
                    vendorDet.SubVendor = false;
                    TryUpdateModel<tbl_vendor_details>(vendorDet);
                    core.ConvertToUppercase(vendorDet);
                    db.SaveChanges();
                    core.LoggingEntries("Vendor Mgmt.", "Vendor has edited with the name " + vd.FirstName + " " + vd.LastName, User.Identity.Name);
                    return RedirectToAction("Index");
                }
            }
            catch (Exception)
            {
                return View(vendorDet);
            }
            return View(vendorDet);
        }
        [HttpGet]
        public ActionResult EditVendorDetails(long VendorID)
        {
            tbl_vendor_details vendorDet = db.tbl_vendor_details.Where(a => a.ID == VendorID).SingleOrDefault();
            return PartialView("_EditVendorDetails", vendorDet);
        }
        [HttpPost]
        public ActionResult EditVendorDetails(HttpPostedFileBase Photo, FormCollection frm, tbl_vendor_details vd)
        {
            long Id = frm["VendorID"] == null ? 0 : Convert.ToInt64(frm["VendorID"]);
            tbl_vendor_details vendorDet = db.tbl_vendor_details.Where(a => a.Status == true && a.ID == Id).SingleOrDefault();
            Photo = Request.Files["Photo"];

            try
            {
                if (ValidateForm(vd))
                {
                    if (Photo != null && Photo.ContentLength > 0)
                    {
                        vendorDet.PhotoUrl = vendorDet.FirstName + "_" + Id + ".jpg";
                        var path = Path.Combine(Server.MapPath("../Content/uploadimages/"), vendorDet.PhotoUrl);
                        Photo.SaveAs(path);
                    }
                    vendorDet.FixedDeposit = vd.FixedDeposit;
                    vendorDet.Status = true;
                    vendorDet.SubVendor = false;
                    TryUpdateModel(vendorDet);
                    core.ConvertToUppercase(vendorDet);
                    db.SaveChanges();
                    core.LoggingEntries("Vendor Mgmt.", "Vendor has Edited with the name " + vd.FirstName + " " + vd.LastName, User.Identity.Name);
                    return Content("<script language='javascript' type='text/javascript'>alert('Updated owner details successfully');EditOwnerDetails('" + vendorDet.ID + "')</script>");
                }
            }
            catch (Exception)
            {
                return PartialView("_EditVendorDetails", vendorDet);
            }
            return PartialView("_EditVendorDetails", vendorDet);
        }
        #endregion

        #region Delete Vendor
        [HttpGet]
        public ActionResult Delete(long Id)
        {
            tbl_vendor_details vendorDet = db.tbl_vendor_details.Where(a => a.Status == true && a.ID == Id).SingleOrDefault();
            return View(vendorDet);
        }
        [HttpPost]
        public ActionResult Delete(long Id, FormCollection frm, tbl_vendor_details vd)
        {
            tbl_vendor_details vendorDet = db.tbl_vendor_details.Where(a => a.Status == true && a.ID == Id).SingleOrDefault();
            try
            {
                vendorDet.Status = false;
                TryUpdateModel(vendorDet);
                db.SaveChanges();
                // Verify whether vendor has vehicles or not.
                //core objCore = new core();
                //List<tbl_vehicles> _vehicleList = objCore.GetVehiclesList((long)vendorDet.ID);
                //if (_vehicleList != null)
                //{
                //    foreach (tbl_vehicles veh in _vehicleList)
                //    {
                //        veh.Status = false;
                //        TryUpdateModel(veh);
                //    }
                //    db.SaveChanges();
                //}
                //// Verify Whether vendor has drivers for his vehicles or not
                //List<tbl_drivers> _driverList = objCore.GetDriversList((long)vendorDet.ID);
                //if (_driverList != null)
                //{
                //    foreach (tbl_drivers driver in _driverList)
                //    {
                //        driver.Status = false;
                //        TryUpdateModel(driver);
                //        // Verify vehicle driver log 
                //        tbl_vehicle_driver vehDriver = db.tbl_vehicle_driver.Where(a => a.DriverID == driver.ID && a.Status == true).SingleOrDefault();
                //        db.tbl_vehicle_driver.DeleteObject(vehDriver);
                //    }
                //    db.SaveChanges();
                //}
                core.LoggingEntries("Vendor Mgmt.", "Vendor has deleted with the name " + vd.FirstName + " " + vd.LastName, User.Identity.Name);
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                return View();
            }
        } 
        #endregion

        #region View Vendor Details
        public ActionResult Details(long Id)
        {
            tbl_vendor_details vendorDet = db.tbl_vendor_details.Where(a => a.ID == Id && a.Status == true).SingleOrDefault();
            return PartialView("_Details", vendorDet);
        }
        // View Vendor Details
        [HttpGet]
        public ActionResult VendorDetails(long ID)
        {
            tbl_vendor_details vendorDet = db.tbl_vendor_details.Where(a => a.ID == ID && a.Status == true).SingleOrDefault();
            return View(vendorDet);
        } 
        #endregion

        #region Vendor Documents

        [HttpGet]
        public ActionResult Documents()
        {
            ViewBag.DocTypeID = new SelectList(db.tbl_documents.Where(a => a.Status == true), "ID", "DocumentType");
            return View();
        }
        [HttpPost]
        public ActionResult Documents(FormCollection frm, HttpPostedFileBase DocPath, tbl_doc_vendor_nodes VendorDocs)
        {
            ViewBag.DocTypeID = new SelectList(db.tbl_documents.Where(a => a.Status == true), "ID", "DocumentType", VendorDocs.DocTypeID);
            long VendorID = Convert.ToInt64(frm["vid"]);
            VendorDocs.VendorID = VendorID;
            ViewBag.VendorID = VendorID;

            tbl_documents docs = db.tbl_documents.Where(d => d.Status == true && d.ID == VendorDocs.DocTypeID).FirstOrDefault();
            try
            {
                if (DocPath != null && DocPath.ContentLength > 0)
                {
                    DirectoryInfo thisFolder = new DirectoryInfo(Server.MapPath("../Content/Documents/Vendor/"));
                    if (!thisFolder.Exists)
                    {
                        thisFolder.Create();
                    }
                    string ext = Path.GetExtension(DocPath.FileName);
                    string filename = Path.GetFileNameWithoutExtension(DocPath.FileName);
                    VendorDocs.DocPath = "VendorDoc" + "_" + docs.DocCode + "_" + +VendorDocs.VendorID + ext;
                    var path = Path.Combine(Server.MapPath("../Content/Documents/Vendor/"), VendorDocs.DocPath);
                    DocPath.SaveAs(path);
                }
                VendorDocs.Status = true;
                db.tbl_doc_vendor_nodes.AddObject(VendorDocs);
                db.SaveChanges();
                core.LoggingEntries("Vendor Mgmt.", "Added Vendor documents with the name " + VendorDocs.tbl_vendor_details.FirstName + " " + VendorDocs.tbl_vendor_details.LastName, User.Identity.Name);
                return Content("<script language='javascript' type='text/javascript'>var r=confirm('Are you want to add one more document?'); if(r==true) {window.location='/Vendor/Documents';} if(r==false) {window.location='/Vendor/Index';}</script>");

            }
            catch (Exception ex)
            {
                return View("Documents", VendorDocs);
            }
        }
        [HttpGet]
        public ActionResult LoadDocumentsListByVendor(long VendorID)
        {
            List<tbl_doc_vendor_nodes> _lsVendorNodes = db.tbl_doc_vendor_nodes.Where(v => v.Status == true && v.VendorID == VendorID).ToList();
            core.ConvertToUppercase<tbl_doc_vendor_nodes>(_lsVendorNodes);
            ViewBag.VendorID = VendorID;
            if (_lsVendorNodes.Count > 0)
                return PartialView("_DocumentsListByVendor", _lsVendorNodes);
            else
                return PartialView("_NoDocuments");
        }
        //For send mail with multiple uploads
        [HttpPost]
        public ActionResult SendMail(FormCollection frm, long VendorID)
        {
            List<tbl_doc_vendor_nodes> _lstVendorNodes = db.tbl_doc_vendor_nodes.Where(v => v.Status == true && v.VendorID == VendorID).ToList();

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
                for (int i = 0; i < _lstVendorNodes.Count; i++)
                {
                    bool isChecked = frm.GetValues("chkdoc_" + _lstVendorNodes[i].ID).Contains("true"); //Getting all checkboxes values
                    long DocID = Convert.ToInt64(_lstVendorNodes[i].ID);
                    if (isChecked == true)
                    {
                        tbl_doc_vendor_nodes _vdocs = db.tbl_doc_vendor_nodes.Where(d => d.Status == true && d.ID == DocID).FirstOrDefault();
                        string Uplodefile = Request.PhysicalApplicationPath + "Content\\Documents\\Vendor\\" + _vdocs.DocPath;
                        mail.Attachments.Add(new Attachment(Uplodefile));
                        docCnt += 1;
                    }
                }
                if (docCnt == 0)
                {
                    return Content("<script language='javascript' type='text/javascript'>alert('Please select atleast on document.');ViewVendorDocuments('" + VendorID + "'); $('#divEMailDet').show();</script>");
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
                return Content("<script language='javascript' type='text/javascript'>alert('Mail send successfully');ViewVendorDocuments('" + VendorID + "');</script>");

            }
            catch (Exception ex)
            {
                return Content("<script language='javascript' type='text/javascript'>alert('Delivery to the following recipient failed permanently'" + sendto + "'); ViewVendorDocuments('" + VendorID + "');</script>");
            }
        }
        [HttpGet]
        public ActionResult DeleteVendorDocuments(long ID)
        {
            tbl_doc_vendor_nodes _vdocs = db.tbl_doc_vendor_nodes.Where(a => a.Status == true && a.ID == ID).SingleOrDefault();
            long VendorID = (long)_vdocs.VendorID;
            try
            {
                _vdocs.Status = false;
                TryUpdateModel(_vdocs);
                db.SaveChanges();
                core.LoggingEntries("Vendor Mgmt.", "Deleted Vendor documents with the name " + _vdocs.tbl_vendor_details.FirstName + " " + _vdocs.tbl_vendor_details.LastName, User.Identity.Name);
                return Content("<script language='javascript' type='text/javascript'>alert('Document delete successfully');ViewVendorDocuments('" + VendorID + "'); </script>");

            }
            catch (Exception ex)
            {
                return View();
            }

        }
        [HttpPost]
        public ActionResult AuditDocments(FormCollection frm, long VendorID)
        {
            List<tbl_doc_vendor_nodes> _lstVendorNodes = db.tbl_doc_vendor_nodes.Where(v => v.Status == true && v.VendorID == VendorID).ToList();
            int docCnt = 0;
            for (int i = 0; i < _lstVendorNodes.Count; i++)
            {
                bool isChecked = frm.GetValues("chkdoc_" + _lstVendorNodes[i].ID).Contains("true"); //Getting all checkboxes values
                long DocID = Convert.ToInt64(_lstVendorNodes[i].ID);
                if (isChecked == true)
                {
                    tbl_doc_vendor_nodes _vdocs = db.tbl_doc_vendor_nodes.Where(d => d.Status == true && d.ID == DocID).FirstOrDefault();
                    _vdocs.AuditedBy = User.Identity.Name.ToUpper();
                    _vdocs.Audited = true;
                    db.SaveChanges();
                    docCnt += 1;
                }
            }
            if (docCnt == 0)
            {
                return Content("<script language='javascript' type='text/javascript'>alert('Please select atleast one document.');ViewVendorDocuments('" + VendorID + "');</script>");
            }
            core.LoggingEntries("Vendor Mgmt.", "Vendor documents has audited", User.Identity.Name);
            return Content("<script language='javascript' type='text/javascript'>alert('Selected Documents are Audited successfully.');ViewVendorDocuments('" + VendorID + "');</script>");

        }
        #endregion

        #region Custom Methods
        [HttpGet]
        public JsonResult LoadVendors(bool IsSubVendor)
        {
            if (!IsSubVendor)
            {
                var vendorList = db.tbl_vendor_details.Where(a => a.Status == true && (a.SubVendor == null || a.SubVendor == false)).Select(a => new { a.FirstName, a.LastName, a.ID }).ToList();
                return Json(vendorList, JsonRequestBehavior.AllowGet);
            }
            else
            {
                var vendorList = db.tbl_vendor_details.Where(a => a.Status == true && a.SubVendor == true).Select(a => new { a.FirstName, a.LastName, a.ID }).ToList();
                return Json(vendorList, JsonRequestBehavior.AllowGet);
            }
        }

        public bool ValidateForm(tbl_vendor_details vd)
        {
            if (vd.FirstName == null || vd.FirstName.ToString().Trim().Length == 0)
                ModelState.AddModelError("FirstName", "Please enter firstname.");
            //if (vd.LastName == null || vd.LastName.ToString().Trim().Length == 0)
            //    ModelState.AddModelError("LastName", "Please enter lastname.");
            //if (vd.ContactNumber.ToString().Trim().Length == 0)
            //    ModelState.AddModelError("ContactNumber", "Please enter contact number.");
            //if (vd.ContactNumber.ToString().Trim().Length > 10)
            //    ModelState.AddModelError("ContactNumber", "Contact number should not be greater than 10 digits.");
            //if (vd.ContactNumber.ToString().Trim().Length < 10)
            //    ModelState.AddModelError("ContactNumber", "Contact number should not be less than 10 digits.");
            //if (vd.PanCardNumber == null || vd.PanCardNumber.ToString().Trim().Length == 0)
            //    ModelState.AddModelError("PanCardNumber", "Please enter PAN card number.");
            return ModelState.IsValid;
        }
        public string getAjaxResult(string f, string q)
        {
            f = f == "undefined" ? "FirstName" : f;
            StringBuilder str = new StringBuilder();
            var vendor = from a in db.tbl_vendor_details
                                    .Where(a => a.Status.Value == true)
                                    .Where<tbl_vendor_details>(f, q, WhereOperation.Contains)
                         select new { a.FirstName, a.LastName, a.ID };


            foreach (var a in vendor)
            {
                str.Append(string.Format("{0}  {1} |{2}\r\n", a.FirstName.ToUpper(), a.LastName.ToUpper(), a.ID)).ToString();
            }

            return str.ToString();
        } 
        #endregion
    }
}
