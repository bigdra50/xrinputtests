# XREAL PlayMode E2Eテスト セットアップガイド

## 実行環境の選択肢

### 1. Editor単体（最小セットアップ）

**適用範囲**: 初期化、シーン構成、FPSなど基礎テスト

**セットアップ**:
- 特別な設定不要
- Test Runnerから直接実行可能

**実行コマンド**:
```
Window > General > Test Runner > PlayMode > Run All
```

**制限事項**:
- 実際のトラッキングデータは取得不可
- HMDデバイス検出不可
- トラッキング系テストはIgnore（スキップ）される

---

### 2. Editor + XR Device Simulator（推奨）

**適用範囲**: ほぼ全てのテスト（デバイスイベント以外）

**セットアップ**:

#### Step 1: XR Interaction Toolkitのインストール
```
1. Window > Package Manager
2. "Unity Registry"を選択
3. "XR Interaction Toolkit"を検索してインストール
```

#### Step 2: XR Device Simulatorのインポート
```
1. Package Manager > XR Interaction Toolkit > Samples
2. "XR Device Simulator"の横の[Import]をクリック
```

#### Step 3: シーンへの配置
テストシーン（`Assets/Tests/TestScenes/XRTestScene.unity`）に以下を追加:
```
1. Hierarchy右クリック > XR > XR Device Simulator
   または
2. Assets/Samples/XR Interaction Toolkit/.../XR Device Simulator.prefab をドラッグ
```

#### Step 4: Input Systemの有効化（必要な場合）
```
1. Edit > Project Settings > Player > Other Settings
2. Active Input Handling を "Both" または "Input System Package (New)"に変更
3. Editorを再起動
```

**実行方法**:
```
Window > General > Test Runner > PlayMode > Run All
```

**Simulatorの使い方**（手動テスト時）:
- マウス右ドラッグ: 頭の回転
- WASD: 頭の位置移動
- Shift: 下方向移動
- Space: 上方向移動

**テスト結果**:
- 初期化系: ✅ Pass
- トラッキング系: ✅ Pass（シミュレートされたデータ）
- デバイスイベント: ⚠️ Ignore（実機でのみ動作）

---

### 3. 実機ビルド + Unity Test Framework

**適用範囲**: 全てのテスト（実際のXREALデバイス）

**セットアップ**:

#### Step 1: ビルド設定
```
1. File > Build Settings
2. Platform: Android
3. "Add Open Scenes"でテストシーンを追加
4. Switch Platform
```

#### Step 2: テストビルド作成
```
1. Edit > Project Settings > Player
2. Company Name, Product Nameを設定
3. Minimum API Level: Android 7.0 (API 24)以上
```

#### Step 3: コマンドラインからテストビルド
```bash
# Windowsの場合
Unity.exe -runTests -testPlatform Android -testResults results.xml -buildTarget Android

# macOS/Linuxの場合
/Applications/Unity/Unity.app/Contents/MacOS/Unity -runTests -testPlatform Android -testResults results.xml -buildTarget Android
```

**実行方法**:
```
1. XREALグラスとスマートフォンを接続
2. ビルドしたAPKをインストール
3. テストが自動実行され、結果がログ出力される
```

**テスト結果**:
- 全テスト: ✅ Pass（実際のトラッキングデータ）
- 実機特有の問題も検出可能

---

## テストコードの環境判定

コード内で環境を判定してテストを分岐させる例:

```csharp
[UnityTest]
public IEnumerator XRInputDevices_ShouldDetectHeadMountedDisplay()
{
    yield return new WaitForSeconds(1.0f);

    var inputDevices = new List<InputDevice>();
    InputDevices.GetDevicesWithCharacteristics(
        InputDeviceCharacteristics.HeadMounted,
        inputDevices
    );

    // Editor環境の判定
    #if UNITY_EDITOR
        if (!XRSettings.enabled)
        {
            Assert.Ignore("XR not enabled in Editor - use XR Device Simulator for testing");
            yield break;
        }
    #endif

    // 実機またはSimulatorが有効な場合のみ実行
    Assert.IsTrue(inputDevices.Count > 0,
        "Should detect at least one HMD");

    Debug.Log($"Detected HMD: {inputDevices[0].name}");
}
```

---

## 推奨ワークフロー

### 開発フェーズ
```
Editor + XR Device Simulator
↓
素早いイテレーション
トラッキングの基本動作確認
```

### 統合テストフェーズ
```
実機ビルド
↓
実際のXREALデバイスで検証
デバイス固有の問題検出
```

### CI/CD
```
Editor単体テスト（高速）
↓
基本的な初期化、シーン構成を自動検証
PRマージ前の品質チェック
```

---

## トラブルシューティング

### XR Device Simulatorが動作しない
```
原因: Input Systemが有効になっていない
解決: Edit > Project Settings > Player > Active Input Handling を "Both"に変更
```

### テストが全てIgnoreされる
```
原因: XRSettings.enabledがfalse
解決:
1. Edit > Project Settings > XR Plug-in Management
2. 使用するXR SDKを有効化（Mock HMD Loaderなど）
```

### 実機でテストが実行されない
```
原因: Test Frameworkが実機ビルドに含まれていない
解決: Player Settings > Scripting Define Symbols に "UNITY_INCLUDE_TESTS"を追加
```

---

## CI/CD統合例

### GitHub Actions（Editor + Simulator）

```yaml
name: XR PlayMode Tests

on: [push, pull_request]

jobs:
  test-playmode:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - uses: game-ci/unity-test-runner@v2
        with:
          testMode: PlayMode
          artifactsPath: test-results
          customParameters: -enableAllModules

      - uses: actions/upload-artifact@v3
        if: always()
        with:
          name: Test results
          path: test-results
```

この設定で、PRごとに自動的にPlayModeテストが実行されます。
