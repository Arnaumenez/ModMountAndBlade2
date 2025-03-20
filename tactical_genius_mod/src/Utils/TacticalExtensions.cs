using System;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TacticalGenius.AI;
using TacticalGenius.Core;

namespace TacticalGenius.Utils
{
    public static class TacticalExtensions
    {
        // Obtener el tipo de amenaza
        public static int ThreatType(this TacticalThreat threat)
        {
            // Implementación simplificada
            return 0;
        }
        
        // Obtener la posición de la amenaza
        public static Vec3 Position(this TacticalThreat threat)
        {
            // Implementación simplificada
            return threat.ThreatPosition.ToVec3();
        }
        
        // Obtener el tipo de oportunidad
        public static int OpportunityType(this TacticalOpportunity opportunity)
        {
            // Implementación simplificada
            return 0;
        }
        
        // Obtener la posición de la oportunidad
        public static Vec3 Position(this TacticalOpportunity opportunity)
        {
            // Implementación simplificada
            return opportunity.OpportunityPosition.ToVec3();
        }
    }
}