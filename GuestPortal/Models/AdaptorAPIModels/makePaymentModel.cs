using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CheckinPortal.Models.AdaptorAPIModels
{
    public partial class makePaymentModel
    {
        [JsonProperty("MerchantAccount")]
        public string MerchantAccount { get; set; }
        [JsonProperty("apiKey")]
        public string ApiKey { get; set; }
        [JsonProperty("RequestObject")]
        public RequestObject RequestObject { get; set; }
    }

    public partial class RequestObject
    {
        [JsonProperty("amount")]
        public string Amount { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("isEnableOneClick")]
        public bool IsEnableOneClick { get; set; }

        [JsonProperty("isRecurringEnable")]
        public bool IsRecurringEnable { get; set; }

        [JsonProperty("mcc")]
        public string Mcc { get; set; }

        [JsonProperty("merchantReference")]
        public string MerchantReference { get; set; }

        [JsonProperty("prev_PSPRefernce")]
        public object PrevPspRefernce { get; set; }

        [JsonProperty("paymentMethod")]
        public object PaymentMethod { get; set; }

        [JsonProperty("refernceUniqueID")]
        public long RefernceUniqueId { get; set; }

        [JsonProperty("returnUrl")]
        public Uri ReturnUrl { get; set; }

        [JsonProperty("hotelDomain")]
        public string HotelDomain { get; set; }

        [JsonProperty("ReservationNameID")]
        public string ReservationNameID { get; set; }

        [JsonProperty("BrowserInfo")]
        public object BrowserInfo { get; set; }

        [JsonProperty("TransactionType")]
        public string TransactionType { get; set; }

        public string orginalPSPRefernce { get; set; }

        public string refernce { get; set; }

        public string AdjustAuthorisationData { get; set; }
    }


    public partial class BrowserInfo
    {
    }
}