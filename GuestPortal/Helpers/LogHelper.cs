using CheckinPortal.Models;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using CheckinPortal.BackOffice.Helpers;

namespace CheckinPortal.Helpers
{
    public class LogHelper
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        private static readonly Lazy<LogHelper>
          lazy = new Lazy<LogHelper>(() => new LogHelper()
          {
              
          });

        public static LogHelper Instance
        {
            get
            {
                return lazy.Value;
            }
        }



        public void Log(string message,string reservationNameID,string actionName, string actionGroup)
        {
            Task.Run(async () =>
            {
                try
                {
                        NlogRequest request = new NlogRequest
                    {
                        Level = LogLevel.Info.Name,
                        Message = message,
                        ActionName = actionName,
                        ActionGroup = actionGroup,
                        ApplicationName = "WebCheckin",
                        ReservationNameID = reservationNameID
                    };
                    await Nlog(request, AppSettingsManager.GetDecryptedSetting("APIBaseUrl"));
                }
                catch
                {
                // ignore logging errors
            }
        });
        }

        public void Warn(string message, string reservationNameID, string actionName, string actionGroup)
        {
            Task.Run(async () =>
            {
                try
                {
                    NlogRequest request = new NlogRequest
                    {
                        Level = LogLevel.Warn.Name,
                        Message = message,
                        ActionName = actionName,
                        ActionGroup = actionGroup,
                        ApplicationName = "WebCheckin",
                        ReservationNameID = reservationNameID
                    };
                    await Nlog(request, AppSettingsManager.GetDecryptedSetting("APIBaseUrl"));
                }
                catch
                {
                    // ignore logging errors
                }
            });
        }

        //public async Task Debug(string message, string reservationNameID, string actionName, string actionGroup)
        //{
        //    NlogRequest request = new NlogRequest
        //    {
        //        Level = LogLevel.Debug,
        //        Message = message,               
        //        ActionName = actionName,
        //        ActionGroup = actionGroup,
        //        ApplicationName = "WebCheckin",
        //        ReservationNameID = reservationNameID
        //    };
        //    await Nlog(request, AppSettingsManager.GetDecryptedSetting("APIBaseUrl"));
        //}
        public void Debug(string message,
                  string reservationNameID,
                  string actionName,
                  string actionGroup)
        {
            Task.Run(async () =>
            {
                try
                {
                    NlogRequest request = new NlogRequest
                    {
                        Level = LogLevel.Debug.Name,
                        Message = message,
                        ActionName = actionName,
                        ActionGroup = actionGroup,
                        ApplicationName = "WebCheckin",
                        ReservationNameID = reservationNameID
                    };

                    await Nlog(
                        request,
                        AppSettingsManager.GetDecryptedSetting("APIBaseUrl")
                    );
                }
                catch
                {
                    // ignore logging errors
                }
            });
        }
        public void Error(Exception  message, string reservationNameID, string actionName, string actionGroup)
        {
            Task.Run(async () =>
            {
                try
                {
                    NlogRequest request = new NlogRequest
                    {
                        Level = LogLevel.Error.Name,
                        Message = message.Message,
                        Exception = message,
                        ActionName = actionName,
                        ActionGroup = actionGroup,
                        ApplicationName = "WebCheckin",
                        ReservationNameID = reservationNameID
                    };
                    await Nlog(
                            request,
                            AppSettingsManager.GetDecryptedSetting("APIBaseUrl")
                        );
                }
                catch
                {
                    // ignore logging errors
                }
            });
        }
        public async Task<APIResponseModel> Nlog(Models.NlogRequest localRequest, string api_url)
        {
            try
            {
                
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    
                    return new Models.APIResponseModel()
                    {
                        result = false,
                        responseMessage = "Failed to generate the proxy http client"
                    };
                }
                httpClient.DefaultRequestHeaders.Clear();
                var accessToken = AuthenticationHelper.GetAPIAccessToken();
                if (!string.IsNullOrEmpty(accessToken))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }
                string requestString = JsonConvert.SerializeObject(localRequest, Formatting.None);
                 var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/local/Nlog", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        Models.APIResponseModel localResponse = JsonConvert.DeserializeObject<Models.APIResponseModel>(apiResponse);
                        return localResponse;
                    }
                    else
                    {
                        return new Models.APIResponseModel()
                        {
                            result = false,
                            responseMessage = response.ReasonPhrase
                        };
                    }
                }
                else
                {
                    return new Models.APIResponseModel()
                    {
                        result = false,
                        responseMessage = "Local web API returned null"
                    };
                }
            }
            catch (Exception ex)
            {
                try
                {
                    string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "LocalLogs");
                    Directory.CreateDirectory(logDir);
                    string line = DateTime.UtcNow.ToString("o")
                        + " | Nlog unreachable | " + (localRequest?.ActionName ?? "")
                        + " | " + (localRequest?.ReservationNameID ?? "")
                        + " | " + (localRequest?.Message ?? "")
                        + " | " + ex.Message
                        + Environment.NewLine;
                    File.AppendAllText(Path.Combine(logDir, "nlog-fallback.txt"), line);
                }
                catch
                {
                    // ignore disk fallback failures
                }
                return new Models.APIResponseModel()
                {
                    result = false,
                    responseMessage = "Generic Exception : " + ex.Message
                };
            }
        }

    }
}