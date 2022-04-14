using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;
using FMS.Helpers;
using System.IO;

namespace FMS.Controllers
{
    [CustomAuthorizationFilter]
    public class ClientInvoiceController : Controller
    {
        #region ctors
        private FMSDBEntities db;
        private core objCore = new core();
        public ClientInvoiceController()
        {
            db = new FMSDBEntities();
        } 
        #endregion

        #region Actions
        public ActionResult Index()
        {
            return View();
        }
        public JsonResult GetClientInvoice(int iDisplayStart, int iDisplayLength, string sSearch, int iColumns,
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
                        l.Date1.ToShortDateString(),
                        l.Text4.ToString(),
                        l.Amount.ToString(),
                        l.Amount1.ToString(),
                        l.Amount2.ToString(),
                        l.Amount3.ToString(),
                        l.Amount4.ToString(),
                        l.Amount5.ToString(),
                        l.Amount6.ToString(),
                        l.Text5.ToString(),
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
            var ClinetInvList = db.tbl_client_invoice.Where(a => a.Status == true).ToList();
            foreach (tbl_client_invoice ls in ClinetInvList)
            {
                list.Add(new CommonFields
                {
                    Id = ls.ID,
                    Text1 = ls.InvoiceNum + (string.IsNullOrEmpty(ls.InvDocFile) ? "" : "$" + ls.InvDocFile),
                    Text2 = ls.ClientID == null ? "" : ls.tbl_clients.CompanyName,
                    Text3 = ls.MonthYear == null ? "" : ls.MonthYear,
                    Date1 = Convert.ToDateTime(ls.InvoiceDate),
                    Text4 = ls.InvoiceParticular == null ? "" : ls.InvoiceParticular,
                    Amount = ls.InvoiceAmount == null ? 0 : (decimal)ls.InvoiceAmount,
                    Amount1 = ls.CGST == null ? 0 : (decimal)ls.CGST,
                    Amount2 = ls.SGST == null ? 0 : (decimal)ls.SGST,
                    Amount3 = ls.IGST== null ? 0 : (decimal)ls.IGST,
                    Amount4 = ls.TDS == null ? 0 : (decimal)ls.TDS,
                    Amount5 = ls.NetTotal == null ? 0 : (decimal)ls.NetTotal,
                    Amount6 = ls.PaidAmount == null ? 0 : (decimal)ls.PaidAmount,
                    Text5 = ls.PaidDate == null ? "" : ls.PaidDate.Value.ToShortDateString(),
                    Status1 = ls.Paid == null ? false : (bool)ls.Paid,
                    Status = (bool)ls.Status
                });
            }
            objCore.ConvertToUppercase<CommonFields>(list);
            return list.OrderBy(a => a.Id);
        }
        [HttpGet]
        public ActionResult GenerateInvoice()
        {
            //Modified After GST 19 Oct 2017
            //ViewBag.ClientServiceTax = System.Configuration.ConfigurationManager.AppSettings["ClientServiceTax"];
            //ViewBag.ClientVatTax = System.Configuration.ConfigurationManager.AppSettings["ClientVatTax"];

            ViewBag.CGST = System.Configuration.ConfigurationManager.AppSettings["CGST"];
            ViewBag.SGST = System.Configuration.ConfigurationManager.AppSettings["SGST"];
            ViewBag.IGST = System.Configuration.ConfigurationManager.AppSettings["IGST"];

            ViewBag.ClientTDS = System.Configuration.ConfigurationManager.AppSettings["ClientTDS"];
            return View();
        }
        [HttpPost]
        public ActionResult GenerateInvoice(FormCollection frm, tbl_client_invoice invoiceDet, HttpPostedFileBase InvDocFile) 
        {
            //Modified After GST 19 Oct 2017
            //ViewBag.ClientServiceTax = System.Configuration.ConfigurationManager.AppSettings["ClientServiceTax"];
            //ViewBag.ClientVatTax = System.Configuration.ConfigurationManager.AppSettings["ClientVatTax"];

            ViewBag.CGST = System.Configuration.ConfigurationManager.AppSettings["CGST"];
            ViewBag.SGST = System.Configuration.ConfigurationManager.AppSettings["SGST"];
            ViewBag.IGST = System.Configuration.ConfigurationManager.AppSettings["IGST"];
            ViewBag.ClientTDS = System.Configuration.ConfigurationManager.AppSettings["ClientTDS"];

            try
            {
                // -------------------------------------------------------
                // ------------------ Verify Valid fields ----------------
                // -------------------------------------------------------
                if (Validation(invoiceDet))
                {
                    if (InvDocFile != null && InvDocFile.ContentLength > 0)
                    {
                        string ext = Path.GetExtension(InvDocFile.FileName);
                        invoiceDet.InvDocFile = "InvoiceDoc_" + DateTime.Now.ToString("ffff") + invoiceDet.ID + ext;
                        var path = Path.Combine(Server.MapPath("../Content/Documents/Client/"), invoiceDet.InvDocFile);
                        InvDocFile.SaveAs(path);
                    }
                    invoiceDet.CreatedDate = DateTime.Now.Date;
                    invoiceDet.CreatedBy = User.Identity.Name;
                    invoiceDet.Paid = false;
                    invoiceDet.Status = true;
 
                    objCore.ConvertToUppercase(invoiceDet);
                    db.tbl_client_invoice.AddObject(invoiceDet);
                    db.SaveChanges();

                    objCore.LoggingEntries("Client Invoice ", "Client Invoice has generated to the client " + invoiceDet.tbl_clients.CompanyName + "", User.Identity.Name);

                    return RedirectToAction("Index");
                }
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Message = "An Error occured while processing your request ==> " + ex.Message.ToString();
                return View();
            }
        }

