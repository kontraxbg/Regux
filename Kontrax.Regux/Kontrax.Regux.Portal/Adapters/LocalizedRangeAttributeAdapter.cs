using Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Kontrax.Regux.Portal.Adapters
{
    public class LocalizedRangeAttributeAdapter : RangeAttributeAdapter
    {
        public LocalizedRangeAttributeAdapter(
            ModelMetadata metadata,
            ControllerContext context,
            RangeAttribute attribute
        )
            : base(metadata, context, attribute)
        {
            if (attribute.ErrorMessage == null)
            {
                if (attribute.ErrorMessageResourceType == null)
                    attribute.ErrorMessageResourceType = typeof(Messages);
                if (attribute.ErrorMessageResourceName == null)
                    attribute.ErrorMessageResourceName = "RangeAttribute_ValidationError";
            }
        }
    }
}
