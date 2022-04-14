using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;

namespace FMS.Areas.Inventory.Controllers
{ 
    [CustomAuthorizationFilter]
    public class ItemCategoriesController : Controller
    {
        private FMSDBEntities db = new FMSDBEntities();
        private core objCr = new core(); 
        public ViewResult Index()
        {
            ViewBag.Msg = TempData["ErrorMag"];
          return View(db.InventoryItemCategories.Where(a=>a.Active ==true).ToList());
        }

       

        public ActionResult Create()
        {
            return View();
        } 
        [HttpPost]
        public ActionResult Create(InventoryItemCategory inventoryitemcategory)
        {
            try
            {
                if (ValidateForm(inventoryitemcategory, true))
                {
                    inventoryitemcategory.Active = true;
                    objCr.ConvertToUppercase(inventoryitemcategory);
                    db.InventoryItemCategories.AddObject(inventoryitemcategory);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }

                return View(inventoryitemcategory);
            }
            catch(Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View();
            }
           
        }
       public ActionResult Edit(int id)
        {
                InventoryItemCategory inventoryitemcategory = db.InventoryItemCategories.Single(i => i.ID == id);
                return View(inventoryitemcategory);
        }

        [HttpPost]
       public ActionResult Edit(int id, InventoryItemCategory inventoryitemcategory)
        {
            InventoryItemCategory updateitemcategory = db.InventoryItemCategories.Single(i => i.ID == id);
            try
            {
                if (ValidateForm(inventoryitemcategory,false))
                {
                    if ((updateitemcategory.ItemCatText.Trim().ToUpper() != inventoryitemcategory.ItemCatText.Trim().ToUpper()))
                    {
                        var isduplicate = db.InventoryItemCategories.Where(a => a.ItemCatText.Trim().ToUpper() == inventoryitemcategory.ItemCatText.Trim().ToUpper()).Any();
                        if (isduplicate == true)
                            ModelState.AddModelError("ItemCatText", "Item category has already exists.");
                        else
                        {
                            objCr.ConvertToUppercase(updateitemcategory);
                            UpdateModel(updateitemcategory);
                            inventoryitemcategory.Active = true;
                            db.SaveChanges();
                            return RedirectToAction("Index");
                        }
                    }
                    else
                    {
                        objCr.ConvertToUppercase(updateitemcategory);
                        UpdateModel(updateitemcategory);
                        inventoryitemcategory.Active = true;
                        db.SaveChanges();
                        return RedirectToAction("Index");
                    }
                }
                return View(inventoryitemcategory);
            }
            catch(Exception ex) 
            {
                ModelState.AddModelError("", ex.Message);
                return View();
            }

           
        }
        public ActionResult Delete(int id)
        { 
            InventoryItemCategory inventoryitemcategory = db.InventoryItemCategories.Single(i => i.ID == id);
            try
            {
                db.DeleteObject(inventoryitemcategory);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMag"] = "This '" + inventoryitemcategory.ItemCatText + "' Item category has already had relation with another table.You cannot be deleted.";
                return RedirectToAction("Index");
            }

        }

        private bool ValidateForm(InventoryItemCategory profile,bool isCreate)
        {
            if (string.IsNullOrEmpty(profile.ItemCatText))
                ModelState.AddModelError("ItemCatText", "Item category name is required.");
            if (isCreate == true)
            {
                // check duplicate Item category name insertion
                var isduplicate = db.InventoryItemCategories.Where(a => a.ItemCatText.Trim().ToUpper() == profile.ItemCatText.Trim().ToUpper()).Any();
                if (isduplicate == true)
                    ModelState.AddModelError("ItemCatText", "Item category has already exists.");
            }
            return ModelState.IsValid;
        }
    }
}