using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FMS
{
    public class ApplicationSettings
    {
        public static string AppName = System.Configuration.ConfigurationManager.AppSettings["AppName"]; 
        public static string OrgName = System.Configuration.ConfigurationManager.AppSettings["OrgName"];
        public static string Address1 = System.Configuration.ConfigurationManager.AppSettings["Address1"];
        public static string Address2 = System.Configuration.ConfigurationManager.AppSettings["Address2"];
        public static string Phone = System.Configuration.ConfigurationManager.AppSettings["Phone"];
        public static string Email = System.Configuration.ConfigurationManager.AppSettings["Email"];
    }
}