using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Xml;
using Kontrax.Regux.Data;
using Kontrax.Regux.Model;
using Kontrax.Regux.Model.Administration;
using Kontrax.Regux.Model.Audit;
using Kontrax.Regux.Model.Batch;
using Kontrax.Regux.Model.Certificate;
using Kontrax.Regux.RegiXClient;
using Kontrax.Regux.RegiXClient.Model;
using Kontrax.Xml;

namespace Kontrax.Regux.Service
{
    public class CallService : BaseService
    {
        #region Изпращане на група от заявки

        /// <summary>
        /// Изпраща всички неизпратени заявки от подадения случай, които не са отказани.
        /// Ако има проблеми във входните данни на една заявка(стъпка), не изпраща нито една.
        /// </summary>
        public async Task<BatchEditModel> BatchCallAsync(int batchId, UserPermissionsModel currentUser, AuditModel auditModel)
        {
            BatchEditModel model;
            using (BatchService service = new BatchService())
            {
                model = await service.GetBatchForEditAsync(batchId, currentUser);
            }

            string eAuthToken;
            using (EAuthService service = new EAuthService())
            {
                eAuthToken = await service.GetLastResponseForRegiXAsync(currentUser.UserId);
            }

            if (!ValidateBatch(model))
            {
                return model;  // Моделът е попълнен с грешките от валидацията. Трябва да се покаже същата страница, но с грешките.
            }

            // Master entity-то на логовете от тип RegiX* е batch-ът.
            auditModel.EntityName = nameof(Batch);
            auditModel.EntityRecordId = batchId.ToString();

            foreach (RequestEditModel step in model.Steps.Where(s => s.IsPending))
            {
                if (step.Dependency.Permission.IsAllowedForCurrentUser)
                {
                    int requestId = step.Id.Value;
                    // Това би могло да хвърли най-различни exception-и. Грешките при обръщение към RegiX
                    // се записват в базата данни към съответната заявка; останалите грешки - не.
                    await CallAsync(requestId, currentUser, eAuthToken, auditModel.Clone());
                }
                else
                {
                    // TODO: Създаване на задача, която да се изпълни от друг потребител.
                }
            }
            return null;  // Извикването на RegiX е минало успешно и трябва да се направи redirect.
        }

        private static bool ValidateBatch(BatchEditModel model)
        {
            if (string.IsNullOrEmpty(model.ServiceUri))
            {
                model.ServiceUriErrorMessage = "Полето е задължително. Моля въведете стойност.";
            }

            // Вече изпратените или отказани заявки не се валидират. Заявките, които ще се изпращат от друг потребител - също.
            bool stepsAreValid = true;
            foreach (RequestEditModel step in model.Steps.Where(s => s.IsPending && s.Dependency.Permission.IsAllowedForCurrentUser))
            {
                List<string> errors = step.ValidationErrors;
                if (!step.Id.HasValue)
                {
                    errors.Add("Не е заредено id-то на заявката.");
                }
                XsdModel xsd = step.Xsd;
                if (xsd != null)
                {
                    if (xsd.Root != null && !xsd.Root.ValidateRequiredFields())
                    {
                        errors.Add("Някои полета не са попълнени.");
                    }
                    // Насрещна проверка дали генерираният XML отговаря на схемата, по която е бил генериран формулярът.
                    errors.AddRange(XsdUtil.ValidateText(step.XmlText, xsd.Path));
                }
                stepsAreValid &= errors.Count == 0;
            }

            return string.IsNullOrEmpty(model.ServiceUriErrorMessage) && stepsAreValid;
        }

        #endregion

        #region Единично обръщение към RegiX

