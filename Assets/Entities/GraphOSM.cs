using System.Collections.Generic;

namespace Assets.Entities
{
    /// <summary>
    /// Graph based on OSM data
    /// </summary>
    public class GraphOSM
    {
        /// <summary>
        /// Edges
        /// </summary>
        public ICollection<Edge> Edges { get; set; } = new LinkedList<Edge>();

        /// <summary>
        /// Nodes
        /// </summary>
        public ICollection<Node> Nodes { get; set; } = new LinkedList<Node>();
    }

    /// <summary>
    /// Edge
    /// </summary>
    public class Edge
    {
        /// <summary>
        /// Source node identifier
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Target node identifier
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// Weight
        /// Distance between source and target
        /// </summary>
        public float Weight { get; set; }
    }
}
