using System;

namespace LexisNexis.Evolution.Overdrive
{
    [Serializable]
    public sealed class PipeMessageEnvelope
    {
        public PipeMessageEnvelope()
        {
            IsPostback = false;
        }

        public string Id { get; internal set; }
        public string CorrelationId { get; internal set; }
        public string Label { get; set; }
        public object Body { get; set; }
        public long BodyLength { get; internal set; }
        public long CompressedBodyLength { get; internal set; }
        public DateTime SentTime { get; internal set; }
        public bool IsPostback { get; set; }
    }
}