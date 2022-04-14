using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;
using ClosedXML.Excel;
using System.Data.SqlClient;
using AsyncUploaderDemo.Models;
using System.Globalization;
using System.IO;
using System.Data.OleDb;
using System.Web.Configuration;

namespace FMS.Controllers
{
    public class LocationController : Controller
    {
        #region ctors
        private FMSDBEntities db;
        private IFileStore _fileStore = new DiskFileStore();
        public string AsyncUpload()
        {
            return _fileStore.SaveUploadedFile(Request.Files[0]);
        }
        public LocationController()
        {
            db = new FMSDBEntities();
        }
        #endregion

        [Authorize]
        public ViewResult Index()
        {
            var locations = db.tbl_locations.Where(l => l.Status == true).ToList();
            return View(locations);
        }

        #region Add Location
        public ActionResult Create()
        {
            return View(new tbl_locations());
        }
        [HttpPost]
        public ActionResult Create(tbl_locations tbl_locations)
        {
            try
            {
                if (ValidateForm(tbl_locations))
                {
                    tbl_locations.Status = true;
                    db.tbl_locations.AddObject(tbl_locations);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                return View(tbl_locations);
            }
            return View(tbl_locations);
        }
        #endregion

        #region Edit Location
        public ActionResult Edit(long id)
        {
            tbl_locations tbl_locations = db.tbl_locations.Single(t => t.ID == id);
            return View(tbl_locations);
        }
        [HttpPost]
        public ActionResult Edit(long ID, tbl_locations tbl_locations)
        {
            try
            {
                if (ValidateForm(tbl_locations))
                {
                    tbl_locations.Status = true;
                    db.tbl_locations.Attach(tbl_locations);
                    db.ObjectStateManager.ChangeObjectState(tbl_locations, EntityState.Modified);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                return View(tbl_locations);
            }
            return View(tbl_locations);
        }
        #endregion

        #region Delete Location
        public ActionResult Delete(long id)
        {
            tbl_locations tbl_locations = db.tbl_locations.Single(t => t.ID == id);
            return View(tbl_locations);
        }
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(long id)
        {
            try
            {
                tbl_locations tbl_locations = db.tbl_locations.Single(t => t.ID == id);
                tbl_locations.Status = false;
                TryUpdateModel(tbl_locations);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return View();
            }
        }
        #endregion

        public bool ValidateForm(tbl_locations location)
        {

             if (location.Location == null || location.Location.ToString().Trim().Length == 0)
                 ModelState.AddModelError("Location", "Please enter Location.");
            
            if (location.RouteId == null || location.RouteId.ToString().Trim().Length == 0)
                 ModelState.AddModelError("RouteId", "Please enter RouteId.");
               
            return ModelState.IsValid;
        }
       

        #region Bulk Upload
        public ActionResult upload()
        {
            Session["InsertFail"] = null;
            Session["InsertFailCnt"] = null;
            ViewBag.ErrorMsg = "Select upload file and type Sheet name,for example:sheet1";
            ViewBag.ExcelStatus = "0";
            return View();
        }
        private bool Validateupload(string filename, string sheetname)
        {

            if (String.IsNullOrEmpty(filename.ToString()))
                ModelState.AddModelError("Upload", "Upload file is  required.");
            if (sheetname.Length == 0)
                ModelState.AddModelError("sheetname", "Type Sheet name.");


            return ModelState.IsValid;
        }
        [HttpPost]
        public ActionResult upload(FormCollection collection)
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
                    dtInsertFail.Columns.Add("RouteId", typeof(string));
                    dtInsertFail.Columns.Add("Error", typeof(string));
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        int intCol = 0;
                        string Location = dt.Rows[i].ItemArray[0].ToString().ToUpper().Trim();
                        string RouteId = dt.Rows[i].ItemArray[1].ToString().ToUpper().Trim();
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
                        if (RouteId == null || RouteId == "") //if RouteId Name are not found.
                        {
                            DataRow dr1 = dtInsertFail.NewRow();
                            foreach (DataColumn Col in dtInsertFail.Columns)
                            {
                                dr1[Col.ColumnName] = dt.Rows[i].ItemArray[intCol];
                                if (Col.ColumnName == "Error")
                                    dr1[Col.ColumnName] = "Please check RouteId and try again.";
                                intCol++;
                            }
                            intCol = 0;
                            dtInsertFail.Rows.Add(dr1);
                            continue; // continue proccess 
                        }
                        else
                        {
                            if (db.tbl_locations.Where(a => a.Status == true && a.RouteId.Trim().ToUpper() == RouteId).Any()) // if RouteId already in Database
                            {
                                DataRow dr1 = dtInsertFail.NewRow();
                                foreach (DataColumn Col in dtInsertFail.Columns)
                                {
                                    dr1[Col.ColumnName] = dt.Rows[i].ItemArray[intCol];
                                    if (Col.ColumnName == "Error")
                                        dr1[Col.ColumnName] = "You have already " + RouteId + " RouteId.";
                                    intCol++;
                                }
                                intCol = 0;
                                dtInsertFail.Rows.Add(dr1);
                                continue; // continue proccess 
                            }
                        }
                        tbl_locations _loc = new tbl_locations();
                        _loc.Location = Location;
                        _loc.RouteId = RouteId;
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
        public FileContentResult DownloadSampleFile()
        {
            var filePath = Path.Combine(Server.MapPath(@"~/Content/uploadimages/SampleLocationsData.xls"));
            var mimeType = "application/vnd.ms-excel";
            var fileContents = System.IO.File.ReadAllBytes(filePath);
            return new FileContentResult(fileContents, mimeType) { FileDownloadName = Path.GetFileName(filePath) };
        }
        #endregion
    }
}