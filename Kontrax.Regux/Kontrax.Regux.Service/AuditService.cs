using Kontrax.Regux.Data;
using Kontrax.Regux.Model.Audit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontrax.Regux.Service
{
    public class AuditService : BaseService
    {
        public int Add(AuditModel model)
        {
            Audit audit = new Audit()
            {
                TimeAccessed = DateTime.Now,
                IPAddress = model.IPAddress,
                URL = model.URLAccessed,
                Data = model.Data,
                Duration = model.Duration.Ticks,
                UserName = model.UserName,
                UserID = model.UserID,
                Controller = model.Controller,
                Action = model.Action,
                SessionID = model.SessionID,
                RequestMethod = model.RequestMethod
            };

            _db.Audits.Add(audit);
            Save();
            return audit.ID;
        }

        public IQueryable<AuditViewModel> GetFiltered(AuditSearchModel model)
        {
            var results = GetAll();
            if (model.FromDate != null)
            {
                results = results.Where(i => i.TimeAccessed >= model.FromDate);
            }
            if (model.ToDate != null)
            {
                results = results.Where(i => i.TimeAccessed <= model.ToDate);
            }
            if (!string.IsNullOrWhiteSpace(model.SessionID))
            {
                results = results.Where(i => i.SessionID == model.SessionID.Trim());
            }

            return results;
        }

        public IQueryable<AuditViewModel> GetAllBySession(string session)
        {
            return GetAll().Where(i => i.SessionID == session);
        }

        public async Task EditActivityAsync(int id, string activity)
        {
            var dbAudit = await _db.Audits.FindAsync(id);
            if (dbAudit != null)
            {
                dbAudit.Activity = activity;
            }
            await SaveAsync();
        }

        public AuditModel GetById(int id)
        {
            var dbAudit = _db.Audits.Find(id);
            var audit = new AuditModel()
            {
                Action = dbAudit.Action,
                Controller = dbAudit.Controller,
                Data = dbAudit.Data,
                ID = dbAudit.ID,
                IPAddress = dbAudit.IPAddress,
                SessionID = dbAudit.SessionID,
                TimeAccessed = dbAudit.TimeAccessed,
                URLAccessed = dbAudit.URL,
                UserID = dbAudit.UserID,
                UserName = dbAudit.UserName,
                Activity = dbAudit.Activity,
                RequestMethod = dbAudit.RequestMethod
            };
            return audit;
        }

        public async Task<AuditViewModel> GetViewModelByIdAsync(int id)
        {
            var dbAudit = await _db.Audits.FindAsync(id);
            var audit = new AuditViewModel()
            {
                Action = dbAudit.Action,
                Controller = dbAudit.Controller,
                Data = dbAudit.Data,
                DurationTicks = dbAudit.Duration ?? 0,
                ID = dbAudit.ID,
                IPAddress = dbAudit.IPAddress,
                SessionID = dbAudit.SessionID,
                TimeAccessed = dbAudit.TimeAccessed,
                URLAccessed = dbAudit.URL,
                UserID = dbAudit.UserID,
                UserName = dbAudit.UserName,
                Activity = dbAudit.Activity,
                RequestMethod = dbAudit.RequestMethod
            };
            return audit;
        }


        public IQueryable<AuditViewModel> GetAll()
        {
            var audits =
                from i in _db.Audits
                orderby i.TimeAccessed descending
                select new AuditViewModel
                {
                    Action = i.Action,
                    Controller = i.Controller,
                    Data = i.Data,
                    DurationTicks = i.Duration ?? 0,
                    ID = i.ID,
                    IPAddress = i.IPAddress,
                    SessionID = i.SessionID,
                    TimeAccessed = i.TimeAccessed,
                    URLAccessed = i.URL,
                    UserID = i.UserID,
                    UserName = i.UserName,
                    Activity = i.Activity,
                    RequestMethod = i.RequestMethod
                };
            return audits;
        }

        public void UpdateDuration(int auditID, TimeSpan duration)
        {
            var dbAudit = _db.Audits.Find(auditID);
            if (dbAudit != null)
            {
                dbAudit.Duration = duration.Ticks;
                Save();
            }
        }
    }
}
