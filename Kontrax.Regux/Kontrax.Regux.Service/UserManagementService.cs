using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Kontrax.Regux.Data;
using Kontrax.Regux.Model;
using Kontrax.Regux.Model.UserManagement;
using System.Security.Cryptography.X509Certificates;
using Kontrax.Regux.Model.Report;
using Kontrax.Regux.Model.Certificate;
using Kontrax.Regux.Model.Account;
using Kontrax.Regux.Model.Audit;

namespace Kontrax.Regux.Service
{
    public class UserManagementService : BaseService
    {
        #region Administrations, access levels, local roles

        private async Task<IdNameModel[]> GetAdministrationsAsync()
        {
            Administration[] administrations = await (
                from a in _db.Administrations
                orderby a.IsClosed, a.Name
                select a
            ).ToArrayAsync();
            return administrations.Select(a => new IdNameModel(a.Id, (a.IsClosed ? "закрита: " : null) + a.Name)).ToArray();
        }

        private async Task<CodeNameModel[]> GetUserTypesAsync()
        {
            List<CodeNameModel> result = new List<CodeNameModel>
            {
                new CodeNameModel(Role.GlobalAdmin, Role.GlobalAdminName)
            };

            Data.AccessLevel[] accessLevels = await (
                from r in _db.AccessLevels
                orderby r.Name
                select r
            ).ToArrayAsync();
            result.AddRange(accessLevels.Select(r => new CodeNameModel(r.Code, r.Name)));
            return result.ToArray();
        }

        public async Task<CodeNameModel[]> GetAccessLevelsAsync()
        {
            Data.AccessLevel[] accessLevels = await (
                from r in _db.AccessLevels
                orderby r.Name
                select r
            ).ToArrayAsync();
            return accessLevels.Select(r => new CodeNameModel(r.Code, r.Name)).ToArray();
        }

        private async Task<CodeNameModel[]> GetAssignableAccessLevelsAsync(UserPermissionsModel currentUser)
        {
            IQueryable<Data.AccessLevel> query = _db.AccessLevels;
            // Само централен администратор може да създава локални администратори.
            if (!currentUser.IsGlobalAdmin)
            {
                // Само потребител, който е локален администратор за поне една администрация може да създава ръководители.
                if (currentUser.CanAddManagers)
                {
                    query = query.Where(r => r.Code == Model.AccessLevel.Manager || r.Code == Model.AccessLevel.Employee);
                }
                // Само потребител, който е ръководител за поне една администрация може да създава служители.
                else if (currentUser.CanAddEmployees)
                {
                    query = query.Where(r => r.Code == Model.AccessLevel.Employee);
                }
                else
                {
                    return new CodeNameModel[0];
                }
            }

            Data.AccessLevel[] accessLevels = await query.OrderBy(r => r.Name).ToArrayAsync();
            return accessLevels.Select(r => new CodeNameModel(r.Code, r.Name)).ToArray();
        }

        private async Task<IdNameModel[]> GetLocalRolesAsync(int administrationId)
        {
            LocalRole[] localRoles = await (
                from r in _db.LocalRoles
                where r.AdministrationId == administrationId
                orderby r.Name
                select r
            ).ToArrayAsync();
            return localRoles.Select(r => new IdNameModel(r.Id, r.Name)).ToArray();
        }

        private async Task<IdNameModel[]> GetAssignableLocalRolesAsync(int administrationId, UserPermissionsModel currentUser)
        {
            if (currentUser.CanSetColleagueLocalRole(administrationId, null))
            {
                return await GetLocalRolesAsync(administrationId);
            }
            // Ръководител, който има длъжност/роля, може да задава своята роля на друг служител само при създаването на нов потребител.
            // Ръководителят не може да премества служител от своята длъжност/роля в друга, нито да приобщава служител с друга длъжност/роля към своята.
            // В частност, ръководителят не може да премахва ролята на служител (защото така ще му даде повече от своите права),
            // нито да зададе своята роля на служител без роля (защото така ще отнеме права от служителя, за които самият той няма право).
            //
            // Първоначалната идея, че ръководител винаги може да задава своята роля, отпада:
            //WorkplaceModel workplace = currentUser.GetWorkplace(administrationId);
            //if (workplace != null && workplace.IsManager)
            //{
            //    return new IdNameModel[] { new IdNameModel(workplace.LocalRoleId.Value, workplace.LocalRoleName) };
            //}
            return new IdNameModel[0];
        }

