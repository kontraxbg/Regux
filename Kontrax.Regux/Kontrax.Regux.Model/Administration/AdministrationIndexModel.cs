using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontrax.Regux.Model.Administration
{
    public class AdministrationIndexModel: AdministrationModel
    {
        [Display(Name="Има PKI Сертификат")]
        public bool HasCertificate { get; set; }
        [Display(Name = "Предложен PKI Сертификат")]
        public bool CertificateIsPending { get; set; }
    }
}
