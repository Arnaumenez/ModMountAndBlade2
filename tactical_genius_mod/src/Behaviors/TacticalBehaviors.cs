using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Engine;
using TacticalGenius.AI;

namespace TacticalGenius.Behaviors
{
    /// <summary>
    /// Enumeración de tipos de maniobras tácticas
    /// </summary>
    public enum ManeuverType
    {
        // Maniobras defensivas
        SeekHighGround,
        FormDefensiveLine,
        PositionRangedBehindInfantry,
        HoldCavalryInReserve,
        FormShieldWall,
        FormSpearWall,
        
        // Maniobras equilibradas
        AdvanceInFormation,
        PositionRangedForEffectiveness,
        PrepareCavalryFlank,
        ExploitFlankingOpportunity,
        
        // Maniobras ofensivas
        FrontalAssault,
        AggressiveCavalryFlank,
        AdvanceRangedForDamage,
        EnvelopmentManeuver,
        CoordinatedStrike,
        
        // Maniobras específicas de cultura
        FormEmbolon,
        ShockInfantryCharge,
        HeavyCavalryCharge,
        HorseArcherHarassment,
        MultiDirectionalAmbush,
        ForestArcherTactics,
        
        // Maniobras inesperadas
        FeignedRetreat,
        SuddenFormationChange,
        HiddenReserveDeployment,
        DistractionManeuver,
        FalseFlankingMovement
    }
    
    /// <summary>
    /// Enumeración de tipos de respuestas a amenazas
    /// </summary>
    public enum ThreatResponseType
    {
        FormAntiCavalryDefense,
        FormDefensiveSquare,
        RaiseShields,
        ChargeCavalryAtRanged,
        AdvanceQuickly,
        FlankWithCavalry,
        ConcentrateRangedFire,
        FormDefensiveLine,
        FormDefensiveCircle,
        ReformAndRegroup
    }
    
    /// <summary>
    /// Enumeración de tipos de acciones de oportunidad
    /// </summary>
    public enum OpportunityActionType
    {
        StrikeVulnerableRanged,
        SurroundIsolatedFormation,
        OccupyAdvantageousPosition,
        ExploitEnemyDivision,
        ExecuteCounterCharge
    }
    
    /// <summary>
    /// Enumeración de posturas generales de combate
    /// </summary>
    public enum CombatPosture
    {
        Defensive,
        Balanced,
        Offensive,
        Aggressive,
        Cautious,
        Opportunistic,
        Adaptive
    }
    
    /// <summary>
    /// Enumeración de tipos de formación
    /// </summary>
    public enum FormationType
    {
        Line,
        Column,
        Square,
        Circle,
        Wedge,
        ShieldWall,
        SpearWall,
        Scatter
    }
    
    /// <summary>
    /// Enumeración de espaciado de formación
    /// </summary>
    public enum FormationSpacing
    {
        VeryTight,
        Tight,
        Normal,
        Loose,
        VeryLoose
    }
    
    /// <summary>
    /// Enumeración de posiciones relativas
    /// </summary>
    public enum RelativePosition
    {
        Center,
        Flank,
        AdvancedFlank,
        ProtectedFlank,
        FarFlank,
        BehindCenter,
        BehindFlank
    }
    
    /// <summary>
    /// Enumeración de lados de flanqueo
    /// </summary>
    public enum FlankSide
    {
        Left,
        Right,
        Rear
    }
    
    /// <summary>
    /// Clase que representa una maniobra táctica
    /// </summary>
    public class TacticalManeuver
    {
        public ManeuverType ManeuverType { get; set; }
        public List<Formation> TargetFormations { get; set; }
        public float Priority { get; set; }
        public Vec3 TargetPosition { get; set; }
        public string AdditionalData { get; set; }
        
        public TacticalManeuver()
        {
            TargetFormations = new List<Formation>();
            TargetPosition = Vec3.Zero;
        }
    }
    
