using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Engine;
using TacticalGenius.Utils;

namespace TacticalGenius.AI
{
    public class DecisionMaker
    {
        private Mission _mission;
        private Dictionary<string, float> _tacticalWeights;
        private Random _random;
        
        // Umbrales para diferentes estados tácticos
        private const float DEFENSIVE_THRESHOLD = 0.4f;
        private const float BALANCED_THRESHOLD = 0.6f;
        // Por encima de BALANCED_THRESHOLD se considera ofensivo
        
        public void Initialize(Mission mission, Dictionary<string, float> tacticalWeights)
        {
            _mission = mission;
            _tacticalWeights = tacticalWeights;
            _random = new Random();
        }
        
        public TacticalDecisions MakeDecisions(Team team, BattlefieldState battlefieldState)
        {
            TacticalDecisions decisions = new TacticalDecisions();
            
            // Determinar postura general basada en la situación general
            decisions.GeneralPosture = DetermineGeneralPosture(battlefieldState.OverallSituation);
            
            // Determinar maniobras primarias basadas en la postura y el estado del campo
            decisions.PrimaryManeuvers = DeterminePrimaryManeuvers(team, battlefieldState, decisions.GeneralPosture);
            
            // Determinar respuestas a amenazas específicas
            decisions.ThreatResponses = DetermineThreatResponses(team, battlefieldState.Threats);
            
            // Determinar acciones para aprovechar oportunidades
            decisions.OpportunityActions = DetermineOpportunityActions(team, battlefieldState.Opportunities);
            
            // Determinar ajustes de formación para cada tipo de unidad
            decisions.FormationAdjustments = DetermineFormationAdjustments(team, battlefieldState);
            
            // Determinar prioridades de objetivo para cada formación
            decisions.TargetPriorities = DetermineTargetPriorities(team, battlefieldState);
            
            // Añadir elemento de imprevisibilidad para evitar comportamiento predecible
            AddTacticalVariation(decisions, battlefieldState.CommanderTacticSkill);
            
            return decisions;
        }
        
        private TacticalPosture DetermineGeneralPosture(float overallSituation)
        {
            if (overallSituation < DEFENSIVE_THRESHOLD)
                return TacticalPosture.Defensive;
            else if (overallSituation < BALANCED_THRESHOLD)
                return TacticalPosture.Balanced;
            else
                return TacticalPosture.Offensive;
        }
        
        private List<TacticalManeuver> DeterminePrimaryManeuvers(Team team, BattlefieldState state, TacticalPosture posture)
        {
            List<TacticalManeuver> maneuvers = new List<TacticalManeuver>();
            
            // Seleccionar maniobras basadas en la postura táctica
            switch (posture)
            {
                case TacticalPosture.Defensive:
                    AddDefensiveManeuvers(team, state, maneuvers);
                    break;
                    
                case TacticalPosture.Balanced:
                    AddBalancedManeuvers(team, state, maneuvers);
                    break;
                    
                case TacticalPosture.Offensive:
                    AddOffensiveManeuvers(team, state, maneuvers);
                    break;
            }
            
            // Añadir maniobras específicas de cultura si es apropiado
            AddCultureSpecificManeuvers(team, state, maneuvers);
            
            // Limitar el número de maniobras primarias para evitar sobrecarga
            if (maneuvers.Count > 3)
            {
                // Ordenar por prioridad y tomar las 3 más importantes
                maneuvers = maneuvers.OrderByDescending(m => m.Priority).Take(3).ToList();
            }
            
            return maneuvers;
        }
        
        private void AddDefensiveManeuvers(Team team, BattlefieldState state, List<TacticalManeuver> maneuvers)
        {
            // Buscar terreno ventajoso para defender
            if (state.TerrainAdvantage < 0.6f)
            {
                maneuvers.Add(new TacticalManeuver
                {
                    ManeuverType = ManeuverType.SeekHighGround,
                    Priority = 0.9f,
                    TargetFormations = GetAllFormations(team)
                });
            }
            
            // Formar línea defensiva con infantería al frente
            maneuvers.Add(new TacticalManeuver
            {
                ManeuverType = ManeuverType.FormDefensiveLine,
                Priority = 0.85f,
                TargetFormations = new List<Formation> { team.GetFormation(FormationClass.Infantry) }
            });
            
            // Posicionar arqueros detrás de la infantería
            Formation rangedFormation = team.GetFormation(FormationClass.Ranged);
            if (rangedFormation != null && rangedFormation.CountOfUnits > 0)
            {
                maneuvers.Add(new TacticalManeuver
                {
                    ManeuverType = ManeuverType.PositionRangedBehindInfantry,
                    Priority = 0.8f,
                    TargetFormations = new List<Formation> { rangedFormation }
                });
            }
            
            // Mantener caballería en reserva para contraataques
            Formation cavalryFormation = team.GetFormation(FormationClass.Cavalry);
            if (cavalryFormation != null && cavalryFormation.CountOfUnits > 0)
            {
                maneuvers.Add(new TacticalManeuver
                {
                    ManeuverType = ManeuverType.HoldCavalryInReserve,
                    Priority = 0.75f,
                    TargetFormations = new List<Formation> { cavalryFormation }
                });
            }
            
            // Usar formaciones defensivas específicas
            if (HasShieldInfantry(team))
            {
                maneuvers.Add(new TacticalManeuver
                {
                    ManeuverType = ManeuverType.FormShieldWall,
                    Priority = 0.7f,
                    TargetFormations = new List<Formation> { team.GetFormation(FormationClass.Infantry) }
                });
            }
            else if (HasSpearInfantry(team))
            {
                maneuvers.Add(new TacticalManeuver
                {
                    ManeuverType = ManeuverType.FormSpearWall,
                    Priority = 0.7f,
                    TargetFormations = new List<Formation> { team.GetFormation(FormationClass.Infantry) }
                });
            }
        }
        
