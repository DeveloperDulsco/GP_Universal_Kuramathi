using CheckinPortal.Helpers;
using CheckinPortal.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace CheckinPortal.Controllers
{
    public class PaymentController : Controller
    {
        // GET: Payment
        public ActionResult Index()
        {
            return View();
        }


        public async Task<ActionResult> MakePayment(MakePaymentCheckinPortalModels makePaymentCheckinPortalModels)
        {
            using (var httpClient = new HttpClient())
            {
                
                System.IO.File.AppendAllLines($"{Server.MapPath("/log.txt")}", new string[] { $"{DateTime.Now.ToString()} :", Newtonsoft.Json.JsonConvert.SerializeObject(makePaymentCheckinPortalModels) });
                Models.AdaptorAPIModels.makePaymentModel makePaymentModel = new Models.AdaptorAPIModels.makePaymentModel();

                string BaseURL = ConfigurationManager.AppSettings["AdaptorAPIPaymentBaseURL"].ToString();
                string APIKey = ConfigurationManager.AppSettings["APIKey"].ToString();
                string MerchantAccount = ConfigurationManager.AppSettings["MerchantAccount"].ToString();
                string CostEstimator_MCC = ConfigurationManager.AppSettings["CostEstimator_MCC"].ToString();

                httpClient.BaseAddress = new Uri(BaseURL);

                try
                {
                    makePaymentModel.ApiKey = APIKey;
                    makePaymentModel.MerchantAccount = MerchantAccount;


                    string transactionTye = TransactionType.Sale.ToString();

                    if (makePaymentCheckinPortalModels.FundingSource != null && makePaymentCheckinPortalModels.FundingSource == "CREDIT")
                    {
                        transactionTye = TransactionType.PreAuth.ToString();
                    }

                    makePaymentModel.RequestObject = new Models.AdaptorAPIModels.RequestObject()
                    {
                        Amount = makePaymentCheckinPortalModels.AmountToCharge.ToString("0.00"),
                        BrowserInfo = makePaymentCheckinPortalModels.makePaymentRequest.browserInfo,
                        Currency = "SGD",
                        ReservationNameID = makePaymentCheckinPortalModels.ReservationNameID,
                        IsEnableOneClick = true,
                        IsRecurringEnable = false,
                        Mcc = CostEstimator_MCC,
                        MerchantReference = makePaymentCheckinPortalModels.MerchantReference,
                        PaymentMethod = makePaymentCheckinPortalModels.makePaymentRequest.paymentMethod,
                        PrevPspRefernce = null,
                        RefernceUniqueId = makePaymentCheckinPortalModels.TransctionID,// Convert.ToInt32(DateTime.Now.ToString("yyMMddHHmm")),
                        ReturnUrl = new Uri(ConfigurationManager.AppSettings["PaymentReturnURL"].ToString() + $"?ConfirmationNo={makePaymentCheckinPortalModels.ConfirmationNo}&TransactionID={makePaymentCheckinPortalModels.TransctionID}"),
                        TransactionType = transactionTye
                    };

                    if(!string.IsNullOrEmpty(makePaymentModel.RequestObject.Amount) && makePaymentModel.RequestObject.Amount.Contains(","))
                    {
                        makePaymentModel.RequestObject.Amount = makePaymentModel.RequestObject.Amount.Replace(",", ".");
                    }

                }
                catch (Exception ex)
                {

                }

                string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(makePaymentModel);

                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Add("x-api-key", ConfigurationManager.AppSettings["APIKey"].ToString());
                var accessToken = AuthenticationHelper.GetAPIAccessToken();
                if (!string.IsNullOrEmpty(accessToken))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }
                HttpContent requestContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync($"MakePayment", requestContent);
                if(response != null){
                    var test = await response.Content.ReadAsStringAsync();
                    if (response.IsSuccessStatusCode)
                    {
                        var resposneObj = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.AdaptorAPIModels.MakePaymentResponseModel>(test);
                        if (resposneObj.Result)
                        {
                            if (resposneObj.ResponseObject.ResultCode == "RedirectShopper")
                            {
                                var resposneObjRedirect = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.AdaptorAPIModels.MakePaymentWithRedirectModel>(test);
                                return Json(new { result = true, adyenResponse = resposneObjRedirect.ResponseObject.ToString(), message = "", isRedirect = true });
                            }
                            else
                            {

                                return Json(new { result = true, adyenResponse = resposneObj, message = "", isRedirect = false });
                            }
                        }
                        else
                        {
                            string errorMessage = resposneObj.ResponseMessage != null ? resposneObj.ResponseMessage : resposneObj.ResponseObject.RefusalReason;

                            return Json(new { result = false, adyenResponse = resposneObj, message = errorMessage, isRedirect = false });
                        }

                    }
                    return Json(new { result = false, adyenResponse = "", message = response.ReasonPhrase });
                    //return Json(await response.Content.ReadAsStringAsync());
                }
                else
                {
                    return Json(new { result = false, adyenResponse = "", message = "Unable to process your payment", isRedirect = false });
                }
            }
        }


        public async Task<ActionResult> MakePaymentCheckout(MakePaymentCheckinPortalModels makePaymentCheckinPortalModels)
        {
            using (var httpClient = new HttpClient())
            {
                
                Models.AdaptorAPIModels.makePaymentModel makePaymentModel = new Models.AdaptorAPIModels.makePaymentModel();

                string BaseURL = ConfigurationManager.AppSettings["AdaptorAPIPaymentBaseURL"].ToString();
                string APIKey = ConfigurationManager.AppSettings["APIKey"].ToString();
                string MerchantAccount = ConfigurationManager.AppSettings["MerchantAccount"].ToString();
                string CostEstimator_MCC = ConfigurationManager.AppSettings["CostEstimator_MCC"].ToString();

                httpClient.BaseAddress = new Uri(BaseURL);

                try
                {
                    makePaymentModel.ApiKey = APIKey;
                    makePaymentModel.MerchantAccount = MerchantAccount;


                    string transactionTye = TransactionType.Sale.ToString();

                    if(makePaymentCheckinPortalModels.FundingSource != null && makePaymentCheckinPortalModels.FundingSource == "CREDIT")
                    {
                        transactionTye = TransactionType.PreAuth.ToString();
                    }

                    makePaymentModel.RequestObject = new Models.AdaptorAPIModels.RequestObject()
                    {
                        Amount = makePaymentCheckinPortalModels.AmountToCharge.ToString("0.00"),
                        BrowserInfo = makePaymentCheckinPortalModels.makePaymentRequest.browserInfo,
                        Currency = "EUR",
                        ReservationNameID = makePaymentCheckinPortalModels.ReservationNameID,
                        IsEnableOneClick = true,
                        IsRecurringEnable = false,
                        Mcc = CostEstimator_MCC,
                        MerchantReference = makePaymentCheckinPortalModels.MerchantReference,
                        PaymentMethod = makePaymentCheckinPortalModels.makePaymentRequest.paymentMethod,
                        PrevPspRefernce = null,
                        RefernceUniqueId = makePaymentCheckinPortalModels.TransctionID,// Convert.ToInt32(DateTime.Now.ToString("yyMMddHHmm")),
                        ReturnUrl = new Uri(ConfigurationManager.AppSettings["PaymentReturnURLCheckout"].ToString() + $"?ConfirmationNo={makePaymentCheckinPortalModels.ConfirmationNo}&TransactionID={makePaymentCheckinPortalModels.TransctionID}"),
                        TransactionType = transactionTye
                    };
                    if (!string.IsNullOrEmpty(makePaymentModel.RequestObject.Amount) && makePaymentModel.RequestObject.Amount.Contains(","))
                    {
                        makePaymentModel.RequestObject.Amount = makePaymentModel.RequestObject.Amount.Replace(",", ".");
                    }

                }
                catch (Exception ex)
                {

                }

                string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(makePaymentModel);

                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Add("x-api-key", ConfigurationManager.AppSettings["APIKey"].ToString());

                HttpContent requestContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync($"MakePayment", requestContent);
                if(response != null){
                    var test = await response.Content.ReadAsStringAsync();
                    if (response.IsSuccessStatusCode)
                    {
                        var resposneObj = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.AdaptorAPIModels.MakePaymentResponseModel>(test);
                        if (resposneObj.Result)
                        {
                            if (resposneObj.ResponseObject.ResultCode == "RedirectShopper")
                            {
                                var resposneObjRedirect = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.AdaptorAPIModels.MakePaymentWithRedirectModel>(test);
                                return Json(new { result = true, adyenResponse = resposneObjRedirect.ResponseObject.ToString(), message = "", isRedirect = true });
                            }
                            else
                            {

                                return Json(new { result = true, adyenResponse = resposneObj, message = "", isRedirect = false });
                            }
                        }
                        else
                        {
                            string errorMessage = resposneObj.ResponseMessage != null ? resposneObj.ResponseMessage : resposneObj.ResponseObject.RefusalReason;

                            return Json(new { result = false, adyenResponse = resposneObj, message = errorMessage, isRedirect = false });
                        }

                    }
                    return Json(new { result = false, adyenResponse = "", message = response.ReasonPhrase });
                    //return Json(await response.Content.ReadAsStringAsync());
                }
                else
                {
                    return Json(new { result = false, adyenResponse = "", message = "Unable to process your payment", isRedirect = false });
                }
            }
        }

    
    }
}