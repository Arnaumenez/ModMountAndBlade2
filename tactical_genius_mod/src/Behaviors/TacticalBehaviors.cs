using System;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Core;
using TacticalGenius.AI;

namespace TacticalGenius.Behaviors
{
    public class TacticalBehaviors : MissionBehavior
    {
        private readonly Mission _mission;
        private readonly TacticalBrain _tacticalBrain;
        
        public TacticalBehaviors(Mission mission, TacticalBrain tacticalBrain)
        {
            _mission = mission;
            _tacticalBrain = tacticalBrain;
        }
        
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;
    }
}