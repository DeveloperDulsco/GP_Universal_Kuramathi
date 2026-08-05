using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace CheckinPortal.Models.BlinkIdModels.API
{
    public class BlinkIdResponse
    {
        public string executionId { get; set; }
        public DateTime finishTime { get; set; }
        public DateTime startTime { get; set; }
        public BlinkIdResult result { get; set; }
    }

    public class BlinkIdResult
    {
        public DateField dateOfBirth { get; set; }
        public ClassInfo classInfo { get; set; }
        public string type { get; set; }
        public bool isBelowAgeLimit { get; set; }
        public int age { get; set; }
        public string recognitionStatus { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string fullName { get; set; }
        public string address { get; set; }
        public DateField dateOfIssue { get; set; }
        public DateField dateOfExpiry { get; set; }
        public string documentNumber { get; set; }
        public string sex { get; set; }
        public DriverLicenseDetailedInfo driverLicenseDetailedInfo { get; set; }
        public string fullDocumentImageBase64 { get; set; }
        public string faceImageBase64 { get; set; }
        public string additionalNameInformation { get; set; }
        public string additionalAddressInformation { get; set; }
        public string additionalOptionalAddressInformation { get; set; }
        public string placeOfBirth { get; set; }
        public string nationality { get; set; }
        public string race { get; set; }
        public string religion { get; set; }
        public string profession { get; set; }
        public string maritalStatus { get; set; }
        public string residentialStatus { get; set; }
        public string employer { get; set; }
        public string personalIdNumber { get; set; }
        public string documentAdditionalNumber { get; set; }
        public string documentOptionalAdditionalNumber { get; set; }
        public string issuingAuthority { get; set; }
        public MrzData mrzData { get; set; }
        public string conditions { get; set; }
        public string localizedName { get; set; }
        public bool dateOfExpiryPermanent { get; set; }
        public string additionalPersonalIdNumber { get; set; }
        public Viz viz { get; set; }
        public Barcode barcode { get; set; }
        public ImageAnalysisResult imageAnalysisResult { get; set; }
        public string processingStatus { get; set; }
        public string recognitionMode { get; set; }
        public string signatureImageBase64 { get; set; }
        public string fathersName { get; set; }
        public string mothersName { get; set; }
    }

    public class DateField : IDateField
    {
        public int day { get; set; }
        public int month { get; set; }
        public int year { get; set; }
        public bool successfullyParsed { get; set; }
        public string originalString { get; set; }
    }

    public class ClassInfo
    {
        public string country { get; set; }
        public string region { get; set; }
        public string type { get; set; }
        public string countryName { get; set; }
        public string isoAlpha3CountryCode { get; set; }
        public string isoAlpha2CountryCode { get; set; }
        public string isoNumericCountryCode { get; set; }
    }

    public class DriverLicenseDetailedInfo
    {
        public string restrictions { get; set; }
        public string endorsements { get; set; }
        public string vehicleClass { get; set; }
        public string conditions { get; set; }
        public List<object> vehicleClassesInfo { get; set; }
    }

    public class MrzData
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
        public string alienNumber { get; set; }
        public string applicationReceiptNumber { get; set; }
        public string immigrantCaseNumber { get; set; }
        public bool mrzVerified { get; set; }
        public bool mrzParsed { get; set; }
        public DateField dateOfBirth { get; set; }
        public DateField dateOfExpiry { get; set; }
        public string documentType { get; set; }
        public string issuerName { get; set; }
        public string nationalityName { get; set; }
    }

    public class Viz
    {
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string fullName { get; set; }
        public string additionalNameInformation { get; set; }
        public string localizedName { get; set; }
        public string address { get; set; }
        public string additionalAddressInformation { get; set; }
        public string additionalOptionalAddressInformation { get; set; }
        public string placeOfBirth { get; set; }
        public string nationality { get; set; }
        public string race { get; set; }
        public string religion { get; set; }
        public string profession { get; set; }
        public string maritalStatus { get; set; }
        public string residentialStatus { get; set; }
        public string employer { get; set; }
        public string sex { get; set; }
        public DateField dateOfBirth { get; set; }
        public DateField dateOfIssue { get; set; }
        public DateField dateOfExpiry { get; set; }
        public bool dateOfExpiryPermanent { get; set; }
        public string documentNumber { get; set; }
        public string personalIdNumber { get; set; }
        public string documentAdditionalNumber { get; set; }
        public string additionalPersonalIdNumber { get; set; }
        public string documentOptionalAdditionalNumber { get; set; }
        public string issuingAuthority { get; set; }
        public DriverLicenseDetailedInfo driverLicenseDetailedInfo { get; set; }
        public string conditions { get; set; }
        public string fathersName { get; set; }
        public string mothersName { get; set; }
    }

    public class Barcode
    {
        public string rawDataBase64 { get; set; }
        public string stringData { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string middleName { get; set; }
        public string fullName { get; set; }
        public string additionalNameInformation { get; set; }
        public string address { get; set; }
        public string placeOfBirth { get; set; }
        public string nationality { get; set; }
        public string race { get; set; }
        public string religion { get; set; }
        public string profession { get; set; }
        public string maritalStatus { get; set; }
        public string residentialStatus { get; set; }
        public string employer { get; set; }
        public string sex { get; set; }
        public DateField dateOfBirth { get; set; }
        public DateField dateOfIssue { get; set; }
        public DateField dateOfExpiry { get; set; }
        public string documentNumber { get; set; }
        public string personalIdNumber { get; set; }
        public string documentAdditionalNumber { get; set; }
        public string issuingAuthority { get; set; }
        public AddressDetailedInfo addressDetailedInfo { get; set; }
        public DriverLicenseDetailedInfo driverLicenseDetailedInfo { get; set; }
        public List<object> extendedElements { get; set; }
    }

    public class AddressDetailedInfo
    {
        public string street { get; set; }
        public string postalCode { get; set; }
        public string city { get; set; }
        public string jurisdiction { get; set; }
    }

    public class ImageAnalysisResult
    {
        public bool blurred { get; set; }
        public string documentImageColorStatus { get; set; }
        public string documentImageMoireStatus { get; set; }
        public string faceDetectionStatus { get; set; }
        public string mrzDetectionStatus { get; set; }
        public string barcodeDetectionStatus { get; set; }
    }
}


