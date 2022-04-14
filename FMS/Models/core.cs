using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FMS.Models;
using System.IO;
using System.Collections;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Web.Configuration;
using System.Net.Mail;
using System.Net;
using NLog;
using System.Data.Objects;
using System.Globalization;
using FMS.Controllers;
using System.Net.NetworkInformation;

namespace FMS.Models
{
    public class core
    {
        #region Intialization
        private FMSDBEntities db = new FMSDBEntities();
        private static FMSDBEntities _db = new FMSDBEntities();
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        #endregion

        #region Methods
        public string GetVehicleModel(long VehicleModelId)
        {
            try
            {
                return db.tbl_vehicle_models.Where(a => a.ID == VehicleModelId && a.Status == true).SingleOrDefault().VehicleModelName.ToString();
            }
            catch (Exception)
            {
                return "";
            }
        }

        public string GetVehicleType(long VehicleTypeId)
        {
            try
            {
                return db.tbl_vehicle_types.Where(a => a.ID == VehicleTypeId && a.Status == true).SingleOrDefault().VehicleType.ToString();
            }
            catch (Exception)
            {
                return "";
            }
        }

        public string GetBillingType(long BillingTypeId)
        {
            try
            {
                return db.tbl_billing_types.Where(a => a.ID == BillingTypeId && a.Status == true).SingleOrDefault().BillingType.ToString();
            }
            catch (Exception)
            {
                return "";
            }
        }

        public string GetSeater(long SeaterId)
        {
            try
            {
                return db.tbl_seaters.Where(a => a.ID == SeaterId && a.Status == true).SingleOrDefault().Seater.ToString();
            }
            catch (Exception)
            {
                return "";
            }
        }

        public string GetOwner(long VendorID)
        {
            string VendorName = string.Empty;
            try
            {
                if (VendorID != 0 || VendorID.ToString() != "" || VendorID != null)
                {
                    var vendor = db.tbl_vendor_details.Where(s => s.Status == true && s.ID == VendorID).Select(a => new { a.FirstName, a.LastName, a.ID }).SingleOrDefault();
                    VendorName = vendor.FirstName + " " + vendor.LastName;
                    return VendorName;
                }
                else
                    return VendorName;
            }
            catch (Exception)
            {
                return VendorName;
            }
        }

        public string GetDriver(long DriverID)
        {
            string DriverName = string.Empty;
            try
            {
                if (DriverID != 0 || DriverID.ToString() != "" || DriverID != null)
                {
                    var vDrivers = db.tbl_drivers.Where(s => s.Status == true && s.ID == DriverID).Select(a => new { a.FirstName, a.LastName, a.ID }).SingleOrDefault();
                    DriverName = vDrivers.FirstName + " " + vDrivers.LastName;
                    return DriverName;
                }
                else
                    return DriverName;
            }
            catch (Exception)
            {
                return DriverName;
            }
        }

        public string GetDriverByVehicle(long VId)
        {
            string DriverName = string.Empty;
            DriverName = string.Join(",", (from m in db.tbl_drivers where m.Status == true && m.CurrentVehicleID == VId select m.FirstName + " " + m.LastName));
            //DriverName =  string.Join(",", (string.IsNullOrEmpty(objDriver.FirstName) ? "" : objDriver.FirstName) + " " + (string.IsNullOrEmpty(objDriver.LastName) ? "" : objDriver.LastName));
            return DriverName;
        }
        public Int64 GetDriverIdByVehicle(long VId)
        {
            Int64 DriverName = 0;
            DriverName = (from m in db.tbl_drivers where m.Status == true && m.CurrentVehicleID == VId select m.ID).FirstOrDefault();
            // DriverName =  string.Join(",", (string.IsNullOrEmpty(objDriver.FirstName) ? "" : objDriver.FirstName) + " " + (string.IsNullOrEmpty(objDriver.LastName) ? "" : objDriver.LastName));
            return DriverName;
        }
        public string GetDriverByInActiveVehicle(long VId)
        {
            string DriverName = string.Empty;
            DriverName = string.Join(",", (from m in db.tbl_drivers where m.Status == false && m.CurrentVehicleID == VId select m.FirstName + " " + m.LastName));
            //DriverName =  string.Join(",", (string.IsNullOrEmpty(objDriver.FirstName) ? "" : objDriver.FirstName) + " " + (string.IsNullOrEmpty(objDriver.LastName) ? "" : objDriver.LastName));
            return DriverName;
        }

        public string GetClient(long ClientID)
        {
            string ClientName = string.Empty;
            try
            {   //|| ClientID != null
                if (ClientID != 0 || ClientID.ToString() != "")
                {
                    var vClients = db.tbl_clients.Where(s => s.Status == true && s.ID == ClientID).Select(a => new { a.CompanyName, a.ID }).SingleOrDefault();
                    ClientName = vClients.CompanyName;
                    return ClientName;
                }
                else
                    return ClientName;
            }
            catch (Exception)
            {
                return "";
            }
        }

        public string GetProject(long ProjectId)
        {
            string ProjectName = string.Empty;
            try
            { // || ProjectId != null
                if (ProjectId != 0 || ProjectId.ToString() != "")
                {
                    var ProjectDet = db.tbl_projects.Where(s => s.IsActive == true && s.Id == ProjectId).Select(a => new { a.ProjectName, a.Id }).SingleOrDefault();
                    ProjectName = ProjectDet.ProjectName;
                    return ProjectName;
                }
                else
                    return ProjectName;
            }
            catch (Exception)
            {
                return "";
            }
        }
        public string GetPackage(long PackageId)
        {
            string PackageName = string.Empty;
            try
            {  //|| PackageId != null
                if (PackageId != 0 || PackageId.ToString() != "" )
                {
                    var packageDet = (from p in db.tbl_package_client_rates.AsEnumerable()
                                      where p.Id == PackageId && p.IsActive == true
                                      select new { Id = p.Id, Package = p.tbl_clients.CompanyName + "-" + p.tbl_projects.ProjectCode + "-" + p.tbl_vehicle_types.VehicleType + "-" + p.tbl_vehicle_models.VehicleModelName + "-" + Convert.ToString(p.WorkingDays.Value) + (p.TimeUnit == 1 ? "Days" : "Months") }).FirstOrDefault();
                    PackageName = packageDet.Package;
                }
                return PackageName;
            }
            catch (Exception)
            {
                return PackageName;
            }
        }


        public string GetVehicleRegNumber(long VehicleID)
        {
            try
            {//|| VehicleID != null
                if (VehicleID != 0 || VehicleID.ToString() != "")
                    return db.tbl_vehicles.Where(s => s.Status == true && s.ID == VehicleID).SingleOrDefault().VehicleRegNum.ToString();
                else
                    return "";
            }
            catch (Exception)
            {
                return "";
            }
        }


        public string GetLocation(long LocationId)
        {
            try
            { //|| LocationId != null
                if (LocationId != 0 || LocationId.ToString() != "")
                {
                    var locationDet = db.tbl_locations.Where(a => a.ID == LocationId && a.Status == true).SingleOrDefault();
                    return (string.IsNullOrEmpty(locationDet.RouteId) ? locationDet.Location : (locationDet.RouteId));
                }
                else
                    return "";
            }
            catch (Exception)
            {
                return "";
            }
        }
        //By sarath 11-02-2016
        public string GetLocationName(long RouteId)
        {
            try
            { //|| RouteId != null
                if (RouteId != 0 || RouteId.ToString() != "")
                {
                    var locationDet = db.tbl_locations.Where(a => a.ID == RouteId && a.Status == true).SingleOrDefault();
                    return (string.IsNullOrEmpty(locationDet.RouteId) ? locationDet.Location : (locationDet.Location));
                }
                else
                    return "";
            }
            catch (Exception)
            {
                return "";
            }
        }

