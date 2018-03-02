using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Kontrax.Regux.Model.UserManagement
{
    public class UserViewModel : CitizenViewModel
    {
        [Display(Name = "еАвт")]
        public bool IsEAuthEnabled { get; set; }

        [Display(Name = Role.GlobalAdminName)]
        public bool IsGlobalAdmin { get; set; }

        public IEnumerable<WorkplaceViewModel> Workplaces { get; set; }
    }
}
