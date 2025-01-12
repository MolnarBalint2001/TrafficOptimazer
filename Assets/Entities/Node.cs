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





    public class AStarNode
    {

        public string Id { get; set; }

        public Vector3 Position { get; set; }

        public AStarNode? Parent { get; set; }


        public float G { get; set; } // Cost from start to current node
        public float H { get; set; } // Heuristic cost from current to end
        public float F { get; set; } // Total cost (G + H)

        public List<AStarNode> Children { get; set; } = new List<AStarNode>();


        public AStarNode(string id, Vector3 position, AStarNode parent)
        {
            Id = id;
            Position = position;
            Parent = parent;
            F = G = H = 0;
        }

        public override bool Equals(object obj)
        {
            if (obj is AStarNode other)
            {
                return Id == other.Id;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }


        public override string ToString()
        {
            StringBuilder sb = new StringBuilder($"<AStarNode(Id={Id}, Pos={Position}, F={F}, G={G}, H={H})> \n");
            foreach (var child in Children)
            {
                sb.Append($"\t {child.ToString()} \n");
            }

            return sb.ToString();
        }
    }
}
