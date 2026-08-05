using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CheckinPortal.Models.AdaptorAPIModels;
using Newtonsoft.Json;

namespace CheckinPortal.Models.BlinkIdModels
{
    public class BlinkIdResponseWrapper
    {
        [JsonProperty("executionId")]
        public string ExecutionId { get; set; }

        [JsonProperty("finishTime")]
        public DateTime FinishTime { get; set; }

        [JsonProperty("startTime")]
        public DateTime StartTime { get; set; }

        [JsonProperty("result")]
        public BlinkIdResult Result { get; set; }
    }
    public class BlinkIdResult
    {
        [JsonProperty("dateOfBirth")] public DateField DateOfBirth { get; set; }
        [JsonProperty("classInfo")] public ClassInfo ClassInfo { get; set; }
        [JsonProperty("type")] public string Type { get; set; }
        [JsonProperty("isBelowAgeLimit")] public bool IsBelowAgeLimit { get; set; }
        [JsonProperty("age")] public int? Age { get; set; }
        [JsonProperty("recognitionStatus")] public string RecognitionStatus { get; set; }
        [JsonProperty("firstName")] public string FirstName { get; set; }
        [JsonProperty("lastName")] public string LastName { get; set; }
        [JsonProperty("fullName")] public string FullName { get; set; }
        [JsonProperty("address")] public string Address { get; set; }
        [JsonProperty("dateOfIssue")] public DateField DateOfIssue { get; set; }
        [JsonProperty("dateOfExpiry")] public DateField DateOfExpiry { get; set; }
        [JsonProperty("documentNumber")] public string DocumentNumber { get; set; }
        [JsonProperty("sex")] public string Sex { get; set; }
        [JsonProperty("driverLicenseDetailedInfo")] public DriverLicenseDetailedInfo DriverLicenseDetailedInfo { get; set; }
        [JsonProperty("fullDocumentImageBase64")] public string FullDocumentImageBase64 { get; set; }
        [JsonProperty("faceImageBase64")] public string FaceImageBase64 { get; set; }
        [JsonProperty("additionalNameInformation")] public string AdditionalNameInformation { get; set; }
        [JsonProperty("additionalAddressInformation")] public string AdditionalAddressInformation { get; set; }
        [JsonProperty("additionalOptionalAddressInformation")] public string AdditionalOptionalAddressInformation { get; set; }
        [JsonProperty("placeOfBirth")] public string PlaceOfBirth { get; set; }
        [JsonProperty("nationality")] public string Nationality { get; set; }
        [JsonProperty("race")] public string Race { get; set; }
        [JsonProperty("religion")] public string Religion { get; set; }
        [JsonProperty("profession")] public string Profession { get; set; }
        [JsonProperty("maritalStatus")] public string MaritalStatus { get; set; }
        [JsonProperty("residentialStatus")] public string ResidentialStatus { get; set; }
        [JsonProperty("employer")] public string Employer { get; set; }
        [JsonProperty("personalIdNumber")] public string PersonalIdNumber { get; set; }
        [JsonProperty("documentAdditionalNumber")] public string DocumentAdditionalNumber { get; set; }
        [JsonProperty("documentOptionalAdditionalNumber")] public string DocumentOptionalAdditionalNumber { get; set; }
        [JsonProperty("issuingAuthority")] public string IssuingAuthority { get; set; }
        [JsonProperty("mrzData")] public MrzData MrzData { get; set; }
        [JsonProperty("conditions")] public string Conditions { get; set; }
        [JsonProperty("localizedName")] public string LocalizedName { get; set; }
        [JsonProperty("dateOfExpiryPermanent")] public bool? DateOfExpiryPermanent { get; set; }
        [JsonProperty("additionalPersonalIdNumber")] public string AdditionalPersonalIdNumber { get; set; }
        [JsonProperty("viz")] public Viz Viz { get; set; }
        [JsonProperty("barcode")] public Barcode Barcode { get; set; }
        [JsonProperty("imageAnalysisResult")] public ImageAnalysisResult ImageAnalysisResult { get; set; }
        [JsonProperty("processingStatus")] public string ProcessingStatus { get; set; }
        [JsonProperty("recognitionMode")] public string RecognitionMode { get; set; }
        [JsonProperty("signatureImageBase64")] public string SignatureImageBase64 { get; set; }
        [JsonProperty("fathersName")] public string FathersName { get; set; }
        [JsonProperty("mothersName")] public string MothersName { get; set; }
    }

