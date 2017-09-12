using System.Data.Entity;
using System.Threading.Tasks;
using Kontrax.Regux.Data;
using Kontrax.Regux.Model.Config;

namespace Kontrax.Regux.Service
{
    public class ConfigService : BaseService
    {
        public Task<string> GetRegiXServiceAddressAsync()
        {
            return GetValueAsync("RegiXServiceAddress");
        }

        public async Task SetRegiXServiceAddressAsync(string value)
        {
            await SetValueAsync("RegiXServiceAddress", value);
            await SaveAsync();
        }

        public async Task<SmtpConfig> GetSmtpConfigAsync()
        {
            await LoadAllAsync();
            string portText = await GetValueAsync("SmtpPort");
            string enableSslText = await GetValueAsync("SmtpEnableSsl");
            return new SmtpConfig
            {
                Host = await GetValueAsync("SmtpHost"),
                Port = portText != null && int.TryParse(portText, out int port) ? port : (int?)null,
                EnableSsl = enableSslText != null && bool.TryParse(enableSslText, out bool enableSsl) ? enableSsl : (bool?)null,
                UserName = await GetValueAsync("SmtpUserName"),
                Password = await GetValueAsync("SmtpPassword"),
                FromEmail = await GetValueAsync("SmtpFromEmail")
            };
        }

        public async Task SetSmtpConfigAsync(SmtpConfig smtpConfig)
        {
            await LoadAllAsync();
            await SetValueAsync("SmtpHost", smtpConfig.Host);
            await SetValueAsync("SmtpPort", smtpConfig.Port.HasValue ? smtpConfig.Port.Value.ToString() : null);
            await SetValueAsync("SmtpEnableSsl", smtpConfig.EnableSsl.HasValue ? smtpConfig.EnableSsl.Value.ToString() : null);
            await SetValueAsync("SmtpUserName", smtpConfig.UserName);
            await SetValueAsync("SmtpPassword", smtpConfig.Password);
            await SetValueAsync("SmtpFromEmail", smtpConfig.FromEmail);
            await SaveAsync();
        }

        private Task LoadAllAsync()
        {
            return _db.Configs.LoadAsync();
        }

        private async Task<string> GetValueAsync(string code)
        {
            Config config = await _db.Configs.FindAsync(code);
            return config?.Value;
        }

        private async Task SetValueAsync(string code, string value)
        {
            Config config = await _db.Configs.FindAsync(code);
            if (config != null)
            {
                config.Value = !string.IsNullOrEmpty(value) ? value : null;
            }
        }
    }
}
