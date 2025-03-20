using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Engine;
using TacticalGenius.AI;

namespace TacticalGenius.Utils
{
    public class FormationUtils
    {
        private Mission _mission;
        private Scene _scene;
        private TerrainAnalyzer _terrainAnalyzer;
        
        public void Initialize(Mission mission)
        {
            _mission = mission;
            _scene = mission.Scene;
            _terrainAnalyzer = new TerrainAnalyzer();
            _terrainAnalyzer.Initialize(mission);
        }
        
        /// <summary>
        /// Calcula el centro de todas las formaciones de un equipo
        /// </summary>
        public Vec3 CalculateTeamCenter(Team team)
        {
            if (team == null || !team.IsValid)
                return Vec3.Zero;
                
            List<Vec3> formationPositions = new List<Vec3>();
            int totalUnits = 0;
            
            for (int i = 0; i < (int)FormationClass.NumberOfRegularFormations; i++)
            {
                Formation formation = team.GetFormation((FormationClass)i);
                if (formation != null && formation.CountOfUnits > 0)
                {
                    formationPositions.Add(formation.CurrentPosition * formation.CountOfUnits);
                    totalUnits += formation.CountOfUnits;
                }
            }
            
            if (totalUnits == 0)
                return Vec3.Zero;
                
            Vec3 center = Vec3.Zero;
            foreach (Vec3 position in formationPositions)
            {
                center += position;
            }
            
            return center / totalUnits;
        }
        
        /// <summary>
        /// Obtiene la dirección hacia el enemigo más cercano
        /// </summary>
        public Vec2 GetDirectionToNearestEnemy(Formation formation)
        {
            if (formation == null || formation.Team == null || !formation.Team.IsValid)
                return Vec2.Zero;
                
            Formation nearestEnemyFormation = GetNearestEnemyFormation(formation);
            if (nearestEnemyFormation == null)
                return Vec2.Zero;
                
            Vec3 directionToEnemy = nearestEnemyFormation.CurrentPosition - formation.CurrentPosition;
            return new Vec2(directionToEnemy.x, directionToEnemy.z).Normalized();
        }
        
        /// <summary>
        /// Obtiene la formación enemiga más cercana
        /// </summary>
        public Formation GetNearestEnemyFormation(Formation formation)
        {
            if (formation == null || formation.Team == null || !formation.Team.IsValid)
                return null;
                
            Formation nearestFormation = null;
            float nearestDistance = float.MaxValue;
            
            foreach (Team enemyTeam in _mission.Teams)
            {
                if (!enemyTeam.IsValid || !enemyTeam.IsEnemyOf(formation.Team))
                    continue;
                    
                for (int i = 0; i < (int)FormationClass.NumberOfRegularFormations; i++)
                {
                    Formation enemyFormation = enemyTeam.GetFormation((FormationClass)i);
                    if (enemyFormation == null || enemyFormation.CountOfUnits == 0)
                        continue;
                        
                    float distance = Vec3.Distance(formation.CurrentPosition, enemyFormation.CurrentPosition);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestFormation = enemyFormation;
                    }
                }
            }
            
            return nearestFormation;
        }
        
        /// <summary>
        /// Obtiene la formación enemiga más vulnerable (basada en tipo y posición)
        /// </summary>
        public Formation GetMostVulnerableEnemyFormation(Formation formation)
        {
            if (formation == null || formation.Team == null || !formation.Team.IsValid)
                return null;
                
            Formation mostVulnerableFormation = null;
            float highestVulnerabilityScore = 0f;
            
            foreach (Team enemyTeam in _mission.Teams)
            {
                if (!enemyTeam.IsValid || !enemyTeam.IsEnemyOf(formation.Team))
                    continue;
                    
                for (int i = 0; i < (int)FormationClass.NumberOfRegularFormations; i++)
                {
                    Formation enemyFormation = enemyTeam.GetFormation((FormationClass)i);
                    if (enemyFormation == null || enemyFormation.CountOfUnits == 0)
                        continue;
                        
                    float vulnerabilityScore = CalculateFormationVulnerability(formation, enemyFormation);
                    if (vulnerabilityScore > highestVulnerabilityScore)
                    {
                        highestVulnerabilityScore = vulnerabilityScore;
                        mostVulnerableFormation = enemyFormation;
                    }
                }
            }
            
            return mostVulnerableFormation;
        }
        
