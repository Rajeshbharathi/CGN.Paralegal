using LexisNexis.Evolution.BusinessEntities;

namespace LexisNexis.Evolution.DocumentExtractionUtilities.ExtensionMethods
{
    public static class ToBusinessEntityExtension
    {
        public static RelationshipBEO ToBusinessEntity(this ThreadRelationshipEntity threadRelationshipObject)
        {
            RelationshipBEO toReturn = new RelationshipBEO()
            {
                ChildDocumentId = threadRelationshipObject.ChildDocumentId,                                
                FamilyDocumentId = threadRelationshipObject.FamilyId,                                            
                ParentDocId = threadRelationshipObject.ParentDocumentId,
                Type = threadRelationshipObject.ThreadRelationshipType.ToString(),
                
            };
            return toReturn;
        }
    }
}
