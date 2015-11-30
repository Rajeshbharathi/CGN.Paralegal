namespace CGN.Paralegal.UI.Utils
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.IO;
    using System.Linq.Dynamic;
    using System.Reflection;
    using System.Resources;
    using System.Text;
    using System.Web;
    using System.Web.Optimization;

    public class ResxTransform : IBundleTransform
    {
        public void Process(BundleContext context, BundleResponse response)
        {
            var cultureName = context.HttpContext.Request.QueryString["c"];
            if (string.IsNullOrWhiteSpace(cultureName)) cultureName = "en-US";
            var culture = CultureInfo.CreateSpecificCulture(cultureName);

            var content = new StringBuilder();
            foreach (var file in response.Files)
            {
                if (file.VirtualFile.Name.EndsWith(".resx", StringComparison.OrdinalIgnoreCase))
                {
                    content.Append(TransformResx(file.VirtualFile.Name, culture));
                }
                else
                {
                    content.Append(GetFileContent(file.VirtualFile.VirtualPath, context.HttpContext));
                }
            }

            response.Content = content.ToString();
        }

        private static string TransformResx(string fileName, CultureInfo culture)
        {
            fileName = fileName.Replace(".resx", string.Empty).Trim();
            var rm = new ResourceManager("CGN.Paralegal.UI.App.resources." + fileName, Assembly.GetExecutingAssembly());
            var resourceSet = rm.GetResourceSet(culture, true, true);
            const string Output = "var {0}Resources = {{ {1} }}; \r\n\r\n"; //note: double curly braces are escaped...
            var sb = new StringBuilder();
            foreach (DictionaryEntry entry in resourceSet)
            {
                sb.Append(entry.Key + ":\"" + entry.Value + "\",\r\n");
            }
            return string.Format(CultureInfo.InvariantCulture, Output, fileName, sb.Remove(sb.Length - 3, 3));
        }

        private static string GetFileContent(string filePath, HttpContextBase context)
        {
            string content = string.Empty;
            var physicalPath = context.Server.MapPath(filePath);
            if (File.Exists(physicalPath))
            {
                content = File.ReadAllText(physicalPath);
            }

            return content;
        }

        //private static string JsEncode(string text)
        //{
        //    text = text.Replace("'", "\\'");
        //    text = text.Replace("\\", "\\\\");
        //    return text;
        //}

    }
}
