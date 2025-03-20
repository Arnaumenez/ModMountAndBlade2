using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TacticalGenius.Core
{
    public static class Settings
    {
        public static bool DebugMode { get; set; } = false;
        public static float TacticalUpdateInterval { get; set; } = 5.0f;
        public static float MinimumTacticalChangeInterval { get; set; } = 10.0f;
        public static float MaxThreatDetectionRange { get; set; } = 200.0f;
        public static float MaxOpportunityDetectionRange { get; set; } = 150.0f;
        public static float TacticalSkillMultiplier { get; set; } = 1.0f;
        public static float AIAdaptabilityFactor { get; set; } = 1.0f;
        public static bool EnableAdvancedManeuvers { get; set; } = true;
        public static bool EnableCultureSpecificTactics { get; set; } = true;
        public static bool EnableUnexpectedManeuvers { get; set; } = true;
        
        public static class FormationConstants
        {
            // Constantes para ArrangementOrder
            public const int ArrangementLine = 0;
            public const int ArrangementColumn = 1;
            public const int ArrangementSquare = 2;
            public const int ArrangementWedge = 3;
            public const int ArrangementCircle = 4;
            public const int ArrangementScatter = 5;
            
            // Constantes para FormOrder
            public const int FormShieldWall = 0;
            public const int FormBraceForCharge = 1;
            public const int FormFireAtWill = 2;
            public const int FormHoldFire = 3;
        }
        
        // Añadir propiedades para adaptar los valores de BattlefieldState
        public static class BattlefieldStateProps
        {
            public const string OverallSituation = "OverallSituation";
            public const string TerrainAdvantage = "TerrainAdvantage";
            public const string CommanderTacticSkill = "CommanderTacticSkill";
            public const string Threats = "Threats";
            public const string Opportunities = "Opportunities";
            public const string FlankingOpportunities = "FlankingOpportunities";
        }
        
        // Tipos para TacticalOpportunity
        public static class OpportunityTypes
        {
            public const int VulnerableRanged = 0;
            public const int IsolatedFormation = 1;
            public const int AdvantageousPosition = 2;
            public const int DivideEnemy = 3;
            public const int CounterCharge = 4;
        }
        
        // Tipos para TacticalThreat
        public static class ThreatTypes
        {
            public const int CavalryCharge = 0;
            public const int RangedFire = 1;
            public const int InfantryOverwhelm = 2;
            public const int Encirclement = 3;
            public const int Ambush = 4;
        }
    }
}