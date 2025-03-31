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
        /// Unique identifier
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Road control points
        /// </summary>
        public List<Node> ControlPoints { get; set; } = new List<Node>();

        /// <summary>
        /// Lanes
        /// </summary>
        public int Lanes { get; set; }

        /// <summary>
        /// Backward lanes count
        /// </summary>
        public int BackwardLanes { get; set; } = 1;

        /// <summary>
        /// Forward lanes count
        /// </summary>
        public int ForwardLanes { get; set; } = 1;


        /// <summary>
        /// Turn lanes forward
        /// </summary>
        public string? TurnLanesForward { get; set; }

        /// <summary>
        /// Turn lanes backward
        /// </summary>
        public string? TurnLanesBackward { get; set; }

        /// <summary>
        /// Overall turn lanes
        /// </summary>
        public string? TurnLanes {  get; set; }

        /// <summary>
        /// Is the road one way or not
        /// </summary>
        public bool IsOneWay { get; set; } = false;


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
