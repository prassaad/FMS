using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.Security;

namespace SecurityGuard.ViewModels
{
    public class GrantRolesToUserViewModel
    {
        public MembershipUser User { get; set; }
        public string UserName { get; set; }
        public SelectList AvailableRoles { get; set; }
        public SelectList GrantedRoles { get; set; }
    }
    public class GrantLinkstoRoles
    {
        public string roleName { get; set; }
        public SelectList AvailableRoles { get; set; }
        public SelectList GrantedRoles { get; set; }
    }
    public class GrantLinkstoUsersViewModel
    {
        public MembershipUser User { get; set; }
        public string UserName { get; set; }
        public SelectList AvailableRoles { get; set; }
        public SelectList GrantedRoles { get; set; }
    }

    public class GrantAccessToRolesViewModel 
    {
        public MembershipUser User { get; set; }
        public string RoleName { get; set; }
        public SelectList AvailablePages { get; set; }
        public SelectList GrantedPages { get; set; }
    }
}