        /// <summary>
        /// Calcula la vulnerabilidad de una formación enemiga frente a la formación actual
        /// </summary>
        private float CalculateFormationVulnerability(Formation attackerFormation, Formation targetFormation)
        {
            float vulnerabilityScore = 0f;
            
            // Factores de vulnerabilidad basados en tipo de formación
            if (attackerFormation.FormationIndex == FormationClass.Cavalry)
            {
                // Caballería es efectiva contra arqueros
                if (targetFormation.FormationIndex == FormationClass.Ranged)
                {
                    vulnerabilityScore += 2.0f;
                }
                // Caballería es menos efectiva contra infantería con lanzas
                else if (targetFormation.FormationIndex == FormationClass.Infantry)
                {
                    vulnerabilityScore += 0.5f;
                }
                else
                {
                    vulnerabilityScore += 1.0f;
                }
            }
            else if (attackerFormation.FormationIndex == FormationClass.Infantry)
            {
                // Infantería es efectiva contra caballería
                if (targetFormation.FormationIndex == FormationClass.Cavalry)
                {
                    vulnerabilityScore += 1.5f;
                }
                // Infantería es vulnerable a arqueros a distancia
                else if (targetFormation.FormationIndex == FormationClass.Ranged)
                {
                    vulnerabilityScore += 1.0f;
                }
                else
                {
                    vulnerabilityScore += 1.0f;
                }
            }
            else if (attackerFormation.FormationIndex == FormationClass.Ranged)
            {
                // Arqueros son efectivos contra infantería
                if (targetFormation.FormationIndex == FormationClass.Infantry)
                {
                    vulnerabilityScore += 1.5f;
                }
                // Arqueros son vulnerables a caballería
                else if (targetFormation.FormationIndex == FormationClass.Cavalry)
                {
                    vulnerabilityScore += 0.5f;
                }
                else
                {
                    vulnerabilityScore += 1.0f;
                }
            }
            
            // Factores de vulnerabilidad basados en posición
            
            // Distancia (formaciones más cercanas son más fáciles de atacar)
            float distance = Vec3.Distance(attackerFormation.CurrentPosition, targetFormation.CurrentPosition);
            float distanceFactor = 1.0f - Math.Min(distance / 200f, 1.0f); // Normalizado a 0-1
            vulnerabilityScore += distanceFactor;
            
            // Aislamiento (formaciones aisladas son más vulnerables)
            float isolationFactor = CalculateIsolationFactor(targetFormation);
            vulnerabilityScore += isolationFactor * 1.5f;
            
            // Terreno (formaciones en terreno desfavorable son más vulnerables)
            float terrainFactor = CalculateTerrainVulnerabilityFactor(targetFormation);
            vulnerabilityScore += terrainFactor;
            
            // Formación (algunas formaciones son más vulnerables a ciertos ataques)
            float formationFactor = CalculateFormationVulnerabilityFactor(targetFormation, attackerFormation);
            vulnerabilityScore += formationFactor;
            
            return vulnerabilityScore;
        }
        
        /// <summary>
        /// Calcula el factor de aislamiento de una formación (qué tan lejos está de apoyo aliado)
        /// </summary>
        private float CalculateIsolationFactor(Formation formation)
        {
            if (formation == null || formation.Team == null)
                return 0f;
                
            float minDistanceToAlly = float.MaxValue;
            
            for (int i = 0; i < (int)FormationClass.NumberOfRegularFormations; i++)
            {
                Formation allyFormation = formation.Team.GetFormation((FormationClass)i);
                if (allyFormation == null || allyFormation == formation || allyFormation.CountOfUnits == 0)
                    continue;
                    
                float distance = Vec3.Distance(formation.CurrentPosition, allyFormation.CurrentPosition);
                minDistanceToAlly = Math.Min(minDistanceToAlly, distance);
            }
            
            // Normalizar a 0-1, donde 1 es completamente aislado
            return Math.Min(minDistanceToAlly / 150f, 1.0f);
        }
        
        /// <summary>
        /// Calcula el factor de vulnerabilidad basado en el terreno
        /// </summary>
        private float CalculateTerrainVulnerabilityFactor(Formation formation)
        {
            // En una implementación real, esto analizaría el terreno bajo la formación
            // Aquí usamos una aproximación basada en la altura relativa
            
            float terrainHeight = _terrainAnalyzer.GetAverageHeightAtPosition(formation.CurrentPosition);
            float surroundingHeight = _terrainAnalyzer.GetAverageSurroundingHeight(formation.CurrentPosition, 20f);
            
            // Formaciones en terreno bajo son más vulnerables
            if (terrainHeight < surroundingHeight)
            {
                return 0.5f;
            }
            // Formaciones en terreno alto son menos vulnerables
            else if (terrainHeight > surroundingHeight)
            {
                return -0.3f;
            }
            
            return 0f;
        }
        
