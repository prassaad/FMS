using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;
using System.IO;
using FMS.Helpers;

namespace FMS.Controllers
{
    [CustomAuthorizationFilter]
    public class SubVendorController : Controller
    {
        #region ctors
        private FMSDBEntities db;
        private core core = new core();
        public SubVendorController()
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
        public JsonResult GetSubVendors(int iDisplayStart, int iDisplayLength, string sSearch, int iColumns,
   int iSortingCols, int iSortCol_0, string sSortDir_0,
   int sEcho, string mDataProp_Key)
        {
            var subvendorsList = GetSubVendorsList();
            var filteredsubvendors = subvendorsList
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
            var orderedlist = filteredsubvendors
                   .OrderByWithDirection(i => (i[iSortCol_0]).Parse(), sSortDir_0 == "asc")
                   .Skip(iDisplayStart)
                   .Take(iDisplayLength);
            var model = new
            {
                aaData = orderedlist,
                iTotalDisplayRecords = filteredsubvendors.Count(),
                iTotalRecords = filteredsubvendors.Count(),
                sEcho = sEcho.ToString()
            };
            return Json(model, JsonRequestBehavior.AllowGet);
        }
        private IEnumerable<CommonFields> GetSubVendorsList()
        {
            var list = new List<CommonFields>();
            var vendorList = db.tbl_vendor_details.Where(a => a.Status == true && a.SubVendor == true).ToList();
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

        #region Add Sub Vendor 
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
                    vd.SubVendor = true;
                    db.tbl_vendor_details.AddObject(vd);
                    db.SaveChanges();
                    core.LoggingEntries("Sub Vendor Mgmt.", "Sub-vendor has created with name " + vd.FirstName + " " + vd.LastName + " ", User.Identity.Name);
                    return RedirectToAction("Index");
                }
                return View();
            }
            catch (Exception)
            {
                return View();
            }

        } 
        #endregion

        #region Edit Sub Vendor
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
                    vendorDet.SubVendor = true;
                    TryUpdateModel<tbl_vendor_details>(vendorDet);
                    core.ConvertToUppercase(vendorDet);
                    db.SaveChanges();
                    core.LoggingEntries("Sub Vendor Mgmt.", "Sub-vendor has updated with name " + vd.FirstName + " " + vd.LastName + " ", User.Identity.Name);
                    return RedirectToAction("Index");
                }
            }
            catch (Exception)
            {
                return View(vendorDet);
            }
            return View(vendorDet);
        } 
        #endregion

        #region Delete Sub Vendor
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
                core.LoggingEntries("Sub Vendor Mgmt.", "Sub-vendor has deleted with name " + vd.FirstName + " " + vd.LastName + " ", User.Identity.Name);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return View();
            }
        } 
        #endregion

        #region View Sub Vendor Details
        public ActionResult Details(long Id)
        {
            tbl_vendor_details vendorDet = db.tbl_vendor_details.SingleOrDefault(a => a.ID == Id && a.Status == true);
            return View(vendorDet);
        } 
        #endregion

        public bool ValidateForm(tbl_vendor_details vd)
        {
            if (vd.FirstName == null || vd.FirstName.ToString().Trim().Length == 0)
                ModelState.AddModelError("FirstName", "Please enter firstname.");
            if (vd.LastName ==null ||  vd.LastName.ToString().Trim().Length == 0)
                ModelState.AddModelError("LastName", "Please enter lastname.");
            if(vd.ContactNumber.ToString().Trim().Length == 0)
                ModelState.AddModelError("ContactNumber", "Please enter contact number.");
            if (vd.ContactNumber.ToString().Trim().Length > 10)
                ModelState.AddModelError("ContactNumber", "Contact number should not be greater than 10 digits.");
            if (vd.ContactNumber.ToString().Trim().Length < 10)
                ModelState.AddModelError("ContactNumber", "Contact number should not be less than 10 digits.");
            if (vd.PanCardNumber ==null || vd.PanCardNumber.ToString().Trim().Length == 0)
                ModelState.AddModelError("PanCardNumber", "Please enter PAN card number.");
            return ModelState.IsValid; 
        }
    }
}
