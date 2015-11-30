using System.Web.Script.Serialization;


namespace LexisNexis.Evolution.BatchJobs.DcbOpticonExports
{
    public static class DcbOpticonUtil
    {
        public static T JsDeserialize<T>(string jsonString)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            T serializedObject = serializer.Deserialize<T>(jsonString);

            return serializedObject;
        }
    }



}
