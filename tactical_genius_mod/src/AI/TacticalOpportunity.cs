using System;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using TaleWorlds.Core;

namespace TacticalGenius.AI
{
    public class TacticalOpportunity
    {
        public Formation SourceFormation { get; private set; }
        public Formation TargetFormation { get; private set; }
        public OpportunityType Type { get; private set; }
        public float Value { get; private set; } // 0.0 a 1.0
        public Vec2 OpportunityPosition { get; private set; }
        public float TimeWindow { get; private set; } // Segundos disponibles para aprovechar la oportunidad
        public bool IsTransient { get; private set; } // Si es temporal o persistente

        public TacticalOpportunity(Formation sourceFormation, Formation targetFormation, OpportunityType type, float value)
        {
            SourceFormation = sourceFormation;
            TargetFormation = targetFormation;
            Type = type;
            Value = Math.Clamp(value, 0f, 1f);
            
            // Posición donde se puede aprovechar la oportunidad
            if (type == OpportunityType.Flanking || type == OpportunityType.Encirclement)
            {
                // Para flanqueo, la posición es lateral al objetivo
                Vec2 targetDir = targetFormation.Direction.AsVec2;
                Vec2 perpendicular = new Vec2(-targetDir.y, targetDir.x);
                OpportunityPosition = targetFormation.OrderPosition.AsVec2 + perpendicular * 20f;
            }
            else if (type == OpportunityType.HighGround)
            {
                // Para ventaja de altura, usar la posición actual
                OpportunityPosition = sourceFormation.OrderPosition.AsVec2;
            }
            else
            {
                // Para otros tipos, posición entre las formaciones
                OpportunityPosition = (sourceFormation.OrderPosition.AsVec2 + targetFormation.OrderPosition.AsVec2) * 0.5f;
            }
            
            // Ventana de tiempo basada en el tipo
            TimeWindow = DetermineTimeWindow(type);
            
            // Determinar si es transitoria
            IsTransient = type == OpportunityType.Flanking || type == OpportunityType.Countercharge;
        }

        private float DetermineTimeWindow(OpportunityType type)
        {
            switch (type)
            {
                case OpportunityType.Flanking:
                case OpportunityType.Countercharge:
                    return 15f; // Oportunidades tácticas de corta duración
                case OpportunityType.Encirclement:
                    return 30f; // Duración media
                case OpportunityType.HighGround:
                case OpportunityType.DefensivePosition:
                    return 120f; // Ventajas de terreno de larga duración
                default:
                    return 60f; // Valor predeterminado
            }
        }

        public bool IsStillValid()
        {
            // Verificar si la oportunidad sigue siendo válida
            return SourceFormation != null && SourceFormation.CountOfUnits > 0 &&
                   (TargetFormation == null || TargetFormation.CountOfUnits > 0);
        }

        public void Update()
        {
            if (!IsStillValid())
            {
                Value = 0f;
                return;
            }
            
            // Actualizar posición si es necesario
            if (Type == OpportunityType.Flanking || Type == OpportunityType.Encirclement)
            {
                Vec2 targetDir = TargetFormation.Direction.AsVec2;
                Vec2 perpendicular = new Vec2(-targetDir.y, targetDir.x);
                OpportunityPosition = TargetFormation.OrderPosition.AsVec2 + perpendicular * 20f;
            }
            
            // Reducir ventana de tiempo
            TimeWindow -= 0.1f; // Reducción por actualización
            
            // Si la ventana se agota, reducir el valor
            if (TimeWindow <= 0f)
            {
                Value *= 0.9f;
                if (Value < 0.1f)
                {
                    Value = 0f;
                }
            }
        }
    }

    public enum OpportunityType
    {
        Flanking,
        Encirclement,
        HighGround,
        DefensivePosition,
        Countercharge,
        RangedAdvantage
    }
}
