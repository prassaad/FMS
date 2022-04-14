using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.Security;
using FMS.Models;
using SecurityGuard.Core.Extensions;
using SecurityGuard.Services;
using SecurityGuard.Core.Attributes;
using routeHelpers = SecurityGuard.Core.RouteHelpers;
using SecurityGuard.Interfaces;
using SecurityGuard.ViewModels;
using FMS.Controllers;
using System.Linq;
using viewModels = FMS.Areas.SecurityGuard.ViewModels;


namespace FMS.Areas.SecurityGuard.Controllers
{
    [Authorize(Roles = "superadmin")]
    public partial class MembershipController : BaseController
    {
        private core objCore = new core(); 

        #region ctors

        private IMembershipService membershipService;
        private readonly IRoleService roleService;
        private FMSDBEntities  db = new FMSDBEntities();
        public MembershipController()
        {
            this.roleService = new RoleService(Roles.Provider);
            this.membershipService = new MembershipService(Membership.Provider);
        }

        #endregion

        #region Index Method
        public  ActionResult Index(string filterby, string searchterm)
        {
            ManageUsersViewModel viewModel = new ManageUsersViewModel();
            viewModel.Users = null;
            viewModel.FilterBy = filterby;
            viewModel.SearchTerm = searchterm;
            int totalRecords;
            int page = Convert.ToInt32(Request["page"]);
            int pageSize = Convert.ToInt32(Request["pagesize"]);
            if (pageSize == 0)
                pageSize = 25;

            viewModel.PageSize = pageSize;

            if (!string.IsNullOrEmpty(filterby))
            {
                if (filterby == "all")
                {
                    
                   viewModel.PaginatedUserList = membershipService.GetAllUsers(page, pageSize, out totalRecords).ToPaginatedList(page, pageSize, totalRecords, searchterm, filterby);
                }
                else if (!string.IsNullOrEmpty(searchterm))
                {
                    string query = searchterm + "%";
                    if (filterby == "email")
                    {
                        viewModel.PaginatedUserList = membershipService.FindUsersByEmail(query, page, pageSize, out totalRecords).ToPaginatedList(page, pageSize, totalRecords, searchterm, filterby);
                    }
                    else if (filterby == "username")
                    {
                        viewModel.PaginatedUserList = membershipService.FindUsersByName(query, page, pageSize, out totalRecords).ToPaginatedList(page, pageSize, totalRecords, searchterm, filterby);
                    }
                }
            }

            return View(viewModel);
        }
        #endregion

        #region Create User Methods

        public virtual ActionResult CreateUser()
        {
            var model = new viewModels.RegisterViewModel()
            {
                RequireSecretQuestionAndAnswer = membershipService.RequiresQuestionAndAnswer
            };
         
            
            return View(model);
        }

        /// <summary>
        /// This method redirects to the GrantRolesToUser method.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public virtual ActionResult CreateUser(viewModels.RegisterViewModel model)
        {
            MembershipUser user;
            MembershipCreateStatus status;
            user = membershipService.CreateUser(model.UserName, model.Password, model.Email, model.SecretQuestion, model.SecretAnswer, model.Approve, out status);

            objCore.LoggingEntries("User Mgmt.", "New User was Created with username " + model.UserName + "", User.Identity.Name);
            return routeHelpers.Actions.GrantRolesToUser(user.UserName);
        }

       

