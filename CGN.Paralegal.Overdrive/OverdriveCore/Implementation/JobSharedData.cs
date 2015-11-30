using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LexisNexis.Evolution.Overdrive
{
    public class PipelineProperty
    {
        public string Name { get; set; }

        private object propertyValue;
        public object Value
        {
            get
            {
                if (propertyValue == null)
                {
                    return null;
                }

                AddRef();
                return propertyValue;
            }
            set
            {
                propertyValue = value;
            }
        }

        private int AddRef()
        {
            return ++usageCounter;
        }

        internal int Release()
        {
            usageCounter--;
            if (usageCounter <= 0)
            {
                propertyValue = null;
            }

            return usageCounter;
        }

        private int usageCounter;
    }
    class PipelinePropertyBag
    {
        private Dictionary<string, PipelineProperty> properties;

        internal PipelineProperty GetProperty(string propertyName)
        {
            if (properties == null)
            {
                properties = new Dictionary<string, PipelineProperty>(); // Property Name to Property object
            }

            PipelineProperty property;
            if (!properties.TryGetValue(propertyName, out property))
            {
                property = new PipelineProperty {Name = propertyName};
                properties.Add(propertyName, property);
            }
            return property;
        }

        internal int PropertyRelease(string propertyName)
        {
            if (properties == null)
            {
                return 0;
            }

            PipelineProperty property;
            if (!properties.TryGetValue(propertyName, out property))
            {
                return 0;
            }

            int refCount = property.Release();
            if (refCount <= 0)
            {
                properties.Remove(propertyName);
            }
            return refCount;
        }
    }
    internal class PipelinesSharedData
    {
        private Dictionary<string, PipelinePropertyBag> propertyBags;

        internal PipelineProperty GetProperty(string pipelineId, string propertyName)
        {
                PipelinePropertyBag pipelinePropertyBag = GetPropertyBag(pipelineId);
                PipelineProperty pipelineProperty = pipelinePropertyBag.GetProperty(propertyName);
                return pipelineProperty;
        }
        private PipelinePropertyBag GetPropertyBag(string pipelineId)
        {
            if (propertyBags == null)
            {
                propertyBags = new Dictionary<string, PipelinePropertyBag>(); // PipelineId to Job Property Bag
            }

            PipelinePropertyBag pipelinePropertyBag;
            if (!propertyBags.TryGetValue(pipelineId, out pipelinePropertyBag))
            {
                pipelinePropertyBag = new PipelinePropertyBag();
                propertyBags.Add(pipelineId, pipelinePropertyBag);
            }
            return pipelinePropertyBag;
        }

        internal int PropertyRelease(string pipelineId, string propertyName)
        {
            if (propertyBags == null)
            {
                return 0;
            }

            PipelinePropertyBag pipelinePropertyBag;
            if (!propertyBags.TryGetValue(pipelineId, out pipelinePropertyBag))
            {
                return 0;
            }

            int refCount = pipelinePropertyBag.PropertyRelease(propertyName);
            if (refCount <= 0)
            {
                propertyBags.Remove(pipelineId);
            }
            return refCount;
        }
    }
}
