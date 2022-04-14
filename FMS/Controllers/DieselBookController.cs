using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;
using System.IO;
using System.Data.SqlClient;
using ClosedXML.Excel;
using System.Data;
using System.Data.OleDb;
using System.Web.Configuration;
using AsyncUploaderDemo.Models;
using FMS.Helpers;

namespace FMS.Controllers
{
    [CustomAuthorizationFilter]
    public class DieselBookController : Controller
    {
        #region ctors
        private FMSDBEntities db;
        private core objCore = new core();
        private List<tbl_diesel_books> _dieselBookList = new List<tbl_diesel_books>();
        public DieselBookController()
        {
            db = new FMSDBEntities();
        } 
        #endregion

        #region Index Diesel Book
        // GET: /DieselBook/
        [Authorize]
        public ActionResult Index()
        {
            return View();
        }
        public JsonResult GetDieselBook(int iDisplayStart, int iDisplayLength, string sSearch, int iColumns,
      int iSortingCols, int iSortCol_0, string sSortDir_0,
      int sEcho, string mDataProp_Key)
        {
            var DieselbookList = GetDieselBookList();
            var filtereddieselbookDet = DieselbookList
                .Select(l => new[]{
                    l.Id.ToString(),
                    l.Text1.ToString(),
                    l.Text2.ToString(),
                    l.Text3.ToString(),
                    l.Value1.ToString(),
                    l.Amount.ToString(),
                    l.Text4.ToString(),
                    l.Status.ToString()
                    
                }).Where(i => string.IsNullOrEmpty(sSearch)
                     || i.Any(y => y.IndexOf(sSearch, StringComparison.InvariantCultureIgnoreCase) >= 0));
            var orderedlist = filtereddieselbookDet
                   .OrderByWithDirection(i => (i[iSortCol_0]).Parse(), sSortDir_0 == "asc")
                   .Skip(iDisplayStart)
                   .Take(iDisplayLength);
            var model = new
            {
                aaData = orderedlist,
                iTotalDisplayRecords = filtereddieselbookDet.Count(),
                iTotalRecords = filtereddieselbookDet.Count(),
                sEcho = sEcho.ToString()
            };
            return Json(model, JsonRequestBehavior.AllowGet);
        }
        private IEnumerable<CommonFields> GetDieselBookList()
        {
            var list = new List<CommonFields>();
            var tbl_diesel_books_list = db.tbl_diesel_books.Where(a => a.Status == true).ToList();
            foreach (tbl_diesel_books ls in tbl_diesel_books_list)
            {
                list.Add(new CommonFields
                {
                    Id = ls.ID,
                    Text1 = ls.tbl_fuel_stations.FuelStation.ToUpper(),
                    Text2 = ls.BookNo.ToString() == null ? "" : ls.BookNo.ToString(),
                    Text3 = ls.TokenNumber == null ? "" : ls.TokenNumber,
                    Value1 = ls.TokenValue == null ? 0 : (double)ls.TokenValue,
                    Amount = ls.PricePerLiter == null ? 0 : (decimal)ls.PricePerLiter,
                    Text4 = ls.Site == null ? "" : ls.Site,
                    Status = (bool)ls.Status
                });
            }
            objCore.ConvertToUppercase<CommonFields>(list);
            return list.OrderBy(a => a.Id);
        } 
        #endregion

        #region Add Diesel Book
        [Authorize]
        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Create(FormCollection frm)
        {
            try
            {
                // List of Diesel Book 
                _dieselBookList = (List<tbl_diesel_books>)Session["DieselBook"];
                objCore.ConvertToUppercase<tbl_diesel_books>(_dieselBookList);
                foreach (tbl_diesel_books obj in _dieselBookList)
                    db.tbl_diesel_books.AddObject(obj);
                db.SaveChanges();
                Session["DieselBook"] = null;
                objCore.LoggingEntries("Masters", "New Diesel Book(s) were created", User.Identity.Name);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Message = "An Error occured :" + ex.Message.ToString();
                return View();
            }
        }
        [HttpGet]
        public ActionResult AddNewDieselBookDetails()
        {
            ViewBag.FuelStationID = objCore.LoadDieselBook();
            return PartialView("_AddNewDieselBookDetails");
        }
        [HttpPost]
        public ActionResult AddNewDieselBookDetails(long FuelStationID, FormCollection frm, tbl_diesel_books dieselBook)
        {
            dieselBook.FuelStationID = FuelStationID;
            ViewBag.FuelStationID = objCore.LoadDieselBook();
            long ID = 0;
            _dieselBookList = (List<tbl_diesel_books>)Session["DieselBook"];
            if (_dieselBookList == null)
                _dieselBookList = new List<tbl_diesel_books>();
            if (_dieselBookList.Count() == 0)
                ID = 1;
            else
                ID = _dieselBookList.Count() + 1;
            _dieselBookList.Add(new tbl_diesel_books { ID = ID, FuelStationID = dieselBook.FuelStationID, BookNo = dieselBook.BookNo, TokenNumber = dieselBook.TokenNumber, TokenValue = dieselBook.TokenValue, PricePerLiter = dieselBook.PricePerLiter, CreatedDate = DateTime.Now.Date, CreatedBy = User.Identity.Name, Status = true, Site = dieselBook.Site });
            Session["DieselBook"] = _dieselBookList;
            return PartialView("_DieselBookList", _dieselBookList);
        }
        #endregion

        #region Edit Diesel Book
        [HttpGet]
        public ActionResult Edit(long Id)
        {
            tbl_diesel_books _diselBook = db.tbl_diesel_books.Where(a => a.ID == Id && a.Status == true).FirstOrDefault();
            ViewBag.FuelStationID = new SelectList(db.tbl_fuel_stations.Where(a => a.Status == true), "ID", "FuelStation", _diselBook.FuelStationID).ToList();
            return PartialView("_EditDieselBook", _diselBook);
        }
        [HttpPost]
        public ActionResult Edit(long Id, FormCollection frm, tbl_diesel_books dieselBookDet)
        {
            tbl_diesel_books _diselBook = db.tbl_diesel_books.Where(a => a.ID == Id && a.Status == true).FirstOrDefault();
            ViewBag.FuelStationID = new SelectList(db.tbl_fuel_stations.Where(a => a.Status == true), "ID", "FuelStation", _diselBook.FuelStationID).ToList();
            try
            {
                TryUpdateModel(_diselBook);
                objCore.ConvertToUppercase(_diselBook);
                db.SaveChanges();
                objCore.LoggingEntries("Masters", "Diesel Book has updated ", User.Identity.Name);
                return Content("<script language='javascript' type='text/javascript'>$.modal.close();alert('Diesel book saved successfully');window.location.href = '/DieselBook/Index'</script>");
            }
            catch (Exception ex)
            {
                ViewBag.Message = "An Error occured ==>" + ex.Message.ToString();
                return PartialView("_EditDieselBook", _diselBook);
            }
        } 
        #endregion

        #region Delete
        [HttpGet]
        public ActionResult Delete(long Id)
        {
            tbl_diesel_books dieselDet = db.tbl_diesel_books.Where(a => a.ID == Id && a.Status == true).FirstOrDefault(); ;
            try
            {
                TryUpdateModel(dieselDet);
                dieselDet.Status = false;
                db.SaveChanges();
                objCore.LoggingEntries("Masters", "Diesel Book has deleted ", User.Identity.Name);
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                return View("Error");
            }
        } 
        #endregion

    }
}
