using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CheckinPortal.Models
{
    public class StateMaster
    {
        public int StateMasterID { get; set; }
        public string Statename { get; set; }
        public string StateCode { get; set; }   
        public int? CountryMasterID { get; set; }
    }

    public class PoliciesModel
    {
        public string Base64Signature { get; set; }
        public bool IsTermsAndConditionAccepted { get; set; }
        public int ReservationID { get; set; }

        public bool? CheckBox1 { get; set; }
        public bool? CheckBox2 { get; set; }
        public bool? CheckBox3 { get; set; }
        public bool? CheckBox4 { get; set; }
        public string ReservationNameID { get; set; }
        public string ReservationNumber { get; set; }
    }

    public class UploadGuestDocumentModel
    {
        public string Doc1Base64 { get; set; }
        public string Doc2Base64 { get; set; }
        public string Doc3Base64 { get; set; }

        public int ReservationID { get; set; }

        public int ProfileDetailID { get; set; }
        public string ProfileID { get; set; }

        public string documentInformation { get; set; }
    }

    public class DocumentInformation
    {
        public string documentType { get; set; }
        public string documentNumber { get; set; }
        public string issueDate { get; set; }
        public string expiryDate { get; set; }
        public string gender { get; set; }
        public string firstName { get; set; }
        public string middleName { get; set; }
        public string lastName { get; set; }
        public string nationality { get; set; }
        public string issueCountry { get; set; }
        public string birthDate { get; set; }
        public string faceImage { get; set; }
        public string fullName { get; set; }
    }

    public class DocumentTypeMasterModel
    {
        public string DocumentCode { get; set; }
        public string DocumentType { get; set; }
        public string OperaDocumentCode { get; set; }
        public string DocumentType3CharCode { get; set; }
        public string IssueCountry3CharCode { get; set; }
    }
}