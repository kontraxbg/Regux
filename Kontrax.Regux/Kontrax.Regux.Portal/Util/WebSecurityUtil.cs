using System.Security.Principal;
using System.Threading.Tasks;
using Kontrax.Regux.Model;
using Kontrax.Regux.Service;
using Microsoft.AspNet.Identity;

public static class WebSecurityUtil
{
    public static async Task<UserPermissionsModel> GetPermissionsAsync(this IPrincipal user)
    {
        if (user == null)
        {
            return new UserPermissionsModel
            {
                AdminOfAdministrationIds = new int[0],
                ManagerOfAdministrationIds = new int[0]
            };
        }

        string userId = user.Identity.GetUserId();
        using (PermissionService service = new PermissionService(userId))
        {
            return await service.GetPermissions();
        }
    }
}
