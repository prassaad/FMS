using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;

namespace FMS.Controllers
{
    public class AdminSettingsController : Controller
    {
        private FMSDBEntities db;
        public AdminSettingsController()
        {
            db = new FMSDBEntities(); 
        }
        // GET: /AdminSettings/
        public ActionResult Create()
        {
            return View();
        }

        public ActionResult SaveSettings(FormCollection frm)
        {
            List<tbl_settings> lstAcSettings = db.tbl_settings.ToList();
            foreach (var n in lstAcSettings)
            {
                var id = n.Id;
                bool isCheck = frm["Allow_" + id].Contains("true");
                string settingsField = frm["SettingField_" + id];
                tbl_settings dbsettingObj = db.tbl_settings.SingleOrDefault(c => c.Id == id);
                dbsettingObj.Allow = isCheck;
                dbsettingObj.SettingsField = settingsField;
            }
            db.SaveChanges();
            return PartialView("_Success");
        }

        public List<tbl_settings> GetSettings()
        {
            List<tbl_settings> lstSettings;
            lstSettings = db.tbl_settings.ToList();
            return lstSettings;
        }
    }
}
