# Phase 6 — Gas Leak & Confined Space AR Training Module Implementation Report

## Executive Summary
Phase 6 implements the **Gas Leak & Confined Space AR Safety Training Module** for SurakshaAR in `D:\AR-mining\SurakshaAR`.

This is the second complete AR safety training module in SurakshaAR, built upon the True Camera-Based AR architecture, 100% software-simulated gas hazards, deterministic 8-step safety verification, PPE selection validation, confined space authorization checks, stand-by buddy system verification, audio-haptic alerts, movement tracking, and offline AI-adaptive training engine.

---

## 1. Zero IoT & 100% Software-Simulated Gas Architecture

> [!IMPORTANT]
> **NO IoT HARDWARE DEPENDENCY:**
> There is NO physical gas sensor, Arduino, ESP32, Raspberry Pi, Bluetooth detector, external hardware device, or sensor API. All gas concentration levels (`SAFE`, `LOW`, `MEDIUM`, `HIGH`, `CRITICAL`), gas clouds, multi-gas detector readings, alarms, and hazards are **100% software-simulated AR elements**.

---

## 2. True Camera-Based AR Integration
- **Live Camera Feed:** Rear camera view covers 85–90% of screen space; no virtual rooms, virtual factory walls, virtual mine tunnels, or pre-rendered videos exist.
- **Real Floor Detection:** Uses `ARPlaneManager` & `ARRaycastManager` to scan physical ground plane and establish `TRAINING_ORIGIN`.
- **Spatial Anchoring:** Uses `ARAnchorManager` to anchor gas hazard clouds, gas detectors, PPE stations, confined space markers, and safe assembly zones to physical real-world coordinates.
- **Physical Movement:** Reuses `MovementTrackingManager` (`ARCamera.transform.position` displacement tracking, **NO GPS**). Distance decreases as worker physically walks toward detectors, exits, and assembly zones.

---

## 3. Deterministic 8-Step Safety Procedure Sequence

1. **Step 1 — Recognize Gas Hazard:** Locate & select 3D AR Gas Leak hazard source on real ground.
2. **Step 2 — Raise Alarm / Warn Crew:** Activate emergency call point button to sound site-wide warning.
3. **Step 3 — Identify Safe Exit:** Evaluate & select unblocked upwind emergency exit route.
4. **Step 4 — Select Required PPE:** Equip SCBA/Respirator, Safety Helmet, Goggles, and Protective Gloves at PPE station; filter out standard dust mask distractors.
5. **Step 5 — Check Gas Detector:** Locate AR multi-gas detector and trigger 1.5s simulated scan animation to obtain scenario reading.
6. **Step 6 — Confined Space Protocol:** Verify confined space hazard entry permit conditions and prerequisite safety checks.
7. **Step 7 — Buddy Procedure:** Confirm stand-by safety monitor presence, test two-way radio link, and authorize entry.
8. **Step 8 — Reach Safe Assembly Area:** Physically walk to outdoor safe assembly beacon.

---

## 4. C# Component Architecture (`SurakshaAR.Training.Gas`)

| Component | Responsibility |
| :--- | :--- |
| [`GasLeakStepType.cs`](file:///D:/AR-mining/SurakshaAR/Assets/_Project/Scripts/Training/Gas/GasLeakStepType.cs) | Enums for 8-step safety sequence and software gas levels (`SAFE`, `LOW`, `MEDIUM`, `HIGH`, `CRITICAL`). |
| [`GasLeakScenarioManager.cs`](file:///D:/AR-mining/SurakshaAR/Assets/_Project/Scripts/Training/Gas/GasLeakScenarioManager.cs) | Configures `BEGINNER`, `INTERMEDIATE`, and `ADVANCED` gas leak difficulty profiles. |
| [`PPESelectionManager.cs`](file:///D:/AR-mining/SurakshaAR/Assets/_Project/Scripts/Training/Gas/PPESelectionManager.cs) | Manages AR/UI PPE selection and rejects dust mask distractors with contextual feedback. |
| [`ConfinedSpaceManager.cs`](file:///D:/AR-mining/SurakshaAR/Assets/_Project/Scripts/Training/Gas/ConfinedSpaceManager.cs) | Enforces prerequisite safety checks before authorizing confined space entry. |
| [`BuddyProcedureManager.cs`](file:///D:/AR-mining/SurakshaAR/Assets/_Project/Scripts/Training/Gas/BuddyProcedureManager.cs) | Simulates stand-by buddy check-in, radio communication test, and entry confirmation. |
| [`GasHazard.cs`](file:///D:/AR-mining/SurakshaAR/Assets/_Project/Scripts/Training/Gas/GasHazard.cs) | Lightweight translucent gas cloud visualizer updating opacity/scale based on simulated gas level. |
| [`GasDetectorAR.cs`](file:///D:/AR-mining/SurakshaAR/Assets/_Project/Scripts/Training/Gas/GasDetectorAR.cs) | Spatial AR gas detector device returning simulated scenario gas readings. |
| [`GasTelemetryLogger.cs`](file:///D:/AR-mining/SurakshaAR/Assets/_Project/Scripts/Training/Gas/GasTelemetryLogger.cs) | Offline JSON session recorder saving telemetry to `Application.persistentDataPath/Telemetry/Gas/`. |
| [`GasLeakController.cs`](file:///D:/AR-mining/SurakshaAR/Assets/_Project/Scripts/Training/Gas/GasLeakController.cs) | Central state machine orchestrating steps, mistakes, adaptive hints, audio-haptics, and scoring. |
| [`GasLearnPlaceholderController.cs`](file:///D:/AR-mining/SurakshaAR/Assets/_Project/Scripts/UI/Screens/GasLearnPlaceholderController.cs) | In-game HUD UI overlay (<15% screen space) displaying step counter, simulated gas status badge, and action buttons. |

---

## 5. Offline Persistent Telemetry Model
Saved to `Application.persistentDataPath/Telemetry/Gas/GasSession_<SessionID>_<Timestamp>.json`:
```json
{
  "sessionId": "a1b2c3d4",
  "workerId": "WRK001",
  "moduleName": "Gas Leak & Confined Space",
  "startTime": "2026-09-05T19:20:00Z",
  "endTime": "2026-09-05T19:24:30Z",
  "difficultyProfile": "BEGINNER",
  "overallScore": 1150,
  "independentAccuracyPercent": 100.0,
  "assistedAccuracyPercent": 100.0,
  "totalMistakes": 0,
  "totalRetries": 0,
  "sequenceErrors": 0
}
```

---

## 6. Build Artifacts & Output Target
- **Compiled APK:** [`D:\AR-mining\SurakshaAR\Builds\SurakshaAR_Phase6_GasConfinedSpace.apk`](file:///D:/AR-mining/SurakshaAR/Builds/SurakshaAR_Phase6_GasConfinedSpace.apk)
- **Target Target Hardware:** `vivo V2576` (Android 16, API 36, `arm64-v8a`).
