using FMS.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Script.Serialization;
using System.Web.Security;

namespace FMS.Controllers
{
    public class UsersController : ApiController
    {
        private FMSDBEntities db;
        public core core = new core();
        public UsersController()
        {
            db = new FMSDBEntities();
        }
        // GET: api/VerifyDriverById
        [Route("api/users/logon")]
        [HttpPost]
        public IHttpActionResult logon([FromBody] Web.ApiModels.UserDTO request)
        {

            try
            {
                if (Membership.ValidateUser(request.username, request.password))
                {

                    //tbl_drivers driverDet = db.tbl_drivers.Where(a => a.Status == true && a.ID == id).SingleOrDefault();
                    //var data = new { ID = driverDet.ID, Name = driverDet.FirstName };
                    //return Json(new { success = true, msg = data });
                    aspnet_Users userDet = db.aspnet_Users.Where(u => u.UserName == request.username).SingleOrDefault();

                    var userid = userDet.UserId.ToString();
                    // Vendor data top level
                    tbl_vendor_details vendorDet = db.tbl_vendor_details.Where(a => a.Status == true && a.UserId == userid).SingleOrDefault();
                   
                    if(vendorDet == null)
                    {
                        return Json(new { success = true, code="10", msg = "Either Profile is created nor Access and Security Updated" });
                    }

                    var retData = new { 
                        
                        UserId = userDet.UserId,  
                        VendorId = vendorDet.ID,
                        FirstName = vendorDet.FirstName,
                        LastName = vendorDet.LastName,
                        ContactNo = vendorDet.ContactNumber,
                        PanNo = vendorDet.PanCardNumber,
                        ProfileImg = vendorDet.PhotoUrl,
                        IsSubVendor = vendorDet.SubVendor,
                        BankAcctNo = vendorDet.AccountNumber,
                        BankAcctHolderName = vendorDet.AccountHolderName,
                        BankAcctType = vendorDet. AccountType,
                        BankName = vendorDet. BankName,
                        BankBaranch = vendorDet.BankName,
                        BankIFSC = vendorDet.IFSCCode

                    } ;


                    return Json(new { success = true, code = "11", msg = retData });
                }
                else
                {
                    return Json(new { success = false, code = "-1", msg = "Login faild" });
                }

            }
            catch (Exception ex)
            {
                return Json(new { success = false, msg = ex.Message });

            }
        }
    }
}
