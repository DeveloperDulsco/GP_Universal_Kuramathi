using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace CheckinPortal.Helpers
{
    public class HttpClientHelper
    {
        private HttpClient httpClient;

        public HttpClientHelper(string BaseURL)
        {
            httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(BaseURL);

        }

        public async Task<T> PostAsync<T>(string functionName, string requestJsonString)
        {
            HttpContent requestContent = new StringContent(requestJsonString, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await httpClient.PostAsync($"{functionName}", requestContent);
            string responsestr1 = await response.Content.ReadAsStringAsync();
            var ResponseObj = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(responsestr1);
            return ResponseObj;
        }
        

    }
}