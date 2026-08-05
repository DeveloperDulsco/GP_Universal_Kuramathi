using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Threading;
using System.Globalization;
using CheckinPortal.BackOffice.Helpers;
using NLog.Targets;
using NLog;

namespace CheckinPortal
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            try
            {
                //if (!string.IsNullOrEmpty(AppSettingsManager.GetDecryptedSetting("EnNLogConnection")))
                //{
                //    var dbTarget = (DatabaseTarget)LogManager.Configuration.FindTargetByName("db");
                //    dbTarget.ConnectionString = AppSettingsManager.GetDecryptedSetting("EnNLogConnection");
                //    LogManager.ReconfigExistingLoggers();
                //}
            }
            catch { }
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            MvcHandler.DisableMvcResponseHeader = true; //Remove the MVC Version from response header (for Security)

            ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback((sender, certificate, chain, policyErrors) => { return true; });
        }


        protected void Application_AcquireRequestState(Object sender, EventArgs e)
        {
            HttpContext context = HttpContext.Current;
            var languageSession = "en";

            if (Request.Cookies.AllKeys.Contains("culture"))
            {
                // File.AppendAllLines($"{Server.MapPath("/log.txt")}", new string[] { $"{DateTime.Now.ToString()} :", Request.Cookies["culture"].Value });

                languageSession = Request.Cookies["culture"].Value.ToString();
            }

            //if (context != null && context.Session != null)
            //{
            //    languageSession = context.Session["SelectedLanguage"] != null ? context.Session["SelectedLanguage"].ToString() : "en";
            //}
            //Thread.CurrentThread.CurrentUICulture = new CultureInfo(languageSession);
            Thread.CurrentThread.CurrentCulture = new CultureInfo(languageSession);
        }

        protected void Application_PreSendRequestHeaders()
        {
            if (HttpContext.Current != null)
            {
                HttpContext.Current.Response.Headers.Remove("Server");
            }
            Response.Headers.Remove("X-Frame-Options");
            Response.AddHeader("X-Frame-Options", "AllowAll");

        }


        /// <summary>
        /// Remove the Server name from the response header. (for security)
        /// </summary>

    }
}
