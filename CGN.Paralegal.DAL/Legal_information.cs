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
    
    public partial class Legal_information
    {
        public Legal_information()
        {
            this.Answers = new HashSet<Answer>();
            this.Questions = new HashSet<Question>();
        }
    
        public int Legal_information_id { get; set; }
        public string Legal_information_name { get; set; }
        public string Blogs { get; set; }
        public string Created_by { get; set; }
        public Nullable<System.DateTime> Created_at { get; set; }
    
        public virtual ICollection<Answer> Answers { get; set; }
        public virtual ICollection<Question> Questions { get; set; }
    }
}