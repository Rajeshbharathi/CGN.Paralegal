//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace CGN.Paralegal.DAL
{
    using System;
    using System.Collections.Generic;
    
    public partial class Para_Legal_Contact
    {
        public int Para_legal_Contact_id { get; set; }
        public Nullable<int> Para_legal_id { get; set; }
        public string Email_id { get; set; }
        public Nullable<int> Phone { get; set; }
        public string Fax { get; set; }
        public string Address { get; set; }
        public string Website { get; set; }
        public string Created_by { get; set; }
        public Nullable<System.DateTime> Created_at { get; set; }
    
        public virtual Para_legal Para_legal { get; set; }
    }
}
