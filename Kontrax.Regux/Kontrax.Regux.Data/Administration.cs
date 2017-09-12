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
    
    public partial class Administration
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Administration()
        {
            this.Children = new HashSet<Administration>();
            this.Services = new HashSet<Service>();
            this.UserLocalRoles = new HashSet<UserLocalRole>();
            this.Signals = new HashSet<Signal>();
        }
    
        public int Id { get; set; }
        public string Code { get; set; }
        public Nullable<int> ParentId { get; set; }
        public string Name { get; set; }
        public byte[] Certificate { get; set; }
        public byte[] ProposedCertificate { get; set; }
        public string CertificatePassword { get; set; }
        public string ProposedCertificatePassword { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Administration> Children { get; set; }
        public virtual Administration Parent { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Service> Services { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<UserLocalRole> UserLocalRoles { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Signal> Signals { get; set; }
    }
}
