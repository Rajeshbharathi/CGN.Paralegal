using LexisNexis.Evolution.Business;
using System;
using System.IO;

namespace LexisNexis.Evolution.Worker
{
    public static class LawVolumeHelper
    {
        public static string GetJobImageFolder(long jobId, long caseId)
        {
            string imageArchiveDirectory;
            #region "debug"
            /*
            var lawCase = LawBO.GetLawCaseDetails(caseId);
            if (lawCase != null)
            {
                if (!string.IsNullOrEmpty(lawCase.ImageArchiveDirectory))
                {
                    imageArchiveDirectory = lawCase.ImageArchiveDirectory;
                }
                else  
                {
                    //It will be replaced by API
                    imageArchiveDirectory = Path.Combine(lawCase.CaseDirectory, Constants.LawImageArchiveFolderName);
                    Directory.CreateDirectory(imageArchiveDirectory);
                }
            }
            */
            #endregion

            var lawEvAdapter = LawBO.GetLawAdapter(caseId);
            lawEvAdapter.CreateImageArchiveFolder();
            imageArchiveDirectory = lawEvAdapter.GetImageArchiveDirectory();

            //1) Create EvImages Folder
            var evImagesFolderPath = Path.Combine(imageArchiveDirectory, Constants.LawEVImagesFolderName);
            if (!Directory.Exists(evImagesFolderPath))
            {
                Directory.CreateDirectory(evImagesFolderPath);

            }

            //2) Create Job Folder
            var jobImageFolder = Path.Combine(evImagesFolderPath, string.Format("{0}{1}", "Job", jobId));
            if (!Directory.Exists(jobImageFolder))
            {
                Directory.CreateDirectory(jobImageFolder);
            }
            return jobImageFolder;
        }

        public static string CreateVolumeFolder(string jobImageFolder, int volume)
        {
            var volumeFolderName = String.Format("{0:D4}", (volume));
            string volumePath = Path.Combine(jobImageFolder, volumeFolderName);
            Directory.CreateDirectory(volumePath);
            return volumePath;
        }

    }
}