    //public class DateField
    //{
    //    [JsonProperty("day")] public int? Day { get; set; }
    //    [JsonProperty("month")] public int? Month { get; set; }
    //    [JsonProperty("year")] public int? Year { get; set; }
    //    [JsonProperty("successfullyParsed")] public bool SuccessfullyParsed { get; set; }
    //    [JsonProperty("originalString")] public string OriginalString { get; set; }
    //}

    public class ClassInfo
    {
        [JsonProperty("country")] public string Country { get; set; }
        [JsonProperty("region")] public string Region { get; set; }
        [JsonProperty("type")] public string Type { get; set; }
        [JsonProperty("countryName")] public string CountryName { get; set; }
        [JsonProperty("isoAlpha3CountryCode")] public string IsoAlpha3CountryCode { get; set; }
        [JsonProperty("isoAlpha2CountryCode")] public string IsoAlpha2CountryCode { get; set; }
        [JsonProperty("isoNumericCountryCode")] public string IsoNumericCountryCode { get; set; }
    }

    public class DriverLicenseDetailedInfo
    {
        [JsonProperty("restrictions")] public string Restrictions { get; set; }
        [JsonProperty("endorsements")] public string Endorsements { get; set; }
        [JsonProperty("vehicleClass")] public string VehicleClass { get; set; }
        [JsonProperty("conditions")] public string Conditions { get; set; }
        [JsonProperty("vehicleClassesInfo")] public List<string> VehicleClassesInfo { get; set; } = new List<string>();
    }

    public class MrzData
    {
        [JsonProperty("rawMrzString")] public string RawMrzString { get; set; }
        [JsonProperty("documentCode")] public string DocumentCode { get; set; }
        [JsonProperty("issuer")] public string Issuer { get; set; }
        [JsonProperty("documentNumber")] public string DocumentNumber { get; set; }
        [JsonProperty("opt1")] public string Opt1 { get; set; }
        [JsonProperty("opt2")] public string Opt2 { get; set; }
        [JsonProperty("gender")] public string Gender { get; set; }
        [JsonProperty("nationality")] public string Nationality { get; set; }
        [JsonProperty("primaryId")] public string PrimaryId { get; set; }
        [JsonProperty("secondaryId")] public string SecondaryId { get; set; }
        [JsonProperty("alienNumber")] public string AlienNumber { get; set; }
        [JsonProperty("applicationReceiptNumber")] public string ApplicationReceiptNumber { get; set; }
        [JsonProperty("immigrantCaseNumber")] public string ImmigrantCaseNumber { get; set; }
        [JsonProperty("mrzVerified")] public bool MrzVerified { get; set; }
        [JsonProperty("mrzParsed")] public bool MrzParsed { get; set; }
        [JsonProperty("dateOfBirth")] public DateField DateOfBirth { get; set; }
        [JsonProperty("dateOfExpiry")] public DateField DateOfExpiry { get; set; }
        [JsonProperty("documentType")] public string DocumentType { get; set; }
        [JsonProperty("issuerName")] public string IssuerName { get; set; }
        [JsonProperty("nationalityName")] public string NationalityName { get; set; }
    }

