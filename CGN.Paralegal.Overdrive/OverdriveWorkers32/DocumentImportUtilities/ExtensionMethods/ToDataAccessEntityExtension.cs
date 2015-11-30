using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.DocumentImportUtilities;

namespace LexisNexis.Evolution.DocumentExtractionUtilities.ExtensionMethods
{
    using LexisNexis.Evolution.Infrastructure;

    public static class ToDataAccessEntityExtension
    {

        /// <summary>
        /// Toes the business entity.
        /// </summary>
        /// <param name="relationShipBeo">The relation ship beo.</param>
        /// <param name="jobRunId">The job run id.</param>
        /// <param name="threadingConstraint">The threading constraint.</param>
        /// <param name="relationshipType">Type of the relation ship.</param>
        /// <returns></returns>
        public static EmailThreadingEntity ToDataAccesEntity(this RelationshipBEO relationShipBeo, long jobRunId, string threadingConstraint, ThreadRelationshipEntity.RelationshipType relationshipType)
        {
            EmailThreadingEntity toReturn = new EmailThreadingEntity()
            {
                JobRunID = jobRunId,
                ChildDocumentID = relationShipBeo.ChildDocumentId,
                ParentDocumentID = relationShipBeo.ParentDocId,
                RelationshipType = relationshipType,
                ThreadingConstraint = threadingConstraint,
                FamilyID = relationShipBeo.FamilyDocumentId

            };
            // Debug 
            //Tracer.Warning("Subj: ChildDocId: {0}, ParentDocId: {1}", relationShipBeo.ChildDocumentId, toReturn.ParentDocumentID);
            return toReturn;
        }


    }
}