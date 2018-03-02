using System;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Kontrax.Regux.Model;
using Kontrax.Regux.Model.Report;
using Kontrax.Regux.Portal.Identity;
using Kontrax.Regux.Service;
using Kontrax.Regux.Shared.Portal.Identity;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;

namespace Kontrax.Regux.Portal.Controllers
{
    public class HomeController : BaseController
    {
        [HttpGet]
        public async Task<ActionResult> Index()
        {
            UserPermissionsModel currentUser = await User.GetPermissionsAsync();
            IncompleteBatchesViewModel model = new IncompleteBatchesViewModel { CurrentUser = currentUser };
            return await TryAsync(
                async () =>
                {
                    using (UserManagementService service = new UserManagementService())
                    {
                        model.UsersForApproval = await service.GetUsersAwaitingApprovalAsync(currentUser);
                    }
                    using (ReportService service = new ReportService())
                    {
                        model.IncompleteBatches = await service.GetIncompleteBatchesAsync(currentUser);
                    }
                    using (PublicSignalService service = new PublicSignalService())
                    {
                        model.PublicSignalsForReview = await service.GetUnresolvedSignals(currentUser);
                    }
                },
                () => View(model)
            );
        }

        #region Отказване на случай/заявка

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> CancelBatch(int batchId)
        {
            UserPermissionsModel currentUser = await User.GetPermissionsAsync();
            using (BatchService service = new BatchService())
            {
                await service.CancelBatchAsync(batchId, currentUser, GetAuditModel());

                // Параметърът позволява да се изведе съобщение за успех и link към отказания случай.
                return RedirectToAction(nameof(Index), new { cancelledBatchId = batchId });
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> CancelRequest(int batchId, int regiXReportId)
        {
            UserPermissionsModel currentUser = await User.GetPermissionsAsync();
            using (BatchService service = new BatchService())
            {
                bool batchIsIncomplete = await service.CancelRequestAsync(batchId, regiXReportId, currentUser, GetAuditModel());

                // Ако няма повече стъпки за отказване, случаят изчезва от екрана с незавършени случаи и се извежда съобщение за успех.
                // Ако има още стъпки за отказване, страницата се позиционира на реда с конкретния случай.
                if (batchIsIncomplete)
                {
                    string urlWithHash = Url.Action(nameof(Index)) + "#" + batchId;
                    return Redirect(urlWithHash);
                }
                return RedirectToAction(nameof(Index), new { cancelledBatchId = batchId });
            }
        }

        #endregion

        #region Чакащи регистрации на потребители

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> ApproveOrBlockUser(string userId, bool approveUser, bool ban)
        {
            using (ApplicationUserManager userManager = CreateUserManager())
            {
                ApplicationUser user = await userManager.FindByIdAsync(userId);
                string displayName = user != null ? user.PersonName ?? user.UserName : null;

                return await TryAsync(
                    async () =>
                    {
                        // Промените се правят през user manager-а. Service layer-ът се използва само за проверка на правата на текущия потребител.
                        // Ако избраният потребител вече е в желаното състояние (блокиран, одобрен и т.н.), операцията се счита за успешна, независимо от правата.
                        if (ban)
                        {
                            if (!user.IsBanned)
                            {
                                await DemandPermissionAsync(userId, "блокира");
                                _successMessage = $"Потребител {displayName} е блокиран и не може да подава повече заявления за регистриране.";
                                user.IsBanned = ban;
                                await userManager.UpdateAsync(user);
                            }
                        }
                        else if (user.IsApproved != approveUser)
                        {
                            await DemandPermissionAsync(userId, (approveUser ? "одобрява" : "отказва") + " регистрацията на");
                            _successMessage = $"Заявлението за регистриране на потребител {displayName} е {(approveUser ? "одобрено" : "отказано")}.";

                            if (!string.IsNullOrEmpty(user.Email))
                            {
                                // Подготвя се e-mail известие.
                                // Внимание: userManager.UpdateSecurityStampAsync() замазва данните на потребтителя ако те са били редактирани
                                // през service layer-а (например връща IsApproved на null). Или всички промени трябва да се правят през
                                // user manager-а, или промените от service layer-а трябва да се правят след тези на user manager-а.
                                string subject, body;
                                if (approveUser)
                                {
                                    subject = "Одобрено заявление за регистриране";
                                    body = "Вашето заявление за регистриране беше ОДОБРЕНО от администратор.";

                                    // Обикновено се одобряват нови потребители, които още нямат парола.
                                    // Това е моментът да им бъде изпратен e-mail с link за първоначално задаване на парола.
                                    if (user.PasswordHash == null)
                                    {
                                        // Ако са били изпращани предишни e-mail-и за задаване на нова парола, техните линкове се анулират.
                                        IdentityResult result = await userManager.UpdateSecurityStampAsync(userId);
                                        if (!result.Succeeded)
                                        {
                                            throw new Exception(string.Join(Environment.NewLine, result.Errors));
                                        }
                                        string code = await userManager.GeneratePasswordResetTokenAsync(userId);
                                        string callbackUrl = Url.Action(nameof(AccountController.ResetPassword), "Account", new { userId, code }, protocol: Request.Url.Scheme);
                                        body += $"<br /><br />Моля щракнете <a href=\"{callbackUrl}\">тук</a> и изберете парола за {user.UserName}.";
                                    }
                                }
                                else
                                {
                                    subject = "Отказано заявление за регистриране";
                                    body = "Вашето заявление за регистриране беше ОТКАЗАНО от администратор.<br />" +
                                        "След като уточнете причините може да подадете ново заявление.";
                                }

                                user.IsApproved = approveUser;
                                await userManager.UpdateAsync(user);
                                await userManager.SendEmailAsync(userId, subject, body);
                            }
                            else
                            {
                                user.IsApproved = approveUser;
                                await userManager.UpdateAsync(user);
                            }
                        }
                    },
                    () => RedirectToAction(nameof(Index)),
                    () =>
                    {
                        Warning($"Заявлението за регистриране на потребител {displayName} НЕ е обработено.");
                        return RedirectToAction(nameof(Index));
                    });
            }
        }

        private async Task DemandPermissionAsync(string userId, string operationName)
        {
            UserPermissionsModel currentUser = await User.GetPermissionsAsync();
            using (UserManagementService service = new UserManagementService())
            {
                await service.DemandPermission_UpdateUserAsync(userId, operationName, currentUser);
            }
        }

        #endregion
    }
}