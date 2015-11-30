using System;

namespace LexisNexis.Evolution.Worker.Data
{
    [Serializable]
    public class ReIndexRecord
    {
        public MatterReadRequest MatterDetails{ get; set; }

        //TODO:Search Engine Replacement - Null Search - Evaulate ReIndexRecord(DTO) and come up with DTO for reindexing
       

        public string Originator { get; set; }

    }
}