        private void AddBalancedManeuvers(Team team, BattlefieldState state, List<TacticalManeuver> maneuvers)
        {
            // Avanzar con infantería en formación
            maneuvers.Add(new TacticalManeuver
            {
                ManeuverType = ManeuverType.AdvanceInFormation,
                Priority = 0.8f,
                TargetFormations = new List<Formation> { team.GetFormation(FormationClass.Infantry) }
            });
            
            // Posicionar arqueros para maximizar efectividad
            Formation rangedFormation = team.GetFormation(FormationClass.Ranged);
            if (rangedFormation != null && rangedFormation.CountOfUnits > 0)
            {
                maneuvers.Add(new TacticalManeuver
                {
                    ManeuverType = ManeuverType.PositionRangedForEffectiveness,
                    Priority = 0.75f,
                    TargetFormations = new List<Formation> { rangedFormation }
                });
            }
            
            // Preparar caballería para flanqueo
            Formation cavalryFormation = team.GetFormation(FormationClass.Cavalry);
            if (cavalryFormation != null && cavalryFormation.CountOfUnits > 0)
            {
                maneuvers.Add(new TacticalManeuver
                {
                    ManeuverType = ManeuverType.PrepareCavalryFlank,
                    Priority = 0.7f,
                    TargetFormations = new List<Formation> { cavalryFormation }
                });
            }
            
            // Explorar oportunidades de flanqueo
            if (state.FlankingOpportunities.Count > 0)
            {
                var bestOpportunity = state.FlankingOpportunities.OrderByDescending(o => o.Score).First();
                
                maneuvers.Add(new TacticalManeuver
                {
                    ManeuverType = ManeuverType.ExploitFlankingOpportunity,
                    Priority = 0.85f,
                    TargetFormations = GetSuitableFlankingFormations(team),
                    TargetPosition = bestOpportunity.TargetFormation.CurrentPosition,
                    AdditionalData = bestOpportunity.FlankSide.ToString()
                });
            }
        }
        
        private void AddOffensiveManeuvers(Team team, BattlefieldState state, List<TacticalManeuver> maneuvers)
        {
            // Carga frontal con infantería
            maneuvers.Add(new TacticalManeuver
            {
                ManeuverType = ManeuverType.FrontalAssault,
                Priority = 0.8f,
                TargetFormations = new List<Formation> { team.GetFormation(FormationClass.Infantry) }
            });
            
            // Flanqueo agresivo con caballería
            Formation cavalryFormation = team.GetFormation(FormationClass.Cavalry);
            if (cavalryFormation != null && cavalryFormation.CountOfUnits > 0)
            {
                maneuvers.Add(new TacticalManeuver
                {
                    ManeuverType = ManeuverType.AggressiveCavalryFlank,
                    Priority = 0.9f,
                    TargetFormations = new List<Formation> { cavalryFormation }
                });
            }
            
            // Avance de arqueros para maximizar daño
            Formation rangedFormation = team.GetFormation(FormationClass.Ranged);
            if (rangedFormation != null && rangedFormation.CountOfUnits > 0)
            {
                maneuvers.Add(new TacticalManeuver
                {
                    ManeuverType = ManeuverType.AdvanceRangedForDamage,
                    Priority = 0.75f,
                    TargetFormations = new List<Formation> { rangedFormation }
                });
            }
            
            // Maniobra de envolvimiento si hay suficientes unidades
            if (team.TeamAgents.Count > 50)
            {
                maneuvers.Add(new TacticalManeuver
                {
                    ManeuverType = ManeuverType.EnvelopmentManeuver,
                    Priority = 0.85f,
                    TargetFormations = GetAllFormations(team)
                });
            }
            
            // Ataque coordinado a objetivos vulnerables
            if (state.Opportunities.Any(o => o.OpportunityType == OpportunityType.VulnerableRanged || 
                                           o.OpportunityType == OpportunityType.IsolatedFormation))
            {
                var bestOpportunity = state.Opportunities
                    .Where(o => o.OpportunityType == OpportunityType.VulnerableRanged || 
                              o.OpportunityType == OpportunityType.IsolatedFormation)
                    .OrderByDescending(o => o.Value)
                    .First();
                
                maneuvers.Add(new TacticalManeuver
                {
                    ManeuverType = ManeuverType.CoordinatedStrike,
                    Priority = 0.95f,
                    TargetFormations = GetSuitableStrikeFormations(team),
                    TargetPosition = bestOpportunity.Position
                });
            }
        }
        
