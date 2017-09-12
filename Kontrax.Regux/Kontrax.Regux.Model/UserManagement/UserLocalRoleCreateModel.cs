using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Kontrax.Regux.Model.UserManagement
{
    public class UserLocalRoleCreateModel
    {
        #region View data

        public IEnumerable<IdNameModel> Administrations { get; set; }

        public IEnumerable<CodeNameModel> LocalRoles { get; set; }

        #endregion

        [Display(Name = "Администрация")]
        public int? AdministrationId { get; set; }

        [Display(Name = "Ниво на достъп")]
        public string LocalRoleCode { get; set; }

        public UserLocalRoleEditModel[] ToEditModel()
        {
            return AdministrationId.HasValue
                ? new UserLocalRoleEditModel[]
                {
                    new UserLocalRoleEditModel
                    {
                        AdministrationId = AdministrationId.Value,
                        LocalRoleCode = LocalRoleCode
                    }
                }
                : new UserLocalRoleEditModel[0];
        }
    }
}
