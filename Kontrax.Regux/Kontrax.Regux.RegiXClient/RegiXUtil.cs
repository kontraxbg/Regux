using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using System.Xml;
using Kontrax.Regux.RegiXClient.RegiXServiceReference;
using Kontrax.Xml;

namespace Kontrax.Regux.RegiXClient
{
    public static class RegiXUtil
    {
        public static async Task<Model.RegiXResponse> CallAsync(
            string operation,
            XmlElement argument,
            Model.CallContext context)
        {
            CallContext callContext = new CallContext
            {
                AdministrationName = context.AdministrationName,
                AdministrationOId = context.AdministrationOId,
                EmployeeIdentifier = context.EmployeeIdentifier,
                EmployeeNames = context.EmployeeNames,
                EmployeePosition = context.EmployeePosition,
                LawReason = context.LegalBasis,
                ServiceURI = context.ServiceUri,
                ServiceType = "За административна услуга",
                Remark = null,
                EmployeeAditionalIdentifier = null,
                ResponsiblePersonIdentifier = null
            };

            ServiceRequestData request = new ServiceRequestData
            {
                Operation = operation,
                Argument = argument,
                CallContext = callContext,
                CitizenEGN = null,
                EmployeeEGN = null,
                ReturnAccessMatrix = false,
                SignResult = true,
                CallbackURL = null,
                EIDToken = null
            };

            ServiceResultData response;
            using (RegiXEntryPointClient client = new RegiXEntryPointClient())
            {
                // Ако не е подаден сертификат, се използва този от конфигурационния файл.
                if (context.Certificate != null)
                {
                    client.ClientCredentials.ClientCertificate.Certificate = context.Certificate;
                }

                if (context.EAuthToken != null)
                {
                    // SAML token-ът от еАвт се изпраща като header. Примери, подводни камъни и решения са описани тук:
                    // http://managedmonkey.blogspot.bg/2012/12/operationcontextscope-and-asyncawait.html
                    // https://stackoverflow.com/questions/13189980/async-wcf-client-calls-with-custom-headers-this-operationcontextscope-is-being
                    // https://stackoverflow.com/questions/44192260/the-value-of-operationcontext-current-is-not-the-operationcontext-value-installe
                    Task<ServiceResultData> responseTask;
                    using (new OperationContextScope(client.InnerChannel))
                    {
                        AddSecurityHeader(context.EAuthToken, OperationContext.Current.OutgoingMessageHeaders);

                        // А) Ако в scope се изчака async версията на метода, възниква следната грешка:
                        // "The value of OperationContext.Current is not the OperationContext value installed by this OperationContextScope".
                        //response = await client.ExecuteSynchronousAsync(request);
                        // Б) Или трябва да се ползва НЕ-async версията на метода,
                        //response = client.ExecuteSynchronous(request);
                        // В) или трябва await-ът да се изведе извън scope-а, което не е ясно дали/защо работи.
                        responseTask = client.ExecuteSynchronousAsync(request);
                    }
                    response = await responseTask;
                }
                else
                {
                    response = await client.ExecuteSynchronousAsync(request);
                }
            }

            // TODO: Валидиране на response.Signature и връщане на информация чрез модела.

            string xmlText = response.ToBeautifiedXmlText();
            return new Model.RegiXResponse(xmlText, response.Error);
        }

        internal static void AddSecurityHeader(string token, MessageHeaders headers)
        {
            if (token != null)
            {
                MessageHeader header = MessageHeader.CreateHeader("Security",
                    "http://docs.oasis-open.org/wss/2004/01/oasis-200401wss-wssecurity-secext-1.0.xsd", token);
                headers.Add(header);
            }
        }

        /// <summary>
        /// Оригиналният примерен код е копиран от тук:
        /// http://regixaisweb.egov.bg/RegiXInfo/RegiXGuides/#!Documents/executesynchronous.htm
        /// </summary>
        private static async Task<string> DemoCallAsync()
        {
            string operation = "TechnoLogica.RegiX.GraoNBDAdapter.APIService.INBDAPI.ValidPersonSearch";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(
                @"<ValidPersonRequest
                    xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
                    xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                    xmlns=""http://egov.bg/RegiX/GRAO/NBD/ValidPersonRequest"">
                    <EGN>8506258485</EGN>
                </ValidPersonRequest>");

            CallContext callContext = new CallContext
            {
                AdministrationName = "Администрация на Министерски съвет",
                AdministrationOId = "2.16.100.1.1.43",
                EmployeeIdentifier = "myusername@administration.domain",
                EmployeeNames = "Първо Второ Трето",
                EmployeePosition = "Експерт в отдел",
                LawReason = "На основание чл. X от Наредба/Закон/Нормативен акт",
                Remark = "За тестване на системата",
                ServiceType = "За административна услуга",
                ServiceURI = "123-12345-01.01.2017"
            };

            ServiceRequestData request = new ServiceRequestData
            {
                Operation = operation,
                Argument = doc.DocumentElement,
                CallContext = callContext,
                CitizenEGN = "XXXXXXXXXX",
                EmployeeEGN = "XXXXXXXXXX",
                ReturnAccessMatrix = false,
                SignResult = true
            };

            ServiceResultData response;
            using (RegiXEntryPointClient client = new RegiXEntryPointClient())
            {
                response = await client.ExecuteSynchronousAsync(request);
            }

            if (response.HasError)
            {
                throw new Exception(response.Error);
            }
            return response.Data.Response.Any.OuterXml;
        }
    }
}