        #endregion

        #region Търсене и преглед на потребители и граждани

        public async Task<UserViewModel> GetUserAsync(string id)
        {
            IQueryable<AspNetUser> query = _db.AspNetUsers.Where(u => u.Id == id);
            return await GetUserViewModelQuery(query).FirstOrDefaultAsync();
        }

        public async Task SearchUsersAsync(UsersViewModel model)
        {
            IQueryable<AspNetUser> query = _db.AspNetUsers.Where(u => u.Workplaces.Any() || u.AspNetRoles.Any());
            query = HavingNameIdOrContact(query, model.NameIdOrContact);

            int? administrationId = model.AdministrationId;
            if (administrationId.HasValue)
            {
                query = query.Where(u => u.Workplaces.Any(r => r.AdministrationId == administrationId.Value));
            }

            string userTypeCode = model.UserTypeCode;
            if (!string.IsNullOrEmpty(userTypeCode))
            {
                if (userTypeCode == Role.GlobalAdmin)
                {
                    query = query.Where(u => u.AspNetRoles.Any(r => r.Name == Role.GlobalAdmin));
                }
                else
                {
                    query = query.Where(u => u.Workplaces.Any(r => r.AccessLevelCode == userTypeCode));
                }
            }

            // Бройката потребители се извлича с предварителна заявка. Резултатът е ограничен до 1000 потребителя.
            model.UserCount = await query.CountAsync();
            model.Users = await GetUserViewModelQuery(query).Take(1000).ToArrayAsync();

            // Зареждане на списъците за избор.
            model.Administrations = await GetAdministrationsAsync();
            model.UserTypes = await GetUserTypesAsync();
        }

        private static IQueryable<UserViewModel> GetUserViewModelQuery(IQueryable<AspNetUser> query)
        {
            return
                from u in query
                orderby u.UserName
                select new UserViewModel
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    PersonName = u.PersonName,
                    PidType = u.KeyType.Name,
                    PersonIdentifier = u.PersonIdentifier,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    IsBanned = u.IsBanned,
                    IsEAuthEnabled = u.TwoFactorEnabled,
                    IsGlobalAdmin = u.AspNetRoles.Any(r => r.Name == Role.GlobalAdmin),
                    Workplaces = (
                        from r in u.Workplaces
                        orderby r.Administration.Name
                        select new WorkplaceViewModel
                        {
                            AdministrationName = r.Administration.Name,
                            AccessLevelName = r.AccessLevel.Name,
                            // Ако няма локална роля, потребителят е без допълнителни ограничения.
                            LocalRoleName = r.LocalRole.Name ?? "*"
                        }
                    )
                };
        }