        public Int64 GetLocationId(string Location)
        {
            try
            {
                if (Location != "" || Location != null)
                {
                    var locationDet = db.tbl_locations.Where(a => a.Location.Trim() == Location && a.Status == true).FirstOrDefault();
                    return (locationDet == null ? 0 : locationDet.ID);
                }
                else
                    return 0;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public Int64 GetVehicleId(string VehicleRegNumber)
        {
            try
            {
                if (!string.IsNullOrEmpty(VehicleRegNumber))
                {
                    var vehicleDet = db.tbl_vehicles.Where(s => s.Status == true && s.VehicleRegNum.ToUpper().Trim() == VehicleRegNumber.ToUpper().Trim()).FirstOrDefault();
                    return (vehicleDet == null ? 0 : vehicleDet.ID);
                }
                else
                {
                    return 0;
                }

            }
            catch (Exception)
            {
                return -1;
            }
        }

        public string GetEHS(long EHSId)
        {
            try
            {
                if (EHSId != 0 || EHSId.ToString() != "")
                    return db.tbl_ehs_codes.Where(a => a.ID == EHSId && a.Status == true).SingleOrDefault().EHSCode.ToString();
                else
                    return "";
            }
            catch (Exception)
            {
                return "";
            }
        }

        public string GetFuelStation(long FuelId)
        {
            try
            {
                if (FuelId != 0 || FuelId.ToString() != "")
                    return db.tbl_fuel_stations.Where(a => a.Status == true && a.ID == FuelId).FirstOrDefault().FuelStation.ToString();
                else
                    return "";
            }
            catch (Exception)
            {
                return "";
            }
        }

        public decimal GetSLABRate(tbl_clients client, tbl_vehicles vehicle, string EmpID, string Location, long VehicleTypeId)
        {
            decimal SLABRate = 0;
            if (EmpID == "")
            {
                tbl_slab_client_rate _clientrate = db.tbl_slab_client_rate.Where(s => s.Status == true && s.ClientID == client.ID && s.Location == Location.ToUpper().Trim()
                                                           && s.SeaterID == vehicle.SeaterId && s.VehicleTypeID == VehicleTypeId).FirstOrDefault();
                if (_clientrate != null)
                    SLABRate = _clientrate.VendorRate == 0 ? 0 : (decimal)_clientrate.VendorRate;
            }
            else
            {
                tbl_slab_client_rate _clientrate = db.tbl_slab_client_rate.Where(s => s.Status == true && s.ClientID == client.ID && s.Location == Location.ToUpper().Trim()
                                                              && s.SeaterID == vehicle.SeaterId && s.VehicleTypeID == VehicleTypeId
                                                              && s.EmpId.ToUpper().Trim() == EmpID.ToUpper().Trim()).FirstOrDefault();
                if (_clientrate != null)
                    SLABRate = _clientrate.VendorRate == 0 ? 0 : (decimal)_clientrate.VendorRate;
            }
            return SLABRate;
        }
        public int GetApprovedKMByLocation(string Location, long ClientID, long VehicleID)
        {
            tbl_vehicles _vehicle = db.tbl_vehicles.Where(c => c.Status == true && c.ID == VehicleID).FirstOrDefault();
            tbl_km_client_rate _clientrate = db.tbl_km_client_rate.Where(s => s.Status == true && s.ClientID == ClientID && s.Location == Location.ToUpper().Trim()).FirstOrDefault();
            if (_clientrate != null)
                return Convert.ToInt32(_clientrate.ApprovedKM);
            else
                return 0;
        }
        public bool VerifySeater(string Seater)
        {
            if (db.tbl_seaters.Where(a => a.Status == true && a.Seater.Trim().ToUpper() == Seater.Trim().ToUpper()).Any())
                return false;
            else
                return true;
        }
        public bool VerifyVehicleType(string VehicleType)
        {
            if (db.tbl_vehicle_types.Where(a => a.Status == true && a.VehicleType.Trim().ToUpper() == VehicleType.Trim().ToUpper()).Any())
                return false;
            else
                return true;
        }
        public bool VerifyVehicleRegNumber(string vehicleRegNum)
        {
            if (db.tbl_vehicles.Where(a => a.Status == true && a.VehicleRegNum.Trim().ToUpper() == vehicleRegNum.Trim().ToUpper()).Any())
                return false;
            else
                return true;
        }
        public bool VerifyCardNumber(string CardNumber)
        {
            try
            {
                return db.tbl_card_assignments.Where(a => a.CardNo.Trim().ToUpper() == CardNumber.Trim().ToUpper() && a.IsClosed == false).Any();
            }
            catch
            {
                return false;
            }
        }

        public string Ordinal(int number)
        {
            const string TH = "th";
            var s = number.ToString();

            number %= 100;

            if ((number >= 11) && (number <= 13))
            {
                return s + TH;
            }

            switch (number % 10)
            {
                case 1:
                    return s + "st";
                case 2:
                    return s + "nd";
                case 3:
                    return s + "rd";
                default:
                    return s + TH;
            }
        }

        public string GetDocumentType(long ID)
        {
            return db.tbl_documents.Where(a => a.ID == ID && a.Status == true).SingleOrDefault().DocumentType.ToString();
        }
        // Get Field Name By Oridinal Position 
        public string GetFieldName(string TableName, long FID)
        {
            string qryTable = "select CONVERT(varchar(15),ORDINAL_POSITION) as value,COLUMN_NAME as text from INFORMATION_SCHEMA.COLUMNS where TABLE_Name='" + TableName + "'  and ORDINAL_POSITION= " + FID + "   order by ORDINAL_POSITION";
            List<SelectListItem> list = (List<SelectListItem>)db.ExecuteStoreQuery<SelectListItem>(qryTable).ToList();
            return list.FirstOrDefault().Text;
        }
        public string SeparateByComma(List<SelectListItem> list)
        {
            string strText = string.Empty;
            foreach (SelectListItem item in list)
                strText = strText + "," + item.Text;
            strText = strText.Substring(1);
            return strText;
        }
        public string GetValidtyField(long VehicleId, string ValidtyField)
        {
            string FieldText = (ValidtyField + "Validity").ToUpper();
            if (GetValidtyFromVeh().Contains(FieldText))
            {
                try
                {
                    string Qry = "select " + FieldText + " as Date1 from tbl_vehicles where status=1 and id =" + VehicleId;
                    var fieldVal = db.ExecuteStoreQuery<GeneralClassFields>(Qry).FirstOrDefault().Date1;
                    return fieldVal.ToString("dd/MM/yyyy");
                }
                catch (Exception)
                {
                    return "";
                }
            }
            return "";
        }
        // Get Validity from vehicles
        public string GetValidtyFromVeh()
        {
            List<SelectListItem> fieldList = GetTableFieldsByTableName("tbl_vehicles");
            string str = string.Empty;
            // i = 7 means ordinal position from 7 is validities upto 7+5 in the table tbl_vehicles 
            for (int i = 7; i < (7 + 5); i++)
                str = str + "," + fieldList[i].Text.ToUpper();
            str = str.Substring(1);
            return str;
        }

        public DataTable GetLogSheetFormatView(DataTable dt)
        {
            DataTable tempDt = new DataTable();
            foreach (DataColumn tempDc in dt.Columns)
            {
                tempDt.Columns.Add(tempDc.ColumnName, typeof(string));
            }
            foreach (DataRow dr in dt.Rows)
            {
                DataRow tempDr = tempDt.NewRow();
                foreach (DataColumn dc in dt.Columns)
                {
                    if (dc.ColumnName.ToUpper() == "VEHICLEID")
                        tempDr[dc.ColumnName] = GetVehicleRegNumber((long)dr[dc.ColumnName]);
                    else if (dc.ColumnName.ToUpper() == "CLIENTID")
                        tempDr[dc.ColumnName] = GetClient((long)dr[dc.ColumnName]);
                    else if (dc.ColumnName.ToUpper() == "VEHICLETYPEID")
                        tempDr[dc.ColumnName] = GetVehicleType((long)dr[dc.ColumnName]);
                    else if (dc.ColumnName.ToUpper() == "DRIVERID")
                        tempDr[dc.ColumnName] = GetDriver((long)dr[dc.ColumnName]);
                    else if (dc.ColumnName.ToUpper() == "SEATERID")
                        tempDr[dc.ColumnName] = GetSeater((long)dr[dc.ColumnName]);
                    else if (dc.ColumnName.ToUpper() == "LOCATIONID")
                        tempDr[dc.ColumnName] = GetLocation((long)dr[dc.ColumnName]);
                    else if (dc.ColumnName.ToUpper() == "VEHICLEMODELID")
                        tempDr[dc.ColumnName] = GetVehicleModel((long)dr[dc.ColumnName]);
                    else
                        tempDr[dc.ColumnName] = dr[dc.ColumnName];
                }
                tempDt.Rows.Add(tempDr);
            }
            return tempDt;
        }

        public DataTable ConvertToDataTable<TSource>(IEnumerable<TSource> source)
        {
            var props = typeof(TSource).GetProperties();

            var dt = new DataTable();
            dt.Columns.AddRange(
              props.Select(p => new DataColumn(p.Name, p.PropertyType)).ToArray()
            );

            source.ToList().ForEach(
              i => dt.Rows.Add(props.Select(p => p.GetValue(i, null)).ToArray())
            );

            return dt;
        }

        public tbl_booking_cab_details GetCabDetails(long BookingId)
        {
            try
            {
                return db.tbl_booking_cab_details.Where(a => a.BookingId == BookingId).FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        public Guid GetUserIdByUsername(string UserName)
        {
            Guid UserId = new Guid();
            UserId = db.aspnet_Users.Where(a => a.UserName.Trim().ToUpper() == UserName.Trim().ToUpper()).FirstOrDefault().UserId;
            return UserId;
        }
        public string GetUserNameByUserId(Guid UserId)
        {
            string UserName = string.Empty;
            UserName = db.aspnet_Users.Where(a => a.UserId == UserId).FirstOrDefault().UserName;
            return UserName;
        }

        public string GetSettingsValueByCode(string code)
        {
            string strVal = string.Empty;
            tbl_settings setting = db.tbl_settings.Where(a => a.SettingsCode == code).SingleOrDefault();
            strVal = setting.SettingsField;
            return strVal;
        }

        public string GetCardNumber(Int64 PackageId)
        {
            tbl_card_assignments cardDet = db.tbl_card_assignments.Where(a => a.PackageId == PackageId && a.IsActive == true).FirstOrDefault();
            if (cardDet != null)
            {
                return cardDet.CardNo;
            }
            return "";
        }

        public Int64 GetCardId(string cardNumber)
        {
            tbl_card_assignments cardDet = db.tbl_card_assignments.Where(a => a.CardNo == cardNumber && a.IsActive == true && a.IsClosed == false).FirstOrDefault();
            if (cardDet != null)
            {
                return cardDet.Id;
            }
            return 0;
        }

        public bool VerifySettingsCode(string code)
        {
            return db.tbl_settings.Where(a => a.SettingsCode == code && a.Allow == true).Any();
        }

        public decimal GetAdvanceAmountByVehicleId(DateTime MonthYear, long VehicleId)
        {
            decimal AdvAmt = 0;
            try
            {
                AdvAmt = (from m in db.tbl_advances
                          where m.Status == true
                          && m.CreatedDate.Value.Date.Month == MonthYear.Month
                          && m.CreatedDate.Value.Date.Year == MonthYear.Year
                          && (m.ClosedFlag == false || m.ClosedFlag == null)
                          select m).Select(a => a.Amount).Sum() ?? 0;
            }
            catch
            {
                return 0;
            }
            return AdvAmt;
        }

        public decimal GetTotalDeductionsByVehicleId(DateTime MonthYear, long VehicleId)
        {
            decimal TotaDedAmt = 0;
            try
            {
                TotaDedAmt += (from m in db.tbl_penalties
                               where m.Status == true
                               && m.tbl_log_sheets.VehicleID == VehicleId
                               && m.CreatedDate.Value.Date.Month == MonthYear.Month
                               && m.CreatedDate.Value.Date.Year == MonthYear.Year
                               && (m.ClosedFlag == false || m.ClosedFlag == null)
                               select m).Select(a => a.PenalityAmt).Sum() ?? 0;

                TotaDedAmt += (from m in db.tbl_ehs_details
                               where m.Status == true
                               && m.VehicleID == VehicleId
                               && m.CreatedDate.Value.Date.Month == MonthYear.Month
                               && m.CreatedDate.Value.Date.Year == MonthYear.Year
                               && (m.ClosedFlag == false || m.ClosedFlag == null)
                               select m).Select(a => a.EHSAmt).Sum() ?? 0;

            }
            catch
            {
                return 0;
            }
            return TotaDedAmt;
        }

        public decimal GetMonthlyVehicleEMIByVehicle(DateTime MonthYear, long VehicleId, string EMIIds)
        {
            decimal EmiAmt = 0;

            try
            {
                if (EMIIds == "")
                    return Math.Round(EmiAmt, 2);

                var list = (from m in db.tbl_veh_emis
                            where m.MonthYear.Value.Month == MonthYear.Month
                            && m.MonthYear.Value.Year == MonthYear.Year
                            && m.VehicleId == VehicleId
                            select m).ToList();

                if (!string.IsNullOrEmpty(EMIIds))
                {
                    List<long> lstIds = EMIIds.Split(',').ToList().Select(a => Convert.ToInt64(a)).ToList();
                    list = list.Where(a => lstIds.Contains(a.Id)).ToList();
                }

                EmiAmt = list.Sum(a => a.Amount) ?? 0;
            }
            catch
            {
                return Math.Round(EmiAmt, 2);
            }
            return Math.Round(EmiAmt, 2);
        }

        public decimal GetMonthlyDriverSalary(DateTime MonthYear, long VehicleId, string DriverSalIds)
        {
            decimal DriverSalAmt = 0;
            try
            {

                if (DriverSalIds == "")
                    return DriverSalAmt;

                var list = (from m in db.tbl_driver_salaries
                            where m.Status == true
                            && m.MonthYear.Value.Month == MonthYear.Month
                            && m.MonthYear.Value.Year == MonthYear.Year
                            && m.VehicleId == VehicleId
                            select m).ToList();
                if (!string.IsNullOrEmpty(DriverSalIds))
                {
                    List<long> lstIds = DriverSalIds.Split(',').ToList().Select(a => Convert.ToInt64(a)).ToList();
                    list = list.Where(a => lstIds.Contains(a.Id)).ToList();
                }

                DriverSalAmt = list.Sum(a => a.TotalAmt) ?? 0;
            }
            catch
            {
                return 0;
            }
            return DriverSalAmt;
        }

        public decimal GetCurrentOdoMeterReadByVehicle(long VehicleId)
        {
            decimal result = 0;
            try
            {
                result = (from m in db.tbl_diesel_tracking
                          where m.Status == true && m.VehicleID == VehicleId
                          select m).Max(a => a.OdoMeterReading) ?? 0;
                return result;
            }
            catch
            {
                return 0;
            }
        }

        public long GetVehicleModelId(long VehicleId)
        {
            string VehicleModel = string.Empty;
            tbl_vehicles vehDet = db.tbl_vehicles.Where(a => a.ID == VehicleId).SingleOrDefault();
            VehicleModel = vehDet.tbl_vehicle_models.VehicleModelName;
            if (VehicleModel.Contains("FORD"))
                return vehDet.VehicleModelID ?? 0;
            else
                return db.tbl_vehicle_models.Where(a => a.Status == true && a.VehicleModelName.Contains("INDICA")).FirstOrDefault().ID;
        }

        public static PhysicalAddress GetMacAddress()
        {
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet &&
                    nic.OperationalStatus == OperationalStatus.Up)
                {
                    //byte[] bytes = nic.GetPhysicalAddress().GetAddressBytes();
                    return nic.GetPhysicalAddress();
                }
            }
            return null;
        }

        public Int64 GetLogSheetCount(DateTime logdate)
        {
            Int64 cnt = db.tbl_log_sheets.AsEnumerable()
                        .Where(a => a.LogDate.Value.Month == logdate.Date.Month && a.LogDate.Value.Year == logdate.Year).ToList().Count;
            return cnt == 0 ? 1 : cnt + 1;
        }

        public bool VerifyExistLogSheetByCardNumber(DateTime logDate, string cardNumber, string TripType)
        {
            return _db.tbl_log_sheets.Where(a => a.Status == true && a.LogDate.Value == logDate.Date && a.CardNumber.Trim() == cardNumber.Trim() && a.TripeType.Trim() == TripType.Trim()).Any();
        }

        public bool VerifyExistingNoCabLogSheet(Int64 ClientId, Int64 ProjectId, Int64 PackageId, DateTime LogDate, string TripType)
        {
            bool IsValid = false;
            var logDet = _db.tbl_log_sheets.Where(a => a.Status == true && a.ClientID == ClientId && a.ProjectId == ProjectId && a.PackId == PackageId &&
              EntityFunctions.TruncateTime(a.LogDate).Value == LogDate.Date && a.TripeType.Trim() == TripType.Trim());

            IsValid = logDet.Any();

            if (IsValid && logDet.Where(a => (a.IsAdhoc == null || a.IsAdhoc == false)).Any())
            {
                return IsValid;
            }

            IsValid = logDet.Where(a => a.IsNoCab == true).Any();

            return IsValid;
        }



        public bool ValidatePackageByLogDate(DateTime logDate, Int64 PackageId)
        {
            return _db.tbl_package_client_rates.AsEnumerable().Where(a => a.IsActive == true && logDate.Date >= a.EffectiveDt.Value.Date && logDate.Date <= a.ExpiredDt.Value.Date
            && a.Id == PackageId).Any();
        }

        public List<UserCardEntryModel> GetCardEntryDetailsListByClientProjectAndDate(Int64 ClientId, Int64 ProjectId, DateTime LogDate)
        {
            List<UserCardEntryModel> _userCardEntryList = new List<UserCardEntryModel>();
            _userCardEntryList = (from p in db.tbl_package_client_rates.AsEnumerable()
                                  join c in db.tbl_card_assignments.AsEnumerable()
                                     on p.Id equals c.PackageId
                                  where c.IsActive == true && p.IsActive == true && c.IsClosed == false
                                     && p.ClientId == ClientId && c.ClientId == ClientId && c.ProjectId == ProjectId
                                     && p.ProjectId == ProjectId && LogDate.Date >= p.EffectiveDt.Value.Date && LogDate.Date <= p.ExpiredDt.Value.Date
                                  select new UserCardEntryModel
                                {
                                    CardNo = c.CardNo,
                                    PackageId = p.Id,
                                    VehicleId = c.VehicleId,
                                    LocationId = p.LocationId,
                                    LogIn = p.LoginTime,
                                    LogOut = p.LogoutTime,
                                    VehicleModelId = c.tbl_vehicles.VehicleModelID,
                                    VehicleTypeId = c.tbl_vehicles.VehicleTypeID,
                                    ClientId = c.ClientId,
                                    ProjectId = c.ProjectId,
                                    SeaterId = c.tbl_vehicles.SeaterId,
                                    EffectedDt = p.EffectiveDt,
                                    ExpiredDt = p.ExpiredDt,

                                }).ToList();
            return _userCardEntryList;
        }



        #endregion

        //By sarath&vinod on 10-03-2016 for Assigned Cards
        public string GetVehicle(long PackageId)
        {
            string VehNum = "";
            var veh = db.tbl_card_assignments.Where(c => c.PackageId == PackageId).ToList();
            foreach (var item in veh)
            {
                VehNum = item.tbl_vehicles.VehicleRegNum.ToString();
            }
            return VehNum;
        }
        public string GetCardNo(long PackageId)
        {
            string cardNo = "";
            var card = db.tbl_card_assignments.Where(c => c.PackageId == PackageId).ToList();
            foreach (var item in card)
            {
                cardNo = item.CardNo;
            }
            return cardNo;

        }
        //By sarath 27/02/2016
        public string GetLogEntryValues(long PackId, DateTime LogDate)
        {
            string s = "";
            var log = db.tbl_log_sheets.Where(a => a.PackId == PackId && a.LogDate == LogDate).ToList();
            foreach (var item in log)
            {
                if (s == "")
                {
                    s = item.TripeType;
                }
                else
                {
                    s += ',' + item.TripeType;
                }
            }
            return s;
        }

        public string GetLogIn(long PackId, DateTime LogDate)
        {
            string log1 = "";
            var log = db.tbl_log_sheets.Where(a => a.PackId == PackId && a.LogDate == LogDate).ToList();
            foreach (var item in log)
            {
                log1 = item.ShiftTime;
            }
            return log1;
        }
        public string GetLogOut(long PackId, DateTime LogDate)
        {
            string log2 = "";
            var log = db.tbl_log_sheets.Where(a => a.PackId == PackId && a.LogDate == LogDate).ToList();
            foreach (var item in log)
            {

                log2 = item.ReachTime;
            }

            return log2;

        }
        //By sarath & vinod on 20-2-2016
        public List<AdhocListModel> GetAdhocListByLogDate(long ClientId, long ProjectId, DateTime LogDate)
        {
            List<AdhocListModel> _AdhocList = new List<AdhocListModel>();
            _AdhocList = (from l in db.tbl_log_sheets.AsEnumerable()
                          where LogDate.Date == l.LogDate.Value.Date && l.IsAdhoc == true && ClientId == l.ClientID && ProjectId == l.ProjectId
                          select new AdhocListModel
                          {
                              ClientId = l.ClientID,
                              ProjectId = l.ProjectId,
                              PackId = l.PackId,
                              AlterVehNumber = l.AlterVehNum,
                              VehicleTypeID = l.VehicleTypeID,
                              EmpName = l.EmpName,
                              Location = l.Location,
                              TripType = l.TripeType,
                              TotalHrs = l.TotalHrs,
                              ContactNumber = l.ContactNumber,
                              NetAmount = l.NetAmount,
                              LogDate = l.LogDate
                          }).ToList();
            return _AdhocList;


        }

        // By Sarath & Vinod on 22-02-2016
        public List<NocabListModel> GetNocabList(long ClientId, long ProjectId, DateTime LogDate)
        {
            List<NocabListModel> nocab = new List<NocabListModel>();
            nocab = (from j in db.tbl_log_sheets.AsEnumerable()
                     where LogDate.Date == j.LogDate.Value.Date && j.IsNoCab == true && ClientId == j.ClientID && ProjectId == j.ProjectId
                     select new NocabListModel
                     {
                         ClientId = j.ClientID,
                         ProjectId = j.ProjectId,
                         PackId = j.PackId,
                         AlterVehNumber = j.AlterVehNum,
                         VehicleTypeID = j.VehicleTypeID,
                         EmpName = j.EmpName,
                         Location = j.Location,
                         TripType = j.TripeType,
                         ContactNumber = j.ContactNumber,
                         NetAmount = j.NetAmount,
                         LogDate = j.LogDate,
                         CardNumber = j.CardNumber,
                         ShiftTime = j.ShiftTime,
                         ReachTime = j.ReachTime
                     }).ToList();
            return nocab;

        }

        #region Select List Methods
        public List<SelectListItem> LoadDrivers()
        {
            List<SelectListItem> list = new List<SelectListItem>();
            foreach (tbl_drivers obj in db.tbl_drivers.Where(a => a.Status == true).ToList())
                list.Add(new SelectListItem { Value = obj.ID.ToString(), Text = obj.FirstName + " " + obj.LastName });
            return list;
        }
        //public List<SelectListItem> LoadPackages()
        //{
        //    List<SelectListItem> Package = new SelectList(db.tbl_package_client_rates.Where(a =>  == true), "ID", "PackageName").ToList();
        //    return Package;
        //}

        public List<SelectListItem> LoadBillTypes()
        {
            List<SelectListItem> billTypeList = new SelectList(db.tbl_billing_types.Where(a => a.Status == true), "ID", "BillingType").ToList();
            return billTypeList;
        }
        public List<SelectListItem> LoadLocations()
        {
            List<SelectListItem> locationList = new SelectList(db.tbl_locations.AsEnumerable().Where(a => a.Status == true)
                .Select(a => new SelectListItem
                {
                    Value = a.ID.ToString(),
                    Text = (string.IsNullOrEmpty(a.RouteId) ? a.Location : (a.RouteId + "-" + a.Location))
                }), "Value", "Text").ToList();
            return locationList;
        }
        public List<SelectListItem> LoadClients()
        {
            List<SelectListItem> clientlist = new SelectList(db.tbl_clients.Where(a => a.Status == true), "ID", "CompanyName").ToList();
            return clientlist;
        }
        public List<SelectListItem> LoadVehicles()
        {
            List<SelectListItem> vehList = new SelectList(db.tbl_vehicles.Where(a => a.Status == true), "ID", "VehicleRegNum").ToList();
            return vehList;
        }

        //By sarath on 04-02-2016
        public List<SelectListItem> LoadPackages()
        {
             List<SelectListItem> PacList = new SelectList(db.tbl_package_client_rates.AsEnumerable().Where(p => p.IsActive == true )
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = (p.tbl_clients.CompanyName + "-" + p.tbl_projects.ProjectName + "-" + p.tbl_projects.ProjectCode + "-" + p.tbl_vehicle_types.VehicleType + "-" + p.tbl_vehicle_models.VehicleModelName + "-" + Convert.ToString(p.WorkingDays.Value) + (p.TimeUnit == 1 ? "Days" : "Months"))
                }), "Value", "Text").ToList();
            return PacList;

        }

