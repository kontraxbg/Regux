using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;

namespace Kontrax.Regux.Model.UserManagement
{
    public class UserEditModel : UserBaseModel
    {
        #region View data

        public string Id { get; set; }

        #endregion

        [Display(Name = "Нова парола")]
        public string NewPassword { get; set; }
        [Display(Name = "Паролата трябва да бъде сменена при следващ вход")]
        public bool ChangePassword { get; set; }
        [Display(Name = "Сертификат")]
        public X509Certificate2 Certificate { get; set; }
        [Display(Name = "Сертификат")]
        public byte[] CertificateData { get; set; }

        public List<UserLocalRoleEditModel> UserLocalRoles { get; set; }
    }
}
