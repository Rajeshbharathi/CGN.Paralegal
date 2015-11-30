using System.Collections;
using System.Collections.Generic;
using PH.DataTree;

// This is a "seed" for non-linear pipeline development

namespace LexisNexis.Evolution.Overdrive.Implementation
{
    public class PipelineSections : IEnumerable<PipelineSection>
    {
        private IEnumerator<PipelineSection> SectionsEnumerator
        {
            get {
                return ((IEnumerable<PipelineSection>) PipelineSectionsList).GetEnumerator();
            }
        }

        public IEnumerator<PipelineSection> GetEnumerator()
        {
            return SectionsEnumerator;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return SectionsEnumerator;
        }

        private List<PipelineSection> PipelineSectionsList { get; set; }

        //private DTreeNode<PipelineSection> root;
    }
}
