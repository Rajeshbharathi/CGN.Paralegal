//-----------------------------------------------------------------------------------------
// <copyright file=" DocumentImportHelper.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Manish</author>
//      <description>
//          Helper Class containing methods for Import
//      </description>
//      <changelog>
//          <date value="28-april-2011">created</date>
//          <date value="11-August-2011">changed a parameter in delete document method</date>
//          <date value="25-Oct-2011">Insert temporary relations issue for compund attachment</date>
//          <date value="28-Oct-2011">delete the temporary relations stored for calucating families</date>
//          <date value="01-05-2012">AvoidExcessiveComplexity </date>
//      </changelog>
// </header>
//-------------------------------------------------------------------------------------------

#region Namespace
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;


#endregion

namespace LexisNexis.Evolution.DocumentImportUtilities
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Common actions for document import into Concordance EV
    /// </summary>
    public sealed class DocumentImportHelper
    {
        /// <summary>
        /// Private constructor to enforce no instantiation of the class
        /// </summary>
        private DocumentImportHelper()
        {
        }

        /// <summary>
        /// Deserilizes XML data and returns Profile BEO so that it can be converted to Import Job BEO.
        /// </summary>
        /// <param name="bootParameters">Job's boot parameter</param>
        /// <returns>
        /// Profile BEO so that it can be converted to Import Job BEO.
        /// </returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public static ProfileBEO GetProfileBeo(string bootParameters)
        {
            try
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(ProfileBEO));
                return (ProfileBEO)xmlSerializer.Deserialize(new StringReader(bootParameters));
            }
            catch (Exception exception)
            {
                exception.AddResMsg(ErrorCodes.bootParameterSerializeException);
                throw;                
            }
        }

        #region Types used internal to Import jobs

        [XmlRoot("Document")]
        [Serializable]
        public struct FailedDocuments : IEquatable<FailedDocuments>
        {
            [XmlElement("Name")]
            private List<string> name;
            public List<string> FailedDocumentsName
            {
                get { return name ?? (name = new List<string>()); }
            }

            public override bool Equals(object obj)
            {
                return Equals((FailedDocuments)obj);
            }
            public bool Equals(FailedDocuments other)
            {
                return name == other.name;
            }

            public override int GetHashCode()
            {
                return name.GetHashCode();
            }

            public static bool operator ==(FailedDocuments failedDocuments1, FailedDocuments failedDocuments2)
            {
                return failedDocuments1.Equals(failedDocuments2);
            }

            public static bool operator !=(FailedDocuments failedDocuments1, FailedDocuments failedDocuments2)
            {
                return !failedDocuments1.Equals(failedDocuments2);
            }

        }
        #endregion
    }
}
