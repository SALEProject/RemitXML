using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Data;

namespace RemitXML
{
    class Validate
    {
        private string dataset = "";

        public Dictionary<string, List<string>> check(List<Dictionary<string, string>> dataSets)
        {
            Dictionary<string, List<string>> errors = new Dictionary<string,List<string>>();
            int i = 0;
            foreach (var dataSet in dataSets)
            {
                i++;
                if (i == 1)
                    dataset = "Order";
                if (i == 2)
                    dataset = "Trade";

                foreach (KeyValuePair<string, string> entry in dataSet)
                {
                    string key = entry.Key;
                    string value = entry.Value;

                    List<string> err = checkField(key, value);
                    if (err.Count > 0)
                    {
                        if (errors.ContainsKey(key))
                        {
                            List<string> existingErrors = errors[key];
                            foreach (var er in err)
                            {
                                existingErrors.Add(er);
                            }
                            errors[key] = existingErrors;
                        }
                        else
                            errors.Add(key, err);
                    }
                        
                }
            }

            return errors;
        }

        private List<string> checkField(string key, string value)
        {
            List<string> errors =  new List<string>();
            switch(key)
            {
                case "1.01": errors = ace(value); break;
                case "3.01": errors = traderCode(value); break;
                case "4.01": errors = ace(value); break;
                case "6.01": errors = ace(value); break;
                case "10.01": errors = tradingCapacityType(value); break;
                case "11.01": errors = buySellIndicatorType(value); break;
                case "12.01": errors = aggressorType(value); break;
                case "13.01": errors = orderIdentifierType(value); break;
                case "14.01": errors = errors = orderTypesType(value); break;
                case "16.01": errors = orderStatusType(value); break;
                case "20.01": errors = orderDurationsType(value); break;
                case "21.01": errors = contractIdType(value); break;
                case "21.02": errors = contractIdType(value); break;
                case "22.01": errors = contractNameType(value); break;
                case "23.01": errors = contractTypeType(value); break;
                case "24.01": errors = energyCommodityType(value); break;
                case "26.01": errors = settlementMethodType(value); break;
                case "27.03": errors = mic(value); break;
                case "27.07": errors = mic(value); break;
                case "28.01": errors = time(value); break;
                case "28.02": errors = time(value); break;
                case "29.01": errors = dateTime(value); break;            
                case "30.01": errors = dateTime(value); break;
                case "31.01": errors = uniqueTransactionIdentifierType(value); break;
                case "33.01": errors = orderIdentifierType(value); break;
                case "35.01": errors = number(value); break;
                case "37.01": errors = currencyCodeType(value); break;
                case "38.01": errors = number(value); break;
                case "39.01": errors = currencyCodeType(value); break;
                case "40.01": errors = number(value); break;
                case "41.01": errors = number(value); break;
                case "42.01": errors = quantityUnitType(value); break;
                case "42.02": errors = notionalQuantityUnitType(value); break;
                case "48.01": errors = eic(value); break;
                case "49.01": errors = dateTime(value); break; //cu semnul intrebarii pentru ca pare a fi doar tip data
                case "50.01": errors = dateTime(value); break;//cu semnul intrebarii pentru ca pare a fi doar tip data
                case "52.01": errors = contractLoadType(value); break;
                case "54.01": errors = time(value); break;
                case "54.02": errors = time(value); break;
                case "58.01": errors = actionTypesType(value); break;
                case "100.01": errors = recordSeqNumberType(value); break;
            }
            
            return errors;
        }


        private bool minLength(string str, int minLength)
        {
            bool hasMinLength = false;
            if (str.Length >= minLength)
                hasMinLength = true;

            return hasMinLength;
        }

        private bool maxLength(string str, int maxLength)
        {
            bool hasMaxLength = false;
            if (str.Length <= maxLength)
                hasMaxLength = true;

            return hasMaxLength;

        }

        private bool matchRegex(string pattern, string str)
        {
            bool match = false;
            try
            {
                Regex rgx = new Regex(pattern);
                match = rgx.IsMatch(str);
                /*Regex.Match(str, pattern);
                match = true;*/
            }
            catch (ArgumentException)
            {
                match = false;
            }

            return match;
        }

