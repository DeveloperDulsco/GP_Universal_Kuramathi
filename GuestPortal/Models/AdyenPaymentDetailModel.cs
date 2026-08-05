using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CheckinPortal.Models.PaymentDetails
{
    public class Data
    {
        public string MD { get; set; }
        public string PaReq { get; set; }
        public string TermUrl { get; set; }
    }

    public class Action
    {
        public string paymentData { get; set; }
        public string paymentMethodType { get; set; }
        public string url { get; set; }
        public Data data { get; set; }
        public string method { get; set; }
        public string type { get; set; }
    }

    public class Detail
    {
        public string key { get; set; }
        public string type { get; set; }
    }

    public class Redirect
    {
        public Data data { get; set; }
        public string method { get; set; }
        public string url { get; set; }
    }

    public class MakePaymentDetailModel
    {
        public string resultCode { get; set; }
        public Action action { get; set; }
        public IList<Detail> details { get; set; }
        public string paymentData { get; set; }
        public Redirect redirect { get; set; }
    }


    public class ProcessExistingTransction
    {
        public string pspReferenceNo { get; set; }
        public string ReservationNo { get; set; }
        public decimal Amount { get; set; }
    }


    public class MakePaymentDetailRequestModelRoot
    {
        public string MerchantAccount { get; set; }
        public string apiKey { get; set; }
        public string RequestIdentifier { get; set; }
        public MakePaymentDetailRequestModel RequestObject { get; set; }
    }


    public class MakePaymentDetailRequestModel
    {
        //public paymentDetilRequestDetail details { get; set; } // direct impelementation

        public Dictionary<string,string> details { get; set; } // for cloud api implementation


        public string paymentData { get; set; }
    }

    public class paymentDetilRequestDetail
    {
        public string MD { get; set; }
        public string PaRes { get; set; }
    }



    public class AdditionalData
    {
        public string issuerCountry { get; set; }
        public string expiryDate { get; set; }
        public string cardBin { get; set; }
        public string cardHolderName { get; set; }
        public string cardSummary { get; set; }
        public string cardPaymentMethod { get; set; }
        public string merchantReference { get; set; }
        public string cardIssuingCountry { get; set; }

        public string authCode { get; set; }
        public string avsResult { get; set; }
        public string refusalReasonRaw { get; set; }

        public string authorisationMid { get; set; }

        public string acquirerAccountCode { get; set; }

        public string authorisedAmountValue { get; set; }

        public string cvcResult { get; set; }

        public string authorisedAmountCurrency { get; set; }

        public string avsResultRaw { get; set; }

        public string cvcResultRaw { get; set; }

        public string acquirerCode { get; set; }

        public string acquirerReference { get; set; }

    }

    public class amount
    {
        public string currency { get; set; }
        public string value { get; set; }
    }

    public class PaymentDetailResponseModel
    {
        public AdditionalData additionalData { get; set; }
        public string pspReference { get; set; }
        public string refusalReason { get; set; }
        public string resultCode { get; set; }
        public string refusalReasonCode { get; set; }
        public string merchantReference { get; set; }

        public amount amount { get; set; }
    }
}