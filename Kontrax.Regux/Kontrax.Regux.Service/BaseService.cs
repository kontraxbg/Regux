using System;
using System.Threading.Tasks;
using Kontrax.Regux.Data;
using Kontrax.Regux.Model.Audit;

namespace Kontrax.Regux.Service
{
    public abstract class BaseService : IDisposable
    {
        protected readonly ReguxEntities _db = new ReguxEntities();

        public void Dispose()
        {
            _db.Dispose();
        }

        /// <summary>
        /// Опростен вариант за master entity-та с числово id. Спестява досадното писане на typeof(Entity) и id.ToString().
        /// </summary>
        public Task SaveAndLogAsync(AuditModel audit, object logMasterEntity, int logMasterEntityId)
        {
            if (logMasterEntity == null)
            {
                throw new ArgumentNullException(nameof(logMasterEntity));
            }
            return SaveAndLogAsync(audit, logMasterEntity.GetType(), logMasterEntityId.ToString());
        }

        /// <summary>
        /// Използва се само при създаване на новo master entity.
        /// </summary>
        protected Task SaveAndLogAsync(AuditModel audit, object logNewMasterEntity)
        {
            if (logNewMasterEntity == null)
            {
                throw new ArgumentNullException(nameof(logNewMasterEntity));
            }
            // Вторият параметър е null, защото entity-то още няма id, НО след _db.SaveChangesAsync() id-то се появява и
            // log.EntityRecordId се попълва автоматично с id-то на първия намерен обект от подадения тип.
            return SaveAndLogAsync(audit, logNewMasterEntity.GetType(), null);
        }

        public Task SaveAndLogAsync(AuditModel audit, Type auditMasterEntityType, string auditMasterEntityId)
        {
            return ChangeLogger.SaveChangesAsync(_db, _db.SaveChangesWithValidationExplainedAsync, audit, auditMasterEntityType, auditMasterEntityId);
        }

        public int Log(AuditModel model)
        {
            return AuditHashUtil.Add(_db, model);
        }

        public static void MustExist(object o, string type, object id)
        {
            if (o == null)
            {
                throw new Exception($"Не съществува {type} с id {id}.");
            }
        }
    }
}
