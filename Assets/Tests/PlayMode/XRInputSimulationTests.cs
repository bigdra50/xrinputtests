// Assets/Tests/PlayMode/XRInputSimulationTests.cs
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR;
using Unity.XR.CoreUtils;

#if UNITY_INPUT_SYSTEM_AVAILABLE
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
#endif

namespace XREALTests.PlayMode
{
    /// <summary>
    /// Input Systemを使った入力シミュレーションテスト
    /// XR Device Simulatorがなくても動作する低レベル入力制御
    /// </summary>
    public class XRInputSimulationTests
    {
        private GameObject xrOriginObject;
        private XROrigin xrOrigin;
        private Camera xrCamera;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            xrOriginObject = new GameObject("XR Origin");
            xrOrigin = xrOriginObject.AddComponent<XROrigin>();

            var cameraOffset = new GameObject("Camera Offset");
            cameraOffset.transform.SetParent(xrOriginObject.transform);
            xrOrigin.CameraFloorOffsetObject = cameraOffset;

            var mainCamera = new GameObject("Main Camera");
            mainCamera.tag = "MainCamera";
            xrCamera = mainCamera.AddComponent<Camera>();
            mainCamera.transform.SetParent(cameraOffset.transform);
            xrOrigin.Camera = xrCamera;

            yield return new WaitForSeconds(0.5f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (xrOriginObject != null)
            {
                Object.Destroy(xrOriginObject);
            }
            yield return null;
        }

        /// <summary>
        /// 複数のトランスフォームを順に適用
        /// </summary>
        [UnityTest]
        public IEnumerator SimulateXRInput_SequentialTransforms()
        {
            // 実際のXRデバイスがある場合のみテスト
            var hmdDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (!hmdDevice.isValid)
            {
                // デバイスがない場合は手動でカメラを制御
                var transforms = new[]
                {
                    new XRTransform { position = new Vector3(0, 1.6f, 0), rotation = Quaternion.identity },
                    new XRTransform { position = new Vector3(1, 1.6f, 1), rotation = Quaternion.Euler(0, 45, 0) },
                    new XRTransform { position = new Vector3(2, 1.6f, 2), rotation = Quaternion.Euler(0, 90, 0) },
                    new XRTransform { position = new Vector3(2, 1.8f, 2), rotation = Quaternion.Euler(15, 90, 0) },
                };

                foreach (var xform in transforms)
                {
                    yield return ApplyXRTransform(xform);

                    Debug.Log($"Applied transform: pos={xform.position}, rot={xform.rotation.eulerAngles}");

                    // 各地点での検証
                    Assert.AreEqual(xform.position, xrCamera.transform.position,
                        "Camera position should match expected");
                }
            }
            else
            {
                Assert.Ignore("Real XR device detected - skipping manual simulation");
            }
        }

        /// <summary>
        /// 視線追跡シミュレーション - 特定のオブジェクトを見る
        /// </summary>
        [UnityTest]
        public IEnumerator SimulateGaze_LookAtMultipleTargets()
        {
            // Arrange - 複数のターゲットを配置
            var targets = new[]
            {
                CreateTarget(new Vector3(0, 1.6f, 5), "Target North"),
                CreateTarget(new Vector3(5, 1.6f, 0), "Target East"),
                CreateTarget(new Vector3(0, 1.6f, -5), "Target South"),
                CreateTarget(new Vector3(-5, 1.6f, 0), "Target West"),
            };

            Vector3 headPosition = new Vector3(0, 1.6f, 0);

            // Act - 各ターゲットを順に見る
            foreach (var target in targets)
            {
                Vector3 lookDirection = (target.transform.position - headPosition).normalized;
                Quaternion lookRotation = Quaternion.LookRotation(lookDirection);

                var xform = new XRTransform
                {
                    position = headPosition,
                    rotation = lookRotation
                };

                yield return ApplyXRTransform(xform, 0.5f);

                // Assert - ターゲットが視界の中心にいるか
                Vector3 viewportPoint = xrCamera.WorldToViewportPoint(target.transform.position);

                Assert.That(viewportPoint.x, Is.InRange(0.4f, 0.6f),
                    $"{target.name} should be horizontally centered");
                Assert.That(viewportPoint.y, Is.InRange(0.4f, 0.6f),
                    $"{target.name} should be vertically centered");
                Assert.That(viewportPoint.z, Is.GreaterThan(0),
                    $"{target.name} should be in front of camera");

                Debug.Log($"Looking at {target.name}: viewport={viewportPoint}");

                yield return new WaitForSeconds(0.3f);
            }

            // Cleanup
            foreach (var target in targets)
            {
                Object.Destroy(target);
            }
        }

