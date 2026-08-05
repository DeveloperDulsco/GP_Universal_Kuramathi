using CheckinPortal.BackOffice.Helpers;
using CheckinPortal.Helpers;
using CheckinPortal.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Threading.Tasks;

namespace CheckinPortal.BusinessLayer
{
    public class MastersLogics
    {
        public MastersLogics()
        {

        }
        public async Task<List<DataAccess.tbCountryMaster>> GetCountryList()
        {
            List<DataAccess.tbCountryMaster> tbCountryMasters = new List<DataAccess.tbCountryMaster>();
            DataTable dataTable = new DataTable();
            string ActionGroup = "Reservation";

            try
            {
                APIRequestModel _APIRequestModel = new APIRequestModel();
               
                var countryTable = await new CloudHelper().GetCountryList("", _APIRequestModel, ActionGroup, AppSettingsManager.GetDecryptedSetting("APIBaseUrl"));
                if (countryTable == null)
                {
                    new LogHelper().Debug("Failed to Get CountryList using web api due to HTTP error : ", "", "GetCountryList", ActionGroup);
                    return null;

                }
                else
                {
                    var localResponse = Helpers.DataTableHelper.DataTableToList<CheckinPortal.DataAccess.tbCountryMaster>(countryTable);
                    return localResponse;
                }
            }
            catch
            {
                return null;
            }
           
        }

        public async Task<List<Models.StateMaster>> GetStateList()
        {
            
            string ActionGroup = "Reservation";

            try
            {
                APIRequestModel _APIRequestModel = new APIRequestModel();

                var countryTable = await new CloudHelper().GetStateList("", _APIRequestModel, ActionGroup, AppSettingsManager.GetDecryptedSetting("APIBaseUrl"));
                if (countryTable == null)
                {
                    new LogHelper().Debug("Failed to get questions using web api due to HTTP error : ", "", "GetStateList", ActionGroup);
                    return null;

                }
                else
                {
                    var localResponse = Helpers.DataTableHelper.DataTableToList<Models.StateMaster>(countryTable);
                    return localResponse;
                }
            }
            catch
            {
                return null;
            }
            
        }

        public async Task<List<Models.StateMaster>> GetStateListByCountryID(int CountryID)
        {

            string ActionGroup = "Reservation";

            try
            {
                APIRequestModel _APIRequestModel = new APIRequestModel();
                var sendGuestRequest = new APIRequestModel()
                {
                    Identifier = CountryID.ToString()
                    
                };
                _APIRequestModel = sendGuestRequest;
                var stateTable = await new CloudHelper().GetStateListByCountryID("", _APIRequestModel, ActionGroup, AppSettingsManager.GetDecryptedSetting("APIBaseUrl"));
                if (stateTable == null)
                {
                    new LogHelper().Debug("No State Exist for this country ", "", "GetStateListByCountryID", ActionGroup);
                    return null;

                }
                else
                {
                    
                    //var localResponse = Helpers.DataTableHelper.DataTableToList<Models.StateMaster>(stateTable);
                    return stateTable;
                }
            }
            catch
            {
                return null;
            }
        }

        public async Task<DataTable> validateDocumentIssueCountry(string docType,string issueCountry)
        {
            string ActionGroup = "Reservation";

            try
            {
                APIRequestModel _APIRequestModel = new APIRequestModel();
                var sendGuestRequest = new APIRequestModel()
                {
                    RequestObject = new DocumentIssueCountryRequest
                    {
                        docType = docType,
                        issueCountry = issueCountry
                    }

                };
                _APIRequestModel = sendGuestRequest;
                var countryTable = await new CloudHelper().validateDocumentIssueCountry("", _APIRequestModel, ActionGroup, AppSettingsManager.GetDecryptedSetting("APIBaseUrl"));
                if (countryTable == null)
                {
                    new LogHelper().Debug("Failed to document issue country validation using web api due to HTTP error : ", "", "validateDocumentIssueCountry", ActionGroup);
                    return null;

                }
                else
                {
                    
                    return countryTable;
                }
            }
            catch
            {
                return null;
            }
            
        }

        public async Task<List<DocumentTypeMasterModel>> GetDocumentList()
        {
            List<DocumentTypeMasterModel> tbDocumentMasters = new List<DocumentTypeMasterModel>();
            DataTable dataTable = new DataTable();
            string ActionGroup = "Reservation";

            try
            {
                APIRequestModel _APIRequestModel = new APIRequestModel();

                var response = await new CloudHelper().GetDocumentList("", _APIRequestModel, ActionGroup, AppSettingsManager.GetDecryptedSetting("APIBaseUrl"));
                if (!response.result)
                {
                    new LogHelper().Debug("Failed to get DocumentList using web api due to HTTP error : ", "", "GetDocumentList", ActionGroup);
                    return null;

                }
                else
                {
                    tbDocumentMasters = JsonConvert.DeserializeObject<List<DocumentTypeMasterModel>>(response.responseData.ToString());
                    return tbDocumentMasters;
                }
            }
            catch
            {
                return null;
            }
            
        }
        public async Task<List<CloudReservationDocument>> GetReservationDocumenttype(string docType, string reservationnumber)
        {
            List<CloudReservationDocument> tbDocumentResdocumenst = new List<CloudReservationDocument>();
           
            
            string ActionGroup = "Reservation";

            try
            {
                APIRequestModel _APIRequestModel = new APIRequestModel();
                var sendRequest = new APIRequestModel()
                {
                    RequestObject = new CloudReservationDocument
                    {
                        DocumentType = docType,
                        reservationnumber = reservationnumber
                       
                    },
                };
                _APIRequestModel = sendRequest;

                var response = await new CloudHelper().GetReservationDocumenttype("", _APIRequestModel, ActionGroup, AppSettingsManager.GetDecryptedSetting("APIBaseUrl"));
                if (!response.result)
                {
                    new LogHelper().Debug("Failed to get  ReservationDocumenttype using web api due to HTTP error : ", "", "GetReservationDocumenttype", ActionGroup);
                    return null;

                }
                else
                {
                    tbDocumentResdocumenst = JsonConvert.DeserializeObject<List<CloudReservationDocument>>(response.responseData.ToString());
                    return tbDocumentResdocumenst;
                }
            }
            catch
            {
                return null;
            }
          
        }
    }
}