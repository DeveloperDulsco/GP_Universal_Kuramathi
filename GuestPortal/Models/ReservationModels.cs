using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CheckinPortal.Models
{
    public partial class GetReservationDetailsModel
    {
        public int ReservationDetailID { get; set; }
        public string ReservationNameID { get; set; }

        [Display(Name ="ReservationNumber",ResourceType =typeof(Resource))]
        public string ReservationNumber { get; set; }
        public Nullable<System.DateTime> ArrivalDate { get; set; }
        public Nullable<System.DateTime> DepartureDate { get; set; }
        public Nullable<int> Adultcount { get; set; }
        public Nullable<int> Childcount { get; set; }
        public string MembershipNo { get; set; }
        public string MembershipType { get; set; }
        public Nullable<bool> IsDepositAvailable { get; set; }
        public Nullable<bool> IsCardDetailPresent { get; set; }
        public Nullable<bool> IsSaavyPaid { get; set; }
        public Nullable<bool> IsPreCheckedInPMS { get; set; }
        public Nullable<bool> IsPreCheckedOutPMS { get; set; }
        public string FlightNo { get; set; }
        public Nullable<System.DateTime> ETA { get; set; }
        public Nullable<decimal> AverageRoomRate { get; set; }
        public Nullable<decimal> TotalTax { get; set; }
        public string RoomType { get; set; }
        public string RoomTypeDescription { get; set; }
        public Nullable<decimal> TotalAmount { get; set; }
        public byte[] FolioDocument { get; set; }
        public Nullable<bool> EcomPaymentCheckInStatus { get; set; }
        public Nullable<bool> EcomPaymentCheckOutStatus { get; set; }
        public Nullable<decimal> PaidAmount { get; set; }
        public Nullable<decimal> BalanceAmount { get; set; }
        public string ReservationSource { get; set; }
    }
}