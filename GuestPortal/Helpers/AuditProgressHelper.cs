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
            public const string RegistrationCardUpdated = "Registration card updated";
            public const string DisclaimerSaved = "Disclaimer saved";
            public const string PrecheckinCompleted = "Precheckin completed";
            public const string ThankYouButtonClicked = "Thank You button clicked";
            public const string FolioAgreed = "Folio agreed";
            public const string FolioSigned = "Folio signed";
            public const string PrecheckoutCompleted = "Precheckout completed";
            public const string LinkSentEmail = "Link sent via email";
            public const string LinkSentWhatsApp = "Link sent via WhatsApp";
            public const string LinkSentSms = "Link sent via SMS";

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
        }

        /// <summary>Format for MovedToNextPage extraDetail, e.g. "Guest Details → Policies".</summary>
        public static string FormatPageMove(string fromStep, string toStep)
        {
            return $"{fromStep} → {toStep}";
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
                    : $"{actionName} — {extraDetail}";
                if (message.Length > 200)
                    message = message.Substring(0, 200);

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