    //public class Viz
    //{
    //    [JsonProperty("firstName")] public string FirstName { get; set; }
    //    [JsonProperty("lastName")] public string LastName { get; set; }
    //    [JsonProperty("fullName")] public string FullName { get; set; }
    //    [JsonProperty("additionalNameInformation")] public string AdditionalNameInformation { get; set; }
    //    [JsonProperty("localizedName")] public string LocalizedName { get; set; }
    //    [JsonProperty("address")] public string Address { get; set; }
    //    [JsonProperty("additionalAddressInformation")] public string AdditionalAddressInformation { get; set; }
    //    [JsonProperty("additionalOptionalAddressInformation")] public string AdditionalOptionalAddressInformation { get; set; }
    //    [JsonProperty("placeOfBirth")] public string PlaceOfBirth { get; set; }
    //    [JsonProperty("nationality")] public string Nationality { get; set; }
    //    [JsonProperty("race")] public string Race { get; set; }
    //    [JsonProperty("religion")] public string Religion { get; set; }
    //    [JsonProperty("profession")] public string Profession { get; set; }
    //    [JsonProperty("maritalStatus")] public string MaritalStatus { get; set; }
    //    [JsonProperty("residentialStatus")] public string ResidentialStatus { get; set; }
    //    [JsonProperty("employer")] public string Employer { get; set; }
    //    [JsonProperty("sex")] public string Sex { get; set; }
    //    [JsonProperty("dateOfBirth")] public DateField DateOfBirth { get; set; }
    //    [JsonProperty("dateOfIssue")] public DateField DateOfIssue { get; set; }
    //    [JsonProperty("dateOfExpiry")] public DateField DateOfExpiry { get; set; }
    //    [JsonProperty("dateOfExpiryPermanent")] public bool? DateOfExpiryPermanent { get; set; }
    //    [JsonProperty("documentNumber")] public string DocumentNumber { get; set; }
    //    [JsonProperty("personalIdNumber")] public string PersonalIdNumber { get; set; }
    //    [JsonProperty("documentAdditionalNumber")] public string DocumentAdditionalNumber { get; set; }
    //    [JsonProperty("additionalPersonalIdNumber")] public string AdditionalPersonalIdNumber { get; set; }
    //    [JsonProperty("documentOptionalAdditionalNumber")] public string DocumentOptionalAdditionalNumber { get; set; }
    //    [JsonProperty("issuingAuthority")] public string IssuingAuthority { get; set; }
    //    [JsonProperty("driverLicenseDetailedInfo")] public DriverLicenseDetailedInfo DriverLicenseDetailedInfo { get; set; }
    //    [JsonProperty("conditions")] public string Conditions { get; set; }
    //    [JsonProperty("fathersName")] public string FathersName { get; set; }
    //    [JsonProperty("mothersName")] public string MothersName { get; set; }
    //}

    public class Barcode
    {
        [JsonProperty("rawDataBase64")] public string RawDataBase64 { get; set; }
        [JsonProperty("stringData")] public string StringData { get; set; }
        [JsonProperty("firstName")] public string FirstName { get; set; }
        [JsonProperty("lastName")] public string LastName { get; set; }
        [JsonProperty("middleName")] public string MiddleName { get; set; }
        [JsonProperty("fullName")] public string FullName { get; set; }
        [JsonProperty("additionalNameInformation")] public string AdditionalNameInformation { get; set; }
        [JsonProperty("address")] public string Address { get; set; }
        [JsonProperty("placeOfBirth")] public string PlaceOfBirth { get; set; }
        [JsonProperty("nationality")] public string Nationality { get; set; }
        [JsonProperty("race")] public string Race { get; set; }
        [JsonProperty("religion")] public string Religion { get; set; }
        [JsonProperty("profession")] public string Profession { get; set; }
        [JsonProperty("maritalStatus")] public string MaritalStatus { get; set; }
        [JsonProperty("residentialStatus")] public string ResidentialStatus { get; set; }
        [JsonProperty("employer")] public string Employer { get; set; }
        [JsonProperty("sex")] public string Sex { get; set; }
        [JsonProperty("dateOfBirth")] public DateField DateOfBirth { get; set; }
        [JsonProperty("dateOfIssue")] public DateField DateOfIssue { get; set; }
        [JsonProperty("dateOfExpiry")] public DateField DateOfExpiry { get; set; }
        [JsonProperty("documentNumber")] public string DocumentNumber { get; set; }
        [JsonProperty("personalIdNumber")] public string PersonalIdNumber { get; set; }
        [JsonProperty("documentAdditionalNumber")] public string DocumentAdditionalNumber { get; set; }
        [JsonProperty("issuingAuthority")] public string IssuingAuthority { get; set; }
        [JsonProperty("addressDetailedInfo")] public AddressDetailedInfo AddressDetailedInfo { get; set; }
        [JsonProperty("driverLicenseDetailedInfo")] public DriverLicenseDetailedInfo DriverLicenseDetailedInfo { get; set; }
        [JsonProperty("extendedElements")] public List<object> ExtendedElements { get; set; } = new List<object>();
    }

