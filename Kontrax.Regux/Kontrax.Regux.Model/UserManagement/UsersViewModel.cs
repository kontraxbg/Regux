using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Kontrax.Regux.Model.UserManagement
{
    public class UsersViewModel
    {
        #region View data

        public UserPermissionsModel CurrentUser { get; set; }

        public IEnumerable<IdNameModel> Administrations { get; set; }

        public IEnumerable<CodeNameModel> UserTypes { get; set; }

        #endregion

        [Display(Name = "Администрация или доставчик на обществени услуги")]
        public int? AdministrationId { get; set; }

        [Display(Name = "Име, ЕГН или контакти")]
        public string NameIdOrContact { get; set; }

        [Display(Name = "Ниво на достъп")]
        public string UserTypeCode { get; set; }

        #region Резултат

        public int? UserCount { get; set; }

        public UserViewModel[] Users { get; set; }

        public string ResultTitle
        {
            get
            {
                if (!UserCount.HasValue || Users == null)
                {
                    // Търсенето още не е изпълнено.
                    return null;
                }
                int count = UserCount.Value;
                if (count == 0)
                {
                    return "Няма резултати";
                }
                int loadedCount = Users.Length;
                return $"{count} потребителя" + (count > loadedCount ? $" (показани са първите {loadedCount})" : null);
            }
        }

        #endregion
    }
}
