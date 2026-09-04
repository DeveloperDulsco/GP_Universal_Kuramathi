using CheckinPortal.BusinessLayer;
using CheckinPortal.DataAccess;

using CheckinPortal.Helpers;

using CheckinPortal.Models;

using CheckinPortal.Models.AdaptorAPIModels;

using CheckinPortal.Models.BlinkIdModels;

using CheckinPortal.Models.PaymentDetails;
using Microsoft.Reporting.WebForms;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;
using WebGrease.Configuration;

namespace CheckinPortal.Controllers 
{
    public class PortalServiceController : ApiController
    {
        
        public async Task<IHttpActionResult> GetStateByCountryID(int CountryID)
        {
            BusinessLayer.MastersLogics mastersLogics = new BusinessLayer.MastersLogics();

            var states = await mastersLogics.GetStateListByCountryID(CountryID);

            if (states == null)
                return Ok(new List<Models.StateMaster>());

            var sortedStates = states
                .OrderBy(s => s.Statename ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return Ok(sortedStates);
        }
        [HttpPost]
        [ActionName("GetPaymentmethods")]
        [Route("api/portalservice/GetPaymentmethods")]
        public async Task<string> GetPaymentmethods()
        {
            HttpClient httpClient = new HttpClient();

            string BaseURL = ConfigurationManager.AppSettings["AdaptorAPIPaymentBaseURL"].ToString();
            string APIKey = ConfigurationManager.AppSettings["APIKey"].ToString();
            string MerchantAccount = ConfigurationManager.AppSettings["MerchantAccount"].ToString();

            Models.AdaptorAPIModels.GetpaymentMethodsRequestModel getpaymentMethodsRequestModel = new Models.AdaptorAPIModels.GetpaymentMethodsRequestModel()
            {
                apiKey = APIKey,
                MerchantAccount = MerchantAccount
            };

            httpClient.BaseAddress = new Uri(BaseURL);

            string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(getpaymentMethodsRequestModel);// "{\"merchantAccount\":\"" + merchantAccount + "\"}";
            var accessToken = AuthenticationHelper.GetAPIAccessToken();
            if (!string.IsNullOrEmpty(accessToken))
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
            HttpContent requestContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await httpClient.PostAsync($"GetPaymentmethods", requestContent);

            if (response != null)
            {
                if (response.IsSuccessStatusCode)
                {
                    string responsestr = await response.Content.ReadAsStringAsync();
                    var Adyenresponse = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.AdaptorAPIModels.AdaptorAPIResponseModel>(responsestr);
                    return Adyenresponse.ResponseObject.ToString();
                }
            }
            return "";
        }
    
        [HttpPost]
        [ActionName("orginkey")]
        [Route("api/portalservice/orginkey")]
        public async Task<string> orginkey(string domainName)
        {
            HttpClient httpClient = new HttpClient();
            string BaseURL = ConfigurationManager.AppSettings["AdaptorAPIPaymentBaseURL"].ToString();
            string APIKey = ConfigurationManager.AppSettings["APIKey"].ToString();
            string MerchantAccount = ConfigurationManager.AppSettings["MerchantAccount"].ToString();

            httpClient.BaseAddress = new Uri(BaseURL);

            Models.AdaptorAPIModels.GetOrginKeysRequestModel getOrginKeysRequestModel = new Models.AdaptorAPIModels.GetOrginKeysRequestModel()
            {
                apiKey = APIKey,
                MerchantAccount = MerchantAccount,
                RequestObject = new List<string>()  { domainName }
            };


            string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(getOrginKeysRequestModel);// "{\"originDomains\": [\"" + domainName + "\"]}";

            //httpClient.DefaultRequestHeaders.Clear();
            //httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            //httpClient.DefaultRequestHeaders.Add("x-api-key", "AQE1hmfuXNWTK0Qc+iSDk2UuvsaOW4JDCIBZa3xF0n2mjVZdiutiFFJB8m+HZPXmoKVywMgI/xQQwV1bDb7kfNy1WIxIIkxgBw==-rODJO2F2/g0t6SNBtX135za8qsAPMapU1bIGmWrLDP8=-:=GN%5%nV5Tpj*=W");
            var accessToken = AuthenticationHelper.GetAPIAccessToken();
            if (!string.IsNullOrEmpty(accessToken))
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
            HttpContent requestContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await httpClient.PostAsync($"GetOrginKey", requestContent);

            if (response != null)
            {
                if (response.IsSuccessStatusCode)
                {
                    string responsestr = await response.Content.ReadAsStringAsync();
                    var Adyenresponse = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.AdaptorAPIModels.AdaptorAPIResponseModel>(responsestr);

                    return Adyenresponse.ResponseObject.ToString();

                }
                //{
                //    //"originKeys": {
                //    //    "https:\/\/localhost:44356\/": "pub.v2.8015887802321585.aHR0cHM6Ly9sb2NhbGhvc3Q6NDQzNTY.klg4CMjQaabQJWhz4xzTFnuM-Y8x9f8jftr1015wnvw"
                //    //}
                //}
            }
            return "";
        }




        [HttpPost]
        [ActionName("getCostEstimater")]
        [Route("api/portalservice/GetCostEstimater")]
        public async Task<CostEstimatorAPIResponseModel> GetCostEstimater(CostEstimatorObject model)
        {
            HttpClient httpClient = new HttpClient();

            try
            {
                string BaseURL = ConfigurationManager.AppSettings["AdaptorAPIPaymentBaseURL"].ToString();
                string APIKey = ConfigurationManager.AppSettings["APIKey"].ToString();
                string MerchantAccount = ConfigurationManager.AppSettings["MerchantAccount"].ToString();

                model.Mcc = Convert.ToInt32(ConfigurationManager.AppSettings["CostEstimator_MCC"].ToString());

                Models.AdaptorAPIModels.CostEstimatorRequest costEstimatorRequest = new Models.AdaptorAPIModels.CostEstimatorRequest()
                {
                    apiKey = APIKey,
                    MerchantAccount = MerchantAccount,
                    RequestObject = model
                };

                httpClient.BaseAddress = new Uri(BaseURL);

                string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(costEstimatorRequest);// "{\"merchantAccount\":\"" + merchantAccount + "\"}";
                var accessToken = AuthenticationHelper.GetAPIAccessToken();
                if (!string.IsNullOrEmpty(accessToken))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }
                HttpContent requestContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync($"GetCostEstimater", requestContent);

                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string responsestr = await response.Content.ReadAsStringAsync();
                        var ResponseObj = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.AdaptorAPIModels.CostEstimatorAPIResponseModel>(responsestr);


                        if(ResponseObj != null && ResponseObj.ResponseObject != null && ResponseObj.ResponseObject.CardBin != null)
                        {
                            ResponseObj.FundingSource = ResponseObj.ResponseObject.CardBin.FundingSource;
                            ResponseObj.PaymentMethod = ResponseObj.ResponseObject.CardBin.PaymentMethod;
                        }

                        return ResponseObj;
                    }
                    else
                    {
                        return new CostEstimatorAPIResponseModel()
                        {
                            Result = false,
                            ResponseMessage = response.ReasonPhrase
                        };
                    }
                }
                return new CostEstimatorAPIResponseModel()
                {
                    Result = false,
                    ResponseMessage = "Unable to get cost estimator"
                };
            }
            catch (Exception ex)
            {
                return new CostEstimatorAPIResponseModel()
                {
                    Result = false,
                    ResponseMessage = "Unable to get cost estimator"
                };
            }
        }


        [HttpPost]
        [ActionName("savePaymentDetails")]
        [Route("api/portalservice/savePaymentDetails")]
        public async Task<string> savePaymentDetails(PaymentDetailsModel paymentDetailsModel)
        {


            return "";
        }



        [HttpPost]
        [ActionName("makePaymentDetails")]
        [Route("api/portalservice/makePaymentDetails")]
        public async Task<string> makePaymentDetails(MakePaymentDetailRequestModel paymentDetailsModel)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri("https://checkout-test.adyen.com");

                string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(paymentDetailsModel);

                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Add("x-api-key", "AQE1hmfuXNWTK0Qc+iSDk2UuvsaOW4JDCIBZa3xF0n2mjVZdiutiFFJB8m+HZPXmoKVywMgI/xQQwV1bDb7kfNy1WIxIIkxgBw==-rODJO2F2/g0t6SNBtX135za8qsAPMapU1bIGmWrLDP8=-:=GN%5%nV5Tpj*=W");

                HttpContent requestContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync($"/v52/payments/details", requestContent);

                if (response.IsSuccessStatusCode)
                {
                    string Test = response.Content.ReadAsStringAsync().Result;
                    return await response.Content.ReadAsStringAsync();
                }

            }

            return "";
        }



        [HttpPost]
        [ActionName("SendEmail")]
        [Route("api/portalservice/SendEmail")]
        public IHttpActionResult SendEmail(SendEmailModel sendEmail)
        {
            const string actionName = "SendEmail";
            const string actionGroup = "Pre-Checkout";

            try
            {
                // Accept both JSON and multipart form (legacy JS used email / reservationId)
                string emailID = sendEmail?.emailID;
                string reservationID = sendEmail?.reservationID;
                if (string.IsNullOrWhiteSpace(emailID) || string.IsNullOrWhiteSpace(reservationID))
                {
                    var form = HttpContext.Current?.Request?.Form;
                    if (form != null)
                    {
                        if (string.IsNullOrWhiteSpace(emailID))
                            emailID = form["emailID"] ?? form["email"];
                        if (string.IsNullOrWhiteSpace(reservationID))
                            reservationID = form["reservationID"] ?? form["reservationId"] ?? form["ReservationID"];
                    }
                }

                emailID = (emailID ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(emailID))
                {
                    return Ok(new { result = false, responseMessage = "Please enter email address" });
                }
                if (!IsValidEmailAddress(emailID))
                {
                    return Ok(new { result = false, responseMessage = "Please enter a valid email address" });
                }

                if (string.IsNullOrWhiteSpace(SessionData.FolioBase64))
                {
                    new LogHelper().Warn("Cannot send guest folio email — FolioBase64 is empty", reservationID ?? "", actionName, actionGroup);
                    return Ok(new { result = false, responseMessage = "Invoice is not available to email. Please contact the front desk." });
                }

                if (SessionData.OperaReservation == null)
                {
                    new LogHelper().Warn("Cannot send guest folio email — OperaReservation session is empty", reservationID ?? "", actionName, actionGroup);
                    return Ok(new { result = false, responseMessage = "Session expired. Please reopen your checkout link and try again." });
                }

                var profile = (SessionData.OperaReservation.GuestProfiles != null && SessionData.OperaReservation.GuestProfiles.Count > 0)
                    ? SessionData.OperaReservation.GuestProfiles[0]
                    : null;
                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                string guestName = "";
                if (profile != null)
                {
                    guestName = ""
                        + (!string.IsNullOrEmpty(profile.FirstName) ? textInfo.ToTitleCase(profile.FirstName) + " " : "")
                        + (!string.IsNullOrEmpty(profile.MiddleName) ? textInfo.ToTitleCase(profile.MiddleName) + " " : "")
                        + (!string.IsNullOrEmpty(profile.LastName) ? textInfo.ToTitleCase(profile.LastName) : "");
                }

                string reservationNameID = SessionData.OperaReservation.ReservationNameID ?? reservationID ?? "";
                string apiBaseUrl = ConfigurationManager.AppSettings["APIBaseUrl"]?.ToString()
                    ?? "";
                string fromEmail = ConfigurationManager.AppSettings["PreCheckoutFolioEmail"]?.ToString();
                string subject = ConfigurationManager.AppSettings["PreCheckoutFolioEmailSubject"]?.ToString();
                string displayFrom = ConfigurationManager.AppSettings["EmailDisplayName"]?.ToString();
                string confirmationNumber = SessionData.OperaReservation.ReservationNumber;
                string folioBase64 = SessionData.FolioBase64;

                new LogHelper().Log("Queueing guest folio email to " + emailID, reservationNameID, actionName, actionGroup);

                try
                {
                    if (!string.IsNullOrWhiteSpace(reservationID))
                    {
                        new ReservationLogics().UpdatePrimaryGuestEmail(emailID, reservationID);
                    }
                }
                catch (Exception updateEx)
                {
                    new LogHelper().Error(updateEx, reservationNameID, actionName, actionGroup);
                }

                HostingEnvironment.QueueBackgroundWorkItem(async cancellationToken =>
                {
                    try
                    {
                        Models.Emails.EmailResponse emailResponse = await new CloudHelper().SendEmail(reservationNameID, new Models.Emails.EmailRequest()
                        {
                            FromEmail = fromEmail,
                            ToEmail = emailID,
                            GuestName = guestName,
                            Subject = subject,
                            confirmationNumber = confirmationNumber,
                            displayFromEmail = displayFrom,
                            EmailType = Models.Emails.EmailType.GuestFolio,
                            AttchmentBase64 = folioBase64,
                            AttachmentFileName = "Folio.pdf"
                        }, actionGroup, apiBaseUrl);

                        if (emailResponse == null || !emailResponse.result)
                        {
                            string reason = emailResponse?.responseMessage ?? "Unknown email API failure";
                            new LogHelper().Log("Failed to send guest folio email with reason :- " + reason, reservationNameID, actionName, actionGroup);
                            new LogHelper().Warn("Failed to send guest folio email with reason :- " + reason, reservationNameID, actionName, actionGroup);
                            AuditProgressHelper.Log(
                                AuditProgressHelper.ModulePreCheckout,
                                AuditProgressHelper.Actions.InvoiceEmailResendFailed,
                                reservationID,
                                reservationNameID,
                                extraDetail: "from Thank you page (background), to " + emailID + " - " + reason);
                            return;
                        }

                        new LogHelper().Log("Guest folio email sent successfully to " + emailID, reservationNameID, actionName, actionGroup);
                        AuditProgressHelper.Log(
                            AuditProgressHelper.ModulePreCheckout,
                            AuditProgressHelper.Actions.InvoiceEmailResent,
                            reservationID,
                            reservationNameID,
                            extraDetail: "from Thank you page, to " + emailID);
                    }
                    catch (Exception bgEx)
                    {
                        new LogHelper().Error(bgEx, reservationNameID, actionName, actionGroup);
                        AuditProgressHelper.Log(
                            AuditProgressHelper.ModulePreCheckout,
                            AuditProgressHelper.Actions.InvoiceEmailResendFailed,
                            reservationID,
                            reservationNameID,
                            extraDetail: "from Thank you page (background), to " + emailID + " - " + bgEx.Message);
                    }
                });

                return Ok(new { result = true });
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, sendEmail?.reservationID ?? "", actionName, actionGroup);
                return Ok(new { result = false, responseMessage = "Unable to send the invoice email. Please try again or contact the front desk." });
            }
        }


        [HttpPost]
        [ActionName("ValidateDocument")]
        [Route("api/portalservice/ValidateDocument")]
        public async Task<ReadDocumentResponseModel> ValidateDocument(ValidateDocumentModel validateDocument)
        {
            HttpClient httpClient = new HttpClient();

            try
            {
                string BaseURL = ConfigurationManager.AppSettings["AdaptorAPIBaseURL"].ToString();

                CloudAPIRequestModel validateDocRequest = new CloudAPIRequestModel()
                {
                    RequestObject = new RegulaRequest()
                    {
                        Base64Image = validateDocument.imageBase64,
                        ImageFormat = validateDocument.extension
                    }
                };

                httpClient.BaseAddress = new Uri(BaseURL);

                string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(validateDocRequest);
                var accessToken = AuthenticationHelper.GetAPIAccessToken();
                if (!string.IsNullOrEmpty(accessToken))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }
                HttpContent requestContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync($"ProcessDocument", requestContent);

                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string responsestr = await response.Content.ReadAsStringAsync();
                        var ResponseObj = Newtonsoft.Json.JsonConvert.DeserializeObject<ReadDocumentResponseModel>(responsestr);

                        BusinessLayer.MastersLogics mastersLogics = new MastersLogics();
                        
                        if (ResponseObj.responseData != null)
                        {

                            Helpers.HttpClientHelper httpClientHelper = new Helpers.HttpClientHelper(BaseURL);

                            string requestJson = "{\"RequestObject\":\"" + ResponseObj.responseData.TransactionID + "\"}";
                            var ResponseObjDocImg = await httpClientHelper.PostAsync<RegulaAPIResponseModel>("GetProcessedImage", requestJson);

                            if (ResponseObjDocImg != null && ResponseObjDocImg.responseData != null)
                            {
                                var ResponseObjDocImgData = Newtonsoft.Json.JsonConvert.DeserializeObject<List<GetDocumentImageResponseData>>(ResponseObjDocImg.responseData.ToString());
                                if(ResponseObjDocImgData != null && ResponseObjDocImgData.Count > 0)
                                {
                                    ResponseObj.responseData.fullImage = ResponseObjDocImgData[0].Base64ImageString;
                                }
                            }

                            string docTypeRequestJson = "{\"RequestObject\":\"" + ResponseObj.responseData.TransactionID + "\"}";
                            var ResponseObjDocType = await httpClientHelper.PostAsync<RegulaAPIResponseModel>("GetProcessedDocumentType", docTypeRequestJson);

                            if(ResponseObjDocType != null)
                            {
                                var ResponseObjDocTypeData = Newtonsoft.Json.JsonConvert.DeserializeObject<RegulaDocumentTypeResponseModel>(ResponseObjDocType.responseData.ToString());
                                if(ResponseObjDocTypeData != null)
                                {
                                    ResponseObj.responseData.idType = ResponseObjDocTypeData.documentTypeCode;
                                    ResponseObj.responseData.issueCountry = ResponseObjDocTypeData.issueCountryCode;
                                }
                            }


                            string faceImageRequestJson = "{\"RequestObject\":\"" + ResponseObj.responseData.TransactionID + "\"}";
                            var ResponseObjFaceImage = await httpClientHelper.PostAsync<RegulaAPIResponseModel>("GetProcessedDocumentFaceImage", docTypeRequestJson);

                            if (ResponseObjFaceImage != null)
                            {
                               // var ResponseObjDocTypeData = Newtonsoft.Json.JsonConvert.DeserializeObject<RegulaDocumentTypeResponseModel>(ResponseObjDocType.responseData.ToString());
                                if (ResponseObjFaceImage.responseData != null)
                                {
                                    ResponseObj.responseData.faceImage = ResponseObjFaceImage.responseData.ToString();
                                }
                            }

                            var ResponseDataTable = await mastersLogics.validateDocumentIssueCountry(ResponseObj.responseData.idType, ResponseObj.responseData.issueCountry);

                            if (ResponseDataTable != null && ResponseDataTable.Rows.Count > 0)
                            {
                                if (ResponseDataTable.Rows[0][0].ToString() == "1")
                                {
                                    return ResponseObj;
                                }
                                else
                                {
                                    ResponseObj.Result = false;
                                    return ResponseObj;
                                }
                            }
                            else
                            {
                                ResponseObj.Result = false;
                                return ResponseObj;
                            }
                        }

                        if(ResponseObj.responseData == null)
                        {
                            ResponseObj.responseData = new ReadDocumentModel();
                            ResponseObj.responseData.fullImage = validateDocument.imageBase64;
                        }
                        return new ReadDocumentResponseModel()
                        {
                            responseData = ResponseObj.responseData,
                            ResponseMessage = "Invalid document",
                            Result = false
                        };
                    }
                    else
                    {
                        return new ReadDocumentResponseModel()
                        {
                            Result = false,
                            ResponseMessage = response.ReasonPhrase
                        };
                    }
                }
                return new ReadDocumentResponseModel()
                {
                    Result = false,
                    ResponseMessage = "Unable to read document"
                };
            }
            catch(Exception ex)
            {
                return new ReadDocumentResponseModel()
                {
                    Result = false,
                    ResponseMessage = "Unable to read document"
                };
            }
        }

        [HttpPost]
        [ActionName("ReadDocument")]
        [Route("api/portalservice/ReadDocument")]
        public async Task<ReadDocumentMBResponseModel> ReadDocument(CloudRequestModel ReadDocument)
        {
            HttpClient httpClient = new HttpClient();

            try
            {
                new LogHelper().Debug("Read Scanned Document portalservice request Size: " + ReadDocument?.ToString().Length, "", "ReadDocument", "Micro Blink Browser");

                BusinessLayer.BlinkIdLogics blinkIdLogics = new BlinkIdLogics();
                // var captureResponse = await blinkIdLogics.ReadScannedDocument(ReadDocument);
                var captureResponse = await blinkIdLogics.ReadScannedDocument(ReadDocument);

                //Task<BlinkResponseModel> ResponseObj = blinkIdLogics.ReadScannedDocument(ReadDocument);
                if (captureResponse != null && captureResponse.result)
                {
                    var responseObj = captureResponse.responseData as BlinkIdDocumentResponseModel;
                    if (responseObj != null)
                    {
                        bool uploadResult = await blinkIdLogics.UploadBlinkIdDocumentAsync(responseObj);
                        new LogHelper().Debug("uploadResult From portalservice : " + uploadResult, "", "ReadDocument", "Micro Blink Browser");

                        return new ReadDocumentMBResponseModel
                        {
                            Result = uploadResult,
                            ResponseMessage = uploadResult ? "Document uploaded successfully" : "Unable to Upload document"
                        };
                    }
                    new LogHelper().Debug("Unable to Upload document From portalservice ", "", "ReadDocument", "Micro Blink Browser");

                    return new ReadDocumentMBResponseModel { Result = false, ResponseMessage = "Unable to Upload document" };
                }
                new LogHelper().Debug("Unable to Upload document From portalservice ", "", "ReadDocument", "Micro Blink Browser");

                return new ReadDocumentMBResponseModel { Result = false, ResponseMessage = "Unable to read document" };
                
            }
            catch (Exception ex)
            {
                return new ReadDocumentMBResponseModel()
                {
                    Result = false,
                    ResponseMessage = "Unable to read document"
                };
            }
        }

        [System.Web.Http.HttpPost]
        [System.Web.Http.ActionName("ExtractDataFromMBDocument")]
        [Route("api/portalservice/ExtractDataFromMBDocument")]
        public async Task<BlinkDocumentResponseModel> ExtractDataFromMBDocument([FromBody] ValidateDocumentModel uplodedDocument)
        {
            HttpClient httpClient = new HttpClient();

            try
            {
                bool hasBack = !string.IsNullOrWhiteSpace(uplodedDocument.imageBase64Back);
                string frontImage = StripDocumentDataUrl(uplodedDocument.imageBase64);
                string backImage = hasBack ? StripDocumentDataUrl(uplodedDocument.imageBase64Back) : null;
                Helpers.LogHelper.Instance.Debug(
                    "Uploading Document Type and Size :- " + uplodedDocument.extension
                    + " : frontLen=" + (frontImage != null ? frontImage.Length : 0)
                    + "; hasBack=" + hasBack
                    + (hasBack ? ("; backLen=" + backImage.Length) : ""),
                    "", "ExtractDataFromMBDocument", "GuestPortal");
                string BaseURL = ConfigurationManager.AppSettings["APIBaseUrl"].ToString();

                CloudAPIRequestModel validateDocRequest = new CloudAPIRequestModel()
                {
                    RequestObject = new RegulaRequest()
                    {
                        Base64Image = frontImage,
                        Base64ImageFront = frontImage,
                        Base64ImageBack = backImage,
                        Base64Image2 = backImage,
                        ImageFormat = uplodedDocument.extension
                    }
                };

                httpClient.BaseAddress = new Uri(BaseURL);

                string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(validateDocRequest);
                var accessToken = AuthenticationHelper.GetAPIAccessToken();
                if (!string.IsNullOrEmpty(accessToken))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }
                HttpContent requestContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
                string cloudAction = hasBack
                    ? "MicroBlink/ExtractDataFromMBDocumentMultiSide"
                    : "MicroBlink/ExtractDataFromMBDocument";
                HttpResponseMessage response = await httpClient.PostAsync(cloudAction, requestContent);

                if (response != null)
                {
                    Helpers.LogHelper.Instance.Debug("ExtractDataFromMBDocument :- " + response.IsSuccessStatusCode, "", "ExtractDataFromMBDocument", "GuestPortal");
                    if (response.IsSuccessStatusCode)
                    {


                        string responsestr = await response.Content.ReadAsStringAsync();

                        var ResponseObj =
                            Newtonsoft.Json.JsonConvert.DeserializeObject<BlinkDocumentResponseModel>(responsestr);

                        if (ResponseObj.responseData == null)
                        {
                            ResponseObj.responseData = new MBlinkDocumentResponseModel();
                            ResponseObj.responseData.fullImage = uplodedDocument.imageBase64;
                        }

                        return ResponseObj;
                    }
                    else
                    {
                        Helpers.LogHelper.Instance.Log(
                            "Microblink unavailable. HTTP=" + response.StatusCode + " " + response.ReasonPhrase,
                            "", "ExtractDataFromMBDocument", "GuestPortal");
                        return new BlinkDocumentResponseModel()
                        {
                            Result = false,
                            ResponseMessage = response.ReasonPhrase
                        };
                    }
                }
                Helpers.LogHelper.Instance.Log("Microblink unavailable. Null response.", "", "ExtractDataFromMBDocument", "GuestPortal");
                return new BlinkDocumentResponseModel()
                {
                    Result = false,
                    ResponseMessage = "Unable to read document"
                };
            }
            catch (Exception ex)
            {
                Helpers.LogHelper.Instance.Log(
                    "Microblink down/unavailable. " + ex.Message,
                    "", "ExtractDataFromMBDocument", "GuestPortal");
                return new BlinkDocumentResponseModel()
                {
                    Result = false,
                    ResponseMessage = "Unable to read document"
                };
            }
        }

        /// <summary>
        /// Upsert precheckin wizard progress via Local API (tbReservationMetaData).
        /// </summary>
        [HttpPost]
        [ActionName("SaveReservationMetaData")]
        [Route("api/portalservice/SaveReservationMetaData")]
        public async Task<APIResponseModel> SaveReservationMetaData([FromBody] ReservationMetaDataModel metaData)
        {
            try
            {
                if (metaData == null || string.IsNullOrWhiteSpace(metaData.ReservationNumber))
                {
                    return new APIResponseModel()
                    {
                        result = false,
                        responseMessage = "ReservationNumber is required",
                        statusCode = -1
                    };
                }

                
                string BaseURL = ConfigurationManager.AppSettings["APIBaseUrl"].ToString();
                using (var httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new Uri(BaseURL);
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var accessToken = AuthenticationHelper.GetAPIAccessToken();
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    }

                    var request = new APIRequestModel { RequestObject = metaData };
                    HttpContent requestContent = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await httpClient.PostAsync("Local/SaveReservationMetaData", requestContent);
                    if (response != null && response.IsSuccessStatusCode)
                    {
                        string responsestr = await response.Content.ReadAsStringAsync();
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<APIResponseModel>(responsestr)
                            ?? new APIResponseModel() { result = true, responseMessage = "Success", statusCode = 101 };
                    }

                    return new APIResponseModel()
                    {
                        result = false,
                        responseMessage = response != null ? response.ReasonPhrase : "No response",
                        statusCode = -1
                    };
                }
            }
            catch (Exception ex)
            {
                Helpers.LogHelper.Instance.Debug("SaveReservationMetaData: " + ex.Message, "", "SaveReservationMetaData", "GuestPortal");
                return new APIResponseModel()
                {
                    result = false,
                    responseMessage = ex.Message,
                    statusCode = -1
                };
            }
        }

        /// <summary>
        /// Fetch precheckin wizard progress via Local API.
        /// </summary>
        [HttpPost]
        [ActionName("FetchReservationMetaData")]
        [Route("api/portalservice/FetchReservationMetaData")]
        public async Task<APIResponseModel> FetchReservationMetaData([FromBody] ReservationMetaDataModel filter)
        {
            try
            {
                if (filter == null || string.IsNullOrWhiteSpace(filter.ReservationNumber))
                {
                    return new APIResponseModel()
                    {
                        result = false,
                        responseMessage = "ReservationNumber is required",
                        statusCode = -1
                    };
                }

                
                string BaseURL = ConfigurationManager.AppSettings["APIBaseUrl"].ToString();
                using (var httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new Uri(BaseURL);
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var accessToken = AuthenticationHelper.GetAPIAccessToken();
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    }

                    var request = new APIRequestModel { RequestObject = filter };
                    HttpContent requestContent = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await httpClient.PostAsync("Local/FetchReservationMetaData", requestContent);
                    if (response != null && response.IsSuccessStatusCode)
                    {
                        string responsestr = await response.Content.ReadAsStringAsync();
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<APIResponseModel>(responsestr)
                            ?? new APIResponseModel() { result = false, responseMessage = "Empty response", statusCode = -1 };
                    }

                    return new APIResponseModel()
                    {
                        result = false,
                        responseMessage = response != null ? response.ReasonPhrase : "No response",
                        statusCode = -1
                    };
                }
            }
            catch (Exception ex)
            {
                Helpers.LogHelper.Instance.Debug("FetchReservationMetaData: " + ex.Message, "", "FetchReservationMetaData", "GuestPortal");
                return new APIResponseModel()
                {
                    result = false,
                    responseMessage = ex.Message,
                    statusCode = -1
                };
            }
        }

        private static string StripDocumentDataUrl(string image)
        {
            if (string.IsNullOrWhiteSpace(image))
            {
                return image;
            }
            int comma = image.IndexOf(',');
            if (image.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma > 0)
            {
                return image.Substring(comma + 1);
            }
            return image;
        }

        private static bool IsValidEmailAddress(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || email.IndexOf(' ') >= 0)
            {
                return false;
            }
            try
            {
                var parsed = new System.Net.Mail.MailAddress(email);
                if (!string.Equals(parsed.Address, email, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
            int at = email.IndexOf('@');
            if (at <= 0 || at != email.LastIndexOf('@'))
            {
                return false;
            }
            string domain = email.Substring(at + 1);
            return domain.IndexOf('.') > 0 && !domain.StartsWith(".") && !domain.EndsWith(".");
        }

    }
}
