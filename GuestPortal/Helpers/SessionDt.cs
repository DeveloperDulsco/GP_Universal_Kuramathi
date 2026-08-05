
using CheckinPortal.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CheckinPortal.Helpers
{
    public class SessionDt
    {
        public string GuestSignedSignature { get; set; }
        public string ReservationNameID { get; set; }
        public string ReservationNumber { get; set; }
        public string ReservationStatus { get; set; }
        public string Nationality { get; set; }

    }
}