    public class AddressDetailedInfo
    {
        [JsonProperty("street")] public string Street { get; set; }
        [JsonProperty("postalCode")] public string PostalCode { get; set; }
        [JsonProperty("city")] public string City { get; set; }
        [JsonProperty("jurisdiction")] public string Jurisdiction { get; set; }
    }

    public class ImageAnalysisResult
    {
        [JsonProperty("blurred")] public bool Blurred { get; set; }
        [JsonProperty("documentImageColorStatus")] public string DocumentImageColorStatus { get; set; }
        [JsonProperty("documentImageMoireStatus")] public string DocumentImageMoireStatus { get; set; }
        [JsonProperty("faceDetectionStatus")] public string FaceDetectionStatus { get; set; }
        [JsonProperty("mrzDetectionStatus")] public string MrzDetectionStatus { get; set; }
        [JsonProperty("barcodeDetectionStatus")] public string BarcodeDetectionStatus { get; set; }
    }

    public class BlinkIdDocumentResponseModel
    {
        public string idType { get; set; }
        public string lastName { get; set; }
        public string middleName { get; set; }
        public string firstName { get; set; }
        public string fullName { get; set; }
        public string gender { get; set; }
        public string dateOfBirth { get; set; }
        public string issueDate { get; set; }
        public string expiryDate { get; set; }
        public string address1 { get; set; }
        public string address2 { get; set; }
        public string state { get; set; }
        public string city { get; set; }
        public string zip { get; set; }
        public string issueCountry { get; set; }
        public string issueCountryCode { get; set; }
        public string issueCountry_fullname { get; set; }
        public string nationality { get; set; }
        public string nationality_fullname { get; set; }
        public string documentNumber { get; set; }
        public string personalNumber { get; set; }
        public string fullImage { get; set; }
        public string faceImage { get; set; }
        public byte[] bfullImage { get; set; }
        public byte[] bfaceImage { get; set; }
        public bool IsExpired { get; set; }
        public int age { get; set; }
        public int ReservationID { get; set; }
        public int ProfileDetailID { get; set; }
        public string backImage { get; set; }
        public string ProfileID { get; set; }

    }
    public class ReadMBDocumentResponseModel
    {
        public bool result { get; set; }
        public string firstName { get; set; }
        public string middleName { get; set; }
        public string lastName { get; set; }
        public string gender { get; set; }
        public string dateOfBirth { get; set; }
        public string address1 { get; set; }
        public string address2 { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string zip { get; set; }
        public string issueCountry { get; set; }
        public string issueCountry_code2 { get; set; }
        public string issueCountry_fullname { get; set; }
        public string nationality { get; set; }
        public string nationality_code2 { get; set; }
        public string nationality_fullname { get; set; }
        public string documentNumber { get; set; }
        public string personalNumber { get; set; }
        public string issueDate { get; set; }
        public string expiryDate { get; set; }
        public string optionalData1 { get; set; }
        public string optionalData2 { get; set; }
        public string personalEyeColor { get; set; }
        public string personalHeight { get; set; }
        public string issuingPlace { get; set; }
        public string placeOfBirth { get; set; }
        public string countryOfBirth { get; set; }
        public string idType { get; set; }
        public string errorMessage { get; set; }
        public string fullImage { get; set; }
        public string faceImage { get; set; }
        public string errorCode { get; set; }
        public string cardNumber { get; set; }
        public string visaNumber { get; set; }
        public string arabicName { get; set; }
        public string mobileNumber { get; set; }
        public string fathersName { get; set; }
        public string mothersName { get; set; }
        public string registeredCity { get; set; }
        public string registeredTown { get; set; }
        public string fullName { get; set; }
        public string arabicDocumentNumber { get; set; }

        public string arabicNationality { get; set; }
        public string idCardType { get; set; }
        //public string martialStatus { get; set; }
        public string occupation { get; set; }

        public string CompanyNameArabic { get; set; }
        public string FieldofStudyArabic { get; set; }
        public string FieldofStudyEnglish { get; set; }
        public string PassportIssueCountryDescriptionArabic { get; set; }
        public string QualificationLevelDescriptionArabic { get; set; }
        public string CompanyNameEnglish { get; set; }
        public string OccupationField { get; set; }
        public string PassportIssueCountry { get; set; }
        public string PassportIssueCountryDescriptionEnglish { get; set; }
        public string PassportExpiryDate { get; set; }
        public string PassportIssueDate { get; set; }
        public string PassportNumber { get; set; }
        public string PassportType { get; set; }
        public string QualificationLevel { get; set; }
        public string QualificationLevelDescriptionEnglish { get; set; }
        public string ResidencyExpiryDate { get; set; }
        public string ResidencyNumber { get; set; }
        public string ResidencyType { get; set; }
        public string SponsorNumber { get; set; }
        public string SponsorType { get; set; }
        public string HomeAreaCode { get; set; }
        public string HomeAddressTypeCode { get; set; }
        public string HomeAreaDescriptionArabic { get; set; }
        public string HomeAreaDescriptionEnglish { get; set; }
        public string HomeCityCode { get; set; }
        public string HomeCityDescriptionArabic { get; set; }
        public string HomeCityDescriptionEnglish { get; set; }
        public string EmirateCode { get; set; }
        public string EmirateDescriptionArabic { get; set; }
        public string POBox { get; set; }
        public string HomeStreetArabic { get; set; }
        public string HomeStreetEnglish { get; set; }
        public string EmirateDescriptionEnglish { get; set; }
        public string spouseName { get; set; }
        public string martialStatus = "SINGLE";

        public string documentTypeAbrevation { get; set; }
        public string genderAbrevation { get; set; }


        //public string error_code { get; set; }
        //public string error_message { get; set; }

        public string DocumentResult { get; set; }
        public string fullImageIR { get; set; }
        public string[] AuthenticationAlerts { get; set; }
        public string fullImageUV { get; set; }
        public bool IsExpired { get; set; }
        public int age { get; set; }
        public string TransactionID { get; set; }
    }
    public class ReadDocumentMBResponseModel
    {
        public bool Result { get; set; }
        public string ResponseMessage { get; set; }
        public ReadDocumentModel responseData { get; set; }
    }
    // Scan document
    //public class Root
    //{
    //    public string mode { get; set; }
    //    public DocumentClassInfo documentClassInfo { get; set; }
    //    public FullName fullName { get; set; }
    //    public DocumentNumber documentNumber { get; set; }
    //    public DateOfBirth dateOfBirth { get; set; }
    //    public DateOfIssue dateOfIssue { get; set; }
    //    public List<SubResult> subResults { get; set; }
    //}

