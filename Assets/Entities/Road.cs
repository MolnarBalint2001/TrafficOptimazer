using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Entities
{

    /// <summary>
    /// Road object
    /// </summary>
    public class Road
    {
        /// <summary>
        /// Road control points
        /// </summary>
        public List<Node> ControlPoints { get; set; } = new List<Node>();

        /// <summary>
        /// Road string representation
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"ControlPointsCount={ControlPoints.Count})");
            ControlPoints.ForEach(p =>
            {
                sb.AppendLine(p.ToString());
            });

            return sb.ToString();
        }

    }
}
