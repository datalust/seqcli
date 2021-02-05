using System.Collections.Generic;

namespace Roastery.Agents
{
    class ArchivingBatch : Agent
    {
        public ArchivingBatch() 
            : base(60000)
        {
        }

        protected override IEnumerable<Behavior> GetBehaviors()
        {
            // Not implemented yet...
            yield break;
        }
    }
}