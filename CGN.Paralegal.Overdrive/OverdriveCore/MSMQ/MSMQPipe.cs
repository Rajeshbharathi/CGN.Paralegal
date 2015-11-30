using System;
using System.Diagnostics;
using System.Messaging;
using System.Runtime.Serialization.Formatters;
using LexisNexis.Evolution.TraceServices;

namespace LexisNexis.Evolution.Overdrive.MSMQ
{
    internal class MSMQPipe : IPipe
    {
        static MSMQPipe()
        {
            //MessageQueue.EnableConnectionCache = true;
        }

        [ThreadStatic]
        private static IMessageFormatter _messageFormatter;
        internal static IMessageFormatter OverdriveMessageFormatter
        {
            get
            {
                if (null == _messageFormatter)
                {
                    BinaryMessageFormatter binaryMessageFormatter = new BinaryMessageFormatter
                                                                        {
                                                                            TopObjectFormat =
                                                                                FormatterAssemblyStyle.Simple,
                                                                            TypeFormat =
                                                                                FormatterTypeStyle.TypesWhenNeeded
                                                                        };

                    CompressFormatter compressFormatter = new CompressFormatter(binaryMessageFormatter);

                    _messageFormatter = compressFormatter;
                    //_messageFormatter = binaryMessageFormatter;
                }
                return _messageFormatter;
            }
        }

        internal MSMQPipe(PipeName pipeName)
        {
            this._pipeName = pipeName;
        }

        public PipeName Name
        {
            get
            {
                return _pipeName;
            }
        }

        private static bool trans = true;

        public void Create()
        {
            Create(trans);
        }

        private void Create(bool transactional)
        {
            Debug.Assert(_pipeName.IsLocal(), "Pipes can not be created on remote machines");
            string msmqPath = _pipeName.GetMSMQPath();

            if (MessageQueue.Exists(msmqPath))
            {
                Open();
                _messageQueue.Purge();
            }
            else
            {
                MessageQueue.Create(msmqPath, transactional); //MSMQPath should look like ".\\private$\\overdrive.Worker.DCBImport.1.Vault.152";
                Open();
            }

            _messageQueue.SetPermissions("Everyone", MessageQueueAccessRights.FullControl, AccessControlEntryType.Allow);
            _messageQueue.SetPermissions("ANONYMOUS LOGON", MessageQueueAccessRights.FullControl, AccessControlEntryType.Allow);
            _messageQueue.SetPermissions("Guest", MessageQueueAccessRights.FullControl, AccessControlEntryType.Allow);
        }

        public void Open()
        {
            if (null != _messageQueue)
            {
                return; // messageQueue is already created and the pipe is already opened
            }
            var msmqDirectFormatName = _pipeName.GetMSMQFormatName();
            _messageQueue = new MessageQueue(msmqDirectFormatName);
            _messageQueue.SetPropertyFilter();
            _messageQueue.Formatter = OverdriveMessageFormatter; // This does not seem to affect anything
        }

        public void Delete()
        {
            MessageQueue.Delete(_pipeName.GetMSMQPath());
        }
        public void Purge()
        {
            //Tracer.Trace("MSMQ Purge({0})", messageQueue.Path);
            _messageQueue.Purge();
        }

        public void Send(PipeMessageEnvelope pipeMessage)
        {
            //Tracer.Trace("MSMQ Send({0})", messageQueue.Path);
            var msg = pipeMessage.GetMSMQMessage();
            msg.Priority = MessagePriority.Normal;
            msg.Formatter = OverdriveMessageFormatter;

            if (trans)
            {
                //using (TransactionScope transaction = OverdriveTransactionScope.CreateInherited())
                //{
                //    messageQueue.Send(msg, MessageQueueTransactionType.Automatic);
                //    transaction.Complete();
                //}

                // How about we pardon Send from participating in any ambient transaction?
                // MessageQueueTransactionType.None causes Send to fail silently, so it has to be set to MessageQueueTransactionType.Single
                _messageQueue.Send(msg, MessageQueueTransactionType.Single);
            }
            else
            {
                _messageQueue.Send(msg);
            }
            pipeMessage.Id = msg.Id;
        }

        public PipeMessageEnvelope Receive()
        {
            //Tracer.Trace("MSMQ Receive({0})", messageQueue.Path);
            var msg = _messageQueue.Receive();
            if (null != msg)
            {
                //Tracer.Trace("MSMQ Receive({0} got message!)", messageQueue.Path);
                return msg.GetPipeMessage();
            }
            return null;
        }
        /*
                public PipeMessageEnvelope Receive(TimeSpan timeout)
                {
                    Message msg = null;
                    if (asyncResultBeginReceive == null)
                    {
                        asyncResultBeginReceive = messageQueue.BeginReceive();
                    }

                    bool received = asyncResultBeginReceive.AsyncWaitHandle.WaitOne(timeout);

                    if (received)
                    {
                        msg = messageQueue.EndReceive(asyncResultBeginReceive);
                        asyncResultBeginReceive = null;
                        return msg.GetPipeMessage();
                    }
                    return null;
                }
        */

