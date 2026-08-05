using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

namespace CheckinPortal.Helpers
{
    public class DataTableHelper
    {
        public static List<T> DataTableToList<T>(DataTable table) where T : class, new()
        {
            try
            {
                T tempT = new T();
                var tType = tempT.GetType();
                List<T> list = new List<T>();
                foreach (var row in table.Rows.Cast<DataRow>())
                {
                    T obj = new T();
                    foreach (var prop in obj.GetType().GetProperties())
                    {
                        var propertyInfo = tType.GetProperty(prop.Name);

                        try
                        {
                            var rowValue = row[prop.Name];
                            var t = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;
                            object safeValue = (rowValue == null || DBNull.Value.Equals(rowValue)) ? null : Convert.ChangeType(rowValue, t);
                            propertyInfo.SetValue(obj, safeValue, null);
                        }
                        catch
                        {

                        }
                    }
                    list.Add(obj);
                }
                return list;
            }
            catch
            {
                return null;
            }
        }

    }
}