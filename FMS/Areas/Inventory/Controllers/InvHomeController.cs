using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FMS.Areas.Inventory.Controllers
{
    [CustomAuthorizationFilter]
    public class InvHomeController : Controller
    {
        //
        // GET: /Inventory/Home/

        public ActionResult Index()
        {
            return View();
        }

    }
}
