using CheckinPortal.Models;
using CheckinPortal.Models.OWS;
using System;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace CheckinPortal.Helpers
{
    /// <summary>
    /// FO progress events → TbAuditTrailUserDetails via Cloud/Local API
    /// /local/InsertAuditLog or /audit/InsertAuditLog → Usp_InsertAuditTrailDetails.
    /// ReservationNumber in payload maps to TbAuditTrailUserDetails.ReservationID (local ReservationDetailID).
    /// UserName is the primary guest full name when available.
    /// </summary>
    public static class AuditProgressHelper
    {
        public const string ModulePreCheckin = "PreCheckin";
        public const string ModulePreCheckout = "PreCheckout";
        public const string ApplicationName = "GuestPortal";
        private const string FallbackUserName = "Guest";
        private const string SessionGuestNameKey = "AuditProgressGuestName";

        public static class Actions
        {
            public const string LinkOpened = "Link opened";
            public const string GuestDetailsSaved = "Guest details saved";
            public const string PoliciesApproved = "Policies approved";
            public const string SignatureCompleted = "Signature completed";
            public const string DocumentUploaded = "Document uploaded";
            public const string DocumentSkipped = "Document skipped";
            public const string DocumentSkippedWithoutProfile = "Document skipped without a profile";
            public const string DocumentUploadSummary = "Document upload summary";
            public const string TriedToUploadInvalidDocument = "Tried to upload invalid document";
            public const string TriedToUploadExpiredDocument = "Tried to upload expired document";
            public const string RegistrationCardUpdated = "Registration card updated";
            public const string DisclaimerSaved = "Disclaimer saved";
            public const string PrecheckinCompleted = "Precheckin completed";
            public const string ThankYouButtonClicked = "Thank You button clicked";
            public const string FolioAgreed = "Folio agreed";
            public const string FolioSigned = "Folio signed";
            public const string FolioApprovedSigned = "Approved by signing the invoice";
            public const string ApprovedMovedToThankYou = "Approved and moved to Thank you";
            public const string InvoiceEmailResent = "Invoice email sent again from Thank you page";
            public const string InvoiceEmailResendFailed = "Invoice email from Thank you page failed";
            public const string PrecheckoutCompleted = "Precheckout completed";
            public const string LinkSentEmail = "Link sent via email";
            public const string LinkSentWhatsApp = "Link sent via WhatsApp";
            public const string LinkSentSms = "Link sent via SMS";
            public const string ConfirmationMailSent = "Confirmation mail sent";
            public const string ConfirmationMailFailed = "Confirmation mail failed";
            public const string ConfirmationSmsSent = "Confirmation SMS sent";
            public const string ConfirmationSmsFailed = "Confirmation SMS failed";

            public const string OperaEmailUpdateSuccess = "Opera email update success";
            public const string OperaEmailUpdateFailed = "Opera email update failed";
            public const string OperaPhoneUpdateSuccess = "Opera phone update success";
            public const string OperaPhoneUpdateFailed = "Opera phone update failed";
            public const string OperaAddressUpdateSuccess = "Opera address update success";
            public const string OperaAddressUpdateFailed = "Opera address update failed";
            public const string OperaNationalityUpdateSuccess = "Opera nationality update success";
            public const string OperaNationalityUpdateFailed = "Opera nationality update failed";
            public const string LocalNationalityUpdateSuccess = "Local nationality update success";
            public const string LocalNationalityUpdateFailed = "Local nationality update failed";
            public const string MovedToNextPage = "Moved to next page";
            public const string GuestDetailsChanged = "Guest details changed";
            public const string LinkReopened = "Link reopened";
            public const string TabClosed = "Guest portal tab closed";
            public const string OpenedInMultipleWindows = "Guest portal opened in another window";
            public const string AllergiesYes = "Allergies: Yes";
            public const string AllergiesNo = "Allergies: No";
            public const string AllergiesNotAdded = "Allergies not added";
            public const string PromotionalConsentAdded = "Promotional consent accepted";
            public const string PromotionalConsentNotAdded = "Promotional consent not added";
            public const string ExcursionDetailsAdded = "Excursion details added";
            public const string ExcursionDetailsNotAdded = "Excursion details not added";
            public const string ProfileCreationFailedDocumentSkipped = "Profile creation failed because the document was skipped";
        }

        public const string LinkSourceGrabber = "Grabber";
        public const string LinkSourceBackoffice = "Back office";
        public const string LinkSourceQr = "QR code";
        public const string LinkSourceEmail = "email / WhatsApp link";

        /// <summary>Format for MovedToNextPage extraDetail, e.g. "Guest details to Policies".</summary>
        public static string FormatPageMove(string fromStep, string toStep)
        {
            return $"{fromStep} to {toStep}";
        }

        public static string PageNameFromTabIndex(object tabIndex)
        {
            int idx = -1;
            if (tabIndex != null)
                int.TryParse(tabIndex.ToString(), out idx);
            switch (idx)
            {
                case 0: return "Guest details";
                case 1: return "Policies";
                case 2: return "Documents";
                case 3: return "Thank you";
                default: return "Start";
            }
        }

        public static string PageNameFromTabId(string tabId)
        {
            if (string.IsNullOrWhiteSpace(tabId))
                return "Guest details";
            switch (tabId.Trim())
            {
                case "guestDetails": return "Guest details";
                case "policies": return "Policies";
                case "document": return "Documents";
                case "qrCode": return "Thank you";
                case "folio": return "Folio invoice";
                case "thankyou": return "Thank you";
                case "payment": return "Payment";
                default: return tabId.Trim();
            }
        }

        /// <summary>
        /// Grabber / Back office should send src=grabber or src=backoffice on the link.
        /// QR search appends src=qr. Otherwise treated as an email / WhatsApp guest link.
        /// </summary>
        public static string ResolveLinkSource(HttpRequestBase request, HttpSessionStateBase session)
        {
            string raw = request?["src"] ?? request?["source"] ?? request?["from"];
            if (string.IsNullOrWhiteSpace(raw) && session != null)
                raw = session["GpLinkSource"] as string;

            string key = (raw ?? "").Trim().ToLowerInvariant();
            if (key == "grabber" || key == "gr" || key == "g")
                return LinkSourceGrabber;
            if (key == "backoffice" || key == "bo" || key == "back office" || key == "fo")
                return LinkSourceBackoffice;
            if (key == "qr" || key == "kiosk" || key == "search")
                return LinkSourceQr;
            if (key == "email" || key == "mail" || key == "sms" || key == "whatsapp")
                return LinkSourceEmail;
            return LinkSourceEmail;
        }

        public static string FormatLinkOpenDetail(string sourceLabel, bool isReopen, string pageName)
        {
            string via = "Opened via " + (sourceLabel ?? LinkSourceEmail);
            if (!isReopen)
                return via;
            if (string.IsNullOrWhiteSpace(pageName) || pageName == "Start")
                return via + ". Link reopened";
            return via + ". Link reopened and returned to " + pageName;
        }

        public static string FormatDocumentUploadedDetail(int profileDetailId, string guestLabel = null)
        {
            if (profileDetailId <= 0)
                return null;
            if (!string.IsNullOrWhiteSpace(guestLabel))
                return "for " + guestLabel.Trim() + " (guest profile " + profileDetailId + ")";
            return "for guest profile " + profileDetailId;
        }

        public static string FormatDocumentSkippedDetail(string profileDetailIdsCsv, string guestLabels = null)
        {
            var ids = ParsePositiveIds(profileDetailIdsCsv);
            if (ids.Count == 0)
                return string.IsNullOrWhiteSpace(guestLabels)
                    ? "Guest skipped ID upload, so a guest profile was not created"
                    : "Skipped for " + guestLabels.Trim() + " - guest profile was not created";
            string idList = string.Join(", ", ids);
            if (!string.IsNullOrWhiteSpace(guestLabels))
                return "Skipped for " + guestLabels.Trim() + " (guest profiles " + idList + ")";
            return "Skipped for guest profiles " + idList;
        }

        public static bool HasNoCreatedProfile(string profileDetailIdsCsv)
        {
            return ParsePositiveIds(profileDetailIdsCsv).Count == 0;
        }

        public static string FormatDocumentOutcome(
            int paxCount,
            int uploadedCount,
            int skippedWithoutProfileCount,
            string skippedWithoutProfileLabels)
        {
            int pax = paxCount > 0 ? paxCount : uploadedCount + skippedWithoutProfileCount;
            string summary = uploadedCount + " of " + pax + " guests uploaded";
            if (skippedWithoutProfileCount > 0)
            {
                summary += ". " + skippedWithoutProfileCount + " skipped without a profile";
                if (!string.IsNullOrWhiteSpace(skippedWithoutProfileLabels))
                    summary += " (" + skippedWithoutProfileLabels.Trim() + ")";
            }
            else
            {
                summary += ". None skipped without a profile";
            }
            return summary;
        }

        public static string MapGuestFieldLabel(string inputName)
        {
            if (string.IsNullOrWhiteSpace(inputName))
                return null;
            string n = inputName.ToLowerInvariant();
            if (n.Contains("email")) return "email";
            if (n.Contains("phone")) return "phone";
            if (n.Contains("firstname") || n.Contains("first_name")) return "first name";
            if (n.Contains("lastname") || n.Contains("last_name")) return "last name";
            if (n.Contains("middlename")) return "middle name";
            if (n.Contains("nationality")) return "nationality";
            if (n.Contains("address")) return "address";
            if (n.Contains("city")) return "city";
            if (n.Contains("postal") || n.Contains("zip")) return "postal code";
            if (n.Contains("country")) return "country";
            if (n.Contains("birth") || n.Contains("dob")) return "date of birth";
            if (n.Contains("gender")) return "gender";
            if (n.Contains("visitpurpose") || n.Contains("purpose")) return "purpose of visit";
            if (n.Contains("flight")) return "flight number";
            if (n.Contains("eta") || n.Contains("arrival")) return "arrival time";
            if (n.Contains("salutation") || n.Contains("title")) return "title";
            return null;
        }

        private static System.Collections.Generic.List<int> ParsePositiveIds(string csv)
        {
            var ids = new System.Collections.Generic.List<int>();
            if (string.IsNullOrWhiteSpace(csv))
                return ids;
            foreach (var part in csv.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int id;
                if (int.TryParse(part.Trim(), out id) && id > 0 && !ids.Contains(id))
                    ids.Add(id);
            }
            return ids;
        }

        /// <summary>
        /// Excel FO CSV is often opened as Windows-1252, so UTF-8 dashes show as â€“.
        /// Keep audit text ASCII-safe for that export.
        /// </summary>
        private static string SanitizeAuditText(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;
            return value
                .Replace('\u2013', '-') // en dash
                .Replace('\u2014', '-') // em dash
                .Replace('\u2212', '-') // minus
                .Replace('\u2018', '\'')
                .Replace('\u2019', '\'')
                .Replace('\u201C', '"')
                .Replace('\u201D', '"')
                .Replace('\u00A0', ' ');
        }

        /// <summary>FO audit for confirmation email or SMS/WhatsApp after pre-check-in / pre-check-out.</summary>
        public static void LogConfirmationSend(
            string moduleName,
            bool emailChannel,
            bool success,
            object reservationDetailId,
            string reservationNameID,
            string extraDetail = null,
            string guestName = null)
        {
            string actionName = emailChannel
                ? (success ? Actions.ConfirmationMailSent : Actions.ConfirmationMailFailed)
                : (success ? Actions.ConfirmationSmsSent : Actions.ConfirmationSmsFailed);

            Log(moduleName, actionName, reservationDetailId, reservationNameID, extraDetail, guestName);
        }

        /// <param name="reservationDetailId">Local ReservationDetailID (or ReservationModel.ReservationID). Stored in SP ReservationNumber column.</param>
        /// <param name="reservationNameID">Opera name ID — logging correlation only; not written as the audit reservation key.</param>
        /// <param name="guestName">Optional explicit guest name; otherwise resolved from session / SessionData.OperaReservation.</param>
        public static void Log(
            string moduleName,
            string actionName,
            object reservationDetailId,
            string reservationNameID = null,
            string extraDetail = null,
            string guestName = null)
        {
            try
            {
                string apiUrl = ConfigurationManager.AppSettings["APIBaseUrl"];
                if (string.IsNullOrWhiteSpace(apiUrl))
                    return;

                // Resolve on the request thread (before Task.Run) so session/profile are available
                string detailKey = NormalizeDetailId(reservationDetailId);
                string userName = ResolveGuestName(guestName);

                string message = string.IsNullOrWhiteSpace(extraDetail)
                    ? actionName
                    : actionName + " - " + extraDetail;
                message = SanitizeAuditText(message);
                if (message.Length > 200)
                    message = message.Substring(0, 200);
                actionName = SanitizeAuditText(actionName);

                var auditRequest = new PortalAuditLogModel
                {
                    ApplicationName = ApplicationName,
                    ModuleName = moduleName,
                    ActionName = actionName,
                    AuditMessage = message,
                    UserName = userName,
                    // TbAuditTrailUserDetails.ReservationID — FO progress stores ReservationDetailID here
                    ReservationNumber = detailKey,
                    DeviceIdentifier = "GuestPortal"
                };

                string logRef = !string.IsNullOrWhiteSpace(reservationNameID)
                    ? reservationNameID
                    : (detailKey ?? "0");

                Task.Run(async () =>
                {
                    try
                    {
                        await new CloudHelper().InsertAuditLog(
                            logRef,
                            new APIRequestModel { RequestObject = auditRequest },
                            moduleName,
                            apiUrl);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Instance.Error(ex, logRef, "InsertAuditLog", moduleName);
                    }
                });
            }
            catch (Exception ex)
            {
                try
                {
                    LogHelper.Instance.Error(ex, reservationNameID ?? "0", "InsertAuditLog", moduleName);
                }
                catch { /* swallow */ }
            }
        }

        /// <summary>Build primary guest display name: FirstName + MiddleName + LastName, or GuestName.</summary>
        public static string ResolveGuestName(string explicitGuestName = null)
        {
            if (!string.IsNullOrWhiteSpace(explicitGuestName))
            {
                string trimmed = TruncateUserName(explicitGuestName.Trim());
                CacheGuestName(trimmed);
                return trimmed;
            }

            string cached = GetCachedGuestName();
            if (!string.IsNullOrWhiteSpace(cached) && !string.Equals(cached, FallbackUserName, StringComparison.OrdinalIgnoreCase))
                return TruncateUserName(cached);

            try
            {
                Models.OWS.GuestProfile primary = SessionData.OperaReservation?.GuestProfiles?
                    .FirstOrDefault(g => g != null && g.IsPrimary)
                    ?? SessionData.OperaReservation?.GuestProfiles?.FirstOrDefault();

                if (primary != null)
                {
                    string fromParts = JoinNameParts(primary.FirstName, primary.MiddleName, primary.LastName);
                    if (string.IsNullOrWhiteSpace(fromParts))
                        fromParts = JoinNameParts(primary.GivenName, null, primary.FamilyName);
                    if (string.IsNullOrWhiteSpace(fromParts) && !string.IsNullOrWhiteSpace(primary.GuestName))
                        fromParts = primary.GuestName.Trim();
                    if (!string.IsNullOrWhiteSpace(fromParts))
                    {
                        string name = TruncateUserName(fromParts);
                        CacheGuestName(name);
                        return name;
                    }
                }
            }
            catch { /* session may be unavailable */ }

            return FallbackUserName;
        }

        public static string BuildGuestName(string firstName, string middleName, string lastName)
        {
            string name = JoinNameParts(firstName, middleName, lastName);
            return string.IsNullOrWhiteSpace(name) ? FallbackUserName : TruncateUserName(name);
        }

        private static void CacheGuestName(string guestName)
        {
            if (string.IsNullOrWhiteSpace(guestName) ||
                string.Equals(guestName, FallbackUserName, StringComparison.OrdinalIgnoreCase))
                return;
            try
            {
                var session = HttpContext.Current?.Session;
                if (session != null)
                    session[SessionGuestNameKey] = guestName;
            }
            catch { /* ignore */ }
        }

        private static string GetCachedGuestName()
        {
            try
            {
                return HttpContext.Current?.Session?[SessionGuestNameKey] as string;
            }
            catch
            {
                return null;
            }
        }

        private static string NormalizeDetailId(object reservationDetailId)
        {
            if (reservationDetailId == null)
                return "";

            if (reservationDetailId is int i)
                return i > 0 ? i.ToString() : "";

            if (reservationDetailId is long l)
                return l > 0 ? l.ToString() : "";

            string s = Convert.ToString(reservationDetailId)?.Trim();
            if (string.IsNullOrWhiteSpace(s) || s == "0")
                return "";
            return s;
        }

        private static string JoinNameParts(string first, string middle, string last)
        {
            return string.Join(" ", new[] { first, middle, last }
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim()));
        }

        private static string TruncateUserName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return FallbackUserName;
            return name.Length > 200 ? name.Substring(0, 200) : name;
        }
    }
}
