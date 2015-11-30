using System;
using System.IO;
using System.Web;
using System.Xml.Serialization;
using LexisNexis.Evolution.Business.UserManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Infrastructure.WebOperationContextManagement;
using LexisNexis.Evolution.ServiceImplementation.DatasetManagement;
using LexisNexis.Evolution.ServiceImplementation.JobMgmt;
using Moq;

namespace LexisNexis.Evolution.Overdrive
{
    internal class ClusterJobWrapper
    {
        #region Helpers

        private IWebOperationContext EstablishSession(string createdBy)
        {
            var userProp = new UserBusinessEntity { UserGUID = createdBy };
            //Mock HttpContext & HttpSession : Calling from Worker so doesn't contain HttpContext. 
            var mockContext = new Mock<HttpContextBase>();
            var mockSession = new Mock<HttpSessionStateBase>();
            userProp = UserBO.AuthenticateUsingUserGuid(createdBy);
            var userSession = new UserSessionBEO();
            SetUserSession(createdBy, userProp, userSession);
            mockSession.Setup(ctx => ctx["UserDetails"]).Returns(userProp);
            mockSession.Setup(ctx => ctx["UserSessionInfo"]).Returns(userSession);
            mockSession.Setup(ctx => ctx.SessionID).Returns(Guid.NewGuid().ToString());
            mockContext.Setup(ctx => ctx.Session).Returns(mockSession.Object);
            EVHttpContext.CurrentContext = mockContext.Object;
            return new MockWebOperationContext().Object;
        }

        private void SetUserSession(string createdByGuid, UserBusinessEntity userProp, UserSessionBEO userSession)
        {
            userSession.UserId = userProp.UserId;
            userSession.UserGUID = createdByGuid;
            userSession.DomainName = userProp.DomainName;
            userSession.IsSuperAdmin = userProp.IsSuperAdmin;
            userSession.LastPasswordChangedDate = userProp.LastPasswordChangedDate;
            userSession.PasswordExpiryDays = userProp.PasswordExpiryDays;
            userSession.Timezone = userProp.Timezone;
        }

        #endregion

        #region Cancel Cluster Job

        internal void CancelClusterJob(int jobId, string datasetId, string userGuid)
        {
            try
            {
                var jobManagement = new JobMgmtService();
                jobManagement.SetWebOperationContext(EstablishSession(userGuid));
                jobManagement.CancelClusterJob(datasetId);
            }
            catch (Exception ex)
            {
                ex.AddDbgMsg("Unable to validate if cluster job exists and cancel it for Job Id: {0}", jobId);
                ex.Trace();
            }
        }

        #endregion

        #region Update Cluster Status

        internal void UpdateClusterStatus(BaseJobBEO job)
        {
            try
            {
                dynamic bootParameters;
                StringReader stream;
                XmlSerializer xmlStream;
                switch (job.JobTypeId)
                {
                    case 2:
                    case 8:
                        stream = new StringReader(job.BootParameters);
                        xmlStream = new XmlSerializer(typeof(ProfileBEO));
                        bootParameters = xmlStream.Deserialize(stream);
                        stream.Close();
                        var profileBeo = bootParameters as ProfileBEO;
                        if (profileBeo != null)
                        {
                            var dataSetService = new DataSetService(EstablishSession(profileBeo.CreatedBy));
                            dataSetService.UpdateClusterStatus(profileBeo.DatasetDetails.FolderID.ToString(),
                                                               ClusterStatus.OutOfDate.ToString());
                        }
                        break;
                    case 14:
                        stream = new StringReader(job.BootParameters);
                        xmlStream = new XmlSerializer(typeof(ImportBEO));
                        bootParameters = xmlStream.Deserialize(stream);
                        stream.Close();
                        var importBeo = bootParameters as ImportBEO;
                        if (importBeo != null)
                        {
                            var dataSetService = new DataSetService(EstablishSession(importBeo.CreatedBy));
                            dataSetService.UpdateClusterStatus(importBeo.DatasetId.ToString(),
                                                               ClusterStatus.OutOfDate.ToString());
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                ex.AddDbgMsg("Unable to update cluster status to out of date by Job Id: {0}", job.JobId);
                ex.Trace();
            }
        }

        #endregion
    }
}