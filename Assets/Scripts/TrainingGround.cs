using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Entities;
using Assets.Services;
using Interfaces;
using UnityEngine;

namespace Assets.Scripts
{
    public class TrainingGround : MonoBehaviour
    {

        public int index = 0;

        public string osm = "manhattan_training0.osm";

        private IXmlProcessorService xmlProcService = new XmlProcessorService();

        private MapData mapData;

        private List<Vector3[]> map = new List<Vector3[]>()
        {
            
        };

        private void Start()
        {
            mapData = xmlProcService.LoadXMLDocument(osm);
        }


        private void OnDrawGizmos()
        {
            if (mapData == null) return;

            foreach (var road in mapData.Roads) {


                for (int i = 0; i < road.ControlPoints.Count - 1; i++) { 
                    var currentNode = road.ControlPoints[i];
                    var nextNode = road.ControlPoints[i + 1];
                    Gizmos.DrawLine(currentNode.Position, nextNode.Position);
                }
            
            
            }
        }

        private void Update()
        {
            
        }






    }
}
