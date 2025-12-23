// Assets/Tests/PlayMode/XRScenarioTests.cs
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.XR.CoreUtils;

namespace XREALTests.PlayMode
{
    /// <summary>
    /// シナリオベースのE2Eテスト
    /// XR Device Simulatorを使用して、決まった経路でカメラを移動させる
    /// </summary>
    public class XRScenarioTests
    {
        private GameObject xrOriginObject;
        private XROrigin xrOrigin;
        private Camera xrCamera;
        private Transform cameraTransform;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // XR Origin セットアップ
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
            cameraTransform = xrCamera.transform;

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
        /// シナリオ1: 3つの地点を順に移動して、各地点でオブジェクトを確認
        /// </summary>
        [UnityTest]
        public IEnumerator Scenario_MoveToThreeWaypointsAndVerifyObjects()
        {
            // Arrange - テスト用オブジェクトを配置
            var waypoint1Object = CreateTestCube(new Vector3(0, 0, 5), "Waypoint1");
            var waypoint2Object = CreateTestCube(new Vector3(5, 0, 5), "Waypoint2");
            var waypoint3Object = CreateTestCube(new Vector3(5, 0, 0), "Waypoint3");

            // ウェイポイント定義
            var waypoints = new List<Waypoint>
            {
                new Waypoint(new Vector3(0, 1.6f, 0), new Vector3(0, 0, 0), "Start"),
                new Waypoint(new Vector3(0, 1.6f, 3), new Vector3(0, 0, 0), "Approach Waypoint1"),
                new Waypoint(new Vector3(3, 1.6f, 3), new Vector3(0, 45, 0), "Approach Waypoint2"),
                new Waypoint(new Vector3(3, 1.6f, 0), new Vector3(0, 90, 0), "Approach Waypoint3")
            };

            // Act & Assert - 各ウェイポイントに移動
            foreach (var waypoint in waypoints)
            {
                yield return MoveToWaypoint(waypoint);

                Debug.Log($"Reached: {waypoint.name} at {cameraTransform.position}");

                // 各地点での検証
                if (waypoint.name.Contains("Waypoint1"))
                {
                    Assert.IsTrue(IsObjectInView(waypoint1Object),
                        "Waypoint1 object should be visible");
                }
                else if (waypoint.name.Contains("Waypoint2"))
                {
                    Assert.IsTrue(IsObjectInView(waypoint2Object),
                        "Waypoint2 object should be visible");
                }
                else if (waypoint.name.Contains("Waypoint3"))
                {
                    Assert.IsTrue(IsObjectInView(waypoint3Object),
                        "Waypoint3 object should be visible");
                }
            }

            // Cleanup
            Object.Destroy(waypoint1Object);
            Object.Destroy(waypoint2Object);
            Object.Destroy(waypoint3Object);
        }

        /// <summary>
        /// シナリオ2: 円周上を移動しながら、中央のオブジェクトを見続ける
        /// </summary>
        [UnityTest]
        public IEnumerator Scenario_OrbitAroundCenterObject()
        {
            // Arrange
            var centerObject = CreateTestCube(Vector3.zero, "CenterObject");
            centerObject.transform.localScale = Vector3.one * 2f;

            int steps = 8; // 8ステップで一周
            float radius = 5f;
            float height = 1.6f;

            // Act - 円周上を移動
            for (int i = 0; i < steps; i++)
            {
                float angle = (360f / steps) * i;
                float radians = angle * Mathf.Deg2Rad;

                Vector3 position = new Vector3(
                    Mathf.Cos(radians) * radius,
                    height,
                    Mathf.Sin(radians) * radius
                );

                // 中央を向く
                Vector3 lookDirection = (Vector3.zero - position).normalized;
                Vector3 rotation = Quaternion.LookRotation(lookDirection).eulerAngles;

                var waypoint = new Waypoint(position, rotation, $"Orbit {i + 1}/{steps}");
                yield return MoveToWaypoint(waypoint);

                // Assert - 中央のオブジェクトが常に見えているか
                Assert.IsTrue(IsObjectInView(centerObject),
                    $"Center object should be visible at orbit position {i + 1}");

                Debug.Log($"Orbit step {i + 1}/{steps}: Camera at {position}, looking at center");
            }

            Object.Destroy(centerObject);
        }

