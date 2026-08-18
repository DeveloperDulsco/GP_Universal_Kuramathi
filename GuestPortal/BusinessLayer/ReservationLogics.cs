using CheckinPortal.BackOffice.Helpers;
using CheckinPortal.DataAccess;
using CheckinPortal.Helpers;
using CheckinPortal.Models;
using CheckinPortal.Models.AdaptorAPIModels;
using CheckinPortal.Models.OWS;
using CheckinPortal.Models.PaymentDetails;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Data;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Results;

namespace CheckinPortal.BusinessLayer
{
    public class ReservationLogics
    {
        public int ReservationID { get; set; }
        public ReservationLogics()
        {
            //var ConnectionString = AppSettingsManager.GetDecryptedSetting("ConnectionString");
            //Helpers.SQLHelpers.Instance.SetConnectionString(ConnectionString);

        }

        //public async Task<DataTable> GetReservationDetailsDT(string ReservationNumber)
        //{
        //    var reservationsDt = await new CloudHelper().FetchReservationDetailsByReferenceNumber(ReservationNumber, new APIRequestModel { RequestObject = ReservationNumber }, "", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());
        //    var reservations = new CloudReservationModel();
        //    if (reservationsDt != null)
        //    {
        //        reservations = JsonConvert.DeserializeObject<List<CloudReservationModel>>(reservationsDt.responseData.ToString()).FirstOrDefault();

        //    }
        //    DataTable transaction = new DataTable();
        //    try
        //    {
        //        string Query = string.Empty;

        //        SqlParameter reservationNoParameter = new SqlParameter()
        //        {
        //            ParameterName = "@ReferenceNumber",
        //            Value = ReservationNumber,
        //            SqlDbType = SqlDbType.VarChar
        //        };

        //        transaction = SQLHelpers.Instance.ExecuteSP("Usp_GetReservationCloudDetailsByReferenceNumber", reservationNoParameter);
        //    }
        //    catch (Exception ex)
        //    {
        //        //Helpers.EvenLogHelper.Instance.LogError($"Unhandled Exception while executing SP Usp_FetchTopOneRecordsforProcessing {ex.ToString()}");
        //    }
        //    return transaction;
        //}




        public async Task<List<Usp_GetProfileInformationByReservationID_Result>> GetReservationProfileList(int ReservationID)
        {
            string ActionGroup = "GetReservationProfileList";
            List<Usp_GetProfileInformationByReservationID_Result> Profiles = new List<DataAccess.Usp_GetProfileInformationByReservationID_Result>();
            try
            {
                APIRequestModel _APIRequestModel = new APIRequestModel();
                var sendGuestRequest = new APIRequestModel()
                {
                    RequestObject = new
                    {
                        ReservationID = ReservationID,
                        
                    },
                };
                _APIRequestModel = sendGuestRequest;
                var localResponse = await new CloudHelper().GetReservationProfileList("", _APIRequestModel, ActionGroup, AppSettingsManager.GetDecryptedSetting("APIBaseUrl"));
                if (!localResponse.result)
                {
                    new LogHelper().Debug("Failed to get profiles using web api due to HTTP error : " + localResponse.statusCode, ReservationID.ToString(), "GetReservationProfileList", ActionGroup);
                    return null;

                }
                else
                {
                    string apiResponse =  localResponse.responseData.ToString();
                    new LogHelper().Debug("web API response :- " + apiResponse, ReservationID.ToString(), "GetReservationProfileList", ActionGroup);
                    Profiles = JsonConvert.DeserializeObject<List<Usp_GetProfileInformationByReservationID_Result>>(apiResponse);
                }
               
                
            }
            catch (Exception ex)
            {

            }
            return Profiles;

        }