        private void AddCultureSpecificManeuvers(Team team, BattlefieldState state, List<TacticalManeuver> maneuvers)
        {
            string teamCulture = GetTeamCulture(team);
            
            switch (teamCulture)
            {
                case "Empire":
                    // Formación en cuña (Embolon)
                    if (team.GetFormation(FormationClass.Infantry)?.CountOfUnits >= 20)
                    {
                        maneuvers.Add(new TacticalManeuver
                        {
                            ManeuverType = ManeuverType.FormEmbolon,
                            Priority = 0.8f,
                            TargetFormations = new List<Formation> { team.GetFormation(FormationClass.Infantry) }
                        });
                    }
                    break;
                    
                case "Sturgia":
                    // Carga de choque con infantería pesada
                    if (team.GetFormation(FormationClass.Infantry)?.CountOfUnits >= 15)
                    {
                        maneuvers.Add(new TacticalManeuver
                        {
                            ManeuverType = ManeuverType.ShockInfantryCharge,
                            Priority = 0.75f,
                            TargetFormations = new List<Formation> { team.GetFormation(FormationClass.Infantry) }
                        });
                    }
                    break;
                    
                case "Vlandia":
                    // Carga de caballería pesada
                    if (team.GetFormation(FormationClass.Cavalry)?.CountOfUnits >= 10)
                    {
                        maneuvers.Add(new TacticalManeuver
                        {
                            ManeuverType = ManeuverType.HeavyCavalryCharge,
                            Priority = 0.85f,
                            TargetFormations = new List<Formation> { team.GetFormation(FormationClass.Cavalry) }
                        });
                    }
                    break;
                    
                case "Khuzait":
                    // Tácticas de hostigamiento con arqueros a caballo
                    if (team.GetFormation(FormationClass.HorseArcher)?.CountOfUnits >= 8)
                    {
                        maneuvers.Add(new TacticalManeuver
                        {
                            ManeuverType = ManeuverType.HorseArcherHarassment,
                            Priority = 0.9f,
                            TargetFormations = new List<Formation> { team.GetFormation(FormationClass.HorseArcher) }
                        });
                    }
                    break;
                    
                case "Aserai":
                    // Emboscada desde múltiples direcciones
                    if (state.TerrainAdvantage > 0.6f)
                    {
                        maneuvers.Add(new TacticalManeuver
                        {
                            ManeuverType = ManeuverType.MultiDirectionalAmbush,
                            Priority = 0.8f,
                            TargetFormations = GetAllFormations(team)
                        });
                    }
                    break;
                    
                case "Battania":
                    // Uso de arqueros en bosques
                    if (team.GetFormation(FormationClass.Ranged)?.CountOfUnits >= 12)
                    {
                        maneuvers.Add(new TacticalManeuver
                        {
                            ManeuverType = ManeuverType.ForestArcherTactics,
                            Priority = 0.85f,
                            TargetFormations = new List<Formation> { team.GetFormation(FormationClass.Ranged) }
                        });
                    }
                    break;
            }
        }
        
        private List<ThreatResponse> DetermineThreatResponses(Team team, List<TacticalThreat> threats)
        {
            List<ThreatResponse> responses = new List<ThreatResponse>();
            
            foreach (var threat in threats.OrderByDescending(t => t.Severity))
            {
                switch (threat.ThreatType)
                {
                    case ThreatType.CavalryCharge:
                        responses.Add(CreateCavalryChargeResponse(team, threat));
                        break;
                        
                    case ThreatType.RangedFire:
                        responses.Add(CreateRangedFireResponse(team, threat));
                        break;
                        
                    case ThreatType.InfantryOverwhelm:
                        responses.Add(CreateInfantryOverwhelmResponse(team, threat));
                        break;
                        
                    case ThreatType.Encirclement:
                        responses.Add(CreateEncirclementResponse(team, threat));
                        break;
                        
                    case ThreatType.Ambush:
                        responses.Add(CreateAmbushResponse(team, threat));
                        break;
                }
            }
            
            return responses;
        }
        
        private ThreatResponse CreateCavalryChargeResponse(Team team, TacticalThreat threat)
        {
            ThreatResponse response = new ThreatResponse
            {
                Threat = threat,
                ResponseType = ThreatResponseType.FormAntiCavalryDefense
            };
            
            // Verificar si hay infantería con lanzas
            if (HasSpearInfantry(team))
            {
                response.TargetFormations = new List<Formation> { team.GetFormation(FormationClass.Infantry) };
                response.Priority = threat.Severity * 1.2f; // Prioridad aumentada para amenazas de caballería
            }
            else
            {
                // Si no hay infantería con lanzas, usar cualquier infantería disponible
                response.TargetFormations = new List<Formation> { team.GetFormation(FormationClass.Infantry) };
                response.Priority = threat.Severity;
                response.ResponseType = ThreatResponseType.FormDefensiveSquare;
            }
            
            return response;
        }
        
        private ThreatResponse CreateRangedFireResponse(Team team, TacticalThreat threat)
        {
            ThreatResponse response = new ThreatResponse
            {
                Threat = threat,
                Priority = threat.Severity
            };
            
            // Verificar si hay infantería con escudos
            if (HasShieldInfantry(team))
            {
                response.ResponseType = ThreatResponseType.RaiseShields;
                response.TargetFormations = new List<Formation> { team.GetFormation(FormationClass.Infantry) };
            }
            else if (team.GetFormation(FormationClass.Cavalry)?.CountOfUnits >= 8)
            {
                // Si no hay escudos pero hay caballería, cargar contra los arqueros
                response.ResponseType = ThreatResponseType.ChargeCavalryAtRanged;
                response.TargetFormations = new List<Formation> { team.GetFormation(FormationClass.Cavalry) };
                response.TargetPosition = threat.Position;
            }
            else
            {
                // Como último recurso, avanzar rápidamente para minimizar el tiempo bajo fuego
                response.ResponseType = ThreatResponseType.AdvanceQuickly;
                response.TargetFormations = GetAllFormations(team);
            }
            
            return response;
        }
        
