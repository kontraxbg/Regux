using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Kontrax.Regux.Data;
using Kontrax.Regux.Model;

namespace Kontrax.Regux.Service
{
    public class PermissionService : BaseService
    {
        private readonly string _userId;

        public PermissionService(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException("userId");
            }
            _userId = userId;
        }

        public async Task<UserPermissionsModel> GetPermissionsAsync()
        {
            AspNetUser user = await (
                from u in _db.AspNetUsers.Include(u => u.AspNetRoles)
                where u.Id == _userId
                select u
            ).FirstOrDefaultAsync();
            MustExist(user, "потребител", _userId);

            UserPermissionsModel model = new UserPermissionsModel(_userId, user.PersonName ?? user.UserName, user.IsGlobalAdmin);

            var workplaces = await (
                from w in _db.Workplaces
                where w.UserId == _userId
                // TODO: Проверка за валиден потребител.
                orderby w.Administration.Name
                select new
                {
                    w.AdministrationId,
                    AdministrationName = (w.Administration.IsClosed ? "закрита: " : string.Empty) + w.Administration.Name,

                    // RegiX справките, които се ползват за поне една дейност на администрацията, се водят "разрешени за администрацията".
                    AdministrationRegiXReportIds = (
                        from a in w.Administration.Activities
                        from d in a.Dependencies
                        select d.RegiXReportId
                    ).Distinct(),

                    w.AccessLevelCode,
                    AccessLevelName = w.AccessLevel.Name,

                    w.LocalRoleId,
                    LocalRoleName = w.LocalRole.Name ?? LocalRoleName.None,

                    LocalRoleRegiXReportIds =
                        from r in w.LocalRole.RegiXReports
                        select r.Id
                }
            ).ToArrayAsync();

            foreach (var w in workplaces)
            {
                model.AddWorkplace(new WorkplaceModel(
                    w.AdministrationId, w.AdministrationName,
                    w.AdministrationRegiXReportIds,
                    w.AccessLevelCode, w.AccessLevelName,
                    w.LocalRoleId, w.LocalRoleName,
                    w.LocalRoleRegiXReportIds));
            }

            return model;
        }
    }
}
