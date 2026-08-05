using CheckinPortal.Helpers;
using CheckinPortal.Models;
using CheckinPortal.Models.AdaptorAPIModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using CheckinPortal.Models.BlinkIdModels;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;
using System.Configuration;
using CheckinPortal.BackOffice.Helpers;

namespace CheckinPortal.BusinessLayer
{
    public class BlinkIdLogics
    {
        ReservationLogics reservationLogics = new ReservationLogics();
        public BlinkIdLogics()
        {

        }
        public async Task<BlinkResponseModel> ReadScannedDocument(CloudRequestModel scanResult)
        {
            try
            {
                // Avoid calling ToString().Length on big objects; if RequestObject is large, measure carefully
                try
                {
                    new LogHelper().Debug("Read Scanned Document webapi request Size: " + (scanResult?.RequestObject != null ? scanResult.RequestObject.ToString().Length : 0), "", "ReadScannedDocument", "Micro Blink Browser");
                }
                catch { /* ignore logging size failures */ }

                // Convert RequestObject to JToken safely
                JToken requestToken = null;

                if (scanResult.RequestObject == null)
                {
                    return new BlinkResponseModel { result = false, responseMessage = "RequestObject is null" };
                }
                

                if (scanResult.RequestObject is JToken token)
                {
                    requestToken = token;
                }
                else
                {
                    // Convert anything else to string and parse
                    requestToken = JToken.Parse(scanResult.RequestObject.ToString());
                }
               

                // Now convert token to strongly typed Root using ToObject<T>()
                Root root = null;
                try
                {
                    root = requestToken.ToObject<Root>();
                }
                catch (Exception ex)
                {
                    Helpers.LogHelper.Instance.Error(ex, null, "ReadScannedDocument", "Micro Blink Browser");
                    return new BlinkResponseModel { result = false, responseMessage = "OCR Engine returned invalid structure" };
                }

                if (root == null)
                {
                    return new BlinkResponseModel { result = false, responseMessage = "OCR Engine returned NULL" };
                }

                // Sanity checks
                var firstSub = root.subResults?.FirstOrDefault();
                if (firstSub == null)
                {
                    return new BlinkResponseModel { result = false, responseMessage = "No subResults returned" };
                }
                var secondSub = root.subResults?.Skip(1).FirstOrDefault();


                // Convert images safely
                string fullImageBase64 = null;
                string fullbackImageBase64 = null;
                string faceImageBase64 = null;

                try
                {
                    // Document: ensure correct dimensions known (you already used 600 x 742)
                    if (firstSub.documentImage?.data != null && firstSub.documentImage.data.Length > 0)
                    {
                        var docImg = firstSub.documentImage;

                        fullImageBase64 = ConvertRawRgbaToBase64Png_Safe(
                            docImg.data,
                            docImg.width,
                            docImg.height
                        );
                    }
                    

                    if (firstSub.faceImage?.data != null && firstSub.faceImage.data.Length > 0)
                    {
                        var faceImg = firstSub.faceImage;
                        faceImageBase64 = ConvertRawRgbaToBase64Png_Safe(faceImg.data, faceImg.width, faceImg.height);
                    }
                    if (secondSub.documentImage?.data != null && secondSub.documentImage.data.Length > 0)
                    {
                        var docImg = secondSub.documentImage;

                        fullbackImageBase64 = ConvertRawRgbaToBase64Png_Safe(
                            docImg.data,
                            docImg.width,
                            docImg.height
                        );
                    }
                }
                catch (Exception ex)
                {
                    Helpers.LogHelper.Instance.Error(ex, null, "ReadScannedDocument.ImageConvert", "Micro Blink Browser");
                    // If image conversion fails, we proceed but note in response
                }

                var readDocumentResponse = new BlinkIdDocumentResponseModel
                {
                    ReservationID = int.TryParse(scanResult.ReservationId, out var rid) ? rid : 0,
                    ProfileDetailID = int.TryParse(scanResult.ProfileDetailId, out var pdid) ? pdid : 0,
                    idType = root?.documentClassInfo?.type,
                    firstName = root?.firstName?.latin?.value ?? firstSub?.viz?.FirstNameValue,
                    lastName = root?.lastName?.latin?.value ?? firstSub?.viz?.LastNameValue,
                    fullName = !string.IsNullOrEmpty(root?.firstName?.latin?.value) ? $"{root.firstName.latin} {root.lastName?.latin}" : firstSub?.viz?.FullNameValue,
                    gender = NormalizeGender(root?.sex?.latin?.value ?? firstSub?.mrz?.gender),
                    dateOfBirth = ToDateString(root?.dateOfBirth),
                    issueDate = ToDateString(root?.dateOfIssue),
                    expiryDate = ToDateString(root?.dateOfExpiry),
                    address1 = firstSub?.viz?.Address,
                    address2 = firstSub?.viz?.AdditionalAddressInformation,
                    state = firstSub?.viz?.ResidentialStatus,
                    city = firstSub?.viz?.PlaceOfBirthValue,
                    issueCountry = root?.documentClassInfo?.isoAlpha3CountryCode,
                    issueCountry_fullname = root?.documentClassInfo?.countryName,
                    nationality = firstSub?.mrz?.nationality,
                    nationality_fullname = firstSub?.mrz?.nationalityName,
                    documentNumber = root?.documentNumber?.latin?.value,
                    personalNumber = firstSub?.viz?.PersonalIdNumberValue,
                    fullImage = fullImageBase64,
                    faceImage = faceImageBase64,
                    backImage= fullbackImageBase64,
                    ProfileID = scanResult.ProfileID
                };
              
                //if(SessionData.ScanDocuments!=null)
                //{
                //    SessionData.ScanDocuments.Add(readDocumentResponse);
                //}
                //else
                //{
                //    SessionData.ScanDocuments
                //}
                Helpers.LogHelper.Instance.Debug("Read Scanned Document Successfully", "", "ReadScannedDocument", "Micro Blink Browser");

                return new BlinkResponseModel
                {
                    result = true,
                    responseData = readDocumentResponse
                };
            }
            catch (Exception ex)
            {
                Helpers.LogHelper.Instance.Error(ex, null, "ReadScannedDocument", "Micro Blink Browser");
                return new BlinkResponseModel { result = false, responseMessage = ex.Message };
            }
        }

