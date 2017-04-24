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

namespace NGStorageReports2XML
{
    class NGStorageReport
    {
        private const string XSD = "App_Data/REMITStorageSchema_V1.xsd";
        private string REPORT_PATH = "Raport.xml";
        private const string ERRORS_PATH = "errors.txt";

        public NGStorageReport(int ID_StorageReport)
        {
            File.Delete(ERRORS_PATH);
            File.Delete(REPORT_PATH);
            string path = System.Reflection.Assembly.GetEntryAssembly().Location;
            path = path.Replace("NGStorageReports2XML.exe", "");
            foreach (string file in Directory.EnumerateFiles(path, "*.xml"))
                if (Path.GetFileName(file) != "DB.xml")
                {
                    File.Delete(file);
                }

            DataSet ds = bitSimpleSQL.Query(@"SELECT SR.*
                                              FROM [REMIT_StorageReports] SR
                                              WHERE SR.[ID] = " + ID_StorageReport.ToString());
            if (!bitSimpleSQL.isValidDSRows(ds)) return;

            XDocument doc = new XDocument(
                new XElement("REMITStorageReport",
                    new XAttribute("someattribute", "http://www.acer.europa.eu/REMIT/REMITStorageSchema_V1.xsd"),
                    new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
                    new XElement("reportingEntityIdentifier", new XElement("acerCode", "B00020987.RO")),
                    new XElement("submissionDateTime", ((DateTime)ds.Tables[0].Rows[0]["SubmitTimestamp"]).ToString("yyyy-MM-ddThh:mm:ss")),
                    writeStorageFacilityReports(ID_StorageReport),
                    writeStorageParticipantActivityReports(ID_StorageReport),
                    writeStorageUnavailabilityReports(ID_StorageReport)
                    ));

            doc.Save(REPORT_PATH);
            string text = File.ReadAllText(@REPORT_PATH);
            text = text.Replace("someattribute", "xmlns");
            File.WriteAllText(@REPORT_PATH, text);
            xsd();
        }

        public XElement[] writeStorageFacilityReports(int ID_StorageReport)
        {
            DataSet ds = bitSimpleSQL.Query("SELECT * FROM [REMIT_StorageFacilityReports] WHERE [ID_StorageReport] = " + ID_StorageReport.ToString());

            XElement[] nodes = new XElement[ds.Tables[0].Rows.Count];
            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                DataRow row = ds.Tables[0].Rows[i];

                nodes[i] = new XElement("storageFacilityReport",
                    new XElement("gasDayStart", ((DateTime)row["gasDayStart"]).ToString("yyyy-MM-ddThh:mm:ss")),
                    new XElement("gasDayEnd", ((DateTime)row["gasDayEnd"]).ToString("yyyy-MM-ddThh:mm:ss")),
                    new XElement("storageFacilityIdentifier", writeIdentifier(row["StorageFacilityIdentifier"].ToString(), row["StorageFacilityIdentifierType"].ToString(), "ACER")),
                    new XElement("storageFacilityOperatorIdentifier", writeIdentifier(row["StorageFacilityOperatorIdentifier"].ToString(), row["StorageFacilityOperatorIdentifierType"].ToString(), "ACER")),
                    new XElement("reportingEntityReferenceID", "NGSTORFREP" + row["ID"].ToString()/*row["ReferenceID"].ToString()*/),
                    new XElement("storageType", row["StorageType"].ToString()),
                    new XElement("storage", writeQuantity(Convert.ToDouble(row["Storage"]), row["StorageMU"].ToString(), "TWh")),
                    new XElement("injection", writeQuantity(Convert.ToDouble(row["Injection"]), row["InjectionMU"].ToString(), "GWh/d")),
                    new XElement("withdrawal", writeQuantity(Convert.ToDouble(row["Withdrawal"]), row["WithdrawalMU"].ToString(), "GWh/d")),
                    new XElement("technicalCapacity", writeQuantity(Convert.ToDouble(row["TechnicalCapacity"]), row["TechnicalCapacityMU"].ToString(), "TWh")),
                    new XElement("contractedCapacity", writeQuantity(Convert.ToDouble(row["ContractedCapacity"]), row["ContractedCapacityMU"].ToString(), "TWh")),
                    new XElement("availableCapacity", writeQuantity(Convert.ToDouble(row["AvailableCapacity"]), row["AvailableCapacityMU"].ToString(), "TWh"))
                    );
            }

            return nodes;
        }

