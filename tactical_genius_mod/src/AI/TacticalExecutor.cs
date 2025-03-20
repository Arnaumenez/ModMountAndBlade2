using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Engine;
using TacticalGenius.Utils;
using TacticalGenius.Behaviors;

namespace TacticalGenius.AI
{
    public class TacticalExecutor
    {
        private Mission _mission;
        private FormationUtils _formationUtils;
        private BattleStateTracker _battleStateTracker;
        
        // Mapeo de tipos de maniobras a métodos de ejecución
        private Dictionary<ManeuverType, Action<TacticalManeuver>> _maneuverExecutors;
        private Dictionary<ThreatResponseType, Action<ThreatResponse>> _threatResponseExecutors;
        private Dictionary<OpportunityActionType, Action<OpportunityAction>> _opportunityActionExecutors;
        
        // Historial de decisiones para evitar cambios constantes
        private Dictionary<Team, TacticalDecisions> _lastExecutedDecisions = new Dictionary<Team, TacticalDecisions>();
        private Dictionary<Team, float> _lastExecutionTime = new Dictionary<Team, float>();
        
        // Tiempo mínimo entre cambios tácticos (en segundos)
        private const float MIN_TACTICAL_CHANGE_INTERVAL = 10.0f;
        
        public void Initialize(Mission mission)
        {
            _mission = mission;
            _formationUtils = new FormationUtils();
            _formationUtils.Initialize(mission);
            _battleStateTracker = new BattleStateTracker();
            _battleStateTracker.Initialize(mission);
            
            InitializeExecutors();
        }
        
        private void InitializeExecutors()
        {
            // Inicializar ejecutores de maniobras
            _maneuverExecutors = new Dictionary<ManeuverType, Action<TacticalManeuver>>
            {
                // Maniobras defensivas
                { ManeuverType.SeekHighGround, ExecuteSeekHighGround },
                { ManeuverType.FormDefensiveLine, ExecuteFormDefensiveLine },
                { ManeuverType.PositionRangedBehindInfantry, ExecutePositionRangedBehindInfantry },
                { ManeuverType.HoldCavalryInReserve, ExecuteHoldCavalryInReserve },
                { ManeuverType.FormShieldWall, ExecuteFormShieldWall },
                { ManeuverType.FormSpearWall, ExecuteFormSpearWall },
                
                // Maniobras equilibradas
                { ManeuverType.AdvanceInFormation, ExecuteAdvanceInFormation },
                { ManeuverType.PositionRangedForEffectiveness, ExecutePositionRangedForEffectiveness },
                { ManeuverType.PrepareCavalryFlank, ExecutePrepareCavalryFlank },
                { ManeuverType.ExploitFlankingOpportunity, ExecuteExploitFlankingOpportunity },
                
                // Maniobras ofensivas
                { ManeuverType.FrontalAssault, ExecuteFrontalAssault },
                { ManeuverType.AggressiveCavalryFlank, ExecuteAggressiveCavalryFlank },
                { ManeuverType.AdvanceRangedForDamage, ExecuteAdvanceRangedForDamage },
                { ManeuverType.EnvelopmentManeuver, ExecuteEnvelopmentManeuver },
                { ManeuverType.CoordinatedStrike, ExecuteCoordinatedStrike },
                
                // Maniobras específicas de cultura
                { ManeuverType.FormEmbolon, ExecuteFormEmbolon },
                { ManeuverType.ShockInfantryCharge, ExecuteShockInfantryCharge },
                { ManeuverType.HeavyCavalryCharge, ExecuteHeavyCavalryCharge },
                { ManeuverType.HorseArcherHarassment, ExecuteHorseArcherHarassment },
                { ManeuverType.MultiDirectionalAmbush, ExecuteMultiDirectionalAmbush },
                { ManeuverType.ForestArcherTactics, ExecuteForestArcherTactics },
                
                // Maniobras inesperadas
                { ManeuverType.FeignedRetreat, ExecuteFeignedRetreat },
                { ManeuverType.SuddenFormationChange, ExecuteSuddenFormationChange },
                { ManeuverType.HiddenReserveDeployment, ExecuteHiddenReserveDeployment },
                { ManeuverType.DistractionManeuver, ExecuteDistractionManeuver },
                { ManeuverType.FalseFlankingMovement, ExecuteFalseFlankingMovement }
            };
            
            // Inicializar ejecutores de respuestas a amenazas
            _threatResponseExecutors = new Dictionary<ThreatResponseType, Action<ThreatResponse>>
            {
                { ThreatResponseType.FormAntiCavalryDefense, ExecuteFormAntiCavalryDefense },
                { ThreatResponseType.FormDefensiveSquare, ExecuteFormDefensiveSquare },
                { ThreatResponseType.RaiseShields, ExecuteRaiseShields },
                { ThreatResponseType.ChargeCavalryAtRanged, ExecuteChargeCavalryAtRanged },
                { ThreatResponseType.AdvanceQuickly, ExecuteAdvanceQuickly },
                { ThreatResponseType.FlankWithCavalry, ExecuteFlankWithCavalry },
                { ThreatResponseType.ConcentrateRangedFire, ExecuteConcentrateRangedFire },
                { ThreatResponseType.FormDefensiveLine, ExecuteFormDefensiveLine },
                { ThreatResponseType.FormDefensiveCircle, ExecuteFormDefensiveCircle },
                { ThreatResponseType.ReformAndRegroup, ExecuteReformAndRegroup }
            };
            
            // Inicializar ejecutores de acciones de oportunidad
            _opportunityActionExecutors = new Dictionary<OpportunityActionType, Action<OpportunityAction>>
            {
                { OpportunityActionType.StrikeVulnerableRanged, ExecuteStrikeVulnerableRanged },
                { OpportunityActionType.SurroundIsolatedFormation, ExecuteSurroundIsolatedFormation },
                { OpportunityActionType.OccupyAdvantageousPosition, ExecuteOccupyAdvantageousPosition },
                { OpportunityActionType.ExploitEnemyDivision, ExecuteExploitEnemyDivision },
                { OpportunityActionType.ExecuteCounterCharge, ExecuteExecuteCounterCharge }
            };
        }
        
        public void ExecuteDecisions(Team team, TacticalDecisions decisions)
        {
            // Verificar si ha pasado suficiente tiempo desde la última ejecución
            if (!ShouldExecuteNewDecisions(team, decisions))
                return;
                
            try
            {
                // Aplicar ajustes de formación
                ApplyFormationAdjustments(team, decisions.FormationAdjustments);
                
                // Ejecutar maniobras primarias (ordenadas por prioridad)
                foreach (var maneuver in decisions.PrimaryManeuvers.OrderByDescending(m => m.Priority))
                {
                    if (_maneuverExecutors.ContainsKey(maneuver.ManeuverType))
                    {
                        _maneuverExecutors[maneuver.ManeuverType](maneuver);
                    }
                }
                
                // Ejecutar respuestas a amenazas (ordenadas por prioridad)
                foreach (var response in decisions.ThreatResponses.OrderByDescending(r => r.Priority))
                {
                    if (_threatResponseExecutors.ContainsKey(response.ResponseType))
                    {
                        _threatResponseExecutors[response.ResponseType](response);
                    }
                }
                
                // Ejecutar acciones de oportunidad (ordenadas por prioridad)
                foreach (var action in decisions.OpportunityActions.OrderByDescending(a => a.Priority))
                {
                    if (_opportunityActionExecutors.ContainsKey(action.ActionType))
                    {
                        _opportunityActionExecutors[action.ActionType](action);
                    }
                }
                
                // Aplicar prioridades de objetivo
                ApplyTargetPriorities(team, decisions.TargetPriorities);
                
                // Actualizar historial de decisiones
                _lastExecutedDecisions[team] = decisions;
                _lastExecutionTime[team] = _mission.CurrentTime;
                
                if (Settings.DebugMode)
                {
                    Debug.Print($"TacticalGenius: Ejecutadas decisiones tácticas para equipo {team.TeamIndex}");
                    Debug.Print($"  - Postura: {decisions.GeneralPosture}");
                    Debug.Print($"  - Maniobras: {string.Join(", ", decisions.PrimaryManeuvers.Select(m => m.ManeuverType))}");
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"TacticalGenius: Error al ejecutar decisiones tácticas: {ex.Message}");
            }
        }
        
