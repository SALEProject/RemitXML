using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;
using System.ServiceModel.Security.Tokens;
using ARISClient.ARISWSTestFramework;

namespace ARISClient
{
    class ARISWS
    {
        public string str_log;
        string str_serviceurl = "https://testframework.test-acer-remit.eu/dci-ws/aris.wsdl";
        string str_certPath = "BRM_DCI_WEB.pfx";
        string str_serverCertificate = "server.cer";
        string str_Password = "acerag";
        string str_ReportingEntity = "B00020987.RO";
        string str_Identity = "459170";

        //ARISWSTestFramework.HeaderType REMIT_RequestHeader; // header standard umplut cu datele BRM
        ARISWSTestFramework.arisservicesClient ws_REMIT; // obiectul de tip proxy
        X509Certificate2 certif, serverCerf; // certificatul digital

        private System.ServiceModel.Channels.Binding GetCustomBinding()
        {
            /*System.ServiceModel.Channels.SymmetricSecurityBindingElement sbe = new SymmetricSecurityBindingElement();
            sbe.MessageSecurityVersion = MessageSecurityVersion.Default;
            sbe.MessageProtectionOrder = MessageProtectionOrder.SignBeforeEncrypt;
            sbe.SecurityHeaderLayout = SecurityHeaderLayout.Strict;
            sbe.DefaultAlgorithmSuite = SecurityAlgorithmSuite.Basic128Rsa15;
            sbe.ProtectionTokenParameters = new System.ServiceModel.Security.Tokens.X509SecurityTokenParameters { InclusionMode = SecurityTokenInclusionMode.Never };
            */

            System.ServiceModel.Channels.AsymmetricSecurityBindingElement asbe = new AsymmetricSecurityBindingElement();
            asbe.MessageSecurityVersion = MessageSecurityVersion.WSSecurity10WSTrust13WSSecureConversation13WSSecurityPolicy12BasicSecurityProfile10;
            //MessageSecurityVersion.WSSecurity10WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11BasicSecurityProfile10;            
            // WSSecurity11WSTrust13WSSecureConversation13WSSecurityPolicy12;
            
            asbe.InitiatorTokenParameters = new System.ServiceModel.Security.Tokens.X509SecurityTokenParameters { InclusionMode = SecurityTokenInclusionMode.Never };
            asbe.RecipientTokenParameters = new System.ServiceModel.Security.Tokens.X509SecurityTokenParameters { InclusionMode = SecurityTokenInclusionMode.Never };
            asbe.MessageProtectionOrder = System.ServiceModel.Security.MessageProtectionOrder.SignBeforeEncrypt;          

            asbe.SecurityHeaderLayout = SecurityHeaderLayout.Strict;
            asbe.EnableUnsecuredResponse = true;
            asbe.IncludeTimestamp = false;
            asbe.SetKeyDerivation(false);
            asbe.ProtectTokens = false;
            asbe.DefaultAlgorithmSuite = System.ServiceModel.Security.SecurityAlgorithmSuite.Basic128Rsa15;
            //asbe.EndpointSupportingTokenParameters.Signed.Add(new UserNameSecurityTokenParameters());
            asbe.EndpointSupportingTokenParameters.Signed.Add(new X509SecurityTokenParameters());

            /*var securityElement = SecurityBindingElement.CreateMutualCertificateBindingElement(MessageSecurityVersion.WSSecurity10WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11BasicSecurityProfile10, true);
            securityElement.IncludeTimestamp = true;
            securityElement.EnableUnsecuredResponse = true;
            securityElement.ProtectTokens = false;
            securityElement.DefaultAlgorithmSuite = System.ServiceModel.Security.SecurityAlgorithmSuite.Basic128Rsa15;
            securityElement.EndpointSupportingTokenParameters.Signed.Add(new X509SecurityTokenParameters());*/

            CustomBinding myBinding = new CustomBinding();
            myBinding.Elements.Add(asbe);
            //myBinding.Elements.Add(securityElement);
            myBinding.Elements.Add(new TextMessageEncodingBindingElement(MessageVersion.Soap11, Encoding.UTF8));

            HttpsTransportBindingElement httpsBindingElement = new HttpsTransportBindingElement();
            httpsBindingElement.RequireClientCertificate = true;
            myBinding.Elements.Add(httpsBindingElement);

            return myBinding;
        }
        
