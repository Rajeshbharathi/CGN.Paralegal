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
    
    public partial class Case
    {
        public Case()
        {
            this.Case_Area_of_law = new HashSet<Case_Area_of_law>();
            this.Case_location = new HashSet<Case_location>();
            this.Para_legal_cases = new HashSet<Para_legal_cases>();
        }
    
        public int Case_id { get; set; }
        public string Case_description { get; set; }
        public string Created_by { get; set; }
        public Nullable<System.DateTime> Created_at { get; set; }
    
        public virtual ICollection<Case_Area_of_law> Case_Area_of_law { get; set; }
        public virtual ICollection<Case_location> Case_location { get; set; }
        public virtual ICollection<Para_legal_cases> Para_legal_cases { get; set; }
    }
}