using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Monopoly.Player
{
    public class DiceController : MonoBehaviour
    {
        private const int CurrentConfigVersion = 6;

        [Header("Dice Spawn")]
        [SerializeField] private Vector3 rollCenter = new Vector3(0f, 1.8f, 0f);
        [SerializeField] private float rollArenaDiameter = 7.2f;
        [SerializeField] private float rollArenaThickness = 0.4f;
        [SerializeField] private float diceSize = 0.9f;

        [Header("Arena Walls")]
        [SerializeField] private int wallSegmentCount = 12;
        [SerializeField] private float wallHeight = 30f;
        [SerializeField] private float wallThickness = 0.35f;
        [SerializeField] private float wallLengthPadding = 0.92f;

        [Header("Physics")]
        [SerializeField] private float throwUpForce = 6.2f;
        [SerializeField] private float throwSideForce = 3.8f;
        [SerializeField] private float torqueForce = 14f;
        [SerializeField] private float centerBiasHeight = 2.4f;
        [SerializeField] private float centerBiasForce = 6.5f;
        [SerializeField] private float settleVelocityThreshold = 0.12f;
        [SerializeField] private float settleAngularVelocityThreshold = 0.12f;
        [SerializeField] private float settleDuration = 0.3f;
        [SerializeField] private float maxRollDuration = 3f;
        [SerializeField, HideInInspector] private int configVersion;

        private Rigidbody diceRigidbody;
        private GameObject diceObject;
        private GameObject rollSurfaceObject;
        private GameObject wallRootObject;
        private bool isRolling;

        public bool IsRolling => isRolling;

        private void Awake()
        {
            MigrateLegacySerializedValues();
            CleanupLegacyArenaArtifacts();
        }

        private void Start()
        {
            RebuildArena();
        }

        private void OnValidate()
        {
            MigrateLegacySerializedValues();
            wallSegmentCount = Mathf.Max(3, wallSegmentCount);
            rollArenaDiameter = Mathf.Max(1f, rollArenaDiameter);
            rollArenaThickness = Mathf.Max(0.05f, rollArenaThickness);
            wallHeight = Mathf.Max(0.5f, wallHeight);
            wallThickness = Mathf.Max(0.05f, wallThickness);

            if (!Application.isPlaying)
            {
                return;
            }

            RebuildArena();
        }

        private void MigrateLegacySerializedValues()
        {
            if (configVersion >= CurrentConfigVersion)
            {
                return;
            }

            rollArenaDiameter = 7.2f;
            wallHeight = 30f;
            throwUpForce = 6.2f;
            throwSideForce = 3.8f;
            torqueForce = 14f;
            centerBiasHeight = 2.4f;
            centerBiasForce = 6.5f;
            settleVelocityThreshold = 0.12f;
            settleAngularVelocityThreshold = 0.12f;
            settleDuration = 0.3f;
            maxRollDuration = 3f;
            configVersion = CurrentConfigVersion;
        }

        public void Roll(Action<int> onRollFinished)
        {
            if (!isRolling)
            {
                StartCoroutine(RollRoutine(onRollFinished));
            }
        }

        private IEnumerator RollRoutine(Action<int> onRollFinished)
        {
            isRolling = true;
            EnsureDiceSetup();
            PrepareDiceForNextRoll();

            Vector3 centerTarget = new Vector3(rollCenter.x, rollCenter.y + centerBiasHeight, rollCenter.z);
            Vector3 towardCenter = (centerTarget - diceObject.transform.position).normalized;
            Vector3 randomSide = new Vector3(
                UnityEngine.Random.Range(-throwSideForce, throwSideForce),
                0f,
                UnityEngine.Random.Range(-throwSideForce, throwSideForce));
            Vector3 launchForce = towardCenter * centerBiasForce + Vector3.up * throwUpForce + randomSide;

            diceRigidbody.velocity = Vector3.zero;
            diceRigidbody.angularVelocity = Vector3.zero;
            diceRigidbody.AddForce(launchForce, ForceMode.Impulse);
            diceRigidbody.AddTorque(UnityEngine.Random.insideUnitSphere * torqueForce, ForceMode.Impulse);

            float settledTimer = 0f;
            float elapsed = 0f;

            while (elapsed < maxRollDuration)
            {
                elapsed += Time.deltaTime;

                bool slowEnough =
                    diceRigidbody.velocity.sqrMagnitude <= settleVelocityThreshold * settleVelocityThreshold &&
                    diceRigidbody.angularVelocity.sqrMagnitude <= settleAngularVelocityThreshold * settleAngularVelocityThreshold;

                if (slowEnough)
                {
                    settledTimer += Time.deltaTime;
                    if (settledTimer >= settleDuration)
                    {
                        break;
                    }
                }
                else
                {
                    settledTimer = 0f;
                }

                yield return null;
            }

            int result = ReadTopFaceValue();
            Debug.Log($"Physical dice settled with result: {result}");

            isRolling = false;
            onRollFinished?.Invoke(result);
        }

        private void EnsureDiceSetup()
        {
            EnsureRollSurface();

            if (diceObject != null && diceRigidbody != null)
            {
                return;
            }

            diceObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            diceObject.name = "PhysicalDice";
            diceObject.transform.SetParent(transform, false);
            diceObject.transform.localScale = Vector3.one * diceSize;
            PlaceDiceAtDefaultPosition();

            Renderer diceRenderer = diceObject.GetComponent<Renderer>();
            if (diceRenderer != null)
            {
                diceRenderer.material.color = new Color(0.96f, 0.95f, 0.9f);
            }

            diceRigidbody = diceObject.AddComponent<Rigidbody>();
            diceRigidbody.mass = 1f;
            diceRigidbody.drag = 1.15f;
            diceRigidbody.angularDrag = 1.7f;
            diceRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            diceRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            CreateFaceLabel(1, Vector3.up * 0.51f, Quaternion.Euler(90f, 0f, 0f));
            CreateFaceLabel(6, Vector3.down * 0.51f, Quaternion.Euler(-90f, 180f, 0f));
            CreateFaceLabel(2, Vector3.right * 0.51f, Quaternion.Euler(0f, 90f, 90f));
            CreateFaceLabel(5, Vector3.left * 0.51f, Quaternion.Euler(0f, -90f, -90f));
            CreateFaceLabel(3, Vector3.forward * 0.51f, Quaternion.identity);
            CreateFaceLabel(4, Vector3.back * 0.51f, Quaternion.Euler(0f, 180f, 0f));
        }

        private void EnsureRollSurface()
        {
            if (rollSurfaceObject == null)
            {
                rollSurfaceObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rollSurfaceObject.name = "DiceRollSurface";
                rollSurfaceObject.transform.SetParent(transform, false);
            }

            rollSurfaceObject.transform.position = new Vector3(rollCenter.x, -0.25f, rollCenter.z);
            rollSurfaceObject.transform.localScale = new Vector3(
                rollArenaDiameter,
                rollArenaThickness,
                rollArenaDiameter);

            Renderer surfaceRenderer = rollSurfaceObject.GetComponent<Renderer>();
            if (surfaceRenderer != null)
            {
                surfaceRenderer.enabled = false;
            }

            EnsureArenaWalls();
        }

        private void EnsureArenaWalls()
        {
            RebuildArenaWalls();
        }

        private void RebuildArena()
        {
            EnsureRollSurface();

            if (diceObject != null)
            {
                PlaceDiceAtDefaultPosition();
            }
        }

        private void CleanupLegacyArenaArtifacts()
        {
            CleanupNamedObjects("DiceRollSurface");
            CleanupNamedObjects("DiceArenaWalls");
            CleanupNamedObjects("PhysicalDice");
        }

        private void CleanupNamedObjects(string objectName)
        {
            GameObject[] sceneObjects = FindObjectsOfType<GameObject>();
            for (int i = 0; i < sceneObjects.Length; i++)
            {
                GameObject sceneObject = sceneObjects[i];
                if (sceneObject == null || sceneObject.name != objectName)
                {
                    continue;
                }

                if (sceneObject.transform.parent == transform)
                {
                    if (objectName == "DiceRollSurface")
                    {
                        rollSurfaceObject = sceneObject;
                    }
                    else if (objectName == "DiceArenaWalls")
                    {
                        wallRootObject = sceneObject;
                    }
                    else if (objectName == "PhysicalDice")
                    {
                        diceObject = sceneObject;
                        diceRigidbody = sceneObject.GetComponent<Rigidbody>();
                    }

                    continue;
                }

                Destroy(sceneObject);
            }
        }

        private void RebuildArenaWalls()
        {
            if (wallRootObject != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(wallRootObject);
                }
                else
                {
                    DestroyImmediate(wallRootObject);
                }
            }

            wallRootObject = new GameObject("DiceArenaWalls");
            wallRootObject.transform.SetParent(transform, false);

            float radius = rollArenaDiameter * 0.5f;
            float segmentArc = Mathf.PI * 2f * radius / Mathf.Max(3, wallSegmentCount);
            float wallLength = segmentArc * wallLengthPadding;
            float wallCenterY = rollSurfaceObject.transform.position.y + rollArenaThickness * 0.5f + wallHeight * 0.5f;

            for (int i = 0; i < wallSegmentCount; i++)
            {
                float angle = (Mathf.PI * 2f / wallSegmentCount) * i;
                Vector3 outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 wallPosition = new Vector3(rollCenter.x, wallCenterY, rollCenter.z) + outward * radius;

                GameObject wallObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wallObject.name = $"ArenaWall_{i:D2}";
                wallObject.transform.SetParent(wallRootObject.transform, false);
                wallObject.transform.position = wallPosition;
                wallObject.transform.rotation = Quaternion.LookRotation(-outward, Vector3.up);
                wallObject.transform.localScale = new Vector3(wallLength, wallHeight, wallThickness);

                Renderer wallRenderer = wallObject.GetComponent<Renderer>();
                if (wallRenderer != null)
                {
                    wallRenderer.enabled = false;
                }
            }
        }

        private void PrepareDiceForNextRoll()
        {
            if (diceObject == null)
            {
                return;
            }

            diceObject.transform.rotation = UnityEngine.Random.rotation;
        }

        private void PlaceDiceAtDefaultPosition()
        {
            if (diceObject == null)
            {
                return;
            }

            diceObject.transform.position = rollCenter + new Vector3(0f, 1.5f, 0f);
            diceObject.transform.rotation = UnityEngine.Random.rotation;
        }

        private int ReadTopFaceValue()
        {
            Dictionary<int, Vector3> faceDirections = new Dictionary<int, Vector3>
            {
                { 1, diceObject.transform.up },
                { 6, -diceObject.transform.up },
                { 2, diceObject.transform.right },
                { 5, -diceObject.transform.right },
                { 3, diceObject.transform.forward },
                { 4, -diceObject.transform.forward }
            };

            int bestValue = 1;
            float bestDot = float.MinValue;

            foreach (KeyValuePair<int, Vector3> pair in faceDirections)
            {
                float dot = Vector3.Dot(pair.Value.normalized, Vector3.up);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    bestValue = pair.Key;
                }
            }

            return bestValue;
        }

        private void CreateFaceLabel(int value, Vector3 localPosition, Quaternion localRotation)
        {
            GameObject labelObject = new GameObject($"Face_{value}");
            labelObject.transform.SetParent(diceObject.transform, false);
            labelObject.transform.localPosition = localPosition;
            labelObject.transform.localRotation = localRotation;

            TextMesh textMesh = labelObject.AddComponent<TextMesh>();
            textMesh.text = value.ToString();
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.characterSize = 0.18f;
            textMesh.fontSize = 64;
            textMesh.color = new Color(0.12f, 0.12f, 0.12f);
        }
    }
}
