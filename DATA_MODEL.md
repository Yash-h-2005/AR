# SurakshaAR — Core Data Models Specification

## 1. Overview
The data models in **SurakshaAR** are designed to be technology-independent, JSON-serializable, and compatible with both local SQLite/JSON storage and cloud Firestore databases.

---

## 2. Model Schemas

### 2.1 Worker Model
Represents a trainee in the mining or manufacturing sector.

```json
{
  "workerId": "WRK_JH_89412",
  "name": "Ramesh Murmu",
  "preferredLanguage": "SAT",
  "department": "Underground Mining Division B",
  "role": "Safety Apprentice",
  "createdTimestamp": 1772640000,
  "completedModules": ["MOD_FIRE_01"]
}
```

---

### 2.2 Training Module Model
Defines high-level module metadata and configuration.

```json
{
  "moduleId": "MOD_FIRE_01",
  "title": "Fire & Explosion Response",
  "description": "Recognize electrical fire hazard, raise alarm, pick correct extinguisher, operate, and evacuate safely.",
  "difficulty": "INTERMEDIATE",
  "supportedLanguages": ["EN", "HI", "SAT"],
  "scenarioId": "SCN_FIRE_ELEC_01"
}
```

---

### 2.3 Training Step Model
Represents an individual safety procedure step within a scenario.

```json
{
  "stepId": "STEP_FIRE_05_OPERATE",
  "instructionKey": "STEP_FIRE_OPERATE_INSTR",
  "targetObject": "Extinguisher_CO2",
  "expectedActionType": "AIM_AND_HOLD",
  "validationRule": "HOLD_AIM_FIRE_BASE_5S",
  "nextStepId": "STEP_FIRE_06_EVACUATE",
  "hintKey": "HINT_FIRE_AIM_BASE",
  "demonstrationPrefab": "Ghost_Extinguisher_Sweep",
  "scoringWeight": 250
}
```

---

### 2.4 Training Session Model
Captures objective telemetry and assessment results of a completed session.

```json
{
  "sessionId": "SESS_20260903_99412",
  "workerId": "WRK_JH_89412",
  "moduleId": "MOD_FIRE_01",
  "startTime": 1772641000,
  "endTime": 1772641240,
  "score": 920,
  "maxScore": 1000,
  "mistakes": 1,
  "retries": 1,
  "helpUsed": {
    "hints": 1,
    "highlights": 0,
    "demonstrations": 0
  },
  "timeTakenSeconds": 240,
  "independentSuccess": true,
  "completed": true,
  "syncStatus": "PENDING"
}
```

---

### 2.5 Certificate Model
Contains verification payload and metadata for earned vocational certificates.

```json
{
  "certificateId": "CERT_SURAKSHA_2026_00491",
  "workerId": "WRK_JH_89412",
  "moduleId": "MOD_FIRE_01",
  "issueDate": "2026-09-03",
  "score": 920,
  "verificationStatus": "VALID",
  "qrPayload": "https://suraksha-ar.web.app/verify/CERT_SURAKSHA_2026_00491?hash=a8f91c..."
}
```
