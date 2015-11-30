using System;
using System.Diagnostics;

namespace LexisNexis.Evolution.Overdrive
{
    [Serializable]
    public class WorkerStatistics
    {
        public WorkerStatistics(WorkAssignment workAssignment, string machineName, Pipe reportPipe)
        {
            WorkerBadge = workAssignment.WorkerBadge;
            WorkerBorn = DateTime.Now;
            HeartBeatInterval = new TimeSpan(0, 0, 1);
            PreviousHeartBeatSentTime = WorkerBorn;
            CurrentHeartBeatSentTime = WorkerBorn;
            if (reportPipe == null) throw new ArgumentException("reportPipe is null");
            ReportPipe = reportPipe;
        }

        public WorkerBadge WorkerBadge { get; private set; }
        public DateTime WorkerBorn { get; private set; }

        [NonSerialized]
        private readonly Stopwatch _workTimeNet = new Stopwatch();
        
        public void PunchInNet ()
        {
            _workTimeNet.Start();
            _betweenPunchInNetAndPunchOutNet = true;
        }

        public void PunchOutNet ()
        {
            _workTimeNet.Stop();
            ProcessedMessages++;
            _dirty = true;
            _betweenPunchInNetAndPunchOutNet = false;
        }

        [NonSerialized]
        private bool _betweenPunchInNetAndPunchOutNet = false;

        public void PauseNetTime()
        {
            if (!_betweenPunchInNetAndPunchOutNet) return;

            _workTimeNet.Stop();
        }

        public void ResumeNetTime()
        {
            if (!_betweenPunchInNetAndPunchOutNet) return;

            _workTimeNet.Start();
        }


        [NonSerialized]
        private readonly Stopwatch _workTimeGross = new Stopwatch();

        public void PunchInGross()
        {
            _workTimeGross.Start();
        }

        public void PunchOutGross()
        {
            _workTimeGross.Stop();
        }

        [NonSerialized]
        private TimeSpan _idleTime = new TimeSpan(0);
        public void RecordIdleTime(TimeSpan newIdleTime)
        {
            _idleTime += newIdleTime;
        }

        public void IncreaseProcessedDocumentsCount(int addedDocumentsCount)
        {
            ProcessedDocuments += (uint)addedDocumentsCount;
        }

        /// <summary>
        /// Sets the total document count.
        /// </summary>
        /// <param name="totalDocumentcount">The total documentcount.</param>
        public void SetTotalDocumentCount(long totalDocumentcount)
        {
            TotalDocuments = totalDocumentcount;
        }
        public uint ProcessedMessages { get; private set; }
        public uint ProcessedDocuments { get; private set; }

        public long TotalDocuments { get; private set; }
        public TimeSpan HeartBeatInterval { get; private set; }
        public DateTime PreviousHeartBeatSentTime { get; private set; }
        public DateTime CurrentHeartBeatSentTime { get; private set; }
        
        [NonSerialized]
        private Pipe _reportPipe;
        private Pipe ReportPipe
        {
            get { return _reportPipe; }
            set { _reportPipe = value; }
        }

        public TimeSpan WorkTimeNet { get; private set; }
        public TimeSpan WorkTimeGross { get; private set; }

        public double PersonalSpeedNet { get { return ProcessedDocuments / WorkTimeNet.TotalSeconds; } }
        public double PersonalSpeedGross { get { return ProcessedDocuments / WorkTimeGross.TotalSeconds; } }

        private long _inputTraffic = 0;
        private long _compressedInputTraffic = 0;
        public void RecordInputTraffic(PipeMessageEnvelope envelope)
        {
            _inputTraffic += envelope.BodyLength;
            _compressedInputTraffic += envelope.CompressedBodyLength;
        }
        public long InputTraffic
        {
            get
            {
                return _inputTraffic;
            }
        }

        public long CompressedInputTraffic
        {
            get
            {
                return _compressedInputTraffic;
            }
        }

        public void Send()
        {
            if (!_dirty)
            {
                return; // Nothing done since the last time we sent the stats, so no need to send anything
            }

            CurrentHeartBeatSentTime = DateTime.Now;
            if (CurrentHeartBeatSentTime < PreviousHeartBeatSentTime + HeartBeatInterval)
            {
                return; // Too early to send heartbeat
            }

            ForceSend();
        }

        public void ForceSend()
        {
            WorkTimeNet = _workTimeNet.Elapsed;
            WorkTimeGross = _workTimeGross.Elapsed - _idleTime; 

            var envelope = new PipeMessageEnvelope()
            {
                Body = this,
                Label = MessageLabel,
            };
            ReportPipe.Send(envelope);
            PreviousHeartBeatSentTime = DateTime.Now;
            _dirty = false;
        }

        public static readonly string MessageLabel = "HeartBeat";

        [NonSerialized] private bool _dirty = false;
    }
}
