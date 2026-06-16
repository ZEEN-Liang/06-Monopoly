using System.Collections.Generic;
using Monopoly.Board;
using Monopoly.Core;
using UnityEngine;

namespace Monopoly.Customer
{
    public class CustomerFlowManager : MonoBehaviour
    {
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
            CustomerAgent agent = customerPrefab != null
                ? Instantiate(customerPrefab, spawnNode.StandPoint.position, Quaternion.identity)
                : CreateRuntimeCustomerAgent();
            agent.Initialize(boardManager, decisionHelper, customerData, spawnNode);
            agent.StartCustomerLife();
            activeCustomers.Add(agent);
        }

        public void Configure(
            CustomerDecisionHelper helper,
            BoardManager manager,
            PathNode startNode,
            List<CustomerData> customers,
            float interval,
            int maxCount)
        {
            decisionHelper = helper;
            boardManager = manager;
            spawnNode = startNode;
            customerPool = customers ?? new List<CustomerData>();
            spawnInterval = interval;
            maxCustomers = maxCount;
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
