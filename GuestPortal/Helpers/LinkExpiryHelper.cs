namespace CheckinPortal.Helpers
{
    /// <summary>
    /// Guest-facing copy and reason codes for the Link Expiry / ReservationNotFound page.
    /// </summary>
    public static class LinkExpiryHelper
    {
        public const string Default = "Default";
        public const string MissingLink = "MissingLink";
        public const string InvalidLink = "InvalidLink";
        public const string AlreadyPreCheckedIn = "AlreadyPreCheckedIn";
        public const string AlreadyPreCheckedOut = "AlreadyPreCheckedOut";
        public const string CheckedOut = "CheckedOut";
        public const string Cancelled = "Cancelled";
        public const string NoShow = "NoShow";
        public const string ReservationNotFound = "ReservationNotFound";
        public const string InvalidStatus = "InvalidStatus";
        public const string NotEligible = "NotEligible";
        /// <summary>Sharer / adult count zero — not eligible for online precheck-in or precheckout.</summary>
        public const string ZeroAdults = "ZeroAdults";

        /// <summary>
        /// Default generic consumed/expired copy (two sentences).
        /// </summary>
        public const string DefaultMessage =
            "This link has either already been used or has expired.\n" +
            "If you have not yet completed the process, please contact the hotel's Guest Reservations team for assistance.";

        public static string GetGuestMessage(string reason)
        {
            switch (reason ?? Default)
            {
                case AlreadyPreCheckedIn:
                    return "Pre check-in has already been completed for this reservation.\n" +
                           "If you need further assistance, please contact the hotel's Guest Reservations team.";

                case AlreadyPreCheckedOut:
                    return "Pre check-out has already been completed for this reservation.\n" +
                           "If you need further assistance, please contact the hotel's Guest Reservations team.";

                case CheckedOut:
                    return "This reservation has already been checked out.\n" +
                           "Please contact the hotel's Guest Reservations team for assistance.";

                case Cancelled:
                    return "This reservation has been cancelled.\n" +
                           "Please contact the hotel's Guest Reservations team for assistance.";

                case NoShow:
                    return "This reservation was recorded as a no-show.\n" +
                           "Please contact the hotel's Guest Reservations team for assistance.";

                case ReservationNotFound:
                    return "We could not find a reservation for this link.\n" +
                           "Please contact the hotel's Guest Reservations team for assistance.";

                case InvalidStatus:
                    return "This reservation is not available for online pre check-in or pre check-out.\n" +
                           "Please contact the hotel's Guest Reservations team for assistance.";

                case ZeroAdults:
                    return "This reservation has 0 adult count and is not available for online pre check-in or pre check-out.\n" +
                           "Please contact the hotel's Front Office for assistance.";

                case MissingLink:
                case InvalidLink:
                    return "This link is invalid or incomplete.\n" +
                           "Please use the original link from your email, or contact the hotel's Guest Reservations team for assistance.";

                case NotEligible:
                    return "This reservation is not eligible to continue with this link.\n" +
                           "Please contact the hotel's Guest Reservations team for assistance.";

                case Default:
                default:
                    return DefaultMessage;
            }
        }

        /// <summary>
        /// Prefer Opera Adults; fall back to cloud Adultcount when Opera did not return a value.
        /// </summary>
        public static int? ResolveAdultCount(int? operaAdults, int? cloudAdultCount)
        {
            if (operaAdults.HasValue)
                return operaAdults;
            return cloudAdultCount;
        }

        /// <summary>True when adult count is present and greater than zero (primary guest, not sharer).</summary>
        public static bool HasEligibleAdultCount(int? adultCount)
        {
            return adultCount.HasValue && adultCount.Value > 0;
        }
    }
}
