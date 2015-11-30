using System;

namespace LexisNexis.Evolution.Worker.Data
{
     [Serializable]
    public class FieldRecord
    {
        /// <summary>
        /// This represents the name of the field
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// This represents the value to the field
        /// </summary>
        public string FieldValue { get; set; }
    }
}
