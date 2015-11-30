#region File Header
//---------------------------------------------------------------------------------------------------
// <copyright file="Utils.cs" company="Cognizant">
//      Copyright (c) Cognizant Technology Solutions. All rights reserved.
// </copyright>
// <header>
//      <author>Kostya</author>
//      <description>
//          This file contains the Utils class
//      </description>
//      <changelog>
//          <date value="05-12-2013">Task # 134432-ADM 03 -Re Convresion</date>
//          <date value="05-21-2013">Bug # 142937,143536 and 143037 -ReConvers Buddy Defects</date>
//          <date value="06-06-2013">Bug # 143682-Fix to reprocess the partially converted document</date>
//          <date value="06-13-2013">Bug # 144594 and 143976 -Added fall back logic to get the error names and fix to show dcn in manage conversion for overlay job</date>
//          <date value="06-26-2013">Bug # 146526 -Disposing WebResponse object and error handling while pushing the document</date>
//          <date value="06-26-2013">Bug # 146858 -  Bulk Print job status is shown as "Failed"</date>
//          <date value="09/30/2013">Task # 152663 -ADM -ADMIN - 006 -  Reprocess Select All Implementation Part 2
//          <date value="11/06/2014">Task # 178804 -Billing Report enhancement</date>
//      </changelog>
// </header>
//---------------------------------------------------------------------------------------------------
#endregion
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Text;
using CGN.Paralegal.Infrastructure.ExceptionManagement;

namespace CGN.Paralegal.Infrastructure
{
    using System.Collections.Specialized;
    using System.Globalization;
    using System.Web;
    using System.Xml;
    using System.Xml.Linq;

    public static class Utils
    {
        public const string FaultContentType = "application/x-fault +xml; Version=1";

        public static long BinSizeOf(object objectToMeasure)
        {
            MemoryStream memoryStream = new MemoryStream();
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(memoryStream, objectToMeasure);
            long objectSize = memoryStream.Length;
            return objectSize;
        }

        public static long XmlSizeOf(object objectToMeasure)
        {
            MemoryStream memoryStream = new MemoryStream();
            XmlSerializer xmlSerializer = new XmlSerializer(typeof (object));
            xmlSerializer.Serialize(memoryStream, objectToMeasure);
            long objectSize = memoryStream.Length;
            return objectSize;
        }

        public static List<FileInfo> TraverseTree(string root, string searchPattern)
        {
            List<FileInfo> foundFiles = new List<FileInfo>();

            // Data structure to hold names of subfolders to be examined for files.
            Stack<string> dirs = new Stack<string>(20);

            if (!Directory.Exists(root))
            {
                throw new EVException().AddDbgMsg("Root directory {0} does not exist.", root);
            }

            dirs.Push(root);

            while (dirs.Count > 0)
            {
                string currentDir = dirs.Pop();
                string[] subDirs;
                try
                {
                    subDirs = Directory.GetDirectories(currentDir);
                }
                // An UnauthorizedAccessException exception will be thrown if we do not have
                // discovery permission on a folder or file. It may or may not be acceptable 
                // to ignore the exception and continue enumerating the remaining files and 
                // folders. It is also possible (but unlikely) that a DirectoryNotFound exception 
                // will be raised. This will happen if currentDir has been deleted by
                // another application or thread after our call to Directory.Exists. The 
                // choice of which exceptions to catch depends entirely on the specific task 
                // you are intending to perform and also on how much you know with certainty 
                // about the systems on which this code will run.
                catch (UnauthorizedAccessException ex)
                {
                    ex.Trace();
                    continue;
                }
                catch (DirectoryNotFoundException ex)
                {
                    ex.Trace();
                    continue;
                }

                string[] files = null;
                try
                {
                    files = Directory.GetFiles(currentDir, searchPattern);
                }

                catch (UnauthorizedAccessException ex)
                {
                    ex.Trace();
                    continue;
                }

                catch (DirectoryNotFoundException ex)
                {
                    ex.Trace();
                    continue;
                }
                // Perform the required action on each file here. Modify this block to perform your required task.
                foreach (string file in files)
                {
                    try
                    {
                        // Perform whatever action is required in your scenario.
                        FileInfo fi = new FileInfo(file);
                        //Tracer.Trace("{0}: {1}, {2}", fi.Name, fi.Length, fi.CreationTime);
                        foundFiles.Add(fi);
                    }
                    catch (FileNotFoundException ex)
                    {
                        // If file was deleted by a separate application or thread since the call to TraverseTree() then just continue.
                        ex.Trace();
                        continue;
                    }
                }

                // Push the subdirectories onto the stack for traversal. This could also be done before handing the files.
                foreach (string str in subDirs)
                    dirs.Push(str);
            }

            return foundFiles;
        }