        private bool inEnumeration(string str, string[] enumeration)
        {
            bool found = false;
            for (int i = 0; i < enumeration.Length; i++)
            {
                if (str.Equals(enumeration[i]))
                {
                    found = true;
                    break;
                }
            }

            return found;
        }

        private bool minValue(int value, int min)
        {
            bool isMinValue = false;
            if(value >= min)
                isMinValue = true;

            return isMinValue;
        }

        private string getMaxLengthErrorMessage(int maxLength)
        {
            return "(Coloana " + dataset + ") - Lungime mai mare de " + maxLength.ToString() + " caractere!";
        }

        private string getMinLengthErrorMessage(int minLength)
        {
            return "(Coloana " + dataset + ") - Lungime mai mica de " + minLength.ToString() + " caractere!";
        }

        private string getRegexErrorMessage(string pattern)
        {
            return "(Coloana " + dataset + ") - Cuvantul nu se potriveste cu pattern-ul " + pattern + "!";
        }

        private string getInEnumerationErrorMessage(string option, string[] enumeration)
        {
            string str = "";
            for (int i = 0; i < enumeration.Length; i++)
            {
                str += enumeration[i] + ",";
            }
            str = str.Remove(str.Length - 1);

            return "(Coloana " + dataset + ") - Optiunea " + option + " nu se regaseste in lista: " + str + " !";
        }

        private string getMinValueErrorMessage(int min)
        {
            return "(Coloana " + dataset + ") - Valoare campului trebuie sa fie mai mare sau egala cu: " + min.ToString();
        }

        public List<string> ace(string str)
        {
            List<string> errors = new List<string>();
            int minLen = 12;
            int maxLen = 12;
            string pattern = @"[A-Za-z0-9_]+\.[A-Z][A-Z]";

            if (!minLength(str, minLen))
                errors.Add(getMinLengthErrorMessage(minLen));

            if(!maxLength(str, maxLen))
                errors.Add(getMaxLengthErrorMessage(maxLen));

            if(!matchRegex(pattern, str))
                errors.Add(getRegexErrorMessage(pattern));

            return errors;
        }

        public List<string> actionTypesType(string str)
        {
            List<string> errors = new List<string>();
            string[] enumeration = { "N", "M", "E", "C" };

            if(!inEnumeration(str, enumeration))
                errors.Add(getInEnumerationErrorMessage(str, enumeration));

            return errors;
        }

        public List<string> aggressorType(string str)
        {
            List<string> errors = new List<string>();
            string[] enumeration = { "A", "I", "S" };

            if (!inEnumeration(str, enumeration))
                errors.Add(getInEnumerationErrorMessage(str, enumeration));

            return errors;
        }

        public List<string> bic(string str)
        {
            List<string> errors = new List<string>();
            int minLen = 11;
            int maxLen = 11;
            string pattern = "[A-Za-z0-9_]+";

            if (!minLength(str, minLen))
                errors.Add(getMinLengthErrorMessage(minLen));

            if (!maxLength(str, maxLen))
                errors.Add(getMaxLengthErrorMessage(maxLen));

            if (!matchRegex(pattern, str))
                errors.Add(getRegexErrorMessage(pattern));

            return errors;
        }

        public List<string> bil(string str)
        {
            List<string> errors = new List<string>();
            string[] enumeration = { "XBIL" };

            if (!inEnumeration(str, enumeration))
                errors.Add(getInEnumerationErrorMessage(str, enumeration));

            return errors;
        }

        public List<string> booleanType(string str)
        {
            List<string> errors = new List<string>();
            string pattern = "(true)|(Y)|(false)|(N)";

            if (!matchRegex(pattern, str))
                errors.Add(getRegexErrorMessage(pattern));

            return errors;
        }

        public List<string> buySellIndicatorType(string str)
        {
            List<string> errors = new List<string>();
            string[] enumeration = { "B", "S", "C" };

            if (!inEnumeration(str, enumeration))
                errors.Add(getInEnumerationErrorMessage(str, enumeration));

            return errors;
        }

        public List<string> contractIdType(string str)
        {
            List<string> errors = new List<string>();
            string pattern = "[A-Za-z0-9_:-]+";

            if (!matchRegex(pattern, str))
                errors.Add(getRegexErrorMessage(pattern));

            return errors;
        }

