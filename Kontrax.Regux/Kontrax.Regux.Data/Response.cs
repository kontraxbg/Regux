//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Kontrax.Regux.Data
{
    using System;
    using System.Collections.Generic;
    
    public partial class Response
    {
        public int RequestId { get; set; }
        public string Document { get; set; }
    
        public virtual Request Request { get; set; }
    }
}