        /// <summary>
        /// 歩行シミュレーション - 自然な頭の揺れを含む移動
        /// </summary>
        [UnityTest]
        public IEnumerator SimulateWalking_WithHeadBobbing()
        {
            // Arrange
            Vector3 startPos = new Vector3(0, 1.6f, 0);
            Vector3 endPos = new Vector3(0, 1.6f, 10);
            float walkDuration = 3.0f; // 3秒かけて歩く

            // 歩行中に通過する地点にマーカーを配置
            var marker1 = CreateTarget(new Vector3(0, 1.6f, 3), "Marker 3m");
            var marker2 = CreateTarget(new Vector3(0, 1.6f, 7), "Marker 7m");

            bool marker1Passed = false;
            bool marker2Passed = false;

            // Act - 頭の揺れを伴う歩行シミュレーション
            float elapsed = 0f;
            while (elapsed < walkDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / walkDuration;

                // 基本的な移動
                Vector3 position = Vector3.Lerp(startPos, endPos, t);

                // 頭の上下動（歩行による揺れ）
                float bobFrequency = 2.5f; // 歩行の周波数
                float bobAmount = 0.05f; // 揺れの大きさ
                position.y += Mathf.Sin(t * walkDuration * bobFrequency * Mathf.PI * 2) * bobAmount;

                // 左右の揺れ
                position.x += Mathf.Sin(t * walkDuration * bobFrequency * Mathf.PI) * bobAmount * 0.3f;

                // 視線の微妙な揺れ
                Quaternion rotation = Quaternion.Euler(
                    Mathf.Sin(t * walkDuration * bobFrequency * Mathf.PI * 2) * 2f, // ピッチ
                    0, // ヨー
                    Mathf.Sin(t * walkDuration * bobFrequency * Mathf.PI) * 1f // ロール
                );

                xrCamera.transform.position = position;
                xrCamera.transform.rotation = rotation;

                // マーカー通過チェック
                if (!marker1Passed && position.z >= 3f)
                {
                    marker1Passed = true;
                    Debug.Log($"Passed Marker 1 at t={t:F2}");
                }

                if (!marker2Passed && position.z >= 7f)
                {
                    marker2Passed = true;
                    Debug.Log($"Passed Marker 2 at t={t:F2}");
                }

                yield return null;
            }

            // Assert
            Assert.IsTrue(marker1Passed, "Should have passed marker 1");
            Assert.IsTrue(marker2Passed, "Should have passed marker 2");
            Assert.That(xrCamera.transform.position.z, Is.InRange(9.5f, 10.5f),
                "Should have reached destination");

            Object.Destroy(marker1);
            Object.Destroy(marker2);
        }

        /// <summary>
        /// 360度回転スキャン - 周囲のオブジェクトを全て検出
        /// </summary>
        [UnityTest]
        public IEnumerator Simulate360Scan_DetectAllSurroundingObjects()
        {
            // Arrange - 周囲にオブジェクトを配置
            int objectCount = 8;
            var surroundingObjects = new GameObject[objectCount];
            float radius = 5f;

            for (int i = 0; i < objectCount; i++)
            {
                float angle = (360f / objectCount) * i;
                float rad = angle * Mathf.Deg2Rad;
                Vector3 pos = new Vector3(Mathf.Cos(rad) * radius, 1.6f, Mathf.Sin(rad) * radius);

                surroundingObjects[i] = CreateTarget(pos, $"Object_{i}");
            }

            Vector3 centerPosition = new Vector3(0, 1.6f, 0);
            var detectedObjects = new System.Collections.Generic.HashSet<GameObject>();

            // Act - 360度スキャン
            int scanSteps = 36; // 10度ずつ
            for (int i = 0; i <= scanSteps; i++)
            {
                float angle = (360f / scanSteps) * i;
                Quaternion rotation = Quaternion.Euler(0, angle, 0);

                var xform = new XRTransform
                {
                    position = centerPosition,
                    rotation = rotation
                };

                yield return ApplyXRTransform(xform, 0.05f);

                // 現在の視界内のオブジェクトをチェック
                foreach (var obj in surroundingObjects)
                {
                    if (IsInCameraView(obj))
                    {
                        if (detectedObjects.Add(obj))
                        {
                            Debug.Log($"Detected {obj.name} at angle {angle:F1}°");
                        }
                    }
                }
            }

            // Assert
            Assert.AreEqual(objectCount, detectedObjects.Count,
                $"Should have detected all {objectCount} objects during 360° scan");

            // Cleanup
            foreach (var obj in surroundingObjects)
            {
                Object.Destroy(obj);
            }
        }

        // ===== ヘルパーメソッド =====

        private IEnumerator ApplyXRTransform(XRTransform xform, float duration = 0.3f)
        {
            Vector3 startPos = xrCamera.transform.position;
            Quaternion startRot = xrCamera.transform.rotation;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                xrCamera.transform.position = Vector3.Lerp(startPos, xform.position, t);
                xrCamera.transform.rotation = Quaternion.Slerp(startRot, xform.rotation, t);

                yield return null;
            }

            xrCamera.transform.position = xform.position;
            xrCamera.transform.rotation = xform.rotation;
        }

        private GameObject CreateTarget(Vector3 position, string name)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = position;
            sphere.transform.localScale = Vector3.one * 0.5f;
            sphere.name = name;

            var renderer = sphere.GetComponent<Renderer>();
            renderer.material.color = Random.ColorHSV();

            return sphere;
        }

        private bool IsInCameraView(GameObject obj)
        {
            Vector3 viewportPoint = xrCamera.WorldToViewportPoint(obj.transform.position);

            return viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
                   viewportPoint.y >= 0 && viewportPoint.y <= 1 &&
                   viewportPoint.z > 0;
        }

        private struct XRTransform
        {
            public Vector3 position;
            public Quaternion rotation;
        }
    }
}
