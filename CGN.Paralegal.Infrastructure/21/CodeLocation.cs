using System;
using System.Runtime.CompilerServices;

namespace CGN.Paralegal.Infrastructure
{
    public struct CodeLocation
    {
        public CodeLocation([CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0,
            [CallerMemberName] string callerMemberName = "")
        {
            _callerFilePath = callerFilePath;
            _callerLineNumber = callerLineNumber;
            _callerMemberName = callerMemberName;
        }

        public override string ToString()
        {
            return String.Format("{0}, line: {1}, member: {2}", _callerFilePath, _callerLineNumber, _callerMemberName);
        }

        private readonly string _callerFilePath;
        private readonly int _callerLineNumber;
        private readonly string _callerMemberName;
    }
}