        private bool ShouldExecuteNewDecisions(Team team, TacticalDecisions newDecisions)
        {
            // Si es la primera ejecución para este equipo, ejecutar inmediatamente
            if (!_lastExecutedDecisions.ContainsKey(team) || !_lastExecutionTime.ContainsKey(team))
                return true;
                
            // Verificar si ha pasado suficiente tiempo desde la última ejecución
            float timeSinceLastExecution = _mission.CurrentTime - _lastExecutionTime[team];
            if (timeSinceLastExecution < MIN_TACTICAL_CHANGE_INTERVAL)
            {
                // Permitir cambios solo si hay amenazas críticas
                bool hasCriticalThreats = newDecisions.ThreatResponses.Any(r => r.Priority > 0.9f);
                if (!hasCriticalThreats)
                    return false;
            }
            
            // Verificar si las nuevas decisiones son significativamente diferentes
            TacticalDecisions lastDecisions = _lastExecutedDecisions[team];
            if (newDecisions.GeneralPosture == lastDecisions.GeneralPosture &&
                AreManeuversSimilar(newDecisions.PrimaryManeuvers, lastDecisions.PrimaryManeuvers))
            {
                // Si las decisiones son similares, permitir cambios con menos frecuencia
                return timeSinceLastExecution >= MIN_TACTICAL_CHANGE_INTERVAL * 2;
            }
            
            return true;
        }
        
        private bool AreManeuversSimilar(List<TacticalManeuver> newManeuvers, List<TacticalManeuver> oldManeuvers)
        {
            // Si el número de maniobras es diferente, no son similares
            if (newManeuvers.Count != oldManeuvers.Count)
                return false;
                
            // Contar cuántas maniobras coinciden
            int matchCount = 0;
            foreach (var newManeuver in newManeuvers)
            {
                if (oldManeuvers.Any(m => m.ManeuverType == newManeuver.ManeuverType))
                {
                    matchCount++;
                }
            }
            
            // Si más del 70% de las maniobras coinciden, considerarlas similares
            return (float)matchCount / newManeuvers.Count >= 0.7f;
        }
        
        private void ApplyFormationAdjustments(Team team, Dictionary<FormationClass, FormationAdjustment> adjustments)
        {
            foreach (var adjustment in adjustments)
            {
                Formation formation = team.GetFormation(adjustment.Key);
                if (formation != null && formation.CountOfUnits > 0)
                {
                    // Aplicar tipo de formación
                    ApplyFormationType(formation, adjustment.Value.FormationType);
                    
                    // Aplicar espaciado
                    ApplyFormationSpacing(formation, adjustment.Value.Spacing);
                    
                    // Aplicar profundidad
                    ApplyFormationDepth(formation, adjustment.Value.Depth);
                    
                    // Aplicar posición relativa
                    ApplyRelativePosition(formation, adjustment.Value.RelativePosition, team);
                }
            }
        }
        
        private void ApplyFormationType(Formation formation, FormationType formationType)
        {
            switch (formationType)
            {
                case FormationType.Line:
                    formation.ArrangementOrder = ArrangementOrder.Line;
                    break;
                    
                case FormationType.Column:
                    formation.ArrangementOrder = ArrangementOrder.Column;
                    break;
                    
                case FormationType.Square:
                    formation.ArrangementOrder = ArrangementOrder.Square;
                    break;
                    
                case FormationType.Circle:
                    formation.ArrangementOrder = ArrangementOrder.Circle;
                    break;
                    
                case FormationType.Wedge:
                    formation.ArrangementOrder = ArrangementOrder.Wedge;
                    break;
                    
                case FormationType.ShieldWall:
                    formation.ArrangementOrder = ArrangementOrder.Line;
                    formation.FormOrder = FormOrder.ShieldWall;
                    break;
                    
                case FormationType.SpearWall:
                    formation.ArrangementOrder = ArrangementOrder.Line;
                    formation.FormOrder = FormOrder.BraceForCharge;
                    break;
                    
                case FormationType.Scatter:
                    formation.ArrangementOrder = ArrangementOrder.Scatter;
                    break;
            }
        }
        
        private void ApplyFormationSpacing(Formation formation, FormationSpacing spacing)
        {
            switch (spacing)
            {
                case FormationSpacing.VeryTight:
                    formation.SetSpacing(0.5f);
                    break;
                    
                case FormationSpacing.Tight:
                    formation.SetSpacing(0.8f);
                    break;
                    
                case FormationSpacing.Normal:
                    formation.SetSpacing(1.0f);
                    break;
                    
                case FormationSpacing.Loose:
                    formation.SetSpacing(1.3f);
                    break;
                    
                case FormationSpacing.VeryLoose:
                    formation.SetSpacing(1.8f);
                    break;
            }
        }
        
        private void ApplyFormationDepth(Formation formation, int depth)
        {
            // En el juego real, esto se implementaría ajustando la profundidad de la formación
            // Aquí usamos una aproximación basada en el espaciado y el ancho
            
            int unitCount = formation.CountOfUnits;
            if (unitCount <= 0)
                return;
                
            // Calcular ancho basado en la profundidad deseada
            int width = Math.Max(1, unitCount / depth);
            
            // Ajustar el ancho de la formación
            formation.SetWidthOfFormationNoManeuver(width);
        }
        
        private void ApplyRelativePosition(Formation formation, RelativePosition relativePosition, Team team)
        {
            // Obtener formación de infantería como referencia
            Formation infantryFormation = team.GetFormation(FormationClass.Infantry);
            if (infantryFormation == null || infantryFormation.CountOfUnits == 0)
                return;
                
            // Obtener dirección y posición de la infantería
            Vec2 infantryDirection = infantryFormation.Direction.AsVec2;
            Vec2 infantryPosition = infantryFormation.CurrentPosition.AsVec2;
            
            // Calcular vectores perpendiculares (flancos)
            Vec2 rightFlank = new Vec2(infantryDirection.y, -infantryDirection.x);
            Vec2 leftFlank = new Vec2(-infantryDirection.y, infantryDirection.x);
            
            // Calcular nueva posición basada en la posición relativa
            Vec2 newPosition = infantryPosition;
            
            switch (relativePosition)
            {
                case RelativePosition.Center:
                    // Mantener en el centro, no cambiar posición
                    break;
                    
                case RelativePosition.Flank:
                    // Posicionar en el flanco derecho
                    newPosition += rightFlank * (infantryFormation.Width + formation.Width) * 0.6f;
                    break;
                    
                case RelativePosition.AdvancedFlank:
                    // Posicionar en flanco avanzado
                    newPosition += rightFlank * (infantryFormation.Width + formation.Width) * 0.6f;
                    newPosition += infantryDirection * infantryFormation.Depth * 0.5f;
                    break;
                    
                case RelativePosition.ProtectedFlank:
                    // Posicionar en flanco protegido (ligeramente atrás)
                    newPosition += leftFlank * (infantryFormation.Width + formation.Width) * 0.6f;
                    newPosition -= infantryDirection * infantryFormation.Depth * 0.3f;
                    break;
                    
                case RelativePosition.FarFlank:
                    // Posicionar en flanco lejano
                    newPosition += rightFlank * (infantryFormation.Width + formation.Width) * 1.2f;
                    break;
                    
                case RelativePosition.BehindCenter:
                    // Posicionar detrás del centro
                    newPosition -= infantryDirection * (infantryFormation.Depth + formation.Depth) * 0.7f;
                    break;
                    
                case RelativePosition.BehindFlank:
                    // Posicionar detrás del flanco
                    newPosition += rightFlank * infantryFormation.Width * 0.7f;
                    newPosition -= infantryDirection * (infantryFormation.Depth + formation.Depth) * 0.7f;
                    break;
            }
            
            // Aplicar nueva posición
            WorldPosition worldPosition = new WorldPosition(_mission.Scene, newPosition.ToVec3());
            formation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
        }
        
