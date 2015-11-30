using System.Collections.Generic;
using System.Data;
using Microsoft.SqlServer.Server;
using LexisNexis.Evolution.Worker.Data;


namespace LexisNexis.Evolution.DocumentImportUtilities
{


    public class EmailThreadingCollectionEntity : List<EmailThreadingEntity>, IEnumerable<SqlDataRecord>
    {
        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns></returns>
        IEnumerator<SqlDataRecord> IEnumerable<SqlDataRecord>.GetEnumerator()
        {

            SqlDataRecord dataRecord = new SqlDataRecord(
                        new SqlMetaData("JobRunID", SqlDbType.BigInt),
                        new SqlMetaData("ParentDocumentID", SqlDbType.NVarChar, 64),
                        new SqlMetaData("ChildDocumentID", SqlDbType.NVarChar, 64),
                        new SqlMetaData("FamilyID", SqlDbType.NVarChar, 64),
                        new SqlMetaData("ThreadingConstraint", SqlDbType.NVarChar, 64),
                        new SqlMetaData("RelationshipType", SqlDbType.NVarChar, 256),
                        new SqlMetaData("OverlayCurrentThreadParentID", SqlDbType.NVarChar, 64)
                        );
            foreach (EmailThreadingEntity emailThreading in this)
            {
                dataRecord.SetInt64(0, emailThreading.JobRunID);
                dataRecord.SetString(1, emailThreading.ParentDocumentID ?? string.Empty);
                dataRecord.SetString(2, emailThreading.ChildDocumentID ?? string.Empty);
                dataRecord.SetString(3, emailThreading.FamilyID ?? string.Empty);
                dataRecord.SetString(4, emailThreading.ThreadingConstraint ?? string.Empty);
                dataRecord.SetString(5, emailThreading.RelationshipType.ToString());
                dataRecord.SetString(6, (!string.IsNullOrEmpty(emailThreading.OverlayCurrentThreadParentID) ? emailThreading.OverlayCurrentThreadParentID : string.Empty));
                yield return dataRecord;
            }

        }


    }

    /// <summary>
    /// EmailThreadingRelationEntity
    /// </summary>
    public class EmailThreadingRelationEntity : List<ConversationInfo>, IEnumerable<SqlDataRecord>
    {
        IEnumerator<SqlDataRecord> IEnumerable<SqlDataRecord>.GetEnumerator()
        {

            SqlDataRecord dataRecord = new SqlDataRecord(
                        new SqlMetaData("JobRunID", SqlDbType.BigInt),
                        new SqlMetaData("ParentDocumentID", SqlDbType.NVarChar, 64),
                        new SqlMetaData("ChildDocumentID", SqlDbType.NVarChar, 64),
                        new SqlMetaData("ConversationIndex", SqlDbType.NVarChar, 1000)
                        );
            foreach (ConversationInfo emailThreading in this)
            {
                dataRecord.SetInt64(0, emailThreading.JobRunId);
                dataRecord.SetString(1, emailThreading.ParentId ?? string.Empty);
                dataRecord.SetString(2, emailThreading.DocId ?? string.Empty);
                dataRecord.SetString(3, emailThreading.ConversationIndex ?? string.Empty);
                yield return dataRecord;
            }

        }
    }
}
