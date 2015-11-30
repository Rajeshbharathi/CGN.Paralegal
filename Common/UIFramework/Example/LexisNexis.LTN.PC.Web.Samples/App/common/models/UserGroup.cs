using System.Collections.Generic;

namespace LexisNexis.LTN.PC.Web.Samples.Models
{
    public class UserGroup
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public List<User> Users { get; set; }
    }
}