        public List<string> contractLoadType(string str)
        {
            List<string> errors = new List<string>();
            string[] enumeration = { "BL", "PL", "OP", "BH", "SH", "GD", "OT" };

            if (!inEnumeration(str, enumeration))
                errors.Add(getInEnumerationErrorMessage(str, enumeration));

            return errors;
        }

        public List<string> contractNameType(string str)
        {
            List<string> errors = new List<string>();
            int maxLen = 200;
           
            if (!maxLength(str, maxLen))
                errors.Add(getMaxLengthErrorMessage(maxLen));

            return errors;
        }

        public List<string> contractTypeType(string str)
        {
            List<string> errors = new List<string>();
            string[] enumeration = { "AU", "CO", "FW", "FU", "OP", "OP_FW", "OP_FU", "OP_SW", "SP", "SW", "OT" };

            if (!inEnumeration(str, enumeration))
                errors.Add(getInEnumerationErrorMessage(str, enumeration));

            return errors;
        }

        public List<string> currencyCodeType(string str)
        {
            List<string> errors = new List<string>();
            string[] enumeration = { "BGN", "CHF", "CZK", "DKK", "EUR", "EUX", "GBX", "GBP", "HRK", "SW", "HUF", "ISK", "NOK", "PCT", "PLN", "RON", "SEK", "USD", "OTH" };

            if (!inEnumeration(str, enumeration))
                errors.Add(getInEnumerationErrorMessage(str, enumeration));

            return errors;
        }

        public List<string> dateTime(string str)
        {
            List<string> errors = new List<string>();
            bool isDateTime = false;
            try
            {
                Convert.ToDateTime(str);
                isDateTime = true;
            }
            catch(Exception ex)
            {
                isDateTime = false;
            }

            if(!isDateTime)
                errors.Add("(Coloana " + dataset + ") - Valoarea pentru acest camp trebuie sa fie de tip DateTime!");

            return errors;
        }

        public List<string> daysOfTheWeekType(string str)
        {
            List<string> errors = new List<string>();
            string pattern = "((SU|MO|TU|WE|TH|FR|SA)to(SU|MO|TU|WE|TH|FR|SA))|(SU|MO|TU|WE|TH|FR|SA|XB|IB|WD|WN)";

            if (!matchRegex(pattern, str))
                errors.Add(getRegexErrorMessage(pattern));

            return errors;
        }

        public List<string> durationType(string str)
        {
            List<string> errors = new List<string>();
            string[] enumeration = { "N", "H", "D", "W", "M", "Q", "S", "Y", "O" };

            if (!inEnumeration(str, enumeration))
                errors.Add(getInEnumerationErrorMessage(str, enumeration));

            return errors;
        }

        public List<string> eic(string str)
        {
            List<string> errors = new List<string>();
            int minLen = 16;
            int maxLen = 16;
            string pattern = "[0-9][0-9][XYZTWV].+";

            if (!minLength(str, minLen))
                errors.Add(getMinLengthErrorMessage(minLen));

            if (!maxLength(str, maxLen))
                errors.Add(getMaxLengthErrorMessage(maxLen));

            if (!matchRegex(pattern, str))
                errors.Add(getRegexErrorMessage(pattern));

            return errors;
        }

        public List<string> energyCommodityType(string str)
        {
            List<string> errors = new List<string>();
            string[] enumeration = { "EL", "NG" };

            if (!inEnumeration(str, enumeration))
                errors.Add(getInEnumerationErrorMessage(str, enumeration));

            return errors;
        }

        public List<string> extraType(string str)
        {
            List<string> errors = new List<string>();
            int maxLen = 1000;
            string pattern = @"((\w+==((\d+\.\d+)|(\d+)|(\w+));)+(\w+==((\d+\.\d+)|(\d+)|(\w+))))";

            if (!maxLength(str, maxLen))
                errors.Add(getMaxLengthErrorMessage(maxLen));

            if (!matchRegex(pattern, str))
                errors.Add(getRegexErrorMessage(pattern));

            return errors;
        }

        public List<string> fixingIndexType(string str)
        {
            List<string> errors = new List<string>();
            int maxLen = 150;
            string pattern = "[A-Za-z0-9_ -]+";

            if (!maxLength(str, maxLen))
                errors.Add(getMaxLengthErrorMessage(maxLen));

            if (!matchRegex(pattern, str))
                errors.Add(getRegexErrorMessage(pattern));

            return errors;
        }

