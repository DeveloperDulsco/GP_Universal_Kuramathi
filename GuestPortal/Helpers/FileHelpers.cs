using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace CheckinPortal.Helpers
{
    public class FileHelpers
    {
        public static void DeleteTempFiles()
        {
            try
            {
                Directory.GetFiles(HttpContext.Current.Server.MapPath($"~/temp/")).Select(f => new FileInfo(f))
                       .Where(f => f.CreationTime < DateTime.Now.AddDays(-1))
                       .ToList()
                       .ForEach(f => f.Delete());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}