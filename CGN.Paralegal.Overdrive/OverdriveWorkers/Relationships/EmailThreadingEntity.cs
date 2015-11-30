
namespace LexisNexis.Evolution.DocumentImportUtilities
{
    


    #region Namespaces

    using LexisNexis.Evolution.BusinessEntities;

    #endregion

    public class EmailThreadingEntity
    {
        public long JobRunID { get; set; }
        public string ParentDocumentID { get; set; }
        public string ChildDocumentID { get; set; }
        public string FamilyID { get; set; }
        public string ThreadingConstraint { get; set; }
        public ThreadRelationshipEntity.RelationshipType RelationshipType { get; set; }
        public string OverlayCurrentThreadParentID { get; set; }
        public string ConversationIndex { get; set; }
    }
}
