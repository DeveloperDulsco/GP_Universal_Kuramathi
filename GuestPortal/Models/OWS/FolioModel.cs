using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CheckinPortal.Models.OWS
{
    public class OperaReservationResponseModel
    {
        public List<ReservationList> Data = new List<ReservationList>();
        public string ResponseMessage { get; set; }
        public int StatusCode { get; set; }
        public bool Result { get; set; }
        public List<string> Documents = new List<string>();
        public FolioModel GuestFolio { get; set; }
        public bool ProceedWithCheckin { get; set; }
    }

    public class ReservationList
    {
        public int ConfirmationID { get; set; }
        public string ReservationNumber { get; set; }
        public string RoomNumber { get; set; }
        public DateTime? ExpectedArrivalDate { get; set; }
        public DateTime? ExpectedDepartureDate { get; set; }
        public string RoomTypeCode { get; set; }
        public string RoomTypeDescription { get; set; }
        public string RateCode { get; set; }
        public decimal? RateCodeAmount { get; set; }
        public string PackageCode { get; set; }
        public DateTime? CreatedDateTime { get; set; }
        public int? AdultsCount { get; set; }
        public int? ChildrenCount { get; set; }
        public string PaymentType { get; set; }
        public bool? SendToCloud { get; set; }
        public bool? EmailSent { get; set; }
        public string VicasConfirmationNo { get; set; }
        public string OperaReservationNameID { get; set; }
        public bool? SendBackToHotel { get; set; }
        public string Base64PDF { get; set; }
        public string SignatureBase64Image { get; set; }
        public string GuestMessage { get; set; }

        public DateTime? ExpectedDepartureTime { get; set; }
        public string CheckoutTime { get; set; }

        public IList<ProfileList> ProfileList { get; set; }
        public IList<ResPreferance> ReservationPreferances { get; set; }
        public IList<ResPackage> ReservationPackages { get; set; }

        public IList<FolioItemsModel> FolioItems { get; set; }
        public bool IsTaxInclusive { get; set; }

        public double? TotalAmount { get; set; } // This field coming null after checkin response.
        public bool BreakfastIncluded { get; set; }


        public string HotelDomain { get; set; }
        public string Language { get; set; }
        public string KioskID { get; set; }
        public string LegNumber { get; set; }
        public string ChainCode { get; set; }
        public string CardType { get; set; }
        public string SystemType { get; set; }
        public string KioskUserName { get; set; }
        public string KioskPassword { get; set; }

        public bool IsCheckedIn { get; set; }

        public string RoomStatus { get; set; }
        public string ReservationStatus { get; set; }
        public string ReservationType { get; set; }
        public bool IsMultipleRate { get; set; }
        public bool PrintRate { get; set; }
        public string RTC { get; set; }

        public string MembershipNumber { get; set; }//not required here 


        public bool IsCheckinAllowed { get; set; }//derived field.
        public bool IsCheckoutAllowed { get; set; }//derived field.
        public decimal PreAuthAmount { get; set; }//derived field
        public string ReservationStatusMessage { get; set; }//for showing message
        public string SharerConfirmationNos { get; set; }//for showing in reservation details
        public int AuthorizationMethod { get; set; }
        public decimal BreakfastAmount { get; set; }
        public bool IsMemberShipPrint { get; set; }


        public List<ReservationList> SharerReservations { get; set; }//to handle sharer reservation

        public List<UserdefinedFIelds> UserDefinedFields { get; set; }

        public string KioskStationName { get; set; }
    }


    public class UserdefinedFIelds
    {
        public string fieldValue { get; set; }
        public string fieldName { get; set; }
    }

    public class ResPackage
    {
        public int ReservationPackageID { get; set; }
        public int ReservationID { get; set; }
        public string PackageCode { get; set; }
        public string PackageDescription { get; set; }
        public decimal? PackageAmount { get; set; }
    }

    public class ResPreferance
    {
        public int ReservationPreferanceID { get; set; }
        public int ReservationID { get; set; }
        public string PreferanceCode { get; set; }
        public decimal? PreferanceAmount { get; set; }
    }

    public class ProfileList
    {

        public string PmsProfileID { get; set; } //Addedd
        public string FamilyName { get; set; } // Added
        public string GivenName { get; set; } //Added
        public string GuestName { get; set; } //Added
        public string Nationality { get; set; } //Added
        public string Gender { get; set; } //Added
        public string PassportNumber { get; set; } //Not Needed
        public string DocumentType { get; set; } // Not Needed
        //public string Address1 { get; set; }//Addedd
        //public string Address2 { get; set; }
        //public string City { get; set; }//Addedd
        //public string State { get; set; }//Addedd
        //public string Zip { get; set; }//Addedd
        //public string Email { get; set; } //Added
        //public string MobileNo { get; set; } //Added
        //public string Country { get; set; }//Addedd
        public bool IsPrimary { get; set; } //Added
        //public int? AddressDisplaySequence { get; set; }
        //public long? AddressOperaID { get; set; }
        //public string AddressType { get; set; }
        //public bool? IsPrimaryAddress { get; set; }
        //public long? EmailOperaID { get; set; }
        //public bool? IsPrimaryEmail { get; set; }
        public string BirthDate { get; set; } // yyyy-MM-dd
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public List<PhoneData> Phones { get; set; }
        public List<AddressData> Address { get; set; }
        public List<EmailData> Email { get; set; }

        public string IssueDate { get; set; }
        public string IssueCountry { get; set; }//2 char
        public string ExpiryDate { get; set; }


        public string PrimaryDocumentImage { get; set; }
        public string FaceImage { get; set; }
        public string MatchedFaceImage { get; set; }
        public List<string> AdditionalDocumentImage { get; set; }

        public string MembershipType { get; set; }
        public string MembershipNumber { get; set; }
        public string MembershipID { get; set; }


        //for local saving purpose
        public string NationalityFullName { get; set; }
        public string NationalityThreeCode { get; set; }
        public string IssueCountryFullName { get; set; }
        public string IssueCountryThreeCode { get; set; }

        public bool IsManualFaceAuthendication { get; set; }
        public string ManualFaceAuthendicatedPerson { get; set; }
    }

    public class PhoneData
    {
        public string phoneType { get; set; }
        public string phoneRole { get; set; }
        public long operaId { get; set; }
        public bool? primary { get; set; }
        public int? displaySequence { get; set; }
        public string PhoneNumber { get; set; }

    }

    public class EmailData
    {
        public string emailType { get; set; }
        public long operaId { get; set; }
        public bool? primary { get; set; }
        public int? displaySequence { get; set; }
        public string email { get; set; }

    }

    public class AddressData
    {
        public string addressType { get; set; }
        public long operaId { get; set; }
        public bool? primary { get; set; }
        public int? displaySequence { get; set; }
        public string address1 { get; set; }
        public string address2 { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string country { get; set; }
        public string zip { get; set; }

    }

    

    

    
}