        /// <summary>
        /// シナリオ3: 高さを変えながら移動して、各高さでのUI表示を確認
        /// </summary>
        [UnityTest]
        public IEnumerator Scenario_VerticalMovementWithUICheck()
        {
            // Arrange - 高さごとに異なる位置にUIを配置
            var lowUI = CreateUICanvas(new Vector3(0, 0.5f, 3), "LowUI");
            var midUI = CreateUICanvas(new Vector3(0, 1.6f, 3), "MidUI");
            var highUI = CreateUICanvas(new Vector3(0, 2.5f, 3), "HighUI");

            var heights = new float[] { 0.5f, 1.6f, 2.5f };

            // Act - 各高さに移動
            for (int i = 0; i < heights.Length; i++)
            {
                var waypoint = new Waypoint(
                    new Vector3(0, heights[i], 0),
                    new Vector3(0, 0, 0),
                    $"Height {heights[i]}m"
                );

                yield return MoveToWaypoint(waypoint);

                // Assert - 対応する高さのUIが見えるか
                if (i == 0)
                {
                    Assert.IsTrue(IsObjectInView(lowUI), "Low UI should be visible");
                }
                else if (i == 1)
                {
                    Assert.IsTrue(IsObjectInView(midUI), "Mid UI should be visible");
                }
                else if (i == 2)
                {
                    Assert.IsTrue(IsObjectInView(highUI), "High UI should be visible");
                }

                Debug.Log($"At height {heights[i]}m");
            }

            Object.Destroy(lowUI);
            Object.Destroy(midUI);
            Object.Destroy(highUI);
        }

        /// <summary>
        /// シナリオ4: トリガーゾーンに入ったときのイベント検証
        /// </summary>
        [UnityTest]
        public IEnumerator Scenario_TriggerZoneActivation()
        {
            // Arrange - トリガーゾーンを作成
            var triggerZone = new GameObject("TriggerZone");
            triggerZone.transform.position = new Vector3(0, 0, 5);
            var collider = triggerZone.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(2, 3, 2);

            var triggerDetector = triggerZone.AddComponent<TestTriggerDetector>();

            // カメラにコライダーを追加（トリガー検出用）
            var cameraCollider = cameraTransform.gameObject.AddComponent<SphereCollider>();
            cameraCollider.radius = 0.3f;
            var rb = cameraTransform.gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            // Act - トリガーゾーンに向かって移動
            var waypoints = new List<Waypoint>
            {
                new Waypoint(new Vector3(0, 1.6f, 0), Vector3.zero, "Start"),
                new Waypoint(new Vector3(0, 1.6f, 3), Vector3.zero, "Before Trigger"),
                new Waypoint(new Vector3(0, 1.6f, 5), Vector3.zero, "Inside Trigger"),
                new Waypoint(new Vector3(0, 1.6f, 7), Vector3.zero, "After Trigger")
            };

            bool triggeredEnter = false;

            foreach (var waypoint in waypoints)
            {
                yield return MoveToWaypoint(waypoint);

                if (triggerDetector.IsTriggered && !triggeredEnter)
                {
                    triggeredEnter = true;
                    Debug.Log($"Trigger entered at: {waypoint.name}");
                }
            }

            // Assert
            Assert.IsTrue(triggeredEnter, "Should have triggered the zone");

            Object.Destroy(triggerZone);
        }

        /// <summary>
        /// シナリオ5: パフォーマンステスト - 移動中のFPS計測
        /// </summary>
        [UnityTest]
        public IEnumerator Scenario_PerformanceDuringMovement()
        {
            // Arrange
            var frameTimes = new List<float>();

            // 複雑なシーンを作成（負荷をかける）
            for (int i = 0; i < 50; i++)
            {
                CreateTestCube(
                    new Vector3(Random.Range(-10f, 10f), Random.Range(0f, 3f), Random.Range(-10f, 10f)),
                    $"Object{i}"
                );
            }

            // 移動経路
            var waypoints = new List<Waypoint>
            {
                new Waypoint(new Vector3(0, 1.6f, 0), Vector3.zero, "Start"),
                new Waypoint(new Vector3(5, 1.6f, 5), Vector3.zero, "Point1"),
                new Waypoint(new Vector3(-5, 1.6f, 5), new Vector3(0, 90, 0), "Point2"),
                new Waypoint(new Vector3(-5, 1.6f, -5), new Vector3(0, 180, 0), "Point3"),
                new Waypoint(new Vector3(0, 1.6f, 0), new Vector3(0, 270, 0), "End")
            };

            // Act - 移動しながらFPSを計測
            foreach (var waypoint in waypoints)
            {
                yield return MoveToWaypointWithPerformanceTracking(waypoint, frameTimes);
            }

            // Assert
            float avgFrameTime = 0f;
            foreach (float ft in frameTimes)
            {
                avgFrameTime += ft;
            }
            avgFrameTime /= frameTimes.Count;
            float avgFPS = 1f / avgFrameTime;

            Debug.Log($"Average FPS during movement: {avgFPS:F2}");
            Debug.Log($"Total frames measured: {frameTimes.Count}");

            Assert.That(avgFPS, Is.GreaterThan(30f),
                $"Should maintain at least 30 FPS during movement. Got {avgFPS:F2}");
        }

