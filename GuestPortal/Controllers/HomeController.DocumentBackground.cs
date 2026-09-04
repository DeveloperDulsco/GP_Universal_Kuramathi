using CheckinPortal.BusinessLayer;
using CheckinPortal.Helpers;
using CheckinPortal.Models;
using CheckinPortal.Models.Emails;
using CheckinPortal.Models.OWS;
using CheckinPortal.Models.Whatsapp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Hosting;

namespace CheckinPortal.Controllers
{
    public partial class HomeController
    {
        private static OwsRequestModel BuildOwsCredentials()
        {
            return new OwsRequestModel
            {
                ChainCode = ConfigurationManager.AppSettings["ChainCode"],
                DestinationEntityID = ConfigurationManager.AppSettings["DestinationEntityID"],
                DestinationSystemType = ConfigurationManager.AppSettings["DestinationSystemType"],
                HotelDomain = ConfigurationManager.AppSettings["HotelDomain"],
                KioskID = ConfigurationManager.AppSettings["KioskID"],
                Language = ConfigurationManager.AppSettings["Language"],
                LegNumber = "1",
                Password = ConfigurationManager.AppSettings["Password"],
                SystemType = ConfigurationManager.AppSettings["SystemType"],
                Username = ConfigurationManager.AppSettings["Username"]
            };
        }

        private static string MapOperaGender(string gender)
        {
            if (string.IsNullOrWhiteSpace(gender))
                return null;
            string g = gender.Trim().ToUpperInvariant();
            if (g == "M" || g == "MALE")
                return "Male";
            if (g == "F" || g == "FEMALE")
                return "Female";
            return null;
        }

        private static Models.OWS.UpdateProfile BuildUpdateProfileFromDocument(string profileId, UpdateReservationModel documentModel)
        {
            var profile = new Models.OWS.UpdateProfile
            {
                ProfileID = profileId,
                DOB = documentModel.BirthDate,
                DocumentNumber = documentModel.DocumentNumber,
                DocumentType = !string.IsNullOrWhiteSpace(documentModel.DocumentType) ? documentModel.DocumentType : null,
                Gender = MapOperaGender(documentModel.Gender),
                IssueCountry = documentModel.IssueCountry,
                IssueDate = documentModel.IssueDate,
                Nationality = documentModel.Nationality
            };

            if (!string.IsNullOrWhiteSpace(documentModel.AddressLine1)
                || !string.IsNullOrWhiteSpace(documentModel.City)
                || !string.IsNullOrWhiteSpace(documentModel.PostalCode))
            {
                profile.Addresses = new List<Models.OWS.Address>
                {
                    new Models.OWS.Address
                    {
                        addressType = "HOME",
                        primary = true,
                        address1 = documentModel.AddressLine1,
                        address2 = documentModel.AddressLine2,
                        city = documentModel.City,
                        zip = documentModel.PostalCode
                    }
                };
            }

            if (!string.IsNullOrWhiteSpace(documentModel.Email))
            {
                profile.Emails = new List<Models.OWS.Email>
                {
                    new Models.OWS.Email { emailType = "EMAIL", primary = true, email = documentModel.Email }
                };
            }

            return profile;
        }

        private static void QueueDocumentOperaSync(string reservationNameId, string reservationNumber, string profileId, UpdateReservationModel documentModel)
        {
            if (documentModel == null || string.IsNullOrWhiteSpace(reservationNameId) || string.IsNullOrWhiteSpace(profileId) || profileId == "0")
            {
                new LogHelper().Log(
                    "Background Opera document sync skipped (missing reservation or profile id)",
                    reservationNameId ?? "",
                    "UploadDocument",
                    "Background Opera document sync");
                return;
            }

            string nameId = reservationNameId;
            string confNo = reservationNumber;
            string pid = profileId;
            var snapshot = new UpdateReservationModel
            {
                ReservationID = documentModel.ReservationID,
                ProfileDetailID = documentModel.ProfileDetailID,
                ProfileID = pid,
                FirstName = documentModel.FirstName,
                MiddleName = documentModel.MiddleName,
                LastName = documentModel.LastName,
                Gender = documentModel.Gender,
                BirthDate = documentModel.BirthDate,
                DocumentNumber = documentModel.DocumentNumber,
                DocumentType = documentModel.DocumentType,
                IssueCountry = documentModel.IssueCountry,
                IssueDate = documentModel.IssueDate,
                ExpiryDate = documentModel.ExpiryDate,
                Nationality = documentModel.Nationality,
                AddressLine1 = documentModel.AddressLine1,
                AddressLine2 = documentModel.AddressLine2,
                City = documentModel.City,
                PostalCode = documentModel.PostalCode,
                Email = documentModel.Email,
                Phone = documentModel.Phone
            };

            HostingEnvironment.QueueBackgroundWorkItem(async ct =>
            {
                try
                {
                    await PushUploadedDocumentToOperaAsync(nameId, confNo, pid, snapshot);
                }
                catch (Exception ex)
                {
                    new LogHelper().Error(ex, nameId, "UploadDocument", "Background Opera document sync");
                }
            });
        }

