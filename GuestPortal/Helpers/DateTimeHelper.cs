using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace CheckinPortal.Helpers
{
    public class DateTimeHelper
    {
        public static string ConvertFromUTC(DateTime dateTime)
        {
            string HotelTimeZone = ConfigurationManager.AppSettings["HotelTimeZone"].ToString();
            TimeZoneInfo cstZone = TimeZoneInfo.FindSystemTimeZoneById(HotelTimeZone);
            DateTime sgtTime = TimeZoneInfo.ConvertTimeFromUtc(dateTime, cstZone);
            return sgtTime.ToString("HH:mm");
        }


        public static string ConvertToUTC(string time)
        {
            if (!string.IsNullOrEmpty(time))
            {
                var splitTime = time.Split(':');
                if (splitTime.Length > 1)
                {
                    string HotelTimeZone = ConfigurationManager.AppSettings["HotelTimeZone"].ToString();
                    TimeZoneInfo sgt = TimeZoneInfo.FindSystemTimeZoneById(HotelTimeZone);
                    DateTime expectedDateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,
                        Convert.ToInt32(splitTime[0]), Convert.ToInt32(splitTime[1]), 0);
                    DateTime tstTime = TimeZoneInfo.ConvertTime(expectedDateTime, sgt, TimeZoneInfo.Utc);
                    return tstTime.ToString("HH:mm");
                }
              
            }
  return "";
        }
        public static DateTime ConvertToUTCDatetime(string time)
        {
            if (!string.IsNullOrEmpty(time))
            {
                var splitTime = time.Split(':');
                if (splitTime.Length > 1)
                {
                    string HotelTimeZone = ConfigurationManager.AppSettings["HotelTimeZone"].ToString();
                    TimeZoneInfo sgt = TimeZoneInfo.FindSystemTimeZoneById(HotelTimeZone);
                    DateTime expectedDateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,
                        Convert.ToInt32(splitTime[0]), Convert.ToInt32(splitTime[1]), 0);
                    DateTime tstTime = TimeZoneInfo.ConvertTime(expectedDateTime, sgt, TimeZoneInfo.Utc);
                    return tstTime;
                }
            }
            return DateTime.MinValue;

        }
        public static string ConvertToTime(string time)
        {
            if (!string.IsNullOrEmpty(time))
            {
                var splitTime = time.Split(':');
                if (splitTime.Length > 1)
                {
                    string HotelTimeZone = ConfigurationManager.AppSettings["HotelTimeZone"].ToString();
                    TimeZoneInfo sgt = TimeZoneInfo.FindSystemTimeZoneById(HotelTimeZone);
                    DateTime expectedDateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,
                        Convert.ToInt32(splitTime[0]), Convert.ToInt32(splitTime[1]), 0);
                    //DateTime tstTime = TimeZoneInfo.ConvertTime(expectedDateTime, sgt, TimeZoneInfo.Utc);
                    return expectedDateTime.ToString("HH:mm");
                }

            }
            return "";
        }
    }
}