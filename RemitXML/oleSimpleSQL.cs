using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.OleDb;
using System.Xml;


namespace RemitXML
{


    public class oleSimpleSQL
    {
        public oleSimpleSQL()
        { 
        
        }

        public static OleDbConnection srv_connection = null;
	    private static string FErrorMessage = "";
        private static bool FConnected;

        public static bool Connected
        {
            get
            {
                return FConnected;
            }
        }

        public static string ErrorMessage
        {
            get
            {
                return FErrorMessage;
            }
        }

        public static bool Connect(string url)
        {
            string str = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + url + @";Extended Properties=""Excel 12.0 Xml;HDR=YES"" ";
            //"Data Source=" + host + ";Persist Security Info=True;User ID=" + user + ";Password=" + password + ";Unicode=True";
            //string str = "siac_user/siac_user@172.17.10.20:1521/SMUDA";

            try
            {
                srv_connection = new OleDbConnection(str);
                srv_connection.Open();
                FConnected = true;
                return true;
            }
            catch (Exception exc)
            {
                FErrorMessage = exc.Message;
                return false;
            }
        }

        public static void Disconnect()
        {
            if (srv_connection != null)
            {
                srv_connection.Close();
                srv_connection.Dispose();
                FConnected = false;
            }
        }

        public static DataSet Query(string str_sql)
        {
            OleDbCommand cmd = null;
            OleDbDataAdapter adapter = null;

            try
            {
                cmd = new OleDbCommand(str_sql, srv_connection);
                cmd.CommandType = CommandType.Text;

                adapter = new OleDbDataAdapter(cmd);

                try
                {
                    DataSet ds = new DataSet();
                    adapter.Fill(ds);
                    return ds;
                }
                catch (Exception exc)
                {
                    return null;
                }
            }
            finally
            {
                if (adapter != null) adapter.Dispose();
                if (cmd != null) cmd.Dispose();
            }
        }

        public static int Execute(string str_sql)
        {
            OleDbCommand cmd = null;
            try
            {
                cmd = new OleDbCommand(str_sql, srv_connection);
                try
                {
                    return cmd.ExecuteNonQuery();
                }
                catch (Exception exc)
                {
                    return -1;
                }
            }
            finally
            {
                if (cmd != null) cmd.Dispose();
            }
        }
    }
}