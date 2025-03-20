using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Engine;

namespace TacticalGenius.AI
{
    public class TacticalAnalyzer : MissionBehavior
    {
        private Mission _mission;
        private BattlefieldEvaluator _battlefieldEvaluator;
        private DecisionMaker _decisionMaker;
        private TacticalExecutor _tacticalExecutor;
        
        // Frecuencia de análisis táctico (en segundos)
        private const float ANALYSIS_INTERVAL = 3.0f;
        private float _lastAnalysisTime = 0f;
        
        // Factores de ponderación para diferentes aspectos tácticos
        private readonly Dictionary<string, float> _tacticalWeights = new Dictionary<string, float>
        {
            { "TerrainAdvantage", 0.8f },
            { "FormationCohesion", 0.7f },
            { "UnitTypeMatchup", 0.9f },
            { "NumericalAdvantage", 0.6f },
            { "FlankingOpportunity", 0.85f },
            { "MoraleState", 0.75f },
            { "CommanderTacticSkill", 0.8f },
            { "BattleMomentum", 0.7f }
        };
        
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public TacticalAnalyzer()
        {
            _battlefieldEvaluator = new BattlefieldEvaluator();
            _decisionMaker = new DecisionMaker();
            _tacticalExecutor = new TacticalExecutor();
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            
            if (_mission == null || _mission.Current == null)
                return;
                
            // Realizar análisis táctico a intervalos regulares
            if (_mission.CurrentTime - _lastAnalysisTime >= ANALYSIS_INTERVAL)
            {
                PerformTacticalAnalysis();
                _lastAnalysisTime = _mission.CurrentTime;
            }
        }

        public override void AfterStart()
        {
            base.AfterStart();
            _mission = Mission.Current;
            
            // Inicializar componentes con la misión actual
            _battlefieldEvaluator.Initialize(_mission);
            _decisionMaker.Initialize(_mission, _tacticalWeights);
            _tacticalExecutor.Initialize(_mission);
            
            InformationManager.DisplayMessage(new InformationMessage("TacticalGenius: Analizador táctico inicializado", Colors.Green));
        }
        
        private void PerformTacticalAnalysis()
        {
            // Solo analizar equipos controlados por IA
            foreach (Team team in _mission.Teams.Where(t => t.IsValid && !t.IsPlayerTeam && t.TeamAgents.Count > 0))
            {
                try
                {
                    // 1. Evaluar la situación táctica actual
                    var battlefieldState = _battlefieldEvaluator.EvaluateBattlefield(team);
                    
                    // 2. Tomar decisiones tácticas basadas en la evaluación
                    var tacticalDecisions = _decisionMaker.MakeDecisions(team, battlefieldState);
                    
                    // 3. Ejecutar las decisiones tácticas
                    _tacticalExecutor.ExecuteDecisions(team, tacticalDecisions);
                    
                    // Registro para depuración
                    if (Settings.DebugMode)
                    {
                        Debug.Print($"TacticalGenius: Análisis completado para equipo {team.TeamIndex}");
                        Debug.Print($"  - Estado del campo: {battlefieldState.OverallSituation}");
                        Debug.Print($"  - Decisiones principales: {string.Join(", ", tacticalDecisions.PrimaryManeuvers)}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.Print($"TacticalGenius: Error en análisis táctico: {ex.Message}");
                }
            }
        }
    }
}
