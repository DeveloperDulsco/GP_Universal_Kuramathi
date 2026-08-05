using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CheckinPortal.Models
{
    public class MakePaymentCheckinPortalModels
    {
        public MakePaymentRequest  makePaymentRequest{ get; set; }
        public string ConfirmationNo { get; set; }
        public decimal  AmountToCharge { get; set; }
        public string MerchantReference { get; set; }
        public string FundingSource { get; set; }
        public long TransctionID { get; set; }

        public string ReservationNameID { get; set; }
    }


    public class TopupTransctionModels
    {
        public string PspReferenceNumber { get; set; }
        public decimal AmountToCharge { get; set; }
        public string AdjustAuthorisationData { get; set; }
    }


    public class CaptureTransctionModels
    {
        public string PspReferenceNumber { get; set; }
        public decimal AmountToCharge { get; set; }
    }




    public class InsertPaymentHistoryUspModel
    {
        public string ReservationNumber { get; set; }
        public string PData { get; set; }
        public string MDData { get; set; }
        public string PaRes { get; set; }
        public string PSPReference { get; set; }
        public string ResultCode { get; set; }
        public string RefusalReason { get; set; }

        public string TransactionType { get; set; }

        public string ReservationNameID { get; set; }

        public string TransactionID { get; set; }
    }

}