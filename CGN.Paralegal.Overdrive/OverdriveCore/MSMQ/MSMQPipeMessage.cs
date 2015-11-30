using System;
using System.IO;
using System.Messaging;
using System.Runtime.Serialization.Formatters.Binary;
using LexisNexis.Evolution.Overdrive;

namespace LexisNexis.Evolution.Overdrive.MSMQ
{
    internal static class MSMQPipeMessageExtensions
    {
        [Flags]
        public enum MessageFlags
        {
            None = 0,
            Postback = 1
        }

        internal static Message GetMSMQMessage(this PipeMessageEnvelope pipeMessage)
        {
            Message MSMQMessage = new Message();

            if (pipeMessage.CorrelationId != null)
            {
                MSMQMessage.CorrelationId = pipeMessage.CorrelationId;
            }
            if (pipeMessage.Label != null)
            {
                MSMQMessage.Label = pipeMessage.Label;
            }
            MSMQMessage.Body = pipeMessage.Body;

            MessageFlags appSpecific = MessageFlags.None;
            if (pipeMessage.IsPostback)
            {
                appSpecific |= MessageFlags.Postback;
            }
            MSMQMessage.AppSpecific = (int)appSpecific;

            // Debugging
            //long messageSize = Utils.BinSizeOf(pipeMessage.Body);
            //if (messageSize > 100000)
            //{
            //    Tracer.Trace(
            //        "Message label = {0}, Message size = {1}", pipeMessage.Label, Utils.BinSizeOf(pipeMessage.Body));
            //}

            return MSMQMessage;
        }

        internal static PipeMessageEnvelope GetPipeMessage(this Message msmqMessage)
        {
            msmqMessage.Formatter = MSMQPipe.OverdriveMessageFormatter;
            var pipeMessage = new PipeMessageEnvelope();
            pipeMessage.Id = msmqMessage.Id;
            pipeMessage.CorrelationId = msmqMessage.CorrelationId;
            pipeMessage.Label = msmqMessage.Label;

            if (msmqMessage.BodyStream.Length > 0)
            {
                pipeMessage.Body = msmqMessage.Body;
            }

            CompressFormatter compressFormatter = msmqMessage.Formatter as CompressFormatter;
            if (null != compressFormatter)
            {
                pipeMessage.BodyLength = compressFormatter.OriginalBodyLength; // Thread local property
                pipeMessage.CompressedBodyLength = compressFormatter.CompressedBodyLength; // Thread local property
            }
            else
            {
                pipeMessage.CompressedBodyLength = 0; // This only works with compressing formatter
                try
                {
                    pipeMessage.BodyLength = msmqMessage.BodyStream.Length;
                }
                catch (Exception)
                {
                    pipeMessage.BodyLength = 0;
                }
            }

            pipeMessage.SentTime = msmqMessage.SentTime;

            MessageFlags appSpecific = (MessageFlags)msmqMessage.AppSpecific;
            if ((appSpecific & MessageFlags.Postback) != 0)
            {
                pipeMessage.IsPostback = true;
            }

            return pipeMessage;
        }

        internal static void SetPropertyFilter(this MessageQueue messageQueue)
        {
            // Specify which MSMQ Message properties to retrieve
            var messagePropertyFilter = new MessagePropertyFilter();
            messagePropertyFilter.ClearAll();
            messagePropertyFilter.Id = true;
            messagePropertyFilter.CorrelationId = true;
            messagePropertyFilter.Body = true;
            messagePropertyFilter.Label = true;
            messagePropertyFilter.SentTime = true;
            messagePropertyFilter.AppSpecific = true;
            messageQueue.MessageReadPropertyFilter = messagePropertyFilter;
        }
    }
}
