using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;
using System.Net;
using System.Net.Mail;
using System.Web.Configuration;

namespace FMS.Controllers
{
    [CustomAuthorizationFilter]
    public class DocumentTypeController : Controller
    {
        #region ctors
        private FMSDBEntities db;
        private core core = new core();
        public DocumentTypeController()
        {
            db = new FMSDBEntities();
        } 
        #endregion

        #region Index Method
        public ViewResult Index()
        {
            List<tbl_documents> _lstDocs = db.tbl_documents.Where(a => a.Status == true).ToList();
            core.ConvertToUppercase<tbl_documents>(_lstDocs);
            return View(_lstDocs);
        } 
        #endregion

        #region View Document Type
        public ViewResult Details(long id)
        {
            tbl_documents tbl_documents = db.tbl_documents.Single(t => t.ID == id);
            return View(tbl_documents);
        } 
        #endregion

        #region Add Document Type
        public ActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Create(tbl_documents tbl_documents)
        {
            try
            {
                if (ValidateForm(tbl_documents))
                {
                    core.ConvertToUppercase(tbl_documents);
                    tbl_documents.Status = true;
                    db.tbl_documents.AddObject(tbl_documents);
                    db.SaveChanges();
                    core.LoggingEntries("Masters", "Document type has created.", User.Identity.Name);
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                return View(tbl_documents);
            }
            return View(tbl_documents);
        }
        
        #endregion

        #region Edit Document Type
        [HttpGet]
        public ActionResult Edit(long id)
        {
            tbl_documents tbl_documents = db.tbl_documents.Single(t => t.ID == id);
            return View(tbl_documents);
        }
        [HttpPost]
        public ActionResult Edit(tbl_documents tbl_documents)
        {
            try
            {
                if (ValidateForm(tbl_documents))
                {
                    core.ConvertToUppercase(tbl_documents);
                    tbl_documents.Status = true;
                    db.tbl_documents.Attach(tbl_documents);
                    db.ObjectStateManager.ChangeObjectState(tbl_documents, EntityState.Modified);
                    db.SaveChanges();
                    core.LoggingEntries("Masters", "Document type has updated.", User.Identity.Name);
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                return View(tbl_documents);
            }
            return View(tbl_documents);
        } 
        #endregion

        #region Delete
        public ActionResult Delete(long id)
        {
            tbl_documents tbl_documents = db.tbl_documents.Single(t => t.ID == id);
            return View(tbl_documents);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(long id)
        {
            try
            {
                tbl_documents tbl_documents = db.tbl_documents.Single(t => t.ID == id);
                tbl_documents.Status = false;
                TryUpdateModel(tbl_documents);
                db.SaveChanges();
                core.LoggingEntries("Masters", "Document type has deleted.", User.Identity.Name);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return View();
            }
        } 
        #endregion

        #region Validation Methods
        public bool ValidateForm(tbl_documents docs)
        {
            if (docs.DocumentType == null || docs.DocumentType.ToString().Trim().Length == 0)
                ModelState.AddModelError("DocumentType", "Please enter document type.");
            if (docs.DocCode == null || docs.DocCode.ToString().Trim().Length == 0)
                ModelState.AddModelError("DocCode", "Please enter document code.");
            return ModelState.IsValid;
        }

        
        #endregion

        #region DocumentSearch

        [HttpGet]
        public ActionResult DocumentSearch()
        {
            return View();
        }

        [HttpPost]
        public ActionResult DocumentSearch(FormCollection frm)
        {
            long VehicleID = frm["vid"] == null ? 0 : Convert.ToInt64(frm["vid"]);
            List<tbl_doc_vehicle_nodes> _vehicleDocList = db.tbl_doc_vehicle_nodes.Where(a => a.Status == true && a.VehicleID == VehicleID).ToList();
            List<tbl_doc_driver_nodes> _driverDocList = db.tbl_doc_driver_nodes.Where(a => a.tbl_drivers.CurrentVehicleID == VehicleID && a.Status == true).ToList();
            List<tbl_doc_vendor_nodes> _vendorDocList = (from m in db.tbl_doc_vendor_nodes join d in db.tbl_vehicles on m.VendorID equals d.VendorID where d.Status == true && m.Status == true && d.ID == VehicleID select m).ToList();
            ViewBag.VehicleID = VehicleID;
            if (_vehicleDocList == null && _driverDocList == null && _vendorDocList == null)
                return PartialView("_NoDocuments");
            if (_vehicleDocList.Count() == 0 && _driverDocList.Count() == 0 && _vendorDocList.Count() == 0)
                return PartialView("_NoDocuments");
            return PartialView("_DocumentsList", new DocumentManagement(null, _driverDocList, _vehicleDocList, _vendorDocList));
        }

        [HttpPost]
        public ActionResult SendMail(FormCollection frm, long VehicleID)
        {
            List<tbl_doc_vehicle_nodes> _lstvehNodes = db.tbl_doc_vehicle_nodes.Where(v => v.Status == true && v.VehicleID == VehicleID).ToList();
            List<tbl_doc_driver_nodes> _lstdriverNodes = db.tbl_doc_driver_nodes.Where(a => a.tbl_drivers.CurrentVehicleID == VehicleID && a.Status == true).ToList();
            List<tbl_doc_vendor_nodes> _lstvendorNodes = (from m in db.tbl_doc_vendor_nodes join d in db.tbl_vehicles on m.VendorID equals d.VendorID where d.Status == true && m.Status == true && d.ID == VehicleID select m).ToList();
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
                mail.From = new MailAddress(EmailAddress, "Documents");
                mail.Subject = subject;
                //Multiple File Attachment
                int docCnt = 0;
                // Vehicle Documents
                for (int i = 0; i < _lstvehNodes.Count; i++)
                {
                    bool isChecked = frm.GetValues("chkdoc_vehicle" + _lstvehNodes[i].ID).Contains("true"); //Getting all checkboxes values
                    long DocID = Convert.ToInt64(_lstvehNodes[i].ID);
                    if (isChecked == true)
                    {
                        tbl_doc_vehicle_nodes _vdocs = db.tbl_doc_vehicle_nodes.Where(d => d.Status == true && d.ID == DocID).FirstOrDefault();
                        string Uplodefile = Request.PhysicalApplicationPath + "Content\\Documents\\Vehicle\\" + _vdocs.DocPath;
                        mail.Attachments.Add(new Attachment(Uplodefile));
                        docCnt += 1;
                    }
                }
                // Driver Documents
                for (int i = 0; i < _lstdriverNodes.Count; i++)
                {
                    bool isChecked = frm.GetValues("chkdoc_driver" + _lstdriverNodes[i].ID).Contains("true"); //Getting all checkboxes values
                    long DocID = Convert.ToInt64(_lstdriverNodes[i].ID);
                    if (isChecked == true)
                    {
                        tbl_doc_driver_nodes _ddocs = db.tbl_doc_driver_nodes.Where(a => a.Status == true && a.ID == DocID).FirstOrDefault();
                        string Uplodefile = Request.PhysicalApplicationPath + "Content\\Documents\\Driver\\" + _ddocs.DocPath;
                        mail.Attachments.Add(new Attachment(Uplodefile));
                        docCnt += 1;
                    }
                }
                // Vendor Documents
                for (int i = 0; i < _lstvendorNodes.Count; i++)
                {
                    bool isChecked = frm.GetValues("chkdoc_vendor" + _lstvendorNodes[i].ID).Contains("true"); //Getting all checkboxes values
                    long DocID = Convert.ToInt64(_lstvendorNodes[i].ID);
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
                    return Content("<script language='javascript' type='text/javascript'>alert('Please select atleast on document.');$('#divEMailDet').show();</script>");
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
                //smtp.SendCompleted += new SendCompletedEventHandler(SendCompletedCallback);
                smtp.Send(mail);
                return Content("<script language='javascript' type='text/javascript'>alert('Mail send successfully');$('#VehicleID').val('');$('#VehicleID').focus();</script>");

            }
            catch (Exception)
            {
                return Content("<script language='javascript' type='text/javascript'>alert('Delivery to the following recipient failed permanently " + sendto + ");</script>");
            }
        }
        [HttpPost]
        public ActionResult AuditDocments(FormCollection frm, long VehicleID)
        {
            List<tbl_doc_vehicle_nodes> _lstvehNodes = db.tbl_doc_vehicle_nodes.Where(v => v.Status == true && v.VehicleID == VehicleID).ToList();
            List<tbl_doc_driver_nodes> _lstdriverNodes = db.tbl_doc_driver_nodes.Where(a => a.tbl_drivers.CurrentVehicleID == VehicleID && a.Status == true).ToList();
            List<tbl_doc_vendor_nodes> _lstvendorNodes = (from m in db.tbl_doc_vendor_nodes join d in db.tbl_vehicles on m.VendorID equals d.VendorID where d.Status == true && m.Status == true && d.ID == VehicleID select m).ToList();
            try
            {
                int docCnt = 0;
                for (int i = 0; i < _lstvehNodes.Count; i++)
                {
                    bool isChecked = frm.GetValues("chkdoc_vehicle" + _lstvehNodes[i].ID).Contains("true"); //Getting all checkboxes values by vehicle
                    long DocID = Convert.ToInt64(_lstvehNodes[i].ID);
                    if (isChecked == true)
                    {
                        tbl_doc_vehicle_nodes _vdocs = db.tbl_doc_vehicle_nodes.Where(d => d.Status == true && d.ID == DocID).FirstOrDefault();
                        _vdocs.AuditedBy = User.Identity.Name.ToUpper();
                        _vdocs.Audited = true;
                        db.SaveChanges();
                        docCnt += 1;
                    }
                }
                // Driver Documents
                for (int i = 0; i < _lstdriverNodes.Count; i++)
                {
                    bool isChecked = frm.GetValues("chkdoc_driver" + _lstdriverNodes[i].ID).Contains("true"); //Getting all checkboxes values by driver
                    long DocID = Convert.ToInt64(_lstdriverNodes[i].ID);
                    if (isChecked == true)
                    {
                        tbl_doc_driver_nodes _ddocs = db.tbl_doc_driver_nodes.Where(a => a.Status == true && a.ID == DocID).FirstOrDefault();
                        _ddocs.AuditedBy = User.Identity.Name.ToUpper();
                        _ddocs.Audited = true;
                        db.SaveChanges();
                        docCnt += 1;
                    }
                }
                // Vendor Documents
                for (int i = 0; i < _lstvendorNodes.Count; i++)
                {
                    bool isChecked = frm.GetValues("chkdoc_vendor" + _lstvendorNodes[i].ID).Contains("true"); //Getting all checkboxes values by vendor
                    long DocID = Convert.ToInt64(_lstvendorNodes[i].ID);
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
                    return Content("<script language='javascript' type='text/javascript'>alert('Please select atleast on document.');$('#VehicleID').val('');$('#VehicleID').focus();</script>");
                }
                return Content("<script language='javascript' type='text/javascript'>alert('Selected Documents are Audited successfully.');$('#VehicleID').val('');$('#VehicleID').focus();</script>");
            }
            catch (Exception ex)
            {
                return Content("<script language='javascript' type='text/javascript'>alert('Documents Auditing fail.'"+ex.ToString()+"');</script>");
            }

        }
        #endregion
    }
}