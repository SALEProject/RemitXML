using System;
using System.IO;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using ARISClient.ARISWSTestFramework;
using NetServices;

namespace ARISClient
{
    class Program
    {
        static void AddTask(int ID_DataSourceHistory, string Task)
        {
            string str_sql =  @"INSERT INTO [REMIT_ARISActivity]
                                ([ID_DataSourceHistory], [Task], [Status], [Log])
                                VALUES
                                (" + ID_DataSourceHistory.ToString() + @", '" + Task + @"', 'unknown', '')
                               ";
            bitSimpleSQL.Execute(str_sql);
            Console.WriteLine(DateTime.Now.ToString() + " Task added - " + Task);
        }

        static void UpdateTask(int ID, string Status, string Message)
        {
            string str_sql =  @"UPDATE [REMIT_ARISActivity] SET
                                [Status] = '" + Status + @"',
                                [Log] = [Log] + '" + Message + @"' 
                                WHERE [ID] = " + ID.ToString();
            bitSimpleSQL.Execute(str_sql);
            Console.WriteLine(DateTime.Now.ToString() + " Task updated - " + Message);            
        }

        static void SetStatus(int ID, string Status, bool isProcessed, bool hasError, string Message)
        {
            string str_msg = Message;
            if (Message.Length > 960) str_msg = Message.Substring(0, 960);

            string str_sql = @"UPDATE [REMIT_DataSourceHistory] SET 
                               [Status] = '" + Status + @"',
                               [isProcessed] = " + Convert.ToInt32(isProcessed).ToString() + @",
                               [hasError] = " + Convert.ToInt32(hasError).ToString() + @",
                               [ProcessLog] = CASE IsNull([ProcessLog], '') WHEN '' THEN '' ELSE [ProcessLog] + CHAR(13) + CHAR(10) END + '" + str_msg.Replace("'", "''") + @"'
                               WHERE [ID] = " + ID.ToString();
            bitSimpleSQL.Execute(str_sql);
        }

