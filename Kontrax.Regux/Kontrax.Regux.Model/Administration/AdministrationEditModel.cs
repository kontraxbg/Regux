using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontrax.Regux.Model.Administration
{
    public class AdministrationEditModel: AdministrationModel
    {
        public IEnumerable<IdNameModel> Administrations { get; set; }
    }
}
