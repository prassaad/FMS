using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;
using AsyncUploaderDemo.Models;
using System.IO;
using System.Text;
using FMS.Helpers;
using System.Data.SqlClient;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Collections;
using System.Linq.Expressions;
using System.Diagnostics;

namespace FMS.Controllers
{
    [CustomAuthorizationFilter]
    public class EmployeesController : Controller
    {

        #region ctors
        private FMSDBEntities db;
        private IFileStore _fileStore = new DiskFileStore();
        private core objCore = new core();
        public string AsyncUpload()
        {
            return _fileStore.SaveUploadedFile(Request.Files[0]);
        }
        public EmployeesController()
        {
            db = new FMSDBEntities();
        } 
        #endregion

        #region Index Methods
        public ActionResult Index()
        {
            return View();
        }
        public JsonResult GetEmployees(int iDisplayStart, int iDisplayLength, string sSearch, int iColumns,
  int iSortingCols, int iSortCol_0, string sSortDir_0,
  int sEcho, string mDataProp_Key)
        {
            var EmployeesList = GetEmployeesList();
            var filteredEmployeesDet = EmployeesList
                .Select(l => new[]{
                    l.Id.ToString(),
                    l.Text1.ToString(),
                    l.Text2.ToString(),
                    l.Text3.ToString(),
                    l.Date1.ToShortDateString(),
                    l.Text4.ToString(),
                    l.Text5.ToString(),
                    l.Text6.ToString(),
                    l.Text7.ToString(),
                    l.Text8.ToString(),
                    l.Status.ToString()
                    
                }).Where(i => string.IsNullOrEmpty(sSearch)
                     || i.Any(y => y.IndexOf(sSearch, StringComparison.InvariantCultureIgnoreCase) >= 0));
            var orderedlist = filteredEmployeesDet
                   .OrderByWithDirection(i => (i[iSortCol_0]).Parse(), sSortDir_0 == "asc")
                   .Skip(iDisplayStart)
                   .Take(iDisplayLength);
            var model = new
            {
                aaData = orderedlist,
                iTotalDisplayRecords = filteredEmployeesDet.Count(),
                iTotalRecords = filteredEmployeesDet.Count(),
                sEcho = sEcho.ToString()
            };
            return Json(model, JsonRequestBehavior.AllowGet);
        }
        private IEnumerable<CommonFields> GetEmployeesList()
        {
            var list = new List<CommonFields>();
            var employee = (from a in db.tbl_employees.Where(a => a.Status == true) select a).ToList(); ;
            foreach (tbl_employees ls in employee)
            {
                list.Add(new CommonFields
                {
                    Id = ls.ID,
                    Text1 = ls.EmpNo == null ? "" : ls.EmpNo.ToString(),
                    Text2 = ls.FirstName + " " + ls.LastName,
                    Text3 = ls.Gender,
                    Date1 = Convert.ToDateTime(ls.DOJ),
                    Text4 = ls.tbl_departments.DisplayName,
                    Text5 = ls.tbl_designations.DisplayName,
                    Text6 = ls.Mobile,
                    Text7 = ls.Email,
                    Text8 = ls.UserName,
                    Status = (bool)ls.Status
                });
            }
            objCore.ConvertToUppercase<CommonFields>(list);
            return list.OrderBy(a => a.Id);
        } 
        #endregion

        #region Add Employee
        public ActionResult Create()
        {
            tbl_employees employee = new tbl_employees();
            ViewBag.DesgID = new SelectList(db.tbl_designations.Where(a => a.Status == true), "ID", "DisplayName");
            ViewBag.DeptID = new SelectList(db.tbl_departments.Where(a => a.Status == true), "ID", "DisplayName");
            ViewBag.Gender = new SelectList(new[] {  "Male","Female" }, (employee.Gender == null ? null : employee.Gender.ToString().PadRight(7).ToCharArray()));
            return View();
        }

        // POST: /Employees/Create
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Create([Bind(Exclude = "ID")] tbl_employees employee, FormCollection collection, HttpPostedFileBase PhotoPath)
        {
            ViewBag.DesgID = new SelectList(db.tbl_designations.Where(a => a.Status == true), "ID", "DisplayName");
            ViewBag.DeptID = new SelectList(db.tbl_departments.Where(a => a.Status == true), "ID", "DisplayName");
            ViewBag.Gender = new SelectList(new[] { "Male", "Female" }, (employee.Gender == null ? null : employee.Gender.ToString().PadRight(7).ToCharArray()));
            try
            {
                if (ValidateForm(employee))
                {
                    string EmpNo = collection["EmpNo"];
                    if (PhotoPath != null && PhotoPath.ContentLength > 0)
                    {
                        var fileName = "Employee_" + EmpNo + ".jpg";
                        employee.PhotoPath = fileName;
                        var path = Path.Combine(Server.MapPath("../Content/UploadImages/employees/"), fileName);
                        PhotoPath.SaveAs(path);
                    }
                    else
                        employee.PhotoPath = "user-avatar.jpg";
                    objCore.ConvertToUppercase(employee);
                    employee.Status = true; 
                    db.tbl_employees.AddObject(employee);
                    db.SaveChanges();
                    objCore.LoggingEntries("Employee Mgmt.", "Employee was added with the name " + employee.FirstName  + " " + employee.LastName + ".", User.Identity.Name);
                    return RedirectToAction("Index");
                }
            }
            catch
            {
                return View();
            }
            return View();
        }

        #endregion