        public List<string> gln(string str)
        {
            List<string> errors = new List<string>();
            int minLen = 13;
            int maxLen = 13;
            string pattern = "[A-Za-z0-9_-]+";

            if (!minLength(str, minLen))
                errors.Add(getMinLengthErrorMessage(minLen));

            if (!maxLength(str, maxLen))
                errors.Add(getMaxLengthErrorMessage(maxLen));

            if (!matchRegex(pattern, str))
                errors.Add(getRegexErrorMessage(pattern));

            return errors;
        }

        public List<string> lei(string str)
        {
            List<string> errors = new List<string>();
            int minLen = 20;
            int maxLen = 20;
            string pattern = "[A-Za-z0-9_]+";

            if (!minLength(str, minLen))
                errors.Add(getMinLengthErrorMessage(minLen));

            if (!maxLength(str, maxLen))
                errors.Add(getMaxLengthErrorMessage(maxLen));

            if (!matchRegex(pattern, str))
                errors.Add(getRegexErrorMessage(pattern));

            return errors;
        }

        public List<string> mic(string str)
        {
            List<string> errors = new List<string>();
            int minLen = 4;
            int maxLen = 4;
            string pattern = "[A-Za-z0-9_]+";

            if (!minLength(str, minLen))
                errors.Add(getMinLengthErrorMessage(minLen));

            if (!maxLength(str, maxLen))
                errors.Add(getMaxLengthErrorMessage(maxLen));

            if (!matchRegex(pattern, str))
                errors.Add(getRegexErrorMessage(pattern));

            return errors;
        }

        public List<string> notionalQuantityUnitType(string str)
        {
            List<string> errors = new List<string>();
            string[] enumeration = { "KWh", "MWh", "GWh", "Therm", "KTherm", "MTherm", "cm", "mcm", "Btu", "MMBtu", "MJ", "100MJ", "MMJ", "GJ" };

            if (!inEnumeration(str, enumeration))
                errors.Add(getInEnumerationErrorMessage(str, enumeration));

            return errors;
        }

        public List<string> number(string str)
        {
            List<string> errors = new List<string>();

            try
            {
                double nr = Convert.ToDouble(str);
            }
            catch(Exception ex)
            {
                errors.Add("Valoarea nu este numerica!");
            }
            
            return errors;
        }
        
        public List<string> optionStyleType(string str)
        {
            List<string> errors = new List<string>();
            string[] enumeration = { "A", "B", "E", "S", "O" };

            if (!inEnumeration(str, enumeration))
                errors.Add(getInEnumerationErrorMessage(str, enumeration));

            return errors;
        }

        public List<string> optionTypeType(string str)
        {
            List<string> errors = new List<string>();
            string[] enumeration = { "C", "P", "O" };

            if (!inEnumeration(str, enumeration))
                errors.Add(getInEnumerationErrorMessage(str, enumeration));

            return errors;
        }

        public List<string> orderConditionsType(string str)
        {
            List<string> errors = new List<string>();
            string[] enumeration = { "AON", "FAF", "FAK", "FOK", "HVO", "MEV", "OCO", "PRE", "PRI", "PTR", "SLO", "OTH" };

            if (!inEnumeration(str, enumeration))
                errors.Add(getInEnumerationErrorMessage(str, enumeration));

            return errors;
        }

        public List<string> orderDurationsType(string str)
        {
            List<string> errors = new List<string>();
            string[] enumeration = { "DAY", "GTC", "GTD", "GTT", "SES", "OTH" };

            if (!inEnumeration(str, enumeration))
                errors.Add(getInEnumerationErrorMessage(str, enumeration));

            return errors;
        }

        public List<string> orderIdentifierType(string str)
        {
            List<string> errors = new List<string>();
            int maxLen = 100;
            string pattern = "[A-Za-z0-9_ -]+";

            if (!maxLength(str, maxLen))
                errors.Add(getMaxLengthErrorMessage(maxLen));

            if (!matchRegex(pattern, str))
                errors.Add(getRegexErrorMessage(pattern));

            return errors;
        }

