using System;
using System.Data.Entity;
using System.Globalization;
using System.Threading.Tasks;
using Kontrax.Regux.Data;
using Kontrax.Regux.Model.Audit;
using Kontrax.Regux.Model.Config;
using Kontrax.Regux.Model.Iisda;

namespace Kontrax.Regux.Service
{
    public class ConfigService : BaseService
    {
        #region Някои настройки от app.config

        private static readonly bool _devModeEnabled;

        static ConfigService()
        {
            bool.TryParse(System.Configuration.ConfigurationManager.AppSettings[nameof(DevModeEnabled)], out _devModeEnabled);
        }

        public static bool DevModeEnabled
        {
            get { return _devModeEnabled; }
        }

        #endregion

        #region SmtpConfig

        /// <summary>
        /// Все още не се използва, защото не се изпраща поща.
        /// </summary>
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

        public async Task SetSmtpConfigAsync(SmtpConfig smtpConfig, AuditModel auditContext)
        {
            await LoadAllAsync();
            await SetValueAsync("SmtpHost", smtpConfig.Host);
            await SetValueAsync("SmtpPort", smtpConfig.Port.HasValue ? smtpConfig.Port.Value.ToString() : null);
            await SetValueAsync("SmtpEnableSsl", smtpConfig.EnableSsl.HasValue ? smtpConfig.EnableSsl.Value.ToString() : null);
            await SetValueAsync("SmtpUserName", smtpConfig.UserName);
            await SetValueAsync("SmtpPassword", smtpConfig.Password);
            await SetValueAsync("SmtpFromEmail", smtpConfig.FromEmail);
            await SaveAndLogAsync(auditContext, typeof(SmtpConfig), string.Empty);
        }

        private Task LoadAllAsync()
        {
            return _db.Configs.LoadAsync();
        }

        #endregion

        #region Обновяване от ИИСДА

        private const string _iisdaUpdateStartKey = "IisdaUpdateStart";

        public async Task<DateTime?> GetIisdaUpdateStartAsync()
        {
            string value = await GetValueAsync(_iisdaUpdateStartKey);
            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTime))
            {
                return dateTime;
            }
            return null;
        }

        public async Task SetIisdaUpdateStartAsync(DateTime? dateTime, AuditModel auditContext)
        {
            await SetValueAsync(_iisdaUpdateStartKey, dateTime.HasValue ? dateTime.Value.ToString(CultureInfo.InvariantCulture) : null);
            await SaveAndLogConfigAsync(auditContext, _iisdaUpdateStartKey);
        }

        private const string _iisdaUpdateErrorKey = "IisdaUpdate{0}Error";

        private static string GetUpdateErrorKey(UpdateType type)
        {
            return string.Format(_iisdaUpdateErrorKey, type);
        }

        public Task<string> GetIisdaUpdateErrorAsync(UpdateType type)
        {
            return GetValueAsync(GetUpdateErrorKey(type));
        }

        public async Task ClearIisdaUpdateErrorAsync(UpdateType type, AuditModel auditContext)
        {
            string key = GetUpdateErrorKey(type);
            await SetValueAsync(key, null);
            await SaveAndLogConfigAsync(auditContext, key);
        }

        public async Task AppendIisdaUpdateErrorAsync(UpdateType type, string newError, AuditModel auditContext)
        {
            string key = GetUpdateErrorKey(type);
            Config config = await _db.Configs.FindAsync(key);
            if (config != null)
            {
                config.Value = $"{config.Value}{Environment.NewLine}{newError}";
                await SaveAndLogConfigAsync(auditContext, key);
            }
        }

        #endregion

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

        private Task SaveAndLogConfigAsync(AuditModel auditContext, string key)
        {
            return SaveAndLogAsync(auditContext, typeof(Config), key);
        }
    }
}
