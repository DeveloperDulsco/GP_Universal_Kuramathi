using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CheckinPortal.Models.AdaptorAPIModels
{
    public partial class MakePaymentWithRedirectModel
    {
        public string ResponseMessage { get; set; }

        public object ResponseObject { get; set; }

        public bool Result { get; set; }
    }


    public class MakePaymentRedirectRootModel
    {
        [JsonProperty("resultCode")]
        public string ResultCode { get; set; }

        [JsonProperty("details")]
        public List<Detail> Details { get; set; }

        [JsonProperty("paymentData")]
        public string PaymentData { get; set; }

        [JsonProperty("redirect")]
        public Redirect Redirect { get; set; }

        [JsonProperty("action")]
        public Action Action { get; set; }
    }

    public partial class Action
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("paymentMethodType")]
        public string PaymentMethodType { get; set; }

        [JsonProperty("url")]
        public Uri Url { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("paymentData")]
        public string PaymentData { get; set; }

        [JsonProperty("data")]
        public Data Data { get; set; }
    }

    public partial class Data
    {
        [JsonProperty("PaReq")]
        public string PaReq { get; set; }

        [JsonProperty("TermUrl")]
        public Uri TermUrl { get; set; }

        [JsonProperty("MD")]
        public string Md { get; set; }
    }

    public partial class Detail
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }

    public partial class Redirect
    {
        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("data")]
        public Data Data { get; set; }

        [JsonProperty("url")]
        public Uri Url { get; set; }
    }
}