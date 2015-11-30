using System;
using System.IO;
using System.Xml;
using LexisNexis.Evolution.BusinessEntities;

namespace LexisNexis.Evolution.DocumentImportUtilities
{
    using System.Diagnostics.CodeAnalysis;

    public class EdrmXmlDeserializer
    {
        EDRMEntity edrmEntity;

        /// <summary>
        /// Gets or sets the edrm entity.
        /// </summary>
        /// <value>
        /// The edrm entity.
        /// </value>
        public EDRMEntity EdrmEntity
        {
            get { return edrmEntity; }
            set { edrmEntity = value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EdrmXmlDeserializer"/> class.
        /// </summary>
        public EdrmXmlDeserializer()
        {
            edrmEntity = new EDRMEntity { BatchEntity = new BatchEntity() };

        }

        /// <summary>
        /// Deserializes the EDRM XML.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public EDRMEntity DeserializeEdrmXml(string filePath)
        {
            // create an xml reader.
            XmlReader reader = new XmlTextReader(new FileStream(filePath, FileMode.Open));

            // read through elements 
            while (reader.Read())
            {
                // parse document elements.
                if (reader.Name.Equals("document", StringComparison.InvariantCultureIgnoreCase))
                {
                    edrmEntity.BatchEntity.DocumentEntity.Add(ParseEdrmDocumentXml(reader.ReadOuterXml()));
                }

                if (reader.Name.Equals("relationship", StringComparison.InvariantCultureIgnoreCase))
                {
                    edrmEntity.BatchEntity.Relationships.Add(new RelationshipEntity()
                    {
                        Type = reader.SafeGetAttribute("Type"),
                        ParentDocID = reader.SafeGetAttribute("ParentDocId"),
                        ChildDocID = reader.SafeGetAttribute("ChildDocId")
                    });
                }
            }

            return edrmEntity;

        }

        /// <summary>
        /// Parses the edrm document XML.
        /// </summary>
        /// <param name="sDocumentEntity">The s document entity.</param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public DocumentEntity ParseEdrmDocumentXml(string sDocumentEntity)
        {
            DocumentEntity documentEntity = new DocumentEntity();
            XmlReader xmlReader = XmlReader.Create(new StringReader(sDocumentEntity));

            while (xmlReader.Read())
            {
                // set these values only if they were not already set.
                if (string.IsNullOrEmpty(documentEntity.MIMEType)) documentEntity.MIMEType = xmlReader.SafeGetAttribute("MimeType");
                if (string.IsNullOrEmpty(documentEntity.DocumentID)) documentEntity.DocumentID = xmlReader.SafeGetAttribute("DocID");

                if (xmlReader.Name.Equals("tag", StringComparison.InvariantCultureIgnoreCase))
                {
                    #region Parse all Tag elements
                    documentEntity.Tags.Add(new TagEntity()
                    {
                        TagName = xmlReader.SafeGetAttribute("TagName"),
                        TagDataType = xmlReader.SafeGetAttribute("TagDataType"),
                        TagValue = xmlReader.SafeGetAttribute("TagValue")
                    });
                    #endregion Parse all Tag elements
                }
                else if (xmlReader.Name.Equals("file", StringComparison.InvariantCultureIgnoreCase))
                {
                    #region Parse File elements

                    FileEntity fileEntity = new FileEntity { FileType = xmlReader.SafeGetAttribute("FileType") };

                    XmlReader xmlReaderForExternalFile = XmlReader.Create(new StringReader(xmlReader.ReadOuterXml()));

                    while (xmlReaderForExternalFile.Read())
                    {
                        #region Parse external file elements
                        if (xmlReaderForExternalFile.Name.Equals("ExternalFile", StringComparison.InvariantCultureIgnoreCase))
                        {
                            fileEntity.ExternalFile.Add(new ExternalFileEntity()
                            {
                                FileName = xmlReaderForExternalFile.SafeGetAttribute("FileName"),
                                FilePath = xmlReaderForExternalFile.SafeGetAttribute("FilePath"),
                                FileSize = xmlReaderForExternalFile.SafeGetAttribute("FileSize"),
                                Hash = xmlReaderForExternalFile.SafeGetAttribute("Hash")
                            });
                        }
                        #endregion Parse external file elements
                    }
                    documentEntity.Files.Add(fileEntity);
                    #endregion Parse File elements
                }
            }

            return documentEntity;
        }
    }

    /// <summary>
    /// Encapsulate XML Reader functionality
    /// </summary>
    public static class ExtensionXmlReader
    {
        /// <summary>
        /// Safe get attribute.
        /// </summary>
        /// <param name="xmlReader">The XML reader.</param>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <returns></returns>
        public static string SafeGetAttribute(this XmlReader xmlReader, string attributeName)
        {
            try
            {
                string attributeValue = xmlReader.GetAttribute(attributeName);
                if (!string.IsNullOrWhiteSpace(attributeValue))
                {
                    return attributeValue;
                }
                return string.Empty;
            }
            catch { return string.Empty; } // safe function- don't crash.
        }
    }
}
