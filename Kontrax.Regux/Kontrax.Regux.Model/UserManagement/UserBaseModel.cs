using System.ComponentModel.DataAnnotations;

namespace Kontrax.Regux.Model.UserManagement
{
    public abstract class UserBaseModel
    {
        #region View data

        public UserPermissionsModel CurrentUser { get; set; }

        #endregion

        [Required]
        [Display(Name = "Потребителско име")]
        public string UserName { get; set; }

        [Display(Name = "Трите имена")]
        public string PersonName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "E-mail")]
        public string Email { get; set; }

        [Phone]
        [Display(Name = "Телефонен номер")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Централен администратор")]
        public bool IsGlobalAdmin { get; set; }

        public UserLocalRoleCreateModel NewLocalRole { get; set; }
    }
}
