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
    
    public partial class Paralegal_Comment
    {
        public long CommentID { get; set; }
        public string CommentText { get; set; }
        public string CreatedBy { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public Nullable<System.DateTime> LastUpdatedDate { get; set; }
        public int Para_legal_id { get; set; }
    
        public virtual Para_legal Para_legal { get; set; }
    }
}
