using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;
using static Assets.Scripts.TrafficLightController;

namespace Assets.Scripts
{

    /// <summary>
    /// Traffic lights master agent
    /// handling red and green timer
    /// sycncronizing the lamps
    /// </summary>
    public class MasterController : Agent
    {
        public Spawner spawner { get; set; }

        private List<TrafficLightController> trafficLights;

        private List<AStarAgentController> vehicles;

        private BehaviorParameters behaviorParameters;

        public float simulationTime = 0f;

        public bool destroyed = false;

        void Start()
        {
            MaxStep = 5000;
            
            trafficLights = GetTrafficLights();
            vehicles = GetVehicles();
            Debug.Log($"Master space size: {(trafficLights == null ? -1 : trafficLights.Count)}");
            behaviorParameters = gameObject.AddComponent<BehaviorParameters>();
            behaviorParameters.BrainParameters.ActionSpec = ActionSpec.MakeContinuous(trafficLights.Count * 2);
        }

        void Update()
        {
            Debug.Log($"StepCount: {StepCount}");
            if (StepCount == MaxStep)
            {
                EndEpisode();
            }
        
         
        }


        /// <summary>
        /// Collecting observations from the environment
        /// to make decisions
        /// </summary>
        /// <param name="sensor">Sensor</param>
        public override void CollectObservations(VectorSensor sensor)
        {
            foreach (var trafficLight in trafficLights)
            {

                // 1. várakozó autók száma
                sensor.AddObservation(trafficLight.queueLength);

                // 2. karanbolozó autók száma
                // TODO

                // 3. Áthaladó autók száma
                sensor.AddObservation(trafficLight.passingCount);

            }
          
        }


        /// <summary>
        /// Make actions based on observations from the environment
        /// Actions: contineous, discrete
        /// </summary>
        /// <param name="actions">Action list</param>
        public override void OnActionReceived(ActionBuffers actions)
        {

            var continuousActions = actions.ContinuousActions.ToList();
            for (int i = 0; i < continuousActions.Count; i++) {
                  
                var greenTimer = continuousActions[i * 2];
                var redTimer = continuousActions[i * 2 + 1];

                trafficLights[i].greenTimer = greenTimer;
                trafficLights[i].redTimer = redTimer;
                 
            }
        }

        /// <summary>
        /// Reset the simulation environment
        /// Traffic lights reset
        /// Destroy, respawn vehicles
        /// </summary>
        public override void OnEpisodeBegin()
        {
            spawner.Respawn();

            foreach (var trafficLight in trafficLights)
            {
                trafficLight.ResetTrafficLight();
            }

        }

        /// <summary>
        /// Get all traffic lights from the network
        /// </summary>
        /// <returns>List of traffic lights</returns>
        private List<TrafficLightController> GetTrafficLights()
        {
            return FindObjectsByType<TrafficLightController>(FindObjectsSortMode.None).ToList();
        }

        /// <summary>
        /// Get all vehicles from the network
        /// </summary>
        /// <returns>List of vehicles</returns>
        private List<AStarAgentController> GetVehicles()
        {
            return FindObjectsByType<AStarAgentController>(FindObjectsSortMode.None).ToList();
        }



     


    }
}
