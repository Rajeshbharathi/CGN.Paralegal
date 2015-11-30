using System;
using System.Globalization;
using System.Linq;
using System.Web;
using LexisNexis.Evolution.Business.Analytics;
using LexisNexis.Evolution.Business.Analytics.Entities;
using LexisNexis.Evolution.Business.Common;
using LexisNexis.Evolution.Business.UserManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.TraceServices;
using Moq;

namespace OverdriveWorkers.Analytics
{
    /// <summary>
    /// ControlSetWorker
    /// </summary>
    internal class ControlSetWorker : WorkerBase
    {
        /// <summary>
        ///     The _job parameter
        /// </summary>
        private ControlSet _jobParameter;

        protected override void BeginWork()
        {
            BootParameters.ShouldNotBe(null);
            base.BeginWork();
            _jobParameter =
                (ControlSet)XmlUtility.DeserializeObject(BootParameters, typeof(ControlSet));
        }
        protected override bool GenerateMessage()
        {
            var analysisSet = new AnalysisSet();
            analysisSet.ProgressChanged += ControlSetProgressChanged;
            SetUserMockSessionForJob(_jobParameter.CreatedBy);
            analysisSet.CreateControlSet(_jobParameter.MatterId.ToString(CultureInfo.InvariantCulture),_jobParameter.DatasetId.ToString(CultureInfo.InvariantCulture), _jobParameter.ProjectId,WorkAssignment.JobId, _jobParameter);
            return true;
        }

        private void ControlSetProgressChanged(object sender, ProgressInfo e)
        {
            ReportProgress(e.TotalDocumentCount,e.ProcessedDocumentCount);
        }

        /// <summary>
        /// Set User Mock Session.
        /// </summary>
        private void SetUserMockSessionForJob(string createdBy)
        {
            try
            {
                var mockContext = new Mock<HttpContextBase>();
                var mockSession = new Mock<HttpSessionStateBase>();
                var userProp = UserBO.GetUserByGuid(createdBy);
                var userSession = new UserSessionBEO
                {
                    UserId = userProp.UserId,
                    UserGUID = createdBy,
                    DomainName = userProp.DomainName,
                    IsSuperAdmin = userProp.IsSuperAdmin,
                    LastPasswordChangedDate = userProp.LastPasswordChangedDate,
                    PasswordExpiryDays = userProp.PasswordExpiryDays,
                    Timezone = userProp.Timezone
                };
                if (userProp.Organizations.Any())
                    userSession.Organizations.AddRange(userProp.Organizations);
                mockSession.Setup(ctx => ctx["UserDetails"]).Returns(userProp);
                mockSession.Setup(ctx => ctx["UserSessionInfo"]).Returns(userSession);
                mockSession.Setup(ctx => ctx.SessionID).Returns(Guid.NewGuid().ToString());
                mockContext.Setup(ctx => ctx.Session).Returns(mockSession.Object);
                EVHttpContext.CurrentContext = mockContext.Object;
            }
            catch (Exception ex)
            {
                Tracer.Info("Create controlSet Job {0} : Failed to set user info ", WorkAssignment.JobId);
                ex.Trace().Swallow();
            }
        }
    }
}
