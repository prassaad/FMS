using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;

namespace FMS.Areas.Inventory.Controllers
{
    [CustomAuthorizationFilter]
    public class UnitController : Controller
    {
        //
        // GET: /Unit/
        private core objCr = new core();
        private FMSDBEntities _dataModel = new FMSDBEntities();
        public ActionResult Index()
        {
            ViewBag.Msg = TempData["ErrorMag"];
            var list = _dataModel.InventoryUnitsMasters.Where(u => u.Active == true).ToList();
            objCr.ConvertToUppercase<InventoryUnitsMaster>(list);
            return View(list);
        }

        //
        // GET: /InventoryUnitsMaster/Create

        public ActionResult Create()
        {
            return View();
        }

        //
        // POST: /InventoryUnitsMaster/Create

        [HttpPost]
        public ActionResult Create([Bind(Exclude = "Id")]InventoryUnitsMaster invUnitMaster)
        {
            try
            {
                if (ValidateForm(invUnitMaster,true))
                {
                    invUnitMaster.Active = true;
                    objCr.ConvertToUppercase(invUnitMaster);
                    _dataModel.AddToInventoryUnitsMasters(invUnitMaster);
                    _dataModel.SaveChanges();
                    return RedirectToAction("Index");
                }
                return View();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(invUnitMaster);
            }

        }

        //
        // GET: /InventoryUnitsMaster/Edit/5

        public ActionResult Edit(int id)
        {
            var InvUnitsData = _dataModel.InventoryUnitsMasters.SingleOrDefault(u => u.Id == id);
            return View(InvUnitsData);
        }

        //
        // POST: /InventoryUnitsMaster/Edit/5

        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection, InventoryUnitsMaster invUnitMaster)
        {
            var InvUnitsData = _dataModel.InventoryUnitsMasters.SingleOrDefault(u => u.Id == id);
            try
            {
                if (ValidateForm(invUnitMaster, false))
                {
                    if ((InvUnitsData.UnitsText.Trim().ToUpper() != invUnitMaster.UnitsText.Trim().ToUpper()))
                    {
                        var isduplicate = _dataModel.InventoryUnitsMasters.Where(a => a.UnitsText.Trim().ToUpper() == invUnitMaster.UnitsText.Trim().ToUpper()).Any();
                        if (isduplicate == true)
                            ModelState.AddModelError("UnitsText", " '" + invUnitMaster.UnitsText + " ' Unit(s) name has already exists.");
                        else
                        {
                            objCr.ConvertToUppercase(InvUnitsData);
                            UpdateModel(InvUnitsData);
                            _dataModel.SaveChanges();
                            return RedirectToAction("Index");
                        }
                    }
                    else
                    {
                        objCr.ConvertToUppercase(InvUnitsData);
                        UpdateModel(InvUnitsData);
                        _dataModel.SaveChanges();
                        return RedirectToAction("Index");
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(InvUnitsData);
            }
            return View(InvUnitsData);
        }
        private bool ValidateForm(InventoryUnitsMaster profile,bool isCreate)
        {
            if (profile.UnitsText == "" ||profile.UnitsText == null )
                ModelState.AddModelError("UnitsText", "Unit(s) name is required.");
            if (profile.UnitsShortText == "" || profile.UnitsShortText == null)
                ModelState.AddModelError("UnitsShortText", "Unit(s) short name is required.");
            if (isCreate == true)
            {
                // check duplicate  insertion
                var isduplicate = _dataModel.InventoryUnitsMasters.Where(a => a.UnitsText.Trim().ToUpper() == profile.UnitsText.Trim().ToUpper()).Any();
                if (isduplicate == true)
                    ModelState.AddModelError("UnitsText", " '" + profile.UnitsText + " ' Unit(s) name has already exists.");
            }
            return ModelState.IsValid;
        }

        public ActionResult Details(int id)
        {
            return View(_dataModel.InventoryUnitsMasters.Where(a => a.Id == id && a.Active == true).FirstOrDefault());
        }
        //
        // GET: /InventoryUnitsMaster/Delete/5

        public ActionResult Delete(int id)
        {  
            var InvUnitsDataToDelete = _dataModel.InventoryUnitsMasters.SingleOrDefault(u => u.Id == id);
            try
            {
                _dataModel.DeleteObject(InvUnitsDataToDelete);
                _dataModel.SaveChanges();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMag"] = "This '" + InvUnitsDataToDelete.UnitsText + "' Unit(s) name has already had relation with another table.You cannot be deleted.";
                return RedirectToAction("Index");
            }

        }
    }
}
