namespace CGN.Paralegal.Mocks
{
    using System;
    using System.Collections.Generic;

    using ClientContracts.Analytics;

    public class MockWorkflowState
    {
        public static event EventHandler<WorkflowStateChangedEventArgs> WorkflowStateChanged;

        public static AnalyticsWorkflowState ChangeToState = new AnalyticsWorkflowState();

        public static AnalyticsWorkflowState ProjectSetup = new AnalyticsWorkflowState
        {
            Name = State.ProjectSetup,
            CreateStatus = Status.NotStarted,
            ReviewStatus = Status.NotStarted,
            IsCurrent = false,
            Order = 1
        };

        public static AnalyticsWorkflowState ControlSet = new AnalyticsWorkflowState
        {
            Name = State.ControlSet,
            CreateStatus = Status.NotStarted,
            ReviewStatus = Status.NotStarted,
            IsCurrent = false,
            Order = 2
        };

        public static AnalyticsWorkflowState TrainingSet = new AnalyticsWorkflowState
        {
            Name = State.TrainingSet,
            CreateStatus = Status.NotStarted,
            ReviewStatus = Status.NotStarted,
            IsCurrent = false,
            Order = 3
        };

        public static AnalyticsWorkflowState PredictSet = new AnalyticsWorkflowState
        {
            Name = State.PredictSet,
            CreateStatus = Status.NotStarted,
            ReviewStatus = Status.NotStarted,
            IsCurrent = false,
            Order = 4
        };

        public static AnalyticsWorkflowState QcSet = new AnalyticsWorkflowState
        {
            Name = State.QcSet,
            CreateStatus = Status.NotStarted,
            ReviewStatus = Status.NotStarted,
            IsCurrent = false,
            Order = 5
        };

        public static AnalyticsWorkflowState Done = new AnalyticsWorkflowState
        {
            Name = State.Done,
            CreateStatus = Status.NotStarted,
            ReviewStatus = Status.NotStarted,
            IsCurrent = false,
            Order = 6
        };

        public static List<AnalyticsWorkflowState> WorkflowState = new List<AnalyticsWorkflowState>
        {
            ProjectSetup,
            ControlSet,
            TrainingSet,
            PredictSet,
            QcSet,
            Done
        };

        public static AnalyticsWorkflowState UpdateState(State name, Status createStatus, Status reviewStatus, bool isCurrent)
        {
            if (isCurrent) //if setting new state to current, all other states should not be current
            {
                foreach (var state in WorkflowState)
                {
                    state.IsCurrent = false;
                }
            }

            AnalyticsWorkflowState updatedState = WorkflowState.Find(p => p.Name == name);
            updatedState.CreateStatus = createStatus;
            updatedState.ReviewStatus = reviewStatus;
            updatedState.IsCurrent = isCurrent;

            FireWorkflowStateChangedEvent();

            return updatedState;
        }

        public static List<AnalyticsWorkflowState> UpdateStates(List<AnalyticsWorkflowState> workflowState)
        {
            //get current state
            var preCurrentState = CurrentState;

            //Updating next workflow state
            if (workflowState.Count == 1)
            {
                if (workflowState[0].Name == State.TrainingSet)
                {
                    workflowState[0].CreateStatus = Status.Completed;
                    workflowState[0].IsCurrent = true;
                }
                if (workflowState[0].ReviewStatus == Status.NotStarted)
                {
                    ChangeToState = workflowState[0];   
                }
            }
            //else if (TrainingSet.ReviewStatus == Status.NotStarted)
            //{
            //    ChangeToState = new State();
            //}

            ProjectSetup = workflowState.Find(p => p.Name == State.ProjectSetup) ?? ProjectSetup;
            ControlSet = workflowState.Find(p => p.Name == State.ControlSet) ?? ControlSet;
            TrainingSet = workflowState.Find(p => p.Name == State.TrainingSet) ?? TrainingSet;
            PredictSet = workflowState.Find(p => p.Name == State.PredictSet) ?? PredictSet;
            QcSet = workflowState.Find(p => p.Name == State.QcSet) ?? QcSet;
            Done = workflowState.Find(p => p.Name == State.Done) ?? Done;
            
            ProjectSetup.Order=1;
            ControlSet.Order = 2;
            TrainingSet.Order = 3;
            PredictSet.Order = 4;
            QcSet.Order = 5;
            Done.Order = 6;
            

            WorkflowState[0] = ProjectSetup;
            WorkflowState[1] = ControlSet;
            WorkflowState[2] = TrainingSet;
            WorkflowState[3] = PredictSet;
            WorkflowState[4] = QcSet;
            WorkflowState[5] = Done;

            AnalyticsWorkflowState postCurrentState = CurrentState;

            if (preCurrentState != null && postCurrentState == null)
            {
                SetCurrentState();
            }

            FireWorkflowStateChangedEvent();

            return WorkflowState;
        }

        private static AnalyticsWorkflowState CurrentState
        {
            get
            {
                return WorkflowState.Find(p => p.IsCurrent == true);
            }
        }

        private static void SetCurrentState()
        {
            for(int i = WorkflowState.Count-1; i>=0; i--)
            {
                AnalyticsWorkflowState state = WorkflowState[i];
                if (state.CreateStatus != Status.NotStarted)
                {
                    state.IsCurrent = true;
                    break;
                }

            }
        }

        public static void SetReviewStatus(AnalysisSetType type, Status status)
        {
            switch (type)
            {
                case AnalysisSetType.ControlSet:
                    ControlSet.ReviewStatus = status;
                    break;
                case AnalysisSetType.TrainingSet:
                    status = ChangeToState.Name == State.PredictSet && status == Status.Completed
                        ? Status.Completed
                        : Status.Inprogress;
                    TrainingSet.ReviewStatus = status;
                    break;
                case AnalysisSetType.PredictSet:
                    PredictSet.ReviewStatus = status;
                    break;
                case AnalysisSetType.QcSet:
                    QcSet.ReviewStatus = status;
                    break;
            }

            FireWorkflowStateChangedEvent();
        }

        public static void Initialize()
        {
            foreach (var state in WorkflowState)
            {
                state.CreateStatus = Status.NotStarted;
                state.ReviewStatus = Status.NotStarted;
                state.IsCurrent = false;
            }

            FireWorkflowStateChangedEvent();
        }

        private static void FireWorkflowStateChangedEvent()
        {
            var projectId = new MockAppStateRestClient().GetAppState().ProjectId;
            var args = new WorkflowStateChangedEventArgs { ProjectId = projectId, WorkflowState = WorkflowState };
            EventHandler<WorkflowStateChangedEventArgs> handler = WorkflowStateChanged;
            if (handler != null)
            {
                handler(null, args);
            }
        }
    }

    public class WorkflowStateChangedEventArgs : EventArgs
    {
        public long ProjectId { get; set; }

        public List<AnalyticsWorkflowState> WorkflowState { get; set; }
    }
}