        public static string RemoveSuffix(string s, string suffix)
        {
            if (s.EndsWith(suffix))
            {
                return s.Substring(0, s.Length - suffix.Length);
            }
            else
            {
                return s;
            }
        }

        public static string RemovePrefix(string s, string prefix)
        {
            if (s.StartsWith(prefix))
            {
                return s.Substring(prefix.Length);
            }
            else
            {
                return s;
            }
        }

        public static string Last(this string s, int suffixLength)
        {
            if (s == null)
            {
                return null;
            }

            if (s.Length <= suffixLength) 
            {
                return s;
                
            }

            return s.Substring(s.Length - suffixLength);
            }

        public static string Hint(this object o, int suffixLength)
        {
            if (o == null)
            {
                return "null";
        }

            return o.ToString().Last(suffixLength);
        }

        public static string CanonicalizePath(string path)
        {
            Uri pathUri = new Uri(path, UriKind.Absolute);
            return pathUri.LocalPath;
        }

        public static string FolderFromPath(string path)
        {
            return CanonicalizePath(Path.GetDirectoryName(path));
        }

        public static bool CanWriteToFolder(string folderPath)
        {
            try
            {
                WindowsIdentity myIdentity = WindowsIdentity.GetCurrent();
                Debug.Assert(myIdentity != null, "myIdentity != null");
                WindowsPrincipal myPrincipal = new WindowsPrincipal(myIdentity);

                // Attempt to get a list of security permissions from the folder. 
                // This will raise an exception if the path is read only or do not have access to view the permissions. 
                DirectorySecurity acl = Directory.GetAccessControl(folderPath);
                AuthorizationRuleCollection rules = acl.GetAccessRules(true, true, typeof(NTAccount));
                foreach (AuthorizationRule rule in rules)
                {
                    string ruleUserGroup = rule.IdentityReference.Value;
                    //Debug.WriteLine(ruleUserGroup);

                    // If we find one that matches the identity we are looking for
                    if (myPrincipal.IsInRole(ruleUserGroup))
                    {
                        FileSystemAccessRule fileSystemAccessRule = rule as FileSystemAccessRule;
                        if (null == fileSystemAccessRule)
                            return false;
                        if ((fileSystemAccessRule.FileSystemRights & FileSystemRights.Write) > 0)
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Trace().Swallow();
            }
            return false;
        }

        public static string CurrentMethod()
        {
            StackTrace stackTrace = new StackTrace();
            StackFrame stackFrame = stackTrace.GetFrame(1);
            if (null == stackFrame) return "Unknown 1";
            MethodBase methodBase = stackFrame.GetMethod();
            if (null == methodBase) return "Unknown 2";
            string methodName = methodBase.Name;
            if (null == methodName) return "Unknown 3";
            return methodName;
        }

        /// <summary>
        /// Calculates and returns MD5 hash for given content.
        /// </summary>
        public static string GetMD5Hash(byte[] content)
        {
            var md5 = new MD5CryptoServiceProvider();
            string hashedValue = String.Empty;

            //Computing hash for the file content
            byte[] byteArray = md5.ComputeHash(content);

            //Covtering hash value into Hexadecimal value

            return byteArray.Aggregate(hashedValue, (current, b) => current + b.ToString("X2"));
        }

        public static string SafeFormat(string format, params Object[] args)
        {
            string message;
            try
            {
                message = String.Format(format, args);
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append("INTERNAL ERROR IN LOGGING:");
                sb.AppendLine();
                sb.AppendFormat("Format string: {0}", format);
                sb.AppendLine();
                for (int argNum = 0; argNum < args.Count(); argNum++)
                {
                    sb.AppendFormat("Argument #{0} = {1}\r\n", argNum, args[argNum]);
                }
                sb.AppendFormat("String.Format thrown exception: {0}", ex);
                message = sb.ToString();
            }
            return message;
        }

        public static string SessionId
        {
            get
            {
                string sessionId = "UNDEFINED";
                if (HttpContext.Current != null && HttpContext.Current.Session != null)
                {
                    sessionId = HttpContext.Current.Session.SessionID;
                }
                return sessionId;
            }
        }

        /// <summary>
        /// Determines whether [has any error] [the specified all request params keys].
        /// </summary>
        /// <param name="allRequestParamsKeys">All request params keys.</param>
        /// <returns>
        ///   <c>true</c> if [has any error] [the specified all request params keys]; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasAnyError(string [] allRequestParamsKeys)
        {
            var isError = false;
            foreach (var allRequestParamsKey in allRequestParamsKeys)
            {
                 if(allRequestParamsKey==null) continue;
                 //the possible request params that RedactIt provides in case of conversion failure 
                 // error=
                 // error 0=
                isError = (allRequestParamsKey.ToLower().Contains("error") ||
                           allRequestParamsKey.ToLower().StartsWith("error"));
                if(isError) break;
           }
            return isError;
        }

        /// <summary>
        /// Gets the redact it error reason.
        /// </summary>
        /// <param name="parametersCollection">The parameters collection.</param>
        /// <returns></returns>
        public static string GetRedactItErrorReason(NameValueCollection parametersCollection)
        {
            string errorName = GetErrorParamName(parametersCollection);
            if (errorName == null)
            {
                return null;
            }

            string reason = parametersCollection[errorName];
            if (reason == null)
            {
                return null;
            }

            int posOfColon = reason.IndexOf(':');
            if (posOfColon != -1)
            {
                reason = reason.Substring(0, posOfColon);
            }
            return reason;
        }

        /// <summary>
        /// Gets the name of the error param.
        /// </summary>
        /// <param name="parametersCollection">The parameters collection.</param>
        /// <returns></returns>
        public static string GetErrorParamName(NameValueCollection parametersCollection)
        {
            string errorName = null;
            for (int errorNumber = 2; errorNumber >= 0; errorNumber--)
            {
                errorName = ConstructErrorName(parametersCollection, errorNumber);
                if (!String.IsNullOrEmpty(errorName)) break;
            }
            if (string.IsNullOrEmpty(errorName)) errorName = "error";
            return errorName;
        }

        /// <summary>
        /// Gets the redact it error message.
        /// </summary>
        /// <param name="parametersCollection">The parameters collection.</param>
        /// <returns></returns>
        public static string GetRedactItErrorMessage(NameValueCollection parametersCollection)
        {
            StringBuilder sb = new StringBuilder();
            for (int errorNum = 4; errorNum >= 0; errorNum--)
            {
                string errorName = ConstructErrorName(parametersCollection, errorNum);
                if (errorName == null)
                {
                    continue;
                }
                string errorValue = parametersCollection[errorName];
                if (errorValue == null)
                {
                    continue;
                }
                sb.AppendLine(errorValue);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Constructs the name of the error.
        /// </summary>
        /// <param name="parametersCollection">The parameters collection.</param>
        /// <param name="errorNum">The error num.</param>
        /// <returns></returns>
        private static string ConstructErrorName(NameValueCollection parametersCollection, int errorNum)
        {
            // ReSharper disable ConditionIsAlwaysTrueOrFalse
            // ReSharper is wrong here - it can be null in case of unit test
            if (parametersCollection.Keys == null)
            // ReSharper restore ConditionIsAlwaysTrueOrFalse
            {
                return null;
            }

            foreach (string paramName in parametersCollection.Keys)
            {
                if (null == paramName)
                {
                    continue;
                }
                if (paramName.StartsWith("error") && paramName.EndsWith(errorNum.ToString(CultureInfo.InvariantCulture)))
                {
                    return paramName;
                }
            }
            return null;
        }

        // This method is here only for debugging
        /// <summary>
        /// Traces the request params collection.
        /// </summary>
        /// <param name="parametersCollection">The parameters collection.</param>
        public static void TraceRequestParamsCollection(NameValueCollection parametersCollection)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("REQUEST START");
            bool errorFound = false;
            foreach (string paramName in parametersCollection.Keys)
            {
                if (null == paramName)
                {
                    continue;
                }
                sb.AppendFormat("    {0} = {1}\r\n", paramName, parametersCollection[paramName]);
                if (paramName.Contains("error"))
                    errorFound = true;
            }
            sb.AppendLine("REQUEST STOP");
            Tracer.Debug(sb.ToString());
            if (errorFound)
            {
                Tracer.Debug("Error found");
            }
        }

        /// <summary>
        /// Gets the non empty files.
        /// </summary>
        /// <param name="allFiles">All files.</param>
        /// <returns></returns>
        public static IEnumerable<string> GetNonEmptyFiles(IEnumerable<string> allFiles)
        {
            foreach (var filePath in allFiles)
            {
                if (String.IsNullOrEmpty(filePath)) continue;
                var fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists) continue;
                yield return filePath;

            }

        }

        public static byte[] HexToBytes(string hexString) 
        {
            if (String.IsNullOrEmpty(hexString))
            {
                return null;
            }

            if (hexString.Length % 2 == 1)
            {
                hexString = '0' + hexString;
            }

            byte[] bytes = new byte[hexString.Length / 2];

            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            }
            return bytes;
        }

        public static string BytesToHex(byte[] bytes)
        {
            if (bytes == null)
            {
                return null;
            }

            // BitConverter returns empty string if parameter array has zero size
            return BitConverter.ToString(bytes).Replace("-", "");
        }

        public static bool Equals(byte[] a, byte[] b)
        {
            if (a == null || b == null)
            {
                return a == b;
            }
            return a.SequenceEqual(b);
        }
        
        public static int GetHashCode(byte[] bytes)
        {
            if (bytes == null)
            {
                return 0;
            }

            int hashCode = 0;
            for (int i = 0; i < bytes.Length; ++i)
            {
                hashCode ^= (bytes[i] << ((0x03 & i) << 3));
            }
            return hashCode;
        }

        public static void RegisterProcessExitHandlerAndMakeItFirstInLineToExecute(EventHandler eventHandler)
        {
            // Here we subscribe to AppDomain.CurrentDomain.ProcessExit event in order to trace a message when process is exiting
            // The problem is that NLOG subscribed to that event before us and therefore is going to shutdown logging subsystem
            // before our handler is called and tries to trace its message.

            // Getting to private field AppDomain.CurrentDomain._processExit
            FieldInfo field = typeof(AppDomain).GetField("_processExit",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            if (field == null || !field.FieldType.IsSubclassOf(typeof(MulticastDelegate)))
            {
                // If the field is not simple MulticastDelegate, then just add our handler to the end of the list
                AppDomain.CurrentDomain.ProcessExit += eventHandler;
                return;
            }

            // Get the list of existing AppDomain.CurrentDomain.ProcessExit event subscribers
            MulticastDelegate multicastDelegate = field.GetValue(AppDomain.CurrentDomain) as MulticastDelegate;
            Debug.Assert(multicastDelegate != null, "multicastDelegate != null");
            Delegate[] subscribers = multicastDelegate.GetInvocationList();

            // Remove all subscriptions
            foreach (var subscriber in subscribers)
            {
                AppDomain.CurrentDomain.ProcessExit -= (EventHandler)subscriber;
            }

            Delegate[] newSubscriptions = new Delegate[subscribers.Length + 1]; // Create new subscriptions list
            newSubscriptions[0] = eventHandler; // Put our delegate first to the new subscriptions list
            Array.Copy(subscribers, 0, newSubscriptions, 1, subscribers.Length); // Move the rest of the old subscriptions list after ours
            Delegate combinedDelegate = Delegate.Combine(newSubscriptions); // Combine subscriptions list to Delegate
            field.SetValue(AppDomain.CurrentDomain, combinedDelegate); // Inject new delegate into event
        }

        public static string ToString(Object obj)
        {
            if (obj == null)
                return null;
            return obj.ToString();
        }

        public static IEnumerable<T> SafeConcat<T>(this IEnumerable<T> first, IEnumerable<T> second)
        {
            if (first == null && second == null) return null;

            if (first != null && second != null) return first.Concat(second);

            if (first != null) return first;

            return second;
        }

        public static string Serialize<T>(T obj)
        {
            var serializer = new XmlSerializer(typeof(T));

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.OmitXmlDeclaration = true;

            StringWriter textWriter = new StringWriter();
            using (XmlWriter xmlWriter = XmlWriter.Create(textWriter, settings))
            {
                serializer.Serialize(xmlWriter, obj);
                return textWriter.ToString();
            }
        }

        public static T Deserialize<T>(string xmlString)
        {
            var serializer = new XmlSerializer(typeof(T));

            using (TextReader reader = new StringReader(xmlString))
            {
                return (T)serializer.Deserialize(reader);
            }
        }

        public static object SmartXmlDeserializer(string xmlString)
        {
            XDocument doc = XDocument.Parse(xmlString);

            string simpleAssemblyName;
            string typeFullName;

            XElement objectTypeInfo = doc.Descendants("ObjectTypeInfo").FirstOrDefault();
            if (objectTypeInfo != null)
            {
                simpleAssemblyName = objectTypeInfo.Descendants("SimpleAssemblyName").First().Value;
                typeFullName = objectTypeInfo.Descendants("TypeFullName").First().Value;
            }
            else 
            {
                Tracer.Warning("ObjectTypeInfo not found. " + Environment.StackTrace);
                
                // Hack to let old unit tests to pass
                simpleAssemblyName = "CGN.Paralegal.BusinessEntities";
                int typeNameLength = xmlString.IndexOf(' ') - 1;
                typeFullName = "CGN.Paralegal.BusinessEntities." + xmlString.Substring(1, typeNameLength);
            }

            Assembly assembly = Assembly.Load(simpleAssemblyName);
            Type type = assembly.GetType(typeFullName);

            var serializer = new XmlSerializer(type);

            using (TextReader reader = new StringReader(xmlString))
            {
                return serializer.Deserialize(reader);
            }
        }

        public static void ShallowCopyFieldsFrom<T>(this T target, T source)
        {
            FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            foreach (FieldInfo fieldInfo in fields)
            {
                fieldInfo.SetValue(target, fieldInfo.GetValue(source));
            }
        }

        public static void ShallowCopyAllLevelFieldsFrom<T>(this T target, T source, Type type) where T: class
        {
            if (target == null) 
                throw new ArgumentNullException("target");
            if (source == null)
                throw new ArgumentNullException("source");
            if (type == null)
                throw new ArgumentNullException("type");

            Debug.Assert(type.IsAssignableFrom(typeof(T)));

            while (true)
            {
                Debug.Assert(type != null, "type != null");
                FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                foreach (FieldInfo fieldInfo in fields)
                {
                    fieldInfo.SetValue(target, fieldInfo.GetValue(source));
                }

                if (type.BaseType == typeof(object))
                {
                    return;
                }

                type = type.BaseType;
            }
        }

        /// <summary>
        /// Will perform a "Duck" copy of two objects, copying values of properties that occur in the source and destination, 
        /// where the properties are of the same type and have the same name.
        /// </summary>
        public static void DuckCopyTo(this object source, object destination)
        {

            PropertyInfo[] propertyInfos = source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var propertyInfo in propertyInfos)
            {
                PropertyInfo destinationPropertyInfo = destination.GetType().GetProperty(propertyInfo.Name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (destinationPropertyInfo != null)
                {
                    if (destinationPropertyInfo.CanWrite && propertyInfo.CanRead && (destinationPropertyInfo.PropertyType == propertyInfo.PropertyType))
                        destinationPropertyInfo.SetValue(destination, propertyInfo.GetValue(source, null), null);
                }
            }
        }

        /// <summary>
        /// Escapes the CSV text.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <returns></returns>
        public static string EscapeCsvText(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return s;
            }
            s = HttpUtility.HtmlDecode(s.Trim());

            if (s.IndexOfAny("\",\x0A\x0D".ToCharArray()) > -1)
            {
                s = s.Replace("\r\n", "  ");
                s = s.Replace("\n", "  ");
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            }
            return s;
        }
        /// <summary>
        /// To the long.
        /// </summary>
        /// <param name="docIdsString">The doc ids string.</param>
        /// <returns></returns>
        public static IEnumerable<long> ToLong(IEnumerable<string> docIdsString)
        {
            return from docId in docIdsString where docId != null select Convert.ToInt64(docId);
        }
        /// <summary>
        /// Gets the columnist from file.
        /// </summary>
        /// <param name="inputFilePath">The input file path.</param>
        /// <param name="idFieldName">Name of the id field.</param>
        /// <returns></returns>
        public static IEnumerable<string> GetColumnListFromFile(string inputFilePath, string idFieldName)
        {
            using (var readFile = new StreamReader(inputFilePath))
            {
                string line;
                int idFieldIndex = -1;
                while ((line = readFile.ReadLine()) != null)
                {
                    var row = line.Split(',');
                    if (idFieldIndex < 0)  //this is the header row.
                        idFieldIndex = GetFieldIndexFromFile(row, idFieldName);
                    else
                        yield return row[idFieldIndex];
                }
            }


        }

        /// <summary>
        /// Gets the field index from file.
        /// </summary>
        /// <param name="headerFields">The header fields.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>index</returns>
        private static int GetFieldIndexFromFile(string[] headerFields, string fieldName)
        {
            if (headerFields != null && headerFields.Length > 0)
            {
                //expecting the field has a name of fieldName
                for (int i = 0; i < headerFields.Length; i++)
                {
                    if (fieldName.ToUpper().Equals(headerFields[i].ToUpper()))
                        return i;
                }
            }

            //field not found
            return -1;
        }

        /// <summary>
        /// Utility function to Write the contents to the specified folder
        /// </summary>
        /// <param name="contentToWrite"> Source Content</param>
        /// <param name="filePath">Path where we can persist the Content</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public static void WriteToFile(string contentToWrite, string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            var fileStream = new FileStream(fileInfo.FullName, FileMode.Create, FileAccess.Write, FileShare.Read);

            var textWriter = new StreamWriter(fileStream);
            textWriter.Write(contentToWrite);

            textWriter.Close();
            fileStream.Close();
        }

