using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FMS.Models;

namespace FMS.Models
{
    public class InventoryModel
    {
        // Class for Inventory  
        public class InventoryViewModel
        {
            public double _Qty { get; set; }
            public string _Receipt { get; set; } 
            public long _Id { get; set; }
        }

        public class StockManagement
        {
            public StockManagement(List<InventoryStockRegDetail> RegDetails, List<InventoryIssuedDetail> IssueDetails)
            {
                this._Regdetiail = RegDetails;
                this._IssuedDetail = IssueDetails;
            }
            public StockManagement(InventoryStockRegister StockReg, List<InventoryStockRegDetail> StockRegDet)
            {
                this._StockReg = StockReg;
                this._Regdetiail = StockRegDet;
            }
            public List<InventoryStockRegDetail> _Regdetiail { get; set; }
            public List<InventoryIssuedDetail> _IssuedDetail { get; set; }
            public InventoryStockRegister _StockReg { get; set; }

        }
        public class IssuedStock
        {
            public IssuedStock(InventoryIssuedMaster IssuedMaster, List<InventoryIssuedDetail> IssuedDetails)
            {
                this._IssuedMaster = IssuedMaster;
                this._IssuedDetail = IssuedDetails;
            }
            public InventoryIssuedMaster _IssuedMaster { get; set; }
            public List<InventoryIssuedDetail> _IssuedDetail { get; set; }
        }
        public class IndentManagement
        {
            public IndentManagement(tbl_inv_indents IndentReg, tbl_inv_indent_details IndentRegDet)
            {
                this._IndentReg = IndentReg;
                this._Regdetiail = IndentRegDet;
            }
            public IndentManagement(tbl_inv_indents IndentReg, List<tbl_inv_indent_details> IndentDetList)
            {
                this._IndentReg = IndentReg;
                this._IndentDetList = IndentDetList;
            }
            public tbl_inv_indent_details _Regdetiail { get; set; }
            public List<tbl_inv_indent_details> _IndentDetList { get; set; }
            public tbl_inv_indents _IndentReg { get; set; }

        }
        
        public class PurchaseOrder
        {
            public PurchaseOrder(tbl_PurchaseOrders PurchaseOrder, List<tbl_PurchaseOrderItemDetails> PurchaseOrderDet)
            {
                this._PurchaseOrder = PurchaseOrder;
                this._PurchaseOrderDettails = PurchaseOrderDet;
            }
            public tbl_PurchaseOrders _PurchaseOrder { get; set; }
            public List<tbl_PurchaseOrderItemDetails> _PurchaseOrderDettails { get; set; }
        }
    }
    public class JobCardManagement
    {
        public JobCardManagement(tbl_jobcard JobCardReg, tbl_jobcard_details JobCardDet)
        {
            this._JobCard = JobCardReg;
            this._JobcardDetails = JobCardDet;
        }
        public JobCardManagement(tbl_jobcard JobCardReg, List<tbl_jobcard_details> JobCardDetList)
        {
            this._JobCard = JobCardReg;
            this._JobCardDetList = JobCardDetList;
        }
        public tbl_jobcard_details _JobcardDetails { get; set; }
        public List<tbl_jobcard_details> _JobCardDetList { get; set; }
        public tbl_jobcard _JobCard { get; set; }

    }
    // Class for Inventory 
    public class InventoryViewModel
    {
        public double _Qty { get; set; }
        public string _Invoice { get; set; }
        public long _Id { get; set; }
    } 
    public class GeneralClassFeilds
    {
        public double IssuedSlod { get; set; }
        public double ReceQty { get; set; }
        public double QOH { get; set; }

        public string ReceiptNo { get; set; }
        public decimal location { get; set; }
        public string ItemText { get; set; }


        public long invSrdId { get; set; }
        public long StockRegId { get; set; }
        public long longVal3 { get; set; }

        //public float ReceQty { get; set; }
       // public float IssuedSlod { get; set; }
        //public float QOH { get; set; }

        public int Slno { get; set; }
        public int UnitsId { get; set; }
        public int ItemId { get; set; }
    }
    public class MessMenu
    {
        public string BreakFast { get; set; }
        public string Lunch { get; set; }
        public string Snacks { get; set; }
        public string Dinner { get; set; }
    }


    public enum IndentItemStatus
    {
        Edit,
        Confirm,
        WaitingForApproval,
        Approved,
        InProgress,
        Received,
        Canceled,
        Deleted,
        Rejected
    }
    public enum JobCardItemStatus
    {
        Edit,
        Confirm,
        WaitingForApproval,
        Approved,
        InProgress,
        Received,
        Canceled,
        Deleted,
        Rejected
    }
    public enum JobCardStatus
    {
        Open,
        Pending,
        Closed
    }
    public enum IssuedItemStatus
    {
        Edit,
        Confirm,
        WaitingForAvailability,
        ReadyToTransfer,
        Transfer,
        Canceled,
        Deleted,
        Rejected
    }
    public enum PurchaseOrderStatus
    {
        Edit,
        PODraft,
        RFQ,
        BidReceived,
        ConfirmOrder,
        Done,
        Canceled,
        Deleted,
        Rejected
    }
}