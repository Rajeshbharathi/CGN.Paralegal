using System;
using System.Diagnostics;
using System.Linq;
using System.Messaging;
using System.Runtime.InteropServices;

namespace LexisNexis.Evolution.Overdrive.MSMQ
{
    internal static class MSMQPipeNameExtensions
    {
        internal static bool IsLocal(this PipeName pipeName)
        {
            return (String.IsNullOrEmpty(pipeName.MachineName) ||
                    pipeName.MachineName == "." ||
                    pipeName.MachineName.ToLower() == Environment.MachineName.ToLower())
                       ? true
                       : false;
        }

        internal static bool GetExists(this PipeName pipeName)
        {
            if (pipeName.IsLocal())
            {
                string msmqPath = pipeName.GetMSMQPath();
                return MessageQueue.Exists(msmqPath);
            }

            string myMSMQFormatName = pipeName.GetMSMQDirectName();
            MessageQueue[] queueList = MessageQueue.GetPrivateQueuesByMachine(pipeName.MachineName);
            return queueList.Select(messageQueue => messageQueue.FormatName).Any(curMSMQFormatName => String.Compare(myMSMQFormatName, curMSMQFormatName, true) == 0);
        }

        internal static string GetMSMQName(this PipeName pipeName)
        {
            var typeName = pipeName.GetType().Name;
            Debug.Assert(typeName.EndsWith(TypeSuffix),
                         "PipeName subclass " + typeName + " does not end with mandatory suffix " + TypeSuffix);
            // TODO: Remove "PipeName" type suffix and lowercase the string.
            typeName = typeName.Substring(0, typeName.Length - TypeSuffix.Length).ToLowerInvariant();
            var msmqName = Privacy + '\\' + Badge + Delimiter + typeName;
            msmqName += Delimiter + pipeName.LongName;
            return msmqName;
        }

        internal static string GetMSMQPath(this PipeName pipeName)
        {
            var machineNameEncoded = (String.IsNullOrEmpty(pipeName.MachineName))
                                            ? "."
                                            : pipeName.MachineName.ToLowerInvariant();
            return machineNameEncoded + '\\' + pipeName.GetMSMQName();
        }

        internal static string GetMSMQDirectName(this PipeName pipeName)
        {
            return DirectNamePrefix + pipeName.GetMSMQPath();
        }

        internal static string GetMSMQFormatName(this PipeName pipeName)
        {
            return FormatNamePrefix + pipeName.GetMSMQDirectName();
        }

        private const char Delimiter = '.';

        // Note: "FormatName:" part of MSMQ direct name seems to be case sensitive.
        private const string FormatNamePrefix = "FormatName:";

        // Note: "Direct=OS:" part of MSMQ direct name seems to be case sensitive.
        private const string DirectNamePrefix = "DIRECT=OS:";

        // Note: "private$" part of MSMQ direct name does not seem to be case sensitive
        private const string Privacy = @"private$";

        // Note: badge part of MSMQ direct name has to be lowercase.
        private const string Badge = "overdrive";

        private const string TypeSuffix = "PipeName";

        internal static uint Count(this PipeName pipeName)
        {
            MSMQExt.MQMGMTPROPS props = new MSMQExt.MQMGMTPROPS { cProp = 1 };
            try
            {
                props.aPropID = Marshal.AllocHGlobal(sizeof(int));
                Marshal.WriteInt32(props.aPropID, (int)MSMQExt.MQMGMT_QUEUE_PROPERTIES.PROPID_MGMT_QUEUE_MESSAGE_COUNT);

                // Marshal.SizeOf(typeof(MSMQExt.MQPROPVARIANT))
                props.aPropVar = Marshal.AllocHGlobal(24);
                Marshal.StructureToPtr(new MSMQExt.MQPROPVARIANT { vt = MSMQExt.VT_NULL }, props.aPropVar, false);

                props.aStatus = Marshal.AllocHGlobal(sizeof(int));
                Marshal.WriteInt32(props.aStatus, 0);

                string machineName = (pipeName.IsLocal()) ? null : pipeName.MachineName;
                string specialPath = "QUEUE=" + pipeName.GetMSMQDirectName();

                int result = MSMQExt.MQMgmtGetInfo(machineName, specialPath, ref props);
                if (result != 0 || Marshal.ReadInt32(props.aStatus) != 0)
                {
                    return 0;
                }

                MSMQExt.MQPROPVARIANT propVar = (MSMQExt.MQPROPVARIANT)Marshal.PtrToStructure(props.aPropVar, typeof(MSMQExt.MQPROPVARIANT));
                if (propVar.vt != MSMQExt.VT_UI4)
                {
                    return 0;
                }
                else
                {
                    return propVar.Union1.ulVal;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(props.aPropID);
                Marshal.FreeHGlobal(props.aPropVar);
                Marshal.FreeHGlobal(props.aStatus);
            }
        }
    }
}
