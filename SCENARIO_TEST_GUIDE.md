# シナリオベースXRテスト実装ガイド

決まった経路でカメラを移動させる自動E2Eテストの実装方法

## 概要

このガイドでは、XR Device Simulatorを使って、**プログラマティックに決まった地点を順に移動**させるテストの実装方法を説明します。

## 提供されるテストクラス

### 1. XRScenarioTests.cs
シナリオベースの高レベルE2Eテスト

### 2. XRInputSimulationTests.cs
低レベル入力シミュレーションテスト

---

## 実装されたシナリオ

### 📍 シナリオ1: ウェイポイント巡回テスト

**用途**: 特定の地点を順に訪問し、各地点でオブジェクトの表示を確認

```csharp
[UnityTest]
public IEnumerator Scenario_MoveToThreeWaypointsAndVerifyObjects()
```

**動作**:
1. 3つのウェイポイントを定義
2. 各ウェイポイントにスムーズに移動
3. 到着時に対応するオブジェクトが視界内にあるか検証

**実行例**:
```
Start (0, 1.6, 0)
  ↓ 1秒かけて移動
Waypoint1付近 (0, 1.6, 3)
  → Waypoint1オブジェクトが見えるか確認 ✓
  ↓ 1秒かけて移動
Waypoint2付近 (3, 1.6, 3)
  → Waypoint2オブジェクトが見えるか確認 ✓
  ↓ 1秒かけて移動
Waypoint3付近 (3, 1.6, 0)
  → Waypoint3オブジェクトが見えるか確認 ✓
```

**カスタマイズ例**:
```csharp
var waypoints = new List<Waypoint>
{
    new Waypoint(new Vector3(0, 1.6f, 0), Vector3.zero, "スタート地点"),
    new Waypoint(new Vector3(5, 1.6f, 5), new Vector3(0, 45, 0), "展示物A"),
    new Waypoint(new Vector3(10, 1.6f, 0), new Vector3(0, 90, 0), "展示物B"),
};

foreach (var wp in waypoints)
{
    yield return MoveToWaypoint(wp);
    // ここで任意の検証を追加
    Assert.IsTrue(SomeCondition(), "検証メッセージ");
}
```

---

### 🔄 シナリオ2: 円周軌道テスト

**用途**: 中心のオブジェクトの周りを回りながら、常に視界内にあるか確認

```csharp
[UnityTest]
public IEnumerator Scenario_OrbitAroundCenterObject()
```

**動作**:
1. 中心にオブジェクトを配置
2. 半径5mの円周上を8ステップで一周
3. 各地点で中心オブジェクトを向く
4. 常にオブジェクトが見えているか検証

**ユースケース**:
- 360度どの角度からも見えるべきオブジェクトのテスト
- 看板やUIの視認性テスト
- LOD（Level of Detail）切り替えのテスト

**パラメータ調整**:
```csharp
int steps = 16; // より細かくスキャン
float radius = 10f; // より遠くから
float height = 2.0f; // より高い位置から
```

---

### 📏 シナリオ3: 垂直移動テスト

**用途**: 高さを変えながら移動し、各高さでのUI表示を確認

```csharp
[UnityTest]
public IEnumerator Scenario_VerticalMovementWithUICheck()
```

**動作**:
1. 低位置（0.5m）、中位置（1.6m）、高位置（2.5m）にUIを配置
2. カメラが各高さに移動
3. 対応する高さのUIが見えるか確認

**ユースケース**:
- 立った状態・座った状態での表示確認
- 高さに応じたUI配置の検証
- アクセシビリティテスト

---

### 🎯 シナリオ4: トリガーゾーン検出テスト

**用途**: 特定のエリアに入ったときのイベント発火を確認

```csharp
[UnityTest]
public IEnumerator Scenario_TriggerZoneActivation()
```

**動作**:
1. トリガーゾーン（BoxCollider）を配置
2. カメラにSphereColliderを追加
3. ゾーンに向かって移動
4. トリガーイベントが発火するか検証

**ユースケース**:
- エリア進入時のチュートリアル表示
- ゲートやドアの自動開閉
- 特定エリアでの機能有効化

**応用例**:
```csharp
// 複数のトリガーゾーンを順に通過
var zones = new[] { "EntryZone", "MiddleZone", "ExitZone" };
foreach (var zoneName in zones)
{
    yield return MoveToZone(zoneName);
    Assert.IsTrue(IsZoneTriggered(zoneName));
}
```

---

### ⚡ シナリオ5: 移動中パフォーマンステスト

**用途**: 移動しながらFPSを計測し、性能劣化を検出

```csharp
[UnityTest]
public IEnumerator Scenario_PerformanceDuringMovement()
```

