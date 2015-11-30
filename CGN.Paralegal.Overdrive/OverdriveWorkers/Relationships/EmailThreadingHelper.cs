using System.Linq;
using System.Security.Cryptography;

namespace LexisNexis.Evolution.DocumentExtractionUtilities
{
    /// <summary>
    /// Helper class for email threading. Manages temporary records used for e-mail threading
    /// </summary>
    public static class EmailThreadingHelper
    {

        /// <summary>
        /// Encapsulates constants of EmailThreadingHelper
        /// </summary>
        public sealed class Constants
        {
            /// <summary>
            /// Private constructor to make sure this class is never instantiated
            /// </summary>
            private Constants() { }

            /// <summary>
            /// Stored Procedure Name for creating relationship record, which is used for calculating e-mail threads (EV_TMP_JOB_CreateRelationshipRecord)
            /// </summary>
            internal const string StoredProcedureCreateRelationshipRecord = "EV_TMP_JOB_CreateRelationshipRecord";

            /// <summary>
            /// Stored procedure name for deleting temporary e-mail threading records
            /// </summary>
            internal const string StoredProcedureDeleteTemporaryThreadingRecords = "EV_TMP_JOB_DeleteEmailThreadData";

            /// <summary>
            /// Column name for Job Run Id in Create relationship record stored procedure (@jobRunId)
            /// </summary>
            internal const string InputParameterJobRunIdInCreateRelationshipRecord = "@jobRunId";

            /// <summary>
            /// Column name for Parent Document Id in Create Relationship Record Stored Procedure (@parentDocumentId)
            /// </summary>
            internal const string InputParameterParentDocumentIdInCreateRelationshipRecord = "@parentDocumentId";

            /// <summary>
            /// Column name for Child Document Id in Create Relationship Record Stored Procedure (@childDocumentId)
            /// </summary>
            internal const string InputParameterChildDocumentIdInCreateRelationshipRecord = "@childDocumentId";

            /// <summary>
            /// Column name for Family Id in Create Relationship Record Stored Procedure (@familyId)
            /// </summary>
            internal const string InputParameterFamilyIdInCreateRelationshipRecord = "@familyId";

            /// <summary>
            /// Column name for Threading Constraint in Create Relationship Record Stored Procedure (@familyId)
            /// </summary>
            internal const string InputParameterThreadingConstraintInCreateRelationshipRecord = "@threadingConstraint";

            /// <summary>
            /// Input parameter name  - relationship type for Create Relationship Record stored procedure
            /// </summary>
            internal const string InputParameterRelationshipTypeInCreateRelationshipRecrod = "@relationshipType";

            /// <summary>
            /// Input parameter name - threading constraint for delete temporary records stored procedure
            /// </summary>
            internal const string InputParameterThreadingConstraintInDeleteTemporaryThreadingRecords = "@threadingConstraint";

            /// <summary>
            /// Stored Procedure Name for insert bulk  relationship record 
            /// </summary>
            internal const string StoredProcedureCreateBulkRelationshipRecord = "EV_TMP_JOB_BulkAddEmailThreading";

            /// <summary>
            /// Stored Procedure Create Bulk Email Thread Relationship Record
            /// </summary>
            internal const string StoredProcedureCreateBulkEmailThreadRelationshipRecord = "EV_TMP_JOB_BulkAddEmailThreadingRelation";
            

            /// <summary>
            /// Table type Column for bulk Insert
            /// </summary>
            internal const string InputParameterBulkRelationship = "@tblEmailThreading";
        }

        /// <summary>
        /// Encapsulates error codes of EmailThreadingHelper
        /// </summary>
        public sealed class ErrorCodes
        {
            /// <summary>
            /// Private constructor to make sure this class is never instantiated
            /// </summary>
            private ErrorCodes() { }

            /// <summary>
            /// Represents error to create relationship record used for calculating email threading
            /// </summary>
            internal const string CreateRelationshipRecordFailure = "CreateRelationshipRecordFailure";

            /// <summary>
            /// Represents error while getting temporary records for thread calculation
            /// </summary>
            internal const string GetTemporaryRecordsFailure = "GetTemporaryRecordsFailure";

            /// <summary>
            /// Represents error to  Bulk insert relationship 
            /// </summary>
            internal const string BulkCreateRelationshipRecordFailure = "Failed on bulk insert for create relationship";

            /// <summary>
            /// Represents error to  Bulk delete relations
            /// </summary>
            internal const string BulkDeleteRelationshipRecordFailure = "Failed on bulk delete  temporary relations";
        }

        /// <summary>
        /// Calculates and returns MD5 hash for given content.
        /// </summary>
        /// <param name="content">byte array for which MD5 hash had to be calculated</param>
        /// <returns>MD5 hash for given content.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public static string GetMD5Hash(byte[] content)
        {
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            string hashedValue = string.Empty;

            //Computing hash for the file content
            byte[] byteArray = md5.ComputeHash(content);

            //Converting hash value into Hexadecimal value

            return byteArray.Aggregate(hashedValue, (current, b) => current + b.ToString("X2"));
        }
    }
}
