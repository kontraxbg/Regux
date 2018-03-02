using System.ComponentModel.DataAnnotations;

namespace Kontrax.Regux.Model.UserManagement
{
    public class UserCreateModel : UserBaseModel
    {
        // По време на създаването на потребител, потребителското име се рекламира като e-mail,
        // за да се стимулира тази конвенция за именуване на потребители. След това e-mail адресът може да се подмени или изтрие.
        [Required]
        [Display(Name = "E-mail")]
        public string UserName { get; set; }

        [Display(Name = "Да се изпрати e-mail за избор на парола")]
        public bool SendSetPasswordEmail { get; set; }
    }
}