        private void ApplyTargetPriorities(Team team, Dictionary<FormationClass, List<TargetPriority>> priorities)
        {
            // En el juego real, esto se implementaría ajustando las prioridades de objetivo de cada formación
            // Aquí simplemente registramos las prioridades para depuración
            
            if (Settings.DebugMode)
            {
                foreach (var priority in priorities)
                {
                    Formation formation = team.GetFormation(priority.Key);
                    if (formation != null && formation.CountOfUnits > 0)
                    {
                        string priorityStr = string.Join(", ", 
                            priority.Value.OrderByDescending(p => p.Priority)
                                .Select(p => $"{p.TargetFormationClass}:{p.Priority:F1}"));
                            
                        Debug.Print($"TacticalGenius: Prioridades para {priority.Key}: {priorityStr}");
                    }
                }
            }
        }
        
        #region Ejecutores de Maniobras
        
        // Maniobras defensivas
        
        private void ExecuteSeekHighGround(TacticalManeuver maneuver)
        {
            // Buscar terreno elevado cercano
            Vec3 highGround = _formationUtils.FindHighGround(maneuver.TargetFormations.FirstOrDefault()?.Team);
            
            if (highGround != Vec3.Zero)
            {
                foreach (Formation formation in maneuver.TargetFormations)
                {
                    WorldPosition worldPosition = new WorldPosition(_mission.Scene, highGround);
                    formation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                }
            }
        }
        
        private void ExecuteFormDefensiveLine(TacticalManeuver maneuver)
        {
            foreach (Formation formation in maneuver.TargetFormations)
            {
                formation.ArrangementOrder = ArrangementOrder.Line;
                formation.FormOrder = FormOrder.HoldFire;
                
                // Orientar hacia el enemigo
                Vec2 enemyDirection = _formationUtils.GetDirectionToNearestEnemy(formation);
                if (enemyDirection != Vec2.Zero)
                {
                    formation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(enemyDirection.ToVec3()));
                }
            }
        }
        
