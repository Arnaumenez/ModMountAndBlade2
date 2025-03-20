using System;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using TaleWorlds.Core;
using TaleWorlds.Engine;

namespace TacticalGenius.AI
{
    public class BattlefieldEvaluator
    {
        private readonly Mission _mission;
        private readonly TacticalAnalyzer _tacticalAnalyzer;
        
        // Implementación interna de TerrainAnalysis en lugar de usar una clase externa
        private class TerrainAnalysis
        {
            public Vec2 AnalyzeTerrainForAdvantageousPosition(Formation formation)
            {
                // Implementación simplificada que devuelve la posición actual
                return formation.OrderPosition.AsVec2;
            }
            
            public bool IsHighGround(Vec2 position, float radius = 10f)
            {
                // Implementación simplificada
                return false;
            }
            
            public bool IsDefensiblePosition(Vec2 position)
            {
                // Implementación simplificada
                return false;
            }
            
            public float GetTerrainAdvantage(Vec2 position, Vec2 enemyPosition)
            {
                // Implementación simplificada
                return 0f;
            }
        }
        
        private readonly TerrainAnalysis _terrainAnalysis = new TerrainAnalysis();

        public BattlefieldEvaluator(Mission mission, TacticalAnalyzer tacticalAnalyzer)
        {
            _mission = mission;
            _tacticalAnalyzer = tacticalAnalyzer;
        }

        public float EvaluatePositionalAdvantage(Formation formation, Formation enemyFormation)
        {
            float advantage = 0f;
            
            // Evaluar ventaja de altura
            Vec2 formationPos = formation.OrderPosition.AsVec2;
            Vec2 enemyPos = enemyFormation.OrderPosition.AsVec2;
            
            advantage += _terrainAnalysis.GetTerrainAdvantage(formationPos, enemyPos);
            
            // Evaluar ventaja de distancia según tipo de unidad
            float distanceAdvantage = EvaluateDistanceAdvantage(formation, enemyFormation);
            advantage += distanceAdvantage;
            
            // Evaluar ventaja de flanqueo
            float flankAdvantage = EvaluateFlankingAdvantage(formation, enemyFormation);
            advantage += flankAdvantage;
            
            return advantage;
        }

        public float EvaluateDistanceAdvantage(Formation formation, Formation enemyFormation)
        {
            float advantage = 0f;
            float distance = formation.OrderPosition.Distance(enemyFormation.OrderPosition);
            
            // Los arqueros prefieren distancia
            if (formation.QuerySystem.IsRangedFormation)
            {
                advantage += Math.Max(0, Math.Min(distance - 20f, 50f)) / 10f;
            }
            // La infantería prefiere distancia media
            else if (formation.QuerySystem.IsInfantryFormation)
            {
                advantage += Math.Max(0, 10f - Math.Abs(distance - 30f)) / 5f;
            }
            // La caballería prefiere distancia para cargar
            else if (formation.QuerySystem.IsCavalryFormation)
            {
                advantage += Math.Max(0, Math.Min(distance - 30f, 80f)) / 20f;
            }
            
            return advantage;
        }

        public float EvaluateFlankingAdvantage(Formation formation, Formation enemyFormation)
        {
            Vec2 formationFacing = formation.Direction.AsVec2;
            Vec2 enemyFacing = enemyFormation.Direction.AsVec2;
            Vec2 formationToEnemy = (enemyFormation.OrderPosition.AsVec2 - formation.OrderPosition.AsVec2).Normalized();
            
            // Calcular ángulo entre la dirección de la formación enemiga y la dirección hacia nuestra formación
            float dotProduct = Vec2.DotProduct(enemyFacing, -formationToEnemy);
            float flankingFactor = (1f - dotProduct) / 2f; // 0 = frente, 1 = retaguardia
            
            return flankingFactor * 2f; // Multiplicador para dar más peso al flanqueo
        }

        public Vec2 FindAdvantageousPosition(Formation formation, List<Formation> enemyFormations)
        {
            // Buscar posición ventajosa basada en el terreno y las posiciones enemigas
            Vec2 bestPosition = _terrainAnalysis.AnalyzeTerrainForAdvantageousPosition(formation);
            
            // Implementación simplificada
            return bestPosition;
        }

        public bool IsPositionExposed(Vec2 position, List<Formation> enemyFormations)
        {
            // Verificar si la posición está expuesta a ataques enemigos
            foreach (Formation enemyFormation in enemyFormations)
            {
                float distance = (enemyFormation.OrderPosition.AsVec2 - position).Length;
                if (distance < 30f && enemyFormation.QuerySystem.IsRangedFormation)
                {
                    return true;
                }
            }
            
            return false;
        }

        public float EvaluateFormationStrength(Formation formation)
        {
            float strength = formation.CountOfUnits;
            
            // Ajustar por tipo de unidad
            if (formation.QuerySystem.IsRangedFormation)
            {
                strength *= 0.8f; // Los arqueros son más débiles en combate directo
            }
            else if (formation.QuerySystem.IsCavalryFormation)
            {
                strength *= 1.5f; // La caballería es más fuerte
            }
            
            // Ajustar por moral
            float averageMorale = 0f;
            int count = 0;
            
            foreach (Agent agent in formation.GetUnits())
            {
                if (agent.IsHuman)
                {
                    averageMorale += agent.GetMorale();
                    count++;
                }
            }
            
            if (count > 0)
            {
                averageMorale /= count;
                strength *= (0.5f + averageMorale / 2f); // La moral afecta a la fuerza efectiva
            }
            
            return strength;
        }
    }
}
