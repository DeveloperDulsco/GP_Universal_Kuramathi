
using Newtonsoft.Json.Linq;

using System.Reflection;
using System.Text;
using System.Web.ModelBinding;
using System.Web;
using System;
using System.Net.Http;
using System.Configuration;
using CheckinPortal.BackOffice.Helpers;

namespace CheckinPortal.Helpers
{
    internal static class AuthenticationHelper
    {
        private static string APIToken { get; set; }
        private static DateTime APITokenExpiryTime { get; set; }
        public static string GetAPIAccessToken()
        {

            try
            {
                if (!string.IsNullOrWhiteSpace(APIToken) && APITokenExpiryTime.AddMinutes(10) > DateTime.Now)
                {
                    return APIToken;
                }

                using (var client = new HttpClient())
                {
                    var payload = new
                    {
                        ClientId = AppSettingsManager.GetDecryptedSetting("ClientId"),
                        ClientSecret = AppSettingsManager.GetDecryptedSetting("ClientSecret")
                    };

                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    var jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    var response = client.PostAsync(new Uri($"{AppSettingsManager.GetDecryptedSetting("APIBaseUrl")}/Auth/GenerateToken"), content).Result;
                    if (response != null && response.IsSuccessStatusCode)
                    {
                        var responseContent = response.Content.ReadAsStringAsync().Result;
                        var tokenObj = JObject.Parse(responseContent);
                        var newToken = tokenObj["token"]?.ToString();

                        APIToken = newToken;
                        APITokenExpiryTime = DateTime.Now;

                        return newToken;
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return null;
        }

    }

}