        private ThreatResponse CreateInfantryOverwhelmResponse(Team team, TacticalThreat threat)
        {
            ThreatResponse response = new ThreatResponse
            {
                Threat = threat,
                Priority = threat.Severity
            };
            
            // Verificar si hay suficiente caballería para flanquear
            if (team.GetFormation(FormationClass.Cavalry)?.CountOfUnits >= 10)
            {
                response.ResponseType = ThreatResponseType.FlankWithCavalry;
                response.TargetFormations = new List<Formation> { team.GetFormation(FormationClass.Cavalry) };
                response.TargetPosition = threat.Position;
            }
            else if (team.GetFormation(FormationClass.Ranged)?.CountOfUnits >= 15)
            {
                // Si hay arqueros, concentrar fuego en la infantería enemiga
                response.ResponseType = ThreatResponseType.ConcentrateRangedFire;
                response.TargetFormations = new List<Formation> { team.GetFormation(FormationClass.Ranged) };
                response.TargetPosition = threat.Position;
            }
            else
            {
                // Como último recurso, formar una línea defensiva sólida
                response.ResponseType = ThreatResponseType.FormDefensiveLine;
                response.TargetFormations = new List<Formation> { team.GetFormation(FormationClass.Infantry) };
            }
            
            return response;
        }
        
        private ThreatResponse CreateEncirclementResponse(Team team, TacticalThreat threat)
        {
            return new ThreatResponse
            {
                Threat = threat,
                ResponseType = ThreatResponseType.FormDefensiveCircle,
                TargetFormations = GetAllFormations(team),
                Priority = threat.Severity * 1.3f // Alta prioridad para evitar encierro
            };
        }
        
        private ThreatResponse CreateAmbushResponse(Team team, TacticalThreat threat)
        {
            return new ThreatResponse
            {
                Threat = threat,
                ResponseType = ThreatResponseType.ReformAndRegroup,
                TargetFormations = GetAllFormations(team),
                Priority = threat.Severity * 1.4f // Máxima prioridad para responder a emboscadas
            };
        }
        
        private List<OpportunityAction> DetermineOpportunityActions(Team team, List<TacticalOpportunity> opportunities)
        {
            List<OpportunityAction> actions = new List<OpportunityAction>();
            
            foreach (var opportunity in opportunities.OrderByDescending(o => o.Value))
            {
                switch (opportunity.OpportunityType)
                {
                    case OpportunityType.VulnerableRanged:
                        actions.Add(CreateVulnerableRangedAction(team, opportunity));
                        break;
                        
                    case OpportunityType.IsolatedFormation:
                        actions.Add(CreateIsolatedFormationAction(team, opportunity));
                        break;
                        
                    case OpportunityType.AdvantageousPosition:
                        actions.Add(CreateAdvantageousPositionAction(team, opportunity));
                        break;
                        
                    case OpportunityType.DivideEnemy:
                        actions.Add(CreateDivideEnemyAction(team, opportunity));
                        break;
                        
                    case OpportunityType.CounterCharge:
                        actions.Add(CreateCounterChargeAction(team, opportunity));
                        break;
                }
            }
            
            return actions;
        }
        
        private OpportunityAction CreateVulnerableRangedAction(Team team, TacticalOpportunity opportunity)
        {
            OpportunityAction action = new OpportunityAction
            {
                Opportunity = opportunity,
                ActionType = OpportunityActionType.StrikeVulnerableRanged,
                Priority = opportunity.Value * 1.2f // Alta prioridad para atacar arqueros vulnerables
            };
            
            // Determinar qué formación es mejor para atacar arqueros
            if (team.GetFormation(FormationClass.Cavalry)?.CountOfUnits >= 5)
            {
                action.TargetFormations = new List<Formation> { team.GetFormation(FormationClass.Cavalry) };
            }
            else if (team.GetFormation(FormationClass.HorseArcher)?.CountOfUnits >= 5)
            {
                action.TargetFormations = new List<Formation> { team.GetFormation(FormationClass.HorseArcher) };
            }
            else
            {
                action.TargetFormations = new List<Formation> { team.GetFormation(FormationClass.Infantry) };
            }
            
            action.TargetPosition = opportunity.Position;
            
            return action;
        }
        
        private OpportunityAction CreateIsolatedFormationAction(Team team, TacticalOpportunity opportunity)
        {
            OpportunityAction action = new OpportunityAction
            {
                Opportunity = opportunity,
                ActionType = OpportunityActionType.SurroundIsolatedFormation,
                Priority = opportunity.Value * 1.1f,
                TargetPosition = opportunity.Position
            };
            
            // Usar todas las formaciones disponibles para rodear al enemigo aislado
            action.TargetFormations = GetAllFormations(team);
            
            return action;
        }
        
        private OpportunityAction CreateAdvantageousPositionAction(Team team, TacticalOpportunity opportunity)
        {
            OpportunityAction action = new OpportunityAction
            {
                Opportunity = opportunity,
                ActionType = OpportunityActionType.OccupyAdvantageousPosition,
                Priority = opportunity.Value,
                TargetPosition = opportunity.Position
            };
            
            // Determinar qué formaciones deberían ocupar la posición ventajosa
            if (team.GetFormation(FormationClass.Ranged)?.CountOfUnits > 0)
            {
                // Priorizar arqueros para posiciones elevadas
                action.TargetFormations = new List<Formation> { team.GetFormation(FormationClass.Ranged) };
                
                // Añadir infantería para proteger a los arqueros
                if (team.GetFormation(FormationClass.Infantry)?.CountOfUnits > 0)
                {
                    action.TargetFormations.Add(team.GetFormation(FormationClass.Infantry));
                }
            }
            else
            {
                // Si no hay arqueros, usar infantería
                action.TargetFormations = new List<Formation> { team.GetFormation(FormationClass.Infantry) };
            }
            
            return action;
        }
        
