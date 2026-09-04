using CheckinPortal.Models;
using CheckinPortal.Models.OWS;
using Newtonsoft.Json;
using System;
using System.Web.Mvc;

namespace CheckinPortal.Helpers
{
    /// <summary>
    /// One-hop cache from SearchReservation POST to the next Index GET (src=qr)
    /// so landing does not repeat Cloud + Opera lookups.
    /// </summary>
    public class QrSearchHandoff
    {
        public string ReservationNumber { get; set; }
        public string Flow { get; set; }
        public string CloudJson { get; set; }
        public string OperaJson { get; set; }
    }

    public static class QrSearchHandoffHelper
    {
        public const string TempDataKey = "QrSearchHandoff";

        public static void Save(
            TempDataDictionary tempData,
            string reservationNumber,
            string flow,
            CloudReservationModel cloud,
            OperaReservation opera)
        {
            if (tempData == null || string.IsNullOrWhiteSpace(reservationNumber) || cloud == null)
                return;

            tempData[TempDataKey] = JsonConvert.SerializeObject(new QrSearchHandoff
            {
                ReservationNumber = reservationNumber,
                Flow = flow,
                CloudJson = JsonConvert.SerializeObject(cloud),
                OperaJson = opera == null ? null : JsonConvert.SerializeObject(opera)
            });
        }

        public static QrSearchHandoff Parse(object raw, string reservationNumber, string src)
        {
            if (!string.Equals(src, "qr", StringComparison.OrdinalIgnoreCase))
                return null;
            string json = raw as string;
            if (string.IsNullOrWhiteSpace(json) || string.IsNullOrWhiteSpace(reservationNumber))
                return null;

            try
            {
                var handoff = JsonConvert.DeserializeObject<QrSearchHandoff>(json);
                if (handoff == null)
                    return null;
                if (!string.Equals(handoff.ReservationNumber, reservationNumber, StringComparison.OrdinalIgnoreCase))
                    return null;
                return handoff;
            }
            catch
            {
                return null;
            }
        }

        public static CloudReservationModel TryCloud(QrSearchHandoff handoff)
        {
            if (handoff == null || string.IsNullOrWhiteSpace(handoff.CloudJson))
                return null;
            try
            {
                return JsonConvert.DeserializeObject<CloudReservationModel>(handoff.CloudJson);
            }
            catch
            {
                return null;
            }
        }

        public static OperaReservation TryOpera(QrSearchHandoff handoff)
        {
            if (handoff == null || string.IsNullOrWhiteSpace(handoff.OperaJson))
                return null;
            try
            {
                return JsonConvert.DeserializeObject<OperaReservation>(handoff.OperaJson);
            }
            catch
            {
                return null;
            }
        }
    }
}
