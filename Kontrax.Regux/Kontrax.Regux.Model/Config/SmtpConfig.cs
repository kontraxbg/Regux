namespace Kontrax.Regux.Model.Config
{
    public class SmtpConfig
    {
        public string Host { get; set; }
        public int? Port { get; set; }
        public bool? EnableSsl { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string FromEmail { get; set; }
    }
}