        private OpportunityAction CreateDivideEnemyAction(Team team, TacticalOpportunity opportunity)
        {
            OpportunityAction action = new OpportunityAction
            {
                Opportunity = opportunity,
                ActionType = OpportunityActionType.ExploitEnemyDivision,
                Priority = opportunity.Value * 1.15f,
                TargetPosition = opportunity.Position
            };
            
            // Usar caballería e infantería para explotar la división
            List<Formation> targetFormations = new List<Formation>();
            
            if (team.GetFormation(FormationClass.Cavalry)?.CountOfUnits > 0)
            {
                targetFormations.Add(team.GetFormation(FormationClass.Cavalry));
            }
            
            if (team.GetFormation(FormationClass.Infantry)?.CountOfUnits > 0)
            {
                targetFormations.Add(team.GetFormation(FormationClass.Infantry));
            }
            
            action.TargetFormations = targetFormations;
            
            return action;
        }
        
        private OpportunityAction CreateCounterChargeAction(Team team, TacticalOpportunity opportunity)
        {
            OpportunityAction action = new OpportunityAction
            {
                Opportunity = opportunity,
                ActionType = OpportunityActionType.ExecuteCounterCharge,
                Priority = opportunity.Value * 1.25f, // Alta prioridad para contracargas
                TargetPosition = opportunity.Position
            };
            
            // Usar caballería para contracargar
            if (team.GetFormation(FormationClass.Cavalry)?.CountOfUnits > 0)
            {
                action.TargetFormations = new List<Formation> { team.GetFormation(FormationClass.Cavalry) };
            }
            else
            {
                // Si no hay caballería, usar infantería
                action.TargetFormations = new List<Formation> { team.GetFormation(FormationClass.Infantry) };
            }
            
            return action;
        }
        
        private Dictionary<FormationClass, FormationAdjustment> DetermineFormationAdjustments(Team team, BattlefieldState state)
        {
            Dictionary<FormationClass, FormationAdjustment> adjustments = new Dictionary<FormationClass, FormationAdjustment>();
            
            // Ajustes para infantería
            Formation infantry = team.GetFormation(FormationClass.Infantry);
            if (infantry != null && infantry.CountOfUnits > 0)
            {
                adjustments[FormationClass.Infantry] = DetermineInfantryAdjustment(team, state);
            }
            
            // Ajustes para arqueros
            Formation ranged = team.GetFormation(FormationClass.Ranged);
            if (ranged != null && ranged.CountOfUnits > 0)
            {
                adjustments[FormationClass.Ranged] = DetermineRangedAdjustment(team, state);
            }
            
            // Ajustes para caballería
            Formation cavalry = team.GetFormation(FormationClass.Cavalry);
            if (cavalry != null && cavalry.CountOfUnits > 0)
            {
                adjustments[FormationClass.Cavalry] = DetermineCavalryAdjustment(team, state);
            }
            
            // Ajustes para arqueros a caballo
            Formation horseArchers = team.GetFormation(FormationClass.HorseArcher);
            if (horseArchers != null && horseArchers.CountOfUnits > 0)
            {
                adjustments[FormationClass.HorseArcher] = DetermineHorseArcherAdjustment(team, state);
            }
            
            return adjustments;
        }
        
        private FormationAdjustment DetermineInfantryAdjustment(Team team, BattlefieldState state)
        {
            FormationAdjustment adjustment = new FormationAdjustment
            {
                FormationClass = FormationClass.Infantry
            };
            
            // Determinar tipo de formación basado en la situación
            if (state.OverallSituation < DEFENSIVE_THRESHOLD)
            {
                // Situación defensiva
                if (HasShieldInfantry(team))
                {
                    adjustment.FormationType = FormationType.ShieldWall;
                    adjustment.Spacing = FormationSpacing.Tight;
                }
                else if (HasSpearInfantry(team))
                {
                    adjustment.FormationType = FormationType.SpearWall;
                    adjustment.Spacing = FormationSpacing.Tight;
                }
                else
                {
                    adjustment.FormationType = FormationType.Line;
                    adjustment.Spacing = FormationSpacing.Tight;
                }
            }
            else if (state.OverallSituation < BALANCED_THRESHOLD)
            {
                // Situación equilibrada
                adjustment.FormationType = FormationType.Line;
                adjustment.Spacing = FormationSpacing.Normal;
            }
            else
            {
                // Situación ofensiva
                string culture = GetTeamCulture(team);
                if (culture == "Empire")
                {
                    adjustment.FormationType = FormationType.Wedge;
                    adjustment.Spacing = FormationSpacing.Normal;
                }
                else if (culture == "Sturgia" || culture == "Vlandia")
                {
                    adjustment.FormationType = FormationType.Line;
                    adjustment.Spacing = FormationSpacing.Loose;
                }
                else
                {
                    adjustment.FormationType = FormationType.Line;
                    adjustment.Spacing = FormationSpacing.Normal;
                }
            }
            
            // Determinar profundidad de la formación
            if (team.GetFormation(FormationClass.Infantry).CountOfUnits > 40)
            {
                adjustment.Depth = 4;
            }
            else if (team.GetFormation(FormationClass.Infantry).CountOfUnits > 20)
            {
                adjustment.Depth = 3;
            }
            else
            {
                adjustment.Depth = 2;
            }
            
            return adjustment;
        }
        
