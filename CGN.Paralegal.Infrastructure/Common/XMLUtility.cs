//---------------------------------------------------------------------------------------------------
// <copyright file="XmlUtility.cs" company="Cognizant">
//      Copyright (c) Cognizant Technology Solutions. All rights reserved.
// </copyright>
// <header>
//      <author>Keerti Kotaru</author>
//      <description>
//          This file contains the XmlUtility class.
//      </description>
//      <changelog>
//          <date value=""></date>
//      </changelog>
// </header>
//---------------------------------------------------------------------------------------------------

using System.Linq;

namespace CGN.Paralegal.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;

    /// <summary>
    /// XML helper class to serialize or de-serialize objects
    /// </summary>
    [Serializable]
    public sealed class XmlUtility
    {
        private XmlUtility()
        {
        }
        /// <summary>
        /// serializes the given object to xml
        /// </summary>
        /// <typeparam name="T">Generic type</typeparam>
        /// <param name="obj">Object to be serialized</param>
        /// <returns>Serialized object in string</returns>
        public static string SerializeObject(object obj)
        {
            StringBuilder sb = new StringBuilder();
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add(string.Empty, string.Empty);
            XmlSerializer xs = new XmlSerializer(obj.GetType());
            using (XmlWriter xwriter = XmlWriter.Create(sb))
            {
                xs.Serialize(xwriter, obj, ns);
                xwriter.Flush();
                return sb.ToString();

            }
        }

        /// <summary>
        /// de-serializes the xml string to object
        /// </summary>        
        /// <param name="xml">xml string</param>
        /// <param name="type">Type</param>
        /// <returns>object</returns>
        public static object DeserializeObject(string xml, Type type)
        {
            if (xml != null && xml.Trim() != string.Empty)
            {
                StringReader sr = null;
                try
                {
                    XmlSerializer xs = new XmlSerializer(type);
                    sr = new StringReader(xml);
                    XmlReader xr = XmlReader.Create(sr);
                    return xs.Deserialize(xr);
                }
                finally
                {
                    if (sr != null)
                    {
                        sr.Dispose();
                    }
                }
            }
            return null;

        }

        /// <summary> 
        /// Converts a single XML tree to an object of type T 
        /// </summary> 
        /// <typeparam name="T">Type to return</typeparam> 
        /// <param name="xml">XML string to convert</param> 
        /// <returns> object of type T</returns> 
        public static T XmlToObject<T>(string xml)
        {
            using (var xmlStream = new StringReader(xml))
            {
                var serializer = new XmlSerializer(typeof(T));
                return (T)serializer.Deserialize(XmlReader.Create(xmlStream));
            }
        }

        /// <summary> 
        /// Converts the XML to a list of objects of type T
        /// </summary> 
        /// <typeparam name="T">Type to return</typeparam> 
        /// <param name="xml">XML string to convert</param> 
        /// <param name="nodePath">XML Node path to select <example>//thesaurus/word</example></param> 
        /// <returns></returns> 
        public static List<T> XmlToObjectList<T>(string xml, string nodePath)
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml);

            return (from XmlNode xmlNode in xmlDocument.SelectNodes(nodePath) select XmlToObject<T>(xmlNode.OuterXml)).ToList();
        }

    }
}
