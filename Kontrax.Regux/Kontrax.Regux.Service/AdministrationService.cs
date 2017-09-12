using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Kontrax.Regux.Data;
using Kontrax.Regux.Model;
using Kontrax.Regux.Model.Administration;
using System.Security.Cryptography.X509Certificates;

namespace Kontrax.Regux.Service
{
    public class AdministrationService : BaseService
    {
        public async Task<IdNameModel[]> GetAdministrationsAsync(bool includeGroups)
        {
            Administration[] administrations = await (
                from a in _db.Administrations
                where includeGroups || string.IsNullOrEmpty(a.Code)
                orderby a.Name
                select a
            ).ToArrayAsync();
            return administrations.Select(a => new IdNameModel(a.Id, a.Name)).ToArray();
        }

        /// <summary>
        /// Само централен администратор може да избира измежду всички администрации.
        /// Останалите потребители има смисъл да избират само администрации, за които имат поне ниво Ръководител.
        /// </summary>
        public async Task<IdNameModel[]> GetAllowedAdministrationsAsync(UserPermissionsModel currentUser)
        {
            if (currentUser.IsGlobalAdmin)
            {
                return await GetAdministrationsAsync(false);
            }
            Administration[] administrations = await (
                from a in _db.Administrations
                where currentUser.ManagerOfAdministrationIds.Contains(a.Id)
                orderby a.Name
                select a
            ).ToArrayAsync();
            return administrations.Select(a => new IdNameModel(a.Id, a.Name)).ToArray();
        }

        public async Task<AdministrationEditModel> GetAdministrationForEditAsync(int id)
        {
            var administration = await (from i in _db.Administrations
                                        where i.Id == id
                                        select new AdministrationEditModel()
                                        {
                                            Id = i.Id,
                                            Code = i.Code,
                                            Name = i.Name,
                                            ParentId = i.ParentId,
                                            Certificate = i.Certificate,
                                            ProposedCertificate = i.ProposedCertificate
                                        }).FirstOrDefaultAsync();
            return administration;
        }

        public async Task<AdministrationViewModel> GetAdministrationByIdAsync(int id)
        {
            var administration = await (from i in _db.Administrations
                                        where i.Id == id
                                        select new AdministrationViewModel()
                                        {
                                            Id = i.Id,
                                            Code = i.Code,
                                            Name = i.Name,
                                            ParentId = i.ParentId,
                                            ParentName = i.Parent.Name,
                                            Certificate = i.Certificate,
                                            ProposedCertificate = i.ProposedCertificate
                                        }).FirstOrDefaultAsync();
            if (administration.Certificate != null)
            {
                administration.CertificateView = new X509Certificate2(administration.Certificate, "password");
            }
            if (administration.ProposedCertificate != null)
            {
                administration.ProposedCertificateView = new X509Certificate2(administration.ProposedCertificate, "password");
            }
            return administration;
        }

        public async Task<IEnumerable<AdministrationModel>> GetAdministrationIndexAsync()
        {
            var administration = await (from i in _db.Administrations
                                        select new AdministrationIndexModel()
                                        {
                                            Id = i.Id,
                                            Code = i.Code,
                                            Name = i.Name,
                                            ParentId = i.ParentId,
                                            HasCertificate = i.Certificate != null,
                                            CertificateIsPending = i.ProposedCertificate != null
                                        }).ToListAsync();
            return administration;
        }

        public async Task UpdateAdministrationAsync(AdministrationModel model)
        {
            var dbAdministration = await _db.Administrations.FindAsync(model.Id);
            dbAdministration.Code = model.Code;
            dbAdministration.Name = model.Name;
            dbAdministration.ParentId = model.ParentId;
            if (dbAdministration.Certificate == null)
            {
                dbAdministration.Certificate = model.Certificate;
            }
            if (dbAdministration.ProposedCertificate == null)
            {
                dbAdministration.ProposedCertificate = model.ProposedCertificate;
                dbAdministration.ProposedCertificatePassword = model.ProposedCertificatePassword;
            }
            await SaveAsync();
        }

        public Task<string> GetAdministrationNameAsync(int administrationId)
        {
            return (
                from a in _db.Administrations
                where a.Id == administrationId
                select a.Name
            ).FirstOrDefaultAsync();
        }

        public async Task<bool> GenerateProposedCertificate(int id)
        {
            var administration = await _db.Administrations.FindAsync(id);
            if (administration != null)
            {
                var _certificateService = new CertificateService();
                string subjectName = $"CN={administration.Name}";
                var certificate = _certificateService.CreateCertificateAuthorityCertificate(subjectName, null, null);
                string certificatePassword = "password";
                var certificateData = _certificateService.WriteCertificate(certificate, certificatePassword);
                administration.ProposedCertificate = certificateData;
                administration.ProposedCertificatePassword = certificatePassword;
                await SaveAsync();
                return true;
            }
            else
            {
                return false;
            }

        }

        public async Task<bool> ApproveProposedCertificate(int id)
        {
            var administration = await _db.Administrations.FindAsync(id);
            if (administration != null && administration.ProposedCertificate != null)
            {
                administration.Certificate = administration.ProposedCertificate;
                administration.CertificatePassword = administration.CertificatePassword;
                administration.ProposedCertificate = null;
                administration.ProposedCertificatePassword = null;

                await SaveAsync();
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> RemoveProposedCertificate(int id)
        {
            var administration = await _db.Administrations.FindAsync(id);
            if (administration != null && administration.ProposedCertificate != null)
            {
                administration.ProposedCertificate = null;

                await SaveAsync();
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> RemoveCertificate(int id)
        {
            var administration = await _db.Administrations.FindAsync(id);
            if (administration != null && administration.Certificate != null)
            {
                administration.Certificate = null;

                await SaveAsync();
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