        /// <summary>
        /// An Ajax method to check if a username is unique.
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult CheckForUniqueUser(string userName)
        {
            MembershipUser user = membershipService.GetUser(userName);
            JsonResponse response = new JsonResponse();
            response.Exists = (user == null) ? false : true;

            return Json(response, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Delete User Methods

        [HttpPost]
        [MultiButtonFormSubmit(ActionName = "UpdateDeleteCancel", SubmitButton = "DeleteUser")]
        public ActionResult DeleteUser(string UserName)
        {
            if (string.IsNullOrEmpty(UserName))
            {
                throw new ArgumentNullException("userName");
            }
            core objCore = new core(); 
            try
            {
                //Delete user Access

                objCore.GetUserAccessListByUser(UserName);

                membershipService.DeleteUser(UserName);
                objCore.LoggingEntries("User Mgmt.", "User was deleted with username " + UserName + "", User.Identity.Name);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "There was an error deleting this user. - " + ex.Message;
            }

            return RedirectToAction("Update", new { userName = UserName });
        }



        #endregion

        #region View User Details Methods

        [HttpGet]
        public ActionResult Update(string userName)
        {
            MembershipUser user = membershipService.GetUser(userName);

            UserViewModel viewModel = new UserViewModel();
            viewModel.User = user;
            viewModel.RequiresSecretQuestionAndAnswer = membershipService.RequiresQuestionAndAnswer;
            viewModel.Roles = roleService.GetRolesForUser(userName);

            return View(viewModel);
        }

        [HttpPost]
        //[ActionName("Update")]
        [MultiButtonFormSubmit(ActionName = "UpdateDeleteCancel", SubmitButton = "UpdateUser")]
        public ActionResult UpdateUser(string UserName)
        {
            if (string.IsNullOrEmpty(UserName))
            {
                throw new ArgumentNullException("userName");
            }

            MembershipUser user = membershipService.GetUser(UserName);

            try
            {
                user.Comment = Request["User.Comment"];
                user.Email = Request["User.Email"];

                membershipService.UpdateUser(user);
                TempData["SuccessMessage"] = "The user was updated successfully!";
                objCore.LoggingEntries("User Mgmt.", "User was updated with username " + UserName + "", User.Identity.Name);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "There was an error updating this user.";
            }

            return RedirectToAction("Update", new { userName = user.UserName });
        }


        #region Ajax methods for Updating the user

        [HttpPost]
        public ActionResult Unlock(string userName)
        {
            JsonResponse response = new JsonResponse();

            MembershipUser user = membershipService.GetUser(userName);

            try
            {
                user.UnlockUser();
                response.Success = true;
                response.Message = "User unlocked successfully!";
                response.Locked = false;
                response.LockedStatus = (response.Locked) ? "Locked" : "Unlocked";
            }
            catch (Exception)
            {
                response.Success = false;
                response.Message = "User unlocked failed.";
            }

            return Json(response);
        }

        [HttpPost]
        public ActionResult ApproveDeny(string userName)
        {
            JsonResponse response = new JsonResponse();

            MembershipUser user = membershipService.GetUser(userName);

            try
            {
                user.IsApproved = !user.IsApproved;
                membershipService.UpdateUser(user);

                string approvedMsg = (user.IsApproved) ? "Approved" : "Denied";

                response.Success = true;
                response.Message = "User " + approvedMsg + " successfully!";
                response.Approved = user.IsApproved;
                response.ApprovedStatus = (user.IsApproved) ? "Approved" : "Not approved";
            }
            catch (Exception)
            {
                response.Success = false;
                response.Message = "User unlocked failed.";
            }

            return Json(response);
        }

        #endregion

        #endregion

        #region Cancel User Methods

        [HttpPost]
        [MultiButtonFormSubmit(ActionName = "UpdateDeleteCancel", SubmitButton = "UserCancel")]
        public ActionResult Cancel()
        {
            return RedirectToAction("Index");
        }

        #endregion

        #region Grant Users with Roles Methods

        /// <summary>
        /// Return two lists:
        ///   1)  a list of Roles not granted to the user
        ///   2)  a list of Roles granted to the user
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public virtual ActionResult GrantRolesToUser(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Index");
            }

            GrantRolesToUserViewModel model = new GrantRolesToUserViewModel();
            model.UserName = username;
            model.AvailableRoles = (string.IsNullOrEmpty(username) ? new SelectList(roleService.GetAllRoles()) : new SelectList(roleService.AvailableRolesForUser(username)));
            model.GrantedRoles = (string.IsNullOrEmpty(username) ? new SelectList(new string[] { }) : new SelectList(roleService.GetRolesForUser(username)));

            return View(model);
        }

        /// <summary>
        /// Grant the selected roles to the user.
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="roleNames"></param>
        /// <returns></returns>
        [HttpPost]
        public virtual ActionResult GrantRolesToUser(string userName, string roles)
        {
            JsonResponse response = new JsonResponse();

            if (string.IsNullOrEmpty(userName))
            {
                response.Success = false;
                response.Message = "The User Name is missing.";
                return Json(response);
            }

            string[] roleNames = roles.Substring(0, roles.Length - 1).Split(',');

            if (roleNames.Length == 0)
            {
                response.Success = false;
                response.Message = "No roles have been granted to the user.";
                return Json(response);
            }

            try
            {
                roleService.AddUserToRoles(userName, roleNames);

                response.Success = true;
                response.Message = "The Role(s) has been GRANTED successfully to " + userName;
                objCore.LoggingEntries("User Mgmt.", "Assigned role to user with username " + userName + " and role is " + roles , User.Identity.Name);
            }
            catch (Exception)
            {
                response.Success = false;
                response.Message = "There was a problem adding the user to the roles.";
            }
           
            return Json(response);
        }

        /// <summary>
        /// Revoke the selected roles for the user.
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="roleNames"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RevokeRolesForUser(string userName, string roles)
        {
            JsonResponse response = new JsonResponse();

            if (string.IsNullOrEmpty(userName))
            {
                response.Success = false;
                response.Message = "The userName is missing.";
                return Json(response);
            }

            if (string.IsNullOrEmpty(roles))
            {
                response.Success = false;
                response.Message = "Roles is missing";
                return Json(response);
            }

            string[] roleNames = roles.Substring(0, roles.Length - 1).Split(',');

            if (roleNames.Length == 0)
            {
                response.Success = false;
                response.Message = "No roles are selected to be revoked.";
                return Json(response);
            }

            try
            {
                roleService.RemoveUserFromRoles(userName, roleNames);

                response.Success = true;
                response.Message = "The Role(s) has been REVOKED successfully for " + userName;
                objCore.LoggingEntries("User Mgmt.", "Deleted role from username " + userName + "", User.Identity.Name);
            }
            catch (Exception)
            {
                response.Success = false;
                response.Message = "There was a problem revoking roles for the user.";
            }
            return Json(response);
        }

        #endregion

        #region Grant Permissions to user
        public ActionResult GrantPermissionsToUser(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Index", "MemberShip");
            }
            ManageUserPermissions model = new ManageUserPermissions();
            model.userName = username;
            model.pageList = db.tbl_pages.Where(a => a.Status == true).OrderBy(a => a.PageName).ToList();
            return View(model);
        }


