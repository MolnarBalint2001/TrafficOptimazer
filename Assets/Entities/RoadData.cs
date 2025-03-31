using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Entities
{
    public class RoadData
    {
        public string RoadId { get; set; }  
        public List<LaneData> Lanes { get; set; } = new List<LaneData>();
        
        public List<Vector3> Path {  get; set; } = new List<Vector3>();

    }



    public class LaneData
    {
        public string Id { get; set; }
        public List<LaneNode> LanePoints { get; set; } = new List<LaneNode>();

        public bool IsBackward { get; set; }

    }



    public class LaneNode
    {


        public LaneNode(bool isBackward, Vector3 position)
        {
            IsBackward = isBackward;
            Position = position;
        }
        public bool IsBackward {get; set;}

        public Vector3 Position { get; set;}
    }
     
}
