using CheckinPortal.BackOffice.Helpers;
using CheckinPortal.DataAccess;
using CheckinPortal.Helpers;
using CheckinPortal.Models;
using CheckinPortal.Models.AdaptorAPIModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace CheckinPortal.BusinessLayer
{
    public class PaymentLogics
    {

        public PaymentLogics()
        {

        }
        public async Task<DataTable> SaveTransactionHistory(InsertPaymentHistoryUspModel model)
        {
            string ActionGroup = "Reservation";

            try
            {
               APIRequestModel _APIRequestModel = new APIRequestModel();
                var sendGuestRequest = new APIRequestModel()
                {
                    RequestObject = model
                };
                _APIRequestModel = sendGuestRequest;
                var TransactionHistory = await new CloudHelper().SaveTransactionHistory("", _APIRequestModel, ActionGroup, AppSettingsManager.GetDecryptedSetting("APIBaseUrl"));
                if (TransactionHistory == null)
                {
                    new LogHelper().Debug("Failed to Save Transaction History using web api due to HTTP error : ", "", "SaveTransactionHistory", ActionGroup);
                    return null;

                }
                else
                {
                    return TransactionHistory;
                }
            }
            catch
            {
                return null;
            }
           
        }

        public async Task<DataTable> GetPaymentHistory(string ReservationNumber)
        {

            
            string ActionGroup = "Reservation";

            try
            {
                APIRequestModel _APIRequestModel = new APIRequestModel();
                var sendGuestRequest = new APIRequestModel()
                {
                    RequestObject = new
                    {
                        ReservationNumber = ReservationNumber,

                    },
                };
                _APIRequestModel = sendGuestRequest;
                var TransactionHistory = await new CloudHelper().GetPaymentHistory("", _APIRequestModel, ActionGroup, AppSettingsManager.GetDecryptedSetting("APIBaseUrl"));
                if (TransactionHistory == null)
                {
                    new LogHelper().Debug("Failed to Get Payment History using web api due to HTTP error : ", "", "GetPaymentHistory", ActionGroup);
                    return null;

                }
                else
                {
                    return TransactionHistory;
                }
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> UpdateReservationPaymentStatus(string ReservationNumber)
        {
            string ActionGroup = "Reservation";
            try
            {
                APIRequestModel _APIRequestModel = new APIRequestModel();
                var sendGuestRequest = new APIRequestModel()
                {
                    RequestObject = new
                    {
                        ReservationNumber = ReservationNumber,

                    },
                };
                _APIRequestModel = sendGuestRequest;
                var localResponse = await new CloudHelper().UpdateReservationPaymentStatus("", _APIRequestModel, ActionGroup, AppSettingsManager.GetDecryptedSetting("APIBaseUrl"));
                if (!localResponse)
                {
                    new LogHelper().Debug("Failed to InsertFeedback using web api due to HTTP error : " + localResponse, ReservationNumber, "InsertFeedback", ActionGroup);
                    //return null;

                }
                else
                {
                    new LogHelper().Debug("Successfully Inserted Feedback : " + localResponse, ReservationNumber, "InsertFeedback", ActionGroup);
                }
                return localResponse;
            }
            catch
            {
                return false;
            }
            
            
        }


        public async Task<MakePaymentResponseModel> CaptureTransaction(TopupTransctionModels model)
        {
            MakePaymentResponseModel CaptTranscitons = new MakePaymentResponseModel();
            string ActionGroup = "Reservation";

            try
            {
                APIRequestModel _APIRequestModel = new APIRequestModel();
                var sendGuestRequest = new APIRequestModel()
                {
                    RequestObject = model
                };
                _APIRequestModel = sendGuestRequest;
                var localResponse = await new CloudHelper().CaptureTransaction("", _APIRequestModel, ActionGroup, AppSettingsManager.GetDecryptedSetting("APIBaseUrl"));
                if (!localResponse.result)
                {
                    new LogHelper().Debug("Failed to CaptureTransaction using web api due to HTTP error : " + localResponse.statusCode, model.AmountToCharge.ToString(), "CaptureTransaction", ActionGroup);
                    return null;

                }
                else
                {
                    string apiResponse = localResponse.responseData.ToString();
                    new LogHelper().Debug("web API response :- " + apiResponse, model.AmountToCharge.ToString(), "MakePaymentResponseModel", ActionGroup);
                    CaptTranscitons = JsonConvert.DeserializeObject<MakePaymentResponseModel>(apiResponse);
                }
            }
            catch
            {

            }
            return CaptTranscitons;
           
            
        }

        public async Task<MakePaymentResponseModel> TopupTransaction(TopupTransctionModels model)
        {
            MakePaymentResponseModel TopupTransactn = new MakePaymentResponseModel();
            string ActionGroup = "Reservation";

            try
            {
                APIRequestModel _APIRequestModel = new APIRequestModel();
                var sendGuestRequest = new APIRequestModel()
                {
                    RequestObject = model
                };
                _APIRequestModel = sendGuestRequest;
                var localResponse = await new CloudHelper().TopupTransaction("", _APIRequestModel, ActionGroup, AppSettingsManager.GetDecryptedSetting("APIBaseUrl"));
                if (!localResponse.result)
                {
                    new LogHelper().Debug("Failed to TopupTransaction using web api due to HTTP error : " + localResponse.statusCode, model.AmountToCharge.ToString(), "TopupTransaction", ActionGroup);
                    return null;

                }
                else
                {
                    string apiResponse = localResponse.responseData.ToString();
                    new LogHelper().Debug("web API response :- " + apiResponse, model.AmountToCharge.ToString(), "TopupTransaction", ActionGroup);
                    TopupTransactn = JsonConvert.DeserializeObject<MakePaymentResponseModel>(apiResponse);
                }
            }
            catch
            {

            }
            return TopupTransactn;
        }
    }
}