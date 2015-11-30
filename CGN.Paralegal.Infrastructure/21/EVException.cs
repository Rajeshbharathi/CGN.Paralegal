using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace CGN.Paralegal.Infrastructure.ExceptionManagement
{
    [Serializable]
    public class EVException : Exception
    {
        public EVException([CallerFilePath] string callerFilePath = "",
                           [CallerLineNumber] int callerLineNumber = 0, 
                           [CallerMemberName] string callerMemberName = "")
        {
            CodeLocation codeLocation = new CodeLocation(callerFilePath, callerLineNumber, callerMemberName);
            this.AddNamedProperty("EVExceptionConstructionLocation", codeLocation.ToString());
        }

        // Constructor to enable binary deserialization of EVException
        protected EVException(SerializationInfo info, StreamingContext ctx)
            : base(info, ctx)
        {
            
        }

        //[Obsolete]
        /// <summary>
        /// DEPRECATED! DO NOT USE!
        /// </summary>
        public override string Message
        {
            get
            {
                if (reentranceGuard)
                {
                    return "";
                }
                reentranceGuard = true;

                try
                {
                    return this.ToUserString();
                }
                finally
                {
                    reentranceGuard = false;
                }
            }
        }
        private static bool reentranceGuard = false;
    }
}