        #region Edit Employee
        // GET: /Employees/Edit/5
        public ActionResult Edit(long id)
        {
            var employee = db.tbl_employees.SingleOrDefault(e => e.ID == id);
            ViewBag.DesgID = new SelectList(db.tbl_designations.Where(a => a.Status == true), "ID", "DisplayName", employee.DesgID);
            ViewBag.DeptID = new SelectList(db.tbl_departments.Where(a => a.Status == true), "ID", "DisplayName", employee.DeptID);
            ViewBag.Gender = new SelectList(new[] { "Male", "Female" }, (employee.Gender == null ? null : employee.Gender.ToString().PadRight(7).ToCharArray()));
            if (employee.Gender != "")
                ViewBag.Gender = new SelectList(new[] { "" }).Select(a => new SelectListItem { Text = employee.Gender, Selected = employee.Gender.ToString() == employee.Gender.ToString() });
            else
                ViewBag.Gender = new SelectList(new[] { "Male", "Female" }, (employee.Gender == null ? null : employee.Gender.ToString().PadRight(7).ToCharArray()));
            return View(employee);
        }

        // POST: /Employees/Edit/5
        [HttpPost]
        public ActionResult Edit(FormCollection collection, tbl_employees employee, HttpPostedFileBase PhotoPath)
        {
            long EmpID = Convert.ToInt64(collection["ID"]);
            var updateemployee = db.tbl_employees.SingleOrDefault(e => e.ID == EmpID);

            ViewBag.DesgID = new SelectList(db.tbl_designations.Where(a => a.Status == true), "ID", "DisplayName", employee.DesgID);
            ViewBag.DeptID = new SelectList(db.tbl_departments.Where(a => a.Status == true), "ID", "DisplayName", employee.DeptID);
            if (updateemployee.Gender != "")
                ViewBag.Gender = new SelectList(new[] { "" }).Select(a => new SelectListItem { Text = updateemployee.Gender, Selected = updateemployee.Gender.ToString() == updateemployee.Gender.ToString() });
            else
                ViewBag.Gender = new SelectList(new[] { "Male", "Female" }, (updateemployee.Gender == null ? null : updateemployee.Gender.ToString().PadRight(7).ToCharArray()));

            try
            {
                if (ValidateForm(employee))
                {
                    if (PhotoPath != null && PhotoPath.ContentLength > 0)
                    {
                        var fileName = updateemployee.PhotoPath;
                        if ((fileName == null) || (fileName == "user-avatar.jpg"))
                            fileName = "Employee_" + updateemployee.EmpNo + ".jpg";
                        updateemployee.PhotoPath = fileName;
                        employee.PhotoPath = fileName;
                        var path = Path.Combine(Server.MapPath("../../Content/UploadImages/employees/"), fileName);
                        PhotoPath.SaveAs(path);
                    }
                    string savedFileName = updateemployee.PhotoPath;
                    TryUpdateModel(updateemployee);
                    updateemployee.Gender = collection["Gender"];
                    updateemployee.PayMode = collection["PayMode"];
                    if (employee.PhotoPath != null)
                        updateemployee.PhotoPath = employee.PhotoPath;
                    else
                        updateemployee.PhotoPath = savedFileName;
                    objCore.ConvertToUppercase(updateemployee);
                    db.SaveChanges();
                    objCore.LoggingEntries("Employee Mgmt.", "Employee was Updated with the name " + employee.FirstName + " " + employee.LastName + ".", User.Identity.Name);
                    return RedirectToAction("Index");
                }
            }
            catch
            {
                return View(employee);
            }
            return View();
        } 
        #endregion

        #region View Employee Details
        // View Employee Details
        [HttpGet]
        public ActionResult Details(long Id)
        {
            tbl_employees _employee = db.tbl_employees.SingleOrDefault(a => a.ID == Id && a.Status == true);
            return PartialView("Details", _employee);
        }
        public ActionResult GetEmployeeDetailsByEmp(long EmpId)
        {
            tbl_employees empDet = db.tbl_employees.Where(a => a.ID == EmpId && a.Status == true).FirstOrDefault();
            GeneralClassFields objGen = new GeneralClassFields();
            objGen.ID = empDet.ID;
            objGen.Text1 = empDet.EmpNo;
            objGen.Text2 = empDet.FirstName + " " + empDet.LastName;
            objGen.Date1 = Convert.ToDateTime(empDet.DOJ);
            objGen.Text3 = empDet.tbl_departments.DisplayName + " - " + empDet.tbl_designations.DisplayName;
            objGen.Text4 = empDet.Mobile;
            objGen.Text5 = empDet.Email;
            return PartialView("_GetEmployeeDetailsByEmp", objGen);
        }

        #endregion

        #region Delete Employee
        public ActionResult Delete(int id)
        {
            tbl_employees employee = db.tbl_employees.SingleOrDefault(d => d.ID == id);
            return View(employee);
        }
        [HttpPost]
        public ActionResult Delete(int id, string confirmButton)
        {
            tbl_employees employee = db.tbl_employees.SingleOrDefault(d => d.ID == id);
            employee.Status = false;
            if (employee == null)
                return View("Error");
            db.SaveChanges();
            objCore.LoggingEntries("Employee Mgmt.", "Employee was deleted with the name " + employee.FirstName + " " + employee.LastName + ".", User.Identity.Name);
            return RedirectToAction("Index");
        } 
        #endregion
        