        public void initWS()
        {
            // se creeaza obiectul de tip proxy       
            /*System.ServiceModel.BasicHttpsBinding binding = new System.ServiceModel.BasicHttpsBinding();            
            binding.Security.Mode = System.ServiceModel.BasicHttpsSecurityMode.TransportWithMessageCredential;
            binding.Security.Message.AlgorithmSuite = System.ServiceModel.Security.SecurityAlgorithmSuite.Basic128Rsa15;
            binding.Security.Message.ClientCredentialType = System.ServiceModel.BasicHttpMessageCredentialType.Certificate;*/
            System.ServiceModel.Channels.Binding binding = GetCustomBinding();
            
            // se creeaza obiectul de tip certificat
            certif = new System.Security.Cryptography.X509Certificates.X509Certificate2(str_certPath, str_Password);
            serverCerf = new System.Security.Cryptography.X509Certificates.X509Certificate2(str_serverCertificate, "");

            // service address
            EndpointAddress address = new EndpointAddress(new Uri(str_serviceurl), EndpointIdentity.CreateDnsIdentity("ACER"));
            
            //  create the proxy and add certificate
            ws_REMIT = new ARISWSTestFramework.arisservicesClient(binding, address);
            ws_REMIT.ClientCredentials.ServiceCertificate.DefaultCertificate = serverCerf;
            ws_REMIT.ClientCredentials.ServiceCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.None;
            ws_REMIT.ClientCredentials.ClientCertificate.Certificate = certif;
            ws_REMIT.Endpoint.Contract.ProtectionLevel = System.Net.Security.ProtectionLevel.Sign;
            
            reset_log();
        }

        public ARISWSTestFramework.HeaderType getHeader()
        {
            ARISWSTestFramework.HeaderType REMIT_RequestHeader = new ARISWSTestFramework.HeaderType();
            REMIT_RequestHeader.ReportingEntity = str_ReportingEntity;
            REMIT_RequestHeader.Identity = str_Identity;
            REMIT_RequestHeader.qualifiedDateTime = DateTime.Now;
            REMIT_RequestHeader.qualifiedDateTimeSpecified = true;

            return REMIT_RequestHeader;
        }

        public void reset_log()
        {
            str_log = "";
        }
        public void add_log(string str)
        {
            str_log = str_log + str + Environment.NewLine;
        }

        // verificat validitatea "str_filename" si returneaza in caz de succes "validationKey" 
        // care este folosit ulterior in "UploadData"
        public string NameConvetionCheck(string str_filename)
        {
            add_log("REMIT NameConventionCheck:");

            //            ws_REMIT.Open();
            //            add_log("State="+ws_REMIT.State.ToString());

            // se creaza obiectul de tip Request si RequestMessage pentru NameConventionCheck
            ARISWSTestFramework.NameConventionCheckRequestMessage req_NameMsg = new ARISWSTestFramework.NameConventionCheckRequestMessage();
            req_NameMsg.header = getHeader(); // set header cerere 
            req_NameMsg.fileName = str_filename; // nume fisier de verificat

            try
            {
                //File.WriteAllBytes("server.cer", Convert.FromBase64String("MIIDrDCCApSgAwIBAgIJAJTB5rvAogj6MA0GCSqGSIb3DQEBBQUAMGUxCzAJBgNVBAYTAklUMRYwFAYDVQQIDA1Nb256YSBCcmlhbnphMRMwEQYDVQQKDApMdXRlY2ggc3BhMQ0wCwYDVQQLDARDQ0VBMRowGAYDVQQDDBFDRVJFTVAgU1RBR0lORyBDQTAeFw0xNDExMDYxNjQzMTNaFw0xNzExMDUxNjQzMTNaMEkxCzAJBgNVBAYTAkVVMQ0wCwYDVQQIDARBQ0VSMQ0wCwYDVQQKDARBQ0VSMQ0wCwYDVQQLDARBQ0VSMQ0wCwYDVQQDDARBQ0VSMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA3mzejnnuDKW2BpjTUHAmrCqLR+daXE1WwTkyBBh57tdh8+FSk92ai+Lqgz3gZfnG1LwUj7GRs31HRpeTsJQKOhCkvqZxh5gz2jVu4aAibPnOmOoK+AD+a/G+VQjnRIQ/vtf8i4ghoEYIwEhfOumH8WfUlHzv9BwS8QDMiJ7A6fHFejp7jM7YRkKQdK9QiTTt9vfCCQnet2/ail3IJfsRMkpM/5G1/6MxkNHT7pO0/4ZD7Z4hFCDiTBJuaCz4eo66fEPSObG0aU/4ISKmolhvoaqa4eSRNDIjIi0q0PATa4jsZyAQgAhsCU56XCnKegciVB6tRYe/XcR/1ghHUfXs7QIDAQABo3sweTAJBgNVHRMEAjAAMCwGCWCGSAGG+EIBDQQfFh1PcGVuU1NMIEdlbmVyYXRlZCBDZXJ0aWZpY2F0ZTAdBgNVHQ4EFgQUzFPSzTFt0loASAemlojuYR9LkSswHwYDVR0jBBgwFoAUxaBstQ+vgmkuBIXX1cTcvWNFb90wDQYJKoZIhvcNAQEFBQADggEBAEMDfD1JLrRpFnsmOiCxqA8CSHqlIoS/ZYQJ3t6Qcs3H9BpIf82Xno8xg5WM9bzhHfOVnbhLKKo+sgSU9/CPmxKifE/DkZDY/178f06qYj95sWc/E7KuvOa6hvIASOez9Rl4iUyj44wEsCI9RvmbPE2ShG7NCP0S25orcfqeYltVjEjNddjEQeRqnO26wCOiAT6LRHT08JuzVvIcXMfVrxYD4M5JpU38jo1uqo6CQQc6B5u9n5vhmb8uSjTrL8+w5MhHSJ+eOtIhnJMP9CHt/KW3Ao9C6Vjpqxiy4WBkSvS6ARDCsDll3PSsJJGqcK/iB5x3e9g0oP2Z/cWxtOdGV14="));

                // se apeleaza proxy-ul cu parametrii construiti anterior
                ARISWSTestFramework.NameConventionCheckResponseMessage res_Name = ws_REMIT.NameConventionCheck(req_NameMsg);
                if (!res_Name.valid)
                {
                    add_log("NameConventionCheck:");
                    add_log("Valid=" + res_Name.valid.ToString());
                    add_log("Msg=" + res_Name.validationErrorDescription);
                    return "";
                }
                else return res_Name.validationKey;
            }
            catch (Exception e)
            {
                add_log("Exception: " + e.Message);
                //add_log("InnerExcp: " + e.InnerException.Message);
                return "";
            }

        }