        /// <summary>
        /// Calcula el factor de vulnerabilidad basado en la formación actual
        /// </summary>
        private float CalculateFormationVulnerabilityFactor(Formation targetFormation, Formation attackerFormation)
        {
            float factor = 0f;
            
            // Formaciones en línea son vulnerables a flanqueos
            if (targetFormation.ArrangementOrder == ArrangementOrder.Line)
            {
                // Verificar si el atacante está en posición de flanqueo
                Vec3 targetDirection = targetFormation.Direction;
                Vec3 attackDirection = (targetFormation.CurrentPosition - attackerFormation.CurrentPosition).NormalizedCopy();
                
                float dot = Vec3.DotProduct(targetDirection, attackDirection);
                if (Math.Abs(dot) < 0.5f) // Ataque desde el flanco
                {
                    factor += 0.7f;
                }
            }
            // Formaciones en columna son vulnerables a ataques frontales
            else if (targetFormation.ArrangementOrder == ArrangementOrder.Column)
            {
                Vec3 targetDirection = targetFormation.Direction;
                Vec3 attackDirection = (targetFormation.CurrentPosition - attackerFormation.CurrentPosition).NormalizedCopy();
                
                float dot = Vec3.DotProduct(targetDirection, attackDirection);
                if (Math.Abs(dot) > 0.7f) // Ataque frontal o trasero
                {
                    factor += 0.5f;
                }
            }
            
            // Formaciones preparadas para carga son menos vulnerables a caballería
            if (targetFormation.FormOrder == FormOrder.BraceForCharge && 
                attackerFormation.FormationIndex == FormationClass.Cavalry)
            {
                factor -= 0.8f;
            }
            
            // Formaciones en muro de escudos son menos vulnerables a proyectiles
            if (targetFormation.FormOrder == FormOrder.ShieldWall && 
                attackerFormation.FormationIndex == FormationClass.Ranged)
            {
                factor -= 0.6f;
            }
            
            return factor;
        }
        
        /// <summary>
        /// Encuentra terreno elevado cercano para posicionar formaciones
        /// </summary>
        public Vec3 FindHighGround(Team team)
        {
            if (team == null)
                return Vec3.Zero;
                
            // Obtener centro del equipo
            Vec3 teamCenter = CalculateTeamCenter(team);
            if (teamCenter == Vec3.Zero)
                return Vec3.Zero;
                
            // Obtener dirección hacia el enemigo
            Vec3 enemyCenter = Vec3.Zero;
            int enemyCount = 0;
            
            foreach (Team enemyTeam in _mission.Teams)
            {
                if (!enemyTeam.IsValid || !enemyTeam.IsEnemyOf(team))
                    continue;
                    
                Vec3 currentEnemyCenter = CalculateTeamCenter(enemyTeam);
                if (currentEnemyCenter != Vec3.Zero)
                {
                    enemyCenter += currentEnemyCenter;
                    enemyCount++;
                }
            }
            
            if (enemyCount == 0)
                return Vec3.Zero;
                
            enemyCenter /= enemyCount;
            
            // Calcular dirección hacia el enemigo
            Vec3 directionToEnemy = (enemyCenter - teamCenter).NormalizedCopy();
            
            // Buscar terreno elevado en un radio alrededor del centro del equipo
            return _terrainAnalyzer.FindHighestPointInDirection(teamCenter, directionToEnemy, 100f);
        }
        
        /// <summary>
        /// Encuentra una posición elevada cercana a la formación
        /// </summary>
        public Vec3 FindNearbyElevatedPosition(Formation formation)
        {
            if (formation == null)
                return Vec3.Zero;
                
            Vec3 formationPosition = formation.CurrentPosition;
            
            // Obtener dirección hacia el enemigo
            Vec2 enemyDirection = GetDirectionToNearestEnemy(formation);
            if (enemyDirection == Vec2.Zero)
                return Vec3.Zero;
                
            // Buscar terreno elevado en dirección al enemigo
            return _terrainAnalyzer.FindHighestPointInDirection(formationPosition, enemyDirection.ToVec3(), 50f);
        }
        