        private FormationAdjustment DetermineRangedAdjustment(Team team, BattlefieldState state)
        {
            FormationAdjustment adjustment = new FormationAdjustment
            {
                FormationClass = FormationClass.Ranged,
                FormationType = FormationType.Line,
                Spacing = FormationSpacing.Loose
            };
            
            // Determinar profundidad basada en el número de arqueros
            if (team.GetFormation(FormationClass.Ranged).CountOfUnits > 30)
            {
                adjustment.Depth = 3;
            }
            else
            {
                adjustment.Depth = 2;
            }
            
            // Ajustar posición relativa a la infantería
            if (state.OverallSituation < DEFENSIVE_THRESHOLD)
            {
                // En situación defensiva, mantener arqueros bien protegidos
                adjustment.RelativePosition = RelativePosition.BehindCenter;
            }
            else if (state.TerrainAdvantage > 0.7f)
            {
                // Si hay ventaja de terreno, posicionar en flancos para mejor ángulo
                adjustment.RelativePosition = RelativePosition.BehindFlank;
            }
            else
            {
                // Posición estándar
                adjustment.RelativePosition = RelativePosition.BehindCenter;
            }
            
            return adjustment;
        }
        
        private FormationAdjustment DetermineCavalryAdjustment(Team team, BattlefieldState state)
        {
            FormationAdjustment adjustment = new FormationAdjustment
            {
                FormationClass = FormationClass.Cavalry
            };
            
            // Determinar tipo de formación basado en la situación
            if (state.OverallSituation < DEFENSIVE_THRESHOLD)
            {
                // Situación defensiva - mantener caballería en reserva
                adjustment.FormationType = FormationType.Column;
                adjustment.Spacing = FormationSpacing.Normal;
                adjustment.RelativePosition = RelativePosition.ProtectedFlank;
            }
            else if (state.OverallSituation < BALANCED_THRESHOLD)
            {
                // Situación equilibrada - preparar para flanqueo
                adjustment.FormationType = FormationType.Line;
                adjustment.Spacing = FormationSpacing.Normal;
                adjustment.RelativePosition = RelativePosition.Flank;
            }
            else
            {
                // Situación ofensiva - preparar para carga
                adjustment.FormationType = FormationType.Wedge;
                adjustment.Spacing = FormationSpacing.Loose;
                adjustment.RelativePosition = RelativePosition.AdvancedFlank;
            }
            
            // Determinar profundidad
            adjustment.Depth = 2; // La caballería generalmente funciona mejor en formaciones poco profundas
            
            return adjustment;
        }
        
        private FormationAdjustment DetermineHorseArcherAdjustment(Team team, BattlefieldState state)
        {
            FormationAdjustment adjustment = new FormationAdjustment
            {
                FormationClass = FormationClass.HorseArcher,
                FormationType = FormationType.Scatter, // Los arqueros a caballo funcionan mejor dispersos
                Spacing = FormationSpacing.VeryLoose,
                Depth = 1 // Una sola línea para máxima movilidad
            };
            
            // Determinar posición relativa
            if (state.OverallSituation < DEFENSIVE_THRESHOLD)
            {
                // En situación defensiva, mantener arqueros a caballo cerca para apoyo
                adjustment.RelativePosition = RelativePosition.ProtectedFlank;
            }
            else
            {
                // En otras situaciones, posicionar para hostigamiento
                adjustment.RelativePosition = RelativePosition.FarFlank;
            }
            
            return adjustment;
        }
        
        private Dictionary<FormationClass, List<TargetPriority>> DetermineTargetPriorities(Team team, BattlefieldState state)
        {
            Dictionary<FormationClass, List<TargetPriority>> priorities = new Dictionary<FormationClass, List<TargetPriority>>();
            
            // Prioridades para infantería
            if (team.GetFormation(FormationClass.Infantry)?.CountOfUnits > 0)
            {
                priorities[FormationClass.Infantry] = DetermineInfantryTargetPriorities(state);
            }
            
            // Prioridades para arqueros
            if (team.GetFormation(FormationClass.Ranged)?.CountOfUnits > 0)
            {
                priorities[FormationClass.Ranged] = DetermineRangedTargetPriorities(state);
            }
            
            // Prioridades para caballería
            if (team.GetFormation(FormationClass.Cavalry)?.CountOfUnits > 0)
            {
                priorities[FormationClass.Cavalry] = DetermineCavalryTargetPriorities(state);
            }
            
            // Prioridades para arqueros a caballo
            if (team.GetFormation(FormationClass.HorseArcher)?.CountOfUnits > 0)
            {
                priorities[FormationClass.HorseArcher] = DetermineHorseArcherTargetPriorities(state);
            }
            
            return priorities;
        }
        
