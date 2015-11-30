using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using LexisNexis.LTN.PC.Web.Samples.Models;

namespace LexisNexis.LTN.PC.Web.Samples.Controllers
{
    public class UserPickerController : ApiController
    {
        /// <summary>
        /// Get User group and User 
        /// </summary>
        [Route("api/userpicker/usergroupsandusers/{organizationId}")]
        public HttpResponseMessage GetUsergroupsAndUsers(string organizationId)
        {
            try
            {
                //TODO : Data need to fetch from WCF
                var userGroup = GetData();          
                
                 return Request.CreateResponse<IEnumerable<UserGroup>>(HttpStatusCode.OK, userGroup);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
      

        /// <summary>
        /// Get User group and User 
        /// </summary>
        [Route("api/userpicker/usergroups/{organizationId}")]
        public HttpResponseMessage GetUsergroups(string organizationId)
        {
            try
            {
                //TODO : Data need to fetch from WCF
                var userGroup = GetUserGroups();

                return Request.CreateResponse<IEnumerable<UserGroup>>(HttpStatusCode.OK, userGroup);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

      

        /// <summary>
        /// Get User group and User 
        /// </summary>
        [Route("api/userpicker/users/{organizationId}")]
        public HttpResponseMessage GetUsers(string organizationId)
        {
            try
            {
                //TODO : Data need to fetch from WCF
                var user = GetUsers();

                return Request.CreateResponse<IEnumerable<User>>(HttpStatusCode.OK, user);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

      

        /// <summary>
        /// Get User group and User 
        /// </summary>
        [Route("api/userpicker/usergroupswithusers/filter/{organizationId}")]
        public HttpResponseMessage PostFilterUsergroupsAndUsers(string organizationId, UserPickerFilter filter)
        {
            try
            {
                //TODO : Data need to fetch from WCF
                var userGroup = GetData();


                var searchText = !string.IsNullOrEmpty(filter.FilterText)?filter.FilterText.ToLower(CultureInfo.CurrentCulture):string.Empty;
                //For custom filter
                var filteredGroups = userGroup.Where(u => u.UserName.ToLower(CultureInfo.CurrentCulture).Contains(searchText) || u.Users.Exists(s => s.UserName.ToLower(CultureInfo.CurrentCulture).Contains(searchText))).ToList();
                foreach (var filteredGroup in filteredGroups)
                {
                    var result = filteredGroup.Users.Where(s => s.UserName.ToLower(CultureInfo.CurrentCulture).Contains(searchText)).ToList();
                    filteredGroup.Users.Clear();
                    filteredGroup.Users = result;
                }

                return Request.CreateResponse<IEnumerable<UserGroup>>(HttpStatusCode.OK, filteredGroups);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }


        #region
        private static List<UserGroup> GetData()
        {
            //TODO : User list need to fetch from WCF
            var userList1 = new List<User>
                                {
                                    new User {UserName = "User1010", UserId = "1010"},
                                    new User {UserName = "senthil", UserId = "1011"}
                                };
            var userList2 = new List<User>
                                {
                                    new User {UserName = "User2010", UserId = "2010"},
                                    new User {UserName = "senthilkumar paramathma", UserId = "2011"}
                                };

            var userList3 = new List<User>
                                {
                                    new User {UserName = "pc User2010", UserId = "3010"},
                                    new User {UserName = "pc senthilkumar paramathma", UserId = "3011"}
                                };

            var userGroup = new List<UserGroup>
                                  {
                                      new UserGroup{UserName="Group-LN",UserId="1001",Users = userList1},
                                      new UserGroup{UserName="Group-EV",UserId="1002",Users = userList2},
                                       new UserGroup{UserName="TAR",UserId="1003",Users = userList3}
                                  };
            return userGroup;
        }


        private static List<User> GetUsers()
        {
            //TODO : User list need to fetch from WCF
            var user = new List<User>
                       {
                           new User {UserName = "User1001", UserId = "1001"},
                           new User {UserName = "senthil", UserId = "1002"},
                           new User {UserName = "Senthilkumar Paramathma", UserId = "2001"},
                           new User {UserName = "paramasx", UserId = "2002"},
                           new User {UserName = "pc reviewer", UserId = "3001"},
                           new User {UserName = "pc admin", UserId = "4002"},
                       };
            return user;
        }

        private static List<UserGroup> GetUserGroups()
        {
            //TODO : User Group list need to fetch from WCF
            var userGroup = new List<UserGroup>
                            {
                                new UserGroup {UserName = "Group-1001", UserId = "1001"},
                                new UserGroup {UserName = "Group-1002", UserId = "1002"},
                                new UserGroup {UserName = "LN-ORG", UserId = "2001"},
                                new UserGroup {UserName = "LN-ADM", UserId = "2002"},
                                new UserGroup {UserName = "LN REVIEW", UserId = "3001"},
                                new UserGroup {UserName = "MTS-PROD", UserId = "4002"},
                            };
            return userGroup;
        }
        #endregion

    }
}