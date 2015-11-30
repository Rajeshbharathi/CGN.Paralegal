using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using LexisNexis.Evolution.Business.UserManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure;
using Moq;

namespace LexisNexis.Evolution.Worker
{
    internal static class Utility
    {
        /// <summary>
        /// Sets the user session.
        /// </summary>
        /// <param name="createdUserGuid">The created user unique identifier.</param>
        internal static void SetUserSession(string createdUserGuid)
        {
            if (EVHttpContext.CurrentContext != null) return;
            var userProp = UserBO.GetUserUsingGuid(createdUserGuid);
            var userSession = new UserSessionBEO();

            var mockContext = new Mock<HttpContextBase>();
            var mockSession = new Mock<HttpSessionStateBase>();
            userSession.UserId = userProp.UserId;
            userSession.UserGUID = createdUserGuid;
            userSession.DomainName = userProp.DomainName;
            userSession.IsSuperAdmin = userProp.IsSuperAdmin;
            userSession.LastPasswordChangedDate = userProp.LastPasswordChangedDate;
            userSession.PasswordExpiryDays = userProp.PasswordExpiryDays;
            userSession.Timezone = userProp.Timezone;
            userSession.FirstName = userProp.FirstName;
            userSession.LastName = userProp.LastName;
            mockSession.Setup(ctx => ctx["UserDetails"]).Returns(userProp);
            mockSession.Setup(ctx => ctx["UserSessionInfo"]).Returns(userSession);
            mockSession.Setup(ctx => ctx.SessionID).Returns(Guid.NewGuid().ToString());
            mockContext.Setup(ctx => ctx.Session).Returns(mockSession.Object);
            EVHttpContext.CurrentContext = mockContext.Object;
        }
        
        
    }
}