        private List<TargetPriority> DetermineInfantryTargetPriorities(BattlefieldState state)
        {
            List<TargetPriority> priorities = new List<TargetPriority>();
            
            // La infantería generalmente prioriza infantería enemiga
            priorities.Add(new TargetPriority
            {
                TargetFormationClass = FormationClass.Infantry,
                Priority = 1.0f
            });
            
            // Luego arqueros enemigos si están al alcance
            priorities.Add(new TargetPriority
            {
                TargetFormationClass = FormationClass.Ranged,
                Priority = 0.9f
            });
            
            // Caballería enemiga es más difícil de enfrentar para la infantería
            priorities.Add(new TargetPriority
            {
                TargetFormationClass = FormationClass.Cavalry,
                Priority = 0.7f
            });
            
            // Arqueros a caballo son los más difíciles de alcanzar
            priorities.Add(new TargetPriority
            {
                TargetFormationClass = FormationClass.HorseArcher,
                Priority = 0.5f
            });
            
            return priorities;
        }
        
        private List<TargetPriority> DetermineRangedTargetPriorities(BattlefieldState state)
        {
            List<TargetPriority> priorities = new List<TargetPriority>();
            
            // Los arqueros priorizan otros arqueros enemigos
            priorities.Add(new TargetPriority
            {
                TargetFormationClass = FormationClass.Ranged,
                Priority = 1.0f
            });
            
            // Luego caballería que puede ser una amenaza
            priorities.Add(new TargetPriority
            {
                TargetFormationClass = FormationClass.Cavalry,
                Priority = 0.9f
            });
            
            // Infantería enemiga que avanza
            priorities.Add(new TargetPriority
            {
                TargetFormationClass = FormationClass.Infantry,
                Priority = 0.8f
            });
            
            // Arqueros a caballo son difíciles de acertar
            priorities.Add(new TargetPriority
            {
                TargetFormationClass = FormationClass.HorseArcher,
                Priority = 0.7f
            });
            
            return priorities;
        }
        
        private List<TargetPriority> DetermineCavalryTargetPriorities(BattlefieldState state)
        {
            List<TargetPriority> priorities = new List<TargetPriority>();
            
            // La caballería prioriza arqueros enemigos
            priorities.Add(new TargetPriority
            {
                TargetFormationClass = FormationClass.Ranged,
                Priority = 1.0f
            });
            
            // Luego infantería enemiga, especialmente si no tiene lanzas
            priorities.Add(new TargetPriority
            {
                TargetFormationClass = FormationClass.Infantry,
                Priority = 0.8f
            });
            
            // Otras unidades de caballería
            priorities.Add(new TargetPriority
            {
                TargetFormationClass = FormationClass.Cavalry,
                Priority = 0.7f
            });
            
            // Arqueros a caballo
            priorities.Add(new TargetPriority
            {
                TargetFormationClass = FormationClass.HorseArcher,
                Priority = 0.6f
            });
            
            return priorities;
        }
        
        private List<TargetPriority> DetermineHorseArcherTargetPriorities(BattlefieldState state)
        {
            List<TargetPriority> priorities = new List<TargetPriority>();
            
            // Los arqueros a caballo priorizan infantería enemiga
            priorities.Add(new TargetPriority
            {
                TargetFormationClass = FormationClass.Infantry,
                Priority = 1.0f
            });
            
            // Luego arqueros enemigos
            priorities.Add(new TargetPriority
            {
                TargetFormationClass = FormationClass.Ranged,
                Priority = 0.9f
            });
            
            // Caballería enemiga
            priorities.Add(new TargetPriority
            {
                TargetFormationClass = FormationClass.Cavalry,
                Priority = 0.7f
            });
            
            // Otros arqueros a caballo
            priorities.Add(new TargetPriority
            {
                TargetFormationClass = FormationClass.HorseArcher,
                Priority = 0.5f
            });
            
            return priorities;
        }
        
        private void AddTacticalVariation(TacticalDecisions decisions, float commanderSkill)
        {
            // Añadir variación basada en la habilidad del comandante
            // Comandantes más hábiles tienen más variación táctica
            
            float variationFactor = commanderSkill * 0.2f;
            
            // Añadir variación a las prioridades de maniobras
            foreach (var maneuver in decisions.PrimaryManeuvers)
            {
                float variation = (float)(_random.NextDouble() * 2 - 1) * variationFactor;
                maneuver.Priority = MathF.Max(0.1f, MathF.Min(1.0f, maneuver.Priority + variation));
            }
            
            // Añadir variación a las respuestas a amenazas
            foreach (var response in decisions.ThreatResponses)
            {
                float variation = (float)(_random.NextDouble() * 2 - 1) * variationFactor;
                response.Priority = MathF.Max(0.1f, MathF.Min(1.0f, response.Priority + variation));
            }
            
            // Añadir variación a las acciones de oportunidad
            foreach (var action in decisions.OpportunityActions)
            {
                float variation = (float)(_random.NextDouble() * 2 - 1) * variationFactor;
                action.Priority = MathF.Max(0.1f, MathF.Min(1.0f, action.Priority + variation));
            }
            
            // Ocasionalmente añadir una maniobra inesperada
            if (_random.NextDouble() < commanderSkill * 0.3f)
            {
                AddUnexpectedManeuver(decisions);
            }
        }
        
        private void AddUnexpectedManeuver(TacticalDecisions decisions)
        {
            // Lista de maniobras inesperadas que pueden añadir variedad
            List<ManeuverType> unexpectedManeuvers = new List<ManeuverType>
            {
                ManeuverType.FeignedRetreat,
                ManeuverType.SuddenFormationChange,
                ManeuverType.HiddenReserveDeployment,
                ManeuverType.DistractionManeuver,
                ManeuverType.FalseFlankingMovement
            };
            
            // Seleccionar una maniobra aleatoria
            ManeuverType selectedManeuver = unexpectedManeuvers[_random.Next(unexpectedManeuvers.Count)];
            
            // Añadir la maniobra a las decisiones
            decisions.PrimaryManeuvers.Add(new TacticalManeuver
            {
                ManeuverType = selectedManeuver,
                Priority = 0.7f + (float)_random.NextDouble() * 0.2f,
                TargetFormations = new List<Formation>() // Se asignarán formaciones en la ejecución
            });
        }
        