        private void ExecutePositionRangedBehindInfantry(TacticalManeuver maneuver)
        {
            Team team = maneuver.TargetFormations.FirstOrDefault()?.Team;
            if (team == null)
                return;
                
            Formation infantryFormation = team.GetFormation(FormationClass.Infantry);
            if (infantryFormation == null || infantryFormation.CountOfUnits == 0)
                return;
                
            foreach (Formation rangedFormation in maneuver.TargetFormations)
            {
                // Posicionar detrás de la infantería
                Vec3 infantryPosition = infantryFormation.CurrentPosition;
                Vec3 infantryDirection = infantryFormation.Direction;
                
                // Calcular posición detrás de la infantería
                Vec3 targetPosition = infantryPosition - (infantryDirection * (infantryFormation.Depth + rangedFormation.Depth) * 0.7f);
                
                WorldPosition worldPosition = new WorldPosition(_mission.Scene, targetPosition);
                rangedFormation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                
                // Orientar en la misma dirección que la infantería
                rangedFormation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(infantryDirection));
            }
        }
        
        private void ExecuteHoldCavalryInReserve(TacticalManeuver maneuver)
        {
            Team team = maneuver.TargetFormations.FirstOrDefault()?.Team;
            if (team == null)
                return;
                
            Formation infantryFormation = team.GetFormation(FormationClass.Infantry);
            if (infantryFormation == null)
                return;
                
            foreach (Formation cavalryFormation in maneuver.TargetFormations)
            {
                // Posicionar en reserva (detrás y a un flanco)
                Vec3 infantryPosition = infantryFormation.CurrentPosition;
                Vec3 infantryDirection = infantryFormation.Direction;
                
                // Calcular vector perpendicular (flanco)
                Vec3 flankDirection = new Vec3(infantryDirection.z, 0, -infantryDirection.x);
                
                // Calcular posición en reserva
                Vec3 targetPosition = infantryPosition - (infantryDirection * infantryFormation.Depth * 1.5f) + (flankDirection * infantryFormation.Width * 0.8f);
                
                WorldPosition worldPosition = new WorldPosition(_mission.Scene, targetPosition);
                cavalryFormation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                
                // Orientar hacia el enemigo
                Vec2 enemyDirection = _formationUtils.GetDirectionToNearestEnemy(cavalryFormation);
                if (enemyDirection != Vec2.Zero)
                {
                    cavalryFormation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(enemyDirection.ToVec3()));
                }
            }
        }
        
        private void ExecuteFormShieldWall(TacticalManeuver maneuver)
        {
            foreach (Formation formation in maneuver.TargetFormations)
            {
                formation.ArrangementOrder = ArrangementOrder.Line;
                formation.FormOrder = FormOrder.ShieldWall;
                formation.SetSpacing(0.8f);
                
                // Orientar hacia el enemigo
                Vec2 enemyDirection = _formationUtils.GetDirectionToNearestEnemy(formation);
                if (enemyDirection != Vec2.Zero)
                {
                    formation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(enemyDirection.ToVec3()));
                }
            }
        }
        
        private void ExecuteFormSpearWall(TacticalManeuver maneuver)
        {
            foreach (Formation formation in maneuver.TargetFormations)
            {
                formation.ArrangementOrder = ArrangementOrder.Line;
                formation.FormOrder = FormOrder.BraceForCharge;
                formation.SetSpacing(0.8f);
                
                // Orientar hacia el enemigo
                Vec2 enemyDirection = _formationUtils.GetDirectionToNearestEnemy(formation);
                if (enemyDirection != Vec2.Zero)
                {
                    formation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(enemyDirection.ToVec3()));
                }
            }
        }
        
        // Maniobras equilibradas
        
        private void ExecuteAdvanceInFormation(TacticalManeuver maneuver)
        {
            foreach (Formation formation in maneuver.TargetFormations)
            {
                // Obtener dirección hacia el enemigo
                Vec2 enemyDirection = _formationUtils.GetDirectionToNearestEnemy(formation);
                if (enemyDirection == Vec2.Zero)
                    continue;
                    
                // Calcular posición de avance
                Vec3 currentPosition = formation.CurrentPosition;
                Vec3 targetPosition = currentPosition + (enemyDirection.ToVec3() * 30f);
                
                // Ordenar avance
                WorldPosition worldPosition = new WorldPosition(_mission.Scene, targetPosition);
                formation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                
                // Orientar hacia el enemigo
                formation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(enemyDirection.ToVec3()));
            }
        }
        
        private void ExecutePositionRangedForEffectiveness(TacticalManeuver maneuver)
        {
            foreach (Formation formation in maneuver.TargetFormations)
            {
                // Buscar posición elevada cercana
                Vec3 elevatedPosition = _formationUtils.FindNearbyElevatedPosition(formation);
                
                if (elevatedPosition != Vec3.Zero)
                {
                    // Mover a posición elevada
                    WorldPosition worldPosition = new WorldPosition(_mission.Scene, elevatedPosition);
                    formation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                }
                
                // Orientar hacia el enemigo
                Vec2 enemyDirection = _formationUtils.GetDirectionToNearestEnemy(formation);
                if (enemyDirection != Vec2.Zero)
                {
                    formation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(enemyDirection.ToVec3()));
                }
                
                // Permitir disparar
                formation.FormOrder = FormOrder.FireAtWill;
            }
        }
        
        private void ExecutePrepareCavalryFlank(TacticalManeuver maneuver)
        {
            Team team = maneuver.TargetFormations.FirstOrDefault()?.Team;
            if (team == null)
                return;
                
            Formation infantryFormation = team.GetFormation(FormationClass.Infantry);
            if (infantryFormation == null)
                return;
                
            foreach (Formation cavalryFormation in maneuver.TargetFormations)
            {
                // Posicionar en el flanco
                Vec3 infantryPosition = infantryFormation.CurrentPosition;
                Vec3 infantryDirection = infantryFormation.Direction;
                
                // Calcular vector perpendicular (flanco)
                Vec3 flankDirection = new Vec3(infantryDirection.z, 0, -infantryDirection.x);
                
                // Calcular posición en el flanco
                Vec3 targetPosition = infantryPosition + (flankDirection * (infantryFormation.Width + cavalryFormation.Width) * 0.7f);
                
                WorldPosition worldPosition = new WorldPosition(_mission.Scene, targetPosition);
                cavalryFormation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                
                // Orientar hacia el enemigo
                Vec2 enemyDirection = _formationUtils.GetDirectionToNearestEnemy(cavalryFormation);
                if (enemyDirection != Vec2.Zero)
                {
                    cavalryFormation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(enemyDirection.ToVec3()));
                }
                
                // Preparar para carga
                cavalryFormation.ArrangementOrder = ArrangementOrder.Line;
            }
        }
        
        private void ExecuteExploitFlankingOpportunity(TacticalManeuver maneuver)
        {
            if (maneuver.TargetPosition == Vec3.Zero || maneuver.TargetFormations.Count == 0)
                return;
                
            // Determinar dirección de flanqueo basada en el lado especificado
            Vec3 targetPosition = maneuver.TargetPosition;
            FlankSide flankSide = FlankSide.Right; // Valor predeterminado
            
            if (!string.IsNullOrEmpty(maneuver.AdditionalData))
            {
                Enum.TryParse(maneuver.AdditionalData, out flankSide);
            }
            
            foreach (Formation formation in maneuver.TargetFormations)
            {
                // Calcular posición de flanqueo
                Vec3 flankPosition = CalculateFlankPosition(targetPosition, flankSide, formation);
                
                // Mover a posición de flanqueo
                WorldPosition worldPosition = new WorldPosition(_mission.Scene, flankPosition);
                formation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                
                // Orientar hacia el objetivo
                Vec3 directionToTarget = (targetPosition - flankPosition).NormalizedCopy();
                formation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(directionToTarget));
                
                // Preparar para carga si es caballería
                if (formation.FormationIndex == FormationClass.Cavalry)
                {
                    formation.ArrangementOrder = ArrangementOrder.Line;
                }
            }
        }
        
        private Vec3 CalculateFlankPosition(Vec3 targetPosition, FlankSide flankSide, Formation formation)
        {
            // Obtener dirección hacia el centro del campo de batalla
            Vec3 battleCenterDirection = (_mission.GetSceneMiddleFrame().origin - targetPosition).NormalizedCopy();
            
            // Calcular vector perpendicular según el lado de flanqueo
            Vec3 flankDirection;
            
            switch (flankSide)
            {
                case FlankSide.Left:
                    flankDirection = new Vec3(-battleCenterDirection.z, 0, battleCenterDirection.x);
                    break;
                    
                case FlankSide.Right:
                    flankDirection = new Vec3(battleCenterDirection.z, 0, -battleCenterDirection.x);
                    break;
                    
                case FlankSide.Rear:
                    flankDirection = -battleCenterDirection;
                    break;
                    
                default:
                    flankDirection = new Vec3(battleCenterDirection.z, 0, -battleCenterDirection.x);
                    break;
            }
            
            // Calcular posición de flanqueo
            float distanceToFlank = 40f; // Distancia de flanqueo
            return targetPosition + (flankDirection * distanceToFlank);
        }
        
        // Maniobras ofensivas
        
        private void ExecuteFrontalAssault(TacticalManeuver maneuver)
        {
            foreach (Formation formation in maneuver.TargetFormations)
            {
                // Obtener posición del enemigo más cercano
                Formation enemyFormation = _formationUtils.GetNearestEnemyFormation(formation);
                if (enemyFormation == null)
                    continue;
                    
                // Calcular dirección de carga
                Vec3 directionToEnemy = (enemyFormation.CurrentPosition - formation.CurrentPosition).NormalizedCopy();
                
                // Ordenar carga
                formation.SetMovementOrder(MovementOrder.MovementOrderCharge);
                
                // Orientar hacia el enemigo
                formation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(directionToEnemy));
            }
        }
        
        private void ExecuteAggressiveCavalryFlank(TacticalManeuver maneuver)
        {
            foreach (Formation formation in maneuver.TargetFormations)
            {
                // Obtener formación enemiga más vulnerable
                Formation enemyFormation = _formationUtils.GetMostVulnerableEnemyFormation(formation);
                if (enemyFormation == null)
                    continue;
                    
                // Calcular posición de flanqueo
                Vec3 enemyPosition = enemyFormation.CurrentPosition;
                Vec3 enemyDirection = enemyFormation.Direction;
                
                // Calcular vector perpendicular (flanco)
                Vec3 flankDirection = new Vec3(enemyDirection.z, 0, -enemyDirection.x);
                
                // Calcular posición de flanqueo
                Vec3 flankPosition = enemyPosition + (flankDirection * enemyFormation.Width * 0.8f);
                
                // Mover a posición de flanqueo
                WorldPosition worldPosition = new WorldPosition(_mission.Scene, flankPosition);
                formation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                
                // Una vez en posición, cargar
                if (Vec3.Distance(formation.CurrentPosition, flankPosition) < 20f)
                {
                    formation.SetMovementOrder(MovementOrder.MovementOrderCharge);
                }
            }
        }
        
        private void ExecuteAdvanceRangedForDamage(TacticalManeuver maneuver)
        {
            foreach (Formation formation in maneuver.TargetFormations)
            {
                // Obtener formación enemiga más cercana
                Formation enemyFormation = _formationUtils.GetNearestEnemyFormation(formation);
                if (enemyFormation == null)
                    continue;
                    
                // Calcular distancia óptima de tiro (lo suficientemente cerca para maximizar daño, pero no demasiado)
                float optimalDistance = 50f;
                
                // Calcular dirección hacia el enemigo
                Vec3 directionToEnemy = (enemyFormation.CurrentPosition - formation.CurrentPosition).NormalizedCopy();
                
                // Calcular posición de avance
                float currentDistance = Vec3.Distance(formation.CurrentPosition, enemyFormation.CurrentPosition);
                Vec3 targetPosition;
                
                if (currentDistance > optimalDistance * 1.2f)
                {
                    // Avanzar hacia el enemigo
                    targetPosition = formation.CurrentPosition + (directionToEnemy * (currentDistance - optimalDistance));
                }
                else
                {
                    // Mantener posición actual
                    targetPosition = formation.CurrentPosition;
                }
                
                // Mover a posición de avance
                WorldPosition worldPosition = new WorldPosition(_mission.Scene, targetPosition);
                formation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                
                // Orientar hacia el enemigo
                formation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(directionToEnemy));
                
                // Permitir disparar
                formation.FormOrder = FormOrder.FireAtWill;
            }
        }
        
        private void ExecuteEnvelopmentManeuver(TacticalManeuver maneuver)
        {
            Team team = maneuver.TargetFormations.FirstOrDefault()?.Team;
            if (team == null)
                return;
                
            // Obtener formación enemiga principal
            Team enemyTeam = _mission.Teams.FirstOrDefault(t => t.IsValid && t.IsEnemyOf(team));
            if (enemyTeam == null)
                return;
                
            // Calcular centro de las formaciones enemigas
            Vec3 enemyCenter = _formationUtils.CalculateTeamCenter(enemyTeam);
            
            // Dividir formaciones para envolvimiento
            List<Formation> leftFlankFormations = new List<Formation>();
            List<Formation> centerFormations = new List<Formation>();
            List<Formation> rightFlankFormations = new List<Formation>();
            
            // Asignar formaciones a grupos
            foreach (Formation formation in maneuver.TargetFormations)
            {
                switch (formation.FormationIndex)
                {
                    case FormationClass.Infantry:
                        centerFormations.Add(formation);
                        break;
                        
                    case FormationClass.Cavalry:
                        if (rightFlankFormations.Count <= leftFlankFormations.Count)
                            rightFlankFormations.Add(formation);
                        else
                            leftFlankFormations.Add(formation);
                        break;
                        
                    case FormationClass.Ranged:
                        centerFormations.Add(formation);
                        break;
                        
                    case FormationClass.HorseArcher:
                        if (leftFlankFormations.Count <= rightFlankFormations.Count)
                            leftFlankFormations.Add(formation);
                        else
                            rightFlankFormations.Add(formation);
                        break;
                        
                    default:
                        centerFormations.Add(formation);
                        break;
                }
            }
            
            // Calcular dirección hacia el enemigo
            Vec3 teamCenter = _formationUtils.CalculateTeamCenter(team);
            Vec3 directionToEnemy = (enemyCenter - teamCenter).NormalizedCopy();
            
            // Calcular vectores perpendiculares (flancos)
            Vec3 rightFlank = new Vec3(directionToEnemy.z, 0, -directionToEnemy.x);
            Vec3 leftFlank = new Vec3(-directionToEnemy.z, 0, directionToEnemy.x);
            
            // Mover formaciones centrales directamente hacia el enemigo
            foreach (Formation formation in centerFormations)
            {
                Vec3 targetPosition = enemyCenter - (directionToEnemy * 30f);
                WorldPosition worldPosition = new WorldPosition(_mission.Scene, targetPosition);
                formation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                formation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(directionToEnemy));
            }
            
            // Mover formaciones del flanco izquierdo
            foreach (Formation formation in leftFlankFormations)
            {
                Vec3 targetPosition = enemyCenter + (leftFlank * 50f);
                WorldPosition worldPosition = new WorldPosition(_mission.Scene, targetPosition);
                formation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                
                // Orientar hacia el centro enemigo
                Vec3 directionToCenter = (enemyCenter - targetPosition).NormalizedCopy();
                formation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(directionToCenter));
            }
            
            // Mover formaciones del flanco derecho
            foreach (Formation formation in rightFlankFormations)
            {
                Vec3 targetPosition = enemyCenter + (rightFlank * 50f);
                WorldPosition worldPosition = new WorldPosition(_mission.Scene, targetPosition);
                formation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                
                // Orientar hacia el centro enemigo
                Vec3 directionToCenter = (enemyCenter - targetPosition).NormalizedCopy();
                formation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(directionToCenter));
            }
        }
        
        private void ExecuteCoordinatedStrike(TacticalManeuver maneuver)
        {
            if (maneuver.TargetPosition == Vec3.Zero || maneuver.TargetFormations.Count == 0)
                return;
                
            // Dividir formaciones para ataque coordinado
            List<Formation> directAssaultFormations = new List<Formation>();
            List<Formation> flankingFormations = new List<Formation>();
            
            // Asignar formaciones a grupos
            foreach (Formation formation in maneuver.TargetFormations)
            {
                if (formation.FormationIndex == FormationClass.Cavalry || formation.FormationIndex == FormationClass.HorseArcher)
                {
                    flankingFormations.Add(formation);
                }
                else
                {
                    directAssaultFormations.Add(formation);
                }
            }
            
            // Calcular dirección hacia el objetivo
            Vec3 teamCenter = _formationUtils.CalculateTeamCenter(maneuver.TargetFormations.First().Team);
            Vec3 directionToTarget = (maneuver.TargetPosition - teamCenter).NormalizedCopy();
            
            // Calcular vectores perpendiculares (flancos)
            Vec3 rightFlank = new Vec3(directionToTarget.z, 0, -directionToTarget.x);
            
            // Mover formaciones de asalto directo
            foreach (Formation formation in directAssaultFormations)
            {
                formation.SetMovementOrder(MovementOrder.MovementOrderCharge);
                formation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(directionToTarget));
            }
            
            // Mover formaciones de flanqueo
            foreach (Formation formation in flankingFormations)
            {
                Vec3 targetPosition = maneuver.TargetPosition + (rightFlank * 40f);
                WorldPosition worldPosition = new WorldPosition(_mission.Scene, targetPosition);
                formation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                
                // Una vez en posición, cargar
                if (Vec3.Distance(formation.CurrentPosition, targetPosition) < 20f)
                {
                    formation.SetMovementOrder(MovementOrder.MovementOrderCharge);
                }
            }
        }
        
        // Maniobras específicas de cultura
        
        private void ExecuteFormEmbolon(TacticalManeuver maneuver)
        {
            foreach (Formation formation in maneuver.TargetFormations)
            {
                // Configurar formación en cuña (Embolon)
                formation.ArrangementOrder = ArrangementOrder.Wedge;
                
                // Obtener dirección hacia el enemigo
                Vec2 enemyDirection = _formationUtils.GetDirectionToNearestEnemy(formation);
                if (enemyDirection != Vec2.Zero)
                {
                    // Orientar hacia el enemigo
                    formation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(enemyDirection.ToVec3()));
                    
                    // Avanzar hacia el enemigo
                    Vec3 currentPosition = formation.CurrentPosition;
                    Vec3 targetPosition = currentPosition + (enemyDirection.ToVec3() * 30f);
                    
                    WorldPosition worldPosition = new WorldPosition(_mission.Scene, targetPosition);
                    formation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                }
            }
        }
        
        private void ExecuteShockInfantryCharge(TacticalManeuver maneuver)
        {
            foreach (Formation formation in maneuver.TargetFormations)
            {
                // Configurar formación para carga de choque
                formation.ArrangementOrder = ArrangementOrder.Line;
                formation.SetSpacing(1.2f); // Espaciado amplio para momentum
                
                // Obtener formación enemiga más cercana
                Formation enemyFormation = _formationUtils.GetNearestEnemyFormation(formation);
                if (enemyFormation == null)
                    continue;
                    
                // Calcular dirección de carga
                Vec3 directionToEnemy = (enemyFormation.CurrentPosition - formation.CurrentPosition).NormalizedCopy();
                
                // Ordenar carga
                formation.SetMovementOrder(MovementOrder.MovementOrderCharge);
                
                // Orientar hacia el enemigo
                formation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(directionToEnemy));
            }
        }
        
        private void ExecuteHeavyCavalryCharge(TacticalManeuver maneuver)
        {
            foreach (Formation formation in maneuver.TargetFormations)
            {
                // Configurar formación para carga de caballería pesada
                formation.ArrangementOrder = ArrangementOrder.Wedge;
                
                // Obtener formación enemiga más vulnerable
                Formation enemyFormation = _formationUtils.GetMostVulnerableEnemyFormation(formation);
                if (enemyFormation == null)
                    continue;
                    
                // Calcular dirección de carga
                Vec3 directionToEnemy = (enemyFormation.CurrentPosition - formation.CurrentPosition).NormalizedCopy();
                
                // Ordenar carga
                formation.SetMovementOrder(MovementOrder.MovementOrderCharge);
                
                // Orientar hacia el enemigo
                formation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(directionToEnemy));
            }
        }
        
        private void ExecuteHorseArcherHarassment(TacticalManeuver maneuver)
        {
            foreach (Formation formation in maneuver.TargetFormations)
            {
                // Configurar formación para hostigamiento
                formation.ArrangementOrder = ArrangementOrder.Scatter;
                formation.SetSpacing(2.0f); // Espaciado muy amplio
                
                // Obtener formación enemiga más cercana
                Formation enemyFormation = _formationUtils.GetNearestEnemyFormation(formation);
                if (enemyFormation == null)
                    continue;
                    
                // Calcular distancia óptima de hostigamiento
                float optimalDistance = 60f;
                
                // Calcular dirección hacia el enemigo
                Vec3 directionToEnemy = (enemyFormation.CurrentPosition - formation.CurrentPosition).NormalizedCopy();
                
                // Calcular posición de hostigamiento
                float currentDistance = Vec3.Distance(formation.CurrentPosition, enemyFormation.CurrentPosition);
                Vec3 targetPosition;
                
                if (currentDistance < optimalDistance * 0.8f)
                {
                    // Alejarse del enemigo
                    targetPosition = formation.CurrentPosition - (directionToEnemy * (optimalDistance - currentDistance));
                }
                else if (currentDistance > optimalDistance * 1.2f)
                {
                    // Acercarse al enemigo
                    targetPosition = formation.CurrentPosition + (directionToEnemy * (currentDistance - optimalDistance));
                }
                else
                {
                    // Mantener distancia, moverse lateralmente
                    Vec3 lateralDirection = new Vec3(directionToEnemy.z, 0, -directionToEnemy.x);
                    targetPosition = formation.CurrentPosition + (lateralDirection * 20f);
                }
                
                // Mover a posición de hostigamiento
                WorldPosition worldPosition = new WorldPosition(_mission.Scene, targetPosition);
                formation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                
                // Orientar hacia el enemigo
                formation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(directionToEnemy));
                
                // Permitir disparar
                formation.FormOrder = FormOrder.FireAtWill;
            }
        }
        
        private void ExecuteMultiDirectionalAmbush(TacticalManeuver maneuver)
        {
            Team team = maneuver.TargetFormations.FirstOrDefault()?.Team;
            if (team == null)
                return;
                
            // Obtener formación enemiga principal
            Team enemyTeam = _mission.Teams.FirstOrDefault(t => t.IsValid && t.IsEnemyOf(team));
            if (enemyTeam == null)
                return;
                
            // Calcular centro de las formaciones enemigas
            Vec3 enemyCenter = _formationUtils.CalculateTeamCenter(enemyTeam);
            
            // Dividir formaciones para emboscada
            List<Formation> formations = maneuver.TargetFormations.ToList();
            if (formations.Count < 2)
                return;
                
            // Calcular direcciones de emboscada (múltiples direcciones)
            List<Vec3> ambushDirections = new List<Vec3>();
            
            // Dirección frontal
            Vec3 teamCenter = _formationUtils.CalculateTeamCenter(team);
            Vec3 frontDirection = (enemyCenter - teamCenter).NormalizedCopy();
            ambushDirections.Add(frontDirection);
            
            // Dirección derecha
            Vec3 rightDirection = new Vec3(frontDirection.z, 0, -frontDirection.x);
            ambushDirections.Add(rightDirection);
            
            // Dirección izquierda
            Vec3 leftDirection = new Vec3(-frontDirection.z, 0, frontDirection.x);
            ambushDirections.Add(leftDirection);
            
            // Dirección trasera
            Vec3 rearDirection = -frontDirection;
            ambushDirections.Add(rearDirection);
            
            // Asignar formaciones a direcciones
            for (int i = 0; i < formations.Count; i++)
            {
                int directionIndex = i % ambushDirections.Count;
                Vec3 direction = ambushDirections[directionIndex];
                
                // Calcular posición de emboscada
                Vec3 ambushPosition = enemyCenter + (direction * 60f);
                
                // Mover a posición de emboscada
                WorldPosition worldPosition = new WorldPosition(_mission.Scene, ambushPosition);
                formations[i].SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                
                // Orientar hacia el centro enemigo
                Vec3 directionToCenter = (enemyCenter - ambushPosition).NormalizedCopy();
                formations[i].SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(directionToCenter));
            }
        }
        
        private void ExecuteForestArcherTactics(TacticalManeuver maneuver)
        {
            foreach (Formation formation in maneuver.TargetFormations)
            {
                // Buscar posición boscosa cercana
                Vec3 forestPosition = _formationUtils.FindNearbyCoverPosition(formation);
                
                if (forestPosition != Vec3.Zero)
                {
                    // Mover a posición boscosa
                    WorldPosition worldPosition = new WorldPosition(_mission.Scene, forestPosition);
                    formation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                }
                
                // Configurar formación para arqueros en bosque
                formation.ArrangementOrder = ArrangementOrder.Scatter;
                formation.SetSpacing(1.5f);
                
                // Orientar hacia el enemigo
                Vec2 enemyDirection = _formationUtils.GetDirectionToNearestEnemy(formation);
                if (enemyDirection != Vec2.Zero)
                {
                    formation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(enemyDirection.ToVec3()));
                }
                
                // Permitir disparar
                formation.FormOrder = FormOrder.FireAtWill;
            }
        }
        
        // Maniobras inesperadas
        
        private void ExecuteFeignedRetreat(TacticalManeuver maneuver)
        {
            Team team = maneuver.TargetFormations.FirstOrDefault()?.Team;
            if (team == null)
                return;
                
            // Obtener dirección hacia el enemigo
            Vec3 teamCenter = _formationUtils.CalculateTeamCenter(team);
            Vec2 enemyDirection = _formationUtils.GetDirectionToNearestEnemy(maneuver.TargetFormations.FirstOrDefault());
            
            if (enemyDirection == Vec2.Zero)
                return;
                
            // Calcular dirección de retirada (opuesta al enemigo)
            Vec3 retreatDirection = -enemyDirection.ToVec3();
            
            // Distancia de retirada
            float retreatDistance = 50f;
            
            foreach (Formation formation in maneuver.TargetFormations)
            {
                // Calcular posición de retirada
                Vec3 retreatPosition = formation.CurrentPosition + (retreatDirection * retreatDistance);
                
                // Mover a posición de retirada
                WorldPosition worldPosition = new WorldPosition(_mission.Scene, retreatPosition);
                formation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                
                // Orientar hacia el enemigo (para mantener la vista en ellos durante la retirada)
                formation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(-retreatDirection));
            }
            
            // Registrar la maniobra para posterior contraataque
            _battleStateTracker.RegisterFeignedRetreat(team, maneuver.TargetFormations);
        }
        
        private void ExecuteSuddenFormationChange(TacticalManeuver maneuver)
        {
            foreach (Formation formation in maneuver.TargetFormations)
            {
                // Cambiar repentinamente la formación para confundir al enemigo
                if (formation.ArrangementOrder == ArrangementOrder.Line)
                {
                    formation.ArrangementOrder = ArrangementOrder.Wedge;
                }
                else if (formation.ArrangementOrder == ArrangementOrder.Column)
                {
                    formation.ArrangementOrder = ArrangementOrder.Line;
                }
                else
                {
                    formation.ArrangementOrder = ArrangementOrder.Column;
                }
                
                // Cambiar espaciado
                if (formation.GetSpacing() < 1.0f)
                {
                    formation.SetSpacing(1.5f);
                }
                else
                {
                    formation.SetSpacing(0.8f);
                }
            }
        }
        
        private void ExecuteHiddenReserveDeployment(TacticalManeuver maneuver)
        {
            // Esta maniobra simula el despliegue de una "reserva oculta"
            // En el juego real, esto implicaría ocultar unidades y luego revelarlas
            // Aquí simulamos el efecto cambiando la posición y comportamiento
            
            foreach (Formation formation in maneuver.TargetFormations)
            {
                // Obtener formación enemiga más cercana
                Formation enemyFormation = _formationUtils.GetNearestEnemyFormation(formation);
                if (enemyFormation == null)
                    continue;
                    
                // Calcular dirección hacia el enemigo
                Vec3 directionToEnemy = (enemyFormation.CurrentPosition - formation.CurrentPosition).NormalizedCopy();
                
                // Calcular vector perpendicular (flanco)
                Vec3 flankDirection = new Vec3(directionToEnemy.z, 0, -directionToEnemy.x);
                
                // Calcular posición de "reserva"
                Vec3 reservePosition = formation.CurrentPosition + (flankDirection * 40f);
                
                // Mover a posición de reserva
                WorldPosition worldPosition = new WorldPosition(_mission.Scene, reservePosition);
                formation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                
                // Una vez en posición, cargar
                if (Vec3.Distance(formation.CurrentPosition, reservePosition) < 20f)
                {
                    formation.SetMovementOrder(MovementOrder.MovementOrderCharge);
                }
            }
        }
        
        private void ExecuteDistractionManeuver(TacticalManeuver maneuver)
        {
            Team team = maneuver.TargetFormations.FirstOrDefault()?.Team;
            if (team == null || maneuver.TargetFormations.Count < 2)
                return;
                
            // Dividir formaciones para distracción
            Formation distractionFormation = maneuver.TargetFormations.First();
            List<Formation> mainForceFormations = maneuver.TargetFormations.Skip(1).ToList();
            
            // Obtener formación enemiga principal
            Formation enemyFormation = _formationUtils.GetNearestEnemyFormation(distractionFormation);
            if (enemyFormation == null)
                return;
                
            // Calcular dirección hacia el enemigo
            Vec3 directionToEnemy = (enemyFormation.CurrentPosition - distractionFormation.CurrentPosition).NormalizedCopy();
            
            // Calcular vector perpendicular (flanco)
            Vec3 flankDirection = new Vec3(directionToEnemy.z, 0, -directionToEnemy.x);
            
            // Mover formación de distracción directamente hacia el enemigo
            distractionFormation.SetMovementOrder(MovementOrder.MovementOrderCharge);
            distractionFormation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(directionToEnemy));
            
            // Mover fuerza principal al flanco
            foreach (Formation formation in mainForceFormations)
            {
                Vec3 flankPosition = enemyFormation.CurrentPosition + (flankDirection * 50f);
                WorldPosition worldPosition = new WorldPosition(_mission.Scene, flankPosition);
                formation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                
                // Orientar hacia el enemigo
                Vec3 directionFromFlank = (enemyFormation.CurrentPosition - flankPosition).NormalizedCopy();
                formation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(directionFromFlank));
            }
        }
        
        private void ExecuteFalseFlankingMovement(TacticalManeuver maneuver)
        {
            foreach (Formation formation in maneuver.TargetFormations)
            {
                // Obtener formación enemiga más cercana
                Formation enemyFormation = _formationUtils.GetNearestEnemyFormation(formation);
                if (enemyFormation == null)
                    continue;
                    
                // Calcular dirección hacia el enemigo
                Vec3 directionToEnemy = (enemyFormation.CurrentPosition - formation.CurrentPosition).NormalizedCopy();
                
                // Calcular vector perpendicular (flanco)
                Vec3 flankDirection = new Vec3(directionToEnemy.z, 0, -directionToEnemy.x);
                
                // Calcular posición de falso flanqueo
                Vec3 flankPosition = formation.CurrentPosition + (flankDirection * 30f);
                
                // Mover a posición de falso flanqueo
                WorldPosition worldPosition = new WorldPosition(_mission.Scene, flankPosition);
                formation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                
                // Registrar la maniobra para posterior cambio de dirección
                _battleStateTracker.RegisterFalseFlankingMovement(formation, flankPosition);
            }
        }
        
        #endregion
        
        #region Ejecutores de Respuestas a Amenazas
        
        private void ExecuteFormAntiCavalryDefense(ThreatResponse response)
        {
            foreach (Formation formation in response.TargetFormations)
            {
                // Configurar formación anti-caballería
                formation.ArrangementOrder = ArrangementOrder.Line;
                formation.FormOrder = FormOrder.BraceForCharge;
                formation.SetSpacing(0.8f);
                
                // Orientar hacia la amenaza
                Vec3 directionToThreat = (response.Threat.Position - formation.CurrentPosition).NormalizedCopy();
                formation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(directionToThreat));
            }
        }
        
        private void ExecuteFormDefensiveSquare(ThreatResponse response)
        {
            foreach (Formation formation in response.TargetFormations)
            {
                // Configurar formación en cuadro defensivo
                formation.ArrangementOrder = ArrangementOrder.Square;
                formation.SetSpacing(0.7f);
                
                // Orientar hacia la amenaza
                Vec3 directionToThreat = (response.Threat.Position - formation.CurrentPosition).NormalizedCopy();
                formation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(directionToThreat));
            }
        }
        
        private void ExecuteRaiseShields(ThreatResponse response)
        {
            foreach (Formation formation in response.TargetFormations)
            {
                // Configurar formación para levantar escudos
                formation.ArrangementOrder = ArrangementOrder.Line;
                formation.FormOrder = FormOrder.ShieldWall;
                
                // Orientar hacia la amenaza
                Vec3 directionToThreat = (response.Threat.Position - formation.CurrentPosition).NormalizedCopy();
                formation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(directionToThreat));
            }
        }
        
        private void ExecuteChargeCavalryAtRanged(ThreatResponse response)
        {
            if (response.TargetPosition == Vec3.Zero)
                return;
                
            foreach (Formation formation in response.TargetFormations)
            {
                // Configurar formación para carga
                formation.ArrangementOrder = ArrangementOrder.Line;
                
                // Ordenar carga hacia la posición de la amenaza
                formation.SetMovementOrder(MovementOrder.MovementOrderCharge);
                
                // Orientar hacia la amenaza
                Vec3 directionToThreat = (response.Threat.Position - formation.CurrentPosition).NormalizedCopy();
                formation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(directionToThreat));
            }
        }
        
        private void ExecuteAdvanceQuickly(ThreatResponse response)
        {
            foreach (Formation formation in response.TargetFormations)
            {
                // Obtener dirección hacia la amenaza
                Vec3 directionToThreat = (response.Threat.Position - formation.CurrentPosition).NormalizedCopy();
                
                // Calcular posición de avance rápido (acercarse a la amenaza)
                Vec3 advancePosition = formation.CurrentPosition + (directionToThreat * 30f);
                
                // Mover rápidamente hacia la posición
                WorldPosition worldPosition = new WorldPosition(_mission.Scene, advancePosition);
                formation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                
                // Aumentar velocidad de movimiento
                formation.SetMovementSpeed(2.0f); // Velocidad rápida
            }
        }
        
        private void ExecuteFlankWithCavalry(ThreatResponse response)
        {
            if (response.TargetPosition == Vec3.Zero)
                return;
                
            foreach (Formation formation in response.TargetFormations)
            {
                // Calcular dirección hacia la amenaza
                Vec3 directionToThreat = (response.Threat.Position - formation.CurrentPosition).NormalizedCopy();
                
                // Calcular vector perpendicular (flanco)
                Vec3 flankDirection = new Vec3(directionToThreat.z, 0, -directionToThreat.x);
                
                // Calcular posición de flanqueo
                Vec3 flankPosition = response.Threat.Position + (flankDirection * 40f);
                
                // Mover a posición de flanqueo
                WorldPosition worldPosition = new WorldPosition(_mission.Scene, flankPosition);
                formation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                
                // Una vez en posición, cargar
                if (Vec3.Distance(formation.CurrentPosition, flankPosition) < 20f)
                {
                    formation.SetMovementOrder(MovementOrder.MovementOrderCharge);
                }
            }
        }
        
        private void ExecuteConcentrateRangedFire(ThreatResponse response)
        {
            if (response.TargetPosition == Vec3.Zero)
                return;
                
            foreach (Formation formation in response.TargetFormations)
            {
                // Configurar formación para fuego concentrado
                formation.ArrangementOrder = ArrangementOrder.Line;
                formation.FormOrder = FormOrder.FireAtWill;
                
                // Orientar hacia la amenaza
                Vec3 directionToThreat = (response.Threat.Position - formation.CurrentPosition).NormalizedCopy();
                formation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(directionToThreat));
                
                // Calcular distancia óptima de tiro
                float optimalDistance = 50f;
                float currentDistance = Vec3.Distance(formation.CurrentPosition, response.Threat.Position);
                
                if (currentDistance > optimalDistance * 1.2f)
                {
                    // Acercarse para mejorar precisión
                    Vec3 advancePosition = formation.CurrentPosition + (directionToThreat * (currentDistance - optimalDistance));
                    WorldPosition worldPosition = new WorldPosition(_mission.Scene, advancePosition);
                    formation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                }
            }
        }
        
        private void ExecuteFormDefensiveLine(ThreatResponse response)
        {
            foreach (Formation formation in response.TargetFormations)
            {
                // Configurar formación en línea defensiva
                formation.ArrangementOrder = ArrangementOrder.Line;
                formation.SetSpacing(0.8f);
                
                // Orientar hacia la amenaza
                Vec3 directionToThreat = (response.Threat.Position - formation.CurrentPosition).NormalizedCopy();
                formation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(directionToThreat));
            }
        }
        
        private void ExecuteFormDefensiveCircle(ThreatResponse response)
        {
            foreach (Formation formation in response.TargetFormations)
            {
                // Configurar formación en círculo defensivo
                formation.ArrangementOrder = ArrangementOrder.Circle;
                formation.SetSpacing(0.7f);
            }
        }
        
        private void ExecuteReformAndRegroup(ThreatResponse response)
        {
            Team team = response.TargetFormations.FirstOrDefault()?.Team;
            if (team == null)
                return;
                
            // Calcular centro del equipo
            Vec3 teamCenter = _formationUtils.CalculateTeamCenter(team);
            
            foreach (Formation formation in response.TargetFormations)
            {
                // Mover hacia el centro del equipo para reagruparse
                WorldPosition worldPosition = new WorldPosition(_mission.Scene, teamCenter);
                formation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                
                // Orientar hacia la amenaza
                Vec3 directionToThreat = (response.Threat.Position - teamCenter).NormalizedCopy();
                formation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(directionToThreat));
            }
        }
        
        #endregion
        
        #region Ejecutores de Acciones de Oportunidad
        
        private void ExecuteStrikeVulnerableRanged(OpportunityAction action)
        {
            if (action.TargetPosition == Vec3.Zero)
                return;
                
            foreach (Formation formation in action.TargetFormations)
            {
                // Configurar formación para ataque
                if (formation.FormationIndex == FormationClass.Cavalry || formation.FormationIndex == FormationClass.HorseArcher)
                {
                    formation.ArrangementOrder = ArrangementOrder.Line;
                }
                else
                {
                    formation.ArrangementOrder = ArrangementOrder.Wedge;
                }
                
                // Ordenar carga hacia la posición objetivo
                formation.SetMovementOrder(MovementOrder.MovementOrderCharge);
                
                // Orientar hacia el objetivo
                Vec3 directionToTarget = (action.TargetPosition - formation.CurrentPosition).NormalizedCopy();
                formation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(directionToTarget));
            }
        }
        
        private void ExecuteSurroundIsolatedFormation(OpportunityAction action)
        {
            if (action.TargetPosition == Vec3.Zero || action.TargetFormations.Count < 2)
                return;
                
            // Dividir formaciones para rodear al objetivo
            List<Formation> formations = action.TargetFormations.ToList();
            
            // Calcular direcciones para rodear (múltiples direcciones)
            List<Vec3> surroundDirections = new List<Vec3>();
            
            // Dirección frontal
            Vec3 teamCenter = _formationUtils.CalculateTeamCenter(formations.First().Team);
            Vec3 frontDirection = (action.TargetPosition - teamCenter).NormalizedCopy();
            surroundDirections.Add(frontDirection);
            
            // Dirección derecha
            Vec3 rightDirection = new Vec3(frontDirection.z, 0, -frontDirection.x);
            surroundDirections.Add(rightDirection);
            
            // Dirección izquierda
            Vec3 leftDirection = new Vec3(-frontDirection.z, 0, frontDirection.x);
            surroundDirections.Add(leftDirection);
            
            // Dirección trasera
            Vec3 rearDirection = -frontDirection;
            surroundDirections.Add(rearDirection);
            
            // Asignar formaciones a direcciones
            for (int i = 0; i < formations.Count; i++)
            {
                int directionIndex = i % surroundDirections.Count;
                Vec3 direction = surroundDirections[directionIndex];
                
                // Calcular posición para rodear
                Vec3 surroundPosition = action.TargetPosition + (direction * 30f);
                
                // Mover a posición para rodear
                WorldPosition worldPosition = new WorldPosition(_mission.Scene, surroundPosition);
                formations[i].SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                
                // Orientar hacia el objetivo
                Vec3 directionToTarget = (action.TargetPosition - surroundPosition).NormalizedCopy();
                formations[i].SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(directionToTarget));
            }
        }
        
        private void ExecuteOccupyAdvantageousPosition(OpportunityAction action)
        {
            if (action.TargetPosition == Vec3.Zero)
                return;
                
            foreach (Formation formation in action.TargetFormations)
            {
                // Mover a posición ventajosa
                WorldPosition worldPosition = new WorldPosition(_mission.Scene, action.TargetPosition);
                formation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                
                // Orientar hacia el enemigo
                Vec2 enemyDirection = _formationUtils.GetDirectionToNearestEnemy(formation);
                if (enemyDirection != Vec2.Zero)
                {
                    formation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(enemyDirection.ToVec3()));
                }
                
                // Configurar formación según tipo de unidad
                if (formation.FormationIndex == FormationClass.Ranged)
                {
                    formation.FormOrder = FormOrder.FireAtWill;
                }
                else if (formation.FormationIndex == FormationClass.Infantry)
                {
                    formation.ArrangementOrder = ArrangementOrder.Line;
                }
            }
        }
        
        private void ExecuteExploitEnemyDivision(OpportunityAction action)
        {
            if (action.TargetPosition == Vec3.Zero)
                return;
                
            foreach (Formation formation in action.TargetFormations)
            {
                // Configurar formación para explotar división
                if (formation.FormationIndex == FormationClass.Cavalry)
                {
                    formation.ArrangementOrder = ArrangementOrder.Wedge;
                }
                else
                {
                    formation.ArrangementOrder = ArrangementOrder.Line;
                }
                
                // Mover a posición de división
                WorldPosition worldPosition = new WorldPosition(_mission.Scene, action.TargetPosition);
                formation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                
                // Una vez en posición, cargar
                if (Vec3.Distance(formation.CurrentPosition, action.TargetPosition) < 20f)
                {
                    formation.SetMovementOrder(MovementOrder.MovementOrderCharge);
                }
            }
        }
        
        private void ExecuteExecuteCounterCharge(OpportunityAction action)
        {
            if (action.TargetPosition == Vec3.Zero)
                return;
                
            foreach (Formation formation in action.TargetFormations)
            {
                // Configurar formación para contracarga
                if (formation.FormationIndex == FormationClass.Cavalry)
                {
                    formation.ArrangementOrder = ArrangementOrder.Wedge;
                }
                else if (formation.FormationIndex == FormationClass.Infantry)
                {
                    formation.ArrangementOrder = ArrangementOrder.Line;
                }
                
                // Ordenar carga hacia la posición objetivo
                formation.SetMovementOrder(MovementOrder.MovementOrderCharge);
                
                // Orientar hacia el objetivo
                Vec3 directionToTarget = (action.TargetPosition - formation.CurrentPosition).NormalizedCopy();
                formation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(directionToTarget));
            }
        }
        
        #endregion
    }
}
