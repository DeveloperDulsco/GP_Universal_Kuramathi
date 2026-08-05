using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Web;

namespace CheckinPortal.Models.Emails
{
    public class EmailRequest
    {
        public string FromEmail { get; set; }
        public string ToEmail { get; set; }
        public bool IsPrecheckinEmail { get; set; }
        public string GuestName { get; set; }
        public string Subject { get; set; }
        public string confirmationNumber { get; set; }
        public string displayFromEmail { get; set; }
        public EmailType EmailType { get; set; }
        public string AttchmentBase64 { get; set; }
        public string ItemName { get; set; }
        public string TotalAmount { get;set; }
        public string AttachmentFileName { get; set; }
        public string ArrivalDate { get; set; }
        public string DepartureDate { get; set; }
        public string ReservationNumber { get; set; }
    }

    public enum EmailType
    {
        Precheckedin,
        PreCheckedout,
        CheckinConfirmation,
            GuestFolio,
            AcceptRequest,
            RejectRequest,
            CheckinSlip,
        ServiceError
    }


    public class EmailResponse
    {
        public object responseData { get; set; }
        public bool result { get; set; }
        public string responseMessage { get; set; }
        public int statusCode { get; set; }
    }
}