        // Métodos auxiliares
        
        private List<Formation> GetAllFormations(Team team)
        {
            return team.FormationsIncludingEmpty
                .Where(f => f.CountOfUnits > 0)
                .ToList();
        }
        
        private List<Formation> GetSuitableFlankingFormations(Team team)
        {
            List<Formation> formations = new List<Formation>();
            
            // Caballería es ideal para flanqueo
            if (team.GetFormation(FormationClass.Cavalry)?.CountOfUnits >= 5)
            {
                formations.Add(team.GetFormation(FormationClass.Cavalry));
            }
            
            // Arqueros a caballo también son buenos para flanqueo
            if (team.GetFormation(FormationClass.HorseArcher)?.CountOfUnits >= 5)
            {
                formations.Add(team.GetFormation(FormationClass.HorseArcher));
            }
            
            // Si no hay unidades montadas, usar infantería ligera
            if (formations.Count == 0 && team.GetFormation(FormationClass.Infantry)?.CountOfUnits >= 10)
            {
                formations.Add(team.GetFormation(FormationClass.Infantry));
            }
            
            return formations;
        }
        
        private List<Formation> GetSuitableStrikeFormations(Team team)
        {
            List<Formation> formations = new List<Formation>();
            
            // Para ataques coordinados, usar combinación de unidades
            if (team.GetFormation(FormationClass.Cavalry)?.CountOfUnits >= 5)
            {
                formations.Add(team.GetFormation(FormationClass.Cavalry));
            }
            
            if (team.GetFormation(FormationClass.Infantry)?.CountOfUnits >= 10)
            {
                formations.Add(team.GetFormation(FormationClass.Infantry));
            }
            
            return formations;
        }
        
        private bool HasShieldInfantry(Team team)
        {
            // Implementación simplificada - en el juego real se verificaría
            // si la infantería tiene escudos basado en el equipamiento
            return true;
        }
        
        private bool HasSpearInfantry(Team team)
        {
            // Implementación simplificada - en el juego real se verificaría
            // si la infantería tiene lanzas basado en el equipamiento
            return true;
        }
        
        private string GetTeamCulture(Team team)
        {
            // Implementación simplificada - en el juego real se obtendría de los datos del equipo
            if (team.TeamIndex == 1)
                return "Empire";
            else if (team.TeamIndex == 2)
                return "Vlandia";
                
            return "Unknown";
        }
    }
    
    public class TacticalDecisions
    {
        public TacticalPosture GeneralPosture { get; set; }
        public List<TacticalManeuver> PrimaryManeuvers { get; set; } = new List<TacticalManeuver>();
        public List<ThreatResponse> ThreatResponses { get; set; } = new List<ThreatResponse>();
        public List<OpportunityAction> OpportunityActions { get; set; } = new List<OpportunityAction>();
        public Dictionary<FormationClass, FormationAdjustment> FormationAdjustments { get; set; } = new Dictionary<FormationClass, FormationAdjustment>();
        public Dictionary<FormationClass, List<TargetPriority>> TargetPriorities { get; set; } = new Dictionary<FormationClass, List<TargetPriority>>();
    }
    
    public enum TacticalPosture
    {
        Defensive,
        Balanced,
        Offensive
    }
    
    public class TacticalManeuver
    {
        public ManeuverType ManeuverType { get; set; }
        public float Priority { get; set; }
        public List<Formation> TargetFormations { get; set; } = new List<Formation>();
        public Vec3 TargetPosition { get; set; } = Vec3.Zero;
        public string AdditionalData { get; set; } = "";
    }
    
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
    
    public class ThreatResponse
    {
        public TacticalThreat Threat { get; set; }
        public ThreatResponseType ResponseType { get; set; }
        public float Priority { get; set; }
        public List<Formation> TargetFormations { get; set; } = new List<Formation>();
        public Vec3 TargetPosition { get; set; } = Vec3.Zero;
    }
    
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
    
    public class OpportunityAction
    {
        public TacticalOpportunity Opportunity { get; set; }
        public OpportunityActionType ActionType { get; set; }
        public float Priority { get; set; }
        public List<Formation> TargetFormations { get; set; } = new List<Formation>();
        public Vec3 TargetPosition { get; set; } = Vec3.Zero;
    }
    
    public enum OpportunityActionType
    {
        StrikeVulnerableRanged,
        SurroundIsolatedFormation,
        OccupyAdvantageousPosition,
        ExploitEnemyDivision,
        ExecuteCounterCharge
    }
    
    public class FormationAdjustment
    {
        public FormationClass FormationClass { get; set; }
        public FormationType FormationType { get; set; }
        public FormationSpacing Spacing { get; set; }
        public int Depth { get; set; }
        public RelativePosition RelativePosition { get; set; }
    }
    
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
    
    public enum FormationSpacing
    {
        VeryTight,
        Tight,
        Normal,
        Loose,
        VeryLoose
    }
    
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
    
    public class TargetPriority
    {
        public FormationClass TargetFormationClass { get; set; }
        public float Priority { get; set; }
    }
}
