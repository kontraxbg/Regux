using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Kontrax.Regux.Data;
using Kontrax.Regux.Model;
using Kontrax.Regux.Model.UserManagement;
using System.Security.Cryptography.X509Certificates;

namespace Kontrax.Regux.Service
{
    public class UserManagementService : BaseService
    {
        public async Task<CodeNameModel[]> GetLocalRolesAsync()
        {
            Data.LocalRole[] localRoles = await (
                from r in _db.LocalRoles
                orderby r.Name
                select r
            ).ToArrayAsync();
            return localRoles.Select(r => new CodeNameModel(r.Code, r.Name)).ToArray();
        }

        public async Task<CodeNameModel[]> GetAssignableLocalRolesAsync(UserPermissionsModel currentUser)
        {
            IQueryable<Data.LocalRole> query;
            // Само централен администратор може да създава локални администратори.
            if (currentUser.IsGlobalAdmin)
            {
                query = _db.LocalRoles;
            }
            // Само потребител, който е локален администратор за поне една администрация може да създава ръководители.
            else if (currentUser.AdminOfAdministrationIds.Length > 0)
            {
                query = _db.LocalRoles.Where(r => r.Code == Model.LocalRole.Manager || r.Code == Model.LocalRole.Employee);
            }
            // Само потребител, който е ръководител за поне една администрация може да създава служители.
            else if (currentUser.ManagerOfAdministrationIds.Length > 0)
            {
                query = _db.LocalRoles.Where(r => r.Code == Model.LocalRole.Employee);
            }
            else
            {
                return new CodeNameModel[0];
            }

            Data.LocalRole[] localRoles = await query.OrderBy(r => r.Name).ToArrayAsync();
            return localRoles.Select(r => new CodeNameModel(r.Code, r.Name)).ToArray();
        }

        public IEnumerable<CodeNameModel> FilterAssignableLocalRoles(CodeNameModel[] localRoles, int administrationId, UserPermissionsModel currentUser)
        {
            // Само локален администратор може да създава ръководители за съответната администрация.
            if (currentUser.IsGlobalAdmin || currentUser.IsAdminOf(administrationId))
            {
                return localRoles;  // Този списък би трябвало да съдържа само Manager и Employee.
            }
            // Само ръководител може да създава потребители за съответната администрация.
            if (currentUser.IsManagerOf(administrationId))
            {
                return localRoles.Where(r => r.Code == Model.LocalRole.Employee);
            }
            return new CodeNameModel[0];
        }

        public Task<UserViewModel[]> SearchUsersAsync(int? administrationId, string roleCode)
        {
            IQueryable<UserViewModel> query =
                from u in _db.AspNetUsers
                from r in u.UserLocalRoles.DefaultIfEmpty()
                orderby u.UserName
                select new UserViewModel
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    PersonName = u.PersonName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    AdministrationId = r.AdministrationId,
                    AdministrationName = r.Administration.Name,
                    LocalRoleCode = r.LocalRoleCode,
                    LocalRoleName = r.LocalRole.Name,
                    IsGlobalAdmin = u.AspNetRoles.Any(r => r.Name == Role.GlobalAdmin)
                };
            if (administrationId.HasValue)
            {
                query = query.Where(u => u.AdministrationId == administrationId.Value);
            }
            if (!string.IsNullOrEmpty(roleCode))
            {
                query = query.Where(u => u.LocalRoleCode == roleCode);
            }
            return query.ToArrayAsync();
        }