        // primeste ca parametru "validationKey" valid,numele fisierului si datele de incarcare si trimite la remit fisierul
        // intoarce un cod de incarcare care se foloseste in analiza stadiului de elaborare/verificare
        public string UploadData(string str_validationKey, string str_filename, byte[] b_UploadData)
        {
            string str_LoadCode = "-1";

            ARISWSTestFramework.UploadDataRequestMessage req_UpMsg = new ARISWSTestFramework.UploadDataRequestMessage();
            req_UpMsg.header = getHeader();

            req_UpMsg.fileName = str_filename;
            req_UpMsg.uploadedData = b_UploadData;
            req_UpMsg.validationKey = str_validationKey;

            ARISWSTestFramework.UploadDataRequest req_Up = new ARISWSTestFramework.UploadDataRequest(req_UpMsg);

            try
            {
                // se apeleaza proxy-ul cu parametrii construiti anterior
                ARISWSTestFramework.UploadDataResponseMessage res_Up = ws_REMIT.UploadData(req_UpMsg);
                if (!res_Up.valid)
                {
                    add_log("UploadData:");
                    add_log("Valid=" + res_Up.valid.ToString());
                    add_log("Msg=" + res_Up.validationErrorDescription);
                    str_LoadCode = "-1";
                }
                else
                    str_LoadCode = res_Up.loadCode;
            }
            catch (Exception e)
            {
                str_LoadCode = "-1";
                add_log("Exception: " + e.Message);
                add_log("InnerExcp: " + e.InnerException.Message);
            }


            return (str_LoadCode);
        }


        // primeste parametru codul de incarcare "LoadCode" 
        // intoarce stadiul/resultatul aprobarii
        public string ElaborationStatus(string str_LoadCode)
        {
            string str_result = "-1";


            ARISWSTestFramework.ElaborationStatusRequestMessage req_StatMsg = new ARISWSTestFramework.ElaborationStatusRequestMessage();
            req_StatMsg.header = getHeader();

            req_StatMsg.loadCode = str_LoadCode;
            req_StatMsg.receiptType = ElaborationStatusRequestMessageReceiptType.RRM_RESTRICTED;

            ARISWSTestFramework.ElaborationStatusRequest req_Stat = new ARISWSTestFramework.ElaborationStatusRequest(req_StatMsg);

            try
            {
                // se apeleaza proxy-ul cu parametrii construiti anterior
                ARISWSTestFramework.ElaborationStatusResponseMessage res_Stat = ws_REMIT.ElaborationStatus(req_StatMsg);
                switch (res_Stat.result)
                {
                    case ElaborationStatusResponseMessageResult.COMPLETE:
                        str_result = "COMPLETE";
                        add_log("Elaboration Status: COMPLETE");
                        break;
                    case ElaborationStatusResponseMessageResult.FAILED:
                        str_result = "FAILED";
                        add_log("Elaboration Status: FAILED");
                        break;
                    case ElaborationStatusResponseMessageResult.ONGOING:
                        str_result = "ONGOING";
                        add_log("Elaboration Status: ONGOING");
                        break;
                }

            }
            catch (Exception e)
            {
                str_result = "-1";
                add_log("Exception: " + e.Message);
                add_log("InnerExcp: " + e.InnerException.Message);
            }


            return (str_result);
        }
    }
}
