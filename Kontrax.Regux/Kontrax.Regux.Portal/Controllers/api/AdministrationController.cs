using Kontrax.Regux.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace Kontrax.Regux.Portal.Controllers.api
{
    public class AdministrationController : ApiController
    {
        protected readonly AdministrationService _administrationService = new AdministrationService();

        [HttpGet]
        public async Task<bool> ApproveProposedCertificate(int id)
        {
            return await _administrationService.ApproveProposedCertificate(id);
        }

        [HttpGet]
        public async Task<bool> GenerateProposedCertificate(int id)
        {
            return await _administrationService.GenerateProposedCertificate(id);
        }

        [HttpGet]
        public async Task<bool> RemoveProposedCertificate(int id)
        {
            return await _administrationService.RemoveProposedCertificate(id);
        }

        [HttpGet]
        public async Task<bool> RemoveCertificate(int id)
        {
            return await _administrationService.RemoveCertificate(id);
        }
    }
}
