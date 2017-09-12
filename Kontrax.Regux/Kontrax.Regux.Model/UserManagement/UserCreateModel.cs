using System.ComponentModel.DataAnnotations;

namespace Kontrax.Regux.Model.UserManagement
{
    public class UserCreateModel : UserBaseModel
    {
        [Required]
        [Display(Name = "Нова парола")]
        public string NewPassword { get; set; }
    }
}
