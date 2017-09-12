using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Kontrax.Regux.Portal.Attributes
{
    [AttributeUsageAttribute(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class SkipChangePasswordAttribute : Attribute
    {
    }
}