    public class DocumentClassInfo
    {
        public string country { get; set; }
        public string type { get; set; }
        public string countryName { get; set; }
        public string isoNumericCountryCode { get; set; }
        public string isoAlpha2CountryCode { get; set; }
        public string isoAlpha3CountryCode { get; set; }
    }

    public class FullName
    {
        public LanguageValue latin { get; set; }
        public LanguageValue arabic { get; set; }
        public LanguageValue cyrillic { get; set; }
        public LanguageValue greek { get; set; }
    }

    public class DocumentNumber
    {
        public LanguageValue latin { get; set; }
        public LanguageValue arabic { get; set; }
        public LanguageValue cyrillic { get; set; }
        public LanguageValue greek { get; set; }
    }

    public class LanguageValue
    {
        public string value { get; set; }
    }

    public class DateOfBirth
    {
        public int day { get; set; }
        public int month { get; set; }
        public int year { get; set; }
        public OriginalString originalString { get; set; }
        public bool filledByDomainKnowledge { get; set; }
        public bool successfullyParsed { get; set; }
    }

    public class DateOfIssue
    {
        public int day { get; set; }
        public int month { get; set; }
        public int year { get; set; }
        public OriginalString originalString { get; set; }
        public bool filledByDomainKnowledge { get; set; }
        public bool successfullyParsed { get; set; }
    }

    public class OriginalString
    {
        public LanguageValue latin { get; set; }
        public LanguageValue arabic { get; set; }
        public LanguageValue cyrillic { get; set; }
        public LanguageValue greek { get; set; }
    }

