using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CheckinPortal.Models
{
    public class SavePaymentDetailModel
    {
        public string PaRes { get; set; }
        public string paymentData { get; set; }
        public string ConfirmationNumber { get; set; }
    }
}