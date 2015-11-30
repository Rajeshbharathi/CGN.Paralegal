using System.Web;
using LexisNexis.Evolution.Business.Analytics;
using LexisNexis.Evolution.Business.Analytics.Entities;
using LexisNexis.Evolution.Business.Common;
using LexisNexis.Evolution.Business.UserManagement;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Overdrive;
using System;
using System.Linq;
using LexisNexis.Evolution.TraceServices;
using Moq;

namespace LexisNexis.Evolution.Worker
{
    public class TrainModelWorker : WorkerBase
    {
        /// <summary>
        /// The trainingset parameter
        /// </summary>
        private ExampleSet _trainingSetParameter;
        /// <summary>
        /// Begins the work.
        /// </summary>
        protected override void BeginWork()
        {
            BootParameters.ShouldNotBe(null);
            base.BeginWork();
            _trainingSetParameter = (ExampleSet)XmlUtility.DeserializeObject(BootParameters, typeof(ExampleSet));
        }

        /// <summary>
        /// Generates the message.
        /// </summary>
        /// <returns></returns>
        protected override bool GenerateMessage()
        {
            var analysisSet = new AnalysisSet();
            analysisSet.ProgressChanged += TrainModelProgressChanged;
            SetUserMockSessionForJob(_trainingSetParameter.CreatedBy);
            analysisSet.TrainModel(_trainingSetParameter.DatasetId, _trainingSetParameter.MatterId, Convert.ToInt64(_trainingSetParameter.ProjectId),_trainingSetParameter.TrainingRound, _trainingSetParameter.IsActiveLearning, WorkAssignment.JobId, _trainingSetParameter.CreatedBy);
            return true;
        }

        /// <summary>
        /// TrainModel progress changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        private void TrainModelProgressChanged(object sender, ProgressInfo e)
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
