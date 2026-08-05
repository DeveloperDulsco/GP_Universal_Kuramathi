using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CheckinPortal.Models.AdaptorAPIModels
{
    public class AdaptorAPIResponseModel
    {
        public bool Result { get; set; }
        public string ResponseMessage { get; set; }
        public object ResponseObject { get; set; }
    }

    public class CostEstimatorAPIResponseModel
    {
        public bool Result { get; set; }
        public string ResponseMessage { get; set; }
        public CostEstimator ResponseObject { get; set; }

        public string FundingSource { get; set; }

        public string PaymentMethod { get; set; }

        public string TransactionType
        {
            get
            {

                return FundingSource == "CREDIT" ? Models.TransactionType.PreAuth.ToString() : Models.TransactionType.Sale.ToString();
            }
            set
            {
                TransactionType = value;
            }
        }
    }




    public partial class CostEstimator
    {
        [JsonProperty("cardBin")]
        public CardBin CardBin { get; set; }

        [JsonProperty("costEstimateAmount")]
        public CostEstimateAmount CostEstimateAmount { get; set; }

        [JsonProperty("resultCode")]
        public string ResultCode { get; set; }

        [JsonProperty("surchargeType")]
        public string SurchargeType { get; set; }
    }

    public partial class CardBin
    {
        [JsonProperty("bin")]
        public string Bin { get; set; }

        [JsonProperty("commercial")]
        public object Commercial { get; set; }

        [JsonProperty("fundingSource")]
        public string FundingSource { get; set; }

        [JsonProperty("fundsAvailability")]
        public object FundsAvailability { get; set; }

        [JsonProperty("issuingBank")]
        public object IssuingBank { get; set; }

        [JsonProperty("issuingCountry")]
        public string IssuingCountry { get; set; }

        [JsonProperty("issuingCurrency")]
        public object IssuingCurrency { get; set; }

        [JsonProperty("paymentMethod")]
        public string PaymentMethod { get; set; }

        [JsonProperty("payoutEligible")]
        public object PayoutEligible { get; set; }

        [JsonProperty("summary")]
        public string Summary { get; set; }
    }

    public partial class CostEstimateAmount
    {
        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("value")]
        public long Value { get; set; }
    }




    public class MakePaymentResponseModel
    {
        public bool Result { get; set; }
        public string ResponseMessage { get; set; }
        public PaymentResponse ResponseObject { get; set; }
    }

    public class PaymentResponse
    {
        public string CardToken { get; set; }

        
        public string RefusalReason { get; set; }
        public string CardExpiryDate { get; set; }
        public string PaymentToken { get; set; }
        public string MerchantRefernce { get; set; }
        public string AuthCode { get; set; }
        public string CardType { get; set; }
        public string FundingSource { get; set; }
        public string PspReference { get; set; }
        public string ResultCode { get; set; }
        public List<AdditionalInfo> additionalInfos { get; set; }

        public string MaskCardNumber { get; set; }
        public string Currency { get; set; }
        public decimal? Amount { get; set; }

        public string ParentPSPReferece { get; set; }
    }

    public class AdditionalInfo
    {
        public string key { get; set; }
        public string value { get; set; }
    }


    public class ReadDocumentResponseModel
    {
        public bool Result { get; set; }
        public string ResponseMessage { get; set; }
        public ReadDocumentModel responseData { get; set; }
    }




    public class RegulaAPIResponseModel
    {
        public bool result { get; set; }
        public string rresponseMessage { get; set; }
        public object responseData { get; set; }
        public string statusCode { get; set; }
    }


    public class RegulaDocumentTypeResponseModel
    {
        public string documentTypeDescription { get; set; }
        public string documentTypeCode { get; set; }
        public string issueCountryCode { get; set; }
    }



    public class GetDocumentImageResponseData
    {
        public string Base64ImageString { get; set; }
        public string Format { get; set; }

        public string LightIndex { get; set; }

        public string PageIndex { get; set; }
    }

    public class ReadDocumentModel
    {
        public string TransactionID { get; set; }
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
    }

   
}