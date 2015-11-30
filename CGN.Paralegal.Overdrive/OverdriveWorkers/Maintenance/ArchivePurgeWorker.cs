using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Overdrive;

namespace LexisNexis.Evolution.Worker
{
    using System.Diagnostics;
    using System.Configuration;
    using LexisNexis.Evolution.Infrastructure;

    public class ArchivePurgeWorker : WorkerBase
    {
        ArchivePurgeConfig config = null;

        protected override void BeginWork()
        {
            try
            {
                base.BeginWork();
                config = GetConfig((string)BootParameters);

            }
            catch (Exception ex)
            {
                Tracer.Error(ex.Message);
                throw;
            }
        }

        private ArchivePurgeConfig GetConfig(string configString)
        {
            //Creating a stringReader stream for the bootparameter
            var stream = new StringReader(configString);

            //Ceating xmlStream for xmlserialization
            var xmlStream = new XmlSerializer(typeof(ArchivePurgeConfig));

            //Deserialization of configString to get ArchivePurgeConfig
            return (ArchivePurgeConfig)xmlStream.Deserialize(stream);
        }
       
        protected override bool GenerateMessage()
        {

            try
            {

                string archiveDirRoot = config.ArchiveDirRoot;

                //1. Archive Brava logs
                // forfiles -p "C:\1\Cleanup" -s -m Brava*.* -d -1 -c "cmd /c ren @path %date:~10%-%date:~4,2%-%date:~7,2%_BravaXClient.txt"
                // cmd /c move C:\1\Cleanup\*BravaXClient.* \\LNGSEAEVDEVOB\TestArchive\BravaLogs\

                string sourceDirectory = config.BravaLogDir;                
                string archiveDirectory = archiveDirRoot + "BravaLogs";
                int daysToKeep = config.BravaLogDaysToKeep;
                
                string filenamePrefix = DateTime.Now.ToString("yyyy-MM-dd") + "_";

                ArchiveFiles(sourceDirectory, archiveDirectory, "Brava*.*", daysToKeep, filenamePrefix, null);
                //PurgeFiles(archiveDirectory, "*Brava*.*", archiveDaysToKeep); //Need tp purge?? what days to keep

                // Archive overdrive logs
                // forfiles -p "C:\1\Cleanup" -s -m OverdriveLog*.* -d -7 -c "cmd /c move @path \\LNGSEAEVDEVOB\TestArchive\OverdriveLogs
                sourceDirectory = config.OverdriveLogDir;
                daysToKeep = config.OverdriveLogDaysToKeep;                
                archiveDirectory = archiveDirRoot + "OverdriveLogs";

                ArchiveFiles(sourceDirectory, archiveDirectory, "OverdriveLog*.*", daysToKeep, null, null);
                //PurgeFiles(archiveDirectory, "OverdriveLog*.*", archiveDaysToKeep); //Need tp purge?? what days to keep

                //Archive redactIt queue
                // forfiles -p "C:\1\Cleanup" -s -m *.* -d -1 -c "cmd /c move @path \\LNGSEAEVDEVOB\TestArchive\Redact-ItQueue"

                sourceDirectory = config.RedactItQueueDir;
                daysToKeep = config.RedactItQueueDaysToKeep;              
                archiveDirectory = archiveDirRoot + "Redact-ItQueue";

                ArchiveFiles(sourceDirectory, archiveDirectory, "*.*", daysToKeep, null, null);
                //PurgeFiles(archiveDirectory, "*.*", archiveDaysToKeep); //Need tp purge?? what days to keep
            }
            catch (Exception ex)
            {
                Tracer.Error(ex.Message);
            }
            return true;
        }

        private void ArchiveFiles(string sourceDir, string destDir, string filePattern, int daysOld, string renamePrefix, string renamePostfix)
        {
            try
            {
                var txtFiles = Directory.EnumerateFiles(sourceDir, filePattern);
                // use this if need to recursive:
                // Directory.EnumerateFiles(@"c:\", "*.txt", SearchOption.AllDirectories)

                foreach (string currentFile in txtFiles)
                {
                    //file older than daysToKeep, move to archive dir
                    if (File.GetCreationTime(currentFile) < DateTime.Now.AddDays((-1) * daysOld))
                    {
                        string fileName = currentFile.Substring(sourceDir.Length + 1);
                        if (renamePrefix != null && renamePrefix.Trim().Length > 0)
                        {
                            fileName = renamePrefix + fileName;
                        }

                        if (renamePostfix != null && renamePostfix.Trim().Length > 0)
                        {
                            fileName = fileName + renamePrefix;
                        }

                        Directory.Move(currentFile, Path.Combine(destDir, fileName));
                    }
                }
            }
            catch (Exception ex)
            {
                //string message = ex.Message;
                Tracer.Error(ex.Message);
                //LogMessage(false, message);
            }
        }

        private void PurgeFiles(string fileDir, string filePattern, int daysOld)
        {
            try
            {
                var txtFiles = Directory.EnumerateFiles(fileDir, filePattern);

                foreach (string currentFile in txtFiles)
                {
                    //file older than daysToKeep, move to archive dir
                    if (File.GetCreationTime(currentFile) < DateTime.Now.AddDays((-1) * daysOld))
                    {
                        File.Delete(currentFile);
                    }
                }
            }
            catch (Exception ex)
            {
                //string message = Utils.ExceptionMessage(ex);
                Tracer.Error(ex.Message);
                //LogMessage(false, message);
            }

        }
    }
}
