using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CheckinPortal.Models.OWS
{
    public class OwsRequestModel
    {
        public string HotelDomain { get; set; }
        public string KioskID { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string SystemType { get; set; }
        public string Language { get; set; }
        public string LegNumber { get; set; }
        public string ChainCode { get; set; }
        public string DestinationEntityID { get; set; }
        public string DestinationSystemType { get; set; }
        public bool? SendFolio { get; set; }
        public BookingSummaryRequestModel BookingSummaryRequest { get; set; }
        public FetchProfileRequest fetchProfileRequest { get; set; }
        public FetchBookingRequestModel FetchBookingRequest { get; set; }
        public FetchBookedPackagesRequestModel FetchBookedPackagesRequest { get; set; }
        //public FetchRoomList FetchRoomList { get; set; }
        public PaymentMethod paymentMethod { get; set; }
        public OperaReservation OperaReservation { get; set; }
        public PreregisterReservationRequest PreregisterReservationRequest { get; set; }
        public UpdateProfile UpdateProileRequest { get; set; }
        public FetchRoomList FetchRoomList { get; set; }
        public ModifyBookingRequest modifyBookingRequest { get; set; }
        public MakePaymentRequest MakePaymentRequest { get; set; }
        public object FetchMaster { get; set; }
        public AssignRoomRequest AssignRoomRequest { get; set; }
        public FetchFolioRequest FetchFolioRequest { get; set; }
        public CreateAccompanyingProfileRequest CreateAccompanyingProfileRequest { get; set; }
        public ModifyPackageRequest ModifyPackageRequest { get; set; }
        public FetchGuestMessageRequest FetchGuestMessageRequest { get; set; }
        public ReservationQueueRequest ReservationQueueRequest { get; set; }
        public FetchGuestRequest FetchGuestRequest { get; set; }
        public EncodeKeyRequest EncodeKeyRequest { get; set; }
        public string RequestIdentifier { get; set; }
        public GuestComments GuestCommentRequest { get; set; }
        public string ProfileID { get; set; }
        public UpdateProfileAllergyRequest UpdateProfileAllergyRequest { get; set; }
    }
    public class FetchGuestRequest
    {
        public string ReservationNumber { get; set; }
    }


    public class FetchGuestMessageRequest
    {
        public string ReservationNameID { get; set; }
    }


    public class ReservationQueueRequest
    {
        public string ReservationNameID { get; set; }
    }
    public class GuestMessage
    {
        public DateTime? MessageDate { get; set; }
        public string MessageID { get; set; }
        public string RecepientName { get; set; }
        public string Message { get; set; }
        public string MessageStatus { get; set; }
    }

    public class GuestComments
    {
        public bool isGuestViewable { get; set; }
        public string Comment { get; set; }
        public string commentID { get; set; }
        public bool isInternal { get; set; }
        public string CommentType { get; set; }
    }

    public class CreateAccompanyingProfileRequest
    {
        public string ReservationNumber { get; set; }
        public string Gender { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }

    }

    public class FetchFolioRequest
    {
        public OperaReservation OperaReservation { get; set; }
        public string GuestSignature { get; set; }
        public string ReservationNameID { get; set; }
        public string ProfileID { get; set; }
        public FolioModel FolioList { get; set; }
    }

    public class FolioModel
    {
        public string ConfirmationNo { get; set; }
        public string FolioNo { get; set; }
        public string GuestName { get; set; }
        public string AddressLine { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string PostalCode { get; set; }
        public bool? IsAllowedForCheckOut { get; set; }
        public List<FolioTaxItemsModel> TaxItems { get; set; }
        public List<FolioItemsModel> Items { get; set; }
        public decimal BalanceAmount { get; set; }
        public List<FolioWindow> FolioWindows { get; set; }
        public decimal ReservationBalance { get; set; }
    }

    public class FolioWindow
    {
        public string GuestName { get; set; }
        public string PMSProfileID { get; set; }
        public List<FolioTaxItemsModel> TaxItems { get; set; }
        public List<FolioItemsModel> Items { get; set; }
        public decimal BalanceAmount { get; set; }
        public int? WindowNumber { get; set; }
    }

    public class FolioItemsModel
    {
        public string ItemName { get; set; }
        public decimal Amount { get; set; }
        public DateTime? Date { get; set; }
        public bool IsCredit { get; set; }
        public string TransactionCode { get; set; }
        public string TransactionNumber { get; set; }
        public bool? IsTax { get; set; }
        public int? WindowNumber { get; set; }
        public int? Quantity { get; set; }
    }

    public class FolioTaxItemsModel
    {
        public string ItemName { get; set; }
        public decimal Amount { get; set; }
        public int? WindowNumber { get; set; }
        public string TransactionCode { get; set; }
        public string TransactionNumber { get; set; }
    }

    public class AssignRoomRequest
    {
        public string ReservationNameID { get; set; }
        public string RoomNumber { get; set; }
        public string StationID { get; set; }
    }

    public class FetchMasters
    {
        public string ContryCode { get; set; }
    }

    public class CountryState
    {
        public string CountryName { get; set; }
        public string CountryCode { get; set; }
        public string StateCode { get; set; }
        public string StateName { get; set; }
    }

    public class DocumentTypeMaster
    {
        public string DocumentTypeCode { get; set; }
        public string DocumentTypeDescription { get; set; }
    }
    public class PhoneTypeMaster
    {
        public string PhoneTypeCode { get; set; }
        public string PhoneTypeDescription { get; set; }
    }

    public class FetchRoomList
    { 
        public List<string> RoomTypes { get; set; }
        public DateTime? DepartureDate { get; set; }
        public string RoomNumber { get; set; }
        public string RoomType { get; set; }
        public string RoomStatus { get; set; }

    }

    public class MakePaymentRequest
    {
        public string PaymentInfo { get; set; }
        public decimal Amount { get; set; }
        public string StationID { get; set; }
        public int? WindowNumber { get; set; }
        public string ReservationNameID { get; set; }
        public string PaymentTypeCode { get; set; }
        public string MaskedCardNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string UserName { get; set; }
        public string PaymentRefernce { get; set; }
        public string ApprovalCode { get; set; }
        public string PaymentTerminalID { get; set; }
        public string Rule { get; set; }
        public bool IsSale { get; set; }

    }

    public class OPIPaymentResponseModel
    {

        public string PaymentTypeCode { get; set; }
        public string AprovalCode { get; set; }
        public string ApprovalAmount { get; set; }
        public string MaskedCardNumber { get; set; }
    }

    public class ModifyBookingRequest
    {
        public string ReservationNumber { get; set; }
        public bool? isUDFFieldSpecified { get; set; }
        public List<UDFField> uDFFields { get; set; }
        public bool? updateCreditCardDetails { get; set; }
        public string GarunteeTypeCode { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public bool? isETASpecified { get; set; }
        public DateTime? ETA { get; set; }
    }

    public class ModifyPackageRequest
    {
        public string ProductCode { get; set; }
        public int? Quantity { get; set; }
        public bool QuantitySpecified { get; set; }
        public string ReservationNumber { get; set; }
    }

    public class UDFField
    {
        public string FieldName { get; set; }
        public string FieldValue { get; set; }
    }

    public class PreregisterReservationRequest
    {
        public string ReservationNameID { get; set; }
        public string ReservationNumber { get; set; }
        public string LegNumber { get; set; }
    }

    public class UpdateProfile
    {
        public List<Address> Addresses { get; set; }
        public string ProfileID { get; set; }
        public List<Email> Emails { get; set; }
        public List<Phone> Phones { get; set; }
        public DateTime? DOB { get; set; }
        public string Gender { get; set; }
        public string Nationality { get; set; }
        public string IssueCountry { get; set; }
        public string DocumentNumber { get; set; }
        public string DocumentType { get; set; }
        public DateTime? IssueDate { get; set; }
        public bool? isFieldMandatory { get; set; }
    }

    public class FetchProfileRequest
    {
        public string NameID { get; set; }
    }

    public class BookingSummaryRequestModel
    {
        public int? ReservationCountToBeFetched { get; set; }
        public bool? IsSummaryOnly { get; set; }
        public DateTime? FromArrivalDate { get; set; }
        public DateTime? ToArrivalDate { get; set; }
        public DateTime? FromDepartureDate { get; set; }
        public DateTime? ToDepartureDate { get; set; }
        public string ReservatioStatus { get; set; }
        public string RoomClass { get; set; }
        public string RoomNumber { get; set; }
    }

    public class FetchBookingRequestModel
    {
        public string ReservationNumber { get; set; }
        public string ReservationNameID { get; set; }
        public string CRSNumber { get; set; }
        public string ExternalRefernceType { get; set; }
    }

    public class FetchBookedPackagesRequestModel
    {
        public string ReservationNumber { get; set; }

    }

    public class OwsResponseModel
    {
        public object responseData { get; set; }
        public bool result { get; set; }
        public string responseMessage { get; set; }
        public int statusCode { get; set; }
    }

    public class OperaReservation
    {
        public string ConfirmationNumber { get; set; } //No Need to process
        public string ReservationNumber { get; set; } //Added
        public string ReservationNameID { get; set; }
        public DateTime? ArrivalDate { get; set; } //Added
        public DateTime? DepartureDate { get; set; } //Added        
        public DateTime? CreatedDateTime { get; set; } //Added
        public int? Adults { get; set; } //Addedd
        public int? Child { get; set; } //Addedd
        public int? Infant { get; set; } //New one
        public string ReservationStatus { get; set; } //New one
        public string ComputedReservationStatus { get; set; } //New one
        public string LegNumber { get; set; }
        public string ChainCode { get; set; }
        public DateTime? ExpectedDepartureTime { get; set; }
        public DateTime? ExpectedArrivalTime { get; set; }
        public string ReservationSourceCode { get; set; }

        public string ReservationType { get; set; }
        public bool? PrintRate { get; set; }
        public bool? NoPost { get; set; }
        public bool? DoNotMoveRoom { get; set; }
        public decimal? TotalAmount { get; set; }
        public bool isInQueue { get; set; }
        public decimal? TotalTax { get; set; }
        public bool IsTaxInclusive { get; set; }
        public decimal CurrentBalance { get; set; }
        public RoomDetails RoomDetails { get; set; }
        public RateDetails RateDetails { get; set; }
        public string PartyCode { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public bool? IsPrimary { get; set; }
        public DateTime? ETA { get; set; }
        public string FlightNo { get; set; }
        public bool? IsCardDetailPresent { get; set; }
        public bool? IsDepositAvailable { get; set; }
        public bool? IsPreCheckedInPMS { get; set; }
        public bool? IsSaavyPaid { get; set; }
        public List<OperaReservation> SharerReservations { get; set; }
        public List<DepositDetail> DepositDetail { get; set; }
        public List<PreferanceDetails> PreferanceDetails { get; set; }
        public List<PackageDetails> PackageDetails { get; set; }
        public List<UserDefinedFields> userDefinedFields { get; set; }
        public List<GuestProfile> GuestProfiles { get; set; }
        public List<Alert> Alerts { get; set; }
        public bool IsMemberShipEnrolled { get; set; }
        public bool? IsBreakFastAvailable { get; set; }
        public ReservationDocument reservationDocument { get; set; }
        public string GuestSignature { get; set; }
        public bool? IsEmailSend { get; set; }
        public string VisitPurposeCode { get; set; }
    }

    public class ReservationDocument
    {
        public string DocumentType { get; set; }
        public string DocumentBase64 { get; set; }
    }

    public class UserDefinedFields
    {
        public string FieldName { get; set; }
        public string FieldValue { get; set; }
    }

    public class RoomDetails
    {
        public string RoomNumber { get; set; }
        public string RoomType { get; set; } //Addedd
        public string RoomTypeDescription { get; set; } //Addedd
        public string RoomTypeShortDescription { get; set; } //Addedd
        public string RoomStatus { get; set; }
        public string RTC { get; set; }
        public string RTCDescription { get; set; }
        public string RTCShortDescription { get; set; }
        public string Floor { get; set; }
        public string FORoomStatus { get; set; }
    }

    public class RateDetails
    {
        public string RateCode { get; set; } //Added
        public decimal? RateAmount { get; set; } //Added

        public List<DailyRates> DailyRates { get; set; }
        public bool IsMultipleRate { get; set; }
    }

    public class DailyRates
    {
        public DateTime PostingDate { get; set; }
        public decimal Amount { get; set; }
        public string description { get; set; }
        public bool IsTaxAmount { get; set; }
    }

    public class PaymentMethod
    {
        public string PaymentType { get; set; } //Added
        public string MaskedCardNumber { get; set; }
        public string ExpiryDate { get; set; }
       public string  AprovalCode { get; set; }
    }

    public class DepositDetail
    {
        public string PaymentType { get; set; }
        public string CreditCardNumber { get; set; }
        public string CardExpiryDate { get; set; }
        public decimal Amount { get; set; }
        public bool? IsCreditCardDeposit { get; set; }
    }
    public class PreferanceDetails
    {
        public string PreferanceCode { get; set; }
        public decimal? PreferanceAmount { get; set; }
    }

    public class PackageDetails
    {
        public string PackageCode { get; set; } // Addedd
        public string PackageDescription { get; set; }
        public decimal? TotalAmount { get; set; } //Addedd
        public decimal? TaxAmount { get; set; } //Addedd
        public decimal? Allowance { get; set; } //Addedd
        public decimal? TotalPackageAmount { get; set; } //Addedd
        public bool isTaxIncluded { get; set; }
        public string CurrecncyCode { get; set; }
        public List<AmountDetails> packageCharges { get; set; }
    }

    public class AmountDetails
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal? UnitAmount { get; set; }
        public decimal? TotalAmount { get; set; }
        public decimal? Tax { get; set; }
        public int Quantity { get; set; }
    }

    public class GuestProfile
    {
        public string PmsProfileID { get; set; }
        public string FamilyName { get; set; }
        public string GivenName { get; set; }
        public string GuestName { get; set; }
        public string Nationality { get; set; }
        public string Gender { get; set; }
        public string PassportNumber { get; set; }
        public string DocumentType { get; set; }
        public bool IsPrimary { get; set; }
        public string MembershipType { get; set; }
        public string MembershipNumber { get; set; }
        public string MembershipID { get; set; }
        public string MembershipName { get; set; }
        public string MembershipClass { get; set; }
        public string MembershipLevel { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public List<Phone> Phones { get; set; }
        public List<Address> Address { get; set; }
        public List<Email> Email { get; set; }
        public string BirthDate { get; set; }
        public string IssueDate { get; set; }
        public string IssueCountry { get; set; }
        public bool IsActive { get; set; }
        public string Title { get; set; }
        public string VipCode { get; set; }
    }

    public class Phone
    {
        public string phoneType { get; set; }
        public string phoneRole { get; set; }
        public long operaId { get; set; }
        public bool? primary { get; set; }
        public int? displaySequence { get; set; }
        public string PhoneNumber { get; set; }

    }

    public class Email
    {
        public string emailType { get; set; } = "EMAIL";
        public long operaId { get; set; }
        public bool? primary { get; set; }
        public int? displaySequence { get; set; }
        public string email { get; set; }

    }

    public class Address
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
    public class Alert
    {
        public string AlertID { get; set; }
        public string AlertCode { get; set; }
        public string Area { get; set; }
        public string Description { get; set; }
        public bool isPrinterNotificationEnabled { get; set; }
        public bool isScreenNotificationEnabled { get; set; }
        public bool isGlobal { get; set; }
    }
    public class EncodeKeyRequest
    {
        public bool isNewKey { get; set; }
        public string EncoderID { get; set; }
        public string PrimaryProfilePMSID { get; set; }
        public string ReservationNameID { get; set; }
    }
    public class OperaReservationModel
    {
        public string ConfirmationNumber { get; set; } //No Need to process
        public string ReservationNumber { get; set; } //Added
        public string ReservationNameID { get; set; }
        public DateTime? ArrivalDate { get; set; } //Added
        public DateTime? DepartureDate { get; set; } //Added        
        public DateTime? CreatedDateTime { get; set; } //Added
        public int? Adults { get; set; } //Addedd
        public int? Child { get; set; } //Addedd        
        public string ReservationStatus { get; set; } //New one
        public string ComputedReservationStatus { get; set; } //New one
        public string LegNumber { get; set; }
        public string ChainCode { get; set; }
        public DateTime? ExpectedDepartureTime { get; set; }
        public DateTime? ExpectedArrivalTime { get; set; }
        public string ReservationSourceCode { get; set; }

        public string ReservationType { get; set; }
        public bool? PrintRate { get; set; }
        public bool? NoPost { get; set; }
        public bool? DoNotMoveRoom { get; set; }
        public decimal? TotalAmount { get; set; }
        public decimal? TotalTax { get; set; }
        public bool IsTaxInclusive { get; set; }
        public decimal CurrentBalance { get; set; }
        public RoomDetails RoomDetails { get; set; }
        public RateDetails RateDetails { get; set; }
        public string PartyCode { get; set; }
        public PaymentMethods PaymentMethod { get; set; }
        public bool? IsPrimary { get; set; }
        public DateTime? ETA { get; set; }
        public string FlightNo { get; set; }
        public bool? IsCardDetailPresent { get; set; }
        public bool? IsDepositAvailable { get; set; }
        public bool? IsPreCheckedInPMS { get; set; }
        public bool? IsSaavyPaid { get; set; }
        public List<OperaReservationModel> SharerReservations { get; set; }
        public List<DepositDetail> DepositDetail { get; set; }
        public List<PreferanceDetails> PreferanceDetails { get; set; }
        public List<PackageDetails> PackageDetails { get; set; }
        public List<UserDefinedFields> userDefinedFields { get; set; }
        public List<GuestProfile> GuestProfiles { get; set; }
        public List<Alert> Alerts { get; set; }
        public bool IsMemberShipEnrolled { get; set; }
        public bool? IsBreakFastAvailable { get; set; }
        public ReservationDocument reservationDocument { get; set; }
        public string GuestSignature { get; set; }
        public bool? IsEmailSend { get; set; }


    }

    public class UpdateProfileAllergyRequest
    {
        public string NameID { get; set; }
        public List<string> Allergies { get; set; }
        public string OtherAllergies { get; set; }
        public string UdfFieldName { get; set; }
    }
}