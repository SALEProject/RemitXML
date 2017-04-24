using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.OleDb;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Linq;
using System.IO;
using System.Linq;
using System.Xml.XPath;
using NetServices;

namespace RemitXML
{
    public class RemitXML
    {      
        //--------------------20151013-----------------------------
        private const string TRADES_SHEET = "T1_trades";
        private const string ORDERS_SHEET = "T1_orders";
        private const string TRADES_INFO = "N4:O144";
        private const string ORDERS_INFO = "O4:P172";
        private const string ORDERS_SEQUENCE = "A1:B33";
        private const string TRADES_SEQUENCE = "A34:B54";
        private const string DATA_BUYER_SHEET = "3.06Buyer";
        private const string DATA_SELLER_SHEET = "3.06Seller";
        private const string DATA_CANCELED_SHEET = "3.54Canceled";
        private const string DATA_RANGE = "A3:F50";
        private const string DATA_RANGE_CANCELED = "A3:C50";
        private const string DATA_ORDER_KEY = "F1";
        private const string DATA_TRADE_KEY = "F2";
        private const string DATA_ORDER = "F5";
        private const string DATA_TRADE = "F6";
        private const string REGISTER_RANGE = "A2:F5000";
        /*private const string REPORTING_DATA_PATH = "D:\\Projects\\BRM\\SALE\\Implementation\\REMIT\\RemitXML\\RemitXML\\bin\\Debug\\1_completat.xlsx";
        private const string XML_PATH = "D:\\Projects\\BRM\\SALE\\Implementation\\REMIT\\RemitXML\\RemitXML\\bin\\Debug\\XML_PATHS.xlsx";
        private const string SEQUENCE_PATH = "D:\\Projects\\BRM\\SALE\\Implementation\\REMIT\\RemitXML\\RemitXML\\bin\\Debug\\Sequence.xlsx";
        private const string ERRORS_PATH = "D:\\Projects\\BRM\\SALE\\Implementation\\REMIT\\RemitXML\\RemitXML\\bin\\Debug\\errors.txt";*/
        private const string XSD = "App_Data/REMITTable1_V2.xsd";
        private const string XML_PATH = "App_Data/XML_PATHS.xlsx";
        private const string SEQUENCE_PATH = "App_Data/Sequence.xlsx";
        private const string ACER_REGISTRY = "App_Data/Registru_ACER.xlsx";
        private const string REPORTING_DATA_PATH_XLS = "Raport.xls";
        private const string REPORTING_DATA_PATH_XLSX = "Raport.xlsx";
        string REPORTING_DATA_PATH = "";
        private const string ERRORS_PATH = "errors.txt";
        private string REPORT_PATH = "Raport.xml";
        private Dictionary<string, string> tradesInfo = new Dictionary<string, string>();
        private Dictionary<string, string> ordersInfo = new Dictionary<string, string>();
        private Dictionary<string, string> ordersSequence = new Dictionary<string, string>();
        private Dictionary<string, string> tradesSequence = new Dictionary<string, string>();
        private Dictionary<string, string> trades = new Dictionary<string, string>();
        private Dictionary<string, string> orders = new Dictionary<string, string>();
        private Dictionary<string, string> canceledOrder = new Dictionary<string, string>();
        private XDocument report = new XDocument();
        private XDocument document = new XDocument();
        private XDocument buyerXML = new XDocument();
        private XDocument sellerXML = new XDocument();

        public int ID_REMIT_DataSourceHistory = 0;