        [HttpPost]
        public virtual ActionResult GrantPermissionsToUser(FormCollection frm) 
        {
            JsonResponse response = new JsonResponse();
            string userName = frm["UserName"];
            if (string.IsNullOrEmpty(userName))
            {
                response.Success = false;
                response.Message = "The userName is missing.";
                return Json(response);
            }

            try
            {
                var pageList = db.tbl_pages.Where(a => a.Status == true).ToList();
                var userDet = db.aspnet_Users.Where(a=>a.UserName.Trim().ToUpper() == userName.Trim().ToUpper()).FirstOrDefault();

                for (int i = 0; i < pageList.Count; i++)
                {
                    bool isChecked = frm.GetValues("chkPage_" + pageList[i].ID).Contains("true"); 
                    bool isAdd = frm.GetValues("chkAdd_" + pageList[i].ID).Contains("true"); 
                    bool isEdit = frm.GetValues("chkEdit_" + pageList[i].ID).Contains("true"); 
                    bool isDelete = frm.GetValues("chkDelete_" + pageList[i].ID).Contains("true"); 
                    bool isView = frm.GetValues("chkView_" + pageList[i].ID).Contains("true"); 
                    bool isAudit = frm.GetValues("chkAudit_" + pageList[i].ID).Contains("true"); 
                    bool isList = frm.GetValues("chkList_" + pageList[i].ID).Contains("true");
                    long PageId = Convert.ToInt64(pageList[i].ID);
                    if (isChecked)
                    {
                        if (!VerifyUserPermission(userName, PageId))
                        {
                            tbl_user_access objUserAccess = new tbl_user_access();
                            objUserAccess.PageID = pageList[i].ID;
                            objUserAccess.Status = true;
                            objUserAccess.UserID = userDet.UserId;
                            objUserAccess.Add = isAdd;
                            objUserAccess.Edit = isEdit;
                            objUserAccess.Delete = isDelete;
                            objUserAccess.Audit = isAudit;
                            objUserAccess.List = isList;
                            objUserAccess.View = isView;
                            db.tbl_user_access.AddObject(objUserAccess);
                        }
                        else
                        {
                            tbl_user_access userAccess = db.tbl_user_access.Where(a => a.aspnet_Users.UserName.ToUpper() == userName.Trim().ToUpper() && a.PageID == PageId).FirstOrDefault();
                            TryUpdateModel(userAccess);
                            userAccess.Add = isAdd;
                            userAccess.Edit = isEdit;
                            userAccess.Delete = isDelete;
                            userAccess.Audit = isAudit;
                            userAccess.List = isList;
                            userAccess.View = isView;

                        }
                    }
                    else if (VerifyUserPermission(userName, PageId))
                    {
                        tbl_user_access userAccess = db.tbl_user_access.Where(a => a.aspnet_Users.UserName.Trim().ToUpper() == userName.Trim().ToUpper() && a.PageID == PageId).FirstOrDefault();
                        TryUpdateModel(userAccess);
                        userAccess.Status = false;
                    }

                }
                db.SaveChanges();
                response.Success = true;
                response.Message = "The User permission has been GRANTED successfully to " + userName;
                objCore.LoggingEntries("User Mgmt.", "Assigned user access to the username " + userName + "", User.Identity.Name);
            }
            catch (Exception)
            {
                response.Success = false;
                response.Message = "There was a problem adding the user permission";
            }

            return Json(response);
        }

