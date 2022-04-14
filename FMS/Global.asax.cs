using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using SecurityGuard.Interfaces;
using SecurityGuard.Services;
using System.Web.Security;
using FMS.Models;

namespace FMS
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {

        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                "Default", // Route name
                "{controller}/{action}/{id}", // URL with parameters
                new { controller = "Account", action = "Authorized", id = UrlParameter.Optional } // Parameter defaults
            );
        }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RegisterGlobalFilters(GlobalFilters.Filters);
            System.Web.Http.GlobalConfiguration.Configure(WebApiConfig.Register);
            RegisterRoutes(RouteTable.Routes);
            
            // Register Custom Exception handler
            //GlobalFilters.Filters.Add(new ExceptionHandlerFilter());

            //System.Timers.Timer timer = new System.Timers.Timer();
            ////Set interval of repeated execution in millisecond
            //timer.Interval = 3600000;  // For Every One Hour
            ////set name of the method to be called
            //timer.Elapsed += GetAllOpenJobCards;
            //timer.Start();
            //// Add Timer to Application
            //Application.Add("timer", timer);


        }

        void Session_Start(object sender, EventArgs e)
        {
            if (Session["UserName"] == null)
            {
                Response.Redirect("~/Account/LogOn");
            }
        }

        static void GetAllOpenJobCards(object sender, System.Timers.ElapsedEventArgs e)
        {
            FMSDBEntities db = new FMSDBEntities(); 
            List<tbl_jobcard> _jobCardList = db.tbl_jobcard.Where(a => a.Status == "Open").ToList();
            DateTime Today = DateTime.Now;
            DateTime JobCardDt;
            foreach (var item in _jobCardList)
            {
                JobCardDt = item.CreatedDt.Value;
                if (Today >= JobCardDt.AddHours(5))
                {
                    tbl_jobcard jobcardDet = db.tbl_jobcard.Where(a => a.Id == item.Id).SingleOrDefault();
                    jobcardDet.Status = JobCardStatus.Pending.ToString();
                    db.SaveChanges();
                }
            }
        }
    }
    public class ExceptionHandlerFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext filterContext) 
        {
            Exception ex = filterContext.Exception;
            if (ex != null)
            {
                core.ExceptionLogging(ex.Message.ToString(), HttpContext.Current.Session["UserName"].ToString(), ex.StackTrace.ToString());
                filterContext.Result = new RedirectResult("~/Account/Error?ex=" + ex.Message.ToString());
            }
        }
    }
    public class CustomAuthorizationFilter : AuthorizeAttribute
    {
        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            if (filterContext.IsChildAction == false)
            {
                if (filterContext.HttpContext.Request.IsAuthenticated == false)
                {
                    filterContext.Result = new RedirectResult("~/Account/LogOn");
                }
                if (filterContext.HttpContext.Session["UserName"] == null)
                {
                    filterContext.Result = new RedirectResult("~/Account/LogOn");
                }
            }
        }
    }
     
}