using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Windows.Documents.DocumentStructures;

namespace CheckinPortal.Models
{
    public class CheckoutReservationModel
    {
        public string  FoliodBase64 { get; set; }
        public string ReservationNumber { get; set; }
        public string EmailID { get; set; }
        public string FullName { get; set; }
        public int ReservationID { get; set; }

        public string ReservationNameID { get; set; }

        public decimal FolioAmount { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal BalanecAmount { get; set; }
        public decimal PaidAmount { get; set; }


    }
}