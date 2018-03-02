using System.ComponentModel.DataAnnotations;
using Kontrax.Regux.Model.Certificate;

namespace Kontrax.Regux.Model.UserManagement
{
    public class UserEditModel : UserBaseModel
    {
        #region View data

        public bool CurrentUserCanEditThisUser { get; set; }

        public string Id { get; set; }

        public CodeNameModel[] PidTypes { get; set; }

        /// <summary>
        /// Показва валидност, subject и сериен номер в Edit view-то чрез shared DisplayTemplate.
        /// </summary>
        [Display(Name = "Сертификат")]
        public CertificateViewModel Certificate { get; set; }

        #endregion

        [Required]
        [Display(Name = "Потребителско име")]
        public string UserName { get; set; }

        [EmailAddress]
        [Display(Name = "E-mail")]
        public string Email { get; set; }

        //[Phone]
        [Display(Name = "Телефонен номер")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Блокиран")]
        public bool IsBanned { get; set; }

        [Display(Name = "Вход чрез системата за е-Автентикация")]
        public bool IsEAuthEnabled { get; set; }

        public string PidTypeCode { get; set; }

        [Display(Name = "с идентификатор")]
        public string PersonIdentifier { get; set; }

        [Display(Name = "Потребителят е " + Role.GlobalAdminName)]
        public bool IsGlobalAdmin { get; set; }

        [Display(Name = "Изпращане на e-mail за избор на нова парола")]
        public string SendResetPasswordEmailAction { get; set; }

        [Display(Name = "Запис")]
        public string SaveAction { get; set; }

        public WorkplaceEditModel[] Workplaces { get; set; }
    }
}