        public XElement[] writeStorageParticipantActivityReports(int ID_StorageReport)
        {
            DataSet ds = bitSimpleSQL.Query("SELECT * FROM [REMIT_StorageParticipantActivityReports] WHERE [ID_StorageReport] = " + ID_StorageReport.ToString());

            XElement[] nodes = new XElement[ds.Tables[0].Rows.Count];
            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                DataRow row = ds.Tables[0].Rows[i];

                nodes[i] = new XElement("storageParticipantActivityReport",
                    new XElement("gasDayStart", ((DateTime)row["gasDayStart"]).ToString("yyyy-MM-ddThh:mm:ss")),
                    new XElement("gasDayEnd", ((DateTime)row["gasDayEnd"]).ToString("yyyy-MM-ddThh:mm:ss")),
                    new XElement("storageFacilityIdentifier", writeIdentifier(row["StorageFacilityIdentifier"].ToString(), row["StorageFacilityIdentifierType"].ToString(), "ACER")),
                    new XElement("storageFacilityOperatorIdentifier", writeIdentifier(row["StorageFacilityOperatorIdentifier"].ToString(), row["StorageFacilityOperatorIdentifierType"].ToString(), "ACER")),
                    new XElement("marketParticipantIdentifier", writeIdentifier(row["MarketParticipantIdentifier"].ToString(), row["MarketParticipantIdentifierType"].ToString(), "ACER")),
                    new XElement("reportingEntityReferenceID", "NGSTORPACT" + row["ID"].ToString()/*row["ReferenceID"].ToString()*/),
                    new XElement("storage", writeQuantity(Convert.ToDouble(row["Storage"]), row["StorageMU"].ToString(), "TWh"))
                    );
            }

            return nodes;
        }

        public XElement[] writeStorageUnavailabilityReports(int ID_StorageReport)
        {
            DataSet ds = bitSimpleSQL.Query("SELECT * FROM [REMIT_StorageUnavailabilityReports] WHERE [ID_StorageReport] = " + ID_StorageReport.ToString());

            XElement[] nodes = new XElement[ds.Tables[0].Rows.Count];
            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                DataRow row = ds.Tables[0].Rows[i];

                nodes[i] = new XElement("storageUnavailabilityReport",
                    new XElement("unavailabilityNotificationTimestamp", ((DateTime)row["UnavailabilityNotificationTimestamp"]).ToString("yyyy-MM-ddThh:mm:ss")),
                    new XElement("storageFacilityIdentifier", writeIdentifier(row["StorageFacilityIdentifier"].ToString(), row["StorageFacilityIdentifierType"].ToString(), "ACER")),
                    new XElement("storageFacilityOperatorIdentifier", writeIdentifier(row["StorageFacilityOperatorIdentifier"].ToString(), row["StorageFacilityOperatorIdentifierType"].ToString(), "ACER")),
                    new XElement("reportingEntityReferenceID", "NGSTORUNAV" + row["ID"].ToString()/*row["ReferenceID"].ToString()*/),
                    new XElement("unavailabilityStart", ((DateTime)row["UnavailabilityStart"]).ToString("yyyy-MM-ddThh:mm:ss")),
                    new XElement("unavailabilityEnd", ((DateTime)row["UnavailabilityEnd"]).ToString("yyyy-MM-ddThh:mm:ss")),
                    new XElement("unavailabilityEndFlag", row["UnavailabilityEndFlag"].ToString()),
                    new XElement("unavailableVolume", writeQuantity(Convert.ToDouble(row["UnavailableVolume"]), row["UnavailableVolumeMU"].ToString(), "TWh")),
                    new XElement("unavailableInjection", writeQuantity(Convert.ToDouble(row["UnavailableInjection"]), row["UnavailableInjectionMU"].ToString(), "GWh/d")),
                    new XElement("unavailableWithdrawal", writeQuantity(Convert.ToDouble(row["UnavailableWithdrawal"]), row["UnavailableWithdrawalMU"].ToString(), "GWh/d")),
                    new XElement("unavailabilityType", row["UnavailabilityType"].ToString()),
                    new XElement("unavailabilityDescription", row["UnavailabilityDescription"].ToString())
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
                    return new XElement("acerCode", Identifier.Trim());
                case "EIC":
                    return new XElement("eicCode", Identifier.Trim());
                case "LEI":
                    return new XElement("leiCode", Identifier.Trim());
                default:
                    return new XElement("invalid", Identifier.Trim());
            }
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
            settings.Schemas.Add("http://www.acer.europa.eu/REMIT/REMITStorageSchema_V1.xsd", XSD);
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