        #region EmployeeAttendance
        [Authorize]
        public ActionResult EmployeeAttendance()
        {
            var employeeAttend = db.tbl_emp_attendance_details.Where(a => a.Status == true).ToList();
            objCore.ConvertToUppercase<tbl_emp_attendance_details>(employeeAttend);
            return View(employeeAttend);
        }
        [HttpGet]
        public ActionResult AddEmployeeAttendance() 
        {
            return View();
        }
        public JsonResult GetEmployeesAttendance(int iDisplayStart, int iDisplayLength, string sSearch, int iColumns,
int iSortingCols, int iSortCol_0, string sSortDir_0,
int sEcho, string mDataProp_Key)
        {
            var EmployeesList = GetEmployeesAttendanceList(); 
            var filteredEmployeesDet = EmployeesList
                .Select(l => new[]{
                    l.Id.ToString(),
                    l.Text1.ToString(),
                    l.Text2.ToString(),
                    l.Text3.ToString(),
                    l.Date1.ToShortDateString(),
                    l.Text4.ToString(),
                    l.Date2.ToShortDateString(),
                    l.Text5.ToString(),
                    l.Id2.ToString(),
                    l.Status.ToString()
                    
                }).Where(i => string.IsNullOrEmpty(sSearch)
                     || i.Any(y => y.IndexOf(sSearch, StringComparison.InvariantCultureIgnoreCase) >= 0));
            var orderedlist = filteredEmployeesDet
                   .OrderByWithDirection(i => (i[iSortCol_0]).Parse(), sSortDir_0 == "asc")
                   .Skip(iDisplayStart)
                   .Take(iDisplayLength);
            var model = new
            {
                aaData = orderedlist,
                iTotalDisplayRecords = filteredEmployeesDet.Count(),
                iTotalRecords = filteredEmployeesDet.Count(),
                sEcho = sEcho.ToString()
            };
            return Json(model, JsonRequestBehavior.AllowGet);
        }
        private IEnumerable<CommonFields> GetEmployeesAttendanceList() 
        {
            var list = new List<CommonFields>();
            var employee = (from a in db.tbl_emp_attendance_details.Where(a => a.Status == true) select a).ToList(); ;
            foreach (tbl_emp_attendance_details ls in employee)
            {
                list.Add(new CommonFields
                {
                    Id = ls.ID,
                    Text1 = ls.EmpNo == null ? "" : ls.EmpNo.ToString(),
                    Text2 = ls.EmpID == null ? "" : ls.tbl_employees.FirstName + " " + ls.tbl_employees.LastName,
                    Text3 = ls.Site == null ? "" : ls.Site,
                    Date1 = Convert.ToDateTime(ls.LogInDate),
                    Text4 = ls.LogInTime == null ? "" : ls.LogInTime,
                    Date2 = Convert.ToDateTime(ls.LogOutDate),
                    Text5 = ls.LogOutTime == null ? "" : ls.LogOutTime,
                    Id2 = ls.TotalHours == null ? 0 : (int)ls.TotalHours,
                    Status = (bool)ls.Status
                });
            }
            objCore.ConvertToUppercase<CommonFields>(list);
            return list.OrderBy(a => a.Id);
        }
        [HttpPost]
        public ActionResult AddEmployeeAttendance([Bind(Exclude = "ID")]FormCollection frm, tbl_emp_attendance_details employee_attendance)
        {
            try
            {
                if (Validation(employee_attendance))
                {
                    employee_attendance.EmpID = frm["EmpID"] == null ? 0 : Convert.ToInt64(frm["EmpID"]);
                    employee_attendance.Status = true;
                    objCore.ConvertToUppercase(employee_attendance);
                    db.tbl_emp_attendance_details.AddObject(employee_attendance);
                    db.SaveChanges();
                    objCore.LoggingEntries("Employee Attendance.", "Employee Attendance was added with the name " + employee_attendance.tbl_employees.FirstName + " " + employee_attendance.tbl_employees.LastName + ".", User.Identity.Name);
                    return RedirectToAction("EmployeeAttendance");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Message = "An error occured while proccessing your request ==> " + ex.Message.ToString(); 
                return View();
            }
            return View();
        }
        [HttpGet]
        public ActionResult EditEmployeeAttendance(long Id)
        {
            tbl_emp_attendance_details emp_attend = db.tbl_emp_attendance_details.Where(a => a.ID == Id).FirstOrDefault();
            return View(emp_attend);
        }
        [HttpPost]
        public ActionResult EditEmployeeAttendance(long Id,FormCollection frm, tbl_emp_attendance_details employee_attendance)  
        {
            tbl_emp_attendance_details emp_attend = db.tbl_emp_attendance_details.Where(a => a.ID == Id).FirstOrDefault();
            try
            {
                if (Validation(employee_attendance))
                {
                    employee_attendance.EmpID = frm["EmpID"] == null ? 0 : Convert.ToInt64(frm["EmpID"]);
                    employee_attendance.Status = true;
                    objCore.ConvertToUppercase(employee_attendance);
                    TryUpdateModel(emp_attend);
                    db.SaveChanges();
                    objCore.LoggingEntries("Employee Attendance.", "Employee Attendance was Edited with the name " + employee_attendance.tbl_employees.FirstName + " " + employee_attendance.tbl_employees.LastName + ".", User.Identity.Name);
                    return RedirectToAction("EmployeeAttendance");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Message = "An error occured while proccessing your request ==> " + ex.Message.ToString();
                return View(emp_attend);
            }
            return View(emp_attend);
        }

        public ActionResult DeleteAttendance(long Id) 
        {
            tbl_emp_attendance_details emp_attend = db.tbl_emp_attendance_details.Where(a => a.ID == Id).FirstOrDefault();
            try
            {
                emp_attend.Status = false;
                TryUpdateModel(emp_attend);
                db.SaveChanges();
                objCore.LoggingEntries("Employee Attendance.", "Employee Attendance was deleted with the name " + emp_attend.tbl_employees.FirstName + " " + emp_attend.tbl_employees.LastName + ".", User.Identity.Name);
                return RedirectToAction("EmployeeAttendance");
            }
            catch (Exception)
            {
                return View("Error");
            }
        }

        #endregion

        #region EmployeePaySlip

        [HttpGet]
        [Authorize]
        public ActionResult PaySlip(string MonthId, long? EmpId) 
        {
            List<SelectListItem> MonthsList = new List<SelectListItem>();
            for (int i = 0; i < 12; i++)
                MonthsList.Add(new SelectListItem { Text = CultureInfo.GetCultureInfo("en-GB").DateTimeFormat.MonthNames[i], Value = (i >= 9) ? (i + 1).ToString() : "0" + (i + 1).ToString() });
            ViewBag.MonthID = MonthsList;
            if (MonthId != null)
                ViewBag.MonthName = MonthId.Split(' ')[0];
            ViewBag.EmpId = EmpId;
            if (EmpId != null)
                ViewBag.EmpNo = db.tbl_employees.Where(a => a.Status == true && a.ID == EmpId).FirstOrDefault().EmpNo;
            else
                ViewBag.EmpNo = "";
            return View("PaySlip");
        }
        [HttpGet]
        public ActionResult GeneratePaySlip(string MonthId, long EmpId)
        {
            string MonthYear = DateTime.Now.Year + "-" + MonthId;
            string Month = CultureInfo.GetCultureInfo("en-GB").DateTimeFormat.MonthNames[Convert.ToInt32(MonthId)-1];
            ViewBag.Month = Month;
            tbl_employees empDet = db.tbl_employees.Where(a => a.Status == true && a.ID == EmpId).FirstOrDefault();
            tbl_emp_payables empPayBill = db.tbl_emp_payables.Where(a => a.EmpID == EmpId && a.Status == true && a.MonthName == Month).FirstOrDefault();
            List<GeneralClassFields> PayableList = GetPayablesByEmp(EmpId, Month);
            if (empPayBill != null)
                return PartialView("_GeneratePaySlip", new EmployeeManagement(empDet, PayableList, empPayBill));
            else
                return PartialView("_GeneratePaySlip", new EmployeeManagement(null, null, null));
        }
        #endregion

        #region EmployeePayBill
        [Authorize]
        public ActionResult EmployeePayBillList()  
        {
            DataTable dt;
            using (SqlConnection sqlcon = new SqlConnection(ConfigurationManager.ConnectionStrings["ApplicationServices"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("Sp_EmployeePayBillList", sqlcon))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    using (System.Data.SqlClient.SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        dt = new DataTable();
                        da.Fill(dt);
                    }
                }
            }
            ViewBag.ViewPayables = dt;
            return PartialView("EmployeePayBillList");
        }

        public ActionResult EmpPayBill()  
        {
            List<SelectListItem> MonthsList = new List<SelectListItem>();
            for (int i = 0; i < 12; i++)
                MonthsList.Add(new SelectListItem { Text = CultureInfo.GetCultureInfo("en-GB").DateTimeFormat.MonthNames[i], Value = (i >= 9) ? (i + 1).ToString() : "0" + (i + 1).ToString() });
            ViewBag.MonthID = MonthsList;
            return View();
        }
        // View Employee Pay Bill 
        [HttpGet]
        public ActionResult GenerateEmpPayBill(string MonthId)
        {
            DataTable dt;
            using (SqlConnection sqlcon = new SqlConnection(ConfigurationManager.ConnectionStrings["ApplicationServices"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("Sp_EmployeePayBill", sqlcon))
                {
                    string Date = DateTime.Now.Year + "-" + MonthId + "-" + "01";
                    cmd.Parameters.AddWithValue("@MonthYear", Date);
                    cmd.CommandType = CommandType.StoredProcedure;
                    using (System.Data.SqlClient.SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        dt = new DataTable();
                        da.Fill(dt);
                    }
                }
            }
            ViewBag.MonthName = CultureInfo.GetCultureInfo("en-GB").DateTimeFormat.MonthNames[Convert.ToInt32(MonthId) - 1];
            ViewBag.PayBill = GetMonthlyEmpPayables(dt);
            return PartialView("_GenerateEmpPayBill");
        }
        [HttpPost]
        public ActionResult GenerateEmpPayBill(FormCollection frm)
        {
            DataTable dt = (DataTable)TempData["DataTable"];
            try
            {
                // Add Emp Payable List 
                foreach (DataRow dr in dt.Rows)
                {
                    long EmpId = Convert.ToInt64(dr["ID"]);
                    int TotalDays = Convert.ToInt32(dr["Total Days"]);
                    int TotalAbsents = Convert.ToInt32(dr["Absent"]);
                    int TotalPresents = Convert.ToInt32(dr["Presents"]);
                    int WorkingHours = Convert.ToInt32(dr["Working Hours"]);
                    decimal GrossSalary = Convert.ToDecimal(dr["Gross Salary"]);
                    decimal PerDay = Convert.ToDecimal(dr["PerDay"]);
                    decimal NetSalary = Convert.ToDecimal(dr["Net Salary"]);

                    // Create Object Employees Monthly Payables

                    tbl_emp_payables objPayable = new tbl_emp_payables();

                    objPayable.EmpID = EmpId;
                    objPayable.TotalDays = TotalDays;
                    objPayable.TotalAbsent = TotalAbsents;
                    objPayable.TotalPresent = TotalPresents;
                    objPayable.TotalHours = WorkingHours;
                    objPayable.Salary = GrossSalary;
                    objPayable.NetSalary = NetSalary;
                    objPayable.CreatedDate = DateTime.Now;
                    objPayable.MonthName = frm["MonthName"];
                    objPayable.YearName = DateTime.Now.Year.ToString();
                    objPayable.PerDay = PerDay;
                    objPayable.Status = true;

                    db.tbl_emp_payables.AddObject(objPayable);
                    db.SaveChanges();

                    SetClosedFlagEmpAttendance(EmpId, objPayable.MonthName);

                    foreach (DataColumn dc in dt.Columns)
                    {
                        if (dr.Table.Columns[dc.ColumnName].Ordinal > 8)
                        {
                            if (dc.ColumnName != "Net Salary")
                            {
                                if (dc.ColumnName == "Salary")
                                    dc.ColumnName = "BP";
                                int PayableId = GetPayableId(dc.ColumnName.ToString().Trim());
                                decimal PayableValue = Convert.ToDecimal(dr[dc.ColumnName]);

                                // Create Object Employees Monthly Payables details

                                tbl_emp_payable_details objPayableDet = new tbl_emp_payable_details();
                                objPayableDet.EmpID = EmpId;
                                objPayableDet.PayFieldID = PayableId;
                                objPayableDet.PayFieldValue = PayableValue;
                                objPayableDet.PayableID = objPayable.ID;
                                objPayableDet.CreatedDate = DateTime.Now;
                                objPayableDet.Status = true;

                                db.tbl_emp_payable_details.AddObject(objPayableDet);
                            }
                        }
                    }
                }
                db.SaveChanges();
                objCore.LoggingEntries("Employee Pay Bill", "Emp.Pay bill has generated.", User.Identity.Name);
                return RedirectToAction("EmployeePayBillList");
                //return Content("<script language='javascript' type='text/javascript'> alert('Employee Pay bill generated sucessfully');windows.location.href='/Employees/Index'</script>");
            }
            catch (Exception ex)
            {
                ViewBag.Message = "An Error occured while processing your request ==> " + ex.Message.ToString();
                return PartialView("_GenerateEmpPayBill");
            }
        }

        [HttpGet]
        public ActionResult ViewEmployeePayables(long EmpId)
        {
            DataTable dt;
            using (SqlConnection sqlcon = new SqlConnection(ConfigurationManager.ConnectionStrings["ApplicationServices"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("Sp_ViewEmployeePayables", sqlcon))
                {
                    cmd.Parameters.AddWithValue("@EmpId", EmpId.ToString());
                    cmd.CommandType = CommandType.StoredProcedure;
                    using (System.Data.SqlClient.SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        dt = new DataTable();
                        da.Fill(dt);
                    }
                }
            }
            ViewBag.ViewPayables = dt;
            return PartialView("_ViewEmployeePayables");
        }

        [HttpGet]
        public ActionResult ApproveEmployeePayBill(long ID)
        {
            tbl_emp_payables empPayable = db.tbl_emp_payables.Where(a => a.ID == ID && a.Status == true).FirstOrDefault();
            empPayable.Approved = true;
            empPayable.ApprovedDate = DateTime.Now;
            empPayable.ApprovedBy = User.Identity.Name;
            try
            {
                TryUpdateModel(empPayable);
                db.SaveChanges();
            }
            catch (Exception)
            {
                 
            }
            objCore.LoggingEntries("Employee Pay Bill", "Emp.Pay bill has Approved .", User.Identity.Name);
            return RedirectToAction("EmployeePayBillList"); 
        }

        [HttpGet]
        public ActionResult DeleteEmployeePayBill(long ID)
        {
            tbl_emp_payables empPayBill = db.tbl_emp_payables.Where(a => a.ID == ID && a.Status == true).FirstOrDefault();
            TryUpdateModel(empPayBill);
            empPayBill.Status = false;

            List<tbl_emp_payable_details> empPayBillDetailsList = db.tbl_emp_payable_details.Where(a => a.PayableID == ID).ToList();

            foreach (var item in empPayBillDetailsList)
            {
                tbl_emp_payable_details empPayBillDetails = db.tbl_emp_payable_details.Where(a => a.ID == item.ID).FirstOrDefault();
                TryUpdateModel(empPayBillDetails);
                empPayBillDetails.Status = false;
            }
            
            try
            {
                RevertToFalseClosedFlagEmpAttendance((long)empPayBill.EmpID, empPayBill.MonthName);
                db.SaveChanges();
            }
            catch (Exception)
            {

            }
            objCore.LoggingEntries("Employee Pay Bill", "Emp.Pay bill has deleted.", User.Identity.Name);
            return RedirectToAction("EmployeePayBillList");
        }

        public decimal GetPayTypeAmount(long EmpId, string PayName, decimal Salary)
        {
            string qry = " select pm.PayFieldID as Value1, pm.Formula as Text1,pf.EntryMode as Text2,pf.CalcType as Text3 ,pf.Type as Text4 from tbl_pay_master pm  inner join tbl_pay_fields pf on pf.PayID = pm.PayFieldID where pm.EmpID = " + EmpId + " and PayName='" + PayName.Trim() + "' and pm.Status = 1 and pf.Status=1 ";
            var list = db.ExecuteStoreQuery<GeneralClassFields>(qry).ToList().FirstOrDefault();
            decimal Amount = 0;
            // Verify whether the employee having basic pay or not
            if (list != null)
            {
                if (list.Text1 != null && list.Text1 != "")
                {
                    Regex Rgx1 = new Regex("[^a-z0-9]");
                    Regex Rgx2 = new Regex(@"[^a-z0-9+*-/\s*]");
                    Regex Rgx3 = new Regex("[a-zA-Z]+");
                    if (Rgx1.IsMatch(list.Text1))
                    {
                        if (ValidateBasicPay(EmpId)) // If employee having basic pay or not 
                        {
                            int PayFieldId = GetPayableId(PayName);
                            DataTable dt = new DataTable();
                            decimal tempVal = 0;
                            string tempStr = string.Empty;
                            if (Rgx2.IsMatch(list.Text1)) // Verify formula having special characters or not
                            {
                                string eqStr = GetStringEquation(EmpId, list.Text1, Salary);
                                while (Rgx3.IsMatch(eqStr))
                                {
                                    eqStr = GetStringEquation(EmpId, eqStr, Salary);
                                    continue;
                                }
                                tempStr = eqStr;
                                tempVal = Convert.ToDecimal(dt.Compute(tempStr, ""));
                            }
                            else
                                tempVal = Convert.ToDecimal(dt.Compute(list.Text1, ""));   // Calculate amount if no pay fields exists

                            tbl_pay_master payFieldsDet = db.tbl_pay_master.Where(a => a.tbl_pay_fields.PayID == PayFieldId && a.EmpID == (long)EmpId && a.Status == true).FirstOrDefault();
                            if (payFieldsDet.tbl_pay_fields.CalcType.ToUpper() == "VARIES WITH PRESENTS")
                                Amount = (decimal)tempVal;
                            else
                                Amount = (decimal)tempVal;
                        }
                    }
                    else
                        Amount = Convert.ToDecimal(list.Text1); // If no formula having fixed value
                    return Math.Round(Amount, 2);
                }
            }
            return Math.Round(Amount, 2);
        }

        public string GetStringEquation(long EmpId , string formula,decimal salary)
        {
            string formulae = formula;
            Regex Rgx1 = new Regex("[^a-z0-9]");
            Regex Rgx2 = new Regex(@"[^a-z0-9+*-/\s*]");
            if (Rgx1.IsMatch(formula)) // verify whether formula having special character or not.
            {
                if (Rgx2.IsMatch(formula))
                {
                    string[] split1 = formula.Split(new string[] { "+", "*", "/", "-" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string payname in split1)
                    {
                        if (Rgx2.IsMatch(payname))
                        {
                            tbl_pay_master pmDet = db.tbl_pay_master.Where(a => a.EmpID == EmpId && a.Status == true && a.tbl_pay_fields.PayName.Trim().ToUpper() == payname.Trim().ToUpper()).FirstOrDefault();
                            if (payname == "BP")
                            {
                                if (pmDet.tbl_pay_fields.CalcType == "VARIES WITH PRESENTS")
                                    formulae = formulae.Replace(payname, salary.ToString());
                                else
                                    formulae = formulae.Replace(payname, pmDet.Formula);
                            }
                            else
                                formulae = formulae.Replace(payname, pmDet.Formula);
                        }
                    }
                }
            }
            return formulae;
        }

        //public decimal GetPayTypeAmount(long EmpId, string PayName, decimal Salary)
        //{
        //    string qry = " select pm.PayFieldID as Value1, pm.Formula as Text1,pf.EntryMode as Text2,pf.CalcType as Text3 ,pf.Type as Text4 from tbl_pay_master pm  inner join tbl_pay_fields pf on pf.PayID = pm.PayFieldID where pm.EmpID = " + EmpId + " and PayName='" + PayName.Trim() + "' and pm.Status = 1 and pf.Status=1 ";
        //    var list = db.ExecuteStoreQuery<GeneralClassFields>(qry).ToList().FirstOrDefault();
        //    decimal Amount = 0;
        //    // Verify whether the employee having basic pay or not
        //    if (list != null)
        //    {
        //        tbl_pay_master BasicPayDet = db.tbl_pay_master.Where(a => a.EmpID == EmpId && a.tbl_pay_fields.PayName == "BP" && a.Status == true).FirstOrDefault();
        //        if (list.Text1.Contains("BP"))
        //        {
        //            if (ValidateBasicPay(EmpId))
        //            {
        //                int PayFieldId = GetPayableId(PayName);
        //                string[] split = list.Text1.Split(new string[] { "BP", "*", "/" }, StringSplitOptions.RemoveEmptyEntries);
        //                tbl_pay_master payFieldsDet = db.tbl_pay_master.Where(a => a.tbl_pay_fields.PayID == PayFieldId && a.EmpID == (long)EmpId && a.Status == true).FirstOrDefault();
        //                if (payFieldsDet.tbl_pay_fields.CalcType.ToUpper() == "VARIES WITH PRESENTS")
        //                    Amount = Salary * Convert.ToDecimal(split[0]) / Convert.ToDecimal(split[1]);
        //                else
        //                    Amount = Convert.ToDecimal(BasicPayDet.Formula) * Convert.ToDecimal(split[0]) / Convert.ToDecimal(split[1]);
        //            }
        //        }
        //        else
        //        {
        //            Amount = Convert.ToDecimal(list.Text1);
        //        }
        //        return Math.Round(Amount, 2);
        //    }
        //    return Math.Round(Amount, 2);
        //}
        // Payable for Employee wise in PaySlip 

        public List<GeneralClassFields> GetPayablesByEmp(long EmpId, string Month)
        {
            string qry = "select pf.PayID as value1,pf.PayName as Text1,epd.PayFieldValue as value2,pf.Type as Text2 from tbl_emp_payable_details epd inner join tbl_pay_fields pf on pf.PayID = epd.PayFieldID inner join tbl_emp_payables ep on ep.ID = epd.PayableID and ep.EmpID = epd.EmpID "+
                " where ep.EmpID=" + EmpId + " and pf.Status=1 and epd.Status =1 " +
                (Month == "" ? "" : "and ep.MonthName = '" + Month + "'");
            var list = db.ExecuteStoreQuery<GeneralClassFields>(qry).ToList();
            return list;
        }

        public DataTable GetMonthlyEmpPayables(DataTable dt)
        {
            DataTable tempDt = new DataTable();
            decimal Salary = 0;
            decimal Deductions = 0;
            decimal Earnings = 0;
            //decimal NetSalary = 0;

            foreach (DataColumn tempDc in dt.Columns)
            {
                if (tempDc.ColumnName.ToUpper() != "TOTAL EARNINGS")
                    tempDt.Columns.Add(tempDc.ColumnName, typeof(string));
            }

            // One New Column at last
            tempDt.Columns.Add("Net Salary", typeof(string));

            //NetSalary = 0;
            foreach (DataRow dr in dt.Rows)
            {
                Salary = 0; Deductions = 0;
                DataRow tempDr = tempDt.NewRow();
                foreach (DataColumn dc in dt.Columns)
                {
                    if (dr.Table.Columns[dc.ColumnName].Ordinal > 8)
                    {
                        if (dc.ColumnName == "Salary")
                        {
                            dc.ColumnName = "BP";
                        }
                        if (dc.ColumnName == "BP")
                        {
                            int PayFieldId = GetPayableId("BP");
                            long EmpId = (long)dr["ID"];
                            tbl_pay_master payFieldsDet = db.tbl_pay_master.Where(a => a.tbl_pay_fields.PayID == PayFieldId && a.EmpID == EmpId && a.Status == true).FirstOrDefault();
                            if (payFieldsDet.tbl_pay_fields.CalcType.ToUpper() == "VARIES WITH PRESENTS")
                                Salary = (decimal)dr["PerDay"] * (int)dr["Presents"];
                            else
                                Salary = Convert.ToDecimal(payFieldsDet.Formula);
                            tempDr[dc.ColumnName] = Salary;
                        }
                        else
                        {
                            int PayFieldId = GetPayableId(dc.ColumnName.ToString());
                            tbl_pay_fields payFields = db.tbl_pay_fields.Where(a => a.PayID == PayFieldId && a.Status == true).FirstOrDefault();
                            if (dc.ColumnName.ToUpper() != "TOTAL EARNINGS")
                            {
                                tempDr[dc.ColumnName] = GetPayTypeAmount((long)dr["ID"], dc.ColumnName.ToString(), Salary);
                                if (payFields.Type.ToUpper() == "EARNINGS")
                                    Earnings += GetPayTypeAmount((long)dr["ID"], dc.ColumnName.ToString(), Salary);
                                else
                                    Deductions += GetPayTypeAmount((long)dr["ID"], dc.ColumnName.ToString(), Salary);
                            }
                        }
                    }
                    else
                        tempDr[dc.ColumnName] = dr[dc.ColumnName];
                }
                tempDr["Net Salary"] = Salary + Earnings - Deductions;
                Earnings = 0 ; Deductions = 0;
                tempDt.Rows.Add(tempDr);
            }
            return tempDt;
        }

        #endregion

        #region EmployeeAdvances

        public ActionResult EmpAdvanceList(long EmpId)
        {
            var list = db.tbl_emp_advances.Where(a => a.EmpID == EmpId && a.Status == true).ToList();
            return PartialView("_EmpAdvanceList", list);
        }

        [HttpGet]
        public ActionResult AddEmpAdvance(long EmpId)
        {
            ViewBag.EmpId = EmpId;
            return PartialView("_AddEmpAdvance");
        }
        [HttpPost]
        public ActionResult AddEmpAdvance([Bind(Exclude = "ID")]FormCollection frm, tbl_emp_advances empAdvanceDet)
        {
            ViewBag.EmpId = empAdvanceDet.EmpID;
            try
            {
                if (ValidationAdvances(empAdvanceDet))
                {
                    empAdvanceDet.Status = true;
                    empAdvanceDet.CreatedDate = DateTime.Now;
                    empAdvanceDet.CreatedBy = User.Identity.Name;
                    db.tbl_emp_advances.AddObject(empAdvanceDet);
                    db.SaveChanges();
                    return Content("<script language='javascript' type='text/javascript'> $.modal.close(); alert('Advance added successfully.'); EmployeeAdvanceList('" + empAdvanceDet.EmpID + "') </script>");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Message = "An Error occured while proccessing your request ==> " + ex.Message.ToString();
                return PartialView("_AddEmpAdvance");
            }
            return PartialView("_AddEmpAdvance");
        }
        [HttpGet]
        public ActionResult EditEmpAdvance(long Id)
        {
            tbl_emp_advances empAdvance = db.tbl_emp_advances.Where(a => a.Status == true && a.ID == Id).FirstOrDefault();
            ViewBag.EmpId = empAdvance.EmpID;
            return PartialView("_EditEmpAdvance", empAdvance);
        }
        [HttpPost]
        public ActionResult EditEmpAdvance(long Id, FormCollection frm, tbl_emp_advances empAdvanceDet)
        {
            ViewBag.EmpId = empAdvanceDet.EmpID;
            tbl_emp_advances empAdvance = db.tbl_emp_advances.Where(a => a.Status == true && a.ID == Id).FirstOrDefault();
            try
            {
                if (ValidationAdvances(empAdvanceDet))
                {
                    empAdvance.Status = true;
                    TryUpdateModel(empAdvance);
                    db.SaveChanges();
                    return Content("<script language='javascript' type='text/javascript'> $.modal.close(); alert('Advance updated successfully.'); EmployeeAdvanceList('" + empAdvanceDet.EmpID + "') </script>");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Message = "An Error occured while proccessing your request ==> " + ex.Message.ToString();
                return PartialView("_EditEmpAdvance", empAdvance);
            }
            return PartialView("_EditEmpAdvance", empAdvance);
        }
        public ActionResult DeleteEmpAdvance(long Id)
        {
            tbl_emp_advances empAdvanceDet = db.tbl_emp_advances.Where(a => a.Status == true && a.ID == Id).FirstOrDefault();
            try
            {
                empAdvanceDet.Status = false;
                TryUpdateModel(empAdvanceDet);
                db.SaveChanges();
                return Content("<script language='javascript' type='text/javascript'>   alert('Advance deleted successfully.'); EmployeeAdvanceList('" + empAdvanceDet.EmpID + "') </script>");
            }
            catch (Exception ex)
            {
                return Content("<script language='javascript' type='text/javascript'>   alert('An Error Occured proccessing ==> " + ex.Message.ToString() + "'); </script>");
            }
        }
        public bool ValidationAdvances(tbl_emp_advances empAdv)
        {
            if (empAdv.AdvanceAmt == null || empAdv.AdvanceAmt == 0)
                ModelState.AddModelError("AdvanceAmt", "Please enter Advance amount");
            if (empAdv.AdvDate == null)
                ModelState.AddModelError("AdvDate", "Please select advance date");
            return ModelState.IsValid;
        }

        #endregion

        #region CustomMethods

        private bool ValidateForm(tbl_employees profile)
        {
            //ModelState.Clear();
            if (String.IsNullOrEmpty(profile.EmpNo))
                ModelState.AddModelError("EmpNo", "Employee No: is required.");
            if (String.IsNullOrEmpty(profile.FirstName))
                ModelState.AddModelError("FirstName", "First name is required");
            if (String.IsNullOrEmpty(profile.LastName))
                ModelState.AddModelError("LastName", "Last name is required");
            if (String.IsNullOrEmpty(profile.Gender))
                ModelState.AddModelError("Gender", "Select Gender.");
            if (String.IsNullOrEmpty(profile.DOJ.ToString()))
                ModelState.AddModelError("DOJ", "Date of joining is required.");
            if (profile.DesgID == 0 || profile.DesgID == null)
                ModelState.AddModelError("DesgID", "Select Designation.");
            if (profile.DeptID == 0 || profile.DeptID == null)
                ModelState.AddModelError("DeptID", "Select Department.");
            if (String.IsNullOrEmpty(profile.Mobile))
                ModelState.AddModelError("Mobile", "Mobile is required");
            if (String.IsNullOrEmpty(profile.Email))
                ModelState.AddModelError("Email", "Email is required");
            if (String.IsNullOrEmpty(profile.UserName))
                ModelState.AddModelError("UserName", "Login name is required");
            if (String.IsNullOrEmpty(profile.Password))
                ModelState.AddModelError("Password", "Password is required");
            return ModelState.IsValid;
        }
         
        public bool Validation(tbl_emp_attendance_details emp_attend)
        {
            if (emp_attend.EmpNo == null || emp_attend.EmpNo == "")
                ModelState.AddModelError("EmpNo", "Please search employee Id");
            return ModelState.IsValid;
        }

        public string GetEmployeeNumber(string f, string q)  
        {
            f = f == "undefined" ? "EmpNo" : f;
            StringBuilder str = new StringBuilder();
            var employee = from a in db.tbl_employees
                                    .Where(a => a.Status.Value == true)
                                    .Where<tbl_employees>(f, q, WhereOperation.Contains)
                           select new { a.EmpNo, a.ID };
            foreach (var a in employee)
                str.Append(string.Format("{0}|{1}\r\n", a.EmpNo.ToUpper(), a.ID)).ToString();
            return str.ToString();
        }

        public string GetEmployeeName(long Id)
        {
            tbl_employees empDet = db.tbl_employees.Where(a => a.ID == Id && a.Status == true).FirstOrDefault();
            if (empDet != null)
                return empDet.FirstName + " " + empDet.LastName;
            else
                return string.Empty;
        }

        public int CalculateTotalhours(string LogInDate, string LogInTime, string LogOutDate, string LogOutTime)
        {
            DateTime StartDate = Convert.ToDateTime(LogInDate.Trim() + " " + LogInTime);
            DateTime EndDate = Convert.ToDateTime(LogOutDate.Trim() + " " + LogOutTime);
            int TotalHrs = (int)EndDate.Subtract(StartDate).TotalHours;
            if (TotalHrs > 0)
                return TotalHrs;
            return 0;
        }

        public bool ValidateBasicPay(long EmpId)
        {
            return db.tbl_pay_master.Where(a => a.EmpID == EmpId && a.tbl_pay_fields.PayName.Contains("BP") && a.Status == true).Any();
        }

        public int CheckEmpNo(string empNo)
        {
            if (db.tbl_employees.Where(a => a.EmpNo.Trim() == empNo.Trim() && a.Status == true).Any())
                return 1;  // if 1 returns already empno exist
            return 0;
        }

        public int GetPayableId(string Payable)
        {
            return db.tbl_pay_fields.Where(a => a.PayName.Trim() == Payable && a.Status == true).FirstOrDefault() == null ? 0 : db.tbl_pay_fields.Where(a => a.PayName.Trim() == Payable && a.Status == true).FirstOrDefault().PayID;
        }

        public List<tbl_emp_attendance_details> GetMonthlyEmpAttendance(long EmpId, string Month)
        {
            int MonthId = Convert.ToInt32(Month);
            List<tbl_emp_attendance_details> EmpAttendList = (from m in db.tbl_emp_attendance_details
                                                              where (m.LogInDate == null || m.LogInDate.Value.Month == MonthId)
                                                              && (m.LogInDate == null || m.LogInDate.Value.Year == DateTime.Now.Year)
                                                              && m.Status == true
                                                              && m.EmpID == EmpId
                                                              select m).ToList();
            return EmpAttendList;
        }

        public void SetClosedFlagEmpAttendance(long EmpId, string Month)
        {
            string MonthId = DateTime.ParseExact(Month, "MMMM", CultureInfo.GetCultureInfo("en-GB")).Month.ToString();
            List<tbl_emp_attendance_details> empAttendList = GetMonthlyEmpAttendance(EmpId, MonthId);

            foreach (var item in empAttendList)
            {
                tbl_emp_attendance_details empAttendDet = db.tbl_emp_attendance_details.Where(a => a.ID == item.ID).FirstOrDefault();
                empAttendDet.ClosedFlag = true;
            }
            db.SaveChanges();
        }

        public void RevertToFalseClosedFlagEmpAttendance(long EmpId, string Month)  
        {
            string MonthId = DateTime.ParseExact(Month, "MMMM", CultureInfo.GetCultureInfo("en-GB")).Month.ToString();
            List<tbl_emp_attendance_details> empAttendList = GetMonthlyEmpAttendance(EmpId, MonthId);

            foreach (var item in empAttendList)
            {
                tbl_emp_attendance_details empAttendDet = db.tbl_emp_attendance_details.Where(a => a.ID == item.ID).FirstOrDefault();
                empAttendDet.ClosedFlag = false;
            }
            db.SaveChanges();
        }

        public bool IsApproveEmpPayBill(long PayableID)
        {
            return db.tbl_emp_payables.Where(a => a.ID == PayableID && a.Status == true && a.Approved == true).Any();
        }

        public long GetEmpId(long PayId)
        {
            return (long)db.tbl_emp_payables.Where(a => a.ID == PayId && a.Status == true).FirstOrDefault().EmpID;
        }

        #endregion

    }
}
