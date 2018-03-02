using System.Security.Principal;
using System.Threading.Tasks;
using Kontrax.Regux.Model;
using Kontrax.Regux.Service;
using Microsoft.AspNet.Identity;

namespace Kontrax.Regux.Portal
{
    public static class WebSecurityUtil
    {
        public static async Task<UserPermissionsModel> GetPermissionsAsync(this IPrincipal user)
        {
            if (user == null)
            {
                return new UserPermissionsModel(null, null, false);
            }

            string userId = user.Identity.GetUserId();
            using (PermissionService service = new PermissionService(userId))
            {
                return await service.GetPermissionsAsync();
            }
        }
    }
}