        private static async Task PushUploadedDocumentToOperaAsync(string reservationNameId, string reservationNumber, string profileId, UpdateReservationModel documentModel)
        {
            const string ActionName = "UploadDocument";
            const string ActionGroup = "Background Opera document sync";
            string apiUrl = ConfigurationManager.AppSettings["APIBaseUrl"];
            var mapper = new HomeController();

            string operaDocumentType = !string.IsNullOrWhiteSpace(documentModel.DocumentType)
                ? await mapper.GetDocumentByCode(documentModel.DocumentType) : null;
            if (string.IsNullOrWhiteSpace(operaDocumentType))
                operaDocumentType = null;

            bool hasDobForOpera = documentModel.BirthDate.HasValue && documentModel.BirthDate.Value.Year > 1900;
            string gender = !string.IsNullOrEmpty(documentModel.Gender)
                ? (documentModel.Gender.ToUpper().Equals("MALE") || documentModel.Gender.ToUpper().Equals("M") ? "Male"
                    : (documentModel.Gender.ToUpper().Equals("FEMALE") || documentModel.Gender.ToUpper().Equals("F") ? "Female" : null))
                : null;
            string issueCountry = !string.IsNullOrWhiteSpace(documentModel.IssueCountry) ? await mapper.GetCountryByCode(documentModel.IssueCountry) : null;
            string nationality = !string.IsNullOrWhiteSpace(documentModel.Nationality) ? await mapper.GetCountryByCode(documentModel.Nationality) : null;
            DateTime? issueDate = documentModel.IssueDate.HasValue && documentModel.IssueDate.Value.Year > 1900 ? documentModel.IssueDate : null;

            var profileOws = BuildOwsCredentials();
            profileOws.UpdateProileRequest = new Models.OWS.UpdateProfile
            {
                ProfileID = profileId,
                DOB = hasDobForOpera ? documentModel.BirthDate.Value.Date : (DateTime?)null,
                DocumentNumber = documentModel.DocumentNumber,
                DocumentType = operaDocumentType,
                Gender = gender,
                IssueCountry = issueCountry,
                IssueDate = issueDate,
                Nationality = nationality
            };
            var profileResponse = await new CloudHelper().UpdateGuestProfile(reservationNameId, profileOws, ActionGroup, apiUrl);
            if (profileResponse == null || !profileResponse.result)
                new LogHelper().Log("Failed to update profile in Opera (background): " + (profileResponse != null ? profileResponse.responseMessage : "null"), reservationNameId, ActionName, ActionGroup);
            else
                new LogHelper().Log("Updated guest profile in Opera (background)", reservationNameId, ActionName, ActionGroup);

            var passportOws = BuildOwsCredentials();
            passportOws.UpdateProileRequest = profileOws.UpdateProileRequest;
            var passportResponse = await new CloudHelper().UpdateGuestPassport(reservationNameId, passportOws, ActionGroup, apiUrl);
            if (passportResponse == null || !passportResponse.result)
                new LogHelper().Log("Failed to update passport in Opera (background): " + (passportResponse != null ? passportResponse.responseMessage : "null"), reservationNameId, ActionName, ActionGroup);
            else
                new LogHelper().Log("Updated passport in Opera (background)", reservationNameId, ActionName, ActionGroup);

            if (!string.IsNullOrWhiteSpace(documentModel.AddressLine1))
            {
                try
                {
                    var addressUpdateRequest = new Models.UpdateProfile()
                    {
                        ProfileID = profileId,
                        Addresses = new List<Models.Address>()
                        {
                            new Models.Address()
                            {
                                address1 = documentModel.AddressLine1,
                                address2 = documentModel.AddressLine2,
                                city = documentModel.City,
                                state = null,
                                country = issueCountry,
                                zip = documentModel.PostalCode,
                                displaySequence = 1,
                                primary = true,
                                addressType = "BUSINESS"
                            }
                        }
                    };
                    var addressResponse = await CloudHelper.UpdateProfileAddressAsync(
                        reservationNameId,
                        new OWSRequestModel()
                        {
                            ChainCode = ConfigurationManager.AppSettings["ChainCode"],
                            DestinationEntityID = ConfigurationManager.AppSettings["DestinationEntityID"],
                            DestinationSystemType = ConfigurationManager.AppSettings["DestinationSystemType"],
                            HotelDomain = ConfigurationManager.AppSettings["HotelDomain"],
                            KioskID = ConfigurationManager.AppSettings["KioskID"],
                            LegNumber = "1",
                            Language = ConfigurationManager.AppSettings["Language"],
                            Password = ConfigurationManager.AppSettings["Password"],
                            Username = ConfigurationManager.AppSettings["Username"],
                            SystemType = ConfigurationManager.AppSettings["SystemType"],
                            UpdateProileRequest = addressUpdateRequest
                        },
                        ActionGroup,
                        apiUrl);
                    if (addressResponse != null && addressResponse.result)
                        new LogHelper().Log("Opera address update from document (background): Success", reservationNameId, ActionName, ActionGroup);
                    else
                        new LogHelper().Log("Opera address update from document (background): Failed - " + (addressResponse != null ? addressResponse.responseMessage : "null"), reservationNameId, ActionName, ActionGroup);
                }
                catch (Exception addressEx)
                {
                    new LogHelper().Error(addressEx, reservationNameId, ActionName, ActionGroup);
                }
            }
        }

