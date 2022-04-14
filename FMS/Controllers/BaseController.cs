using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using ClosedXML.Excel;
using System.Data;
using System.Data.SqlClient;
using System.Web.Configuration;
using System.Data.OleDb;
using FMS.Models;
using AsyncUploaderDemo.Models;


namespace FMS.Controllers
{
    public partial class BaseController : Controller
    {

        private core objCore = new core();
        private FMSDBEntities db;
        private IFileStore _fileStore = new DiskFileStore();
        public string AsyncUpload()
        {
            return _fileStore.SaveUploadedFile(Request.Files[0]);
        }
        public BaseController()
        {
            db = new FMSDBEntities(); 
        }
        /// <summary>
        /// This provides simple feedback to the modelstate in the case of errors.
        /// </summary>
        /// <param name="filterContext"></param>
        protected override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            if (filterContext.Result is RedirectToRouteResult)
            {
                try
                {
                    // put the ModelState into TempData
                    TempData.Add("_MODELSTATE", ModelState);
                }
                catch (Exception)
                {
                    TempData.Clear();
                    // swallow exception
                }
            }
            else if (filterContext.Result is ViewResult && TempData.ContainsKey("_MODELSTATE"))
            {
                // merge modelstate from TempData
                var modelState = TempData["_MODELSTATE"] as ModelStateDictionary;
                foreach (var item in modelState)
                {
                    if (!ModelState.ContainsKey(item.Key))
                        ModelState.Add(item);
                }
            }
            base.OnActionExecuted(filterContext);
        }
       
    }
}
