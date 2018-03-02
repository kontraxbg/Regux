using System;
using System.Collections.Generic;
using System.Linq;

namespace Kontrax.Regux.Model
{
    public class UserPermissionsModel
    {
        private readonly string _userId;
        private readonly string _displayName;
        private readonly bool _isGlobalAdmin;
        private readonly Dictionary<int, WorkplaceModel> _workplaces = new Dictionary<int, WorkplaceModel>();

        public UserPermissionsModel(string userId, string displayName, bool isGlobalAdmin)
        {
            _userId = userId;
            _displayName = displayName;
            _isGlobalAdmin = isGlobalAdmin;
        }

        public string UserId { get { return _userId; } }

        public string DisplayName { get { return _displayName; } }

        public bool IsGlobalAdmin { get { return _isGlobalAdmin; } }

        public void AddWorkplace(WorkplaceModel workplace)
        {
            _workplaces.Add(workplace.AdministrationId, workplace);
        }

        public IEnumerable<WorkplaceModel> Workplaces
        {
            get { return _workplaces.Values; }
        }

        public WorkplaceModel GetWorkplace(int administrationId)
        {
            _workplaces.TryGetValue(administrationId, out WorkplaceModel workplace);
            return workplace;
        }

        /// <summary>
        /// Дали потребителят може да добавя ръководители към поне една администрация.
        /// </summary>
        public bool CanAddManagers
        {
            get { return _isGlobalAdmin || _workplaces.Values.Any(r => r.IsAdmin); }
        }

        /// <summary>
        /// Дали потребителят може да добавя служители към поне една администрация.
        /// </summary>
        public bool CanAddEmployees
        {
            get { return _isGlobalAdmin || _workplaces.Values.Any(r => r.IsManagerOrAdmin); }
        }

        /// <summary>
        /// Централните и локалните администратори могат да редактират списъка с длължности/роли на администрацията.
        /// </summary>
        public bool CanManageLocalRoles(int administrationId)
        {
            return _isGlobalAdmin || _workplaces.TryGetValue(administrationId, out WorkplaceModel workplace) && workplace.IsAdmin;
        }

        /// <summary>
        /// Централните и локалните администратори могат да предлагат root сертификат и да качват сертификат за достъп до RegiX.
        /// </summary>
        public bool CanManageCertificates(int administrationId)
        {
            return CanManageLocalRoles(administrationId);
        }

        /// <summary>
        /// Централните и локалните администратори, както и ръководителите без роля, могат да задават длължност/роля на потребител в същата администрация.
        /// Ръководителите с роля не могат да променят ролята на друг служител, но ако двете роли са еднакви проверката връща true.
        /// </summary>
        public bool CanSetColleagueLocalRole(int administrationId, int? newLocalRoleId)
        {
            return _isGlobalAdmin || _workplaces.TryGetValue(administrationId, out WorkplaceModel workplace) &&
            (
                workplace.IsAdmin ||
                workplace.IsManager && (!workplace.LocalRoleId.HasValue || workplace.LocalRoleId == newLocalRoleId)
            );
        }

        /// <summary>
        /// Централните и локалните администратори, както и ръководителите без роля, могат да задават:
        /// - специфичните печатни шаблони;
        /// - правните основания за извикване на необходимите удостоверения;
        /// - имената на вътрешните дейности.
        /// Ръководителите с роля нямат такива права, защото ролите не дават права за дейности, а само за различни удостоверения от RegiX.
        /// </summary>
        public bool CanManageActivities(int administrationId)
        {
            return _isGlobalAdmin || _workplaces.TryGetValue(administrationId, out WorkplaceModel workplace) && (workplace.IsAdmin || workplace.IsManager && !workplace.LocalRoleId.HasValue);
        }

        public RegiXReportPermissionModel RegiXReportIsAllowed(int regiXReportId, IdNameModel administration)
        {
            if (_isGlobalAdmin)
            {
                return new RegiXReportPermissionModel(true, $"Имате право да заявявате това удостоверение, тъй като сте {Role.GlobalAdminName}.");
            }

            if (administration != null)
            {
                if (_workplaces.TryGetValue(administration.Id, out WorkplaceModel workplace))
                {
                    return new RegiXReportPermissionModel(
                        workplace.RegiXReportIsAllowed(regiXReportId),
                        workplace.ExplainRegiXReportPermission(regiXReportId));
                }
                return new RegiXReportPermissionModel(false, $"Нямате право да заявявате това удостоверение, тъй като не сте служител на \"{administration.Name}\".");
            }
            else
            {
                return new RegiXReportPermissionModel(
                    _workplaces.Values.Any(w => w.RegiXReportIsAllowed(regiXReportId)),
                    string.Join(Environment.NewLine, _workplaces.Values.Select(w => w.ExplainRegiXReportPermission(regiXReportId))));
            }
        }
    }
}