        [HttpGet]
        public ActionResult DoPayment(long InvoiceId)
        {
            var InvoiceDet = db.tbl_client_invoice.Where(a => a.ID == InvoiceId && a.Status == true).FirstOrDefault();
            ViewBag.InvoiceDet = InvoiceDet;
            return PartialView("_DoPayment");
        }

        [HttpPost]
        public ActionResult DoPayment(long InvoiceId, FormCollection frm, tbl_client_payments clientPayments)
        {
            tbl_client_invoice clientInvoiceDet = db.tbl_client_invoice.Where(a => a.Status == true && a.ID == InvoiceId).FirstOrDefault();
            try
            {
                if (clientPayments.PayMode.ToUpper() == "CASH")
                {
                    clientPayments.DDChequeDate = null; clientPayments.DDChequeNo = null;
                    clientPayments.BankName = null; clientPayments.Branch = null;
                    clientPayments.TransactionNum = null;
                }
                else if (clientPayments.PayMode.ToUpper() == "CHEQUE")
                {
                    clientPayments.TransactionNum = null;
                }
                else
                {
                    clientPayments.DDChequeDate = null; clientPayments.DDChequeNo = null;
                    clientPayments.BankName = null; clientPayments.Branch = null;
                }
                clientPayments.CreatedBy = User.Identity.Name;
                clientPayments.CreatedDate = DateTime.Now.Date;
                clientPayments.ClientID = clientInvoiceDet.ClientID;
                clientPayments.InvoiceID = clientInvoiceDet.ID;
                clientPayments.PaidDate = DateTime.Now.Date;
                clientPayments.ReceptNo = DateTime.Now.ToString("hhmmssddMM");
                clientPayments.Status = true;
                objCore.ConvertToUppercase(clientPayments);
                db.tbl_client_payments.AddObject(clientPayments);
                db.SaveChanges();

                MakeInvoicePayment((decimal)clientPayments.Amount, InvoiceId);

                objCore.LoggingEntries("Client Invoice ", "Client Invoice payment received for the client "+ clientInvoiceDet.tbl_clients.CompanyName +".", User.Identity.Name);
            }
            catch (Exception ex)
            {
                return Json(new { success = true, msg = "An Error Occured while your request.Please try again ==> " + ex.Message.ToString() });
            }
            return Json(new { success = true, msg = "Payment has done successfully." });
        }

