
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Assets.Scripts
{

    /// <summary>
    /// Traffic controller
    /// Control the traffic light itself
    /// </summary>
    public class TrafficLightController : MonoBehaviour
    {

        /// <summary>
        /// Unique identifier
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Light status enum
        /// </summary>
        public enum LightStatus { Red, Green, Yellow };

        /// <summary>
        /// Agent queques
        /// </summary>
        public int queueLength { get; set; }

        /// <summary>
        /// Current status
        /// </summary>
        public LightStatus currentStatus { get; set; } = LightStatus.Green;

        /// <summary>
        /// Traffic light timer
        /// controlled by AI
        /// </summary>
        private float timer = 5f;

        /// <summary>
        /// Renderer
        /// </summary>
        private Renderer objRenderer;

        /// <summary>
        /// Position of the traffic light
        /// </summary>
        public Vector3 position { get; set; }

        /// <summary>
        /// Queue scope threshold
        /// </summary>
        private const float queueScope = 20f;


        #region Timers - controlled by master

        public float redTimer = 5f;

        public float greenTimer = 8f;

        public float yellowTimer = 2f;

        #endregion


        private void Start()
        {
            objRenderer = GetComponent<Renderer>();
            objRenderer.transform.position = position;
            objRenderer.material.color = Color.green;

        }

        private void Update()
        {


            ObserveQueue();

            if (timer > 0)
            {
                timer -= Time.deltaTime; // Csökkentjük az időt
            }
            else
            {
                ChangeStatus();
                ChangeColor();
                ResetTimer();

            }
        }

        /// <summary>
        /// Change color and state of the traffic light
        /// </summary>
        void ChangeColor()
        {
            switch (currentStatus)
            {
                case LightStatus.Green:
                    objRenderer.material.color = Color.green;
                    break;
                case LightStatus.Yellow:
                    objRenderer.material.color = Color.yellow;
                    break;
                case LightStatus.Red:
                    objRenderer.material.color = Color.red;
                    break;
            }
        }

        void ResetTimer()
        {
            timer = 10f;
        }

        /// <summary>
        /// Change status
        /// </summary>
        void ChangeStatus()
        {
            switch (currentStatus)
            {
                case LightStatus.Green:
                    currentStatus = LightStatus.Yellow;
                    break;
                case LightStatus.Yellow:
                    currentStatus = LightStatus.Red;
                    break;
                case LightStatus.Red:
                    currentStatus = LightStatus.Green;
                    break;
            }
        }

        public void SetPosition(Vector3 pos)
        {
            position = pos;
        }


        public void SetTimer(float t)
        {
            timer = t;
        }



        public void SetQueueLength(int length)
        {
            queueLength = length;
        }


        /// <summary>
        /// Queue observation
        /// </summary>
        /// <returns>Number of agents in the scope</returns>
        public int ObserveQueue()
        {
            Vector3 tflPosition = gameObject.transform.position;


            List<AgentController> agents = new List<AgentController>(FindObjectsOfType<AgentController>());
            List<AgentController> agentsInScope = agents.Where(agent =>
            {
                Vector3 agentPosition = agent.transform.position;
                float distance = Vector3.Distance(tflPosition, agentPosition);

                if (distance <= queueScope)
                    return true;

                return false;
            }).ToList();


            //Debug.Log($"TFL-{Id} queue count: {agentsInScope.Count}");

            return agentsInScope.Count;


        }

        #region Status checkers
        public bool IsRed()
        {
            return currentStatus == LightStatus.Red;
        }


        public bool IsGreen()
        {
            return currentStatus == LightStatus.Green;
        }


        public bool IsYellow()
        {
            return currentStatus == LightStatus.Yellow;
        }



        #endregion
    }
}
