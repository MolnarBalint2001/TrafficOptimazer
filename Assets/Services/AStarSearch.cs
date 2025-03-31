using Assets.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;


namespace Assets.Services
{

    public class AStarSearch
    {

        public List<Node> FindPath(
            Dictionary<string, List<Node>> graph,
            Node start,
            Node goal,
            Dictionary<string, Node> nodes
         )
        {



            var openSet = new List<Node>();
            var closedSet = new HashSet<Node>();

            var cameFrom = new Dictionary<Node, Node>();

            var (gScore, fScore) = InitializeScores(nodes);


            openSet.Add(start);
            gScore[start.Id] = 0;
            fScore[start.Id] = Heuristic(start, goal);



            while (openSet.Count > 0)
            {
                var currentNode = openSet.OrderBy(x => fScore[x.Id]).First();

                if (currentNode == goal)
                {
                    return ReconstuctPath(cameFrom, currentNode);
                }


                openSet.Remove(currentNode);

                var children = graph[currentNode.Id];

                foreach (var child in children)
                {


                    if (closedSet.Contains(child))
                    {
                        continue;
                    }

                    float tentiativeGScore = gScore[currentNode.Id] + Heuristic(currentNode, child);

                    if (tentiativeGScore < gScore[child.Id] && !openSet.Contains(child))
                    {
                        cameFrom[child] = currentNode;
                        gScore[child.Id] = tentiativeGScore;
                        fScore[child.Id] = tentiativeGScore + Heuristic(child, goal);
                        openSet.Add(child);

                    }
                }



            }


            return null;
        }

        private (Dictionary<string, float>, Dictionary<string, float>) InitializeScores(Dictionary<string, Node> nodes)
        {

            var gScore = new Dictionary<string, float>();
            var fScore = new Dictionary<string, float>();
            foreach (var entry in nodes)
            {
                gScore[entry.Key] = float.MaxValue;
                fScore[entry.Key] = float.MaxValue;
            }

            return (gScore, fScore);
        }

        private List<Node> ReconstuctPath(Dictionary<Node, Node> cameFrom, Node current)
        {
            var path = new List<Node>();
            path.Add(current);
            while (cameFrom.ContainsKey(current))
            {
                cameFrom.TryGetValue(current, out current);
                path.Add(current);
            }
            path.Reverse();
            return path;
        }


        private float Heuristic(Node a, Node b)
        {

            return Vector3.Distance(a.Position, b.Position);
        }


        #region Find V2


        public List<Node> FindPathV2(string startId, string goalId, GraphOSM graph)
        {

            // A csúcsok, amiket már megvizsgáltunk
            var closedSet = new HashSet<string>();
            // A csúcsok, amiket még meg kell vizsgálnunk
            var openSet = new HashSet<string> { startId };
            // A g értékek (valós útvonal költség)
            var gScore = new Dictionary<string, float>();
            gScore[startId] = 0;
            // Az f értékek (g + h)
            var fScore = new Dictionary<string, float>();
            fScore[startId] = HeuristicV2(startId, goalId, graph);

            // Az előző csúcsok, hogy vissza tudjuk rekonstruálni az utat
            var cameFrom = new Dictionary<string, string>();

            while (openSet.Count > 0)
            {
                // A legkisebb fScore értékű csúcs kiválasztása
                string current = GetNodeWithLowestFScoreV2(fScore, openSet);

                // Ha elértük a célt, rekonstruáljuk az utat
                if (current == goalId)
                {
                    return ReconstructPathV2(cameFrom, current, graph);
                }

                openSet.Remove(current);
                closedSet.Add(current);

                foreach (var edge in graph.Edges)
                {
                    if (edge.Source != current) continue;

                    string neighborId = edge.Target;
                    if (closedSet.Contains(neighborId)) continue;

                    float tentativeGScore = gScore[current] + edge.Weight;

                    if (!openSet.Contains(neighborId))
                        openSet.Add(neighborId);
                    else if (tentativeGScore >= gScore[neighborId])
                        continue;

                    // A csúcsot nem vizsgáltuk meg, frissítjük a gScore-t és fScore-t
                    cameFrom[neighborId] = current;
                    gScore[neighborId] = tentativeGScore;
                    fScore[neighborId] = gScore[neighborId] + HeuristicV2(neighborId, goalId, graph);
                }
            }

            // Ha nincs út
            return null;
        }

        // Heurisztikus függvény: egyszerű Euclidean távolság a cél pozíciótól
        private float HeuristicV2(string nodeId, string goalId, GraphOSM graph)
        {
            var node = graph.Nodes.FirstOrDefault(n => n.Id == nodeId);
            var goalNode = graph.Nodes.FirstOrDefault(n => n.Id == goalId);
            if (goalNode == null || node == null)
            {
                return 0;
            }
            return Vector3.Distance(node.PaddedLanePosition, goalNode.PaddedLanePosition);
        }

        // A legkisebb fScore értékű csúcs keresése
        private string GetNodeWithLowestFScoreV2(Dictionary<string, float> fScore, HashSet<string> openSet)
        {
            string lowestNode = null;
            float lowestScore = float.MaxValue;

            foreach (var nodeId in openSet)
            {
                if (fScore.ContainsKey(nodeId) && fScore[nodeId] < lowestScore)
                {
                    lowestScore = fScore[nodeId];
                    lowestNode = nodeId;
                }
            }

            return lowestNode;
        }

        // Az útvonal rekonstruálása a "cameFrom" szótár alapján
        private List<Node> ReconstructPathV2(Dictionary<string, string> cameFrom, string current, GraphOSM graph)
        {
            var path = new List<Node>();
            while (cameFrom.ContainsKey(current))
            {
                path.Insert(0, graph.Nodes.FirstOrDefault(n => n.Id == current));
                current = cameFrom[current];
            }

            path.Insert(0, graph.Nodes.FirstOrDefault(n => n.Id == current));  // Az induló csúcs hozzáadása
            return path;
        }
    }

    #endregion
}

