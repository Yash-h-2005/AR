# SurakshaAR — Phase 2 Setup & Deployment Specification

## 1. Verified Environment & Package Specifications

- **Unity Editor Version:** `6000.3.23f1` (Unity 6 LTS)
- **Target Platform:** Android (Minimum API Level 29 — Android 10.0+)
- **Target Device:** vivo V2576 (Android 16 / API Level 36, `arm64-v8a`)
- **Scripting Backend:** IL2CPP
- **Engine Code Stripping:** Disabled (`PlayerSettings.stripEngineCode = false`)
- **Package Identifier:** `com.surakshaar.training`
- **AR Framework Packages:**
  - `com.unity.xr.arfoundation` — Version `6.0.5`
  - `com.unity.xr.arcore` — Version `6.0.5`
  - `com.unity.ugui` — Version `2.0.0`

---

## 2. Opening & Importing the Project

1. Launch Unity Hub.
2. Click **Add -> Add project from disk**.
3. Select the folder `D:\AR-mining\SurakshaAR`.
4. Ensure Unity Editor `6000.3.23f1` is assigned.
5. Open the project.

---

## 3. Verified Script & Scene Architecture

```
Assets/
├── link.xml                      <- Preserves UnityEngine, UI & XR assemblies from IL2CPP stripping
└── _Project/
    ├── Prefabs/
    │   ├── AR/                   <- AR Plane visualizers & Anchor prefabs
    │   └── UI/                   <- Status HUD & Banner UI prefabs
    ├── Scenes/
    │   └── Bootstrap/
    │       └── ARFoundationTest.unity  <- Verified AR prototype scene
    └── Scripts/
        ├── AR/
        │   ├── ARSessionController.cs
        │   ├── ARPlaneController.cs
        │   ├── ARRaycastPlacementController.cs
        │   └── ARObjectPlacementController.cs
        ├── Editor/
        │   └── BuildScript.cs     <- Automated Android APK build pipeline
        └── UI/
            └── ARUIController.cs
```

---

## 4. Building the Android APK

### Option A: Via Command Line Batch Mode
```powershell
Start-Process -FilePath "C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe" -ArgumentList "-batchmode -nographics -projectPath `"D:\AR-mining\SurakshaAR`" -executeMethod SurakshaAR.Editor.BuildScript.BuildAndroidAPK -quit -logFile `"D:\AR-mining\build_output.log`"" -Wait -NoNewWindow
```

Output: `D:\AR-mining\SurakshaAR\Builds\SurakshaAR_Phase2_Prototype.apk` (110.2 MB)

---

## 5. Installing & Testing on Android (vivo V2576)

### 5.1 ADB Installation
```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe" install -r "D:\AR-mining\SurakshaAR\Builds\SurakshaAR_Phase2_Prototype.apk"
```

### 5.2 Application Launch
```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe" shell monkey -p com.surakshaar.training -c android.intent.category.LAUNCHER 1
```

### 5.3 Verified Physical AR Workflow
1. App launches on the phone; camera permission is granted.
2. Rear camera feed opens and AR Session initializes (`SetGameState: isLoading: false`).
3. Scan floor or table surface; horizontal planes appear.
4. Tap plane; 3D **AR TEST OBJECT** spawns on the real surface.
5. Move phone around room; object stays anchored in physical space.
6. Tap another plane area; object moves cleanly to the new location.
7. Tap non-plane area; "Place on a detected surface" status alert displays without crashing.

---

## 6. Troubleshooting Notes

| Issue | Resolution Applied |
| :--- | :--- |
| **IL2CPP Class ID 115 Stripping Error** | Created `Assets/link.xml` preserving `UnityEngine.CoreModule`, `UnityEngine.UI`, and `UnityEngine.TextCoreModule`, and set `PlayerSettings.stripEngineCode = false`. |
| **BOM Syntax Error in manifest.json** | Saved `Packages/manifest.json` in UTF-8 without BOM encoding (`System.Text.UTF8Encoding($false)`). |
