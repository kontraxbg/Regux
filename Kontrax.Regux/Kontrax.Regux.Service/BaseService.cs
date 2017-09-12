using System;
using System.Threading.Tasks;
using Kontrax.Regux.Data;

namespace Kontrax.Regux.Service
{
    public abstract class BaseService : IDisposable
    {
        protected readonly ReguxEntities _db = new ReguxEntities();

        public void Dispose()
        {
            _db.Dispose();
        }

        public Task SaveAsync()
        {
            // TODO: Централно логване на промените.
            return _db.SaveChangesAsync();
        }

        public void Save()
        {
            // TODO: Централно логване на промените.
            _db.SaveChanges();
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