        public void saveDB(Dictionary<string, string> order, Dictionary<string, string> trade = null)
        {
            //  Extract the data we need
            int isTransacted = Convert.ToInt32(trade != null);
            string ParticipantCode = order["1.01"];
            string AgencyCode = order["3.01"];
            string ContractID = order["21.01"];
            string ContractName = order["22.01"];
            string OrderID = order["13.01"];
            int isInitiator = isTransacted == 1 ? Convert.ToInt32(trade["12.01"] == "I") : 0;// Convert.ToInt32(order["12.01"] == "I");
            int Direction = Convert.ToInt32(order["11.01"] == "S");
            double Volume = Convert.ToDouble(order["40.01"]);
            string VolumeMU = order["42.01"];
            double NotionalQuantity = isTransacted == 1 ? Convert.ToDouble(order["41.01"]) : 0;
            string NotionalQuantityMU = isTransacted == 1 ? order["42.02"] : "";
            double Price = Convert.ToDouble(order["35.01"]);
            string Currency = order["37.01"];
            DateTime StartDeliveryDate = Convert.ToDateTime(order["49.01"]);
            DateTime EndDeliveryDate = Convert.ToDateTime(order["50.01"]);
            DateTime TransactionTimestamp = Convert.ToDateTime(order["30.01"]);
            DateTime LastTradingDateTime = Convert.ToDateTime(order["29.01"]);

            //  update participant codes
            string sql = "SELECT * FROM [REMIT_Participants] WHERE [AgencyCode] = '" + AgencyCode + "'";
            DataSet ds = bitSimpleSQL.Query(sql);
            switch (bitSimpleSQL.isValidDSRows(ds))
            {
                case false:
                    sql = "INSERT INTO [REMIT_Participants] ([AgencyCode], [ParticipantCode]) " +
                          "VALUES ('" + AgencyCode + "', '" + ParticipantCode + "')";
                    break;
                case true:
                    sql = "UPDATE [REMIT_Participants] SET [ParticipantCode] = '" + ParticipantCode + "' WHERE [AgencyCode] = '" + AgencyCode + "'";
                    break;
            }
            bitSimpleSQL.Execute(sql);

            //  check if update or insert order
            sql = "SELECT * FROM [REMIT_Orders] WHERE [OrderID] = '" + OrderID.ToString() + "'";
            ds = bitSimpleSQL.Query(sql);
            switch (bitSimpleSQL.isValidDSRows(ds))
            {
                case false:
                    //  insert
                    sql = "INSERT INTO [REMIT_Orders] " +
                          "([ID_REMIT_DataSourceHistory], [ParticipantCode], [AgencyCode], [ContractID], [ContractName], [OrderID], [isInitiator], " +
                          "[Direction], [Volume], [VolumeMU], [NotionalQuantity], [NotionalQuantityMU], [Price], [Currency], " +
                          "[StartDeliveryDate], [EndDeliveryDate], [isTransacted], [TransactionTimestamp], [LastTradingDateTime]) " +
                          "VALUES " +
                          "(" + ID_REMIT_DataSourceHistory.ToString() + ", '" + ParticipantCode + "', '" + AgencyCode + "', '" + ContractID + "', '" + ContractName + "', '" + OrderID + "', " + isInitiator.ToString() + ", " +
                          Direction.ToString() + ", " + Volume.ToString() + ", '" + VolumeMU + "', " + NotionalQuantity.ToString() + ", '" + NotionalQuantityMU + "', " + Price.ToString() + ", '" + Currency + "', '" +
                          StartDeliveryDate.ToString() + "', '" + EndDeliveryDate.ToString() + "', " + isTransacted.ToString() + ", '" + TransactionTimestamp.ToString() + "', '" + LastTradingDateTime.ToString() + "')";
                    break;
                case true:
                    //  update
                    sql = "UPDATE [REMIT_Orders] SET " +
                          "[ID_REMIT_DataSourceHistory] = " + ID_REMIT_DataSourceHistory.ToString() +", " +
                          "[ParticipantCode] = '" + ParticipantCode +"', " +
                          "[AgencyCode] = '" + AgencyCode + "', " +
                          "[ContractID] = '" + ContractID + "', " +
                          "[ContractName] = '" + ContractName + "', " +
                          "[OrderID] = '" + OrderID + "', " +
                          "[isInitiator] = " + isInitiator.ToString() + ", " +
                          "[Direction] = " + Direction.ToString() + ", " +
                          "[Volume] = " + Volume.ToString() + ", " +
                          "[VolumeMU] = '" + VolumeMU + "', " +
                          "[NotionalQuantity] = " + NotionalQuantity.ToString() + ", " +
                          "[NotionalQuantityMU] = '" + NotionalQuantityMU + "', " +
                          "[Price] = " + Price.ToString() + ", " +
                          "[Currency] = '" + Currency + "', " +
                          "[StartDeliveryDate] = '" + StartDeliveryDate.ToString() + "', " +
                          "[EndDeliveryDate] = '" + EndDeliveryDate.ToString() + "', " +
                          "[isTransacted] = " + isTransacted.ToString() + ", " +
                          "[TransactionTimestamp] = '" + TransactionTimestamp.ToString() + "', " +
                          "[LastTradingDateTime] = '" + LastTradingDateTime.ToString() + "' " +
                          "WHERE [ID] = " + ds.Tables[0].Rows[0]["ID"].ToString();
                    break;
            }
            
            bitSimpleSQL.Execute(sql);
        }

