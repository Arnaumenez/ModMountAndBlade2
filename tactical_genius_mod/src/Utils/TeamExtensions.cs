using System;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using TaleWorlds.Core;

namespace TacticalGenius.Utils
{
    public static class TeamExtensions
    {
        // Obtiene el conteo inicial de tropas
        public static int InitialTroopCount(this Team team)
        {
            // Implementación adaptada a la API actual
            return team.ActiveAgents.Count;
        }
        
        // Obtiene el líder del equipo
        public static Agent TeamLeader(this Team team)
        {
            // Implementación adaptada a la API actual
            return team.Leader;
        }
    }
}