using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace LexisNexis.Evolution.Overdrive
{
    [Serializable]
    public class JobsInfo
    {
        public JobsInfo()
        {
            _jobsInfo = new List<JobInfo>();
        }

        public JobsInfo(JobInfo[] jobsInfo)
        {
            _jobsInfo = jobsInfo.ToList();
        }

        public void StoreDebugData()
        {
            //XmlSerializer serializer = new XmlSerializer(typeof(JobsInfo));
            //TextWriter textWriter = new StreamWriter(@"JobsInfo.xml");
            //serializer.Serialize(textWriter, this);
            //textWriter.Close();

            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream("JobsInfo.bin", FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, this);
            stream.Close();
        }

        public static JobsInfo LoadDebugData()
        {
            //XmlSerializer deserializer = new XmlSerializer(typeof(JobsInfo));
            //TextReader textReader = new StreamReader(@"JobsInfo.xml");
            //JobsInfo ji = deserializer.Deserialize(textReader) as JobsInfo;
            //textReader.Close();

            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream("JobsInfo.bin", FileMode.Open, FileAccess.Read, FileShare.Read);
            JobsInfo ji = (JobsInfo)formatter.Deserialize(stream);
            stream.Close();

            return ji;
        }

        public List<JobInfo> _jobsInfo = new List<JobInfo>();
    }

}