        public Task<CitizenViewModel[]> SearchCitizensAsync(string nameIdOrContact)
        {
            IQueryable<AspNetUser> query = _db.AspNetUsers.Where(u => !u.Workplaces.Any() && !u.AspNetRoles.Any());
            query = HavingNameIdOrContact(query, nameIdOrContact);

            return (
                from u in query
                orderby u.UserName
                select new CitizenViewModel
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    PersonName = u.PersonName,
                    PidType = u.KeyType.Name,
                    PersonIdentifier = u.PersonIdentifier,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    IsBanned = u.IsBanned
                }
            ).ToArrayAsync();
        }

        private static IQueryable<AspNetUser> HavingNameIdOrContact(IQueryable<AspNetUser> query, string nameIdOrContact)
        {
            if (!string.IsNullOrEmpty(nameIdOrContact))
            {
                query = query.Where(u =>
                    u.UserName.Contains(nameIdOrContact) ||
                    u.PersonName.Contains(nameIdOrContact) ||
                    u.PersonIdentifier.Contains(nameIdOrContact) ||
                    u.PhoneNumber.Contains(nameIdOrContact) ||
                    u.Email.Contains(nameIdOrContact));
            }
            return query;
        }

        public async Task<byte[]> GetUserCertificateAsync(string id)
        {
            return await (
                from u in _db.AspNetUsers
                where u.Id == id
                select u.Certificate
            ).FirstOrDefaultAsync();
        }

        #endregion

        #region Редактиране на потребител

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
                    IsBanned = u.IsBanned,
                    IsEAuthEnabled = u.TwoFactorEnabled,
                    PidTypeCode = u.PidTypeCode,
                    PersonIdentifier = u.PersonIdentifier,
                    IsGlobalAdmin = u.AspNetRoles.Any(r => r.Name == Role.GlobalAdmin),
                }
            ).FirstOrDefaultAsync();
            MustExist(user, id);

            user.Workplaces = await (
                from w in _db.Workplaces
                where w.UserId == id
                orderby w.Administration.Name
                select new WorkplaceEditModel
                {
                    AdministrationId = w.AdministrationId,
                    AccessLevelCode = w.AccessLevelCode,
                    LocalRoleId = w.LocalRoleId
                }
            ).ToArrayAsync();

            return user;
        }

        public async Task SetCreateModelViewDataAsync(UserCreateModel model)
        {
            CodeNameModel[] accessLevels = await GetAssignableAccessLevelsAsync(model.CurrentUser);
            await SetNewWorkplaceViewData(model, accessLevels);
        }

        public async Task SetEditModelViewDataAsync(UserEditModel model)
        {
            KeyType[] keyTypes = await (
                from t in _db.KeyTypes
                orderby t.Name
                select t
            ).ToArrayAsync();
            model.PidTypes = keyTypes.Select(t => new CodeNameModel(t.Code, t.Name)).ToArray();

            AspNetUser user = await LoadUserWithPermissionsAsync(model.Id);
            UserPermissionsModel currentUser = model.CurrentUser;
            model.CurrentUserCanEditThisUser = CanUpdateUser(user, currentUser);

            Dictionary<string, string> accessLevelNames = await _db.AccessLevels.ToDictionaryAsync(r => r.Code, r => r.Name);
            CodeNameModel[] assignableAccessLevels = await GetAssignableAccessLevelsAsync(currentUser);

            foreach (WorkplaceEditModel workplace in model.Workplaces)
            {
                int admId = workplace.AdministrationId;
                workplace.AdministrationName = await (
                    from a in _db.Administrations
                    where a.Id == admId
                    select a.Name
                ).FirstOrDefaultAsync();

                // Само локален/централен администратор може да създава ръководители за съответната администрация; само централен - локални администратори.
                WorkplaceModel cuWorkplace = currentUser.GetWorkplace(admId);
                if (currentUser.IsGlobalAdmin || cuWorkplace != null && cuWorkplace.IsAdmin)
                {
                    workplace.AccessLevels = assignableAccessLevels;
                }
                // Само ръководител може да създава потребители за съответната администрация.
                else if (cuWorkplace != null && cuWorkplace.IsManager)
                {
                    workplace.AccessLevels = new CodeNameModel[] { new CodeNameModel(Model.AccessLevel.Employee, accessLevelNames[Model.AccessLevel.Employee]) };
                }
                else
                {
                    workplace.AccessLevels = new CodeNameModel[0];
                }

                if (!string.IsNullOrEmpty(workplace.AccessLevelCode))
                {
                    workplace.AccessLevelName = accessLevelNames[workplace.AccessLevelCode];
                }

                workplace.LocalRoles = await GetAssignableLocalRolesAsync(admId, currentUser);
                workplace.LocalRoleName = workplace.LocalRoleId.HasValue
                    ? await (
                        from lr in _db.LocalRoles
                        where lr.AdministrationId == admId && lr.Id == workplace.LocalRoleId
                        select lr.Name
                    ).FirstOrDefaultAsync()
                    : LocalRoleName.None;
            }

            await SetNewWorkplaceViewData(model, assignableAccessLevels);
            // От списъка за избор се премахват всички администрации, в които избраният портебител вече работи.
            model.NewWorkplace.Administrations = model.NewWorkplace.Administrations.Where(a => !model.Workplaces.Any(r => r.AdministrationId == a.Id));

            await LoadCertificateAsync(model);
        }

        private async Task LoadCertificateAsync(UserEditModel model)
        {
            byte[] certData = await (
                from u in _db.AspNetUsers
                where u.Id == model.Id
                select u.Certificate
            ).FirstOrDefaultAsync();
            CertificateViewModel certModel = new CertificateViewModel();
            if (certData != null)
            {
                try
                {
                    certModel.X509 = new X509Certificate2(certData, "password");
                }
                catch (Exception ex)
                {
                    certModel.Error = ex.Messages();
                }
            }
            model.Certificate = certModel;
        }

        public async Task SetEditCitizenModelViewDataAsync(UserEditModel model)
        {
            KeyType[] keyTypes = await (
                from t in _db.KeyTypes
                orderby t.Name
                select t
            ).ToArrayAsync();
            model.PidTypes = keyTypes.Select(t => new CodeNameModel(t.Code, t.Name)).ToArray();
        }

        public async Task SetNewWorkplaceViewData(UserBaseModel model, IEnumerable<CodeNameModel> accessLevels)
        {
            // Само централен администратор може да избира измежду всички администрации.
            // Останалите потребители има смисъл да избират само администрации, за които имат поне ниво Ръководител.
            IEnumerable<IdNameModel> administrations;
            if (model.CurrentUser.IsGlobalAdmin)
            {
                administrations = await GetAdministrationsAsync();
            }
            else
            {
                administrations =
                    from w in model.CurrentUser.Workplaces
                    where w.IsManagerOrAdmin
                    orderby w.AdministrationName
                    select new IdNameModel(w.AdministrationId, w.AdministrationName);

                // Само централен администратор може да добавя локални администратори.
                accessLevels = accessLevels.Where(r => r.Code != Model.AccessLevel.Admin);
            }

            model.NewWorkplace.Administrations = administrations;
            // Въпреки че се показва и роля Ръководител, дали това е разрешено зависи от избраната администрация и се проверява при запис.
            model.NewWorkplace.AccessLevels = accessLevels;
        }

        public async Task UpdateUserAsync(UserEditModel model, UserPermissionsModel currentUser, AuditModel auditContext)
        {
            AspNetUser user = await LoadUserWithPermissionsAsync(model.Id);

            if (user.UserName != model.UserName ||
                user.PersonName != model.PersonName ||
                user.Email != model.Email ||
                user.PhoneNumber != model.PhoneNumber ||
                user.IsBanned != model.IsBanned ||
                user.TwoFactorEnabled != model.IsEAuthEnabled ||
                user.PidTypeCode != model.PidTypeCode ||
                user.PersonIdentifier != model.PersonIdentifier)
            {
                DemandPermission_UpdateUser(user, "редактира данните за", currentUser);
                user.UserName = model.UserName;
                user.PersonName = model.PersonName;
                user.Email = model.Email;
                user.PhoneNumber = model.PhoneNumber;
                user.IsBanned = model.IsBanned;
                user.TwoFactorEnabled = model.IsEAuthEnabled;
                user.PidTypeCode = model.PidTypeCode;
                user.PersonIdentifier = model.PersonIdentifier;
            }
            else
            {
                // Ако няма промяна в данните за потребителя, операцията се счита за успешна, независимо от правата.
            }

            TrySetGlobalAdmin(user, model.IsGlobalAdmin, currentUser);

            await SaveAndLogUserAsync(auditContext, user.Id);
        }

        private void TrySetGlobalAdmin(AspNetUser user, bool toBeGlobalAdmin, UserPermissionsModel currentUser)
        {
            AspNetRole existingRole = user.AspNetRoles.FirstOrDefault(r => r.Name == Role.GlobalAdmin);

            if (toBeGlobalAdmin && existingRole == null)
            {
                if (!currentUser.IsGlobalAdmin)
                {
                    throw new Exception($"Потребител {currentUser.DisplayName} няма право да задава ролята \"{Role.GlobalAdminName}\".");
                }
                user.AspNetRoles.Add(_db.AspNetRoles.First(r => r.Name == Role.GlobalAdmin));
            }
            else if (!toBeGlobalAdmin && existingRole != null)
            {
                if (!currentUser.IsGlobalAdmin)
                {
                    throw new Exception($"Потребител {currentUser.DisplayName} няма право да отнема ролята \"{Role.GlobalAdminName}\".");
                }
                user.AspNetRoles.Remove(existingRole);
            }
        }

        public async Task SetUserWorkplacesAsync(string userId, IEnumerable<WorkplaceEditModel> newWorkplaces, UserPermissionsModel currentUser, AuditModel auditContext)
        {
            Dictionary<int, Workplace> oldWorkplaces = await _db.Workplaces.
                Include(w => w.AspNetUser).
                Include(w => w.Administration).
                Include(w => w.AccessLevel).
                Include(w => w.LocalRole).
                Where(w => w.UserId == userId).
                ToDictionaryAsync(w => w.AdministrationId);

            foreach (WorkplaceEditModel newWorkplace in newWorkplaces)
            {
                int admId = newWorkplace.AdministrationId;
                string newLevelCode = newWorkplace.AccessLevelCode;
                Data.AccessLevel newLevel = !string.IsNullOrEmpty(newLevelCode) ? await _db.AccessLevels.FindAsync(newLevelCode) : null;
                int? newRoleId = newWorkplace.LocalRoleId;

                oldWorkplaces.TryGetValue(admId, out Workplace oldWorkplace);
                if (oldWorkplace != null)
                {
                    // Ако няма промяна в съответното поле, операцията се счита за успешна, независимо от правата.

                    if (newLevelCode != oldWorkplace.AccessLevelCode)
                    {
                        DemandPermission_AddRemoveOrSetAccessLevel(oldWorkplace, newLevel, currentUser);
                        if (!string.IsNullOrEmpty(newLevelCode))
                        {
                            oldWorkplace.AccessLevelCode = newLevelCode;
                        }
                        else  // Изтриване на работно място.
                        {
                            _db.Workplaces.Remove(oldWorkplace);
                        }
                    }

                    // Ако по-горе работното място е било маркирано за изтриване, следващите редакции нямат значение.
                    if (_db.Entry(oldWorkplace).State != EntityState.Deleted)
                    {
                        if (newRoleId != oldWorkplace.LocalRoleId)
                        {
                            LocalRole newRole = newRoleId.HasValue ? await _db.LocalRoles.FindAsync(admId, newRoleId) : null;
                            DemandPermission_SetLocalRole(oldWorkplace, newRole, currentUser);
                            oldWorkplace.LocalRoleId = newRoleId;
                        }
                    }

                    oldWorkplaces.Remove(admId);
                }
                else  // Избрано е ново работно място.
                {
                    if (!string.IsNullOrEmpty(newLevelCode))
                    {
                        // Локалната роля за новото работно място се копира от текущия потребител (за същата администрация).
                        // Така ръководител с локална роля може да добавя служители със собствената си длъжност/роля,
                        // въпреки че после няма да има право да променя длъжностите/ролите.
                        if (newRoleId == null)
                        {
                            newRoleId = currentUser.GetWorkplace(admId)?.LocalRoleId;
                        }
                        LocalRole newRole = newRoleId.HasValue ? await _db.LocalRoles.FindAsync(admId, newRoleId) : null;
                        Workplace workplace = new Workplace
                        {
                            UserId = userId,
                            AspNetUser = await _db.AspNetUsers.FindAsync(userId),
                            AdministrationId = admId,
                            Administration = await _db.Administrations.FindAsync(admId),
                            // AccessLevel се оставя празно, докато мине DemandPermission_AddRemoveOrSetAccessLevel().
                            LocalRoleId = newRoleId,
                            LocalRole = newRole
                        };

                        DemandPermission_AddRemoveOrSetAccessLevel(workplace, newLevel, currentUser);
                        workplace.AccessLevelCode = newLevelCode;
                        DemandPermission_SetLocalRole(workplace, newRole, currentUser);

                        _db.Workplaces.Add(workplace);
                    }
                }
            }

            // Всички "ненамерени" работни места се изтриват.
            foreach (Workplace oldWorkplace in oldWorkplaces.Values)
            {
                DemandPermission_AddRemoveOrSetAccessLevel(oldWorkplace, null, currentUser);
            }
            _db.Workplaces.RemoveRange(oldWorkplaces.Values);

            await SaveAndLogUserAsync(auditContext, userId);
        }

        public async Task DeleteUserAsync(string id, UserPermissionsModel currentUser, AuditModel auditContext)
        {
            AspNetUser user = await LoadUserWithPermissionsAsync(id);
            DemandPermission_UpdateUser(user, "изтрива", currentUser);
            _db.AspNetUsers.Remove(user);
            await SaveAndLogUserAsync(auditContext, id);
        }

        private async Task<AspNetUser> LoadUserWithPermissionsAsync(string id)
        {
            AspNetUser user = await _db.AspNetUsers.
                Include(u => u.AspNetRoles).
                Include(u => u.Workplaces).
                FirstOrDefaultAsync(u => u.Id == id);
            MustExist(user, id);
            return user;
        }

        private static void MustExist(object user, string id)
        {
            MustExist(user, "потребител", id);
        }

        #endregion

        #region Регистриране и одобряване/отказване на потребител

        public async Task<List<UserApprovalModel>> GetUsersAwaitingApprovalAsync(UserPermissionsModel currentUser)
        {
            // AspNetRoles и Workplaces се зареждат заради проверката CanUpdateUser().
            // Administration и AccessLevel се зареждат, защото имената им се показват на екрана.
            AspNetUser[] users = await (
                from u in _db.AspNetUsers.
                    Include(u => u.AspNetRoles).
                    Include(u => u.Workplaces.Select(w => w.Administration)).
                    Include(u => u.Workplaces.Select(w => w.AccessLevel))
                where !u.IsApproved.HasValue && !u.IsBanned
                select u
            ).ToArrayAsync();

            return (
                from u in users
                let w = u.Workplaces.FirstOrDefault()
                // Тъй като проверката "да бъде по-високо ниво във всяка от администрациите" е сложна, тя се прави в паметта.
                where CanUpdateUser(u, currentUser)
                select new UserApprovalModel
                {
                    UserId = u.Id,
                    UserDisplayName = u.PersonName ?? u.UserName,
                    Administration = w?.Administration.Name,
                    AccessLevel = w?.AccessLevel.Name
                }
            ).ToList();
        }

        public async Task<bool> RequestInitialWorkplaceAndQueueForApprovalAsync(string userId, RegisterModel registration, AuditModel auditContext)
        {
            if (!registration.AdministrationId.HasValue && !registration.PublicServiceProviderId.HasValue)
            {
                throw new Exception("Не е избрано работно място в завлението за регистриране на потребител.");
            }
            if (registration.PublicServiceProviderId == RegisterModel.NewPspId && string.IsNullOrEmpty(registration.NewPspName))
            {
                throw new Exception("Не е въведено наименованието, с което да се регистрира нов доставчик на обществени услуги.");
            }

            // Блокиран потребител не може да се регистрира повторно, независимо дали изчаква одобрение или е временно отказан.
            AspNetUser user = _db.AspNetUsers.Find(userId);
            if (user == null || user.IsBanned)
            {
                return false;
            }

            Dictionary<int, Workplace> oldWorkplaces = await _db.Workplaces.Where(w => w.UserId == userId).ToDictionaryAsync(w => w.AdministrationId);
            // Одобрен потребител може да се регистрира отново само след като администратор изтрие работните му места.
            // Потребител, който очаква одобрение или е отказан (и не е блокиран), може да промени мнението си и да повтори регистрацията с друго работно място.
            if (user.IsApproved == true && oldWorkplaces.Count > 0)
            {
                return false;
            }

            // Потребителят се добавя/връща в опашката за одобрение.
            user.IsApproved = null;

            int admId = registration.AdministrationId ?? registration.PublicServiceProviderId.Value;
            string newLevelCode = registration.AccessLevelCode;

            if (admId != RegisterModel.NewPspId)
            {
                oldWorkplaces.TryGetValue(admId, out Workplace oldWorkplace);
                if (oldWorkplace != null)
                {
                    oldWorkplace.AccessLevelCode = newLevelCode;
                    oldWorkplaces.Remove(admId);
                }
                else  // Избрано е ново работно място.
                {
                    Workplace workplace = new Workplace
                    {
                        UserId = userId,
                        AdministrationId = admId,
                        AccessLevelCode = newLevelCode
                    };
                    _db.Workplaces.Add(workplace);
                }
            }
            else  // Трябва да се регистрира нов доставчик на обществени услуги.
            {
                Administration newPsp = new Administration
                {
                    Name = registration.NewPspName,
                    Uic = registration.NewPspUic,
                    IsPublicServiceProvider = true
                };
                Workplace workplace = new Workplace
                {
                    UserId = userId,
                    Administration = newPsp,
                    AccessLevelCode = newLevelCode
                };
                _db.Workplaces.Add(workplace);
            }

            // Останалите работни места се изтриват.
            _db.Workplaces.RemoveRange(oldWorkplaces.Values);

            await SaveAndLogUserAsync(auditContext, userId);
            return true;
        }

        private Task SaveAndLogUserAsync(AuditModel auditContext, string userId)
        {
            return SaveAndLogAsync(auditContext, typeof(AspNetUser), userId);
        }

        #endregion

        #region Проверки на правата за редактиране

        public async Task DemandPermission_UpdateUserAsync(string id, string operationName, UserPermissionsModel currentUser)
        {
            AspNetUser user = await LoadUserWithPermissionsAsync(id);
            DemandPermission_UpdateUser(user, operationName, currentUser);
        }

        private static void DemandPermission_UpdateUser(AspNetUser user, string operationName, UserPermissionsModel currentUser)
        {
            if (!CanUpdateUser(user, currentUser))
            {
                throw new Exception($"Потребител {currentUser.DisplayName} няма право да {operationName} потребител {user.PersonName ?? user.UserName}.");
            }
        }

        private void DemandPermission_AddRemoveOrSetAccessLevel(Workplace workplace, Data.AccessLevel newAccessLevel, UserPermissionsModel currentUser)
        {
            if (!CanUpdateAccessLevel(workplace, newAccessLevel?.Code, currentUser))
            {
                string administrationName = workplace.Administration.Name;
                string oldAccessLevelName = workplace.AccessLevel?.Name;
                string newAccessLevelName = newAccessLevel?.Name;
                AspNetUser user = workplace.AspNetUser;
                string userDisplayName = user.PersonName ?? user.UserName;
                string userLocalRoleName = workplace.LocalRole != null ? workplace.LocalRole.Name : LocalRoleName.None;  // await GetLocalRoleNameAsync(administrationId, userLocalRoleId);

                WorkplaceModel cuWorkplace = currentUser.GetWorkplace(workplace.AdministrationId);
                string cuDescription = $"{(cuWorkplace != null ? cuWorkplace.AccessLevelName : "Потребител")} {currentUser.DisplayName}";
                bool cuIsManagerWithAnotherRole = cuWorkplace != null && cuWorkplace.IsManager &&
                    cuWorkplace.LocalRoleId.HasValue && cuWorkplace.LocalRoleId != workplace.LocalRoleId;

                string message;
                if (workplace.AccessLevel == null)
                {
                    if (cuIsManagerWithAnotherRole)
                    {
                        message = $"{cuDescription} с длъжност/роля {cuWorkplace.LocalRoleName} няма право да добавя служител с друга длъжност/роля в {administrationName}. " +
                            $"Опитът е за добавяне на {userDisplayName} с длъжност/роля {userLocalRoleName}.";
                    }
                    else
                    {
                        message = $"{cuDescription} няма право да добавя потребител в {administrationName} с ниво на достъп {newAccessLevelName}. " +
                            $"Опитът е за добавяне на {userDisplayName}.";
                    }
                }
                else if (newAccessLevel == null)
                {
                    if (cuIsManagerWithAnotherRole)
                    {
                        message = $"{cuDescription} с длъжност/роля {cuWorkplace.LocalRoleName} няма право да премахва служител с друга длъжност/роля от {administrationName}. " +
                            $"Опитът е за премахване на {userDisplayName} с длъжност/роля {userLocalRoleName}.";
                    }
                    else
                    {
                        message = $"{cuDescription} няма право да премахва потребител от {administrationName}. " +
                            $"Опитът е за премахване на {userDisplayName} с ниво на достъп {oldAccessLevelName}.";
                    }
                }
                else  // Очаква се старото и новото ниво на достъп да са различни.
                {
                    message = $"{cuDescription} няма право да променя нивото на достъп в {administrationName} от {oldAccessLevelName} на {newAccessLevelName} за потребител {userDisplayName}.";
                }
                throw new Exception(message);
            }
        }

        private void DemandPermission_SetLocalRole(Workplace workplace, LocalRole newLocalRole, UserPermissionsModel currentUser)
        {
            int administrationId = workplace.AdministrationId;
            AspNetUser user = workplace.AspNetUser;
            if (!currentUser.CanSetColleagueLocalRole(administrationId, newLocalRole?.Id))
            {
                string administrationName = workplace.Administration.Name;
                string newLocalRoleName = newLocalRole != null ? newLocalRole.Name : LocalRoleName.None;
                string userDisplayName = user.PersonName ?? user.UserName;
                string message = $"Потребител {currentUser.DisplayName} няма право да задава длъжност/роля в {administrationName}. " +
                    $"Опитът е за задаване на \"{newLocalRoleName}\" на {userDisplayName}.";
                throw new Exception(message);
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

            // Граничен случай: ако редактираният потребител няма нито едно работно място, всеки служител е по-високо ниво от него,
            // но не е редно всеки да може да го редактира, затова се изисква текущият потребител да бъде поне ръководител на нещо.
            if (otherUser.Workplaces.Count == 0)
            {
                return currentUser.CanAddEmployees;
            }

            // Текущият потребител трябва да бъде по-високо ниво от редактирания потребител във всяка от администрациите, в които работи редактираният.
            return otherUser.Workplaces.All(w =>
            {
                WorkplaceModel workplace = currentUser.GetWorkplace(w.AdministrationId);
                if (workplace == null)
                {
                    return false;
                }
                return workplace.IsAdmin && IsLowerLevel(w.AccessLevelCode, Model.AccessLevel.Admin) ||
                    workplace.IsManager && IsLowerLevel(w.AccessLevelCode, Model.AccessLevel.Manager);
            });
        }

        private static bool CanUpdateAccessLevel(Workplace workplace, string newAccessLevelCode, UserPermissionsModel currentUser)
        {
            // Само централните администратори могат да редактират нивото на достъп на други централни администратори.
            if (currentUser.IsGlobalAdmin)
            {
                return true;
            }
            if (workplace.AspNetUser.IsGlobalAdmin)
            {
                return false;
            }
            WorkplaceModel cuWorkplace = currentUser.GetWorkplace(workplace.AdministrationId);
            if (cuWorkplace == null)
            {
                return false;
            }

            string oldAccessLevelCode = workplace.AccessLevelCode;
            // Текущият потребител трябва да бъде по-високо ниво и да остава по-високо ниво след редакцията.
            return cuWorkplace.IsAdmin &&
                IsLowerLevel(oldAccessLevelCode, Model.AccessLevel.Admin) &&
                IsLowerLevel(newAccessLevelCode, Model.AccessLevel.Admin) ||
                cuWorkplace.IsManager &&
                IsLowerLevel(oldAccessLevelCode, Model.AccessLevel.Manager) &&
                IsLowerLevel(newAccessLevelCode, Model.AccessLevel.Manager) &&
                // Ръководител с локална роля може да създава/изтрива само служители със същата локална роля.
                (!cuWorkplace.LocalRoleId.HasValue || cuWorkplace.LocalRoleId == workplace.LocalRoleId);
        }

        private static bool IsLowerLevel(string accessLevelCode, string otherAccessLevelCode)
        {
            if (accessLevelCode == null)
            {
                return otherAccessLevelCode != null;
            }
            if (accessLevelCode == Model.AccessLevel.Employee)
            {
                return otherAccessLevelCode == Model.AccessLevel.Admin || otherAccessLevelCode == Model.AccessLevel.Manager;
            }
            if (accessLevelCode == Model.AccessLevel.Manager)
            {
                return otherAccessLevelCode == Model.AccessLevel.Admin;
            }
            if (accessLevelCode == Model.AccessLevel.Admin)
            {
                return false;
            }
            throw new NotSupportedException($"Не се поддържа локална роля {accessLevelCode}.");
        }

        #endregion
    }
}
