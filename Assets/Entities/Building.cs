
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Assets.Entities
{
    /// <summary>
    /// Building object
    /// </summary>
    public class Building
    {

        /// <summary>
        /// Building control points
        /// for polygon representation
        /// </summary>
        public List<Vector3> ControlPoints { get; set; } = new List<Vector3>();

        /// <summary>
        /// Building string representation
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Building(ControlPointsCount={ControlPoints.Count})");
            ControlPoints.ForEach(p =>
            {
                sb.AppendLine(p.ToString());
            });

            return sb.ToString();
        }
    }
}
