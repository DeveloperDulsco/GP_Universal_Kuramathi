using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Net;


using System.Web.Mvc;

using CheckinPortal.Models.PaymentDetails;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using CheckinPortal.Models.BlinkIdModels;
using System.Net.NetworkInformation;
using CheckinPortal.Models.AdaptorAPIModels;
using Newtonsoft.Json.Linq;
using CheckinPortal.BusinessLayer;
using System.Web.Hosting;
using CheckinPortal.Helpers;
using System.Threading;
using CheckinPortal.Models;
using System.Net.Http.Headers;
using System.Configuration;
using System.Web.Http;

namespace CheckinPortal.Controllers
{
   
    public class BlinkIDController : Controller
    {
     
        [System.Web.Http.HttpPost]
        public async Task<ActionResult> ScanResult()
        {
            try
            {
                Request.InputStream.Position = 0;
                using (var reader = new StreamReader(Request.InputStream))
                {
                    var body = reader.ReadToEnd();
                   
                    // Get the path to the bin folder
                    string binPath = AppDomain.CurrentDomain.BaseDirectory;

                    // Combine with file name
                    string filePath = Path.Combine(binPath, "ScanResult1.txt");

                    // Write JSON to file

                    System.IO.File.WriteAllText(filePath, body);
                    dynamic jsonObj = JsonConvert.DeserializeObject(body);

                    string guestProfileId = Session["guestprofileid"]?.ToString() ?? "0"; //jsonObj.guestProfileId;
                    string reservationId = Session["reservationId"]?.ToString() ?? "0";
                    string currentguestpmsprofileid = Session["currentguestpmsprofileid"]?.ToString() ?? null;
                    Helpers.LogHelper.Instance.Debug("ScanResult - guestProfileId: " + guestProfileId + " reservationId : " + reservationId, null, "ScanResult", "ScanResult");

                        var wrapper = new CloudRequestModel
                        {
                            ReservationId = reservationId,
                            ProfileDetailId= guestProfileId,
                            ProfileID = currentguestpmsprofileid,
                            RequestObject = JObject.Parse(body) //JsonConvert.DeserializeObject(body)
                        };
                    //Task.Run(() => ProcessScanAsync(wrapper));
                    HostingEnvironment.QueueBackgroundWorkItem(async token =>
                    {
                        await ProcessScanAsync(wrapper);
                    });
                    //await  Task.Run(async () => await SafeLongOperation(async () =>
                    //  {
                    //      await ProcessScanAsync(wrapper);
                    //  })).ConfigureAwait(false);

                    // BackgroundJob.Enqueue(() => SafeLongOperationAsync(() => ProcessScanAsync(wrapper)));
                    return Json(new { status = true, message = "Scan received" });
                        
                        
                }
                return Json(new { status = false, message = "Documet not scanned successfully, Try again" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        public async Task SafeLongOperation(Func<Task> work)
        {
            try
            {
                // Prevent ASP.NET thread aborts
                try { Thread.ResetAbort(); } catch { }

                await work();
            }
            catch (ThreadAbortException)
            {
                try { Thread.ResetAbort(); } catch { }
                // log but do NOT rethrow
               
            }
            catch (Exception ex)
            {
                //LogHelper.Instance.Error(ex);
            }
        }

        public async Task ProcessScanAsync(CloudRequestModel wrapper)
        {
            LogHelper.Instance.Debug("Background ProcessScanAsync started...", "", "ProcessScanAsync", "BlinkIdController");
            var logic = new BlinkIdLogics();
            var response = await logic.ReadScannedDocument(wrapper);

            if (response.result)
            {
                var model = response.responseData as BlinkIdDocumentResponseModel;
                await logic.UploadBlinkIdDocumentAsync(model);
            }
        }
        public ActionResult Index()
        {
            string guestProfileId = Request.QueryString["guestProfileId"];
            string resrvnId = Request.QueryString["ReservationId"];
            string currentguestpmsprofileid = Request.QueryString["currentguestpmsprofileid"];
            //  Store it in session for later use in ScanResult
            if (!string.IsNullOrEmpty(guestProfileId))
            {
                Session["guestprofileid"] = guestProfileId;
            }
            if (!string.IsNullOrEmpty(resrvnId))
            {
                Session["reservationId"] = resrvnId;
            }
            if (!string.IsNullOrEmpty(currentguestpmsprofileid))
            {
                Session["currentguestpmsprofileid"] = currentguestpmsprofileid;
            }
            return View();
        }
        //[System.Web.Http.HttpPost]
        //[System.Web.Http.ActionName("ExtractDataFromMBDocument")]
        //[System.Web.Http.Route("blinkid/ExtractDataFromMBDocument")]
        //public async Task<ReadDocumentResponseModel> ExtractDataFromMBDocument([FromBody] ValidateDocumentModel uplodedDocument)
        //{
        //    HttpClient httpClient = new HttpClient();

        //    try
        //    {
        //        Helpers.LogHelper.Instance.Debug("Uploading Document Type and Size :- " + uplodedDocument.extension +" : "+ uplodedDocument.imageBase64.Length, "", "ExtractDataFromMBDocument", "GuestPortal");
        //        string BaseURL = ConfigurationManager.AppSettings["APIBaseUrl"].ToString();

        //        CloudAPIRequestModel validateDocRequest = new CloudAPIRequestModel()
        //        {
        //            RequestObject = new RegulaRequest()
        //            {
        //                Base64Image = uplodedDocument.imageBase64,
        //                ImageFormat = uplodedDocument.extension
        //            }
        //        };

        //        httpClient.BaseAddress = new Uri(BaseURL);

        //        string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(validateDocRequest);
        //        var accessToken = AuthenticationHelper.GetAPIAccessToken();
        //        if (!string.IsNullOrEmpty(accessToken))
        //        {
        //            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        //        }
        //        HttpContent requestContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
        //        HttpResponseMessage response = await httpClient.PostAsync($"MicroBlink/ExtractDataFromMBDocument", requestContent);

        //        if (response != null)
        //        {
        //            Helpers.LogHelper.Instance.Debug("ExtractDataFromMBDocument :- " + response.IsSuccessStatusCode, "", "ExtractDataFromMBDocument", "GuestPortal");
        //            if (response.IsSuccessStatusCode)
        //            {


        //                string responsestr = await response.Content.ReadAsStringAsync();

        //                var ResponseObj =
        //                    Newtonsoft.Json.JsonConvert.DeserializeObject<ReadDocumentResponseModel>(responsestr);

        //                if (ResponseObj.responseData == null)
        //                {
        //                    ResponseObj.responseData = new ReadDocumentModel();
        //                    ResponseObj.responseData.fullImage = uplodedDocument.imageBase64;
        //                }

        //                return ResponseObj;
        //            }
        //            else
        //            {
        //                return new ReadDocumentResponseModel()
        //                {
        //                    Result = false,
        //                    ResponseMessage = response.ReasonPhrase
        //                };
        //            }
        //        }
        //        return new ReadDocumentResponseModel()
        //        {
        //            Result = false,
        //            ResponseMessage = "Unable to read document"
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        return new ReadDocumentResponseModel()
        //        {
        //            Result = false,
        //            ResponseMessage = "Unable to read document"
        //        };
        //    }
        //}


    }

}