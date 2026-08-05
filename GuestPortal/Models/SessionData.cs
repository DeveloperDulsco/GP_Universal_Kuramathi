using CheckinPortal.Models.BlinkIdModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CheckinPortal.Models
{

    //public static class SessionData
    //{
    //    public static OperaReservationModel OperaReservation { get; set; }
    //    public static List<OperaReservationModel> SharerReservation
    //    {
    //        get; set;
    //    }
    //    public static List<OperaReservationModel> OperaReservationList { get; set; }
    //}
    public static class SessionData
    {
        public static OWS.OperaReservation OperaReservation { get; set; }
        public static List<OWS.OperaReservation> SharerReservation
        {
            get; set;
        }
        public static OWS.FolioModel FolioModel { get; set; }
        public static string FolioBase64 { get; set; }
        public static List<OperaReservationModel> OperaReservationList { get; set; }

        public static List<BlinkIdDocumentResponseModel> ScanDocuments { get; set; }
    }
}