        public async Task<string> RegiXTestConnectionAsync(string eAuthToken)
        {
            // Примерни данни са взети от http://regixaisweb.egov.bg/RegiXInfo/RegiXGuides/#!Documents/executesynchronous.htm
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(
                @"<ValidPersonRequest
                    xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
                    xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                    xmlns=""http://egov.bg/RegiX/GRAO/NBD/ValidPersonRequest"">
                    <EGN>8506258485</EGN>
                </ValidPersonRequest>");

            RegiXResponse regiXResponse = await RegiXUtil.CallAsync(
                "TechnoLogica.RegiX.GraoNBDAdapter.APIService.INBDAPI.ValidPersonSearch",
                doc.DocumentElement,
                new CallContext(
                    null,  // Използва default сертификата от конфигурационния файл.
                    "Администрация на Министерски съвет",
                    "2.16.100.1.1.43",
                    "myusername@administration.domain",
                    "Първо Второ Трето",
                    "Експерт в отдел",
                    "На основание чл. X от Наредба/Закон/Нормативен акт",
                    "123-12345-01.01.2017",
                    eAuthToken
                ));
            return regiXResponse.Error + Environment.NewLine + regiXResponse.XmlText;
        }

        private async Task CallAsync(int requestId, UserPermissionsModel currentUser, string eAuthToken, AuditModel auditModel)
        {
            // Зареждане на заявката.
            Request request = await _db.Requests.
                Include(r => r.Batch.Administration).
                Include(r => r.Batch.Activity.Service).
                Include(r => r.RegiXReport).
                FirstOrDefaultAsync(r => r.Id == requestId);
            MustExist(request, "заявка", requestId);

            // Повторна проверка на правата (първата е при зареждане на екрана, от който се извиква този метод).
            Batch batch = request.Batch;
            int administrationId = batch.AdministrationId;
            IdNameModel admIdName = new IdNameModel(administrationId, batch.Administration.Name);
            RegiXReportPermissionModel permission = currentUser.RegiXReportIsAllowed(request.RegiXReportId, admIdName);
            if (!permission.IsAllowedForCurrentUser)
            {
                throw new Exception(permission.Explanation);
            }

            X509Certificate2 certificate;
            using (AdministrationManagementService admService = new AdministrationManagementService())
            {
                AdministrationCertificate cert = await _db.AdministrationCertificates.FindAsync(administrationId, CertTypeCode.RegiX.ToString());
                certificate = cert != null ? new X509Certificate2(cert.Data, cert.Password) : null;
            }

            RegiXReport regiXReport = request.RegiXReport;
            List<string> configErrors = GetRegiXReportRequestConfigErrors(regiXReport);
            if (configErrors.Count > 0)
            {
                throw new Exception(string.Join(", ", configErrors));
            }

            // Четенето на останалите данни за потребителя е нужно само заради потребителското име.
            AspNetUser user = await _db.AspNetUsers.FindAsync(currentUser.UserId);
            MustExist(user, "потребител", currentUser.UserId);

            // Няма изрично поле за длъжност в базата данни, затова длъжността се извлича от други
            // свойства на потребителя - ролята в рамките на администрацията (ако има) или нивото на достъп.
            WorkplaceModel workplace = currentUser.GetWorkplace(administrationId);
            string employeePosition = workplace != null
                ? workplace.LocalRoleId.HasValue ? workplace.LocalRoleName : workplace.AccessLevelName
                : currentUser.IsGlobalAdmin ? Role.GlobalAdminName : null;

            // Зареждане на правното основание.
            string legalBasis = await (
                from d in _db.Dependencies
                where d.ActivityId == batch.ActivityId && d.RegiXReportId == request.RegiXReportId
                select d.LegalBasis
            ).FirstOrDefaultAsync();

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(request.Argument);

            await LogCallStartAsync(request, auditModel);

            // Обръщение към RegiX.
            try
            {
                RegiXResponse regiXResponse = await RegiXUtil.CallAsync(
                    regiXReport.Operation,
                    doc.DocumentElement,
                    // Описание на структурата има тук:
                    // http://regixaisweb.egov.bg/RegiXInfo/RegiXGuides/#!Documents/callcontext.htm
                    new CallContext(
                        certificate,
                        batch.Administration.Name,
                        batch.Administration.Oid,
                        user.UserName,  // или user.Email, защото като пример беше дадено "myusername@administration.domain",
                        currentUser.DisplayName,
                        employeePosition,
                        legalBasis,
                        batch.ServiceUri,  // Номер на преписка или друг уникален регистрационен индекс.
                        eAuthToken
                    ));

                // Ако в резултата има съобщение за грешка, се създават два журнални записа - за резултата и за грешката.
                await LogCallResponseAsync(request, regiXResponse.XmlText, auditModel);
                if (!string.IsNullOrEmpty(regiXResponse.Error))
                {
                    throw new Exception("Отговор от RegiX: " + regiXResponse.Error);
                }
            }
            catch (Exception ex)
            {
                await LogCallErrorAsync(request, ex.Message, auditModel);
                throw;
            }
        }

        /// <summary>
        /// Проверява дали извикването на удостоверението е технически възможно.
        /// </summary>
        internal static List<string> GetRegiXReportRequestConfigErrors(RegiXReport regiXReport)
        {
            List<string> errorMessages = new List<string>();

            if (string.IsNullOrEmpty(regiXReport.Operation) ||
                string.IsNullOrEmpty(regiXReport.AdapterSubdirectory) ||
                string.IsNullOrEmpty(regiXReport.RequestXsd))
            {
                errorMessages.Add($"RegiX справката \"{regiXReport.FullName}\" не е конфигурирана.");
                if (regiXReport != null)
                {
                    // Ако конфигурацията е попълнена частично, се посочва точният проблем.
                    if (string.IsNullOrEmpty(regiXReport.Operation))
                    {
                        errorMessages.Add("Не е посочена операцията в RegiX.");
                    }
                    if (string.IsNullOrEmpty(regiXReport.AdapterSubdirectory))
                    {
                        errorMessages.Add("Не е посочена папката на адаптера.");
                    }
                    if (string.IsNullOrEmpty(regiXReport.RequestXsd))
                    {
                        errorMessages.Add("Не е посочена схемата на заявката.");
                    }
                }
            }
            return errorMessages;
        }

        #endregion

        #region Журнал на обръщения към RegiX, резултати и грешки

        private async Task LogCallStartAsync(Request request, AuditModel auditModel)
        {
            // Проверка дали заявката вече е изпращана към RegiX - не трябва да има Start, End, Error и Response.
            // Проверката за наличие на Response се прави само ако останалите колони са празни.
            if (request.StartDateTime.HasValue || request.EndDateTime.HasValue || request.Error != null ||
                await _db.Responses.AnyAsync(r => r.RequestId == request.Id))
            {
                throw new Exception("Опит за изпращане на вече изпратена заявка. Ако е необходимо повторно изпращане, направете копие на заявката.");
            }

            request.StartDateTime = DateTime.Now;
            auditModel.AuditTypeCode = AuditTypeCode.RegiXRequest;

            auditModel.Request = AuditHashUtil.LoadFrom(request);
            request.RequestTimeStamp = TimeStampUtil.Timestamp(auditModel.Request);
            auditModel.Request.RequestTimeStampBytes = request.RequestTimeStamp;

            //логването променя auditModel.AuditTypeCode и добавя AuditDetails, затова клонираме обекта
            await SaveAndLogAsync(auditModel.Clone(), request.Batch, request.BatchId);
            Log(auditModel);
        }

        private async Task LogCallErrorAsync(Request request, string message, AuditModel auditModel)
        {
            request.EndDateTime = DateTime.Now;
            request.Error = message;

            auditModel.AuditTypeCode = AuditTypeCode.RegiXError;

            auditModel.Request = AuditHashUtil.LoadFrom(request);
            request.ResponseTimeStamp = TimeStampUtil.TimestampResponse(auditModel.Request);
            auditModel.Request.Response.ResponseTimeStampBytes = request.ResponseTimeStamp;

            //логването променя auditModel.AuditTypeCode и добавя AuditDetails, затова клонираме обекта
            await SaveAndLogAsync(auditModel.Clone(), request.Batch, request.BatchId);
            Log(auditModel);
        }

        private async Task LogCallResponseAsync(Request request, string responseXml, AuditModel auditModel)
        {
            var response = new Response
            {
                Request = request,
                Document = responseXml,
            };
            _db.Responses.Add(response);
            await SaveAndLogAsync(auditModel, request.Batch, request.BatchId);

            // датата трябва за изчслението на хеша, затова е преди записването на Audit
            request.EndDateTime = DateTime.Now;
            auditModel.AuditTypeCode = AuditTypeCode.RegiXResponse;

            auditModel.Request = AuditHashUtil.LoadFrom(request);
            request.ResponseTimeStamp = TimeStampUtil.TimestampResponse(auditModel.Request);
            auditModel.Request.Response.ResponseTimeStampBytes = request.ResponseTimeStamp;

            //логването променя auditModel.AuditTypeCode и добавя AuditDetails, затова клонираме обекта
            await SaveAndLogAsync(auditModel.Clone(), request.Batch, request.BatchId);
            Log(auditModel);
        }

        private async Task<Request> LoadRequestAsync(int requestId)
        {
            Request request = await _db.Requests.FindAsync(requestId);
            MustExist(request, "заявка", requestId);
            return request;
        }


        //private void LogCall(Request request, AuditModel auditModel)
        //{
        //    using (var auditService = new AuditService())
        //    {
        //        auditModel.Request = AuditHashUtil.LoadFrom(request);
        //        auditService.Add(auditModel);
        //    }
        //}
        #endregion
    }
}
