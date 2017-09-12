using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontrax.Regux.Model.Administration
{
    public class AdministrationModel
    {
        public int? Id { get; set; }
        [Display(Name = "Код")]
        public string Code { get; set; }
        [Display(Name = "Родител")]
        public int? ParentId { get; set; }
        [Display(Name = "Име")]
        public string Name { get; set; }
        [Display(Name = "Сертификат")]
        public byte[] Certificate { get; set; }
        [Display(Name = "Сертификат подлежащ на одобрение")]
        public byte[] ProposedCertificate { get; set; }
        [Display(Name = "Парола за Сертификат подлежащ на одобрение")]
        public string ProposedCertificatePassword { get; set; }
    }
}
