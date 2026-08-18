using CheckinPortal.BusinessLayer;
using CheckinPortal.DataAccess;
using CheckinPortal.Helpers;
using CheckinPortal.Models;
using CheckinPortal.Models.AdaptorAPIModels;
using CheckinPortal.Models.BlinkIdModels;
using CheckinPortal.Models.Emails;
using CheckinPortal.Models.OWS;
using CheckinPortal.Models.PaymentDetails;
using Newtonsoft.Json;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity.Core.Mapping;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Windows;
using static QRCoder.PayloadGenerator;

namespace CheckinPortal.Controllers
{
    public class HomeController : Controller
    {
        ReservationLogics reservationLogics = new ReservationLogics();

        public async Task<ActionResult> Index(string id)
        {
            Session.Clear();
            Session.Abandon();
            ViewBag.id = id;

            string ActionName = "Index";
            string ActionGroup = "Pre-Checkin";
            var currentCulture = Thread.CurrentThread.CurrentCulture;

            var languageSession = "en";

            if (Request.Cookies.AllKeys.Contains("culture"))
            {
                languageSession = Request.Cookies["culture"].Value.ToString();
            }


            //if (Session["SelectedLanguage"] == null)
            {
                Session["SelectedLanguage"] = languageSession;
            }



            var test = Url.Encode(Helpers.EncryptionHelper.EncryptString("23403"));
            if (string.IsNullOrEmpty(id))
            {
                return ShowLinkExpiry(LinkExpiryHelper.MissingLink, "0", ActionName, ActionGroup);
            }

            string confirmationNo = Helpers.EncryptionHelper.DecryptString(id.ToString());

            if (string.IsNullOrEmpty(confirmationNo))
            {
                return ShowLinkExpiry(LinkExpiryHelper.InvalidLink, id, ActionName, ActionGroup);
            }

            ViewBag.ReservationFound = false;
            ViewBag.PaymentProcessed = false;
            ViewBag.uploadedcompleted = false;
            ViewBag.CompletedTabIndex = -1;
            ViewBag.ResumeTabIndex = -1;
            ViewBag.SkipPrecheckinSplash = false;
            Models.ReservationModel reservationModel = new Models.ReservationModel();
            reservationModel.IsDepositAvailable = false;
            OperaReservation operaReservation = new OperaReservation();

            MastersLogics mastersLogics = new MastersLogics();

            var CountryList = await new CloudHelper().fetchcountryMaster(ConfigurationManager.AppSettings
                    ["APIBaseUrl"].ToString(), ActionGroup);

            var reservationsDt = await new CloudHelper().FetchReservationDetailsByReferenceNumber(confirmationNo, new APIRequestModel { RequestObject = confirmationNo }, ActionGroup, ConfigurationManager.AppSettings
                    ["APIBaseUrl"].ToString());
            var reservations = new CloudReservationModel();
            if (reservationsDt?.responseData != null)
            {
                reservations = JsonConvert.DeserializeObject<List<CloudReservationModel>>(reservationsDt.responseData.ToString()).FirstOrDefault();

            }
            if (reservations != null && reservations.ReservationDetailID > 0)
            {

                Helpers.LogHelper.Instance.Log($"Reservation details fetched from DB {JsonConvert.SerializeObject(reservations, Formatting.Indented)} ", $"{reservations.ReservationNameID}", ActionName, ActionGroup);

                var reservationfromopera = await new CloudHelper().fetchReservationFromPMS(new Models.OWS.OwsRequestModel()
                {
                    ChainCode = ConfigurationManager.AppSettings["ChainCode"].ToString(),
                    DestinationEntityID = ConfigurationManager.AppSettings["DestinationEntityID"].ToString(),
                    DestinationSystemType = ConfigurationManager.AppSettings["DestinationSystemType"].ToString(),
                    HotelDomain = ConfigurationManager.AppSettings["HotelDomain"].ToString(),
                    KioskID = ConfigurationManager.AppSettings["KioskID"].ToString(),
                    LegNumber = "1",
                    Language = ConfigurationManager.AppSettings["Language"].ToString(),
                    Password = ConfigurationManager.AppSettings["Password"].ToString(),
                    Username = ConfigurationManager.AppSettings["Username"].ToString(),
                    SystemType = ConfigurationManager.AppSettings["SystemType"].ToString(),
                    FetchBookingRequest = new Models.OWS.FetchBookingRequestModel()
                    {
                        ReservationNumber = confirmationNo

                    }
                }, ConfigurationManager.AppSettings["APIBaseUrl"].ToString(), "precheckin", ActionGroup);

                if (reservationfromopera != null && reservationfromopera.Count > 0)
                {

                    if (reservationfromopera.First().Adults != null && reservationfromopera.First().Adults.Value > 0)
                        operaReservation = reservationfromopera.FirstOrDefault();
                    Helpers.LogHelper.Instance.Log($"Reservation details fetched from opera {JsonConvert.SerializeObject(operaReservation, Formatting.Indented)} ", $"{reservations.ReservationNameID}", ActionName, ActionGroup);


                    #region setsession
                    SessionDt booking = new SessionDt
                    {
                        ReservationNameID = operaReservation.ReservationNumber,
                        ReservationNumber = operaReservation.ReservationNameID,
                        ReservationStatus = operaReservation.ReservationStatus,
                    };
                    //Session["ReservationNameID"] = operaReservation.ReservationNumber;
                    //Session["BookingSession"] = booking;
                    #endregion
                    bool isredirectfromPaymentPage = false;
                    bool IsPaymentSuccess = false;
                    string PaymentFailureMessage = "";

                    if (TempData["IsredirectedfromPaymentPage"] != null)
                    {
                        isredirectfromPaymentPage = Convert.ToBoolean(TempData["IsredirectedfromPaymentPage"].ToString());
                        IsPaymentSuccess = Convert.ToBoolean(TempData["IsPaymentSuccess"].ToString());
                        PaymentFailureMessage = TempData["PaymentFailureMessage"].ToString();
                    }

                    ViewBag.IsredirectedfromPaymentPage = isredirectfromPaymentPage;
                    ViewBag.IsPaymentSuccess = IsPaymentSuccess;
                    ViewBag.PaymentFailureMessage = PaymentFailureMessage;

                    ViewBag.uploadcomplete = false;

                    string reservationStatus = NormalizeReservationStatus(operaReservation);
                    if (!isredirectfromPaymentPage && !string.IsNullOrEmpty(reservationStatus))
                    {
                        if (IsCheckedOutStatus(reservationStatus))
                        {
                            return ShowLinkExpiry(LinkExpiryHelper.CheckedOut, confirmationNo, ActionName, ActionGroup);
                        }
                        if (!IsPreCheckinStatus(reservationStatus))
                        {
                            return ShowLinkExpiry(LinkExpiryHelper.InvalidStatus, confirmationNo, ActionName, ActionGroup);
                        }
                    }

                    var PackageLists = await reservationLogics.GetPackages(reservations.RoomType);
                    ViewBag.PackageList = PackageLists;
                    if (reservations.IsPreCheckedInPMS.HasValue && !reservations.IsPreCheckedInPMS.Value || isredirectfromPaymentPage)
                    {
                        ViewBag.ReservationFound = true;

                        #region Get the existing selected package and Upsells

                        var ReservationPackagesList = await reservationLogics.GetReservationPackages(reservations.ReservationDetailID);

                        if (ReservationPackagesList != null && ReservationPackagesList.Count > 0)
                        {
                            Helpers.LogHelper.Instance.Log($"Existing reservation packages found.", $"{reservations.ReservationNameID}", ActionName, ActionGroup);

                            var RoomUpsellPackages = ReservationPackagesList.Where(x => x.IsRoomUpsell).Select(x => x.PackageID).ToArray();
                            var SpecialsPackages = ReservationPackagesList.Where(x => !x.IsRoomUpsell).Select(x => x.PackageID).ToArray();

                            ViewBag.RoomUpsellPackages = string.Join(",", RoomUpsellPackages);
                            ViewBag.SpecialPackages = string.Join(",", SpecialsPackages);
                            ViewBag.ReservationPackagesList = ReservationPackagesList;
                        }
                        else
                        {
                            Helpers.LogHelper.Instance.Log($"Existing reservation packages not found.", $"{reservations.ReservationNameID}", ActionName, ActionGroup);
                            ViewBag.RoomUpsellPackages = string.Empty;
                            ViewBag.SpecialPackages = string.Empty;
                            ViewBag.ReservationPackagesList = new List<ReservationPackageModel>();
                        }
                        #endregion

                        #region VerifyVIPReservationOrNot
                        if (operaReservation.userDefinedFields != null && operaReservation.userDefinedFields.Count > 0)
                        {
                            if (operaReservation.userDefinedFields.Find(x => x.FieldName.Equals(ConfigurationManager.AppSettings
                    ["PreAuthUDF"].ToString())) != null &&
                                operaReservation.userDefinedFields.Find(x => x.FieldName.Equals(ConfigurationManager.AppSettings
                    ["PreAuthUDF"].ToString())).FieldValue.Equals("NO"))
                            {

                                reservations.IsDepositAvailable = true;
                            }
                            else if (operaReservation.userDefinedFields.Find(x => x.FieldName.Equals(ConfigurationManager.AppSettings
                    ["PreAuthAmntUDF"])) != null &&
                                operaReservation.userDefinedFields.Find(x => x.FieldName.Equals(ConfigurationManager.AppSettings
                    ["PreAuthAmntUDF"])).FieldValue.Equals("NO"))
                            {



                                reservations.IsDepositAvailable = true;
                            }
                        }
                        #endregion

                        #region PaymentDesabling
                        ViewBag.IsPaymentDisabled = Convert.ToBoolean(ConfigurationManager.AppSettings["IsPaymentDisabled"] ?? "false");
                        if (ViewBag.IsPaymentDisabled)
                        {
                            // Phase 1: skip payment — do not call Adyen; wizard must not block
                            reservations.IsDepositAvailable = true;
                            ViewBag.IsPaymentSuccess = true;
                        }
                        #endregion

                        #region Update ETA
                        if (Convert.ToBoolean(ConfigurationManager.AppSettings["IsETADefault"]))
                        {
                            new LogHelper().Log("Assigning NULL value to ETA as per the config", operaReservation.ReservationNameID, ActionName, ActionGroup);
                            // reservations.ETA = null;
                        }
                        #endregion


                        #region Processing MealPlan
                        if (ConfigurationManager.AppSettings["IsBreakFastValidationWithUDF"] != null && Convert.ToBoolean(ConfigurationManager.AppSettings["IsBreakFastValidationWithUDF"]))
                        {

                            if (operaReservation.userDefinedFields != null && operaReservation.userDefinedFields.Count > 0)
                            {
                                if (operaReservation.userDefinedFields.Find(x => x.FieldName.Equals(ConfigurationManager.AppSettings["MealPlanFieldName"])) != null)
                                {

                                    if (!operaReservation.userDefinedFields.Find(x => x.FieldName.Equals(ConfigurationManager.AppSettings["MealPlanFieldName"])).FieldValue.Equals("NP"))
                                    {
                                        string tempUDFValue = operaReservation.userDefinedFields.Find(x => x.FieldName.Equals(ConfigurationManager.AppSettings["MealPlanFieldName"])).FieldValue;
                                        if (!string.IsNullOrEmpty(tempUDFValue))
                                        {
                                            bool isPackageFound = false;
                                            if (ConfigurationManager.AppSettings["PackageCodes"].Split(';').ToList().Contains(tempUDFValue))
                                                isPackageFound = true;
                                            if (isPackageFound)
                                            {
                                                reservations.IsBreakFastAvailable = true;
                                                new LogHelper().Debug("Meal plan updated", operaReservation.ReservationNameID, ActionName, ActionGroup);
                                            }
                                        }
                                    }
                                    else
                                        new LogHelper().Log("Processing meal plan not updated (NP not present in UDF)", operaReservation.ReservationNameID, ActionName, ActionGroup); ;
                                }
                            }
                            else
                                new LogHelper().Log("No UDF fields for meal plan not found", operaReservation.ReservationNameID, ActionName, ActionGroup);
                        }
                        if (ConfigurationManager.AppSettings["IsBreakFastValidationWithPackage"] != null && Convert.ToBoolean(ConfigurationManager.AppSettings["IsBreakFastValidationWithPackage"]))
                        {
                            if (((operaReservation.PackageDetails != null && operaReservation.PackageDetails.Count > 0) || (operaReservation.PreferanceDetails != null && operaReservation.PreferanceDetails.Count > 0)) && (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["PackageCodes"]) && ConfigurationManager.AppSettings["PackageCodes"].Split(';').ToList() != null))
                            {
                                if (operaReservation.PackageDetails != null && operaReservation.PackageDetails.Count > 0)
                                {
                                    bool isPackageFound = false;
                                    foreach (Models.OWS.PackageDetails package in operaReservation.PackageDetails)
                                    {
                                        if (ConfigurationManager.AppSettings["PackageCodes"].Split(';').ToList().Contains(package.PackageCode))
                                        {
                                            isPackageFound = true;
                                            break;
                                        }
                                    }
                                    if (isPackageFound)
                                    {
                                        reservations.IsBreakFastAvailable = true;
                                        new LogHelper().Debug("Meal plan updated", operaReservation.ReservationNameID, ActionName, ActionGroup);
                                    }
                                }
                                if (operaReservation.PreferanceDetails != null && operaReservation.PreferanceDetails.Count > 0)
                                {
                                    if (operaReservation.IsBreakFastAvailable == null || !operaReservation.IsBreakFastAvailable.Value)
                                    {
                                        bool isPrefernceFound = false;
                                        foreach (Models.OWS.PreferanceDetails prefernce in operaReservation.PreferanceDetails)
                                        {
                                            if (ConfigurationManager.AppSettings["PackageCodes"].Split(';').ToList().Contains(prefernce.PreferanceCode))
                                            {
                                                isPrefernceFound = true;
                                                break;
                                            }
                                        }
                                        if (isPrefernceFound)
                                        {
                                            reservations.IsBreakFastAvailable = true;

                                        }
                                    }
                                }
                            }


                        }
                        #endregion
                        if ((reservations.IsEcomchekinPaymentStaus != null && reservations.IsEcomchekinPaymentStaus.Value) || isredirectfromPaymentPage)
                        {
                            Helpers.LogHelper.Instance.Log($"Reservation #{reservations.ReservationNameID} payment already done.", $"{reservations.ReservationNameID}", ActionName, ActionGroup);
                            ViewBag.PaymentProcessed = true;
                            ViewBag.IsPaymentSuccess = true;
                        }

                        //push events to DB
                        reservationLogics.InsertEvent(reservations.ReservationDetailID, "Email Link Click");
                        Helpers.LogHelper.Instance.Log($"Getting prfile details", $"{reservations.ReservationNameID}", ActionName, ActionGroup);
                        var ProfileList = await reservationLogics.GetReservationProfileList(reservations.ReservationDetailID);
                        AuditProgressHelper.Log(
                            AuditProgressHelper.ModulePreCheckin,
                            AuditProgressHelper.Actions.LinkOpened,
                            reservations.ReservationDetailID,
                            reservations.ReservationNameID,
                            guestName: ProfileList != null && ProfileList.Count > 0
                                ? AuditProgressHelper.BuildGuestName(ProfileList[0].FirstName, ProfileList[0].MiddleName, ProfileList[0].LastName)
                                : null);

                        ViewBag.Profiles = ProfileList;
                        ViewBag.CountryList = BuildCountryList(CountryList, ProfileList[0].CountryMasterID);
                        ViewBag.NationalityList = BuildNationalityList(CountryList, ProfileList[0].NationalityCode);
                        ViewBag.VisitPurposeCode = !string.IsNullOrEmpty(ProfileList[0].VisitPurposeCode)
                            ? ProfileList[0].VisitPurposeCode
                            : reservations.VisitPurposeCode;
                        try
                        {
                            if (ProfileList[0].CountryMasterID != null)
                            {
                                List<Models.StateMaster> StateList =
    await mastersLogics.GetStateListByCountryID(ProfileList[0].CountryMasterID.Value)
    ?? new List<Models.StateMaster>();

                                if (!StateList.Any())
                                {
                                    StateList.Add(new Models.StateMaster
                                    {
                                        StateMasterID = -1,
                                        Statename = "No states available"
                                    });
                                }

                                ViewBag.StateList = BuildStateSelectList(StateList, ProfileList[0].StateMasterID);
                            }
                            else
                            {
                                List<Models.StateMaster> tbstateMasters = new List<Models.StateMaster>();
                                tbstateMasters.Add(new Models.StateMaster()
                                {
                                    Statename = "Please select country",
                                    StateMasterID = -1

                                });
                                ViewBag.StateList = BuildStateSelectList(tbstateMasters, null);

                            }
                        }
                        catch (Exception e)
                        {
                        }
                        List<Models.Profile> profiles = new List<Models.Profile>();
                        int i = 0;
                        foreach (var profile in ProfileList)
                        {
                            if (profile.DocumentImage1 != null)
                            {
                                if (profile.DocumentImage1.Length > 0)
                                {
                                    i++;
                                }
                                else if (!string.IsNullOrEmpty(profile.DocumentNumber) && profile.DocumentNumber.Length > 0)
                                {
                                    i++;
                                }
                            }
                            else if (!string.IsNullOrEmpty(profile.DocumentNumber) && profile.DocumentNumber.Length > 0)
                            {
                                i++;
                            }
                            if (i == 0)
                            {
                                Models.OWS.GuestProfile guestProfile = null;
                                if (operaReservation?.GuestProfiles != null &&
     operaReservation.GuestProfiles.Count > 0 &&
     operaReservation.GuestProfiles[0] != null)
                                {
                                    guestProfile = operaReservation.GuestProfiles[0];
                                }

                                var primaryemail = guestProfile?.Email?
                                                    .FirstOrDefault(x => x?.primary == true);

                                var primaryphone = guestProfile?.Phones?
                                                    .FirstOrDefault(x => x?.primary == true);
                                profiles.Add(new Models.Profile()
                                {
                                    AddressLine1 = profile.AddressLine1,
                                    AddressLine2 = profile.AddressLine2,
                                    City = profile.City,
                                    CountryID = profile.CountryMasterID,
                                    Email = profile.Email != null ? profile.Email : (string.IsNullOrWhiteSpace(primaryemail?.email) ? null : primaryemail.email),
                                    FirstName = profile.FirstName,
                                    LastName = profile.LastName,
                                    Phone = profile.Phone != null ? profile.Phone : (string.IsNullOrWhiteSpace(primaryphone?.PhoneNumber) ? null : primaryphone.PhoneNumber),
                                    PostalCode = profile.PostalCode != null ? profile.PostalCode : "",
                                    StateID = profile.StateMasterID,
                                    ProfileDetailID = profile.ProfileDetailID,
                                    ProfileID = Convert.ToInt32(profile.ProfileID ?? "0"),
                                    MiddleName = profile.MiddleName,
                                    Nationality = profile.Nationality,
                                    DocumentImage1 = profile.DocumentImage1 != null ? "0" : "1",
                                    DocumentNumber = profile.DocumentNumber,
                                    HasDocumentUploaded = (profile.DocumentImage1 != null && profile.DocumentImage1.Length > 0)
                                        || !string.IsNullOrEmpty(profile.DocumentNumber),
                                    IsDocumentSkipped = profile.IsDocumentSkipped
                                        && !((profile.DocumentImage1 != null && profile.DocumentImage1.Length > 0)
                                            || !string.IsNullOrEmpty(profile.DocumentNumber))
                                });
                            }
                            else
                            {
                                profiles.Add(new Models.Profile()
                                {
                                    AddressLine1 = profile.AddressLine1,
                                    AddressLine2 = profile.AddressLine2,
                                    City = profile.City,
                                    CountryID = profile.CountryMasterID,
                                    Email = profile.Email,
                                    FirstName = profile.FirstName,
                                    LastName = profile.LastName,
                                    Phone = profile.Phone,
                                    PostalCode = profile.PostalCode != null ? profile.PostalCode : "",
                                    StateID = profile.StateMasterID,
                                    ProfileDetailID = profile.ProfileDetailID,
                                    ProfileID = Convert.ToInt32(profile.ProfileID ?? "0"),
                                    MiddleName = profile.MiddleName,
                                    Nationality = profile.Nationality,
                                    DocumentImage1 = profile.DocumentImage1 != null ? "0" : "1",
                                    DocumentNumber = profile.DocumentNumber,
                                    HasDocumentUploaded = (profile.DocumentImage1 != null && profile.DocumentImage1.Length > 0)
                                        || !string.IsNullOrEmpty(profile.DocumentNumber),
                                    IsDocumentSkipped = profile.IsDocumentSkipped
                                        && !((profile.DocumentImage1 != null && profile.DocumentImage1.Length > 0)
                                            || !string.IsNullOrEmpty(profile.DocumentNumber))
                                });
                            }
                        }
                        if (profiles != null)
                        {
                            if (profiles.Count() > 0 && profiles.Count() == i)
                            {
                                //Helpers.LogHelper.Instance.Log($"Reservation already contains document details updating IsUploadComplete as True", $"{reservations.ReservationNameID}", ActionName, ActionGroup);

                                //reservationLogics.UpdateReservationByStage("Upload Completes", new UpdateReservationModel()
                                //{
                                //    ReservationID = reservations.ReservationDetailID
                                //});
                                //ViewBag.uploadcomplete = true;
                                //reservations.IsUploadComplete = true;
                            }
                        }

                        decimal TotalRoomRate = 0;
                        TotalRoomRate = reservations.TotalAmount.HasValue ? reservations.TotalAmount.Value : 0;


                        string ExpectedTimeofArrival = "";

                        if (reservations.ETA.HasValue && !(reservations.ETA.Value.ToString("HH:mm") == "00:00"))
                        {
                            Helpers.LogHelper.Instance.Log($"Estimate time of arrival already exist {reservations.ETA.Value.ToString("HH:mm")} ", $"{reservations.ReservationNameID}", ActionName, ActionGroup);
                            DateTime timeUtc = reservations.ETA.Value;
                            ExpectedTimeofArrival = Helpers.DateTimeHelper.ConvertFromUTC(timeUtc);
                        }

                        // Per-guest resume: do not hide Document tab while any guest is still pending
                        int totalGuestSlots = (reservations.Adultcount ?? 0) + (reservations.Childcount ?? 0) + (reservations.InfantCount ?? 0);
                        if (totalGuestSlots < 1) totalGuestSlots = 1;
                        bool anyGuestDocPending = profiles.Any(p => !p.HasDocumentUploaded && !p.IsDocumentSkipped)
                            || profiles.Count < totalGuestSlots;
                        bool allGuestsResolved = profiles.Count >= totalGuestSlots
                            && profiles.All(p => p.HasDocumentUploaded || p.IsDocumentSkipped);

                        reservationModel = new Models.ReservationModel()
                        {
                            ReservationNameID = reservations.ReservationNameID,
                            ReservationID = reservations.ReservationDetailID,
                            AdultCount = reservations.Adultcount.HasValue ? reservations.Adultcount.Value : 0,
                            ArrivalDate = reservations.ArrivalDate.HasValue ? reservations.ArrivalDate.Value.ToString("dd MMM yyyy") : "",
                            AverageRoomRate = reservations.AverageRoomRate.HasValue ? reservations.AverageRoomRate.Value : 0,
                            ChildCount = reservations.Childcount.HasValue ? reservations.Childcount.Value : 0,
                            InfantCount = reservations.InfantCount.HasValue ? reservations.InfantCount.Value : 0,
                            DepartureDate = reservations.DepartureDate.HasValue ? reservations.DepartureDate.Value.ToString("dd MMM yyyy") : "",
                            ExpectedTimeofArrival = ExpectedTimeofArrival,
                            FlightNo = reservations.FlightNo,
                            IsMembershipRequested = reservations.IsMemberShipEnrolled.HasValue ? reservations.IsMemberShipEnrolled.Value : false,
                            IsTermsAndConditions = false,
                            MembershipNo = reservations.MembershipNo,
                            ReservationNumber = reservations.ReservationNumber,
                            Profiles = profiles,
                            Questions = null,
                            RoomType = reservations.RoomType,
                            RoomTypeDescription = reservations.RoomTypeDescription,
                            RTCShortDescription = reservations.RTCShortDescription,
                            TotalRoomRate = TotalRoomRate,
                            IsDepositAvailable = reservations.IsDepositAvailable != null ? reservations.IsDepositAvailable.Value : false,
                            IsBreakFastAvailable = reservations.IsBreakFastAvailable != null ? reservations.IsBreakFastAvailable.Value : false,
                            IsUploadComplete = allGuestsResolved
                                || ((reservations.IsUploadComplete != null && reservations.IsUploadComplete.Value) && !anyGuestDocPending),
                            VisitPurposeCode = !string.IsNullOrEmpty(ProfileList[0].VisitPurposeCode)
                                ? ProfileList[0].VisitPurposeCode
                                : reservations.VisitPurposeCode,
                        };

                        if (reservationModel.IsUploadComplete)
                        {
                            ViewBag.uploadcomplete = true;
                        }
                        else
                        {
                            ViewBag.uploadcomplete = false;
                            ViewBag.ForceDocumentResume = anyGuestDocPending;
                        }

                        // Resume mid-wizard from tbReservationMetaData (email link + QR search).
                        // Skip START splash when CompletedTabIndex >= 0 so guests land on the next step immediately.
                        await ApplyPrecheckinResumeProgressAsync(
                            reservations.ReservationNumber ?? confirmationNo,
                            ActionName,
                            ActionGroup);

                        // Mid-flow: reopen must land on Document with completed/skipped guests restored
                        if (anyGuestDocPending)
                        {
                            ViewBag.uploadcomplete = false;
                            ViewBag.SkipPrecheckinSplash = true;
                            int ci = -1;
                            int completedIdx = ViewBag.CompletedTabIndex != null
                                && int.TryParse(ViewBag.CompletedTabIndex.ToString(), out ci)
                                    ? ci
                                    : -1;
                            int ri = -1;
                            int resumeIdx = ViewBag.ResumeTabIndex != null
                                && int.TryParse(ViewBag.ResumeTabIndex.ToString(), out ri) ? ri : -1;
                            // Force Document only if guest already passed Guest Details, or was wrongly sent to Thank You
                            if (completedIdx >= 1 || resumeIdx >= 2 || resumeIdx < 0)
                                ViewBag.ResumeTabIndex = 2;
                            Helpers.LogHelper.Instance.Log(
                                $"Per-guest document resume. pendingGuests=true profiles={profiles.Count} slots={totalGuestSlots} ResumeTabIndex={ViewBag.ResumeTabIndex}",
                                reservations.ReservationNameID, ActionName, ActionGroup);
                        }

                        QRCodeGenerator qrGenerator = new QRCodeGenerator();
                        QRCodeData qrCodeData = qrGenerator.CreateQrCode(confirmationNo, QRCodeGenerator.ECCLevel.Q);
                        QRCode qrCode = new QRCode(qrCodeData);
                        Bitmap qrCodeImage = qrCode.GetGraphic(20);

                        System.IO.MemoryStream ms = new MemoryStream();
                        qrCodeImage.Save(ms, ImageFormat.Jpeg);
                        byte[] byteImage = ms.ToArray();
                        var QRCodeBase64 = Convert.ToBase64String(byteImage);

                        ViewBag.QRcode = QRCodeBase64;

                        ViewBag.AdaptorAPIBaseURL = ConfigurationManager.AppSettings["AdaptorAPIBaseURL"].ToString();
                        ViewBag.CalQRcode = "";
                        Helpers.LogHelper.Instance.Log($"Reservation  {reservations.ReservationNameID} successfully returned", $"{confirmationNo}", ActionName, ActionGroup);
                        return View(reservationModel);

                    }
                    else
                    {
                        // Reservation not found page — already completed pre-checkin
                        return ShowLinkExpiry(LinkExpiryHelper.AlreadyPreCheckedIn, confirmationNo, ActionName, ActionGroup);
                    }
                }
                else
                {
                    // Reservation not found page
                    return ShowLinkExpiry(LinkExpiryHelper.ReservationNotFound, confirmationNo, ActionName, ActionGroup);
                }
            }
            else
            {
                return ShowLinkExpiry(LinkExpiryHelper.ReservationNotFound, confirmationNo, ActionName, ActionGroup);
            }
        }

