namespace CGN.Paralegal.Infrastructure
{
    using System.Diagnostics;

    public static class DirtyHacksForConfigurationServices
    {
		static DirtyHacksForConfigurationServices()
		{
			RunByUnitTest = false;
			var runningProcessName = Process.GetCurrentProcess().ProcessName;

			if (runningProcessName.StartsWith("QTAgent"))
			{
				RunByUnitTest = true;
			}
		}

	    public static bool RunByUnitTest { get; private set; }
    }
}
