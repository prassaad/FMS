using System.Collections.Generic;
using System.Web.Mvc;
using FMS.Models;

namespace SecurityGuard.ViewModels
{
    public class ManageRolesViewModel
    {
        public SelectList Roles { get; set; }
        public string[] RoleList { get; set; }
        public List<aspnet_Roles> RoleDetList { get; set; }
    }

}
