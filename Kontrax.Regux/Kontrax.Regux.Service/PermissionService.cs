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

        public async Task<UserPermissionsModel> GetPermissions()
        {
            AspNetUser user = await (
                from u in _db.AspNetUsers.Include(u => u.AspNetRoles)
                where u.Id == _userId
                select u
            ).FirstOrDefaultAsync();
            MustExist(user, "потребител", _userId);

            UserLocalRole[] localRoles = await (
                from r in _db.UserLocalRoles
                where r.UserId == _userId
                // TODO: Проверка за валиден потребител.
                orderby r.AdministrationId
                select r
            ).ToArrayAsync();

            return new UserPermissionsModel
            {
                UserId = _userId,
                DisplayName = user.PersonName ?? user.UserName,
                IsGlobalAdmin = user.IsGlobalAdmin,
                ManagerOfAdministrationIds = (
                    from r in localRoles
                    where r.LocalRoleCode == Model.LocalRole.Admin || r.LocalRoleCode == Model.LocalRole.Manager
                    select r.AdministrationId
                ).ToArray(),
                AdminOfAdministrationIds = (
                    from r in localRoles
                    where r.LocalRoleCode == Model.LocalRole.Admin
                    select r.AdministrationId
                ).ToArray()
            };
        }
    }
}
