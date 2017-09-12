using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Kontrax.Regux.Model.UserManagement
{
    public class UserLocalRoleEditModel
    {
        #region View data

        public string AdministrationName { get; set; }

        public IEnumerable<CodeNameModel> LocalRoles { get; set; }

        public string LocalRoleName { get; set; }

        /// <summary>
        /// Списъкът за избор на ниво на достъп е филтриран според правата на потребителя за конкретната администрация.
        /// Ако текущото ниво на достъп присъства в списъка за избор, значи то може да се редактира.
        /// </summary>
        public bool CanEditLocalRole
        {
            get
            {
                return LocalRoles != null && LocalRoles.Any(r => r.Code == LocalRoleCode);
            }
        }

        #endregion

        [Display(Name = "Администрация")]
        public int AdministrationId { get; set; }

        [Display(Name = "Ниво на достъп")]
        public string LocalRoleCode { get; set; }
    }
}