        public ActionResult ChangeLanguage(string currentLanguage, string confirmationToken)
        {
            //Set the UI culture baseon currentLanguage

            if (string.IsNullOrEmpty(currentLanguage))
                currentLanguage = "en";


            var cultureInfo = new CultureInfo(currentLanguage);
            //Thread.CurrentThread.CurrentUICulture = cultureInfo;
            Thread.CurrentThread.CurrentCulture = cultureInfo;
            //Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(cultureInfo.Name);

            Session["SelectedLanguage"] = currentLanguage;

            if (Request.Cookies.AllKeys.Contains("culture"))
            {
                Request.Cookies.Remove("culture");
            }

            HttpCookie langCookie = new HttpCookie("culture", currentLanguage);
            langCookie.Expires = DateTime.Now.AddYears(1);
            langCookie.Path = "/";
            langCookie.Domain = Request.Url.Host;
            langCookie.Secure = false;

            Response.Cookies.Remove("culture");

            Response.SetCookie(langCookie);
            Response.Cookies.Add(langCookie);
            var x = $"{Request.Url.Scheme}://{Request.Url.Authority}/Home/Index?ID={HttpUtility.UrlEncode(confirmationToken)}";
            Response.Redirect(@x);
            return null;//RedirectToAction("Index", new { id = confirmationToken });
        }
        public async Task<ActionResult> IndexPayment(string id)
        {

            string ActionName = "IndexPayment", ActionGroup = "Pre-Checkin";
            // var test = Helpers.EncryptionHelper.EncryptString("41443868");
            if (string.IsNullOrEmpty(id))
            {
                return ShowLinkExpiry(LinkExpiryHelper.MissingLink, "0", ActionName, ActionGroup);
            }
            string confirmationNo = Helpers.EncryptionHelper.DecryptString(id.ToString());

            if (string.IsNullOrEmpty(confirmationNo))
            {
                return ShowLinkExpiry(LinkExpiryHelper.InvalidLink, id, ActionName, ActionGroup);
            }

            ViewBag.ReservationFound = false;
            ViewBag.PaymentProcessed = false;
            OperaReservation operaReservation = new OperaReservation();
            Models.ReservationModel reservationModel = new Models.ReservationModel();
            reservationModel.IsDepositAvailable = false;

            MastersLogics mastersLogics = new MastersLogics();

            var CountryList = await new CloudHelper().fetchcountryMaster(ConfigurationManager.AppSettings
                    ["APIBaseUrl"].ToString(), ActionGroup);

            var reservationsDt = await new CloudHelper().FetchReservationDetailsByReferenceNumber(confirmationNo, new APIRequestModel { RequestObject = confirmationNo }, ActionGroup, ConfigurationManager.AppSettings
                    ["APIBaseUrl"].ToString());
            var reservations = new CloudReservationModel();
            if (reservationsDt?.responseData != null)
            {
                reservations = JsonConvert.DeserializeObject<List<CloudReservationModel>>(reservationsDt.responseData.ToString()).FirstOrDefault();

            }


            if (reservations != null)
            {
                //var reservationfromopera = await new CloudHelper().fetchReservationFromPMS(new Models.OWS.OwsRequestModel()
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
                //        ReservationNumber = confirmationNo

                //    }
                //}, ConfigurationManager.AppSettings["APIBaseUrl"].ToString(), "precheckin", ActionGroup);

                //if (reservationfromopera != null && reservationfromopera.Count > 0)
                {

                    //if (reservationfromopera.First().Adults != null && reservationfromopera.First().Adults.Value > 0)
                    //    operaReservation = reservationfromopera.First();



                    bool isredirectfromPaymentPage = false;
                    bool IsPaymentSuccess = false;
                    string PaymentFailureMessage = "";

                    if (TempData["IsredirectedfromPaymentPage"] != null)
                    {
                        isredirectfromPaymentPage = Convert.ToBoolean(TempData["IsredirectedfromPaymentPage"].ToString());
                        IsPaymentSuccess = Convert.ToBoolean(TempData["IsPaymentSuccess"].ToString());
                        PaymentFailureMessage = TempData["PaymentFailureMessage"].ToString();
                    }

                    ViewBag.IsredirectedfromPaymentPage = isredirectfromPaymentPage;
                    ViewBag.IsPaymentSuccess = IsPaymentSuccess;
                    ViewBag.PaymentFailureMessage = PaymentFailureMessage;

                    ViewBag.uploadcomplete = false;


                    if ((reservations.IsPreCheckedInPMS.HasValue && !reservations.IsPreCheckedInPMS.Value) || (isredirectfromPaymentPage))
                    {
                        ViewBag.ReservationFound = true;
                        if ((reservations.IsEcomchekinPaymentStaus != null && reservations.IsEcomchekinPaymentStaus.Value) || isredirectfromPaymentPage && IsPaymentSuccess)
                        {
                            Helpers.LogHelper.Instance.Log($"Reservation #{confirmationNo} payment already done.", $"{confirmationNo}", ActionName, ActionGroup);
                            ViewBag.PaymentProcessed = true;
                            ViewBag.IsPaymentSuccess = true;
                        }

                        // var Questions = reservationLogics.GetQuestions();
                        var PackageLists = await reservationLogics.GetPackages(reservations.RoomType);
                        ViewBag.PackageList = PackageLists;
                        // ViewBag.Questions = Questions;



                        Helpers.LogHelper.Instance.Log($"Getting prfile details", $"{confirmationNo}", ActionName, ActionGroup);
                        var ProfileList = await reservationLogics.GetReservationProfileList(reservations.ReservationDetailID);

                        ViewBag.Profiles = ProfileList;
                        ViewBag.CountryList = BuildCountryList(CountryList, ProfileList[0].CountryMasterID);
                        ViewBag.NationalityList = BuildNationalityList(CountryList, ProfileList[0].Nationality);
                        ViewBag.VisitPurposeCode = !string.IsNullOrEmpty(ProfileList[0].VisitPurposeCode)
                            ? ProfileList[0].VisitPurposeCode
                            : reservations.VisitPurposeCode;

                        if (ProfileList[0].CountryMasterID != null)
                        {
                            var StateList = await mastersLogics.GetStateListByCountryID(ProfileList[0].CountryMasterID.Value)
                                ?? new List<Models.StateMaster>();
                            ViewBag.StateList = BuildStateSelectList(StateList, ProfileList[0].StateMasterID);
                        }
                        else
                        {
                            List<Models.StateMaster> tbstateMasters = new List<Models.StateMaster>();
                            tbstateMasters.Add(new Models.StateMaster()
                            {
                                Statename = "Please select country",
                                StateMasterID = -1

                            });
                            ViewBag.StateList = BuildStateSelectList(tbstateMasters, null);

                        }
                        List<Models.Profile> profiles = new List<Models.Profile>();
                        int i = 0;
                        foreach (var profile in ProfileList)
                        {
                            if (profile.DocumentImage1 != null)
                            {
                                if (profile.DocumentImage1.Length > 0)
                                {
                                    i++;
                                }
                                else if (!string.IsNullOrEmpty(profile.DocumentNumber) && profile.DocumentNumber.Length > 0)
                                {
                                    i++;
                                }
                            }
                            else if (!string.IsNullOrEmpty(profile.DocumentNumber) && profile.DocumentNumber.Length > 0)
                            {
                                i++;
                            }
                            profiles.Add(new Models.Profile()
                            {
                                AddressLine1 = profile.AddressLine1,
                                AddressLine2 = profile.AddressLine2,
                                City = profile.City,
                                CountryID = profile.CountryMasterID,
                                Email = profile.Email,
                                FirstName = profile.FirstName,
                                LastName = profile.LastName,
                                Phone = profile.Phone,
                                PostalCode = profile.PostalCode != null ? profile.PostalCode : "",
                                StateID = profile.StateMasterID,
                                ProfileDetailID = profile.ProfileDetailID,
                                ProfileID = Convert.ToInt32(profile.ProfileID ?? "0"),
                                MiddleName = profile.MiddleName,
                                Nationality = profile.Nationality,
                                DocumentImage1 = profile.DocumentImage1 != null ? "0" : "1"
                            });
                        }
                        if (profiles != null)
                        {
                            if (profiles.Count() > 0 && profiles.Count() == i)
                            {
                                //reservationLogics.UpdateReservationByStage("Upload Completes", new UpdateReservationModel()
                                //{
                                //    ReservationID = reservations.ReservationDetailID
                                //});
                                //ViewBag.uploadcomplete = true;
                                //reservations.IsUploadComplete = true;
                            }
                        }

                        decimal TotalRoomRate = 0;
                        TotalRoomRate = reservations.TotalAmount.HasValue ? reservations.TotalAmount.Value : 0;


                        string ExpectedTimeofArrival = "";

                        if (reservations.ETA.HasValue && !(reservations.ETA.Value.ToString("HH:mm") == "00:00"))
                        {
                            Helpers.LogHelper.Instance.Log($"Estimate time of arrival already exist {reservations.ETA.Value.ToString("HH:mm")} ", $"{confirmationNo}", ActionName, ActionGroup);
                            DateTime timeUtc = reservations.ETA.Value;
                            ExpectedTimeofArrival = Helpers.DateTimeHelper.ConvertFromUTC(timeUtc);
                        }



                        reservationModel = new Models.ReservationModel()
                        {
                            ReservationNameID = reservations.ReservationNameID,
                            ReservationID = reservations.ReservationDetailID,
                            AdultCount = reservations.Adultcount.HasValue ? reservations.Adultcount.Value : 0,
                            ArrivalDate = reservations.ArrivalDate.HasValue ? reservations.ArrivalDate.Value.ToString("dd MMM yyyy") : "",
                            AverageRoomRate = reservations.AverageRoomRate.HasValue ? reservations.AverageRoomRate.Value : 0,
                            ChildCount = reservations.Childcount.HasValue ? reservations.Childcount.Value : 0,
                            InfantCount = reservations.InfantCount.HasValue ? reservations.InfantCount.Value : 0,
                            DepartureDate = reservations.DepartureDate.HasValue ? reservations.DepartureDate.Value.ToString("dd MMM yyyy") : "",
                            ExpectedTimeofArrival = ExpectedTimeofArrival,
                            FlightNo = reservations.FlightNo,
                            IsMembershipRequested = reservations.IsMemberShipEnrolled.HasValue ? reservations.IsMemberShipEnrolled.Value : false,
                            IsTermsAndConditions = false,
                            MembershipNo = reservations.MembershipNo,
                            ReservationNumber = reservations.ReservationNumber,
                            Profiles = profiles,
                            Questions = null,
                            RoomType = reservations.RoomType,
                            RoomTypeDescription = reservations.RoomTypeDescription,
                            TotalRoomRate = TotalRoomRate,
                            IsDepositAvailable = reservations.IsDepositAvailable != null ? reservations.IsDepositAvailable.Value : false,
                            IsBreakFastAvailable = reservations.IsBreakFastAvailable != null ? reservations.IsBreakFastAvailable.Value : false,
                            VisitPurposeCode = !string.IsNullOrEmpty(ProfileList[0].VisitPurposeCode)
                                ? ProfileList[0].VisitPurposeCode
                                : reservations.VisitPurposeCode,
                        };

                        QRCodeGenerator qrGenerator = new QRCodeGenerator();
                        QRCodeData qrCodeData = qrGenerator.CreateQrCode(confirmationNo, QRCodeGenerator.ECCLevel.Q);
                        QRCode qrCode = new QRCode(qrCodeData);
                        Bitmap qrCodeImage = qrCode.GetGraphic(20);

                        System.IO.MemoryStream ms = new MemoryStream();
                        qrCodeImage.Save(ms, ImageFormat.Jpeg);
                        byte[] byteImage = ms.ToArray();
                        var QRCodeBase64 = Convert.ToBase64String(byteImage);

                        ViewBag.QRcode = QRCodeBase64;
                        string eventName = await new ReservationLogics().GetLastEvetIDByReservation(reservations.ReservationDetailID);
                        ViewBag.IsPaymentFailed = "";
                        if (!string.IsNullOrEmpty(eventName))
                        {
                            ViewBag.IsPaymentFailed = eventName;
                        }
                        ViewBag.AdaptorAPIBaseURL = ConfigurationManager.AppSettings["AdaptorAPIBaseURL"].ToString();
                        ViewBag.CalQRcode = "";
                        Helpers.LogHelper.Instance.Log($"Reservation  {confirmationNo} successfully returned", $"{confirmationNo}", ActionName, ActionGroup);
                        return View("Index", reservationModel);

                    }
                    else
                    {
                        // Reservation not found page — already completed pre-checkin
                        return ShowLinkExpiry(LinkExpiryHelper.AlreadyPreCheckedIn, confirmationNo, ActionName, ActionGroup);
                    }
                }
                //else
                //{
                //    Helpers.LogHelper.Instance.Warn($"Reservation not found for given confirmation no {confirmationNo}", $"{confirmationNo}", ActionName, ActionGroup);
                //    // Reservation not found page
                //    return View("ReservationNotFound");
                //}
            }
            else
            {
                // Reservation not found page
                return ShowLinkExpiry(LinkExpiryHelper.ReservationNotFound, confirmationNo, ActionName, ActionGroup);
            }

        }

        public async Task<ActionResult> UpdateReservation(Models.ReservationModel reservationModel)
        {

            string ActionName = "UpdateReservation", ActionGroup = "PreCheckin";
            try
            {
                SessionDt session = new SessionDt();
                session.ReservationNameID = reservationModel.ReservationNameID;
                int count = 0;
                ReservationLogics reservationLogics = new ReservationLogics();
                var localResponse = new APIResponseModel();
                //push events to DB
                reservationLogics.InsertEvent(reservationModel.ReservationID, "Guest Details");
                string auditGuestName = reservationModel.Profiles != null && reservationModel.Profiles.Count > 0
                    ? AuditProgressHelper.BuildGuestName(
                        reservationModel.Profiles[0].FirstName,
                        reservationModel.Profiles[0].MiddleName,
                        reservationModel.Profiles[0].LastName)
                    : null;
                AuditProgressHelper.Log(
                    AuditProgressHelper.ModulePreCheckin,
                    AuditProgressHelper.Actions.GuestDetailsSaved,
                    reservationModel.ReservationID,
                    reservationModel.ReservationNameID,
                    guestName: auditGuestName);

                foreach (var profile in reservationModel.Profiles)
                {
                    if (profile.ProfileID > 0)
                    {
                        #region Update email in opera reservation 
                        if (!string.IsNullOrEmpty(profile.Email))
                        {
                            #region Update to PMS
                            bool emailUpdated = await CloudHelper.updateGuestProfileInPMS(new OWSRequestModel()
                            {
                                ChainCode = ConfigurationManager.AppSettings["ChainCode"].ToString(),
                                DestinationEntityID = ConfigurationManager.AppSettings["DestinationEntityID"].ToString(),
                                DestinationSystemType = ConfigurationManager.AppSettings["DestinationSystemType"].ToString(),
                                HotelDomain = ConfigurationManager.AppSettings["HotelDomain"].ToString(),
                                KioskID = ConfigurationManager.AppSettings
                    ["KioskID"].ToString(),
                                LegNumber = "1",
                                Language = ConfigurationManager.AppSettings
                    ["Language"].ToString(),
                                Password = ConfigurationManager.AppSettings
                    ["Password"].ToString(),
                                Username = ConfigurationManager.AppSettings
                    ["Username"].ToString(),
                                SystemType = ConfigurationManager.AppSettings
                    ["SystemType"].ToString(),
                                UpdateProileRequest = new Models.UpdateProfile()
                                {
                                    Emails = new List<Models.Email>()
                            {
                                new Models.Email()
                                {
                                    displaySequence = 1,
                                    email = profile.Email,
                                    emailType ="EMAIL",
                                    primary = true
                                }
                            },
                                    ProfileID = profile.ProfileID.ToString()
                                }
                            }, "UpdateEmailList", ConfigurationManager.AppSettings
                    ["APIBaseUrl"].ToString(), ActionGroup, session.ReservationNameID);
                            if (emailUpdated)
                            {
                                new LogHelper().Log("Opera email update: Success", reservationModel.ReservationNameID, ActionName, ActionGroup);
                                AuditProgressHelper.Log(
                                    AuditProgressHelper.ModulePreCheckin,
                                    AuditProgressHelper.Actions.OperaEmailUpdateSuccess,
                                    reservationModel.ReservationID,
                                    reservationModel.ReservationNameID,
                                    guestName: auditGuestName);
                            }
                            else
                            {
                                string emailFailReason = "PMS update returned false";
                                new LogHelper().Log("Opera email update: Failed - " + emailFailReason, reservationModel.ReservationNameID, ActionName, ActionGroup);
                                new LogHelper().Warn("Opera email update: Failed - " + emailFailReason, reservationModel.ReservationNameID, ActionName, ActionGroup);
                                AuditProgressHelper.Log(
                                    AuditProgressHelper.ModulePreCheckin,
                                    AuditProgressHelper.Actions.OperaEmailUpdateFailed,
                                    reservationModel.ReservationID,
                                    reservationModel.ReservationNameID,
                                    extraDetail: emailFailReason,
                                    guestName: auditGuestName);
                            }
                            #endregion
                        }
                        #endregion
                        #region Update phone in opera reservation stored in session
                        if (!string.IsNullOrEmpty(profile.Phone))
                        {
                            #region Update to PMS
                            bool phoneUpdated = await CloudHelper.updateGuestProfileInPMS(new OWSRequestModel()
                            {
                                ChainCode = ConfigurationManager.AppSettings["ChainCode"].ToString(),
                                DestinationEntityID = ConfigurationManager.AppSettings["DestinationEntityID"].ToString(),
                                DestinationSystemType = ConfigurationManager.AppSettings["DestinationSystemType"].ToString(),
                                HotelDomain = ConfigurationManager.AppSettings["HotelDomain"].ToString(),
                                KioskID = ConfigurationManager.AppSettings
                        ["KioskID"].ToString(),
                                LegNumber = "1",
                                Language = ConfigurationManager.AppSettings
                        ["Language"].ToString(),
                                Password = ConfigurationManager.AppSettings
                        ["Password"].ToString(),
                                Username = ConfigurationManager.AppSettings
                        ["Username"].ToString(),
                                SystemType = ConfigurationManager.AppSettings
                        ["SystemType"].ToString(),

                                UpdateProileRequest = new Models.UpdateProfile()
                                {
                                    ProfileID = profile.ProfileID.ToString(),
                                    Phones = new List<Models.Phone>()
                            {

                                new Models.Phone()
                        {
                            displaySequence = 1,
                            PhoneNumber = profile.Phone,
                            phoneRole = "PHONE",
                            phoneType = "HOME",
                            primary = true
                        }
                            },

                                }
                            }, "UpdatePhoneList", ConfigurationManager.AppSettings
                    ["APIBaseUrl"].ToString(), ActionGroup, session.ReservationNameID);
                            if (phoneUpdated)
                            {
                                new LogHelper().Log("Opera phone update: Success", reservationModel.ReservationNameID, ActionName, ActionGroup);
                                AuditProgressHelper.Log(
                                    AuditProgressHelper.ModulePreCheckin,
                                    AuditProgressHelper.Actions.OperaPhoneUpdateSuccess,
                                    reservationModel.ReservationID,
                                    reservationModel.ReservationNameID,
                                    guestName: auditGuestName);
                            }
                            else
                            {
                                string phoneFailReason = "PMS update returned false";
                                new LogHelper().Log("Opera phone update: Failed - " + phoneFailReason, reservationModel.ReservationNameID, ActionName, ActionGroup);
                                new LogHelper().Warn("Opera phone update: Failed - " + phoneFailReason, reservationModel.ReservationNameID, ActionName, ActionGroup);
                                AuditProgressHelper.Log(
                                    AuditProgressHelper.ModulePreCheckin,
                                    AuditProgressHelper.Actions.OperaPhoneUpdateFailed,
                                    reservationModel.ReservationID,
                                    reservationModel.ReservationNameID,
                                    extraDetail: phoneFailReason,
                                    guestName: auditGuestName);
                            }
                            #endregion
                        }
                        #endregion
                        #region Address 
                        var UpdateProileRequest = new Models.UpdateProfile()
                        {
                            ProfileID = profile.ProfileID.ToString(),
                            Addresses = new List<Models.Address>()
                                        {
                                            new Models.Address()
                                            {
                                                address1 = profile.AddressLine1,
                                                address2 = profile.AddressLine2,
                                                city = profile.City,
                                                state = profile.StateID!=null?await GetStatelist(profile.StateID.Value):null,
                                country = await GetCountry(profile.CountryID.Value),
                                zip = profile.PostalCode,
                                                displaySequence = 1,
                                                primary = true,
                                                addressType = "BUSINESS"
                                            }
                                        }
                        };

                        Models.OwsResponseModel owsResponse = await CloudHelper.UpdateProfileAddressAsync(session.ReservationNameID, new OWSRequestModel()
                        {
                            ChainCode = ConfigurationManager.AppSettings["ChainCode"].ToString(),
                            DestinationEntityID = ConfigurationManager.AppSettings["DestinationEntityID"].ToString(),
                            DestinationSystemType = ConfigurationManager.AppSettings["DestinationSystemType"].ToString(),
                            HotelDomain = ConfigurationManager.AppSettings["HotelDomain"].ToString(),
                            KioskID = ConfigurationManager.AppSettings
                ["KioskID"].ToString(),
                            LegNumber = "1",
                            Language = ConfigurationManager.AppSettings
                ["Language"].ToString(),
                            Password = ConfigurationManager.AppSettings
                ["Password"].ToString(),
                            Username = ConfigurationManager.AppSettings
                ["Username"].ToString(),
                            SystemType = ConfigurationManager.AppSettings
                ["SystemType"].ToString(),
                            UpdateProileRequest = UpdateProileRequest
                        }, "Pre-Checkin", ConfigurationManager.AppSettings
                ["APIBaseUrl"].ToString());

                        if (!owsResponse.result)
                        {
                            string addressFailReason = owsResponse.responseMessage ?? "unknown";
                            new LogHelper().Log("Opera address update: Failed - " + addressFailReason, reservationModel.ReservationNameID, ActionName, ActionGroup);
                            new LogHelper().Warn("Opera address update: Failed - " + addressFailReason, reservationModel.ReservationNameID, ActionName, ActionGroup);
                            AuditProgressHelper.Log(
                                AuditProgressHelper.ModulePreCheckin,
                                AuditProgressHelper.Actions.OperaAddressUpdateFailed,
                                reservationModel.ReservationID,
                                reservationModel.ReservationNameID,
                                extraDetail: addressFailReason,
                                guestName: auditGuestName);
                        }
                        else
                        {
                            new LogHelper().Log("Opera address update: Success", reservationModel.ReservationNameID, ActionName, ActionGroup);
                            AuditProgressHelper.Log(
                                AuditProgressHelper.ModulePreCheckin,
                                AuditProgressHelper.Actions.OperaAddressUpdateSuccess,
                                reservationModel.ReservationID,
                                reservationModel.ReservationNameID,
                                guestName: auditGuestName);
                        }
                        #endregion

                        #region Nationality (Opera via UpdateGuestProfile / UpdateName)
                        if (!string.IsNullOrWhiteSpace(profile.Nationality))
                        {
                            try
                            {
                                string nationalityForOpera = await GetCountryByCode(profile.Nationality);
                                Models.OWS.OwsResponseModel nationalityResponse = await new CloudHelper().UpdateGuestProfile(session.ReservationNameID, new Models.OWS.OwsRequestModel()
                                {
                                    ChainCode = ConfigurationManager.AppSettings["ChainCode"].ToString(),
                                    DestinationEntityID = ConfigurationManager.AppSettings["DestinationEntityID"].ToString(),
                                    DestinationSystemType = ConfigurationManager.AppSettings["DestinationSystemType"].ToString(),
                                    HotelDomain = ConfigurationManager.AppSettings["HotelDomain"].ToString(),
                                    KioskID = ConfigurationManager.AppSettings["KioskID"].ToString(),
                                    LegNumber = "1",
                                    Language = ConfigurationManager.AppSettings["Language"].ToString(),
                                    Password = ConfigurationManager.AppSettings["Password"].ToString(),
                                    Username = ConfigurationManager.AppSettings["Username"].ToString(),
                                    SystemType = ConfigurationManager.AppSettings["SystemType"].ToString(),
                                    UpdateProileRequest = new Models.OWS.UpdateProfile()
                                    {
                                        ProfileID = profile.ProfileID.ToString(),
                                        Nationality = nationalityForOpera
                                    }
                                }, "Pre-Checkin", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());

                                if (nationalityResponse != null && nationalityResponse.result)
                                {
                                    new LogHelper().Log("Opera nationality update: Success", reservationModel.ReservationNameID, ActionName, ActionGroup);
                                    AuditProgressHelper.Log(
                                        AuditProgressHelper.ModulePreCheckin,
                                        AuditProgressHelper.Actions.OperaNationalityUpdateSuccess,
                                        reservationModel.ReservationID,
                                        reservationModel.ReservationNameID,
                                        guestName: auditGuestName);
                                }
                                else
                                {
                                    string nationalityFailReason = nationalityResponse != null ? nationalityResponse.responseMessage : "null response";
                                    new LogHelper().Log("Opera nationality update: Failed - " + nationalityFailReason, reservationModel.ReservationNameID, ActionName, ActionGroup);
                                    new LogHelper().Warn("Opera nationality update: Failed - " + nationalityFailReason, reservationModel.ReservationNameID, ActionName, ActionGroup);
                                    AuditProgressHelper.Log(
                                        AuditProgressHelper.ModulePreCheckin,
                                        AuditProgressHelper.Actions.OperaNationalityUpdateFailed,
                                        reservationModel.ReservationID,
                                        reservationModel.ReservationNameID,
                                        extraDetail: nationalityFailReason,
                                        guestName: auditGuestName);
                                }
                            }
                            catch (Exception nationalityEx)
                            {
                                new LogHelper().Error(nationalityEx, reservationModel.ReservationNameID, ActionName, ActionGroup);
                                new LogHelper().Log("Opera nationality update: Failed - " + nationalityEx.Message, reservationModel.ReservationNameID, ActionName, ActionGroup);
                                AuditProgressHelper.Log(
                                    AuditProgressHelper.ModulePreCheckin,
                                    AuditProgressHelper.Actions.OperaNationalityUpdateFailed,
                                    reservationModel.ReservationID,
                                    reservationModel.ReservationNameID,
                                    extraDetail: nationalityEx.Message,
                                    guestName: auditGuestName);
                            }
                        }
                        #endregion
                    }

                    var resUpdateModel = new UpdateReservationModel()
                    {
                        ReservationID = reservationModel.ReservationID,
                        AddressLine1 = profile.AddressLine1,
                        AddressLine2 = profile.AddressLine2,
                        City = profile.City,
                        CountryMasterID = profile.CountryID,
                        Email = profile.Email,
                        ETA = Helpers.DateTimeHelper.ConvertToTime(reservationModel.ExpectedTimeofArrival),// reservationModel.ExpectedTimeofArrival,
                        FlightNo = reservationModel.FlightNo,
                        MembershipNo = reservationModel.MembershipNo,
                        Phone = profile.Phone,
                        PostalCode = profile.PostalCode,
                        ProfileDetailID = profile.ProfileDetailID,
                        StateMasterID = profile.StateID,
                        Nationality = profile.Nationality,
                        //SignatureImage = Convert.FromBase64String(reservationModel.SignatureBase64),
                        IsMemberShipEnrolled = reservationModel.IsMembershipRequested,
                        VisitPurposeCode = reservationModel.VisitPurposeCode
                    };

                    Helpers.LogHelper.Instance.Debug($"Profile info Json : {Newtonsoft.Json.JsonConvert.SerializeObject(resUpdateModel)}", $"{reservationModel.ReservationNumber}", ActionName, ActionGroup);

                    try
                    {
                        reservationLogics.UpdateReservationByStage("Guest Details", resUpdateModel);
                        Helpers.LogHelper.Instance.Debug($"Guest details updated successfully: {Newtonsoft.Json.JsonConvert.SerializeObject(resUpdateModel)}", $"{reservationModel.ReservationNumber}", ActionName, ActionGroup);

                        // Local nationality audit when Opera nationality was not attempted (no ProfileID)
                        if (profile.ProfileID <= 0 && !string.IsNullOrWhiteSpace(profile.Nationality))
                        {
                            new LogHelper().Log("Local nationality update: Success", reservationModel.ReservationNameID, ActionName, ActionGroup);
                            AuditProgressHelper.Log(
                                AuditProgressHelper.ModulePreCheckin,
                                AuditProgressHelper.Actions.LocalNationalityUpdateSuccess,
                                reservationModel.ReservationID,
                                reservationModel.ReservationNameID,
                                guestName: auditGuestName);
                        }
                    }
                    catch (Exception localUpdateEx)
                    {
                        Helpers.LogHelper.Instance.Error(localUpdateEx, $"{reservationModel.ReservationNumber}", ActionName, ActionGroup);
                        if (profile.ProfileID <= 0 && !string.IsNullOrWhiteSpace(profile.Nationality))
                        {
                            new LogHelper().Log("Local nationality update: Failed - " + localUpdateEx.Message, reservationModel.ReservationNameID, ActionName, ActionGroup);
                            AuditProgressHelper.Log(
                                AuditProgressHelper.ModulePreCheckin,
                                AuditProgressHelper.Actions.LocalNationalityUpdateFailed,
                                reservationModel.ReservationID,
                                reservationModel.ReservationNameID,
                                extraDetail: localUpdateEx.Message,
                                guestName: auditGuestName);
                        }
                        throw;
                    }

                }




                //var localAPIResponse = await new CloudHelper().UpdateReservationStatus(new APIRequestModel()
                //{
                //    RequestObject = new List<Models.OWS.OperaReservation> { session },
                //    SyncFromCloud = true
                //}, ConfigurationManager.AppSettings
                //    ["APIBaseUrl"], ActionGroup);
                #region Updating reservation additional details locally
                new LogHelper().Debug("Pushing reservation additional details to local DB", session.ReservationNameID, ActionName, ActionGroup);
                List<ReservationAdditionalDetails> additionalDetails = new List<ReservationAdditionalDetails>();
                additionalDetails.Add(new ReservationAdditionalDetails
                {
                    FieldName = "Place Of Birth",
                    FIeldValue = reservationModel.Profiles[0].PlaceOfBirth,
                    ResID = session.ReservationNameID
                });
                additionalDetails.Add(new ReservationAdditionalDetails
                {
                    FieldName = "Place Of Issue",
                    FIeldValue = reservationModel.Profiles[0].placeofissue,
                    ResID = session.ReservationNameID
                });
                localResponse = await new CloudHelper().PushReservationAdditionalDetails(reservationModel.ReservationNameID, new Models.APIRequestModel()
                {
                    RequestObject = additionalDetails
                }, "Pre-Checkin", ConfigurationManager.AppSettings
                   ["APIBaseUrl"]);
                if (!localResponse.result)
                {
                    new LogHelper().Log("Failed to push reservation policy with reason :- " + localResponse.responseMessage, session.ReservationNameID, ActionName, ActionGroup);
                    new LogHelper().Warn("Failed to update reservation policy with reason :- " + localResponse.responseMessage, session.ReservationNameID, ActionName, ActionGroup);
                }
                else
                    new LogHelper().Debug("reservation policy updated successfully", session.ReservationNameID, ActionName, ActionGroup);
                #endregion
                Helpers.LogHelper.Instance.Log($"Updating reservation/guest details,", $"{reservationModel.ReservationNumber}", ActionName, ActionGroup);
                AuditProgressHelper.Log(
                    AuditProgressHelper.ModulePreCheckin,
                    AuditProgressHelper.Actions.MovedToNextPage,
                    reservationModel.ReservationID,
                    reservationModel.ReservationNameID,
                    extraDetail: AuditProgressHelper.FormatPageMove("Guest Details", "Policies"),
                    guestName: auditGuestName);
                return Json(new { result = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Helpers.LogHelper.Instance.Error(ex, $"{reservationModel.ReservationNumber}", ActionName, ActionGroup);
                return Json(new { result = false }, JsonRequestBehavior.AllowGet);
            }


        }

        public async Task<ActionResult> UpdatePolicies(PoliciesModel policiesModel)
        {
            int count = 0;
            string RegcardBase64 = null;
            //push events to DB
            string ActionName = "UpdatePolicies", ActionGroup = "Pre-Checkin";
            reservationLogics.InsertEvent(policiesModel.ReservationID, "Registration Card");
            SessionDt session = new SessionDt();
            session.ReservationNameID = policiesModel.ReservationNameID;
            session.ReservationNumber = policiesModel.ReservationNumber;
            AuditProgressHelper.Log(
                AuditProgressHelper.ModulePreCheckin,
                AuditProgressHelper.Actions.PoliciesApproved,
                policiesModel.ReservationID,
                policiesModel.ReservationNameID);
            Helpers.LogHelper.Instance.Log($"Updating Signature,", $"{policiesModel.ReservationNumber}", ActionName, ActionGroup);



            #region signature update
            #region Pushing Guest Signature to Local DB
            if (!string.IsNullOrEmpty(policiesModel.Base64Signature))
            {
                try
                {
                    new LogHelper().Log("Pushing guest signature to Local DB", policiesModel.ReservationNumber, ActionName, ActionGroup);
                    byte[] guestSignature = null;
                    guestSignature = Convert.FromBase64String(policiesModel.Base64Signature);
                    var localResponse = await new CloudHelper().InsertReservationDocuments(policiesModel.ReservationNameID, new Models.APIRequestModel()
                    {
                        RequestObject = new List<Models.ReservationDocumentsDataTableModel>()
                                {
                                    new Models.ReservationDocumentsDataTableModel()
                                    {
                                        Document = guestSignature,
                                        DocumentType = "Signature",
                                        ReservationNameID = session.ReservationNameID
                                    }
                                }
                    }, "Pre-Checkin", ConfigurationManager.AppSettings
                     ["APIBaseUrl"].ToString());
                    if (!localResponse.result)
                    {
                        new LogHelper().Log("Failed to push guest signature with reason :- " + localResponse.responseMessage, policiesModel.ReservationNameID, ActionName, ActionGroup);
                        new LogHelper().Warn("Failed to push guest signature with reason :- " + localResponse.responseMessage, policiesModel.ReservationNameID, ActionName, ActionGroup);
                    }
                    else
                    {
                        new LogHelper().Log("Signature updated successfully", policiesModel?.ReservationNameID, ActionName, ActionGroup);
                        AuditProgressHelper.Log(
                            AuditProgressHelper.ModulePreCheckin,
                            AuditProgressHelper.Actions.SignatureCompleted,
                            policiesModel.ReservationID,
                            policiesModel.ReservationNameID);
                    }
                }
                catch (Exception exc)
                {
                    new LogHelper().Error(exc, policiesModel?.ReservationNameID, ActionName, ActionGroup);
                }
            }
            #endregion

            session.GuestSignedSignature = !string.IsNullOrEmpty(policiesModel.Base64Signature) ? policiesModel.Base64Signature : "";
            #endregion

            // Promotional consent (optional) -> TbPolicyDetails via scalar UpsertPolicyDetails
            bool promotionalConsent = policiesModel.CheckBox2.HasValue && policiesModel.CheckBox2.Value;
            Helpers.LogHelper.Instance.Log(
                $"Saving promotional consent CheckBox2={promotionalConsent} for ReservationID={policiesModel.ReservationID}",
                $"{policiesModel.ReservationNumber}", ActionName, ActionGroup);

            var policyResponse = await new CloudHelper().UpsertPolicyDetails(
                policiesModel.ReservationNameID,
                new Models.APIRequestModel()
                {
                    RequestObject = new
                    {
                        ResID = policiesModel.ReservationID,
                        PolicyValue = promotionalConsent,
                        PolicyType = "CheckBox2"
                    }
                },
                ActionGroup,
                ConfigurationManager.AppSettings["APIBaseUrl"].ToString());

            if (policyResponse == null || !policyResponse.result)
            {
                new LogHelper().Warn(
                    "Failed to save promotional consent to TbPolicyDetails :- " + (policyResponse != null ? policyResponse.responseMessage : "null"),
                    policiesModel?.ReservationNameID, ActionName, ActionGroup);
            }
            else
            {
                new LogHelper().Log("Promotional consent saved to TbPolicyDetails", policiesModel?.ReservationNameID, ActionName, ActionGroup);
            }

            // Allergen declaration -> tbReservationMetaData.Allergies (JSON) for RegCard CheckBox2/Allergies/OtherAllergies
            // Excursion participants JSON also stored on same meta row
            bool hasAllergies = policiesModel.HasAllergies.HasValue && policiesModel.HasAllergies.Value;
            bool excursionAccepted = policiesModel.ExcursionAccepted.HasValue && policiesModel.ExcursionAccepted.Value;
            string allergyJson = JsonConvert.SerializeObject(new
            {
                hasAllergies = hasAllergies,
                allergens = hasAllergies ? (policiesModel.Allergies ?? "") : "",
                other = hasAllergies ? (policiesModel.OtherAllergies ?? "") : ""
            });
            // Persist Aqua Sports names + participant pad signatures (do not strip; NVarChar(MAX) on API)
            string excursionParticipantsJson = "{\"participants\":[]}";
            int excursionPartCount = 0;
            int excursionSigCount = 0;
            int excursionRawLen = policiesModel.ExcursionParticipants != null ? policiesModel.ExcursionParticipants.Length : 0;
            if (excursionAccepted && !string.IsNullOrWhiteSpace(policiesModel.ExcursionParticipants))
            {
                try
                {
                    var raw = Newtonsoft.Json.Linq.JObject.Parse(policiesModel.ExcursionParticipants);
                    var parts = raw["participants"] as Newtonsoft.Json.Linq.JArray;
                    var cleaned = new Newtonsoft.Json.Linq.JArray();
                    if (parts != null)
                    {
                        foreach (var p in parts)
                        {
                            string pname = (p.Value<string>("name") ?? "").Trim();
                            if (string.IsNullOrEmpty(pname)) continue;
                            // Keep participant pad base64 as posted — never blank intentionally
                            string psig = (p.Value<string>("signature") ?? "").Trim();
                            int b64 = psig.IndexOf("base64,", StringComparison.OrdinalIgnoreCase);
                            if (b64 >= 0) psig = psig.Substring(b64 + 7);
                            if (!string.IsNullOrEmpty(psig)) excursionSigCount++;
                            cleaned.Add(new Newtonsoft.Json.Linq.JObject { ["name"] = pname, ["signature"] = psig });
                        }
                    }
                    excursionPartCount = cleaned.Count;
                    excursionParticipantsJson = new Newtonsoft.Json.Linq.JObject { ["participants"] = cleaned }.ToString(Newtonsoft.Json.Formatting.None);
                }
                catch (Exception parseEx)
                {
                    new LogHelper().Warn(
                        "ExcursionParticipants parse failed rawLen=" + excursionRawLen + " err=" + parseEx.Message,
                        policiesModel?.ReservationNameID, ActionName, ActionGroup);
                    excursionParticipantsJson = "{\"participants\":[]}";
                }
            }
            try
            {
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    string apiBase = ConfigurationManager.AppSettings["APIBaseUrl"].ToString();
                    httpClient.BaseAddress = new Uri(apiBase);
                    httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    var accessToken = AuthenticationHelper.GetAPIAccessToken();
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                    }
                    var metaRequest = new Models.APIRequestModel
                    {
                        RequestObject = new
                        {
                            ReservationNumber = policiesModel.ReservationNumber,
                            CompletedTabIndex = "1",
                            Allergies = allergyJson,
                            ExcursionParticipants = excursionParticipantsJson
                        }
                    };
                    var metaContent = new System.Net.Http.StringContent(JsonConvert.SerializeObject(metaRequest), Encoding.UTF8, "application/json");
                    var metaHttp = await httpClient.PostAsync("Local/SaveReservationMetaData", metaContent);
                    // Log lengths only — full JSON with base64 is too large and previously hid empty-sig bugs
                    new LogHelper().Log(
                        "Policies meta save status=" + (metaHttp != null && metaHttp.IsSuccessStatusCode)
                        + " allergies=" + allergyJson
                        + " excursionPartCount=" + excursionPartCount
                        + " excursionSigCount=" + excursionSigCount
                        + " excursionRawLen=" + excursionRawLen
                        + " excursionJsonLen=" + (excursionParticipantsJson != null ? excursionParticipantsJson.Length : 0),
                        policiesModel?.ReservationNameID, ActionName, ActionGroup);
                }
            }
            catch (Exception allergySaveEx)
            {
                new LogHelper().Error(allergySaveEx, policiesModel?.ReservationNameID, ActionName, ActionGroup);
            }

            // Opera profile Name UDF for allergen declaration (not a guest comment)
            if (!string.IsNullOrWhiteSpace(policiesModel.ProfileID) && policiesModel.ProfileID != "0")
            {
                List<string> allergyList = new List<string>();
                string otherAllergies = "";
                if (hasAllergies)
                {
                    if (!string.IsNullOrWhiteSpace(policiesModel.Allergies))
                    {
                        allergyList = policiesModel.Allergies
                            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(a => a.Trim())
                            .Where(a => !string.IsNullOrWhiteSpace(a))
                            .ToList();
                    }
                    otherAllergies = policiesModel.OtherAllergies ?? "";
                }
                else
                {
                    otherAllergies = "NO ALLERGIES DECLARED";
                }

                try
                {
                    var allergyUdfResponse = await new CloudHelper().UpdateProfileAllergy(
                        policiesModel.ReservationNameID,
                        new Models.OWS.OwsRequestModel()
                        {
                            ChainCode = ConfigurationManager.AppSettings["ChainCode"].ToString(),
                            DestinationEntityID = ConfigurationManager.AppSettings["DestinationEntityID"].ToString(),
                            DestinationSystemType = ConfigurationManager.AppSettings["DestinationSystemType"].ToString(),
                            HotelDomain = ConfigurationManager.AppSettings["HotelDomain"].ToString(),
                            KioskID = ConfigurationManager.AppSettings["KioskID"].ToString(),
                            LegNumber = "1",
                            Language = ConfigurationManager.AppSettings["Language"].ToString(),
                            Password = ConfigurationManager.AppSettings["Password"].ToString(),
                            Username = ConfigurationManager.AppSettings["Username"].ToString(),
                            SystemType = ConfigurationManager.AppSettings["SystemType"].ToString(),
                            UpdateProfileAllergyRequest = new Models.OWS.UpdateProfileAllergyRequest()
                            {
                                NameID = policiesModel.ProfileID,
                                Allergies = allergyList,
                                OtherAllergies = otherAllergies
                            }
                        },
                        ActionGroup,
                        ConfigurationManager.AppSettings["APIBaseUrl"].ToString());
                    new LogHelper().Log(
                        "Opera allergy UDF result=" + (allergyUdfResponse != null && allergyUdfResponse.result)
                        + " msg=" + (allergyUdfResponse != null ? allergyUdfResponse.responseMessage : "null")
                        + " allergens=" + string.Join(",", allergyList)
                        + " other=" + otherAllergies,
                        policiesModel?.ReservationNameID, ActionName, ActionGroup);
                }
                catch (Exception allergyUdfEx)
                {
                    new LogHelper().Error(allergyUdfEx, policiesModel?.ReservationNameID, ActionName, ActionGroup);
                }
            }
            else
            {
                new LogHelper().Warn("Skipped Opera allergy UDF — ProfileID missing", policiesModel?.ReservationNameID, ActionName, ActionGroup);
            }

            // Excursion disclaimer (mandatory) -> TbPolicyDetails ExcursionDisclaimer -> RegCard CheckBox3
            var excursionResponse = await new CloudHelper().UpsertPolicyDetails(
                policiesModel.ReservationNameID,
                new Models.APIRequestModel()
                {
                    RequestObject = new
                    {
                        ResID = policiesModel.ReservationID,
                        PolicyValue = excursionAccepted,
                        PolicyType = "ExcursionDisclaimer"
                    }
                },
                ActionGroup,
                ConfigurationManager.AppSettings["APIBaseUrl"].ToString());
            if (excursionResponse == null || !excursionResponse.result)
            {
                new LogHelper().Warn(
                    "Failed to save excursion disclaimer :- " + (excursionResponse != null ? excursionResponse.responseMessage : "null"),
                    policiesModel?.ReservationNameID, ActionName, ActionGroup);
            }

            AuditProgressHelper.Log(
                AuditProgressHelper.ModulePreCheckin,
                AuditProgressHelper.Actions.MovedToNextPage,
                policiesModel.ReservationID,
                policiesModel.ReservationNameID,
                extraDetail: AuditProgressHelper.FormatPageMove("Policies", "Document"));

            return Json(new
            {
                result = true,
                promotionalConsentSaved = policyResponse != null && policyResponse.result,
                allergenSaved = true,
                excursionSaved = excursionResponse != null && excursionResponse.result
            });
        }

