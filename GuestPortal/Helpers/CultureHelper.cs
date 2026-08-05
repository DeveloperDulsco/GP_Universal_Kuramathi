
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web;

namespace CheckinPortal.Helpers
{
    public class CultureHelper
    {
        public static string GetCultureLabel(string lang)
        {
            return Resource.ResourceManager.GetString(lang, Thread.CurrentThread.CurrentCulture);

        }
    }
}