using System;
using System.Collections.Generic;
using System.Configuration;
using System.Xml;

namespace LexisNexis.Evolution.Overdrive
{
    internal struct JobTypePipelineMappingConfigurationItem
    {
        public int JobTypeId;
        public PipelineType PipelineType;
        public Type PipelineClassToInstantiate;
    }

    internal class JobTypePipelineMappingConfigurationHandler : IConfigurationSectionHandler
    {
        public object Create(object parent, object configContext, XmlNode section)
        {
            var jobTypePipelineMappingConfigurationList = new List<JobTypePipelineMappingConfigurationItem>();
            var jobTypePipelineMappings = section.SelectNodes("JobTypePipelineMapping");

            if (null == jobTypePipelineMappings)
            {
                throw new Exception("Bad Director configuration: cannot find JobTypePipelineMapping.");
            }

            foreach (XmlNode jobTypePipelineMapping in jobTypePipelineMappings)
            {
                var attributes = jobTypePipelineMapping.Attributes;
                if (null == attributes)
                {
                    throw new Exception("Bad Director configuration: JobTypePipelineMapping with no attributes.");
                }

                Type type = Type.GetType(attributes["PipelineClassToInstantiate"].InnerText);
                if (null == type)
                {
                    throw new Exception("Bad Director configuration: cannot find Type \"" + 
                        attributes["PipelineClassToInstantiate"].InnerText +
                    "\" specified in the PipelineMapping for JobTypeId " +
                    attributes["JobTypeId"].InnerText
                    );
                }

                var jobTypePipelineMappingConfigurationItem = new JobTypePipelineMappingConfigurationItem
                    {
                        JobTypeId = Convert.ToInt32(attributes["JobTypeId"].InnerText),
                        PipelineType = new PipelineType(attributes["PipelineType"].InnerText),
                        PipelineClassToInstantiate = type
                    };

                jobTypePipelineMappingConfigurationList.Add(jobTypePipelineMappingConfigurationItem);
            }

            return jobTypePipelineMappingConfigurationList;
        }
    }
}