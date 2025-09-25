using MySql.Data.MySqlClient;
using System.Data;
using System.Text;
using System.Text.Json;



public class dbServices{
    IConfiguration appsettings = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
    //MySqlConnection conn = null; // this will store the connection which will be persistent 
    private readonly Dictionary<string, string> _acceptReferalURL = new Dictionary<string, string>();
    MySqlConnection connPrimary = null; // this will store the connection which will be persistent 

    public  dbServices() // constructor
    {
       
    } 
        public string connectDBPrimary()
    {   
        try
        {
            connPrimary = new MySqlConnection(appsettings["db:connStrPrimary"]);
            connPrimary.Open();
            return "Connected";
        }
        catch (Exception ex)
        {
            //throw new ErrorEventArgs(ex); // check as this will throw exception error
            Console.WriteLine(ex);
            return ex.ToString();
        }
    }
   



    public List<List<object[]>> executeSQL(string sq, MySqlParameter[] prms)
    {
        var allTables = new List<List<object[]>>();

        try
        {
            // Ensure the connection is open
            using (var conn = new MySqlConnection(appsettings["db:connStrPrimary"])) // Use the connection string
            {
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sq;
                    if (prms != null)
                        cmd.Parameters.AddRange(prms);

                    using (var trans = conn.BeginTransaction())
                    {
                        cmd.Transaction = trans;
                        try
                        {
                            using (var dr = cmd.ExecuteReader())
                            {
                                do
                                {
                                    var tblRows = new List<object[]>();
                                    while (dr.Read())
                                    {
                                        var values = new object[dr.FieldCount];
                                        dr.GetValues(values);
                                        tblRows.Add(values);
                                    }
                                    allTables.Add(tblRows);
                                } while (dr.NextResult());
                            } 
                            trans.Commit(); // Commit the transaction if no errors
                        }
                        catch
                        {
                            trans.Rollback(); // Rollback on error 
                            conn.Close();
                            throw; // Rethrow to be handled outside
                        }
                        finally{ 
                            conn.Close();
                        }
                    }
                } 
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return null; // Handle the exception appropriately
        }

        return allTables;
    }
    
     public int ExecuteInsertAndGetLastId(string sq, MySqlParameter[] prms)
    {
        MySqlTransaction trans = null;
        int lastInsertedId = -1;

        try
        {
            if (connPrimary == null || connPrimary.State == 0)
                connectDBPrimary();

            trans = connPrimary.BeginTransaction();

            var cmd = connPrimary.CreateCommand();
            cmd.CommandText = "SET NAMES 'utf8mb4'";  // Set the character set for this session
            cmd.ExecuteNonQuery();
            cmd.Transaction = trans;
            cmd.CommandText = sq;

            if (prms != null)
                cmd.Parameters.AddRange(prms);
            cmd.ExecuteNonQuery();
            cmd.CommandText = "SELECT LAST_INSERT_ID();";
            lastInsertedId = Convert.ToInt32(cmd.ExecuteScalar());
            trans.Commit();
        }
        catch (Exception ex)
        {
            Console.Write(ex.Message);
            if (trans != null)
            {
                trans.Rollback();
            }
        }
        finally
        {
            connPrimary?.Close();
        }

        return lastInsertedId;
    }

    public List<Dictionary<string, object>[]> ExecuteSQLName(string sq, MySqlParameter[] prms)
    {
        MySqlTransaction transaction = null;
        List<Dictionary<string, object>[]> allTables = new List<Dictionary<string, object>[]>();

        try
        {
            using (var conn = new MySqlConnection(appsettings["db:connStrPrimary"])) // Use the connection string
            {
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sq;
                    if (prms != null)
                        cmd.Parameters.AddRange(prms);

                    using (var trans = conn.BeginTransaction())
                    {
                        cmd.Transaction = trans;
                        try
                        {
                            using (MySqlDataReader reader = cmd.ExecuteReader())
                            {
                                do
                                {
                                    List<Dictionary<string, object>> tblRows = new List<Dictionary<string, object>>();

                                    while (reader.Read())
                                    {
                                        Dictionary<string, object> values = new Dictionary<string, object>();

                                        for (int i = 0; i < reader.FieldCount; i++)
                                        {
                                            string columnName = reader.GetName(i);
                                            object columnValue = reader.GetValue(i);
                                            values[columnName] = columnValue;
                                        }

                                        tblRows.Add(values);
                                    }

                                    allTables.Add(tblRows.ToArray());
                                } while (reader.NextResult());
                            }

                            trans.Commit(); // Commit the transaction if no errors
                            conn.Close();
                        }
                        catch
                        {

                            trans.Rollback(); // Rollback on error
                            conn.Close();
                            throw; // Rethrow to be handled outside
                        }
                        finally
                        {
                            conn.Close();
                        }
                    }
                }

            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            transaction?.Rollback();
            //connPrimary?.Close(); 
            return null;
        }

        return allTables;
    }

        }