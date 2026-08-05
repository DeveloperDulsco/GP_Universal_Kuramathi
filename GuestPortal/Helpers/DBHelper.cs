using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace CheckinPortal.Helpers
{
    public class DBHelper
    {
        private static readonly Lazy<DBHelper>
           lazy = new Lazy<DBHelper>(() => 
           new DBHelper()
           );

        public static DBHelper Instance 
        {
            get { 
                return lazy.Value; 
            } 
        }


        //public DataTable GetReservationDetails(string ReservationNumber)
        //{
        //    DataTable transaction = new DataTable();
        //    try
        //    {
        //        string Query = string.Empty;

        //        SqlParameter reservationNoParameter = new SqlParameter()
        //        {
        //            ParameterName = "@ReservationNumber",
        //            Value = ReservationNumber,
        //            SqlDbType = SqlDbType.VarChar
        //        };

        //        transaction = SQLHelpers.Instance.ExecuteSP("usp_GetReservationDetails", reservationNoParameter);
        //    }
        //    catch (Exception ex)
        //    {
        //        //Helpers.EvenLogHelper.Instance.LogError($"Unhandled Exception while executing SP Usp_FetchTopOneRecordsforProcessing {ex.ToString()}");
        //    }
        //    return transaction;
        //}
        //public DataTable GetReservationDetailsbyRefNo(string ReservationNumber)
        //{
        //    DataTable transaction = new DataTable();
        //    try
        //    {
        //        string Query = string.Empty;
        //        SqlParameter reservationNoParameter = new SqlParameter()
        //        {
        //            ParameterName = "@ReferenceNumber",
        //            Value = ReservationNumber,
        //            SqlDbType = SqlDbType.VarChar
        //        };

        //        transaction = SQLHelpers.Instance.ExecuteSP("Usp_GetReservationDetailsByReferenceNumber", reservationNoParameter);
        //    }
        //    catch (Exception ex)
        //    {
        //        //Helpers.EvenLogHelper.Instance.LogError($"Unhandled Exception while executing SP Usp_FetchTopOneRecordsforProcessing {ex.ToString()}");
        //    }
        //    return transaction;
        //}
    }

}