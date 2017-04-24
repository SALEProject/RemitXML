using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemitXML
{
    class BusinessRules
    {
        private string dataset = "";

        public Dictionary<string, List<string>> check(List<Dictionary<string, string>> dataSets)
        {
            Dictionary<string, List<string>> errors = new Dictionary<string, List<string>>();
            int i = 0;
            foreach (var dataSet in dataSets)
            {
                i++;
                if (i == 1)
                {
                    dataset = "Order";
                    /*R2CDPRCMOSP();
                    R3CDQVCMSV();
                    R1DPDEDCHK();
                    R1DPLDINTCHK();*/
                }
                if (i == 2)
                {
                    dataset = "Trade";
                    /*R2CLTDTOT();
                    R2CLTDTDSTOT();
                    R2TRTDCONDED();
                    R2CDPRCMTSP();
                    R3CDPRBSPM();
                    R2CDQVNZ();
                    R4CDQVSVPT();*/
                }
            }

            return errors;
        }

        private string R2CLTDTOT(string transactionDT, string lasttradingDT)
        {
            bool hasError = false;
            string error = "";

            DateTime trzDT = new DateTime();
            DateTime tradDT = new DateTime();
            try
            {
                trzDT = Convert.ToDateTime(transactionDT);
                tradDT = Convert.ToDateTime(lasttradingDT);
            }
            catch (Exception ex)
            {
                hasError = true;
            }

            if (trzDT > tradDT)
                hasError = true;

            if(hasError)
                error = "Transaction timestamp greater than last trading time";

            return error;
        }

        private string R2CLTDTDSTOT(string transactionDT, string contractStartDate)
        {
            bool hasError = false;
            string error = "";

            DateTime trzDT = new DateTime();
            DateTime ctrDT = new DateTime();
            try
            {
                trzDT = Convert.ToDateTime(transactionDT);
                ctrDT = Convert.ToDateTime(contractStartDate);
            }
            catch (Exception ex)
            {
                hasError = true;
            }

            if (trzDT > ctrDT)
                hasError = true;

            if(hasError)
                error = "Transaction timestamp greater than contract delivery start date";

            return error;
        }

        private string R2TRTDCONDED(string tradeDT, string contractEndDate)
        {
            bool hasError = false;
            string error = "";

            DateTime trzDT = new DateTime();
            DateTime trdDT = new DateTime();
            try
            {
                trzDT = Convert.ToDateTime(tradeDT);
                trdDT = Convert.ToDateTime(contractEndDate);
            }
            catch (Exception ex)
            {
                hasError = true;
            }

            if (trzDT > trdDT)
                hasError = true;

            if (hasError)
                error = "Trade termination date greater than contract delivery end date";

            return error;
        }

        private string R2CDPRCMOSP(string orderPrice)
        {
            bool hasErrors = false;
            string error = "";

            double price = 0;
            try
            {
                price = Convert.ToDouble(orderPrice);
            }
            catch (Exception ex)
            {
                hasErrors = true;
            }

            if (price == 0)
                hasErrors = true;

            if (hasErrors)
                error = "Order price zero or not defined";

            return error;
        }

        private string R2CDPRCMTSP(string tradePrice)
        {
            bool hasErrors = false;
            string error = "";

            double price = 0;
            try
            {
                price = Convert.ToDouble(tradePrice);
            }
            catch (Exception ex)
            {
                hasErrors = true;
            }

            if (price == 0)
                hasErrors = true;

            if (hasErrors)
                error = "Trade price zero or not defined";

            return error;
        }

        private string R3CDPRBSPM(string orderPrice, string tradePrice)
        {
            bool hasErrors = false;
            string error = "";

            double ordPrice = 0;
            double trdPrice = 0;

            try
            {
                ordPrice = Convert.ToDouble(ordPrice);
                trdPrice = Convert.ToDouble(tradePrice);
            }
            catch (Exception ex)
            {
                hasErrors = true;
            }

            if (ordPrice!= trdPrice)
                hasErrors = true;

            if (hasErrors)
                error = "Order price and trade price do not match";

            return error;
        }

        private string R2CDQVNZ(string quantity)
        {
            bool hasErrors = false;
            string error = "";

            double qty = 0;
            try
            {
                qty = Convert.ToDouble(quantity);
            }
            catch (Exception ex)
            {
                hasErrors = true;
            }

            if (qty == 0)
                hasErrors = true;

            if (hasErrors)
                error = "Trade with invalid zero quantity";

            return error;
        }

        private string R3CDQVCMSV(string quantity)
        {
            bool hasErrors = false;
            string error = "";

            double qty = 0;
            try
            {
                qty = Convert.ToDouble(quantity);
            }
            catch (Exception ex)
            {
                hasErrors = true;
            }

            if (qty <= 0)
                hasErrors = true;

            if (hasErrors)
                error = "Order with invalid quantity";

            return error;
        }

        private string R4CDQVSVPT(string quantity)
        {
            bool hasErrors = false;
            string error = "";

            double qty = 0;
            try
            {
                qty = Convert.ToDouble(quantity);
            }
            catch (Exception ex)
            {
                hasErrors = true;
            }

            if (qty <= 0)
                hasErrors = true;

            if (hasErrors)
                error = "Trade with invalid quantity";

            return error;
        }

        private string R1DPDEDCHK(string ctrStartDate, string ctrEndDate)
        {
            bool hasError = false;
            string error = "";

            DateTime sDate = new DateTime();
            DateTime eDate = new DateTime();
            try
            {
                sDate = Convert.ToDateTime(ctrStartDate);
                eDate = Convert.ToDateTime(ctrEndDate);
            }
            catch (Exception ex)
            {
                hasError = true;
            }

            if (sDate > eDate)
                hasError = true;

            if (hasError)
                error = "Contract start date greater than contract end date";

            return error;
        }

        private string R1DPLDINTCHK(string ctrStartDate, string ctrEndDate, string loadStartTime, string loadEndTime)
        {
            bool hasError = false;
            string error = "";

            DateTime sDate = new DateTime();
            DateTime eDate = new DateTime();
            try
            {
                sDate = Convert.ToDateTime(ctrStartDate + "T" + loadStartTime);
                eDate = Convert.ToDateTime(ctrEndDate  + "T" + loadEndTime);
            }
            catch (Exception ex)
            {
                hasError = true;
            }

            if ((sDate >= eDate))
                hasError = true;

            if (hasError)
                error = "Load delivery start time greater than load delivery end time";

            return error;
        }
    }
}
