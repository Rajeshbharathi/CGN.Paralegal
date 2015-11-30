using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Xml;

namespace LexisNexis.Evolution.Overdrive
{
    internal struct PipelineBuildOrderConfigurationItem
    {
        public string PipelineType;
        public List<RolePlanConfigurationItem> RolePlans;
    }

    internal struct RolePlanConfigurationItem
    {
        public RoleType RoleType;
        public string Name;
        public uint DesiredInstance;
        public List<string> OutputSectionsNames;
    }

    internal class PipelineBuildOrderConfigurationHandler : IConfigurationSectionHandler
    {
        public object Create(object parent, object configContext, XmlNode section)
        {
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
                                    if (i < rolePlans.Count - 2 && attributes["OutputSectionsNames"] == null)
                                    {
                                        // Default case - OutputSectionsNames completely omited
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
    }
}