using System;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using TaleWorlds.Core;

namespace TacticalGenius.AI
{
    public class TacticalThreat
    {
        public Formation SourceFormation { get; private set; }
        public Formation TargetFormation { get; private set; }
        public ThreatType Type { get; private set; }
        public float Severity { get; private set; } // 0.0 a 1.0
        public Vec2 ThreatPosition { get; private set; }
        public Vec2 ThreatDirection { get; private set; }
        public float TimeToImpact { get; private set; } // Segundos estimados hasta que la amenaza se materialice
        public bool RequiresImmediateResponse { get; private set; }

        public TacticalThreat(Formation sourceFormation, Formation targetFormation, ThreatType type, float severity)
        {
            SourceFormation = sourceFormation;
            TargetFormation = targetFormation;
            Type = type;
            Severity = Math.Clamp(severity, 0f, 1f);
            ThreatPosition = sourceFormation.OrderPosition.AsVec2;
            ThreatDirection = (targetFormation.OrderPosition.AsVec2 - sourceFormation.OrderPosition.AsVec2).Normalized();
            
            // Calcular tiempo estimado hasta impacto
            float distance = sourceFormation.OrderPosition.Distance(targetFormation.OrderPosition);
            float speed = EstimateFormationSpeed(sourceFormation);
            TimeToImpact = speed > 0 ? distance / speed : float.MaxValue;
            
            // Determinar si requiere respuesta inmediata
            RequiresImmediateResponse = Severity > 0.7f || TimeToImpact < 10f;
        }

        private float EstimateFormationSpeed(Formation formation)
        {
            // Velocidad estimada basada en el tipo de formación
            if (formation.QuerySystem.IsCavalryFormation)
            {
                return 8f; // Unidades a caballo más rápidas
            }
            else if (formation.QuerySystem.IsInfantryFormation)
            {
                return 3f; // Infantería velocidad media
            }
            else if (formation.QuerySystem.IsRangedFormation)
            {
                return 2.5f; // Arqueros ligeramente más lentos
            }
            
            return 3f; // Valor predeterminado
        }

        public bool IsStillValid()
        {
            // Verificar si la amenaza sigue siendo válida
            return SourceFormation != null && SourceFormation.CountOfUnits > 0 &&
                   TargetFormation != null && TargetFormation.CountOfUnits > 0;
        }

        public void Update()
        {
            if (!IsStillValid())
            {
                Severity = 0f;
                return;
            }
            
            // Actualizar posición y dirección
            ThreatPosition = SourceFormation.OrderPosition.AsVec2;
            ThreatDirection = (TargetFormation.OrderPosition.AsVec2 - SourceFormation.OrderPosition.AsVec2).Normalized();
            
            // Recalcular tiempo hasta impacto
            float distance = SourceFormation.OrderPosition.Distance(TargetFormation.OrderPosition);
            float speed = EstimateFormationSpeed(SourceFormation);
            TimeToImpact = speed > 0 ? distance / speed : float.MaxValue;
            
            // Actualizar si requiere respuesta inmediata
            RequiresImmediateResponse = Severity > 0.7f || TimeToImpact < 10f;
        }
    }

    public enum ThreatType
    {
        FrontalAssault,
        Flanking,
        Encirclement,
        RangedAttack,
        CavalryCharge,
        Ambush
    }
}
