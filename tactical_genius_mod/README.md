# TacticalGenius - Mod Avanzado de IA Táctica para Mount & Blade II: Bannerlord

## Descripción del Proyecto
Este mod está diseñado para transformar radicalmente la IA táctica en Mount & Blade II: Bannerlord, creando comandantes enemigos que actúan como verdaderos genios tácticos en el campo de batalla. A diferencia de otros mods que se centran en mejoras estadísticas o comportamientos predefinidos, TacticalGenius implementa un sistema de toma de decisiones dinámico que permite a la IA adaptarse en tiempo real a las condiciones cambiantes del combate.

## Características Principales
- Sistema de evaluación táctica en tiempo real
- Adaptación dinámica a las tácticas del jugador
- Toma de decisiones basada en múltiples factores (terreno, composición de fuerzas, moral, etc.)
- Maniobras tácticas avanzadas (flanqueos, emboscadas, retiradas estratégicas)
- Comportamiento específico según la cultura y el comandante
- Uso inteligente de unidades especializadas
- Coordinación avanzada entre diferentes tipos de unidades

## Estructura del Mod
```
TacticalGenius/
├── SubModule.xml
├── bin/
│   └── Win64_Shipping_Client/
│       └── TacticalGenius.dll
└── src/
    ├── Core/
    │   ├── TacticalGeniusSubModule.cs
    │   ├── Settings.cs
    │   └── ModuleData.cs
    ├── AI/
    │   ├── TacticalAnalyzer.cs
    │   ├── BattlefieldEvaluator.cs
    │   ├── DecisionMaker.cs
    │   └── TacticalExecutor.cs
    ├── Behaviors/
    │   ├── AdvancedFormationBehavior.cs
    │   ├── FlankingBehavior.cs
    │   ├── TerrainUtilizationBehavior.cs
    │   ├── UnitCoordinationBehavior.cs
    │   └── AdaptiveResponseBehavior.cs
    ├── Tactics/
    │   ├── CultureSpecificTactics.cs
    │   ├── CommanderSpecificTactics.cs
    │   ├── AdvancedManeuvers.cs
    │   └── TacticalResponses.cs
    ├── Utils/
    │   ├── TerrainAnalysis.cs
    │   ├── FormationUtils.cs
    │   ├── BattleStateTracker.cs
    │   └── DebugHelpers.cs
    └── Patches/
        ├── MissionBehaviorPatch.cs
        ├── AIBehaviorPatch.cs
        ├── FormationAIPatch.cs
        └── TacticComponentPatch.cs
```

## Enfoque de Implementación
Este mod utiliza Harmony para aplicar parches a las clases del juego base sin modificar los archivos originales, garantizando compatibilidad y estabilidad. La implementación se centra en extender y mejorar el comportamiento de la IA existente en lugar de reemplazarla por completo.