        /// <summary>
        /// Encuentra una posición con cobertura cercana a la formación
        /// </summary>
        public Vec3 FindNearbyCoverPosition(Formation formation)
        {
            if (formation == null)
                return Vec3.Zero;
                
            // En una implementación real, esto buscaría árboles, rocas, etc.
            // Aquí simulamos encontrando una posición "boscosa" cercana
            
            Vec3 formationPosition = formation.CurrentPosition;
            
            // Obtener dirección hacia el enemigo
            Vec2 enemyDirection = GetDirectionToNearestEnemy(formation);
            if (enemyDirection == Vec2.Zero)
                return Vec3.Zero;
                
            // Simular encontrar cobertura en dirección opuesta al enemigo
            Vec3 coverDirection = -enemyDirection.ToVec3();
            
            // Buscar posición con cobertura
            return formationPosition + (coverDirection * 30f);
        }
    }
    
    public class TerrainAnalyzer
    {
        private Mission _mission;
        private Scene _scene;
        
        public void Initialize(Mission mission)
        {
            _mission = mission;
            _scene = mission.Scene;
        }
        
        /// <summary>
        /// Obtiene la altura del terreno en una posición específica
        /// </summary>
        public float GetHeightAtPosition(Vec3 position)
        {
            // En una implementación real, esto usaría la API del juego para obtener la altura
            // Aquí simulamos con una aproximación
            
            return position.y;
        }
        
        /// <summary>
        /// Obtiene la altura promedio del terreno en una posición
        /// </summary>
        public float GetAverageHeightAtPosition(Vec3 position)
        {
            // En una implementación real, esto promediaría múltiples puntos
            // Aquí simulamos con una aproximación
            
            return position.y;
        }
        
        /// <summary>
        /// Obtiene la altura promedio del terreno circundante
        /// </summary>
        public float GetAverageSurroundingHeight(Vec3 position, float radius)
        {
            // En una implementación real, esto promediaría múltiples puntos en un radio
            // Aquí simulamos con una aproximación
            
            return position.y - 0.5f; // Simular que el terreno circundante es ligeramente más bajo
        }
        
        /// <summary>
        /// Encuentra el punto más alto en una dirección específica
        /// </summary>
        public Vec3 FindHighestPointInDirection(Vec3 startPosition, Vec3 direction, float maxDistance)
        {
            // En una implementación real, esto buscaría el punto más alto en la dirección
            // Aquí simulamos con una aproximación
            
            Vec3 highestPoint = startPosition;
            float highestElevation = startPosition.y;
            
            // Simular búsqueda de punto más alto
            for (float distance = 10f; distance <= maxDistance; distance += 10f)
            {
                Vec3 testPosition = startPosition + (direction * distance);
                
                // Simular variación de altura
                float elevation = startPosition.y + (float)Math.Sin(distance * 0.1f) * 2f;
                
                if (elevation > highestElevation)
                {
                    highestElevation = elevation;
                    highestPoint = new Vec3(testPosition.x, elevation, testPosition.z);
                }
            }
            
            return highestPoint;
        }
    }
    
    public class BattleStateTracker
    {
        private Dictionary<Team, List<Formation>> _feignedRetreatFormations = new Dictionary<Team, List<Formation>>();
        private Dictionary<Formation, Vec3> _falseFlankingMovements = new Dictionary<Formation, Vec3>();
        
        public void Initialize(Mission mission)
        {
            // Inicializar el rastreador de estado de batalla
        }
        
        /// <summary>
        /// Registra una maniobra de retirada fingida
        /// </summary>
        public void RegisterFeignedRetreat(Team team, List<Formation> formations)
        {
            if (!_feignedRetreatFormations.ContainsKey(team))
            {
                _feignedRetreatFormations[team] = new List<Formation>();
            }
            
            _feignedRetreatFormations[team].AddRange(formations);
        }
        
        /// <summary>
        /// Registra un movimiento de flanqueo falso
        /// </summary>
        public void RegisterFalseFlankingMovement(Formation formation, Vec3 targetPosition)
        {
            _falseFlankingMovements[formation] = targetPosition;
        }
        
        /// <summary>
        /// Verifica si una formación está en retirada fingida
        /// </summary>
        public bool IsInFeignedRetreat(Formation formation)
        {
            foreach (var pair in _feignedRetreatFormations)
            {
                if (pair.Value.Contains(formation))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Verifica si una formación está en movimiento de flanqueo falso
        /// </summary>
        public bool IsInFalseFlankingMovement(Formation formation)
        {
            return _falseFlankingMovements.ContainsKey(formation);
        }
        
        /// <summary>
        /// Obtiene la posición objetivo de un movimiento de flanqueo falso
        /// </summary>
        public Vec3 GetFalseFlankingTargetPosition(Formation formation)
        {
            if (_falseFlankingMovements.ContainsKey(formation))
            {
                return _falseFlankingMovements[formation];
            }
            
            return Vec3.Zero;
        }
    }
}
