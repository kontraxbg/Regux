using System.ComponentModel.DataAnnotations;

namespace Kontrax.Regux.Model.GlobalAdmin
{
    public class SmtpEditModel
    {
        [Display(Name = "Име или IP на SMTP сървър")]
        public string Host { get; set; }

        [Display(Name = "TCP порт")]
        public int? Port { get; set; }

        [Display(Name = "Използване на SSL")]
        public bool? EnableSsl { get; set; }

        [Display(Name = "Потребителско име")]
        public string UserName { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Парола")]
        public string Password { get; set; }

        //[EmailAddress]  // Не допуска триъгълни скоби, например "Full Name <email@domain.com>".
        [Display(Name = "Писмата се изпращат от името на")]
        public string FromEmail { get; set; }
    }
}
