using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Engine;
using TacticalGenius.Behaviors;
using TacticalGenius.Utils;

namespace TacticalGenius.AI
{
    public class TacticalBrain
    {
        private Mission _mission;
        private TacticalAnalyzer _analyzer;
        private BattlefieldEvaluator _evaluator;
        private DecisionMaker _decisionMaker;
        private TacticalExecutor _executor;
        
        // Diccionario para almacenar el nivel de táctica de cada equipo
        private Dictionary<Team, float> _teamTacticalSkill = new Dictionary<Team, float>();
        
        // Diccionario para almacenar el historial de decisiones de cada equipo
        private Dictionary<Team, List<TacticalDecisions>> _decisionHistory = new Dictionary<Team, List<TacticalDecisions>>();
        
        // Tiempo de la última actualización táctica para cada equipo
        private Dictionary<Team, float> _lastUpdateTime = new Dictionary<Team, float>();
        
        public void Initialize(Mission mission)
        {
            _mission = mission;
            
            // Inicializar componentes
            _analyzer = new TacticalAnalyzer();
            _analyzer.Initialize(mission);
            
            _evaluator = new BattlefieldEvaluator();
            _evaluator.Initialize(mission);
            
            _decisionMaker = new DecisionMaker();
            _decisionMaker.Initialize(mission);
            
            _executor = new TacticalExecutor();
            _executor.Initialize(mission);
            
            // Registrar para eventos de misión
            mission.OnMissionTick += OnMissionTick;
            
            // Inicializar niveles tácticos para cada equipo
            foreach (Team team in mission.Teams)
            {
                if (team.IsValid && !team.IsPlayerTeam)
                {
                    InitializeTeamTacticalSkill(team);
                }
            }
        }
        
        private void InitializeTeamTacticalSkill(Team team)
        {
            if (team == null || !team.IsValid)
                return;
                
            // Calcular nivel táctico basado en el líder del equipo
            float tacticalSkill = 50f; // Valor base
            
            // Si hay un líder, usar su habilidad táctica
            if (team.TeamLeader != null)
            {
                // En el juego real, esto accedería a la habilidad táctica del líder
                // Aquí simulamos con un valor aleatorio
                Random random = new Random();
                tacticalSkill = 30f + (float)random.NextDouble() * 70f; // 30-100
            }
            
            // Aplicar multiplicador de configuración
            tacticalSkill *= Settings.TacticalSkillMultiplier;
            
            // Guardar nivel táctico
            _teamTacticalSkill[team] = tacticalSkill;
            
            // Inicializar historial de decisiones
            _decisionHistory[team] = new List<TacticalDecisions>();
            
            // Inicializar tiempo de última actualización
            _lastUpdateTime[team] = 0f;
            
            if (Settings.DebugMode)
            {
                Debug.Print($"TacticalGenius: Equipo {team.TeamIndex} inicializado con nivel táctico {tacticalSkill:F1}");
            }
        }
        
        private void OnMissionTick(float dt)
        {
            // Actualizar cada equipo según el intervalo configurado
            foreach (Team team in _mission.Teams)
            {
                if (team.IsValid && !team.IsPlayerTeam && team.HasBots)
                {
                    UpdateTeamTactics(team);
                }
            }
        }
        
        private void UpdateTeamTactics(Team team)
        {
            // Verificar si es tiempo de actualizar las tácticas
            float currentTime = _mission.CurrentTime;
            
            if (!_lastUpdateTime.ContainsKey(team))
            {
                _lastUpdateTime[team] = currentTime;
            }
            
            float timeSinceLastUpdate = currentTime - _lastUpdateTime[team];
            
            if (timeSinceLastUpdate < Settings.TacticalUpdateInterval)
                return;
                
            // Actualizar tiempo de última actualización
            _lastUpdateTime[team] = currentTime;
            
            try
            {
                // Analizar el campo de batalla
                BattlefieldState battlefieldState = _analyzer.AnalyzeBattlefield(team);
                
                // Evaluar la situación
                BattlefieldEvaluation evaluation = _evaluator.EvaluateBattlefield(battlefieldState);
                
                // Tomar decisiones tácticas
                float tacticalSkill = GetTeamTacticalSkill(team);
                TacticalDecisions decisions = _decisionMaker.MakeDecisions(team, evaluation, tacticalSkill);
                
                // Registrar decisiones en el historial
                RecordDecisions(team, decisions);
                
                // Ejecutar decisiones
                _executor.ExecuteDecisions(team, decisions);
                
                if (Settings.DebugMode)
                {
                    Debug.Print($"TacticalGenius: Actualización táctica para equipo {team.TeamIndex} completada");
                    Debug.Print($"  - Postura: {decisions.GeneralPosture}");
                    Debug.Print($"  - Maniobras: {decisions.PrimaryManeuvers.Count}");
                    Debug.Print($"  - Amenazas: {decisions.ThreatResponses.Count}");
                    Debug.Print($"  - Oportunidades: {decisions.OpportunityActions.Count}");
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"TacticalGenius: Error en actualización táctica: {ex.Message}");
            }
        }
        
        private float GetTeamTacticalSkill(Team team)
        {
            if (_teamTacticalSkill.ContainsKey(team))
            {
                return _teamTacticalSkill[team];
            }
            
            // Si no hay nivel táctico registrado, inicializar
            InitializeTeamTacticalSkill(team);
            return _teamTacticalSkill[team];
        }
        
        private void RecordDecisions(Team team, TacticalDecisions decisions)
        {
            if (!_decisionHistory.ContainsKey(team))
            {
                _decisionHistory[team] = new List<TacticalDecisions>();
            }
            
            // Limitar el historial a las últimas 10 decisiones
            if (_decisionHistory[team].Count >= 10)
            {
                _decisionHistory[team].RemoveAt(0);
            }
            
            _decisionHistory[team].Add(decisions);
        }
        
        public void OnBattleEnd()
        {
            // Limpiar recursos al finalizar la batalla
            _mission.OnMissionTick -= OnMissionTick;
            
            _teamTacticalSkill.Clear();
            _decisionHistory.Clear();
            _lastUpdateTime.Clear();
        }
    }
}
