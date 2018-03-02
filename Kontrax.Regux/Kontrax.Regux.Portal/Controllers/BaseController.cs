using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using Kontrax.Regux.Portal.Attributes;
using Kontrax.Regux.Portal.Audit;
using Kontrax.Regux.Portal.Identity;
using Kontrax.Regux.Portal.Util;
using Kontrax.Regux.Service;
using Microsoft.AspNet.Identity.Owin;

namespace Kontrax.Regux.Portal.Controllers
{
    public abstract class BaseController : Controller
    {
        protected IsolationLevel _transactionIsolationLevel = IsolationLevel.ReadCommitted;
        protected string _successMessage;

        #region Audit

        /// <summary>
        /// Идентификатор на одит записа или 0, ако не може да се извлече
        /// </summary>
        public int AuditId
        {
            get
            {
                int auditId = 0;
                if (RouteData.Values.ContainsKey("AuditID"))
                {
                    auditId = (int)RouteData.Values["AuditID"];
                }
                return auditId;
            }
        }

        protected Model.Audit.AuditModel GetAuditModel(AuditLevel auditLevel = AuditLevel.Minimal)
        {
            return AuditUtil.CreateModel(ControllerContext, auditLevel, Model.Audit.AuditTypeCode.Write);
        }
        #endregion

        #region Централно място на създаване на ApplicationUserManager
        protected ApplicationUserManager CreateUserManager()
        {
            ApplicationUserManager manager = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            manager.SetAuditContext(GetAuditModel(AuditLevel.All));
            return manager;
        }
        #endregion

        #region Общ вид на action с обработка на грешки и транзакционност.

        /// <summary>
        /// Този overload се използва, когато view-то се показва дори ако възникне грешка, обикновео при GET.
        /// </summary>
        protected Task<ActionResult> TryAsync(Func<Task> tryAsync, Func<ActionResult> then)
        {
            return TryAsync(false, tryAsync, () => Task.FromResult(then()), () => Task.FromResult(then()));
        }

        protected Task<ActionResult> TryAsync(Func<Task> tryAsync, Func<Task<ActionResult>> then)
        {
            return TryAsync(false, tryAsync, () => then(), () => then());
        }

        /// <summary>
        /// Този overload се използва при запис на нещо, обикновено с POST.
        /// </summary>
        protected Task<ActionResult> TryAsync(Func<Task> tryAsync, Func<ActionResult> success, Func<ActionResult> fail)
        {
            return TryAsync(true, tryAsync, () => Task.FromResult(success()), () => Task.FromResult(fail()));
        }

        /// <summary>
        /// Този overload се използва при запис на нещо, обикновено с POST.
        /// </summary>
        protected Task<ActionResult> TryAsync(Func<Task> tryAsync, Func<ActionResult> success, Func<Task<ActionResult>> failAsync)
        {
            return TryAsync(true, tryAsync, () => Task.FromResult(success()), failAsync);
        }

        private async Task<ActionResult> TryAsync(bool isWriteOperation, Func<Task> tryAsync, Func<Task<ActionResult>> successAsync, Func<Task<ActionResult>> failAsync)
        {
            if (tryAsync == null)
            {
                throw new ArgumentNullException("saveAsync");
            }
            if (successAsync == null)
            {
                throw new ArgumentNullException("successAsync");
            }
            if (failAsync == null)
            {
                throw new ArgumentNullException("failAsync");
            }

            if (!ModelState.IsValid)
            {
                Warning(string.Join(Environment.NewLine, ModelState.Values.SelectMany(s => s.Errors)
                    .Where(e => !string.IsNullOrEmpty(e.ErrorMessage) || e.Exception != null)
                    .Select(e => !string.IsNullOrEmpty(e.ErrorMessage) ? e.ErrorMessage : e.Exception.Message)));
                return await failAsync();
            }

            TransactionScope scope = new TransactionScope(TransactionScopeOption.Required,
                new TransactionOptions { IsolationLevel = _transactionIsolationLevel },
                // Тази опция е нова за .net 4.5.1 и позволява scope-ът за завърши в друга нишка.
                TransactionScopeAsyncFlowOption.Enabled);
            try
            {
                await tryAsync();
                scope.Complete();
                scope.Dispose();
            }
            catch (Exception ex)
            {
                scope.Dispose();
                string errorMessage = isWriteOperation ? "Грешка при записването" : "Грешка";
                LogAndShowError(errorMessage, ex);
                return await failAsync();
            }

            string successMessage = _successMessage ?? (isWriteOperation ? "Промените са записани успешно." : string.Empty);
            if (successMessage.Length > 0)  // Чрез изрично подаване на string.Empty съобщението може да се потисне.
            {
                Success(successMessage, true);
            }

            try
            {
                return await successAsync();
            }
            catch (Exception ex)
            {
                string errorMessage = isWriteOperation ? "Грешка след записването" : "Грешка след действието";
                LogAndShowError(errorMessage, ex);
                return await failAsync();
            }
        }

        #endregion

        #region Alerts

        protected void Success(string message, bool isDismissable = false)
        {
            AlertUtil.Success(TempData, message, isDismissable);
        }

        protected void Information(string message, bool isDismissable = false)
        {
            AlertUtil.Information(TempData, message, isDismissable);
        }

        protected void Warning(string message, bool isDismissable = false)
        {
            AlertUtil.Warning(TempData, message, isDismissable);
        }

        protected void Danger(string message, bool isDismissable = false)
        {
            AlertUtil.Danger(TempData, message, isDismissable);
        }

        /// <summary>
        /// Използва се в catch блок, за да запише грешката в базата данни и да я покаже на екрана в Danger панел.
        /// </summary>
        protected void LogAndShowError(string prefix, Exception ex)
        {
            Danger(LogAndGetError(prefix, ex));
        }

        /// <summary>
        /// За случаите при извикване с ajax, когато няма смисъл от стандартните alert функции.
        /// </summary>
        protected string LogAndGetError(string prefix, Exception ex)
        {
            if (ex == null)
            {
                throw new ArgumentNullException("ex");
            }

            // Записването на грешки не трябва да се rollback-ва. В случай че сме в scope, той не трябва да се ползва по време на логването.
            using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Suppress))
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                scope.Complete();
            }
            string messages = ex.Messages();
            return !string.IsNullOrEmpty(prefix) ? $"{prefix}: {messages}" : messages;
        }

        #endregion

        #region ViewData/ViewBag забранени

        public const string UseModelsInsteadOfViewData = "Ползвай модели вместо ViewData!";
        public const string UseModelsInsteadOfViewBag = "Ползвай модели вместо ViewBag!";

        /// <summary>
        /// Ползването на ViewData е лоша практика, затова property-то е анотирано, така че да извежда грешка
        /// на компилатора. Също така гърми runtime.
        /// Property-то ViewData на view-тата извежда само warning, защото се ползва вътрешно от Razor.
        /// </summary>
        [Obsolete(UseModelsInsteadOfViewData, true)]
        public new ViewDataDictionary ViewData
        {
            get => throw new Exception(UseModelsInsteadOfViewData);
            set => throw new Exception(UseModelsInsteadOfViewData);
        }

        /// <summary>
        /// Ползването на ViewBag е лоша практика, затова property-то е анотирано, така че да извежда грешка
        /// на компилатора. Също така гърми runtime.
        /// </summary>
        [Obsolete(UseModelsInsteadOfViewBag, true)]
        public new dynamic ViewBag
        {
            get => throw new Exception(UseModelsInsteadOfViewBag);
        }

        #endregion
    }
}