        public async Task<List<Models.Questions>> GetQuestions()
        {
            string ActionGroup = "GetQuestions";
            List<Models.Questions> Questions = new List<Models.Questions>();
            try
            {
                APIRequestModel _APIRequestModel = new APIRequestModel();
                var sendGuestRequest = new APIRequestModel()
                {
                    RequestObject = new
                    {
                        ReservationID = ReservationID,

                    },
                };
                _APIRequestModel = sendGuestRequest;
                var localResponse = await new CloudHelper().GetQuestions("", _APIRequestModel, ActionGroup, AppSettingsManager.GetDecryptedSetting("APIBaseUrl"));
                if (!localResponse.result)
                {
                    new LogHelper().Debug("Failed to get questions using web api due to HTTP error : " + localResponse.statusCode, ReservationID.ToString(), "GetQuestions", ActionGroup);
                    return null;

                }
                else
                {
                    string apiResponse = localResponse.responseData.ToString();
                    new LogHelper().Debug("web API response :- " + apiResponse, ReservationID.ToString(), "GetQuestions", ActionGroup);
                    Questions = JsonConvert.DeserializeObject<List<Questions>>(apiResponse);
                }
              
                
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, ReservationID.ToString(), "GetQuestions", ActionGroup);
            }
            return Questions;
        }


        public async Task<List<Models.PackageMasterModal>> GetPackages(string RoomType)
        {
            string ActionGroup = "GetPackages";
            List<Models.PackageMasterModal> PackageList = new List<Models.PackageMasterModal>();
            try
            {
                APIRequestModel _APIRequestModel = new APIRequestModel();
                var sendGuestRequest = new APIRequestModel()
                {
                    RequestObject = new
                    {
                        RoomTypeCode = RoomType,

                    },
                };
                _APIRequestModel = sendGuestRequest;
                var localResponse = await new CloudHelper().GetPackages("", _APIRequestModel, ActionGroup, AppSettingsManager.GetDecryptedSetting("APIBaseUrl"));
                if (!localResponse.result)
                {
                    new LogHelper().Debug("Failed to get packages using web api due to HTTP error : " + localResponse.statusCode, ReservationID.ToString(), "GetPackages", ActionGroup);
                    return null;

                }
                else
                {
                    string apiResponse = localResponse.responseData.ToString();
                    new LogHelper().Debug("web API response :- " + apiResponse, ReservationID.ToString(), "GetPackages", ActionGroup);
                    PackageList = JsonConvert.DeserializeObject<List<PackageMasterModal>>(apiResponse);
                }
            }
            catch 
            {

            }
            return PackageList;
        }

        public void UpdateReservationByStage(string Stage, Models.UpdateReservationModel reservationModel)
        {
            string ActionGroup = "UpdateReservationByStage";
            new LogHelper().Debug("UpdateReservationByStage Request : "  + JsonConvert.SerializeObject(reservationModel), ReservationID.ToString(), "UpdateReservationByStage", ActionGroup);

            Task.Run(async () =>
            {
                try
                {
                    APIRequestModel _APIRequestModel = new APIRequestModel();
                var sendGuestRequest = new APIRequestModel()
                {
                    Identifier = Stage,
                    RequestObject =  reservationModel
                };
                _APIRequestModel = sendGuestRequest;
                    new LogHelper().Debug("Before calling CloudHelper method",
ReservationID.ToString(),
"UpdateReservationByStage",
ActionGroup);
                    var localResponse = await new CloudHelper().UpdateReservationByStage("", _APIRequestModel, ActionGroup, AppSettingsManager.GetDecryptedSetting("APIBaseUrl"));
                if (!localResponse)
                {
                    new LogHelper().Debug("Failed to UpdateReservationByStage using web api due to HTTP error : " + localResponse, ReservationID.ToString(), "UpdateReservationByStage", ActionGroup);
                    //return null;

                }
                else
                {
                    new LogHelper().Debug("Successfully Updated Reservation By Stage : " + localResponse, ReservationID.ToString(), "UpdateReservationByStage", ActionGroup);
                }
            }
            catch
            {

            }
            });
        }

