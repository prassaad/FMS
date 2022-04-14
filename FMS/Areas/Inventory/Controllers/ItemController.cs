using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;
using FMS.Areas.Inventory.Controllers;

namespace FMS.Areas.Inventory.Controllers
{
    [CustomAuthorizationFilter]
    public class ItemController : Controller
    {

        private FMSDBEntities _dataModel = new FMSDBEntities();
        List<SelectListItem> items = new List<SelectListItem>();
        StoresController objStore = new StoresController();
        private core objCr = new core();
        public ItemController()
        {
            items.Add(new SelectListItem { Value = "1", Text = "Item" });
            items.Add(new SelectListItem { Value = "0", Text = "Service" });
        }
        public ActionResult Index()
        {
            ViewBag.Msg = TempData["ErrorMag"];
            var data = _dataModel.InventoryItemsMasters.Where(m => m.Active == true).ToList();
            ViewBag.CampusId = Convert.ToInt64(Session["Campus_Id"]);
            objCr.ConvertToUppercase<InventoryItemsMaster>(data);
            return View(data);
        }
        public ActionResult Create()
        {
            ViewBag.ItemCategory = new SelectList(_dataModel.InventoryItemCategories.Where(a => a.Active == true), "ID", "ItemCatText");
            ViewBag.Units = new SelectList(_dataModel.InventoryUnitsMasters.Where(a => a.Active == true), "ID", "UnitsText");
            ViewBag.IsItem = new SelectList(items, "Value", "Text");
            ViewBag.ItemCodes = objCr.GetInvItemCodes();
            return View(new InventoryItemsMaster());
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Create([Bind(Exclude = "Id")]InventoryItemsMaster InvItemsMaster, FormCollection frm)
        {
            ViewBag.ItemCategory = new SelectList(_dataModel.InventoryItemCategories.Where(a => a.Active == true), "ID", "ItemCatText", InvItemsMaster.ItemCatId);
            ViewBag.IsItem = new SelectList(items, "Value", "Text", InvItemsMaster.IsItem.ToString());
            ViewBag.Units = new SelectList(_dataModel.InventoryUnitsMasters.Where(a => a.Active == true), "ID", "UnitsText", InvItemsMaster.UnitId);
            ViewBag.ItemCodes = objCr.GetInvItemCodes();
            try
            {
                bool IsMessItem = frm.GetValues("MessItem").Contains("true");
                bool InSummary = frm.GetValues("InSummary").Contains("true");
                if (IsMessItem == true)
                    InvItemsMaster.MessItem = true;
                else
                    InvItemsMaster.MessItem = false;

                if (InSummary == true)
                    InvItemsMaster.InSummary = true;
                else
                    InvItemsMaster.InSummary = false;
                if (ValidateForm(InvItemsMaster, true))
                {
                    InvItemsMaster.Active = true;
                    objCr.ConvertToUppercase(InvItemsMaster);
                    _dataModel.AddToInventoryItemsMasters(InvItemsMaster);
                    // Add Price Details
                    tbl_inven_item_price_details objPrice = new tbl_inven_item_price_details();
                    objPrice.ItemId = InvItemsMaster.ID;
                    objPrice.EffectFromDt = DateTime.Now;
                    objPrice.EffectToDt = null;
                    objPrice.Amount = InvItemsMaster.Amount;
                    objPrice.ApprovedDt = DateTime.Now;
                    objPrice.ApprovedBy = User.Identity.Name;
                    objCr.ConvertToUppercase(objPrice);
                    _dataModel.tbl_inven_item_price_details.AddObject(objPrice);
                    _dataModel.SaveChanges();

                    // insert into stock details
                    string ReceiptNo = DateTime.Now.ToString("ddMMyyhhmmss").ToString();

                    InventoryStockRegister objInvStockMaster = new InventoryStockRegister();
                    objInvStockMaster.EntryDt = InvItemsMaster.EffectedFrom;
                    objInvStockMaster.ReceivedDt = InvItemsMaster.EffectedFrom;
                    objInvStockMaster.ReceiptNO = ReceiptNo;
                    objInvStockMaster.ReceivedBy = User.Identity.Name;
                    objInvStockMaster.Active = true;
                    objCr.ConvertToUppercase(objInvStockMaster);
                    _dataModel.InventoryStockRegisters.AddObject(objInvStockMaster);
                    _dataModel.SaveChanges();
                    InventoryStockRegDetail objInvStockDet = new InventoryStockRegDetail();
                    objInvStockDet.StockRegId = objInvStockMaster.ID;
                    objInvStockDet.ItemId = InvItemsMaster.ID;
                    objInvStockDet.ItemText = InvItemsMaster.ItemText;
                    objInvStockDet.Slno = 1;
                    objInvStockDet.Weight = InvItemsMaster.Size;
                    objInvStockDet.Rate = (double)InvItemsMaster.Amount;
                    objInvStockDet.UnitsId = InvItemsMaster.UnitId;
                    objInvStockDet.ReceQty = InvItemsMaster.OpeningBalance == null ? 0 : (double)InvItemsMaster.OpeningBalance;
                    objInvStockDet.IssuedSlod = 0;
                    objInvStockDet.QOH = InvItemsMaster.OpeningBalance == null ? 0 : (double)InvItemsMaster.OpeningBalance;
                    objCr.ConvertToUppercase(InvItemsMaster);
                    _dataModel.InventoryStockRegDetails.AddObject(objInvStockDet);
                    _dataModel.SaveChanges();
                    return RedirectToAction("Index");
                }
                return View(InvItemsMaster);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(InvItemsMaster);
            }
        }
        public ActionResult Edit(int id)
        {
            var invMstData = _dataModel.InventoryItemsMasters.SingleOrDefault(m => m.ID == id);
            ViewBag.ItemCategory = new SelectList(_dataModel.InventoryItemCategories.Where(a => a.Active == true), "ID", "ItemCatText", invMstData.ItemCatId);
            ViewBag.IsItem = new SelectList(items, "Value", "Text", invMstData.IsItem);
            ViewBag.Units = new SelectList(_dataModel.InventoryUnitsMasters.Where(a => a.Active == true), "ID", "UnitsText", invMstData.UnitId);
            long CampusId = Convert.ToInt64(Session["Campus_Id"]);
            List<GeneralClassFeilds> _StockDetList = objStore.GetStockRegList("", id, CampusId);
            decimal QOH = Convert.ToDecimal(_StockDetList.Sum(a => a.ReceQty) - _StockDetList.Sum(a => a.IssuedSlod));
            invMstData.OpeningBalance = QOH == null ? 0 : QOH;
            ViewBag.ItemCodes = objCr.GetInvItemCodes();
            return View(invMstData);
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Edit(int id, FormCollection collection, InventoryItemsMaster InvItemsMaster)
        {
            var invMstData = _dataModel.InventoryItemsMasters.SingleOrDefault(m => m.ID == id);
            ViewBag.ItemCategory = new SelectList(_dataModel.InventoryItemCategories.Where(a => a.Active == true), "ID", "ItemCatText", invMstData.ItemCatId);
            ViewBag.IsItem = new SelectList(items, "Value", "Text", invMstData.IsItem);
            ViewBag.Units = new SelectList(_dataModel.InventoryUnitsMasters.Where(a => a.Active == true), "ID", "UnitsText", invMstData.UnitId);
            ViewBag.ItemCodes = objCr.GetInvItemCodes();
            tbl_inven_item_price_details priceDet = _dataModel.tbl_inven_item_price_details.Where(a => a.ItemId == InvItemsMaster.ID && a.EffectToDt == null).FirstOrDefault();
            decimal Amt = 0;
            try
            {
                bool IsMessItem = collection.GetValues("MessItem").Contains("true");
                bool InSummary = collection.GetValues("InSummary").Contains("true");
                if (IsMessItem == true)
                    invMstData.MessItem = true;
                else
                    invMstData.MessItem = false;

                if (InSummary == true)
                    invMstData.InSummary = true;
                else
                    invMstData.InSummary = false;
                if (ValidateForm(InvItemsMaster, false))
                {

                    if ((invMstData.ItemText.Trim().ToUpper() != InvItemsMaster.ItemText.Trim().ToUpper()))
                    {
                        var isduplicate = _dataModel.InventoryItemsMasters.Where(a => a.ItemText.Trim().ToUpper() == InvItemsMaster.ItemText.Trim().ToUpper() && a.ItemCatId == InvItemsMaster.ItemCatId).Any();
                        if (isduplicate == true)
                            ModelState.AddModelError("ItemText", " '" + InvItemsMaster.ItemText + " 'Item name has already exists.");
                        else
                        {

                            UpdateModel(invMstData);
                            //==> update stock details while item updation
                            DateTime itemEffDt = Convert.ToDateTime(invMstData.EffectedFrom);
                            var x = (from isr in _dataModel.InventoryStockRegisters
                                     join isd in _dataModel.InventoryStockRegDetails on isr.ID equals isd.StockRegId
                                     where isr.Active == true && isd.ItemId == invMstData.ID
                                     select isr).FirstOrDefault();
                            if (x != null)
                            {
                                x.EntryDt = itemEffDt;
                                x.ReceivedDt = itemEffDt;
                                _dataModel.SaveChanges();
                            }
                            if (priceDet != null)
                            {
                                Amt = Convert.ToDecimal(priceDet.Amount);
                                if (InvItemsMaster.Amount != priceDet.Amount)
                                {
                                    priceDet.EffectToDt = DateTime.Now;
                                    priceDet.Amount = Amt;
                                    _dataModel.SaveChanges();

                                    // Add Price Details
                                    tbl_inven_item_price_details objPrice = new tbl_inven_item_price_details();
                                    objPrice.ItemId = InvItemsMaster.ID;
                                    objPrice.EffectFromDt = DateTime.Now;
                                    objPrice.EffectToDt = null;
                                    objPrice.Amount = InvItemsMaster.Amount;
                                    objPrice.ApprovedDt = DateTime.Now;
                                    objPrice.ApprovedBy = User.Identity.Name;
                                    objCr.ConvertToUppercase(objPrice);
                                    _dataModel.tbl_inven_item_price_details.AddObject(objPrice);
                                    _dataModel.SaveChanges();
                                }
                            }
                            return RedirectToAction("Index");
                        }
                    }
                    else
                    {
                        UpdateModel(invMstData);
                        //==> update stock details while item updation
                        DateTime itemEffDt = Convert.ToDateTime(invMstData.EffectedFrom);
                        var x = (from isr in _dataModel.InventoryStockRegisters
                                 join isd in _dataModel.InventoryStockRegDetails on isr.ID equals isd.StockRegId
                                 where isr.Active == true && isd.ItemId == invMstData.ID
                                 select isr).FirstOrDefault();
                        if (x != null)
                        {
                            x.EntryDt = itemEffDt;
                            x.ReceivedDt = itemEffDt;
                            _dataModel.SaveChanges();
                        }
                        if (priceDet != null)
                        {
                            Amt = Convert.ToDecimal(priceDet.Amount);
                            if (InvItemsMaster.Amount != priceDet.Amount)
                            {
                                priceDet.EffectToDt = DateTime.Now;
                                priceDet.Amount = Amt;
                                _dataModel.SaveChanges();

                                // Add Price Details
                                tbl_inven_item_price_details objPrice = new tbl_inven_item_price_details();
                                objPrice.ItemId = InvItemsMaster.ID;
                                objPrice.EffectFromDt = DateTime.Now;
                                objPrice.EffectToDt = null;
                                objPrice.Amount = InvItemsMaster.Amount;
                                objPrice.ApprovedDt = DateTime.Now;
                                objPrice.ApprovedBy = User.Identity.Name;
                                _dataModel.tbl_inven_item_price_details.AddObject(objPrice);
                                _dataModel.SaveChanges();
                            }
                        }
                        _dataModel.SaveChanges();
                        return RedirectToAction("Index");
                    }
                }
                return View(invMstData);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(invMstData);
            }
        }

        public ActionResult Details(int id)
        {
            var invItemDet = _dataModel.InventoryItemsMasters.Where(a => a.ID == id).FirstOrDefault();
            return View(invItemDet);
        }

        private bool ValidateForm(InventoryItemsMaster profile, bool isCreate)
        {
            if (string.IsNullOrEmpty(profile.ItemText))
                ModelState.AddModelError("ItemText", "Item name is required.");
            
            else if (isCreate)
            {
                if (string.IsNullOrEmpty(profile.ItemCode))
                    ModelState.AddModelError("ItemCode", "Item code is required.");
                // Verify duplicate item code 
                if (VerifyItemCode(profile.ItemCode.Trim().ToUpper()))
                    ModelState.AddModelError("ItemCode", "Item code has already exists");
            }

            if (profile.ItemCatId == 0 || profile.ItemCatId == null)
                ModelState.AddModelError("ItemCatId", "Item category is required.");
            if (profile.IsItem == null)
                ModelState.AddModelError("IsItem", "Item type is required.");
            //if (profile.Amount == 0 || profile.Amount == null)
            //    ModelState.AddModelError("Amount", "Price is required.");
            if (profile.UnitId == 0 || profile.UnitId == null)
                ModelState.AddModelError("UnitId", "UOM is required.");
            if (isCreate == true)
            {
                // check duplicate Item category name insertion
                var isduplicate = _dataModel.InventoryItemsMasters.Where(a => a.ItemText.Trim().ToUpper() == profile.ItemText.Trim().ToUpper() && a.ItemCatId != profile.ItemCatId).Any();
                if (isduplicate == true)
                    ModelState.AddModelError("ItemText", " '" + profile.ItemText + "' Item name has already exists.");
            }
            return ModelState.IsValid;
        }
        public ActionResult Delete(int id)
        {
            var invMstDataToDelete = _dataModel.InventoryItemsMasters.SingleOrDefault(m => m.ID == id);
            try
            {
                _dataModel.DeleteObject(invMstDataToDelete);
                _dataModel.SaveChanges();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMag"] = "This '" + invMstDataToDelete.ItemText + "' Item name has already had relation with another table.You cannot be deleted.";
                return RedirectToAction("Index");
            }

        }

        public bool VerifyItemCode(string _itemCode)
        {
            return _dataModel.InventoryItemsMasters.Where(a => a.ItemCode.Trim().ToUpper() == _itemCode && a.Active == true).Any();
        }

        [HttpGet]
        public ActionResult AddItemPartial(int ItemType)
        {
            ViewBag.ItemCategory = new SelectList(_dataModel.InventoryItemCategories.Where(a => a.Active == true), "ID", "ItemCatText");
            ViewBag.Units = new SelectList(_dataModel.InventoryUnitsMasters.Where(a => a.Active == true), "ID", "UnitsText");
            ViewBag.IsItem = ItemType;
            ViewBag.ItemCodes = objCr.GetInvItemCodes();
            return PartialView("_AddItemPartial", new InventoryItemsMaster());
        }

        [HttpPost]
        public ActionResult AddItemPartial(FormCollection frm, InventoryItemsMaster InvItemsMaster)
        {
            string ItemType = frm["IsItem"] == "1" ? "Item" : "Service";
            try
            {
                bool IsMessItem = Convert.ToBoolean(frm["MessItem"]);
                bool InSummary = Convert.ToBoolean(frm["InSummary"]);
                if (IsMessItem == true)
                    InvItemsMaster.MessItem = true;
                else
                    InvItemsMaster.MessItem = false;

                if (InSummary == true)
                    InvItemsMaster.InSummary = true;
                else
                    InvItemsMaster.InSummary = false;

                InvItemsMaster.Active = true;
                InvItemsMaster.EffectedFrom = DateTime.Now;
                InvItemsMaster.IsItem = Convert.ToInt32(frm["IsItem"]);
                InvItemsMaster.Amount = 0;
                objCr.ConvertToUppercase(InvItemsMaster);
                _dataModel.AddToInventoryItemsMasters(InvItemsMaster);
                _dataModel.SaveChanges();
                
                // Add Price Details
                tbl_inven_item_price_details objPrice = new tbl_inven_item_price_details();
                objPrice.ItemId = InvItemsMaster.ID;
                objPrice.EffectFromDt = DateTime.Now;
                objPrice.EffectToDt = null;
                objPrice.Amount = InvItemsMaster.Amount;
                objPrice.ApprovedDt = DateTime.Now;
                objPrice.ApprovedBy = User.Identity.Name;
                objCr.ConvertToUppercase(objPrice);
                _dataModel.tbl_inven_item_price_details.AddObject(objPrice);
                _dataModel.SaveChanges();
            }
            catch
            {
                return Json(new { success = false, ItemType = ItemType, msg = "An error occured while proccesing your request." });
            }
            return Json(new { success = true , ItemType = ItemType , msg = "Item has been added successfully." });
        }


    }
}
