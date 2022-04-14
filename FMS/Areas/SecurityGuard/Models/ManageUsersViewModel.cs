using System.Web.Security;
using SecurityGuard.Core.Pagination;
using FMS.Models;
using FMS.Areas.SecurityGuard;
using System.Collections.Generic;

namespace SecurityGuard.ViewModels
{
    public class ManageUsersViewModel
    {
        public MembershipUserCollection Users { get; set; }
        public PaginatedList<MembershipUser> PaginatedUserList { get; set; }

        public PaginatedList<aspnet_Roles> RolesList { get; set; }

        public PaginatedList<aspnet_Users> AspnetUserReportList { get; set; }

        //public PaginatedList<aspnet_users_view> AspnetUserView { get; set; }
        public string FilterBy { get; set; }
        public string SearchTerm { get; set; }
        public int PageSize { get; set; }
        public int? TableID { get; set; }
    }

    public class ManageUserPermissions
    {
        public string userName { get; set; }
        public List<tbl_pages> pageList { get; set; }
        
    }
}
