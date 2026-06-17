using System.Collections.Generic;
using Monopoly.Board;
using Monopoly.Core;
using UnityEngine;

namespace Monopoly.Customer
{
    public class CustomerFlowManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private bool preferInspectorConfiguration = true;
        [SerializeField] private CustomerAgent customerPrefab;
        [SerializeField] private CustomerDecisionHelper decisionHelper;
        [SerializeField] private BoardManager boardManager;
        [SerializeField] private PathNode spawnNode;
        [SerializeField] private List<CustomerData> customerPool = new List<CustomerData>();
        [SerializeField] private float spawnInterval = 5f;
        [SerializeField] private int maxCustomers = 10;

        private readonly List<CustomerAgent> activeCustomers = new List<CustomerAgent>();
        private float spawnTimer;
        private bool isSpawning;

        private void Update()
        {
            if (!isSpawning || customerPrefab == null || spawnNode == null || customerPool.Count == 0)
            {
                if (!isSpawning || spawnNode == null || customerPool.Count == 0)
                {
                    return;
                }
            }

            activeCustomers.RemoveAll(item => item == null);
            if (activeCustomers.Count >= maxCustomers)
            {
                return;
            }

            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnInterval)
            {
                spawnTimer = 0f;
                SpawnCustomer();
            }
        }

        public void StartSpawning()
        {
            isSpawning = true;
            spawnTimer = 0f;
        }

        public void StopSpawning()
        {
            isSpawning = false;
        }

        public void SpawnCustomer()
        {
            CustomerData customerData = customerPool[Random.Range(0, customerPool.Count)];
            SpawnCustomer(customerData, false, null);
        }

        public CustomerAgent SpawnEvaluationCustomer(CustomerData customerData, System.Action<CustomerAgent> onCompleted)
        {
            return SpawnCustomer(customerData, true, onCompleted);
        }

        private CustomerAgent SpawnCustomer(CustomerData customerData, bool evaluationMode, System.Action<CustomerAgent> onCompleted)
        {
            if (spawnNode == null)
            {
                return null;
            }

            CustomerData resolvedData = customerData != null
                ? customerData
                : (customerPool != null && customerPool.Count > 0 ? customerPool[Random.Range(0, customerPool.Count)] : null);

            CustomerAgent agent = customerPrefab != null
                ? Instantiate(customerPrefab, spawnNode.StandPoint.position, Quaternion.identity)
                : CreateRuntimeCustomerAgent();
            agent.Initialize(boardManager, decisionHelper, resolvedData, spawnNode);
            if (evaluationMode)
            {
                agent.ConfigureEvaluationRun(onCompleted);
            }
            agent.StartCustomerLife();
            activeCustomers.Add(agent);
            return agent;
        }

        public void Configure(
            CustomerDecisionHelper helper,
            BoardManager manager,
            PathNode startNode,
            List<CustomerData> customers,
            float interval,
            int maxCount)
        {
            if (!preferInspectorConfiguration || decisionHelper == null)
            {
                decisionHelper = helper;
            }

            if (!preferInspectorConfiguration || boardManager == null)
            {
                boardManager = manager;
            }

            if (!preferInspectorConfiguration || spawnNode == null)
            {
                spawnNode = startNode;
            }

            if (!preferInspectorConfiguration || customerPool == null || customerPool.Count == 0)
            {
                customerPool = customers ?? new List<CustomerData>();
            }

            if (!preferInspectorConfiguration || spawnInterval <= 0f)
            {
                spawnInterval = interval;
            }

            if (!preferInspectorConfiguration || maxCustomers <= 0)
            {
                maxCustomers = maxCount;
            }
        }

        private CustomerAgent CreateRuntimeCustomerAgent()
        {
            GameObject customerObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            customerObject.name = "RuntimeCustomer";
            customerObject.transform.localScale = Vector3.one * 0.6f;

            Renderer renderer = customerObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(1f, 0.8f, 0.2f);
            }

            return customerObject.AddComponent<CustomerAgent>();
        }
    }
}
