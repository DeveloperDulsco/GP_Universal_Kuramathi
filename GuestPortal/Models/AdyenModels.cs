using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CheckinPortal.Models
{

    public enum TransactionType
    {
        PreAuth,
        Capture,
        Sale
    }

    public partial class PaymentModel
    {
        [JsonProperty("amount")]
        public Amount Amount { get; set; }

        [JsonProperty("reference")]
        public string Reference { get; set; }

        [JsonProperty("paymentMethod")]
        public PaymentMethod PaymentMethod { get; set; }

        [JsonProperty("browserInfo")]
        public BrowserInfo browserInfo { get; set; }

        [JsonProperty("returnUrl")]
        public Uri ReturnUrl { get; set; }

        [JsonProperty("merchantAccount")]
        public string MerchantAccount { get; set; }
    }

    public partial class Amount
    {
        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("value")]
        public long Value { get; set; }
    }

    public partial class PaymentMethod
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("encryptedCardNumber")]
        public string EncryptedCardNumber { get; set; }

        [JsonProperty("encryptedExpiryMonth")]
        public string EncryptedExpiryMonth { get; set; }

        [JsonProperty("encryptedExpiryYear")]
        public string EncryptedExpiryYear { get; set; }

        [JsonProperty("encryptedSecurityCode")]
        public string EncryptedSecurityCode { get; set; }

        
    }

    public class BrowserInfo
    {
        public string acceptHeader { get; set; }
        public string colorDepth { get; set; }
        public string javaEnabled { get; set; }
        public string language { get; set; }
        public string screenHeight { get; set; }
        public string screenWidth { get; set; }

        public string timeZoneOffset { get; set; }

        public string userAgent { get; set; }
    }

    public class RiskData
    {
        public string clientData { get; set; }
    }

    public class MakePaymentRequest
    {
        public BrowserInfo browserInfo { get; set; }
        public PaymentMethod paymentMethod { get; set; }

        public RiskData riskData { get; set; }
    }


    public class PaymentDetailsModel
    {
        public string paymentData { get; set; }
        public paymentDetailsDetailModel details { get; set; }

    }

    

    public class paymentDetailsDetailModel
    {
        public string MD { get; set; }
        public string PaRes { get; set; }
    }
}