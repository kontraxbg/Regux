using System.ComponentModel.DataAnnotations;

namespace Kontrax.Regux.Model.UserManagement
{
    public abstract class UserBaseModel
    {
        protected UserBaseModel()
        {
            NewWorkplace = new WorkplaceCreateModel();
        }

        public UserPermissionsModel CurrentUser { get; set; }

        [Display(Name = "Трите имена")]
        public string PersonName { get; set; }

        public WorkplaceCreateModel NewWorkplace { get; set; }
    }
}
