using System.Linq;

namespace Kontrax.Regux.Model
{
    public class UserPermissionsModel
    {
        public string UserId { get; set; }

        public string DisplayName { get; set; }

        public bool IsGlobalAdmin { get; set; }

        public int[] ManagerOfAdministrationIds { get; set; }

        public int[] AdminOfAdministrationIds { get; set; }

        public bool IsAdminOf(int administrationId)
        {
            return AdminOfAdministrationIds.Contains(administrationId);
        }

        public bool IsManagerOf(int administrationId)
        {
            return ManagerOfAdministrationIds.Contains(administrationId);
        }

        /// <summary>
        /// Дали потребителят може да добавя служители към поне една администрация.
        /// </summary>
        public bool CanAddEmployees
        {
            get
            {
                return IsGlobalAdmin || ManagerOfAdministrationIds != null && ManagerOfAdministrationIds.Length > 0;
            }
        }
    }
}
