using System.ComponentModel.DataAnnotations;

namespace Kontrax.Regux.Model.Administration
{
    public class AdministrationEditModel: AdministrationBaseModel
    {
        #region View data

        public CodeNameModel[] Kinds { get; set; }

        [Display(Name = "Доставчик на обществени услуги")]
        public bool IsPublicServiceProvider { get; set; }

        #endregion

        [Display(Name = "Вид")]
        public string KindCode { get; set; }

        [Display(Name = "ЕИК")]
        public string Uic { get; set; }

        [Display(Name = "OID")]
        public string Oid { get; set; }
    }
}
