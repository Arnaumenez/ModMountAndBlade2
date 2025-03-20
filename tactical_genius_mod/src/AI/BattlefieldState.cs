using System;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using TaleWorlds.Core;

namespace TacticalGenius.AI
{
    public class BattlefieldState
    {
        public Mission Mission { get; private set; }
        public Team PlayerTeam { get; private set; }
        public Team EnemyTeam { get; private set; }
        public List<Formation> PlayerFormations { get; private set; }
        public List<Formation> EnemyFormations { get; private set; }
        public Vec2 BattleCenterPosition { get; private set; }
        public float BattlefieldWidth { get; private set; }
        public float BattlefieldDepth { get; private set; }
        public float TerrainDifficulty { get; private set; }
        public bool IsPlayerAttacking { get; private set; }
        public float BattleProgress { get; private set; } // 0.0 = inicio, 1.0 = final
        public float PlayerAdvantage { get; private set; } // -1.0 = desventaja total, 1.0 = ventaja total
        public List<TacticalThreat> CurrentThreats { get; private set; }
        public List<TacticalOpportunity> CurrentOpportunities { get; private set; }
        public WeatherType CurrentWeather { get; private set; }
        public TimeOfDay TimeOfDay { get; private set; }

        public BattlefieldState(Mission mission)
        {
            Mission = mission;
            PlayerFormations = new List<Formation>();
            EnemyFormations = new List<Formation>();
            CurrentThreats = new List<TacticalThreat>();
            CurrentOpportunities = new List<TacticalOpportunity>();
            
            // Valores predeterminados
            BattlefieldWidth = 500f;
            BattlefieldDepth = 500f;
            TerrainDifficulty = 0.5f;
            IsPlayerAttacking = true;
            BattleProgress = 0f;
            PlayerAdvantage = 0f;
            CurrentWeather = WeatherType.Clear;
            TimeOfDay = TimeOfDay.Noon;
            
            InitializeTeams();
            CalculateBattlefieldDimensions();
        }

        private void InitializeTeams()
        {
            foreach (Team team in Mission.Teams)
            {
                if (team.IsPlayerTeam)
                {
                    PlayerTeam = team;
                    foreach (Formation formation in team.FormationsIncludingEmpty)
                    {
                        if (formation.CountOfUnits > 0)
                        {
                            PlayerFormations.Add(formation);
                        }
                    }
                }
                else if (team.IsEnemyOf(Mission.PlayerTeam))
                {
                    EnemyTeam = team;
                    foreach (Formation formation in team.FormationsIncludingEmpty)
                    {
                        if (formation.CountOfUnits > 0)
                        {
                            EnemyFormations.Add(formation);
                        }
                    }
                }
            }
        }

        private void CalculateBattlefieldDimensions()
        {
            // Calcular el centro del campo de batalla basado en las posiciones de las formaciones
            Vec2 sumPositions = Vec2.Zero;
            int formationCount = 0;
            
            foreach (Formation formation in PlayerFormations)
            {
                sumPositions += formation.OrderPosition.AsVec2;
                formationCount++;
            }
            
            foreach (Formation formation in EnemyFormations)
            {
                sumPositions += formation.OrderPosition.AsVec2;
                formationCount++;
            }
            
            if (formationCount > 0)
            {
                BattleCenterPosition = sumPositions / formationCount;
            }
            
            // Calcular dimensiones aproximadas del campo de batalla
            float maxDistanceX = 0f;
            float maxDistanceZ = 0f;
            
            foreach (Formation formation in PlayerFormations.Concat(EnemyFormations))
            {
                Vec2 formationPos = formation.OrderPosition.AsVec2;
                Vec2 distanceVec = formationPos - BattleCenterPosition;
                
                maxDistanceX = Math.Max(maxDistanceX, Math.Abs(distanceVec.x));
                maxDistanceZ = Math.Max(maxDistanceZ, Math.Abs(distanceVec.y));
            }
            
            // Añadir margen
            BattlefieldWidth = maxDistanceX * 2.5f;
            BattlefieldDepth = maxDistanceZ * 2.5f;
        }

        public void Update()
        {
            // Actualizar formaciones
            PlayerFormations.Clear();
            EnemyFormations.Clear();
            
            if (PlayerTeam != null)
            {
                foreach (Formation formation in PlayerTeam.FormationsIncludingEmpty)
                {
                    if (formation.CountOfUnits > 0)
                    {
                        PlayerFormations.Add(formation);
                    }
                }
            }
            
            if (EnemyTeam != null)
            {
                foreach (Formation formation in EnemyTeam.FormationsIncludingEmpty)
                {
                    if (formation.CountOfUnits > 0)
                    {
                        EnemyFormations.Add(formation);
                    }
                }
            }
            
            // Actualizar progreso de batalla
            UpdateBattleProgress();
            
            // Actualizar ventaja del jugador
            UpdatePlayerAdvantage();
            
            // Actualizar amenazas y oportunidades
            UpdateThreatsAndOpportunities();
        }

        private void UpdateBattleProgress()
        {
            // Implementación simplificada: basada en bajas
            int initialPlayerTroops = PlayerTeam?.InitialTroopCount ?? 1;
            int initialEnemyTroops = EnemyTeam?.InitialTroopCount ?? 1;
            
            int currentPlayerTroops = PlayerTeam?.ActiveAgents.Count ?? 0;
            int currentEnemyTroops = EnemyTeam?.ActiveAgents.Count ?? 0;
            
            float playerCasualtiesRatio = 1f - (float)currentPlayerTroops / initialPlayerTroops;
            float enemyCasualtiesRatio = 1f - (float)currentEnemyTroops / initialEnemyTroops;
            
            // Promedio de bajas como indicador de progreso
            BattleProgress = (playerCasualtiesRatio + enemyCasualtiesRatio) / 2f;
        }

        private void UpdatePlayerAdvantage()
        {
            // Implementación simplificada: basada en proporción de tropas
            int playerTroops = PlayerTeam?.ActiveAgents.Count ?? 0;
            int enemyTroops = EnemyTeam?.ActiveAgents.Count ?? 0;
            
            if (playerTroops + enemyTroops > 0)
            {
                PlayerAdvantage = (float)(playerTroops - enemyTroops) / (playerTroops + enemyTroops);
            }
            else
            {
                PlayerAdvantage = 0f;
            }
        }

        private void UpdateThreatsAndOpportunities()
        {
            // Limpiar listas anteriores
            CurrentThreats.Clear();
            CurrentOpportunities.Clear();
            
            // Implementación simplificada
            // En una implementación real, aquí se analizarían las formaciones enemigas
            // para identificar amenazas y oportunidades específicas
        }
    }

    public enum WeatherType
    {
        Clear,
        Cloudy,
        Rainy,
        Foggy,
        Snowy
    }

    public enum TimeOfDay
    {
        Dawn,
        Morning,
        Noon,
        Afternoon,
        Dusk,
        Night
    }
}
