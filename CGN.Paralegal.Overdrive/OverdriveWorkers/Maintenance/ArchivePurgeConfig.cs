using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LexisNexis.Evolution.Worker
{
    public class ArchivePurgeConfig
    {
        public ArchivePurgeConfig() { }

        public string ArchiveDirRoot;

        public string BravaLogDir;
        public int BravaLogDaysToKeep;

        public string OverdriveLogDir;
        public int OverdriveLogDaysToKeep;

        public string RedactItQueueDir;
        public int RedactItQueueDaysToKeep;

        /* code to serialize config to xml
        public static int Main(string[] args)
        {
            ArchivePurgeConfig config = new ArchivePurgeConfig();

            config.ArchiveDirRoot = @"\\LNGSEAEVDEVOB\TestArchive\";

            config.BravaLogDir = @"C:\1\Cleanup";
            config.BravaLogDaysToKeep = 7;

            config.OverdriveLogDir = @"C:\1\Cleanup";
            config.OverdriveLogDaysToKeep = 7;

            config.RedactItQueueDir = @"C:\1\Cleanup";
            config.RedactItQueueDaysToKeep = 7;

            System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(config.GetType());
            System.IO.StreamWriter swriter = new System.IO.StreamWriter(@"c:\temp\ArchivePurgeConfig.xml", true, Encoding.UTF8);

            x.Serialize(swriter, config);
            swriter.Close();

            return 0;
        }*/

    }
}
