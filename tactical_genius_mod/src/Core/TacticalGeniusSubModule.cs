using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using TaleWorlds.Engine;
using TacticalGenius.AI;
using TacticalGenius.Behaviors;

namespace TacticalGenius.Core
{
    public class TacticalGeniusSubModule : MBSubModuleBase
    {
        private bool _isInitialized = false;
        private TacticalBrain _tacticalBrain;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            
            // Inicialización del mod
            _isInitialized = true;
            
            // Registrar mensajes de depuración
            InformationManager.DisplayMessage(new InformationMessage("Tactical Genius: Mod cargado correctamente."));
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
            
            if (!_isInitialized)
            {
                InformationManager.DisplayMessage(new InformationMessage("Tactical Genius: Error de inicialización."));
                return;
            }
            
            InformationManager.DisplayMessage(new InformationMessage("Tactical Genius: Listo para mejorar la IA táctica."));
        }

        // Cambiado de override a protected para evitar problemas de firma
        protected void OnMissionEnded(Mission mission)
        {
            // Limpieza de recursos cuando termina una misión
            if (_tacticalBrain != null)
            {
                _tacticalBrain.Cleanup();
                _tacticalBrain = null;
            }
        }

        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            base.OnMissionBehaviorInitialize(mission);
            
            // Solo aplicar en misiones de batalla
            if (mission.CombatType == Mission.MissionCombatType.Combat)
            {
                // Inicializar el cerebro táctico
                _tacticalBrain = new TacticalBrain(mission);
                
                // Registrar comportamientos tácticos
                mission.AddMissionBehavior(new TacticalBehaviors(mission, _tacticalBrain));
                
                InformationManager.DisplayMessage(new InformationMessage("Tactical Genius: IA táctica mejorada activada para esta batalla."));
            }
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarter)
        {
            base.OnGameStart(game, gameStarter);
            
            // Modificado para evitar el uso de MissionBasedGameStarter
            // Registrar lógica de juego adicional si es necesario
            if (gameStarter != null)
            {
                // Aquí puedes agregar lógica adicional para el inicio del juego
                // sin depender de MissionBasedGameStarter
            }
        }

        public override void OnGameLoaded(Game game, object initializerObject)
        {
            base.OnGameLoaded(game, initializerObject);
            
            // Reinicializar componentes después de cargar una partida guardada
            _isInitialized = true;
            
            InformationManager.DisplayMessage(new InformationMessage("Tactical Genius: Mod reiniciado después de cargar partida."));
        }
    }
}