        public async Task<ActionResult> savePackages(int ReservationID, string Packages)
        {
            SessionDt session = Session["BookingSession"] as SessionDt;
            var PackageMAsterLists = await reservationLogics.GetPackages("");
            int count = 1;
            List<Models.UpsellPackageModel> upsellPackages =
            new List<Models.UpsellPackageModel>();
            var packageList = Packages.Split(',');

            if (packageList.Count() > 0 && PackageMAsterLists.Count > 0)
            {
                foreach (var packages in packageList)
                {
                    var package = PackageMAsterLists.Where(x => x.PackageID.ToString() == packages).FirstOrDefault();
                    upsellPackages.Add(new Models.UpsellPackageModel() { ReservationNameID = session.ReservationNameID, PackageAmount = package.PackageAmount.Value.ToString(), PackageCode = package.PackageCode, PackageDesc = package.PackageDesc, PackageName = package.PackageName, IsRoomUpsell = package.IsRoomUpsell });
                }

            }

            #region Updating upsell package locally
            new LogHelper().Log("Pushing upsell selected to local DB", "", "FetchPreCheckedInReservation", "Pre-Checkin");
            var localResponse = await new CloudHelper().PushUpsellpackages("", new Models.APIRequestModel()
            {
                RequestObject = upsellPackages
            }, "Pre-Checkin", ConfigurationManager.AppSettings
                     ["APIBaseUrl"].ToString());
            if (!localResponse.result)
            {
                new LogHelper().Log("Failed to update upsell with reason :- " + localResponse.responseMessage, "", "FetchPreCheckedInReservation", "Pre-Checkin");
                new LogHelper().Warn("Failed to update upsell with reason :- " + localResponse.responseMessage, "", "FetchPreCheckedInReservation", "Pre-Checkin");
            }
            else
                new LogHelper().Log("Upsell updated successfully", "", "FetchPreCheckedInReservation", "Pre-Checkin");
            #endregion

            return Json(new { result = count > 0 });
        }

        public async Task<ActionResult> UploadDocument(UploadGuestDocumentModel uploadGuestDocumentModel, SessionDt dt)
        {
            string ActionName = "UploadDocument"; string ActionGroup = "Pre-Checkin";
            try
            {
                var files = Request.Files;
                
                int count = 0;

                //push events to DB
                SessionDt session = dt;
                reservationLogics.InsertEvent(uploadGuestDocumentModel.ReservationID, "DocumentUploadTry");
                Helpers.LogHelper.Instance.Log($"Uploading guest document", $"{dt.ReservationNameID}", ActionName, ActionGroup);


                var documentModel = new UpdateReservationModel();

                if (uploadGuestDocumentModel.documentInformation != null)
                {
                    var docInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<DocumentInformation>(uploadGuestDocumentModel.documentInformation);
                    if (docInfo != null)
                    {
                        DateTime expiryDate = new DateTime(1900, 01, 01);
                        DateTime issueDate = new DateTime(1900, 01, 01);
                        DateTime birthDate = new DateTime(1900, 01, 01);

                        if (!DateTime.TryParseExact(docInfo.issueDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out issueDate))
                        {
                            issueDate = new DateTime(1900, 01, 01);
                        }
                        if (!DateTime.TryParseExact(docInfo.expiryDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out expiryDate))
                        {
                            expiryDate = new DateTime(1900, 01, 01);
                        }
                        if (!DateTime.TryParseExact(docInfo.birthDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out birthDate))
                        {
                            birthDate = new DateTime(1900, 01, 01);
                        }

                        documentModel.Gender = docInfo.gender;
                        documentModel.IssueCountry = docInfo.issueCountry;
                        documentModel.ExpiryDate = expiryDate;
                        documentModel.IssueDate = issueDate;
                        documentModel.DocumentType = docInfo.documentType;
                        documentModel.DocumentNumber = docInfo.documentNumber;
                        documentModel.BirthDate = birthDate;
                        documentModel.Nationality = docInfo.nationality;
                        if (string.IsNullOrWhiteSpace(documentModel.Nationality))
                        {
                            documentModel.Nationality = await ResolveBookingNationalityAsync(uploadGuestDocumentModel.ReservationID);
                        }

                        documentModel.FirstName = docInfo.firstName;
                        documentModel.MiddleName = docInfo.middleName;
                        documentModel.LastName = docInfo.lastName;
                        if (string.IsNullOrEmpty(docInfo.lastName) && !string.IsNullOrEmpty(docInfo.fullName))
                        {
                            documentModel.LastName = docInfo.fullName;
                        }
                        if (!string.IsNullOrEmpty(docInfo.faceImage))
                        {
                            documentModel.FaceImage = Convert.FromBase64String(docInfo.faceImage);
                        }

                    }
                }

                documentModel.ReservationID = uploadGuestDocumentModel.ReservationID;
                documentModel.ProfileDetailID = uploadGuestDocumentModel.ProfileDetailID;


                //documentModel.DocumentImage1 = Request.Files[0].InputStream


                if (!string.IsNullOrEmpty(uploadGuestDocumentModel.Doc1Base64))
                {
                    documentModel.DocumentImage1 = Convert.FromBase64String(uploadGuestDocumentModel.Doc1Base64);
                }

                if (!string.IsNullOrEmpty(uploadGuestDocumentModel.Doc2Base64))
                {
                    documentModel.DocumentImage2 = Convert.FromBase64String(uploadGuestDocumentModel.Doc2Base64);
                }
                #region check for duplicate
                // Only treat as duplicate when a real document number was previously saved
                // for a different profile. Empty/whitespace numbers must not block uploads
                // (common after failed OCR / incomplete attempts that still created rows).
                var incomingDocumentNumber = (documentModel.DocumentNumber ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(incomingDocumentNumber))
                {
                    DataTable existingDocs =
                       await reservationLogics.CheckDuplicateDocumentByReservationID(
                        uploadGuestDocumentModel.ReservationID
                        );
                    if (existingDocs != null && existingDocs.Rows.Count > 0)
                    {
                        foreach (DataRow row in existingDocs.Rows)
                        {
                            var existingDocumentNumber = row["DocumentNumber"] == DBNull.Value
                                ? string.Empty
                                : (row["DocumentNumber"]?.ToString() ?? string.Empty).Trim();
                            if (string.IsNullOrWhiteSpace(existingDocumentNumber))
                            {
                                continue;
                            }

                            if (row["ProfileDetailID"].ToString() != uploadGuestDocumentModel.ProfileDetailID.ToString()
                                && string.Equals(existingDocumentNumber, incomingDocumentNumber, StringComparison.OrdinalIgnoreCase))
                            {
                                Helpers.LogHelper.Instance.Debug($"This document is already uploaded for another guest with Profileid:{row["ProfileDetailID"].ToString()} and doumentNumber : {incomingDocumentNumber}", $"{session.ReservationNameID}", ActionName, ActionGroup);

                                return Json(new
                                {
                                    result = false,
                                    message = "This document is already uploaded for another guest."
                                });
                            }
                        }
                    }
                }
                #endregion

                //Helpers.LogHelper.Instance.Debug($"Uploading Document Json : {Newtonsoft.Json.JsonConvert.SerializeObject(documentModel)}", $"{session.ReservationNameID}", ActionName, ActionGroup);
                new LogHelper().Log("uploadGuestDocumentModel.ProfileID - " + uploadGuestDocumentModel.ProfileID, session.ReservationNameID, ActionName, ActionGroup);

                if (string.IsNullOrEmpty(uploadGuestDocumentModel.ProfileID) || uploadGuestDocumentModel.ProfileID == "0")
                {
                    new LogHelper().Log("Creating accompanying profile in opera  - (Last name - +" + documentModel.LastName + ")", session.ReservationNameID, ActionName, ActionGroup);
                    Models.OWS.OwsResponseModel responseModel = await new CloudHelper().CreateAccompanyingProfile(session.ReservationNameID, new Models.OWS.OwsRequestModel()
                    {
                        ChainCode = ConfigurationManager.AppSettings["ChainCode"].ToString(),
                        DestinationEntityID = ConfigurationManager.AppSettings["DestinationEntityID"].ToString(),
                        HotelDomain = ConfigurationManager.AppSettings["HotelDomain"].ToString(),
                        KioskID = ConfigurationManager.AppSettings["KioskID"].ToString(),
                        Language = ConfigurationManager.AppSettings["Language"].ToString(),
                        LegNumber = "1",
                        Password = ConfigurationManager.AppSettings["Password"].ToString(),
                        SystemType = ConfigurationManager.AppSettings["SystemType"].ToString(),
                        Username = ConfigurationManager.AppSettings["Username"].ToString(),



                        CreateAccompanyingProfileRequest = new Models.OWS.CreateAccompanyingProfileRequest()
                        {
                            FirstName = documentModel.FirstName,
                            MiddleName = documentModel.MiddleName,
                            LastName = documentModel.LastName,
                            Gender = !string.IsNullOrEmpty(documentModel.Gender) ? (documentModel.Gender.ToUpper().Equals("MALE") ? "Male" : (documentModel.Gender.ToUpper().Equals("FEMALE") ? "Female" : null)) : null,
                            ReservationNumber = session.ReservationNumber
                        }
                    }, "Pre-Checkin", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());
                    if (!responseModel.result)
                    {
                        new LogHelper().Log("Failed to create accompanying profile with reason :- " + responseModel.responseMessage, session.ReservationNameID, ActionName, ActionGroup);
                        new LogHelper().Warn("Failed to fetch profile documents with reason :- " + responseModel.responseMessage, session.ReservationNameID, ActionName, ActionGroup);
                    }

                    else if (responseModel.responseData == null)
                    {
                        new LogHelper().Log("Failed to create accompanying profile with reason :- API response data is NULL" + responseModel.responseMessage, session.ReservationNameID, ActionName, ActionGroup);
                        new LogHelper().Warn("Failed to create accompanying profile with reason :- API response data is NULL" + responseModel.responseMessage, session.ReservationNameID, ActionName, ActionGroup);
                    }
                    else
                    {
                        new LogHelper().Debug("Converting accompanying profile API json to object", session.ReservationNameID, "FetchPreCheckedInReservation", "Pre-Checkin");
                        try
                        {
                            Models.OWS.GuestProfile guest = JsonConvert.DeserializeObject<Models.OWS.GuestProfile>(responseModel.responseData.ToString());
                            uploadGuestDocumentModel.ProfileID = guest.PmsProfileID;
                        }
                        catch (Exception ex)
                        {
                            new LogHelper().Error(ex, session.ReservationNameID, ActionName, ActionGroup);
                            new LogHelper().Log("Failed to covert API response to object", session.ReservationNameID, ActionName, ActionGroup);
                            new LogHelper().Warn("Failed to create accompanying profile with reason :- " + ex.Message, session.ReservationNameID, ActionName, ActionGroup);
                            new LogHelper().Debug("Failed to create accompanying profile with reason :- " + ex.Message, session.ReservationNameID, ActionName, ActionGroup);
                        }
                        new LogHelper().Log("Accompanying profile created successfully", session.ReservationNameID, ActionName, ActionGroup);
                    }
                }

                if (!string.IsNullOrEmpty(uploadGuestDocumentModel.ProfileID))
                {
                    new LogHelper().Log("Updating guest profile in opera  - (Last name - +" + documentModel.LastName + ")", session.ReservationNameID, ActionName, ActionGroup);
                    Models.OWS.OwsResponseModel owsResponse = await new CloudHelper().UpdateGuestProfile(session.ReservationNameID, new Models.OWS.OwsRequestModel()
                    {
                        ChainCode = ConfigurationManager.AppSettings["ChainCode"].ToString(),
                        DestinationEntityID = ConfigurationManager.AppSettings["DestinationEntityID"].ToString(),
                        HotelDomain = ConfigurationManager.AppSettings["HotelDomain"].ToString(),
                        KioskID = ConfigurationManager.AppSettings["KioskID"].ToString(),
                        Language = ConfigurationManager.AppSettings["Language"].ToString(),
                        LegNumber = "1",
                        Password = ConfigurationManager.AppSettings["Password"].ToString(),
                        SystemType = ConfigurationManager.AppSettings["SystemType"].ToString(),
                        Username = ConfigurationManager.AppSettings["Username"].ToString(),
                        UpdateProileRequest = new Models.OWS.UpdateProfile()
                        {
                            ProfileID = uploadGuestDocumentModel.ProfileID,
                            DOB = documentModel.BirthDate.Value,

                            DocumentNumber = documentModel.DocumentNumber,
                            DocumentType = !string.IsNullOrWhiteSpace(documentModel?.DocumentType)
                 ? await GetDocumentByCode(documentModel.DocumentType) : null,
                            Gender = !string.IsNullOrEmpty(documentModel.Gender) ? (documentModel.Gender.ToUpper().Equals("MALE") ? "Male" : (documentModel.Gender.ToUpper().Equals("FEMALE") ? "Female" : null)) : null,
                            IssueCountry = !string.IsNullOrWhiteSpace(documentModel?.IssueCountry) ? await GetCountryByCode(documentModel.IssueCountry) : null,
                            IssueDate = documentModel.IssueDate.Value,
                            Nationality = !string.IsNullOrWhiteSpace(documentModel?.Nationality) ? await GetCountryByCode(documentModel.Nationality) : null

                        }
                    }, "Pre-Checkin", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());
                    documentModel.ProfileID = uploadGuestDocumentModel.ProfileID;
                    if (!owsResponse.result)
                    {
                        new LogHelper().Log("Failed to update profile with reason :- " + owsResponse.responseMessage, session.ReservationNameID, ActionName, ActionGroup);
                        new LogHelper().Warn("Failed to update profile with reason :- " + owsResponse.responseMessage, session.ReservationNameID, ActionName, ActionGroup);
                    }
                    else
                    {
                        documentModel.ProfileID = uploadGuestDocumentModel.ProfileID;
                        new LogHelper().Log("Updated profile successfully", session.ReservationNameID, ActionName, ActionGroup);
                    }
                }
                if (!string.IsNullOrEmpty(uploadGuestDocumentModel.ProfileID))
                {
                    documentModel.ProfileID = uploadGuestDocumentModel.ProfileID;
                    new LogHelper().Log("Updating passport info in opera  - (Last name - +" + documentModel.LastName + ")", session.ReservationNameID, ActionName, ActionGroup);
                    Models.OWS.OwsResponseModel owsResponse = await new CloudHelper().UpdateGuestPassport(session.ReservationNameID, new Models.OWS.OwsRequestModel()
                    {
                        ChainCode = ConfigurationManager.AppSettings["ChainCode"].ToString(),
                        DestinationEntityID = ConfigurationManager.AppSettings["DestinationEntityID"].ToString(),
                        HotelDomain = ConfigurationManager.AppSettings["HotelDomain"].ToString(),
                        KioskID = ConfigurationManager.AppSettings["KioskID"].ToString(),
                        Language = ConfigurationManager.AppSettings["Language"].ToString(),
                        LegNumber = "1",
                        Password = ConfigurationManager.AppSettings["Password"].ToString(),
                        SystemType = ConfigurationManager.AppSettings["SystemType"].ToString(),
                        Username = ConfigurationManager.AppSettings["Username"].ToString(),
                        UpdateProileRequest = new Models.OWS.UpdateProfile()
                        {
                            ProfileID = uploadGuestDocumentModel.ProfileID,
                            DOB = documentModel.BirthDate.Value,

                            DocumentNumber = documentModel.DocumentNumber,
                            DocumentType = !string.IsNullOrWhiteSpace(documentModel?.DocumentType)
                 ? await GetDocumentByCode(documentModel.DocumentType) : null,
                            Gender = !string.IsNullOrEmpty(documentModel.Gender) ? (documentModel.Gender.ToUpper().Equals("MALE") ? "Male" : (documentModel.Gender.ToUpper().Equals("FEMALE") ? "Female" : null)) : null,
                            IssueCountry = !string.IsNullOrWhiteSpace(documentModel?.IssueCountry) ? await GetCountryByCode(documentModel.IssueCountry) : null,
                            IssueDate = documentModel.IssueDate,
                            Nationality = !string.IsNullOrWhiteSpace(documentModel?.Nationality) ? await GetCountryByCode(documentModel.Nationality) : null


                        }
                    }, "Pre-Checkin", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());

                    if (!owsResponse.result)
                    {
                        new LogHelper().Log("Failed to update passport info with reason :- " + owsResponse.responseMessage, session.ReservationNameID, ActionName, ActionGroup);
                        new LogHelper().Warn("Failed to update passport info with reason :- " + owsResponse.responseMessage, session.ReservationNameID, ActionName, ActionGroup);
                    }
                    else
                    {
                        new LogHelper().Log("Updated passport info successfully", session.ReservationNameID, ActionName, ActionGroup);
                    }
                }







                #region updateprofileby fetchingfrompms
                //OperaReservation operaReservation = new OperaReservation();
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
                //        ReservationNumber = session.ReservationNumber

                //    }
                //}, ConfigurationManager.AppSettings["APIBaseUrl"].ToString(), "precheckin", "uplod");

                //if (reservationpms != null && reservationpms.Count > 0)
                //{

                //    operaReservation = reservationpms.FirstOrDefault();
                //}

                //var localAPIResponse = await new CloudHelper().updateReservationDetailsInLocalDB(new APIRequestModel()
                //{
                //    RequestObject = new List<Models.OWS.OperaReservation> { operaReservation },
                //    SyncFromCloud = true
                //    //changed to false by jeena on 1072023
                //}, ConfigurationManager.AppSettings
                //    ["APIBaseUrl"], "upload");
                //#endregion
                //    #region Updating Profile documents
                //    new LogHelper().Log("Pushing profile documents", session.ReservationNameID, "UploadDocument", "Pre-Checkin");
                //    var localResponse = await new CloudHelper().InsertDocuments(session.ReservationNameID, new Models.APIRequestModel()
                //    {
                //        RequestObject = new List<ProfileDocuments>() {
                //                            new ProfileDocuments
                //                            {
                //                                ReservationNameID = session.ReservationNameID,
                //                                DocumentNumber = documentModel.DocumentNumber,

                //                                ExpiryDate = documentModel.ExpiryDate,
                //                                 DocumentTypeCode =!string.IsNullOrWhiteSpace(documentModel?.DocumentType)
                //     ? GetDocumentByCode(documentModel.DocumentType): null,
                //                                 IssueDate=documentModel.IssueDate,
                //                                IssueCountry = GetCountryByCode(documentModel.IssueCountry),
                //                                ProfileID = uploadGuestDocumentModel.ProfileID.ToString(),
                //                                DocumentImage1 = documentModel.DocumentImage1,
                //                                DocumentImage2 = documentModel.DocumentImage2,
                //                                DocumentImage3 = documentModel.DocumentImage3,
                //                                FaceImage = documentModel.FaceImage
                //                            }
                //                        },
                //        SyncFromCloud = false

                //    }, "Pre-Checkin", ConfigurationManager.AppSettings
                //["APIBaseUrl"].ToString());
                //    if (!localResponse.result)
                //    {
                //        new LogHelper().Log("Failed to pushing profile documents with reason :- " + localResponse.responseMessage, session.ReservationNameID, "FetchPreCheckedInReservation", "Pre-Checkin");
                //        new LogHelper().Warn("Failed to pushing profile documents with reason :- " + localResponse.responseMessage, session.ReservationNameID, "UploadDocument", "Pre-Checkin");
                //    }
                //    else
                //        new LogHelper().Log("Profile documents updated in Local DB successfully", session.ReservationNameID, "UploadDocument", "Pre-Checkin");
                #endregion
                var logRequest = JsonConvert.DeserializeObject<UpdateReservationModel>(
                                        JsonConvert.SerializeObject(documentModel)
                                        );


                if (logRequest?.DocumentImage1 != null)
                {
                    
                    logRequest.DocumentImage1 = new byte[12];
                }
                if (logRequest?.DocumentImage2 != null)
                {

                    logRequest.DocumentImage2 = new byte[12];
                }
                if (logRequest?.DocumentImage3 != null)
                {

                    logRequest.DocumentImage3 = new byte[12];
                }
                if (logRequest?.FaceImage != null)
                {

                    logRequest.FaceImage = new byte[12];
                }
                Helpers.LogHelper.Instance.Debug("Uploading Document Json :- " + JsonConvert.SerializeObject(logRequest), $"{uploadGuestDocumentModel.ReservationID}", ActionName, ActionGroup);

                // Helpers.LogHelper.Instance.Debug($"Uploading Document Json : {Newtonsoft.Json.JsonConvert.SerializeObject(documentModel)}", $"{uploadGuestDocumentModel.ReservationID}", ActionName, ActionGroup);

               await  reservationLogics.ExecuteUpdateReservationByStage("Upload", documentModel);
                AuditProgressHelper.Log(
                    AuditProgressHelper.ModulePreCheckin,
                    AuditProgressHelper.Actions.DocumentUploaded,
                    documentModel.ReservationID > 0 ? documentModel.ReservationID : (object)uploadGuestDocumentModel.ReservationID,
                    session?.ReservationNameID ?? dt?.ReservationNameID,
                    extraDetail: "ProfileDetailID=" + uploadGuestDocumentModel.ProfileDetailID);

                // Clear per-guest skip flag if this profile was previously skipped then uploaded
                if (uploadGuestDocumentModel.ProfileDetailID > 0)
                {
                    try
                    {
                        await new CloudHelper().UpdateReservationStatus(
                            session?.ReservationNameID ?? dt?.ReservationNameID,
                            new Models.APIRequestModel()
                            {
                                RequestObject = new ReservationStatusRequestModel
                                {
                                    ReservationID = session?.ReservationNameID ?? dt?.ReservationNameID,
                                    ReservationNameID = session?.ReservationNameID ?? dt?.ReservationNameID,
                                    Type = "documentSkipCleared",
                                    ProfileDetailIDs = uploadGuestDocumentModel.ProfileDetailID.ToString()
                                }
                            },
                            ActionGroup,
                            ConfigurationManager.AppSettings["APIBaseUrl"].ToString());
                    }
                    catch (Exception clearEx)
                    {
                        Helpers.LogHelper.Instance.Debug(
                            "documentSkipCleared failed: " + clearEx.Message,
                            session?.ReservationNameID ?? "", ActionName, ActionGroup);
                    }
                }

                return Json(new { result = true });
            }
            catch (Exception ex)
            {
                Helpers.LogHelper.Instance.Error(ex,"", ActionName, ActionGroup);
                return Json(new { result = false });
            }
        }

        [HttpPost]
        public async Task<ActionResult> SaveDeclaration(Answers answers, int ReservationID)
        {
            int count = 0;


            var Questions = await reservationLogics.GetQuestions();


            Helpers.LogHelper.Instance.Log($"Updating Disclaimer", $"{ReservationID}", "SaveDeclaration", "Pre-Checkin");

            foreach (var question in Questions)
            {
                if (Request.Form["Answer[" + question.QuestionID + "]"] != null)
                {
                    var questionAns = Request.Form["Answer[" + question.QuestionID + "]"].ToString();
                    if (!string.IsNullOrEmpty(questionAns))
                    {
                        reservationLogics.InsertFeedback(ReservationID, question.QuestionID, questionAns);
                    }

                }
            }
            //push events to DB
            reservationLogics.InsertEvent(ReservationID, "Disclaimer");
            var disclaimerSession = Session["BookingSession"] as SessionDt;
            AuditProgressHelper.Log(
                AuditProgressHelper.ModulePreCheckin,
                AuditProgressHelper.Actions.DisclaimerSaved,
                ReservationID,
                disclaimerSession?.ReservationNameID);
            return Json(new { result = true });
        }
        [HttpPost]
        public async Task<ActionResult> CompletePreCheckin(int ReservationID, string ReservationNameID, string ReservationNumber)
        {
            string ActionName = "CompletePreCheckin"; string ActionGroup = "Pre-Checkin";
            string refKey = string.IsNullOrEmpty(ReservationNameID) ? ReservationNumber : ReservationNameID;
            // Completion flag + confirmation email run at Thank You transition (CompletedocUploadAsync).
            // OK only logs the click and redirects quickly (do not block on status API).
            Helpers.LogHelper.Instance.Log(
                $"Thank You OK clicked. ReservationNumber={ReservationNumber}",
                refKey, ActionName, ActionGroup);

            var redirectURL = ConfigurationManager.AppSettings["PreCheckinCompleteRedirectURL"].ToString();
            return Json(new
            {
                success = true,
                redirectUrl = redirectURL
            });
        }

        /// <summary>
        /// NLog for Thank You page button clicks (OK is logged via CompletePreCheckin).
        /// </summary>
        [HttpPost]
        public ActionResult LogThankYouClick(string ButtonName, string ReservationNumber, string ReservationNameID)
        {
            string refKey = string.IsNullOrEmpty(ReservationNameID) ? ReservationNumber : ReservationNameID;
            Helpers.LogHelper.Instance.Log(
                $"Thank You button clicked: {ButtonName}. ReservationNumber={ReservationNumber}",
                refKey ?? "0", "ThankYouClick", "Pre-Checkin");
            return Json(new { result = true });
        }

