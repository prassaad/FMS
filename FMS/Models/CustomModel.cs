using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FMS.Models;
using System.Web.Mvc;

namespace FMS.Models
{
    public class CustomModel
    {
        public CustomModel()
        {

        }
        public CustomModel(List<SelectListItem> headerList, List<SelectListItem> availColsList)
        {
            this._headerList = headerList;
            this._availColsList = availColsList;
        }

        public List<SelectListItem> _headerList { get; set; }
        public List<SelectListItem> _availColsList { get; set; }
    }

    // For Vendor Invoice 

    public class GenerateInvoice
    {
        public GenerateInvoice()
        {
        }
        public GenerateInvoice(List<InvoiceList> InvoiceList, tbl_vehicles VehicleDet, List<DieselTracking> DieselList)
        {
            this._InvoiceList = InvoiceList;
            this._VehicleDet = VehicleDet;
            this._DieselDet = DieselList;
        }

        public GenerateInvoice(List<tbl_vendor_invoice_details> venInvDetList, tbl_vendor_invoices venInvDet, tbl_vehicles VehicleDet, tbl_vendor_payments venInvPayment)
        {
            this._VehicleDet = VehicleDet;
            this._venInvDetList = venInvDetList;
            this._venInvDet = venInvDet;
            this._venInvPayment = venInvPayment;
        }

        public List<InvoiceList> _InvoiceList { get; set; }
        public tbl_vehicles _VehicleDet { get; set; }
        public List<DieselTracking> _DieselDet { get; set; }
        public tbl_vendor_invoices _venInvDet { get; set; }
        public List<tbl_vendor_invoice_details> _venInvDetList { get; set; }
        public tbl_vendor_payments _venInvPayment { get; set; }
    }

    // For Documents 

    public class DocumentManagement
    {
        public DocumentManagement()
        {
        }
        public DocumentManagement(List<tbl_doc_client_nodes> _clientList, List<tbl_doc_driver_nodes> _driverList, List<tbl_doc_vehicle_nodes> _vehicleList, List<tbl_doc_vendor_nodes> _vendorList)
        {
            this.clientList = _clientList;
            this.vehicleList = _vehicleList;
            this.vendorList = _vendorList;
            this.driverList = _driverList;
        }
        public List<tbl_doc_client_nodes> clientList { get; set; }
        public List<tbl_doc_driver_nodes> driverList { get; set; }
        public List<tbl_doc_vehicle_nodes> vehicleList { get; set; }
        public List<tbl_doc_vendor_nodes> vendorList { get; set; }
    }

    // For Employee Management

    public class EmployeeManagement
    {
        public EmployeeManagement(tbl_employees _EmployeesDet, List<GeneralClassFields> _CustomList, tbl_emp_payables _EmpPayBill)
        {
            this.employees = _EmployeesDet;
            this.customList = _CustomList;
            this.empPayBill = _EmpPayBill;
        }
        public tbl_employees employees { get; set; }
        public tbl_emp_payables empPayBill { get; set; }
        public List<GeneralClassFields> customList { get; set; }
    }

    public class UserCardEntryModel
    {
        public Int64 Id { get; set; }
        public Int64? VehicleId { get; set; }
        public Int64? ClientId { get; set; }
        public Int64? PackageId { get; set; }
        public Int64? VehicleTypeId { get; set; }
        public Int64? VehicleModelId { get; set; }
        public Int64? ProjectId { get; set; }
        public Int64? DriverId { get; set; }
        public Int64? LocationId { get; set; }
        public Int64? SeaterId { get; set; }
        public Int64? AlterVehId { get; set; }
        public DateTime? EffectedDt { get; set; }
        public DateTime? ExpiredDt { get; set; }
        public string TripType { get; set; }
        public string CardNo { get; set; }
        public string LogIn { get; set; }
        public string LogOut { get; set; }
        public string AlterVehNumber { get; set; }
        public string Mobile { get; set; }
        public decimal? Amount { get; set; }
        public bool IsNoCab { get; set; }


    }