        // ===== ヘルパーメソッド =====

        /// <summary>
        /// ウェイポイントまでスムーズに移動
        /// </summary>
        private IEnumerator MoveToWaypoint(Waypoint waypoint, float duration = 1.0f)
        {
            Vector3 startPos = cameraTransform.position;
            Quaternion startRot = cameraTransform.rotation;
            Quaternion targetRot = Quaternion.Euler(waypoint.rotation);

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // スムーズな補間（Ease In-Out）
                t = t * t * (3f - 2f * t);

                cameraTransform.position = Vector3.Lerp(startPos, waypoint.position, t);
                cameraTransform.rotation = Quaternion.Slerp(startRot, targetRot, t);

                yield return null;
            }

            // 最終位置を確定
            cameraTransform.position = waypoint.position;
            cameraTransform.rotation = targetRot;

            // 到着後の待機時間
            yield return new WaitForSeconds(0.2f);
        }

        /// <summary>
        /// パフォーマンストラッキング付き移動
        /// </summary>
        private IEnumerator MoveToWaypointWithPerformanceTracking(Waypoint waypoint, List<float> frameTimes, float duration = 1.0f)
        {
            Vector3 startPos = cameraTransform.position;
            Quaternion startRot = cameraTransform.rotation;
            Quaternion targetRot = Quaternion.Euler(waypoint.rotation);

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = t * t * (3f - 2f * t);

                cameraTransform.position = Vector3.Lerp(startPos, waypoint.position, t);
                cameraTransform.rotation = Quaternion.Slerp(startRot, targetRot, t);

                // フレームタイム記録
                frameTimes.Add(Time.unscaledDeltaTime);

                yield return null;
            }

            cameraTransform.position = waypoint.position;
            cameraTransform.rotation = targetRot;
        }

        /// <summary>
        /// オブジェクトがカメラの視界内にあるか判定
        /// </summary>
        private bool IsObjectInView(GameObject obj)
        {
            if (obj == null) return false;

            Vector3 viewportPoint = xrCamera.WorldToViewportPoint(obj.transform.position);

            // ビューポート内 (0-1の範囲) かつカメラの前方にある
            bool inViewport = viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
                             viewportPoint.y >= 0 && viewportPoint.y <= 1 &&
                             viewportPoint.z > 0;

            if (!inViewport) return false;

            // Raycastで遮蔽物チェック
            Vector3 direction = (obj.transform.position - cameraTransform.position).normalized;
            float distance = Vector3.Distance(cameraTransform.position, obj.transform.position);

            if (Physics.Raycast(cameraTransform.position, direction, out RaycastHit hit, distance + 1f))
            {
                return hit.collider.gameObject == obj || hit.collider.transform.IsChildOf(obj.transform);
            }

            return true;
        }

        /// <summary>
        /// テスト用キューブを作成
        /// </summary>
        private GameObject CreateTestCube(Vector3 position, string name)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = position;
            cube.name = name;

            // 視認性を上げる
            var renderer = cube.GetComponent<Renderer>();
            renderer.material.color = Random.ColorHSV();

            return cube;
        }

        /// <summary>
        /// テスト用UIキャンバスを作成
        /// </summary>
        private GameObject CreateUICanvas(Vector3 position, string name)
        {
            var canvasObj = new GameObject(name);
            canvasObj.transform.position = position;
            canvasObj.transform.rotation = Quaternion.Euler(0, 180, 0); // カメラ側を向く

            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var rectTransform = canvasObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200, 100);
            rectTransform.localScale = Vector3.one * 0.01f;

            // コライダー追加（視界判定用）
            var collider = canvasObj.AddComponent<BoxCollider>();
            collider.size = new Vector3(200, 100, 1);

            return canvasObj;
        }
    }

    // ===== サポートクラス =====

    /// <summary>
    /// ウェイポイントデータ
    /// </summary>
    public class Waypoint
    {
        public Vector3 position;
        public Vector3 rotation;
        public string name;

        public Waypoint(Vector3 pos, Vector3 rot, string n)
        {
            position = pos;
            rotation = rot;
            name = n;
        }
    }

    /// <summary>
    /// トリガー検出用コンポーネント
    /// </summary>
    public class TestTriggerDetector : MonoBehaviour
    {
        public bool IsTriggered { get; private set; }

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log($"Trigger entered by: {other.gameObject.name}");
            IsTriggered = true;
        }

        private void OnTriggerExit(Collider other)
        {
            Debug.Log($"Trigger exited by: {other.gameObject.name}");
            IsTriggered = false;
        }
    }
}