**動作**:
1. 50個のオブジェクトをランダム配置（負荷）
2. 5地点を巡回
3. 全フレームのフレームタイムを記録
4. 平均FPSが30fps以上か検証

**ユースケース**:
- 重いシーンでの性能確認
- 最適化のリグレッション検出
- 許容できるオブジェクト数の把握

**閾値調整**:
```csharp
Assert.That(avgFPS, Is.GreaterThan(60f), "60fps維持"); // より厳しく
Assert.That(avgFPS, Is.GreaterThan(20f), "20fps維持"); // より緩く
```

---

### 👁️ シナリオ6: 視線追跡シミュレーション

**用途**: 複数のターゲットを順に見て、視界の中心に来るか確認

```csharp
[UnityTest]
public IEnumerator SimulateGaze_LookAtMultipleTargets()
```

**動作**:
1. 東西南北にターゲット配置
2. 各ターゲットに視線を向ける
3. ターゲットがビューポートの中心（0.4-0.6の範囲）にあるか確認

**ユースケース**:
- 視線UIの動作確認
- Gaze入力のテスト
- 注視点ベースのインタラクション

---

### 🚶 シナリオ7: 歩行シミュレーション

**用途**: 自然な頭の揺れを含む歩行をシミュレート

```csharp
[UnityTest]
public IEnumerator SimulateWalking_WithHeadBobbing()
```

**動作**:
1. 10m先まで3秒かけて歩く
2. 歩行に伴う上下・左右の揺れを再現
3. 途中のマーカー通過を検証

**リアリズム**:
- 上下動: ±5cm（Bobbing）
- 左右動: ±1.5cm
- ピッチ/ロール回転: ±2度

**パラメータ**:
```csharp
float bobFrequency = 2.5f; // 歩行速度（速くするほど速足）
float bobAmount = 0.05f; // 揺れの大きさ
```

---

### 🔍 シナリオ8: 360度スキャン

**用途**: 周囲360度を回転して、全オブジェクトを検出

```csharp
[UnityTest]
public IEnumerator Simulate360Scan_DetectAllSurroundingObjects()
```

**動作**:
1. 周囲に8個のオブジェクトを配置
2. 10度ずつ回転（計36ステップ）
3. 全オブジェクトを検出できたか確認

**ユースケース**:
- オブジェクトのカリング検証
- 背後のオブジェクトのロード確認
- ミニマップやレーダーのテスト

---

## コアヘルパー関数

### MoveToWaypoint()

```csharp
private IEnumerator MoveToWaypoint(Waypoint waypoint, float duration = 1.0f)
```

**機能**:
- 指定したウェイポイントまでスムーズに移動
- Ease In-Out補間（加速→減速）
- 位置と回転を同時に補間

**パラメータ**:
- `waypoint`: 目標地点（位置、回転、名前）
- `duration`: 移動にかける時間（秒）

**使用例**:
```csharp
var wp = new Waypoint(new Vector3(5, 1.6f, 5), new Vector3(0, 45, 0), "地点A");
yield return MoveToWaypoint(wp, 2.0f); // 2秒かけて移動
```

---

### IsObjectInView()

```csharp
private bool IsObjectInView(GameObject obj)
```

**機能**:
- オブジェクトがカメラの視界内にあるか判定
- ビューポート範囲チェック（0-1）
- Raycastによる遮蔽物チェック

**判定条件**:
1. ビューポート内（画面内）
2. カメラの前方にある（z > 0）
3. 遮蔽物がない

**使用例**:
```csharp
if (IsObjectInView(targetObject))
{
    Debug.Log("ターゲットが見えています");
}
```

---

### CreateTestCube() / CreateTarget()

```csharp
private GameObject CreateTestCube(Vector3 position, string name)
```

**機能**:
- テスト用のキューブ/球体を生成
- ランダムな色を割り当て（視認性）
- コライダー付き

---

## カスタムシナリオの作成方法

### 基本テンプレート

```csharp
[UnityTest]
public IEnumerator MyCustomScenario()
{
    // ===== 1. Arrange: テスト環境のセットアップ =====
    var targetObject = CreateTestCube(new Vector3(0, 1.6f, 5), "Target");

    // ===== 2. Act: シナリオ実行 =====
    var waypoints = new List<Waypoint>
    {
        new Waypoint(new Vector3(0, 1.6f, 0), Vector3.zero, "Start"),
        new Waypoint(new Vector3(0, 1.6f, 3), Vector3.zero, "Near Target"),
    };

    foreach (var wp in waypoints)
    {
        yield return MoveToWaypoint(wp);

        // ===== 3. Assert: 各地点での検証 =====
        if (wp.name == "Near Target")
        {
            Assert.IsTrue(IsObjectInView(targetObject),
                "ターゲットが見えるはず");
        }
    }

    // ===== 4. Cleanup: 後片付け =====
    Object.Destroy(targetObject);
}
```