    //By sarath & vinod 19-02-2016
    public class AdhocListModel
    {
        public Int64 Id { get; set; }
        public Int64? ClientId { get; set; }
        public Int64? ProjectId { get; set; }
        public string Location { get; set; }
        public string AlterVehNumber { get; set; }
        public string ContactNumber { get; set; }
        public decimal? NetAmount { get; set; }
        public DateTime? LogDate { get; set; }
        public bool? IsAdhoc { get; set; }
        public Int64? VehicleTypeID { get; set; }
        public string EmpName { get; set; }
        public string TripType { get; set; }
        public Int64? TotalHrs { get; set; }
        public Int64? PackId { get; set; }
    }

    public class NocabListModel
    {
        public Int64 Id { get; set; }
        public Int64? ClientId { get; set; }
        public Int64? ProjectId { get; set; }
        public string Location { get; set; }
        public string AlterVehNumber { get; set; }
        public string ContactNumber { get; set; }
        public decimal? NetAmount { get; set; }
        public DateTime? LogDate { get; set; }
        public bool? IsNocab { get; set; }
        public Int64? VehicleTypeID { get; set; }
        public string EmpName { get; set; }
        public string TripType { get; set; }
        public string CardNumber { get; set; }
        public string ShiftTime { get; set; }
        public string ReachTime { get; set; }
        public Int64? PackId { get; set; }
    }


    // For Vendor Invoice List
    public class InvoiceList
    {
        public long ClientID { get; set; }
        public string Client { get; set; }
        public int Qty { get; set; }
        public double Rate { get; set; }
        public decimal TotalAmt { get; set; }
        public decimal NetTotal { get; set; }
        public int Trips { get; set; }
        public long VehicleTypeID { get; set; }
        public string BillingType { get; set; }
        public int IsBooking { get; set; }

    }
    // For Diesel Book Management
    public class DieselTracking
    {
        public double TokenValue { get; set; }
        public decimal PricePerLiter { get; set; }
        public decimal TotalAmt { get; set; }
    }
    // For Penalties 
    public class LogSheetDetail
    {
        public string VehicleRegNum { get; set; }
        public string Seater { get; set; }
        public string Location { get; set; }
        public string TripType { get; set; }
        public string LogTime { get; set; }
        public string ActualPax { get; set; }
    }

    //For UserLogReport
    public class UserLogReport
    {
        public string UserName { get; set; }
        public DateTime UserDt { get; set; }
        public string LogAddCnt { get; set; }
        public string LogEditCnt { get; set; }
        public string LogAudCnt { get; set; }
        public string DesAddCnt { get; set; }
        public string DesAudCnt { get; set; }
    }

    // General Class 

    public class GeneralClassFields
    {
        public long ID { get; set; }
        public long ID1 { get; set; }


        public int Value1 { get; set; }
        public int Value7 { get; set; }

        public decimal Value2 { get; set; }
        public decimal Value4 { get; set; }
        public decimal Value5 { get; set; }

        public decimal Amount { get; set; }
        public decimal Amount1 { get; set; }
        public decimal Amount2 { get; set; }

        public double Value3 { get; set; }
        public double Value6 { get; set; }

        public string Text1 { get; set; }
        public string Text2 { get; set; }
        public string Text3 { get; set; }
        public string Text4 { get; set; }
        public string Text5 { get; set; }
        public string Text6 { get; set; }
        public string Text7 { get; set; }
        public string Text8 { get; set; }
        public string Text9 { get; set; }
        public string Text10 { get; set; }

        public DateTime Date1 { get; set; }
        public DateTime StartDt { get; set; }
        public DateTime EndDt { get; set; }

        public bool Status { get; set; }
    }

    public class VehicleDieselMailAge
    {
        public long? ID { get; set; }
        public DateTime Date { get; set; }
    }

    // ----------------------------------------------------------
    // ------------------ For Client Invoice --------------------
    // ----------------------------------------------------------

    public class ClientInvoice
    {
        public tbl_client_invoice clientInvoices { get; set; }
        public List<tbl_client_payments> clientPayments { get; set; }

        public ClientInvoice(tbl_client_invoice _clientInvoice, List<tbl_client_payments> _clientPayment)
        {
            this.clientInvoices = _clientInvoice;
            this.clientPayments = _clientPayment;
        }
    }

}