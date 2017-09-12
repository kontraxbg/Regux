using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Kontrax.Regux.Model.Administration
{
    public class AdministrationViewModel : AdministrationModel
    {
        [Display(Name = "Родителска администрация")]
        public string ParentName { get; set; }
        [UIHint("X509Certificate2")]
        public X509Certificate2 CertificateView { get; set; }
        [UIHint("X509Certificate2")]
        public X509Certificate2 ProposedCertificateView { get; set; }
    }
}
