using CheckinPortal.BusinessLayer;
using CheckinPortal.Helpers;
using CheckinPortal.Models;
using CheckinPortal.Models.AdaptorAPIModels;
//using CheckinPortal.Models.OWS;

using CheckinPortal.Models.PaymentDetails;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Windows.Input;

namespace CheckinPortal.Controllers
{
    public class CheckoutController : Controller
    {
        ReservationLogics reservationLogics = new ReservationLogics();
        // GET: Checkout
        public async Task<ActionResult> Index(string id)
        {
            string ActionName = "Index", ActionGroup = "Pre-Checkout";
            ViewBag.id = id;
            string folioAsBase64 = "";
            var currentCulture = Thread.CurrentThread.CurrentUICulture;
            //Check cookie have language and set to selected language
            var languageSession = "en";
            List<Models.PaymentHeader> paymentHeaders = null;
            Models.OWS.FolioModel guestFolio = null;
            if (Request.Cookies.AllKeys.Contains("culture"))
            {
                languageSession = Request.Cookies["culture"].Value.ToString();
            }


            //if (Session["SelectedLanguage"] == null)
            {
                Session["SelectedLanguage"] = languageSession;
            }
            var test = Url.Encode(Helpers.EncryptionHelper.EncryptString("316914"));
            if (string.IsNullOrEmpty(id))
            {
                return ShowLinkExpiry(LinkExpiryHelper.MissingLink, "0", ActionName, ActionGroup);
            }

            string ConfirmationNo = Helpers.EncryptionHelper.DecryptString(id.ToString());


            if (string.IsNullOrEmpty(ConfirmationNo))
            {
                return ShowLinkExpiry(LinkExpiryHelper.InvalidLink, id, ActionName, ActionGroup);
            }

            Helpers.FileHelpers.DeleteTempFiles();
            ViewBag.ReservationFound = false;
            ViewBag.PaymentProcessed = false;
            ViewBag.IsPaymentSuccess = false;
            ViewBag.EcomStatus = false;

            Models.CheckoutReservationModel checkoutReservation = new Models.CheckoutReservationModel();

            var reservationsDt = await new CloudHelper().FetchReservationDetailsByReferenceNumber(ConfirmationNo, new APIRequestModel { RequestObject = ConfirmationNo }, "", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());
            var reservations = new CloudReservationModel();
            if (reservationsDt != null)
            {
                reservations = JsonConvert.DeserializeObject<List<CloudReservationModel>>(reservationsDt.responseData.ToString()).FirstOrDefault();
            }
            if (reservations != null)
            {

                var reservation = reservations;
                bool isPreCheckoutComplete = false;
                if (reservation != null)
                {
                    if (reservation.IsPrecheckOutPMS ?? false)
                    {
                        return ShowLinkExpiry(LinkExpiryHelper.AlreadyPreCheckedOut, ConfirmationNo, ActionName, ActionGroup);
                    }

                    var reservationfromopera = await new CloudHelper().fetchReservationFromPMS1(new OWSRequestModel()
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
                        FetchBookingRequest = new FetchBookingRequestModel()
                        {
                            ReservationNumber = ConfirmationNo

                        }
                    }, ConfigurationManager.AppSettings["APIBaseUrl"].ToString(), "checkout", ActionGroup);
                    if (reservationfromopera != null && reservationfromopera.Count > 0)
                    {

                        if (reservation != null && !(reservation.IsPrecheckOutPMS ?? false))
                        {
                            var operaFirst = reservationfromopera.First();
                            int? adultCount = LinkExpiryHelper.ResolveAdultCount(operaFirst.Adults, reservation.Adultcount);
                            if (LinkExpiryHelper.HasEligibleAdultCount(adultCount))
                            {
                                SessionData.OperaReservation = operaFirst;
                                isPreCheckoutComplete = reservation.IsPrecheckOutPMS ?? false;
                                ViewBag.EcomStatus = reservation.IsEcomchekOUtPaymentStaus ?? false;
                            }
                            else
                            {
                                Helpers.LogHelper.Instance.Warn(
                                    $"Blocked precheckout — adult count is zero or missing (sharer). Adults={adultCount}. Res#={ConfirmationNo}",
                                    reservation.ReservationNameID ?? ConfirmationNo, ActionName, ActionGroup);
                                return ShowLinkExpiry(LinkExpiryHelper.ZeroAdults, ConfirmationNo, ActionName, ActionGroup);
                            }
                        }
                    }
                    else
                    {
                        return ShowLinkExpiry(LinkExpiryHelper.ReservationNotFound, ConfirmationNo, ActionName, ActionGroup);
                    }
                }
                bool IsPaymentDisabled = false;
                IsPaymentDisabled = (ConfigurationManager.AppSettings["IsPaymentDisabled"] != null && !string.IsNullOrEmpty(ConfigurationManager.AppSettings["IsPaymentDisabled"].ToString()) && bool.TryParse(ConfigurationManager.AppSettings["IsPaymentDisabled"].ToString(), out IsPaymentDisabled)) ? IsPaymentDisabled : false;
                ViewBag.IsPaymentDisabled = IsPaymentDisabled;

                #region CheckFolio
                #region Check Payment in Saavy
                //Models.Local.LocalResponseModel localResponse = null;
                if (!IsPaymentDisabled)
                {
                    new LogHelper().Log("Fetching payment details for reservation No. : " + SessionData.OperaReservation.ReservationNumber + " in Saavy Pay", SessionData.OperaReservation.ReservationNameID, "PushDueOutReservation", "Due-Out push");
                    APIResponseModel localResponse = await new CloudHelper().FetchPaymentDetails(SessionData.OperaReservation.ReservationNameID, new Models.APIRequestModel()
                    {
                        RequestObject = new Models.DueOut.FetchPaymentRequest()
                        {
                            ReservationNameID = SessionData.OperaReservation.ReservationNameID,
                            isActive = true
                        }
                    }, "Due-Out push", ConfigurationManager.AppSettings
                        ["APIBaseUrl"].ToString());

                    if (!localResponse.result || localResponse.responseData == null)
                    {
                        new LogHelper().Log("Failed to fetch payment details with reason :- " + localResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                        new LogHelper().Warn("Failed to fetch payment details with reason :- " + localResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);

                    }
                    else

                    {
                        new LogHelper().Debug("Converting API json to object", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                        try
                        {
                            paymentHeaders = JsonConvert.DeserializeObject<List<Models.PaymentHeader>>(localResponse.responseData.ToString());
                            new LogHelper().Log("Payment details fetched successfully", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                        }
                        catch (Exception ex)
                        {
                            new LogHelper().Error(ex, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                            new LogHelper().Log("Failed to covert API response to object", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                            new LogHelper().Warn("Failed to fetch payment details with reason :- " + ex.Message, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                            new LogHelper().Debug("Failed to fetch payment details with reason :- " + ex.Message, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                        }
                    }
                }
                #endregion

                #region FetchFolioItemsByWindow
                new LogHelper().Log("Fetching reservation folio by window for reservation No. : " + SessionData.OperaReservation.ReservationNumber, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                Models.OWS.OwsResponseModel owsResponse1 = await new CloudHelper().GetFolioByWindow(SessionData.OperaReservation.ReservationNameID, new Models.OWS.OwsRequestModel()
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
                    FetchFolioRequest = new Models.OWS.FetchFolioRequest()
                    {
                        ReservationNameID = SessionData.OperaReservation.ReservationNameID,
                        ProfileID = (SessionData.OperaReservation.GuestProfiles != null && SessionData.OperaReservation.GuestProfiles.Count > 0) ? SessionData.OperaReservation.GuestProfiles[0].PmsProfileID : ""
                    }
                }, "Due-Out push", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());

                if (!owsResponse1.result)
                {
                    new LogHelper().Log("Failed to fetch folio by window with reason :- " + owsResponse1.responseMessage, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                    new LogHelper().Warn("Failed to fetch folio by window with reason :- " + owsResponse1.responseMessage, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                    return ShowLinkExpiry(LinkExpiryHelper.NotEligible, ConfirmationNo, ActionName, ActionGroup);
                }
                else
                {
                    new LogHelper().Debug("Converting API json to object", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                    try
                    {
                        guestFolio = DeserializeFolioModel(owsResponse1.responseData);
                        if (guestFolio != null)
                        {
                            int windowCount = guestFolio.FolioWindows != null ? guestFolio.FolioWindows.Count : 0;
                            new LogHelper().Log(
                                "Current guest balance of the reservation is : " + guestFolio.BalanceAmount
                                + " (primaryGuestWindows=" + windowCount + ")",
                                SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                            new LogHelper().Log("Reservation folio by window fetched successfully", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                        }
                        else
                        {
                            new LogHelper().Debug("No folio items found in the reservation", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                        }
                    }
                    catch (Exception ex)
                    {
                        new LogHelper().Error(ex, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                        new LogHelper().Log("Failed to covert API response to object", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                        new LogHelper().Warn("Failed to fetch folio by window with reason :- " + ex.Message, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                        new LogHelper().Debug("Failed to fetch folio by window with reason :- " + ex.Message, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                    }
                }
                #endregion
                #endregion
                bool isOPIEnabled = false;
                isOPIEnabled = (ConfigurationManager.AppSettings["OPIEnabled"] != null && !string.IsNullOrEmpty(ConfigurationManager.AppSettings["OPIEnabled"].ToString()) && bool.TryParse(ConfigurationManager.AppSettings["OPIEnabled"].ToString(), out isOPIEnabled)) ? isOPIEnabled : false;
                if (isOPIEnabled || IsPaymentDisabled)
                {
                    //if ((paymentHeaders == null || paymentHeaders.Count == 0) )
                    //{
                    //    new LogHelper().Log("Not able to process reservation No. : " + SessionData.OperaReservation.ReservationNumber + " because there is no payment details in the saavy pay as well as current balance is greater than 0", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                    //    return View("ReservationNotFound");
                    //}
                }

                #region FetchFolioAsBase64
                new LogHelper().Log("Fetching reservation folio as base64 for reservation No. : " + SessionData.OperaReservation.ReservationNumber, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                owsResponse1 = await new CloudHelper().GetFolio(SessionData.OperaReservation.ReservationNameID, new Models.OWS.OwsRequestModel()
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
                    FetchFolioRequest = new Models.OWS.FetchFolioRequest()
                    {
                        ReservationNameID = SessionData.OperaReservation.ReservationNameID,
                        OperaReservation = (Models.OWS.OperaReservation)SessionData.OperaReservation,
                        ProfileID = (SessionData.OperaReservation.GuestProfiles != null && SessionData.OperaReservation.GuestProfiles.Count > 0) ? SessionData.OperaReservation.GuestProfiles[0].PmsProfileID : "",
                        FolioList = guestFolio
                    }
                }, "Due-Out push", ConfigurationManager.AppSettings
                    ["APIBaseUrl"].ToString());

                if (!owsResponse1.result || owsResponse1.responseData == null)
                {
                    new LogHelper().Log("Failed to fetch folio with reason :- " + owsResponse1.responseMessage, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                    new LogHelper().Warn("Failed to fetch folio with reason :- " + owsResponse1.responseMessage, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                    return ShowLinkExpiry(LinkExpiryHelper.NotEligible, ConfirmationNo, ActionName, ActionGroup);
                }
                else
                {
                    folioAsBase64 = owsResponse1.responseData.ToString();
                    new LogHelper().Log("Fetched guest folio as base64 successfully", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                }
                #endregion

                decimal guestBalanceForUi;
                string folioBlockReason;
                bool guestFoliosClear = EvaluatePrimaryGuestFoliosForPreCheckout(
                    guestFolio,
                    SessionData.OperaReservation?.ReservationNameID,
                    ActionName,
                    ActionGroup,
                    out guestBalanceForUi,
                    out folioBlockReason);

                if (!guestFoliosClear)
                {
                    new LogHelper().Log(
                        "Precheckout blocked on Index — " + folioBlockReason,
                        SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                }

                #region check
                // UI / Agree uses guest-window balance only (company/TA outstanding does not set this)
                SessionData.OperaReservation.CurrentBalance = guestBalanceForUi;
                if (guestFolio != null)
                    guestFolio.BalanceAmount = guestBalanceForUi;
                SessionData.FolioModel = guestFolio;
                SessionData.SharerReservation = SessionData.OperaReservation.SharerReservations;
                #endregion
                reservation.FolioDocument = Convert.FromBase64String(folioAsBase64);
                if (reservation != null && reservation.FolioDocument != null && !isPreCheckoutComplete)
                {
                    ViewBag.ReservationFound = true;

                    //save the folio PDF locally and send link to front end

                    string path = Server.MapPath($"~/temp/{reservation.ReservationNumber}.pdf");

                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.Delete(path);
                    }

                    System.IO.File.WriteAllBytes(path, reservation.FolioDocument);

                    //checkoutReservation.ReservationID = SessionData.ReservationDetailID;
                    checkoutReservation.ReservationNumber = SessionData.OperaReservation.ReservationNumber;
                    checkoutReservation.ReservationNameID = SessionData.OperaReservation.ReservationNameID;

                    checkoutReservation.TotalAmount = SessionData.OperaReservation.TotalAmount != null ? SessionData.OperaReservation.TotalAmount.Value : 0;
                    checkoutReservation.BalanecAmount = guestBalanceForUi;
                    checkoutReservation.PaidAmount = SessionData.OperaReservation.DepositDetail != null ? SessionData.OperaReservation.DepositDetail.Count() > 0 ? SessionData.OperaReservation.DepositDetail[0].Amount : 0 : 0;
                    if (SessionData.OperaReservation.GuestProfiles != null && SessionData.OperaReservation.GuestProfiles.Count > 0)
                    {

                        if (SessionData.OperaReservation.GuestProfiles != null && SessionData.OperaReservation.GuestProfiles.Count > 0)
                        {
                            checkoutReservation.FullName = $"{SessionData.OperaReservation.GuestProfiles[0].LastName}";
                            checkoutReservation.EmailID = SessionData.OperaReservation.GuestProfiles[0].Email[0].email;
                        }
                        else
                        {
                            checkoutReservation.FullName = $"Guest";
                            checkoutReservation.EmailID = "";
                        }
                    }

                    var activeTransactions = paymentHeaders != null && paymentHeaders.Count() > 0 ? paymentHeaders.Where(x => x.TransactionType == "PreAuth") : new List<PaymentHeader>(); //List *//*only Active pre-auth

                    decimal preAuthAmount = 0;
                    foreach (var preauth in activeTransactions)
                    {
                        if (preauth.IsActive.Value)
                        {

                            preAuthAmount += Convert.ToDecimal(preauth.Amount, CultureInfo.InvariantCulture);
                        }
                    }

                    Helpers.LogHelper.Instance.Log($"Reservation {ConfirmationNo} opening precheckout link", "", ActionName, ActionGroup);
                    AuditProgressHelper.Log(
                        AuditProgressHelper.ModulePreCheckout,
                        AuditProgressHelper.Actions.LinkOpened,
                        reservation.ReservationDetailID,
                        SessionData.OperaReservation?.ReservationNameID);
                    ViewBag.PreauthAmount = preAuthAmount;
                    ViewBag.ActiveTransactions = activeTransactions != null && activeTransactions.Count() > 0 ? activeTransactions.ToList() : new List<PaymentHeader>();
                    return View(checkoutReservation);
                }
                else
                {
                    string expiryReason = isPreCheckoutComplete
                        ? LinkExpiryHelper.AlreadyPreCheckedOut
                        : LinkExpiryHelper.NotEligible;
                    return ShowLinkExpiry(expiryReason, ConfirmationNo, ActionName, ActionGroup);
                }
            }
            else
            {
                return ShowLinkExpiry(LinkExpiryHelper.ReservationNotFound, ConfirmationNo, ActionName, ActionGroup);
            }

        }

        public ActionResult ChangeLanguage(string currentLanguage, string confirmationToken)
        {
            //Set the UI culture baseon currentLanguage

            if (string.IsNullOrEmpty(currentLanguage))
                currentLanguage = "en";


            var cultureInfo = new CultureInfo(currentLanguage);
            Thread.CurrentThread.CurrentUICulture = cultureInfo;
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(cultureInfo.Name);

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
            var x = $"{Request.Url.Scheme}://{Request.Url.Authority}/Checkout/Index?ID={HttpUtility.UrlEncode(confirmationToken)}";
            Response.Redirect(@x);
            return null;
            //return RedirectToAction("Index", new { id = confirmationToken });
        }


        public async Task<ActionResult> IndexPayment(string id, string paymentResponse = "")
        {
            string ActionName = "IndexPayment", ActionGroup = "Pre-CheckOut";
            if (string.IsNullOrEmpty(id))
            {
                return ShowLinkExpiry(LinkExpiryHelper.MissingLink, "0", ActionName, ActionGroup);
            }
            List<Models.PaymentHeader> paymentHeaders = null;

            //  List<Models.PaymentHeader> paymentHeaders = null;
            string ConfirmationNo = id;

            if (string.IsNullOrEmpty(ConfirmationNo))
            {
                return ShowLinkExpiry(LinkExpiryHelper.InvalidLink, id, ActionName, ActionGroup);
            }
            Models.OWS.FolioModel guestFolio = null;
            Helpers.FileHelpers.DeleteTempFiles();
            ViewBag.ReservationFound = false;
            ViewBag.PaymentProcessed = false;
            bool IsPaymentSuccess = false;
            bool EcomStatus = false;
            Models.OWS.OperaReservation operaReservation = new Models.OWS.OperaReservation();
            Models.CheckoutReservationModel checkoutReservation = new Models.CheckoutReservationModel();
            var reservationsDt = await new CloudHelper().FetchReservationDetailsByReferenceNumber(ConfirmationNo, new APIRequestModel { RequestObject = ConfirmationNo }, "", ConfigurationManager.AppSettings
                    ["APIBaseUrl"].ToString());
            var reservation = new CloudReservationModel();
            if (reservationsDt != null)
            {
                reservation = JsonConvert.DeserializeObject<List<CloudReservationModel>>(reservationsDt.responseData.ToString()).FirstOrDefault();

            }
            if (reservation != null)
            {

                bool isredirectfromPaymentPage = true;

                EcomStatus = Convert.ToBoolean(reservation.IsEcomchekOUtPaymentStaus.ToString());

                ViewBag.IsredirectedfromPaymentPage = isredirectfromPaymentPage;
                ViewBag.IsPaymentSuccess = EcomStatus;
                ViewBag.PaymentFailureMessage = paymentResponse;
                ViewBag.EcomStatus = EcomStatus;


                ViewBag.ReturnFromPayment = true;


                var reservationfromopera = await new CloudHelper().fetchReservationFromPMS1(new OWSRequestModel()
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
                    FetchBookingRequest = new FetchBookingRequestModel()
                    {
                        ReservationNumber = ConfirmationNo

                    }
                }, ConfigurationManager.AppSettings
                 ["APIBaseUrl"].ToString(), "checkout", "IndexPayment");
                if (reservationfromopera != null && reservationfromopera.Count > 0)
                {
                    operaReservation = reservationfromopera.FirstOrDefault();
                    if (reservation != null)
                    {
                        ViewBag.PaymentProcessed = true;


                        #region FetchFolioItemsByWindow
                        new LogHelper().Log("Fetching reservation folio by window for reservation No. : " + SessionData.OperaReservation.ReservationNumber, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                        Models.OWS.OwsResponseModel owsResponse1 = await new CloudHelper().GetFolioByWindow(SessionData.OperaReservation.ReservationNameID, new Models.OWS.OwsRequestModel()
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
                            FetchFolioRequest = new Models.OWS.FetchFolioRequest()
                            {
                                ReservationNameID = SessionData.OperaReservation.ReservationNameID,
                                ProfileID = (SessionData.OperaReservation.GuestProfiles != null && SessionData.OperaReservation.GuestProfiles.Count > 0) ? SessionData.OperaReservation.GuestProfiles[0].PmsProfileID : ""
                            }
                        }, "Due-Out push", ConfigurationManager.AppSettings
                            ["APIBaseUrl"].ToString());

                        if (!owsResponse1.result)
                        {
                            new LogHelper().Log("Failed to fetch folio by window with reason :- " + owsResponse1.responseMessage, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                            new LogHelper().Warn("Failed to fetch folio by window with reason :- " + owsResponse1.responseMessage, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                            return ShowLinkExpiry(LinkExpiryHelper.NotEligible, ConfirmationNo, ActionName, ActionGroup);
                        }
                        else
                        {
                            new LogHelper().Debug("Converting API json to object", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                            try
                            {

                                guestFolio = DeserializeFolioModel(owsResponse1.responseData);
                                if (guestFolio != null)
                                {
                                    decimal guestBalanceForUi;
                                    string folioBlockReason;
                                    EvaluatePrimaryGuestFoliosForPreCheckout(
                                        guestFolio,
                                        SessionData.OperaReservation?.ReservationNameID,
                                        ActionName,
                                        ActionGroup,
                                        out guestBalanceForUi,
                                        out folioBlockReason);
                                    guestFolio.BalanceAmount = guestBalanceForUi;
                                    SessionData.FolioModel = guestFolio;
                                    SessionData.OperaReservation.CurrentBalance = guestBalanceForUi;
                                    operaReservation.CurrentBalance = guestBalanceForUi;
                                    int windowCount = guestFolio.FolioWindows != null ? guestFolio.FolioWindows.Count : 0;
                                    new LogHelper().Log(
                                        "Current guest balance of the reservation is : " + guestBalanceForUi
                                        + " (primaryGuestWindows=" + windowCount + ")",
                                        SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                                    new LogHelper().Log("Reservation folio by window fetched successfully", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                                }
                                else
                                {
                                    new LogHelper().Debug("No folio items found in the reservation", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                                }
                            }
                            catch (Exception ex)
                            {
                                new LogHelper().Error(ex, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                                new LogHelper().Log("Failed to covert API response to object", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                                new LogHelper().Warn("Failed to fetch folio by window with reason :- " + ex.Message, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                                new LogHelper().Debug("Failed to fetch folio by window with reason :- " + ex.Message, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                            }
                        }
                        #endregion

                        ViewBag.ReservationFound = true;

                        //save the folio PDF locally and send link to front end

                        string path = Server.MapPath($"~/temp/{reservation.ReservationNumber}.pdf");

                        if (System.IO.File.Exists(path))
                        {
                            System.IO.File.Delete(path);
                        }

                        System.IO.File.WriteAllBytes(path, reservation.FolioDocument);

                        checkoutReservation.ReservationID = reservation.ReservationDetailID;
                        checkoutReservation.ReservationNumber = reservation.ReservationNumber;
                        checkoutReservation.ReservationNameID = reservation.ReservationNameID;

                        checkoutReservation.TotalAmount = operaReservation.TotalAmount != null ? operaReservation.TotalAmount.Value : 0;
                        // Guest-window balance only (negative and outstanding both surface; company/TA excluded)
                        checkoutReservation.BalanecAmount = operaReservation.CurrentBalance;
                        checkoutReservation.PaidAmount = operaReservation.DepositDetail != null ? SessionData.OperaReservation.DepositDetail.Count() > 0 ? SessionData.OperaReservation.DepositDetail[0].Amount : 0 : 0;

                        var profile = await reservationLogics.GetReservationProfileList(reservation.ReservationDetailID);

                        if (profile != null && profile.Count > 0)
                        {
                            checkoutReservation.FullName = $"{profile[0].LastName}";
                            checkoutReservation.EmailID = profile[0].Email;
                        }
                        else
                        {
                            checkoutReservation.FullName = $"Guest";
                            checkoutReservation.EmailID = "";
                        }



                        #region Check Payment in Saavy
                        //Models.Local.LocalResponseModel localResponse = null;
                        bool IsPaymentDisabled = false;
                        IsPaymentDisabled = (ConfigurationManager.AppSettings["IsPaymentDisabled"] != null && !string.IsNullOrEmpty(ConfigurationManager.AppSettings["IsPaymentDisabled"].ToString()) && bool.TryParse(ConfigurationManager.AppSettings["IsPaymentDisabled"].ToString(), out IsPaymentDisabled)) ? IsPaymentDisabled : false;
                        ViewBag.IsPaymentDisabled = IsPaymentDisabled;

                        if (!IsPaymentDisabled)
                        {
                            new LogHelper().Log("Fetching payment details for reservation No. : " + SessionData.OperaReservation.ReservationNumber + " in Saavy Pay", SessionData.OperaReservation.ReservationNameID, "PushDueOutReservation", "Due-Out push");
                            APIResponseModel localResponse = await new CloudHelper().FetchPaymentDetails(SessionData.OperaReservation.ReservationNameID, new Models.APIRequestModel()
                            {
                                RequestObject = new Models.DueOut.FetchPaymentRequest()
                                {
                                    ReservationNameID = SessionData.OperaReservation.ReservationNameID,
                                    isActive = true
                                }
                            }, "Due-Out push", ConfigurationManager.AppSettings
                                ["APIBaseUrl"].ToString());

                            if (!localResponse.result || localResponse.responseData == null)
                            {
                                new LogHelper().Log("Failed to fetch payment details with reason :- " + localResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                                new LogHelper().Warn("Failed to fetch payment details with reason :- " + localResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);

                            }
                            else

                            {
                                new LogHelper().Debug("Converting API json to object", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                                try
                                {
                                    paymentHeaders = JsonConvert.DeserializeObject<List<Models.PaymentHeader>>(localResponse.responseData.ToString());
                                    new LogHelper().Log("Payment details fetched successfully", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                                }
                                catch (Exception ex)
                                {
                                    new LogHelper().Error(ex, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                                    new LogHelper().Log("Failed to covert API response to object", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                                    new LogHelper().Warn("Failed to fetch payment details with reason :- " + ex.Message, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                                    new LogHelper().Debug("Failed to fetch payment details with reason :- " + ex.Message, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                                }
                            }
                        }

                        #endregion
                        var activeTransactions = paymentHeaders != null && paymentHeaders.Count() > 0 ? paymentHeaders.Where(x => x.TransactionType == "PreAuth") : new List<PaymentHeader>(); //List *//*only Active pre-auth


                        decimal preAuthAmount = 0;
                        foreach (var preauth in activeTransactions)
                        {
                            if (preauth.IsActive.Value)
                            {
                                preAuthAmount += Convert.ToDecimal(preauth.Amount, CultureInfo.InvariantCulture);
                            }
                        }




                        ViewBag.PreauthAmount = preAuthAmount;
                        ViewBag.ActiveTransactions = activeTransactions;

                        return View("Index", checkoutReservation);
                    }
                    else
                    {
                        Helpers.LogHelper.Instance.Log($"Reservation {ConfirmationNo} not valid for checkout or foilo not found", "", "IndexPayment", "Pre-Checkout");
                        return ShowLinkExpiry(LinkExpiryHelper.NotEligible, ConfirmationNo, "IndexPayment", "Pre-Checkout");
                    }
                }
                else
                {
                    return ShowLinkExpiry(LinkExpiryHelper.ReservationNotFound, ConfirmationNo, "IndexPayment", "Pre-Checkout");
                }



            }
            else
            {
                Helpers.LogHelper.Instance.Log($"Unable to find the reservation no {ConfirmationNo}", "", "IndexPayment", "Pre-Checkout");
                return ShowLinkExpiry(LinkExpiryHelper.ReservationNotFound, ConfirmationNo, "IndexPayment", "Pre-Checkout");
            }

        }
        public async Task<ActionResult> IndexPaymentold(string id, string paymentResponse = "")
        {

            if (string.IsNullOrEmpty(id))
            {
                return ShowLinkExpiry(LinkExpiryHelper.MissingLink, "0", "IndexPaymentold", "Pre-Checkout");
            }

            string ConfirmationNo = id;

            if (string.IsNullOrEmpty(ConfirmationNo))
            {
                return ShowLinkExpiry(LinkExpiryHelper.InvalidLink, id, "IndexPaymentold", "Pre-Checkout");
            }

            Helpers.FileHelpers.DeleteTempFiles();
            ViewBag.ReservationFound = false;
            ViewBag.PaymentProcessed = false;
            bool IsPaymentSuccess = false;
            bool EcomStatus = false;

            Models.CheckoutReservationModel checkoutReservation = new Models.CheckoutReservationModel();

            // var reservationsDt = reservationLogics.GetReservationDetailsDT(ConfirmationNo);
            var reservationsDt = await new CloudHelper().FetchReservationDetailsByReferenceNumber(ConfirmationNo, new APIRequestModel { RequestObject = ConfirmationNo }, "", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());
            var reservations = new List<CloudReservationModel>();
            if (reservationsDt != null)
            {
                reservations = JsonConvert.DeserializeObject<List<CloudReservationModel>>(reservationsDt.responseData.ToString());

            }
         

                if (reservations != null)
                {

                    bool isredirectfromPaymentPage = true;


                    EcomStatus = Convert.ToBoolean(reservations.FirstOrDefault().EcomPaymentStatus);

                    ViewBag.IsredirectedfromPaymentPage = isredirectfromPaymentPage;
                    ViewBag.IsPaymentSuccess = EcomStatus;
                    ViewBag.PaymentFailureMessage = paymentResponse;
                    ViewBag.EcomStatus = EcomStatus;


                    ViewBag.ReturnFromPayment = true;



                    //var reservationList = Helpers.DataTableHelper.DataTableToList<Models.GetReservationDetailsModel>(reservationsDt);

                    var reservation = reservations.FirstOrDefault();

                    if (reservation != null)
                    {
                        ViewBag.PaymentProcessed = true;




                        ViewBag.ReservationFound = true;

                        //save the folio PDF locally and send link to front end

                        string path = Server.MapPath($"~/temp/{reservation.ReservationNumber}.pdf");

                        if (System.IO.File.Exists(path))
                        {
                            System.IO.File.Delete(path);
                        }

                        System.IO.File.WriteAllBytes(path, reservation.FolioDocument);

                        checkoutReservation.ReservationID = reservation.ReservationDetailID;
                        checkoutReservation.ReservationNumber = reservation.ReservationNumber;
                        checkoutReservation.ReservationNameID = reservation.ReservationNameID;

                        checkoutReservation.TotalAmount = reservation.TotalAmount != null ? reservation.TotalAmount.Value : 0;
                        checkoutReservation.BalanecAmount = reservation.BalanceAmount != null ? reservation.BalanceAmount.Value : 0;
                        checkoutReservation.PaidAmount = reservation.PaidAmount != null ? reservation.PaidAmount.Value : 0;

                        var profile = await reservationLogics.GetReservationProfileList(reservation.ReservationDetailID);

                        if (profile != null && profile.Count > 0)
                        {
                            checkoutReservation.FullName = $"{profile[0].LastName}";
                            checkoutReservation.EmailID = profile[0].Email;
                        }
                        else
                        {
                            checkoutReservation.FullName = $"Guest";
                            checkoutReservation.EmailID = "";
                        }




                        var activeTransactions = (await reservationLogics
                                                    .GetActivePaymentTransctions(reservation.ReservationNumber))
                                                    .Where(x => x.IsActive)
                                                    .ToList(); //List only Active pre-auth

                    decimal preAuthAmount = 0;
                        foreach (var preauth in activeTransactions)
                        {
                            if (preauth.IsActive)
                            {
                                preAuthAmount += Convert.ToDecimal(preauth.Amount, CultureInfo.InvariantCulture);
                            }
                        }




                        ViewBag.PreauthAmount = preAuthAmount;
                        ViewBag.ActiveTransactions = activeTransactions;

                        bool IsPaymentDisabledOld = false;
                        IsPaymentDisabledOld = (ConfigurationManager.AppSettings["IsPaymentDisabled"] != null && !string.IsNullOrEmpty(ConfigurationManager.AppSettings["IsPaymentDisabled"].ToString()) && bool.TryParse(ConfigurationManager.AppSettings["IsPaymentDisabled"].ToString(), out IsPaymentDisabledOld)) ? IsPaymentDisabledOld : false;
                        ViewBag.IsPaymentDisabled = IsPaymentDisabledOld;

                        return View("Index", checkoutReservation);
                    }
                    else
                    {
                        Helpers.LogHelper.Instance.Log($"Reservation {ConfirmationNo} not valid for checkout or foilo not found", "", "IndexPayment", "Pre-Checkout");
                        return ShowLinkExpiry(LinkExpiryHelper.NotEligible, ConfirmationNo, "IndexPaymentold", "Pre-Checkout");
                    }
                }
                else
                {
                    Helpers.LogHelper.Instance.Log($"Unable to find the reservation no {ConfirmationNo}", "", "IndexPayment", "Pre-Checkout");
                    return ShowLinkExpiry(LinkExpiryHelper.ReservationNotFound, ConfirmationNo, "IndexPaymentold", "Pre-Checkout");
                }

            }


     
        public async Task<ActionResult> CompletePreCheckout(int ReservationID)
        {
            #region variable
            var ChainCode = ConfigurationManager.AppSettings["ChainCode"].ToString();
            var DestinationEntityID = ConfigurationManager.AppSettings["DestinationEntityID"].ToString();
            var DestinationSystemType = ConfigurationManager.AppSettings["DestinationSystemType"].ToString();
            var HotelDomain = ConfigurationManager.AppSettings["HotelDomain"].ToString();
            var KioskID = ConfigurationManager.AppSettings["KioskID"].ToString();
            var LegNumber = "1";
            var Language = ConfigurationManager.AppSettings["Language"].ToString();
            var Password = ConfigurationManager.AppSettings["Password"].ToString();
            var Username = ConfigurationManager.AppSettings["Username"].ToString();
            var SystemType = ConfigurationManager.AppSettings["SystemType"].ToString();
            #endregion

            string ActionName = "CompletePreCheckout", ActionGroup = "Pre-Checkout";

            // Block only when any primary-guest folio window is non-zero (outstanding or negative).
            // Company/TA outstanding must NOT block — FO settles those manually.
            decimal guestBalanceForUi;
            string folioBlockReason;
            bool guestFoliosClear = EvaluatePrimaryGuestFoliosForPreCheckout(
                SessionData.FolioModel,
                SessionData.OperaReservation?.ReservationNameID,
                ActionName,
                ActionGroup,
                out guestBalanceForUi,
                out folioBlockReason);

            if (!guestFoliosClear)
            {
                new LogHelper().Log(
                    "Blocked CompletePreCheckout — " + folioBlockReason,
                    SessionData.OperaReservation?.ReservationNameID, ActionName, ActionGroup);
                return Json(new
                {
                    result = false,
                    contactFrontDesk = true,
                    message = "Please contact front desk for assistance. Your folio balance is not zero."
                });
            }

            // Idempotent: skip Opera trace if pre-checkout already completed (link OK / double-submit)
            bool alreadyPrecheckoutCompleted = false;
            string precheckoutTraceSessionKey = "PreCheckoutTracePosted_" + (SessionData.OperaReservation?.ReservationNameID ?? "");
            try
            {
                if (!string.IsNullOrEmpty(precheckoutTraceSessionKey) && Session[precheckoutTraceSessionKey] as bool? == true)
                {
                    alreadyPrecheckoutCompleted = true;
                }
                else
                {
                    var existingResDt = await new CloudHelper().FetchReservationDetailsByReferenceNumber(
                        SessionData.OperaReservation.ReservationNumber,
                        new APIRequestModel { RequestObject = SessionData.OperaReservation.ReservationNumber },
                        ActionGroup,
                        ConfigurationManager.AppSettings["APIBaseUrl"].ToString());
                    if (existingResDt?.responseData != null)
                    {
                        var existingCloudRes = JsonConvert.DeserializeObject<List<CloudReservationModel>>(existingResDt.responseData.ToString()).FirstOrDefault();
                        alreadyPrecheckoutCompleted = existingCloudRes != null && (existingCloudRes.IsPrecheckOutPMS ?? false);
                    }
                }
            }
            catch (Exception existingEx)
            {
                new LogHelper().Error(existingEx, SessionData.OperaReservation?.ReservationNameID, ActionName, ActionGroup);
            }

            #region Pushing Reservation Track
            new LogHelper().Log("Pushing reservation track in local DB ", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
            #region Updating record status in Local DB
            new LogHelper().Log("Updating the reservation status in Local DB", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
            var localResponse = await new CloudHelper().UpdateReservationStatus(SessionData.OperaReservation.ReservationNameID, new Models.APIRequestModel()
            {
                RequestObject = new ReservationStatusRequestModel
                {
                    ReservationID = SessionData.OperaReservation.ReservationNameID,
                    Type = "PreCheckOutComplete"
                }
            }, "Due-In push", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());
            if (!localResponse.result)
            {
                new LogHelper().Log("Updating the reservation status in Local DB with email send flag failed with reason :- " + localResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
            }
            else
            {
                new LogHelper().Log("Updating the reservation status in Local DB ", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                if (!alreadyPrecheckoutCompleted)
                {
                    AuditProgressHelper.Log(
                        AuditProgressHelper.ModulePreCheckout,
                        AuditProgressHelper.Actions.PrecheckoutCompleted,
                        ReservationID,
                        SessionData.OperaReservation?.ReservationNameID);
                }
            }
            #endregion

            #region Opera reservation TRACE (main page — not profile)
            if (!alreadyPrecheckoutCompleted)
            {
                // Mark early so a parallel Agree/OK click does not post a second trace
                if (!string.IsNullOrEmpty(precheckoutTraceSessionKey))
                    Session[precheckoutTraceSessionKey] = true;

                try
                {
                    string precheckoutTraceText = ConfigurationManager.AppSettings["PreCheckoutCompletedTraceMessage"];
                    if (string.IsNullOrWhiteSpace(precheckoutTraceText))
                        precheckoutTraceText = "pre-check-out completed";

                    // When guest windows are clear but company/TA still has balance, note it for FO in Opera
                    if (SessionData.FolioModel != null)
                    {
                        decimal guestSum = (SessionData.FolioModel.FolioWindows != null && SessionData.FolioModel.FolioWindows.Count > 0)
                            ? SessionData.FolioModel.FolioWindows.Sum(w => w.BalanceAmount)
                            : SessionData.FolioModel.BalanceAmount;
                        decimal companyOrTaBalance = SessionData.FolioModel.ReservationBalance - guestSum;
                        if (Math.Abs(companyOrTaBalance) > 0.0001m)
                        {
                            precheckoutTraceText = precheckoutTraceText
                                + " — company/TA folio balance " + companyOrTaBalance.ToString("0.00")
                                + " remains; settle company folio manually";
                        }
                    }

                    var traceResponse = await new CloudHelper().AddReservationCompletionTrace(
                        SessionData.OperaReservation.ReservationNameID,
                        SessionData.OperaReservation.ReservationNumber,
                        precheckoutTraceText,
                        ActionGroup,
                        ConfigurationManager.AppSettings["APIBaseUrl"].ToString());

                    if (traceResponse != null && traceResponse.result)
                        new LogHelper().Log("Opera reservation trace posted: " + precheckoutTraceText, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                    else
                        new LogHelper().Log(
                            "Failed to post Opera reservation trace with reason :- " + (traceResponse != null ? traceResponse.responseMessage : "null"),
                            SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                }
                catch (Exception traceEx)
                {
                    new LogHelper().Error(traceEx, SessionData.OperaReservation?.ReservationNameID, ActionName, ActionGroup);
                }
            }
            else
            {
                new LogHelper().Log("Pre check-out already completed — skipping Opera reservation trace", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
            }
            #endregion

            localResponse = await new CloudHelper().PushReservationTrackLocally(SessionData.OperaReservation.ReservationNameID, new Models.APIRequestModel()
            {
                RequestObject = new Models.ReservationTrackStatus()
                {
                    ReservationNameID = SessionData.OperaReservation.ReservationNameID,
                    ProcessType = Models.ReservationProcessType.PreCheckedOutFetched.ToString(),
                    ReservationNumber = SessionData.OperaReservation.ReservationNumber,
                    ProcessStatus = "Precheckout",
                    EmailSent = false
                }
            }, "pre checked-in fetch", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());

            if (localResponse.result)
            {
                new LogHelper().Log("Reservation track in local DB updated successfully ", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
            }
            else
            {
                new LogHelper().Log("Failed to update reservation track in local DB with reason :- " + localResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
            }

            #endregion

            #region Checkout Reservation
            bool isAutoCheckOutEnabled = bool.TryParse(
    ConfigurationManager.AppSettings["isAutoCheckOutEnabled"],
    out bool result
) && result;
            new LogHelper().Debug("isAutoCheckOutEnabled : " + isAutoCheckOutEnabled, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);

            if (isAutoCheckOutEnabled)
            {
                new LogHelper().Debug("Processing reservation No. : " + SessionData.OperaReservation.ReservationNumber + " to do check out", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                if (SessionData.FolioModel != null)
                {
                    new LogHelper().Debug("verifying guest balance : " + SessionData.FolioModel.BalanceAmount, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                    if (SessionData.FolioModel.BalanceAmount > 0)
                    {
                        #region Pushing Reservation Track

                        new LogHelper().Debug("Pushing reservation track in local DB ", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                        localResponse = await new CloudHelper().PushReservationTrackLocally(SessionData.OperaReservation.ReservationNameID, new Models.APIRequestModel()
                        {
                            RequestObject = new Models.ReservationTrackStatus()
                            {
                                ReservationNameID = SessionData.OperaReservation.ReservationNameID,
                                ProcessType = Models.ReservationProcessType.CheckoutFailled.ToString(),
                                ReservationNumber = SessionData.OperaReservation.ReservationNumber,
                                ProcessStatus = "Checkout",
                                EmailSent = false
                            }
                        }, "pre checked-out fetch", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());
                        if (localResponse.result)
                        {
                            new LogHelper().Log("Reservation track in local DB updated successfully ", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                        }
                        else
                        {
                            new LogHelper().Log("Failed to update reservation track in local DB with reason :- " + localResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                        }
                        #endregion
                        new LogHelper().Log("Failed to process check out, where the guest balance is greater than 0", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                    }
                    else
                    {
                        new LogHelper().Log("verifying reservation balance : " + SessionData.FolioModel.ReservationBalance + " and isallowed to check out flag", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);

                        if (SessionData.FolioModel.ReservationBalance > 0)
                        {
                            #region Pushing Reservation Track

                            new LogHelper().Log("Pushing reservation track in local DB ", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);

                            localResponse = await new CloudHelper().PushReservationTrackLocally(SessionData.OperaReservation.ReservationNameID, new Models.APIRequestModel()
                            {
                                RequestObject = new Models.ReservationTrackStatus()
                                {
                                    ReservationNameID = SessionData.OperaReservation.ReservationNameID,
                                    ProcessType = Models.ReservationProcessType.CheckoutFailled.ToString(),
                                    ReservationNumber = SessionData.OperaReservation.ReservationNumber,
                                    ProcessStatus = "Checkout",
                                    EmailSent = false
                                }
                            }, "pre checked-out fetch", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());
                            if (localResponse.result)
                            {
                                new LogHelper().Log("Reservation track in local DB updated successfully ", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                            }
                            else
                            {
                                new LogHelper().Log("Failed to update reservation track in local DB with reason :- " + localResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                            }

                            #endregion

                            new LogHelper().Log("Failed to process check out, where the reservation balance is greater than 0", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                        }
                        else
                        {
                            if (SessionData.FolioModel.IsAllowedForCheckOut == true)
                            {
                                Models.OWS.OwsResponseModel owsresponse1 = await new CloudHelper().CheckoutReservation(SessionData.OperaReservation.ReservationNameID, new Models.OWS.OwsRequestModel()
                                {
                                    ChainCode = ChainCode,
                                    DestinationEntityID = DestinationEntityID,
                                    HotelDomain = HotelDomain,
                                    KioskID = KioskID,
                                    Language = Language,
                                    LegNumber = LegNumber,
                                    Password = Password,
                                    SystemType = SystemType,
                                    Username = Username,
                                    SendFolio = true,
                                    OperaReservation = new Models.OWS.OperaReservation()
                                    {
                                        ReservationNameID = SessionData.OperaReservation.ReservationNameID
                                    }
                                }, "pre checked-out fetch", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());

                                if (!owsresponse1.result || owsresponse1.responseData == null)
                                {
                                    #region pushing reservation track

                                    new LogHelper().Debug("pushing reservation track in local db ", SessionData.OperaReservation.ReservationNameID, "FetchDueoutReservation", "pre checked-out fetch");

                                    localResponse = await new Helpers.CloudHelper().PushReservationTrackLocally(SessionData.OperaReservation.ReservationNameID, new Models.APIRequestModel()
                                    {
                                        RequestObject = new Models.ReservationTrackStatus()
                                        {
                                            ReservationNameID = SessionData.OperaReservation.ReservationNameID,
                                            ProcessType = Models.ReservationProcessType.CheckoutFailled.ToString(),
                                            ReservationNumber = SessionData.OperaReservation.ReservationNumber,
                                            ProcessStatus = "Checkout",
                                            EmailSent = false
                                        }
                                    }, "pre checked-out fetch", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());
                                    if (localResponse.result)
                                    {
                                        new LogHelper().Debug("reservation track in local db updated successfully ", SessionData.OperaReservation.ReservationNameID, "FetchDueoutReservation", "pre checked-out fetch");
                                    }
                                    else
                                    {
                                        new LogHelper().Log("failed to update reservation track in local db with reason :- " + localResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, "FetchDueoutReservation", "pre checked-out fetch");
                                    }

                                    #endregion

                                    new LogHelper().Log("failed to check-out with reason :- " + owsresponse1.responseMessage, SessionData.OperaReservation.ReservationNameID, "FetchDueoutReservation", "pre checked-out fetch");
                                }
                                else
                                {
                                    new LogHelper().Debug("checked out successfully", SessionData.OperaReservation.ReservationNameID, "FetchDueoutReservation", "pre checked-out fetch");

                                    #region pushing reservation track

                                    new LogHelper().Debug("pushing reservation track in local db ", SessionData.OperaReservation.ReservationNameID, "FetchDueoutReservation", "pre checked-out fetch");

                                    localResponse = await new Helpers.CloudHelper().PushReservationTrackLocally(SessionData.OperaReservation.ReservationNameID, new Models.APIRequestModel()
                                    {
                                        RequestObject = new Models.ReservationTrackStatus()
                                        {
                                            ReservationNameID = SessionData.OperaReservation.ReservationNameID,
                                            ProcessType = Models.ReservationProcessType.CheckedoutSuccessfully.ToString(),
                                            ReservationNumber = SessionData.OperaReservation.ReservationNumber,
                                            ProcessStatus = "Checkout",
                                            EmailSent = false
                                        }
                                    }, "pre checked-out fetch", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());
                                    if (localResponse.result)
                                    {
                                        new LogHelper().Debug("reservation track in local db updated successfully ", SessionData.OperaReservation.ReservationNameID, "FetchDueoutReservation", "pre checked-out fetch");
                                    }
                                    else
                                    {
                                        new LogHelper().Log("failed to update reservation track in local db with reason :- " + localResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, "FetchDueoutReservation", "pre checked-out fetch");
                                    }

                                    #endregion

                                    //if (SessionData.OperaReservation.SharerReservations != null && operareservations.count > 0)
                                    if (SessionData.OperaReservation.SharerReservations != null)
                                    {
                                        new LogHelper().Debug("processing sharers", SessionData.OperaReservation.ReservationNameID, "FetchDueoutReservation", "pre checked-out fetch");
                                        foreach (Models.OWS.OperaReservation sharer in SessionData.OperaReservation.SharerReservations)
                                        {
                                            new LogHelper().Debug("processing sharer reservation - " + sharer.ReservationNumber, SessionData.OperaReservation.ReservationNameID, "FetchDueoutReservation", "pre checked-out fetch");

                                            if (!string.IsNullOrEmpty(sharer.ReservationNameID))
                                            {
                                                owsresponse1 = await new Helpers.CloudHelper().CheckoutReservation(SessionData.OperaReservation.ReservationNameID, new Models.OWS.OwsRequestModel()
                                                {
                                                    ChainCode = ChainCode,
                                                    DestinationEntityID = DestinationEntityID,
                                                    HotelDomain = HotelDomain,
                                                    KioskID = KioskID,
                                                    Language = Language,
                                                    LegNumber = LegNumber,
                                                    Password = Password,
                                                    SystemType = SystemType,
                                                    Username = Username,
                                                    OperaReservation = new Models.OWS.OperaReservation()
                                                    {
                                                        ReservationNameID = sharer.ReservationNameID
                                                    }
                                                }, "pre checked-out fetch", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());

                                                if (!owsresponse1.result || owsresponse1.responseData == null)
                                                {
                                                    #region pushing reservation track

                                                    new LogHelper().Debug("pushing reservation track in local db for sharer " + sharer.ReservationNumber, SessionData.OperaReservation.ReservationNameID, "FetchDueoutReservation", "pre checked-out fetch");

                                                    localResponse = await new Helpers.CloudHelper().PushReservationTrackLocally(SessionData.OperaReservation.ReservationNameID, new Models.APIRequestModel()
                                                    {
                                                        RequestObject = new Models.ReservationTrackStatus()
                                                        {
                                                            ReservationNameID = SessionData.OperaReservation.ReservationNameID,
                                                            ProcessType = Models.ReservationProcessType.CheckoutFailled.ToString(),
                                                            ReservationNumber = SessionData.OperaReservation.ReservationNumber,
                                                            ProcessStatus = "Checkout",
                                                            EmailSent = false
                                                        }
                                                    }, "pre checked-out fetch", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());
                                                    if (localResponse.result)
                                                    {
                                                        new LogHelper().Debug("reservation track in local db updated successfully ", SessionData.OperaReservation.ReservationNameID, "FetchDueoutReservation", "pre checked-out fetch");
                                                    }
                                                    else
                                                    {
                                                        new LogHelper().Log("failed to update reservation track in local db with reason :- " + localResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, "FetchDueoutReservation", "pre checked-out fetch");
                                                    }

                                                    #endregion

                                                    new LogHelper().Log("failed to check-out sharer with reason :- " + owsresponse1.responseMessage + "sharer reservation no. : " + sharer.ReservationNumber, SessionData.OperaReservation.ReservationNameID, "FetchDueoutReservation", "pre checked-out fetch");
                                                    new LogHelper().Warn("failed to check-out sharer with reason :- " + owsresponse1.responseMessage + "sharer reservation no. : " + sharer.ReservationNumber, SessionData.OperaReservation.ReservationNameID, "FetchDueoutReservation", "pre checked-out fetch");

                                                }
                                                else
                                                {
                                                    #region pushing reservation track

                                                    new LogHelper().Debug("pushing reservation track in local db for sharer " + sharer.ReservationNumber, SessionData.OperaReservation.ReservationNameID, "FetchDueoutReservation", "pre checked-out fetch");

                                                    localResponse = await new Helpers.CloudHelper().PushReservationTrackLocally(SessionData.OperaReservation.ReservationNameID, new Models.APIRequestModel()
                                                    {
                                                        RequestObject = new Models.ReservationTrackStatus()
                                                        {
                                                            ReservationNameID = SessionData.OperaReservation.ReservationNameID,
                                                            ProcessType = Models.ReservationProcessType.CheckedoutSuccessfully.ToString(),
                                                            ReservationNumber = SessionData.OperaReservation.ReservationNumber,
                                                            ProcessStatus = "Checkout",
                                                            EmailSent = false
                                                        }
                                                    }, "pre checked-out fetch", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());
                                                    if (localResponse.result)
                                                    {
                                                        new LogHelper().Debug("reservation track in local db updated successfully ", SessionData.OperaReservation.ReservationNameID, "FetchDueoutReservation", "pre checked-out fetch");
                                                    }
                                                    else
                                                    {
                                                        new LogHelper().Log("failed to update reservation track in local db with reason :- " + localResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, "FetchDueoutReservation", "pre checked-out fetch");
                                                    }

                                                    #endregion

                                                    new LogHelper().Log("sharer checked out successfully", SessionData.OperaReservation.ReservationNameID, "FetchDueoutReservation", "pre checked-out fetch");
                                                }
                                            }
                                            else
                                            {
                                                #region pushing reservation track

                                                new LogHelper().Debug("pushing reservation track in local db for sharer " + sharer.ReservationNumber, SessionData.OperaReservation.ReservationNameID, "FetchDueoutReservation", "pre checked-out fetch");

                                                localResponse = await new Helpers.CloudHelper().PushReservationTrackLocally(SessionData.OperaReservation.ReservationNameID, new Models.APIRequestModel()
                                                {
                                                    RequestObject = new Models.ReservationTrackStatus()
                                                    {
                                                        ReservationNameID = SessionData.OperaReservation.ReservationNameID,
                                                        ProcessType = Models.ReservationProcessType.CheckoutFailled.ToString(),
                                                        ReservationNumber = SessionData.OperaReservation.ReservationNumber,
                                                        ProcessStatus = "Checkout",
                                                        EmailSent = false
                                                    }
                                                }, "pre checked-out fetch", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());
                                                if (localResponse.result)
                                                {
                                                    new LogHelper().Debug("reservation track in local db updated successfully ", SessionData.OperaReservation.ReservationNameID, "FetchDueoutReservation", "pre checked-out fetch");
                                                }
                                                else
                                                {
                                                    new LogHelper().Log("failed to update reservation track in local db with reason :- " + localResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, "FetchDueoutReservation", "pre checked-out fetch");
                                                }

                                                #endregion

                                                new Helpers.LogHelper().Log("reservation name id is null ", SessionData.OperaReservation.ReservationNameID, "FetchDueoutReservation", "pre checked-out fetch");
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                #region pushing reservation track

                                new LogHelper().Debug("pushing reservation track in local db ", SessionData.OperaReservation.ReservationNameID, "FetchDueoutReservation", "pre checked-out fetch");

                                localResponse = await new Helpers.CloudHelper().PushReservationTrackLocally(SessionData.OperaReservation.ReservationNameID, new Models.APIRequestModel()
                                {
                                    RequestObject = new Models.ReservationTrackStatus()
                                    {
                                        ReservationNameID = SessionData.OperaReservation.ReservationNameID,
                                        ProcessType = Models.ReservationProcessType.CheckoutFailled.ToString(),
                                        ReservationNumber = SessionData.OperaReservation.ReservationNumber,
                                        ProcessStatus = "Checkout",
                                        EmailSent = false
                                    }
                                }, "pre checked-out fetch", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());
                                if (localResponse.result)
                                {
                                    new LogHelper().Debug("reservation track in local db updated successfully ", SessionData.OperaReservation.ReservationNameID, "FetchDueoutReservation", "pre checked-out fetch");
                                }
                                else
                                {
                                    new LogHelper().Log("failed to update reservation track in local db with reason :- " + localResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, "FetchDueoutReservation", "pre checked-out fetch");
                                }

                                #endregion

                                new LogHelper().Log("failed to process check out, where the is allowedfor checkout flag is either null or  false", SessionData.OperaReservation.ReservationNameID, "FetchDueoutReservation", "pre checked-out fetch");
                            }
                        }
                    }
                }
                else
                {
                    #region Pushing Reservation Track

                    new LogHelper().Debug("Pushing reservation track in local DB ", SessionData.OperaReservation.ReservationNameID, "FetchDueOutReservation", "pre checked-out fetch");

                    localResponse = await new Helpers.CloudHelper().PushReservationTrackLocally(SessionData.OperaReservation.ReservationNameID, new Models.APIRequestModel()
                    {
                        RequestObject = new Models.ReservationTrackStatus()
                        {
                            ReservationNameID = SessionData.OperaReservation.ReservationNameID,
                            ProcessType = Models.ReservationProcessType.CheckoutFailled.ToString(),
                            ReservationNumber = SessionData.OperaReservation.ReservationNumber,
                            ProcessStatus = "Checkout",
                            EmailSent = false
                        }
                    }, "pre checked-out fetch", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());
                    if (localResponse.result)
                    {
                        new LogHelper().Debug("reservation track in local db updated successfully ", SessionData.OperaReservation.ReservationNameID, "FetchDueoutReservation", "pre checked-out fetch");
                    }
                    else
                    {
                        new LogHelper().Log("failed to update reservation track in local db with reason :- " + localResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, "FetchDueoutReservation", "pre checked-out fetch");
                    }

                    #endregion

                    new LogHelper().Log("Failed to process check out, failed to retreave the guest balance", SessionData.OperaReservation.ReservationNameID, "FetchDueOutReservation", "pre checked-out fetch");
                }
            }
            #endregion

            #region Sending Email
            bool? isEmailSent = false;
            new LogHelper().Log("Sending final folio email", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
            string folioToEmail = null;
            if (SessionData.OperaReservation.GuestProfiles != null && SessionData.OperaReservation.GuestProfiles.Count > 0
                && SessionData.OperaReservation.GuestProfiles[0].Email != null && SessionData.OperaReservation.GuestProfiles[0].Email.Count > 0)
            {
                var emails = SessionData.OperaReservation.GuestProfiles[0].Email;
                var primary = emails.FirstOrDefault(e => e.primary != null && e.primary.Value && !string.IsNullOrWhiteSpace(e.email));
                folioToEmail = primary != null
                    ? primary.email
                    : emails.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e.email))?.email;
            }

            if (!string.IsNullOrEmpty(SessionData.FolioBase64) && !string.IsNullOrWhiteSpace(folioToEmail))
            {
                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                var guestProfile = SessionData.OperaReservation.GuestProfiles[0];
                Models.Emails.EmailResponse emailResponse = await new CloudHelper().SendEmail(SessionData.OperaReservation.ReservationNameID, new Models.Emails.EmailRequest()
                {
                    FromEmail = ConfigurationManager.AppSettings["PreCheckoutFolioEmail"].ToString(),
                    ToEmail = folioToEmail,
                    GuestName = "" + (!string.IsNullOrEmpty(guestProfile.FirstName) ? textInfo.ToTitleCase(guestProfile.FirstName) + " " : "")
                                    + (!string.IsNullOrEmpty(guestProfile.MiddleName) ? textInfo.ToTitleCase(guestProfile.MiddleName) + " " : "")
                                    + (!string.IsNullOrEmpty(guestProfile.LastName) ? textInfo.ToTitleCase(guestProfile.LastName) : ""),
                    Subject = ConfigurationManager.AppSettings["PreCheckoutFolioEmailSubject"].ToString(),
                    confirmationNumber = SessionData.OperaReservation.ReservationNumber,
                    displayFromEmail = ConfigurationManager.AppSettings["EmailDisplayName"].ToString(),
                    EmailType = Models.Emails.EmailType.GuestFolio,
                    AttchmentBase64 = SessionData.FolioBase64,
                    AttachmentFileName = "Folio.pdf"

                }, "pre checked-out fetch", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());

                if (!emailResponse.result)
                {
                    isEmailSent = false;
                    new LogHelper().Log("Failed to send confirmation email with reason :- " + emailResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                    new LogHelper().Warn("Failed to send confirmation email with reason :- " + emailResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                }
                else
                {
                    isEmailSent = true;
                    new LogHelper().Log("Email send successfully to " + folioToEmail, SessionData.OperaReservation.ReservationNameID, "CompletePreCheckout", "pre checked-out complete");

                }
            }
            else
            {
                string skipReason = string.IsNullOrEmpty(SessionData.FolioBase64)
                    ? "folio attachment missing"
                    : "email address not found on guest profile";
                new LogHelper().Log("Failed to send guest folio email since " + skipReason, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                new LogHelper().Warn("Failed to send guest folio email since " + skipReason, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
            }
            #endregion

            #region Final step

            new LogHelper().Debug("Pushing reservation track in local DB ", SessionData.OperaReservation.ReservationNameID, "FetchDueOutReservation", "pre checked-out fetch");

            new LogHelper().Debug("Pushing reservation track in local DB ", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
            localResponse = await new CloudHelper().PushReservationTrackLocally(SessionData.OperaReservation.ReservationNameID, new Models.APIRequestModel()
            {
                RequestObject = new Models.ReservationTrackStatus()
                {
                    ReservationNameID = SessionData.OperaReservation.ReservationNameID,
                    ProcessType = Models.ReservationProcessType.PrecheckoutCompleted.ToString(),
                    ReservationNumber = SessionData.OperaReservation.ReservationNumber,
                    ProcessStatus = "Precheckout",
                    EmailSent = isEmailSent
                }
            }, "pre checked-out complete", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());
            if (localResponse.result)
            {
                new LogHelper().Debug("Reservation track in local DB updated successfully ", SessionData.OperaReservation.ReservationNameID, "FetchDueOutReservation", "pre checked-out fetch");
            }
            else
            {
                new LogHelper().Log("Failed to update reservation track in local DB with reason :- " + localResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, "FetchDueOutReservation", "pre checked-out fetch");
            }

            #endregion
            return Json(new { result = true });
        }

        public ActionResult UpdateCheckoutFlag(int ReservationID)
        {
            Helpers.LogHelper.Instance.Log($"Updating  checkout flag.", "", "Checkout/UpdateSignature", "Pre-Checkout");
            Models.CheckoutReservationModel checkoutReservation = new Models.CheckoutReservationModel();
            var reservations = reservationLogics.UpdateCheckoutFlag(ReservationID);
            return Json(new { result = true });
        }


        public async Task<ActionResult> UpdateSignature(PoliciesModel policiesModel)
        {
            string ActionName = "UpdateSignature", ActionGroup = "Pre-Checkout";
            int count = 0;
            string folioAsBase64 = "";
            //push events to DB
            reservationLogics.InsertEvent(policiesModel.ReservationID, "Folio Sign");
            AuditProgressHelper.Log(
                AuditProgressHelper.ModulePreCheckout,
                AuditProgressHelper.Actions.FolioAgreed,
                policiesModel.ReservationID,
                SessionData.OperaReservation?.ReservationNameID ?? policiesModel.ReservationNameID);

            Helpers.LogHelper.Instance.Log($"Updating folio signature", "", ActionName, ActionGroup);
            #region FetchFolioAsBase64
            new LogHelper().Log("Fetching reservation folio as base64 for reservation No. : " + SessionData.OperaReservation.ReservationNumber, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
            var owsResponse1 = await new CloudHelper().GetFolio(SessionData.OperaReservation.ReservationNameID, new Models.OWS.OwsRequestModel()
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
                FetchFolioRequest = new Models.OWS.FetchFolioRequest()
                {
                    GuestSignature = policiesModel.Base64Signature,
                    ReservationNameID = SessionData.OperaReservation.ReservationNameID,
                    OperaReservation = (Models.OWS.OperaReservation)SessionData.OperaReservation,
                    ProfileID = (SessionData.OperaReservation.GuestProfiles != null && SessionData.OperaReservation.GuestProfiles.Count > 0) ? SessionData.OperaReservation.GuestProfiles[0].PmsProfileID : "",
                    FolioList = SessionData.FolioModel
                }
            }, "Due-Out push", ConfigurationManager.AppSettings
                ["APIBaseUrl"].ToString());

            if (!owsResponse1.result || owsResponse1.responseData == null)
            {
                new LogHelper().Log("Failed to fetch folio with reason :- " + owsResponse1.responseMessage, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                new LogHelper().Warn("Failed to fetch folio with reason :- " + owsResponse1.responseMessage, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                return ShowLinkExpiry(LinkExpiryHelper.NotEligible, SessionData.OperaReservation?.ReservationNameID ?? "0", ActionName, ActionGroup);
            }
            else
            {
                folioAsBase64 = owsResponse1.responseData.ToString();
                SessionData.FolioBase64 = folioAsBase64;
                new LogHelper().Log("Fetched guest folio as base64 successfully", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
            }
            #endregion
            #region Pushing Guest Signature to Local DB
            if (!string.IsNullOrEmpty(policiesModel.Base64Signature))
            {
                try
                {
                    new LogHelper().Log("Pushing guest signature to Local DB", "", ActionName, ActionGroup);
                    byte[] guestSignature = null;
                    guestSignature = Convert.FromBase64String(policiesModel.Base64Signature);
                    var localResponse = await new CloudHelper().InsertReservationDocuments("", new Models.APIRequestModel()
                    {
                        RequestObject = new List<Models.ReservationDocumentsDataTableModel>()
                                {
                                    new Models.ReservationDocumentsDataTableModel()
                                    {
                                        Document = guestSignature,
                                        DocumentType = "Signature",
                                        ReservationNameID = SessionData.OperaReservation.ReservationNameID
                                    }
                                }
                    }, "pre checked-in fetch", ConfigurationManager.AppSettings
                     ["APIBaseUrl"].ToString());
                    if (!localResponse.result)
                    {
                        new LogHelper().Log("Failed to push guest signature with reason :- " + localResponse.responseMessage, "", ActionName, ActionGroup);
                        new LogHelper().Warn("Failed to push guest signature with reason :- " + localResponse.responseMessage, "", ActionName, ActionGroup);
                    }
                    else
                    {
                        new LogHelper().Log("Signature updated successfully", "", ActionName, ActionGroup);
                        AuditProgressHelper.Log(
                            AuditProgressHelper.ModulePreCheckout,
                            AuditProgressHelper.Actions.SignatureCompleted,
                            policiesModel.ReservationID,
                            SessionData.OperaReservation?.ReservationNameID);
                        AuditProgressHelper.Log(
                            AuditProgressHelper.ModulePreCheckout,
                            AuditProgressHelper.Actions.FolioSigned,
                            policiesModel.ReservationID,
                            SessionData.OperaReservation?.ReservationNameID);
                    }
                }
                catch (Exception exc)
                {
                    new LogHelper().Error(exc, "", ActionName, ActionGroup);
                }
            }
            #endregion
            #region Pushing folio to Local DB
            if (!string.IsNullOrEmpty(folioAsBase64))
            {
                try
                {
                    new LogHelper().Log("Pushing registration card to Local DB", "", ActionName, ActionGroup); byte[] folio = null;
                    folio = Convert.FromBase64String(folioAsBase64);
                    var localResponse = await new CloudHelper().InsertReservationDocuments("", new Models.APIRequestModel()
                    {
                        RequestObject = new List<Models.ReservationDocumentsDataTableModel>()
                                {
                                    new Models.ReservationDocumentsDataTableModel()
                                    {
                                        Document = folio,
                                        DocumentType = "Folio",
                                        ReservationNameID =SessionData.OperaReservation.ReservationNameID
                                    }
                                }
                    }, "pre checked-in fetch", ConfigurationManager.AppSettings
                     ["APIBaseUrl"].ToString());
                    if (!localResponse.result)
                    {
                        new LogHelper().Log("Failed to push reservation document with reason :- " + localResponse.responseMessage, "", ActionName, ActionGroup); new LogHelper().Warn("Failed to push reservation document with reason :- " + localResponse.responseMessage, "", ActionName, ActionGroup);
                    }
                    else
                    {
                        new LogHelper().Log("Reservation document updated successfully", "", ActionName, ActionGroup);
                    }
                }
                catch (Exception exc)
                {
                    new LogHelper().Error(exc, "", ActionName, ActionGroup);
                }
            }
            #endregion
            //reservationLogics.UpdateReservationByStage("Policies", new UpdateReservationModel()
            //{
            //    SignatureImage = Convert.FromBase64String(policiesModel.Base64Signature),
            //    ReservationID = policiesModel.ReservationID
            //});
            return Json(new { result = count > 0 });
        }


        //This is the function where adyen response will be send back
        public async Task<ActionResult> PaymentResponseFromGateway(string ConfirmationNo, string TransactionID)
        {
            string ActionName = "PaymentResponseFromGateway", ActionGroup = "Pre-Checkout";

            Helpers.LogHelper.Instance.Log($"Payment response from PG after 3ds.", $"{ConfirmationNo}", ActionName, ActionGroup);
            List<Models.PaymentHistory> paymentHistories = new List<Models.PaymentHistory>();
            List<Models.PaymentHistory> paymentHistory = new List<Models.PaymentHistory>();
            List<Models.PaymentAdditionalInfo> paymentAdditionalInfos = new List<Models.PaymentAdditionalInfo>();
            List<Models.PaymentHeader> paymentHeaders = new List<Models.PaymentHeader>();

            PaymentLogics paymentLogics = new PaymentLogics();

            var paymentHistrory = await new CloudHelper().FetchPaymentHistory(ConfirmationNo, new APIRequestModel() { RequestObject = ConfirmationNo }, "", ConfigurationManager.AppSettings
                    ["APIBaseUrl"].ToString());
            if (paymentHistrory != null)
            {
                paymentHistory = (List<PaymentHistory>)paymentHistrory.responseData;
            }
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
                    RequestObject = paymentDetailsModel
                };

                httpClient.BaseAddress = new Uri(BaseURL);

                string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(makePaymentDetailRequestModelRoot);

                httpClient.DefaultRequestHeaders.Clear();

                HttpContent requestContent = new StringContent(jsonString, Encoding.UTF8, "application/json");

                Helpers.LogHelper.Instance.Log($"Getting Payment Details from PG", $"{ConfirmationNo}", ActionName, ActionGroup);

                HttpResponseMessage response = await httpClient.PostAsync($"GetPaymentDetails", requestContent);

                if (response.IsSuccessStatusCode)
                {
                    string Test = await response.Content.ReadAsStringAsync();

                    Helpers.LogHelper.Instance.Debug($"Got Payment Response from PG : {Test}", $"{ConfirmationNo}", ActionName, ActionGroup);

                    var resposneObj = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.AdaptorAPIModels.MakePaymentResponseModel>(Test);

                    if (resposneObj != null && resposneObj.Result)
                    {
                        Helpers.LogHelper.Instance.Log($"Updating payment details to DB", $"{ConfirmationNo}", ActionName, ActionGroup);

                        #region create pamentdata
                        try
                        {
                            paymentHeaders.Add(new PaymentHeader
                            {
                                MaskedCardNumber = resposneObj.ResponseObject.MaskCardNumber,
                                FundingSource = resposneObj.ResponseObject.FundingSource,
                                Amount = resposneObj.ResponseObject.Amount.Value.ToString("0.00"),
                                TransactionID = paymentHistory != null ? paymentHistory.FirstOrDefault().TransactionID : "",
                                ReservationNumber = SessionData.OperaReservation.ReservationNumber,
                                ReservationNameID = SessionData.OperaReservation.ReservationNameID,
                                ExpiryDate = resposneObj.ResponseObject.CardExpiryDate,
                                AuthorisationCode = resposneObj.ResponseObject.AuthCode,
                                Currency = resposneObj.ResponseObject.Currency,
                                RecurringIdentifier = resposneObj.ResponseObject.PaymentToken,
                                pspReferenceNumber = resposneObj.ResponseObject.PspReference,
                                ParentPspRefereceNumber = string.IsNullOrEmpty(resposneObj.ResponseObject.ParentPSPReferece) ? resposneObj.ResponseObject.PspReference : resposneObj.ResponseObject.ParentPSPReferece,
                                TransactionType = paymentHistory != null ? paymentHistory.FirstOrDefault().TransactionID : "TransactionType",
                                ResultCode = resposneObj.ResponseObject.ResultCode,
                                ResponseMessage = resposneObj.ResponseObject.RefusalReason,
                                CardType = resposneObj.ResponseObject.CardType
                            });



                            DataTable dataTable = new DataTable();
                            dataTable.Columns.Add("KeyHeader", typeof(string));
                            dataTable.Columns.Add("KeyValue", typeof(string));


                            if (resposneObj.ResponseObject.additionalInfos != null)
                            {
                                foreach (var item in resposneObj.ResponseObject.additionalInfos)
                                {
                                    paymentAdditionalInfos.Add(new PaymentAdditionalInfo
                                    {
                                        KeyHeader = item.key,
                                        KeyValue = item.value
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

                        if (resposneObj.ResponseObject.ResultCode == "Authorised")
                        {
                            Helpers.LogHelper.Instance.Log($"Payment {resposneObj.ResponseObject.ResultCode}, Updating payment status to BD", $"{ConfirmationNo}", ActionName, ActionGroup);
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
                                new LogHelper().Log("Iterating the payment headers", "", ActionName, ActionGroup);
                                foreach (Models.PaymentHeader paymentHeader in paymentDetails.paymentHeaders)
                                {
                                    if (paymentHeader.IsActive == null)
                                    {
                                        new LogHelper().Log("Processing the payment header with psprefernce - " + paymentHeader.pspReferenceNumber + " where IsActive falg is NULL", "", ActionName, ActionGroup);

                                        #region Update Opera
                                        if (paymentDetails.paymentHeaders[x].TransactionType.Equals(Models.TransactionType.PreAuth.ToString()))
                                        {
                                            new LogHelper().Log("Processing the payment header with psprefernce - " + paymentHeader.pspReferenceNumber + " as a pre-auth transaction", "", ActionName, ActionGroup);

                                            paymentDetails.paymentHeaders[x].IsActive = true;

                                            #region Updating Card details in opera Reservation
                                            new LogHelper().Log("Updating credit card details in the reservation", "", ActionName, ActionGroup);

                                            Models.OWS.OwsResponseModel owsResponse = await new CloudHelper().UpdateCardDetailsInReservationAsyn("", new Models.OWS.OwsRequestModel()
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
                                                    ReservationNumber = SessionData.OperaReservation.ReservationNumber,
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
                                            }, "pre checked-in fetch", ConfigurationManager.AppSettings
                ["APIBaseUrl"].ToString());
                                            if (!owsResponse.result)
                                            {
                                                new LogHelper().Log("Updating credit card details in the reservation failed with reason :- " + owsResponse.responseMessage, "", ActionName, ActionGroup);
                                                new LogHelper().Warn("Updating credit card details in the reservation failed with reason :- " + owsResponse.responseMessage, "", ActionName, ActionGroup);
                                            }
                                            else
                                            {
                                                new LogHelper().Log("Updating credit card details in the reservation succeeded", "", ActionName, ActionGroup);
                                                new LogHelper().Warn("Updating credit card details in the reservation succeeded", "", ActionName, ActionGroup);
                                            }
                                            #endregion

                                            #region Updating UDF fields in Opera reservation
                                            try
                                            {
                                                new LogHelper().Log("Updating pre auth code and amount in UDF fileds", "", ActionName, ActionGroup);
                                                owsResponse = await new CloudHelper().ModifyBooking(SessionData.OperaReservation.ReservationNameID, new Models.OWS.OwsRequestModel()
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
                                                        ReservationNumber = SessionData.OperaReservation.ReservationNumber,
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
                                                }, "pre checked-in fetch", ConfigurationManager.AppSettings
                ["APIBaseUrl"].ToString()
);
                                                if (!owsResponse.result)
                                                {
                                                    new LogHelper().Log("Updating pre auth code and amount in UDF fileds failed with reason : - " + owsResponse.responseMessage, "", ActionName, ActionGroup);
                                                    new LogHelper().Warn("Updating pre auth code and amount in UDF fileds failed with reason : - " + owsResponse.responseMessage, "", ActionName, ActionGroup);
                                                }
                                                else
                                                    new LogHelper().Log("Updating pre auth code and amount in UDF fileds succeeded ", "", ActionName, ActionGroup);
                                            }
                                            catch (Exception ex)
                                            {
                                                new LogHelper().Error(ex, "", ActionName, ActionGroup);
                                            }
                                            #endregion

                                        }
                                        else if (paymentDetails.paymentHeaders[x].TransactionType.Equals(Models.TransactionType.Sale.ToString()))
                                        {
                                            paymentDetails.paymentHeaders[x].IsActive = false;

                                            #region Updating Card details in opera Reservation
                                            new LogHelper().Log("Updating credit card details in the reservation", "", ActionName, ActionGroup);

                                            Models.OWS.OwsResponseModel owsResponse = await new CloudHelper().UpdateCardDetailsInReservationAsyn(SessionData.OperaReservation.ReservationNameID, new Models.OWS.OwsRequestModel()
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
                                                    ReservationNumber = SessionData.OperaReservation.ReservationNumber,
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
                                            }, "pre checked-in fetch", ConfigurationManager.AppSettings
                ["APIBaseUrl"].ToString());
                                            if (!owsResponse.result)
                                            {
                                                new LogHelper().Log("Updating credit card details in the reservation failed with reason :- " + owsResponse.responseMessage, "", ActionName, ActionGroup);
                                                new LogHelper().Warn("Updating credit card details in the reservation failed with reason :- " + owsResponse.responseMessage, "", ActionName, ActionGroup);
                                            }
                                            else
                                            {
                                                new LogHelper().Log("Updating credit card details in the reservation succeeded", "", ActionName, ActionGroup);
                                                new LogHelper().Warn("Updating credit card details in the reservation succeeded", "", ActionName, ActionGroup);
                                            }
                                            #endregion

                                            #region Updating UDF fields in Opera reservation
                                            try
                                            {
                                                new LogHelper().Log("Updating pre auth code and amount in UDF fileds", "", ActionName, ActionGroup);
                                                owsResponse = await new CloudHelper().ModifyBooking(SessionData.OperaReservation.ReservationNameID, new Models.OWS.OwsRequestModel()
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
                                                        ReservationNumber = SessionData.OperaReservation.ReservationNumber,
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
                                                }, "pre checked-in fetch", ConfigurationManager.AppSettings
                ["APIBaseUrl"].ToString());
                                                if (!owsResponse.result)
                                                {
                                                    new LogHelper().Log("Updating pre auth code and amount in UDF fileds failed with reason : - " + owsResponse.responseMessage, "", ActionName, ActionGroup);
                                                    new LogHelper().Warn("Updating pre auth code and amount in UDF fileds failed with reason : - " + owsResponse.responseMessage, "", ActionName, ActionGroup);
                                                }
                                                else
                                                    new LogHelper().Log("Updating pre auth code and amount in UDF fileds succeeded ", "", ActionName, ActionGroup);
                                            }
                                            catch (Exception ex)
                                            {
                                                new LogHelper().Error(ex, "", ActionName, ActionGroup);
                                            }
                                            #endregion

                                            #region Posting payment in opera reservation
                                            try
                                            {
                                                new LogHelper().Log("Posting payment in the reservation", "", ActionName, ActionGroup);
                                                owsResponse = await new CloudHelper().MakePayment(SessionData.OperaReservation.ReservationNameID, new Models.OWS.OwsRequestModel()
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
                                                        ReservationNameID = SessionData.OperaReservation.ReservationNameID,
                                                        MaskedCardNumber = paymentHeader.MaskedCardNumber.ToLower(),
                                                        PaymentRefernce = "Payment from web checkin - Sale",
                                                        PaymentTypeCode = paymentHeader.OperaPaymentTypeCode,
                                                        ApprovalCode = paymentHeader.pspReferenceNumber
                                                    }
                                                }, "pre checked-in fetch", ConfigurationManager.AppSettings
                ["APIBaseUrl"].ToString());
                                                if (!owsResponse.result)
                                                {
                                                    new LogHelper().Log("Posting payment failed with reason : - " + owsResponse.responseMessage, "", ActionName, ActionGroup);
                                                    new LogHelper().Warn("Posting payment failed with reason : - " + owsResponse.responseMessage, "", ActionName, ActionGroup);
                                                }
                                                else
                                                    new LogHelper().Log("Posting payment in opera reservation succeeded ", "", ActionName, ActionGroup);
                                            }
                                            catch (Exception ex)
                                            {
                                                new LogHelper().Error(ex, "", ActionName, ActionGroup);
                                            }
                                            #endregion
                                        }
                                        else if (paymentDetails.paymentHeaders[x].TransactionType.Equals(Models.TransactionType.Capture.ToString()))
                                        {
                                            new LogHelper().Log("Wrong payment header retuned and Is active NULL (Capture)", "", ActionName, ActionGroup);
                                            paymentDetails.paymentHeaders[x].IsActive = false;
                                        }

                                        #endregion
                                    }
                                    x++;
                                }
                                new LogHelper().Log("payment details updated successfully", "", ActionName, ActionGroup);

                            }
                            ////jkj
                            #endregion

                            #region Pushing Payment details in LOcal Db

                            new LogHelper().Log("Updating payment details in local DB", "", ActionName, ActionGroup);
                            var localResponse = await new CloudHelper().PushPaymentDetails(SessionData.OperaReservation.ReservationNameID, new Models.APIRequestModel()
                            {
                                RequestObject = paymentDetails
                            }, "pre checked-in fetch", ConfigurationManager.AppSettings
                      ["APIBaseUrl"].ToString());
                            if (!localResponse.result)
                            {
                                new LogHelper().Log("Failed to update payment details in Local DB with reason :- " + localResponse.responseMessage, "", ActionName, ActionGroup);
                                new LogHelper().Warn("Failed to update payment details in local DB with reason :- " + localResponse.responseMessage, "", ActionName, ActionGroup);
                            }
                            else
                                new LogHelper().Log("Payment details updated successfully", "", ActionName, ActionGroup);
                            #endregion

                            #region Pushing Reservation Track

                            new LogHelper().Log("Pushing reservation track in local DB ", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                            #region Updating record status in Local DB
                            new LogHelper().Log("Updating the reservation status in Local DB", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                            localResponse = await new CloudHelper().UpdateReservationStatus(SessionData.OperaReservation.ReservationNameID, new Models.APIRequestModel()
                            {
                                RequestObject = new ReservationStatusRequestModel
                                {
                                    ReservationID = SessionData.OperaReservation.ReservationNameID,
                                    Type = "PreCheckOutComplete"
                                }
                            }, "Due-In push", ConfigurationManager.AppSettings
                    ["APIBaseUrl"].ToString());
                            if (!localResponse.result)
                            {
                                new LogHelper().Log("Updating the reservation status in Local DB with email send flag failed with reason :- " + localResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                            }
                            else
                                new LogHelper().Log("Updating the reservation status in Local DB ", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                            #endregion
                            localResponse = await new CloudHelper().PushReservationTrackLocally(SessionData.OperaReservation.ReservationNameID, new Models.APIRequestModel()
                            {
                                RequestObject = new Models.ReservationTrackStatus()
                                {
                                    ReservationNameID = SessionData.OperaReservation.ReservationNameID,
                                    ProcessType = Models.ReservationProcessType.PreCheckedOutFetched.ToString(),
                                    ReservationNumber = SessionData.OperaReservation.ReservationNumber,
                                    ProcessStatus = "Checkout",
                                    EmailSent = false
                                }
                            }, "pre checked-in fetch", ConfigurationManager.AppSettings
                                   ["APIBaseUrl"].ToString());
                            if (localResponse.result)
                            {
                                new LogHelper().Log("Reservation track in local DB updated successfully ", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                            }
                            else
                            {
                                new LogHelper().Log("Failed to update reservation track in local DB with reason :- " + localResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                            }

                            #endregion



                            TempData["IsredirectedfromPaymentPage"] = true;
                            TempData["IsPaymentSuccess"] = true;
                            TempData["PaymentFailureMessage"] = "";
                        }
                        else
                        {
                            Helpers.LogHelper.Instance.Log($"Payment {resposneObj.ResponseObject.ResultCode}", $"{ConfirmationNo}", ActionName, ActionGroup);
                            TempData["IsredirectedfromPaymentPage"] = true;
                            TempData["IsPaymentSuccess"] = false;
                            TempData["PaymentFailureMessage"] = resposneObj.ResponseObject.RefusalReason;
                        }
                    }
                    else
                    {
                        string FailureMessage = string.Empty;
                        if (resposneObj != null)
                        {
                            FailureMessage = resposneObj.ResponseMessage;
                        }

                        Helpers.LogHelper.Instance.Log($"Payment failed {FailureMessage}", $"{ConfirmationNo}", ActionName, ActionGroup);
                        TempData["IsredirectedfromPaymentPage"] = true;
                        TempData["IsPaymentSuccess"] = false;
                        TempData["PaymentFailureMessage"] = "Unable to complete the payment, Please try again";
                    }
                }
                else
                {
                    Helpers.LogHelper.Instance.Log($"Payment failed {response.IsSuccessStatusCode}", $"{ConfirmationNo}", "PaymentResponseFromGateway", "Pre-Checkout");
                    TempData["IsredirectedfromPaymentPage"] = true;
                    TempData["IsPaymentSuccess"] = false;
                    TempData["PaymentFailureMessage"] = "Unable to complete the payment, Please try again";
                }
            }

            string encConfirmationNo = Helpers.EncryptionHelper.EncryptString(ConfirmationNo);

            return RedirectToAction("IndexPayment", new { id = ConfirmationNo, paymentResponse = TempData["PaymentFailureMessage"] });

        }

        public async Task<ActionResult> InsertPaymentResponseHeaderOld(PaymentResponse paymentResponse, string ConfirmationNo, string ReservationNameID, string TransactionID, string TransactionType)
        {
            Helpers.LogHelper.Instance.Log($"Adding payment details to DB.", "", "Checkout/InsertPaymentResponseHeader", "Pre-Checkout");

            PaymentLogics paymentLogics = new PaymentLogics();

             reservationLogics.InsertPaymentData(paymentResponse, ConfirmationNo, ReservationNameID, TransactionID, TransactionType);




            paymentLogics.SaveTransactionHistory(new InsertPaymentHistoryUspModel()
            {
                ReservationNameID = paymentResponse.MerchantRefernce.Split('-')[1],
                // PData = paymentDetailResponseModel.r,
                PaRes = Request.Params["PaRes"],
                MDData = Request.Params["MD"],
                PSPReference = paymentResponse.PspReference,
                RefusalReason = paymentResponse.RefusalReason,
                ReservationNumber = paymentResponse.MerchantRefernce.Split('-')[0],
                ResultCode = paymentResponse.ResultCode,
                TransactionID = TransactionID,
                TransactionType = TransactionType
            });

            #region  Precheckin Completed


           // var reservationsDt = reservationLogics.GetReservationDetailsDT(ConfirmationNo);
           // var reservations = Helpers.DataTableHelper.DataTableToList<DataAccess.usp_GetReservationDetails_Result>(reservationsDt);
            var reservationsDt = await new CloudHelper().FetchReservationDetailsByReferenceNumber(ConfirmationNo, new APIRequestModel { RequestObject = ConfirmationNo }, "", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());
            var reservations = new CloudReservationModel();
            if (reservationsDt != null)
            {
                reservations = JsonConvert.DeserializeObject<List<CloudReservationModel>>(reservationsDt.responseData.ToString()).FirstOrDefault();

            }
            if (reservations != null)
            {
                Helpers.LogHelper.Instance.Log($"Updating precheckout complete flag.", "", "Checkout/InsertPaymentResponseHeader", "Pre-Checkout");

                reservationLogics.UpdateCheckoutFlag(reservations.ReservationDetailID);

                #endregion
                return Json(new { result = true });
            }
            else
            {
                return Json(new { result = false });
            }
        }
        public async Task<ActionResult> InsertPaymentResponseHeader(PaymentResponse paymentResponse, string ConfirmationNo, string ReservationNameID, string TransactionID, string TransactionType)
        {
            string ActionName = "InsertPaymentResponseHeader", ActionGroup = "Pre-Checkout";
            Helpers.LogHelper.Instance.Log($"Adding payment details to DB.", "", ActionName, ActionGroup);
            List<Models.PaymentHistory> paymentHistory = new List<Models.PaymentHistory>();
            List<Models.PaymentAdditionalInfo> paymentAdditionalInfos = new List<Models.PaymentAdditionalInfo>();
            List<Models.PaymentHeader> paymentHeaders = new List<Models.PaymentHeader>();
            List<PaymentTypeMasterModel> payments = new List<PaymentTypeMasterModel>();

            PaymentLogics paymentLogics = new PaymentLogics();

            payments = await new CloudHelper().fetchPaymentTypeMaster(ConfigurationManager.AppSettings
                     ["APIBaseUrl"].ToString(), ActionGroup);
            #region create pamentdata
            try
            {
                paymentHeaders.Add(new PaymentHeader
                {
                    MaskedCardNumber = paymentResponse.MaskCardNumber,
                    FundingSource = paymentResponse.FundingSource,
                    Amount = paymentResponse.Amount.Value.ToString("0.00"),
                    TransactionID = TransactionID,
                    ReservationNumber = SessionData.OperaReservation.ReservationNumber,
                    ReservationNameID = SessionData.OperaReservation.ReservationNameID,
                    ExpiryDate = paymentResponse.CardExpiryDate,
                    AuthorisationCode = paymentResponse.AuthCode,
                    Currency = paymentResponse.Currency,
                    RecurringIdentifier = paymentResponse.PaymentToken,
                    pspReferenceNumber = paymentResponse.PspReference,
                    ParentPspRefereceNumber = string.IsNullOrEmpty(paymentResponse.ParentPSPReferece) ? paymentResponse.PspReference : paymentResponse.ParentPSPReferece,
                    TransactionType = TransactionType,
                    ResultCode = paymentResponse.ResultCode,
                    ResponseMessage = paymentResponse.RefusalReason,
                    CardType = paymentResponse.CardType,
                    OperaPaymentTypeCode = payments.Where(x => x.VendorPaymentTypeCode.ToUpper() == paymentResponse.CardType.ToUpper()).FirstOrDefault().OperaPaymentTypeCode
                });



                DataTable dataTable = new DataTable();
                dataTable.Columns.Add("KeyHeader", typeof(string));
                dataTable.Columns.Add("KeyValue", typeof(string));


                if (paymentResponse.additionalInfos != null)
                {
                    foreach (var item in paymentResponse.additionalInfos)
                    {
                        paymentAdditionalInfos.Add(new PaymentAdditionalInfo
                        {
                            KeyHeader = item.key,
                            KeyValue = item.value
                        });

                    }
                }



            }
            catch (Exception ex)
            {
                throw ex;
            }
            #endregion



            paymentHistory.Add(new PaymentHistory
            {

                ReservationNameID = paymentResponse.MerchantRefernce.Split('-')[1],
                // PData = paymentDetailResponseModel.r,
                PaRes = Request.Params["PaRes"],
                MDData = Request.Params["MD"],
                PSPReference = paymentResponse.PspReference,
                RefusalReason = paymentResponse.RefusalReason,
                ReservationNumber = paymentResponse.MerchantRefernce.Split('-')[0],
                ResultCode = paymentResponse.ResultCode,
                TransactionID = TransactionID,
                TransactionType = TransactionType

            });
            #region update payment to opera
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
                new LogHelper().Log("Iterating the payment headers", "", ActionName, ActionGroup);
                foreach (Models.PaymentHeader paymentHeader in paymentDetails.paymentHeaders)
                {
                    if (paymentHeader.IsActive == null)
                    {
                        new LogHelper().Log("Processing the payment header with psprefernce - " + paymentHeader.pspReferenceNumber + " where IsActive falg is NULL", "", ActionName, ActionGroup);

                        #region Update Opera
                        if (paymentDetails.paymentHeaders[x].TransactionType.Equals(Models.TransactionType.PreAuth.ToString()))
                        {
                            new LogHelper().Log("Processing the payment header with psprefernce - " + paymentHeader.pspReferenceNumber + " as a pre-auth transaction", "", ActionName, ActionGroup);

                            paymentDetails.paymentHeaders[x].IsActive = true;

                            #region Updating Card details in opera Reservation
                            new LogHelper().Log("Updating credit card details in the reservation", "", ActionName, ActionGroup);

                            Models.OWS.OwsResponseModel owsResponse = await new CloudHelper().UpdateCardDetailsInReservationAsyn("", new Models.OWS.OwsRequestModel()
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
                                    ReservationNumber = SessionData.OperaReservation.ReservationNumber,
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
                            }, "pre checked-in fetch", ConfigurationManager.AppSettings
["APIBaseUrl"].ToString());
                            if (!owsResponse.result)
                            {
                                new LogHelper().Log("Updating credit card details in the reservation failed with reason :- " + owsResponse.responseMessage, "", ActionName, ActionGroup);
                                new LogHelper().Warn("Updating credit card details in the reservation failed with reason :- " + owsResponse.responseMessage, "", ActionName, ActionGroup);
                            }
                            else
                            {
                                new LogHelper().Log("Updating credit card details in the reservation succeeded", "", ActionName, ActionGroup);
                                new LogHelper().Warn("Updating credit card details in the reservation succeeded", "", ActionName, ActionGroup);
                            }
                            #endregion

                            #region Updating UDF fields in Opera reservation
                            try
                            {
                                new LogHelper().Log("Updating pre auth code and amount in UDF fileds", "", ActionName, ActionGroup);
                                owsResponse = await new CloudHelper().ModifyBooking(SessionData.OperaReservation.ReservationNameID, new Models.OWS.OwsRequestModel()
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
                                        ReservationNumber = SessionData.OperaReservation.ReservationNumber,
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
                                }, "pre checked-in fetch", ConfigurationManager.AppSettings
["APIBaseUrl"].ToString()
);
                                if (!owsResponse.result)
                                {
                                    new LogHelper().Log("Updating pre auth code and amount in UDF fileds failed with reason : - " + owsResponse.responseMessage, "", ActionName, ActionGroup);
                                    new LogHelper().Warn("Updating pre auth code and amount in UDF fileds failed with reason : - " + owsResponse.responseMessage, "", ActionName, ActionGroup);
                                }
                                else
                                    new LogHelper().Log("Updating pre auth code and amount in UDF fileds succeeded ", "", ActionName, ActionGroup);
                            }
                            catch (Exception ex)
                            {
                                new LogHelper().Error(ex, "", ActionName, ActionGroup);
                            }
                            #endregion

                        }
                        else if (paymentDetails.paymentHeaders[x].TransactionType.Equals(Models.TransactionType.Sale.ToString()))
                        {
                            paymentDetails.paymentHeaders[x].IsActive = false;

                            #region Updating Card details in opera Reservation
                            new LogHelper().Log("Updating credit card details in the reservation", "", ActionName, ActionGroup);

                            Models.OWS.OwsResponseModel owsResponse = await new CloudHelper().UpdateCardDetailsInReservationAsyn(SessionData.OperaReservation.ReservationNameID, new Models.OWS.OwsRequestModel()
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
                                    ReservationNumber = SessionData.OperaReservation.ReservationNumber,
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
                            }, "pre checked-in fetch", ConfigurationManager.AppSettings
["APIBaseUrl"].ToString());
                            if (!owsResponse.result)
                            {
                                new LogHelper().Log("Updating credit card details in the reservation failed with reason :- " + owsResponse.responseMessage, "", ActionName, ActionGroup);
                                new LogHelper().Warn("Updating credit card details in the reservation failed with reason :- " + owsResponse.responseMessage, "", ActionName, ActionGroup);
                            }
                            else
                            {
                                new LogHelper().Log("Updating credit card details in the reservation succeeded", "", ActionName, ActionGroup);
                                new LogHelper().Warn("Updating credit card details in the reservation succeeded", "", ActionName, ActionGroup);
                            }
                            #endregion

                            #region Updating UDF fields in Opera reservation
                            try
                            {
                                new LogHelper().Log("Updating pre auth code and amount in UDF fileds", "", ActionName, ActionGroup);
                                owsResponse = await new CloudHelper().ModifyBooking(SessionData.OperaReservation.ReservationNameID, new Models.OWS.OwsRequestModel()
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
                                        ReservationNumber = SessionData.OperaReservation.ReservationNumber,
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
                                }, "pre checked-in fetch", ConfigurationManager.AppSettings
["APIBaseUrl"].ToString());
                                if (!owsResponse.result)
                                {
                                    new LogHelper().Log("Updating pre auth code and amount in UDF fileds failed with reason : - " + owsResponse.responseMessage, "", ActionName, ActionGroup);
                                    new LogHelper().Warn("Updating pre auth code and amount in UDF fileds failed with reason : - " + owsResponse.responseMessage, "", ActionName, ActionGroup);
                                }
                                else
                                    new LogHelper().Log("Updating pre auth code and amount in UDF fileds succeeded ", "", ActionName, ActionGroup);
                            }
                            catch (Exception ex)
                            {
                                new LogHelper().Error(ex, "", ActionName, ActionGroup);
                            }
                            #endregion

                            #region Posting payment in opera reservation
                            try
                            {
                                new LogHelper().Log("Posting payment in the reservation", "", ActionName, ActionGroup);
                                owsResponse = await new CloudHelper().MakePayment(SessionData.OperaReservation.ReservationNameID, new Models.OWS.OwsRequestModel()
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
                                        ReservationNameID = SessionData.OperaReservation.ReservationNameID,
                                        MaskedCardNumber = paymentHeader.MaskedCardNumber.ToLower(),
                                        PaymentRefernce = "Payment from web checkin - Sale",
                                        PaymentTypeCode = paymentHeader.OperaPaymentTypeCode,
                                        ApprovalCode = paymentHeader.pspReferenceNumber
                                    }
                                }, "pre checked-in fetch", ConfigurationManager.AppSettings
["APIBaseUrl"].ToString());
                                if (!owsResponse.result)
                                {
                                    new LogHelper().Log("Posting payment failed with reason : - " + owsResponse.responseMessage, "", ActionName, ActionGroup);
                                    new LogHelper().Warn("Posting payment failed with reason : - " + owsResponse.responseMessage, "", ActionName, ActionGroup);
                                }
                                else
                                    new LogHelper().Log("Posting payment in opera reservation succeeded ", "", ActionName, ActionGroup);
                            }
                            catch (Exception ex)
                            {
                                new LogHelper().Error(ex, "", ActionName, ActionGroup);
                            }
                            #endregion
                        }
                        else if (paymentDetails.paymentHeaders[x].TransactionType.Equals(Models.TransactionType.Capture.ToString()))
                        {
                            new LogHelper().Log("Wrong payment header retuned and Is active NULL (Capture)", "", ActionName, ActionGroup);
                            paymentDetails.paymentHeaders[x].IsActive = false;
                        }

                        #endregion
                    }
                    x++;
                }
                new LogHelper().Log("payment details updated successfully", "", ActionName, ActionGroup);

            }
            ////jkj
            #endregion

            #region Pushing Payment details in LOcal Db

            new LogHelper().Log("Updating payment details in local DB", "", ActionName, ActionGroup);
            var localResponse = await new CloudHelper().PushPaymentDetails(SessionData.OperaReservation.ReservationNameID, new Models.APIRequestModel()
            {
                RequestObject = paymentDetails
            }, "pre checked-in fetch", ConfigurationManager.AppSettings
      ["APIBaseUrl"].ToString());
            if (!localResponse.result)
            {
                new LogHelper().Log("Failed to update payment details in Local DB with reason :- " + localResponse.responseMessage, "", ActionName, ActionGroup);
                new LogHelper().Warn("Failed to update payment details in local DB with reason :- " + localResponse.responseMessage, "", ActionName, ActionGroup);
            }
            else
                new LogHelper().Log("Payment details updated successfully", "", ActionName, ActionGroup);
            #endregion
            #endregion
            #region Pushing Reservation Track

            new LogHelper().Log("Pushing reservation track in local DB ", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
            #region Updating record status in Local DB
            new LogHelper().Log("Updating the reservation status in Local DB", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
            localResponse = await new CloudHelper().UpdateReservationStatus(SessionData.OperaReservation.ReservationNameID, new Models.APIRequestModel()
            {
                RequestObject = new ReservationStatusRequestModel
                {
                    ReservationID = SessionData.OperaReservation.ReservationNameID,
                    Type = "PreCheckOutComplete"
                }
            }, "Due-In push", ConfigurationManager.AppSettings
    ["APIBaseUrl"].ToString());
            if (!localResponse.result)
            {
                new LogHelper().Log("Updating the reservation status in Local DB with email send flag failed with reason :- " + localResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
            }
            else
                new LogHelper().Log("Updating the reservation status in Local DB ", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
            #endregion
            localResponse = await new CloudHelper().PushReservationTrackLocally(SessionData.OperaReservation.ReservationNameID, new Models.APIRequestModel()
            {
                RequestObject = new Models.ReservationTrackStatus()
                {
                    ReservationNameID = SessionData.OperaReservation.ReservationNameID,
                    ProcessType = Models.ReservationProcessType.PreCheckedOutFetched.ToString(),
                    ReservationNumber = SessionData.OperaReservation.ReservationNumber,
                    ProcessStatus = "Checkout",
                    EmailSent = false
                }
            }, "pre checked-in fetch", ConfigurationManager.AppSettings
                   ["APIBaseUrl"].ToString());
            if (localResponse.result)
            {
                new LogHelper().Log("Reservation track in local DB updated successfully ", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
            }
            else
            {
                new LogHelper().Log("Failed to update reservation track in local DB with reason :- " + localResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
            }

            #endregion
            return Json(new { result = true });
        }
        [HttpPost]
        public async Task<ActionResult> SaveAdyenPaymentDetails(MakePaymentDetailModel makePaymentDetailModel, string ConfirmationNo, long TransactionID, string ReservationNameID, string TransactionType)
        {
            string ActionName = "SaveAdyenPaymentDetails", ActionGroup = "Pre-Checkout";

            Helpers.LogHelper.Instance.Log($"Adding payment details to DB.", "", ActionName, ActionGroup);

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

            new LogHelper().Log("Updating payment details in local DB", "", ActionName, ActionGroup);
            var paymentDetails = new Models.UpdatePaymentDetails()
            {
                paymentHistories = paymentHistory
            };
            var localResponse = await new CloudHelper().PushPaymentDetails(SessionData.OperaReservation.ReservationNameID, new Models.APIRequestModel()
            {
                RequestObject = paymentDetails
            }, "pre checked-in fetch", ConfigurationManager.AppSettings
      ["APIBaseUrl"].ToString());
            if (!localResponse.result)
            {
                new LogHelper().Log("Failed to update payment details in Local DB with reason :- " + localResponse.responseMessage, "", ActionName, ActionGroup);
                new LogHelper().Warn("Failed to update payment details in local DB with reason :- " + localResponse.responseMessage, "", ActionName, ActionGroup);
            }
            else
                new LogHelper().Log("Payment details updated successfully", "", ActionName, ActionGroup);
            #endregion
            return Json(new { result = true });
        }




        public async Task<ActionResult> processExistingTransction(ProcessExistingTransction model)
        {
            string ActionName = "processExistingTransction", ActionGroup = "Pre-Checkout";
            Helpers.LogHelper.Instance.Log($"Processing existing transactions.", "", "Checkout/processExistingTransction", "Pre-Checkout");
            List<Models.PaymentHeader> paymentHeaders = null;
            List<Models.PaymentHeader> paymentHeader = null;
            //var reservationsDt = reservationLogics.GetReservationDetailsDT(model.ReservationNo);
            List<Models.PaymentHistory> paymentHistory = new List<Models.PaymentHistory>();
            List<Models.PaymentAdditionalInfo> paymentAdditionalInfos = new List<Models.PaymentAdditionalInfo>();
            //var reservations = Helpers.DataTableHelper.DataTableToList<Models.GetReservationDetailsModel>(reservationsDt);
            var reservationsDt = await new CloudHelper().FetchReservationDetailsByReferenceNumber(model.ReservationNo, new APIRequestModel { RequestObject = model.ReservationNo }, "", ConfigurationManager.AppSettings["APIBaseUrl"].ToString());
            var reservations = new CloudReservationModel();
            if (reservationsDt != null)
            {
                reservations = JsonConvert.DeserializeObject<List<CloudReservationModel>>(reservationsDt.responseData.ToString()).FirstOrDefault();

            }
            
                #region Check Payment in Saavy
                //Models.Local.LocalResponseModel localResponse = null;
                new LogHelper().Log("Fetching payment details for reservation No. : " + SessionData.OperaReservation.ReservationNumber + " in Saavy Pay", SessionData.OperaReservation.ReservationNameID, "PushDueOutReservation", "Due-Out push");
            APIResponseModel localResponse = await new CloudHelper().FetchPaymentDetails(SessionData.OperaReservation.ReservationNameID, new Models.APIRequestModel()
            {
                RequestObject = new Models.DueOut.FetchPaymentRequest()
                {
                    ReservationNameID = SessionData.OperaReservation.ReservationNameID,
                    isActive = true
                }
            }, "Due-Out push", ConfigurationManager.AppSettings
                ["APIBaseUrl"].ToString());

            if (!localResponse.result || localResponse.responseData == null)
            {
                new LogHelper().Log("Failed to fetch payment details with reason :- " + localResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                new LogHelper().Warn("Failed to fetch payment details with reason :- " + localResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);

            }
            else

            {
                new LogHelper().Debug("Converting API json to object", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                try
                {
                    paymentHeaders = JsonConvert.DeserializeObject<List<Models.PaymentHeader>>(localResponse.responseData.ToString());
                    new LogHelper().Log("Payment details fetched successfully", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                }
                catch (Exception ex)
                {
                    new LogHelper().Error(ex, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                    new LogHelper().Log("Failed to covert API response to object", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                    new LogHelper().Warn("Failed to fetch payment details with reason :- " + ex.Message, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                    new LogHelper().Debug("Failed to fetch payment details with reason :- " + ex.Message, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                }
            }

            #endregion
            var activeTransactions = paymentHeaders != null && paymentHeaders.Count() > 0 ? paymentHeaders.Where(x => x.IsActive == true) : new List<PaymentHeader>(); //List *//*only Active pre-auth

            activeTransactions = activeTransactions.ToList();
            PaymentLogics paymentLogics = new PaymentLogics();
            decimal preAuthAmount = 0;
            string ParentPSPReference = string.Empty;
            string AdjustAuthorisationData = string.Empty;
            foreach (var preauth in activeTransactions)//for now only one transaction
            {
                if (preauth.IsActive.Value)
                {
                    preAuthAmount += Convert.ToDecimal(preauth.Amount);
                    ParentPSPReference = preauth.ParentPspRefereceNumber;
                    AdjustAuthorisationData = preauth.AuthorisationCode;
                }
            }


            if (reservations.BalanceAmount <= preAuthAmount)
            {
                Helpers.LogHelper.Instance.Log($"Balance < Preauth .", "", "Checkout/processExistingTransction", "Pre-Checkout");
                Helpers.LogHelper.Instance.Log($"Capturing preauth.", "", "Checkout/processExistingTransction", "Pre-Checkout");
                //Directly Capture
                var captureResponse = await paymentLogics.CaptureTransaction(new TopupTransctionModels()
                {
                    AmountToCharge = reservations.BalanceAmount.Value,
                    PspReferenceNumber = ParentPSPReference
                });

                if (captureResponse != null && captureResponse.Result)
                {
                    Helpers.LogHelper.Instance.Log($"Preauth capture success.", "", "Checkout/processExistingTransction", "Pre-Checkout");

                    string transactionType = Models.TransactionType.PreAuth.ToString();
                    //var updatecaptureTransctionToDB = reservationLogics.UpdatePaymentHeaderData(activeTransactions.FirstOrDefault().TransactionID.ToString(), transactionType, "Modified", false, transactionType, reservations[0].BalanceAmount.Value);

                    string TransactionID = DateTime.Now.ToString("yyMMddHHss");
                    transactionType = Models.TransactionType.Capture.ToString();
                    ///Region update payment
                    #region update payment in opera
                    #region Posting payment in opera reservation
                    try
                    {
                        new LogHelper().Log("Posting payment in the reservation", "", ActionName, ActionGroup);
                        var owsResponse = await new CloudHelper().MakePayment(SessionData.OperaReservation.ReservationNameID, new Models.OWS.OwsRequestModel()
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
                                Amount = Convert.ToDecimal(reservations.BalanceAmount.Value),
                                PaymentInfo = "Payment from web checkin",
                                StationID = "MCI",
                                WindowNumber = 1,
                                ReservationNameID = SessionData.OperaReservation.ReservationNameID,
                                MaskedCardNumber = activeTransactions.FirstOrDefault().MaskedCardNumber.ToLower(),
                                PaymentRefernce = "Payment from web checkin - Sale",
                                PaymentTypeCode = activeTransactions.FirstOrDefault().OperaPaymentTypeCode,
                                ApprovalCode = activeTransactions.FirstOrDefault().pspReferenceNumber
                            }
                        }, "pre checked-in fetch", ConfigurationManager.AppSettings
  ["APIBaseUrl"].ToString());
                        if (!owsResponse.result)
                        {
                            new LogHelper().Log("Posting payment failed with reason : - " + owsResponse.responseMessage, "", ActionName, ActionGroup);
                            new LogHelper().Warn("Posting payment failed with reason : - " + owsResponse.responseMessage, "", ActionName, ActionGroup);
                        }
                        else
                            new LogHelper().Log("Posting payment in opera reservation succeeded ", "", ActionName, ActionGroup);
                    }
                    catch (Exception ex)
                    {
                        new LogHelper().Error(ex, "", ActionName, ActionGroup);
                    }
                    #endregion


                    #endregion
                    #region create pamentdata
                    try
                    {
                        paymentHeader.Add(new PaymentHeader
                        {
                            MaskedCardNumber = activeTransactions.FirstOrDefault().MaskedCardNumber,
                            FundingSource = activeTransactions.FirstOrDefault().FundingSource,
                            Amount = activeTransactions.FirstOrDefault().Amount,
                            TransactionID = TransactionID,
                            ReservationNumber = SessionData.OperaReservation.ReservationNumber,
                            ReservationNameID = SessionData.OperaReservation.ReservationNameID,
                            ExpiryDate = activeTransactions.FirstOrDefault().ExpiryDate,
                            AuthorisationCode = activeTransactions.FirstOrDefault().AuthorisationCode,
                            Currency = activeTransactions.FirstOrDefault().Currency,
                            RecurringIdentifier = activeTransactions.FirstOrDefault().RecurringIdentifier,
                            pspReferenceNumber = captureResponse.ResponseObject.PspReference,
                            ParentPspRefereceNumber = string.IsNullOrEmpty(activeTransactions.FirstOrDefault().ParentPspRefereceNumber) ? activeTransactions.FirstOrDefault().ParentPspRefereceNumber : captureResponse.ResponseObject.ParentPSPReferece,
                            TransactionType = transactionType,
                            ResultCode = "Capture",
                            ResponseMessage = "",
                            CardType = activeTransactions.FirstOrDefault().CardType,
                            OperaPaymentTypeCode = activeTransactions.FirstOrDefault().OperaPaymentTypeCode
                        });



                        DataTable dataTable = new DataTable();
                        dataTable.Columns.Add("KeyHeader", typeof(string));
                        dataTable.Columns.Add("KeyValue", typeof(string));


                        if (captureResponse.ResponseObject.additionalInfos != null)
                        {
                            foreach (var item in captureResponse.ResponseObject.additionalInfos)
                            {
                                paymentAdditionalInfos.Add(new PaymentAdditionalInfo
                                {
                                    KeyHeader = item.key,
                                    KeyValue = item.value
                                });

                            }
                        }



                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                    #endregion
                    var paymentDetails = new Models.UpdatePaymentDetails()
                    {
                        paymentHeaders = paymentHeaders,
                        paymentAdditionalInfos = paymentAdditionalInfos,
                        paymentHistories = paymentHistory
                    };
                    #region Pushing Payment details in LOcal Db

                    new LogHelper().Log("Updating payment details in local DB", "", ActionName, ActionGroup);
                    localResponse = await new CloudHelper().PushPaymentDetails(SessionData.OperaReservation.ReservationNameID, new Models.APIRequestModel()
                    {
                        RequestObject = paymentDetails
                    }, "pre checked-in fetch", ConfigurationManager.AppSettings
             ["APIBaseUrl"].ToString());
                    if (!localResponse.result)
                    {
                        new LogHelper().Log("Failed to update payment details in Local DB with reason :- " + localResponse.responseMessage, "", ActionName, ActionGroup);
                        new LogHelper().Warn("Failed to update payment details in local DB with reason :- " + localResponse.responseMessage, "", ActionName, ActionGroup);
                    }
                    else
                        new LogHelper().Log("Payment details updated successfully", "", ActionName, ActionGroup);
                    #endregion




                    if (localResponse.result)
                    {
                        #region Pushing Reservation Track
                        new LogHelper().Log("Pushing reservation track in local DB ", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                        #region Updating record status in Local DB
                        new LogHelper().Log("Updating the reservation status in Local DB", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                        localResponse = await new CloudHelper().UpdateReservationStatus(SessionData.OperaReservation.ReservationNameID, new Models.APIRequestModel()
                        {
                            RequestObject = new ReservationStatusRequestModel
                            {
                                ReservationID = SessionData.OperaReservation.ReservationNameID,
                                Type = "PreCheckOutComplete"
                            }
                        }, "Due-In push", ConfigurationManager.AppSettings
               ["APIBaseUrl"].ToString());
                        if (!localResponse.result)
                        {
                            new LogHelper().Log("Updating the reservation status in Local DB with email send flag failed with reason :- " + localResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                        }
                        else
                            new LogHelper().Log("Updating the reservation status in Local DB ", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                        #endregion
                        localResponse = await new CloudHelper().PushReservationTrackLocally(SessionData.OperaReservation.ReservationNameID, new Models.APIRequestModel()
                        {
                            RequestObject = new Models.ReservationTrackStatus()
                            {
                                ReservationNameID = SessionData.OperaReservation.ReservationNameID,
                                ProcessType = Models.ReservationProcessType.PreCheckedOutFetched.ToString(),
                                ReservationNumber = SessionData.OperaReservation.ReservationNumber,
                                ProcessStatus = "Checkout",
                                EmailSent = false
                            }
                        }, "pre checked-in fetch", ConfigurationManager.AppSettings
                               ["APIBaseUrl"].ToString());
                        if (localResponse.result)
                        {
                            new LogHelper().Log("Reservation track in local DB updated successfully ", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                        }
                        else
                        {
                            new LogHelper().Log("Failed to update reservation track in local DB with reason :- " + localResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                        }

                        #endregion());
                        return Json(new { result = true, message = "success" });
                    }
                    else
                    {
                        //return failure
                        Helpers.LogHelper.Instance.Warn($"Unable to save payment details to DB.", "", "Checkout/processExistingTransction", "Pre-Checkout");
                        return Json(new { result = false, message = "Unable to process payment" });
                    }
                }
                else
                {
                    Helpers.LogHelper.Instance.Warn($"Unable to capture pre-auth", "", "Checkout/processExistingTransction", "Pre-Checkout");
                    //return failure
                    return Json(new { result = false, message = "Unable to process payment" });
                }
            }
            else
            {
                Helpers.LogHelper.Instance.Log($"Balance > Preauth .", "", "Checkout/processExistingTransction", "Pre-Checkout");
                //Topup balance amount
                decimal AmountToTopup = reservations.BalanceAmount.Value;

                var topUpResponse = await paymentLogics.TopupTransaction(new TopupTransctionModels()
                {
                    AmountToCharge = AmountToTopup,
                    PspReferenceNumber = ParentPSPReference,
                    AdjustAuthorisationData = AdjustAuthorisationData
                });

                if (topUpResponse != null && topUpResponse.Result)
                {
                    Helpers.LogHelper.Instance.Log($"Transaction topup successfully for {AmountToTopup}", "", "Checkout/processExistingTransction", "Pre-Checkout");


                    //Save the transaction to DB

                    ReservationLogics reservationLogics = new ReservationLogics();

                    PaymentResponse paymentResponse = new PaymentResponse()
                    {
                        additionalInfos = new System.Collections.Generic.List<AdditionalInfo>(),
                        Amount = Convert.ToDecimal(activeTransactions.FirstOrDefault().Amount),
                        PspReference = topUpResponse.ResponseObject.PspReference,
                        ParentPSPReferece = ParentPSPReference,
                        AuthCode = activeTransactions.FirstOrDefault().AuthorisationCode,
                        CardExpiryDate = activeTransactions.FirstOrDefault().ExpiryDate,
                        CardToken = activeTransactions.FirstOrDefault().RecurringIdentifier,
                        CardType = activeTransactions.FirstOrDefault().CardType,
                        Currency = activeTransactions.FirstOrDefault().Currency,
                        FundingSource = activeTransactions.FirstOrDefault().FundingSource,
                        MaskCardNumber = activeTransactions.FirstOrDefault().MaskedCardNumber,
                        MerchantRefernce = activeTransactions.FirstOrDefault().ReservationNumber + "-" + activeTransactions.FirstOrDefault().ReservationNameID,
                        PaymentToken = activeTransactions.FirstOrDefault().RecurringIdentifier,
                        RefusalReason = "",
                        ResultCode = "Completed"
                    };

                    //call update sp insted

                    string transctionType = activeTransactions.FirstOrDefault().TransactionType;

                    //var updateTransctionToDB = reservationLogics.UpdatePaymentHeaderData(activeTransactions.FirstOrDefault().TransactionID.ToString(), "Modified", "Transction modified", false, transctionType, Convert.ToDecimal(activeTransactions.FirstOrDefault().Amount));

                    paymentResponse.PspReference = topUpResponse.ResponseObject.PspReference;
                    paymentResponse.Amount = AmountToTopup;
                    paymentResponse.ResultCode = "Modification";

                    string TransactionID = DateTime.Now.ToString("yyMMddHHss");
                    string transactionType = Models.TransactionType.PreAuth.ToString();

                    //var saveTopupTransctionToDB = await reservationLogics.InsertPaymentData(paymentResponse, reservations[0].ReservationNumber, reservations[0].ReservationNameID, TransactionID, transactionType);

                    if (true)
                    {
                        var captureResponse = await paymentLogics.CaptureTransaction(new TopupTransctionModels()
                        {
                            AmountToCharge = reservations.BalanceAmount.Value,
                            PspReferenceNumber = ParentPSPReference
                        });

                        if (captureResponse != null && captureResponse.Result)
                        {
                            Helpers.LogHelper.Instance.Log($"Transaction captured successfully for {reservations.BalanceAmount.Value}", "", "Checkout/processExistingTransction", "Pre-Checkout");

                            transctionType = Models.TransactionType.Capture.ToString();
                            //var updatecaptureTransctionToDB = reservationLogics.UpdatePaymentHeaderData(TransactionID, transctionType, "Captured", null, transctionType, reservations[0].BalanceAmount.Value);
                            #region update payment in opera
                            #region Posting payment in opera reservation
                            try
                            {
                                new LogHelper().Log("Posting payment in the reservation", "", ActionName, ActionGroup);
                                var owsResponse = await new CloudHelper().MakePayment(SessionData.OperaReservation.ReservationNameID, new Models.OWS.OwsRequestModel()
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
                                        Amount = Convert.ToDecimal(reservations.BalanceAmount.Value),
                                        PaymentInfo = "Payment from web checkin",
                                        StationID = "MCI",
                                        WindowNumber = 1,
                                        ReservationNameID = SessionData.OperaReservation.ReservationNameID,
                                        MaskedCardNumber = activeTransactions.FirstOrDefault().MaskedCardNumber.ToLower(),
                                        PaymentRefernce = "Payment from web checkin - Sale",
                                        PaymentTypeCode = activeTransactions.FirstOrDefault().OperaPaymentTypeCode,
                                        ApprovalCode = activeTransactions.FirstOrDefault().pspReferenceNumber
                                    }
                                }, "pre checked-in fetch", ConfigurationManager.AppSettings
          ["APIBaseUrl"].ToString());
                                if (!owsResponse.result)
                                {
                                    new LogHelper().Log("Posting payment failed with reason : - " + owsResponse.responseMessage, "", ActionName, ActionGroup);
                                    new LogHelper().Warn("Posting payment failed with reason : - " + owsResponse.responseMessage, "", ActionName, ActionGroup);
                                }
                                else
                                    new LogHelper().Log("Posting payment in opera reservation succeeded ", "", ActionName, ActionGroup);
                            }
                            catch (Exception ex)
                            {
                                new LogHelper().Error(ex, "", ActionName, ActionGroup);
                            }
                            #endregion


                            #endregion
                            #region create pamentdata
                            try
                            {
                                paymentHeader.Add(new PaymentHeader
                                {
                                    MaskedCardNumber = activeTransactions.FirstOrDefault().MaskedCardNumber,
                                    FundingSource = activeTransactions.FirstOrDefault().FundingSource,
                                    Amount = activeTransactions.FirstOrDefault().Amount,
                                    TransactionID = TransactionID,
                                    ReservationNumber = SessionData.OperaReservation.ReservationNumber,
                                    ReservationNameID = SessionData.OperaReservation.ReservationNameID,
                                    ExpiryDate = activeTransactions.FirstOrDefault().ExpiryDate,
                                    AuthorisationCode = activeTransactions.FirstOrDefault().AuthorisationCode,
                                    Currency = activeTransactions.FirstOrDefault().Currency,
                                    RecurringIdentifier = activeTransactions.FirstOrDefault().RecurringIdentifier,
                                    pspReferenceNumber = captureResponse.ResponseObject.PspReference,
                                    ParentPspRefereceNumber = string.IsNullOrEmpty(activeTransactions.FirstOrDefault().ParentPspRefereceNumber) ? activeTransactions.FirstOrDefault().ParentPspRefereceNumber : captureResponse.ResponseObject.ParentPSPReferece,
                                    TransactionType = transactionType,
                                    ResultCode = "Capture",
                                    ResponseMessage = "",
                                    CardType = activeTransactions.FirstOrDefault().CardType,
                                    OperaPaymentTypeCode = activeTransactions.FirstOrDefault().OperaPaymentTypeCode
                                });



                                DataTable dataTable = new DataTable();
                                dataTable.Columns.Add("KeyHeader", typeof(string));
                                dataTable.Columns.Add("KeyValue", typeof(string));


                                if (captureResponse.ResponseObject.additionalInfos != null)
                                {
                                    foreach (var item in captureResponse.ResponseObject.additionalInfos)
                                    {
                                        paymentAdditionalInfos.Add(new PaymentAdditionalInfo
                                        {
                                            KeyHeader = item.key,
                                            KeyValue = item.value
                                        });

                                    }
                                }



                            }
                            catch (Exception ex)
                            {
                                throw ex;
                            }
                            #endregion
                            var paymentDetails = new Models.UpdatePaymentDetails()
                            {
                                paymentHeaders = paymentHeaders,
                                paymentAdditionalInfos = paymentAdditionalInfos,
                                paymentHistories = paymentHistory
                            };
                            #region Pushing Payment details in LOcal Db

                            new LogHelper().Log("Updating payment details in local DB", "", ActionName, ActionGroup);
                            localResponse = await new CloudHelper().PushPaymentDetails(SessionData.OperaReservation.ReservationNameID, new Models.APIRequestModel()
                            {
                                RequestObject = paymentDetails
                            }, "pre checked-in fetch", ConfigurationManager.AppSettings
                     ["APIBaseUrl"].ToString());
                            if (!localResponse.result)
                            {
                                new LogHelper().Log("Failed to update payment details in Local DB with reason :- " + localResponse.responseMessage, "", ActionName, ActionGroup);
                                new LogHelper().Warn("Failed to update payment details in local DB with reason :- " + localResponse.responseMessage, "", ActionName, ActionGroup);
                            }
                            else
                                new LogHelper().Log("Payment details updated successfully", "", ActionName, ActionGroup);
                            #endregion

                            //Pass null for isactive flag to mark as active transaction
                            // Update trasaction to captured
                            //return success
                            if (localResponse.result)
                            {

                                #region Pushing Reservation Track

                                new LogHelper().Log("Pushing reservation track in local DB ", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                                #region Updating record status in Local DB
                                new LogHelper().Log("Updating the reservation status in Local DB", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                                localResponse = await new CloudHelper().UpdateReservationStatus(SessionData.OperaReservation.ReservationNameID, new Models.APIRequestModel()
                                {
                                    RequestObject = new ReservationStatusRequestModel
                                    {
                                        ReservationID = SessionData.OperaReservation.ReservationNameID,
                                        Type = "PreCheckOutComplete"
                                    }
                                }, "Due-In push", ConfigurationManager.AppSettings
                        ["APIBaseUrl"].ToString());
                                if (!localResponse.result)
                                {
                                    new LogHelper().Log("Updating the reservation status in Local DB with email send flag failed with reason :- " + localResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                                }
                                else
                                    new LogHelper().Log("Updating the reservation status in Local DB ", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                                #endregion
                                localResponse = await new CloudHelper().PushReservationTrackLocally(SessionData.OperaReservation.ReservationNameID, new Models.APIRequestModel()
                                {
                                    RequestObject = new Models.ReservationTrackStatus()
                                    {
                                        ReservationNameID = SessionData.OperaReservation.ReservationNameID,
                                        ProcessType = Models.ReservationProcessType.PreCheckedOutFetched.ToString(),
                                        ReservationNumber = SessionData.OperaReservation.ReservationNumber,
                                        ProcessStatus = "Checkout",
                                        EmailSent = false
                                    }
                                }, "pre checked-in fetch", ConfigurationManager.AppSettings
                                       ["APIBaseUrl"].ToString());
                                if (localResponse.result)
                                {
                                    new LogHelper().Log("Reservation track in local DB updated successfully ", SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                                }
                                else
                                {
                                    new LogHelper().Log("Failed to update reservation track in local DB with reason :- " + localResponse.responseMessage, SessionData.OperaReservation.ReservationNameID, ActionName, ActionGroup);
                                }

                                #endregion

                            }
                            return Json(new { result = true, message = "success" });
                        }
                        else
                        {
                            Helpers.LogHelper.Instance.Warn($"Unable to captured transaction", "", "Checkout/processExistingTransction", "Pre-Checkout");

                            //return failure
                            return Json(new { result = false, message = "Unable to process payment" });
                        }
                    }
                    else
                    {
                        Helpers.LogHelper.Instance.Warn($"Unable to save topup details to DB", "", "Checkout/processExistingTransction", "Pre-Checkout");

                        //return failure
                        return Json(new { result = false, message = "Unable to process payment" });
                    }
                }
                else
                {
                    Helpers.LogHelper.Instance.Warn($"Unable to topup transacation", "", "Checkout/processExistingTransction", "Pre-Checkout");

                    //return failure
                    return Json(new { result = false, message = "Unable to process payment" });
                }

            }

        }

        /// <summary>
        /// Deserializes FolioModel from Cloud API responseData (JObject, FolioModel, or JSON string).
        /// </summary>
        private static Models.OWS.FolioModel DeserializeFolioModel(object responseData)
        {
            if (responseData == null)
                return null;
            var asModel = responseData as Models.OWS.FolioModel;
            if (asModel != null)
                return asModel;
            var asToken = responseData as JToken;
            if (asToken != null)
                return asToken.ToObject<Models.OWS.FolioModel>();
            return JsonConvert.DeserializeObject<Models.OWS.FolioModel>(responseData.ToString());
        }

        /// <summary>
        /// Precheckout folio rules (QA):
        /// - Evaluate ALL primary-guest folio windows (ProfileID-matched FolioWindows from Opera).
        /// - Block if any guest window balance != 0 (outstanding or negative).
        /// - Do NOT block solely because company/TA (ReservationBalance beyond guest) has outstanding;
        ///   log a clear trace so FO can settle company folio manually.
        /// Returns true when precheckout may proceed; guestBalanceForUi is what Agree/IsPaymentDisabled UI uses.
        /// </summary>
        private static bool EvaluatePrimaryGuestFoliosForPreCheckout(
            Models.OWS.FolioModel folio,
            string reservationNameId,
            string actionName,
            string actionGroup,
            out decimal guestBalanceForUi,
            out string blockReason)
        {
            guestBalanceForUi = 0m;
            blockReason = null;
            var log = new LogHelper();

            if (folio == null)
            {
                blockReason = "Folio model is null";
                return false;
            }

            decimal companyOrTaBalance = folio.ReservationBalance - folio.BalanceAmount;
            if (folio.FolioWindows != null && folio.FolioWindows.Count > 0)
            {
                decimal windowsSum = 0m;
                Models.OWS.FolioWindow blockingWindow = null;
                log.Log(
                    "Evaluating " + folio.FolioWindows.Count + " primary-guest folio window(s) for precheckout",
                    reservationNameId, actionName, actionGroup);

                foreach (var window in folio.FolioWindows)
                {
                    windowsSum += window.BalanceAmount;
                    log.Log(
                        "Primary guest folio window " + (window.WindowNumber.HasValue ? window.WindowNumber.Value.ToString() : "?")
                        + " balance=" + window.BalanceAmount,
                        reservationNameId, actionName, actionGroup);

                    // Any non-zero window blocks — do not stop after the first clear window
                    if (blockingWindow == null && Math.Abs(window.BalanceAmount) > 0.0001m)
                        blockingWindow = window;
                }

                // Prefer per-window sum for display; if windows net to 0 but any single window != 0, surface that amount so Agree stays blocked
                guestBalanceForUi = windowsSum;
                if (blockingWindow != null && Math.Abs(guestBalanceForUi) <= 0.0001m)
                    guestBalanceForUi = blockingWindow.BalanceAmount;

                companyOrTaBalance = folio.ReservationBalance - windowsSum;
                if (Math.Abs(companyOrTaBalance) > 0.0001m)
                {
                    log.Log(
                        "Company/travel-agent folio still has balance " + companyOrTaBalance
                        + " (reservationBalance=" + folio.ReservationBalance + ", guestWindowsSum=" + windowsSum
                        + "). Precheckout is allowed when all guest windows are 0; FO will checkout company/TA manually.",
                        reservationNameId, actionName, actionGroup);
                }

                if (blockingWindow != null)
                {
                    string winLabel = blockingWindow.WindowNumber.HasValue
                        ? blockingWindow.WindowNumber.Value.ToString()
                        : "?";
                    blockReason = blockingWindow.BalanceAmount < 0m
                        ? "Primary guest folio window " + winLabel + " has negative balance: " + blockingWindow.BalanceAmount
                        : "Primary guest folio window " + winLabel + " has outstanding balance: " + blockingWindow.BalanceAmount;
                    return false;
                }

                log.Log(
                    "All primary guest folio windows are 0 — precheckout allowed"
                    + (Math.Abs(companyOrTaBalance) > 0.0001m
                        ? " (company/TA balance " + companyOrTaBalance + " remains for FO)"
                        : " (guest and company/TA clear)"),
                    reservationNameId, actionName, actionGroup);
                return true;
            }

            // Fallback when FolioWindows missing: use aggregated guest BalanceAmount only
            guestBalanceForUi = folio.BalanceAmount;
            if (Math.Abs(companyOrTaBalance) > 0.0001m)
            {
                log.Log(
                    "Company/travel-agent folio still has balance " + companyOrTaBalance
                    + ". Precheckout uses guest BalanceAmount only; FO settles company/TA manually.",
                    reservationNameId, actionName, actionGroup);
            }

            if (Math.Abs(folio.BalanceAmount) > 0.0001m)
            {
                blockReason = folio.BalanceAmount < 0m
                    ? "Primary guest folio balance is negative: " + folio.BalanceAmount
                    : "Primary guest folio has outstanding balance: " + folio.BalanceAmount;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Shows the Link Expiry page with a reason-specific guest message and server log entry.
        /// </summary>
        private ActionResult ShowLinkExpiry(string reason, string reference, string actionName, string actionGroup)
        {
            string message = LinkExpiryHelper.GetGuestMessage(reason);
            Helpers.LogHelper.Instance.Warn(
                $"Link expiry page shown. Reason={reason}. Message={message.Replace("\n", " ")}",
                string.IsNullOrEmpty(reference) ? "0" : reference,
                actionName ?? "Index",
                actionGroup ?? "Pre-Checkout");
            ViewBag.ExpiryReason = reason;
            ViewBag.ExpiryMessage = message;
            return View("ReservationNotFound");
        }

    }
}