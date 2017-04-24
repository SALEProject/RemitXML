using System;
using System.IO;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetServices;

namespace NGStorageReports2XML
{
    class Program
    {
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
                        try
                        {
                            try
                            {
                                //  delete any Raport.xls or Raport.xlsx that might exist on disk
                                if (File.Exists("Raport.xls")) File.Delete("Raport.xls");
                                if (File.Exists("Raport.xlsx")) File.Delete("Raport.xlsx");
                                if (File.Exists("errors.txt")) File.Delete("errors.txt");
                                if (File.Exists("Raport.xml")) File.Delete("Raport.xml");

                                //  retrieve the first unprocessed data entry
                                sql = @"SELECT TOP 1 H.*, SR.[ID] AS [ID_StorageReport] FROM [REMIT_DataSources] DS
                                        LEFT JOIN [REMIT_DataSourceHistory] H ON (DS.[ID] = H.[ID_REMIT_DataSource])
                                        LEFT JOIN [REMIT_StorageReports] SR ON (H.[ID] = SR.[ID_DataSourceHistory])
                                        WHERE (DS.[DataSourceType] = 'NGStorage') AND (SR.[isSubmitted] = 1)
                                        AND (H.[isProcessed] = 0) AND (H.[hasError] = 0)";
                                DataSet ds_input = bitSimpleSQL.Query(sql);
                                if (!bitSimpleSQL.isValidDSRows(ds_input)) continue;

                                //  extract XLS file and save it on disk for processing
                                int ID = Convert.ToInt32(ds_input.Tables[0].Rows[0]["ID"]);
                                int ID_StorageReport = Convert.ToInt32(ds_input.Tables[0].Rows[0]["ID_StorageReport"]);

                                SetStatus(ID, "unknown", false, false, "");

                                //  check extension to eliminate garbage
                                if (counter % 10000 == 0)
                                {
                                    SetStatus(ID, "processing", false, false, "");

                                    try
                                    {
                                        NGStorageReport remit = new NGStorageReport(ID_StorageReport);

                                        bool isProcessed = true;
                                        bool hasErrors = false;
                                        string Message = "";

                                        //  check if xml file has been generated
                                        string path = System.Reflection.Assembly.GetEntryAssembly().Location;
                                        path = path.Replace("NGStorageReports2XML.exe", "");
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
                                            string schema = "REMITStorage_V1";
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
                                }
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
        //----- end main

    }
}
