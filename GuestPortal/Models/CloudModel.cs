using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NLog;

namespace CheckinPortal.Models
{
    public class APIResponseModel
    {
        public object responseData { get; set; }
        public bool result { get; set; }
        public string responseMessage { get; set; }
        public int statusCode { get; set; }
    }
    public class APIRequestModel
    {
        public object RequestObject { get; set; }
        public bool? SyncFromCloud { get; set; }
        public string Identifier { get; set; }

    }
    public class ReservationStatusRequestModel
    {
        public string ReservationID { get; set; }
        public string ReservationNameID { get; set; }
        public string Type { get; set; }
        /// <summary>CSV of ProfileDetailIDs to mark as document-skipped.</summary>
        public string ProfileDetailIDs { get; set; }
        /// <summary>When true, set IsUploadComplete if every profile is uploaded or skipped.</summary>
        public bool FinalizeDocumentStep { get; set; }
    }

    public class CloudReservationModel
    {
        public int ReservationDetailID { get; set; }
        public string ReservationNameID { get; set; }
        public string ReservationNumber { get; set; }
        public Nullable<System.DateTime> ArrivalDate { get; set; }
        public Nullable<System.DateTime> DepartureDate { get; set; }
        public Nullable<int> Adultcount { get; set; }
        public Nullable<int> Childcount { get; set; }
        [Newtonsoft.Json.JsonProperty("InfantCount")]
        public Nullable<int> InfantCount { get; set; }
        public string MembershipNo { get; set; }
        public string MembershipType { get; set; }
        public Nullable<bool> IsDepositAvailable { get; set; }
        public Nullable<bool> IsCardDetailPresent { get; set; }
        public Nullable<bool> IsSaavyPaid { get; set; }
        public Nullable<bool> IsPreCheckedInPMS { get; set; }
        public string RoomType { get; set; }    
        public Nullable<bool> IsPrecheckOutPMS { get; set; }
        public Nullable<bool> IsEcomchekinPaymentStaus { get; set; }
        public Nullable<bool> IsEcomchekOUtPaymentStaus { get; set; }
        public Nullable<bool> IsUploadComplete { get; set; }
        public Nullable<bool> IsDocumentSkipped { get; set; }
        public bool? IsBreakFastAvailable { get; set; }
        public bool? IsMemberShipEnrolled { get; set; }
        public string FlightNo { get; set; }
        public Nullable<System.DateTime> ETA { get; set; }
        public Nullable<decimal> AverageRoomRate { get; set; }
        public Nullable<decimal> TotalTax { get; set; }
        public string RoomTypeDescription { get; set; }
        public string RTCShortDescription { get; set; }
        
        public Nullable<decimal> TotalAmount { get; set; }
        public byte[] FolioDocument { get; set; }
        public Nullable<decimal> PaidAmount { get; set; }
        public Nullable<decimal> BalanceAmount { get; set; }
        public string ReservationSource { get; set; }
        public string EcomPaymentStatus { get; set; }
        public string VisitPurposeCode { get; set; }
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

    public class PaymentMethods
    {
        public string PaymentType { get; set; } //Added
        public string MaskedCardNumber { get; set; }
        public string ExpiryDate { get; set; }
        public string AprovalCode { get; set; }
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

    public class UpdatedInformation
    {
        public Email Email { get; set; }
        public Phone Phone { get; set; }
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
    public  class ReservationPolicyModel
    {
        public string ReservationNameID { get; set; }
        public string RequestType { get; set; }
        public bool ReqStatus { get; set; }
        public int UserID { get; set; }
        public bool IsRoomUpsell { get; set; }
        public string PackageCode { get; set; }
        public string PackageDescription { get; set; }

    }
    public enum ReservationProcessType
    {
        Precheckoutemail,
        PreCheckedInFetched,
        PreCheckedOutFetched,
        Precheckinemail,
        CheckoutFailled,
        CheckedoutSuccessfully,
        GuestFolioEmail,
        PrecheckinSMS,
        PrecheckinCompleted,
        PrecheckoutCompleted
    }
    public class PaymentTypeMasterModel
    {
        public int ID { get; set; }
        public string OperaPaymentTypeCode { get; set; }
        public string VendorPaymentTypeCode { get; set; }
    }
    public class ReservationTrackStatus
    {
        public string ReservationNumber { get; set; }
        public string ReservationNameID { get; set; }
        public string ProcessType { get; set; }
        public string ProcessStatus { get; set; }
        public bool? EmailSent { get; set; }
        public int? ID { get; set; }
    }
    public partial class PolicyMaster
    {
        public bool Ismandatory { get; set; }
        public string PolicyType { get; set; }
        public string PolicyDescription { get; set; }
    }
    public class ReservationDocumentsDataTableModel
    {
        public string ReservationNameID { get; set; }
        public byte[] Document { get; set; }
        public string DocumentType { get; set; }


    }
    public partial class UpsellPackageModel
    {
        public string ReservationNameID { get; set; }
        public string PackageCode { get; set; }
        public string PackageName { get; set; }
        public string PackageDesc { get; set; }
        public string PackageAmount { get; set; }
        public bool? IsRoomUpsell { get; set; }
    }
    public class PaymentAdditionalInfo
    {
        public string TransactionID { get; set; }
        public string KeyHeader { get; set; }
        public string KeyValue { get; set; }
    }
    public class PaymentHeader
    {
        public string TransactionID { get; set; }
        public string ReservationNumber { get; set; }
        public string ReservationNameID { get; set; }
        public string MaskedCardNumber { get; set; }
        public string ExpiryDate { get; set; }
        public string FundingSource { get; set; }
        public string Amount { get; set; }
        public string Currency { get; set; }
        public string RecurringIdentifier { get; set; }
        public string AuthorisationCode { get; set; }
        public string pspReferenceNumber { get; set; }
        public string ParentPspRefereceNumber { get; set; }
        public string TransactionType { get; set; }
        public string ResultCode { get; set; }
        public string ResponseMessage { get; set; }
        public bool? IsActive { get; set; }
        public string StatusType { get; set; }
        public string CardType { get; set; }
        public string OperaPaymentTypeCode { get; set; }

    }

