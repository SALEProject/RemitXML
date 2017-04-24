using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Linq;
using System.IO;
using System.Xml.XPath;
using NetServices;

namespace DB2XML
{
    class RemitNonStandard
    {
        private const string XSD = "App_Data/REMITTable1_V2.xsd";
        private string REPORT_PATH = "Raport.xml";
        private const string ERRORS_PATH = "errors.txt";

        public RemitNonStandard(int ID_Contract)
        {
            DataSet ds = bitSimpleSQL.Query(@"SELECT NSC.*, CN.[Name] AS [ContractName], CT.[Name] AS [ContractType], 
                                              C.[Code] AS [PriceCurrency], NC.[Code] AS [NotionalCurrency],
                                              MU.[Code] AS [VolumeMU], NMU.[Code] AS [TotalNotionalQuantityMU], LT.[Code] AS [LoadType]
                                              FROM [REMIT_NonStandardContracts] NSC
                                              LEFT JOIN [REMIT_ContractNames] CN ON (NSC.[ID_ContractName] = CN.[ID])
                                              LEFT JOIN [REMIT_ContractTypes] CT ON (NSC.[ID_ContractType] = CT.[ID])
                                              LEFT JOIN [Currencies] C ON (NSC.[ID_Currency] = C.[ID])
                                              LEFT JOIN [Currencies] NC ON (NSC.[ID_NotionalCurrency] = NC.[ID])
                                              LEFT JOIN [MeasuringUnits] MU ON (NSC.[ID_VolumeMU] = MU.[ID])
                                              LEFT JOIN [MeasuringUnits] NMU ON (NSC.[ID_TotalNotionalQuantityMU] = NMU.[ID])
                                              LEFT JOIN [REMIT_LoadTypes] LT ON (NSC.[ID_LoadType] = LT.[ID])  
                                              WHERE NSC.[ID] = " + ID_Contract.ToString());
            if (!bitSimpleSQL.isValidDSRows(ds)) return;

            XDocument doc = new XDocument(
                new XElement("REMITTable1",
                    //new XAttribute("xmlns", "http://www.acer.europa.eu/REMIT/REMITTable1_V2.xsd"),
                    //new XAttribute("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance"),
                    new XElement("reportingEntityID", "B00020987.RO"),
                    new XElement("contractList",
                        new XElement("contract",
                            new XElement("contractId", ds.Tables[0].Rows[0]["ContractID"].ToString()),
                            new XElement("contractName", ds.Tables[0].Rows[0]["ContractName"].ToString()),
                            new XElement("contractType", ds.Tables[0].Rows[0]["ContractType"].ToString()),
                            new XElement("energyCommodity", "NG"),
                            new XElement("settlementMethod", "P"),
                            new XElement("organizedMarketPlaceIdentifier", 
                                new XElement("mic", "XBRM")
                                ),
                            new XElement("contractTradingHours",
                                new XElement("startTime", "06:00:00Z"),
                                new XElement("endTime", "15:00:00Z")
                                ),
                            new XElement("lastTradingDateTime", "2016-01-13T13:45:00"),
                            new XElement("deliveryPointOrZone", "21Y0000000000395"),
                            new XElement("deliveryStartDate", Convert.ToDateTime(ds.Tables[0].Rows[0]["DeliveryStartDate"]).ToString("yyyy-mm-dd")),
                            new XElement("deliveryEndDate", Convert.ToDateTime(ds.Tables[0].Rows[0]["DeliveryEndDate"]).ToString("yyyy-mm-dd")),
                            new XElement("loadType", ds.Tables[0].Rows[0]["LoadType"].ToString()),
                            new XElement("deliveryProfile", 
                                new XElement("loadDeliveryStartTime", "06:00:00Z"),
                                new XElement("loadDeliveryEndTime", "06:00:00Z")
                                )
                            )
                        ),
                        new XElement("OrderList")
                    )
                    );

            doc.Save("Raport.xml");
            xsd();
        }

        public void xsd()
        {

            // Set the validation settings.
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ValidationType = ValidationType.Schema;
            settings.Schemas.Add("http://www.acer.europa.eu/REMIT/REMITTable1_V2.xsd", XSD);
            settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessInlineSchema;
            settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessSchemaLocation;
            settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
            settings.ValidationEventHandler += new ValidationEventHandler(ValidationCallBack);

            // Create the XmlReader object.
            XmlReader reader = XmlReader.Create(REPORT_PATH, settings);

            // Parse the file. 
            while (reader.Read()) ;

        }

        // Display any warnings or errors.
        private static void ValidationCallBack(object sender, ValidationEventArgs args)
        {
            if (args.Severity == XmlSeverityType.Warning)
                //Console.WriteLine("\tWarning: Matching schema not found.  No validation occurred." + args.Message);
                using (StreamWriter sw = File.AppendText(ERRORS_PATH))
                {
                    sw.WriteLine("Warning: Matching schema not found.  No validation occurred." + args.Message);
                }
            else
                //Console.WriteLine("\tValidation error: " + args.Message);
                using (StreamWriter sw = File.AppendText(ERRORS_PATH))
                {
                    sw.WriteLine("Validation error: " + args.Message);
                }

        }

    }
}
