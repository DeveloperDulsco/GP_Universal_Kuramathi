using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CheckinPortal.Models.DueOut
{
    public class FetchPaymentRequest
    {
        public string ReservationNameID { get; set; }
        public bool? isActive { get; set; }
    }
}