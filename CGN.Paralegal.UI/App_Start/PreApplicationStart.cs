namespace CGN.Paralegal.UI
{
    using System.Web.Optimization;

    public static class PreApplicationStart
    {
        //Any PC Web startup and initialization code goes here
        //This approach works as long as the parent application does not need to pass any data to the module at initialization
        public static void Start()
        {
            // Pre application startup code goes here
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}