        public async Task<UserEditModel> GetUserForEditAsync(string id)
        {
            UserEditModel user = await (
                from u in _db.AspNetUsers
                where u.Id == id
                select new UserEditModel
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    PersonName = u.PersonName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    UserLocalRoles = (
                        from r in u.UserLocalRoles
                        orderby r.Administration.Name
                        select new UserLocalRoleEditModel
                        {
                            AdministrationId = r.AdministrationId,
                            LocalRoleCode = r.LocalRoleCode
                        }
                    ).ToList(),
                    ChangePassword = u.ChangePassword,
                    CertificateData = u.Certificate,
                    IsGlobalAdmin = u.AspNetRoles.Any(r => r.Name == Role.GlobalAdmin)
                }
            ).FirstOrDefaultAsync();
            MustExist(user, id);
            if (user.CertificateData!= null)
            {
                user.Certificate = new X509Certificate2(user.CertificateData, "password");
            }
            return user;
        }

        public async Task UpdateUserAsync(string id,
            string userName, string personName, string email, string phoneNumber, bool isGlobalAdmin,
            UserPermissionsModel currentUser)
        {
            AspNetUser user = await LoadUserWithRolesAsync(id);

            if (user.UserName != userName ||
                user.PersonName != personName ||
                user.Email != email ||
                user.PhoneNumber != phoneNumber)
            {
                DemandPermission_UpdateUser(user, "редактира данните за", currentUser);
                user.UserName = userName;
                user.PersonName = personName;
                user.Email = email;
                user.PhoneNumber = phoneNumber;
            }
            else
            {
                // Ако няма промяна в данните за потребителя, операцията се счита за успешна, независимо от правата.
            }

            TrySetGlobalAdmin(user, isGlobalAdmin, currentUser);

            await SaveAsync();
        }

        public async Task DeleteUserAsync(string id, UserPermissionsModel currentUser)
        {
            AspNetUser user = await LoadUserWithRolesAsync(id);
            DemandPermission_UpdateUser(user, "изтрива", currentUser);
            _db.AspNetUsers.Remove(user);
            await SaveAsync();
        }

        public async Task SetGlobalAdminAsync(string userId, bool isGlobalAdmin, UserPermissionsModel currentUser)
        {
            AspNetUser user = await LoadUserWithRolesAsync(userId);
            TrySetGlobalAdmin(user, isGlobalAdmin, currentUser);
            await SaveAsync();
        }

        private void TrySetGlobalAdmin(AspNetUser user, bool toBeGlobalAdmin, UserPermissionsModel currentUser)
        {
            AspNetRole existingRole = user.AspNetRoles.FirstOrDefault(r => r.Name == Role.GlobalAdmin);

            if (toBeGlobalAdmin && existingRole == null)
            {
                if (!currentUser.IsGlobalAdmin)
                {
                    throw new Exception($"Потребител {currentUser.DisplayName} няма право да задава ролята \"централен администратор\".");
                }
                user.AspNetRoles.Add(_db.AspNetRoles.First(r => r.Name == Role.GlobalAdmin));
            }
            else if (!toBeGlobalAdmin && existingRole != null)
            {
                if (!currentUser.IsGlobalAdmin)
                {
                    throw new Exception($"Потребител {currentUser.DisplayName} няма право да отнема ролята \"централен администратор\".");
                }
                user.AspNetRoles.Remove(existingRole);
            }
        }

        public async Task SetUserLocalRolesAsync(string userId, IEnumerable<UserLocalRoleEditModel> newRoles, UserPermissionsModel currentUser)
        {
            AspNetUser user = await LoadUserWithRolesAsync(userId);
            Dictionary<int, UserLocalRole> oldRoles = user.UserLocalRoles.ToDictionary(r => r.AdministrationId);

            foreach (UserLocalRoleEditModel newRole in newRoles)
            {
                oldRoles.TryGetValue(newRole.AdministrationId, out UserLocalRole oldRole);
                if (oldRole != null)
                {
                    // Ако няма промяна в ролята, операцията се счита за успешна, независимо от правата.
                    if (newRole.LocalRoleCode != oldRole.LocalRoleCode)
                    {
                        await DemandPermission_SetUserLocalRole(newRole.AdministrationId, oldRole.LocalRoleCode, newRole.LocalRoleCode, currentUser, user);
                        if (!string.IsNullOrEmpty(newRole.LocalRoleCode))
                        {
                            oldRole.LocalRoleCode = newRole.LocalRoleCode;
                        }
                        else  // Изтриване на работно място.
                        {
                            _db.UserLocalRoles.Remove(oldRole);
                        }
                    }
                    oldRoles.Remove(oldRole.AdministrationId);
                }
                else  // Избрано е ново работно място.
                {
                    if (!string.IsNullOrEmpty(newRole.LocalRoleCode))
                    {
                        await DemandPermission_SetUserLocalRole(newRole.AdministrationId, null, newRole.LocalRoleCode, currentUser, user);
                        _db.UserLocalRoles.Add(new UserLocalRole
                        {
                            UserId = userId,
                            AdministrationId = newRole.AdministrationId,
                            LocalRoleCode = newRole.LocalRoleCode
                        });
                    }
                }
            }

            // Всички "ненамерени" работни места се изтриват.
            foreach (UserLocalRole oldRole in oldRoles.Values)
            {
                await DemandPermission_SetUserLocalRole(oldRole.AdministrationId, oldRole.LocalRoleCode, null, currentUser, user);
            }
            _db.UserLocalRoles.RemoveRange(oldRoles.Values);

            await SaveAsync();
        }

        public void DemandPermission_AddEmployee(UserPermissionsModel currentUser)
        {
            if (!currentUser.CanAddEmployees)
            {
                throw new Exception($"Потребител {currentUser.DisplayName} няма право да добавя потребители.");
            }
        }

        public async Task DemandPermission_UpdateUserAsync(string id, string operationName, UserPermissionsModel currentUser)
        {
            AspNetUser user = await LoadUserWithRolesAsync(id);
            DemandPermission_UpdateUser(user, operationName, currentUser);
        }

        private async Task<AspNetUser> LoadUserWithRolesAsync(string id)
        {
            AspNetUser user = await _db.AspNetUsers.
                Include(u => u.AspNetRoles).
                Include(u => u.UserLocalRoles).
                FirstOrDefaultAsync(u => u.Id == id);
            MustExist(user, id);
            return user;
        }

        private static void MustExist(object user, string id)
        {
            MustExist(user, "потребител", id);
        }

        private static void DemandPermission_UpdateUser(AspNetUser user, string operationName, UserPermissionsModel currentUser)
        {
            if (!CanUpdateUser(user, currentUser))
            {
                throw new Exception($"Потребител {currentUser.DisplayName} няма право да {operationName} потребител {user.PersonName ?? user.UserName}.");
            }
        }

        private static async Task DemandPermission_SetUserLocalRole(int administrationId, string oldRoleCode, string newRoleCode, UserPermissionsModel currentUser, AspNetUser user)
        {
            if (!CanUpdateLocalRole(administrationId, oldRoleCode, newRoleCode, user.IsGlobalAdmin, currentUser))
            {
                string admName;
                using (AdministrationService service = new AdministrationService())
                {
                    admName = await service.GetAdministrationNameAsync(administrationId);
                }
                throw new Exception($"Потребител {currentUser.DisplayName} няма право да променя нивото на достъп в {admName} от \"{oldRoleCode}\" на \"{newRoleCode}\" за потребител {user.PersonName ?? user.UserName}.");
            }
        }

        private static bool CanUpdateUser(AspNetUser otherUser, UserPermissionsModel currentUser)
        {
            // Само централните администратори могат да редактират и изтриват други централни администратори.
            if (currentUser.IsGlobalAdmin)
            {
                return true;
            }
            if (otherUser.IsGlobalAdmin)
            {
                return false;
            }

            // Текущият потребител трябва да бъде по-високо ниво от редактирания потребител във всяка от администрациите, в които работи редактираният.
            return otherUser.UserLocalRoles.All(r =>
                currentUser.IsAdminOf(r.AdministrationId) &&
                IsLowerLevel(r.LocalRoleCode, Model.LocalRole.Admin) ||
                currentUser.IsManagerOf(r.AdministrationId) &&
                IsLowerLevel(r.LocalRoleCode, Model.LocalRole.Manager));
        }

        private static bool CanUpdateLocalRole(int administrationId, string oldRoleCode, string newRoleCode, bool userIsGlobalAdmin, UserPermissionsModel currentUser)
        {
            // Само централните администратори могат да редактират работните места на други централни администратори.
            if (currentUser.IsGlobalAdmin)
            {
                return true;
            }
            if (userIsGlobalAdmin)
            {
                return false;
            }

            // Текущият потребител трябва да бъде по-високо ниво и да остава по-високо ниво след редакцията.
            return currentUser.IsAdminOf(administrationId) &&
                IsLowerLevel(oldRoleCode, Model.LocalRole.Admin) &&
                IsLowerLevel(newRoleCode, Model.LocalRole.Admin) ||
                currentUser.IsManagerOf(administrationId) &&
                IsLowerLevel(oldRoleCode, Model.LocalRole.Manager) &&
                IsLowerLevel(newRoleCode, Model.LocalRole.Manager);
        }

        private static bool IsLowerLevel(string roleCode, string otherRoleCode)
        {
            if (roleCode == null)
            {
                return otherRoleCode != null;
            }
            if (roleCode == Model.LocalRole.Employee)
            {
                return otherRoleCode == Model.LocalRole.Admin || otherRoleCode == Model.LocalRole.Manager;
            }
            if (roleCode == Model.LocalRole.Manager)
            {
                return otherRoleCode == Model.LocalRole.Admin;
            }
            if (roleCode == Model.LocalRole.Admin)
            {
                return false;
            }
            throw new NotSupportedException($"Не се поддържа локална роля {roleCode}.");
        }
    }
}
