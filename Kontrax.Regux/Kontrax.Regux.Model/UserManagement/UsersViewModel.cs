using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Kontrax.Regux.Model.UserManagement
{
    public class UsersViewModel
    {
        #region View data

        public UserPermissionsModel CurrentUser { get; set; }

        public IEnumerable<IdNameModel> Administrations { get; set; }

        public IEnumerable<CodeNameModel> LocalRoles { get; set; }

        #endregion

        [Display(Name = "Администрация")]
        public int? AdministrationId { get; set; }

        [Display(Name = "Ниво на достъп")]
        public string LocalRoleCode { get; set; }

        #region Резултат

        public UserViewModel[] Users { get; set; }

        #endregion
    }
}