    public class Root
    {
        public string mode { get; set; }
        public DocumentClassInfo documentClassInfo { get; set; }
        public DataMatchResult dataMatchResult { get; set; }
        public MultiLangValue firstName { get; set; }
        public MultiLangValue lastName { get; set; }
        public MultiLangValue placeOfBirth { get; set; }
        public MultiLangValue nationality { get; set; }
        public MultiLangValue sex { get; set; }
        public MultiLangValue documentNumber { get; set; }
        public MultiLangValue issuingAuthority { get; set; }
        public DateField dateOfBirth { get; set; }
        public DateField dateOfIssue { get; set; }
        public DateField dateOfExpiry { get; set; }
        public bool dateOfExpiryPermanent { get; set; }
        public List<SubResult> subResults { get; set; }
    }

 

    public class DataMatchResult
    {
        public List<StatePerField> statePerField { get; set; }
        public string overallState { get; set; }
    }

    public class StatePerField
    {
        public string fieldType { get; set; }
        public string state { get; set; }
    }

    public class MultiLangValue
    {
        public LangValue latin { get; set; }
        public LangValue arabic { get; set; }
        public LangValue cyrillic { get; set; }
        public LangValue greek { get; set; }
    }

    public class LangValue
    {
        public string value { get; set; }
    }
    public interface IDateField
    {
        int day { get; }
        int month { get; }
        int year { get; }
        bool successfullyParsed { get; }
    }
    public class DateField : IDateField
    {
        public int day { get; set; }
        public int month { get; set; }
        public int year { get; set; }
        public OriginalString originalString { get; set; }
        public bool filledByDomainKnowledge { get; set; }
        public bool successfullyParsed { get; set; }
    }

   

    public class SubResult
    {
        public Viz viz { get; set; }
        public Mrz mrz { get; set; }
        public DocumentImage documentImage { get; set; }
        public FaceImage faceImage { get; set; }
    }
    //public class DocumentImage
    //{
    //    public Dictionary<string, int> data { get; set; }
    //}
    public class DocumentImage
    {
        //public Dictionary<string, int> data { get; set; }
        public int[] data { get; set; }     // MUST be byte[]
        public int width { get; set; }
        public int height { get; set; }
        public string pixelFormat { get; set; }
        public string colorSpace { get; set; }
    }
    // Face Image Model
    public class FaceImage
    {
        //public FaceInner image { get; set; }
        public int[] data { get; set; }     // MUST be byte[]
        public int width { get; set; }
        public int height { get; set; }
        public string pixelFormat { get; set; }
        public string colorSpace { get; set; }
        public string side { get; set; }
    }

    public class FaceInner
    {
        public Dictionary<string, int> data { get; set; }
    }
    //public class Viz
    //{
    //    public MultiLangValue firstName { get; set; }
    //    public MultiLangValue lastName { get; set; }
    //    public MultiLangValue placeOfBirth { get; set; }
    //    public MultiLangValue nationality { get; set; }
    //    public MultiLangValue sex { get; set; }
    //    public DateField dateOfBirth { get; set; }
    //    public DateField dateOfIssue { get; set; }
    //    public DateField dateOfExpiry { get; set; }
    //    public bool dateOfExpiryPermanent { get; set; }
    //    public MultiLangValue documentNumber { get; set; }
    //    public MultiLangValue issuingAuthority { get; set; }
    //}
    public class Viz
    {
        // --- Multilingual fields (used in Root/subResults JSON) ---
        [JsonProperty("firstName")]
        public MultiLangValue FirstName { get; set; }

        [JsonProperty("lastName")]
        public MultiLangValue LastName { get; set; }

        [JsonProperty("fullName")]
        public MultiLangValue FullName { get; set; }

        [JsonProperty("placeOfBirth")]
        public MultiLangValue PlaceOfBirth { get; set; }

        [JsonProperty("nationality")]
        public MultiLangValue Nationality { get; set; }

        [JsonProperty("sex")]
        public MultiLangValue Sex { get; set; }

        [JsonProperty("documentNumber")]
        public MultiLangValue DocumentNumber { get; set; }

        [JsonProperty("issuingAuthority")]
        public MultiLangValue IssuingAuthority { get; set; }

        // --- Common date fields ---
        [JsonProperty("dateOfBirth")]
        public DateField DateOfBirth { get; set; }

        [JsonProperty("dateOfIssue")]
        public DateField DateOfIssue { get; set; }

        [JsonProperty("dateOfExpiry")]
        public DateField DateOfExpiry { get; set; }

        [JsonProperty("dateOfExpiryPermanent")]
        public bool? DateOfExpiryPermanent { get; set; }