    /// <summary>
    /// Clase que representa una amenaza en el campo de batalla
    /// </summary>
    public class BattlefieldThreat
    {
        public Formation SourceFormation { get; set; }
        public Vec3 Position { get; set; }
        public float Severity { get; set; }
        public ThreatType Type { get; set; }
        
        public enum ThreatType
        {
            CavalryCharge,
            RangedConcentration,
            InfantryAdvance,
            Flanking,
            Encirclement,
            HighGroundOccupation
        }
    }
    
    /// <summary>
    /// Clase que representa una respuesta a una amenaza
    /// </summary>
    public class ThreatResponse
    {
        public ThreatResponseType ResponseType { get; set; }
        public List<Formation> TargetFormations { get; set; }
        public float Priority { get; set; }
        public BattlefieldThreat Threat { get; set; }
        public Vec3 TargetPosition { get; set; }
        
        public ThreatResponse()
        {
            TargetFormations = new List<Formation>();
            TargetPosition = Vec3.Zero;
        }
    }
    
    /// <summary>
    /// Clase que representa una oportunidad en el campo de batalla
    /// </summary>
    public class BattlefieldOpportunity
    {
        public Vec3 Position { get; set; }
        public float Value { get; set; }
        public OpportunityType Type { get; set; }
        public Formation TargetFormation { get; set; }
        
        public enum OpportunityType
        {
            VulnerableRanged,
            IsolatedFormation,
            AdvantageousPosition,
            EnemyDivision,
            CounterChargeOpportunity
        }
    }
    
    /// <summary>
    /// Clase que representa una acción de oportunidad
    /// </summary>
    public class OpportunityAction
    {
        public OpportunityActionType ActionType { get; set; }
        public List<Formation> TargetFormations { get; set; }
        public float Priority { get; set; }
        public BattlefieldOpportunity Opportunity { get; set; }
        public Vec3 TargetPosition { get; set; }
        
        public OpportunityAction()
        {
            TargetFormations = new List<Formation>();
            TargetPosition = Vec3.Zero;
        }
    }
    
    /// <summary>
    /// Clase que representa un ajuste de formación
    /// </summary>
    public class FormationAdjustment
    {
        public FormationType FormationType { get; set; }
        public FormationSpacing Spacing { get; set; }
        public int Depth { get; set; }
        public RelativePosition RelativePosition { get; set; }
        
        public FormationAdjustment()
        {
            FormationType = FormationType.Line;
            Spacing = FormationSpacing.Normal;
            Depth = 4;
            RelativePosition = RelativePosition.Center;
        }
    }
    
    /// <summary>
    /// Clase que representa una prioridad de objetivo
    /// </summary>
    public class TargetPriority
    {
        public FormationClass TargetFormationClass { get; set; }
        public float Priority { get; set; }
    }
    
    /// <summary>
    /// Clase que representa un conjunto de decisiones tácticas
    /// </summary>
    public class TacticalDecisions
    {
        public CombatPosture GeneralPosture { get; set; }
        public List<TacticalManeuver> PrimaryManeuvers { get; set; }
        public List<ThreatResponse> ThreatResponses { get; set; }
        public List<OpportunityAction> OpportunityActions { get; set; }
        public Dictionary<FormationClass, FormationAdjustment> FormationAdjustments { get; set; }
        public Dictionary<FormationClass, List<TargetPriority>> TargetPriorities { get; set; }
        
        public TacticalDecisions()
        {
            PrimaryManeuvers = new List<TacticalManeuver>();
            ThreatResponses = new List<ThreatResponse>();
            OpportunityActions = new List<OpportunityAction>();
            FormationAdjustments = new Dictionary<FormationClass, FormationAdjustment>();
            TargetPriorities = new Dictionary<FormationClass, List<TargetPriority>>();
        }
    }
    
    /// <summary>
    /// Clase que contiene configuraciones del mod
    /// </summary>
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
    }
}
