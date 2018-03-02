using System.Threading.Tasks;
using System.Web.Mvc;
using Kontrax.Regux.Model.Config;
using Kontrax.Regux.Model.GlobalAdmin;
using Kontrax.Regux.Service;

namespace Kontrax.Regux.Portal.Controllers
{
    [GlobalAdmin]
    public class GlobalAdminController : BaseController
    {
        // Полето за подател трябва да допуска триъгълни скоби, но атрибутът AllowHtml не се вижда от Model проекта,
        // затова се добавя чрез този изкуствен наследник на SmtpEditModel.
        public class SmtpEditModel : Model.GlobalAdmin.SmtpEditModel
        {
            [AllowHtml]
            public new string FromEmail { get; set; }
        }

        [HttpGet]
        public async Task<ActionResult> Smtp()
        {
            SmtpConfig smtpConfig;
            using (ConfigService service = new ConfigService())
            {
                smtpConfig = await service.GetSmtpConfigAsync();
            }

            SmtpEditModel model = new SmtpEditModel
            {
                Host = smtpConfig.Host,
                Port = smtpConfig.Port,
                EnableSsl = smtpConfig.EnableSsl,
                UserName = smtpConfig.UserName,
                Password = smtpConfig.Password,
                FromEmail = smtpConfig.FromEmail
            };
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public Task<ActionResult> Smtp(SmtpEditModel model)
        {
            return TryAsync(
                async () =>
                {
                    SmtpConfig smtpConfig = new SmtpConfig
                    {
                        Host = model.Host,
                        Port = model.Port,
                        EnableSsl = model.EnableSsl,
                        UserName = model.UserName,
                        Password = model.Password,
                        FromEmail = model.FromEmail
                    };
                    using (ConfigService service = new ConfigService())
                    {
                        await service.SetSmtpConfigAsync(smtpConfig, GetAuditModel());
                    }
                },
                () => View(model),
                () => View(model)
            );
        }
    }
}