        public RemitXML(int ID_REMIT_DataSourceHistory)
        {
            this.ID_REMIT_DataSourceHistory = ID_REMIT_DataSourceHistory;

            if (File.Exists(REPORTING_DATA_PATH_XLS)) REPORTING_DATA_PATH = REPORTING_DATA_PATH_XLS;
            else if (File.Exists(REPORTING_DATA_PATH_XLSX)) REPORTING_DATA_PATH = REPORTING_DATA_PATH_XLSX;
            
            if (REPORTING_DATA_PATH == "") return;

            //1. we need to delete any existing errors and reports
            File.Delete(ERRORS_PATH);
            File.Delete(REPORT_PATH);
            string path = System.Reflection.Assembly.GetEntryAssembly().Location;
            path = path.Replace("RemitXML.exe", "");
            foreach (string file in Directory.EnumerateFiles(path, "*.xml"))
                if (Path.GetFileName(file) != "DB.xml")
                {
                    File.Delete(file);
                }
            
            //2. gather the data
            getTradesInfo();
            getOrdersInfo();
            getOrdersSequence();
            getTradesSequence();

            string initiatorDirection = "";
            int seqNumber = 0;

            //3. get the buyer data and create xml
            document = new XDocument(new XDeclaration("1.0", "utf-8", null));
            string buyOrderID = getData(DATA_BUYER_SHEET);
            if (buyOrderID != "")
            {
                if (trades["12.01"].ToUpper() == "I") initiatorDirection = orders["11.01"];
                seqNumber++;
                updateNodeValue();
                saveDB(orders, trades);
                createXML();
                document.Element("REMITTable1").Add(new XAttribute("cucubau", "http://www.acer.europa.eu/REMIT/REMITTable1_V2.xsd"));
                document.Element("REMITTable1").Add(new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"));
                buyerXML = document;
                report = document;
                buyerXML.Save("Buyer.xml");
            }

            //4. get the seller data and create xml
            trades = new Dictionary<string, string>();
            orders = new Dictionary<string, string>();
            document = new XDocument(new XDeclaration("1.0", "utf-8", null));
            string sellOrderID = getData(DATA_SELLER_SHEET);
            if (buyOrderID != "")
            {
                if (initiatorDirection == "" && trades["12.01"].ToUpper() == "I") initiatorDirection = orders["11.01"];
                seqNumber++;
                updateNodeValue();
                saveDB(orders, trades);
                createXML();
                document.Element("REMITTable1").Add(new XAttribute("cucubau", "http://www.acer.europa.eu/REMIT/REMITTable1_V2.xsd"));
                document.Element("REMITTable1").Add(new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"));
                sellerXML = document;
                sellerXML.Save("Seller.xml");
            }

            //5. get the seller OrderReport and TradeReport and save in buyer xml
            if (buyOrderID != "")
            {
                IEnumerable<XElement> orderReports = sellerXML.XPathSelectElements("/REMITTable1/OrderList/OrderReport");
                foreach (XElement el in orderReports)
                {
                    report.XPathSelectElement("/REMITTable1/OrderList").Add(el);
                }

                IEnumerable<XElement> tradeReports = sellerXML.XPathSelectElements("/REMITTable1/TradeList/TradeReport");
                foreach (XElement el in tradeReports)
                {
                    report.XPathSelectElement("/REMITTable1/TradeList").Add(el);
                }
            }

            //6 get canceled orders
            int countCanceled = 0;
            string canceledOrderID = getCanceledData(DATA_CANCELED_SHEET, countCanceled);
            while (canceledOrderID != "")
            {
                bool validCancel = true;
                
                //  validate order ID
                if (buyOrderID != "" && (canceledOrderID == buyOrderID || canceledOrderID == sellOrderID))
                {
                    AppendError("Ordinul netranzactionat " + canceledOrderID + " nu este unic.");
                    validCancel = false;
                }
                
                //  validate aggressor against initiator
                if (initiatorDirection != "")
                {
                    switch (initiatorDirection)
                    {
                        case "B":
                            if (canceledOrder["11.01"] != "S") validCancel = false;
                            break;
                        case "S":
                            if (canceledOrder["11.01"] != "B") validCancel = false;
                            break;
                    }

                    if (!validCancel) AppendError("Ordinul netranzactionat " + canceledOrderID + " are acelasi sens cu initiatorul.");
                }

                if (validCancel)
                {
                    seqNumber++;
                    canceledOrder["100.01"] = seqNumber.ToString();
                    document = new XDocument(new XDeclaration("1.0", "utf-8", null));
                    updateCanceledNodeValue();
                    saveDB(canceledOrder);
                    createCanceledXML();

                    if (buyOrderID == "" && countCanceled == 0)
                    {
                        document.Element("REMITTable1").Add(new XAttribute("cucubau", "http://www.acer.europa.eu/REMIT/REMITTable1_V2.xsd"));
                        document.Element("REMITTable1").Add(new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"));
                        report = document;
                    }
                    else
                    {
                        IEnumerable<XElement> cancelReport = document.XPathSelectElements("/REMITTable1/OrderList/OrderReport");
                        foreach (XElement el in cancelReport)
                            report.XPathSelectElement("/REMITTable1/OrderList").Add(el);
                    }
                }

                canceledOrder = new Dictionary<string, string>();
                countCanceled++;
                canceledOrderID = getCanceledData(DATA_CANCELED_SHEET, countCanceled);
            }

            //7. save the report and make that little tweak
            if (buyOrderID != "")
            {
                File.Delete("Seller.xml");
                File.Delete("Buyer.xml");
            }
            
            if (File.Exists(ERRORS_PATH))
                REPORT_PATH = getFileName(true) + ".xml";
            else
                REPORT_PATH = getFileName(false) + ".xml";

            File.Delete(REPORT_PATH);
            report.Save(REPORT_PATH);
            string text = File.ReadAllText(@REPORT_PATH);
            text = text.Replace("cucubau", "xmlns");
            File.WriteAllText(@REPORT_PATH, text);

            //7. validate agains xsd schema
            xsd();

            //8. final message
            Console.WriteLine("Conversie finalizata. Va rugam sa verificati daca exista erori in fisierul 'errors.txt' si daca a fost generat raportul in fisierul 'Raport.xml'");
            //Console.ReadKey();
        }

        public void getTradesInfo()
        {
            if (oleSimpleSQL.Connect(XML_PATH))
            {
                string str_sql = @"SELECT * FROM [" + TRADES_SHEET + "$" + TRADES_INFO + "] IN '' 'Excel 8.0;HDR=NO;DATABASE=" + XML_PATH + @"' ";
                DataSet ds_range = oleSimpleSQL.Query(str_sql);
                if (ds_range == null) return;
                if (ds_range.Tables.Count == 0) return;
                for (int i = 0; i < ds_range.Tables[0].Rows.Count; i++)
                { 
                    string key = ds_range.Tables[0].Rows[i]["F1"].ToString();
                    string path = ds_range.Tables[0].Rows[i]["F2"].ToString();
                    if (!path.Contains("REMITTable1"))
                        continue;
                    tradesInfo.Add(key, path);
                }
                oleSimpleSQL.Disconnect();
            }
        }

        public void getOrdersInfo()
        {
            if (oleSimpleSQL.Connect(XML_PATH))
            {
                string str_sql = @"SELECT * FROM [" + ORDERS_SHEET + "$" + ORDERS_INFO + "] IN '' 'Excel 8.0;HDR=NO;DATABASE=" + XML_PATH + @"' ";
                DataSet ds_range = oleSimpleSQL.Query(str_sql);
                if (ds_range == null) return;
                if (ds_range.Tables.Count == 0) return;
                for (int i = 0; i < ds_range.Tables[0].Rows.Count; i++)
                {
                    string key = ds_range.Tables[0].Rows[i]["F1"].ToString();
                    string path = ds_range.Tables[0].Rows[i]["F2"].ToString();
                    if (!path.Contains("REMITTable1"))
                        continue;
                    ordersInfo.Add(key, path);
                }
                oleSimpleSQL.Disconnect();
            }
        }

        public void AppendError(string text)
        {
            using (StreamWriter sw = File.AppendText(ERRORS_PATH))
            {
                sw.WriteLine(text);
            }
        }

        public string getData(string SHEET)
        {
            if (oleSimpleSQL.Connect(REPORTING_DATA_PATH))
            {
                try
                {
                    string str_sql = @"SELECT * FROM [" + SHEET + "$" + DATA_RANGE + "] IN '' 'Excel 8.0;HDR=NO;DATABASE=" + REPORTING_DATA_PATH + @"' ";
                    DataSet ds_range = oleSimpleSQL.Query(str_sql);
                    if (ds_range == null) return "";
                    if (ds_range.Tables.Count == 0) return "";

                    for (int i = 0; i < ds_range.Tables[0].Rows.Count; i++)
                    {
                        string orderKey = ds_range.Tables[0].Rows[i][DATA_ORDER_KEY].ToString();
                        string tradeKey = ds_range.Tables[0].Rows[i][DATA_TRADE_KEY].ToString();
                        string orderInfo = ds_range.Tables[0].Rows[i][DATA_ORDER].ToString();
                        string tradeInfo = ds_range.Tables[0].Rows[i][DATA_TRADE].ToString();

                        if ((orderKey != "") && (orderInfo != ""))
                        {
                            orders.Add(orderKey, orderInfo);
                        }

                        if ((tradeKey != "") && (tradeInfo != ""))
                        {
                            trades.Add(tradeKey, tradeInfo);
                        }
                    }

                    Validate validate = new Validate();
                    Dictionary<string, List<string>> errors = new Dictionary<string, List<string>>();
                    List<Dictionary<string, string>> dataSets = new List<Dictionary<string, string>>();
                    dataSets.Add(orders);
                    dataSets.Add(trades);
                    errors = validate.check(dataSets);

                    if (errors.Count > 0)
                    {
                        AppendError(SHEET);
                        List<string> keys = errors.Keys.ToList();
                        keys.Sort();
                        foreach (var key in keys)
                        {
                            string field = key.Split('.')[0];
                            List<string> errs = errors[key];
                            foreach (var err in errs) AppendError(field + ": " + err);
                        }
                    }
                }
                finally
                {
                    oleSimpleSQL.Disconnect();
                }

                //  validate true if there is an order ID there
                string orderID = orders["13.01"];
                if (orderID.Trim().ToUpper().Replace("NOTRADE", "") == "") return "";
                else return orderID;
            }

            return "";
        }

        protected string ExcelColumn(int k)
        {
            char[] letters = {'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z'};
            int int_letter = k % 26;
            int int_div = k / 26;

            string res = "";
            if (int_div > 0) res = ExcelColumn(int_div);

            return res + letters[int_letter];
        }

        public string getCanceledData(string SHEET, int count)
        {
            if (oleSimpleSQL.Connect(REPORTING_DATA_PATH))
            {
                try
                {
                    string column = ExcelColumn(4 + count);
                    string range = "A3:" + column + "50";
                    string column_name = "F" + (5 + count).ToString();
                    string str_sql = @"SELECT F1, F2, F4, F" + (5 + count).ToString() + " FROM [" + SHEET + "$" + range + "] IN '' 'Excel 8.0;HDR=NO;DATABASE=" + REPORTING_DATA_PATH + @"' ";
                    DataSet ds_range = oleSimpleSQL.Query(str_sql);
                    if (ds_range == null) return "";
                    if (ds_range.Tables.Count == 0) return "";

                    for (int i = 0; i < ds_range.Tables[0].Rows.Count; i++)
                    {
                        string orderKey = ds_range.Tables[0].Rows[i][DATA_ORDER_KEY].ToString();
                        string tradeKey = ds_range.Tables[0].Rows[i][DATA_TRADE_KEY].ToString();
                        string orderInfo = ds_range.Tables[0].Rows[i][column_name].ToString();
                        //string tradeInfo = ds_range.Tables[0].Rows[i][DATA_TRADE].ToString();

                        if ((orderKey != "") && (orderInfo != ""))
                        {
                            canceledOrder.Add(orderKey, orderInfo);
                        }
                    }

                    Validate validate = new Validate();
                    Dictionary<string, List<string>> errors = new Dictionary<string, List<string>>();
                    List<Dictionary<string, string>> dataSets = new List<Dictionary<string, string>>();
                    dataSets.Add(canceledOrder);
                    errors = validate.check(dataSets);

                    if (errors.Count > 0)
                    {
                        AppendError(SHEET);
                        List<string> keys = errors.Keys.ToList();
                        keys.Sort();
                        foreach (var key in keys)
                        {
                            string field = key.Split('.')[0];
                            List<string> errs = errors[key];
                            foreach (var err in errs) AppendError(field + ": " + err);
                        }
                    }
                }
                finally
                {
                    oleSimpleSQL.Disconnect();
                }

                //  validate true if there is an order ID there
                string orderID = canceledOrder["13.01"];
                if (orderID.Trim().ToUpper().Replace("NOORDER", "") == "") return "";
                else return orderID;
            }

            return "";
        }
        
        public void updateNodeValue()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();

            foreach (KeyValuePair<string, string> entry in orders)
            {
                string key = entry.Key;
                string value = entry.Value;
                switch (key)
                { 
                    case "28.01": 
                    case "28.02":
                    case "54.01":
                    case "54.02":
                        value += "Z";
                        dict.Add(key, value);
                        break;
                    case "30.01":
                        value += "+02:00";
                        dict.Add(key, value);
                        break;
                }
            }

            foreach (KeyValuePair<string, string> entry in dict)
            {
                string key = entry.Key;
                string value = entry.Value;
                orders[key] = value;
            }

            dict = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> entry in trades)
            {
                string key = entry.Key;
                string value = entry.Value;
                switch (key)
                {
                    case "28.01":
                    case "28.02":
                    case "54.01":
                    case "54.02":
                        value += "Z";
                        dict.Add(key, value);
                        break;
                    case "30.01":
                        value += "+02:00";
                        dict.Add(key, value);
                        break;
                }
                orders[key] = value;
            }

            foreach (KeyValuePair<string, string> entry in dict)
            {
                string key = entry.Key;
                string value = entry.Value;
                trades[key] = value;
            }
        }

        public void updateCanceledNodeValue()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();

            foreach (KeyValuePair<string, string> entry in canceledOrder)
            {
                string key = entry.Key;
                string value = entry.Value;
                switch (key)
                {
                    case "28.01":
                    case "28.02":
                    case "54.01":
                    case "54.02":
                        value += "Z";
                        dict.Add(key, value);
                        break;
                    case "30.01":
                        value += "+02:00";
                        dict.Add(key, value);
                        break;
                }
            }

            foreach (KeyValuePair<string, string> entry in dict)
            {
                string key = entry.Key;
                string value = entry.Value;
                canceledOrder[key] = value;
            }
        }