        public async Task ExecuteUpdateReservationByStage(string stage, Models.UpdateReservationModel reservationModel)
        {
            string actionGroup = "UpdateReservationByStage";

            new LogHelper().Debug(
                "UpdateReservationByStage Request : " + JsonConvert.SerializeObject(reservationModel),
                ReservationID.ToString(),
                "UpdateReservationByStage",
                actionGroup);

            try
            {
                var request = new APIRequestModel
                {
                    Identifier = stage,
                    RequestObject = reservationModel
                };

                new LogHelper().Debug(
                    "Before calling CloudHelper method",
                    ReservationID.ToString(),
                    "UpdateReservationByStage",
                    actionGroup);

                bool localResponse = await new CloudHelper().UpdateReservationByStage(
                    "",
                    request,
                    actionGroup,
                    AppSettingsManager.GetDecryptedSetting("APIBaseUrl"));

                if (!localResponse)
                {
                    new LogHelper().Debug(
                        "Failed to UpdateReservationByStage",
                        ReservationID.ToString(),
                        "UpdateReservationByStage",
                        actionGroup);
                }
                else
                {
                    new LogHelper().Debug(
                        "Successfully Updated Reservation By Stage",
                        ReservationID.ToString(),
                        "UpdateReservationByStage",
                        actionGroup);
                }
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, ReservationID.ToString(), "UpdateReservationByStage", actionGroup);
            }
        }

        public bool InsertFeedback(int reservationID,int questionID,string answer)
        {
            string ActionGroup = "InsertFeedback";
            Task.Run(async () =>
            {
                try
                {
                    APIRequestModel _APIRequestModel = new APIRequestModel();
                var sendGuestRequest = new APIRequestModel()
                {
                    RequestObject = new FeedBackRequestModel
                    {
                        reservationID = reservationID,
                        questionID = questionID,
                        answer = answer
                    },
                };
                _APIRequestModel = sendGuestRequest;
                var localResponse = await new CloudHelper().InsertFeedback("", _APIRequestModel, ActionGroup, AppSettingsManager.GetDecryptedSetting("APIBaseUrl"));
                if (!localResponse)
                {
                    new LogHelper().Debug("Failed to InsertFeedback using web api due to HTTP error : " + localResponse, ReservationID.ToString(), "InsertFeedback", ActionGroup);
                    //return null;

                }
                else
                {
                    new LogHelper().Debug("Successfully Inserted Feedback : " + localResponse, ReservationID.ToString(), "InsertFeedback", ActionGroup);
                }
                return localResponse;
                }
                catch
                {
                    return false;
                }
            });
            return false;

        }


        public bool InsertEvent(int reservationID, string evenSubModuleName)
        {
            string ActionGroup = "InsertEvent";
            Task.Run(async () =>
            {
                try
                {
                    APIRequestModel _APIRequestModel = new APIRequestModel();
                    var sendGuestRequest = new APIRequestModel()
                    {
                        RequestObject = new EventRequestModel
                        {
                            reservationID = reservationID,
                            evenSubModuleName = evenSubModuleName
                        
                        },
                    };
                    _APIRequestModel = sendGuestRequest;
                    var localResponse = await new CloudHelper().InsertEvent("", _APIRequestModel, ActionGroup, AppSettingsManager.GetDecryptedSetting("APIBaseUrl"));
                    if (!localResponse)
                    {
                        new LogHelper().Debug("Failed to InsertEvent using web api due to HTTP error : " + localResponse, ReservationID.ToString(), "InsertEvent", ActionGroup);
                        //return null;

                    }
                    else
                    {
                        new LogHelper().Debug("Successfully Inserted Event : " + localResponse, ReservationID.ToString(), "InsertEvent", ActionGroup);
                    }
                    return localResponse;
                }
                catch
                {
                    return false;
                }
            });
            return false;
        }


        public bool InsertReservationPackageDetails(int reservationID, string[] PackageList)
        {
            string ActionGroup = "Reservation";
            Task.Run(async () =>
            {
                try
                {
                    APIRequestModel _APIRequestModel = new APIRequestModel();
                var sendGuestRequest = new APIRequestModel()
                {
                    RequestObject = new ReservationPackageRequestModel
                    {
                        reservationID = reservationID,
                        PackageList = PackageList

                    },
                };
                _APIRequestModel = sendGuestRequest;
                var localResponse = await new CloudHelper().InsertReservationPackageDetails("", _APIRequestModel, ActionGroup, AppSettingsManager.GetDecryptedSetting("APIBaseUrl"));
                if (!localResponse)
                {
                    new LogHelper().Debug("Failed to InsertFeedback using web api due to HTTP error : " + localResponse, ReservationID.ToString(), "InsertFeedback", ActionGroup);
                    //return null;

                }
                else
                {
                    new LogHelper().Debug("Successfully Inserted Feedback : " + localResponse, ReservationID.ToString(), "InsertFeedback", ActionGroup);
                }
                return localResponse;
                }
                catch
                {
                    return false;
                }
            });
            return false;
        }

        public bool UpdateCheckoutFlag(int reservationID)
        {
            string ActionGroup = "Reservation";
            Task.Run(async () =>
            {
                try
                {
                    APIRequestModel _APIRequestModel = new APIRequestModel();
                var sendGuestRequest = new APIRequestModel()
                {
                    RequestObject = new 
                    {
                        ReservationID = reservationID
                        
                    },
                };
                _APIRequestModel = sendGuestRequest;
                var localResponse = await new CloudHelper().UpdateCheckoutFlag("", _APIRequestModel, ActionGroup, AppSettingsManager.GetDecryptedSetting("APIBaseUrl"));
                if (!localResponse)
                {
                    new LogHelper().Debug("Failed to InsertFeedback using web api due to HTTP error : " + localResponse, ReservationID.ToString(), "InsertFeedback", ActionGroup);
                    //return null;

                }
                else
                {
                    new LogHelper().Debug("Successfully Inserted Feedback : " + localResponse, ReservationID.ToString(), "InsertFeedback", ActionGroup);
                }
                return localResponse;
                }
                catch
                {
                    return false;
                }
            });
            return false;
        }

        public bool UpdatePaymentHeaderData(string transactionID, string ResultCode, string ResponseMessage,bool? isActive,string transactionType,decimal Amount)
        {
            string ActionGroup = "Reservation";
            Task.Run(async () =>
            {
                try
                {
                    APIRequestModel _APIRequestModel = new APIRequestModel();
                var sendGuestRequest = new APIRequestModel()
                {
                    RequestObject = new PaymentHeaderRequestModel
                    {
                        transactionID = transactionID,
                        ResultCode = ResultCode,
                        ResponseMessage = ResponseMessage,
                        isActive = isActive,
                        transactionType = transactionType,
                        Amount = Amount

                    },
                };
                _APIRequestModel = sendGuestRequest;
                var localResponse = await new CloudHelper().UpdatePaymentHeaderData("", _APIRequestModel, ActionGroup, AppSettingsManager.GetDecryptedSetting("APIBaseUrl"));
                if (!localResponse)
                {
                    new LogHelper().Debug("Failed to InsertFeedback using web api due to HTTP error : " + localResponse, ReservationID.ToString(), "InsertFeedback", ActionGroup);
                    //return null;

                }
                else
                {
                    new LogHelper().Debug("Successfully Inserted Feedback : " + localResponse, ReservationID.ToString(), "InsertFeedback", ActionGroup);
                }
                return localResponse;
            }
            catch
            {
                return false;
            }
        });
            return false;
           
        }

        public async Task<List<Models.ActiveTransctionsModel>> GetActivePaymentTransctions(string ReservationNumber)
        {
            //ReservationNumber
            List<Models.ActiveTransctionsModel> ActiveTranscitons = new List<Models.ActiveTransctionsModel>();
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
                var localResponse = await new CloudHelper().GetActivePaymentTransctions("", _APIRequestModel, ActionGroup, AppSettingsManager.GetDecryptedSetting("APIBaseUrl"));
                if (!localResponse.result)
                {
                    new LogHelper().Debug("Failed to GetActivePaymentTransctions using web api due to HTTP error : " + localResponse.statusCode, ReservationID.ToString(), "GetActivePaymentTransctions", ActionGroup);
                    return null;

                }
                else
                {
                    string apiResponse = localResponse.responseData.ToString();
                    new LogHelper().Debug("web API response :- " + apiResponse, ReservationID.ToString(), "GetActivePaymentTransctions", ActionGroup);
                    ActiveTranscitons = JsonConvert.DeserializeObject<List<ActiveTransctionsModel>>(apiResponse);
                }
            }
            catch
            {

            }
            return ActiveTranscitons;

          
        }

        public bool InsertPaymentData(PaymentResponse paymentDetailResponseModel, string ReservationNumber, string ReservationNameID,string TransactionID,string transactionType)
        {
            string ActionGroup = "Reservation";
            Task.Run(async () =>
            {
                try
                {
                    APIRequestModel _APIRequestModel = new APIRequestModel();
                var sendGuestRequest = new APIRequestModel()
                {
                    RequestObject = new PaymentDetailsRequest
                    {
                        CardToken = paymentDetailResponseModel.CardToken,
                        RefusalReason = paymentDetailResponseModel.RefusalReason,
                        CardExpiryDate = paymentDetailResponseModel.CardExpiryDate,
                        PaymentToken = paymentDetailResponseModel.PaymentToken,
                        MerchantRefernce = paymentDetailResponseModel.MerchantRefernce,
                        AuthCode = paymentDetailResponseModel.AuthCode,
                        CardType = paymentDetailResponseModel.CardType,
                        FundingSource = paymentDetailResponseModel.FundingSource,
                        PspReference = paymentDetailResponseModel.PspReference,
                        ResultCode = paymentDetailResponseModel.ResultCode,
                        additionalInfos = paymentDetailResponseModel.additionalInfos,
                        MaskCardNumber = paymentDetailResponseModel.MaskCardNumber,
                        Currency = paymentDetailResponseModel.Currency,
                        Amount = paymentDetailResponseModel.Amount,
                        ParentPSPReferece = paymentDetailResponseModel.ParentPSPReferece,

                        ReservationNumber = ReservationNumber,
                        ReservationNameID = ReservationNameID,
                        TransactionID = TransactionID,
                        transactionType = transactionType

                    },
                };
                _APIRequestModel = sendGuestRequest;
                var localResponse = await new CloudHelper().InsertPaymentData("", _APIRequestModel, ActionGroup, AppSettingsManager.GetDecryptedSetting("APIBaseUrl"));
                if (!localResponse)
                {
                    new LogHelper().Debug("Failed to InsertPaymentData using web api due to HTTP error : " + localResponse, ReservationID.ToString(), "InsertPaymentData", ActionGroup);
                    //return null;

                }
                else
                {
                    new LogHelper().Debug("Successfully InsertPaymentData  : " + localResponse, ReservationID.ToString(), "InsertPaymentData", ActionGroup);
                }
                return localResponse;
                }
                catch
                {
                    return false;
                }
            });
            return false;
        }

        public bool updateCheckoutFlag(string reservationNameID)
        {
            string ActionGroup = "Reservation";
            Task.Run(async () =>
            {
                try
                {
                    APIRequestModel _APIRequestModel = new APIRequestModel();
                var sendGuestRequest = new APIRequestModel()
                {
                    RequestObject = new
                    {
                        ReservationNameID = reservationNameID

                    },
                };
                _APIRequestModel = sendGuestRequest;
                var localResponse = await new CloudHelper().UpdateCheckoutFlag("", _APIRequestModel, ActionGroup, AppSettingsManager.GetDecryptedSetting("APIBaseUrl"));
                if (!localResponse)
                {
                    new LogHelper().Debug("Failed to InsertFeedback using web api due to HTTP error : " + localResponse, ReservationID.ToString(), "InsertFeedback", ActionGroup);
                    //return null;

                }
                else
                {
                    new LogHelper().Debug("Successfully Inserted Feedback : " + localResponse, ReservationID.ToString(), "InsertFeedback", ActionGroup);
                }
                return localResponse;
            }
            catch
            {
                return false;
            }
        });
            return false;
        }


        public bool UpdatePrimaryGuestEmail(string emailID, string ReservationID)
        {
            string ActionGroup = "Reservation";
            Task.Run(async () =>
            {
                try
                {
                    APIRequestModel _APIRequestModel = new APIRequestModel();
                var sendGuestRequest = new APIRequestModel()
                {
                    RequestObject = new
                    {
                        ReservationID = ReservationID,
                        EmailAddress = emailID

                    },
                };
                _APIRequestModel = sendGuestRequest;
                var localResponse = await new CloudHelper().UpdateCheckoutFlag("", _APIRequestModel, ActionGroup, AppSettingsManager.GetDecryptedSetting("APIBaseUrl"));
                if (!localResponse)
                {
                    new LogHelper().Debug("Failed to InsertFeedback using web api due to HTTP error : " + localResponse, ReservationID.ToString(), "InsertFeedback", ActionGroup);
                    //return null;

                }
                else
                {
                    new LogHelper().Debug("Successfully Inserted Feedback : " + localResponse, ReservationID.ToString(), "InsertFeedback", ActionGroup);
                }
                return localResponse;
                }
                catch
                {
                    return false;
                }
            });
            return false;

        }

        public async Task<List<ReservationPackageModel>> GetReservationPackages(int ReservationDetailID)
        {
            List<ReservationPackageModel> ReservationPackagesList = new List<ReservationPackageModel>();
            string ActionGroup = "Reservation";

            try
            {
                APIRequestModel _APIRequestModel = new APIRequestModel();
                var sendGuestRequest = new APIRequestModel()
                {
                    RequestObject = new
                    {
                        ReservationID = ReservationDetailID,

                    },
                };
                _APIRequestModel = sendGuestRequest;
                var localResponse = await new CloudHelper().GetReservationPackages("", _APIRequestModel, ActionGroup, AppSettingsManager.GetDecryptedSetting("APIBaseUrl"));
                if (!localResponse.result)
                {
                    new LogHelper().Debug("No Reservation packages exist " , ReservationID.ToString(), "GetReservationPackages", ActionGroup);
                    return null;

                }
                else
                {
                    string apiResponse = localResponse.responseData.ToString();
                    new LogHelper().Debug("web API response :- " + apiResponse, ReservationID.ToString(), "GetReservationPackages", ActionGroup);
                    ReservationPackagesList = JsonConvert.DeserializeObject<List<ReservationPackageModel>>(apiResponse);
                }
            }
            catch
            {

            }
            return ReservationPackagesList;
        }

        public async Task<DataTable> GetReservationByReservationID(int ReservationID)
        {
            DataTable dataTable = new DataTable();
            List<ReservationPackageModel> ReservationPackagesList = new List<ReservationPackageModel>();
            string ActionGroup = "Reservation";

            try
            {
                APIRequestModel _APIRequestModel = new APIRequestModel();
                var sendGuestRequest = new APIRequestModel()
                {
                    RequestObject = new
                    {
                        ReservationID = ReservationID,

                    },
                };
                _APIRequestModel = sendGuestRequest;
                var localResponse = await new CloudHelper().GetReservationByReservationID("", _APIRequestModel, ActionGroup, AppSettingsManager.GetDecryptedSetting("APIBaseUrl"));
                if (localResponse==null)
                {
                    new LogHelper().Debug("Failed to get Reservation using web api due to HTTP error ", ReservationID.ToString(), "GetReservationByReservationID", ActionGroup);
                    return null;

                }
                else
                {

                    dataTable = localResponse;
                }
            }
            catch
            {

            }
            

            return dataTable;
        }
        /// <summary>
        /// Fetch reservation from Opera without process filtering.
        /// Callers must decide precheckin vs precheckout from reservation status.
        /// Passing empty Process skips verifyReservationisAllowed (which would force DUEIN/RESERVED only).
        /// </summary>
        public async Task<List<Models.OWS.OperaReservation>> FetchReservationDetailFromPMS(string ReservationNumber)
        {
            string ActionGroup = "Guest Search";
            // Empty process = return reservation as-is (do not filter as precheckin-only)
            string process = string.Empty;

            var reservationfromopera = await new CloudHelper().fetchReservationFromPMS(new Models.OWS.OwsRequestModel()
            {
                ChainCode = AppSettingsManager.GetDecryptedSetting("ChainCode"),
                DestinationEntityID = AppSettingsManager.GetDecryptedSetting("DestinationEntityID"),
                DestinationSystemType = AppSettingsManager.GetDecryptedSetting("DestinationSystemType"),
                HotelDomain = AppSettingsManager.GetDecryptedSetting("HotelDomain"),
                KioskID = AppSettingsManager.GetDecryptedSetting("KioskID"),
                LegNumber = "1",
                Language = AppSettingsManager.GetDecryptedSetting("Language"),
                Password = AppSettingsManager.GetDecryptedSetting("Password"),
                Username = AppSettingsManager.GetDecryptedSetting("Username"),
                SystemType = AppSettingsManager.GetDecryptedSetting("SystemType"),
                FetchBookingRequest = new Models.OWS.FetchBookingRequestModel()
                {
                    ReservationNumber = ReservationNumber

                }

            }, AppSettingsManager.GetDecryptedSetting("APIBaseUrl"), process, ActionGroup);

            if (reservationfromopera == null || reservationfromopera.Count == 0)
            {
                reservationfromopera = await new CloudHelper().fetchReservationFromPMS(new Models.OWS.OwsRequestModel()
                {
                    ChainCode = AppSettingsManager.GetDecryptedSetting("ChainCode"),
                    DestinationEntityID = AppSettingsManager.GetDecryptedSetting("DestinationEntityID"),
                    DestinationSystemType = AppSettingsManager.GetDecryptedSetting("DestinationSystemType"),
                    HotelDomain = AppSettingsManager.GetDecryptedSetting("HotelDomain"),
                    KioskID = AppSettingsManager.GetDecryptedSetting("KioskID"),
                    LegNumber = "1",
                    Language = AppSettingsManager.GetDecryptedSetting("Language"),
                    Password = AppSettingsManager.GetDecryptedSetting("Password"),
                    Username = AppSettingsManager.GetDecryptedSetting("Username"),
                    SystemType = AppSettingsManager.GetDecryptedSetting("SystemType"),
                    FetchBookingRequest = new Models.OWS.FetchBookingRequestModel()
                    {
                        CRSNumber = ReservationNumber
                    }

                }, AppSettingsManager.GetDecryptedSetting("APIBaseUrl"), process, ActionGroup);
            }

            return reservationfromopera;
        }

        public async Task<bool> PushDueInSearchedReservation(string ConfirmationNo)
        {
            try
            {
                string ActionGroup = "Pre-Checkin Push";

                string _HotelDomain = AppSettingsManager.GetDecryptedSetting("HotelDomain");
                string _KioskID = AppSettingsManager.GetDecryptedSetting("KioskID");
                string _Username = AppSettingsManager.GetDecryptedSetting("Username");
                string _Password = AppSettingsManager.GetDecryptedSetting("Password");
                string _SystemType = AppSettingsManager.GetDecryptedSetting("SystemType");
                string _Language = AppSettingsManager.GetDecryptedSetting("Language");
                string _ChainCode = AppSettingsManager.GetDecryptedSetting("ChainCode");
                string _DestinationEntityID = AppSettingsManager.GetDecryptedSetting("DestinationEntityID");
                string _GarunteeTypeCode = AppSettingsManager.GetDecryptedSetting("GarunteeTypeCode");
                string _preAuthUDF = AppSettingsManager.GetDecryptedSetting("preAuthUDF");
                string _preAuthAmntUDF = AppSettingsManager.GetDecryptedSetting("preAuthAmntUDF");
                string _ApiBaseUrl = AppSettingsManager.GetDecryptedSetting("APIBaseUrl");

                string _PreArrivalFromEmail = AppSettingsManager.GetDecryptedSetting("PreArrivalFromEmail");
                string _PreArrivalEmailSubject = AppSettingsManager.GetDecryptedSetting("PreArrivalEmailSubject");
                string _EmailDisplayName = AppSettingsManager.GetDecryptedSetting("EmailDisplayName");

                string _PreArrivalConfirmationEmail = AppSettingsManager.GetDecryptedSetting("PreArrivalConfirmationEmail");
                string _PreArrivalConfirmationEmailSubject = AppSettingsManager.GetDecryptedSetting("PreArrivalConfirmationEmailSubject");

                APIRequestModel _APIRequestModel = new APIRequestModel();

                var sendPrecheckinRequest = new APIRequestModel()
                {
                    RequestObject = new
                    {
                        ReservationNumber = ConfirmationNo,
                        isForceFetch = true,
                        ServiceParameters = new
                        {
                            isProxyEnableForCloudAPI = false,// true,
                            CloudAPIProxyHost = "",// 
                            CloudAPIProxyUN = "",// "CPH\\_rtpfps",
                            CloudAPIProxyPswd = "",// "IT$upp0rt",
                            CloudAPIURL = _ApiBaseUrl,
                            isProxyEnableForLocalAPI = false,// false,
                            LocalAPIProxyHost = "",// null,
                            LocalAPIProxyUN = "",// null,
                            LocalAPIProxyPswd = "",// null,
                            LocalAPIURL = _ApiBaseUrl,
                            isProxyEnableForEmailAPI = false,// false,
                            EmailAPIProxyHost = "",// null,
                            EmailAPIProxyUN = "",// null,
                            EmailAPIProxyPswd = "",// null,
                            EmailURL = _ApiBaseUrl,

                            PreArrivalConfirmationEmail = _PreArrivalConfirmationEmail,
                            PreArrivalConfirmationEmailSubject = _PreArrivalConfirmationEmailSubject,

                            PreArrivalFromEmail = _PreArrivalFromEmail,
                            PreArrivalEmailSubject = _PreArrivalEmailSubject,
                            EmailDisplayName = _EmailDisplayName,

                            ChainCode = _ChainCode,// "CHA",
                            DestinationEntityID = _DestinationEntityID,// "TI",
                            HotelDomain = _HotelDomain,// "RTP",
                            KioskID = _KioskID,// "KIOSK",
                            Language = _Language,// "EN",
                            Legnumber = "1",// "1",
                            Password = _Password,// "$$$KIOSK$$",
                            SystemType = _SystemType,// "KIOSK",
                            Username = _Username,//"KIOSK",
                            ClientID = "MCI",
                            PreAuthUDF = _preAuthUDF,// "Appr_Code",
                            PreAuthAmntUDF = _preAuthAmntUDF,// "Appr_Code"
                            GarunteeTypeCode = _GarunteeTypeCode,
                            IsETADefault = false,
                            IsPaymentDisabled = false
                        }
                    },
                };

                _APIRequestModel = sendPrecheckinRequest;

                var localResponse = await new CloudHelper().PushSearchedReservation("", _APIRequestModel, ActionGroup, AppSettingsManager.GetDecryptedSetting("APIBaseUrl"));
                if (!localResponse.result)
                {
                    new LogHelper().Log("Failed to pushing Searched Reservation with reason :- " + localResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, "FetchPreCheckedInReservation", "pre checked-in fetch");
                    new LogHelper().Warn("Failed to pushing Searched Reservation with reason :- " + localResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, "", ActionGroup);
                    return false;
                }

                new LogHelper().Log("Searched Reservation updated in Cloud DB successfully", "", "", ActionGroup);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public async Task<string> GetLastEvetIDByReservation(int ReservationID)
        {
            string ActionGroup = "Reservation";
            
                try
                {
                    APIRequestModel _APIRequestModel = new APIRequestModel();
                var sendGuestRequest = new APIRequestModel()
                {
                    RequestObject = new
                    {
                        ReservationID = ReservationID

                    },
                };
                _APIRequestModel = sendGuestRequest;
                var localResponse = await new CloudHelper().GetLastEvetIDByReservation("", _APIRequestModel, ActionGroup, AppSettingsManager.GetDecryptedSetting("APIBaseUrl"));
                if (string.IsNullOrEmpty(localResponse))
                {
                    new LogHelper().Debug("Failed to InsertFeedback using web api due to HTTP error : " + localResponse, ReservationID.ToString(), "InsertFeedback", ActionGroup);
                    //return null;

                }
                else
                {
                    new LogHelper().Debug("Successfully Inserted Feedback : " + localResponse, ReservationID.ToString(), "InsertFeedback", ActionGroup);
                }
                return localResponse;
            }
            catch
            {
                return null;
            }
      
        }

        //Usp_GetProfileDocumentsByReservationID
        public async Task<DataTable> CheckDuplicateDocumentByReservationID(int reservationID)
        {
            DataTable dataTable = new DataTable();
            List<ReservationPackageModel> ReservationPackagesList = new List<ReservationPackageModel>();
            string ActionGroup = "Reservation";

            try
            {
                APIRequestModel _APIRequestModel = new APIRequestModel();
                var sendGuestRequest = new APIRequestModel()
                {
                    RequestObject = new
                    {
                        ReservationID = reservationID,

                    },
                };
                _APIRequestModel = sendGuestRequest;
                var localResponse = await new CloudHelper().CheckDuplicateDocumentByReservationID("", _APIRequestModel, ActionGroup, AppSettingsManager.GetDecryptedSetting("APIBaseUrl"));
                if (localResponse == null)
                {
                    new LogHelper().Debug("Failed to get Reservation using web api due to HTTP error ", ReservationID.ToString(), "CheckDuplicateDocumentByReservationID", ActionGroup);
                    return null;

                }
                else
                {

                    dataTable = localResponse;
                }
            }
            catch
            {
            }

            return dataTable ?? new DataTable();
           
        }

    }

}

