using CheckinPortal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CheckinPortal.BusinessLayer
{
    public class ReservationCloudLogic
    {
        public ReservationCloudLogic()
        {

        }
        public  List<Models.OWS.OperaReservation> verifyReservationisAllowed(List<Models.OWS.OperaReservation> reservationDataTables,string process)
        {
            if (reservationDataTables != null && reservationDataTables.Count > 0)
            {
                List<Models.OWS.OperaReservation> reservationDatas = new List<Models.OWS.OperaReservation>();
                foreach (Models.OWS.OperaReservation reservationDataTable in reservationDataTables)
                {
                    if (!string.IsNullOrEmpty(reservationDataTable.ReservationStatus))
                    {
                        if (process.ToUpper().Trim().Equals("PRECHECKIN"))
                            {

                            if (reservationDataTable.ReservationStatus.ToUpper().Trim().Equals("DUEIN") ||
                               reservationDataTable.ReservationStatus.ToUpper().Trim().Equals("RESERVED"))
                            {
                                reservationDatas.Add(reservationDataTable);
                            }
                        }
                        else if (process.ToUpper().Trim().Equals("CHECKOUT"))
                        {
                            if (reservationDataTable.ReservationStatus.ToUpper().Trim().Equals("DUEOUT"))
                            {
                                reservationDatas.Add(reservationDataTable);
                            }
                        }


                    }
                }
                return reservationDatas;
            }
            else
                return null;
        }

        public List<Models.OWS.OperaReservation> verifyReservationsisAllowed(List<Models.OWS.OperaReservation> reservationDataTables, string process)
        {
            if (reservationDataTables != null && reservationDataTables.Count > 0)
            {
                List<Models.OWS.OperaReservation> reservationDatas = new List<Models.OWS.OperaReservation>();
                foreach (Models.OWS.OperaReservation reservationDataTable in reservationDataTables)
                {
                    if (!string.IsNullOrEmpty(reservationDataTable.ReservationStatus))
                    {
                        if (process.ToUpper().Trim().Equals("PRECHECKIN"))
                        {

                            if (reservationDataTable.ReservationStatus.ToUpper().Trim().Equals("DUEIN") ||
                               reservationDataTable.ReservationStatus.ToUpper().Trim().Equals("RESERVED"))
                            {
                                reservationDatas.Add(reservationDataTable);
                            }
                        }
                        else if (process.ToUpper().Trim().Equals("CHECKOUT"))
                        {
                            if (reservationDataTable.ReservationStatus.ToUpper().Trim().Equals("DUEOUT"))
                            {
                                reservationDatas.Add(reservationDataTable);
                            }
                        }


                    }
                }
                return reservationDatas;
            }
            else
                return null;
        }
    }
}