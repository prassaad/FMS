using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;
namespace FMS.Areas.Inventory.Controllers
{
    [CustomAuthorizationFilter]
    public class SuppliersController : Controller
    {
        private FMSDBEntities db;
        private core objCr = new core(); 
        public SuppliersController()
        {
            db=new FMSDBEntities();
        }
        public ActionResult Index()
        {
            var list = db.tbl_SuppliersMaster.Where(a => a.Active == true).ToList();
            objCr.ConvertToUppercase<tbl_SuppliersMaster>(list);
            return View(list);
        }
        public ActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Create(tbl_SuppliersMaster suppliesmaster,FormCollection frm)
        {
            try
            {
                if (ValidateForm(suppliesmaster, true))
                {
                    if (frm["ddltitle"] != "0")
                    {
                        if (suppliesmaster.IsCompany == "Yes")
                            suppliesmaster.Title1 = suppliesmaster.Title1.Trim() + " " + frm["ddltitle"];
                        else
                            suppliesmaster.Title1 = frm["ddltitle"] + " " + suppliesmaster.Title1.Trim();
                    }
                    suppliesmaster.Active = true;
                    db.tbl_SuppliersMaster.AddObject(suppliesmaster);
                    db.SaveChanges();
                    return Content("<script language='javascript' type='text/javascript'>alert('Suppliers Details has been added successfully');window.location.href='/Inventory/Suppliers/Index';</script>");
                }
            }
            catch
            {
                return Content("<script language='javascript' type='text/javascript'>alert('An error occured while proccessing your request');</script>");
            }
            return View(suppliesmaster);
        }
        public ActionResult Edit(long id)
        {
            tbl_SuppliersMaster supliersdet = db.tbl_SuppliersMaster.Where(a => a.SupplierId == id).SingleOrDefault();
            return View(supliersdet);
        }
        [HttpPost]
        public ActionResult Edit(tbl_SuppliersMaster suplies,FormCollection frm)
        {   
            tbl_SuppliersMaster supliersdet = db.tbl_SuppliersMaster.Where(a => a.SupplierId == suplies.SupplierId).SingleOrDefault();
           
            try
            {
                if(ValidateForm(suplies,false))
                {  
                    supliersdet.Active = true;
                    if ((supliersdet.SupplierName.Trim().ToUpper() != suplies.SupplierName.Trim().ToUpper()))
                    {
                        var isduplicate = db.tbl_SuppliersMaster.Where(a => a.SupplierName.Trim().ToUpper() == suplies.SupplierName.Trim().ToUpper() && a.Active == true).Any();
                        if (isduplicate == true)
                            ModelState.AddModelError("SupplierName", "Suplier Name  already exists.");
                        else
                        {
                            UpdateModel(supliersdet);
                            if (frm["ddltitle"] != "0")
                            {
                                if (suplies.IsCompany == "Yes")
                                    supliersdet.Title1 = suplies.Title1.Trim() + " " + frm["ddltitle"];
                                else
                                    supliersdet.Title1 = frm["ddltitle"] + " " + suplies.Title1.Trim();
                            }
                            db.SaveChanges();
                            return Content("<script language='javascript' type='text/javascript'>alert('Suppliers Details has been updated successfully');window.location.href='/Inventory/Suppliers/Index';</script>");

                        }
                    }
                    else
                    {
                        UpdateModel(supliersdet);
                        if (frm["ddltitle"] != "0")
                        {
                            if (suplies.IsCompany == "Yes")
                                supliersdet.Title1 = suplies.Title1.Trim() + " " + frm["ddltitle"];
                            else
                                supliersdet.Title1 = frm["ddltitle"] + " " + suplies.Title1.Trim();
                        }
                        db.SaveChanges();
                        return Content("<script language='javascript' type='text/javascript'>alert('Suppliers Details has been updated successfully');window.location.href='/Inventory/Suppliers/Index';</script>");
                    }
                }
            }
            catch
            {
                return Content("<script language='javascript' type='text/javascript'>alert('An error occured while proccessing your request');</script>");

            }
            return View(suplies);
        }
        public ActionResult Delete(long Id)
        {
            var supliersdet = db.tbl_SuppliersMaster.Where(a => a.SupplierId == Id).SingleOrDefault();
            try
            {
                supliersdet.Active = false;
                db.SaveChanges();
                return Content("<script language='javascript' type='text/javascript'>alert('Suppliers Details has been deleted successfully');window.location.href='/Inventory/Suppliers/Index';</script>");
            }
            catch
            {
                return Content("<script language='javascript' type='text/javascript'>alert('An error occured while proccessing your request');</script>");

            }
        }
        private bool ValidateForm(tbl_SuppliersMaster supplies, bool isCreate)
        {
            if (supplies.SupplierName == null || supplies.SupplierName.Trim().Length == 0)
                ModelState.AddModelError("SupplierName", "Supplier Name is required.");
            if (supplies.IsCompany == null)
                ModelState.AddModelError("IsCompany", "Please selecet Yes/No.");
            if (supplies.Address == null || supplies.Address.Trim().Length == 0)
                ModelState.AddModelError("Address", "Address is required.");
            if (supplies.City == null || supplies.City.Trim().Length == 0)
                ModelState.AddModelError("City", "City Name is required.");
            if (supplies.State == null || supplies.State.Trim().Length == 0)
                ModelState.AddModelError("State", "State Name is required.");
            if (supplies.PinCode == null || supplies.PinCode.Trim().Length == 0)
                ModelState.AddModelError("PinCode", "PinCode is required.");
            //if (supplies.Website == null || supplies.Website.Trim().Length == 0)
            //    ModelState.AddModelError("Website", "Website Name required.");
            //if (supplies.Phone == null || supplies.Phone.Trim().Length == 0)
            //    ModelState.AddModelError("Phone", "Phone no. is required.");
            if (supplies.Mobile == null || supplies.Mobile.Trim().Length == 0)
                ModelState.AddModelError("Mobile", "Mobile no. is required");
            if (supplies.Email == null || supplies.Email.Trim().Length == 0)
                ModelState.AddModelError("Email", "Email Id is required.");
            if (supplies.Title1 == null || supplies.Title1.Trim().Length == 0)
                ModelState.AddModelError("Title1", "Title is required");
            if (isCreate == true) // check duplicate  insertion
            {
                var isduplicate = db.tbl_SuppliersMaster.Where(a => a.SupplierName.Trim().ToUpper() == supplies.SupplierName.Trim().ToUpper() && a.Active == true).Any();
                if (isduplicate == true)
                    ModelState.AddModelError("SupplierName", "Supplier Name  already exists.");
            }
            return ModelState.IsValid;
        }

    }
}