        private string ToDateString(IDateField date)
        {
            if (date == null || !date.successfullyParsed) return null;
            //return $"{date.day:D2}/{date.month:D2}/{date.year:D4}";
            // Return in "yyyy-MM-dd" format
            return $"{date.year:D4}-{date.month:D2}-{date.day:D2}";
        }

        private string NormalizeGender(string gender)
        {
            if (string.IsNullOrEmpty(gender)) return null;
            string genderText;
            switch (gender)
            {
                case "M":
                    genderText = "MALE";
                    break;
                case "F":
                    genderText = "FEMALE";
                    break;
                default:
                    genderText = "UNKNOWN";
                    break;
            }
            return genderText;
        }
        private string ConvertRawRgbaToBase64Png_Safe(int[] data, int width, int height)
        {
            if (data == null || data.Length == 0)
                return null;

            // Validate length
            long expected = (long)width * height * 4L;
            if (data.Length != expected)
                throw new InvalidOperationException($"Raw data length mismatch. Expected {expected} bytes (width*height*4) but got {data.Length}.");

            // Convert int[] (0-255) to byte[]
            byte[] rgba = Array.ConvertAll(data, item => (byte)item);

            // Create and write bitmap
            using (var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            {
                var rect = new Rectangle(0, 0, width, height);
                var bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, bmp.PixelFormat);
                try
                {
                    System.Runtime.InteropServices.Marshal.Copy(rgba, 0, bmpData.Scan0, rgba.Length);
                }
                finally
                {
                    bmp.UnlockBits(bmpData);
                }

                // Save as PNG and convert to Base64
                using (var ms = new MemoryStream())
                {
                    bmp.Save(ms, ImageFormat.Png);
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public string ConvertToBase64(Dictionary<string, int> data)
        {
            if (data == null) return null;

            byte[] bytes = data
                           .OrderBy(kvp => int.Parse(kvp.Key))
                           .Select(kvp => (byte)kvp.Value)
                           .ToArray();

            return Convert.ToBase64String(bytes);
        }
        public string ConvertRawRgbaToBase64Png(
    Dictionary<string, int> data, int width, int height)
        {
            if (data == null || data.Count == 0) return null;

            // 1. Convert ordered dictionary to byte[]
            byte[] rgba = data.OrderBy(k => int.Parse(k.Key))
                              .Select(k => (byte)k.Value)
                              .ToArray();

            // 2. Create Bitmap
            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            // 3. Copy raw RGBA into bitmap
            var rect = new Rectangle(0, 0, width, height);
            var bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, bmp.PixelFormat);

            System.Runtime.InteropServices.Marshal.Copy(rgba, 0, bmpData.Scan0, rgba.Length);

            bmp.UnlockBits(bmpData);

            // 4. Convert Bitmap → PNG → Base64
            using (var ms = new MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Png);
                return Convert.ToBase64String(ms.ToArray());
            }
        }
        public async Task<bool> UploadBlinkIdDocumentAsync(BlinkIdDocumentResponseModel uploadGuestDocumentModel)
        {
            //var files = Request.Files;
            try
            {
                int count = 0;

                //push events to DB
                reservationLogics.InsertEvent(uploadGuestDocumentModel.ReservationID, "DocumentUploadTry");
                Helpers.LogHelper.Instance.Log($"Uploading guest document,", $"{uploadGuestDocumentModel.ReservationID}", "UploadDocument", "Pre-Checkin");


                var documentModel = new UpdateReservationModel();

                if (uploadGuestDocumentModel.fullImage != null)
                {

                    DateTime expiryDate = new DateTime(1900, 01, 01);
                    DateTime issueDate = new DateTime(1900, 01, 01);
                    DateTime birthDate = new DateTime(1900, 01, 01);

                    if (!DateTime.TryParseExact(uploadGuestDocumentModel.issueDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out issueDate))
                    {
                        issueDate = new DateTime(1900, 01, 01);
                    }
                    if (!DateTime.TryParseExact(uploadGuestDocumentModel.expiryDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out expiryDate))
                    {
                        expiryDate = new DateTime(1900, 01, 01);
                    }
                    if (!DateTime.TryParseExact(uploadGuestDocumentModel.dateOfBirth, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out birthDate))
                    {
                        birthDate = new DateTime(1900, 01, 01);
                    }

                    documentModel.Gender = uploadGuestDocumentModel.gender;
                    documentModel.IssueCountry = uploadGuestDocumentModel.issueCountry;
                    documentModel.ExpiryDate = expiryDate;
                    documentModel.IssueDate = issueDate;
                    documentModel.DocumentType = uploadGuestDocumentModel.idType;
                    documentModel.DocumentNumber = uploadGuestDocumentModel.documentNumber;
                    documentModel.BirthDate = birthDate;
                    documentModel.Nationality = uploadGuestDocumentModel.nationality;

                    documentModel.FirstName = !string.IsNullOrEmpty(uploadGuestDocumentModel.firstName)
                            ? uploadGuestDocumentModel.firstName : uploadGuestDocumentModel.fullName;
                    documentModel.MiddleName = uploadGuestDocumentModel.middleName;
                    documentModel.LastName = uploadGuestDocumentModel.lastName;

                    if (!string.IsNullOrEmpty(uploadGuestDocumentModel.faceImage))
                    {
                        documentModel.FaceImage = Convert.FromBase64String(uploadGuestDocumentModel.faceImage);
                    }


                }

                documentModel.ReservationID = uploadGuestDocumentModel.ReservationID;
                documentModel.ProfileDetailID = uploadGuestDocumentModel.ProfileDetailID;


                //documentModel.DocumentImage1 = Request.Files[0].InputStream


                if (!string.IsNullOrEmpty(uploadGuestDocumentModel.fullImage))
                {
                    documentModel.DocumentImage1 = Convert.FromBase64String(uploadGuestDocumentModel.fullImage);
                }

                if (!string.IsNullOrEmpty(uploadGuestDocumentModel.backImage))
                {
                    documentModel.DocumentImage2 = Convert.FromBase64String(uploadGuestDocumentModel.backImage);
                }

                Helpers.LogHelper.Instance.Debug($"Uploading Document Json : {Newtonsoft.Json.JsonConvert.SerializeObject(documentModel)}", $"{uploadGuestDocumentModel.ReservationID}", "UploadMBDocument", "Pre-Checkin");

                if (string.IsNullOrEmpty(uploadGuestDocumentModel.ProfileID))
                {
                    new LogHelper().Log("Creating accompanying profile in opera  - (Last name - +" + documentModel.LastName + ")", documentModel.ReservationID.ToString(), "FetchPreCheckedInReservation", "pre checked-in fetch");
                    Models.OWS.OwsResponseModel responseModel = await new CloudHelper().CreateAccompanyingProfile(SessionData.OperaReservation.ReservationNameID, new Models.OWS.OwsRequestModel()
                    {
                        ChainCode = AppSettingsManager.GetDecryptedSetting("ChainCode"),
                        DestinationEntityID = AppSettingsManager.GetDecryptedSetting("DestinationEntityID"),
                        HotelDomain = AppSettingsManager.GetDecryptedSetting("HotelDomain"),
                        KioskID = AppSettingsManager.GetDecryptedSetting("KioskID"),
                        Language = AppSettingsManager.GetDecryptedSetting("Language"),
                        LegNumber = "1",
                        Password = AppSettingsManager.GetDecryptedSetting("Password"),
                        SystemType = AppSettingsManager.GetDecryptedSetting("SystemType"),
                        Username = AppSettingsManager.GetDecryptedSetting("Username"),


                       
                        CreateAccompanyingProfileRequest = new Models.OWS.CreateAccompanyingProfileRequest()
                        {
                            FirstName = documentModel.FirstName,
                            MiddleName = documentModel.MiddleName,
                            LastName = documentModel.LastName,
                            Gender = !string.IsNullOrEmpty(documentModel.Gender) ? (documentModel.Gender.ToUpper().Equals("M") ? "Male" : (documentModel.Gender.ToUpper().Equals("F") ? "Female" : null)) : null,
                            ReservationNumber = SessionData.OperaReservation.ReservationNumber
                        }
                    }, "pre checked-in fetch", AppSettingsManager.GetDecryptedSetting("APIBaseUrl"));
                    if (!responseModel.result)
                    {
                        new LogHelper().Log("Failed to create accompanying profile with reason :- " + responseModel.responseMessage, SessionData.OperaReservation.ReservationNameID, "FetchPreCheckedInReservation", "pre checked-in fetch");
                        new LogHelper().Warn("Failed to fetch profile documents with reason :- " + responseModel.responseMessage, SessionData.OperaReservation.ReservationNameID, "FetchPreCheckedInReservation", "pre checked-in fetch");
                    }

                    else if (responseModel.responseData == null)
                    {
                        new LogHelper().Log("Failed to create accompanying profile with reason :- API response data is NULL" + responseModel.responseMessage, SessionData.OperaReservation.ReservationNameID, "FetchPreCheckedInReservation", "pre checked-in fetch");
                        new LogHelper().Warn("Failed to create accompanying profile with reason :- API response data is NULL" + responseModel.responseMessage, SessionData.OperaReservation.ReservationNameID, "FetchPreCheckedInReservation", "pre checked-in fetch");
                    }
                    else
                    {
                        new LogHelper().Debug("Converting API json to object", SessionData.OperaReservation.ReservationNameID, "FetchPreCheckedInReservation", "pre checked-in fetch");
                        try
                        {
                            Models.OWS.GuestProfile guest = JsonConvert.DeserializeObject<Models.OWS.GuestProfile>(responseModel.responseData.ToString());
                            uploadGuestDocumentModel.ProfileID = guest.PmsProfileID;
                        }
                        catch (Exception ex)
                        {
                            new LogHelper().Error(ex, SessionData.OperaReservation.ReservationNameID, "FetchPreCheckedInReservation", "pre checked-in fetch");
                            new LogHelper().Log("Failed to covert API response to object", SessionData.OperaReservation.ReservationNameID, "FetchPreCheckedInReservation", "pre checked-in fetch");
                            new LogHelper().Warn("Failed to create accompanying profile with reason :- " + ex.Message, SessionData.OperaReservation.ReservationNameID, "FetchPreCheckedInReservation", "pre checked-in fetch");
                            new LogHelper().Debug("Failed to create accompanying profile with reason :- " + ex.Message, SessionData.OperaReservation.ReservationNameID, "FetchPreCheckedInReservation", "pre checked-in fetch");
                        }
                        new LogHelper().Log("Accompanying profile created successfully", SessionData.OperaReservation.ReservationNameID, "FetchPreCheckedInReservation", "pre checked-in fetch");
                    }
                }

              else  if (!string.IsNullOrEmpty(uploadGuestDocumentModel.ProfileID))
                {
                    new LogHelper().Log("Updating guest profile in opera  - (Last name - +" + documentModel.LastName + ")", SessionData.OperaReservation.ReservationNameID, "FetchPreCheckedInReservation", "pre checked-in fetch");
                    Models.OWS.OwsResponseModel owsResponse = await new CloudHelper().UpdateGuestProfile(SessionData.OperaReservation.ReservationNameID, new Models.OWS.OwsRequestModel()
                    {
                        ChainCode = AppSettingsManager.GetDecryptedSetting("ChainCode"),
                        DestinationEntityID = AppSettingsManager.GetDecryptedSetting("DestinationEntityID"),
                        HotelDomain = AppSettingsManager.GetDecryptedSetting("HotelDomain"),
                        KioskID = AppSettingsManager.GetDecryptedSetting("KioskID"),
                        Language = AppSettingsManager.GetDecryptedSetting("Language"),
                        LegNumber = "1",
                        Password = AppSettingsManager.GetDecryptedSetting("Password"),
                        SystemType = AppSettingsManager.GetDecryptedSetting("SystemType"),
                        Username = AppSettingsManager.GetDecryptedSetting("Username"),
                        UpdateProileRequest = new Models.OWS.UpdateProfile()
                        {
                            ProfileID = uploadGuestDocumentModel.ProfileID,
                            DOB = documentModel.BirthDate,

                            DocumentNumber = documentModel.DocumentNumber,
                            DocumentType = !string.IsNullOrWhiteSpace(documentModel?.DocumentType)
                 ? documentModel.DocumentType
                 : null,
                            Gender = !string.IsNullOrEmpty(documentModel.Gender) ? (documentModel.Gender.ToUpper().Equals("M") ? "Male" : (documentModel.Gender.ToUpper().Equals("F") ? "Female" : null)) : null,
                            IssueCountry = documentModel.IssueCountry,
                            IssueDate = documentModel.IssueDate,
                            Nationality = documentModel.Nationality

                        }
                    }, "pre checked-in fetch", AppSettingsManager.GetDecryptedSetting("APIBaseUrl"));

                    if (!owsResponse.result)
                    {
                        new LogHelper().Log("Failed to update profile with reason :- " + owsResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, "FetchPreCheckedInReservation", "pre checked-in fetch");
                        new LogHelper().Warn("Failed to update profile with reason :- " + owsResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, "FetchPreCheckedInReservation", "pre checked-in fetch");
                    }
                    else
                    {
                        new LogHelper().Log("Updated profile successfully", SessionData.OperaReservation.ReservationNameID, "FetchPreCheckedInReservation", "pre checked-in fetch");
                    }
                }

                if (!string.IsNullOrEmpty(uploadGuestDocumentModel.ProfileID))
                {
                    new LogHelper().Log("Updating passport info in opera  - (Last name - +" + documentModel.LastName + ")", SessionData.OperaReservation.ReservationNameID, "FetchPreCheckedInReservation", "pre checked-in fetch");
                    Models.OWS.OwsResponseModel owsResponse = await new CloudHelper().UpdateGuestProfile(SessionData.OperaReservation.ReservationNameID, new Models.OWS.OwsRequestModel()
                    {
                        ChainCode = AppSettingsManager.GetDecryptedSetting("ChainCode"),
                        DestinationEntityID = AppSettingsManager.GetDecryptedSetting("DestinationEntityID"),
                        HotelDomain = AppSettingsManager.GetDecryptedSetting("HotelDomain"),
                        KioskID = AppSettingsManager.GetDecryptedSetting("KioskID"),
                        Language = AppSettingsManager.GetDecryptedSetting("Language"),
                        LegNumber = "1",
                        Password = AppSettingsManager.GetDecryptedSetting("Password"),
                        SystemType = AppSettingsManager.GetDecryptedSetting("SystemType"),
                        Username = AppSettingsManager.GetDecryptedSetting("Username"),
                        UpdateProileRequest = new Models.OWS.UpdateProfile()
                        {
                            ProfileID = uploadGuestDocumentModel.ProfileID,
                            DOB = documentModel.BirthDate,

                            DocumentNumber = documentModel.DocumentNumber,
                            DocumentType = documentModel.DocumentType,
                            Gender = !string.IsNullOrEmpty(documentModel.Gender) ? (documentModel.Gender.ToUpper().Equals("M") ? "Male" : (documentModel.Gender.ToUpper().Equals("F") ? "Female" : null)) : null,
                            IssueCountry = documentModel.IssueCountry,
                            IssueDate = documentModel.IssueDate,
                            Nationality = documentModel.Nationality

                        }
                    }, "pre checked-in fetch", AppSettingsManager.GetDecryptedSetting("APIBaseUrl"));

                    if (!owsResponse.result)
                    {
                        new LogHelper().Log("Failed to update passport info with reason :- " + owsResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, "FetchPreCheckedInReservation", "pre checked-in fetch");
                        new LogHelper().Warn("Failed to update passport info with reason :- " + owsResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, "FetchPreCheckedInReservation", "pre checked-in fetch");
                    }
                    else
                    {
                        new LogHelper().Log("Updated passport info successfully", SessionData.OperaReservation.ReservationNameID, "FetchPreCheckedInReservation", "pre checked-in fetch");
                    }
                }






                    #region Updating Profile guest
                new LogHelper().Log("Pushing profile guest", SessionData.OperaReservation.ReservationNameID, "UploadDocument", "Pre-Checkin");
                var localResponses = await new CloudHelper().InsertGuestProfile(SessionData.OperaReservation.ReservationNameID, new Models.APIRequestModel()
                {
                    RequestObject = new List<ProfileDetails>() {
                                    new ProfileDetails
                                    {
                                        ReservationNameID = SessionData.OperaReservation.ReservationNameID,
                                        LastName = documentModel.DocumentNumber,

                                        Gender = documentModel.Gender,
                                        FirstName = documentModel.FirstName,
                                        Email = documentModel.Email,
                                        CountryCode = null,
                                        MiddleName = documentModel.MiddleName,
                                         BirthDate= documentModel.BirthDate,
                                        Nationality = documentModel.Nationality,
                                        City = documentModel.City,
                                        Phone= documentModel.Phone,
                                        PostalCode=documentModel.PostalCode,
                                        AddressLine1 = documentModel.AddressLine1,
                                        AddressLine2 = documentModel.AddressLine2,
                                        StateCode=null,
                                        ProfileID=uploadGuestDocumentModel.ProfileID
                                    }
                                },
                    SyncFromCloud = false

                }, "pre checked-in fetch", ConfigurationManager.AppSettings
            ["APIBaseUrl"].ToString());
                #endregion

                //#region updateprofileby fetchingfrompms

                //var reservationpms = await new CloudHelper().fetchReservationFromPMS(new Models.OWS.OwsRequestModel()
                //{
                //    ChainCode = ConfigurationManager.AppSettings["ChainCode"].ToString(),
                //    DestinationEntityID = ConfigurationManager.AppSettings["DestinationEntityID"].ToString(),
                //    DestinationSystemType = ConfigurationManager.AppSettings["DestinationSystemType"].ToString(),
                //    HotelDomain = ConfigurationManager.AppSettings["HotelDomain"].ToString(),
                //    KioskID = ConfigurationManager.AppSettings["KioskID"].ToString(),
                //    LegNumber = "1",
                //    Language = ConfigurationManager.AppSettings["Language"].ToString(),
                //    Password = ConfigurationManager.AppSettings["Password"].ToString(),
                //    Username = ConfigurationManager.AppSettings["Username"].ToString(),
                //    SystemType = ConfigurationManager.AppSettings["SystemType"].ToString(),
                //    FetchBookingRequest = new Models.OWS.FetchBookingRequestModel()
                //    {
                //        ReservationNumber = SessionData.OperaReservation.ReservationNumber

                //    }
                //}, ConfigurationManager.AppSettings["APIBaseUrl"].ToString(), "precheckin", "uplod");

                //if (reservationpms != null && reservationpms.Count > 0)
                //{

                //    if (reservationpms.First().Adults != null && reservationpms.First().Adults.Value > 0)
                //        SessionData.OperaReservation.GuestProfiles = reservationpms.First().GuestProfiles;
                //}

                //var localAPIResponse = await new CloudHelper().updateReservationDetailsInLocalDB(new APIRequestModel()
                //{
                //    RequestObject = new List<Models.OWS.OperaReservation> { SessionData.OperaReservation },
                //    SyncFromCloud = false
                //    //changed to false by jeena on 1072023
                //}, ConfigurationManager.AppSettings
                //    ["APIBaseUrl"], "upload");
                //#endregion
                #region Updating Profile documents
                new LogHelper().Log("Pushing profile documents", SessionData.OperaReservation.ReservationNameID, "UploadDocument", "Pre-Checkin");
                var localResponse = await new CloudHelper().InsertDocuments(SessionData.OperaReservation.ReservationNameID, new Models.APIRequestModel()
                {
                    RequestObject = new List<ProfileDocuments>() {
                                    new ProfileDocuments
                                    {
                                        ReservationNameID = SessionData.OperaReservation.ReservationNameID,
                                        DocumentNumber = documentModel.DocumentNumber,

                                        ExpiryDate = documentModel.ExpiryDate,
                                        DocumentTypeCode = documentModel.DocumentType,
                                        IssueCountry = documentModel.IssueCountry,
                                        ProfileID = uploadGuestDocumentModel.ProfileID.ToString(),
                                        DocumentImage1 = documentModel.DocumentImage1,
                                        DocumentImage2 = documentModel.DocumentImage2,
                                        DocumentImage3 = documentModel.DocumentImage3,
                                        FaceImage = documentModel.FaceImage
                                    }
                                },
                    SyncFromCloud = false

                }, "pre checked-in fetch", ConfigurationManager.AppSettings
            ["APIBaseUrl"].ToString());
                if (!localResponse.result)
                {
                    new LogHelper().Log("Failed to pushing profile documents with reason :- " + localResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, "FetchPreCheckedInReservation", "pre checked-in fetch");
                    new LogHelper().Warn("Failed to pushing profile documents with reason :- " + localResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, "UploadDocument", "Pre-Checkin");
                }
                else
                    new LogHelper().Log("Profile documents updated in Local DB successfully", SessionData.OperaReservation.ReservationNameID, "UploadDocument", "Pre-Checkin");
                #endregion
               // reservationLogics.UpdateReservationByStage("Upload", documentModel);
                return true;
            }
            catch (Exception ex)
            {
                Helpers.LogHelper.Instance.Error(ex, $"{uploadGuestDocumentModel.ReservationID}", "UploadMBDocument", "Pre-Checkin");
                return false;
            }
        }
        public async Task<string> GetCountry(int Id)
        {
            var countryList = await new MastersLogics().GetCountryList();
            if (countryList != null && countryList.Count() > 0)
            {
                var countrydetails = countryList.Where(x => x.CountryMasterID == Id).ToList();
                if (countrydetails != null && countrydetails.Count() > 0)
                {
                    return countrydetails.FirstOrDefault().Country_2Char_code;
                }
                return "";
            }

            return "";
        }
        public async Task<string> GetStateById(int Id)
        {
            var StateList = await new MastersLogics().GetStateList();
            if (StateList != null && StateList.Count() > 0)
            {
                var Statedetails = StateList.Where(x => x.StateMasterID == Id).ToList();
                if (Statedetails != null && Statedetails.Count() > 0)
                {
                    return Statedetails.FirstOrDefault().StateCode;
                }
                return "";
            }

            return "";
        }
    }
}