        public List<string> orderStatusType(string str)
        {
            List<string> errors = new List<string>();
            string[] enumeration = { "ACT", "COV", "EXP", "MAC", "PMA", "REF", "SUS", "WIT", "OTH" };

            if (!inEnumeration(str, enumeration))
                errors.Add(getInEnumerationErrorMessage(str, enumeration));

            return errors;
        }

        public List<string> orderTypesType(string str)
        {
            List<string> errors = new List<string>();
            string[] enumeration = { "BLO", "CON", "COM", "EXC", "FHR", "IOI", "LIM", "LIN", "LIS", "MAR", "MTL", "SMA", "SPR", "STP", "VBL", "OTH" };

            if (!inEnumeration(str, enumeration))
                errors.Add(getInEnumerationErrorMessage(str, enumeration));

            return errors;
        }

        public List<string> quantityUnitType(string str)
        {
            List<string> errors = new List<string>();
            string[] enumeration = { "KW", "KWh/h", "KWh/d", "MW", "MWh/h", "MWh/d", "GW", "GWh/h", "GWh/d", "Therm/d", "KTherm/d", "MTherm/d", "cm/d", "mcm/d", "Btu/d", "MMBtu/d", "MJ/d", "100MJ/d", "MMJ/d", "GJ/d" };

            if (!inEnumeration(str, enumeration))
                errors.Add(getInEnumerationErrorMessage(str, enumeration));

            return errors;
        }
        
        
        public List<string> recordSeqNumberType(string value)
        {
            List<string> errors = new List<string>();
            int minVal = 1;
            try
            {
                Convert.ToInt32(value);
            }
            catch(Exception ex)
            {
                errors.Add(getMinValueErrorMessage(minVal));
            }

            if (!minValue(Convert.ToInt32(value), minVal))
                errors.Add(getMinValueErrorMessage(minVal));

            return errors;
        }
        
        public List<string> restrictedString(string str)
        {
            List<string> errors = new List<string>();
            int maxLen = 100;
            string pattern = "[A-Za-z0-9_ ]+";

            if (!maxLength(str, maxLen))
                errors.Add(getMaxLengthErrorMessage(maxLen));

            if (!matchRegex(pattern, str))
                errors.Add(getRegexErrorMessage(pattern));

            return errors;
        }

        public List<string> settlementMethodType(string str)
        {
            List<string> errors = new List<string>();
            string[] enumeration = { "P", "C", "O" };

            if (!inEnumeration(str, enumeration))
                errors.Add(getInEnumerationErrorMessage(str, enumeration));

            return errors;
        }

        public List<string> time(string str)
        {
            List<string> errors = new List<string>();
            DateTime ignored;
            bool isTime = false;
            try
            {
                isTime = DateTime.TryParseExact(str, "HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture,  System.Globalization.DateTimeStyles.None, out ignored);
            }
            catch(Exception ex)
            {
                isTime = false;
            }
            if(!isTime)
                errors.Add("(Coloana " + dataset + ") - Valoarea pentru acest camp trebuie sa fie de tip Ora:Minute:Secunde (HH:mm:ss)");

            return errors;
        }

        public List<string> traderCode(string str)
        {
            List<string> errors = new List<string>();
            int maxLen = 100;
            string pattern = "[A-Za-z0-9_ -]+";

            if (!maxLength(str, maxLen))
                errors.Add(getMaxLengthErrorMessage(maxLen));

            if (!matchRegex(pattern, str))
                errors.Add(getRegexErrorMessage(pattern));

            return errors;
        }

        public List<string> tradingCapacityType(string str)
        {
            List<string> errors = new List<string>();
            string[] enumeration = { "P", "A" };

            if (!inEnumeration(str, enumeration))
                errors.Add(getInEnumerationErrorMessage(str, enumeration));

            return errors;
        }

        public List<string> uniqueTransactionIdentifierType(string str)
        {
            List<string> errors = new List<string>();
            int maxLen = 100;
            string pattern = "[A-Za-z0-9_ -]+";

            if (!maxLength(str, maxLen))
                errors.Add(getMaxLengthErrorMessage(maxLen));

            if (!matchRegex(pattern, str))
                errors.Add(getRegexErrorMessage(pattern));

            return errors;
        }
    }
}