        private static void QueueCompletedocFollowUp(
            SessionDt session,
            CloudReservationModel reservation,
            bool alreadyPrecheckinCompleted,
            bool notificationEnabled,
            string[] channels)
        {
            if (session == null || string.IsNullOrWhiteSpace(session.ReservationNameID))
                return;

            string nameId = session.ReservationNameID;
            string confNo = session.ReservationNumber;
            bool alreadyDone = alreadyPrecheckinCompleted;
            bool notify = notificationEnabled;
            string[] channelCopy = channels == null ? new string[0] : (string[])channels.Clone();
            CloudReservationModel cloud = reservation;

            HostingEnvironment.QueueBackgroundWorkItem(async ct =>
            {
                try
                {
                    await RunCompletedocFollowUpAsync(nameId, confNo, alreadyDone, notify, channelCopy, cloud);
                }
                catch (Exception ex)
                {
                    new LogHelper().Error(ex, nameId, "CompletedocUploadAsync", "Background document Next follow-up");
                }
            });
        }

        private static bool ChannelEnabled(string[] channels, string name)
        {
            if (channels == null || channels.Length == 0)
                return string.Equals(name, "Email", StringComparison.OrdinalIgnoreCase);
            return channels.Any(c => string.Equals(c, name, StringComparison.OrdinalIgnoreCase));
        }

        private static async Task RunCompletedocFollowUpAsync(
            string reservationNameId,
            string reservationNumber,
            bool alreadyPrecheckinCompleted,
            bool notificationEnabled,
            string[] channels,
            CloudReservationModel reservation)
        {
            const string ActionName = "CompletedocUploadAsync";
            const string ActionGroup = "Background document Next follow-up";
            string apiUrl = ConfigurationManager.AppSettings["APIBaseUrl"];
            var reservationLogics = new ReservationLogics();

            OperaReservation operaReservation = null;
            try
            {
                var pmsList = await reservationLogics.FetchReservationDetailFromPMS(reservationNumber);
                if (pmsList != null && pmsList.Count > 0)
                    operaReservation = pmsList[0];
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameId, ActionName, ActionGroup);
            }

