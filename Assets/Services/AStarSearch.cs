using Assets.Entities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Assets.Services
{

    public class AStarSearch
    {

        public List<Vector3> FindPath(
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

        private List<Vector3> ReconstuctPath(Dictionary<Node, Node> cameFrom, Node current)
        {
            var path = new List<Vector3>();
            path.Add(current.Position);
            while (cameFrom.ContainsKey(current))
            {
                cameFrom.TryGetValue(current, out current);
                path.Add(current.Position);
            }
            path.Reverse();
            return path;
        }


        private float Heuristic(Node a, Node b)
        {

            return Vector3.Distance(a.Position, b.Position);
        }
    }
}