        public ActionResult InsertEvent(string EventName, int reservationid)
        {
            reservationLogics.InsertEvent(reservationid, EventName);
            return Json(new { result = true });
        }

        /// <summary>
        /// Persist document-upload skip per guest (ProfileDetailIDs).
        /// FinalizeDocumentStep=true sets IsUploadComplete only when every profile is uploaded or skipped.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> DocumentUploadSkipped(
            int ReservationID,
            string ReservationNameID,
            string ReservationNumber,
            string ProfileDetailIDs = null,
            bool FinalizeDocumentStep = false)
        {
            string ActionName = "DocumentUploadSkipped", ActionGroup = "Pre-Checkin";
            try
            {
                reservationLogics.InsertEvent(ReservationID, FinalizeDocumentStep ? "DocumentUploadSkipFinalize" : "DocumentUploadSkip");
                AuditProgressHelper.Log(
                    AuditProgressHelper.ModulePreCheckin,
                    AuditProgressHelper.Actions.DocumentSkipped,
                    ReservationID,
                    ReservationNameID,
                    extraDetail: "Profiles=" + (ProfileDetailIDs ?? "") + ";Finalize=" + FinalizeDocumentStep);

                var statusResponse = await new CloudHelper().UpdateReservationStatus(
                    ReservationNameID,
                    new Models.APIRequestModel()
                    {
                        RequestObject = new ReservationStatusRequestModel
                        {
                            ReservationID = ReservationNameID,
                            ReservationNameID = ReservationNameID,
                            Type = "documentSkipped",
                            ProfileDetailIDs = ProfileDetailIDs,
                            FinalizeDocumentStep = FinalizeDocumentStep
                        }
                    },
                    ActionGroup,
                    ConfigurationManager.AppSettings["APIBaseUrl"].ToString());

                new LogHelper().Log(
                    "DocumentSkipped result=" + (statusResponse != null && statusResponse.result)
                    + " Finalize=" + FinalizeDocumentStep
                    + " Profiles=" + (ProfileDetailIDs ?? "")
                    + " ReservationNumber=" + ReservationNumber,
                    ReservationNameID, ActionName, ActionGroup);

                return Json(new { result = statusResponse != null && statusResponse.result });
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, ReservationNameID, ActionName, ActionGroup);
                return Json(new { result = false, responseMessage = ex.Message });
            }
        }

        //This is the function where adyen response will be send back
        public async Task<ActionResult> PaymentResponseFromGatewayold(string ConfirmationNo, string TransactionID)
        {
            Helpers.LogHelper.Instance.Log($"Payment response from PG after 3ds.", $"", "PaymentResponseFromGateway", "Pre-Checkin");

            PaymentLogics paymentLogics = new PaymentLogics();
            SessionDt session = Session["BookingSession"] as SessionDt;
            var paymentHistrory = await paymentLogics.GetPaymentHistory(ConfirmationNo);

            //payment Detail call
            var details = new Dictionary<string, string>();
            details.Add("MD", Request.Params["MD"]);
            details.Add("PaRes", Request.Params["PaRes"]);

            MakePaymentDetailRequestModel paymentDetailsModel = new MakePaymentDetailRequestModel()
            {
                details = details,
                paymentData = paymentHistrory.Rows.Count > 0 ? paymentHistrory.Rows[0]["PData"].ToString() : "",// "Ab02b4c0!BQABAgCCVkrX4BAddf3ZRQieRyf7zQliVQ1hjGNu2cMjlepeRHzwglanbruU833xL6S8a+lcsZtRfQ1unFUFezebpaBCtAZ8N6exja8x/gWnig+cAXzhvfcOjFkDX8AZ6NFmuoBJOk/rp39Tnn8D6iRygQiVL6/uOUhLNCgpgfHLlRKLuczQyOAZV6xl19rtllRiPdrUEIv9jKrl/Io+VRnE0XJa3QcYagdQuBSw1xk7Koe2bHAvvjAs1Jk3YPw7lpOL6/nqBtXRAt/r/P28odxQzYmcwKpxqPQcKz+we0Xsn6971qBLWXW7of/npGb50ae8AboPPFoYj03YSOnkBidTVzQnRzbaZ/n/G2CHgf2mG0ZiOpf79cUGJuY8uyik5mdRLAxLpBoTh+jw9ffmRciipHR+PUMSP3UIgmdyuFcyOQ/T7ezV2g/HwIWKeXjJxUYFV7HnDAsP/dN0cUtkPn3e1ft1nQumjZtGu0kigahtmHIvJuFFGE0YYk9ac8uzDCkEzWebwacqxbKhwcyDllsYB/25HS1dxNi3xtGGSxMQhYG1kMyTw+LB3w/xvGLLnlryl7HUAL5NqCZLwKq9RhtUj0REdNjP4d8iNIvRmkUhzCrvtZBBYz73me1+AcTgc+Biwau2ok2n2aiPe7QP/Uom118Sqhws0ouhBMDgp+O6y4MC5RARnSR7VgAJdqyykBT8xXCLAEp7ImtleSI6IkFGMEFBQTEwM0NBNTM3RUFFRDg3QzI0REQ1MzkwOUI4MEE3OEE5MjNFMzgyM0Q2OERBQ0M5NEI5RkY4MzA1REMifWkYZm0bzeyTrwSZsSpXyNhsM5eZqnptl0Vs9fvoIp5/bI8Di+mNBtkaRbxKYANCio6uLe8RdMqsIL6fRf6K1KcvkUqvlHFuctOgX2JMLHyvlr8rdU/sZ6X1yx7IhqKTrAWIRmbCzrx+9VkX7DKKTmz2dg0h4QFNX8E6hhR8R3XShvNqkbyNNuhIH9xUnC7lPo9dkyxg6Gqf6L9ctPDQtvxW0p4Z/2riBCKljvpFpRw7SheCT+QxAK5GLUGGhbquz28ybr8gybN3OLm54JvAhI0+k1SQP7rzfWkThGZ34qjRPmR+o0a4nLZYBNImVTKOdHOyNhtgPycS0LqMf5QKpzOvbtU5A8LtNM6gVqGMo6BsywILANs/aHsKIan9aLtyanojUYrQBrVEH8Pb6/bHLhzG1sgAWudPI4zlmdTGSAlQagDC6Q/uWGsiPo5fk4S96FcxskuMkcixMdihioxwxuQpgJW1l8OzX8VA8T4lpU3Zejw3EOdjN59LTpCCl9sdRJOPt4dsKtrgWWcYxdoSsYKu+JrXgHTzo3f7fcV/xzkN8im9ACwJNGLOwOIxCZK26kUSrzKIHNjtQkPOue9ofZ3AaStDaCo3PANsgI5H2gmZZFCDOD+WVYOjHEZvrphHRCkoq+CUyYRl9ysDj1c1vLRg+lNHUyHW8z8XdCLI2hTZN3TQVW+T0PhGVBSVn8cHjWJuFaPaPwfwScOUBDrpDMRzQXaS/c3ZbvLUkhpSuBSTFtVrjO06vNtVIBWXz8hl79Jn6lEWqKCrEtoLehHG+6Xvmzz517lvRzaC6MbOrYdAi3YlyXWwNxJqEfA97idShaQNFk2RIObG5KttVbr5ZBVchF4gHduCxrZEBj90vlSBObpos/WMu0ox3fzcCrRyc2k0AbAJvQ8ygt1O054uYsdZWRdswyXx3FK+mdlXLyTQH9LjcKCXEk7RBChjBEPfsSdry3C+NvfQ/MIWRvXI6+ojBiPVG287NL7OahM5xrY+VfQTRMmmePt+LZKJxXc4+HlTySoYBXXteVQ6tBdWmUmk7hycjjL8tHQINBXB5vpR5M2ZAzfdhywom3065j/X2OwhIi3Qlvf3m2CMbPgeA3AdcGcdslAfmhKG62rwRov0d52XTPbl9XFk2GPjR6OF77oV4J5Fi1euat7ZMF4VDYgpx4Ekj/897aUJVq1UmOHAlEVS7npCeWNu8SXOFt86tXiA5DSkd+9zYqoo7KmThaIny6w40/pQlW5IV3Q2uQdzyCAprwVrfij1hTGIcmP7AZQohd6Ha4Im6tmEvDZC7lPr/IOCDbQ9cNtOcNRTC+hSHXpZ0hbimDGXRdZOFeuW7NFmmn2A1aQu4mU5H++doxAZ7kjLERb0BjtUe8TUeqO2SOySoLm4wazwd6/MmR64KDCYxhE5192vps88raZrdgmOy7Vq+MqzMZkzJ6Dl8d3ITlCoSh3/LWGmMUglMtJJvATwoD+rQ5rfU13RW4r6f2w9rBovkFV9UiAQY+Hi228W5uaiBQeQzVpqNJ6hfy1rqcHJuHHo03IRY3nz0czIxQVftyi0fivsKI7jVXDLKXRKWbBATH61iDhvw54mvj8Lk+TSXBBh0dbq/2KkdVPk/1eLSQgeMXzVjXG7KA=="
            };

            using (var httpClient = new HttpClient())
            {

                string BaseURL = ConfigurationManager.AppSettings["AdaptorAPIPaymentBaseURL"].ToString();
                string APIKey = ConfigurationManager.AppSettings["APIKey"].ToString();
                string MerchantAccount = ConfigurationManager.AppSettings["MerchantAccount"].ToString();
                string CostEstimator_MCC = ConfigurationManager.AppSettings["CostEstimator_MCC"].ToString();

                MakePaymentDetailRequestModelRoot makePaymentDetailRequestModelRoot = new MakePaymentDetailRequestModelRoot()
                {
                    apiKey = APIKey,
                    MerchantAccount = MerchantAccount,
                    RequestObject = paymentDetailsModel
                };

                httpClient.BaseAddress = new Uri(BaseURL);

                string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(makePaymentDetailRequestModelRoot);

                httpClient.DefaultRequestHeaders.Clear();
                //httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //httpClient.DefaultRequestHeaders.Add("x-api-key", "AQE1hmfuXNWTK0Qc+iSDk2UuvsaOW4JDCIBZa3xF0n2mjVZdiutiFFJB8m+HZPXmoKVywMgI/xQQwV1bDb7kfNy1WIxIIkxgBw==-rODJO2F2/g0t6SNBtX135za8qsAPMapU1bIGmWrLDP8=-:=GN%5%nV5Tpj*=W");

                HttpContent requestContent = new StringContent(jsonString, Encoding.UTF8, "application/json");

                Helpers.LogHelper.Instance.Log($"Getting Payment Details from PG", $"", "PaymentResponseFromGateway", "Pre-Checkin");

                HttpResponseMessage response = await httpClient.PostAsync($"GetPaymentDetails", requestContent);

                if (response.IsSuccessStatusCode)
                {

                    string Test = await response.Content.ReadAsStringAsync();

                    Helpers.LogHelper.Instance.Debug($"Got Payment Response from PG : {Test}", $"", "PaymentResponseFromGateway", "Pre-Checkin");

                    var resposneObj = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.AdaptorAPIModels.MakePaymentResponseModel>(Test);

                    if (resposneObj != null && resposneObj.Result)
                    {
                        Helpers.LogHelper.Instance.Log($"Updating payment details to DB", $"", "PaymentResponseFromGateway", "Pre-Checkin");

                        reservationLogics.InsertPaymentData(resposneObj.ResponseObject, ConfirmationNo, paymentHistrory.Rows[0]["ReservationNameID"].ToString(), paymentHistrory.Rows[0]["TransactionID"].ToString(), paymentHistrory.Rows[0]["TransactionType"].ToString());

                        paymentLogics.SaveTransactionHistory(new InsertPaymentHistoryUspModel()
                        {
                            ReservationNameID = resposneObj.ResponseObject.MerchantRefernce.Split('-')[1],
                            PData = paymentHistrory.Rows[0]["PData"].ToString(),
                            PaRes = Request.Params["PaRes"],
                            MDData = Request.Params["MD"],
                            PSPReference = resposneObj.ResponseObject.PspReference,
                            RefusalReason = resposneObj.ResponseObject.RefusalReason,
                            ReservationNumber = resposneObj.ResponseObject.MerchantRefernce.Split('-')[0],
                            ResultCode = resposneObj.ResponseObject.ResultCode,
                            TransactionID = TransactionID,
                            TransactionType = paymentHistrory.Rows[0]["TransactionType"].ToString()
                        });

                        if (resposneObj.ResponseObject.ResultCode == "Authorised")
                        {
                            Helpers.LogHelper.Instance.Log($"Payment {resposneObj.ResponseObject.ResultCode}, Updating payment status to BD", $"", "PaymentResponseFromGateway", "Pre-Checkin");

                            #region  Precheckin Completed
                           // var reservationsDt = reservationLogics.GetReservationDetailsDT(ConfirmationNo);
                          //  var reservations = Helpers.DataTableHelper.DataTableToList<DataAccess.usp_GetReservationDetails_Result>(reservationsDt);

                            var localResponse = await new CloudHelper().PushReservationTrackLocally(session.ReservationNameID, new Models.APIRequestModel()
                            {
                                RequestObject = new Models.ReservationTrackStatus()
                                {
                                    ReservationNameID = session.ReservationNameID,
                                    ProcessType = Models.ReservationProcessType.PreCheckedInFetched.ToString(),
                                    ReservationNumber = session.ReservationNumber,
                                    ProcessStatus = "Precheckin",
                                    EmailSent = false
                                }
                            }, "Pre-Checkin", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());
                            if (localResponse.result)
                            {
                                new LogHelper().Log("Reservation track in local DB updated successfully ", session.ReservationNameID, "FetchPreCheckedInReservation", "Pre-Checkin");
                            }
                            else
                            {
                                new LogHelper().Log("Failed to update reservation track in local DB with reason :- " + localResponse.responseMessage, session.ReservationNameID, "FetchPreCheckedInReservation", "Pre-Checkin");
                            }


                            #endregion



                            //Make payment sucess flag.
                            await paymentLogics.UpdateReservationPaymentStatus(ConfirmationNo);

                            TempData["IsredirectedfromPaymentPage"] = true;
                            TempData["IsPaymentSuccess"] = true;
                            TempData["PaymentFailureMessage"] = "";


                        }
                        else
                        {
                            Helpers.LogHelper.Instance.Log($"Payment {resposneObj.ResponseObject.ResultCode}", $"", "PaymentResponseFromGateway", "Pre-Checkin");

                            //log error
                            TempData["IsredirectedfromPaymentPage"] = true;
                            TempData["IsPaymentSuccess"] = false;
                            TempData["PaymentFailureMessage"] = resposneObj.ResponseObject.RefusalReason;

                        }
                        //return Json(new { result = true, content = Test });
                    }
                    else
                    {
                        string FailureMessage = string.Empty;
                        if (resposneObj != null)
                        {
                            FailureMessage = resposneObj.ResponseMessage;
                        }

                        Helpers.LogHelper.Instance.Log($"Payment failed {FailureMessage}", $"", "PaymentResponseFromGateway", "Pre-Checkin");

                        TempData["IsredirectedfromPaymentPage"] = true;
                        TempData["IsPaymentSuccess"] = false;
                        TempData["PaymentFailureMessage"] = "Unable to complete the payment, Please try again";
                    }
                }
                else
                {
                    Helpers.LogHelper.Instance.Log($"Payment failed {response.IsSuccessStatusCode}", $"", "PaymentResponseFromGateway", "Pre-Checkin");

                    TempData["IsredirectedfromPaymentPage"] = true;
                    TempData["IsPaymentSuccess"] = false;
                    TempData["PaymentFailureMessage"] = "Unable to complete the payment, Please try again";
                }
            }

            //redirect back to registraton document processing tab
            string encConfirmationNo = Helpers.EncryptionHelper.EncryptString(ConfirmationNo);


            return RedirectToAction("IndexPayment", new { id = ConfirmationNo });

        }
        public async Task<ActionResult> PaymentResponseFromGateway(string ConfirmationNo, string TransactionID)
        {
            string ActionName = "PaymentResponseFromGateway"; string ActionGroup = "Pre-Checkin";

            List<Models.PaymentHistory> paymentHistories = new List<Models.PaymentHistory>();
            List<Models.PaymentHistory> paymentHistory = new List<Models.PaymentHistory>();
            List<Models.PaymentAdditionalInfo> paymentAdditionalInfos = new List<Models.PaymentAdditionalInfo>();
            List<Models.PaymentHeader> paymentHeaders = new List<Models.PaymentHeader>();
            List<PaymentTypeMasterModel> payments = new List<PaymentTypeMasterModel>();
            SessionDt session = new SessionDt();
            Helpers.LogHelper.Instance.Log($"Payment response from PG after 3ds.", ConfirmationNo, ActionName, ActionGroup);
            // var reservationsDt = reservationLogics.GetReservationDetailsDT(ConfirmationNo);
            //var reservations = Helpers.DataTableHelper.DataTableToList<GetReservationDetailsModel>(reservationsDt);

            var reservationsDt = await new CloudHelper().FetchReservationDetailsByReferenceNumber(ConfirmationNo, new APIRequestModel { RequestObject = ConfirmationNo }, "", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());
            var reservations = new CloudReservationModel();
            if (reservationsDt != null)
            {
                reservations = JsonConvert.DeserializeObject<List<CloudReservationModel>>(reservationsDt.responseData.ToString()).FirstOrDefault();

            }
            if (reservations != null )
            {
                var reservation = reservations;
                if (reservation != null)
                {
                    session.ReservationNumber = reservation.ReservationNumber;
                    session.ReservationNameID = reservation.ReservationNameID;
                }

            }

            PaymentLogics paymentLogics = new PaymentLogics();

            var paymentHistrory = await new CloudHelper().FetchPaymentHistory(ConfirmationNo, new APIRequestModel() { RequestObject = ConfirmationNo }, "", ConfigurationManager.AppSettings
                    ["APIBaseUrl"].ToString());
            if (paymentHistrory != null)
            {

                paymentHistory = JsonConvert.DeserializeObject<List<PaymentHistory>>(paymentHistrory.responseData.ToString());
            }
            payments = await new CloudHelper().fetchPaymentTypeMaster(ConfigurationManager.AppSettings
                   ["APIBaseUrl"].ToString(), "Pre-Checkin");
            //payment Detail call
            var details = new Dictionary<string, string>();
            details.Add("MD", Request.Params["MD"]);
            details.Add("PaRes", Request.Params["PaRes"]);

            MakePaymentDetailRequestModel paymentDetailsModel = new MakePaymentDetailRequestModel()
            {
                details = details,
                paymentData = paymentHistory != null ? paymentHistory.FirstOrDefault().PData : "",
            };

            using (var httpClient = new HttpClient())
            {

                string BaseURL = ConfigurationManager.AppSettings["AdaptorAPIPaymentBaseURL"].ToString();
                string APIKey = ConfigurationManager.AppSettings["APIKey"].ToString();
                string MerchantAccount = ConfigurationManager.AppSettings["MerchantAccount"].ToString();
                string CostEstimator_MCC = ConfigurationManager.AppSettings["CostEstimator_MCC"].ToString();

                MakePaymentDetailRequestModelRoot makePaymentDetailRequestModelRoot = new MakePaymentDetailRequestModelRoot()
                {
                    apiKey = APIKey,
                    MerchantAccount = MerchantAccount,
                    RequestObject = paymentDetailsModel,
                    RequestIdentifier = ConfirmationNo
                };

                httpClient.BaseAddress = new Uri(BaseURL);

                string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(makePaymentDetailRequestModelRoot);

                httpClient.DefaultRequestHeaders.Clear();

                var accessToken = AuthenticationHelper.GetAPIAccessToken();
                if (!string.IsNullOrEmpty(accessToken))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }
                HttpContent requestContent = new StringContent(jsonString, Encoding.UTF8, "application/json");

                Helpers.LogHelper.Instance.Log($"Getting Payment Details from PG", paymentHistory != null ? paymentHistory.FirstOrDefault().ReservationNameID : session.ReservationNameID, ActionName, ActionGroup);

                HttpResponseMessage response = await httpClient.PostAsync($"GetPaymentDetails", requestContent);

                if (response.IsSuccessStatusCode)
                {

                    string Test = await response.Content.ReadAsStringAsync();

                    Helpers.LogHelper.Instance.Debug($"Got Payment Response from PG : {Test}", ConfirmationNo, ActionName, ActionGroup);

                    var resposneObj = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.AdaptorAPIModels.MakePaymentResponseModel>(Test);

                    if (resposneObj != null && resposneObj.Result)
                    {
                        Helpers.LogHelper.Instance.Log($"Updating payment details to DB", ConfirmationNo, ActionName, ActionGroup);

                        #region create pamentdata
                        try
                        {
                            paymentHeaders.Add(new PaymentHeader
                            {
                                MaskedCardNumber = resposneObj.ResponseObject.MaskCardNumber,
                                FundingSource = resposneObj.ResponseObject.FundingSource,
                                Amount = resposneObj.ResponseObject.Amount.Value.ToString("0.00"),
                                TransactionID = paymentHistory != null ? paymentHistory.FirstOrDefault().TransactionID : TransactionID,
                                ReservationNumber = ConfirmationNo,
                                ReservationNameID = paymentHistory != null ? paymentHistory.FirstOrDefault().ReservationNameID : session.ReservationNameID,
                                ExpiryDate = resposneObj.ResponseObject.CardExpiryDate,
                                AuthorisationCode = resposneObj.ResponseObject.AuthCode,
                                Currency = resposneObj.ResponseObject.Currency,
                                RecurringIdentifier = resposneObj.ResponseObject.PaymentToken,
                                pspReferenceNumber = resposneObj.ResponseObject.PspReference,
                                ParentPspRefereceNumber = string.IsNullOrEmpty(resposneObj.ResponseObject.ParentPSPReferece) ? resposneObj.ResponseObject.PspReference : resposneObj.ResponseObject.ParentPSPReferece,
                                TransactionType = paymentHistory != null ? paymentHistory.FirstOrDefault().TransactionType : "TransactionType",
                                ResultCode = ((string.IsNullOrWhiteSpace(resposneObj?.ResponseObject?.ResultCode) || string.Equals(resposneObj?.ResponseObject?.ResultCode, "Authorised", StringComparison.OrdinalIgnoreCase)) && string.Equals(paymentHistory?.FirstOrDefault()?.TransactionType, Models.TransactionType.PreAuth.ToString(), StringComparison.OrdinalIgnoreCase)) ? "PreAuth" : resposneObj?.ResponseObject?.ResultCode,
                                ResponseMessage = resposneObj.ResponseObject.RefusalReason,
                                CardType = resposneObj.ResponseObject.CardType,
                                OperaPaymentTypeCode = resposneObj.ResponseObject.CardType != null ? payments.Where(x => x.VendorPaymentTypeCode.ToUpper() == resposneObj.ResponseObject.CardType.ToUpper()).FirstOrDefault()?.OperaPaymentTypeCode : null
                            });



                            DataTable dataTable = new DataTable();
                            dataTable.Columns.Add("KeyHeader", typeof(string));
                            dataTable.Columns.Add("KeyValue", typeof(string));
                            dataTable.Columns.Add("TransactionID", typeof(string));

                            if (resposneObj.ResponseObject.additionalInfos != null)
                            {
                                foreach (var item in resposneObj.ResponseObject.additionalInfos)
                                {
                                    paymentAdditionalInfos.Add(new PaymentAdditionalInfo
                                    {
                                        KeyHeader = item.key,
                                        KeyValue = item.value,
                                        TransactionID = paymentHistory != null ? paymentHistory.FirstOrDefault().TransactionID : TransactionID
                                    });

                                }
                            }



                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }
                        #endregion
                        #region create paymenthistory
                        paymentHistories.Add(new PaymentHistory
                        {
                            MDData = Request.Params["MD"],
                            PaRes = Request.Params["PaRes"],
                            PData = paymentHistory != null ? paymentHistory.FirstOrDefault().PData : "",
                            PSPReference = resposneObj.ResponseObject.PspReference,
                            RefusalReason = resposneObj.ResponseObject.RefusalReason,
                            ReservationNameID = resposneObj.ResponseObject.MerchantRefernce.Split('-')[1],
                            ReservationNumber = resposneObj.ResponseObject.MerchantRefernce.Split('-')[0],
                            ResultCode = resposneObj.ResponseObject.ResultCode,
                            TransactionID = TransactionID,
                            TransactionType = paymentHistory != null ? paymentHistory.FirstOrDefault().TransactionType : ""

                        });
                        #endregion
                        #region InsertPayment

                        #endregion
                        if (resposneObj.ResponseObject.ResultCode == "Authorised")
                        {
                            #region update payment to opera
                            #region Update payment in opera
                            var paymentDetails = new Models.UpdatePaymentDetails()
                            {
                                paymentHeaders = paymentHeaders,
                                paymentAdditionalInfos = paymentAdditionalInfos,
                                paymentHistories = paymentHistories
                            };
                            if (paymentDetails != null)
                            {


                                int x = 0;
                                new LogHelper().Log("Iterating the payment headers", ConfirmationNo, ActionName, ActionGroup);
                                foreach (Models.PaymentHeader paymentHeader in paymentDetails.paymentHeaders)
                                {
                                    if (paymentHeader.IsActive == null)
                                    {
                                        new LogHelper().Log("Processing the payment header with psprefernce - " + paymentHeader.pspReferenceNumber + " where IsActive falg is NULL", "", ActionName, ActionGroup);

                                        #region Update Opera
                                        if (paymentDetails.paymentHeaders[x].TransactionType.Equals(Models.TransactionType.PreAuth.ToString()))
                                        {
                                            new LogHelper().Log("Processing the payment header with psprefernce - " + paymentHeader.pspReferenceNumber + " as a pre-auth transaction", ConfirmationNo, ActionName, ActionGroup);

                                            paymentDetails.paymentHeaders[x].IsActive = true;


                                            #region Updating Card details in opera Reservation
                                            new LogHelper().Log("Updating credit card details in the reservation", ConfirmationNo, ActionName, ActionGroup);

                                            Models.OWS.OwsResponseModel owsResponse = await new CloudHelper().UpdateCardDetailsInReservationAsyn(session.ReservationNumber, new Models.OWS.OwsRequestModel()
                                            {
                                                ChainCode = ConfigurationManager.AppSettings["ChainCode"].ToString(),
                                                DestinationEntityID = ConfigurationManager.AppSettings["DestinationEntityID"].ToString(),
                                                DestinationSystemType = ConfigurationManager.AppSettings["DestinationSystemType"].ToString(),
                                                HotelDomain = ConfigurationManager.AppSettings["HotelDomain"].ToString(),
                                                KioskID = ConfigurationManager.AppSettings
                ["KioskID"].ToString(),
                                                LegNumber = "1",
                                                Language = ConfigurationManager.AppSettings
                ["Language"].ToString(),
                                                Password = ConfigurationManager.AppSettings
                ["Password"].ToString(),
                                                Username = ConfigurationManager.AppSettings
                ["Username"].ToString(),
                                                SystemType = ConfigurationManager.AppSettings
                ["SystemType"].ToString(),
                                                modifyBookingRequest = new Models.OWS.ModifyBookingRequest()
                                                {
                                                    ReservationNumber = session.ReservationNumber,
                                                    isUDFFieldSpecified = false,
                                                    updateCreditCardDetails = true,
                                                    GarunteeTypeCode = ConfigurationManager.AppSettings
                ["GarunteeTypeCode"].ToString(),// "CC",
                                                    PaymentMethod = new Models.OWS.PaymentMethod()
                                                    {
                                                        ExpiryDate = !string.IsNullOrEmpty(paymentDetails.paymentHeaders[x].ExpiryDate) ? "01/" + paymentDetails.paymentHeaders[x].ExpiryDate : null,
                                                        MaskedCardNumber = paymentDetails.paymentHeaders[x].MaskedCardNumber,
                                                        PaymentType = paymentDetails.paymentHeaders[x].OperaPaymentTypeCode

                                                    }
                                                }
                                            }, "Pre-Checkin", ConfigurationManager.AppSettings
                ["APIBaseUrl"].ToString());
                                            if (!owsResponse.result)
                                            {
                                                new LogHelper().Log("Updating credit card details in the reservation failed with reason :- " + owsResponse.responseMessage, ConfirmationNo, ActionName, ActionGroup);
                                                new LogHelper().Warn("Updating credit card details in the reservation failed with reason :- " + owsResponse.responseMessage, ConfirmationNo, ActionName, ActionGroup);
                                            }
                                            else
                                            {
                                                new LogHelper().Log("Updating credit card details in the reservation succeeded", ConfirmationNo, ActionName, ActionGroup);
                                                new LogHelper().Warn("Updating credit card details in the reservation succeeded", ConfirmationNo, ActionName, ActionGroup);
                                            }
                                            #endregion

                                            #region Updating UDF fields in Opera reservation
                                            try
                                            {
                                                new LogHelper().Log("Updating pre auth code and amount in UDF fileds", ConfirmationNo, ActionName, ActionGroup);
                                                owsResponse = await new CloudHelper().ModifyBooking(session.ReservationNameID, new Models.OWS.OwsRequestModel()
                                                {
                                                    ChainCode = ConfigurationManager.AppSettings["ChainCode"].ToString(),
                                                    DestinationEntityID = ConfigurationManager.AppSettings["DestinationEntityID"].ToString(),
                                                    DestinationSystemType = ConfigurationManager.AppSettings["DestinationSystemType"].ToString(),
                                                    HotelDomain = ConfigurationManager.AppSettings["HotelDomain"].ToString(),
                                                    KioskID = ConfigurationManager.AppSettings
                  ["KioskID"].ToString(),
                                                    LegNumber = "1",
                                                    Language = ConfigurationManager.AppSettings
                  ["Language"].ToString(),
                                                    Password = ConfigurationManager.AppSettings
                  ["Password"].ToString(),
                                                    Username = ConfigurationManager.AppSettings
                  ["Username"].ToString(),
                                                    SystemType = ConfigurationManager.AppSettings
                  ["SystemType"].ToString(),
                                                    modifyBookingRequest = new Models.OWS.ModifyBookingRequest()
                                                    {
                                                        isUDFFieldSpecified = true,
                                                        ReservationNumber = session.ReservationNumber,
                                                        uDFFields = new List<Models.OWS.UDFField>()
                                                                                {
                                                                                    new Models.OWS.UDFField()
                                                                                    {
                                                                                        FieldName  = ConfigurationManager.AppSettings["PreAuthUDF"].ToString(),
                                                                                        FieldValue = paymentHeader.pspReferenceNumber
                                                                                    },
                                                                                    new Models.OWS.UDFField()
                                                                                    {
                                                                                        FieldName  = ConfigurationManager.AppSettings["PreAuthAmntUDF"].ToString(),
                                                                                        FieldValue = paymentHeader.Amount
                                                                                    }
                                                                                }
                                                    }
                                                }, "Pre-Checkin", ConfigurationManager.AppSettings
                  ["APIBaseUrl"].ToString()
  );
                                                if (!owsResponse.result)
                                                {
                                                    new LogHelper().Log("Updating pre auth code and amount in UDF fileds failed with reason : - " + owsResponse.responseMessage, ConfirmationNo, ActionName, ActionGroup);
                                                    new LogHelper().Warn("Updating pre auth code and amount in UDF fileds failed with reason : - " + owsResponse.responseMessage, ConfirmationNo, ActionName, ActionGroup);
                                                }
                                                else
                                                    new LogHelper().Log("Updating pre auth code and amount in UDF fileds succeeded ", ConfirmationNo, ActionName, ActionGroup);
                                            }
                                            catch (Exception ex)
                                            {
                                                new LogHelper().Error(ex, ConfirmationNo, ActionName, ActionGroup);
                                            }
                                            #endregion

                                        }
                                        else if (paymentDetails.paymentHeaders[x].TransactionType.Equals(Models.TransactionType.Sale.ToString()))
                                        {
                                            paymentDetails.paymentHeaders[x].IsActive = false;


                                            #region Updating Card details in opera Reservation
                                            new LogHelper().Log("Updating credit card details in the reservation", ConfirmationNo, ActionName, ActionGroup);

                                            Models.OWS.OwsResponseModel owsResponse = await new CloudHelper().UpdateCardDetailsInReservationAsyn(session.ReservationNameID, new Models.OWS.OwsRequestModel()
                                            {
                                                ChainCode = ConfigurationManager.AppSettings["ChainCode"].ToString(),
                                                DestinationEntityID = ConfigurationManager.AppSettings["DestinationEntityID"].ToString(),
                                                DestinationSystemType = ConfigurationManager.AppSettings["DestinationSystemType"].ToString(),
                                                HotelDomain = ConfigurationManager.AppSettings["HotelDomain"].ToString(),
                                                KioskID = ConfigurationManager.AppSettings
                ["KioskID"].ToString(),
                                                LegNumber = "1",
                                                Language = ConfigurationManager.AppSettings
                ["Language"].ToString(),
                                                Password = ConfigurationManager.AppSettings
                ["Password"].ToString(),
                                                Username = ConfigurationManager.AppSettings
                ["Username"].ToString(),
                                                SystemType = ConfigurationManager.AppSettings
                ["SystemType"].ToString(),
                                                modifyBookingRequest = new Models.OWS.ModifyBookingRequest()
                                                {
                                                    ReservationNumber = session.ReservationNumber,
                                                    isUDFFieldSpecified = false,
                                                    updateCreditCardDetails = true,
                                                    GarunteeTypeCode = ConfigurationManager.AppSettings
                ["GarunteeTypeCode"].ToString(),//"CC",
                                                    PaymentMethod = new Models.OWS.PaymentMethod()
                                                    {
                                                        ExpiryDate = !string.IsNullOrEmpty(paymentDetails.paymentHeaders[x].ExpiryDate) ? "01/" + paymentDetails.paymentHeaders[x].ExpiryDate : null,
                                                        MaskedCardNumber = paymentDetails.paymentHeaders[x].MaskedCardNumber,
                                                        PaymentType = paymentDetails.paymentHeaders[x].OperaPaymentTypeCode

                                                    }
                                                }
                                            }, "Pre-Checkin", ConfigurationManager.AppSettings
                ["APIBaseUrl"].ToString());
                                            if (!owsResponse.result)
                                            {
                                                new LogHelper().Log("Updating credit card details in the reservation failed with reason :- " + owsResponse.responseMessage, ConfirmationNo, ActionName, ActionGroup);
                                                new LogHelper().Warn("Updating credit card details in the reservation failed with reason :- " + owsResponse.responseMessage, ConfirmationNo, ActionName, ActionGroup);
                                            }
                                            else
                                            {
                                                new LogHelper().Log("Updating credit card details in the reservation succeeded", ConfirmationNo, ActionName, ActionGroup);
                                                new LogHelper().Warn("Updating credit card details in the reservation succeeded", ConfirmationNo, ActionName, ActionGroup);
                                            }
                                            #endregion


                                            #region Updating UDF fields in Opera reservation
                                            try
                                            {
                                                new LogHelper().Log("Updating pre auth code and amount in UDF fileds", ConfirmationNo, ActionName, ActionGroup);
                                                owsResponse = await new CloudHelper().ModifyBooking(session.ReservationNameID, new Models.OWS.OwsRequestModel()
                                                {
                                                    ChainCode = ConfigurationManager.AppSettings["ChainCode"].ToString(),
                                                    DestinationEntityID = ConfigurationManager.AppSettings["DestinationEntityID"].ToString(),
                                                    DestinationSystemType = ConfigurationManager.AppSettings["DestinationSystemType"].ToString(),
                                                    HotelDomain = ConfigurationManager.AppSettings["HotelDomain"].ToString(),
                                                    KioskID = ConfigurationManager.AppSettings
                 ["KioskID"].ToString(),
                                                    LegNumber = "1",
                                                    Language = ConfigurationManager.AppSettings
                 ["Language"].ToString(),
                                                    Password = ConfigurationManager.AppSettings
                 ["Password"].ToString(),
                                                    Username = ConfigurationManager.AppSettings
                 ["Username"].ToString(),
                                                    SystemType = ConfigurationManager.AppSettings
                 ["SystemType"].ToString(),
                                                    modifyBookingRequest = new Models.OWS.ModifyBookingRequest()
                                                    {
                                                        isUDFFieldSpecified = true,
                                                        ReservationNumber = session.ReservationNumber,
                                                        uDFFields = new List<Models.OWS.UDFField>()
                                                                                {
                                                                                    new Models.OWS.UDFField()
                                                                                    {
                                                                                        FieldName  = ConfigurationManager.AppSettings["PreAuthUDF"].ToString(),
                                                                                        FieldValue = paymentHeader.pspReferenceNumber
                                                                                    },
                                                                                    new Models.OWS.UDFField()
                                                                                    {
                                                                                        FieldName  = ConfigurationManager.AppSettings["PreAuthAmntUDF"].ToString(),
                                                                                        FieldValue = paymentHeader.Amount
                                                                                    }
                                                                                }
                                                    }
                                                }, "Pre-Checkin", ConfigurationManager.AppSettings
                 ["APIBaseUrl"].ToString());
                                                if (!owsResponse.result)
                                                {
                                                    new LogHelper().Log("Updating pre auth code and amount in UDF fileds failed with reason : - " + owsResponse.responseMessage, ConfirmationNo, ActionName, ActionGroup);
                                                    new LogHelper().Warn("Updating pre auth code and amount in UDF fileds failed with reason : - " + owsResponse.responseMessage, ConfirmationNo, ActionName, ActionGroup);
                                                }
                                                else
                                                    new LogHelper().Log("Updating pre auth code and amount in UDF fileds succeeded ", ConfirmationNo, ActionName, ActionGroup);
                                            }
                                            catch (Exception ex)
                                            {
                                                new LogHelper().Error(ex, ConfirmationNo, ActionName, ActionGroup);
                                            }
                                            #endregion

                                            #region Posting payment in opera reservation
                                            try
                                            {
                                                new LogHelper().Log("Posting payment in the reservation", "", ActionName, ActionGroup);
                                                owsResponse = await new CloudHelper().MakePayment(session.ReservationNameID, new Models.OWS.OwsRequestModel()
                                                {
                                                    ChainCode = ConfigurationManager.AppSettings["ChainCode"].ToString(),
                                                    DestinationEntityID = ConfigurationManager.AppSettings["DestinationEntityID"].ToString(),
                                                    DestinationSystemType = ConfigurationManager.AppSettings["DestinationSystemType"].ToString(),
                                                    HotelDomain = ConfigurationManager.AppSettings["HotelDomain"].ToString(),
                                                    KioskID = ConfigurationManager.AppSettings
                 ["KioskID"].ToString(),
                                                    LegNumber = "1",
                                                    Language = ConfigurationManager.AppSettings
                 ["Language"].ToString(),
                                                    Password = ConfigurationManager.AppSettings
                 ["Password"].ToString(),
                                                    Username = ConfigurationManager.AppSettings
                 ["Username"].ToString(),
                                                    SystemType = ConfigurationManager.AppSettings
                 ["SystemType"].ToString(),
                                                    MakePaymentRequest = new Models.OWS.MakePaymentRequest()
                                                    {
                                                        Amount = Convert.ToDecimal(paymentHeader.Amount),
                                                        PaymentInfo = "Payment from web checkin",
                                                        StationID = "MCI",
                                                        WindowNumber = 1,
                                                        ReservationNameID = session.ReservationNameID,
                                                        MaskedCardNumber = paymentHeader.MaskedCardNumber.ToLower(),
                                                        PaymentRefernce = "Payment from web checkin - Sale",
                                                        PaymentTypeCode = paymentHeader.OperaPaymentTypeCode,
                                                        ApprovalCode = paymentHeader.pspReferenceNumber
                                                    }
                                                }, "Pre-Checkin", ConfigurationManager.AppSettings
                 ["APIBaseUrl"].ToString());
                                                if (!owsResponse.result)
                                                {
                                                    new LogHelper().Log("Posting payment failed with reason : - " + owsResponse.responseMessage, ConfirmationNo, ActionName, ActionGroup);
                                                    new LogHelper().Warn("Posting payment failed with reason : - " + owsResponse.responseMessage, ConfirmationNo, ActionName, ActionGroup);
                                                }
                                                else
                                                    new LogHelper().Log("Posting payment in opera reservation succeeded ", ConfirmationNo, ActionName, ActionGroup);
                                            }
                                            catch (Exception ex)
                                            {
                                                new LogHelper().Error(ex, ConfirmationNo, ActionName, ActionGroup);
                                            }
                                            #endregion
                                        }
                                        else if (paymentDetails.paymentHeaders[x].TransactionType.Equals(Models.TransactionType.Capture.ToString()))
                                        {
                                            new LogHelper().Log("Wrong payment header retuned and Is active NULL (Capture)", ConfirmationNo, ActionName, ActionGroup);
                                            paymentDetails.paymentHeaders[x].IsActive = false;
                                        }

                                        #endregion
                                    }
                                    x++;
                                }
                                new LogHelper().Log("payment details updated successfully", ConfirmationNo, ActionName, ActionGroup);

                            }
                            ////jkj
                            #endregion

                            #region Pushing Payment details in LOcal Db

                            new LogHelper().Log("Updating payment details in local DB", ConfirmationNo, ActionName, ActionGroup);
                            var localResponse = await new CloudHelper().PushPaymentDetails(session.ReservationNameID, new Models.APIRequestModel()
                            {
                                RequestObject = paymentDetails
                            }, "Pre-Checkin", ConfigurationManager.AppSettings
                      ["APIBaseUrl"].ToString());
                            if (!localResponse.result)
                            {
                                new LogHelper().Log("Failed to update payment details in Local DB with reason :- " + localResponse.responseMessage, ConfirmationNo, ActionName, ActionGroup);
                                new LogHelper().Warn("Failed to update payment details in local DB with reason :- " + localResponse.responseMessage, ConfirmationNo, ActionName, ActionGroup);
                            }
                            else
                                new LogHelper().Log("Payment details updated successfully", ConfirmationNo, ActionName, ActionGroup);
                            #endregion
                            #endregion

                            Helpers.LogHelper.Instance.Log($"Payment {resposneObj.ResponseObject.ResultCode}, Updating payment status to BD", ConfirmationNo, ActionName, ActionGroup);

                            #region  Precheckin Completed

                            #region Updating record status in Local DB
                            new LogHelper().Log("Updating the reservation status in Local DB", session.ReservationNameID, ActionName, ActionGroup);
                            localResponse = await new CloudHelper().UpdateReservationStatus(session.ReservationNameID, new Models.APIRequestModel()
                            {
                                RequestObject = new ReservationStatusRequestModel
                                {
                                    ReservationID = session.ReservationNameID,
                                    Type = "EcomInComplete"
                                }
                            }, "Pre-Checkin", ConfigurationManager.AppSettings
                  ["APIBaseUrl"].ToString());
                            if (!localResponse.result)
                            {
                                new LogHelper().Log("Updating the reservation status in Local DB with email send flag failed with reason :- " + localResponse.responseMessage, session.ReservationNameID, ActionName, ActionGroup);
                            }
                            else
                                new LogHelper().Log("Updating the reservation status in Local DB ", session.ReservationNameID, "PushDueInReservation", "Pre-Checkin");
                            #endregion
                            localResponse = await new CloudHelper().PushReservationTrackLocally(session.ReservationNameID, new Models.APIRequestModel()
                            {
                                RequestObject = new Models.ReservationTrackStatus()
                                {
                                    ReservationNameID = session.ReservationNameID,
                                    ProcessType = Models.ReservationProcessType.PreCheckedInFetched.ToString(),
                                    ReservationNumber = session.ReservationNumber,
                                    ProcessStatus = "Precheckin",
                                    EmailSent = false
                                }
                            }, "Pre-Checkin", ConfigurationManager.AppSettings
                   ["APIBaseUrl"].ToString());
                            if (localResponse.result)
                            {
                                new LogHelper().Log("Reservation track in local DB updated successfully ", session.ReservationNameID, ActionName, ActionGroup);
                            }
                            else
                            {
                                new LogHelper().Log("Failed to update reservation track in local DB with reason :- " + localResponse.responseMessage, session.ReservationNameID, ActionName, ActionGroup);
                            }


                            #endregion



                            //Make payment sucess flag.
                            // await paymentLogics.UpdateReservationPaymentStatus(ConfirmationNo);

                            TempData["IsredirectedfromPaymentPage"] = true;
                            TempData["IsPaymentSuccess"] = true;
                            TempData["PaymentFailureMessage"] = "";


                        }
                        else
                        {
                            Helpers.LogHelper.Instance.Log($"Payment {resposneObj.ResponseObject.ResultCode}", ConfirmationNo, ActionName, ActionGroup);

                            //log error
                            TempData["IsredirectedfromPaymentPage"] = true;
                            TempData["IsPaymentSuccess"] = false;
                            TempData["PaymentFailureMessage"] = resposneObj.ResponseObject.RefusalReason;
                            return RedirectToAction("IndexPayments", new { id = EncryptionHelper.EncryptString(ConfirmationNo) });


                        }
                        //return Json(new { result = true, content = Test });
                    }
                    else
                    {
                        string FailureMessage = string.Empty;
                        if (resposneObj != null)
                        {
                            FailureMessage = resposneObj.ResponseMessage;
                        }

                        Helpers.LogHelper.Instance.Log($"Payment failed {FailureMessage}", ConfirmationNo, ActionName, ActionGroup);

                        TempData["IsredirectedfromPaymentPage"] = true;
                        TempData["IsPaymentSuccess"] = false;
                        TempData["PaymentFailureMessage"] = "Unable to complete the payment, Please try again";
                        return RedirectToAction("IndexPayments", new { id = EncryptionHelper.EncryptString(ConfirmationNo) });
                    }
                }
                else
                {
                    Helpers.LogHelper.Instance.Log($"Payment failed {response.IsSuccessStatusCode}", ConfirmationNo, ActionName, ActionGroup);

                    TempData["IsredirectedfromPaymentPage"] = true;
                    TempData["IsPaymentSuccess"] = false;
                    TempData["PaymentFailureMessage"] = "Unable to complete the payment, Please try again";

                    return RedirectToAction("IndexPayments", new { id = EncryptionHelper.EncryptString(ConfirmationNo) });
                }
            }

            //redirect back to registraton document processing tab
            string encConfirmationNo = Helpers.EncryptionHelper.EncryptString(ConfirmationNo);
            return RedirectToAction("IndexPayment", new { id = encConfirmationNo });



        }
        public async Task<ActionResult> InsertPaymentResponseHeader(PaymentResponse paymentResponse, string ConfirmationNo, string ReservationNameID, string TransactionID, string TransactionType)
        {

            string ActionName = "InsertPaymentResponseHeader", ActionGroup = "Pre-Checkin";

            PaymentLogics paymentLogics = new PaymentLogics();
            List<Models.PaymentHistory> paymentHistory = new List<Models.PaymentHistory>();
            List<Models.PaymentAdditionalInfo> paymentAdditionalInfos = new List<Models.PaymentAdditionalInfo>();
            List<Models.PaymentHeader> paymentHeaders = new List<Models.PaymentHeader>();
            List<PaymentTypeMasterModel> payments = new List<PaymentTypeMasterModel>();
            Helpers.LogHelper.Instance.Log($"Updating payment details non 3ds", ConfirmationNo, ActionName, ActionGroup);

            if (paymentResponse != null)
            {
                Helpers.LogHelper.Instance.Log($"Updating payment response {Newtonsoft.Json.JsonConvert.SerializeObject(paymentResponse)}", ReservationNameID, ActionName, ActionGroup);
            }
            payments = await new CloudHelper().fetchPaymentTypeMaster(ConfigurationManager.AppSettings
                    ["APIBaseUrl"].ToString(), ActionGroup);
            bool response = false;
            #region create pamentdata
            try
            {
                paymentHeaders.Add(new PaymentHeader
                {
                    MaskedCardNumber = paymentResponse.MaskCardNumber,
                    FundingSource = paymentResponse.FundingSource,
                    Amount = paymentResponse.Amount.Value.ToString("0.00"),
                    TransactionID = TransactionID,
                    ReservationNumber = ConfirmationNo,
                    ReservationNameID = ReservationNameID,
                    ExpiryDate = paymentResponse.CardExpiryDate,
                    AuthorisationCode = paymentResponse.AuthCode,
                    Currency = paymentResponse.Currency,
                    RecurringIdentifier = paymentResponse.PaymentToken,
                    pspReferenceNumber = paymentResponse.PspReference,
                    ParentPspRefereceNumber = string.IsNullOrEmpty(paymentResponse.ParentPSPReferece) ? paymentResponse.PspReference : paymentResponse.ParentPSPReferece,
                    TransactionType = TransactionType,
                    ResultCode = (string.IsNullOrWhiteSpace(paymentResponse?.ResultCode) || string.Equals(paymentResponse?.ResultCode, "Authorised", StringComparison.OrdinalIgnoreCase)) && string.Equals(TransactionType, Models.TransactionType.PreAuth.ToString(), StringComparison.OrdinalIgnoreCase) ? "PreAuth" : paymentResponse?.ResultCode,
                    ResponseMessage = paymentResponse.RefusalReason ?? "",
                    CardType = paymentResponse.CardType,
                    OperaPaymentTypeCode = payments.Where(x => x.VendorPaymentTypeCode.ToUpper() == paymentResponse.CardType.ToUpper()).FirstOrDefault().OperaPaymentTypeCode,


                });



                DataTable dataTable = new DataTable();
                dataTable.Columns.Add("KeyHeader", typeof(string));
                dataTable.Columns.Add("KeyValue", typeof(string));
                dataTable.Columns.Add("TransactionID", typeof(string));

                if (paymentResponse.additionalInfos != null)
                {
                    foreach (var item in paymentResponse.additionalInfos)
                    {
                        paymentAdditionalInfos.Add(new PaymentAdditionalInfo
                        {
                            KeyHeader = item.key,
                            KeyValue = item.value,
                            TransactionID = TransactionID
                        });

                    }
                }

                paymentHistory.Add(new PaymentHistory
                {
                    TransactionID = TransactionID,
                    ReservationNameID = paymentResponse.MerchantRefernce.Split('-')[1],
                    ReservationNumber = paymentResponse.MerchantRefernce.Split('-')[0],

                    // PData = paymentDetailResponseModel.r,
                    PaRes = Request.Params["PaRes"],
                    MDData = Request.Params["MD"],
                    PSPReference = paymentResponse.PspReference,
                    RefusalReason = paymentResponse.RefusalReason,
                    ResultCode = paymentResponse.ResultCode,
                    TransactionType = TransactionType
                });

            }
            catch (Exception ex)
            {
                throw ex;
            }
            #endregion
            #region update payment 
            #region Update payment in opera
            var paymentDetails = new Models.UpdatePaymentDetails()
            {
                paymentHeaders = paymentHeaders,
                paymentAdditionalInfos = paymentAdditionalInfos,
                paymentHistories = paymentHistory
            };
            if (paymentDetails != null)
            {


                int x = 0;
                new LogHelper().Log("Iterating the payment headers", ReservationNameID, ActionName, ActionGroup);
                foreach (Models.PaymentHeader paymentHeader in paymentDetails.paymentHeaders)
                {
                    if (paymentHeader.IsActive == null)
                    {
                        new LogHelper().Log("Processing the payment header with psprefernce - " + paymentHeader.pspReferenceNumber + " where IsActive falg is NULL", ReservationNameID, ActionName, ActionGroup);

                        #region Update Opera
                        if (paymentDetails.paymentHeaders[x].TransactionType.Equals(Models.TransactionType.PreAuth.ToString()))
                        {
                            new LogHelper().Log("Processing the payment header with psprefernce - " + paymentHeader.pspReferenceNumber + " as a pre-auth transaction", ReservationNameID, ActionName, ActionGroup);

                            paymentDetails.paymentHeaders[x].IsActive = true;


                            #region Updating Card details in opera Reservation
                            new LogHelper().Log("Updating credit card details in the reservation", ReservationNameID, ActionName, ActionGroup);
                            Models.OWS.OwsResponseModel owsResponse = await new CloudHelper().UpdateCardDetailsInReservationAsyn(ConfirmationNo, new Models.OWS.OwsRequestModel()
                            {
                                ChainCode = ConfigurationManager.AppSettings["ChainCode"].ToString(),
                                DestinationEntityID = ConfigurationManager.AppSettings["DestinationEntityID"].ToString(),
                                DestinationSystemType = ConfigurationManager.AppSettings["DestinationSystemType"].ToString(),
                                HotelDomain = ConfigurationManager.AppSettings["HotelDomain"].ToString(),
                                KioskID = ConfigurationManager.AppSettings["KioskID"].ToString(),
                                LegNumber = "1",
                                Language = ConfigurationManager.AppSettings["Language"].ToString(),
                                Password = ConfigurationManager.AppSettings["Password"].ToString(),
                                Username = ConfigurationManager.AppSettings["Username"].ToString(),
                                SystemType = ConfigurationManager.AppSettings["SystemType"].ToString(),
                                modifyBookingRequest = new Models.OWS.ModifyBookingRequest()
                                {
                                    ReservationNumber = ConfirmationNo,
                                    isUDFFieldSpecified = false,
                                    updateCreditCardDetails = true,
                                    GarunteeTypeCode = ConfigurationManager.AppSettings["GarunteeTypeCode"].ToString(),// "CC",
                                    PaymentMethod = new Models.OWS.PaymentMethod()
                                    {
                                        ExpiryDate = !string.IsNullOrEmpty(paymentDetails.paymentHeaders[x].ExpiryDate) ? "01/" + paymentDetails.paymentHeaders[x].ExpiryDate : null,
                                        MaskedCardNumber = paymentDetails.paymentHeaders[x].MaskedCardNumber,
                                        PaymentType = paymentDetails.paymentHeaders[x].OperaPaymentTypeCode

                                    }
                                }
                            }, "Pre-Checkin", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());
                            if (!owsResponse.result)
                            {
                                new LogHelper().Log("Updating credit card details in the reservation failed with reason :- " + owsResponse.responseMessage, ReservationNameID, ActionName, ActionGroup); new LogHelper().Warn("Updating credit card details in the reservation failed with reason :- " + owsResponse.responseMessage, "", ActionName, ActionGroup);
                            }
                            else
                            {
                                new LogHelper().Log("Updating credit card details in the reservation succeeded", ReservationNameID, ActionName, ActionGroup);
                                new LogHelper().Warn("Updating credit card details in the reservation succeeded", ReservationNameID, ActionName, ActionGroup);
                            }
                            #endregion


                            #region Updating UDF fields in Opera reservation
                            try
                            {
                                new LogHelper().Log("Updating pre auth code and amount in UDF fileds", ReservationNameID, ActionName, ActionGroup);
                                owsResponse = await new CloudHelper().ModifyBooking(ReservationNameID, new Models.OWS.OwsRequestModel()
                                {
                                    ChainCode = ConfigurationManager.AppSettings["ChainCode"].ToString(),
                                    DestinationEntityID = ConfigurationManager.AppSettings["DestinationEntityID"].ToString(),
                                    DestinationSystemType = ConfigurationManager.AppSettings["DestinationSystemType"].ToString(),
                                    HotelDomain = ConfigurationManager.AppSettings["HotelDomain"].ToString(),
                                    KioskID = ConfigurationManager.AppSettings["KioskID"].ToString(),
                                    LegNumber = "1",
                                    Language = ConfigurationManager.AppSettings["Language"].ToString(),
                                    Password = ConfigurationManager.AppSettings["Password"].ToString(),
                                    Username = ConfigurationManager.AppSettings["Username"].ToString(),
                                    SystemType = ConfigurationManager.AppSettings["SystemType"].ToString(),
                                    modifyBookingRequest = new Models.OWS.ModifyBookingRequest()
                                    {
                                        isUDFFieldSpecified = true,
                                        ReservationNumber = ConfirmationNo,
                                        uDFFields = new List<Models.OWS.UDFField>()
                                                                                {
                                                                                    new Models.OWS.UDFField()
                                                                                    {
                                                                                        FieldName  = ConfigurationManager.AppSettings["PreAuthUDF"].ToString(),
                                                                                        FieldValue = paymentHeader.pspReferenceNumber
                                                                                    },
                                                                                    new Models.OWS.UDFField()
                                                                                    {
                                                                                        FieldName  = ConfigurationManager.AppSettings["PreAuthAmntUDF"].ToString(),
                                                                                        FieldValue = paymentHeader.Amount
                                                                                    }
                                                                                }
                                    }
                                }, "Pre-Checkin", ConfigurationManager.AppSettings["APIBaseUrl"].ToString()
);
                                if (!owsResponse.result)
                                {
                                    new LogHelper().Log("Updating pre auth code and amount in UDF fileds failed with reason : - " + owsResponse.responseMessage, ReservationNameID, ActionName, ActionGroup);
                                    new LogHelper().Warn("Updating pre auth code and amount in UDF fileds failed with reason : - " + owsResponse.responseMessage, ReservationNameID, ActionName, ActionGroup);
                                }
                                else
                                    new LogHelper().Log("Updating pre auth code and amount in UDF fileds succeeded ", ReservationNameID, ActionName, ActionGroup);
                            }
                            catch (Exception ex)
                            {
                                new LogHelper().Error(ex, ReservationNameID, ActionName, ActionGroup);
                            }
                            #endregion

                        }
                        else if (paymentDetails.paymentHeaders[x].TransactionType.Equals(Models.TransactionType.Sale.ToString()))
                        {
                            paymentDetails.paymentHeaders[x].IsActive = false;



                            #region Updating Card details in opera Reservation
                            new LogHelper().Log("Updating credit card details in the reservation", ReservationNameID, ActionName, ActionGroup);

                            Models.OWS.OwsResponseModel owsResponse = await new CloudHelper().UpdateCardDetailsInReservationAsyn(ConfirmationNo, new Models.OWS.OwsRequestModel()
                            {
                                ChainCode = ConfigurationManager.AppSettings["ChainCode"].ToString(),
                                DestinationEntityID = ConfigurationManager.AppSettings["DestinationEntityID"].ToString(),
                                DestinationSystemType = ConfigurationManager.AppSettings["DestinationSystemType"].ToString(),
                                HotelDomain = ConfigurationManager.AppSettings["HotelDomain"].ToString(),
                                KioskID = ConfigurationManager.AppSettings["KioskID"].ToString(),
                                LegNumber = "1",
                                Language = ConfigurationManager.AppSettings["Language"].ToString(),
                                Password = ConfigurationManager.AppSettings["Password"].ToString(),
                                Username = ConfigurationManager.AppSettings["Username"].ToString(),
                                SystemType = ConfigurationManager.AppSettings["SystemType"].ToString(),
                                modifyBookingRequest = new Models.OWS.ModifyBookingRequest()
                                {
                                    ReservationNumber = ConfirmationNo,
                                    isUDFFieldSpecified = false,
                                    updateCreditCardDetails = true,
                                    GarunteeTypeCode = ConfigurationManager.AppSettings["GarunteeTypeCode"].ToString(),//"CC",
                                    PaymentMethod = new Models.OWS.PaymentMethod()
                                    {
                                        ExpiryDate = !string.IsNullOrEmpty(paymentDetails.paymentHeaders[x].ExpiryDate) ? "01/" + paymentDetails.paymentHeaders[x].ExpiryDate : null,
                                        MaskedCardNumber = paymentDetails.paymentHeaders[x].MaskedCardNumber,
                                        PaymentType = paymentDetails.paymentHeaders[x].OperaPaymentTypeCode

                                    }
                                }
                            }, "Pre-Checkin", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());
                            if (!owsResponse.result)
                            {
                                new LogHelper().Log("Updating credit card details in the reservation failed with reason :- " + owsResponse.responseMessage, ReservationNameID, ActionName, ActionGroup); new LogHelper().Warn("Updating credit card details in the reservation failed with reason :- " + owsResponse.responseMessage, "", ActionName, ActionGroup);
                            }
                            else
                            {
                                new LogHelper().Log("Updating credit card details in the reservation succeeded", ReservationNameID, ActionName, ActionGroup);
                                new LogHelper().Warn("Updating credit card details in the reservation succeeded", ReservationNameID, ActionName, ActionGroup);
                            }
                            #endregion


                            #region Updating UDF fields in Opera reservation
                            try
                            {
                                new LogHelper().Log("Updating pre auth code and amount in UDF fileds", ReservationNameID, ActionName, ActionGroup);
                                owsResponse = await new CloudHelper().ModifyBooking(ReservationNameID, new Models.OWS.OwsRequestModel()
                                {
                                    ChainCode = ConfigurationManager.AppSettings["ChainCode"].ToString(),
                                    DestinationEntityID = ConfigurationManager.AppSettings["DestinationEntityID"].ToString(),
                                    DestinationSystemType = ConfigurationManager.AppSettings["DestinationSystemType"].ToString(),
                                    HotelDomain = ConfigurationManager.AppSettings["HotelDomain"].ToString(),
                                    KioskID = ConfigurationManager.AppSettings["KioskID"].ToString(),
                                    LegNumber = "1",
                                    Language = ConfigurationManager.AppSettings["Language"].ToString(),
                                    Password = ConfigurationManager.AppSettings["Password"].ToString(),
                                    Username = ConfigurationManager.AppSettings["Username"].ToString(),
                                    SystemType = ConfigurationManager.AppSettings["SystemType"].ToString(),
                                    modifyBookingRequest = new Models.OWS.ModifyBookingRequest()
                                    {
                                        isUDFFieldSpecified = true,
                                        ReservationNumber = ConfirmationNo,
                                        uDFFields = new List<Models.OWS.UDFField>()
                                                                                {
                                                                                    new Models.OWS.UDFField()
                                                                                    {
                                                                                        FieldName  = ConfigurationManager.AppSettings["PreAuthUDF"].ToString(),
                                                                                        FieldValue = paymentHeader.pspReferenceNumber
                                                                                    },
                                                                                    new Models.OWS.UDFField()
                                                                                    {
                                                                                        FieldName  = ConfigurationManager.AppSettings["PreAuthAmntUDF"].ToString(),
                                                                                        FieldValue = paymentHeader.Amount
                                                                                    }
                                                                                }
                                    }
                                }, "Pre-Checkin", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());
                                if (!owsResponse.result)
                                {
                                    new LogHelper().Log("Updating pre auth code and amount in UDF fileds failed with reason : - " + owsResponse.responseMessage, ReservationNameID, ActionName, ActionGroup); new LogHelper().Warn("Updating pre auth code and amount in UDF fileds failed with reason : - " + owsResponse.responseMessage, "", ActionName, ActionGroup);
                                }
                                else
                                    new LogHelper().Log("Updating pre auth code and amount in UDF fileds succeeded ", ReservationNameID, ActionName, ActionGroup);
                            }
                            catch (Exception ex)
                            {
                                new LogHelper().Error(ex, ReservationNameID, ActionName, ActionGroup);
                            }
                            #endregion

                            #region Posting payment in opera reservation
                            try
                            {
                                new LogHelper().Log("Posting payment in the reservation", ReservationNameID, ActionName, ActionGroup);
                                owsResponse = await new CloudHelper().MakePayment(ReservationNameID, new Models.OWS.OwsRequestModel()
                                {
                                    ChainCode = ConfigurationManager.AppSettings["ChainCode"].ToString(),
                                    DestinationEntityID = ConfigurationManager.AppSettings["DestinationEntityID"].ToString(),
                                    DestinationSystemType = ConfigurationManager.AppSettings["DestinationSystemType"].ToString(),
                                    HotelDomain = ConfigurationManager.AppSettings["HotelDomain"].ToString(),
                                    KioskID = ConfigurationManager.AppSettings["KioskID"].ToString(),
                                    LegNumber = "1",
                                    Language = ConfigurationManager.AppSettings["Language"].ToString(),
                                    Password = ConfigurationManager.AppSettings["Password"].ToString(),
                                    Username = ConfigurationManager.AppSettings["Username"].ToString(),
                                    SystemType = ConfigurationManager.AppSettings["SystemType"].ToString(),
                                    MakePaymentRequest = new Models.OWS.MakePaymentRequest()
                                    {
                                        Amount = Convert.ToDecimal(paymentHeader.Amount),
                                        PaymentInfo = "Payment from web checkin",
                                        StationID = "MCI",
                                        WindowNumber = 1,
                                        ReservationNameID = ReservationNameID,
                                        MaskedCardNumber = paymentHeader.MaskedCardNumber.ToLower(),
                                        PaymentRefernce = "Payment from web checkin - Sale",
                                        PaymentTypeCode = paymentHeader.OperaPaymentTypeCode,
                                        ApprovalCode = paymentHeader.pspReferenceNumber
                                    }
                                }, "Pre-Checkin", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());
                                if (!owsResponse.result)
                                {
                                    new LogHelper().Log("Posting payment failed with reason : - " + owsResponse.responseMessage, ReservationNameID, ActionName, ActionGroup);
                                    new LogHelper().Warn("Posting payment failed with reason : - " + owsResponse.responseMessage, ReservationNameID, ActionName, ActionGroup);
                                }
                                else
                                    new LogHelper().Log("Posting payment in opera reservation succeeded ", ReservationNameID, ActionName, ActionGroup);
                            }
                            catch (Exception ex)
                            {
                                new LogHelper().Error(ex, ReservationNameID, ActionName, ActionGroup);
                            }
                            #endregion
                        }
                        else if (paymentDetails.paymentHeaders[x].TransactionType.Equals(Models.TransactionType.Capture.ToString()))
                        {
                            new LogHelper().Log("Wrong payment header retuned and Is active NULL (Capture)", ReservationNameID, ActionName, ActionGroup);
                            paymentDetails.paymentHeaders[x].IsActive = false;
                        }

                        #endregion
                    }
                    x++;
                }
                new LogHelper().Log("payment details updated successfully", ReservationNameID, ActionName, ActionGroup);
            }
            ////jkj
            #endregion

            #region Pushing Payment details in LOcal Db

            new LogHelper().Log("Updating payment details in local DB", ReservationNameID, ActionName, ActionGroup);
            var localResponse = await new CloudHelper().PushPaymentDetails(ReservationNameID, new Models.APIRequestModel()
            {
                RequestObject = paymentDetails
            }, "Pre-Checkin", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());
            if (!localResponse.result)
            {
                new LogHelper().Log("Failed to update payment details in Local DB with reason :- " + localResponse.responseMessage, ReservationNameID, ActionName, ActionGroup); new LogHelper().Warn("Failed to update payment details in local DB with reason :- " + localResponse.responseMessage, "", ActionName, ActionGroup);
            }
            else
                new LogHelper().Log("Payment details updated successfully", ReservationNameID, ActionName, ActionGroup);
            #endregion
            #endregion
            if (localResponse.result)
            {
                #region Updating record status in Local DB
                new LogHelper().Log("Updating the reservation status in Local DB", ReservationNameID, ActionName, ActionGroup);
                localResponse = await new CloudHelper().UpdateReservationStatus(ReservationNameID, new Models.APIRequestModel()
                {
                    RequestObject = new ReservationStatusRequestModel
                    {
                        ReservationID = ReservationNameID,
                        Type = "EcomInComplete"
                    }
                }, "Pre-Checkin", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());
                if (!localResponse.result)
                {
                    new LogHelper().Log("Updating the reservation status in Local DB with email send flag failed with reason :- " + localResponse.responseMessage, ReservationNameID, ActionName, ActionGroup);
                }
                else
                    new LogHelper().Log("Updating the reservation status in Local DB ", ReservationNameID, ActionName, ActionGroup);
                #endregion
                #region  Precheckin Completed
                //var reservationsDt = reservationLogics.GetReservationDetailsDT(ConfirmationNo);
                //var reservations = Helpers.DataTableHelper.DataTableToList<DataAccess.usp_GetReservationDetails_Result>(reservationsDt);
                
                localResponse = await new CloudHelper().PushReservationTrackLocally(ReservationNameID, new Models.APIRequestModel()
                {
                    RequestObject = new Models.ReservationTrackStatus()
                    {
                        ReservationNameID = ReservationNameID,
                        ProcessType = Models.ReservationProcessType.PreCheckedInFetched.ToString(),
                        ReservationNumber = ConfirmationNo,
                        ProcessStatus = "Precheckin",
                        EmailSent = false
                    }
                }, "Pre-Checkin", ConfigurationManager.AppSettings
                 ["APIBaseUrl"].ToString());
                if (localResponse.result)
                {
                    new LogHelper().Log("Reservation track in local DB updated successfully ", ReservationNameID, ActionName, ActionGroup);
                }
                else
                {
                    new LogHelper().Log("Failed to update reservation track in local DB with reason :- " + localResponse.responseMessage, ReservationNameID, ActionName, ActionGroup);
                }


                #endregion


                return Json(new { result = true });
            }
            else
            {
                return Json(new { result = false });
            }
        }

        [HttpPost]
        public async Task<ActionResult> SaveAdyenPaymentDetails(MakePaymentDetailModel makePaymentDetailModel, string ConfirmationNo, long TransactionID, string ReservationNameID, string TransactionType)
        {
            SessionDt session = Session["BookingSession"] as SessionDt;
            string ActionName = "SaveAdyenPaymentDetails", ActionGroup = "Pre-Checkin";

            Helpers.LogHelper.Instance.Log($"Updating payment PData", $"", ActionName, ActionGroup);
            try
            {
                List<Models.PaymentHistory> paymentHistory = new List<Models.PaymentHistory>();

                #region create paymenthistory
                paymentHistory.Add(new PaymentHistory
                {

                    MDData = "",
                    PaRes = "",
                    PData = makePaymentDetailModel.paymentData,
                    PSPReference = "",
                    RefusalReason = "",
                    ReservationNameID = ReservationNameID,
                    ReservationNumber = ConfirmationNo,
                    ResultCode = "",
                    TransactionID = TransactionID.ToString(),
                    TransactionType = TransactionType

                });
                #endregion
                #region Pushing Payment details in LOcal Db

                new LogHelper().Log("Updating payment details in local DB", ReservationNameID, ActionName, ActionGroup);

                var localResponse = await new CloudHelper().PushPaymentHistoryDetails(ReservationNameID, new Models.APIRequestModel()
                {
                    RequestObject = paymentHistory
                }, "Pre-Checkin", ConfigurationManager.AppSettings
          ["APIBaseUrl"].ToString());
                if (!localResponse.result)
                {
                    new LogHelper().Log("Failed to update payment details in Local DB with reason :- " + localResponse.responseMessage, ReservationNameID, ActionName, ActionGroup);
                    new LogHelper().Warn("Failed to update payment details in local DB with reason :- " + localResponse.responseMessage, ReservationNameID, ActionName, ActionGroup);
                }
                else
                    new LogHelper().Log("Payment details updated successfully", ReservationNameID, ActionName, ActionGroup);
                #endregion
                return Json(new { result = true });
            }
            catch (Exception ex)
            {
                Helpers.LogHelper.Instance.Log($"Failed insert the payment details  {ex.ToString()}", ReservationNameID, ActionName, ActionGroup);
                return Json(new { result = false });
            }

        }

        public async Task<ActionResult> CalculatePreAuthAmount(string ReservationNumber, string FundingSource, string PaymentMethod)
        {
            string ActionName = "CalculatePreAuthAmount", ActionGroup = "Pre-Checkin";
            int incidentalCap = -1;
            decimal IncidentialCharge = 100;
            if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["IncidentalCapValue"].ToString()))
            {
                incidentalCap = Int32.Parse(ConfigurationManager.AppSettings["IncidentalCapValue"].ToString());
            }
            if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["IncidentalCharge"].ToString()))
            {
                IncidentialCharge = Int32.Parse(ConfigurationManager.AppSettings["IncidentalCharge"].ToString());
            }
            //Get the reservation details for room rate
           // var reservationsDt = reservationLogics.GetReservationDetailsDT(ReservationNumber);
           // var reservations = Helpers.DataTableHelper.DataTableToList<GetReservationDetailsModel>(reservationsDt);
            var reservationsDt =  await new CloudHelper().FetchReservationDetailsByReferenceNumber(ReservationNumber, new APIRequestModel { RequestObject = ReservationNumber }, "", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());
            var reservations = new CloudReservationModel();
            if (reservationsDt != null)
            {
                reservations = JsonConvert.DeserializeObject<List<CloudReservationModel>>(reservationsDt.responseData.ToString()).FirstOrDefault();

            }
            Helpers.LogHelper.Instance.Log($"Calculating Pre-Auth Amount", ReservationNumber, ActionName, ActionGroup);

            if (reservations != null )
            {
                var reservation = reservations;
                var ReservationPackages = await reservationLogics.GetReservationPackages(reservation.ReservationDetailID);
                if (ReservationPackages != null && ReservationPackages.Count > 0)
                {
                    ReservationPackages = ReservationPackages.Where(x => !x.IsRoomUpsell).ToList();
                }

                decimal packageAmount = 0;
                if (ReservationPackages != null)
                {
                    foreach (var item in ReservationPackages)
                    {
                        if (item.PackageAmount != null)
                        {
                            packageAmount += Convert.ToDecimal(item.PackageAmount.ToString());
                        }
                    }
                }

                decimal PreuthAmount = 0;
                decimal roomrate = reservation.TotalAmount != null ? reservation.TotalAmount.Value : 0;
                int paxCount = reservation.Adultcount != null ? reservation.Adultcount.Value : 1;
                //decimal IncidentialCharge = 100;
                int NoofNights = (reservation.DepartureDate.Value - reservation.ArrivalDate.Value).Days;

                if (NoofNights == 0)
                {
                    NoofNights = 1;
                }

                List<string> AuthoRuleOneReservationTypes = new List<string>();
                AuthoRuleOneReservationTypes.Add("GCC");

                AuthoRuleOneReservationTypes.Add("NON");

                List<string> AuthoRuleTwoReservationTypes = new List<string>();
                AuthoRuleTwoReservationTypes.Add("GRD");
                AuthoRuleTwoReservationTypes.Add("DEP");
                AuthoRuleTwoReservationTypes.Add("TA");
                AuthoRuleTwoReservationTypes.Add("GCO");
                AuthoRuleTwoReservationTypes.Add("PPO");

                string ReservationType = reservation.ReservationSource != null ? reservation.ReservationSource : "";


                if (AuthoRuleOneReservationTypes.Any(x => x.Contains(ReservationType)))
                {
                    //Rule 1

                    if (FundingSource == "CREDIT")
                    {
                        if (!string.IsNullOrEmpty(PaymentMethod) && (PaymentMethod.ToUpper().Contains("JCB") || PaymentMethod.ToUpper().Contains("CUP")))
                        {
                            PreuthAmount = roomrate;

                        }
                        else
                        {
                            PreuthAmount = roomrate + (NoofNights * IncidentialCharge);
                        }
                    }
                    else
                    {

                        PreuthAmount = roomrate;
                    }

                }
                else if (AuthoRuleTwoReservationTypes.Any(x => x.Contains(reservation.ReservationSource)))
                {
                    //Rule 2

                    if (FundingSource == "CREDIT")
                    {
                        if (!string.IsNullOrEmpty(PaymentMethod) && (PaymentMethod.ToUpper() == "JCB" || PaymentMethod.ToUpper() == "CUP"))
                        {
                            PreuthAmount = 0;

                        }
                        else
                        {
                            PreuthAmount = (NoofNights * IncidentialCharge);
                        }
                    }
                    else
                    {
                        PreuthAmount = 0;
                    }

                    roomrate = 0;

                }
                else
                {
                    //Default Rule
                    //PreuthAmount = NoofNights * (roomrate + IncidentialCharge);
                    //PreuthAmount = roomrate + (NoofNights * IncidentialCharge);//as room rate include all night charge formula changed
                    PreuthAmount = 0; //Changed as per the  support ticket number 418
                }



                if (PreuthAmount > incidentalCap)
                {
                    PreuthAmount = incidentalCap;
                }

                //if any packages selected in previously 
                PreuthAmount += packageAmount;

                Helpers.LogHelper.Instance.Log($"Calculated pre-auth Amount  {PreuthAmount}", ReservationNumber, ActionName, ActionGroup);

                return Json(new { result = true, PreuthAmount = PreuthAmount, IncidentialCharge = IncidentialCharge, RoomRate = (roomrate + packageAmount), NoofNights = NoofNights });
            }
            return Json(new { result = false, AmountToCharge = 0 });
        }

        public async Task<ActionResult> ValidateDocumentIssueCountry(string idType, string issueCountry)
        {
            string ActionName = "ValidateDocumentIssueCountry", ActionGroup = "Pre-Checkin";
            Helpers.LogHelper.Instance.Log($"Validating Document Type {idType} and Issue Country {issueCountry} ", $"", ActionName, ActionGroup);
            MastersLogics mastersLogics = new MastersLogics();
            var ResponseDataTable = await mastersLogics.validateDocumentIssueCountry(idType, issueCountry);
            if (ResponseDataTable != null && ResponseDataTable.Rows.Count > 0)
            {
                if (ResponseDataTable.Rows[0][0].ToString() == "1")
                {
                    Helpers.LogHelper.Instance.Log($"Validating Document Type {idType} and Issue Country {issueCountry} is valid", $"", ActionName, ActionGroup);
                    return Json(new { result = true });
                }
                else
                {
                    Helpers.LogHelper.Instance.Log($"Validating Document Type {idType} and Issue Country {issueCountry} is not valid", $"", ActionName, ActionGroup);
                    return Json(new { result = false });
                }
            }
            else
            {
                Helpers.LogHelper.Instance.Log($"Validating Document Type {idType} and Issue Country {issueCountry} is not valid", $"", ActionName, ActionGroup);
                return Json(new { result = false });
            }
        }


        public async Task<string> GetStatelist(int Id)
        {
            var StateList = await new MastersLogics().GetStateListByCountryID(Id);
            if (StateList != null && StateList.Count() > 0)
            {
                return StateList.FirstOrDefault().StateCode;
            }

            return "";
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
        public async Task<string> GetCountryByCode(string Id)
        {
            if (string.IsNullOrWhiteSpace(Id))
            {
                return "";
            }

            var countryList = await new MastersLogics().GetCountryList();
            Helpers.LogHelper.Instance.Log($"GetCountryByCode by ID " + Id + " countryList count " + (countryList?.Count ?? 0), $"", "GetCountryByCode", "GuestPortal");
            if (countryList != null && countryList.Count > 0)
            {
                var normalized = Id.Trim();
                var countrydetails = countryList.FirstOrDefault(x =>
                    string.Equals(x.Country_Full_name, normalized, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(x.Country_3Char_code, normalized, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(x.Country_2Char_code, normalized, StringComparison.OrdinalIgnoreCase));

                if (countrydetails != null)
                {
                    return countrydetails.Country_2Char_code;
                }
            }

            return "";
        }

        public async Task<string> GetDocumentByCode(string Id)
        {

            var DocumentList = await new MastersLogics().GetDocumentList();
            if (DocumentList != null && DocumentList.Count() > 0)
            {
                var documentdetails = DocumentList.Where(x => x.DocumentCode == Id).ToList();
                if (documentdetails != null && documentdetails.Count() > 0)
                {
                    return documentdetails.FirstOrDefault().OperaDocumentCode;
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
        public async Task<string> GetReservationDocumentById(string reservationnnumber, string documnettype)
        {
            string Base64regacrd = "";

            var ResDocumentList = await new MastersLogics().GetReservationDocumenttype(documnettype, reservationnnumber);
            if (ResDocumentList != null && ResDocumentList.Count() > 0)
            {

                var reservationdoc = ResDocumentList.FirstOrDefault();
                Base64regacrd = Convert.ToBase64String(reservationdoc.Document);
                return Base64regacrd;


            }

            return "";
        }


        [HttpPost]
        public async Task<ActionResult> CompletedocUploadAsync(string ReservationNumber, string ReservationNameID)
        {
            #region Get Regcard
            string ActionName = "CompletedocUploadAsync", ActionGroup = "Pre-Checkin";
            string RegcardBase64 = null;
            // Match LocalAPI: NotificationDisabled=false ⇒ notifications enabled
            bool NotificationEnabled = !(bool.TryParse(System.Configuration.ConfigurationManager.AppSettings["NotificationDisabled"], out bool nresult) ? nresult : false);
            string NotificationChannels = System.Configuration.ConfigurationManager.AppSettings["NotificationChannels"] ?? "";
            string[] channels = NotificationChannels.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim()).ToArray();

            //bool PreCheckInWhatsappMsg = bool.TryParse(System.Configuration.ConfigurationManager.AppSettings["PreCheckInWhatsappMsg"], out bool result) ? result : false;

            SessionDt session = new SessionDt();
            session.ReservationNumber = ReservationNumber;
            session.ReservationNameID = ReservationNameID;
            OperaReservation operaReservation = new OperaReservation();
            CloudReservationModel reservation = new CloudReservationModel();
            bool alreadyPrecheckinCompleted = false;
            try
            {


                var reservationpms = await new CloudHelper().fetchReservationFromPMS(new Models.OWS.OwsRequestModel()
                {
                    ChainCode = ConfigurationManager.AppSettings["ChainCode"].ToString(),
                    DestinationEntityID = ConfigurationManager.AppSettings["DestinationEntityID"].ToString(),
                    DestinationSystemType = ConfigurationManager.AppSettings["DestinationSystemType"].ToString(),
                    HotelDomain = ConfigurationManager.AppSettings["HotelDomain"].ToString(),
                    KioskID = ConfigurationManager.AppSettings["KioskID"].ToString(),
                    LegNumber = "1",
                    Language = ConfigurationManager.AppSettings["Language"].ToString(),
                    Password = ConfigurationManager.AppSettings["Password"].ToString(),
                    Username = ConfigurationManager.AppSettings["Username"].ToString(),
                    SystemType = ConfigurationManager.AppSettings["SystemType"].ToString(),
                    FetchBookingRequest = new Models.OWS.FetchBookingRequestModel()
                    {
                        ReservationNumber = session.ReservationNumber

                    }
                }, ConfigurationManager.AppSettings["APIBaseUrl"].ToString(), "precheckin", "uplod");

                if (reservationpms != null && reservationpms.Count > 0)
                {

                    operaReservation = reservationpms.FirstOrDefault();
                }



                session.GuestSignedSignature = await 
                    GetReservationDocumentById(
                        session.ReservationNumber,
                        "Signature");
               // DataTable dtres = await new CloudHelper().FetchReservationDetailsByReferenceNumber(""session.ReservationNumber);
                var reservationsDt = await new CloudHelper().FetchReservationDetailsByReferenceNumber(session.ReservationNumber, new APIRequestModel { RequestObject = session.ReservationNumber }, ActionGroup, ConfigurationManager.AppSettings
                    ["APIBaseUrl"].ToString());
                if (reservationsDt?.responseData != null)
                {
                    reservation = JsonConvert.DeserializeObject<List<CloudReservationModel>>(reservationsDt.responseData.ToString()).FirstOrDefault();

                }
                alreadyPrecheckinCompleted = reservation != null && (reservation.IsPreCheckedInPMS ?? false);
                if (reservation != null && !string.IsNullOrEmpty(reservation.ReservationNumber))
                {
                    //var row = dtres.Rows[0];

                    operaReservation.VisitPurposeCode = string.IsNullOrEmpty(reservation.VisitPurposeCode) ? null : reservation.VisitPurposeCode;
                   

                    operaReservation.ExpectedArrivalTime = reservation.ETA;
                    if (!string.IsNullOrWhiteSpace(reservation.FlightNo))
                        operaReservation.FlightNo = reservation.FlightNo;
                }

                operaReservation.GuestSignature = session.GuestSignedSignature;
                new LogHelper().Log("VisitPurposeCode :- " + operaReservation.VisitPurposeCode+ " GuestSignature :- " + operaReservation.GuestSignature, session.ReservationNameID, ActionName, ActionGroup);
                Models.OWS.OwsResponseModel regcardResponse = await new CloudHelper().GetRegistrationCard(session.ReservationNameID, new Models.OWS.OwsRequestModel()
                {
                    ChainCode = ConfigurationManager.AppSettings["ChainCode"].ToString(),
                    DestinationEntityID = ConfigurationManager.AppSettings["DestinationEntityID"].ToString(),
                    DestinationSystemType = ConfigurationManager.AppSettings["DestinationSystemType"].ToString(),
                    HotelDomain = ConfigurationManager.AppSettings["HotelDomain"].ToString(),
                    KioskID = ConfigurationManager.AppSettings
                ["KioskID"].ToString(),
                    LegNumber = "1",
                    Language = ConfigurationManager.AppSettings
                ["Language"].ToString(),
                    Password = ConfigurationManager.AppSettings
                ["Password"].ToString(),
                    Username = ConfigurationManager.AppSettings
                ["Username"].ToString(),
                    SystemType = ConfigurationManager.AppSettings
                ["SystemType"].ToString(),
                    OperaReservation = operaReservation
                }, "Pre-Checkin", ConfigurationManager.AppSettings
                ["APIBaseUrl"].ToString());

                if (!regcardResponse.result || regcardResponse.responseData == null)
                {
                    new LogHelper().Log("Failed to generate regcard with reason :- " + regcardResponse.responseMessage, session.ReservationNameID, ActionName, ActionGroup);
                    new LogHelper().Warn("Failed to generate regcard with reason :- " + regcardResponse.responseMessage, session.ReservationNameID, ActionName, ActionGroup);
                }
                else
                {
                    new LogHelper().Log("Regcard generated successfully", session.ReservationNameID, ActionName, ActionGroup);
                    RegcardBase64 = regcardResponse.responseData.ToString();
                }
            }


            catch (Exception ex)
            {
                new LogHelper().Error(ex, session.ReservationNameID, ActionName, ActionGroup);
            }
            #endregion

            #region Pushing regcard to Local DB
            if (!string.IsNullOrEmpty(RegcardBase64))
            {
                try
                {
                    new LogHelper().Log("Pushing registration card to Local DB", session.ReservationNameID, ActionName, ActionGroup);
                    byte[] regCard = null;
                    regCard = Convert.FromBase64String(RegcardBase64);
                    var localResponse1 = await new CloudHelper().InsertReservationDocuments(session.ReservationNameID, new Models.APIRequestModel()
                    {
                        RequestObject = new List<Models.ReservationDocumentsDataTableModel>()
                                {
                                    new Models.ReservationDocumentsDataTableModel()
                                    {
                                        Document = regCard,
                                        DocumentType = "Registration Card",
                                        ReservationNameID =session.ReservationNameID
                                    }
                                }
                    }, "Pre-Checkin", ConfigurationManager.AppSettings
                     ["APIBaseUrl"].ToString());
                    if (!localResponse1.result)
                    {
                        new LogHelper().Log("Failed to push reservation document with reason :- " + localResponse1.responseMessage, session.ReservationNameID, ActionName, ActionGroup);
                        new LogHelper().Warn("Failed to push reservation document with reason :- " + localResponse1.responseMessage, session.ReservationNameID, ActionName, ActionGroup);
                    }
                    else
                    {
                        new LogHelper().Log("Reservation document updated successfully", ReservationNameID, ActionName, ActionGroup);
                        AuditProgressHelper.Log(
                            AuditProgressHelper.ModulePreCheckin,
                            AuditProgressHelper.Actions.RegistrationCardUpdated,
                            reservation?.ReservationDetailID > 0 ? reservation.ReservationDetailID : (object)session.ReservationNameID,
                            session.ReservationNameID,
                            guestName: operaReservation?.GuestProfiles != null && operaReservation.GuestProfiles.Count > 0
                                ? AuditProgressHelper.BuildGuestName(
                                    operaReservation.GuestProfiles[0].FirstName,
                                    operaReservation.GuestProfiles[0].MiddleName,
                                    operaReservation.GuestProfiles[0].LastName)
                                : null);
                    }
                }
                catch (Exception exc)
                {
                    new LogHelper().Error(exc, ReservationNameID, ActionName, ActionGroup);
                }
            }
            #endregion


            #region Updating record status in Local DB
            new LogHelper().Log("Updating the reservation status in Local DB", session.ReservationNameID, ActionName, ActionGroup);
            var localResponse = await new CloudHelper().UpdateReservationStatus(session.ReservationNameID, new Models.APIRequestModel()
            {
                RequestObject = new ReservationStatusRequestModel
                {
                    ReservationID = session.ReservationNameID,
                    Type = "uploadComplete"
                }
            }, "Pre-Checkin", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());

            if (!localResponse.result)
            {
                new LogHelper().Log("Updating the reservation status in Local DB with email send flag failed with reason :- " + localResponse.responseMessage, session.ReservationNameID, ActionName, ActionGroup);
            }
            else
                new LogHelper().Log("Updating the reservation status in Local DB ", session.ReservationNameID, ActionName, ActionGroup);

            // Thank You transition: mark IsPreCheckedInPMS (IsPrecheckinCompleted) + completed log (not on OK click).
            if (!alreadyPrecheckinCompleted)
            {
                Helpers.LogHelper.Instance.Log(
                    $"Pre check-in completed. ReservationNumber={session.ReservationNumber}",
                    session.ReservationNameID, ActionName, ActionGroup);
                AuditProgressHelper.Log(
                    AuditProgressHelper.ModulePreCheckin,
                    AuditProgressHelper.Actions.PrecheckinCompleted,
                    reservation?.ReservationDetailID > 0 ? reservation.ReservationDetailID : (object)session.ReservationNameID,
                    session.ReservationNameID,
                    guestName: operaReservation?.GuestProfiles != null && operaReservation.GuestProfiles.Count > 0
                        ? AuditProgressHelper.BuildGuestName(
                            operaReservation.GuestProfiles[0].FirstName,
                            operaReservation.GuestProfiles[0].MiddleName,
                            operaReservation.GuestProfiles[0].LastName)
                        : null);
                AuditProgressHelper.Log(
                    AuditProgressHelper.ModulePreCheckin,
                    AuditProgressHelper.Actions.MovedToNextPage,
                    reservation?.ReservationDetailID > 0 ? reservation.ReservationDetailID : (object)session.ReservationNameID,
                    session.ReservationNameID,
                    extraDetail: AuditProgressHelper.FormatPageMove("Document", "Thank You"),
                    guestName: operaReservation?.GuestProfiles != null && operaReservation.GuestProfiles.Count > 0
                        ? AuditProgressHelper.BuildGuestName(
                            operaReservation.GuestProfiles[0].FirstName,
                            operaReservation.GuestProfiles[0].MiddleName,
                            operaReservation.GuestProfiles[0].LastName)
                        : null);

                var completeStatusResponse = await new CloudHelper().UpdateReservationStatus(session.ReservationNameID, new Models.APIRequestModel()
                {
                    RequestObject = new ReservationStatusRequestModel
                    {
                        ReservationID = session.ReservationNameID,
                        Type = "PreCheckinComplete"
                    }
                }, "Pre-Checkin", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());

                if (!completeStatusResponse.result)
                {
                    new LogHelper().Log("Updating PreCheckinComplete status failed with reason :- " + completeStatusResponse.responseMessage, session.ReservationNameID, ActionName, ActionGroup);
                }
                else
                    new LogHelper().Log("PreCheckinComplete status updated in Local DB", session.ReservationNameID, ActionName, ActionGroup);

                #region Opera reservation TRACE (main page — not profile)
                try
                {
                    string precheckinTraceText = ConfigurationManager.AppSettings["PreCheckinCompletedTraceMessage"];
                    if (string.IsNullOrWhiteSpace(precheckinTraceText))
                        precheckinTraceText = "pre-check-in completed";

                    var traceResponse = await new CloudHelper().AddReservationCompletionTrace(
                        session.ReservationNameID,
                        session.ReservationNumber,
                        precheckinTraceText,
                        ActionGroup,
                        ConfigurationManager.AppSettings["APIBaseUrl"].ToString());

                    if (traceResponse != null && traceResponse.result)
                        new LogHelper().Log("Opera reservation trace posted: " + precheckinTraceText, session.ReservationNameID, ActionName, ActionGroup);
                    else
                        new LogHelper().Log(
                            "Failed to post Opera reservation trace with reason :- " + (traceResponse != null ? traceResponse.responseMessage : "null"),
                            session.ReservationNameID, ActionName, ActionGroup);
                }
                catch (Exception traceEx)
                {
                    new LogHelper().Error(traceEx, session.ReservationNameID, ActionName, ActionGroup);
                }
                #endregion
            }
            else
            {
                new LogHelper().Log("Pre check-in already completed — skipping flag/email/track re-apply", session.ReservationNameID, ActionName, ActionGroup);
            }
            #endregion


            bool? isEmailSent = null;
            bool? IsEmailProcessed = null;
            if (alreadyPrecheckinCompleted)
            {
                // Idempotent: do not re-send confirmation or re-push PrecheckinCompleted track
                return Json(new { result = true }, JsonRequestBehavior.AllowGet);
            }

            if (NotificationEnabled)
            {
                new LogHelper().Log("PreCheckIn channels enabled : " + NotificationChannels, session.ReservationNameID, ActionName, ActionGroup);

                if (channels.Any(c => c.Equals("Email", StringComparison.OrdinalIgnoreCase)))
                {
                    #region Sending Email
                    new LogHelper().Log("Sending confirmation email", session.ReservationNameID, ActionName, ActionGroup);
                    string toEmail = null;
                    if (operaReservation != null && operaReservation.GuestProfiles != null && operaReservation.GuestProfiles.Count > 0
                        && operaReservation.GuestProfiles[0].Email != null && operaReservation.GuestProfiles[0].Email.Count > 0)
                    {
                        var emails = operaReservation.GuestProfiles[0].Email;
                        var primary = emails.FirstOrDefault(e => e.primary != null && e.primary.Value && !string.IsNullOrWhiteSpace(e.email));
                        toEmail = primary != null
                            ? primary.email
                            : emails.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e.email))?.email;
                    }

                    if (!string.IsNullOrWhiteSpace(toEmail))
                    {
                        TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                        Models.Emails.EmailResponse emailResponse = await new CloudHelper().SendEmail(operaReservation.ReservationNameID, new Models.Emails.EmailRequest()
                        {
                            FromEmail = ConfigurationManager.AppSettings["PreArrivalConfirmationEmail"].ToString(),
                            ToEmail = toEmail,

                            GuestName = "" + (!string.IsNullOrEmpty(operaReservation.GuestProfiles[0].FirstName) ? textInfo.ToTitleCase(operaReservation.GuestProfiles[0].FirstName) + " " : "")
                                            + (!string.IsNullOrEmpty(operaReservation.GuestProfiles[0].MiddleName) ? textInfo.ToTitleCase(operaReservation.GuestProfiles[0].MiddleName) + " " : "")
                                            + (!string.IsNullOrEmpty(operaReservation.GuestProfiles[0].LastName) ? textInfo.ToTitleCase(operaReservation.GuestProfiles[0].LastName) : ""),

                            Subject = ConfigurationManager.AppSettings["PreArrivalConfirmationEmailSubject"].ToString(),
                            confirmationNumber = session.ReservationNumber,
                            displayFromEmail = ConfigurationManager.AppSettings["EmailDisplayName"].ToString(),
                            EmailType = Models.Emails.EmailType.CheckinConfirmation,
                            ArrivalDate = operaReservation.ArrivalDate.Value.ToString("dd-MMM-yyyy"),
                            DepartureDate = operaReservation.DepartureDate.Value.ToString("dd-MMM-yyy")

                        }, "Pre-Checkin", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());

                        if (!emailResponse.result)
                        {
                            isEmailSent = false;
                            new LogHelper().Log("Failed to send confirmation email with reason :- " + emailResponse.responseMessage, session.ReservationNameID, ActionName, ActionGroup);
                            new LogHelper().Warn("Failed to send confirmation email with reason :- " + emailResponse.responseMessage, session.ReservationNameID, ActionName, ActionGroup);
                        }
                        else
                        {
                            isEmailSent = true;
                            new LogHelper().Log("Email send successfully to primary email :- " + toEmail, session.ReservationNameID, ActionName, ActionGroup);
                        }
                    }
                    else
                    {
                        new LogHelper().Log("Failed to send confirmation email since email address not found from pre checked in list response", session.ReservationNameID, ActionName, ActionGroup);
                        new LogHelper().Warn("Failed to send confirmation email since email address not found from pre checked in list response", session.ReservationNameID, ActionName, ActionGroup);
                    }
                    #endregion
                }
                if (channels.Any(c => c.Equals("WhatsApp", StringComparison.OrdinalIgnoreCase)))
                {
                    #region Sending Whatsapp Message

                    try
                    {

                        new LogHelper().Log("Sending confirmation whatsapp SMS", session.ReservationNameID, ActionName, ActionGroup);
                        if (operaReservation != null && operaReservation.GuestProfiles != null && operaReservation.GuestProfiles[0].Phones != null && operaReservation.GuestProfiles[0].Phones.Count > 0)
                        {
                            foreach (Models.OWS.Phone phone in operaReservation.GuestProfiles[0].Phones)
                            {
                                if (phone.primary != null && phone.primary.Value && !string.IsNullOrEmpty(phone.PhoneNumber))
                                {
                                    TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;

                                    string fullName = string.Join(" ",
                                                                new[]
                                                                {
                                                                operaReservation.GuestProfiles[0].FirstName,
                                                                operaReservation.GuestProfiles[0].MiddleName,
                                                                operaReservation.GuestProfiles[0].LastName
                                                                }.Where(x => !string.IsNullOrWhiteSpace(x))
                                                            );
                                    Models.Whatsapp.WhatsAppResponse whatsappResponse = await new CloudHelper().SendWhatsappMsg(operaReservation.ReservationNameID, new Models.Whatsapp.WhatsAppTemplateRequest()
                                    {
                                        ReceiverPhone = phone.PhoneNumber,
                                        TemplateName = EmailType.CheckinConfirmation,
                                        LanguageCode = "en",
                                        BodyParameters = new List<string>
                                    {
                                        operaReservation.GuestProfiles[0].Title??"Mr/Ms",
                                        fullName,
                                        operaReservation.ReservationNumber
                                    }

                                    }, "Pre-Checkin", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());

                                    if (!whatsappResponse.result)
                                    {
                                        new LogHelper().Log("Failed to send confirmation whatsapp with reason :- " + whatsappResponse.responseMessage, session.ReservationNameID, ActionName, ActionGroup);
                                        new LogHelper().Warn("Failed to send confirmation whatsapp with reason :- " + whatsappResponse.responseMessage, session.ReservationNameID, ActionName, ActionGroup);
                                    }
                                    else
                                        new LogHelper().Log("Whatsapp message send successfully", session.ReservationNameID, ActionName, ActionGroup);
                                }

                            }
                        }
                        else
                        {
                            new LogHelper().Log("Failed to send pre-checkin since phone number not found from pre checked in list response", operaReservation.ReservationNameID, ActionName, "Due-In push");
                            new LogHelper().Warn("Failed to send pre-checkin since phone number not found from pre checked in list response", operaReservation.ReservationNameID, ActionName, "Due-In push");
                        }


                    }
                    catch (Exception ex)
                    {
                        new LogHelper().Error(ex, operaReservation.ReservationNameID, ActionName, "Due-In push");
                    }
                    #endregion
                }
            }
            else
            {
                new LogHelper().Log("Notification Disabled : True", session.ReservationNameID, ActionName, ActionGroup);

            }
            var localResponses = await new CloudHelper().PushReservationTrackLocally(session.ReservationNameID, new Models.APIRequestModel()
            {
                RequestObject = new Models.ReservationTrackStatus()
                {
                    ReservationNameID = session.ReservationNameID,
                    ProcessType = Models.ReservationProcessType.PrecheckinCompleted.ToString(),
                    ReservationNumber = session.ReservationNumber,
                    ProcessStatus = "Precheckin",
                    EmailSent = isEmailSent
                }
            }, "Pre-Checkin", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());

            if (localResponses.result)
            {
                new LogHelper().Log("Reservation track in local DB updated successfully ", session.ReservationNameID, ActionName, ActionGroup);
            }
            else
            {
                new LogHelper().Log("Failed to update reservation track in local DB with reason :- " + localResponses.responseMessage, session.ReservationNameID, ActionName, ActionGroup);
            }
            return Json(new { result = true }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult PaymentSkipped(int ReservationID)
        {

            Helpers.LogHelper.Instance.Log($"PaymentSkip", $"{ReservationID}", "PaymentSkipped", "Pre-Checkin");

            reservationLogics.UpdateReservationByStage("PaymentSkip", new UpdateReservationModel()
            {
                ReservationID = ReservationID
            });
            if (ViewBag.uploadcomplete)
            {
                reservationLogics.UpdateReservationByStage("Finish", new UpdateReservationModel()
                {
                    ReservationID = ReservationID
                });
            }

            return Json(new { result = true });
        }
        [HttpPost]
        public JsonResult ConvertPdfToImage(HttpPostedFileBase pdfFile, string ReservationID)
        {
            if (pdfFile != null && pdfFile.ContentLength > 0)
            {
                try
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        pdfFile.InputStream.CopyTo(memoryStream);
                        memoryStream.Position = 0;

                        using (var document = PdfiumViewer.PdfDocument.Load(memoryStream))
                        {
                            // Render the first page (page index 0)
                            using (var image = document.Render(0, 300, 300, true))
                            {
                                using (var imgStream = new MemoryStream())
                                {
                                    image.Save(imgStream, System.Drawing.Imaging.ImageFormat.Png);

                                    //string base64String = "data:image/png;base64," +
                                    //    Convert.ToBase64String(imgStream.ToArray());
                                    string base64String = Convert.ToBase64String(imgStream.ToArray());
                                    return Json(new
                                    {
                                        Success = true,
                                        Base64Image = base64String
                                    });
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    return Json(new { Success = false, Message = ex.Message });
                }
            }

            return Json(new { Success = false, Message = "No file uploaded!" });
        }
        [HttpPost]
        public ActionResult ScanResult()
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
                    string filePath = Path.Combine(binPath, "ScanResult.txt");

                    // Write JSON to file

                    System.IO.File.WriteAllText(filePath, body);

                    return Json(new { status = "received", data = body });
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetApiToken()
        {
            var token = AuthenticationHelper.GetAPIAccessToken();
            return Json(new { token = token }, JsonRequestBehavior.AllowGet);
        }
        public async Task<ActionResult> IndexPayments(string id)
        {
            //Response.Cache.SetCacheability(HttpCacheability.NoCache);
            //Response.Cache.SetExpires(DateTime.UtcNow.AddHours(-1));
            //Response.Cache.SetNoStore();



            #region initialization
            ViewBag.id = id;

            string ActionName = "IndexPayments";
            string ActionGroup = "Pre-Checkin";
            var currentCulture = Thread.CurrentThread.CurrentCulture;
            #endregion


            var test = Url.Encode(Helpers.EncryptionHelper.EncryptString("381199"));

            if (string.IsNullOrEmpty(id))
            {
                return ShowLinkExpiry(LinkExpiryHelper.MissingLink, "0", ActionName, ActionGroup);
            }

            string confirmationNo = Helpers.EncryptionHelper.DecryptString(id.ToString());

            if (string.IsNullOrEmpty(confirmationNo))
            {
                return ShowLinkExpiry(LinkExpiryHelper.InvalidLink, id, ActionName, ActionGroup);
            }

            ViewBag.ReservationFound = false;
            ViewBag.PaymentProcessed = false;
            ViewBag.uploadedcompleted = false;
            Models.ReservationModel reservationModel = new Models.ReservationModel();
            reservationModel.IsDepositAvailable = false;
            OperaReservation operaReservation = new OperaReservation();

            MastersLogics mastersLogics = new MastersLogics();

            var CountryList = await new CloudHelper().fetchcountryMaster(ConfigurationManager.AppSettings
                    ["APIBaseUrl"].ToString(), ActionGroup);

            var reservationsDt = await new CloudHelper().FetchReservationDetailsByReferenceNumber(confirmationNo, new APIRequestModel { RequestObject = confirmationNo }, ActionGroup, ConfigurationManager.AppSettings
                    ["APIBaseUrl"].ToString());
            var reservations = new CloudReservationModel();
            if (reservationsDt?.responseData != null)
            {
                reservations = JsonConvert.DeserializeObject<List<CloudReservationModel>>(reservationsDt.responseData.ToString()).FirstOrDefault();

            }
            if (reservations != null && reservations.ReservationDetailID > 0)
            {

                Helpers.LogHelper.Instance.Log($"Reservation details fetched from DB {JsonConvert.SerializeObject(reservations, Formatting.Indented)} ", $"{reservations.ReservationNameID}", ActionName, ActionGroup);

                var reservationfromopera = await new CloudHelper().fetchReservationFromPMS(new Models.OWS.OwsRequestModel()
                {
                    ChainCode = ConfigurationManager.AppSettings["ChainCode"].ToString(),
                    DestinationEntityID = ConfigurationManager.AppSettings["DestinationEntityID"].ToString(),
                    DestinationSystemType = ConfigurationManager.AppSettings["DestinationSystemType"].ToString(),
                    HotelDomain = ConfigurationManager.AppSettings["HotelDomain"].ToString(),
                    KioskID = ConfigurationManager.AppSettings["KioskID"].ToString(),
                    LegNumber = "1",
                    Language = ConfigurationManager.AppSettings["Language"].ToString(),
                    Password = ConfigurationManager.AppSettings["Password"].ToString(),
                    Username = ConfigurationManager.AppSettings["Username"].ToString(),
                    SystemType = ConfigurationManager.AppSettings["SystemType"].ToString(),
                    FetchBookingRequest = new Models.OWS.FetchBookingRequestModel()
                    {
                        ReservationNumber = confirmationNo

                    }
                }, ConfigurationManager.AppSettings["APIBaseUrl"].ToString(), "precheckin", ActionGroup);

                if (reservationfromopera != null && reservationfromopera.Count > 0)
                {

                    if (reservationfromopera.First().Adults != null && reservationfromopera.First().Adults.Value > 0)
                        operaReservation = reservationfromopera.FirstOrDefault();
                    Helpers.LogHelper.Instance.Log($"Reservation details fetched from opera {JsonConvert.SerializeObject(operaReservation, Formatting.Indented)} ", $"{reservations.ReservationNameID}", ActionName, ActionGroup);


                    #region setsession
                    SessionDt booking = new SessionDt
                    {
                        ReservationNameID = operaReservation.ReservationNumber,
                        ReservationNumber = operaReservation.ReservationNameID,
                        ReservationStatus = operaReservation.ReservationStatus,
                    };
                    //Session["ReservationNameID"] = operaReservation.ReservationNumber;
                    //Session["BookingSession"] = booking;
                    #endregion
                    bool isredirectfromPaymentPage = false;
                    bool IsPaymentSuccess = false;
                    string PaymentFailureMessage = "";

                    if (TempData["IsredirectedfromPaymentPage"] != null)
                    {
                        isredirectfromPaymentPage = Convert.ToBoolean(TempData["IsredirectedfromPaymentPage"].ToString());
                        IsPaymentSuccess = Convert.ToBoolean(TempData["IsPaymentSuccess"].ToString());
                        PaymentFailureMessage = TempData["PaymentFailureMessage"].ToString();
                    }

                    ViewBag.IsredirectedfromPaymentPage = isredirectfromPaymentPage;
                    ViewBag.IsPaymentSuccess = IsPaymentSuccess;
                    ViewBag.PaymentFailureMessage = PaymentFailureMessage;



                    if (reservations.IsPreCheckedInPMS.HasValue && !reservations.IsPreCheckedInPMS.Value || isredirectfromPaymentPage && IsPaymentSuccess)
                    {
                        ViewBag.ReservationFound = true;

                        #region Get the existing selected package and Upsells

                        var ReservationPackagesList = await reservationLogics.GetReservationPackages(reservations.ReservationDetailID);

                        if (ReservationPackagesList != null && ReservationPackagesList.Count > 0)
                        {
                            Helpers.LogHelper.Instance.Log($"Existing reservation packages found.", $"{reservations.ReservationNameID}", ActionName, ActionGroup);

                            var RoomUpsellPackages = ReservationPackagesList.Where(x => x.IsRoomUpsell).Select(x => x.PackageID).ToArray();
                            var SpecialsPackages = ReservationPackagesList.Where(x => !x.IsRoomUpsell).Select(x => x.PackageID).ToArray();

                            ViewBag.RoomUpsellPackages = string.Join(",", RoomUpsellPackages);
                            ViewBag.SpecialPackages = string.Join(",", SpecialsPackages);
                            ViewBag.ReservationPackagesList = ReservationPackagesList;
                        }
                        else
                        {
                            Helpers.LogHelper.Instance.Log($"Existing reservation packages not found.", $"{reservations.ReservationNameID}", ActionName, ActionGroup);
                            ViewBag.RoomUpsellPackages = string.Empty;
                            ViewBag.SpecialPackages = string.Empty;
                            ViewBag.ReservationPackagesList = new List<ReservationPackageModel>();
                        }
                        #endregion

                        #region VerifyVIPReservationOrNot
                        if (operaReservation.userDefinedFields != null && operaReservation.userDefinedFields.Count > 0)
                        {
                            if (operaReservation.userDefinedFields.Find(x => x.FieldName.Equals(ConfigurationManager.AppSettings
                    ["PreAuthUDF"].ToString())) != null &&
                                operaReservation.userDefinedFields.Find(x => x.FieldName.Equals(ConfigurationManager.AppSettings
                    ["PreAuthUDF"].ToString())).FieldValue.Equals("NO"))
                            {

                                reservations.IsDepositAvailable = true;
                            }
                            else if (operaReservation.userDefinedFields.Find(x => x.FieldName.Equals(ConfigurationManager.AppSettings
                    ["PreAuthAmntUDF"])) != null &&
                                operaReservation.userDefinedFields.Find(x => x.FieldName.Equals(ConfigurationManager.AppSettings
                    ["PreAuthAmntUDF"])).FieldValue.Equals("NO"))
                            {



                                reservations.IsDepositAvailable = true;
                            }
                        }
                        #endregion

                        #region PaymentDesabling
                        ViewBag.IsPaymentDisabled = Convert.ToBoolean(ConfigurationManager.AppSettings["IsPaymentDisabled"] ?? "false");
                        if (ViewBag.IsPaymentDisabled)
                        {
                            // Phase 1: skip payment — do not call Adyen; wizard must not block
                            reservations.IsDepositAvailable = true;
                            ViewBag.IsPaymentSuccess = true;
                        }
                        #endregion

                        #region Update ETA
                        if (Convert.ToBoolean(ConfigurationManager.AppSettings["IsETADefault"]))
                        {
                            new LogHelper().Log("Assigning NULL value to ETA as per the config", operaReservation.ReservationNameID, ActionName, ActionGroup);
                            // reservations.ETA = null;
                        }
                        #endregion


                        #region Processing MealPlan
                        if (ConfigurationManager.AppSettings["IsBreakFastValidationWithUDF"] != null && Convert.ToBoolean(ConfigurationManager.AppSettings["IsBreakFastValidationWithUDF"]))
                        {

                            if (operaReservation.userDefinedFields != null && operaReservation.userDefinedFields.Count > 0)
                            {
                                if (operaReservation.userDefinedFields.Find(x => x.FieldName.Equals(ConfigurationManager.AppSettings["MealPlanFieldName"])) != null)
                                {

                                    if (!operaReservation.userDefinedFields.Find(x => x.FieldName.Equals(ConfigurationManager.AppSettings["MealPlanFieldName"])).FieldValue.Equals("NP"))
                                    {
                                        string tempUDFValue = operaReservation.userDefinedFields.Find(x => x.FieldName.Equals(ConfigurationManager.AppSettings["MealPlanFieldName"])).FieldValue;
                                        if (!string.IsNullOrEmpty(tempUDFValue))
                                        {
                                            bool isPackageFound = false;
                                            if (ConfigurationManager.AppSettings["PackageCodes"].Split(';').ToList().Contains(tempUDFValue))
                                                isPackageFound = true;
                                            if (isPackageFound)
                                            {
                                                reservations.IsBreakFastAvailable = true;
                                                new LogHelper().Debug("Meal plan updated", operaReservation.ReservationNameID, ActionName, ActionGroup);
                                            }
                                        }
                                    }
                                    else
                                        new LogHelper().Log("Processing meal plan not updated (NP not present in UDF)", operaReservation.ReservationNameID, ActionName, ActionGroup); ;
                                }
                            }
                            else
                                new LogHelper().Log("No UDF fields for meal plan not found", operaReservation.ReservationNameID, ActionName, ActionGroup);
                        }
                        if (ConfigurationManager.AppSettings["IsBreakFastValidationWithPackage"] != null && Convert.ToBoolean(ConfigurationManager.AppSettings["IsBreakFastValidationWithPackage"]))
                        {
                            if (((operaReservation.PackageDetails != null && operaReservation.PackageDetails.Count > 0) || (operaReservation.PreferanceDetails != null && operaReservation.PreferanceDetails.Count > 0)) && (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["PackageCodes"]) && ConfigurationManager.AppSettings["PackageCodes"].Split(';').ToList() != null))
                            {
                                if (operaReservation.PackageDetails != null && operaReservation.PackageDetails.Count > 0)
                                {
                                    bool isPackageFound = false;
                                    foreach (Models.OWS.PackageDetails package in operaReservation.PackageDetails)
                                    {
                                        if (ConfigurationManager.AppSettings["PackageCodes"].Split(';').ToList().Contains(package.PackageCode))
                                        {
                                            isPackageFound = true;
                                            break;
                                        }
                                    }
                                    if (isPackageFound)
                                    {
                                        reservations.IsBreakFastAvailable = true;
                                        new LogHelper().Debug("Meal plan updated", operaReservation.ReservationNameID, ActionName, ActionGroup);
                                    }
                                }
                                if (operaReservation.PreferanceDetails != null && operaReservation.PreferanceDetails.Count > 0)
                                {
                                    if (operaReservation.IsBreakFastAvailable == null || !operaReservation.IsBreakFastAvailable.Value)
                                    {
                                        bool isPrefernceFound = false;
                                        foreach (Models.OWS.PreferanceDetails prefernce in operaReservation.PreferanceDetails)
                                        {
                                            if (ConfigurationManager.AppSettings["PackageCodes"].Split(';').ToList().Contains(prefernce.PreferanceCode))
                                            {
                                                isPrefernceFound = true;
                                                break;
                                            }
                                        }
                                        if (isPrefernceFound)
                                        {
                                            reservations.IsBreakFastAvailable = true;

                                        }
                                    }
                                }
                            }


                        }
                        #endregion

                        if ((reservations.IsEcomchekinPaymentStaus != null && reservations.IsEcomchekinPaymentStaus.Value) || isredirectfromPaymentPage && IsPaymentSuccess)
                        {
                            Helpers.LogHelper.Instance.Log($"Reservation #{reservations.ReservationNameID} payment already done.", $"{reservations.ReservationNameID}", ActionName, ActionGroup);
                            ViewBag.PaymentProcessed = true;
                            ViewBag.IsPaymentSuccess = true;
                        }

                        //push events to DB
                        reservationLogics.InsertEvent(reservations.ReservationDetailID, "Email Link Click");
                        Helpers.LogHelper.Instance.Log($"Getting prfile details", $"{reservations.ReservationNameID}", ActionName, ActionGroup);
                        var ProfileList = await reservationLogics.GetReservationProfileList(reservations.ReservationDetailID);
                        AuditProgressHelper.Log(
                            AuditProgressHelper.ModulePreCheckin,
                            AuditProgressHelper.Actions.LinkOpened,
                            reservations.ReservationDetailID,
                            reservations.ReservationNameID,
                            guestName: ProfileList != null && ProfileList.Count > 0
                                ? AuditProgressHelper.BuildGuestName(ProfileList[0].FirstName, ProfileList[0].MiddleName, ProfileList[0].LastName)
                                : null);

                        ViewBag.Profiles = ProfileList;
                        ViewBag.CountryList = BuildCountryList(CountryList, ProfileList[0].CountryMasterID);
                        ViewBag.NationalityList = BuildNationalityList(CountryList, ProfileList[0].Nationality);

                        ViewBag.VisitPurposeCode = !string.IsNullOrEmpty(ProfileList[0].VisitPurposeCode)
                            ? ProfileList[0].VisitPurposeCode
                            : reservations.VisitPurposeCode;

                        if (ProfileList[0].CountryMasterID != null)
                        {
                            var StateList = await mastersLogics.GetStateListByCountryID(ProfileList[0].CountryMasterID.Value)
                                ?? new List<Models.StateMaster>();
                            ViewBag.StateList = BuildStateSelectList(StateList, ProfileList[0].StateMasterID);
                        }
                        else
                        {
                            List<Models.StateMaster> tbstateMasters = new List<Models.StateMaster>();
                            tbstateMasters.Add(new Models.StateMaster()
                            {
                                Statename = "Please select country",
                                StateMasterID = -1

                            });
                            ViewBag.StateList = BuildStateSelectList(tbstateMasters, null);

                        }
                        List<Models.Profile> profiles = new List<Models.Profile>();
                        int i = 0;
                        foreach (var profile in ProfileList)
                        {
                            if (profile.DocumentImage1 != null)
                            {
                                if (profile.DocumentImage1.Length > 0)
                                {
                                    i++;
                                }
                                else if (!string.IsNullOrEmpty(profile.DocumentNumber) && profile.DocumentNumber.Length > 0)
                                {
                                    i++;
                                }
                            }
                            else if (!string.IsNullOrEmpty(profile.DocumentNumber) && profile.DocumentNumber.Length > 0)
                            {
                                i++;
                            }
                            if (i == 0)
                            {
                                Models.OWS.GuestProfile guestProfile = null;
                                if (operaReservation?.GuestProfiles != null &&
     operaReservation.GuestProfiles.Count > 0 &&
     operaReservation.GuestProfiles[0] != null)
                                {
                                    guestProfile = operaReservation.GuestProfiles[0];
                                }

                                var primaryemail = guestProfile?.Email?
                                                    .FirstOrDefault(x => x?.primary == true);

                                var primaryphone = guestProfile?.Phones?
                                                    .FirstOrDefault(x => x?.primary == true);
                                profiles.Add(new Models.Profile()
                                {
                                    AddressLine1 = profile.AddressLine1,
                                    AddressLine2 = profile.AddressLine2,
                                    City = profile.City,
                                    CountryID = profile.CountryMasterID,
                                    Email = profile.Email != null ? profile.Email : (string.IsNullOrWhiteSpace(primaryemail?.email) ? null : primaryemail.email),
                                    FirstName = profile.FirstName,
                                    LastName = profile.LastName,
                                    Phone = profile.Phone != null ? profile.Phone : (string.IsNullOrWhiteSpace(primaryphone?.PhoneNumber) ? null : primaryphone.PhoneNumber),
                                    PostalCode = profile.PostalCode != null ? profile.PostalCode : "",
                                    StateID = profile.StateMasterID,
                                    ProfileDetailID = profile.ProfileDetailID,
                                    ProfileID = Convert.ToInt32(profile.ProfileID ?? "0"),
                                    MiddleName = profile.MiddleName,
                                    Nationality = profile.Nationality,
                                    DocumentImage1 = profile.DocumentImage1 != null ? "0" : "1"
                                });
                            }
                            else
                            {
                                profiles.Add(new Models.Profile()
                                {
                                    AddressLine1 = profile.AddressLine1,
                                    AddressLine2 = profile.AddressLine2,
                                    City = profile.City,
                                    CountryID = profile.CountryMasterID,
                                    Email = profile.Email,
                                    FirstName = profile.FirstName,
                                    LastName = profile.LastName,
                                    Phone = profile.Phone,
                                    PostalCode = profile.PostalCode != null ? profile.PostalCode : "",
                                    StateID = profile.StateMasterID,
                                    ProfileDetailID = profile.ProfileDetailID,
                                    ProfileID = Convert.ToInt32(profile.ProfileID ?? "0"),
                                    MiddleName = profile.MiddleName,
                                    Nationality = profile.Nationality,
                                    DocumentImage1 = profile.DocumentImage1 != null ? "0" : "1"
                                });
                            }
                        }
                        if (profiles != null)
                        {
                            if (profiles.Count() > 0 && profiles.Count() == i)
                            {
                                //Helpers.LogHelper.Instance.Log($"Reservation already contains document details updating IsUploadComplete as True", $"{reservations.ReservationNameID}", ActionName, ActionGroup);

                                //reservationLogics.UpdateReservationByStage("Upload Completes", new UpdateReservationModel()
                                //{
                                //    ReservationID = reservations.ReservationDetailID
                                //});
                                //ViewBag.uploadcomplete = true;
                                //reservations.IsUploadComplete = true;
                            }
                        }

                        decimal TotalRoomRate = 0;
                        TotalRoomRate = reservations.TotalAmount.HasValue ? reservations.TotalAmount.Value : 0;


                        string ExpectedTimeofArrival = "";

                        if (reservations.ETA.HasValue && !(reservations.ETA.Value.ToString("HH:mm") == "00:00"))
                        {
                            Helpers.LogHelper.Instance.Log($"Estimate time of arrival already exist {reservations.ETA.Value.ToString("HH:mm")} ", $"{reservations.ReservationNameID}", ActionName, ActionGroup);
                            DateTime timeUtc = reservations.ETA.Value;
                            ExpectedTimeofArrival = Helpers.DateTimeHelper.ConvertFromUTC(timeUtc);
                        }

                        reservationModel = new Models.ReservationModel()
                        {
                            ReservationNameID = reservations.ReservationNameID,
                            ReservationID = reservations.ReservationDetailID,
                            AdultCount = reservations.Adultcount.HasValue ? reservations.Adultcount.Value : 0,
                            ArrivalDate = reservations.ArrivalDate.HasValue ? reservations.ArrivalDate.Value.ToString("dd MMM yyyy") : "",
                            AverageRoomRate = reservations.AverageRoomRate.HasValue ? reservations.AverageRoomRate.Value : 0,
                            ChildCount = reservations.Childcount.HasValue ? reservations.Childcount.Value : 0,
                            InfantCount = reservations.InfantCount.HasValue ? reservations.InfantCount.Value : 0,
                            DepartureDate = reservations.DepartureDate.HasValue ? reservations.DepartureDate.Value.ToString("dd MMM yyyy") : "",
                            ExpectedTimeofArrival = ExpectedTimeofArrival,
                            FlightNo = reservations.FlightNo,
                            IsMembershipRequested = reservations.IsMemberShipEnrolled.HasValue ? reservations.IsMemberShipEnrolled.Value : false,
                            IsTermsAndConditions = false,
                            MembershipNo = reservations.MembershipNo,
                            ReservationNumber = reservations.ReservationNumber,
                            Profiles = profiles,
                            Questions = null,
                            RoomType = reservations.RoomType,
                            RoomTypeDescription = reservations.RoomTypeDescription,
                            TotalRoomRate = TotalRoomRate,
                            IsDepositAvailable = reservations.IsDepositAvailable != null ? reservations.IsDepositAvailable.Value : false,
                            IsBreakFastAvailable = reservations.IsBreakFastAvailable != null ? reservations.IsBreakFastAvailable.Value : false,
                            IsUploadComplete = reservations.IsUploadComplete != null ? reservations.IsUploadComplete.Value : false,
                            VisitPurposeCode = !string.IsNullOrEmpty(ProfileList[0].VisitPurposeCode)
                                ? ProfileList[0].VisitPurposeCode
                                : reservations.VisitPurposeCode,
                        };

                        if (reservationModel.IsUploadComplete)
                        {
                            ViewBag.uploadcomplete = true;
                        }
                        QRCodeGenerator qrGenerator = new QRCodeGenerator();
                        QRCodeData qrCodeData = qrGenerator.CreateQrCode(confirmationNo, QRCodeGenerator.ECCLevel.Q);
                        QRCode qrCode = new QRCode(qrCodeData);
                        Bitmap qrCodeImage = qrCode.GetGraphic(20);

                        System.IO.MemoryStream ms = new MemoryStream();
                        qrCodeImage.Save(ms, ImageFormat.Jpeg);
                        byte[] byteImage = ms.ToArray();
                        var QRCodeBase64 = Convert.ToBase64String(byteImage);

                        ViewBag.QRcode = QRCodeBase64;

                        ViewBag.AdaptorAPIBaseURL = ConfigurationManager.AppSettings["AdaptorAPIBaseURL"].ToString();
                        ViewBag.CalQRcode = "";
                        Helpers.LogHelper.Instance.Log($"Reservation  {reservations.ReservationNameID} successfully returned", $"{confirmationNo}", ActionName, ActionGroup);

                        return View("Index", reservationModel);

                    }
                    else
                    {
                        // Reservation not found page — already completed pre-checkin
                        return ShowLinkExpiry(LinkExpiryHelper.AlreadyPreCheckedIn, confirmationNo, ActionName, ActionGroup);
                    }
                }
                else
                {
                    // Reservation not found page
                    return ShowLinkExpiry(LinkExpiryHelper.ReservationNotFound, confirmationNo, ActionName, ActionGroup);
                }
            }
            else
            {
                return ShowLinkExpiry(LinkExpiryHelper.ReservationNotFound, confirmationNo, ActionName, ActionGroup);
            }
        }

        //public string GetStatelist(int Id)
        //{
        //    var StateList = new MastersLogics().GetStateListByCountryID(Id);
        //    if (StateList != null && StateList.Count() > 0)
        //    {
        //        return StateList.FirstOrDefault().StateCode;
        //    }

        //    return "";
        //}
        //public string GetCountry(int Id)
        //{
        //    var countryList = new MastersLogics().GetCountryList();
        //    if (countryList != null && countryList.Count() > 0)
        //    {
        //        var countrydetails = countryList.Where(x => x.CountryMasterID == Id).ToList();
        //        if (countrydetails != null && countrydetails.Count() > 0)
        //        {
        //            return countrydetails.FirstOrDefault().Country_2Char_code;
        //        }
        //        return "";
        //    }

        //    return "";
        //}
        //public string GetCountryByCode(string Id)
        //{
        //    var countryList = new MastersLogics().GetCountryList();
        //    if (countryList != null && countryList.Count() > 0)
        //    {
        //        var countrydetails = countryList.Where(x => x.Country_Full_name == Id || x.Country_3Char_code == Id).ToList();
        //        if (countrydetails != null && countrydetails.Count() > 0)
        //        {
        //            return countrydetails.FirstOrDefault().Country_2Char_code;
        //        }
        //        return "";
        //    }

        //    return "";
        //}

        //public string GetDocumentByCode(string Id)
        //{

        //    var DocumentList = new MastersLogics().GetDocumentList();
        //    if (DocumentList != null && DocumentList.Count() > 0)
        //    {
        //        var documentdetails = DocumentList.Where(x => x.DocumentCode == Id).ToList();
        //        if (documentdetails != null && documentdetails.Count() > 0)
        //        {
        //            return documentdetails.FirstOrDefault().OperaDocumentCode;
        //        }
        //        return "";
        //    }

        //    return "";
        //}

        //public string GetStateById(int Id)
        //{
        //    var StateList = new MastersLogics().GetStateList();
        //    if (StateList != null && StateList.Count() > 0)
        //    {
        //        var Statedetails = StateList.Where(x => x.StateMasterID == Id).ToList();
        //        if (Statedetails != null && Statedetails.Count() > 0)
        //        {
        //            return Statedetails.FirstOrDefault().StateCode;
        //        }
        //        return "";
        //    }

        //    return "";
        //}
        private string getPrimaryEmail()
        {
            if (SessionData.OperaReservation != null &&
                SessionData.OperaReservation.GuestProfiles != null &&
                SessionData.OperaReservation.GuestProfiles.Count > 0 &&
                SessionData.OperaReservation.GuestProfiles.First().Email != null &&
                SessionData.OperaReservation.GuestProfiles.First().Email.Count > 0)
            {
                var ProfileEmail = SessionData.OperaReservation.GuestProfiles.First().Email.Where(x => x.primary != null && x.primary.Value).ToList();
                if (ProfileEmail != null)
                {
                    return ProfileEmail.First().email;
                }
                else
                {
                    return SessionData.OperaReservation.GuestProfiles.First().Email.First().email;
                }
            }
            else
                return "";
        }
        private string getPrimaryPhone()
        {
            if (SessionData.OperaReservation != null &&
               SessionData.OperaReservation.GuestProfiles != null &&
               SessionData.OperaReservation.GuestProfiles.Count > 0 &&
                SessionData.OperaReservation.GuestProfiles.First().Phones != null &&
                SessionData.OperaReservation.GuestProfiles.First().Phones.Count > 0)
            {
                var ProfileEmail = SessionData.OperaReservation.GuestProfiles.First().Phones.Where(x => x.primary != null && x.primary.Value).ToList();
                if (ProfileEmail != null)
                {
                    return ProfileEmail.First().PhoneNumber;
                }
                else
                {
                    return SessionData.OperaReservation.GuestProfiles.First().Phones.First().PhoneNumber;
                }
            }
            else
                return "";
        }

        //[HttpPost]
        //public async Task<ActionResult> CompletedocUploadAsync()
        //{
        //    #region Get Regcard
        //    string RegcardBase64 = null;
        //    try
        //    {

        //        if (SessionData.OperaReservation != null)
        //        {
        //            new LogHelper().Log("Generating registration card", "", "FetchPreCheckedInReservation", "pre checked-in fetch");
        //            if (string.IsNullOrEmpty(SessionData.OperaReservation.GuestSignature))
        //                new LogHelper().Log("Guest signature missing", "", "FetchPreCheckedInReservation", "pre checked-in fetch");

        //            Models.OWS.OwsResponseModel regcardResponse = await new CloudHelper().GetRegistrationCard(SessionData.OperaReservation.ReservationNameID, new Models.OWS.OwsRequestModel()
        //            {
        //                ChainCode = ConfigurationManager.AppSettings["ChainCode"].ToString(),
        //                DestinationEntityID = ConfigurationManager.AppSettings["DestinationEntityID"].ToString(),
        //                DestinationSystemType = ConfigurationManager.AppSettings["DestinationSystemType"].ToString(),
        //                HotelDomain = ConfigurationManager.AppSettings["HotelDomain"].ToString(),
        //                KioskID = ConfigurationManager.AppSettings
        //            ["KioskID"].ToString(),
        //                LegNumber = "1",
        //                Language = ConfigurationManager.AppSettings
        //            ["Language"].ToString(),
        //                Password = ConfigurationManager.AppSettings
        //            ["Password"].ToString(),
        //                Username = ConfigurationManager.AppSettings
        //            ["Username"].ToString(),
        //                SystemType = ConfigurationManager.AppSettings
        //            ["SystemType"].ToString(),
        //                OperaReservation = SessionData.OperaReservation
        //            }, "pre checked-in fetch", ConfigurationManager.AppSettings
        //            ["APIBaseUrl"].ToString());

        //            if (!regcardResponse.result || regcardResponse.responseData == null)
        //            {
        //                new LogHelper().Log("Failed to generate regcard with reason :- " + regcardResponse.responseMessage, "", "FetchPreCheckedInReservation", "pre checked-in fetch");
        //                new LogHelper().Warn("Failed to generate regcard with reason :- " + regcardResponse.responseMessage, "", "FetchPreCheckedInReservation", "pre checked-in fetch");
        //            }
        //            else
        //            {
        //                new LogHelper().Log("Regcard generated successfully", "", "FetchPreCheckedInReservation", "pre checked-in fetch");
        //                RegcardBase64 = regcardResponse.responseData.ToString();
        //            }
        //        }
        //        else
        //        {
        //            new LogHelper().Log("Failed to generating registration card, failed to get opera reservation", "", "FetchPreCheckedInReservation", "pre checked-in fetch");
        //            new LogHelper().Warn("Failed to generating registration card, failed to get opera reservation", "", "FetchPreCheckedInReservation", "pre checked-in fetch");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        new LogHelper().Error(ex, "", "FetchPreCheckedInReservation", "pre checked-in fetch");
        //    }
        //    #endregion

        //    #region Pushing regcard to Local DB
        //    if (!string.IsNullOrEmpty(RegcardBase64))
        //    {
        //        try
        //        {
        //            new LogHelper().Log("Pushing registration card to Local DB", "", "FetchPreCheckedInReservation", "pre checked-in fetch");
        //            byte[] regCard = null;
        //            regCard = Convert.FromBase64String(RegcardBase64);
        //            var localResponse1 = await new CloudHelper().InsertReservationDocuments("", new Models.APIRequestModel()
        //            {
        //                RequestObject = new List<Models.ReservationDocumentsDataTableModel>()
        //                        {
        //                            new Models.ReservationDocumentsDataTableModel()
        //                            {
        //                                Document = regCard,
        //                                DocumentType = "Registration Card",
        //                                ReservationNameID =SessionData.OperaReservation.ReservationNameID
        //                            }
        //                        }
        //            }, "pre checked-in fetch", ConfigurationManager.AppSettings
        //             ["APIBaseUrl"].ToString());
        //            if (!localResponse1.result)
        //            {
        //                new LogHelper().Log("Failed to push reservation document with reason :- " + localResponse1.responseMessage, "", "FetchPreCheckedInReservation", "pre checked-in fetch");
        //                new LogHelper().Warn("Failed to push reservation document with reason :- " + localResponse1.responseMessage, "", "FetchPreCheckedInReservation", "pre checked-in fetch");
        //            }
        //            else
        //            {
        //                new LogHelper().Log("Reservation document updated successfully", "", "FetchPreCheckedInReservation", "pre checked-in fetch");
        //            }
        //        }
        //        catch (Exception exc)
        //        {
        //            new LogHelper().Error(exc, "", "FetchPreCheckedInReservation", "pre checked-in fetch");
        //        }
        //    }
        //    #endregion


        //    #region Updating record status in Local DB
        //    new LogHelper().Log("Updating the reservation status in Local DB", SessionData.OperaReservation.ReservationNameID, "PushDueInReservation", "Due-In push");
        //    var localResponse = await new CloudHelper().UpdateReservationStatus(SessionData.OperaReservation.ReservationNameID, new Models.APIRequestModel()
        //    {
        //        RequestObject = new ReservationStatusRequestModel
        //        {
        //            ReservationID = SessionData.OperaReservation.ReservationNameID,
        //            Type = "uploadComplete"
        //        }
        //    }, "Due-In push", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());

        //    if (!localResponse.result)
        //    {
        //        new LogHelper().Log("Updating the reservation status in Local DB with email send flag failed with reason :- " + localResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, "PushDueInReservation", "Due-In push");
        //    }
        //    else
        //        new LogHelper().Log("Updating the reservation status in Local DB ", SessionData.OperaReservation.ReservationNameID, "PushDueInReservation", "Due-In push");
        //    #endregion
        //    // ViewBag.uploadcomplete = true;
        //    return Json(new { result = true });
        //}

        //public ActionResult PaymentSkipped(int ReservationID)
        //{

        //    Helpers.LogHelper.Instance.Log($"PaymentSkip", $"{ReservationID}", "PaymentSkipped", "Pre-Checkin");

        //    reservationLogics.UpdateReservationByStage("PaymentSkip", new UpdateReservationModel()
        //    {
        //        ReservationID = ReservationID
        //    });
        //    if (ViewBag.uploadcomplete)
        //    {
        //        reservationLogics.UpdateReservationByStage("Finish", new UpdateReservationModel()
        //        {
        //            ReservationID = ReservationID
        //        });
        //    }

        //    return Json(new { result = true });
        //}
        //[HttpPost]
        //public JsonResult ConvertPdfToImage(HttpPostedFileBase pdfFile, string ReservationID)
        //{
        //    if (pdfFile != null && pdfFile.ContentLength > 0)
        //    {
        //        try
        //        {
        //            using (var memoryStream = new MemoryStream())
        //            {
        //                pdfFile.InputStream.CopyTo(memoryStream);
        //                memoryStream.Position = 0;

        //                using (var document = PdfiumViewer.PdfDocument.Load(memoryStream))
        //                {
        //                    // Render the first page (page index 0)
        //                    using (var image = document.Render(0, 300, 300, true))
        //                    {
        //                        using (var imgStream = new MemoryStream())
        //                        {
        //                            image.Save(imgStream, System.Drawing.Imaging.ImageFormat.Png);

        //                            //string base64String = "data:image/png;base64," +
        //                            //    Convert.ToBase64String(imgStream.ToArray());
        //                            string base64String = Convert.ToBase64String(imgStream.ToArray());
        //                            return Json(new
        //                            {
        //                                Success = true,
        //                                Base64Image = base64String
        //                            });
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            return Json(new { Success = false, Message = ex.Message });
        //        }
        //    }

        //    return Json(new { Success = false, Message = "No file uploaded!" });
        //}
        //[HttpPost]
        //public ActionResult ScanResult()
        //{
        //    try
        //    {
        //        Request.InputStream.Position = 0;
        //        using (var reader = new StreamReader(Request.InputStream))
        //        {
        //            var body = reader.ReadToEnd();
        //            // Get the path to the bin folder
        //            string binPath = AppDomain.CurrentDomain.BaseDirectory;

        //            // Combine with file name
        //            string filePath = Path.Combine(binPath, "ScanResult.txt");

        //            // Write JSON to file

        //            System.IO.File.WriteAllText(filePath, body);

        //            return Json(new { status = "received", data = body });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { error = true, message = ex.Message });
        //    }
        //}

        //[HttpGet]
        //public JsonResult GetApiToken()
        //{
        //    var token = AuthenticationHelper.GetAPIAccessToken();
        //    return Json(new { token = token }, JsonRequestBehavior.AllowGet);
        //}
        [HttpGet]
        public async Task<ActionResult> SearchReservation()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> SearchReservation(string ConfirmationNo)
        {
            string ActionName = "SearchReservation", ActionGroup = "Guest Search";
            const int MaxSearchLength = 40;
            const string InvalidSearchMessage = "Please enter a valid confirmation, reservation, or OTA booking number.";
            const string EmptySearchMessage = "Please enter a confirmation, reservation, or OTA booking number.";

            ConfirmationNo = (ConfirmationNo ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(ConfirmationNo))
            {
                Helpers.LogHelper.Instance.Warn(
                    $"Search reservation failed. Reason={EmptySearchMessage}. Res#=",
                    "0", ActionName, ActionGroup);
                return Json(new { result = false, redirectUrl = string.Empty, errorMessage = EmptySearchMessage });
            }

            if (ConfirmationNo.Length > MaxSearchLength || !System.Text.RegularExpressions.Regex.IsMatch(ConfirmationNo, @"^[a-zA-Z0-9\-]+$"))
            {
                Helpers.LogHelper.Instance.Warn(
                    $"Search reservation failed. Reason={InvalidSearchMessage}. Res#={ConfirmationNo}",
                    ConfirmationNo, ActionName, ActionGroup);
                return Json(new { result = false, redirectUrl = string.Empty, errorMessage = InvalidSearchMessage });
            }

            try
            {
                string apiBaseUrl = ConfigurationManager.AppSettings["APIBaseUrl"].ToString();
                List<CloudReservationModel> cloudList = null;
                CloudReservationModel cloudRes = null;
                Models.OWS.OperaReservation operaRes = null;

                // 1) Cloud lookup (reservation # or confirmation/reference #)
                var cloudResponse = await new CloudHelper().FetchReservationDetailsByReferenceNumber(
                    ConfirmationNo,
                    new APIRequestModel { RequestObject = ConfirmationNo },
                    ActionGroup,
                    apiBaseUrl);

                if (cloudResponse?.responseData != null)
                {
                    cloudList = JsonConvert.DeserializeObject<List<CloudReservationModel>>(cloudResponse.responseData.ToString());
                    cloudRes = cloudList?.FirstOrDefault();
                }

                // 2) Opera lookup WITHOUT process filter — status decides precheckin vs precheckout
                var pmsList = await reservationLogics.FetchReservationDetailFromPMS(ConfirmationNo);
                if (pmsList != null && pmsList.Count > 0)
                {
                    operaRes = pmsList[0];
                }

                if (cloudRes == null && operaRes == null)
                {
                    Helpers.LogHelper.Instance.Warn(
                        $"Search reservation failed. Reason=Reservation Not Found!. Res#={ConfirmationNo}",
                        ConfirmationNo, ActionName, ActionGroup);
                    return Json(new { result = false, redirectUrl = string.Empty, errorMessage = "Reservation Not Found!" });
                }

                string reservationNumber = cloudRes?.ReservationNumber
                    ?? operaRes?.ReservationNumber
                    ?? ConfirmationNo;

                // Prefer Opera status for routing; if missing, fetch by resolved reservation number
                string status = NormalizeReservationStatus(operaRes);
                if (string.IsNullOrEmpty(status) && !string.IsNullOrEmpty(reservationNumber))
                {
                    var statusList = await reservationLogics.FetchReservationDetailFromPMS(reservationNumber);
                    if (statusList != null && statusList.Count > 0)
                    {
                        operaRes = statusList[0];
                        status = NormalizeReservationStatus(operaRes);
                        reservationNumber = operaRes.ReservationNumber ?? reservationNumber;
                    }
                }

                if (cloudRes != null && (cloudRes.IsPrecheckOutPMS ?? false))
                {
                    string alreadyCheckoutMsg = "Pre check-out has already been completed for this reservation.";
                    Helpers.LogHelper.Instance.Warn(
                        $"Search reservation failed. Reason={alreadyCheckoutMsg}. Res#={reservationNumber}",
                        reservationNumber, ActionName, ActionGroup);
                    return Json(new { result = false, redirectUrl = string.Empty, errorMessage = alreadyCheckoutMsg });
                }

                if (IsCheckedOutStatus(status))
                {
                    string checkedOutMsg = "This reservation has already been checked out.";
                    Helpers.LogHelper.Instance.Warn(
                        $"Search reservation failed. Reason={checkedOutMsg}. Status={status}. Res#={reservationNumber}",
                        reservationNumber, ActionName, ActionGroup);
                    return Json(new
                    {
                        result = false,
                        redirectUrl = string.Empty,
                        errorMessage = checkedOutMsg
                    });
                }

                // Secondary signal: tbProcessTracking (email/completion history) — does not override clear Opera status
                string trackingHint = await ResolveProcessTrackingFlowHintAsync(reservationNumber, ActionName, ActionGroup, apiBaseUrl);
                string flow = ResolveSearchFlow(status, trackingHint);

                string encryptedId = Url.Encode(Helpers.EncryptionHelper.EncryptString(reservationNumber));
                string hostedUrl = Request.Url.GetLeftPart(UriPartial.Authority) + Request.ApplicationPath.TrimEnd('/');

                // 3) Pre-checkout — Opera DUEOUT/INHOUSE wins; PrecheckinCompleted must NOT block checkout
                if (flow == "precheckout")
                {
                    string checkoutUrl = hostedUrl + "/Checkout/Index?id=" + encryptedId;
                    Helpers.LogHelper.Instance.Log(
                        $"Search routed to Pre Check-out. Status={status}, TrackHint={trackingHint ?? "none"}, Res#={reservationNumber}",
                        reservationNumber, ActionName, ActionGroup);
                    return Json(new { result = true, redirectUrl = checkoutUrl, flow = "precheckout" });
                }

                // 4) Pre-checkin candidates — push to cloud if missing, then route to Home/Index
                if (flow == "precheckin")
                {
                    if (cloudRes == null && operaRes != null)
                    {
                        await reservationLogics.PushDueInSearchedReservation(operaRes.ReservationNumber);

                        const int maxFetchAttempts = 5;
                        const int delayMs = 500;
                        for (int attempt = 1; attempt <= maxFetchAttempts; attempt++)
                        {
                            await Task.Delay(delayMs);
                            cloudResponse = await new CloudHelper().FetchReservationDetailsByReferenceNumber(
                                operaRes.ReservationNumber,
                                new APIRequestModel { RequestObject = operaRes.ReservationNumber },
                                ActionGroup,
                                apiBaseUrl);

                            if (cloudResponse?.responseData != null)
                            {
                                cloudList = JsonConvert.DeserializeObject<List<CloudReservationModel>>(cloudResponse.responseData.ToString());
                                cloudRes = cloudList?.FirstOrDefault();
                            }

                            if (cloudRes != null)
                            {
                                reservationNumber = cloudRes.ReservationNumber ?? operaRes.ReservationNumber;
                                encryptedId = Url.Encode(Helpers.EncryptionHelper.EncryptString(reservationNumber));
                                break;
                            }
                        }
                    }

                    if (cloudRes == null)
                    {
                        Helpers.LogHelper.Instance.Warn(
                            $"Search reservation failed. Reason=Reservation Not Found! (cloud push/fetch). Res#={reservationNumber}",
                            reservationNumber, ActionName, ActionGroup);
                        return Json(new { result = false, redirectUrl = string.Empty, errorMessage = "Reservation Not Found!" });
                    }

                    // Already completed precheckin and not due-out → not eligible for either flow.
                    // PrecheckinCompleted / tracking must NOT block when Opera status is due-out (handled in precheckout branch).
                    if ((cloudRes.IsPreCheckedInPMS ?? false) && !IsPreCheckoutStatus(status) && !string.IsNullOrEmpty(status))
                    {
                        string ineligibleMsg = "This reservation is not available for pre check-in or pre check-out. Please contact the Front Desk.";
                        Helpers.LogHelper.Instance.Warn(
                            $"Search reservation failed. Reason={ineligibleMsg} Status={status}. Res#={reservationNumber}",
                            reservationNumber, ActionName, ActionGroup);
                        return Json(new
                        {
                            result = false,
                            redirectUrl = string.Empty,
                            errorMessage = ineligibleMsg
                        });
                    }

                    string precheckinUrl = hostedUrl + "/Home/Index?id=" + encryptedId;
                    Helpers.LogHelper.Instance.Log(
                        $"Search routed to Pre Check-in. Status={status}, TrackHint={trackingHint ?? "none"}, Res#={reservationNumber}",
                        reservationNumber, ActionName, ActionGroup);
                    return Json(new { result = true, redirectUrl = precheckinUrl, flow = "precheckin" });
                }

                string invalidStatusMsg = "This reservation status (" + status + ") is not available for online pre check-in or pre check-out. Please contact the Front Desk.";
                Helpers.LogHelper.Instance.Warn(
                    $"Search reservation failed. Reason={invalidStatusMsg} TrackHint={trackingHint ?? "none"}. Res#={reservationNumber}",
                    reservationNumber, ActionName, ActionGroup);
                return Json(new
                {
                    result = false,
                    redirectUrl = string.Empty,
                    errorMessage = invalidStatusMsg
                });
            }
            catch (Exception ex)
            {
                Helpers.LogHelper.Instance.Error(ex, ConfirmationNo, ActionName, ActionGroup);
                return Json(new { result = false, redirectUrl = string.Empty, errorMessage = "An error occurred while searching for the reservation." });
            }
        }

        /// <summary>
        /// Primary: Opera status. Secondary: ProcessTracking hint when status empty/unknown,
        /// or as a soft guard when status already points at checkout/checkin.
        /// </summary>
        private static string ResolveSearchFlow(string status, string trackingHint)
        {
            // Opera / PMS status is source of truth when clear
            if (IsPreCheckoutStatus(status))
                return "precheckout";

            if (IsPreCheckinStatus(status))
                return "precheckin";

            // Status missing or unrecognized — use latest relevant ProcessTracking
            if (trackingHint == "precheckout")
                return "precheckout";
            if (trackingHint == "precheckin")
                return "precheckin";

            // Empty status with no tracking: keep prior behavior (attempt precheckin / cloud push path)
            if (string.IsNullOrEmpty(status))
                return "precheckin";

            return null;
        }

        /// <summary>
        /// Loads CompletedTabIndex from Local API and sets ViewBag resume flags so Index can
        /// open the next incomplete wizard step without flashing the START splash.
        /// </summary>
        private async Task ApplyPrecheckinResumeProgressAsync(string reservationNumber, string actionName, string actionGroup)
        {
            ViewBag.CompletedTabIndex = -1;
            ViewBag.ResumeTabIndex = -1;
            ViewBag.SkipPrecheckinSplash = false;

            if (string.IsNullOrWhiteSpace(reservationNumber))
                return;

            try
            {
                string apiBase = ConfigurationManager.AppSettings["APIBaseUrl"].ToString();
                using (var httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new Uri(apiBase);
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var accessToken = AuthenticationHelper.GetAPIAccessToken();
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    }

                    var metaRequest = new APIRequestModel
                    {
                        RequestObject = new ReservationMetaDataModel
                        {
                            ReservationNumber = reservationNumber
                        }
                    };
                    var content = new StringContent(JsonConvert.SerializeObject(metaRequest), Encoding.UTF8, "application/json");
                    var httpResponse = await httpClient.PostAsync("Local/FetchReservationMetaData", content);
                    if (httpResponse == null || !httpResponse.IsSuccessStatusCode)
                        return;

                    string responseStr = await httpResponse.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<APIResponseModel>(responseStr);
                    if (apiResponse == null || apiResponse.result != true || apiResponse.responseData == null)
                        return;

                    List<ReservationMetaDataModel> rows = null;
                    try
                    {
                        rows = JsonConvert.DeserializeObject<List<ReservationMetaDataModel>>(apiResponse.responseData.ToString());
                    }
                    catch
                    {
                        // responseData may already be a typed list/object
                        try
                        {
                            rows = JsonConvert.DeserializeObject<List<ReservationMetaDataModel>>(
                                JsonConvert.SerializeObject(apiResponse.responseData));
                        }
                        catch (Exception parseEx)
                        {
                            Helpers.LogHelper.Instance.Error(parseEx, reservationNumber, actionName, actionGroup);
                            return;
                        }
                    }

                    if (rows == null || rows.Count == 0)
                        return;

                    int completedIdx = -1;
                    int parsed;
                    if (!string.IsNullOrWhiteSpace(rows[0].CompletedTabIndex)
                        && int.TryParse(rows[0].CompletedTabIndex, out parsed))
                    {
                        completedIdx = parsed;
                    }

                    if (completedIdx < 0)
                        return;

                    // Next incomplete step (GuestDetails=0 … ThankYou=3), same as wizard.js resolveResumeStep
                    const int maxTabIndex = 3;
                    int resumeIdx = completedIdx >= maxTabIndex
                        ? maxTabIndex
                        : completedIdx + 1;

                    // Document pane omitted when upload already complete — jump to Thank You
                    // (unless ForceDocumentResume: per-guest pending uploads still need Document tab)
                    if (resumeIdx == 2 && ViewBag.uploadcomplete == true && ViewBag.ForceDocumentResume != true)
                        resumeIdx = 3;

                    ViewBag.CompletedTabIndex = completedIdx;
                    ViewBag.ResumeTabIndex = resumeIdx;
                    ViewBag.SkipPrecheckinSplash = true;

                    Helpers.LogHelper.Instance.Log(
                        $"Precheckin resume: CompletedTabIndex={completedIdx}, ResumeTabIndex={resumeIdx}",
                        reservationNumber, actionName, actionGroup);
                }
            }
            catch (Exception ex)
            {
                // Non-fatal — client-side resumePrecheckinProgress remains as fallback
                Helpers.LogHelper.Instance.Error(ex, reservationNumber, actionName, actionGroup);
            }
        }

        private async Task<string> ResolveProcessTrackingFlowHintAsync(string reservationNumber, string actionName, string actionGroup, string apiBaseUrl)
        {
            if (string.IsNullOrWhiteSpace(reservationNumber))
                return null;

            try
            {
                var trackResponse = await new CloudHelper().FetchReservationTrackLocally(reservationNumber, new APIRequestModel
                {
                    RequestObject = new ReservationTrackStatus
                    {
                        ReservationNumber = reservationNumber,
                        ReservationNameID = null,
                        ProcessType = null
                    }
                }, actionGroup, apiBaseUrl);

                if (trackResponse?.result != true || trackResponse.responseData == null)
                    return null;

                List<ReservationTrackStatus> tracks = null;
                try
                {
                    tracks = JsonConvert.DeserializeObject<List<ReservationTrackStatus>>(trackResponse.responseData.ToString());
                }
                catch (Exception ex)
                {
                    Helpers.LogHelper.Instance.Error(ex, reservationNumber, actionName, actionGroup);
                    return null;
                }

                string hint = ClassifyProcessTrackingHint(tracks);
                Helpers.LogHelper.Instance.Log(
                    $"ProcessTracking hint={hint ?? "none"} for Res#={reservationNumber} (rows={tracks?.Count ?? 0})",
                    reservationNumber, actionName, actionGroup);
                return hint;
            }
            catch (Exception ex)
            {
                Helpers.LogHelper.Instance.Error(ex, reservationNumber, actionName, actionGroup);
                return null;
            }
        }

        /// <summary>
        /// Latest relevant ProcessType wins. Checkout-related types beat earlier precheckin history
        /// (e.g. PrecheckinCompleted + later Precheckoutemail → precheckout).
        /// </summary>
        private static string ClassifyProcessTrackingHint(List<ReservationTrackStatus> tracks)
        {
            if (tracks == null || tracks.Count == 0)
                return null;

            string[] checkoutTypes =
            {
                ReservationProcessType.Precheckoutemail.ToString(),
                ReservationProcessType.PreCheckedOutFetched.ToString(),
                ReservationProcessType.CheckedoutSuccessfully.ToString(),
                ReservationProcessType.CheckoutFailled.ToString(),
                ReservationProcessType.GuestFolioEmail.ToString()
            };
            string[] checkinTypes =
            {
                ReservationProcessType.Precheckinemail.ToString(),
                ReservationProcessType.PrecheckinSMS.ToString(),
                ReservationProcessType.PrecheckinCompleted.ToString(),
                ReservationProcessType.PreCheckedInFetched.ToString()
            };

            var ordered = tracks
                .Where(t => t != null && !string.IsNullOrWhiteSpace(t.ProcessType))
                .OrderByDescending(t => t.ID ?? 0)
                .ToList();

            foreach (var track in ordered)
            {
                string pt = track.ProcessType.Trim();
                if (checkoutTypes.Any(c => string.Equals(c, pt, StringComparison.OrdinalIgnoreCase)))
                    return "precheckout";
                if (checkinTypes.Any(c => string.Equals(c, pt, StringComparison.OrdinalIgnoreCase)))
                    return "precheckin";
            }

            return null;
        }

        private static string NormalizeReservationStatus(Models.OWS.OperaReservation operaRes)
        {
            if (operaRes == null)
                return string.Empty;

            string status = !string.IsNullOrWhiteSpace(operaRes.ComputedReservationStatus)
                ? operaRes.ComputedReservationStatus
                : operaRes.ReservationStatus;

            return (status ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static bool IsPreCheckinStatus(string status)
        {
            return status == "DUEIN" || status == "RESERVED" || status == "PROSPECT";
        }

        private static bool IsPreCheckoutStatus(string status)
        {
            // DUEOUT = MCO day; INHOUSE = in-house guest may use pre check-out when property allows
            return status == "DUEOUT" || status == "INHOUSE" || status == "CHECKED IN" || status == "CHECKEDIN";
        }

        private static bool IsCheckedOutStatus(string status)
        {
            return status == "CHECKEDOUT" || status == "CHECKED OUT" || status == "NOSHOW" || status == "CANCELLED" || status == "CANCELED";
        }

        [HttpPost]
        public async Task<ActionResult> UpdatePreCheckinStatus(int ReservationID)
        {
            Helpers.LogHelper.Instance.Log(
                $"Pre check-in completed. ReservationID={ReservationID}",
                ReservationID.ToString(), "CompletePreCheckin", "Pre-Checkin");
            #region Pushing Reservation Track

            new LogHelper().Log("Pushing reservation track in local DB ", SessionData.OperaReservation.ReservationNameID, "FetchPreCheckedInReservation", "pre checked-in fetch");
            #region Updating record status in Local DB
            new LogHelper().Log("Updating the reservation status in Local DB", SessionData.OperaReservation.ReservationNameID, "PushDueInReservation", "Due-In push");
            var localResponse = await new CloudHelper().UpdateReservationStatus(SessionData.OperaReservation.ReservationNameID, new Models.APIRequestModel()
            {
                RequestObject = new ReservationStatusRequestModel
                {
                    ReservationID = SessionData.OperaReservation.ReservationNameID,
                    Type = "PreCheckinComplete"
                }
            }, "Due-In push", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());

            if (!localResponse.result)
            {
                new LogHelper().Log("Updating the reservation status in Local DB with email send flag failed with reason :- " + localResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, "PushDueInReservation", "Due-In push");
            }
            else
                new LogHelper().Log("Updating the reservation status in Local DB ", SessionData.OperaReservation.ReservationNameID, "PushDueInReservation", "Due-In push");
            #endregion

            localResponse = await new CloudHelper().PushReservationTrackLocally(SessionData.OperaReservation.ReservationNameID, new Models.APIRequestModel()
            {
                RequestObject = new Models.ReservationTrackStatus()
                {
                    ReservationNameID = SessionData.OperaReservation.ReservationNameID,
                    ProcessType = Models.ReservationProcessType.PreCheckedInFetched.ToString(),
                    ReservationNumber = SessionData.OperaReservation.ReservationNumber,
                    ProcessStatus = "Precheckin",
                    EmailSent = false
                }
            }, "pre checked-in fetch", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());

            if (localResponse.result)
            {
                new LogHelper().Log("Reservation track in local DB updated successfully ", SessionData.OperaReservation.ReservationNameID, "FetchPreCheckedInReservation", "pre checked-in fetch");
            }
            else
            {
                new LogHelper().Log("Failed to update reservation track in local DB with reason :- " + localResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, "FetchPreCheckedInReservation", "pre checked-in fetch");
                return Json(new { result = false, Message = localResponse.responseMessage });
            }
            #region Sending Email
            new LogHelper().Log("Sending confirmation email", SessionData.OperaReservation.ReservationNameID, "FetchPreCheckedInReservation", "pre checked-in fetch");
            if (!string.IsNullOrEmpty(SessionData.OperaReservation.GuestProfiles[0].Email[0].email))
            {
                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                Models.Emails.EmailResponse emailResponse = await new CloudHelper().SendEmail(SessionData.OperaReservation.ReservationNameID, new Models.Emails.EmailRequest()
                {
                    FromEmail = ConfigurationManager.AppSettings["PreArrivalConfirmationEmail"].ToString(),
                    ToEmail = SessionData.OperaReservation.GuestProfiles[0].Email[0].email,
                    GuestName = "" + (!string.IsNullOrEmpty(SessionData.OperaReservation.GuestProfiles[0].FirstName) ? textInfo.ToTitleCase(SessionData.OperaReservation.GuestProfiles[0].FirstName) + " " : "")
                                    + (!string.IsNullOrEmpty(SessionData.OperaReservation.GuestProfiles[0].MiddleName) ? textInfo.ToTitleCase(SessionData.OperaReservation.GuestProfiles[0].MiddleName) + " " : "")
                                    + (!string.IsNullOrEmpty(SessionData.OperaReservation.GuestProfiles[0].LastName) ? textInfo.ToTitleCase(SessionData.OperaReservation.GuestProfiles[0].LastName) : ""),

                    Subject = ConfigurationManager.AppSettings["PreArrivalConfirmationEmailSubject"].ToString(),
                    confirmationNumber = SessionData.OperaReservation.ReservationNumber,
                    displayFromEmail = ConfigurationManager.AppSettings["EmailDisplayName"].ToString(),
                    EmailType = Models.Emails.EmailType.CheckinConfirmation,
                    ArrivalDate = SessionData.OperaReservation.ArrivalDate.Value.ToString("dd-MMM-yyyy"),
                    DepartureDate = SessionData.OperaReservation.DepartureDate.Value.ToString("dd-MMM-yyy")

                }, "pre checked-in fetch", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());

                if (!emailResponse.result)
                {
                    new LogHelper().Log("Failed to send confirmation email with reason :- " + emailResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, "FetchPreCheckedInReservation", "pre checked-in fetch");
                    new LogHelper().Warn("Failed to send confirmation email with reason :- " + emailResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, "FetchPreCheckedInReservation", "pre checked-in fetch");
                }
                else
                    new LogHelper().Log("Email send successfully", SessionData.OperaReservation.ReservationNameID, "FetchPreCheckedInReservation", "pre checked-in fetch");
            }
            else
            {
                new LogHelper().Log("Failed to send confirmation email since email address not found from pre checked in list response", SessionData.OperaReservation.ReservationNameID, "FetchPreCheckedInReservation", "pre checked-in fetch");
                new LogHelper().Warn("Failed to send confirmation email since email address not found from pre checked in list response", SessionData.OperaReservation.ReservationNameID, "FetchPreCheckedInReservation", "pre checked-in fetch");
            }
            #endregion

            #endregion

            return Json(new { result = true, Message = "Success" });
        }

        /// <summary>
        /// Shows the Link Expiry page with a reason-specific guest message and server log entry.
        /// </summary>
        private ActionResult ShowLinkExpiry(string reason, string reference, string actionName, string actionGroup)
        {
            string message = LinkExpiryHelper.GetGuestMessage(reason);
            string refKey = string.IsNullOrEmpty(reference) ? "0" : reference;
            if (reason == LinkExpiryHelper.AlreadyPreCheckedIn)
            {
                Helpers.LogHelper.Instance.Log(
                    $"Pre check-in already completed — directing to landing/expiry. Res#={reference}. Message={message.Replace("\n", " ")}",
                    refKey, actionName ?? "Index", actionGroup ?? "Pre-Checkin");
            }
            else
            {
                Helpers.LogHelper.Instance.Warn(
                    $"Link expiry page shown. Reason={reason}. Message={message.Replace("\n", " ")}",
                    refKey, actionName ?? "Index", actionGroup ?? "Pre-Checkin");
            }
            ViewBag.ExpiryReason = reason;
            ViewBag.ExpiryMessage = message;
            return View("ReservationNotFound");
        }

        private async Task<string> ResolveBookingNationalityAsync(int reservationId)
        {
            var profileList = await reservationLogics.GetReservationProfileList(reservationId);
            if (profileList != null && profileList.Count > 0 && !string.IsNullOrWhiteSpace(profileList[0].Nationality))
            {
                return profileList[0].Nationality.Trim();
            }

            return null;
        }

        private static List<tbCountryMaster> SortCountriesByName(List<tbCountryMaster> countryList)
        {
            if (countryList == null)
            {
                return new List<tbCountryMaster>();
            }

            return countryList
                .OrderBy(c => c.Country_Full_name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<Models.StateMaster> SortStatesByName(List<Models.StateMaster> stateList)
        {
            if (stateList == null)
            {
                return new List<Models.StateMaster>();
            }

            return stateList
                .OrderBy(s => s.Statename ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private SelectList BuildCountryList(List<tbCountryMaster> countryList, object selectedCountryId)
        {
            return new SelectList(SortCountriesByName(countryList), "CountryMasterID", "Country_Full_name", selectedCountryId);
        }

        private SelectList BuildNationalityList(List<tbCountryMaster> countryList, string selectedNationality)
        {
            return new SelectList(SortCountriesByName(countryList), "Country_2Char_code", "Country_Full_name", selectedNationality);
        }

        private SelectList BuildStateSelectList(List<Models.StateMaster> stateList, object selectedStateId)
        {
            return new SelectList(SortStatesByName(stateList), "StateMasterID", "Statename", selectedStateId);
        }
    
    
    }

}