        static void Main(string[] args)
        {
            string sql = "";
            int counter = 0;

            while (!Console.KeyAvailable)
            {
                System.Threading.Thread.Sleep(100);
                counter += 100;
                if (counter >= 300000) counter = 0;

                if (counter % 2000 == 0)
                {
                    if (bitSimpleSQL.Connect("DB.xml"))
                    {
                        ARISWS WS = new ARISWS();
                        WS.initWS();

                        try
                        {
                            try
                            {
                                //  delete any Raport.xls or Raport.xlsx that might exist on disk
                                if (File.Exists("Raport.xls")) File.Delete("Raport.xls");
                                if (File.Exists("Raport.xlsx")) File.Delete("Raport.xlsx");
                                if (File.Exists("errors.txt")) File.Delete("errors.txt");
                                if (File.Exists("Raport.xml")) File.Delete("Raport.xml");

                                //  obtain current number of tasks
                                sql = @"SELECT Count(*) AS [Count] FROM [REMIT_ARISActivity]";
                                DataSet ds_count = bitSimpleSQL.Query(sql);
                                if (!bitSimpleSQL.isValidDSRows(ds_count)) continue;
                                int tasks_count = Convert.ToInt32(ds_count.Tables[0].Rows[0]["Count"]);

                                if (tasks_count < 5)
                                {
                                    //  create a new task
                                    sql = @"SELECT TOP 1 DS.[DataSourceType], DSH.* FROM [REMIT_DataSources] DS
                                            LEFT JOIN [REMIT_DataSourceHistory] DSH ON (DS.[ID] = DSH.[ID_REMIT_DataSource])
                                            LEFT JOIN [REMIT_ARISActivity] T ON (DSH.[ID] = T.[ID_DataSourceHistory])
                                            WHERE (DS.[isActive] = 1) AND (DSH.[isProcessed] = 1) AND (DSH.[hasError] = 0)
                                            AND (IsNull([ReceiptDataName], '') = '') AND (T.[ID] IS NULL)
                                           ";
                                    DataSet ds_histories = bitSimpleSQL.Query(sql);
                                    if (bitSimpleSQL.isValidDSRows(ds_histories))
                                    {
                                        AddTask(Convert.ToInt32(ds_histories.Tables[0].Rows[0]["ID"]), ds_histories.Tables[0].Rows[0]["DataSourceType"].ToString() + ": " + ds_histories.Tables[0].Rows[0]["OutputDataName"].ToString());
                                    }
                                }

                                //  get tasks and follow them
                                sql = @"SELECT * FROM [REMIT_ARISActivity] ACT
                                        LEFT JOIN [REMIT_DataSourceHistory] DSH ON (ACT.[ID_DataSourceHistory] = DSH.[ID])
                                        ";
                                using (DataSet ds_tasks = bitSimpleSQL.Query(sql))
                                {
                                    if (!bitSimpleSQL.isValidDS(ds_tasks)) continue;
                                    foreach (DataRow row in ds_tasks.Tables[0].Rows)
                                    {
                                        int ID_Task = Convert.ToInt32(row["ID"]);
                                        string filename = row["OutputDataName"].ToString();
                                        string WSresult = "";

                                        if ((DateTime.Now - Convert.ToDateTime(row["Date_LastCheck"])).TotalMilliseconds > 10000)
                                        switch (row["Status"].ToString())
                                        {
                                            case "unknown":
                                            case "name_check":
                                                UpdateTask(ID_Task, "name_check", "Validating name convention");
                                                WSresult = WS.NameConvetionCheck("20160826_REMITTable1_V2_B00020987.RO_1.xml.asc.pgp");
                                                if (WSresult == "") UpdateTask(ID_Task, "name_check", "failed. Message: " + WS.str_log);
                                                break;
                                            case "file_upload":
                                                break;
                                            case "receipt":
                                                WS.ElaborationStatus("fisier");
                                                break;
                                            default:
                                                break;
                                        }
                                    }
                                }

                                /*//  retrieve the first unprocessed data entry
                                sql = @"SELECT TOP 1 H.*, T1R.[ID] AS [ID_Table1Report] FROM [REMIT_DataSources] DS
                                        LEFT JOIN [REMIT_DataSourceHistory] H ON (DS.[ID] = H.[ID_REMIT_DataSource])
                                        LEFT JOIN [REMIT_Table1Reports] T1R ON (H.[ID] = T1R.[ID_DataSourceHistory])
                                        WHERE (DS.[DataSourceType] = 'Table1') AND (T1R.[isSubmitted] = 1)
                                        AND (H.[isProcessed] = 0) AND (H.[hasError] = 0)";
                                DataSet ds_input = bitSimpleSQL.Query(sql);
                                if (!bitSimpleSQL.isValidDSRows(ds_input)) continue;

                                //  extract XLS file and save it on disk for processing
                                int ID = Convert.ToInt32(ds_input.Tables[0].Rows[0]["ID"]);
                                int ID_Table1Report = Convert.ToInt32(ds_input.Tables[0].Rows[0]["ID_Table1Report"]);

                                SetStatus(ID, "unknown", false, false, "");

                                //  check extension to eliminate garbage
                                if (counter % 10000 == 0)
                                {
                                    SetStatus(ID, "processing", false, false, "");

                                    try
                                    {
                                        Table1Report remit = new Table1Report(ID_Table1Report);

                                        bool isProcessed = true;
                                        bool hasErrors = false;
                                        string Message = "";

                                        //  check if xml file has been generated
                                        string path = System.Reflection.Assembly.GetEntryAssembly().Location;
                                        path = path.Replace("Table1Reports2XML.exe", "");
                                        foreach (string file in Directory.EnumerateFiles(path, "*.xml"))
                                            if (Path.GetFileName(file) != "DB.xml")
                                            {
                                                //  a file has been found
                                                byte[] xml_bytes = File.ReadAllBytes(file);
                                                if (xml_bytes.Length > 100)
                                                {
                                                    string OutputData = Convert.ToBase64String(xml_bytes);
                                                    string OutputDataName = Path.GetFileName(file);

                                                    sql = @"UPDATE [REMIT_DataSourceHistory] SET 
                                                            [OutputDataName] = '" + OutputDataName + @"',
                                                            [OutputData] = '" + OutputData + @"'
                                                            WHERE [ID] = " + ID.ToString();
                                                    bitSimpleSQL.Execute(sql);
                                                }
                                            }

                                        //  check if errors.txt exists and if the xml file has been generated
                                        if (File.Exists("errors.txt"))
                                        {
                                            hasErrors = true;
                                            string[] errors = File.ReadAllLines("errors.txt");
                                            for (int i = 0; i < errors.Length; i++)
                                                if (i == 0) Message = errors[i];
                                                else Message += ((char)13).ToString() + ((char)10).ToString() + errors[i];
                                        }
                                        else
                                        {
                                            //  generate file id
                                            string str_date = DateTime.Today.ToString("yyyyMMdd");
                                            string schema = "REMITTable1_V2";
                                            string RRM = "B00020987.RO";
                                            int FileID = 1;

                                            sql = "SELECT * FROM [REMIT_SequenceGenerator] WHERE ([Date] = '" + str_date + "') AND ([Schema] = '" + schema + "')";
                                            DataSet ds_FileID = bitSimpleSQL.Query(sql);
                                            if (bitSimpleSQL.isValidDS(ds_FileID))
                                            {
                                                if (ds_FileID.Tables[0].Rows.Count == 0)
                                                {
                                                    sql = "INSERT INTO [REMIT_SequenceGenerator] ([Date], [Schema], [FileID]) VALUES ('" + str_date + "', '" + schema + "', " + FileID.ToString() + ")";
                                                    bitSimpleSQL.Execute(sql);
                                                }
                                                else
                                                {
                                                    FileID = Convert.ToInt32(ds_FileID.Tables[0].Rows[0]["FileID"]) + 1;
                                                    sql = "UPDATE [REMIT_SequenceGenerator] SET [FileID] = " + FileID + " WHERE ([Date] = '" + str_date + "') AND ([Schema] = '" + schema + "')";
                                                    bitSimpleSQL.Execute(sql);
                                                }

                                                string RepName = str_date + "_" + schema + "_" + RRM + "_" + FileID.ToString() + ".xml";
                                                sql = "UPDATE [REMIT_DataSourceHistory] SET [OutputDataName] = '" + RepName + "' WHERE [ID] = " + ID.ToString();
                                                bitSimpleSQL.Execute(sql);
                                            }
                                        }

                                        SetStatus(ID, "finalized", isProcessed, hasErrors, Message);
                                    }
                                    catch (Exception exc)
                                    {
                                        SetStatus(ID, "finalized", true, true, exc.Message);
                                    }
                                }*/
                            }
                            catch (Exception exc)
                            {
                            }
                        }
                        finally
                        {
                            bitSimpleSQL.Disconnect();
                        }
                    }
                }
            }
                        
        }
    }
}
