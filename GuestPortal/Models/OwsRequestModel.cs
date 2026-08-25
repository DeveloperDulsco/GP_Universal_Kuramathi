using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CheckinPortal.Models
{
    public class OWSRequestModel
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
        public BookingSummaryRequestModel BookingSummaryRequest { get; set; }
        public FetchProfileRequest fetchProfileRequest { get; set; }
        public FetchBookingRequestModel FetchBookingRequest { get; set; }
        public FetchBookedPackagesRequestModel FetchBookedPackagesRequest { get; set; }
        public PaymentMethods paymentMethod { get; set; }
        public OperaReservationModel OperaReservation { get; set; }
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
    }
    public class FetchProfileRequest
    {
        public string NameID { get; set; }
    }
    public class FetchBookingRequestModel
    {
        public string ReservationNumber { get; set; }
        public string ReservationNameID { get; set; }
    }
    public class FetchBookedPackagesRequestModel
    {
        public string ReservationNumber { get; set; }

    }
    public class PreregisterReservationRequest
    {
        public string ReservationNameID { get; set; }
        public string ReservationNumber { get; set; }
        public string LegNumber { get; set; }
    }
    public class FetchRoomList
    {
        public List<string> RoomTypes { get; set; }
        public DateTime? DepartureDate { get; set; }
        public string RoomNumber { get; set; }
        public string RoomType { get; set; }

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
    }
    public class ModifyBookingRequest
    {
        public string ReservationNumber { get; set; }
        public string ReservationNameID { get; set; }
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
    public class AssignRoomRequest
    {
        public string ReservationNameID { get; set; }
        public string RoomNumber { get; set; }
        public string StationID { get; set; }
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
        public OperaReservationModel OperaReservation { get; set; }
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
    }

    public class FolioTaxItemsModel
    {
        public string ItemName { get; set; }
        public decimal Amount { get; set; }
        public int? WindowNumber { get; set; }
    }
    public class UDFField
    {
        public string FieldName { get; set; }
        public string FieldValue { get; set; }
    }
    public class OwsResponseModel
    {
        public object responseData { get; set; }
        public bool result { get; set; }
        public string responseMessage { get; set; }
        public int statusCode { get; set; }
    }
}