using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace Assets.Scripts
{
    public class MasterController : Agent
    {




        void Start()
        {

        }



        void Update()
        {
        }


        public override void CollectObservations(VectorSensor sensor)
        {

            var trafficLights = GetTrafficLights();


            foreach (var trafficLight in trafficLights)
            {
                // 1. lámpák állapota - sensor.AddObservation(trafficLight.IsGreen ? 1 : 0);

                // 2. várakozó autók száma -  sensor.AddObservation(trafficLight.WaitingCars);

                // 3. átlagos várakozási idő - sensor.AddObservation(1);

                // 4. karanbolozó autók száma

            }
        }



        public override void OnActionReceived(ActionBuffers actions)
        {

            /*var trafficLights = GetTrafficLights();
            for (int i = 0; i < trafficLights.Count; i++)
            {
                // Akciók: 0 = piros, 1 = zöld
                //trafficLights[i].SetState(actions.DiscreteActions[i] == 1);
            }*/
        }


        public override void OnEpisodeBegin()
        {
            // Állítsd vissza a lámpákat alapértelmezett állapotba
            /*foreach (var trafficLight in trafficLights)
            {
                trafficLight.ResetState();
            }*/

            // Új autók random elhelyezése
            //SpawnCars();
        }


        private List<TrafficLightController> GetTrafficLights()
        {
            return FindObjectsByType<TrafficLightController>(FindObjectsSortMode.None).ToList();
        }




    }
}
