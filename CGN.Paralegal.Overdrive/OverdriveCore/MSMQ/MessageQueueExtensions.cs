using System.Runtime.InteropServices;
namespace LexisNexis.Evolution.Overdrive.MSMQ
{   
    public static class MSMQExt
    {
        #region -- Constants –
        /// NULL VALUE
        public const byte VT_NULL = 1;
        /// UNSIGNED INTEGER
        public const byte VT_UI4 = 19;
        /// MQ_ADMIN_ACCESS -> 0x00000080
        public const int MQ_ADMIN_ACCESS = 128;
        /// MSMQ_CONNECTED -> L"CONNECTED"
        public const string MSMQ_CONNECTED = "CONNECTED";
        /// MSMQ_DISCONNECTED -> L"DISCONNECTED"
        public const string MSMQ_DISCONNECTED = "DISCONNECTED";
        /// MGMT_QUEUE_TYPE_PUBLIC -> L"PUBLIC"
        public const string MGMT_QUEUE_TYPE_PUBLIC = "PUBLIC";
        /// MGMT_QUEUE_TYPE_PRIVATE -> L"PRIVATE"
        public const string MGMT_QUEUE_TYPE_PRIVATE = "PRIVATE";
        /// MGMT_QUEUE_TYPE_MACHINE -> L"MACHINE"
        public const string MGMT_QUEUE_TYPE_MACHINE = "MACHINE";
        /// MGMT_QUEUE_TYPE_CONNECTOR -> L"CONNECTOR"
        public const string MGMT_QUEUE_TYPE_CONNECTOR = "CONNECTOR";
        /// MGMT_QUEUE_STATE_LOCAL -> L"LOCAL CONNECTION"
        public const string MGMT_QUEUE_STATE_LOCAL = "LOCAL CONNECTION";
        /// MGMT_QUEUE_STATE_NONACTIVE -> L"INACTIVE"
        public const string MGMT_QUEUE_STATE_NONACTIVE = "INACTIVE";
        /// MGMT_QUEUE_STATE_WAITING -> L"WAITING"
        public const string MGMT_QUEUE_STATE_WAITING = "WAITING";
        /// MGMT_QUEUE_STATE_NEED_VALIDATE -> L"NEED VALIDATION"
        public const string MGMT_QUEUE_STATE_NEED_VALIDATE = "NEED VALIDATION";
        /// MGMT_QUEUE_STATE_ONHOLD -> L"ONHOLD"
        public const string MGMT_QUEUE_STATE_ONHOLD = "ONHOLD";
        /// MGMT_QUEUE_STATE_CONNECTED -> L"CONNECTED"
        public const string MGMT_QUEUE_STATE_CONNECTED = "CONNECTED";
        /// MGMT_QUEUE_STATE_DISCONNECTING -> L"DISCONNECTING"
        public const string MGMT_QUEUE_STATE_DISCONNECTING = "DISCONNECTING";
        /// MGMT_QUEUE_STATE_DISCONNECTED -> L"DISCONNECTED"
        public const string MGMT_QUEUE_STATE_DISCONNECTED = "DISCONNECTED";
        /// MGMT_QUEUE_LOCAL_LOCATION -> L"LOCAL"
        public const string MGMT_QUEUE_LOCAL_LOCATION = "LOCAL";
        /// MGMT_QUEUE_REMOTE_LOCATION -> L"REMOTE"
        public const string MGMT_QUEUE_REMOTE_LOCATION = "REMOTE";
        /// MGMT_QUEUE_UNKNOWN_TYPE -> L"UNKNOWN"
        public const string MGMT_QUEUE_UNKNOWN_TYPE = "UNKNOWN";
        /// MGMT_QUEUE_CORRECT_TYPE -> L"YES"
        public const string MGMT_QUEUE_CORRECT_TYPE = "YES";
        /// MGMT_QUEUE_INCORRECT_TYPE -> L"NO"
        public const string MGMT_QUEUE_INCORRECT_TYPE = "NO";
        /// MO_MACHINE_TOKEN -> L"MACHINE"
        public const string MO_MACHINE_TOKEN = "MACHINE";
        /// MO_QUEUE_TOKEN -> L"QUEUE"
        public const string MO_QUEUE_TOKEN = "QUEUE";
        /// MACHINE_ACTION_CONNECT -> L"CONNECT"
        public const string MACHINE_ACTION_CONNECT = "CONNECT";
        /// MACHINE_ACTION_DISCONNECT -> L"DISCONNECT"
        public const string MACHINE_ACTION_DISCONNECT = "DISCONNECT";
        /// MACHINE_ACTION_TIDY -> L"TIDY"
        public const string MACHINE_ACTION_TIDY = "TIDY";
        /// QUEUE_ACTION_PAUSE -> L"PAUSE"
        public const string QUEUE_ACTION_PAUSE = "PAUSE";
        /// QUEUE_ACTION_RESUME -> L"RESUME"
        public const string QUEUE_ACTION_RESUME = "RESUME";
        /// QUEUE_ACTION_EOD_RESEND -> L"EOD_RESEND"
        public const string QUEUE_ACTION_EOD_RESEND = "EOD_RESEND";
        #endregion
        #region -- Enums --
        public enum MQMGMT_MACHINE_PROPERTIES
        {
            /// PROPID_MGMT_MSMQ_BASE -> 0
            PROPID_MGMT_MSMQ_BASE = 0,
            PROPID_MGMT_MSMQ_ACTIVEQUEUES,
            PROPID_MGMT_MSMQ_PRIVATEQ,
            PROPID_MGMT_MSMQ_DSSERVER,
            PROPID_MGMT_MSMQ_CONNECTED,
            PROPID_MGMT_MSMQ_TYPE,
        }
        public enum MQMGMT_QUEUE_PROPERTIES
        {
            /// PROPID_MGMT_QUEUE_BASE -> 0
            PROPID_MGMT_QUEUE_BASE = 0,
            PROPID_MGMT_QUEUE_PATHNAME,
            PROPID_MGMT_QUEUE_FORMATNAME,
            PROPID_MGMT_QUEUE_TYPE,
            PROPID_MGMT_QUEUE_LOCATION,
            PROPID_MGMT_QUEUE_XACT,
            PROPID_MGMT_QUEUE_FOREIGN,
            PROPID_MGMT_QUEUE_MESSAGE_COUNT,
            PROPID_MGMT_QUEUE_USED_QUOTA,
            PROPID_MGMT_QUEUE_JOURNAL_MESSAGE_COUNT,
            PROPID_MGMT_QUEUE_JOURNAL_USED_QUOTA,
            PROPID_MGMT_QUEUE_STATE,
            PROPID_MGMT_QUEUE_NEXTHOPS,
            PROPID_MGMT_QUEUE_EOD_LAST_ACK,
            PROPID_MGMT_QUEUE_EOD_LAST_ACK_TIME,
            PROPID_MGMT_QUEUE_EOD_LAST_ACK_COUNT,
            PROPID_MGMT_QUEUE_EOD_FIRST_NON_ACK,
            PROPID_MGMT_QUEUE_EOD_LAST_NON_ACK,
            PROPID_MGMT_QUEUE_EOD_NEXT_SEQ,
            PROPID_MGMT_QUEUE_EOD_NO_READ_COUNT,
            PROPID_MGMT_QUEUE_EOD_NO_ACK_COUNT,
            PROPID_MGMT_QUEUE_EOD_RESEND_TIME,
            PROPID_MGMT_QUEUE_EOD_RESEND_INTERVAL,
            PROPID_MGMT_QUEUE_EOD_RESEND_COUNT,
            PROPID_MGMT_QUEUE_EOD_SOURCE_INFO,
        }
        #endregion

#region -- Structures --
        [StructLayoutAttribute(LayoutKind.Explicit)]
        public struct Union
        {
            /// UCHAR->unsigned char
            [FieldOffsetAttribute(0)]
            public byte bVal;
            /// SHORT->short
            [FieldOffsetAttribute(0)]
            public short iVal;
            /// USHORT->unsigned short
            [FieldOffsetAttribute(0)]
            public ushort uiVal;
            /// VARIANT_BOOL->short
            [FieldOffsetAttribute(0)]
            public short boolVal;
            /// LONG->int
            [FieldOffsetAttribute(0)]
            public int lVal;
            /// ULONG->unsigned int
            [FieldOffsetAttribute(0)]
            public uint ulVal;
            /// SCODE->LONG->int
            [FieldOffsetAttribute(0)]
            public int scode;
            /// DATE->double
            [FieldOffsetAttribute(0)]
            public double date;
            /// CLSID*
            [FieldOffsetAttribute(0)]
            private System.IntPtr puuid;
            /// LPOLESTR->OLECHAR*
            [FieldOffsetAttribute(0)]
            private System.IntPtr bstrVal;
            /// LPSTR->CHAR*
            [FieldOffsetAttribute(0)]
            private System.IntPtr pszVal;
            /// LPWSTR->WCHAR*
            [FieldOffsetAttribute(0)]
            private System.IntPtr pwszVal;
        }
        [StructLayoutAttribute(LayoutKind.Sequential)]
        public struct MQPROPVARIANT
        {
            /// VARTYPE->unsigned short
            public ushort vt;
            /// WORD->unsigned short
            public ushort wReserved1;
            /// WORD->unsigned short
            public ushort wReserved2;
            /// WORD->unsigned short
            public ushort wReserved3;
            /// Anonymous_1506164d_aea5_43ce_9c68_e6f00748bae9
            public Union Union1;
        }
        
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1049:TypesThatOwnNativeResourcesShouldBeDisposable"), 
        StructLayoutAttribute(LayoutKind.Sequential)]
        public struct MQMGMTPROPS
        {
            /// DWORD->unsigned int
            public uint cProp;
            /// MGMTPROPID*
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
            internal System.IntPtr aPropID;
            /// MQPROPVARIANT*
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
            internal System.IntPtr aPropVar;
            /// HRESULT*
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
            internal System.IntPtr aStatus;
        }
        #endregion
  #region -- External Methods --
        /// Return Type: HRESULT->LONG->int
        ///pMachineName: LPCWSTR->WCHAR*
        ///pObjectName: LPCWSTR->WCHAR*
        ///pMgmtProps: MQMGMTPROPS*
        [DllImportAttribute("mqrt.dll", EntryPoint = "MQMgmtGetInfo")]
        internal static extern int MQMgmtGetInfo([InAttribute()] [MarshalAsAttribute(UnmanagedType.LPWStr)] string pMachineName, [InAttribute()] [MarshalAsAttribute(UnmanagedType.LPWStr)] string pObjectName, ref MQMGMTPROPS pMgmtProps);
        /// Return Type: HRESULT->LONG->int
        ///pMachineName: LPCWSTR->WCHAR*
        ///pObjectName: LPCWSTR->WCHAR*
        ///pAction: LPCWSTR->WCHAR*
        [DllImportAttribute("mqrt.dll", EntryPoint = "MQMgmtAction")]
        internal static extern int MQMgmtAction([InAttribute()] [MarshalAsAttribute(UnmanagedType.LPWStr)] string pMachineName, [InAttribute()] [MarshalAsAttribute(UnmanagedType.LPWStr)] string pObjectName, [InAttribute()] [MarshalAsAttribute(UnmanagedType.LPWStr)] string pAction);
        /// Return Type: HRESULT->LONG->int
        ///hQueue: HANDLE->void*
        [DllImportAttribute("mqrt.dll", EntryPoint = "MQPurgeQueue")]
        internal static extern int MQPurgeQueue(System.IntPtr hQueue);
        #endregion
    }
}
