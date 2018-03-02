using System.ComponentModel.DataAnnotations;

namespace Kontrax.Regux.Model.Administration
{
    public class AdministrationIndexModel: AdministrationBaseModel
    {
        public UserPermissionsModel CurrentUser { get; set; }

        [Display(Name = "Вид")]
        public string Kind { get; set; }

        [Display(Name = "Необходими удостоверения")]
        public int RequiredRegiXReportCount { get; set; }

        [Display(Name = "Има RegiX сертификат")]
        public bool HasRegiXCertificate { get; set; }
    }
}