            try
            {
                string precheckinTraceText = ConfigurationManager.AppSettings["PreCheckinCompletedTraceMessage"];
                if (string.IsNullOrWhiteSpace(precheckinTraceText))
                    precheckinTraceText = "pre-check-in completed";

                var traceResponse = await new CloudHelper().AddReservationCompletionTrace(
                    reservationNameId,
                    reservationNumber,
                    precheckinTraceText,
                    ActionGroup,
                    apiUrl);

                if (traceResponse != null && traceResponse.result)
                    new LogHelper().Log("Opera reservation trace posted (background): " + precheckinTraceText, reservationNameId, ActionName, ActionGroup);
                else
                    new LogHelper().Log("Failed to post Opera reservation trace (background): " + (traceResponse != null ? traceResponse.responseMessage : "null"), reservationNameId, ActionName, ActionGroup);
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameId, ActionName, ActionGroup);
            }

            if (operaReservation != null)
            {
                try
                {
                    new LogHelper().Log("Generating registration card (background)", reservationNameId, ActionName, ActionGroup);
                    var ows = BuildOwsCredentials();
                    ows.OperaReservation = operaReservation;
                    var regcardResponse = await new CloudHelper().GetRegistrationCard(reservationNameId, ows, ActionGroup, apiUrl);
                    if (regcardResponse != null && regcardResponse.result && regcardResponse.responseData != null)
                    {
                        string regcardBase64 = regcardResponse.responseData.ToString();
                        if (!string.IsNullOrEmpty(regcardBase64))
                        {
                            byte[] regCard = Convert.FromBase64String(regcardBase64);
                            var insertResponse = await new CloudHelper().InsertReservationDocuments(reservationNameId, new APIRequestModel
                            {
                                RequestObject = new List<ReservationDocumentsDataTableModel>
                                {
                                    new ReservationDocumentsDataTableModel
                                    {
                                        Document = regCard,
                                        DocumentType = "Registration Card",
                                        ReservationNameID = reservationNameId
                                    }
                                }
                            }, ActionGroup, apiUrl);
                            if (insertResponse != null && insertResponse.result)
                                new LogHelper().Log("Registration card saved (background)", reservationNameId, ActionName, ActionGroup);
                            else
                                new LogHelper().Log("Failed to save registration card (background): " + (insertResponse != null ? insertResponse.responseMessage : "null"), reservationNameId, ActionName, ActionGroup);
                        }
                    }
                    else
                    {
                        new LogHelper().Log("Failed to generate regcard (background): " + (regcardResponse != null ? regcardResponse.responseMessage : "null"), reservationNameId, ActionName, ActionGroup);
                    }
                }
                catch (Exception ex)
                {
                    new LogHelper().Error(ex, reservationNameId, ActionName, ActionGroup);
                }
            }

            if (alreadyPrecheckinCompleted)
            {
                new LogHelper().Log("Background follow-up skipped email/track (already completed)", reservationNameId, ActionName, ActionGroup);
                return;
            }

            try
            {
                var trackResponse = await new CloudHelper().PushReservationTrackLocally(reservationNameId, new APIRequestModel
                {
                    RequestObject = new ReservationTrackStatus
                    {
                        ReservationNameID = reservationNameId,
                        ProcessType = ReservationProcessType.PrecheckinCompleted.ToString(),
                        ReservationNumber = reservationNumber,
                        ProcessStatus = "Precheckin",
                        EmailSent = false
                    }
                }, ActionGroup, apiUrl);
                if (trackResponse != null && trackResponse.result)
                    new LogHelper().Log("Reservation track updated (background)", reservationNameId, ActionName, ActionGroup);
                else
                    new LogHelper().Log("Failed to update reservation track (background): " + (trackResponse != null ? trackResponse.responseMessage : "null"), reservationNameId, ActionName, ActionGroup);
            }
            catch (Exception ex)
            {
                new LogHelper().Error(ex, reservationNameId, ActionName, ActionGroup);
            }

            if (!notificationEnabled)
                return;

            Models.OWS.GuestProfile guestProfile = operaReservation?.GuestProfiles != null && operaReservation.GuestProfiles.Count > 0
                ? operaReservation.GuestProfiles[0]
                : null;

