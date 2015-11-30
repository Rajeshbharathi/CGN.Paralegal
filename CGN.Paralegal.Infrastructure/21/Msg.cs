using System;
using System.Reflection;
using System.Resources;
using System.Text;

namespace CGN.Paralegal.Infrastructure.ExceptionManagement
{
    public class Msg
    {
        public static string FromRes(string resID, params Object[] args)
        {
            if (null == resID)
            {
                throw new ArgumentNullException("resID");
            }

            ResourceManager resMgr = new ResourceManager("CGN.Paralegal.Infrastructure.Exception", Assembly.GetExecutingAssembly());
            string strErrorMessageFromResources = resMgr.GetString(resID);
            
            if (null == strErrorMessageFromResources)
            {
                var sb = new StringBuilder();
                sb.Append("Detected a bug in how application code uses logging facility: ");
                sb.AppendFormat("Resource dictionary does not have a string for Resource ID = \"{0}\"", resID);
                for (int argNum = 0; argNum < args.Length; argNum++)
                {
                    sb.AppendFormat("Argument #{0} = {1}\r\n", argNum, args[argNum]);
                }
                sb.AppendLine(Environment.StackTrace);
                string message = sb.ToString();
                Tracer.Error(message);
                return "Error Code " + resID; // Better than nothing
            }

            string expandedMessage = Utils.SafeFormat(strErrorMessageFromResources, args);
            return expandedMessage;
        }
    }
}
