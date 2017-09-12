using System.Linq;

namespace Kontrax.Regux.Data
{
    public partial class AspNetUser
    {
        public bool IsGlobalAdmin
        {
            get
            {
                // Константите са в Model проекта и не могат да се ползват тук.
                return AspNetRoles.Any(r => r.Name == "GlobalAdmin");
            } 
        }
    }
}