        // --- Additional optional metadata ---
        [JsonProperty("personalIdNumber")]
        public MultiLangValue PersonalIdNumber { get; set; }
        [JsonIgnore]
        public string PersonalIdNumberValue => PersonalIdNumber?.latin?.value ?? "";
        [JsonProperty("documentAdditionalNumber")]
        public string DocumentAdditionalNumber { get; set; }

        [JsonProperty("documentOptionalAdditionalNumber")]
        public string DocumentOptionalAdditionalNumber { get; set; }

        [JsonProperty("fathersName")]
        public string FathersName { get; set; }

        [JsonProperty("mothersName")]
        public string MothersName { get; set; }
        [JsonProperty("address")] 
        public string Address { get; set; }
        [JsonProperty("additionalAddressInformation")]
        public string AdditionalAddressInformation { get; set; }
        [JsonProperty("residentialStatus")] 
        public string ResidentialStatus { get; set; }

        // --- Helper properties (flattened access for convenience) ---
        [JsonIgnore]
        public string FirstNameValue => FirstName?.latin?.value ?? "";
        [JsonIgnore]
        public string LastNameValue => LastName?.latin?.value ?? "";
        [JsonIgnore]
        public string FullNameValue => FullName?.latin?.value ?? "";
        [JsonIgnore]
        public string DocumentNumberValue => DocumentNumber?.latin?.value ?? "";
        [JsonIgnore]
        public string NationalityValue => Nationality?.latin?.value ?? "";
        [JsonIgnore]
        public string PlaceOfBirthValue => PlaceOfBirth?.latin?.value ?? "";
    }
    public class Mrz
    {
        public string rawMrzString { get; set; }
        public string documentCode { get; set; }
        public string issuer { get; set; }
        public string documentNumber { get; set; }
        public string opt1 { get; set; }
        public string opt2 { get; set; }
        public string gender { get; set; }
        public string nationality { get; set; }
        public string primaryId { get; set; }
        public string secondaryId { get; set; }
        public string issuerName { get; set; }
        public string nationalityName { get; set; }
        public bool verified { get; set; }
        public MrzDate dateOfBirth { get; set; }
        public MrzDate dateOfExpiry { get; set; }
        public string documentType { get; set; }
        public string sanitizedOpt1 { get; set; }
        public string sanitizedOpt2 { get; set; }
        public string sanitizedNationality { get; set; }
        public string sanitizedIssuer { get; set; }
        public string sanitizedDocumentCode { get; set; }
        public string sanitizedDocumentNumber { get; set; }
    }

    public class MrzDate
    {
        public int day { get; set; }
        public int month { get; set; }
        public int year { get; set; }
        public string originalString { get; set; }
        public bool filledByDomainKnowledge { get; set; }
        public bool successfullyParsed { get; set; }
    }
    public class BlinkResponseModel
    {
        public object responseData { get; set; }
        public bool result { get; set; }
        public string responseMessage { get; set; }
        public int statusCode { get; set; }

    }
    public class BlinkDocumentResponseModel
    {
        public bool Result { get; set; }
        public string ResponseMessage { get; set; }
        public MBlinkDocumentResponseModel responseData { get; set; }
    }

    public class MBlinkDocumentResponseModel
    {
        public string idType { get; set; }
        public string lastName { get; set; }
        public string middleName { get; set; }
        public string firstName { get; set; }
        public string fullName { get; set; }
        public string gender { get; set; }
        public string dateOfBirth { get; set; }
        public string issueDate { get; set; }
        public string expiryDate { get; set; }
        public string address1 { get; set; }
        public string address2 { get; set; }
        public string state { get; set; }
        public string city { get; set; }
        public string zip { get; set; }
        public string issueCountry { get; set; }
        public string issueCountryCode { get; set; }
        public string issueCountry_fullname { get; set; }
        public string nationality { get; set; }
        public string nationality_fullname { get; set; }
        public string documentNumber { get; set; }
        public string personalNumber { get; set; }
        public string fullImage { get; set; }
        public string faceImage { get; set; }
        public byte[] bfullImage { get; set; }
        public byte[] bfaceImage { get; set; }
        public bool IsExpired { get; set; }
        public int age { get; set; }
    }

}