        public ActionResult ViewPayment(long InvoiceId)
        {
            tbl_client_invoice clientInvoiceDet = db.tbl_client_invoice.Where(a => a.ID == InvoiceId && a.Status == true).FirstOrDefault();
            List<tbl_client_payments> clientPaymentList = db.tbl_client_payments.Where(a => a.Status == true && a.InvoiceID == InvoiceId).ToList();
            return PartialView("_ViewPayment", new ClientInvoice(clientInvoiceDet, clientPaymentList));
        }

        public ActionResult Edit(long Id)
        {
            tbl_client_invoice clientInvoiceDet = db.tbl_client_invoice.Where(a => a.Status == true && a.ID == Id).FirstOrDefault();
             
            //Modified After GST 19 Oct 2017
            //ViewBag.ClientServiceTax = System.Configuration.ConfigurationManager.AppSettings["ClientServiceTax"];
            //ViewBag.ClientVatTax = System.Configuration.ConfigurationManager.AppSettings["ClientVatTax"];

            //ViewBag.CGST = System.Configuration.ConfigurationManager.AppSettings["CGST"];
            //ViewBag.SGST = System.Configuration.ConfigurationManager.AppSettings["SGST"];
            //ViewBag.IGST = System.Configuration.ConfigurationManager.AppSettings["IGST"];
            //ViewBag.ClientTDS = System.Configuration.ConfigurationManager.AppSettings["ClientTDS"];


            return View(clientInvoiceDet);
        }
        [HttpPost]
        public ActionResult Edit(long Id, FormCollection frm, tbl_client_invoice clientInvDet, HttpPostedFileBase InvDocFile) 
        {
            tbl_client_invoice clientInvoiceDet = db.tbl_client_invoice.Where(a => a.Status == true && a.ID == Id).FirstOrDefault();

            //Modified After GST 19 Oct 2017
            //ViewBag.ClientServiceTax = System.Configuration.ConfigurationManager.AppSettings["ClientServiceTax"];
            //ViewBag.ClientVatTax = System.Configuration.ConfigurationManager.AppSettings["ClientVatTax"];

            //ViewBag.CGST = System.Configuration.ConfigurationManager.AppSettings["CGST"];
            //ViewBag.SGST = System.Configuration.ConfigurationManager.AppSettings["SGST"];
            //ViewBag.IGST = System.Configuration.ConfigurationManager.AppSettings["IGST"];
            //ViewBag.ClientTDS = System.Configuration.ConfigurationManager.AppSettings["ClientTDS"];


            string FileStr = clientInvoiceDet.InvDocFile;
            try
            {
                if (Validation(clientInvDet))
                {
                    if (clientInvoiceDet.PaidAmount != null)
                    {
                        if (clientInvDet.NetTotal > clientInvoiceDet.PaidAmount)
                        {
                            clientInvDet.Paid = false;
                            clientInvoiceDet.Paid = false;
                        }
                    }
                    if (InvDocFile != null && InvDocFile.ContentLength > 0)
                    {
                        string ext = Path.GetExtension(InvDocFile.FileName);
                        clientInvDet.InvDocFile = "InvoiceDoc_" + DateTime.Now.ToString("ffff") + clientInvDet.ID + ext;
                        FileStr = clientInvDet.InvDocFile;
                        var path = Path.Combine(Server.MapPath("../../Content/Documents/Client/"), clientInvDet.InvDocFile);
                        InvDocFile.SaveAs(path);
                    }
                    else
                    {
                        clientInvDet.InvDocFile = FileStr;
                        clientInvoiceDet.InvDocFile = FileStr;
                    }
                    TryUpdateModel(clientInvoiceDet);
                    if (!string.IsNullOrEmpty(FileStr))
                        clientInvoiceDet.InvDocFile = FileStr;
                    db.SaveChanges();
                    objCore.LoggingEntries("Client Invoice ", "Client Invoice was edited for the client " + clientInvoiceDet.tbl_clients.CompanyName + ".", User.Identity.Name);
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                ViewBag.message = "An Error occured while processing your request ==>" + ex.Message.ToString();
                return View(clientInvoiceDet);
            }
            return View(clientInvoiceDet);
        }

        public ActionResult Delete(long Id)
        {
            tbl_client_invoice clientDet = db.tbl_client_invoice.Where(a => a.ID == Id).FirstOrDefault();
            clientDet.Status = false;
            try
            {
                TryUpdateModel(clientDet);
                // -------------------------------------------------------------
                //-------------- Verify Whether Payments exists or not.----------
                //----------------------------------------------------------------
                List<tbl_client_payments> paymentList = db.tbl_client_payments.Where(a => a.InvoiceID == clientDet.ID && a.Status == true).ToList();

                foreach (tbl_client_payments obj in paymentList)
                {
                    tbl_client_payments p = db.tbl_client_payments.Where(a => a.ID == obj.ID && a.Status == true).FirstOrDefault();
                    TryUpdateModel(p);
                    p.Status = false;
                }
                // Save all changes
                db.SaveChanges();

                objCore.LoggingEntries("Client Invoice ", "Client Invoice has deleted for the client " + clientDet.tbl_clients.CompanyName + ".", User.Identity.Name);

                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                return View("Error");
            }
        } 
        #endregion

        #region Methods
        public bool Validation(tbl_client_invoice invoiceDet)
        {
            if (invoiceDet.ClientID == null)
                ModelState.AddModelError("ClientID", "Please search client to generate invoice");
            if (invoiceDet.InvoiceDate == null)
                ModelState.AddModelError("InvoiceDate", "Please select invoice date.");
            if (invoiceDet.MonthYear == null)
                ModelState.AddModelError("MonthYear", "Please select month year");
            if (invoiceDet.InvoiceNum == null)
                ModelState.AddModelError("InvoiceNum", "Please enter invoice number");
            if (invoiceDet.InvoiceAmount == null)
                ModelState.AddModelError("InvoiceAmount", "Please enter valid invoice amount");
            return ModelState.IsValid;
        }

        public string GetInvoiceNumber(string MonthYear)
        {
            string InvoiceNum = string.Empty;
            tbl_sequences seqDet = db.tbl_sequences.Where(a => a.Type == "CLIENTINVOICE" && a.Status == true && a.Auto == true).FirstOrDefault();
            InvoiceNum = seqDet.Prefix + "/" + (seqDet.Number).ToString() + "/" + MonthYear.Split(' ')[0] + "/" + MonthYear.Split(' ')[1];
            return InvoiceNum;
        }

        public int MakeInvoicePayment(decimal Amount, long InvoiceId)
        {
            decimal fedAmt = 0; // Received Amt
            decimal curInvAmt = 0; // Actual Due
            decimal InvoiceAmt = 0;
            fedAmt = Amount;

            try
            {
                if (Amount != 0)
                {
                    List<tbl_client_invoice> Invoices = GetUnPaidInvoice(InvoiceId);
                    if (Invoices != null)
                    {
                        // loop through make invoice payment
                        foreach (tbl_client_invoice obj in Invoices)
                        {
                            InvoiceAmt = (decimal)obj.NetTotal;

                            if ((InvoiceAmt - obj.PaidAmount) != 0)
                            {
                                obj.PaidAmount = obj.PaidAmount == null ? 0 : (decimal)obj.PaidAmount;
                                curInvAmt = InvoiceAmt - (decimal)obj.PaidAmount;
                                if (curInvAmt > fedAmt)
                                {
                                    obj.PaidAmount = fedAmt + obj.PaidAmount;
                                    obj.Paid = false;
                                    obj.PaidDate = DateTime.Now;
                                    db.SaveChanges();
                                    fedAmt = 0;
                                }
                                else if (curInvAmt <= fedAmt)
                                {
                                    obj.Paid = true;
                                    obj.PaidDate = DateTime.Now;
                                    obj.PaidAmount = curInvAmt + obj.PaidAmount;
                                    db.SaveChanges();
                                    fedAmt = fedAmt - curInvAmt;
                                }
                            }
                        } // END Foreach Loop
                    } // END IF 
                } // END IF Amount ! =0 
                return 1;
            } // END TRY BLOCK
            catch (Exception)
            {
                return 0;
            }
        }

        public List<tbl_client_invoice> GetUnPaidInvoice(long InvoiceId)
        {
            List<tbl_client_invoice> clientInvoiceList = db.tbl_client_invoice.Where(a => a.ID == InvoiceId && a.Status == true).ToList();
            return clientInvoiceList;
        } 
        #endregion

    }
}
