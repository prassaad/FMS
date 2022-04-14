using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;

namespace FMS.Controllers
{
    public class IpRulesController : Controller
    {
        #region ctors
        FMSDBEntities con;
        public IpRulesController()
        {
            con = new FMSDBEntities();
        } 
        #endregion

        public ActionResult Index()
        {
            try
            {
                return View(con.IPRules.ToList());
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #region Add IP Rules
        [HttpGet]
        public ActionResult Create()
        {
            return View();

        }
        [HttpPost]
        public ActionResult Create(IPRule iprule)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    con.IPRules.AddObject(iprule);
                    con.SaveChanges();
                    return RedirectToAction("Index");
                }
                return View(iprule);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        } 
        #endregion

        #region Edit IP Rules
        [HttpGet]
        public ActionResult Edit(int id)
        {
            try
            {

                var data = (from n in con.IPRules where n.Id == id select n).First();
                return View(data);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        [HttpPost]
        public ActionResult Edit(IPRule iprule)
        {
            try
            {
                var data = (from n in con.IPRules where n.Id == iprule.Id select n).First();
                if (!ModelState.IsValid)
                {
                    return View(data);
                }
                else
                {

                    con.ApplyCurrentValues(data.EntityKey.EntitySetName, iprule);
                    con.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        } 
        #endregion

        #region Delete IP Rules
        public ActionResult Delete(long id)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var data = con.IPRules.First(e => e.Id == id);
                    con.DeleteObject(data);
                    con.SaveChanges();
                    return RedirectToAction("Index");
                }
                else
                {
                    return View(id);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        } 
        #endregion

    }
}
