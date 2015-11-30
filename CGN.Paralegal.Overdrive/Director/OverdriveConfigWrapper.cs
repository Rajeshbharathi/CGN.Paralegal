using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace LexisNexis.Evolution.Overdrive
{
    internal static class OverdriveConfigWrapper
    {
        public enum OverdriveJobType
        {
            Custom,
            System
        }

        public enum ThrottleValue
        {
            Off = 0,
            Low = 50,
            Medium = 200,
            High = 500
        }

        internal struct AppSetting
        {
            public string KeyName;
            public string Value;
        }

        #region PublicMethods
     
        public static void CustomJobServiceStart(string configPath, ThrottleValue throttleValue = ThrottleValue.Medium)
        {
            UpdateOverdriveConfig(OverdriveJobType.Custom, configPath, (int)throttleValue);
        }

        public static void SystemJobServiceStart(string configPath)
        {
            UpdateOverdriveConfig(OverdriveJobType.Custom, configPath, 1);
        }

        public static void CustomJobServiceStop(string configPath)
        {
            UpdateOverdriveConfig(OverdriveJobType.Custom, configPath, 0);
        }

        public static void SystemJobServiceStop(string configPath)
        {
            UpdateOverdriveConfig(OverdriveJobType.System, configPath, 0);
        }

        
        #endregion

        #region Private Methods
        private static List<string> GetJobPipeLineTypes(OverdriveJobType overdriveJobType)
        {
            if (overdriveJobType == OverdriveJobType.System)
            {
               return  new List<string>
                                                     {
                                                         "DeleteDataSet",
                                                         "SearchAlerts",
                                                         "GlobalReplace",
                                                         "Deduplication",
                                                         "UpdateServerStatus",
                                                         "EmailDocuments",
                                                         "PrintDocuments",
                                                         "DownloadDocuments",
                                                         "FindAndReplaceRedactionXml",
                                                         "RefreshReports",
                                                         "ReviewerBulkTag",
                                                         "UpdateReviewSet",
                                                         "MergeReviewSet",
                                                         "SplitReviewSet",
                                                         "PrivilegeLog",
                                                         "DCBOpticonExports",
                                                         "SaveSearchResults",
                                                         "CompareSaveSearchResults",
                                                         "DeleteDocumentField",
                                                         "BulkPrint",
                                                         "ConvertDCBLinksToCaseMap",
                                                         "DeleteTag",
                                                         "SendDocumentLinksToCaseMap",
                                                         "BulkDocumentDelete",
                                                         "FullDocumentStaticClustering",
                                                         "SlowWorkersTest",
                                                         "FaultToleranceTest",
                                                         "ScaleOutTest",
                                                         "WorkCompletionTest",
                                                         "LogFilesMaintenance"
                                                     };
            }
            return  new List<string>
                        {
                            "ImportDcb",
                            "ImportEdocs",
                            "ImportLoadFileAppend",
                            "ImportLoadFileOverlay",
                            "Production",
                            "ExportLoadFile",
                            "ExportDcb",
                            "Reviewset",
                            "MatterBackupRestore",
                            "ImportDcb"
                        };
        }

        private static void UpdateOverdriveConfig(OverdriveJobType overdriveJobType, string configPath, int maxInstance)
        {
            List<string> lstJobPipeLineTypes = GetJobPipeLineTypes(overdriveJobType);
            XmlDocument overdriveConfigdocument = new XmlDocument();
            overdriveConfigdocument.Load(configPath);
            List<PipelineBuildOrderConfigurationItem> pipelineBuildOrderConfig = GetPipelineBuildOrder(overdriveConfigdocument) as List<PipelineBuildOrderConfigurationItem>;

            if (pipelineBuildOrderConfig != null)
            {
                List<PipelineBuildOrderConfigurationItem> lstCustomJobs =
                    pipelineBuildOrderConfig.Where(o => lstJobPipeLineTypes.Contains(o.PipelineType)).ToList();
                List<string> lstCutomWorkers = new List<string>();

                foreach (PipelineBuildOrderConfigurationItem job in lstCustomJobs)
                {
                    foreach (RolePlanConfigurationItem role in job.RolePlans)
                    {
                        string keyName = role.Name + "MaxInstances";
                        UpdateKey(overdriveConfigdocument, keyName, maxInstance.ToString(CultureInfo.CurrentCulture));
                    }
                }
                overdriveConfigdocument.Save(configPath);
            }
        }
        
        public static object GetPipelineBuildOrder(XmlDocument xmlDoc)
        {
            XmlNode section = xmlDoc.SelectSingleNode("configuration/PipelineBuildOrders");
            var items = new List<PipelineBuildOrderConfigurationItem>();
            var pipelineBuildOrders = section.SelectNodes("PipelineBuildOrder");
            if (null != pipelineBuildOrders)
            {
                foreach (XmlNode pipelineBuildOrder in pipelineBuildOrders)
                {
                    if (pipelineBuildOrder.Attributes["PipelineType"] == null) throw new ArgumentNullException("section", "PipelineType attribute is missing.");
                    var pipelineBuildOrderConfigurationItem = new PipelineBuildOrderConfigurationItem
                    {
                        PipelineType = pipelineBuildOrder.Attributes["PipelineType"].InnerText
                    };
                    if (pipelineBuildOrder.HasChildNodes)
                    {
                        var rolePlans = pipelineBuildOrder.ChildNodes[0].SelectNodes("RolePlan");
                        if (rolePlans == null) throw new ArgumentNullException("section", "RolePlan is missing in configuration.");
                        if (rolePlans.Count > 0)
                        {
                            var rolePlanItemList = new List<RolePlanConfigurationItem>();
                            var previousItem = new RolePlanConfigurationItem();
                            for (var i = rolePlans.Count - 1; i >= 0; i--)
                            {
                                var attributes = rolePlans[i].Attributes;
                                if (attributes == null) throw new ArgumentNullException("section", "Missing attributes for RolePlan.");
                                var item = new RolePlanConfigurationItem
                                {
                                    DesiredInstance = Convert.ToUInt32(attributes["DesiredInstance"].InnerText),
                                    RoleType = new RoleType(attributes["RoleType"].InnerText),
                                    Name = attributes["RoleType"].InnerText,
                                    OutputSectionsNames = new List<string>()
                                };
                                if (attributes["Name"] != null)
                                {
                                    item.Name = attributes["Name"].InnerText;
                                }
                                if (attributes["OutputSectionsNames"] != null)
                                {
                                    var sectionNames = attributes["OutputSectionsNames"].InnerText.Split(',').ToList();
                                    foreach (var sectionName in sectionNames)
                                    {
                                        if (!string.IsNullOrEmpty(sectionName.Trim()))
                                            item.OutputSectionsNames.Add(sectionName);
                                    }
                                }
                                else
                                {
                                    if (i < rolePlans.Count - 2)
                                    {
                                        item.OutputSectionsNames.Add(previousItem.Name);
                                    }
                                }
                                rolePlanItemList.Add(item);
                                previousItem = item;
                            }
                            rolePlanItemList.Reverse();
                            pipelineBuildOrderConfigurationItem.RolePlans = rolePlanItemList;
                        }
                    }
                    items.Add(pipelineBuildOrderConfigurationItem);
                }
            }
            return items;
        }
        
        // Updates a key within the Config
        private static void UpdateKey(XmlDocument xmlDoc, string strKey, string newValue)
        {
            XmlNode appSettingsNode =
               xmlDoc.SelectSingleNode("configuration/appSettings");
            // Attempt to locate the requested setting.
            foreach (XmlNode childNode in appSettingsNode)
            {
                if (childNode.Attributes != null)
                {
                    if (childNode.Attributes["key"].Value == strKey)
                        childNode.Attributes["value"].Value = newValue;
                }
            }
        }
        #endregion
    }
}
