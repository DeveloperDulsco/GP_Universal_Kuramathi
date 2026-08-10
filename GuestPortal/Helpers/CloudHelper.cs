using CheckinPortal.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CheckinPortal.BusinessLayer;
using CheckinPortal.Models.Emails;
using System.Net.Http.Headers;
using System.Data;
using Newtonsoft.Json.Linq;

namespace CheckinPortal.Helpers
{
     class CloudHelper
    {
        public  async Task<List<Models.OWS.OperaReservation>> fetchReservationFromPMS(Models.OWS.OwsRequestModel owsRequest, string api_url, string Process,string ActionGroup)
        {
            string functionName = "fetchReservationFromPMS";
            string applicationName = "GuestPortal";
            string description = ActionGroup;
            if (owsRequest == null)
            {
                LogHelper.Instance.Debug($"OWS request can not be null : -7",
                    functionName, applicationName, description);
                return null;
            }
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    HttpContent requestContent = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(owsRequest), Encoding.UTF8, "application/json");
                    httpClient.Timeout = TimeSpan.FromMinutes(2);
                    var accessToken = AuthenticationHelper.GetAPIAccessToken();
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    }
                    HttpResponseMessage httpResponse = await httpClient.PostAsync(new Uri($"{api_url}/ows/FetchReservation"), requestContent);
                    if (httpResponse != null && httpResponse.IsSuccessStatusCode)
                    {
                        var responseMessage = await httpResponse.Content.ReadAsStringAsync();
                        if (!string.IsNullOrEmpty(responseMessage))
                        {
                            var apiResponse = JsonConvert.DeserializeObject<APIResponseModel>(responseMessage);
                            if (apiResponse != null && apiResponse.responseData != null && apiResponse.result)
                            {
                                if (!string.IsNullOrEmpty(Process))
                                {
                                    var checkinReservationList = new ReservationCloudLogic().verifyReservationisAllowed(JsonConvert.DeserializeObject<List<Models.OWS.OperaReservation>>(apiResponse.responseData.ToString()), Process);
                                    if (checkinReservationList != null)
                                        return checkinReservationList;
                                    else
                                    {
                                        LogHelper.Instance.Debug($"Reservation is not valid for check in",
                                        functionName, applicationName, description);
                                        return null;
                                    }
                                }
                                else
                                {
                                    var reservationList = JsonConvert.DeserializeObject<List<Models.OWS.OperaReservation>>(apiResponse.responseData.ToString());
                                    if (reservationList != null)
                                    {
                                        LogHelper.Instance.Debug($"Reservation details : {apiResponse.responseData.ToString()}",
                                        functionName, applicationName, description);
                                        return reservationList;
                                    }
                                    else
                                    {
                                        LogHelper.Instance.Debug($"Reservation is not exists",
                                        functionName, applicationName, description);
                                        return null;
                                    }
                                }
                            }
                            else if (apiResponse != null)
                            {
                                LogHelper.Instance.Debug($"Failed to fetch the reservation details from PMS with the below response from API :- {apiResponse.responseMessage}",
                               functionName, applicationName, description);
                                return null;
                            }
                            else
                            {
                                LogHelper.Instance.Debug($"Failed to fetch the reservation details from PMS, since API returned NULL",
                                functionName, applicationName, description);
                                return null;
                            }
                        }
                        else
                        {
                            LogHelper.Instance.Debug("Failed to fetch the reservation details from PMS, since API returned NULL",
                                functionName, applicationName, description);
                            return null;
                        }
                    }
                    else
                    {
                        LogHelper.Instance.Debug($"Failed to fetch reservation details from PMS with the below response from API, {httpResponse.ReasonPhrase} code:- {httpResponse.StatusCode}",
                            functionName, applicationName, description);
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error(ex, functionName, applicationName, description);
                return null;
            }
        }

        public  async Task<bool> updateReservationDetailsInLocalDB(APIRequestModel Request, string api_url, string ActionGroup)
        {

            string functionName = "updateReservationDetailsInLocalDB";
            string applicationName = "GuestPortal";
            string description = ActionGroup;

            if (Request == null)
            {
                LogHelper.Instance.Debug($"API request can not be null : -7",
                    functionName, applicationName, description);
                return false;
            }
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    string temp = Newtonsoft.Json.JsonConvert.SerializeObject(Request);
                    HttpContent requestContent = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(Request), Encoding.UTF8, "application/json");
                    httpClient.Timeout = TimeSpan.FromMinutes(2);
                    var accessToken = AuthenticationHelper.GetAPIAccessToken();
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    }
                    HttpResponseMessage httpResponse = await httpClient.PostAsync(new Uri($"{api_url}/local/PushReservationDetails"), requestContent);
                    if (httpResponse != null && httpResponse.IsSuccessStatusCode)
                    {
                        var responseMessage = await httpResponse.Content.ReadAsStringAsync();
                        if (!string.IsNullOrEmpty(responseMessage))
                        {
                            var localResponse = JsonConvert.DeserializeObject<APIResponseModel>(responseMessage);
                            if (localResponse != null && localResponse.result)
                            {

                                return true;
                            }
                            else
                            {
                                if (localResponse != null)
                                {
                                    LogHelper.Instance.Debug($"Failed updateReservationDetailsInLocalDB to DB, reason - {localResponse.responseMessage}",
                                    functionName, applicationName, description);
                                   
                                    return false;
                                }
                                else
                                {
                                    LogHelper.Instance.Debug($"Failed updateReservationDetailsInLocalDB to DB, UNKNOW Reason",
                                    functionName, applicationName, description);
                                   
                                    return false;
                                }
                            }
                        }
                        else
                        {
                            LogHelper.Instance.Debug("Failed updateReservationDetailsInLocalDB to DB, since API returned NULL",
                                functionName, applicationName, description);
                           
                            return false;
                        }
                    }
                    else
                    {
                        LogHelper.Instance.Debug($"Failed updateReservationDetailsInLocalDB to DB, {httpResponse.ReasonPhrase} code:- {httpResponse.StatusCode}",
                            functionName, applicationName, description);
                     
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error(ex, functionName, applicationName, description);
               
                return false;
            }
        }

        //public static async Task<bool> updateGuestProfileInPMS(OWSRequestModel owsRequest,
        //                                string ApifunctionName, string api_url, string ActionGroup)
        //{

        //    string functionName = "updateGuestProfileInPMS";
        //    string applicationName = "GuestPortal";
        //    string description = ActionGroup;

        //    if (owsRequest == null)
        //    {
        //        LogHelper.Instance.Debug($"OWS request can not be null : -7",
        //            functionName, applicationName, description);
        //        return false;
        //    }
        //    try
        //    {
        //        using (var httpClient = new HttpClient())
        //        {
        //            httpClient.DefaultRequestHeaders.Clear();
        //            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        //            HttpContent requestContent = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(owsRequest), Encoding.UTF8, "application/json");
        //            httpClient.Timeout = TimeSpan.FromMinutes(2);
        //            var accessToken = AuthenticationHelper.GetAPIAccessToken();
        //            if (!string.IsNullOrEmpty(accessToken))
        //            {
        //                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        //            }
        //            HttpResponseMessage httpResponse = await httpClient.PostAsync(new Uri($"{api_url}/ows/{ApifunctionName}"), requestContent);
        //            if (httpResponse != null && httpResponse.IsSuccessStatusCode)
        //            {
        //                var responseMessage = await httpResponse.Content.ReadAsStringAsync();
        //                if (!string.IsNullOrEmpty(responseMessage))
        //                {
        //                    OwsResponseModel owsResponse = new OwsResponseModel();
        //                    owsResponse = JsonConvert.DeserializeObject<OwsResponseModel>(responseMessage);
        //                    if (owsResponse != null && owsResponse.result)
        //                    {
        //                        return true;
        //                    }
        //                    else
        //                    {
        //                        if (owsResponse != null)
        //                        {
        //                            LogHelper.Instance.Debug($"Failed to update profile details in PMS, reason - {owsResponse.responseMessage}",
        //                            functionName, applicationName, description);

        //                            return false;
        //                        }
        //                        else
        //                        {
        //                            LogHelper.Instance.Debug($"Failed to update profile details in PMS, UNKNOW Reason",
        //                            functionName, applicationName, description);

        //                            return false;
        //                        }
        //                    }
        //                }
        //                else
        //                {
        //                    LogHelper.Instance.Debug("Failed to update profile details in PMS, since API returned NULL",
        //                        functionName, applicationName, description);

        //                    return false;
        //                }
        //            }
        //            else
        //            {
        //                LogHelper.Instance.Debug($"Failed to update profile details in PMS with the below response from API, {httpResponse.ReasonPhrase} code:- {httpResponse.StatusCode}",
        //                    functionName, applicationName, description);

        //                return false;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        LogHelper.Instance.Error(ex, functionName, applicationName, description);

        //        return false;
        //    }
        //}
       
        
        public static async Task<bool> updateGuestProfileInPMS(OWSRequestModel owsRequest,
                                       string ApifunctionName, string api_url, string ActionGroup, string ReservationNameID)
        {

            string functionName = "updateGuestProfileInPMS";
            string applicationName = "GuestPortal";
            string description = ActionGroup;


            new LogHelper().Debug($"Updating {ApifunctionName} info using web api", ReservationNameID, ApifunctionName, ActionGroup);

            if (owsRequest == null)
            {
                LogHelper.Instance.Debug($"OWS request can not be null : -7",
                    functionName, applicationName, description);
                return false;
            }
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    HttpContent requestContent = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(owsRequest), Encoding.UTF8, "application/json");
                    httpClient.Timeout = TimeSpan.FromMinutes(2);
                    var accessToken = AuthenticationHelper.GetAPIAccessToken();
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    }
                    string requestString = JsonConvert.SerializeObject(owsRequest, Formatting.None);

                    new LogHelper().Debug($"web api url :- /ows/{ApifunctionName}", ReservationNameID, ApifunctionName, ActionGroup);
                    new LogHelper().Debug("web api request :- " + requestString, ReservationNameID, ApifunctionName, ActionGroup);


                    HttpResponseMessage httpResponse = await httpClient.PostAsync(new Uri($"{api_url}/ows/{ApifunctionName}"), requestContent);
                    if (httpResponse != null && httpResponse.IsSuccessStatusCode)
                    {
                        var responseMessage = await httpResponse.Content.ReadAsStringAsync();
                        if (!string.IsNullOrEmpty(responseMessage))
                        {
                            OwsResponseModel owsResponse = new OwsResponseModel();
                            owsResponse = JsonConvert.DeserializeObject<OwsResponseModel>(responseMessage);
                            if (owsResponse != null && owsResponse.result)
                            {
                                return true;
                            }
                            else
                            {
                                if (owsResponse != null)
                                {
                                    LogHelper.Instance.Debug($"Failed to update {ApifunctionName} details in PMS, reason - {owsResponse.responseMessage}", ReservationNameID,
                                    ApifunctionName, ActionGroup);

                                    return false;
                                }
                                else
                                {
                                    LogHelper.Instance.Debug($"Failed to update {ApifunctionName} details in PMS, UNKNOW Reason"
                                  , ReservationNameID,
                                    ApifunctionName, ActionGroup);

                                    return false;
                                }
                            }
                        }
                        else
                        {
                            LogHelper.Instance.Debug("Failed to update profile details in PMS, since API returned NULL"
                                , ReservationNameID,
                                    functionName, ActionGroup);

                            return false;
                        }
                    }
                    else
                    {
                        LogHelper.Instance.Debug($"Failed to update profile details in PMS with the below response from API, {httpResponse.ReasonPhrase} code:- {httpResponse.StatusCode}"
                           , ReservationNameID,
                                    functionName, ActionGroup);

                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error(ex, ReservationNameID,
                                    functionName, ActionGroup);

                return false;
            }
        }

        public static async Task<OwsResponseModel> UpdateProfileAddressAsync(string reservationNameID,OWSRequestModel owsRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("Updating address info using web api", reservationNameID, "UpdateProfileAddressAsync",  groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to update address info using web api due to proxy error", reservationNameID, "UpdateProfileAddressAsync",  groupName);
                    return new OwsResponseModel()
                    {
                        result = false,
                        responseMessage = "Failed to generate the proxy http client"
                    };
                }
                httpClient.DefaultRequestHeaders.Clear();
                string requestString = JsonConvert.SerializeObject(owsRequest, Formatting.None);
                new LogHelper().Debug("web api url :- " + @"/ows/UpdateAddresList", reservationNameID, "UpdateProfileAddressAsync",groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "UpdateProfileAddressAsync",groupName);
                var accessToken = AuthenticationHelper.GetAPIAccessToken();
                if (!string.IsNullOrEmpty(accessToken))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync( api_url + @"/ows/UpdateAddresList", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "UpdateProfileAddressAsync",  groupName);
                        Models.OwsResponseModel owsResponse = JsonConvert.DeserializeObject<OwsResponseModel>(apiResponse);
                        return owsResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed to update address info using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "UpdateProfileAddressAsync", groupName);
                        return new Models.OwsResponseModel()
                        {
                            result = false,
                            responseMessage = response.ReasonPhrase
                        };
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed to update address info using web api due to null returned from the local web api", reservationNameID, "UpdateProfileAddressAsync",groupName);
                    return new Models.OwsResponseModel()
                    {
                        result = false,
                        responseMessage = "Local web API returned null"
                    };
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "UpdateProfileAddressAsync",  groupName);
                return new Models.OwsResponseModel()
                {
                    result = false,
                    responseMessage = "Generic Exception : " + ex.Message
                };
            }
        }
        public async Task<List<PolicyMaster>> fetchPolicyMaster(string api_url,string ActionGroup)
        {
            string functionName = "fetchPolicyMaster";
            string applicationName = "GuestPortal";
            string description = ActionGroup;

            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    
                    httpClient.Timeout = TimeSpan.FromMinutes(2);
                    var accessToken = AuthenticationHelper.GetAPIAccessToken();
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    }
                    HttpResponseMessage httpResponse = await httpClient.PostAsync(new Uri($"{api_url}/Cloud/FetchPolicyMaster"), null);
                    if (httpResponse != null && httpResponse.IsSuccessStatusCode)
                    {
                        var responseMessage = await httpResponse.Content.ReadAsStringAsync();
                        if (!string.IsNullOrEmpty(responseMessage))
                        {
                            var apiResponse = JsonConvert.DeserializeObject<APIResponseModel>(responseMessage);
                            if (apiResponse != null && apiResponse.responseData != null && apiResponse.result)
                            {
                                var policymaster = JsonConvert.DeserializeObject<List<Models.PolicyMaster>>(apiResponse.responseData.ToString());
                                return policymaster;
                            }
                            return null;
                        }
                        else
                        {
                            LogHelper.Instance.Debug("Failed to fetch the policy details from DB, since API returned NULL",
                                functionName, applicationName, description);
                            return null;
                        }
                    }
                    else
                    {
                        LogHelper.Instance.Debug($"Failed to fetch policy details from DB with the below response from API, {httpResponse.ReasonPhrase} code:- {httpResponse.StatusCode}",
                            functionName, applicationName, description);
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error(ex, functionName, applicationName, description);
                return null;
            }
        }
        public async Task<Models.APIResponseModel> PushReservationPolicies(string reservationNameID, APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("Pushing reservation policies using web api", reservationNameID, "PushReservationPolicies", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to push reservation policies using web api due to proxy error", reservationNameID, "PushReservationPolicies",groupName);
                    return new APIResponseModel()
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
                new LogHelper().Debug("web api url :- " + api_url + @"/local/PushReservationPolicy", reservationNameID, "PushReservationPolicies", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "PushReservationPolicies",groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/local/PushReservationPolicy", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "PushReservationPolicies",groupName);
                        APIResponseModel localResponse = JsonConvert.DeserializeObject<Models.APIResponseModel>(apiResponse);
                        return localResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed to push reservation policies using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "PushReservationPolicies", groupName);
                        return new APIResponseModel()
                        {
                            result = false,
                            responseMessage = response.ReasonPhrase
                        };
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed to push reservation policies using web api due to null returned from the local web api", reservationNameID, "PushReservationPolicies",  groupName);
                    return new APIResponseModel()
                    {
                        result = false,
                        responseMessage = "Local web API returned null"
                    };
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "PushReservationPolicies",  groupName);
                return new APIResponseModel()
                {
                    result = false,
                    responseMessage = "Generic Exception : " + ex.Message
                };
            }
        }

        public async Task<Models.APIResponseModel> UpsertPolicyDetails(string reservationNameID, APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("Upserting policy details using web api", reservationNameID, "UpsertPolicyDetails", groupName);
                HttpClient httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Clear();
                var accessToken = AuthenticationHelper.GetAPIAccessToken();
                if (!string.IsNullOrEmpty(accessToken))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }
                string requestString = JsonConvert.SerializeObject(localRequest, Formatting.None);
                new LogHelper().Debug("web api url :- " + api_url + @"/local/UpsertPolicyDetails", reservationNameID, "UpsertPolicyDetails", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "UpsertPolicyDetails", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/local/UpsertPolicyDetails", requestContent);
                if (response != null && response.IsSuccessStatusCode)
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "UpsertPolicyDetails", groupName);
                    return JsonConvert.DeserializeObject<Models.APIResponseModel>(apiResponse)
                        ?? new APIResponseModel() { result = false, responseMessage = "Empty response" };
                }

                return new APIResponseModel()
                {
                    result = false,
                    responseMessage = response != null ? response.ReasonPhrase : "No response"
                };
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "UpsertPolicyDetails", groupName);
                return new APIResponseModel()
                {
                    result = false,
                    responseMessage = "Generic Exception : " + ex.Message
                };
            }
        }

        public async Task<Models.APIResponseModel> InsertReservationDocuments(string reservationNameID, Models.APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("Pushing reservation document using web api", reservationNameID, "InsertReservationDocuments", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to push reservation document using web api due to proxy error", reservationNameID, "InsertReservationDocuments",groupName);
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
                new LogHelper().Debug("web api url :- " + api_url + @"/local/PushReservationDocumentDetails", reservationNameID, "InsertReservationDocuments", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "InsertReservationDocuments",  groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/local/PushReservationDocumentDetails", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "InsertReservationDocuments", groupName);
                        Models.APIResponseModel localResponse = JsonConvert.DeserializeObject<Models.APIResponseModel>(apiResponse);
                        return localResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed to push reservation documents using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "InsertReservationDocuments", groupName);
                        return new Models.APIResponseModel()
                        {
                            result = false,
                            responseMessage = response.ReasonPhrase
                        };
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed to push reservation documents using web api due to null returned from the local web api", reservationNameID, "InsertReservationDocuments","");
                    return new Models.APIResponseModel()
                    {
                        result = false,
                        responseMessage = "Local web API returned null"
                    };
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "InsertReservationDocuments",groupName);
                return new Models.APIResponseModel()
                {
                    result = false,
                    responseMessage = "Generic Exception : " + ex.Message
                };
            }
        }

        public async Task<Models.OWS.OwsResponseModel> GetRegistrationCard(string reservationNameID, Models.OWS.OwsRequestModel owsRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("Generating regcard using web api", reservationNameID, "GetRegistrationCard", groupName);
                HttpClient httpClient =  new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to generate regcard using web api due to proxy error", reservationNameID, "GetRegistrationCard", groupName);
                    return new Models.OWS.OwsResponseModel()
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
                string requestString = JsonConvert.SerializeObject(owsRequest, Formatting.None);
                new LogHelper().Debug("web api url :- " + api_url + @"/ows/GetRegCardAsBase64", reservationNameID, "GetRegistrationCard", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "GetRegistrationCard", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/ows/GetRegCardAsBase64", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "GetRegistrationCard", groupName);
                        Models.OWS.OwsResponseModel owsResponse = JsonConvert.DeserializeObject<Models.OWS.OwsResponseModel>(apiResponse);
                        return owsResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed to generate regcard using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "GetRegistrationCard", groupName);
                        return new Models.OWS.OwsResponseModel()
                        {
                            result = false,
                            responseMessage = response.ReasonPhrase
                        };
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed to generate regcard using web api due to null returned from the local web api", reservationNameID, "GetRegistrationCard",  groupName);
                    return new Models.OWS.OwsResponseModel()
                    {
                        result = false,
                        responseMessage = "Local web API returned null"
                    };
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "GetRegistrationCard", groupName);
                return new Models.OWS.OwsResponseModel()
                {
                    result = false,
                    responseMessage = "Generic Exception : " + ex.Message
                };
            }
        }
        public async Task<Models.APIResponseModel> PushUpsellpackages(string reservationNameID, Models.APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("Pushing upsell details using web api", reservationNameID, "PushUpsellpackages",groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to push upsell details using web api due to proxy error", reservationNameID, "PushUpsellpackages", groupName);
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
                new LogHelper().Debug("web api url :- " + api_url + @"/local/PushUpsellPackages", reservationNameID, "PushUpsellpackages",  groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "PushUpsellpackages",groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/local/PushUpsellPackages", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "PushUpsellpackages",groupName);
                        Models.APIResponseModel localResponse = JsonConvert.DeserializeObject<Models.APIResponseModel>(apiResponse);
                        return localResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed to push upsell details using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "PushUpsellpackages", groupName);
                        return new Models.APIResponseModel()
                        {
                            result = false,
                            responseMessage = response.ReasonPhrase
                        };
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed to push upsell details using web api due to null returned from the local web api", reservationNameID, "PushUpsellpackages", groupName);
                    return new Models.APIResponseModel()
                    {
                        result = false,
                        responseMessage = "Local web API returned null"
                    };
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "PushUpsellpackages", groupName);
                return new Models.APIResponseModel()
                {
                    result = false,
                    responseMessage = "Generic Exception : " + ex.Message
                };
            }
        }
        public async Task<Models.APIResponseModel> FetchPaymentHistory(string reservationNameID, Models.APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("Pushing payment history details using web api", reservationNameID, "FetchPaymentHistory", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to payment history details using web api due to proxy error", reservationNameID, "FetchPaymentHistory", groupName);
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
                new LogHelper().Debug("web api url :- " + api_url + @"/cloud/FetchPaymentHistory", reservationNameID, "FetchPaymentHistory", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "FetchPaymentHistory", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/cloud/FetchPaymentHistory", null);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "FetchPaymentHistory", groupName);
                        Models.APIResponseModel localResponse = JsonConvert.DeserializeObject<Models.APIResponseModel>(apiResponse);
                        return localResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed to fetch payment  history details using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "FetchPaymentHistory", groupName);
                        return new Models.APIResponseModel()
                        {
                            result = false,
                            responseMessage = response.ReasonPhrase
                        };
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed to fetch  payment history details using web api due to null returned from the local web api", reservationNameID, "FetchPaymentHistory", groupName);
                    return new Models.APIResponseModel()
                    {
                        result = false,
                        responseMessage = "Local web API returned null"
                    };
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "FetchPaymentHistory", groupName);
                return new Models.APIResponseModel()
                {
                    result = false,
                    responseMessage = "Generic Exception : " + ex.Message
                };
            }
        }

        public async Task<Models.OWS.OwsResponseModel> UpdateCardDetailsInReservationAsyn(string reservationNameID, Models.OWS.OwsRequestModel owsRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("Updating CC details info using web api", reservationNameID, "UpdateCardDetailsInReservationAsyn",  groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to update CC details info using web api due to proxy error", reservationNameID, "UpdateCardDetailsInReservationAsyn", groupName);
                    return new Models.OWS.OwsResponseModel()
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
                string requestString = JsonConvert.SerializeObject(owsRequest, Formatting.None);
                new LogHelper().Debug("web api url :- " + api_url + @"/ows/ModifyBooking", reservationNameID, "UpdateCardDetailsInReservationAsyn",  groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "UpdateCardDetailsInReservationAsyn",groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/ows/ModifyBooking", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "UpdateCardDetailsInReservationAsyn", groupName);
                        Models.OWS.OwsResponseModel owsResponse = JsonConvert.DeserializeObject<Models.OWS.OwsResponseModel>(apiResponse);
                        return owsResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed to update CC details using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "UpdateCardDetailsInReservationAsyn", groupName);
                        return new Models.OWS.OwsResponseModel()
                        {
                            result = false,
                            responseMessage = response.ReasonPhrase
                        };
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed to update CC details using web api due to null returned from the local web api", reservationNameID, "UpdateCardDetailsInReservationAsyn",groupName);
                    return new Models.OWS.OwsResponseModel()
                    {
                        result = false,
                        responseMessage = "Local web API returned null"
                    };
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "UpdateCardDetailsInReservationAsyn", groupName);
                return new Models.OWS.OwsResponseModel()
                {
                    result = false,
                    responseMessage = "Generic Exception : " + ex.Message
                };
            }
        }

        public async Task<Models.OWS.OwsResponseModel> ModifyBooking(string reservationNameID, Models.OWS.OwsRequestModel owsRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("Modifying reservatioon using web api", reservationNameID, "ModifyBooking",  groupName);
                HttpClient httpClient =new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to modify reservation using web api due to proxy error", reservationNameID, "ModifyBooking", groupName);
                    return new Models.OWS.OwsResponseModel()
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
                string requestString = JsonConvert.SerializeObject(owsRequest, Formatting.None);
                new LogHelper().Debug("web api url :- " +  api_url + @"/ows/ModifyBooking", reservationNameID, "ModifyBooking",  groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "ModifyBooking", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/ows/ModifyBooking", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "ModifyBooking",  groupName);
                        Models.OWS.OwsResponseModel owsResponse = JsonConvert.DeserializeObject<Models.OWS.OwsResponseModel>(apiResponse);
                        return owsResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed to modify reservation using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "ModifyBooking", groupName);
                        return new Models.OWS.OwsResponseModel()
                        {
                            result = false,
                            responseMessage = response.ReasonPhrase
                        };
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed to modify reservation using web api due to null returned from the local web api", reservationNameID, "ModifyBooking", groupName);
                    return new Models.OWS.OwsResponseModel()
                    {
                        result = false,
                        responseMessage = "Local web API returned null"
                    };
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "ModifyBooking", groupName);
                return new Models.OWS.OwsResponseModel()
                {
                    result = false,
                    responseMessage = "Generic Exception : " + ex.Message
                };
            }
        }

        public async Task<Models.OWS.OwsResponseModel> MakePayment(string reservationNameID, Models.OWS.OwsRequestModel owsRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("Posting payment using web api", reservationNameID, "MakePayment", groupName);
                HttpClient httpClient =  new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to post payment using web api due to proxy error", reservationNameID, "MakePayment", groupName);
                    return new Models.OWS.OwsResponseModel()
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
                string requestString = JsonConvert.SerializeObject(owsRequest, Formatting.None);
                new LogHelper().Debug("web api url :- " +  api_url + @"/ows/MakePayment", reservationNameID, "MakePayment",  groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "MakePayment",  groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/ows/MakePayment", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "MakePayment", groupName);
                        Models.OWS.OwsResponseModel owsResponse = JsonConvert.DeserializeObject<Models.OWS.OwsResponseModel>(apiResponse);
                        return owsResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed to post payment using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "MakePayment",groupName);
                        return new Models.OWS.OwsResponseModel()
                        {
                            result = false,
                            responseMessage = response.ReasonPhrase
                        };
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed to post payment using web api due to null returned from the local web api", reservationNameID, "MakePayment", groupName);
                    return new Models.OWS.OwsResponseModel()
                    {
                        result = false,
                        responseMessage = "Local web API returned null"
                    };
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "MakePayment", groupName);
                return new Models.OWS.OwsResponseModel()
                {
                    result = false,
                    responseMessage = "Generic Exception : " + ex.Message
                };
            }
        }
        public async Task<APIResponseModel> PushPaymentDetails(string reservationNameID, Models.APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("Pushing payment details using web api", reservationNameID, "PushPaymentDetails", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to push payment details using web api due to proxy error", reservationNameID, "PushPaymentDetails",  groupName);
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
                new LogHelper().Debug("web api url :- " + api_url + @"/local/PushPaymentDetails", reservationNameID, "PushPaymentDetails", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "PushPaymentDetails",  groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/local/PushPaymentDetails", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "PushPaymentDetails", groupName);
                        Models.APIResponseModel localResponse = JsonConvert.DeserializeObject<APIResponseModel>(apiResponse);
                        return localResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed to push payment details using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "PushPaymentDetails",  groupName);
                        return new APIResponseModel()
                        {
                            result = false,
                            responseMessage = response.ReasonPhrase
                        };
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed to push payment details using web api due to null returned from the local web api", reservationNameID, "PushPaymentDetails", groupName);
                    return new Models.APIResponseModel()
                    {
                        result = false,
                        responseMessage = "Local web API returned null"
                    };
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "PushPaymentDetails",  groupName);
                return new Models.APIResponseModel()
                {
                    result = false,
                    responseMessage = "Generic Exception : " + ex.Message
                };
            }
        }

        public async Task<APIResponseModel> FetchPaymentDetails(string reservationNameID, Models.APIRequestModel cloudRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("Fetching payment details using web api", reservationNameID, "FetchPaymentDetails",groupName);
             
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to fetch payment details using web api due to proxy error", reservationNameID, "FetchPaymentDetails",groupName);
                    return new APIResponseModel()
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
                string requestString = JsonConvert.SerializeObject(cloudRequest, Formatting.None);
                new LogHelper().Debug("web api url :- " + api_url + @"/local/FetchPaymentDetails", reservationNameID, "FetchPaymentDetails",groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "FetchPaymentDetails",groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/local/FetchPaymentDetails", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "FetchPaymentDetails", groupName);
                        new LogHelper().Log("web API response :- " + apiResponse, reservationNameID, "FetchPaymentDetails", groupName);
                        Models.APIResponseModel cloudResponse = JsonConvert.DeserializeObject<APIResponseModel>(apiResponse);
                        return cloudResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed to fetch payment details using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "FetchPaymentDetails",groupName);
                        return new APIResponseModel()
                        {
                            result = false,
                            responseMessage = response.ReasonPhrase
                        };
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed to fetch payment details using web api due to null returned from the local web api", reservationNameID, "FetchPaymentDetails",groupName);
                    return new Models.APIResponseModel()
                    {
                        result = false,
                        responseMessage = "Cloud web API returned null"
                    };
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "FetchPaymentDetails", groupName);
                return new Models.APIResponseModel()
                {
                    result = false,
                    responseMessage = "Generic Exception : " + ex.Message
                };
            }
        }

        public async Task<Models.OWS.OwsResponseModel> GetFolioByWindow(string reservationNameID, Models.OWS.OwsRequestModel owsRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("Fetching reservation folio by window using web api", reservationNameID, "GetFolioByWindow", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to fetch folio by window using web api due to proxy error", reservationNameID, "GetFolioByWindow",  groupName);
                    return new Models.OWS.OwsResponseModel()
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
                string requestString = JsonConvert.SerializeObject(owsRequest, Formatting.None);
                new LogHelper().Debug("web api url :- " +  api_url + @"/ows/GetGuestFolioByWindow", reservationNameID, "GetFolioByWindow", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "GetFolioByWindow", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/ows/GetGuestFolioByWindow", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "GetFolioByWindow", groupName);
                        Models.OWS.OwsResponseModel owsResponse = JsonConvert.DeserializeObject<Models.OWS.OwsResponseModel>(apiResponse);
                        return owsResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed to fetch folio by window using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "GetFolioByWindow",  groupName);
                        return new Models.OWS.OwsResponseModel()
                        {
                            result = false,
                            responseMessage = response.ReasonPhrase
                        };
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed to fetch folio by window using web api due to null returned from the local web api", reservationNameID, "GetFolioByWindow",  groupName);
                    return new Models.OWS.OwsResponseModel()
                    {
                        result = false,
                        responseMessage = "Local web API returned null"
                    };
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "GetFolioByWindow", groupName);
                return new Models.OWS.OwsResponseModel()
                {
                    result = false,
                    responseMessage = "Generic Exception : " + ex.Message
                };
            }
        }

        public async Task<Models.OWS.OwsResponseModel> GetFolio(string reservationNameID, Models.OWS.OwsRequestModel owsRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("Fetching reservation folio using web api", reservationNameID, "GetFolio", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to fetch folio using web api due to proxy error", reservationNameID, "GetFolio",groupName);
                    return new Models.OWS.OwsResponseModel()
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
                string requestString = JsonConvert.SerializeObject(owsRequest, Formatting.None);
                new LogHelper().Debug("web api url :- " + api_url + @"/ows/GetGuestFolioAsBase64", reservationNameID, "GetFolio", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "GetFolio",groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/ows/GetGuestFolioAsBase64", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "GetFolio", groupName);
                        Models.OWS.OwsResponseModel owsResponse = JsonConvert.DeserializeObject<Models.OWS.OwsResponseModel>(apiResponse);
                        return owsResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed to fetch folio using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "GetFolio", groupName);
                        return new Models.OWS.OwsResponseModel()
                        {
                            result = false,
                            responseMessage = response.ReasonPhrase
                        };
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed to fetch folio using web api due to null returned from the local web api", reservationNameID, "GetFolio", groupName);
                    return new Models.OWS.OwsResponseModel()
                    {
                        result = false,
                        responseMessage = "Local web API returned null"
                    };
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "GetFolio", groupName);
                return new Models.OWS.OwsResponseModel()
                {
                    result = false,
                    responseMessage = "Generic Exception : " + ex.Message
                };
            }
        }

        public async Task<List<Models.OWS.OperaReservation>> fetchReservationFromPMS1(OWSRequestModel owsRequest, string api_url, string Process,string ActionGroup)
        {
            string functionName = "fetchReservationFromPMS";
            string applicationName = "GuestPortal";
            string description = ActionGroup;
            if (owsRequest == null)
            {
                LogHelper.Instance.Debug($"OWS request can not be null : -7",
                    functionName, applicationName, description);
                return null;
            }
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    HttpContent requestContent = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(owsRequest), Encoding.UTF8, "application/json");
                    httpClient.Timeout = TimeSpan.FromMinutes(2);
                    var accessToken = AuthenticationHelper.GetAPIAccessToken();
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    }
                    HttpResponseMessage httpResponse = await httpClient.PostAsync(new Uri($"{api_url}/ows/FetchReservation"), requestContent);
                    if (httpResponse != null && httpResponse.IsSuccessStatusCode)
                    {
                        var responseMessage = await httpResponse.Content.ReadAsStringAsync();
                        if (!string.IsNullOrEmpty(responseMessage))
                        {
                            var apiResponse = JsonConvert.DeserializeObject<APIResponseModel>(responseMessage);
                            if (apiResponse != null && apiResponse.responseData != null && apiResponse.result)
                            {
                                if (!string.IsNullOrEmpty(Process))
                                {
                                    var checkinReservationList = new ReservationCloudLogic().verifyReservationsisAllowed(JsonConvert.DeserializeObject<List<Models.OWS.OperaReservation>>(apiResponse.responseData.ToString()), Process);
                                    if (checkinReservationList != null)
                                        return checkinReservationList;
                                    else
                                    {
                                        LogHelper.Instance.Debug($"Reservation is not valid for check in",
                                        functionName, applicationName, description);
                                        return null;
                                    }
                                }
                                else
                                {
                                    var reservationList = JsonConvert.DeserializeObject<List<Models.OWS.OperaReservation>>(apiResponse.responseData.ToString());
                                    if (reservationList != null)
                                    {
                                        LogHelper.Instance.Debug($"Reservation details : {apiResponse.responseData.ToString()}",
                                        functionName, applicationName, description);
                                        return reservationList;
                                    }
                                    else
                                    {
                                        LogHelper.Instance.Debug($"Reservation is not exists",
                                        functionName, applicationName, description);
                                        return null;
                                    }
                                }
                            }
                            else if (apiResponse != null)
                            {
                                LogHelper.Instance.Debug($"Failed to fetch the reservation details from PMS with the below response from API :- {apiResponse.responseMessage}",
                               functionName, applicationName, description);
                                return null;
                            }
                            else
                            {
                                LogHelper.Instance.Debug($"Failed to fetch the reservation details from PMS, since API returned NULL",
                                functionName, applicationName, description);
                                return null;
                            }
                        }
                        else
                        {
                            LogHelper.Instance.Debug("Failed to fetch the reservation details from PMS, since API returned NULL",
                                functionName, applicationName, description);
                            return null;
                        }
                    }
                    else
                    {
                        LogHelper.Instance.Debug($"Failed to fetch reservation details from PMS with the below response from API, {httpResponse.ReasonPhrase} code:- {httpResponse.StatusCode}",
                            functionName, applicationName, description);
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error(ex, functionName, applicationName, description);
                return null;
            }
        }

        public async Task<Models.APIResponseModel> FetchReservationDetailsByReferenceNumber(string reservationNameID, Models.APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("Fetching Reservation  details using web api", reservationNameID, "FetchReservationDetailsByReferenceNumber", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Fetching reservation details  using web api due to proxy error", reservationNameID, "FetchReservationDetailsByReferenceNumber", groupName);
                    return new Models.APIResponseModel()
                    {
                        result = false,
                        responseMessage = "Failed to generate the proxy http client"
                    };
                }
                httpClient.Timeout = TimeSpan.FromMinutes(5);
                httpClient.DefaultRequestHeaders.Clear();
                var accessToken = AuthenticationHelper.GetAPIAccessToken();
                if (!string.IsNullOrEmpty(accessToken))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }
                string requestString = JsonConvert.SerializeObject(localRequest, Formatting.None);
                new LogHelper().Debug("web api url :- " + api_url + @"/cloud/FetchReservationDetailsByReferenceNumber", reservationNameID, "FetchReservationDetailsByReferenceNumber", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "FetchReservationDetailsByReferenceNumber", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/cloud/FetchReservationDetailsByReferenceNumber", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("FetchReservationDetailsByReferenceNumber web API response :- " + apiResponse, reservationNameID, "FetchReservationDetailsByReferenceNumber", groupName);
                        Models.APIResponseModel localResponse = JsonConvert.DeserializeObject<Models.APIResponseModel>(apiResponse);
                        return localResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed to fetch reservation details using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "FetchReservationDetailsByReferenceNumber", groupName);
                        return new Models.APIResponseModel()
                        {
                            result = false,
                            responseMessage = response.ReasonPhrase
                        };
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed to fetch reservation details using web api due to null returned from the local web api", reservationNameID, "FetchReservationDetailsByReferenceNumber", groupName);
                    return new Models.APIResponseModel()
                    {
                        result = false,
                        responseMessage = "Local web API returned null"
                    };
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "FetchReservationDetailsByReferenceNumber", groupName);
                return new Models.APIResponseModel()
                {
                    result = false,
                    responseMessage = "Generic Exception : " + ex.Message
                };
            }
        }
        public async Task<List<DataAccess.tbCountryMaster>> fetchcountryMaster(string api_url, string ActionGroup)
        {
            string functionName = "fetchcountryMaster";
            string applicationName = "GuestPortal";
            string description = ActionGroup;

            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    httpClient.Timeout = TimeSpan.FromMinutes(2);
                    var accessToken = AuthenticationHelper.GetAPIAccessToken();
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    }
                    HttpResponseMessage httpResponse = await httpClient.PostAsync(new Uri($"{api_url}/Cloud/GetCountry"), null);
                    if (httpResponse != null && httpResponse.IsSuccessStatusCode)
                    {
                        var responseMessage = await httpResponse.Content.ReadAsStringAsync();
                        if (!string.IsNullOrEmpty(responseMessage))
                        {
                            var apiResponse = JsonConvert.DeserializeObject<APIResponseModel>(responseMessage);
                            if (apiResponse != null && apiResponse.responseData != null && apiResponse.result)
                            {
                                var policymaster = JsonConvert.DeserializeObject<List<DataAccess.tbCountryMaster>>(apiResponse.responseData.ToString());
                                return policymaster;
                            }
                            return null;
                        }
                        else
                        {
                            LogHelper.Instance.Debug("Failed to fetch the country details from DB, since API returned NULL",
                                functionName, applicationName, description);
                            return null;
                        }
                    }
                    else
                    {
                        LogHelper.Instance.Debug($"Failed to fetch country details from DB with the below response from API, {httpResponse.ReasonPhrase} code:- {httpResponse.StatusCode}",
                            functionName, applicationName, description);
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error(ex, functionName, applicationName, description);
                return null;
            }
        }

        public async Task<List<DataAccess.tbStateMaster>> fetchstateMaster(Models.APIRequestModel localRequest,string api_url,string ActionGroup)
        {
            string functionName = "fetchstateMaster";
            string applicationName = "GuestPortal";
            string description = ActionGroup;

            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    httpClient.Timeout = TimeSpan.FromMinutes(2);
                    HttpContent requestContent = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(localRequest), Encoding.UTF8, "application/json");
                    httpClient.Timeout = TimeSpan.FromMinutes(2);
                    var accessToken = AuthenticationHelper.GetAPIAccessToken();
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    }
                    HttpResponseMessage httpResponse = await httpClient.PostAsync(new Uri($"{api_url}/Cloud/GetState"), requestContent);
                   
                    if (httpResponse != null && httpResponse.IsSuccessStatusCode)
                    {
                        var responseMessage = await httpResponse.Content.ReadAsStringAsync();
                        if (!string.IsNullOrEmpty(responseMessage))
                        {
                            var apiResponse = JsonConvert.DeserializeObject<APIResponseModel>(responseMessage);
                            if (apiResponse != null && apiResponse.responseData != null && apiResponse.result)
                            {
                                var policymaster = JsonConvert.DeserializeObject<List<DataAccess.tbStateMaster>>(apiResponse.responseData.ToString());
                                return policymaster;
                            }
                            return null;
                        }
                        else
                        {
                            LogHelper.Instance.Debug("Failed to fetch the state details from PMS, since API returned NULL",
                                functionName, applicationName, description);
                            return null;
                        }
                    }
                    else
                    {
                        LogHelper.Instance.Debug($"Failed to fetch state details from PMS with the below response from API, {httpResponse.ReasonPhrase} code:- {httpResponse.StatusCode}",
                            functionName, applicationName, description);
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error(ex, functionName, applicationName, description);
                return null;
            }
        }
        public async Task<List<PaymentTypeMasterModel>> fetchPaymentTypeMaster(string api_url, string ActionGroup)
        {
            string functionName = "fetchPaymentTypeMaster";
            string applicationName = "GuestPortal";
            string description = ActionGroup;

            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    var accessToken = AuthenticationHelper.GetAPIAccessToken();
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    }
                    httpClient.Timeout = TimeSpan.FromMinutes(2);
                    HttpResponseMessage httpResponse = await httpClient.PostAsync(new Uri($"{api_url}/Payment/FetchPaymentTypeMaster"), null);
                    if (httpResponse != null && httpResponse.IsSuccessStatusCode)
                    {
                        var responseMessage = await httpResponse.Content.ReadAsStringAsync();
                        if (!string.IsNullOrEmpty(responseMessage))
                        {
                            var apiResponse = JsonConvert.DeserializeObject<APIResponseModel>(responseMessage);
                            if (apiResponse != null && apiResponse.responseData != null && apiResponse.result)
                            {
                                var policymaster = JsonConvert.DeserializeObject<List<Models.PaymentTypeMasterModel>>(apiResponse.responseData.ToString());
                                return policymaster;
                            }
                            return null;
                        }
                        else
                        {
                            LogHelper.Instance.Debug("Failed to fetch the payment master details from DB, since API returned NULL",
                                functionName, applicationName, description);
                            return null;
                        }
                    }
                    else
                    {
                        LogHelper.Instance.Debug($"Failed to fetch payment master details from DB with the below response from API, {httpResponse.ReasonPhrase} code:- {httpResponse.StatusCode}",
                            functionName, applicationName, description);
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error(ex, functionName, applicationName, description);
                return null;
            }
        }

        public async Task<Models.APIResponseModel> PushReservationTrackLocally(string reservationNameID, Models.APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("Pushing reservation track in local DB using web api", reservationNameID, "PushReservationTrackLocally", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to push reservation track in local DB using web api due to proxy error", reservationNameID, "PushReservationTrackLocally",groupName);
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
                new LogHelper().Debug("web api url :- " + api_url + @"/local/PushReservationTrackDetailStatus", reservationNameID, "PushReservationTrackLocally", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "PushReservationTrackLocally", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/local/PushReservationTrackDetailStatus", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "PushReservationTrackLocally", groupName);
                        Models.APIResponseModel localResponse = JsonConvert.DeserializeObject<Models.APIResponseModel>(apiResponse);
                        return localResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed to push reservation track in local DB using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "PushReservationTrackLocally", groupName);
                        return new Models.APIResponseModel()
                        {
                            result = false,
                            responseMessage = response.ReasonPhrase
                        };
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed to push reservation track in local DB using web api due to null returned from the local web api", reservationNameID, "PushReservationTrackLocally", groupName);
                    return new Models.APIResponseModel()
                    {
                        result = false,
                        responseMessage = "Local web API returned null"
                    };
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "PushReservationTrackLocally", groupName);
                return new Models.APIResponseModel()
                {
                    result = false,
                    responseMessage = "Generic Exception : " + ex.Message
                };
            }
        }
        public async Task<Models.APIResponseModel> UpdateReservationStatus(string reservationNameID, Models.APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("Updating reservation status in local DB using web api", reservationNameID, "UpdateReservationStatus",  groupName);
                HttpClient httpClient =new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to update reservation status in local DB using web api due to proxy error", reservationNameID, "UpdateReservationStatus", groupName);
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
                new LogHelper().Debug("web api url :- " + api_url + @"/Cloud/UpdateReservationDetails", reservationNameID, "UpdateReservationStatus", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "UpdateReservationStatus", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/Cloud/UpdateReservationStatus", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "UpdateReservationStatus",  groupName);
                        Models.APIResponseModel localResponse = JsonConvert.DeserializeObject<Models.APIResponseModel>(apiResponse);
                        return localResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed to update reservation status in local DB using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "UpdateReservationStatus", groupName);
                        return new Models.APIResponseModel()
                        {
                            result = false,
                            responseMessage = response.ReasonPhrase
                        };
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed to update reservation status in local DB using web api due to null returned from the local web api", reservationNameID, "UpdateReservationStatus", groupName);
                    return new Models.APIResponseModel()
                    {
                        result = false,
                        responseMessage = "Local web API returned null"
                    };
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "UpdateReservationStatus", groupName);
                return new Models.APIResponseModel()
                {
                    result = false,
                    responseMessage = "Generic Exception : " + ex.Message
                };
            }
        }
        public async Task<Models.OWS.OwsResponseModel> UpdateGuestProfile(string reservationNameID, Models.OWS.OwsRequestModel owsRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("Updating guest profile using web api", reservationNameID, "UpdateGuestProfile",groupName);
                HttpClient httpClient =new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to update guest profile using web api due to proxy error", reservationNameID, "UpdateGuestProfile", groupName);
                    return new Models.OWS.OwsResponseModel()
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
                string requestString = JsonConvert.SerializeObject(owsRequest, Formatting.None);
                new LogHelper().Debug("web api url :- " + api_url + @"/ows/UpdateName", reservationNameID, "UpdateGuestProfile",groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "UpdateGuestProfile", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/ows/UpdateName", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "UpdateGuestProfile",  groupName);
                        Models.OWS.OwsResponseModel owsResponse = JsonConvert.DeserializeObject<Models.OWS.OwsResponseModel>(apiResponse);
                        return owsResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed to update profile using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "UpdateGuestProfile", groupName);
                        return new Models.OWS.OwsResponseModel()
                        {
                            result = false,
                            responseMessage = response.ReasonPhrase
                        };
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed to update profile using web api due to null returned from the local web api", reservationNameID, "UpdateGuestProfile",  groupName);
                    return new Models.OWS.OwsResponseModel()
                    {
                        result = false,
                        responseMessage = "Local web API returned null"
                    };
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "UpdateGuestProfile",  groupName);
                return new Models.OWS.OwsResponseModel()
                {
                    result = false,
                    responseMessage = "Generic Exception : " + ex.Message
                };
            }
        }

        public async Task<Models.OWS.OwsResponseModel> InsertGuestComment(string reservationNameID, Models.OWS.OwsRequestModel owsRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("Inserting guest comment using web api", reservationNameID, "InsertGuestComment", groupName);
                HttpClient httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Clear();
                var accessToken = AuthenticationHelper.GetAPIAccessToken();
                if (!string.IsNullOrEmpty(accessToken))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }
                string requestString = JsonConvert.SerializeObject(owsRequest, Formatting.None);
                new LogHelper().Debug("web api url :- " + api_url + @"/ows/InsertGuestComment", reservationNameID, "InsertGuestComment", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "InsertGuestComment", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/ows/InsertGuestComment", requestContent);
                if (response != null && response.IsSuccessStatusCode)
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "InsertGuestComment", groupName);
                    return JsonConvert.DeserializeObject<Models.OWS.OwsResponseModel>(apiResponse);
                }
                new LogHelper().Debug("Failed to insert guest comment : " + (response != null ? response.ReasonPhrase : "null"), reservationNameID, "InsertGuestComment", groupName);
                return new Models.OWS.OwsResponseModel()
                {
                    result = false,
                    responseMessage = response != null ? response.ReasonPhrase : "No response"
                };
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "InsertGuestComment", groupName);
                return new Models.OWS.OwsResponseModel()
                {
                    result = false,
                    responseMessage = "Generic Exception : " + ex.Message
                };
            }
        }

        public async Task<Models.OWS.OwsResponseModel> UpdateGuestPassport(string reservationNameID, Models.OWS.OwsRequestModel owsRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("Updating guest passport using web api", reservationNameID, "UpdateGuestProfile", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to update guest passport using web api due to proxy error", reservationNameID, "UpdateGuestPassport", groupName);
                    return new Models.OWS.OwsResponseModel()
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
                owsRequest.RequestIdentifier = reservationNameID;
                string requestString = JsonConvert.SerializeObject(owsRequest, Formatting.None);
                new LogHelper().Debug("web api url :- " + api_url + @"/ows/UpdatePassport", reservationNameID, "UpdateGuestPassport", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "UpdateGuestPassport", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/ows/UpdatePassport", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "UpdateGuestPassport", groupName);
                        Models.OWS.OwsResponseModel owsResponse = JsonConvert.DeserializeObject<Models.OWS.OwsResponseModel>(apiResponse);
                        return owsResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed to update passport using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "UpdateGuestPassport", groupName);
                        return new Models.OWS.OwsResponseModel()
                        {
                            result = false,
                            responseMessage = response.ReasonPhrase
                        };
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed to update passport using web api due to null returned from the local web api", reservationNameID, "UpdateGuestPassport", groupName);
                    return new Models.OWS.OwsResponseModel()
                    {
                        result = false,
                        responseMessage = "Local web API returned null"
                    };
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "UpdateGuestPassport", groupName);
                return new Models.OWS.OwsResponseModel()
                {
                    result = false,
                    responseMessage = "Generic Exception : " + ex.Message
                };
            }
        }


        public async Task<APIResponseModel> InsertDocuments(string reservationNameID, APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("Pushing profile documents using web api", reservationNameID, "InsertDocuments",groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to push profile documents using web api due to proxy error", reservationNameID, "InsertDocuments",groupName);
                    return new APIResponseModel()
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
                new LogHelper().Debug("web api url :- " + api_url + @"/local/PushDocumentDetails", reservationNameID, "InsertDocuments", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "InsertDocuments", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/local/PushDocumentDetails", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "InsertDocuments",groupName);
                        Models.APIResponseModel localResponse = JsonConvert.DeserializeObject<Models.APIResponseModel>(apiResponse);
                        return localResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed to push profile documents using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "InsertDocuments",groupName);
                        return new Models.APIResponseModel()
                        {
                            result = false,
                            responseMessage = response.ReasonPhrase
                        };
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed to push profile documents using web api due to null returned from the local web api", reservationNameID, "InsertDocuments",groupName);
                    return new Models.APIResponseModel()
                    {
                        result = false,
                        responseMessage = "Local web API returned null"
                    };
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "InsertDocuments", groupName);
                return new Models.APIResponseModel()
                {
                    result = false,
                    responseMessage = "Generic Exception : " + ex.Message
                };
            }
        }
        public async Task<APIResponseModel> PushReservationAdditionalDetails(string reservationNameID, APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("Pushing reservation additional details using web api", reservationNameID, "PushReservationAdditionalDetails", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to push reservation additional details using web api due to proxy error", reservationNameID, "PushReservationAdditionalDetails",groupName);
                    return new APIResponseModel()
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
                new LogHelper().Debug("web api url :- " + api_url + @"/local/PushReservationAdditionalDetails", reservationNameID, "PushReservationAdditionalDetails",groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "PushReservationAdditionalDetails",groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/local/PushReservationAdditionalDetails", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "PushReservationAdditionalDetails",  groupName);
                     APIResponseModel localResponse = JsonConvert.DeserializeObject<APIResponseModel>(apiResponse);
                        return localResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed to push reservation additional details using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "PushReservationAdditionalDetails", groupName);
                        return new Models.APIResponseModel()
                        {
                            result = false,
                            responseMessage = response.ReasonPhrase
                        };
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed to push reservation additional details using web api due to null returned from the local web api", reservationNameID, "PushReservationAdditionalDetails", groupName);
                    return new Models.APIResponseModel()
                    {
                        result = false,
                        responseMessage = "Local web API returned null"
                    };
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "PushReservationPolicies", groupName);
                return new APIResponseModel()
                {
                    result = false,
                    responseMessage = "Generic Exception : " + ex.Message
                };
            }
        }
        public async Task<Models.Emails.EmailResponse> SendEmail(string reservationNameID, Models.Emails.EmailRequest emailRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("Sending email using web api", reservationNameID, "SendEmail",groupName);
               

                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to send email using web api due to proxy error", reservationNameID, "SendEmail",groupName);
                    return new Models.Emails.EmailResponse()
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
                string requestString = JsonConvert.SerializeObject(emailRequest, Formatting.None);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                new LogHelper().Debug("web api url :- " + api_url + @"/email/SendEmail", reservationNameID, "SendEmail",  groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "SendEmail", groupName);
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/email/SendEmail", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "SendEmail",  groupName);
                        Models.Emails.EmailResponse emailResponse = JsonConvert.DeserializeObject<Models.Emails.EmailResponse>(apiResponse);
                        return emailResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed to send email using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "SendEmail",  groupName);
                        return new Models.Emails.EmailResponse()
                        {
                            result = false,
                            responseMessage = response.ReasonPhrase
                        };
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed to send email using web api due to null returned from the local web api", reservationNameID, "SendEmail", groupName);
                    return new Models.Emails.EmailResponse()
                    {
                        result = false,
                        responseMessage = "Email web API returned null"
                    };
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "SendEmail",  groupName);
                return new Models.Emails.EmailResponse()
                {
                    result = false,
                    responseMessage = "Generic Exception : " + ex.Message
                };
            }
        }

        public async Task<Models.OWS.OwsResponseModel> CheckoutReservation(string reservationNameID, Models.OWS.OwsRequestModel owsRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("Checking out reservation using web api", reservationNameID, "CheckoutReservation", groupName);


                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to check-out using web api due to proxy error", reservationNameID, "CheckoutReservation", groupName);
                    return new Models.OWS.OwsResponseModel()
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
                string requestString = JsonConvert.SerializeObject(owsRequest, Formatting.None);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                new LogHelper().Debug("web api url :- " + api_url + @"/ows/GuestCheckOut", reservationNameID, "CheckoutReservation",  groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "CheckoutReservation", groupName);
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/ows/GuestCheckOut", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "CheckoutReservation", groupName);
                        Models.OWS.OwsResponseModel owsResponse = JsonConvert.DeserializeObject<Models.OWS.OwsResponseModel>(apiResponse);
                        return owsResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed to check-out using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "CheckoutReservation", groupName);
                        return new Models.OWS.OwsResponseModel()
                        {
                            result = false,
                            responseMessage = response.ReasonPhrase
                        };
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed to check-out using web api due to null returned from the local web api", reservationNameID, "CheckoutReservation", groupName);
                    return new Models.OWS.OwsResponseModel()
                    {
                        result = false,
                        responseMessage = "Local web API returned null"
                    };
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "CheckoutReservation", groupName);
                return new Models.OWS.OwsResponseModel()
                {
                    result = false,
                    responseMessage = "Generic Exception : " + ex.Message
                };
            }
        }
        public async Task<Models.OWS.OwsResponseModel> CreateAccompanyingProfile(string reservationNameID, Models.OWS.OwsRequestModel owsRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("Create accompanying profile using web api", reservationNameID, "CreateAccompanyingProfile", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to Create accompanying profile using web api due to proxy error", reservationNameID, "CreateAccompanyingProfile", groupName);
                    return new Models.OWS.OwsResponseModel()
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
                string requestString = JsonConvert.SerializeObject(owsRequest, Formatting.None);
                new LogHelper().Debug("web api url :- " + api_url + @"/ows/CreateAccompanyingGuset", reservationNameID, "CreateAccompanyingProfile", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "CreateAccompanyingProfile", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/ows/CreateAccompanyingGuset", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "CreateAccompanyingProfile", groupName);
                        Models.OWS.OwsResponseModel owsResponse = JsonConvert.DeserializeObject<Models.OWS.OwsResponseModel>(apiResponse);
                        return owsResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed to create accompanying profile using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "CreateAccompanyingProfile",  groupName);
                        return new Models.OWS.OwsResponseModel()
                        {
                            result = false,
                            responseMessage = response.ReasonPhrase
                        };
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed to create accompanying profile using web api due to null returned from the local web api", reservationNameID, "CreateAccompanyingProfile",  groupName);
                    return new Models.OWS.OwsResponseModel()
                    {
                        result = false,
                        responseMessage = "Local web API returned null"
                    };
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "CreateAccompanyingProfile",  groupName);
                return new Models.OWS.OwsResponseModel()
                {
                    result = false,
                    responseMessage = "Generic Exception : " + ex.Message
                };
            }
        }
        public async Task<APIResponseModel> InsertGuestProfile(string reservationNameID, APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("Pushing profile documents using web api", reservationNameID, "InsertDocuments", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to push profile documents using web api due to proxy error", reservationNameID, "InsertDocuments", groupName);
                    return new APIResponseModel()
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
                new LogHelper().Debug("web api url :- " + api_url + @"/local/PushProfileDetails", reservationNameID, "InsertDocuments", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "InsertGuestProfile", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/local/PushProfileDetails", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "InsertDocuments", groupName);
                        Models.APIResponseModel localResponse = JsonConvert.DeserializeObject<Models.APIResponseModel>(apiResponse);
                        return localResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed to push profile documents using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "InsertDocuments", groupName);
                        return new Models.APIResponseModel()
                        {
                            result = false,
                            responseMessage = response.ReasonPhrase
                        };
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed to push profile documents using web api due to null returned from the local web api", reservationNameID, "InsertDocuments", groupName);
                    return new Models.APIResponseModel()
                    {
                        result = false,
                        responseMessage = "Local web API returned null"
                    };
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "InsertDocuments", groupName);
                return new Models.APIResponseModel()
                {
                    result = false,
                    responseMessage = "Generic Exception : " + ex.Message
                };
            }
        }

        public async Task<APIResponseModel> PushPaymentHistoryDetails(string reservationNameID, Models.APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("Pushing payment details using web api", reservationNameID, "PushPaymentDetails", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to push payment details using web api due to proxy error", reservationNameID, "PushPaymentDetails", groupName);
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
                new LogHelper().Debug("web api url :- " + api_url + @"/local/PushPaymentHistoryDetails", reservationNameID, "PushPaymentHistoryDetails", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "PushPaymentHistoryDetails", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/local/PushPaymentHistoryDetails", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "PushPaymentDetails", groupName);
                        Models.APIResponseModel localResponse = JsonConvert.DeserializeObject<APIResponseModel>(apiResponse);
                        return localResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed to push payment history details using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "PushPaymentDetails", groupName);
                        return new APIResponseModel()
                        {
                            result = false,
                            responseMessage = response.ReasonPhrase
                        };
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed to push  payment history details using web api due to null returned from the local web api", reservationNameID, "PushPaymentDetails", groupName);
                    return new Models.APIResponseModel()
                    {
                        result = false,
                        responseMessage = "Local web API returned null"
                    };
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "PushPaymentDetails", groupName);
                return new Models.APIResponseModel()
                {
                    result = false,
                    responseMessage = "Generic Exception : " + ex.Message
                };
            }
        }
        public async Task<APIResponseModel> PushSearchedReservation(string reservationNameID, Models.APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("Pushing reservation details using web api", reservationNameID, "PushSearchedReservation", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to push reservation details using web api due to proxy error", reservationNameID, "PushSearchedReservation", groupName);
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
                new LogHelper().Debug("web api url :- " + api_url + @"/localService/PushCloudSearchedReservation", reservationNameID, "PushSearchedReservation", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "PushSearchedReservation", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/localService/PushCloudSearchedReservation", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "PushSearchedReservation", groupName);
                        Models.APIResponseModel localResponse = JsonConvert.DeserializeObject<Models.APIResponseModel>(apiResponse);
                        return localResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed to push reservation details using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "PushSearchedReservation", groupName);
                        return new Models.APIResponseModel()
                        {
                            result = false,
                            responseMessage = response.ReasonPhrase
                        };
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed to push reservation details using web api due to null returned from the local web api", reservationNameID, "PushSearchedReservation", groupName);
                    return new Models.APIResponseModel()
                    {
                        result = false,
                        responseMessage = "Local web API returned null"
                    };
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "PushSearchedReservation", groupName);
                return new Models.APIResponseModel()
                {
                    result = false,
                    responseMessage = "Generic Exception : " + ex.Message
                };
            }
        }

        public async Task<CheckinPortal.Models.Whatsapp.WhatsAppResponse> SendWhatsappMsg(string reservationNameID, Models.Whatsapp.WhatsAppTemplateRequest emailRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("Sending whatsapp using web api", reservationNameID, "SendWhatsappMsg", groupName);


                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to send whatsapp using web api due to proxy error", reservationNameID, "SendWhatsappMsg", groupName);
                    return new Models.Whatsapp.WhatsAppResponse()
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
                string requestString = JsonConvert.SerializeObject(emailRequest, Formatting.None);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                new LogHelper().Debug("web api url :- " + api_url + @"/SMSGateWay/SendWhatsappTemplate", reservationNameID, "SendWhatsappMsg", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "SendWhatsappMsg", groupName);
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/SMSGateWay/SendWhatsappTemplate", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "SendWhatsappMsg", groupName);
                        Models.Whatsapp.WhatsAppResponse emailResponse = JsonConvert.DeserializeObject<Models.Whatsapp.WhatsAppResponse>(apiResponse);
                        return emailResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed to send whatsapp using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "SendWhatsappMsg", groupName);
                        return new Models.Whatsapp.WhatsAppResponse()
                        {
                            result = false,
                            responseMessage = response.ReasonPhrase
                        };
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed to send whatsapp using web api due to null returned from the local web api", reservationNameID, "SendWhatsappMsg", groupName);
                    return new Models.Whatsapp.WhatsAppResponse()
                    {
                        result = false,
                        responseMessage = "whatsapp web API returned null"
                    };
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "SendWhatsappMsg", groupName);
                return new Models.Whatsapp.WhatsAppResponse()
                {
                    result = false,
                    responseMessage = "Generic Exception : " + ex.Message
                };
            }
        }


        public async Task<APIResponseModel> GetReservationProfileList(string reservationNameID, Models.APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("Get Reservation Profile List using web api", reservationNameID, "GetReservationProfileList", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to Get Reservation Profile List using web api due to proxy error", reservationNameID, "GetReservationProfileList", groupName);
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
                new LogHelper().Debug("web api url :- " + api_url + @"/local/GetReservationProfileList", reservationNameID, "GetReservationProfileList", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "GetReservationProfileList", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/local/GetReservationProfileList", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "GetReservationProfileList", groupName);
                        Models.APIResponseModel localResponse = JsonConvert.DeserializeObject<Models.APIResponseModel>(apiResponse);
                        return localResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed GetReservationProfileList using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "GetReservationProfileList", groupName);
                        return new Models.APIResponseModel()
                        {
                            result = false,
                            responseMessage = response.ReasonPhrase
                        };
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed GetReservationProfileList using web api due to null returned from the local web api", reservationNameID, "GetReservationProfileList", groupName);
                    return new Models.APIResponseModel()
                    {
                        result = false,
                        responseMessage = "Local web API returned null"
                    };
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "GetReservationProfileList", groupName);
                return new Models.APIResponseModel()
                {
                    result = false,
                    responseMessage = "Generic Exception : " + ex.Message
                };
            }
        }

        public async Task<APIResponseModel> GetQuestions(string reservationNameID, Models.APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("Get Questions using web api", reservationNameID, "GetQuestions", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to Get Questions using web api due to proxy error", reservationNameID, "GetQuestions", groupName);
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
                new LogHelper().Debug("web api url :- " + api_url + @"/local/GetQuestions", reservationNameID, "GetQuestions", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "GetQuestions", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/local/GetQuestions", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "GetQuestions", groupName);
                        Models.APIResponseModel localResponse = JsonConvert.DeserializeObject<Models.APIResponseModel>(apiResponse);
                        return localResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed Questions using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "GetQuestions", groupName);
                        return new Models.APIResponseModel()
                        {
                            result = false,
                            responseMessage = response.ReasonPhrase
                        };
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed Questions using web api due to null returned from the local web api", reservationNameID, "PushSearchedReservation", groupName);
                    return new Models.APIResponseModel()
                    {
                        result = false,
                        responseMessage = "Local web API returned null"
                    };
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "PushSearchedReservation", groupName);
                return new Models.APIResponseModel()
                {
                    result = false,
                    responseMessage = "Generic Exception : " + ex.Message
                };
            }
        }

        public async Task<APIResponseModel> GetPackages(string reservationNameID, Models.APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("GetPackages Profile List using web api", reservationNameID, "GetPackages", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to GetPackages Profile List using web api due to proxy error", reservationNameID, "GetPackages", groupName);
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
                new LogHelper().Debug("web api url :- " + api_url + @"/local/GetPackages", reservationNameID, "GetPackages", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "GetPackages", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/local/GetPackages", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "GetPackages", groupName);
                        Models.APIResponseModel localResponse = JsonConvert.DeserializeObject<Models.APIResponseModel>(apiResponse);
                        return localResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed GetPackages using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "GetPackages", groupName);
                        return new Models.APIResponseModel()
                        {
                            result = false,
                            responseMessage = response.ReasonPhrase
                        };
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed GetPackages using web api due to null returned from the local web api", reservationNameID, "GetPackages", groupName);
                    return new Models.APIResponseModel()
                    {
                        result = false,
                        responseMessage = "Local web API returned null"
                    };
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "GetPackages", groupName);
                return new Models.APIResponseModel()
                {
                    result = false,
                    responseMessage = "Generic Exception : " + ex.Message
                };
            }
        }

        public async Task<bool> UpdateReservationByStage(string reservationNameID, Models.APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("Update Reservation By Stage using web api", reservationNameID, "UpdateReservationByStage", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed Update ReservationByStage using web api due to proxy error", reservationNameID, "UpdateReservationByStage", groupName);
                    return false;
                }
                httpClient.DefaultRequestHeaders.Clear();
                var accessToken = AuthenticationHelper.GetAPIAccessToken();
                if (!string.IsNullOrEmpty(accessToken))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }
                string requestString = JsonConvert.SerializeObject(localRequest, Formatting.None);
                new LogHelper().Debug("web api url :- " + api_url + @"/local/UpdateReservationByStage", reservationNameID, "UpdateReservationByStage", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "UpdateReservationByStage", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/local/UpdateReservationByStage", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse =
                            await response.Content.ReadAsStringAsync();

                        new LogHelper().Debug(
                            "web API response :- " + apiResponse,
                            reservationNameID,
                            "UpdateReservationByStage",
                            groupName
                        );

                        bool result =
                            JsonConvert.DeserializeObject<bool>(apiResponse);

                        return result;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed UpdateReservationByStage using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "UpdateReservationByStage", groupName);
                        return false;
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed UpdateReservationByStage using web api due to null returned from the local web api", reservationNameID, "UpdateReservationByStage", groupName);
                    return  false;
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "UpdateReservationByStage", groupName);
                return false;
            }
        }
        public async Task<bool> InsertFeedback(string reservationNameID, Models.APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("UpdateInsertFeedback using web api", reservationNameID, "InsertFeedback", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed InsertFeedback using web api due to proxy error", reservationNameID, "InsertFeedback", groupName);
                    return false;
                }
                httpClient.DefaultRequestHeaders.Clear();
                var accessToken = AuthenticationHelper.GetAPIAccessToken();
                if (!string.IsNullOrEmpty(accessToken))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }
                string requestString = JsonConvert.SerializeObject(localRequest, Formatting.None);
                new LogHelper().Debug("web api url :- " + api_url + @"/local/InsertFeedback", reservationNameID, "InsertFeedback", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "InsertFeedback", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/local/InsertFeedback", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "InsertFeedback", groupName);
                        bool result =
                            JsonConvert.DeserializeObject<bool>(apiResponse);

                        return result;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed InsertFeedback using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "InsertFeedback", groupName);
                        return false;
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed InsertFeedback using web api due to null returned from the local web api", reservationNameID, "InsertFeedback", groupName);
                    return false;
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "InsertFeedback", groupName);
                return false;
            }
        }
        public async Task<bool> InsertEvent(string reservationNameID, Models.APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("Insert Event using web api", reservationNameID, "InsertEvent", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed Inser tEvent using web api due to proxy error", reservationNameID, "InsertEvent", groupName);
                    return false;
                }
                httpClient.DefaultRequestHeaders.Clear();
                var accessToken = AuthenticationHelper.GetAPIAccessToken();
                if (!string.IsNullOrEmpty(accessToken))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }
                string requestString = JsonConvert.SerializeObject(localRequest, Formatting.None);
                new LogHelper().Debug("web api url :- " + api_url + @"/local/InsertEvent", reservationNameID, "InsertEvent", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "InsertEvent", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/local/InsertEvent", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "InsertEvent", groupName);
                        bool result =
                            JsonConvert.DeserializeObject<bool>(apiResponse);

                        return result;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed InsertEvent using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "InsertEvent", groupName);
                        return false;
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed InsertEvent using web api due to null returned from the local web api", reservationNameID, "InsertEvent", groupName);
                    return false;
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "InsertEvent", groupName);
                return false;
            }
        }
        public async Task<bool> InsertReservationPackageDetails(string reservationNameID, Models.APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("Insert ReservationPackageDetails using web api", reservationNameID, "InsertReservationPackageDetails", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed Insert ReservationPackageDetails using web api due to proxy error", reservationNameID, "InsertReservationPackageDetails", groupName);
                    return false;
                }
                httpClient.DefaultRequestHeaders.Clear();
                var accessToken = AuthenticationHelper.GetAPIAccessToken();
                if (!string.IsNullOrEmpty(accessToken))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }
                string requestString = JsonConvert.SerializeObject(localRequest, Formatting.None);
                new LogHelper().Debug("web api url :- " + api_url + @"/local/InsertReservationPackageDetails", reservationNameID, "InsertReservationPackageDetails", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "InsertReservationPackageDetails", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/local/InsertReservationPackageDetails", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "InsertReservationPackageDetails", groupName);
                        bool result =
                            JsonConvert.DeserializeObject<bool>(apiResponse);

                        return result;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed to Insert Reservation Package Details using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "InsertReservationPackageDetails", groupName);
                        return false;
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed to Insert Reservation Package Details using web api due to null returned from the local web api", reservationNameID, "InsertReservationPackageDetails", groupName);
                    return false;
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "InsertReservationPackageDetails", groupName);
                return false;
            }
        }

        public async Task<bool> UpdateCheckoutFlag(string reservationNameID, Models.APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("Update CheckoutFlag using web api", reservationNameID, "UpdateCheckoutFlag", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed UpdateCheckoutFlag using web api due to proxy error", reservationNameID, "UpdateCheckoutFlag", groupName);
                    return false;
                }
                httpClient.DefaultRequestHeaders.Clear();
                var accessToken = AuthenticationHelper.GetAPIAccessToken();
                if (!string.IsNullOrEmpty(accessToken))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }
                string requestString = JsonConvert.SerializeObject(localRequest, Formatting.None);
                new LogHelper().Debug("web api url :- " + api_url + @"/local/UpdateCheckoutFlag", reservationNameID, "UpdateCheckoutFlag", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "UpdateCheckoutFlag", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/local/UpdateCheckoutFlag", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "UpdateCheckoutFlag", groupName);
                        bool result =
                            JsonConvert.DeserializeObject<bool>(apiResponse);

                        return result;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed UpdateCheckoutFlag using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "UpdateCheckoutFlag", groupName);
                        return false;
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed UpdateCheckoutFlag using web api due to null returned from the local web api", reservationNameID, "UpdateCheckoutFlag", groupName);
                    return false;
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "UpdateCheckoutFlag", groupName);
                return false;
            }
        }


        public async Task<bool> UpdatePaymentHeaderData(string reservationNameID, Models.APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("UpdatePaymentHeaderData using web api", reservationNameID, "UpdatePaymentHeaderData", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed UpdatePaymentHeaderData using web api due to proxy error", reservationNameID, "UpdatePaymentHeaderData", groupName);
                    return false;
                }
                httpClient.DefaultRequestHeaders.Clear();
                var accessToken = AuthenticationHelper.GetAPIAccessToken();
                if (!string.IsNullOrEmpty(accessToken))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }
                string requestString = JsonConvert.SerializeObject(localRequest, Formatting.None);
                new LogHelper().Debug("web api url :- " + api_url + @"/local/UpdatePaymentHeaderData", reservationNameID, "UpdatePaymentHeaderData", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "UpdatePaymentHeaderData", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/local/UpdatePaymentHeaderData", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "UpdatePaymentHeaderData", groupName);
                        bool result =
                             JsonConvert.DeserializeObject<bool>(apiResponse);

                        return result;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed UpdatePaymentHeaderData using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "UpdatePaymentHeaderData", groupName);
                        return false;
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed UpdatePaymentHeaderData using web api due to null returned from the local web api", reservationNameID, "UpdatePaymentHeaderData", groupName);
                    return false;
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "UpdatePaymentHeaderData", groupName);
                return false;
            }
        }

        public async Task<APIResponseModel> GetActivePaymentTransctions(string reservationNameID, Models.APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("GetActivePaymentTransctions using web api", reservationNameID, "GetActivePaymentTransctions", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to GetActivePaymentTransctions using web api due to proxy error", reservationNameID, "GetActivePaymentTransctions", groupName);
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
                new LogHelper().Debug("web api url :- " + api_url + @"/local/GetActivePaymentTransctions", reservationNameID, "GetActivePaymentTransctions", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "GetActivePaymentTransctions", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/local/GetActivePaymentTransctions", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "GetActivePaymentTransctions", groupName);
                        Models.APIResponseModel localResponse = JsonConvert.DeserializeObject<Models.APIResponseModel>(apiResponse);
                        return localResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed GetActivePaymentTransctions using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "GetActivePaymentTransctions", groupName);
                        return new Models.APIResponseModel()
                        {
                            result = false,
                            responseMessage = response.ReasonPhrase
                        };
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed GetActivePaymentTransctions using web api due to null returned from the local web api", reservationNameID, "GetActivePaymentTransctions", groupName);
                    return new Models.APIResponseModel()
                    {
                        result = false,
                        responseMessage = "Local web API returned null"
                    };
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "GetActivePaymentTransctions", groupName);
                return new Models.APIResponseModel()
                {
                    result = false,
                    responseMessage = "Generic Exception : " + ex.Message
                };
            }
        }
        public async Task<bool> InsertPaymentData(string reservationNameID, Models.APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("InsertPaymentData using web api", reservationNameID, "InsertPaymentData", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed InsertPaymentData using web api due to proxy error", reservationNameID, "InsertPaymentData", groupName);
                    return false;
                }
                httpClient.DefaultRequestHeaders.Clear();
                var accessToken = AuthenticationHelper.GetAPIAccessToken();
                if (!string.IsNullOrEmpty(accessToken))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }
                string requestString = JsonConvert.SerializeObject(localRequest, Formatting.None);
                new LogHelper().Debug("web api url :- " + api_url + @"/local/InsertPaymentData", reservationNameID, "InsertPaymentData", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "InsertPaymentData", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/local/InsertPaymentData", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "InsertPaymentData", groupName);
                        bool result =
                            JsonConvert.DeserializeObject<bool>(apiResponse);

                        return result;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed InsertPaymentData using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "InsertPaymentData", groupName);
                        return false;
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed InsertPaymentData using web api due to null returned from the local web api", reservationNameID, "InsertPaymentData", groupName);
                    return false;
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "InsertPaymentData", groupName);
                return false;
            }
        }
        public async Task<bool> UpdatePrimaryGuestEmail(string reservationNameID, Models.APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("UpdatePrimaryGuestEmail using web api", reservationNameID, "UpdatePrimaryGuestEmail", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed UpdatePrimaryGuestEmail using web api due to proxy error", reservationNameID, "UpdatePrimaryGuestEmail", groupName);
                    return false;
                }
                httpClient.DefaultRequestHeaders.Clear();
                var accessToken = AuthenticationHelper.GetAPIAccessToken();
                if (!string.IsNullOrEmpty(accessToken))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }
                string requestString = JsonConvert.SerializeObject(localRequest, Formatting.None);
                new LogHelper().Debug("web api url :- " + api_url + @"/local/UpdatePrimaryGuestEmail", reservationNameID, "UpdatePrimaryGuestEmail", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "UpdatePrimaryGuestEmail", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/local/UpdatePrimaryGuestEmail", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "UpdatePrimaryGuestEmail", groupName);
                        bool result =
                            JsonConvert.DeserializeObject<bool>(apiResponse);

                        return result;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed UpdatePrimaryGuestEmail using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "UpdatePrimaryGuestEmail", groupName);
                        return false;
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed UpdatePrimaryGuestEmail using web api due to null returned from the local web api", reservationNameID, "UpdatePrimaryGuestEmail", groupName);
                    return false;
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "UpdatePrimaryGuestEmail", groupName);
                return false;
            }
        }
        public async Task<APIResponseModel> GetReservationPackages(string reservationNameID, Models.APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("GetReservationPackages using web api", reservationNameID, "GetReservationPackages", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to GetReservationPackages using web api due to proxy error", reservationNameID, "GetReservationPackages", groupName);
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
                new LogHelper().Debug("web api url :- " + api_url + @"/local/GetReservationPackages", reservationNameID, "GetReservationPackages", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "GetReservationPackages", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/local/GetReservationPackages", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "GetReservationPackages", groupName);
                        Models.APIResponseModel localResponse = JsonConvert.DeserializeObject<Models.APIResponseModel>(apiResponse);
                        return localResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed GetReservationPackages using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "GetReservationPackages", groupName);
                        return new Models.APIResponseModel()
                        {
                            result = false,
                            responseMessage = response.ReasonPhrase
                        };
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed GetReservationPackages using web api due to null returned from the local web api", reservationNameID, "GetReservationPackages", groupName);
                    return new Models.APIResponseModel()
                    {
                        result = false,
                        responseMessage = "Local web API returned null"
                    };
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "GetActivePaymentTransctions", groupName);
                return new Models.APIResponseModel()
                {
                    result = false,
                    responseMessage = "Generic Exception : " + ex.Message
                };
            }
        }
        public async Task<DataTable> GetReservationByReservationID(string reservationNameID, Models.APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("GetReservationByReservationID using web api", reservationNameID, "GetReservationByReservationID", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to GetReservationByReservationID using web api due to proxy error", reservationNameID, "GetReservationByReservationID", groupName);
                    return null;
                }
                httpClient.DefaultRequestHeaders.Clear();
                var accessToken = AuthenticationHelper.GetAPIAccessToken();
                if (!string.IsNullOrEmpty(accessToken))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }
                string requestString = JsonConvert.SerializeObject(localRequest, Formatting.None);
                new LogHelper().Debug("web api url :- " + api_url + @"/local/GetReservationByReservationID", reservationNameID, "GetReservationByReservationID", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "GetReservationByReservationID", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/local/GetReservationByReservationID", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "GetReservationByReservationID", groupName);
                        //DataTable localResponse = JsonConvert.DeserializeObject<DataTable>(apiResponse);
                        var jsonObject = JObject.Parse(apiResponse);

                        DataTable localResponse =
                            JsonConvert.DeserializeObject<DataTable>(
                                jsonObject["responseData"].ToString()
                            );
                        return localResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed GetReservationByReservationID using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "GetReservationByReservationID", groupName);
                        return null;
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed GetReservationByReservationID using web api due to null returned from the local web api", reservationNameID, "GetReservationByReservationID", groupName);
                    return null;
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "GetActivePaymentTransctions", groupName);
                return null;
            }
        }
        public async Task<string> GetLastEvetIDByReservation(string reservationNameID, Models.APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("GetLastEvetIDByReservation using web api", reservationNameID, "GetLastEvetIDByReservation", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed GetLastEvetIDByReservation using web api due to proxy error", reservationNameID, "GetLastEvetIDByReservation", groupName);
                    return null;
                }
                httpClient.DefaultRequestHeaders.Clear();
                var accessToken = AuthenticationHelper.GetAPIAccessToken();
                if (!string.IsNullOrEmpty(accessToken))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }
                string requestString = JsonConvert.SerializeObject(localRequest, Formatting.None);
                new LogHelper().Debug("web api url :- " + api_url + @"/local/GetLastEvetIDByReservation", reservationNameID, "GetLastEvetIDByReservation", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "GetLastEvetIDByReservation", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/local/GetLastEvetIDByReservation", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "GetLastEvetIDByReservation", groupName);
                        Models.APIResponseModel localResponse = JsonConvert.DeserializeObject<Models.APIResponseModel>(apiResponse);
                        return localResponse.responseData.ToString();
                    }
                    else
                    {
                        new LogHelper().Debug("Failed GetLastEvetIDByReservation using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "GetLastEvetIDByReservation", groupName);
                        return "";
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed GetLastEvetIDByReservation using web api due to null returned from the local web api", reservationNameID, "GetLastEvetIDByReservation", groupName);
                    return null;
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "GetLastEvetIDByReservation", groupName);
                return null;
            }
        }

        public async Task<DataTable> CheckDuplicateDocumentByReservationID(string reservationNameID, Models.APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("CheckDuplicateDocumentByReservationID using web api", reservationNameID, "CheckDuplicateDocumentByReservationID", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to CheckDuplicateDocumentByReservationID using web api due to proxy error", reservationNameID, "CheckDuplicateDocumentByReservationID", groupName);
                    return null;
                }
                httpClient.DefaultRequestHeaders.Clear();
                var accessToken = AuthenticationHelper.GetAPIAccessToken();
                if (!string.IsNullOrEmpty(accessToken))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }
                string requestString = JsonConvert.SerializeObject(localRequest, Formatting.None);
                new LogHelper().Debug("web api url :- " + api_url + @"/local/CheckDuplicateDocumentByReservationID", reservationNameID, "CheckDuplicateDocumentByReservationID", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "CheckDuplicateDocumentByReservationID", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/local/CheckDuplicateDocumentByReservationID", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "CheckDuplicateDocumentByReservationID", groupName);
                        //DataTable localResponse = JsonConvert.DeserializeObject<DataTable>(apiResponse);                        

                        var jsonObject = JObject.Parse(apiResponse);

                        DataTable localResponse =
                            JsonConvert.DeserializeObject<DataTable>(
                                jsonObject["responseData"].ToString()
                            );
                        return localResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed to CheckDuplicateDocumentByReservationID using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "CheckDuplicateDocumentByReservationID", groupName);
                        return null;
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed to CheckDuplicateDocumentByReservationID using web api due to null returned from the local web api", reservationNameID, "CheckDuplicateDocumentByReservationID", groupName);
                    return null;
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "CheckDuplicateDocumentByReservationID", groupName);
                return null;
            }
        }

      
        public async Task<DataTable> GetCountryList(string reservationNameID, Models.APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("GetCountryList using web api", reservationNameID, "GetCountryList", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to GetCountryList using web api due to proxy error", reservationNameID, "GetCountryList", groupName);
                    return null;
                }
                httpClient.DefaultRequestHeaders.Clear();
                var accessToken = AuthenticationHelper.GetAPIAccessToken();
                if (!string.IsNullOrEmpty(accessToken))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }
                string requestString = JsonConvert.SerializeObject(localRequest, Formatting.None);
                new LogHelper().Debug("web api url :- " + api_url + @"/local/GetCountryList", reservationNameID, "GetCountryList", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "GetCountryList", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/local/GetCountryList", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "GetCountryList", groupName);
                        //DataTable localResponse = JsonConvert.DeserializeObject<DataTable>(apiResponse);
                        var jsonObject = JObject.Parse(apiResponse);

                        DataTable localResponse =
                            JsonConvert.DeserializeObject<DataTable>(
                                jsonObject["responseData"].ToString()
                            );
                        return localResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed GetCountryList using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "GetCountryList", groupName);
                        return null;
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed GetCountryList using web api due to null returned from the local web api", reservationNameID, "GetCountryList", groupName);
                    return null;
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "GetActivePaymentTransctions", groupName);
                return null;
            }
        }

        public async Task<DataTable> GetStateList(string reservationNameID, Models.APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("GetStateList using web api", reservationNameID, "GetStateList", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to GetStateList using web api due to proxy error", reservationNameID, "GetStateList", groupName);
                    return null;
                }
                httpClient.DefaultRequestHeaders.Clear();
                var accessToken = AuthenticationHelper.GetAPIAccessToken();
                if (!string.IsNullOrEmpty(accessToken))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }
                string requestString = JsonConvert.SerializeObject(localRequest, Formatting.None);
                new LogHelper().Debug("web api url :- " + api_url + @"/local/GetStateList", reservationNameID, "GetStateList", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "GetStateList", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/local/GetStateList", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "GetStateList", groupName);
                        //DataTable localResponse = JsonConvert.DeserializeObject<DataTable>(apiResponse);
                        var jsonObject = JObject.Parse(apiResponse);

                        DataTable localResponse =
                            JsonConvert.DeserializeObject<DataTable>(
                                jsonObject["responseData"].ToString()
                            );
                        return localResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed GetStateList using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "GetStateList", groupName);
                        return null;
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed GetStateList using web api due to null returned from the local web api", reservationNameID, "GetStateList", groupName);
                    return null;
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "GetActivePaymentTransctions", groupName);
                return null;
            }
        }

        public async Task<List<Models.StateMaster>> GetStateListByCountryID(string reservationNameID, Models.APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("GetStateListByCountryID using web api", reservationNameID, "GetStateListByCountryID", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to GetStateListByCountryIDt using web api due to proxy error", reservationNameID, "GetStateListByCountryID", groupName);
                    return null;
                }
                httpClient.DefaultRequestHeaders.Clear();
                var accessToken = AuthenticationHelper.GetAPIAccessToken();
                if (!string.IsNullOrEmpty(accessToken))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }
                string requestString = JsonConvert.SerializeObject(localRequest, Formatting.None);
                new LogHelper().Debug("web api url :- " + api_url + @"/local/GetStateListByCountryID", reservationNameID, "GetStateListByCountryID", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "GetStateListByCountryID", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/local/GetStateListByCountryID", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "GetStateListByCountryID", groupName);
                        //DataTable localResponse = JsonConvert.DeserializeObject<DataTable>(apiResponse);
                        var apiResult =
    JsonConvert.DeserializeObject<APIResponseModel>(apiResponse);

                        if (apiResult != null && apiResult.result && apiResult.responseData != null)
                        {
                            return JsonConvert.DeserializeObject<List<Models.StateMaster>>(
                                JsonConvert.SerializeObject(apiResult.responseData)
                            );
                        }
                        
                        return null;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed GetStateListByCountryID using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "GetStateListByCountryID", groupName);
                        return null;
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed GetStateListByCountryID using web api due to null returned from the local web api", reservationNameID, "GetStateListByCountryID", groupName);
                    return null;
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "GetActivePaymentTransctions", groupName);
                return null;
            }
        }

        public async Task<DataTable> validateDocumentIssueCountry(string reservationNameID, Models.APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("validateDocumentIssueCountry using web api", reservationNameID, "validateDocumentIssueCountry", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to validateDocumentIssueCountry using web api due to proxy error", reservationNameID, "validateDocumentIssueCountry", groupName);
                    return null;
                }
                httpClient.DefaultRequestHeaders.Clear();
                var accessToken = AuthenticationHelper.GetAPIAccessToken();
                if (!string.IsNullOrEmpty(accessToken))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }
                string requestString = JsonConvert.SerializeObject(localRequest, Formatting.None);
                new LogHelper().Debug("web api url :- " + api_url + @"/local/validateDocumentIssueCountry", reservationNameID, "validateDocumentIssueCountry", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "validateDocumentIssueCountry", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/local/validateDocumentIssueCountry", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "validateDocumentIssueCountry", groupName);
                        // DataTable localResponse = JsonConvert.DeserializeObject<DataTable>(apiResponse);
                        var jsonObject = JObject.Parse(apiResponse);

                        DataTable localResponse =
                            JsonConvert.DeserializeObject<DataTable>(
                                jsonObject["responseData"].ToString()
                            );

                        return localResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed validateDocumentIssueCountry using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "validateDocumentIssueCountry", groupName);
                        return null;
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed validateDocumentIssueCountry using web api due to null returned from the local web api", reservationNameID, "validateDocumentIssueCountry", groupName);
                    return null;
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "GetActivePaymentTransctions", groupName);
                return null;
            }
        }

        public async Task<APIResponseModel> GetDocumentList(string reservationNameID, Models.APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("Get GetDocumentList  using web api", reservationNameID, "GetDocumentList", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to GetDocumentList using web api due to proxy error", reservationNameID, "GetDocumentList", groupName);
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
                new LogHelper().Debug("web api url :- " + api_url + @"/local/GetDocumentList", reservationNameID, "GetDocumentList", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "GetDocumentList", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/local/GetDocumentList", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "GetDocumentList", groupName);
                        Models.APIResponseModel localResponse = JsonConvert.DeserializeObject<Models.APIResponseModel>(apiResponse);
                        return localResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed to GetDocumentList using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "GetDocumentList", groupName);
                        return new Models.APIResponseModel()
                        {
                            result = false,
                            responseMessage = response.ReasonPhrase
                        };
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed GetDocumentList using web api due to null returned from the local web api", reservationNameID, "GetDocumentList", groupName);
                    return new Models.APIResponseModel()
                    {
                        result = false,
                        responseMessage = "Local web API returned null"
                    };
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "GetDocumentList", groupName);
                return new Models.APIResponseModel()
                {
                    result = false,
                    responseMessage = "Generic Exception : " + ex.Message
                };
            }
        }

        public async Task<APIResponseModel> GetReservationDocumenttype(string reservationNameID, Models.APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("GetReservationDocumenttype using web api Request: "+ JsonConvert.SerializeObject(localRequest), reservationNameID, "GetReservationDocumentBytype", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to GetReservationDocumenttype using web api due to proxy error", reservationNameID, "GetReservationDocumentBytype", groupName);
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
                new LogHelper().Debug("web api url :- " + api_url + @"/local/GetReservationDocumentBytype", reservationNameID, "GetReservationDocumentBytype", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "GetReservationDocumentBytype", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/local/GetReservationDocumentBytype", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "GetReservationDocumentBytype", groupName);
                        Models.APIResponseModel localResponse = JsonConvert.DeserializeObject<Models.APIResponseModel>(apiResponse);
                        return localResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed GetReservationDocumenttype using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "GetReservationDocumentBytype", groupName);
                        return new Models.APIResponseModel()
                        {
                            result = false,
                            responseMessage = response.ReasonPhrase
                        };
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed GetReservationDocumenttype using web api due to null returned from the local web api", reservationNameID, "GetReservationDocumenttype", groupName);
                    return new Models.APIResponseModel()
                    {
                        result = false,
                        responseMessage = "Local web API returned null"
                    };
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "GetActivePaymentTransctions", groupName);
                return new Models.APIResponseModel()
                {
                    result = false,
                    responseMessage = "Generic Exception : " + ex.Message
                };
            }
        }

        public async Task<DataTable> SaveTransactionHistory(string reservationNameID, Models.APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("SaveTransactionHistory using web api", reservationNameID, "SaveTransactionHistory", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to SaveTransactionHistory List using web api due to proxy error", reservationNameID, "SaveTransactionHistory", groupName);
                    return null;
                }
                httpClient.DefaultRequestHeaders.Clear();
                var accessToken = AuthenticationHelper.GetAPIAccessToken();
                if (!string.IsNullOrEmpty(accessToken))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }
                string requestString = JsonConvert.SerializeObject(localRequest, Formatting.None);
                new LogHelper().Debug("web api url :- " + api_url + @"/local/SaveTransactionHistory", reservationNameID, "SaveTransactionHistory", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "SaveTransactionHistory", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/local/SaveTransactionHistory", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "SaveTransactionHistory", groupName);
                        //DataTable localResponse = JsonConvert.DeserializeObject<DataTable>(apiResponse);
                        var jsonObject = JObject.Parse(apiResponse);

                        DataTable localResponse =
                            JsonConvert.DeserializeObject<DataTable>(
                                jsonObject["responseData"].ToString()
                            );
                        return localResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed SaveTransactionHistory using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "SaveTransactionHistory", groupName);
                        return null;
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed SaveTransactionHistory using web api due to null returned from the local web api", reservationNameID, "SaveTransactionHistory", groupName);
                    return null;
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "GetActivePaymentTransctions", groupName);
                return null;
            }
        }

        public async Task<DataTable> GetPaymentHistory(string reservationNameID, Models.APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("GetPaymentHistory using web api", reservationNameID, "GetPaymentHistory", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to GetPaymentHistory using web api due to proxy error", reservationNameID, "GetPaymentHistory", groupName);
                    return null;
                }
                httpClient.DefaultRequestHeaders.Clear();
                var accessToken = AuthenticationHelper.GetAPIAccessToken();
                if (!string.IsNullOrEmpty(accessToken))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }
                string requestString = JsonConvert.SerializeObject(localRequest, Formatting.None);
                new LogHelper().Debug("web api url :- " + api_url + @"/local/GetPaymentHistory", reservationNameID, "GetPaymentHistory", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "GetPaymentHistory", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/local/GetPaymentHistory", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "GetPaymentHistory", groupName);
                        //DataTable localResponse = JsonConvert.DeserializeObject<DataTable>(apiResponse);
                        var jsonObject = JObject.Parse(apiResponse);

                        DataTable localResponse =
                            JsonConvert.DeserializeObject<DataTable>(
                                jsonObject["responseData"].ToString()
                            );
                        return localResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed GetPaymentHistory using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "GetPaymentHistory", groupName);
                        return null;
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed GetPaymentHistory using web api due to null returned from the local web api", reservationNameID, "GetPaymentHistory", groupName);
                    return null;
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "GetActivePaymentTransctions", groupName);
                return null;
            }
        }
        public async Task<bool> UpdateReservationPaymentStatus(string reservationNameID, Models.APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("UpdateReservationPaymentStatus using web api", reservationNameID, "UpdateReservationPaymentStatus", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed UpdateReservationPaymentStatus using web api due to proxy error", reservationNameID, "UpdateReservationPaymentStatus", groupName);
                    return false;
                }
                httpClient.DefaultRequestHeaders.Clear();
                var accessToken = AuthenticationHelper.GetAPIAccessToken();
                if (!string.IsNullOrEmpty(accessToken))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }
                string requestString = JsonConvert.SerializeObject(localRequest, Formatting.None);
                new LogHelper().Debug("web api url :- " + api_url + @"/local/UpdateReservationPaymentStatus", reservationNameID, "UpdateReservationPaymentStatus", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "UpdateReservationPaymentStatus", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/local/UpdateReservationPaymentStatus", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "UpdateReservationPaymentStatus", groupName);
                        bool result =
                             JsonConvert.DeserializeObject<bool>(apiResponse);

                        return result;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed UpdateReservationPaymentStatus using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "UpdateReservationPaymentStatus", groupName);
                        return false;
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed UpdateReservationPaymentStatus using web api due to null returned from the local web api", reservationNameID, "UpdateReservationPaymentStatus", groupName);
                    return false;
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "UpdateReservationPaymentStatus", groupName);
                return false;
            }
        }

        public async Task<APIResponseModel> CaptureTransaction(string reservationNameID, Models.APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("CaptureTransaction using web api", reservationNameID, "CaptureTransaction", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to CaptureTransaction using web api due to proxy error", reservationNameID, "CaptureTransaction", groupName);
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
                new LogHelper().Debug("web api url :- " + api_url + @"/local/CaptureTransaction", reservationNameID, "CaptureTransaction", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "CaptureTransaction", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/local/CaptureTransaction", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "CaptureTransaction", groupName);
                        Models.APIResponseModel localResponse = JsonConvert.DeserializeObject<Models.APIResponseModel>(apiResponse);
                        return localResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed CaptureTransaction using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "CaptureTransaction", groupName);
                        return new Models.APIResponseModel()
                        {
                            result = false,
                            responseMessage = response.ReasonPhrase
                        };
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed CaptureTransaction using web api due to null returned from the local web api", reservationNameID, "CaptureTransaction", groupName);
                    return new Models.APIResponseModel()
                    {
                        result = false,
                        responseMessage = "Local web API returned null"
                    };
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "GetActivePaymentTransctions", groupName);
                return new Models.APIResponseModel()
                {
                    result = false,
                    responseMessage = "Generic Exception : " + ex.Message
                };
            }
        }

        public async Task<APIResponseModel> TopupTransaction(string reservationNameID, Models.APIRequestModel localRequest, string groupName, string api_url)
        {
            try
            {
                new LogHelper().Debug("Get TopupTransaction using web api using web api", reservationNameID, "TopupTransaction", groupName);
                HttpClient httpClient = new HttpClient();
                if (httpClient == null)
                {
                    new LogHelper().Debug("Failed to TopupTransaction using web api using web api due to proxy error", reservationNameID, "TopupTransaction", groupName);
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
                new LogHelper().Debug("web api url :- " + api_url + @"/local/TopupTransaction", reservationNameID, "TopupTransaction", groupName);
                new LogHelper().Debug("web api request :- " + requestString, reservationNameID, "TopupTransaction", groupName);
                var requestContent = new StringContent(requestString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(api_url + @"/local/TopupTransaction", requestContent);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        new LogHelper().Debug("web API response :- " + apiResponse, reservationNameID, "TopupTransaction", groupName);
                        Models.APIResponseModel localResponse = JsonConvert.DeserializeObject<Models.APIResponseModel>(apiResponse);
                        return localResponse;
                    }
                    else
                    {
                        new LogHelper().Debug("Failed to TopupTransaction using web api due to HTTP error : " + response.ReasonPhrase, reservationNameID, "TopupTransaction", groupName);
                        return new Models.APIResponseModel()
                        {
                            result = false,
                            responseMessage = response.ReasonPhrase
                        };
                    }
                }
                else
                {
                    new LogHelper().Debug("Failed to TopupTransaction using web api due to null returned from the local web api", reservationNameID, "TopupTransaction", groupName);
                    return new Models.APIResponseModel()
                    {
                        result = false,
                        responseMessage = "Local web API returned null"
                    };
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameID, "GetActivePaymentTransctions", groupName);
                return new Models.APIResponseModel()
                {
                    result = false,
                    responseMessage = "Generic Exception : " + ex.Message
                };
            }
        }

    }
}