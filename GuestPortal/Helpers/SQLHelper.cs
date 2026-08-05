using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace CheckinPortal.Helpers
{
    internal sealed class SQLHelpers
    {
        private static readonly Lazy<SQLHelpers>
           lazy = new Lazy<SQLHelpers>(() => new SQLHelpers());

        public static SQLHelpers Instance { get { return lazy.Value; } }

        private string ConnectionString;

        public SQLHelpers()
        {

        }

        public void SetConnectionString(string connectionString)
        {
            ConnectionString = connectionString;

        }


        public bool ValidateConnection()
        {
            using (SqlConnection con = new SqlConnection(this.ConnectionString))
            {
                DataTable resultTable = new DataTable();
                try
                {
                    con.Open();
                    return true;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    con.Close();
                }
            }
        }

        public DataTable ExecuteSP(string SPName, params SqlParameter[] parameters)
        {
            using (SqlConnection con = new SqlConnection(this.ConnectionString))
            {
                DataTable resultTable = new DataTable();
                try
                {
                    using (SqlCommand cmd = new SqlCommand(SPName, con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = SPName;

                        cmd.Parameters.Clear();
                        if (parameters != null && parameters.Length > 0)
                            cmd.Parameters.AddRange(parameters);

                        con.Open();
                        SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                        adapter.Fill(resultTable);
                    }
                    return resultTable;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    con.Close();
                }
            }
        }

        public DataTable ExecuteSP(string SPName)
        {
            using (SqlConnection con = new SqlConnection(this.ConnectionString))
            {
                DataTable resultTable = new DataTable();
                try
                {
                    using (SqlCommand cmd = new SqlCommand(SPName, con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = SPName;

                        con.Open();
                        SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                        adapter.Fill(resultTable);
                    }
                    return resultTable;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    con.Close();
                }
            }
        }


        public DataTable ExecuteSP(string SPName, List<SqlParameter> parameters)
        {
            using (SqlConnection con = new SqlConnection(this.ConnectionString))
            {
                DataTable resultTable = new DataTable();
                try
                {
                    using (SqlCommand cmd = new SqlCommand(SPName, con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = SPName;

                        cmd.Parameters.Clear();
                        foreach (var parameter in parameters)
                        {
                            cmd.Parameters.Add(parameter);
                        }
                        con.Open();
                        SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                        adapter.Fill(resultTable);
                    }
                    return resultTable;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    con.Close();
                }
            }
        }


        public int ExecuteBulkCopy(string tableName, DataTable dataTable)
        {
            using (SqlConnection con = new SqlConnection(this.ConnectionString))
            {
                DataTable resultTable = new DataTable();

                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(con, SqlBulkCopyOptions.TableLock, null))
                {
                    bulkCopy.BatchSize = 1000;
                    bulkCopy.BulkCopyTimeout = 0;
                    //bulkCopy.SqlRowsCopied += new SqlRowsCopiedEventHandler(OnSqlRowsCopied);
                    bulkCopy.NotifyAfter = 3000;
                    bulkCopy.DestinationTableName = tableName;
                    try
                    {
                        con.Open();
                        bulkCopy.WriteToServer(dataTable);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        return 0;
                    }
                    finally
                    {
                    }
                }
            }

            return 1;
        }

        public object ExecuteScalar(string query)
        {
            using (SqlConnection con = new SqlConnection(this.ConnectionString))
            {
                DataTable resultTable = new DataTable();
                try
                {
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.Clear();
                        con.Open();
                        var result = cmd.ExecuteScalar();
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    con.Close();
                }
            }
        }

        public async Task<int> ExecuteNonQuery(string query)
        {
            using (SqlConnection con = new SqlConnection(this.ConnectionString))
            {
                DataTable resultTable = new DataTable();
                try
                {
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.Clear();
                        con.Open();
                        var result = await cmd.ExecuteNonQueryAsync();
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    con.Close();
                }
            }
        }

        public DataTable ExecuteDataset(string query)
        {
            using (SqlConnection con = new SqlConnection(this.ConnectionString))
            {
                DataTable resultTable = new DataTable();
                try
                {
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.Clear();
                        con.Open();
                        SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                        adapter.Fill(resultTable);
                        return resultTable;
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    con.Close();
                }
            }
        }

        //public DataTable ExecuteVicasSP(string SPName, params SqlParameter[] parameters)
        //{
        //    using (SqlConnection con = new SqlConnection(this.VicasConnectionString))
        //    {
        //        DataTable resultTable = new DataTable();
        //        try
        //        {
        //            using (SqlCommand cmd = new SqlCommand(SPName, con))
        //            {
        //                cmd.CommandType = CommandType.StoredProcedure;
        //                cmd.CommandText = SPName;

        //                cmd.Parameters.Clear();
        //                if (parameters != null && parameters.Length > 0)
        //                    cmd.Parameters.AddRange(parameters);

        //                con.Open();
        //                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
        //                adapter.Fill(resultTable);
        //            }
        //            return resultTable;
        //        }
        //        catch (Exception ex)
        //        {
        //            throw ex;
        //        }
        //        finally
        //        {
        //            con.Close();
        //        }
        //    }

        //}

        //public DataTable ExecuteVicasDataset(string query)
        //{
        //    using (SqlConnection con = new SqlConnection(this.VicasConnectionString))
        //    {
        //        DataTable resultTable = new DataTable();
        //        try
        //        {
        //            using (SqlCommand cmd = new SqlCommand(query, con))
        //            {
        //                cmd.CommandType = CommandType.Text;
        //                cmd.Parameters.Clear();
        //                con.Open();
        //                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
        //                adapter.Fill(resultTable);
        //                return resultTable;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            throw ex;
        //        }
        //        finally
        //        {
        //            con.Close();
        //        }
        //    }
        //}

    }
}