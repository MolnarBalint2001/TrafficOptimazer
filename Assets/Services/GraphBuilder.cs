using Assets.Entities;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Services
{

    /// <summary>
    /// Road network graph builder
    /// </summary>
    public class GraphBuilder
    {

        /// <summary>
        /// Build the graph from proccesed OSM data
        /// </summary>
        /// <param name="network">List of roads</param>
        /// <returns>Graph</returns>
        public static Dictionary<string, List<Node>> BuildGraph(List<Road> network)
        {
            Dictionary<string, List<Node>> graph = new Dictionary<string, List<Node>>();

            foreach (var road in network)
            {
                for (int i = 0; i < road.ControlPoints.Count; i++)
                {

                    Node current = road.ControlPoints[i];

                    if (!graph.ContainsKey(current.Id))
                    {

                        graph[current.Id] = new List<Node>();

                        // first
                        if (i == 0)
                        {
                            graph[current.Id].Add(road.ControlPoints[i + 1]);
                        }

                        // not last, not first
                        if (i > 0 && i < road.ControlPoints.Count - 1)
                        {
                            graph[current.Id].Add(road.ControlPoints[i - 1]);
                            graph[current.Id].Add(road.ControlPoints[i + 1]);
                        }

                        // last
                        if (i == road.ControlPoints.Count - 1)
                        {
                            graph[current.Id].Add(road.ControlPoints[i - 1]);
                        }


                    }
                    else
                    {
                        if (i == 0)
                        {
                            graph[current.Id].Add(road.ControlPoints[i + 1]);
                        }

                        // not last, not first
                        if (i > 0 && i < road.ControlPoints.Count - 1)
                        {
                            graph[current.Id].Add(road.ControlPoints[i - 1]);
                            graph[current.Id].Add(road.ControlPoints[i + 1]);
                        }

                        // last
                        if (i == road.ControlPoints.Count - 1)
                        {
                            graph[current.Id].Add(road.ControlPoints[i - 1]);
                        }
                    }
                }
            }

            return graph;
        }

    
        /// <summary>
        /// Print graph
        /// </summary>
        /// <param name="graph">Graph to print</param>
        public static void PrintGraph(Dictionary<string, List<Node>> graph)
        {


            foreach (var entry in graph)
            {
                Debug.Log($"Key: {entry.Key}\n");
                entry.Value.ForEach(x => Debug.Log($"\t{x.ToString()}\n"));
            }
        }

        /// <summary>
        /// Road node collection builder 
        /// </summary>
        /// <param name="roads">Roads</param>
        /// <returns>All nodes with O1 lookup</returns>

        public static Dictionary<string, Node> BuildNodeCollection(List<Road> roads)
        {

            Dictionary<string, Node> nodeCollection = new Dictionary<string, Node>();


            foreach (var road in roads)
            {
                foreach (var cp in road.ControlPoints)
                {

                    if (nodeCollection.ContainsKey(cp.Id))
                    {
                        continue;
                    }

                    if (!nodeCollection.ContainsKey(cp.Id))
                    {
                        nodeCollection.Add(cp.Id, cp);
                    }
                }
            }

            return nodeCollection;


        }
    }


}
