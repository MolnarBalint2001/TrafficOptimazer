using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Assets.Entities
{
    /// <summary>
    /// Object node
    /// </summary>
    public class Node
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Node position
        /// </summary>
        public Vector3 Position { get; set; }

        /// <summary>
        /// Is the node an intersection point
        /// </summary>
        public bool IsIntersectionNode { get; set; } = false;


        /// <summary>
        /// String representation of the Node
        /// </summary>
        /// <returns>Representation</returns>
        public override string ToString()
        {
            return $"<Node(Id={Id}, Pos={Position} IsIntersection={IsIntersectionNode}>";
        }

    }

}