        /// <summary>
        /// List to CSV 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <param name="headerAndColumns"></param>
        /// <returns></returns>
        public static string ListToCsv<T>(List<T> items, string[,] headerAndColumns)
        {
            var sw = new StringWriter();

            if (items != null && items.Any())
            {
                //building Header Row..
                sw.Write(BuildingHeaderItems(headerAndColumns));
                sw.Write(sw.NewLine);

                //building datarow
                sw.Write(BuildingDataRowItems(items, headerAndColumns));
            }

            return sw.ToString();
        }
        /// <summary>
        /// Building CSV file header rows
        /// </summary>
        /// <returns></returns>
        private static string BuildingHeaderItems(string[,] headerAndColumns)
        {
            int length = headerAndColumns.GetLength(0);
            var headerRows = new StringWriter();

            for (int i = 0; i < length; i++)
            {
                headerRows.Write(headerAndColumns[i, 0].Replace(',', ' '));
                if (i < length - 1)
                    headerRows.Write(",");
            }
            return headerRows.ToString();
        }

        /// <summary>
        /// Building CSV file data rows based on list of items
        /// </summary>
        /// <param name="items"></param>
        /// <param name="headerAndColumns"></param>
        /// <returns></returns>
        private static string BuildingDataRowItems<T>(IEnumerable<T> items, string[,] headerAndColumns)
        {
            int length = headerAndColumns.GetLength(0);
            var dataRows = new StringWriter();
            foreach (var item in items)
            {
                for (int i = 0; i < length; i++)
                {
                    var sbContent = new StringBuilder();
                    if (headerAndColumns[i, 1].Contains("~"))
                    {
                        var columnNames = headerAndColumns[i, 1].Split('~');
                        var message = Convert.ToBoolean(item.GetType().GetProperty(columnNames[0]).GetValue(item, null)) ? columnNames[0].Replace("Is", String.Empty) : columnNames[1];
                        sbContent.Append(message);
                        sbContent.Replace("</br>", " ").Replace(Environment.NewLine, " ");
                    }
                    else
                    {
                        sbContent.Append(Convert.ToString(item.GetType().GetProperty(headerAndColumns[i, 1]).GetValue(item, null)).Replace(',', ' '));
                        sbContent.Replace("</br>", " ").Replace(Environment.NewLine, " ");
                    }

                    // Bug 174945: Need to replace special characters for CSV line.
                    sbContent.Replace('\n', ' ');
                    sbContent.Replace('\r', ' ');
                    sbContent.Replace('"', '\'');

                    if (i < length - 1)
                        sbContent.Append(",");

                    dataRows.Write(sbContent.ToString());
                }
                dataRows.Write(dataRows.NewLine);
            }

            return dataRows.ToString();
        }
        public static readonly string NewLine = Environment.NewLine;

       
    }
    
    public static class Partitioner
    {
        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> collection, int batchSize)
        {
            List<T> nextbatch = new List<T>(batchSize);
            foreach (T item in collection)
            {
                nextbatch.Add(item);
                if (nextbatch.Count >= batchSize)
                {
                    yield return nextbatch;
                    nextbatch.Clear();
                }
            }
            if (nextbatch.Any())
            {
                yield return nextbatch;
            }
        }
    }

}
