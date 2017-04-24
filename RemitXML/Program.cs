using System;
using System.IO;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetServices;

namespace RemitXML
{
    class Program
    {
        static void SetStatus(int ID, string Status, bool isProcessed, bool hasError, string Message)
        {            
            string str_sql = @"UPDATE [REMIT_DataSourceHistory] SET 
                               [Status] = '" + Status + @"',
                               [isProcessed] = " + Convert.ToInt32(isProcessed).ToString() + @",
                               [hasError] = " + Convert.ToInt32(hasError).ToString() + @",
                               [ProcessLog] = CASE IsNull([ProcessLog], '') WHEN '' THEN '' ELSE [ProcessLog] + CHAR(13) + CHAR(10) END + '" + Message.Replace("'", "''") + @"'
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

                                //  retrieve the first unprocessed data entry
                                sql = @"SELECT TOP 1 H.* FROM [REMIT_DataSources] DS
                                        LEFT JOIN [REMIT_DataSourceHistory] H ON (DS.[ID] = H.[ID_REMIT_DataSource])
                                        WHERE (DS.[DataSourceType] = 'XLS') AND (H.[isProcessed] = 0) AND (H.[hasError] = 0)";
                                DataSet ds_input = bitSimpleSQL.Query(sql);
                                if (!bitSimpleSQL.isValidDSRows(ds_input)) continue;

                                //  extract XLS file and save it on disk for processing
                                int ID = Convert.ToInt32(ds_input.Tables[0].Rows[0]["ID"]);
                                string FileName = ds_input.Tables[0].Rows[0]["InputDataName"].ToString();
                                string ext = Path.GetExtension(FileName);

                                SetStatus(ID, "unknown", false, false, "");

                                //  check extension to eliminate garbage
                                if (ext != ".xls" && ext != ".xlsx") SetStatus(ID, "error", false, true, "extensie fisier nepermisa");
                                else if (counter % 10000 == 0)
                                {
                                    SetStatus(ID, "processing", false, false, "");

                                    string str_B64 = ds_input.Tables[0].Rows[0]["InputData"].ToString();
                                    byte[] bytes = Convert.FromBase64String(str_B64);
                                    using (FileStream fs = new FileStream("Raport" + ext, FileMode.Create))
                                    {
                                        fs.Write(bytes, 0, bytes.Length);
                                        fs.Close();
                                    }

                                    try
                                    {
                                        RemitXML remit = new RemitXML(ID);

                                        bool isProcessed = true;
                                        bool hasErrors = false;
                                        string Message = "";

                                        //  check if xml file has been generated
                                        string path = System.Reflection.Assembly.GetEntryAssembly().Location;
                                        path = path.Replace("RemitXML.exe", "");
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
    }
}