        public void createXML()
        {
            foreach (KeyValuePair<string, string> sequence in ordersSequence)
            {
                string seqKey = sequence.Value;
                string seq = sequence.Key;

                foreach (KeyValuePair<string, string> ord in orders)
                {
                    string ordKey = ord.Key;
                    string ordInfo = ord.Value;
                    if (seqKey == ordKey)
                        createOrderNode(ordKey, ordInfo);
                }
            }

            foreach (KeyValuePair<string, string> sequence in tradesSequence)
            {
                string seqKey = sequence.Value;
                string seq = sequence.Key;

                foreach (KeyValuePair<string, string> trd in trades)
                {
                    string trdKey = trd.Key;
                    string trdInfo = trd.Value;
                    if (seqKey == trdKey)
                        createTradeNode(trdKey, trdInfo);
                }
            }
        }

        public void createCanceledXML()
        {
            foreach (KeyValuePair<string, string> sequence in ordersSequence)
            {
                string seqKey = sequence.Value;
                string seq = sequence.Key;

                foreach (KeyValuePair<string, string> ord in canceledOrder)
                {
                    string ordKey = ord.Key;
                    string ordInfo = ord.Value;
                    if (seqKey == ordKey)
                        createOrderNode(ordKey, ordInfo);
                }
            }
        }

