using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CheckinPortal.Models.Emails;

namespace CheckinPortal.Models.Whatsapp
{
    public class WhatsappModel
    {
    }
    public class WhatsAppTemplateRequest
    {
        public string ReceiverPhone { get; set; }

        public EmailType TemplateName { get; set; }

        public string LanguageCode { get; set; }

        public List<string> BodyParameters { get; set; }

        public string ButtonUrlParameter { get; set; }
    }
    public class WhatsAppResponse
    {
        public object responseData { get; set; }
        public bool result { get; set; }
        public string responseMessage { get; set; }
        public int statusCode { get; set; }
    }
}