        public List<SelectListItem> NOCabLoadPackages(long ClientId, long ProjectId, string logDate)
        {

            DateTime _logdate = string.IsNullOrEmpty(logDate) ? DateTime.Now.Date : Convert.ToDateTime(logDate);

            List<SelectListItem> PacList = new SelectList(db.tbl_package_client_rates.AsEnumerable().Where(p => p.IsActive == true && _logdate.Date >= p.EffectiveDt.Value.Date && _logdate.Date <= p.ExpiredDt.Value.Date
                && p.ClientId == ClientId && p.ProjectId == ProjectId)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = (p.tbl_clients.CompanyName + "-" + p.tbl_projects.ProjectName + "-" + p.tbl_projects.ProjectCode + "-" + p.tbl_vehicle_types.VehicleType + "-" + p.tbl_vehicle_models.VehicleModelName + "-" + Convert.ToString(p.WorkingDays.Value) + (p.TimeUnit == 1 ? "Days" : "Months"))
                }), "Value", "Text").ToList();
            return PacList;

        }
        public List<SelectListItem> LoadProjects()
        {
            List<SelectListItem> projList = new SelectList(db.tbl_projects.Where(a => a.IsActive == true), "ID", "ProjectName").ToList();
            return projList;
        }

        public List<SelectListItem> LoadSeaters()
        {
            List<SelectListItem> seaterList = new SelectList(db.tbl_seaters.Where(a => a.Status == true), "ID", "SEATER").ToList();
            return seaterList;
        }

        public List<SelectListItem> LoadVehicleTypes()
        {
            List<SelectListItem> typeList = new SelectList(db.tbl_vehicle_types.Where(a => a.Status == true), "ID", "VehicleType").ToList();
            return typeList;
        }

        public List<SelectListItem> LoadVehicleModels()
        {
            List<SelectListItem> vehregnumList = new SelectList(db.tbl_vehicle_models.Where(a => a.Status == true), "ID", "VehicleModelName").ToList();
            return vehregnumList;
        }

        public List<SelectListItem> LoadVehicleRegNum()
        {
            List<SelectListItem> typeList = new SelectList(db.tbl_vehicles.Where(a => a.Status == true), "Id", "VehicleRegNum").ToList();
            return typeList;
        }
        public List<SelectListItem> LoadPackModel()
        {
            List<SelectListItem> packModel = new List<SelectListItem>();
            //packModel.Add(new SelectListItem { Text = "-Select-", Value = "0" });
            packModel.Add(new SelectListItem { Text = "Waiting", Value = "WAITING" });
            packModel.Add(new SelectListItem { Text = "Non-Waiting", Value = "NON-WAITING" });
            return packModel;
        }

        public List<SelectListItem> LoadTimeUnit()
        {
            List<SelectListItem> timeUnit = new List<SelectListItem>();
            timeUnit.Add(new SelectListItem { Text = "Days", Value = "1" });
            timeUnit.Add(new SelectListItem { Text = "Months", Value = "2" });
            return timeUnit;
        }

        public List<SelectListItem> LoadEHSCodes()
        {
            List<SelectListItem> EHSCodesList = new SelectList(db.tbl_ehs_codes.Where(a => a.Status == true), "ID", "EHSCode").ToList();
            return EHSCodesList;
        }

        public List<SelectListItem> LoadDieselBook()
        {
            List<SelectListItem> FuelList = new SelectList(db.tbl_fuel_stations.Where(a => a.Status == true), "ID", "FuelStation").ToList();
            return FuelList;
        }

        public List<SelectListItem> LoadServiceNumber()
        {
            List<SelectListItem> serviceList = new List<SelectListItem>();
            for (int i = 1; i <= 30; i++)
            {
                serviceList.Add(new SelectListItem { Text = i.ToString(), Value = i.ToString() });
            }
            return serviceList;
        }

        public List<SelectListItem> LoadServiceStations()
        {
            List<SelectListItem> serviceList = new SelectList(_db.tbl_service_stations.Where(a => a.Status == true), "Id", "ServiceStation").ToList();
            return serviceList;
        }

        public List<SelectListItem> LoadInvItems()
        {
            var invList = new List<SelectListItem>();
            invList = (from m in _db.InventoryItemsMasters
                       where m.Active == true
                       select m
                       ).ToList().Select(a => new SelectListItem { Value = a.ID.ToString(), Text = a.ItemText + "-" + a.ItemCode + (a.Size == null ? "" : "-" + a.Size) }).ToList()
                       ?? new List<SelectListItem>() { new SelectListItem() { Text = "No Records", Value = "" } };
            invList = new SelectList(invList, "Value", "Text").ToList();
            return invList;
        }

        // Get Field IDs and Names of tbl_log_sheet
        public List<SelectListItem> GetTableFieldsByTableName(string tableName)
        {
            string qryTable = "select CONVERT(varchar(15),ORDINAL_POSITION) as value,COLUMN_NAME as text from INFORMATION_SCHEMA.COLUMNS where TABLE_Name='" + tableName + "' order by ORDINAL_POSITION";
            List<SelectListItem> list = (List<SelectListItem>)db.ExecuteStoreQuery<SelectListItem>(qryTable).ToList();
            return list;
        }

        public List<SelectListItem> GetColsList(long ClientID)
        {
            string qryTable = "select COLUMN_NAME as Text,CONVERT(varchar(15),ORDINAL_POSITION) as value from information_schema.columns where table_name = 'tbl_log_sheets' and ordinal_position in (select FID from tbl_client_log_sheet_cols where ClientID = " + ClientID + ") order by ordinal_position";
            List<SelectListItem> list = (List<SelectListItem>)db.ExecuteStoreQuery<SelectListItem>(qryTable).ToList();
            return list;
        }
        // Get Mapped Cols List 
        public List<SelectListItem> GetHeaderColsList(long ClientID)
        {
            string qryTable = "select FieldText as Text , CONVERT(varchar(15),FID) as value from tbl_client_log_sheet_cols where clientid =" + ClientID + " order by fid ";
            List<SelectListItem> list = (List<SelectListItem>)db.ExecuteStoreQuery<SelectListItem>(qryTable).ToList();
            return list;
        }

        public List<SelectListItem> GetTripTypeList()
        {
            List<SelectListItem> _lstTrip = new List<SelectListItem>();
            _lstTrip.Add(new SelectListItem { Value = "ADHOC PICKUP", Text = "ADHOC PICKUP" });
            _lstTrip.Add(new SelectListItem { Value = "ADHOC DROP", Text = "ADHOC DROP" });
            _lstTrip.Add(new SelectListItem { Value = "AIRPORT PICKUP", Text = "AIRPORT PICKUP" });
            _lstTrip.Add(new SelectListItem { Value = "AIRPORT DROP", Text = "AIRPORT DROP" });
            _lstTrip.Add(new SelectListItem { Value = "DISPOSAL", Text = "DISPOSAL" });
            _lstTrip.Add(new SelectListItem { Value = "4Hrs.40KM", Text = "4Hrs.40KM" });
            _lstTrip.Add(new SelectListItem { Value = "8Hrs.80KM", Text = "8Hrs.80KM" });
            _lstTrip.Add(new SelectListItem { Value = "LADY MIND", Text = "LADY MIND" });
            return _lstTrip;
        }

        public List<SelectListItem> GetPriorityList()
        {
            List<SelectListItem> _lstPrty = new List<SelectListItem>();
            _lstPrty.Add(new SelectListItem { Value = "LOW", Text = "LOW" });
            _lstPrty.Add(new SelectListItem { Value = "MEDIUM", Text = "MEDIUM" });
            _lstPrty.Add(new SelectListItem { Value = "HIGH", Text = "HIGH" });
            return _lstPrty;
        }

        public List<SelectListItem> GetModeOfEntryList()
        {
            List<SelectListItem> _lstModEntry = new List<SelectListItem>();
            _lstModEntry.Add(new SelectListItem { Value = "MOBILE", Text = "Mobile" });
            _lstModEntry.Add(new SelectListItem { Value = "WEB", Text = "Web" });
            return _lstModEntry;
        }

        public static List<SelectListItem> GetAllYears()
        {
            DateTime CurrentYear = DateTime.Now;
            DateTime StartYear = CurrentYear.AddYears(-5);
            List<SelectListItem> _lstYears = new List<SelectListItem>();
            for (int i = StartYear.Year; i <= CurrentYear.Year; i++)
                _lstYears.Add(new SelectListItem { Value = i.ToString(), Text = i.ToString() });
            return _lstYears;
        }

        public static List<SelectListItem> GetAllMonths()
        {
            List<SelectListItem> MonthsList = new List<SelectListItem>();
            for (int i = 0; i < 12; i++)
                MonthsList.Add(new SelectListItem { Text = CultureInfo.GetCultureInfo("en-GB").DateTimeFormat.MonthNames[i], Value = (i >= 9) ? (i + 1).ToString() : "0" + (i + 1).ToString() });
            return MonthsList;
        }

        public List<SelectListItem> GetInvItemCodes()
        {
            List<SelectListItem> list = new List<SelectListItem>();
            list = db.InventoryItemsMasters.Where(a => a.Active == true).AsEnumerable().Select(a =>
                   new SelectListItem
                   {
                       Text = a.InventoryItemCategory.ItemCatText.Trim() + "-" + a.ItemText.Trim() + (a.ItemCode == null ? "" : "-" + a.ItemCode.Trim()),
                       Value = a.ID.ToString()

                   }).ToList();
            return list;
        }

        public List<SelectListItem> GetVehicleServiceNumbers(long VehicleId, long? Id)
        {
            List<SelectListItem> list = new List<SelectListItem>();
            var lst = db.tbl_veh_service_schedules.Where(a => a.VehicleId == VehicleId).AsEnumerable();
            if (Id.HasValue)
                lst = lst.Where(a => a.Id == Id).AsEnumerable();

            list = lst.Select(a => new SelectListItem
                   {
                       Selected = (a.IsDone == null ? false : (bool)a.IsDone),
                       Text = a.ServiceNo + " - " +
                              a.ODMeterReading + "KMs to " +
                              a.ODMeterMax + "KMs - " + ((from m in db.tbl_jobcard where m.ServiceId == a.Id select m).Any() ? "- Servicing" : ((a.IsDone == null ? false : (bool)a.IsDone) ? " Done " : "- Not Done")),
                       Value = a.Id.ToString(),

                   }).ToList();
            return list;
        }

        /* Get Available Card numbers list */

        public List<SelectListItem> BindCardNumbers()
        {
            List<SelectListItem> _cardList = new List<SelectListItem>();
            List<string> _logSheetNum = (from m in db.tbl_log_sheets
                                         where m.Status == true
                                         select m.LogSheetNum).ToList();  //&& !_logSheetNum.Contains(a.CardNo)

            _cardList = new SelectList(db.tbl_card_assignments.Where(a => a.IsActive == true).ToList(), "Id", "CardNo").ToList();

            return _cardList;
        }

        public List<SelectListItem> BindMonthlyPackages(long ClientId, long ProjectId, string SelectedMonth)
        {
            DateTime CurrentMonth = string.IsNullOrEmpty(SelectedMonth) ? DateTime.Now : Convert.ToDateTime(SelectedMonth);
            var packDet = (from p in db.tbl_package_client_rates.AsEnumerable()
                           where p.ClientId == ClientId && p.ProjectId == ProjectId && p.IsActive == true
                           && (p.EffectiveDt.Value.Month == CurrentMonth.Month
                           && p.EffectiveDt.Value.Year == CurrentMonth.Year) && p.Approved == true
                           select new { Id = p.Id, Package = p.tbl_clients.CompanyName + "-" + p.tbl_projects.ProjectCode + "-" + p.tbl_vehicle_types.VehicleType + "-" + p.tbl_vehicle_models.VehicleModelName + "-" + Convert.ToString(p.WorkingDays.Value) + " Days" }).ToList();

            return packDet.Select(a => new SelectListItem { Value = a.Id.ToString(), Text = a.Package }).ToList();
        }

        /* modified by Abdul on 12/24/2015 */


        /* Get Average Percetage , By Abdul on 23rd Jan 2016 */
        public List<SelectListItem> LoadAvgPercentage()
        {
            List<SelectListItem> AvgPerCentage = new List<SelectListItem>();
            foreach (var item in GetAvgPercentage())
            {
                AvgPerCentage.Add(new SelectListItem { Text = item, Value = item });
            }
            return AvgPerCentage;
        }

        public List<string> GetAvgPercentage()
        {
            List<string> lstStr = new List<string>();
            string item = string.Empty;
            int i = 0;
            while (i < 100)
            {
                if (i != 0)
                    item = (i + 1) + "-" + (i + 5) + "%";
                else
                    item = i + "-" + (i + 5) + "%";
                i = i + 5;
                lstStr.Add(item);
            }
            return lstStr;
        }
        /*  End of update */
        #endregion

        #region Generic List Methods
        public List<tbl_vehicles> GetVehiclesList(long VendorId)
        {
            return db.tbl_vehicles.Where(a => a.VendorID == VendorId && a.Status == true).ToList();
        }
        public List<tbl_drivers> GetDriversList(long VendorId)
        {
            List<tbl_drivers> _driverlist = db.tbl_drivers.Where(a => a.VendorID == VendorId && a.Status == true).ToList();
            ConvertToUppercase<tbl_drivers>(_driverlist);
            return _driverlist;
        }
        public List<tbl_km_client_rate> GetKMClientRateList(long ClientID)
        {
            List<tbl_km_client_rate> KMList = db.tbl_km_client_rate.Where(a => a.Status == true && a.ClientID == ClientID).ToList();
            if (KMList.Count() == 0)
                return null;
            return KMList;
        }
        // Client Mgmt. Slab rate
        public List<tbl_slab_client_rate> GetSlabClientRateList(long ClientID)
        {
            List<tbl_slab_client_rate> SBList = db.tbl_slab_client_rate.Where(a => a.Status == true && a.ClientID == ClientID).ToList();
            if (SBList.Count() == 0)
                return null;
            return SBList;
        }
        // Client Mgmt. Package Rate
        public List<tbl_package_client_rates> GetPackageClientRateList(long ClientID)
        {
            List<tbl_package_client_rates> PKList = db.tbl_package_client_rates.Where(a => a.ClientId == ClientID && a.IsActive == true).ToList();
            if (PKList.Count() == 0)
                return null;
            return PKList;
        }
        // Get List Mapped cols by Client 
        public List<tbl_client_log_sheet_cols> GetMappedColsList(long ClientID)
        {
            return (db.tbl_client_log_sheet_cols.Where(a => a.ClientID == ClientID).ToList().Count() == 0 ? null : db.tbl_client_log_sheet_cols.Where(a => a.ClientID == ClientID).ToList());
        }
        public List<tbl_log_sheets> GetLogSheetList(long ClientID)
        {
            return (db.tbl_log_sheets.Where(a => a.ClientID == ClientID).ToList().Count() == 0 ? null : db.tbl_log_sheets.Where(a => a.ClientID == ClientID).ToList());
        }

        public List<tbl_vehicles> GetVehiclesList(bool IsOwn)
        {
            List<tbl_vehicles> list = (from m in db.tbl_vehicles
                                       where m.Status == true && (m.IsProxy == null || m.IsProxy == false)
                                       && (m.Isown == IsOwn)
                                       select m).ToList();
            return list;
        }

        public List<tbl_veh_emis> GetVehicleEMIsList(long VehicleId, DateTime MonthYear)
        {
            List<tbl_veh_emis> list = (from m in db.tbl_veh_emis
                                       where m.VehicleId == VehicleId
                                       && m.MonthYear.Value.Month == MonthYear.Month
                                       && m.MonthYear.Value.Year == MonthYear.Year
                                       select m).ToList();
            return list;
        }

        public List<tbl_veh_emis> GetVehicleEMIsList(long VehicleId, DateTime MonthYear, string EMIIds, bool IsClosedFlag)
        {
            List<long> lstIds = EMIIds.Split(',').ToList().Select(a => Convert.ToInt64(a)).ToList();
            List<tbl_veh_emis> list = (from m in db.tbl_veh_emis
                                       where m.VehicleId == VehicleId
                                       && m.MonthYear.Value.Month == MonthYear.Month
                                       && m.MonthYear.Value.Year == MonthYear.Year
                                       && lstIds.Contains(m.Id)
                                       && m.ClosedFlag == IsClosedFlag
                                       select m).ToList();
            return list;
        }

        public List<tbl_driver_salaries> GetDriverSalaryList(long VehicleId, DateTime MonthYear)
        {
            List<tbl_driver_salaries> list = (from m in db.tbl_driver_salaries
                                              where m.VehicleId == VehicleId
                                              && m.MonthYear.Value.Month == MonthYear.Month
                                              && m.MonthYear.Value.Year == MonthYear.Year
                                              select m).ToList();
            return list;
        }
        public List<tbl_driver_salaries> GetDriverSalaryList(long VehicleId, DateTime MonthYear, string DriverSalIds, bool IsClosedFlag)
        {
            List<long> lstIds = DriverSalIds.Split(',').ToList().Select(a => Convert.ToInt64(a)).ToList();
            List<tbl_driver_salaries> list = (from m in db.tbl_driver_salaries
                                              where m.VehicleId == VehicleId
                                              && m.MonthYear.Value.Month == MonthYear.Month
                                              && m.MonthYear.Value.Year == MonthYear.Year
                                              && lstIds.Contains(m.Id)
                                              && m.ClosedFlag == IsClosedFlag
                                              select m).ToList();
            return list;
        }

        /*  Get Previous Packages List by Project and Client , modified by Abdul on 05 Dec 2015 */

        public List<tbl_package_client_rates> GetPreviouMonthPackageList(Int64 ClientId, Int64 ProjectId, string rdFilter, int PackageMonth)
        {
            DateTime CurrentMonth = DateTime.Now;
            List<tbl_package_client_rates> packList = (from m in db.tbl_package_client_rates.AsEnumerable()
                                                       where m.ClientId == ClientId && m.IsActive == true
                                                       && m.ProjectId == ProjectId
                                                       && ((m.EffectiveDt.Value.Month == PackageMonth && m.EffectiveDt.Value.Year == CurrentMonth.Year)
                                                       || (m.ExpiredDt.Value.Month <= PackageMonth))
                                                       select m).ToList();
            if (rdFilter == "1")  // 1 = Not Expired
            {
                packList = packList.Where(a => DateTime.Now.Date < a.ExpiredDt.Value.Date).ToList();
            }
            else if (rdFilter == "2") // 2 = Expired
            {
                packList = packList.Where(a => DateTime.Now.Date >= a.ExpiredDt.Value.Date).ToList();
            }

            return packList;
        }

        public List<tbl_log_sheets> GetLogSheetListByClient(Int64 ClientId, Int32 MonthId, Int32 YearId)
        {
            List<tbl_log_sheets> _lstlog = (from m in _db.tbl_log_sheets.AsEnumerable()
                                            where m.Status == true && m.ClientID == ClientId
                                            && m.LogDate.Value.Month == MonthId && m.LogDate.Value.Year == YearId
                                            select m).ToList();
            return _lstlog;
        }

        /* end of update */



        #endregion

        #region Validate Images
        public bool IsValid(HttpPostedFileBase file)
        {
            bool isValid = false;
            //  var file = value as HttpPostedFileBase;
            if (file == null)
                return isValid;
            if (IsFileTypeValid(file))
            {
                isValid = true;
            }

            return isValid;
        }

        private bool IsFileTypeValid(HttpPostedFileBase file)
        {
            bool isValid = false;

            try
            {
                if (file == null)
                    return isValid;
                using (var img = Image.FromStream(file.InputStream))
                {
                    if (IsOneOfValidFormats(img.RawFormat))
                    {
                        isValid = true;
                    }
                }
            }
            catch
            {
                //Image is invalid
            }
            return isValid;
        }
        private bool IsOneOfValidFormats(ImageFormat rawFormat)
        {
            List<ImageFormat> formats = getValidFormats();

            foreach (ImageFormat format in formats)
            {
                if (rawFormat.Equals(format))
                {
                    return true;
                }
            }
            return false;
        }
        private List<ImageFormat> getValidFormats()
        {
            List<ImageFormat> formats = new List<ImageFormat>();
            formats.Add(ImageFormat.Png);
            formats.Add(ImageFormat.Jpeg);
            formats.Add(ImageFormat.Gif);
            //add types here
            return formats;
        }
        #endregion

        #region To Upper Case List or Class Model
        // Common method for convert uppercase
        public void ConvertToUppercase<T>(T theInstance)
        {
            if (theInstance != null)
            {
                foreach (var property in theInstance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (property.Name == "Password")
                        continue;
                    if (property.Name == "Email")
                        continue;
                    var theValue = property.GetValue(theInstance, null);
                    if (theValue is string)
                    {
                        property.SetValue(theInstance, ((string)theValue).ToUpper(), null);
                    }
                }
            }
        }

        public void ConvertToUppercase<T>(IEnumerable<T> theInstance)
        {
            if (theInstance != null)
            {
                foreach (var theItem in theInstance)
                {
                    ConvertToUppercase(theItem);
                }
            }
        }
        #endregion

        #region Closing Entry Monthly
        // Get Monthly Duty Log Sheet List by Vehicle 
        public List<tbl_log_sheets> GetLogSheetListByVehicle(string startdate, string enddate, long vehicleId, string logIds, bool IsAdhoc)
        {
            var fromdt = DateTime.Parse(startdate);
            var todt = DateTime.Parse(enddate);
            List<tbl_log_sheets> _logList = null;
            if (string.IsNullOrEmpty(logIds) && IsAdhoc)
                return new List<tbl_log_sheets>();
            if (string.IsNullOrEmpty(logIds))
            {
                _logList = (from m in db.tbl_log_sheets
                            where (m.Status == true
                            && (m.ClosedFlag.Value == false || m.ClosedFlag.Value == null)
                            && m.LogDate.Value >= fromdt
                            && m.LogDate.Value <= todt
                            && m.VehicleID == vehicleId)
                            select m).ToList();
            }
            else
            {
                List<long> lstIds = logIds.Split(',').ToList().Select(a => Convert.ToInt64(a)).ToList();
                _logList = (from m in db.tbl_log_sheets
                            where (m.Status == true
                            && (m.ClosedFlag.Value == false || m.ClosedFlag.Value == null)
                            && lstIds.Contains(m.ID)
                            && m.VehicleID == vehicleId)
                            select m).ToList();
            }

            if (_logList.Count() == 0)
                return null;
            return _logList;
        }
        // Get Monthly Duty Log Sheet List by Vehicle For Closed Flag 1
        public List<tbl_log_sheets> GetClosedLogSheetListByVehicle(string startdate, string enddate, long vehicleId, string logIds)
        {
            var fromdt = DateTime.Parse(startdate);
            var todt = DateTime.Parse(enddate);
            List<tbl_log_sheets> _logList = null;
            if (string.IsNullOrEmpty(logIds))
            {
                // return null;
                _logList = (from m in db.tbl_log_sheets
                            where (m.Status == true
                            && (m.ClosedFlag.Value == true)
                            && m.LogDate.Value >= fromdt
                            && m.LogDate.Value <= todt
                            && m.VehicleID == vehicleId)
                            select m).ToList();
            }
            else
            {
                List<long> lstIds = logIds.Split(',').ToList().Select(a => Convert.ToInt64(a)).ToList();
                _logList = (from m in db.tbl_log_sheets
                            where (m.Status == true
                            && (m.ClosedFlag.Value == true)
                            && lstIds.Contains(m.ID)
                            && m.VehicleID == vehicleId)
                            select m).ToList();
            }
            if (_logList.Count() == 0)
                return null;
            return _logList;
        }
        // Get Monthly Diesel List by Vehicle 
        public List<tbl_diesel_tracking> GetDieselListByVehicle(string startdate, string enddate, long vehicleId, DateTime? closedDate, string dislIds, bool isAdhoc)
        {
            var fromdt = DateTime.Parse(startdate);
            var todt = DateTime.Parse(enddate);
            List<tbl_diesel_tracking> _dieselList = new List<tbl_diesel_tracking>();
            if (string.IsNullOrEmpty(dislIds) && isAdhoc)
                return _dieselList;
            if (string.IsNullOrEmpty(dislIds))
            {
                if (closedDate != null)
                {
                    _dieselList = (from m in db.tbl_diesel_tracking
                                   where (m.Status == true
                                   && m.Date.Value >= fromdt
                                   && m.Date.Value <= todt
                                   && m.PrevClosedDate.Value == closedDate
                                   && (m.ClosedFlag == null || m.ClosedFlag == false)
                                   && m.VehicleID == vehicleId)
                                   select m).ToList();
                }
                else
                {
                    _dieselList = (from m in db.tbl_diesel_tracking
                                   where (m.Status == true
                                   && m.Date.Value >= fromdt
                                   && m.Date.Value <= todt
                                   && (m.ClosedFlag == null || m.ClosedFlag == false)
                                   && m.VehicleID == vehicleId)
                                   select m).ToList();
                }
            }
            else
            {
                List<long> lstIds = dislIds.Split(',').ToList().Select(a => Convert.ToInt64(a)).ToList();
                _dieselList = (from m in db.tbl_diesel_tracking
                               where (m.Status == true
                               && lstIds.Contains(m.ID)
                               && (m.ClosedFlag == null || m.ClosedFlag == false)
                               && m.VehicleID == vehicleId)
                               select m).ToList();

            }
            if (_dieselList.Count() == 0)
                return null;
            return _dieselList;
        }
        // Get Monthly Diesel List by Vehicle For Closed 
        public List<tbl_diesel_tracking> GetClosedDieselListByVehicle(string startdate, string enddate, long vehicleId, DateTime? closedDate, string dislIds)
        {
            var fromdt = DateTime.Parse(startdate);
            var todt = DateTime.Parse(enddate);
            List<tbl_diesel_tracking> _dieselList = new List<tbl_diesel_tracking>();
            if (string.IsNullOrEmpty(dislIds))
            {
                return _dieselList;
            }
            else
            {
                List<long> lstIds = dislIds.Split(',').ToList().Select(a => Convert.ToInt64(a)).ToList();
                _dieselList = (from m in db.tbl_diesel_tracking
                               where (m.Status == true
                               && lstIds.Contains(m.ID)
                               && (m.ClosedFlag == true)
                               && m.VehicleID == vehicleId)
                               select m).ToList();
            }

            if (_dieselList.Count() == 0)
                return null;
            return _dieselList;
        }
        // Get Monthly Advances List by Vehicle 
        public List<tbl_advances> GetAdvancesListByVehicle(string startdate, string enddate, long vehicleId, DateTime? closedDate, string advIds, bool IsAdhoc)
        {
            var fromdt = DateTime.Parse(startdate);
            var todt = DateTime.Parse(enddate);
            List<tbl_advances> _advanceList = new List<tbl_advances>();
            if (string.IsNullOrEmpty(advIds) && IsAdhoc)
                return _advanceList;
            if (string.IsNullOrEmpty(advIds))
            {
                return _advanceList;
            }
            else
            {
                List<long> lstIds = advIds.Split(',').ToList().Select(a => Convert.ToInt64(a)).ToList();
                _advanceList = (from m in db.tbl_advances
                                where (m.Status == true
                                && (m.ClosedFlag == false || m.ClosedFlag == null)
                                && m.VehicleID == vehicleId
                                && lstIds.Contains(m.ID))
                                select m).ToList();
            }
            if (_advanceList.Count() == 0)
                return null;
            return _advanceList;
        }
        // Get Monthly Advances List by Vehicle For Closed
        public List<tbl_advances> GetClosedAdvancesListByVehicle(string startdate, string enddate, long vehicleId, DateTime? closedDate, string advIds)
        {
            var fromdt = DateTime.Parse(startdate);
            var todt = DateTime.Parse(enddate);
            List<tbl_advances> _advanceList = new List<tbl_advances>();
            if (string.IsNullOrEmpty(advIds))
            {
                if (closedDate != null)
                {
                    _advanceList = (from m in db.tbl_advances
                                    where (m.Status == true
                                    && m.PrevClosedDate.Value == closedDate
                                    && (m.ClosedFlag == true)
                                    && m.VehicleID == vehicleId)
                                    select m).ToList();
                }
                else
                {
                    _advanceList = (from m in db.tbl_advances
                                    where (m.Status == true
                                    && (m.ClosedFlag == true)
                                    && m.VehicleID == vehicleId)
                                    select m).ToList();
                }
            }
            else
            {
                List<long> lstIds = advIds.Split(',').ToList().Select(a => Convert.ToInt64(a)).ToList();
                _advanceList = (from m in db.tbl_advances
                                where (m.Status == true
                                && lstIds.Contains(m.ID)
                                && (m.ClosedFlag == true)
                                && m.VehicleID == vehicleId)
                                select m).ToList();
            }
            if (_advanceList.Count() == 0)
                return null;
            return _advanceList;
        }
        // Get Monthly EHS List by Vehicle 
        public List<tbl_ehs_details> GetEHSListByVehicle(string startdate, string enddate, long vehicleId, DateTime? closedDate)
        {
            var fromdt = DateTime.Parse(startdate);
            var todt = DateTime.Parse(enddate);
            List<tbl_ehs_details> _ehsList = new List<tbl_ehs_details>();
            if (closedDate != null)
            {
                _ehsList = (from m in db.tbl_ehs_details
                            where (m.Status == true
                            && m.CreatedDate.Value >= fromdt
                            && m.CreatedDate.Value <= todt
                            && m.PrevClosedDate.Value == closedDate
                            && m.VehicleID == vehicleId)
                            select m).ToList();
            }
            else
            {
                _ehsList = (from m in db.tbl_ehs_details
                            where (m.Status == true
                            && m.CreatedDate.Value >= fromdt
                            && m.CreatedDate.Value <= todt
                            && m.VehicleID == vehicleId)
                            select m).ToList();
            }
            if (_ehsList.Count() == 0)
                return null;
            return _ehsList;
        }
        // Get Monthly Penalties List by Vehicle 
        public List<tbl_penalties> GetPenaltiesListByVehicle(string startdate, string enddate, long vehicleId, DateTime? closedDate)
        {
            var fromdt = DateTime.Parse(startdate);
            var todt = DateTime.Parse(enddate);
            List<tbl_penalties> _penaltyList = new List<tbl_penalties>();
            if (closedDate != null)
            {
                _penaltyList = (from m in db.tbl_penalties
                                where (m.Status == true
                                && m.CreatedDate.Value >= fromdt
                                && m.CreatedDate.Value <= todt
                                && m.PrevClosedDate == closedDate
                                && m.tbl_log_sheets.VehicleID == vehicleId)
                                select m).ToList();
            }
            else
            {
                _penaltyList = (from m in db.tbl_penalties
                                where (m.Status == true
                                && m.CreatedDate.Value >= fromdt
                                && m.CreatedDate.Value <= todt
                                && m.tbl_log_sheets.VehicleID == vehicleId)
                                select m).ToList();
            }
            if (_penaltyList.Count() == 0)
                return null;
            return _penaltyList;
        }
        #endregion

        #region User Access
        public int GetUserAccessListByUser(string userName)
        {
            var userAccessList = db.tbl_user_access.Where(a => a.aspnet_Users.UserName.Trim() == userName.Trim()).ToList();
            foreach (var item in userAccessList)
            {
                tbl_user_access userAccess = db.tbl_user_access.Where(a => a.ID == item.ID).FirstOrDefault();
                db.DeleteObject(userAccess);
            }
            db.SaveChanges();
            return 0;
        }

        public static tbl_user_access GetUserAccess(int PageNo, string userName)
        {
            FMSDBEntities _db = new FMSDBEntities();
            tbl_user_access userAccess = _db.tbl_user_access.Where(a => a.tbl_pages.PageNo == PageNo && a.Status == true && a.aspnet_Users.UserName.Trim().ToUpper() == userName.Trim().ToUpper()).FirstOrDefault();
            if (userAccess == null)
            {
                if (HttpContext.Current.User.IsInRole("superadmin"))
                {

                    tbl_user_access userPer = new tbl_user_access();
                    userPer.List = true;
                    userPer.Add = true;
                    userPer.Edit = true;
                    userPer.Delete = true;
                    userPer.View = true;
                    userPer.Audit = true;
                    return userPer;
                }
                else
                {
                    tbl_user_access userPer = new tbl_user_access();
                    userPer.List = false;
                    userPer.Add = false;
                    userPer.Edit = false;
                    userPer.Delete = false;
                    userPer.View = false;
                    userPer.Audit = false;
                    return userPer;
                }
            }
            else
                return userAccess;
        }
        #endregion

        #region Send Mail
        public bool Sendmail(string Sendto, string username, string Password)
        {
            string EmailAddress = string.Empty;
            string PassWord = string.Empty;
            if (VerifySettingsCode("SenderId") && VerifySettingsCode("Password"))
            {
                EmailAddress = GetSettingsValueByCode("SenderId");
                PassWord = GetSettingsValueByCode("Password");
            }
            else
            {
                EmailAddress = WebConfigurationManager.AppSettings["EmailAddress"].ToString();
                PassWord = WebConfigurationManager.AppSettings["PassWord"].ToString();
            }

            String str = String.Empty;
            System.Net.Mail.MailMessage mail = new System.Net.Mail.MailMessage();
            SmtpClient smtp = new SmtpClient();
            try
            {
                mail.To.Add(Sendto);
                mail.From = new MailAddress(EmailAddress, ApplicationSettings.OrgName);
                mail.Subject = "Reset Password";
                mail.Body = "Dear " + username + ", <br/> <br/> Please find below username and password.<br/> <br/> <u> Username</u> :<b>" + username + "</b> <br/> <u> Password </u>: <b>" + Password + "</b> <br/> - <br/>Regards <br/> <br/> " + ApplicationSettings.OrgName + " Team.  ";
                mail.IsBodyHtml = true;
                mail.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;
                smtp.EnableSsl = true;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential(EmailAddress, PassWord);
                smtp.Host = "smtp.gmail.com";
                smtp.Port = 587;
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                mail.Headers.Add("Disposition-Notification-To", EmailAddress);
                smtp.Send(mail);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool Sendmail(string sendTo, string messageBody, string sendCC, string sendBCC)
        {
            string EmailAddress = string.Empty;
            string PassWord = string.Empty;
            if (VerifySettingsCode("SenderId") && VerifySettingsCode("Password"))
            {
                EmailAddress = GetSettingsValueByCode("SenderId");
                PassWord = GetSettingsValueByCode("Password");
            }
            else
            {
                EmailAddress = WebConfigurationManager.AppSettings["EmailAddress"].ToString();
                PassWord = WebConfigurationManager.AppSettings["PassWord"].ToString();
            }
            String str = String.Empty;
            System.Net.Mail.MailMessage mail = new System.Net.Mail.MailMessage();
            SmtpClient smtp = new SmtpClient();
            try
            {
                mail.To.Add(sendTo);
                mail.From = new MailAddress(EmailAddress, ApplicationSettings.OrgName);
                mail.Subject = ApplicationSettings.OrgName;
                if (!string.IsNullOrEmpty(sendCC))
                {
                    string[] ToMuliId = sendCC.Split(',');
                    foreach (string ToEMailId in ToMuliId)
                    {
                        mail.CC.Add(new MailAddress(ToEMailId)); //adding multiple TO Email Id
                    }
                }
                if (!string.IsNullOrEmpty(sendBCC))
                    mail.Bcc.Add(new MailAddress(sendBCC));
                mail.Body = messageBody;
                mail.IsBodyHtml = true;
                mail.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;
                smtp.EnableSsl = true;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential(EmailAddress, PassWord);
                smtp.Host = "smtp.gmail.com";
                smtp.Port = 587;
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                mail.Headers.Add("Disposition-Notification-To", EmailAddress);
                smtp.Send(mail);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string GetUnit(int id)
        {
            try
            {
                return _db.InventoryUnitsMasters.Where(a => a.Active == true && a.Id == id).FirstOrDefault().UnitsText;
            }
            catch
            {
                return "";
            }
        }
        #endregion

        #region Nlogging Each entries by UserName

        public void LoggingEntries(string Module, string Action, string UserName)
        {
            string clientIP = System.Web.HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
            GlobalDiagnosticsContext.Set("Module", Module);
            GlobalDiagnosticsContext.Set("UserName", UserName);
            GlobalDiagnosticsContext.Set("IpAddress", clientIP);
            _logger.Trace(Action);
        }

        public static void ExceptionLogging(string Message, string UserName, string StackTrace)
        {
            string clientIP = System.Web.HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
            GlobalDiagnosticsContext.Set("UserName", UserName);
            GlobalDiagnosticsContext.Set("IpAddress", clientIP);
            GlobalDiagnosticsContext.Set("StackTrace", StackTrace);
            _logger.Error(Message);
        }

        #endregion


        public string CardNo1 { get; set; }

        public string GetCardNo(long p, long? nullable)
        {
            throw new NotImplementedException();
        }
    }
    public enum ParticularType
    {
        Earnings = 1,
        Deductions = 2
    }
    public enum BookingStatus
    {
        Edit,
        WaitingForAcknowledgement,
        Acknowledgement,
        ConfirmBooking,
        LogSheet,
        AuditlogSheet,
        BookingDone
    }

    public class CustomJsonResponse
    {
        public bool success { get; set; }
        public string msg { get; set; }
    }
}