        public bool VerifyUserPermission(string userName,long PageId)
        {
            if (db.tbl_user_access.Where(a => a.aspnet_Users.UserName.Trim().ToUpper() == userName.Trim().ToUpper() && a.Status == true &&a.PageID == PageId).Any())
                return true;
            else
                return false;
        }

        public tbl_user_access VerifyUserAccess(long PageId,string userName)
        {
            tbl_user_access userAccess = db.tbl_user_access.Where(a => a.PageID == PageId && a.Status == true && a.aspnet_Users.UserName.Trim().ToUpper() == userName.Trim().ToUpper()).FirstOrDefault();
            return userAccess;
        }

        public bool VerifyCheckAll(long PageId, string userName)
        {
            tbl_user_access userAccess = db.tbl_user_access.Where(a => a.PageID == PageId && a.Status == true && a.aspnet_Users.UserName.Trim().ToUpper() == userName.Trim().ToUpper()).FirstOrDefault();
            if ((bool)userAccess.Add) { if ((bool)userAccess.List) { if ((bool)userAccess.Edit) { if ((bool)userAccess.Delete) { if ((bool)userAccess.View) { if ((bool)userAccess.Audit) { return true; } } } } } } 
            return false;
        }


        #endregion

        #region ResetPassword
        public ActionResult ResetPassowrd(string userName)
        {
            if (Membership.GetUser(userName) == null)
                return Json(new { success = false, msg = "UserName does not exists." });
            MembershipUser u = Membership.GetUser(userName, false);
            string newPassword = string.Empty;
            try
            {
                newPassword = u.ResetPassword();
                if (u.Email != null || u.Email != "")
                    new core().Sendmail(u.Email, userName, newPassword);
                else
                    return Content("<script> alert('Please check whether your mail is configure or not.'); window.location.href='/SecurityGuard/Membership/index'; </script>"); //Json(new { success = false, msg = "Please check whether your mail is configure or not." });
                objCore.LoggingEntries("User Mgmt.", "Password has been reset for the user " + userName + "", User.Identity.Name);
                return Content("<script> alert('Password was reset successfully.Password will sent to your mail.'); window.location.href='/SecurityGuard/Membership/index'; </script>"); //Json(new { success = true, msg = "Password was reset successfully.Password will sent to your mail." });
            }
            catch (Exception)
            {
                return Content("<script> alert('Please check whether your mail is configure or not.or else contact administrator.'); window.location.href='/SecurityGuard/Membership/index'; </script>");  //Json(new { success = true, msg = "Please check whether your mail is configure or not.or else contact administrator." });
            }
        }
        #endregion

        public int VerifyIfUserSuperadmin(string userName)
        {
            string[] m = roleService.GetRolesForUser(userName);
            foreach (string role in m)
            {
                if (role == "superadmin")
                {
                    return 1;
                }
            }
            return 0;
        }
    }
}