            if (ChannelEnabled(channels, "Email") || ChannelEnabled(channels, "email"))
            {
                try
                {
                    string toEmail = null;
                    if (guestProfile?.Email != null && guestProfile.Email.Count > 0)
                    {
                        var emails = guestProfile.Email;
                        var primary = emails.FirstOrDefault(e => e.primary != null && e.primary.Value && !string.IsNullOrWhiteSpace(e.email));
                        toEmail = primary != null
                            ? primary.email
                            : emails.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e.email))?.email;
                    }

                    if (!string.IsNullOrWhiteSpace(toEmail) && operaReservation != null)
                    {
                        TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                        var emailResponse = await new CloudHelper().SendEmail(reservationNameId, new EmailRequest
                        {
                            FromEmail = ConfigurationManager.AppSettings["PreArrivalConfirmationEmail"],
                            ToEmail = toEmail,
                            GuestName = "" + (!string.IsNullOrEmpty(guestProfile.FirstName) ? textInfo.ToTitleCase(guestProfile.FirstName) + " " : "")
                                        + (!string.IsNullOrEmpty(guestProfile.MiddleName) ? textInfo.ToTitleCase(guestProfile.MiddleName) + " " : "")
                                        + (!string.IsNullOrEmpty(guestProfile.LastName) ? textInfo.ToTitleCase(guestProfile.LastName) : ""),
                            Subject = ConfigurationManager.AppSettings["PreArrivalConfirmationEmailSubject"],
                            confirmationNumber = operaReservation.ReservationNumber,
                            displayFromEmail = ConfigurationManager.AppSettings["EmailDisplayName"],
                            EmailType = EmailType.CheckinConfirmation,
                            ArrivalDate = operaReservation.ArrivalDate.HasValue ? operaReservation.ArrivalDate.Value.ToString("dd-MMM-yyyy") : "",
                            DepartureDate = operaReservation.DepartureDate.HasValue ? operaReservation.DepartureDate.Value.ToString("dd-MMM-yyy") : ""
                        }, ActionGroup, apiUrl);

                        if (emailResponse != null && emailResponse.result)
                            new LogHelper().Log("Confirmation email sent (background) to " + toEmail, reservationNameId, ActionName, ActionGroup);
                        else
                            new LogHelper().Log("Failed to send confirmation email (background): " + (emailResponse != null ? emailResponse.responseMessage : "null"), reservationNameId, ActionName, ActionGroup);
                    }
                    else
                    {
                        new LogHelper().Log("Confirmation email skipped (background): no email address", reservationNameId, ActionName, ActionGroup);
                    }
                }
                catch (Exception ex)
                {
                    new LogHelper().Error(ex, reservationNameId, ActionName, ActionGroup);
                }
            }

            if (ChannelEnabled(channels, "WhatsApp") || ChannelEnabled(channels, "Whatsapp") || ChannelEnabled(channels, "SMS"))
            {
                try
                {
                    string phone = null;
                    if (guestProfile?.Phones != null && guestProfile.Phones.Count > 0)
                    {
                        var primary = guestProfile.Phones.FirstOrDefault(p => p.primary != null && p.primary.Value && !string.IsNullOrWhiteSpace(p.PhoneNumber));
                        phone = primary != null
                            ? primary.PhoneNumber
                            : guestProfile.Phones.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.PhoneNumber))?.PhoneNumber;
                    }

                    if (!string.IsNullOrWhiteSpace(phone))
                    {
                        var waResponse = await new CloudHelper().SendWhatsappMsg(reservationNameId, new WhatsAppTemplateRequest
                        {
                            ReceiverPhone = phone,
                            TemplateName = EmailType.CheckinConfirmation,
                            LanguageCode = ConfigurationManager.AppSettings["Language"] ?? "en",
                            BodyParameters = new List<string>
                            {
                                guestProfile?.FirstName ?? "",
                                reservationNumber ?? ""
                            }
                        }, ActionGroup, apiUrl);
                        if (waResponse != null && waResponse.result)
                            new LogHelper().Log("Confirmation WhatsApp sent (background)", reservationNameId, ActionName, ActionGroup);
                        else
                            new LogHelper().Log("Failed to send confirmation WhatsApp (background): " + (waResponse != null ? waResponse.responseMessage : "null"), reservationNameId, ActionName, ActionGroup);
                    }
                }
                catch (Exception ex)
                {
                    new LogHelper().Error(ex, reservationNameId, ActionName, ActionGroup);
                }
            }
        }
    }
}