        //[MethodImpl(MethodImplOptions.Synchronized)]
        [DebuggerNonUserCode] // Preventing those pesky MessageQueueExceptions from polluting debugger output window
        public PipeMessageEnvelope Receive(TimeSpan timeout)
        {
            //Tracer.Trace("MSMQ Receive({0})", messageQueue.Path);
            Message msg = null;
            try
            {
                _pipeName.ShouldNotBe(null);
                _pipeName.LongName.ShouldNotBe(null);
                _messageQueue.ShouldNotBe(null);
                msg = trans ? _messageQueue.Receive(timeout, MessageQueueTransactionType.Automatic) : _messageQueue.Receive(timeout);
            }
            catch (MessageQueueException ex)
            {
                if (ex.MessageQueueErrorCode == System.Messaging.MessageQueueErrorCode.IOTimeout)
                {
                    return null;
                }
                if (null != _pipeName && null != _pipeName.LongName && null != _pipeName.MachineName)
                {
                    ex.Data["PipeName"] = _pipeName.LongName;
                    ex.Data["MachineName"] = _pipeName.MachineName;
                    throw;
                }
                throw; // Some other exception we don't know how to handle
            }
            catch (Exception ex)
            {
                if (null != _pipeName && null != _pipeName.LongName && null != _pipeName.MachineName)
                {
                    ex.Data["LongName"] = _pipeName.LongName;
                    ex.Data["MachineName"] = _pipeName.MachineName;
                    throw;
                }
                throw; // Some other exception we don't know how to handle
            }

            if (null != msg)
            {
                //Tracer.Trace("MSMQ Receive({0} got message!)", messageQueue.Path);
                return msg.GetPipeMessage();
            }
            return null;
        }

        //public PipeMessageEnvelope Receive(TimeSpan timeout)
        //{
        //    Message msg = null;
        //    if (_asyncResultBeginPeek == null)
        //    {
        //        _asyncResultBeginPeek = _messageQueue.BeginPeek();
        //        //Tracer.Trace("BeginPeek, Time = " + DateTime.Now.Millisecond);
        //    }

        //    bool messageAvailable = _asyncResultBeginPeek.AsyncWaitHandle.WaitOne(timeout);
        //    //Tracer.Trace("messageAvailable = " + messageAvailable.ToString() + ", Time = " + DateTime.Now.Millisecond);
        //    if (!messageAvailable)
        //    {
        //        return null;
        //    }

        //    _messageQueue.EndPeek(_asyncResultBeginPeek);

        //    //msg.Formatter = OverdriveMessageFormatter;
        //    //Tracer.Trace("EndPeek, Body = " + msg.Body.ToString() + " Time = " + DateTime.Now.Millisecond);

        //    _asyncResultBeginPeek = null;

        //    try
        //    {
        //        if (trans)
        //        {
        //            msg = _messageQueue.Receive(_zeroWait, MessageQueueTransactionType.Automatic);
        //        }
        //        else
        //        {
        //            msg = _messageQueue.Receive(_zeroWait);
        //        }
        //    }
        //    catch (MessageQueueException ex)
        //    {
        //        if (ex.MessageQueueErrorCode == System.Messaging.MessageQueueErrorCode.IOTimeout)
        //        {
        //            // We get here if Peek detected message, but it was stolen before we were able to receive it. 
        //            // It is ok - we react as if there were no message within given timeout
        //            return null;
        //        }
        //        throw; // Some other exception we don't know how to handle
        //    }
        //    return null == msg ? null : msg.GetPipeMessage();
        //}
        //private IAsyncResult _asyncResultBeginPeek = null;
        //private readonly TimeSpan _zeroWait = new TimeSpan(0);

        #region "static"
        //enumerate pipes on the machine
        //public IEnumerable<PipeName> GetPipes(string machineName)
        //{
        //    MessageQueue[] queueList = MessageQueue.GetPrivateQueuesByMachine(machineName);

        //    List<PipeName> allPipes = new List<PipeName>();

        //    // Display the paths of the queues in the list.
        //    foreach (MessageQueue queueItem in queueList)
        //    {
        //        PipeName PipeName = PipeName.Factory(queueItem.MachineName, queueItem.QueueName);
        //        //Tracer.Trace(queueItem.Path);
        //        if (PipeName != null)
        //        {
        //            allPipes.Add(PipeName);
        //        }
        //    }

        //    return allPipes;
        //}
        #endregion

        #region IDisposable

        // Interesting fact: here we have public method inside internal class. We need to have it declarted as public, 
        // because it is interface implementation and therefore compiler requires it to be public, but
        // internal accessibility of the whole class makes this method also effectively internal.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool disposed = false; // to detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources.
                    //Tracer.Trace("MSMQ Dispose({0})", messageQueue.Path);
                    if (_messageQueue != null)
                    {
                        _messageQueue.Dispose();
                    }
                }
                // Dispose unmanaged resources.
            }
            disposed = true;
        }

        #endregion

        private readonly PipeName _pipeName;
        private MessageQueue _messageQueue;
        //private IAsyncResult asyncResultBeginReceive = null;
    }
}

