using Kontrax.Regux.Portal.Attributes;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Web.Mvc;

namespace Kontrax.Regux.Portal.Controllers
{
    [Audit(AuditingLevel = AuditingLevelEnum.None)]
    public abstract class BaseController : Controller
    {
        protected IsolationLevel _transactionIsolationLevel = IsolationLevel.ReadCommitted;
        protected string _successMessage;

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

        #region Общ вид на POST action с обработка на грешки и транзакционност.

        protected Task<ActionResult> TryAsync(bool isWriteOperation, Func<Task> tryAsync, Func<ActionResult> success, Func<ActionResult> fail)
        {
            return TryAsync(isWriteOperation, tryAsync, () => Task.FromResult(success()), () => Task.FromResult(fail()));
        }

        protected Task<ActionResult> TryAsync(bool isWriteOperation, Func<Task> tryAsync, Func<ActionResult> success, Func<Task<ActionResult>> failAsync)
        {
            return TryAsync(isWriteOperation, tryAsync, () => Task.FromResult(success()), failAsync);
        }

        protected async Task<ActionResult> TryAsync(bool isWriteOperation, Func<Task> tryAsync, Func<Task<ActionResult>> successAsync, Func<Task<ActionResult>> failAsync)
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
                Warning(string.Join("<br />", ModelState.Values.SelectMany(s => s.Errors)
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
                LogAndShowErrorAsync(errorMessage, ex);
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
                LogAndShowErrorAsync(errorMessage, ex);
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
        protected void LogAndShowErrorAsync(string prefix, Exception ex)
        {
            Danger(LogAndGetErrorAsync(prefix, ex));
        }

        /// <summary>
        /// За случаите при извикване с ajax, когато няма смисъл от стандартните alert функции.
        /// </summary>
        protected string LogAndGetErrorAsync(string prefix, Exception ex)
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

            StringBuilder text = new StringBuilder(ex.Message);
            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
                text.Insert(0, ex.Message + "; ");
            }
            if (!string.IsNullOrEmpty(prefix))
            {
                text.Insert(0, prefix + ": ");
            }

            return text.ToString();
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