---

## 実践例: ミュージアム展示ツアー

```csharp
[UnityTest]
public IEnumerator MuseumTour_VisitAllExhibits()
{
    // 展示物を配置
    var exhibits = new Dictionary<string, GameObject>
    {
        ["恐竜化石"] = CreateExhibit(new Vector3(0, 1.6f, 5)),
        ["古代遺物"] = CreateExhibit(new Vector3(5, 1.6f, 5)),
        ["現代アート"] = CreateExhibit(new Vector3(5, 1.6f, 0)),
    };

    // 見学ルート
    var tour = new List<Waypoint>
    {
        new Waypoint(new Vector3(0, 1.6f, 0), Vector3.zero, "エントランス"),
        new Waypoint(new Vector3(0, 1.6f, 3), new Vector3(0, 0, 0), "恐竜化石前"),
        new Waypoint(new Vector3(3, 1.6f, 5), new Vector3(0, 45, 0), "移動中"),
        new Waypoint(new Vector3(5, 1.6f, 3), new Vector3(0, 90, 0), "古代遺物前"),
        new Waypoint(new Vector3(5, 1.6f, 1), new Vector3(0, 135, 0), "現代アート前"),
    };

    var visitedExhibits = new HashSet<string>();

    foreach (var wp in tour)
    {
        yield return MoveToWaypoint(wp, 1.5f);

        // 視界内の展示物を記録
        foreach (var exhibit in exhibits)
        {
            if (IsObjectInView(exhibit.Value))
            {
                visitedExhibits.Add(exhibit.Key);
                Debug.Log($"展示物「{exhibit.Key}」を鑑賞");
            }
        }

        yield return new WaitForSeconds(0.5f); // 鑑賞時間
    }

    // 全展示物を見たか確認
    Assert.AreEqual(exhibits.Count, visitedExhibits.Count,
        "全ての展示物を見学すべき");

    foreach (var exhibit in exhibits.Values)
    {
        Object.Destroy(exhibit);
    }
}
```

---

## トラブルシューティング

### Q: カメラが移動しない

**原因**: XR Originの構造が正しくない

**解決**:
```csharp
// SetUp()で正しい階層を作成
XR Origin (GameObject)
└─ Camera Offset (GameObject)
   └─ Main Camera (Camera component)
```

---

### Q: オブジェクトが見えているはずなのにIsObjectInView()がfalseを返す

**原因**: Raycastがコライダーに当たっていない

**解決**:
```csharp
// オブジェクトにコライダーがあるか確認
var cube = GameObject.CreatePrimitive(PrimitiveType.Cube); // コライダー自動付与
```

---

### Q: 移動が一瞬で完了してしまう

**原因**: durationが0または非常に小さい

**解決**:
```csharp
yield return MoveToWaypoint(wp, 1.0f); // 最低0.5秒以上推奨
```

---

### Q: FPSテストが常に失敗する

**原因**: エディタでの実行で負荷が高い

**解決**:
```csharp
#if UNITY_EDITOR
    Assert.That(avgFPS, Is.GreaterThan(15f), "エディタでは15fps");
#else
    Assert.That(avgFPS, Is.GreaterThan(30f), "実機では30fps");
#endif
```

---

## ベストプラクティス

### 1. 移動速度は現実的に
```csharp
// ❌ 速すぎる
yield return MoveToWaypoint(wp, 0.1f);

// ✅ 自然な速度
yield return MoveToWaypoint(wp, 1.0f);
```

### 2. 到着後に待機時間を設ける
```csharp
yield return MoveToWaypoint(wp);
yield return new WaitForSeconds(0.3f); // 安定化待ち
Assert.IsTrue(condition); // この時点で検証
```

### 3. デバッグログを活用
```csharp
Debug.Log($"[{wp.name}] 位置={cameraTransform.position}, 回転={cameraTransform.rotation.eulerAngles}");
```

### 4. ビューポートの余裕を持たせる
```csharp
// ❌ 厳密すぎる
Assert.That(viewportPoint.x, Is.EqualTo(0.5f));

// ✅ 余裕を持たせる
Assert.That(viewportPoint.x, Is.InRange(0.4f, 0.6f));
```

---

## まとめ

このガイドで提供されたシナリオテストは、以下を自動化できます：

✅ 決まった経路でのカメラ移動
✅ 各地点でのオブジェクト視認性確認
✅ トリガーゾーン検出
✅ パフォーマンス計測
✅ 360度スキャン
✅ 自然な歩行シミュレーション

これらを組み合わせることで、XREAL向けの包括的なE2Eテストスイートを構築できます。
