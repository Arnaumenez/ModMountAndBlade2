using System;
using System.Linq;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using TaleWorlds.Core;

namespace TacticalGenius.Utils
{
    public static class VectorExtensions
    {
        // Método de extensión para convertir Vec3 a Vec2
        public static Vec2 AsVec2(this Vec3 vec3)
        {
            return new Vec2(vec3.x, vec3.z);
        }
        
        // Método de extensión para convertir Vec2 a Vec3 (con y=0)
        public static Vec3 ToVec3(this Vec2 vec2)
        {
            return new Vec3(vec2.x, 0f, vec2.y);
        }
        
        // Método de extensión para normalizar un Vec2
        public static Vec2 NormalizedCopy(this Vec2 vec2)
        {
            float length = vec2.Length;
            if (length > 1e-6f)
            {
                return new Vec2(vec2.x / length, vec2.y / length);
            }
            return vec2;
        }
        
        // Método de extensión para calcular distancia entre dos Vec2
        public static float Distance(this Vec2 a, Vec2 b)
        {
            return (a - b).Length;
        }
        
        // Método para reemplazar Math.Clamp
        public static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
        
        // Método de extensión para concatenar listas (reemplaza List.Concat)
        public static System.Collections.Generic.List<T> ConcatList<T>(
            this System.Collections.Generic.List<T> first, 
            System.Collections.Generic.List<T> second)
        {
            System.Collections.Generic.List<T> result = new System.Collections.Generic.List<T>(first);
            result.AddRange(second);
            return result;
        }
    }
}