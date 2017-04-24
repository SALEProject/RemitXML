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
    class Table2Report
    {
        private const string XSD = "App_Data/REMITTable2_V1.xsd";
        private string REPORT_PATH = "Raport.xml";
        private const string ERRORS_PATH = "errors.txt";

        public Table2Report(int ID_Table2Report)
        {
            File.Delete(ERRORS_PATH);
            File.Delete(REPORT_PATH);
            string path = System.Reflection.Assembly.GetEntryAssembly().Location;
            path = path.Replace("Table2Reports2XML.exe", "");
            foreach (string file in Directory.EnumerateFiles(path, "*.xml"))
                if (Path.GetFileName(file) != "DB.xml")
                {
                    File.Delete(file);
                }

            DataSet ds = bitSimpleSQL.Query(@"SELECT T2R.*
                                              FROM [REMIT_Table2Reports] T2R
                                              WHERE T2R.[ID] = " + ID_Table2Report.ToString());
            if (!bitSimpleSQL.isValidDSRows(ds)) return;

            XDocument doc = new XDocument(
                new XElement("REMITTable2",
                    new XAttribute("someattribute", "http://www.acer.europa.eu/REMIT/REMITTable2_V1.xsd"),
                    new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
                    new XElement("reportingEntityID", new XElement("ace", "B00020987.RO")),
                    new XElement("TradeList", 
                        writeNonStandardContractReport(ID_Table2Report)
                        )
                    //new XElement("submissionDateTime", ((DateTime)ds.Tables[0].Rows[0]["SubmitTimestamp"]).ToString("yyyy-MM-ddThh:mm:ss")),
                    ));

            doc.Save(REPORT_PATH);
            string text = File.ReadAllText(@REPORT_PATH);
            text = text.Replace("someattribute", "xmlns");
            File.WriteAllText(@REPORT_PATH, text);
            xsd();
        }

        public XElement[] writeNonStandardContractReport(int ID_Table2Report)
        {
            DataSet ds = bitSimpleSQL.Query(@"SELECT T2R.*, CT.[Code] AS [ContractType], LT.[Code] AS [LoadType], PC.[Code] AS [PriceCurrency], NC.[Code] AS [NotionalCurrency]
                                              FROM [REMIT_Table2Reports] T2R 
                                              LEFT JOIN [REMIT_ContractTypes] CT ON (T2R.[ID_ContractType] = CT.[ID])
                                              LEFT JOIN [REMIT_LoadTypes] LT ON (T2R.[ID_LoadType] = LT.[ID])
                                              LEFT JOIN [Currencies] PC ON (T2R.[ID_Currency] = PC.[ID])
                                              LEFT JOIN [Currencies] NC ON (T2R.[ID_NotionalCurrency] = NC.[ID])
                                              WHERE T2R.[ID] = " + ID_Table2Report.ToString());
            if (!bitSimpleSQL.isValidDSRows(ds)) return null;
            DataRow row = ds.Tables[0].Rows[0];

            XElement[] nodes = new XElement[1];
            nodes[0] = new XElement("nonStandardContractReport",
                new XElement("RecordSeqNumber", 1),
                new XElement("idOfMarketParticipant", writeIdentifier(row["ParticipantIdentifier"].ToString(), row["ParticipantIdentifierType"].ToString(), "ACER")),
                new XElement("otherMarketParticipant", writeIdentifier(row["OtherParticipantIdentifier"].ToString(), row["OtherParticipantIdentifierType"].ToString(), "ACER")),
                (row["BeneficiaryIdentifier"].ToString().Trim() != "" ? new XElement("beneficiaryIdentification", writeIdentifier(row["BeneficiaryIdentifier"].ToString(), row["BeneficiaryIdentifierType"].ToString(), "ACER")) : null),
                new XElement("tradingCapacity", row["TradingCapacityType"].ToString()),
                new XElement("buySellIndicator", row["BuySellIndicator"].ToString()),
                new XElement("contractId", row["ContractID"].ToString()),
                new XElement("contractDate", ((DateTime)row["ContractDate"]).ToString("yyyy-MM-dd")),
                new XElement("contractType", row["ContractType"].ToString()),
                new XElement("energyCommodity", "NG"),
                new XElement("priceOrPriceFormula", 
                    Convert.ToDouble(row["Price"]) != 0 ? new XElement("price", writePrice(Convert.ToDouble(row["Price"]), row["PriceCurrency"].ToString(), "RON")) : new XElement("priceFormula", row["PriceFormula"].ToString())
                    ),
                row["EstimatedNotionalAmount"] == DBNull.Value ? null : new XElement("estimatedNotionalAmount", writePrice(Convert.ToDouble(row["EstimatedNotionalAmount"]), row["NotionalCurrency"].ToString(), "RON")),
                row["TotalNotionalQuantity"] == DBNull.Value ? null : new XElement("totalNotionalContractQuantity", writeQuantity(Convert.ToDouble(row["TotalNotionalQuantity"]), row["TotalNotionalQuantityMU"].ToString(), "MWh")),
                new XElement("volumeOptionality", row["VolumeOptionality"].ToString()),
                new XElement("volumeOptionalityFrequency", row["VolumeOptionalityFrequency"].ToString()),
                writeVolumeOptionalityIntervals(ID_Table2Report),
                new XElement("typeOfIndexPrice", row["TypeOfIndexPrice"].ToString()),
                writeFixingIndexDetails(ID_Table2Report),
                new XElement("settlementMethod", row["SettlementMethod"].ToString()),
                //new XElement("optionDetails", ),
                writeDeliveryPointOrZone(row["DeliveryPoint"].ToString()),
                //new XElement("deliveryPointOrZone", row["DeliveryPoint"].ToString()),
                new XElement("deliveryStartDate", ((DateTime)row["DeliveryStartDate"]).ToString("yyyy-MM-dd")),
                new XElement("deliveryEndDate", ((DateTime)row["DeliveryEndDate"]).ToString("yyyy-MM-dd")),
                new XElement("loadType", row["LoadType"].ToString()),
                new XElement("actionType", row["ActionType"].ToString())
                );

            return nodes;
        }

        public XElement[] writeDeliveryPointOrZone(string DeliveryPoint)
        {
            string[] points = DeliveryPoint.Split(',');

            XElement[] nodes = new XElement[points.Length];
            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i] = new XElement("deliveryPointOrZone", points[i].Trim());
            }

            return nodes;
        }

        public XElement[] writeVolumeOptionalityIntervals(int ID_Table2Report)
        {
            DataSet ds = bitSimpleSQL.Query("SELECT * FROM [REMIT_Table2VolumeOptionalities] WHERE [ID_Table2Report] = " + ID_Table2Report.ToString());
            if (!bitSimpleSQL.isValidDSRows(ds)) return null;

            XElement[] nodes = new XElement[ds.Tables[0].Rows.Count];
            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                DataRow row = ds.Tables[0].Rows[i];

                nodes[i] = new XElement("volumeOptionalityIntervals",
                    new XElement("capacity", writeQuantity(Convert.ToDouble(row["Capacity"]), row["CapacityMU"].ToString(), "MW")),
                    new XElement("startDate", ((DateTime)row["StartDate"]).ToString("yyyy-MM-dd")),
                    new XElement("endDate", ((DateTime)row["EndDate"]).ToString("yyyy-MM-dd"))
                    );
            }

            return nodes;
        }

        public XElement[] writeFixingIndexDetails(int ID_Table2Report)
        {
            DataSet ds = bitSimpleSQL.Query(@"SELECT FID.*, CT.[Code] AS [FixingIndexContractType] FROM [REMIT_Table2FixingIndexDetails] FID 
                                              LEFT JOIN [REMIT_ContractTypes] CT ON (FID.[ID_FixingIndexContractType] = CT.[ID]) 
                                              WHERE FID.[ID_Table2Report] = " + ID_Table2Report.ToString());
            if (!bitSimpleSQL.isValidDSRows(ds)) return null;

            XElement[] nodes = new XElement[ds.Tables[0].Rows.Count];
            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                DataRow row = ds.Tables[0].Rows[i];

                nodes[i] = new XElement("fixingIndexDetails",
                    new XElement("fixingIndex", row["FixingIndex"].ToString()),
                    new XElement("fixingIndexType", row["FixingIndexContractType"].ToString()),
                    new XElement("fixingIndexSource", row["FixingIndexSource"].ToString()),
                    new XElement("firstFixingDate", ((DateTime)row["FirstFixingDate"]).ToString("yyyy-MM-dd")),
                    new XElement("lastFixingDate", ((DateTime)row["LastFixingDate"]).ToString("yyyy-MM-dd")),
                    new XElement("fixingFrequency", row["FixingFrequency"].ToString())
                    );
            }

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
            res[0] = new XElement("value", value.ToString("F5"));
            res[1] = new XElement("currency", Currency.Trim());

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
            settings.Schemas.Add("http://www.acer.europa.eu/REMIT/REMITTable2_V1.xsd", XSD);
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
