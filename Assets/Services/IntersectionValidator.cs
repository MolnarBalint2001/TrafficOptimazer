using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Scripts;
using Grpc.Core;

namespace Assets.Services
{
    /// <summary>
    /// Intersection validator
    /// </summary>
    public static class IntersectionValidator
    {
        /// <summary>
        /// Returns that any intersection's traffic lights are fully green
        /// </summary>
        /// <param name="intersections">Intersections</param>
        /// <returns></returns>
        public static bool IsAnyIntersectionFullyGreen(List<IntersectionController> intersections)
        {
            foreach (var intersection in intersections) { 
                if (intersection.IsAllTrafficLightsGreen())
                {
                    return true;
                }
            }
            return false;
        }
    }
}
