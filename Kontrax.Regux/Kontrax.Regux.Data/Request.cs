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
    
    public partial class Request
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Request()
        {
            this.Audits = new HashSet<Audit>();
        }
    
        public int Id { get; set; }
        public int BatchId { get; set; }
        public int RegiXReportId { get; set; }
        public string Argument { get; set; }
        public string PersonOrCompanyId { get; set; }
        public byte[] RequestTimeStamp { get; set; }
        public byte[] ResponseTimeStamp { get; set; }
        public Nullable<System.DateTime> StartDateTime { get; set; }
        public Nullable<System.DateTime> EndDateTime { get; set; }
        public string Error { get; set; }
        public bool IsCanceled { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Audit> Audits { get; set; }
        public virtual Batch Batch { get; set; }
        public virtual RegiXReport RegiXReport { get; set; }
        public virtual Response Response { get; set; }
    }
}
