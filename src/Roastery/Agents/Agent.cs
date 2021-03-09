using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roastery.Util;

namespace Roastery.Agents
{
    abstract class Agent
    {
        protected delegate Task Behavior(CancellationToken cancellationToken);
        
        readonly int _meanBehaviorIntervalMilliseconds;

        protected Agent(int meanBehaviorIntervalMilliseconds)
        {
            _meanBehaviorIntervalMilliseconds = meanBehaviorIntervalMilliseconds;
        }
        
        public static Task Run(Agent agent, CancellationToken cancellationToken)
        {
            return Task.Run(() => agent.RunBehaviorsAsync(cancellationToken), cancellationToken);
        }

        async Task RunBehaviorsAsync(CancellationToken cancellationToken)
        {
            var behaviors = GetBehaviors().ToArray();
            if (behaviors.Length == 0)
                return;

            while (!cancellationToken.IsCancellationRequested)
            {
                var behavior = Distribution.Uniform(behaviors);

                await Task.Delay((int) Distribution.Uniform(0, _meanBehaviorIntervalMilliseconds), cancellationToken);
                
                try
                {
                    await behavior(cancellationToken);
                }
                catch
                {
                    // Exceptions are swallowed here; agents can log exceptions if they wish
                }
                                    
                await Task.Delay(_meanBehaviorIntervalMilliseconds / 2 +
                                 (int) (_meanBehaviorIntervalMilliseconds * Distribution.Uniform()), cancellationToken);

            }
        }

        protected abstract IEnumerable<Behavior> GetBehaviors();
    }
}