        public void createOrderNode(string key, string value)
        {
            foreach (KeyValuePair<string, string> keyValue in ordersInfo)
            {
                string code = keyValue.Key;
                string path = keyValue.Value;

                if (key == code)
                {
                    path = path.Replace(" ", "");
                    string[] nodes = path.Split('>');
                    document = createXPATH(document, nodes, value);
                    break;
                }
            }
        }

        public void createTradeNode(string key, string value)
        {
            foreach (KeyValuePair<string, string> keyValue in tradesInfo)
            {
                string code = keyValue.Key;
                string path = keyValue.Value;

                if (key == code)
                {
                    path = path.Replace(" ", "");
                    string[] nodes = path.Split('>');
                    document = createXPATH(document, nodes, value);
                    break;
                }
            }
        }

        public XDocument createXPATH(XDocument document, string[] nodes, string value)
        {
            XElement element = null;
            for (int i = 0; i < nodes.Length; i++)
            {
                if (element == null)
                {
                    string a = nodes[i];
                    element = new XElement(a);

                    if (document.Element(a) != null)
                        continue;
                    else
                    {
                        document.Add(element);
                        continue;
                    }
                }

                string currentNodeName = "/";
                for (int j = 0; j <= i; j++)
                {
                    currentNodeName += nodes[j] + "/";
                }
                currentNodeName = currentNodeName.Remove(currentNodeName.Length - 1);
                IEnumerable<XElement> list = document.XPathSelectElements(currentNodeName);
                if (list.Count() > 0)
                    continue;

                currentNodeName = nodes[i];
                if (i == nodes.Length - 1)
                    element = new XElement(currentNodeName, value);
                else
                    element = new XElement(currentNodeName);

                string parent = nodes[i - 1];
                document.Descendants(parent).Last().Add(element);
            }

            return document;
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

        private void getOrdersSequence()
        {
            if (oleSimpleSQL.Connect(SEQUENCE_PATH))
            {
                string str_sql = @"SELECT * FROM [Sequence$" + ORDERS_SEQUENCE + "] IN '' 'Excel 8.0;HDR=NO;DATABASE=" + SEQUENCE_PATH + @"' ";
                DataSet ds_range = oleSimpleSQL.Query(str_sql);
                if (ds_range == null) return;
                if (ds_range.Tables.Count == 0) return;
                for (int i = 0; i < ds_range.Tables[0].Rows.Count; i++)
                {
                    string seq = ds_range.Tables[0].Rows[i]["F1"].ToString();
                    string key = ds_range.Tables[0].Rows[i]["F2"].ToString();
                    ordersSequence.Add(seq, key);
                }
                oleSimpleSQL.Disconnect();
            }
        }

        private void getTradesSequence()
        {
            if (oleSimpleSQL.Connect(SEQUENCE_PATH))
            {
                string str_sql = @"SELECT * FROM [Sequence$" + TRADES_SEQUENCE + "] IN '' 'Excel 8.0;HDR=NO;DATABASE=" + SEQUENCE_PATH + @"' ";
                DataSet ds_range = oleSimpleSQL.Query(str_sql);
                if (ds_range == null) return;
                if (ds_range.Tables.Count == 0) return;
                for (int i = 0; i < ds_range.Tables[0].Rows.Count; i++)
                {
                    string seq = ds_range.Tables[0].Rows[i]["F1"].ToString();
                    string key = ds_range.Tables[0].Rows[i]["F2"].ToString();
                    tradesSequence.Add(seq, key);
                }
                oleSimpleSQL.Disconnect();
            }
        }

        private string getFileName(bool hasError)
        {
            string fileName = "";
            string date = DateTime.Now.ToString("yyyyMMdd");
            string schemaName = "REMITTable1";
            string schemaVersion = "V2";
            string ID = "B00020987.RO";
            string basicName = date + "_" + schemaName + "_" + schemaVersion + "_" + ID + "_";

            if (hasError)
                return basicName + "ERROR";

            if (oleSimpleSQL.Connect(ACER_REGISTRY))
            {
                string str_sql = @"SELECT * FROM [Register$" + REGISTER_RANGE + "] IN '' 'Excel 8.0;HDR=NO;DATABASE=" + ACER_REGISTRY + @"' ";
                DataSet ds_range = oleSimpleSQL.Query(str_sql);
                if (ds_range == null) return basicName;
                if (ds_range.Tables.Count == 0) return basicName;
                for (int i = 0; i < ds_range.Tables[0].Rows.Count; i++)
                {
                    string lastDate = ds_range.Tables[0].Rows[i]["F1"].ToString();
                    string newDate = ds_range.Tables[0].Rows[i + 1]["F1"].ToString();
                    if (newDate == "")
                    {

                        int sequence = 0;
                        try
                        {
                            sequence = Convert.ToInt32(ds_range.Tables[0].Rows[i]["F2"].ToString());
                        }
                        catch (Exception ex)
                        {
                            sequence = 0;
                        }
                        fileName = date + "_" + schemaName + "_" + schemaVersion + "_" + ID + "_";
                        if (date == lastDate)
                            sequence++;
                        else
                            sequence = 1;
                        fileName += sequence.ToString();
                        
                        int row = i + 2;
                        oleSimpleSQL.Execute("INSERT INTO [Register$A" + row.ToString() + ":A" + row.ToString() + "] VALUES( \'" + date + "\')");
                        oleSimpleSQL.Execute("INSERT INTO [Register$B" + row.ToString() + ":B" + row.ToString() + "] VALUES( \'" + sequence.ToString() + "\')");
                        break;
                    }
                }
                oleSimpleSQL.Disconnect();
            }
            return fileName;
        }
    }
}
