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
    public class UploadController : Controller
    {

        #region ctors
        private FMSDBEntities db;
        private core core = new core();
        private IFileStore _fileStore = new DiskFileStore();
        public UploadController()
        {
            db = new FMSDBEntities();
        }
        public string AsyncUpload()
        {
            return _fileStore.SaveUploadedFile(Request.Files[0]);
        } 
        #endregion

        #region Diesel Tracking Bulk upload
        public ActionResult DieselTrackingUpload()
        {
            Session["InsertFail"] = null;
            Session["InsertFailCnt"] = null;
            ViewBag.ErrorMsg = "Select upload file and type Sheet name,for example:sheet1";
            ViewBag.ExcelStatus = "0";
            return View();
        }
        [HttpPost]
        public ActionResult DieselTrackingUpload(FormCollection collection)
        {
            OleDbConnection excelConnection = null;
            string connect = WebConfigurationManager.ConnectionStrings["ApplicationServices"].ToString();
            string filename = Request.Params[0];
            string sheetname = Request.Params[2];
            if (Validateupload(filename, sheetname))
            {
                String path = Server.MapPath("~/Content/uploadimages/");
                string file = path + filename;
                //string excelConnectionString = @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + file + ";Extended Properties=\"Excel 8.0;HDR=YES;IMEX=1;\"";
                //string excelConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + file + ";Extended Properties=\"Excel 8.0;HDR=YES;IMEX=1;\"";

                // Excel File Extension .xls excute Microsoft.Jet.OLEDB.4.0 or Excel File Extension .xlsx excute Microsoft.ACE.OLEDB.12.0
                string excelConnectionString = string.Empty;
                if (Path.GetExtension(file) == ".xls" || Path.GetExtension(file) == ".XLS")
                    excelConnectionString = @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source = '" + file + "';Extended Properties=\"Excel 8.0;HDR=YES;IMEX=2\"";
                else if (Path.GetExtension(file) == ".xlsx" || Path.GetExtension(file) == ".XLSX") 
                    excelConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source='" + file + "';Extended Properties=\"Excel 8.0;HDR=YES;IMEX=2\"";
                
                excelConnection = new OleDbConnection(excelConnectionString);
                OleDbDataAdapter cmd = new OleDbDataAdapter("Select * from [" + sheetname + "$]", excelConnection);
                try
                {
                    DataTable dt = new DataTable();
                    DataTable dtInsertFail = new DataTable();
                    cmd.Fill(dt);
                    dt.Columns.Add("error", typeof(string));
                    // Adding columns to temp datatable 
                    //dtInsertFail.Columns.Add("VehicleRegNo", typeof(string));
                    //dtInsertFail.Columns.Add("ClientName", typeof(string));
                    //dtInsertFail.Columns.Add("Date", typeof(DateTime));

                    dtInsertFail.Columns.Add("FuelStationName", typeof(string));
                    dtInsertFail.Columns.Add("BookNo", typeof(string));

                    dtInsertFail.Columns.Add("DieselTokenNum", typeof(string));
                    dtInsertFail.Columns.Add("TokenValue", typeof(string));
                    dtInsertFail.Columns.Add("PricePerLiter", typeof(decimal));

                    //dtInsertFail.Columns.Add("CardNum", typeof(string));
                    dtInsertFail.Columns.Add("IssuedTo", typeof(string));
                    //dtInsertFail.Columns.Add("IssuedBy", typeof(string));

                    dtInsertFail.Columns.Add("error", typeof(string));
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        int intCol = 0;
                        string VehicleRegNo = dt.Rows[i].ItemArray[0].ToString().ToUpper().Trim();
                        tbl_vehicles vehicle = db.tbl_vehicles.Where(v => v.Status == true && v.VehicleRegNum.ToUpper().Trim() == VehicleRegNo).FirstOrDefault();
                        if (vehicle == null) //if vehicle details are not found on particular vehicle registration number.
                        {
                            DataRow dr1 = dtInsertFail.NewRow();
                            foreach (DataColumn Col in dtInsertFail.Columns)
                            {
                                dr1[Col.ColumnName] = dt.Rows[i].ItemArray[intCol];
                                if (Col.ColumnName == "error")
                                    dr1[Col.ColumnName] = "Please check vehicle registration number and try again.";
                                intCol++;
                            }
                            intCol = 0;
                            dtInsertFail.Rows.Add(dr1);
                            continue; // continue proccess 
                        }
                        string ClientName = dt.Rows[i].ItemArray[1].ToString().ToUpper().Trim();
                        tbl_clients client = db.tbl_clients.Where(c => c.Status == true && c.CompanyName.ToUpper().Trim() == ClientName).FirstOrDefault();
                        if (client == null)
                        {
                            DataRow dr1 = dtInsertFail.NewRow();
                            foreach (DataColumn Col in dtInsertFail.Columns)
                            {
                                dr1[Col.ColumnName] = dt.Rows[i].ItemArray[intCol];
                                if (Col.ColumnName.ToUpper() == "ERROR")
                                    dr1[Col.ColumnName] = "Please check client name(FirstName) and try again.";
                                intCol++;
                            }
                            intCol = 0;
                            dtInsertFail.Rows.Add(dr1);
                            continue; // continue proccess 
                        }
                        string dtime = DateTime.Parse(dt.Rows[i].ItemArray[2].ToString() == "" ? DateTime.Now.ToString() : dt.Rows[i].ItemArray[2].ToString()).ToString("MM-dd-yyyy hh:mm:ss");
                        string FuelStationName = dt.Rows[i].ItemArray[3].ToString().ToUpper().Trim();
                        string DieselTokenNum = dt.Rows[i].ItemArray[4].ToString().ToUpper().Trim();
                        string TokenValue = dt.Rows[i].ItemArray[5].ToString().Trim();
                        string PricePerLiter = dt.Rows[i].ItemArray[6].ToString().Trim();
                        string CardNum = dt.Rows[i].ItemArray[7].ToString().ToUpper().Trim();
                        string IssuedTo = dt.Rows[i].ItemArray[8].ToString().ToUpper().Trim();
                        string IssuedBy = dt.Rows[i].ItemArray[9].ToString().ToUpper().Trim();
                        if (CardNum != "" && DieselTokenNum != "")
                        {
                            var dtList = db.tbl_diesel_tracking.Where(a => a.FuelStationName == FuelStationName && a.CardNum == CardNum && a.DieselTokenNum == DieselTokenNum && (a.ClosedFlag == false || a.ClosedFlag == null) && a.Status == true).ToList();
                            if (dtList.Count >= 1)
                            {
                                DataRow dr1 = dtInsertFail.NewRow();
                                foreach (DataColumn Col in dtInsertFail.Columns)
                                {
                                    dr1[Col.ColumnName] = dt.Rows[i].ItemArray[intCol];
                                    if (Col.ColumnName.ToUpper() == "ERROR")
                                        dr1[Col.ColumnName] = "Diesel tracking record is already available on this Fuel Station: " + FuelStationName + ", Book No.: " + CardNum + " , Token No.: " + DieselTokenNum;
                                    intCol++;
                                }
                                intCol = 0;
                                dtInsertFail.Rows.Add(dr1);
                                continue; // continue proccess 
                            }
                        }
                        tbl_diesel_tracking DT = new tbl_diesel_tracking();
                        DT.VehicleID = vehicle.ID;
                        DT.ClientID = client.ID;
                        DT.Date = Convert.ToDateTime(dtime);
                        DT.FuelStationName = FuelStationName;
                        DT.DieselTokenNum = DieselTokenNum;
                        DT.TokenValue = Convert.ToDouble(TokenValue);
                        DT.PricePerLiter = Convert.ToDecimal(PricePerLiter);
                        DT.CardNum = CardNum;
                        DT.CreatedBy = User.Identity.Name;
                        DT.CreatedDt = DateTime.Now;
                        DT.IssuedTo = IssuedTo;
                        DT.IssuedBy = IssuedBy;
                        DT.Status = true;
                        DT.TotalAmount = (decimal)((decimal)DT.TokenValue * DT.PricePerLiter);
                        db.tbl_diesel_tracking.AddObject(DT);
                        db.SaveChanges();
                    }
                    //SqlBulkCopy sqlBulk = new SqlBulkCopy(connect);
                    //sqlBulk.DestinationTableName = "tbl_diesel_tracking";
                    //sqlBulk.WriteToServer(ds);
                    int updatedCount = dt.Rows.Count - dtInsertFail.Rows.Count;
                    ViewBag.ErrorMsg = "Success: Diesel Tracking data uploaded Successfully. Updated record(s) are " + updatedCount + " out of " + dt.Rows.Count + ".";
                    Session["InsertFail"] = dtInsertFail;
                    Session["InsertFailCnt"] = dtInsertFail.Rows.Count;
                    if (dtInsertFail.Rows.Count >= 1)
                        ViewBag.ExcelStatus = "1";
                    else
                        ViewBag.ExcelStatus = "0";
                    core.LoggingEntries("Diesel Tracking", "Bulk upload for diesel tracking has done.", User.Identity.Name);
                }
                catch (SqlException ex)
                {
                    ViewBag.ErrorMsg = "InValid File Name/See Excel Sheet Sample Format" + ex.Message;
                }
                catch (Exception e)
                {
                    ViewBag.ErrorMsg = "Invalid File Name/See Excel Sheet Sample Format,Please try again." + e.Message;
                }
                return View();
            }
            return View();
        }
        #endregion

        #region Diesel Book Bulk upload
        public ActionResult DieselBookUpload()
        {
            Session["InsertFail"] = null;
            Session["InsertFailCnt"] = null;
            ViewBag.ErrorMsg = "Select upload file and type Sheet name,for example:Sheet1";
            ViewBag.ExcelStatus = "0";
            return View();
        }
        [HttpPost]
        public ActionResult DieselBookUpload(FormCollection collection)
        {
            OleDbConnection excelConnection = null;
            string connect = WebConfigurationManager.ConnectionStrings["ApplicationServices"].ToString();
            string filename = Request.Params[0];
            string sheetname = Request.Params[2];
            if (Validateupload(filename, sheetname))
            {
                String path = Server.MapPath("~/Content/uploadimages/");
                string file = path + filename;
               
                //Check whether file extension is xls or xslx
                string excelConnectionString = string.Empty;
                if (Path.GetExtension(file) == ".xls" || Path.GetExtension(file) == ".XLS")
                    excelConnectionString = @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source = '" + file + "';Extended Properties=\"Excel 8.0;HDR=YES;IMEX=2\"";
                else if (Path.GetExtension(file) == ".xlsx" || Path.GetExtension(file) == ".XLSX")
                    excelConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source='" + file + "';Extended Properties=\"Excel 8.0;HDR=YES;IMEX=2\"";

                excelConnection = new OleDbConnection(excelConnectionString);
                OleDbDataAdapter cmd = new OleDbDataAdapter("Select * from [" + sheetname + "$]", excelConnection);
                try
                {
                    DataTable dt = new DataTable();
                    DataTable dtInsertFail = new DataTable();
                    cmd.Fill(dt);
                    dt.Columns.Add("Error", typeof(string));
                    // Adding columns to temp datatable 
                    dtInsertFail.Columns.Add("FuelStation", typeof(string));
                    dtInsertFail.Columns.Add("BookNo", typeof(string));
                    dtInsertFail.Columns.Add("TokenNumber", typeof(string));
                    dtInsertFail.Columns.Add("Liters", typeof(float));
                    dtInsertFail.Columns.Add("RatePerLiter", typeof(float));
                    dtInsertFail.Columns.Add("Site", typeof(string));
                    dtInsertFail.Columns.Add("Error", typeof(string));

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        int intCol = 0; long FuelStationID = 0;
                        string FuelStation = dt.Rows[i].ItemArray[0].ToString().ToUpper().Trim();
                        string BookNo = dt.Rows[i].ItemArray[1].ToString().ToUpper().Trim();
                        string TokenNum = dt.Rows[i].ItemArray[2].ToString().ToUpper().Trim();
                        double Liter = Convert.ToDouble(dt.Rows[i].ItemArray[3].ToString().Trim());
                        double Rate = Convert.ToDouble(dt.Rows[i].ItemArray[4].ToString().Trim());
                        string Site = dt.Rows[i].ItemArray[5].ToString().ToUpper().Trim();
                        if (FuelStation != "")
                        {
                            if (VerifyFuelStation(FuelStation))
                            {
                                tbl_fuel_stations fuelStationDet = db.tbl_fuel_stations.Where(a => a.Status == true && a.FuelStation.Trim().ToUpper() == FuelStation).FirstOrDefault();
                                FuelStationID = fuelStationDet.ID;
                            }
                            else
                            {
                                tbl_fuel_stations objFuel = new tbl_fuel_stations();
                                objFuel.FuelStation = FuelStation;
                                objFuel.Status = true;
                                db.tbl_fuel_stations.AddObject(objFuel);
                                db.SaveChanges();
                                FuelStationID = objFuel.ID;
                            }
                        }
                        else
                        {
                            DataRow dr1 = dtInsertFail.NewRow();
                            foreach (DataColumn Col in dtInsertFail.Columns)
                            {
                                dr1[Col.ColumnName] = dt.Rows[i].ItemArray[intCol];
                                if (Col.ColumnName == "Error")
                                    dr1[Col.ColumnName] = "Please enter fuel station name and try again.";
                                intCol++;
                            }
                            intCol = 0;
                            dtInsertFail.Rows.Add(dr1);
                            continue; // continue proccess 
                        }
                        // Verify Book no and Token number ( book no,token no has not same in two records)
                        List<tbl_diesel_books> dieselList = db.tbl_diesel_books.Where(a => a.FuelStationID == FuelStationID && a.BookNo.ToUpper().Trim() == BookNo && a.TokenNumber.Trim() == TokenNum).ToList();
                        if (dieselList.Count >= 1)
                        {
                            string FuelStationName = db.tbl_fuel_stations.Where(a => a.ID == FuelStationID && a.Status == true).SingleOrDefault().FuelStation;
                            DataRow dr1 = dtInsertFail.NewRow();
                            foreach (DataColumn Col in dtInsertFail.Columns)
                            {
                                dr1[Col.ColumnName] = dt.Rows[i].ItemArray[intCol];
                                if (Col.ColumnName == "Error")
                                    dr1[Col.ColumnName] = "Diesel book record is  already available on this Fuel Station: " + FuelStationName + ", Book No.: " + BookNo + " , Token No.: " + TokenNum;
                                intCol++;
                            }
                            intCol = 0;
                            dtInsertFail.Rows.Add(dr1);
                            continue; // continue proccess 
                        }
                        tbl_diesel_books objDB = new tbl_diesel_books();
                        objDB.FuelStationID = FuelStationID;
                        objDB.BookNo = BookNo;
                        objDB.TokenNumber = TokenNum;
                        objDB.TokenValue = Liter;
                        objDB.PricePerLiter = (decimal)Rate;
                        objDB.CreatedBy = User.Identity.Name.ToUpper();
                        objDB.CreatedDate = DateTime.Now.Date;
                        objDB.Status = true;
                        objDB.Site = Site;
                        db.tbl_diesel_books.AddObject(objDB);
                        db.SaveChanges();
                    }
                    int updatedCount = dt.Rows.Count - dtInsertFail.Rows.Count;
                    ViewBag.ErrorMsg = "Success: Diesel Book Entries uploaded Successfully. Updated record(s) are " + updatedCount + " out of " + dt.Rows.Count + ".";
                    Session["InsertFail"] = dtInsertFail;
                    Session["InsertFailCnt"] = dtInsertFail.Rows.Count;
                    if (dtInsertFail.Rows.Count >= 1)
                        ViewBag.ExcelStatus = "1";
                    else
                        ViewBag.ExcelStatus = "0";
                    core.LoggingEntries("Masters ", "Diesel Book bulk upload has done.", User.Identity.Name);
                }
                catch (SqlException ex)
                {
                    ViewBag.ErrorMsg = "Invalid File Name/See Sample Excel Sheet Format" + ex.Message;
                }
                catch (Exception e)
                {
                    ViewBag.ErrorMsg = "Invalid File Name/See Sample Excel Sheet Format,Please try again." + e.Message;
                }
                return View();
            }
            return View();
        }
      
      
        #endregion

        #region Location Bulk Upload
        public ActionResult LocationUpload()
        {
            Session["InsertFail"] = null;
            Session["InsertFailCnt"] = null;
            ViewBag.ErrorMsg = "Select upload file and type Sheet name,for example:sheet1";
            ViewBag.ExcelStatus = "0";
            return View();
        }
        [HttpPost]
        public ActionResult LocationUpload(FormCollection collection)
        {
            OleDbConnection excelConnection = null;
            string connect = WebConfigurationManager.ConnectionStrings["ApplicationServices"].ToString();
            string filename = Request.Params[0];
            string sheetname = Request.Params[2];
            if (Validateupload(filename, sheetname))
            {
                String path = Server.MapPath("~/Content/uploadimages/");
                string file = path + filename;
                
                //Check whether file extension is xls or xslx
                string excelConnectionString = string.Empty;
                if (Path.GetExtension(file) == ".xls" || Path.GetExtension(file) == ".XLS")
                    excelConnectionString = @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source = '" + file + "';Extended Properties=\"Excel 8.0;HDR=YES;IMEX=2\"";
                else if (Path.GetExtension(file) == ".xlsx" || Path.GetExtension(file) == ".XLSX")
                    excelConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source='" + file + "';Extended Properties=\"Excel 8.0;HDR=YES;IMEX=2\"";

                excelConnection = new OleDbConnection(excelConnectionString);
                OleDbDataAdapter cmd = new OleDbDataAdapter("Select * from [" + sheetname + "$]", excelConnection);
                try
                {
                    DataTable dt = new DataTable();
                    DataTable dtInsertFail = new DataTable();
                    cmd.Fill(dt);
                    dt.Columns.Add("Error", typeof(string));
                    // Adding columns to temp datatable 
                    dtInsertFail.Columns.Add("Location", typeof(string));
                    dtInsertFail.Columns.Add("Error", typeof(string));
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        int intCol = 0;
                        string Location = dt.Rows[i].ItemArray[0].ToString().ToUpper().Trim();
                        if (Location == null || Location == "") //if Location Name are not found.
                        {
                            DataRow dr1 = dtInsertFail.NewRow();
                            foreach (DataColumn Col in dtInsertFail.Columns)
                            {
                                dr1[Col.ColumnName] = dt.Rows[i].ItemArray[intCol];
                                if (Col.ColumnName == "Error")
                                    dr1[Col.ColumnName] = "Please check location and try again.";
                                intCol++;
                            }
                            intCol = 0;
                            dtInsertFail.Rows.Add(dr1);
                            continue; // continue proccess 
                        }
                        else
                        {
                            if (db.tbl_locations.Where(a => a.Status == true && a.Location.Trim().ToUpper() == Location).Any()) // if Location name already in Database
                            {
                                DataRow dr1 = dtInsertFail.NewRow();
                                foreach (DataColumn Col in dtInsertFail.Columns)
                                {
                                    dr1[Col.ColumnName] = dt.Rows[i].ItemArray[intCol];
                                    if (Col.ColumnName == "Error")
                                        dr1[Col.ColumnName] = "You have already " + Location + " Location.";
                                    intCol++;
                                }
                                intCol = 0;
                                dtInsertFail.Rows.Add(dr1);
                                continue; // continue proccess 
                            }
                        }
                        tbl_locations _loc = new tbl_locations();
                        _loc.Location = Location;
                        _loc.Status = true;
                        db.tbl_locations.AddObject(_loc);
                        db.SaveChanges();
                    }
                    //SqlBulkCopy sqlBulk = new SqlBulkCopy(connect);
                    //sqlBulk.DestinationTableName = "tbl_diesel_tracking";
                    //sqlBulk.WriteToServer(ds);
                    int updatedCount = dt.Rows.Count - dtInsertFail.Rows.Count;
                    ViewBag.ErrorMsg = "Success: Location data uploaded Successfully. Updated record(s) are " + updatedCount + " out of " + dt.Rows.Count + ".";
                    Session["InsertFail"] = dtInsertFail;
                    Session["InsertFailCnt"] = dtInsertFail.Rows.Count;
                    if (dtInsertFail.Rows.Count >= 1)
                        ViewBag.ExcelStatus = "1";
                    else
                        ViewBag.ExcelStatus = "0";
                }
                catch (SqlException ex)
                {
                    ViewBag.ErrorMsg = "In Valid File Name/See Excel Sheet Sample Format" + ex.Message;
                }
                catch (Exception e)
                {
                    ViewBag.ErrorMsg = "Invalid File Name/See Excel Sheet Sample Format,Please try again." + e.Message;
                }
                return View();
            }
            return View();
        }
        #endregion

        #region Log sheet  Bulk upload
        public ActionResult LogSheetUpload()
        {
            Session["InsertFail"] = null;
            Session["InsertFailCnt"] = null;
            ViewBag.ErrorMsg = "Select upload file and type Sheet name,for example:sheet1";
            ViewBag.ExcelStatus = "0";
            return View();
        }
        [HttpPost]
        public ActionResult LogSheetUpload(FormCollection collection)
        {
            OleDbConnection excelConnection = null;
            string connect = WebConfigurationManager.ConnectionStrings["ApplicationServices"].ToString();
            string filename = Request.Params[0];
            string sheetname = Request.Params[2];
            if (Validateupload(filename, sheetname))
            {
                String path = Server.MapPath("~/Content/uploadimages/");
                string file = path + filename;

                //Check whether file extension is xls or xslx
                string excelConnectionString = string.Empty;
                if (Path.GetExtension(file) == ".xls" || Path.GetExtension(file) == ".XLS")
                    excelConnectionString = @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source = '" + file + "';Extended Properties=\"Excel 8.0;HDR=YES;IMEX=2\"";
                else if (Path.GetExtension(file) == ".xlsx" || Path.GetExtension(file) == ".XLSX")
                    excelConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source='" + file + "';Extended Properties=\"Excel 8.0;HDR=YES;IMEX=2\"";

                excelConnection = new OleDbConnection(excelConnectionString);
                OleDbDataAdapter cmd = new OleDbDataAdapter("Select * from [" + sheetname + "$]", excelConnection);
                try
                {
                    DataTable dt = new DataTable();
                    DataTable dtInsertFail = new DataTable();
                    cmd.Fill(dt);
                    dt.Columns.Add("Error", typeof(string));
                    // Adding columns to temp datatable 
                    dtInsertFail.Columns.Add("ClientName", typeof(string));
                    dtInsertFail.Columns.Add("VehicleRegNo", typeof(string));
                    dtInsertFail.Columns.Add("VehicleType", typeof(string));
                    dtInsertFail.Columns.Add("LogDate", typeof(DateTime));
                    dtInsertFail.Columns.Add("LogSheetNum", typeof(string));
                    dtInsertFail.Columns.Add("TripeType", typeof(string));
                    dtInsertFail.Columns.Add("Actual Pax", typeof(string));
                    dtInsertFail.Columns.Add("ShiftTime", typeof(string));
                    dtInsertFail.Columns.Add("Location", typeof(string));
                    dtInsertFail.Columns.Add("StartKM", typeof(string));
                    dtInsertFail.Columns.Add("EndKM", typeof(string));
                    dtInsertFail.Columns.Add("FinalApprovedKM", typeof(string));
                    dtInsertFail.Columns.Add("FinalSlabRate", typeof(string));
                    dtInsertFail.Columns.Add("PassengerEmpID", typeof(string));
                    dtInsertFail.Columns.Add("Remark", typeof(string));
                    dtInsertFail.Columns.Add("Travel Pax", typeof(string));
                    dtInsertFail.Columns.Add("FullLocation", typeof(string));
                    dtInsertFail.Columns.Add("UserName", typeof(string));
                    dtInsertFail.Columns.Add("Error", typeof(string));

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        int intCol = 0; string Location = string.Empty;
                        string ClientName = dt.Rows[i].ItemArray[0].ToString().ToUpper().Trim();
                        string VehicleRegNo = dt.Rows[i].ItemArray[1].ToString().ToUpper().Trim();
                        string VehicleType = dt.Rows[i].ItemArray[2].ToString().ToUpper().Trim();
                        string dtime = dt.Rows[i].ItemArray[3].ToString() == "" ? DateTime.Now.ToString() : dt.Rows[i].ItemArray[3].ToString();
                        string LogSheetNum = dt.Rows[i].ItemArray[4].ToString().ToUpper().Trim();
                        string TripeType = dt.Rows[i].ItemArray[5].ToString().ToUpper().Trim();
                        string Pax = dt.Rows[i].ItemArray[6].ToString();
                        string ShiftTime = dt.Rows[i].ItemArray[7].ToString().Trim();
                        Location = dt.Rows[i].ItemArray[8].ToString().ToUpper().Trim();
                        string StartKM = dt.Rows[i].ItemArray[9].ToString();
                        string EndKM = dt.Rows[i].ItemArray[10].ToString();
                        string FinalApprovedKM = dt.Rows[i].ItemArray[11].ToString();
                        string FinalSlabRate = dt.Rows[i].ItemArray[12].ToString();
                        string EmpID = dt.Rows[i].ItemArray[13].ToString().ToUpper().Trim();
                        string Remark = dt.Rows[i].ItemArray[14].ToString().ToUpper().Trim();
                        string travelPax = dt.Rows[i].ItemArray[15].ToString();
                        string fullLocation = dt.Rows[i].ItemArray[16].ToString();
                        string UserName = dt.Rows[i].ItemArray[17].ToString();
                        tbl_clients client = db.tbl_clients.Where(c => c.Status == true && c.CompanyName.Trim().ToUpper() == ClientName.Trim().ToUpper()).FirstOrDefault();
                        if (client == null)//if client details are not found on particular client name.
                        {
                            DataRow dr1 = dtInsertFail.NewRow();
                            foreach (DataColumn Col in dtInsertFail.Columns)
                            {
                                dr1[Col.ColumnName] = dt.Rows[i].ItemArray[intCol];
                                if (Col.ColumnName == "Error")
                                    dr1[Col.ColumnName] = "Please check client name and try again.";
                                intCol++;
                            }
                            intCol = 0;
                            dtInsertFail.Rows.Add(dr1);
                            continue; // continue proccess 
                        }
                        tbl_vehicles vehicle = db.tbl_vehicles.Where(v => v.Status == true && v.VehicleRegNum.ToUpper().Trim() == VehicleRegNo).FirstOrDefault();
                        if (vehicle == null) //if vehicle details are not found on particular vehicle registration number.
                        {
                            DataRow dr1 = dtInsertFail.NewRow();
                            foreach (DataColumn Col in dtInsertFail.Columns)
                            {
                                dr1[Col.ColumnName] = dt.Rows[i].ItemArray[intCol];
                                if (Col.ColumnName == "Error")
                                    dr1[Col.ColumnName] = "Please check vehicle registration number and try again.";
                                intCol++;
                            }
                            intCol = 0;
                            dtInsertFail.Rows.Add(dr1);
                            continue; // continue proccess 
                        }
                        tbl_drivers _driver = db.tbl_drivers.Where(d => d.Status == true && d.CurrentVehicleID == vehicle.ID).FirstOrDefault();
                        if (_driver == null)
                        {
                            DataRow dr1 = dtInsertFail.NewRow();
                            foreach (DataColumn Col in dtInsertFail.Columns)
                            {
                                dr1[Col.ColumnName] = dt.Rows[i].ItemArray[intCol];
                                if (Col.ColumnName == "Error")
                                    dr1[Col.ColumnName] = "Please check vehicle whether driver has assigned or not";
                                intCol++;
                            }
                            intCol = 0;
                            dtInsertFail.Rows.Add(dr1);
                            continue; // continue proccess 
                        }
                        if (EmpID != "")
                        {
                            tbl_slab_client_rate _slab = db.tbl_slab_client_rate.Where(a => a.Status == true && a.EmpId.ToUpper().Trim() == EmpID.ToUpper().Trim() && a.ClientID == client.ID).FirstOrDefault();
                            if (_slab != null)
                                Location = _slab.Location;
                        }
                        tbl_vehicle_types vehType = db.tbl_vehicle_types.Where(a => a.VehicleType.ToUpper().Trim() == VehicleType && a.Status == true).FirstOrDefault();
                        if (vehType == null) //if vehicle type is not found 
                        {
                            DataRow dr1 = dtInsertFail.NewRow();
                            foreach (DataColumn Col in dtInsertFail.Columns)
                            {
                                dr1[Col.ColumnName] = dt.Rows[i].ItemArray[intCol];
                                if (Col.ColumnName == "Error")
                                    dr1[Col.ColumnName] = "Please check vehicle type or please check with entered spelling here available keywords are AC,NON-AC";
                                intCol++;
                            }
                            intCol = 0;
                            dtInsertFail.Rows.Add(dr1);
                            continue; // continue proccess 
                        }
                        decimal slabRate = new core().GetSLABRate(client, vehicle, EmpID, Location, vehType.ID);
                        if (client.tbl_billing_types.BillingType.ToUpper() == "TRIPS")
                        {
                            if (slabRate == 0)
                            {
                                DataRow dr1 = dtInsertFail.NewRow();
                                foreach (DataColumn Col in dtInsertFail.Columns)
                                {
                                    dr1[Col.ColumnName] = dt.Rows[i].ItemArray[intCol];
                                    if (Col.ColumnName == "Error")
                                        dr1[Col.ColumnName] = "Slab rate does not exists";
                                    intCol++;
                                }
                                intCol = 0;
                                dtInsertFail.Rows.Add(dr1);
                                continue; // continue proccess 
                            }
                        }
                        int ApprovedKM = new core().GetApprovedKMByLocation(Location, client.ID, vehicle.ID);
                        if (client.tbl_billing_types.BillingType.ToUpper() == "KILO METER")
                        {
                            if (ApprovedKM == 0)
                            {
                                DataRow dr1 = dtInsertFail.NewRow();
                                foreach (DataColumn Col in dtInsertFail.Columns)
                                {
                                    dr1[Col.ColumnName] = dt.Rows[i].ItemArray[intCol];
                                    if (Col.ColumnName == "Error")
                                        dr1[Col.ColumnName] = "Approved KM does not exists";
                                    intCol++;
                                }
                                intCol = 0;
                                dtInsertFail.Rows.Add(dr1);
                                continue; // continue proccess 
                            }
                        }
                        if (db.tbl_log_sheets.Where(a => a.LogSheetNum.ToUpper().Trim() == LogSheetNum && a.Status == true).Any())
                        {

                            DataRow dr1 = dtInsertFail.NewRow();
                            foreach (DataColumn Col in dtInsertFail.Columns)
                            {
                                dr1[Col.ColumnName] = dt.Rows[i].ItemArray[intCol];
                                if (Col.ColumnName == "Error")
                                    dr1[Col.ColumnName] = "Log sheet number has already exists.";
                                intCol++;
                            }
                            intCol = 0;
                            dtInsertFail.Rows.Add(dr1);
                            continue; // continue proccess 

                        }
                        tbl_log_sheets LS = new tbl_log_sheets();
                        LS.ClientID = client.ID;
                        LS.VehicleID = vehicle.ID;
                        LS.VehicleTypeID = vehType.ID;
                        LS.VehicleModelID = vehicle.VehicleModelID;
                        LS.DriverID = _driver.ID;
                        LS.SeaterID = vehicle.SeaterId;
                        LS.LogDate = Convert.ToDateTime(dtime);
                        LS.LogSheetNum = LogSheetNum;
                        LS.TripeType = (TripeType == "PICK" ? "PICKUP" : TripeType);
                        LS.Pax = Pax == "" ? 0 : Convert.ToInt32(Pax);
                        LS.ShiftTime = ShiftTime;
                        LS.Location = Location;
                        LS.StartKM = StartKM == "" ? 0 : Convert.ToInt32(StartKM);
                        LS.EndKM = EndKM == "" ? 0 : Convert.ToInt32(EndKM);
                        LS.TotalKM = LS.EndKM - LS.StartKM;
                        LS.Approved = ApprovedKM;
                        LS.FinalApprovedKM = FinalApprovedKM == "" ? 0 : Convert.ToInt32(FinalApprovedKM);
                        LS.PassengerEmpID = EmpID;
                        LS.CreatedDt = DateTime.Now;
                        LS.UserName = string.IsNullOrEmpty(UserName) ? User.Identity.Name.ToString().ToUpper() : UserName.ToUpper();
                        LS.SlabRate = slabRate;
                        LS.FinalSlabRate = FinalSlabRate == "" ? 0 : Convert.ToDecimal(FinalSlabRate);
                        LS.Remark = Remark;
                        LS.TravelPax = travelPax == "" ? 0 : Convert.ToInt32(travelPax);
                        LS.FullLocation = fullLocation;
                        LS.Status = true;
                        core.ConvertToUppercase(LS);
                        db.tbl_log_sheets.AddObject(LS);
                        db.SaveChanges();
                    }
                    int updatedCount = dt.Rows.Count - dtInsertFail.Rows.Count;
                    ViewBag.ErrorMsg = "Success: Log Sheet Entries has uploaded Successfully. Updated record(s) are " + updatedCount + " out of " + dt.Rows.Count + ".";
                    Session["InsertFail"] = dtInsertFail;
                    Session["InsertFailCnt"] = dtInsertFail.Rows.Count;
                    if (dtInsertFail.Rows.Count >= 1)
                        ViewBag.ExcelStatus = "1";
                    else
                        ViewBag.ExcelStatus = "0";
                    core.LoggingEntries("LogSheet Mgmt.", "Bulk Upload for LogSheets has done.", User.Identity.Name);
                }
                catch (SqlException ex)
                {
                    ViewBag.ErrorMsg = "Invalid File Name/See Sample Excel Sheet Format" + ex.Message;
                }
                catch (Exception e)
                {
                    ViewBag.ErrorMsg = "Invalid File Name/See Sample Excel Sheet Format,Please try again." + e.Message;
                }
                return View();
            }
            return View();
        }
       
        #endregion

        #region Mis Report Upload
        public ActionResult MISUpload()
        {
            Session["InsertFail"] = null;
            Session["InsertFailCnt"] = null;
            ViewBag.ErrorMsg = "Select upload file and type Sheet name,for example:sheet1";
            ViewBag.ExcelStatus = "0";
            return View();
        }
        [HttpPost]
        public ActionResult MISUpload(FormCollection collection)
        {
            OleDbConnection excelConnection = null;
            string connect = WebConfigurationManager.ConnectionStrings["ApplicationServices"].ToString();
            string filename = Request.Params[0];
            string sheetname = Request.Params[2];
            if (Validateupload(filename, sheetname))
            {
                String path = Server.MapPath("~/Content/uploadimages/");
                string file = path + filename;

                //Check whether file extension is xls or xslx
                string excelConnectionString = string.Empty;
                if (Path.GetExtension(file) == ".xls" || Path.GetExtension(file) == ".XLS")
                    excelConnectionString = @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source = '" + file + "';Extended Properties=\"Excel 8.0;HDR=YES;IMEX=2\"";
                else if (Path.GetExtension(file) == ".xlsx" || Path.GetExtension(file) == ".XLSX")
                    excelConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source='" + file + "';Extended Properties=\"Excel 8.0;HDR=YES;IMEX=2\"";

                excelConnection = new OleDbConnection(excelConnectionString);
                OleDbDataAdapter cmd = new OleDbDataAdapter("Select * from [" + sheetname + "$]", excelConnection);
                try
                {
                    DataTable dt = new DataTable();
                    DataTable dtInsertFail = new DataTable();
                    cmd.Fill(dt);
                    dt.Columns.Add("Error", typeof(string));
                    // Adding columns to temp datatable 
                    dtInsertFail.Columns.Add("LogDate", typeof(DateTime));
                    dtInsertFail.Columns.Add("Client", typeof(string));
                    dtInsertFail.Columns.Add("VehicleReg", typeof(string));
                    dtInsertFail.Columns.Add("VehicleType", typeof(string));
                    dtInsertFail.Columns.Add("VehicleModel", typeof(string));
                    dtInsertFail.Columns.Add("Driver", typeof(string));
                    dtInsertFail.Columns.Add("Seater", typeof(string));
                    dtInsertFail.Columns.Add("LogSheetNum", typeof(string));
                    dtInsertFail.Columns.Add("TripeType", typeof(string));
                    dtInsertFail.Columns.Add("Pax", typeof(string));
                    dtInsertFail.Columns.Add("ShiftTime", typeof(string));
                    dtInsertFail.Columns.Add("Location", typeof(string));
                    dtInsertFail.Columns.Add("StartKM", typeof(string));
                    dtInsertFail.Columns.Add("EndKM", typeof(string));
                    dtInsertFail.Columns.Add("TotalKM", typeof(string));
                    dtInsertFail.Columns.Add("Approved", typeof(string));
                    dtInsertFail.Columns.Add("SlabRate", typeof(string));
                    dtInsertFail.Columns.Add("PassengerEmpID", typeof(string));
                    dtInsertFail.Columns.Add("FinalApprovedKM", typeof(string));
                    dtInsertFail.Columns.Add("FinalSlabRate", typeof(string));
                    dtInsertFail.Columns.Add("Error", typeof(string));

                    LogsheetManagementController objLs = new LogsheetManagementController();

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        int intCol = 0; string Location = string.Empty;

                        string dtime = dt.Rows[i].ItemArray[0].ToString() == "" ? DateTime.Now.ToString() : dt.Rows[i].ItemArray[0].ToString();
                        string ClientName = dt.Rows[i].ItemArray[1].ToString().ToUpper().Trim();
                        string VehicleRegNo = dt.Rows[i].ItemArray[2].ToString().ToUpper().Trim();
                        string VehicleType = dt.Rows[i].ItemArray[3].ToString().ToUpper().Trim();
                        string VehicleModel = dt.Rows[i].ItemArray[4].ToString().ToUpper().Trim();
                        string LogSheetNum = dt.Rows[i].ItemArray[7].ToString().ToUpper().Trim();
                        string TripeType = dt.Rows[i].ItemArray[8].ToString().ToUpper().Trim();
                        string Pax = dt.Rows[i].ItemArray[9].ToString();
                        string ShiftTime = dt.Rows[i].ItemArray[10].ToString().Trim();
                        string TravelPax = dt.Rows[i].ItemArray[11].ToString().Trim();
                        Location = dt.Rows[i].ItemArray[12].ToString().ToUpper().Trim();
                        string StartKM = dt.Rows[i].ItemArray[13].ToString();
                        string EndKM = dt.Rows[i].ItemArray[14].ToString();
                        string EmpID = dt.Rows[i].ItemArray[17].ToString();
                        string FinalApprovedKM = dt.Rows[i].ItemArray[19].ToString();
                        string FinalSlabRate = dt.Rows[i].ItemArray[20].ToString();

                        tbl_clients client = db.tbl_clients.Where(c => c.Status == true && c.CompanyName.Trim().ToUpper() == ClientName.Trim().ToUpper()).FirstOrDefault();
                        if (client == null)//if client details are not found on particular client name.
                        {
                            DataRow dr1 = dtInsertFail.NewRow();
                            foreach (DataColumn Col in dtInsertFail.Columns)
                            {
                                dr1[Col.ColumnName] = dt.Rows[i].ItemArray[intCol];
                                if (Col.ColumnName == "Error")
                                    dr1[Col.ColumnName] = "Please check client name and try again.";
                                intCol++;
                            }
                            intCol = 0;
                            dtInsertFail.Rows.Add(dr1);
                            continue; // continue proccess 
                        }
                        tbl_vehicles vehicle = db.tbl_vehicles.Where(v => v.Status == true && v.VehicleRegNum.ToUpper().Trim() == VehicleRegNo).FirstOrDefault();
                        if (vehicle == null) //if vehicle details are not found on particular vehicle registration number.
                        {
                            DataRow dr1 = dtInsertFail.NewRow();
                            foreach (DataColumn Col in dtInsertFail.Columns)
                            {
                                dr1[Col.ColumnName] = dt.Rows[i].ItemArray[intCol];
                                if (Col.ColumnName == "Error")
                                    dr1[Col.ColumnName] = "Please check vehicle registration number and try again.";
                                intCol++;
                            }
                            intCol = 0;
                            dtInsertFail.Rows.Add(dr1);
                            continue; // continue proccess 
                        }
                        tbl_drivers _driver = db.tbl_drivers.Where(d => d.Status == true && d.CurrentVehicleID == vehicle.ID).FirstOrDefault();
                        if (_driver == null)
                        {
                            DataRow dr1 = dtInsertFail.NewRow();
                            foreach (DataColumn Col in dtInsertFail.Columns)
                            {
                                dr1[Col.ColumnName] = dt.Rows[i].ItemArray[intCol];
                                if (Col.ColumnName == "Error")
                                    dr1[Col.ColumnName] = "Please check vehicle whether driver has assigned or not";
                                intCol++;
                            }
                            intCol = 0;
                            dtInsertFail.Rows.Add(dr1);
                            continue; // continue proccess 
                        }
                        if (EmpID != "")
                        {
                            tbl_slab_client_rate _slab = db.tbl_slab_client_rate.Where(a => a.Status == true && a.EmpId.ToUpper().Trim() == EmpID.ToUpper().Trim() && a.ClientID == client.ID).FirstOrDefault();
                            if (_slab != null)
                                Location = _slab.Location;
                        }
                        tbl_vehicle_types vehType = db.tbl_vehicle_types.Where(a => a.VehicleType.ToUpper().Trim() == VehicleType && a.Status == true).FirstOrDefault();
                        if (vehType == null) //if vehicle type is not found 
                        {
                            DataRow dr1 = dtInsertFail.NewRow();
                            foreach (DataColumn Col in dtInsertFail.Columns)
                            {
                                dr1[Col.ColumnName] = dt.Rows[i].ItemArray[intCol];
                                if (Col.ColumnName == "Error")
                                    dr1[Col.ColumnName] = "Please check vehicle type or please check with entered spelling here available keywords are AC,NON-AC";
                                intCol++;
                            }
                            intCol = 0;
                            dtInsertFail.Rows.Add(dr1);
                            continue; // continue proccess 
                        }
                        decimal slabRate = new core().GetSLABRate(client, vehicle, EmpID, Location, vehType.ID);
                        if (client.tbl_billing_types.BillingType.ToUpper() == "TRIPS")
                        {
                            if (slabRate == 0)
                            {
                                DataRow dr1 = dtInsertFail.NewRow();
                                foreach (DataColumn Col in dtInsertFail.Columns)
                                {
                                    dr1[Col.ColumnName] = dt.Rows[i].ItemArray[intCol];
                                    if (Col.ColumnName == "Error")
                                        dr1[Col.ColumnName] = "Slab rate does not exists";
                                    intCol++;
                                }
                                intCol = 0;
                                dtInsertFail.Rows.Add(dr1);
                                continue; // continue proccess 
                            }
                        }
                        int ApprovedKM = new core().GetApprovedKMByLocation(Location, client.ID, vehicle.ID);
                        if (client.tbl_billing_types.BillingType.ToUpper() == "KILO METER")
                        {
                            if (ApprovedKM == 0)
                            {
                                DataRow dr1 = dtInsertFail.NewRow();
                                foreach (DataColumn Col in dtInsertFail.Columns)
                                {
                                    dr1[Col.ColumnName] = dt.Rows[i].ItemArray[intCol];
                                    if (Col.ColumnName == "Error")
                                        dr1[Col.ColumnName] = "Approved KM does not exists";
                                    intCol++;
                                }
                                intCol = 0;
                                dtInsertFail.Rows.Add(dr1);
                                continue; // continue proccess 
                            }
                        }
                        if (db.tbl_log_sheets.Where(a => a.LogSheetNum.Trim() == LogSheetNum && a.Status == true).Any())
                        {

                            tbl_log_sheets logsheetDet = db.tbl_log_sheets.Where(a => a.Status == true && a.ClientID == client.ID && a.VehicleID == vehicle.ID && a.LogSheetNum.ToUpper().Trim() == LogSheetNum).FirstOrDefault();

                            if (logsheetDet != null)
                            {
                                if (logsheetDet.FinalApprovedKM != null && logsheetDet.FinalApprovedKM != 0)
                                    if (!User.IsInRole("superadmin"))
                                    {
                                        DataRow dr1 = dtInsertFail.NewRow();
                                        foreach (DataColumn Col in dtInsertFail.Columns)
                                        {
                                            dr1[Col.ColumnName] = dt.Rows[i].ItemArray[intCol];
                                            if (Col.ColumnName == "Error")
                                                dr1[Col.ColumnName] = "User access is denied to modify final approved KM.Please contact superadmin.";
                                            intCol++;
                                        }
                                        intCol = 0;
                                        dtInsertFail.Rows.Add(dr1);
                                        continue; // continue proccess 
                                    }

                                if (logsheetDet.FinalSlabRate != null && logsheetDet.FinalSlabRate != 0)
                                    if (!User.IsInRole("superadmin"))
                                    {
                                        DataRow dr1 = dtInsertFail.NewRow();
                                        foreach (DataColumn Col in dtInsertFail.Columns)
                                        {
                                            dr1[Col.ColumnName] = dt.Rows[i].ItemArray[intCol];
                                            if (Col.ColumnName == "Error")
                                                dr1[Col.ColumnName] = "User access is denied to modify final slab rate.Please contact superadmin.";
                                            intCol++;
                                        }
                                        intCol = 0;
                                        dtInsertFail.Rows.Add(dr1);
                                        continue; // continue proccess 
                                    }


                                core.ConvertToUppercase(logsheetDet);
                                TryUpdateModel(logsheetDet);
                                logsheetDet.FinalSlabRate = string.IsNullOrEmpty(FinalSlabRate) ? 0 : Convert.ToDecimal(FinalSlabRate);
                                logsheetDet.FinalApprovedKM = string.IsNullOrEmpty(FinalApprovedKM) ? 0 : Convert.ToInt32(FinalApprovedKM);
                                logsheetDet.ModifiedBy = Session["UserName"] == null ? "" : Session["UserName"].ToString();
                                logsheetDet.ModifiedDt = DateTime.Now;
                                logsheetDet.AuditDt = DateTime.Now;
                                logsheetDet.AuditedBy = Session["UserName"] == null ? "" : Session["UserName"].ToString();
                                db.SaveChanges();
                            }
                            //else
                            //{
                            //    DataRow dr1 = dtInsertFail.NewRow();
                            //    foreach (DataColumn Col in dtInsertFail.Columns)
                            //    {
                            //        dr1[Col.ColumnName] = dt.Rows[i].ItemArray[intCol];
                            //        if (Col.ColumnName == "Error")
                            //            dr1[Col.ColumnName] = "Log sheet number already exists.";
                            //        intCol++;
                            //    }
                            //    intCol = 0;
                            //    dtInsertFail.Rows.Add(dr1);
                            //    continue; // continue proccess 
                            //}
                        }
                        else
                        {
                            tbl_log_sheets logsheetDet = new tbl_log_sheets(); 
                            logsheetDet.ClientID = client.ID;
                            logsheetDet.VehicleID = vehicle.ID;
                            logsheetDet.VehicleTypeID = vehType.ID;
                            logsheetDet.VehicleModelID = vehicle.VehicleModelID;
                            logsheetDet.DriverID = _driver.ID;
                            logsheetDet.SeaterID = vehicle.SeaterId;
                            logsheetDet.LogDate = Convert.ToDateTime(dtime);
                            logsheetDet.LogSheetNum = LogSheetNum;
                            logsheetDet.TripeType = (TripeType == "PICK" ? "PICKUP" : TripeType);
                            logsheetDet.Pax = Pax == "" ? 0 : Convert.ToInt32(Pax);
                            logsheetDet.TravelPax = TravelPax == "" ? 0 : Convert.ToInt32(TravelPax);
                            logsheetDet.ShiftTime = ShiftTime;
                            logsheetDet.CreatedDt = DateTime.Now;
                            logsheetDet.Location = Location;
                            logsheetDet.StartKM = StartKM == "" ? 0 : Convert.ToInt32(StartKM);
                            logsheetDet.EndKM = EndKM == "" ? 0 : Convert.ToInt32(EndKM);
                            logsheetDet.TotalKM = logsheetDet.EndKM - logsheetDet.StartKM;
                            logsheetDet.Approved = ApprovedKM;
                            logsheetDet.FinalApprovedKM = FinalApprovedKM == "" ? 0 : Convert.ToInt32(FinalApprovedKM);
                            logsheetDet.PassengerEmpID = EmpID;
                            logsheetDet.SlabRate = slabRate;
                            logsheetDet.FinalSlabRate = FinalSlabRate == "" ? 0 : Convert.ToDecimal(FinalSlabRate);
                            logsheetDet.UserName = User.Identity.Name;
                            logsheetDet.Status = true;
                            db.tbl_log_sheets.AddObject(logsheetDet);
                            db.SaveChanges();
                        }
                    }
                    int updatedCount = dt.Rows.Count - dtInsertFail.Rows.Count;
                    ViewBag.ErrorMsg = "Success: Log Sheet Entries has uploaded Successfully. Updated record(s) are " + updatedCount + " out of " + dt.Rows.Count + ".";
                    Session["InsertFail"] = dtInsertFail;
                    Session["InsertFailCnt"] = dtInsertFail.Rows.Count;
                    if (dtInsertFail.Rows.Count >= 1)
                        ViewBag.ExcelStatus = "1";
                    else
                        ViewBag.ExcelStatus = "0";
                }
                catch (SqlException ex)
                {
                    ViewBag.ErrorMsg = "Invalid File Name/See Sample Excel Sheet Format" + ex.Message;
                }
                catch (Exception e)
                {
                    ViewBag.ErrorMsg = "Invalid File Name/See Sample Excel Sheet Format,Please try again." + e.Message;
                }
                return View();
            }
            return View();
        }
     
        #endregion

        #region Bulk Upload for KM rate
        public ActionResult KMUpload(long ClientID)
        {
            Session["InsertFail"] = null;
            Session["InsertFailCnt"] = null;
            ViewBag.ClientID = ClientID;
            ViewBag.ErrorMsg = "Select upload file and type Sheet name,for example:sheet1";
            ViewBag.ExcelStatus = "0";
            return View();
        }
                [HttpPost]
        public ActionResult KMUpload(FormCollection collection)
        {
            string connect = WebConfigurationManager.ConnectionStrings["ApplicationServices"].ToString();
            string filename = Request.Params[0];
            string sheetname = Request.Params[2];
            long ClientID = Convert.ToInt64(collection["ClientID"]);

            ViewBag.ClientID = ClientID;
            tbl_clients client = db.tbl_clients.Where(c => c.Status == true && c.ID == ClientID).SingleOrDefault();
            try
            {
                if (Validateupload(filename, sheetname))
                {
                    KMBulkUpload(filename, sheetname, ClientID);
                    core.LoggingEntries("Client Mgmt.", "client Kilo meter rate bulk upload has done", User.Identity.Name);
                }
            }
            catch (SqlException ex)
            {
                ViewBag.ErrorMsg = "In Valid File Name/See Excel Sheet Sample Format" + ex.Message;
            }
            catch (Exception e)
            {
                ViewBag.ErrorMsg = "Invalid File Name/See Excel Sheet Sample Format,Please try again." + e.Message;
            }
            return View();
        }
        private void KMBulkUpload(string filename, string sheetname, long ClientID)
        {
            tbl_clients client = db.tbl_clients.Where(c => c.Status == true && c.ID == ClientID).SingleOrDefault();
            OleDbConnection excelConnection = null;
            String path = Server.MapPath("~/Content/uploadimages/");
            string file = path + filename;

            //Check whether file extension is xls or xslx
            string excelConnectionString = string.Empty;
            if (Path.GetExtension(file) == ".xls" || Path.GetExtension(file) == ".XLS")
                excelConnectionString = @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source = '" + file + "';Extended Properties=\"Excel 8.0;HDR=YES;IMEX=2\"";
            else if (Path.GetExtension(file) == ".xlsx" || Path.GetExtension(file) == ".XLSX")
                excelConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source='" + file + "';Extended Properties=\"Excel 8.0;HDR=YES;IMEX=2\"";

            excelConnection = new OleDbConnection(excelConnectionString);
            OleDbDataAdapter cmd = new OleDbDataAdapter("Select * from [" + sheetname + "$]", excelConnection);
            DataTable dt = new DataTable();
            DataTable dtInsertFail = new DataTable();
            cmd.Fill(dt);
            dt.Columns.Add("Error", typeof(string));
            // Adding columns to temp datatable 

            dtInsertFail.Columns.Add("Location", typeof(string));
            dtInsertFail.Columns.Add("ApprovedKM", typeof(string));
            dtInsertFail.Columns.Add("ClientRate", typeof(string));
            dtInsertFail.Columns.Add("Error", typeof(string));

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string Location = dt.Rows[i].ItemArray[0].ToString().ToUpper().Trim();
                decimal ApprovedKM = dt.Rows[i].ItemArray[1].ToString() == "" ? 0 : Convert.ToDecimal(dt.Rows[i].ItemArray[1].ToString());
                string ClientRate = (dt.Rows[i].ItemArray[2] == null || dt.Rows[i].ItemArray[2] == "") ? "" : dt.Rows[i].ItemArray[2].ToString();
                int RndAppKM = Convert.ToInt32(Math.Round(ApprovedKM));
                // Insert Here

                tbl_km_client_rate objKm = new tbl_km_client_rate();
                objKm.ClientID = ClientID;
                objKm.ApprovedKM = RndAppKM;
                objKm.Status = true;
                objKm.Location = Location;
                objKm.ClientRate = ClientRate == "" ? 0 : Convert.ToDouble(ClientRate);

                db.tbl_km_client_rate.AddObject(objKm);
                db.SaveChanges();


            }
            int updatedCount = dt.Rows.Count - dtInsertFail.Rows.Count;
            ViewBag.ErrorMsg = "Success: Kilo Meter rate data uploaded Successfully. Updated record(s) are " + updatedCount + " out of " + dt.Rows.Count + ".";
            Session["InsertFail"] = dtInsertFail;
            Session["InsertFailCnt"] = dtInsertFail.Rows.Count;
            if (dtInsertFail.Rows.Count >= 1)
                ViewBag.ExcelStatus = "1";
            else
                ViewBag.ExcelStatus = "0";
        }
        #endregion

        #region Bulk Upload for SLAB Rate
        public ActionResult SLABUpload(long ClientID, string LocBased, string EmpBased)
        {
            Session["InsertFail"] = null;
            Session["InsertFailCnt"] = null;
            ViewBag.ClientID = ClientID;
            ViewBag.LocBased = LocBased;
            ViewBag.EmpBased = EmpBased;
            ViewBag.ErrorMsg = "Select upload file and type Sheet name,for example:sheet1";
            ViewBag.ExcelStatus = "0";
            return View();
        }

        [HttpPost]
        public ActionResult SLABUpload(FormCollection collection, HttpPostedFileBase slabdatafile)
        {

            _fileStore.SaveUploadedFile(slabdatafile);


            string connect = WebConfigurationManager.ConnectionStrings["ApplicationServices"].ToString();
            string filename = slabdatafile.FileName;
            string sheetname = collection["sheetname"].ToString();
            long ClientID = Convert.ToInt64(collection["ClientID"]);
            bool checkLoc = collection["hdnLocationBased"] == "1" ? true : false;
            bool checkEmp = collection["hdnEmpIdBased"] == "1" ? true : false;
            ViewBag.ClientID = ClientID;
            tbl_clients client = db.tbl_clients.Where(c => c.Status == true && c.ID == ClientID).SingleOrDefault();
            try
            {
                if (Validateupload(filename, sheetname))
                {
                    if (checkLoc == true)
                    {
                        LocationBasedBulkUpload(filename, sheetname, ClientID);
                        client.IsEmpBased = false;
                        TryUpdateModel(client);
                        db.SaveChanges();
                        core.LoggingEntries("Client Mgmt.", "client slab rate bulk upload has done", User.Identity.Name);
                    }
                    else
                    {
                        EmployeeBasedBulkUpload(filename, sheetname, ClientID);
                        client.IsEmpBased = true;
                        TryUpdateModel(client);
                        db.SaveChanges();
                        core.LoggingEntries("Client Mgmt.", "client slab rate bulk upload has done", User.Identity.Name);
                    }
                }
            }
            catch (SqlException ex)
            {
                ViewBag.ErrorMsg = "In Valid File Name/See Excel Sheet Sample Format" + ex.Message;
            }
            catch (Exception e)
            {
                ViewBag.ErrorMsg = "Invalid File Name/See Excel Sheet Sample Format,Please try again." + e.Message;
            }

            return View();

        }
        private void EmployeeBasedBulkUpload(string filename, string sheetname, long ClientID)
        {
            tbl_clients client = db.tbl_clients.Where(c => c.Status == true && c.ID == ClientID).SingleOrDefault();
            OleDbConnection excelConnection = null;
            String path = Server.MapPath("~/Content/uploadimages/");
            string file = path + filename;

            //Check whether file extension is xls or xslx
            string excelConnectionString = string.Empty;
            if (Path.GetExtension(file) == ".xls" || Path.GetExtension(file) == ".XLS")
                excelConnectionString = @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source = '" + file + "';Extended Properties=\"Excel 8.0;HDR=YES;IMEX=2\"";
            else if (Path.GetExtension(file) == ".xlsx" || Path.GetExtension(file) == ".XLSX")
                excelConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source='" + file + "';Extended Properties=\"Excel 8.0;HDR=YES;IMEX=2\"";

            excelConnection = new OleDbConnection(excelConnectionString);
            OleDbDataAdapter cmd = new OleDbDataAdapter("Select * from [" + sheetname + "$]", excelConnection);
            DataTable dt = new DataTable();
            DataTable dtInsertFail = new DataTable();
            cmd.Fill(dt);
            dt.Columns.Add("Error", typeof(string));
            // Adding columns to temp datatable 
            dtInsertFail.Columns.Add("EmpId", typeof(long));
            dtInsertFail.Columns.Add("EmpName", typeof(string));
            dtInsertFail.Columns.Add("EmpGender", typeof(string));
            dtInsertFail.Columns.Add("Location", typeof(string));
            dtInsertFail.Columns.Add("Seater", typeof(string));
            dtInsertFail.Columns.Add("VehicleType", typeof(string));
            dtInsertFail.Columns.Add("ClientSlab", typeof(string));
            dtInsertFail.Columns.Add("ClientRate", typeof(decimal));
            dtInsertFail.Columns.Add("VendorSlab", typeof(string));
            dtInsertFail.Columns.Add("VendorRate", typeof(decimal));
            dtInsertFail.Columns.Add("Error", typeof(string));
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string EmpId = dt.Rows[i].ItemArray[0] == null ? "" : dt.Rows[i].ItemArray[0].ToString().ToUpper().Trim();
                string EmpName = dt.Rows[i].ItemArray[1].ToString().ToUpper().Trim();
                string EmpGender = dt.Rows[i].ItemArray[2].ToString().ToUpper().Trim();

                string Location = dt.Rows[i].ItemArray[3].ToString().ToUpper().Trim();
                string Seater = dt.Rows[i].ItemArray[4].ToString().Trim().ToUpper();
                string VehicleType = dt.Rows[i].ItemArray[5].ToString().Trim().ToUpper();
                long SeaterID = 0, VehicleTypeID = 0; int intCol = 0;
                if (EmpId == "")
                {
                    DataRow dr1 = dtInsertFail.NewRow();
                    foreach (DataColumn Col in dtInsertFail.Columns)
                    {
                        dr1[Col.ColumnName] = dt.Rows[i].ItemArray[intCol];
                        if (Col.ColumnName == "Error")
                            dr1[Col.ColumnName] = "Please fill Emp Id column and try again.";
                        intCol++;
                    }
                    intCol = 0;
                    dtInsertFail.Rows.Add(dr1);
                    continue; // continue proccess 
                }
                if (Seater != "")
                {
                    if (!core.VerifySeater(Seater))
                    {
                        tbl_seaters _seater = db.tbl_seaters.Where(s => s.Status == true && s.Seater.ToUpper().Trim() == Seater.ToUpper().Trim()).SingleOrDefault();
                        SeaterID = _seater.ID;
                    }
                    else
                    {
                        tbl_seaters st = new tbl_seaters();
                        st.Seater = Seater;
                        st.Status = true;
                        db.tbl_seaters.AddObject(st);
                        db.SaveChanges();
                        SeaterID = st.ID;
                    }
                }
                else
                {
                    DataRow dr1 = dtInsertFail.NewRow();
                    foreach (DataColumn Col in dtInsertFail.Columns)
                    {
                        dr1[Col.ColumnName] = dt.Rows[i].ItemArray[intCol];
                        if (Col.ColumnName == "Error")
                            dr1[Col.ColumnName] = "Please fill Seater column and try again.";
                        intCol++;
                    }
                    intCol = 0;
                    dtInsertFail.Rows.Add(dr1);
                    continue; // continue proccess 
                }
                if (VehicleType != "")
                {
                    if (!core.VerifyVehicleType(VehicleType))
                    {
                        tbl_vehicle_types _types = db.tbl_vehicle_types.Where(s => s.Status == true && s.VehicleType.ToUpper().Trim() == VehicleType.ToUpper().Trim()).SingleOrDefault();
                        VehicleTypeID = _types.ID;
                    }
                    else
                    {
                        //tbl_vehicle_types VT = new tbl_vehicle_types();
                        //VT.VehicleType = VehicleType;
                        //VT.Status = true;
                        //db.tbl_vehicle_types.AddObject(VT);
                        //db.SaveChanges();
                        //VehicleTypeID = VT.ID;
                        DataRow dr1 = dtInsertFail.NewRow();
                        foreach (DataColumn Col in dtInsertFail.Columns)
                        {
                            dr1[Col.ColumnName] = dt.Rows[i].ItemArray[intCol];
                            if (Col.ColumnName == "Error")
                                dr1[Col.ColumnName] = "Please check vehicle type or please check with entered spelling here available keywords are AC,NON-AC";
                            intCol++;
                        }
                        intCol = 0;
                        dtInsertFail.Rows.Add(dr1);
                        continue; // continue proccess 
                    }

                }
                else
                {
                    DataRow dr1 = dtInsertFail.NewRow();
                    foreach (DataColumn Col in dtInsertFail.Columns)
                    {
                        dr1[Col.ColumnName] = dt.Rows[i].ItemArray[intCol];
                        if (Col.ColumnName == "Error")
                            dr1[Col.ColumnName] = "Please fill Vehicle Type column and try again.";
                        intCol++;
                    }
                    intCol = 0;
                    dtInsertFail.Rows.Add(dr1);
                    continue; // continue proccess 
                }
                string seaterName = core.GetSeater(SeaterID);
                string Type = core.GetVehicleType(VehicleTypeID);
                tbl_slab_client_rate _checkLoc = db.tbl_slab_client_rate.Where(s => s.Status == true && s.Location.ToUpper().Trim() == Location
                                                                            && s.ClientID == ClientID && s.SeaterID == SeaterID && s.VehicleTypeID == VehicleTypeID && s.EmpId.ToUpper().Trim() == EmpId.ToUpper().Trim()).SingleOrDefault();
                if (_checkLoc != null) //if location details are  found on particular Location Name. based on ClientID,SeaterID
                {
                    DataRow dr1 = dtInsertFail.NewRow();
                    foreach (DataColumn Col in dtInsertFail.Columns)
                    {
                        dr1[Col.ColumnName] = dt.Rows[i].ItemArray[intCol];
                        if (Col.ColumnName == "Error")
                            dr1[Col.ColumnName] = "TRIP Rate is available on this Location: " + Location + ", Client: " + client.CompanyName.ToUpper() + " , Seater: " + seaterName.ToUpper() + ", Vehicle Type: " + Type.ToUpper() + ", and Employee No: " + EmpId + "";
                        intCol++;
                    }
                    intCol = 0;
                    dtInsertFail.Rows.Add(dr1);
                    continue; // continue proccess 
                }
                string ClientSLAB = dt.Rows[i].ItemArray[6].ToString().ToUpper().Trim();
                double ClientRate = dt.Rows[i].ItemArray[7].ToString() == "" ? 0 : Convert.ToDouble(dt.Rows[i].ItemArray[7]);

                string VendorSLAB = dt.Rows[i].ItemArray[8].ToString().ToUpper().Trim();
                double VendorRate = dt.Rows[i].ItemArray[9].ToString() == "" ? 0 : Convert.ToDouble(dt.Rows[i].ItemArray[9]);

                tbl_slab_client_rate SB = new tbl_slab_client_rate();
                SB.ClientID = ClientID;
                SB.Location = Location;
                SB.SeaterID = SeaterID;
                SB.VehicleTypeID = VehicleTypeID;
                SB.ClientSlab = ClientSLAB;
                SB.ClientRate = ClientRate;
                SB.VendorSlab = VendorSLAB;
                SB.VendorRate = VendorRate;
                SB.EmpId = (EmpId == null || EmpId == "") ? null : EmpId;
                SB.EmpName = EmpName;
                SB.EmpGender = EmpGender;
                SB.Status = true;
                db.tbl_slab_client_rate.AddObject(SB);
                db.SaveChanges();
            }
            int updatedCount = dt.Rows.Count - dtInsertFail.Rows.Count;
            ViewBag.ErrorMsg = "Success: SLAB Rates data uploaded Successfully. Updated record(s) are " + updatedCount + " out of " + dt.Rows.Count + ".";
            Session["InsertFail"] = dtInsertFail;
            Session["InsertFailCnt"] = dtInsertFail.Rows.Count;
            if (dtInsertFail.Rows.Count >= 1)
                ViewBag.ExcelStatus = "1";
            else
                ViewBag.ExcelStatus = "0";
        }
        private void LocationBasedBulkUpload(string filename, string sheetname, long ClientID)
        {
            tbl_clients client = db.tbl_clients.Where(c => c.Status == true && c.ID == ClientID).SingleOrDefault();
            OleDbConnection excelConnection = null;
            String path = Server.MapPath("~/Content/uploadimages/");
            string file = path + filename;

          //Check whether file extension is xls or xslx
            string excelConnectionString = string.Empty;
            if (Path.GetExtension(file) == ".xls" || Path.GetExtension(file) == ".XLS")
                excelConnectionString = @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source = '" + file + "';Extended Properties=\"Excel 8.0;HDR=YES;IMEX=2\"";
            else if (Path.GetExtension(file) == ".xlsx" || Path.GetExtension(file) == ".XLSX")
                excelConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source='" + file + "';Extended Properties=\"Excel 8.0;HDR=YES;IMEX=2\"";

            excelConnection = new OleDbConnection(excelConnectionString);
            OleDbDataAdapter cmd = new OleDbDataAdapter("Select * from [" + sheetname + "$]", excelConnection);
            DataTable dt = new DataTable();
            DataTable dtInsertFail = new DataTable();
            cmd.Fill(dt);
            dt.Columns.Add("Error", typeof(string));
            // Adding columns to temp datatable 
            dtInsertFail.Columns.Add("Location", typeof(string));
            dtInsertFail.Columns.Add("Seater", typeof(string));
            dtInsertFail.Columns.Add("VehicleType", typeof(string));
            dtInsertFail.Columns.Add("ClientSlab", typeof(string));
            dtInsertFail.Columns.Add("ClientRate", typeof(decimal));
            dtInsertFail.Columns.Add("VendorSlab", typeof(string));
            dtInsertFail.Columns.Add("VendorRate", typeof(decimal));
            dtInsertFail.Columns.Add("Error", typeof(string));
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string Location = dt.Rows[i].ItemArray[0].ToString().ToUpper().Trim();
                string Seater = dt.Rows[i].ItemArray[1].ToString().Trim().ToUpper();
                string VehicleType = dt.Rows[i].ItemArray[2].ToString().Trim().ToUpper();
                long SeaterID = 0, VehicleTypeID = 0; int intCol = 0;
                if (Seater != "")
                {
                    if (!core.VerifySeater(Seater))
                    {
                        tbl_seaters _seater = db.tbl_seaters.Where(s => s.Status == true && s.Seater.ToUpper().Trim() == Seater.ToUpper().Trim()).SingleOrDefault();
                        SeaterID = _seater.ID;
                    }
                    else
                    {
                        tbl_seaters st = new tbl_seaters();
                        st.Seater = Seater;
                        st.Status = true;
                        db.tbl_seaters.AddObject(st);
                        db.SaveChanges();
                        SeaterID = st.ID;
                    }
                }
                else
                {
                    DataRow dr1 = dtInsertFail.NewRow();
                    foreach (DataColumn Col in dtInsertFail.Columns)
                    {
                        dr1[Col.ColumnName] = dt.Rows[i].ItemArray[intCol];
                        if (Col.ColumnName == "Error")
                            dr1[Col.ColumnName] = "Please fill Seater column and try again.";
                        intCol++;
                    }
                    intCol = 0;
                    dtInsertFail.Rows.Add(dr1);
                    continue; // continue proccess 
                }
                if (VehicleType != "")
                {
                    if (!core.VerifyVehicleType(VehicleType))
                    {
                        tbl_vehicle_types _types = db.tbl_vehicle_types.Where(s => s.Status == true && s.VehicleType.ToUpper().Trim() == VehicleType.ToUpper().Trim()).SingleOrDefault();
                        VehicleTypeID = _types.ID;
                    }
                    else
                    {
                        //tbl_vehicle_types VT = new tbl_vehicle_types();
                        //VT.VehicleType = VehicleType;
                        //VT.Status = true;
                        //db.tbl_vehicle_types.AddObject(VT);
                        //db.SaveChanges();
                        //VehicleTypeID = VT.ID;
                        DataRow dr1 = dtInsertFail.NewRow();
                        foreach (DataColumn Col in dtInsertFail.Columns)
                        {
                            dr1[Col.ColumnName] = dt.Rows[i].ItemArray[intCol];
                            if (Col.ColumnName == "Error")
                                dr1[Col.ColumnName] = "Please check vehicle type or please check with entered spelling here available keywords are AC,NON-AC";
                            intCol++;
                        }
                        intCol = 0;
                        dtInsertFail.Rows.Add(dr1);
                        continue; // continue proccess 
                    }

                }
                else
                {
                    DataRow dr1 = dtInsertFail.NewRow();
                    foreach (DataColumn Col in dtInsertFail.Columns)
                    {
                        dr1[Col.ColumnName] = dt.Rows[i].ItemArray[intCol];
                        if (Col.ColumnName == "Error")
                            dr1[Col.ColumnName] = "Please fill Vehicle Type column and try again.";
                        intCol++;
                    }
                    intCol = 0;
                    dtInsertFail.Rows.Add(dr1);
                    continue; // continue proccess 
                }
                string seaterName = core.GetSeater(SeaterID);
                string Type = core.GetVehicleType(VehicleTypeID);
                tbl_slab_client_rate _checkLoc = db.tbl_slab_client_rate.Where(s => s.Status == true && s.Location.ToUpper().Trim() == Location
                                                                            && s.ClientID == ClientID && s.SeaterID == SeaterID && s.VehicleTypeID == VehicleTypeID).SingleOrDefault();
                if (_checkLoc != null) //if location details are  found on particular Location Name. based on ClientID,SeaterID
                {
                    DataRow dr1 = dtInsertFail.NewRow();
                    foreach (DataColumn Col in dtInsertFail.Columns)
                    {
                        dr1[Col.ColumnName] = dt.Rows[i].ItemArray[intCol];
                        if (Col.ColumnName == "Error")
                            dr1[Col.ColumnName] = "TRIP Rate is available on this Location: " + Location + ", Client: " + client.CompanyName.ToUpper() + " , Seater: " + seaterName.ToUpper() + "  and Vehicle Type: " + Type.ToUpper() + "";
                        intCol++;
                    }
                    intCol = 0;
                    dtInsertFail.Rows.Add(dr1);
                    continue; // continue proccess 
                }
                string ClientSLAB = dt.Rows[i].ItemArray[3].ToString().ToUpper().Trim();
                double ClientRate = dt.Rows[i].ItemArray[4].ToString() == "" ? 0 : Convert.ToDouble(dt.Rows[i].ItemArray[4]);

                string VendorSLAB = dt.Rows[i].ItemArray[5].ToString().ToUpper().Trim();
                double VendorRate = dt.Rows[i].ItemArray[6].ToString() == "" ? 0 : Convert.ToDouble(dt.Rows[i].ItemArray[6]);
                string EmpId = string.Empty;
                string EmpName = string.Empty;
                string EmpGender = string.Empty;
                tbl_slab_client_rate SB = new tbl_slab_client_rate();
                SB.ClientID = ClientID;
                SB.Location = Location;
                SB.SeaterID = SeaterID;
                SB.VehicleTypeID = VehicleTypeID;
                SB.ClientSlab = ClientSLAB;
                SB.ClientRate = ClientRate;
                SB.VendorSlab = VendorSLAB;
                SB.VendorRate = VendorRate;
                SB.EmpId = EmpId;
                SB.EmpName = EmpName;
                SB.EmpGender = EmpGender;
                SB.Status = true;
                db.tbl_slab_client_rate.AddObject(SB);
                db.SaveChanges();
            }
            int updatedCount = dt.Rows.Count - dtInsertFail.Rows.Count;
            ViewBag.ErrorMsg = "Success: Location Based SLAB Rates data uploaded Successfully. Updated record(s) are " + updatedCount + " out of " + dt.Rows.Count + ".";
            Session["InsertFail"] = dtInsertFail;
            Session["InsertFailCnt"] = dtInsertFail.Rows.Count;
            if (dtInsertFail.Rows.Count >= 1)
                ViewBag.ExcelStatus = "1";
            else
                ViewBag.ExcelStatus = "0";
        }

      
       
        #endregion

        #region Methods
        private bool Validateupload(string filename, string sheetname)
        {
            if (String.IsNullOrEmpty(filename.ToString()))
                ModelState.AddModelError("upload", "Upload file is  required.");
            if (sheetname.Length == 0)
                ModelState.AddModelError("sheetname", "Type Sheet name.");
            return ModelState.IsValid;
        }
        public ActionResult NotUpdatedExportToExcel()
        {
            DataTable dt = (DataTable)Session["InsertFail"];
            int rowCnt = (int)Session["InsertFailCnt"];
            string Filename = "NotUpdated_" + DateTime.Now.ToString("hhmmss") + ".xlsx";
            MemoryStream ms = DownLoadExcel(dt, rowCnt);
            if (ms != null)
            {
                ms.Seek(0, SeekOrigin.Begin);
                return File(ms, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml", Filename);
            }
            return View();
        }
        public MemoryStream DownLoadExcel(DataTable table, int tbl_rowsCount)
        {
            string sheetName = "NotUpdated";
            var wb = new XLWorkbook();
            try
            {
                var ws = wb.Worksheets.Add(sheetName);
                int iCol = 0;
                // Add column headings...
                foreach (DataColumn Col in table.Columns)
                {
                    iCol++;
                    ws.Cell(3, iCol).SetValue(Col.ColumnName);
                    ws.Cell(3, iCol).Style.Font.Bold = true;
                    ws.Cell(3, iCol).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    ws.Cell(3, iCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
                // for each row of data...
                int iRow = 3;
                foreach (DataRow r in table.Rows)
                {
                    iRow++;
                    // add each row's cell data...
                    iCol = 0;
                    foreach (DataColumn c in table.Columns)
                    {
                        iCol++;
                        ws.Cell(iRow, iCol).SetValue(r[c.ColumnName]);
                    }
                }
                ws.Columns().AdjustToContents();
            }
            catch (SqlException ex)
            {

            }
            MemoryStream ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms;
        }
        public bool VerifyFuelStation(string FuelStation)
        {
            return db.tbl_fuel_stations.Where(a => a.FuelStation.Trim().ToUpper() == FuelStation && a.Status == true).Any();
        }
        public FileContentResult DownloadSampleFile(string ID)
        {
            string filePath = string.Empty;
            if (ID == "2")
                filePath = Path.Combine(Server.MapPath(@"~/Content/uploadimages/KiloMeterRate.xls"));
            else if (ID == "0")
                filePath = Path.Combine(Server.MapPath(@"~/Content/uploadimages/BasedOnEmpIDSLABRates.xls"));
            else
                filePath = Path.Combine(Server.MapPath(@"~/Content/uploadimages/BasedOnLocationSLABRates.xls"));

            var mimeType = "application/vnd.ms-excel";
            var fileContents = System.IO.File.ReadAllBytes(filePath);
            return new FileContentResult(fileContents, mimeType) { FileDownloadName = Path.GetFileName(filePath) };
        }
      
        #endregion
    }
}
