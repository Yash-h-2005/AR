# SurakshaAR — Phase 4: Fire & Explosion Response Learn Mode Implementation Plan

**Project Location:** `D:\AR-mining\SurakshaAR`  
**Unity Version:** `6000.3.23f1` (Unity 6 LTS)  
**Target Platform:** Android (API Level 29+ ARM64 IL2CPP)  
**Target Device:** vivo V2576 (Android 16 / API Level 36, `arm64-v8a`)  
**Prerequisite:** Phase 3 UI Redesign Verified on Physical Device (PASS)

---

## 1. Executive Summary & Objective

Phase 4 implements the **Fire & Explosion Response Learn Mode**, a hands-on, guided 3D AR vocational training module for mining and manufacturing workers in Jharkhand.

The worker uses a standard Android phone to scan their physical environment, anchor 3D industrial hazards and safety equipment, and physically walk through the 8-step emergency procedure.

---

## 2. The 8-Step Deterministic Safety Sequence

The safety procedure is governed by strict, deterministic rules that cannot be altered or bypassed:

| Step # | Step Name | AR Spatial Action | Interaction Type | Success Condition |
| :---: | :--- | :--- | :---: | :--- |
| **1** | **Identify Fire Hazard** | Recognize 3D fire hazard (electrical box / flammable solvent). | `SELECT / IDENTIFY` | Tap hazard within 5 seconds. |
| **2** | **Raise Alarm** | Locate wall-mounted manual call point (MCP) in AR space. | `ACTIVATE / TAP` | Tap MCP alarm glass to break/press. |
| **3** | **Identify Safe Exit** | Evaluate exit doors; identify unblocked emergency route. | `SELECT SAFE EXIT` | Select green emergency exit door. |
| **4** | **Select Extinguisher** | Choose correct extinguisher type (CO2 vs Water vs Dry Powder). | `SELECT TYPE` | Select CO2/Dry Powder for chemical fire. |
| **5** | **Pick Up & Prepare** | Pick up extinguisher; pull safety pin & release seal. | `GRASP & PULL PIN` | Drag pin away from extinguisher handle. |
| **6** | **Use Extinguisher** | Apply PASS technique: Aim at base of fire & sweep spray nozzle. | `AIM & SPRAY` | Aim raycast at fire base and hold SPRAY. |
| **7** | **Evacuate Safely** | Crouch below smoke layer and move along designated exit path. | `SPATIAL MOVE` | Physical movement toward exit marker. |
| **8** | **Assembly Point** | Reach designated outdoor emergency assembly area. | `REACH TARGET` | Move camera within 1m of assembly beacon. |

---

## 3. Adaptive Assistance & Multi-Tiered Hint Engine

To support workers with low technical literacy without artificially failing them:

```
[ Worker Action ]
       │
       ├── Correct Action ──> Positive Audio/Visual Feedback ──> Next Step
       │
       └── Wrong Action
              │
              ├── 1st Mistake ──> Tier 1: Contextual Tip Banner ("Aim at the base of the fire")
              │
              ├── 2nd Mistake ──> Tier 2: Visual Highlight (Glowing outline on target object)
              │
              ├── 3rd Mistake ──> Tier 3: Animated Arrow / Ghost Demo
              │
              └── 4th Mistake ──> Tier 4: Guided Step Practice ──> Independent Retry
```

### Help Request Options (`NEED HELP?` Menu):
- **SHOW HINT:** Displays Tier 1/2 text & visual clue.
- **SHOW EXAMPLE:** Renders 3-second ghost animation showing correct gesture.
- **PRACTICE THIS STEP:** Enters risk-free guided mini-practice before returning to live test.

*Note: Assistance usage is recorded separately from independent mastery metrics.*

---

## 4. Local Telemetry & Offline Data Model

All telemetry is recorded locally to `PlayerPrefs` / JSON file in `Application.persistentDataPath`:

```json
{
  "sessionId": "SES_20260905_175200",
  "workerId": "WRK001",
  "moduleId": "MOD_FIRE_01",
  "mode": "LEARN",
  "startTime": "2026-09-05T17:52:00Z",
  "endTime": "2026-09-05T17:55:42Z",
  "totalDurationSeconds": 222,
  "overallScore": 85,
  "steps": [
    {
      "stepId": 1,
      "stepName": "Identify Fire Hazard",
      "timeSpentSeconds": 12,
      "mistakes": 0,
      "retries": 0,
      "hintsUsed": 0,
      "examplesUsed": 0,
      "independentSuccess": true
    },
    {
      "stepId": 6,
      "stepName": "Use Extinguisher Correctly",
      "timeSpentSeconds": 45,
      "mistakes": 2,
      "retries": 1,
      "hintsUsed": 1,
      "examplesUsed": 0,
      "independentSuccess": false
    }
  ]
}
```

---

## 5. Code Architecture (`Assets/_Project/Scripts/Training/Fire/`)

```
Assets/_Project/Scripts/Training/Fire/
├── FireLearnStep.cs              <- Enum of 8 step states
├── FireLearnStateMachine.cs      <- State machine managing step progression
├── AdaptiveHintEngine.cs         <- Multi-tiered hint & assistance manager
├── FireTelemetryLogger.cs        <- Local JSON telemetry logger
├── FireHazardController.cs       <- 3D AR Fire & Smoke FX controller
├── FireExtinguisherController.cs <- Extinguisher spray particle & PASS logic
└── ARInteractionController.cs    <- Raycasting & touch interaction handler
```

---

## 6. Verification Plan

1. **Unity Batchmode Build:** Compile `SurakshaAR_Phase4_FireLearn.apk` cleanly.
2. **Physical Device Deployment (vivo V2576):** Verify step 1 through 8 sequence on physical Android 16 hardware.
3. **Adaptive Hint Verification:** Test Tier 1, 2, 3, and 4 hint escalations on intentional mistakes.
4. **AR Camera & Tracking:** Confirm AR camera feed, plane tracking, and spatial object anchoring remain 100% stable during physical movement.
