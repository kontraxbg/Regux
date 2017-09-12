using Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Kontrax.Regux.Portal.Adapters
{
    public class LocalizedRequiredAttributeAdapter: RequiredAttributeAdapter
    {
        public LocalizedRequiredAttributeAdapter(ModelMetadata metadata, 
                                            ControllerContext context, 
                                            RequiredAttribute attribute) 
                 : base(metadata, context, attribute)
          {
              if (attribute.ErrorMessage == null)
              {
                  if (attribute.ErrorMessageResourceType == null)
                      attribute.ErrorMessageResourceType = typeof(Messages);
                  if (attribute.ErrorMessageResourceName == null)
                      attribute.ErrorMessageResourceName = "RequiredAttribute_ValidationError";
              }
          }
    }
}