    public class PaymentHistory
    {
        public string TransactionID { get; set; }
        public string ReservationNameID { get; set; }
        public string ReservationNumber { get; set; }
        public string PData { get; set; }
        public string MDData { get; set; }
        public string PaRes { get; set; }
        public string PSPReference { get; set; }
        public string ResultCode { get; set; }
        public string RefusalReason { get; set; }
        public string TransactionType { get; set; }
    }
    public partial class UpdatePaymentDetails
    {
        public List<PaymentHeader> paymentHeaders { get; set; }
        public List<PaymentAdditionalInfo> paymentAdditionalInfos { get; set; }
        public List<PaymentHistory> paymentHistories { get; set; }
    }

    public class Request
    {
       public string Reference { get; set; }    
        public string ArrivalDate { get; set; } 
    }
    public partial class ProfileDocuments
    {
        public string ReservationNameID { get; set; }
        public string ProfileID { get; set; }
        public string DocumentNumber { get; set; }
        public Nullable<System.DateTime> ExpiryDate { get; set; }
        public Nullable<System.DateTime> IssueDate { get; set; }
        public byte[] DocumentImage1 { get; set; }
        public byte[] DocumentImage2 { get; set; }
        public byte[] DocumentImage3 { get; set; }
        public byte[] FaceImage { get; set; }
        public string CloudProfileDetailID { get; set; }
        public string DocumentTypeCode { get; set; }
        public string IssueCountry { get; set; }

    }
    public class ReservationAdditionalDetails
    {
        public string ResID { get; set; }
        public string FieldName { get; set; }
        public string FIeldValue { get; set; }
    }
    public class ProfileDetails
    {
        public string ProfileID { get; set; }
        public string ReservationNameID { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime? BirthDate { get; set; }
        public string Nationality { get; set; }
        public string UpdatedEmail { get; set; }
        public string UpdatedPhone { get; set; }

        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string StateCode { get; set; }
        public string PostalCode { get; set; }
        public string CountryCode { get; set; }

        public string UpdatedAddressLine1 { get; set; }
        public string UpdatedAddressLine2 { get; set; }
        public string UpdatedCity { get; set; }
        public string UpdatedStateCode { get; set; }
        public string UpdatedPostalCode { get; set; }
        public string UpdatedCountryCode { get; set; }
        public string CloudProfileDetailID { get; set; }
        public string Gender { get; set; }

    }
    public class CloudReservationDocument
    {
        public string DocumentType { get; set; }
        //public string DocumentBase64 { get; set; }
        public Byte[] Document { get; set; }
        public string reservationnumber { get; set; }
    }
    public class ReservationGuestRequestModel
    {
        public int ReservationID { get; set; }
        public string ReservationNumber { get; set; }
        public string ReservationNameID { get; set; }
    }
    public class NlogRequest
    {
        public string Level { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }
        public string ActionName { get; set; }
        public string ApplicationName { get; set; }
        public string ActionGroup { get; set; }
        public string ReservationNameID { get; set; }

    }

    /// <summary>
    /// Payload for /local/InsertAuditLog or /audit/InsertAuditLog → Usp_InsertAuditTrailDetails → TbAuditTrailUserDetails.
    /// ReservationNumber maps to TbAuditTrailUserDetails.ReservationID — FO progress stores ReservationDetailID there.
    /// </summary>
    public class PortalAuditLogModel
    {
        public string ApplicationName { get; set; }
        public string ModuleName { get; set; }
        public string ActionName { get; set; }
        public string AuditMessage { get; set; }
        public string UserName { get; set; }
        public string ReservationNumber { get; set; }
        public string DeviceIdentifier { get; set; }
    }
}