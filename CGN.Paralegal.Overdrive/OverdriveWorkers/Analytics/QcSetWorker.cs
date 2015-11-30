using System;
using System.Linq;
using System.Web;
using LexisNexis.Evolution.Business.Analytics;
using LexisNexis.Evolution.Business.Analytics.Entities;
using LexisNexis.Evolution.Business.Common;
using LexisNexis.Evolution.Business.UserManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Overdrive;
using LexisNexis.Evolution.TraceServices;
using Moq;
using System.Globalization;



namespace OverdriveWorkers.Analytics
{
    /// <summary>
    ///     QcSetWorker
    /// </summary>
    internal class QcSetWorker : WorkerBase
    {

        /// <summary>
        /// The _QC set parameter
        /// </summary>
        private QcSet _qcSetParameter;
        /// <summary>
        /// Begins the work.
        /// </summary>
        protected override void BeginWork()
        {
            BootParameters.ShouldNotBe(null);
            base.BeginWork();
            _qcSetParameter = (QcSet)XmlUtility.DeserializeObject(BootParameters, typeof(QcSet));
        }

        /// <summary>
        /// Generates the message.
        /// </summary>
        /// <returns></returns>
        protected override bool GenerateMessage()
        {
            var analysisSet = new AnalysisSet();
            analysisSet.ProgressChanged += ControlSetProgressChanged;
            SetUserMockSessionForJob(_qcSetParameter.CreatedBy);            
            analysisSet.CreateQcSet(_qcSetParameter.MatterId.ToString(CultureInfo.InvariantCulture), _qcSetParameter.DatasetId.ToString(CultureInfo.InvariantCulture), _qcSetParameter.ProjectId, WorkAssignment.JobId, _qcSetParameter);
            return true;
        }

        /// <summary>
        /// Controls the set progress changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        private void ControlSetProgressChanged(object sender, ProgressInfo e)
        {
            ReportProgress(e.TotalDocumentCount, e.ProcessedDocumentCount);
        }

        /// <summary>
        /// Set User Mock Session.
        /// </summary>
        private void SetUserMockSessionForJob(string createdBy)
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
    }
}
