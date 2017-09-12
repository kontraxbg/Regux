using System.ComponentModel.DataAnnotations;

namespace Kontrax.Regux.Model.UserManagement
{
    public class UserViewModel
    {
        public string Id { get; set; }

        [Display(Name = "Потребител")]
        public string UserName { get; set; }

        [Display(Name = "Трите имена")]
        public string PersonName { get; set; }

        [Display(Name = "E-mail адрес")]
        public string Email { get; set; }

        [Display(Name = "Телефон")]
        public string PhoneNumber { get; set; }

        public int? AdministrationId { get; set; }

        [Display(Name = "Администрация")]
        public string AdministrationName { get; set; }

        public string LocalRoleCode { get; set; }

        [Display(Name = "Ниво на достъп")]
        public string LocalRoleName { get; set; }

        [Display(Name = "Централен администратор")]
        public bool IsGlobalAdmin { get; set; }
    }
}
