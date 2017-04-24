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

namespace Table1Reports2XML
{
    class Table1Report
    {
        private const string XSD = "App_Data/REMITTable1_V2.xsd";
        private string REPORT_PATH = "Raport.xml";
        private const string ERRORS_PATH = "errors.txt";

        public Table1Report(int ID_Table1Report)
        {
            File.Delete(ERRORS_PATH);
            File.Delete(REPORT_PATH);
            string path = System.Reflection.Assembly.GetEntryAssembly().Location;
            path = path.Replace("Table1Reports2XML.exe", "");
            foreach (string file in Directory.EnumerateFiles(path, "*.xml"))
                if (Path.GetFileName(file) != "DB.xml")
                {
                    File.Delete(file);
                }

            DataSet ds = bitSimpleSQL.Query(@"SELECT T1R.*, CN.[Name] AS [ContractName], CT.[Code] AS [ContractType], 
                                              PC.[Code] AS [PriceCurrency], NC.[Code] AS [NotionalCurrency], LT.[Code] AS [LoadType]
                                              FROM [REMIT_Table1Reports] T1R
                                              LEFT JOIN [REMIT_ContractNames] CN ON (T1R.[ID_ContractName] = CN.[ID])
                                              LEFT JOIN [REMIT_ContractTypes] CT ON (T1R.[ID_ContractType] = CT.[ID])
                                              LEFT JOIN [Currencies] PC ON (T1R.[ID_Currency] = PC.[ID])
                                              LEFT JOIN [Currencies] NC ON (T1R.[ID_NotionalCurrency] = NC.[ID])
                                              LEFT JOIN [REMIT_LoadTypes] LT ON (T1R.[ID_LoadType] = LT.[ID])  
                                              WHERE T1R.[ID] = " + ID_Table1Report.ToString());
            if (!bitSimpleSQL.isValidDSRows(ds)) return;

            XDocument doc = new XDocument(
                new XElement("REMITTable1",
                    new XAttribute("someattribute", "http://www.acer.europa.eu/REMIT/REMITTable1_V2.xsd"),
                    new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
                    new XElement("reportingEntityID", new XElement("ace", "B00020987.RO")),
                    /*new XElement("contractList",
                        new XElement("contract",
                            new XElement("contractId", ds.Tables[0].Rows[0]["ContractID"].ToString()),
                            new XElement("contractName", ds.Tables[0].Rows[0]["ContractName"].ToString()),
                            new XElement("contractType", ds.Tables[0].Rows[0]["ContractType"].ToString()),
                            new XElement("energyCommodity", "NG"),
                            new XElement("settlementMethod", ds.Tables[0].Rows[0]["SettlementMethod"].ToString()),
                            new XElement("organisedMarketPlaceIdentifier", 
                                new XElement("mic", "XBRM")
                                ),
                            new XElement("contractTradingHours",
                                new XElement("startTime", "06:00:00Z"),
                                new XElement("endTime", "15:00:00Z")
                                ),
                            new XElement("lastTradingDateTime", "2016-01-13T13:45:00"),
                            new XElement("deliveryPointOrZone", "21Y0000000000395"),
                            new XElement("deliveryStartDate", Convert.ToDateTime(ds.Tables[0].Rows[0]["DeliveryStartDate"]).ToString("yyyy-MM-dd")),
                            new XElement("deliveryEndDate", Convert.ToDateTime(ds.Tables[0].Rows[0]["DeliveryEndDate"]).ToString("yyyy-MM-dd")),
                            new XElement("loadType", ds.Tables[0].Rows[0]["LoadType"].ToString()),
                            new XElement("deliveryProfile", 
                                new XElement("loadDeliveryStartTime", "06:00:00Z"),
                                new XElement("loadDeliveryEndTime", "06:00:00Z")
                                )
                            )
                        ),*/
                        //new XElement("OrderList", writeOrders(ID_Table1Report)),
                        new XElement("TradeList", writeTrades(ID_Table1Report))
                    ));

            doc.Save(REPORT_PATH);
            string text = File.ReadAllText(@REPORT_PATH);
            text = text.Replace("someattribute", "xmlns");
            File.WriteAllText(@REPORT_PATH, text);
            xsd();
        }

        public XElement[] writeOrders(int ID_Table1Report)
        {
            XElement[] nodes = new XElement[1];
            nodes[0] = new XElement("OrderReport"/*,
                new XElement("RecordSeqNumber",),
                new XElement("idOfMarketParticipant", writeIdentifier("", "", "ACER")),
                new XElement("tradingCapacity", ),
                new XElement("buySellIndicator", ),
                new XElement("orderId", ),
                new XElement("orderType", )*/
                );


            nodes[1] = new XElement("OrderReport");

            return nodes;
        }

        public XElement[] writeTrades(int ID_Table1Report)
        {
            DataSet ds = bitSimpleSQL.Query(@"SELECT T1R.*, CN.[Name] AS [ContractName], CT.[Code] AS [ContractType], 
                                              PC.[Code] AS [PriceCurrency], NC.[Code] AS [NotionalCurrency], LT.[Code] AS [LoadType]
                                              FROM [REMIT_Table1Reports] T1R
                                              LEFT JOIN [REMIT_ContractNames] CN ON (T1R.[ID_ContractName] = CN.[ID])
                                              LEFT JOIN [REMIT_ContractTypes] CT ON (T1R.[ID_ContractType] = CT.[ID])
                                              LEFT JOIN [Currencies] PC ON (T1R.[ID_Currency] = PC.[ID])
                                              LEFT JOIN [Currencies] NC ON (T1R.[ID_NotionalCurrency] = NC.[ID])
                                              LEFT JOIN [REMIT_LoadTypes] LT ON (T1R.[ID_LoadType] = LT.[ID])  
                                              WHERE T1R.[ID] = " + ID_Table1Report.ToString());
            if (!bitSimpleSQL.isValidDSRows(ds)) return null;
            DataRow row = ds.Tables[0].Rows[0];

            XElement[] nodes = new XElement[1];
            nodes[0] = new XElement("TradeReport",
                new XElement("RecordSeqNumber", 1),
                new XElement("idOfMarketParticipant", writeIdentifier(row["ParticipantIdentifier"].ToString(), row["ParticipantIdentifierType"].ToString(), "ACER")),
                new XElement("otherMarketParticipant", writeIdentifier(row["CounterpartIdentifier"].ToString(), row["CounterpartIdentifierType"].ToString(), "ACER")),
                (row["BeneficiaryIdentifier"].ToString().Trim() != "" ? new XElement("beneficiaryIdentification", writeIdentifier(row["BeneficiaryIdentifier"].ToString(), row["BeneficiaryIdentifierType"].ToString(), "ACER")) : null),
                new XElement("tradingCapacity", "P"),
                new XElement("buySellIndicator", row["BuySellIndicator"].ToString()),
                new XElement("contractInfo",
                    new XElement("contract",
                        new XElement("contractId", "NA"/*row["ContractID"].ToString()*/),
                        new XElement("contractName", row["ContractName"].ToString()),
                        new XElement("contractType", row["ContractType"].ToString()),
                        new XElement("energyCommodity", "NG"),
                        new XElement("settlementMethod", row["SettlementMethod"].ToString()),
                        new XElement("organisedMarketPlaceIdentifier", new XElement("bil", "XBIL")),
                        new XElement("deliveryPointOrZone", row["DeliveryPoint"].ToString()),
                        new XElement("deliveryStartDate", ((DateTime)row["DeliveryStartDate"]).ToString("yyyy-MM-dd")),
                        new XElement("deliveryEndDate", ((DateTime)row["DeliveryEndDate"]).ToString("yyyy-MM-dd")),
                        new XElement("loadType", row["LoadType"].ToString()),
                        new XElement("deliveryProfile",
                            new XElement("loadDeliveryStartTime", "06:00:00"),
                            new XElement("loadDeliveryEndTime", "05:59:00")
                            )
                        )
                        ),
                new XElement("organisedMarketPlaceIdentifier", new XElement("bil", "XBIL")),
                new XElement("transactionTime", ((DateTime)row["TransactionTimestamp"]).ToString("yyyy-MM-ddThh:mm:ss")),
                new XElement("uniqueTransactionIdentifier", new XElement("uniqueTransactionIdentifier", row["TransactionID"].ToString())),
                //<linkedTransactionId>q1w2e3r4t5</linkedTransactionId>
                row["LinkedTransactionID"].ToString().Trim() != "" ? new XElement("linkedTransactionId", row["LinkedTransactionID"].ToString()) : null,
                new XElement("priceDetails", writePrice(Convert.ToDouble(row["Price"]), row["PriceCurrency"].ToString(), "RON")),
                new XElement("notionalAmountDetails", writeNotionalAmount(Convert.ToDouble(row["NotionalAmount"]), row["NotionalCurrency"].ToString(), "RON")),
                new XElement("quantity", writeQuantity(Convert.ToDouble(row["Volume"]), row["VolumeMU"].ToString(), "MWh")),
                new XElement("totalNotionalContractQuantity", writeQuantity(Convert.ToDouble(row["TotalNotionalQuantity"]), row["TotalNotionalQuantityMU"].ToString(), "MWh")),
                /*new XElement("priceIntervalQuantityDetails",
                    new XElement("intervalStartDate", ((DateTime)row["DeliveryStartDate"]).ToString("yyyy-MM-dd")),
                    new XElement("intervalEndDate", ((DateTime)row["DeliveryEndDate"]).ToString("yyyy-MM-dd")),
                    new XElement("intervalStartTime", "06:00:00"),
                    new XElement("intervalEndTime", "05:59:00")
                    ),*/
                new XElement("actionType", row["ActionType"].ToString())
                );                

            return nodes;
        }

        public XElement writeIdentifier(string Identifier, string IdentifierType, string defaultType)
        {
            if (IdentifierType == "") IdentifierType = defaultType;
            switch (IdentifierType)
            {
                case "ACER":
                    return new XElement("ace", Identifier.Trim());
                case "EIC":
                    return new XElement("eic", Identifier.Trim());
                case "LEI":
                    return new XElement("lei", Identifier.Trim());
                default:
                    return new XElement("invalid", Identifier.Trim());
            }
        }

        public XElement[] writePrice(double value, string Currency, string defaultCurrency)
        {
            if (Currency == "") Currency = defaultCurrency;

            XElement[] res = new XElement[2];
            res[0] = new XElement("price", value.ToString("F5"));
            res[1] = new XElement("priceCurrency", Currency.Trim());

            return res;
        }

        public XElement[] writeNotionalAmount(double value, string Currency, string defaultCurrency)
        {
            if (Currency == "") Currency = defaultCurrency;

            XElement[] res = new XElement[2];
            res[0] = new XElement("notionalAmount", value.ToString("F5"));
            res[1] = new XElement("notionalCurrency", Currency.Trim());

            return res;
        }

        public XElement[] writeQuantity(double value, string unit, string defaultUnit)
        {
            if (unit == "") unit = defaultUnit;

            XElement[] res = new XElement[2];
            res[0] = new XElement("value", value.ToString("F5"));
            res[1] = new XElement("unit", unit.Trim());

            return res;
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

            try
            {
                // Parse the file. 
                while (reader.Read()) ;
            }
            finally
            {
                reader.Dispose();
            }
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
