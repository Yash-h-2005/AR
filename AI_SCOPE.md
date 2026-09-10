# SurakshaAR — AI Scope & Boundaries Specification

## 1. Strict Architectural Boundaries

> **CRITICAL RULE:** Safety-critical vocational training procedures must be 100% deterministic. **AI is strictly barred from real-time simulation control.**

```mermaid
graph TD
    subgraph Core Simulation - Deterministic ONLY
        StateEngine[Training State Machine]
        ARLogic[AR Object Placement & Raycast]
        Validation[Rule-Based Step Validator]
        Scoring[Objective Telemetry & Scoring]
    end

    subgraph Post-Session AI Engine - Asynchronous ONLY
        SessionJSON[Completed Session JSON] --> AIEngine[Post-Simulation Performance Analyzer]
        AIEngine --> WeakAreas[Weak-Area Analysis Report]
        AIEngine --> Recommendations[Personalized Retraining Recommendation]
    end

    Core Simulation - Deterministic ONLY -. Completed Telemetry .-> SessionJSON
```

---

## 2. Prohibited AI Uses (What AI Must NOT Do)

- **NO Real-Time Safety Rules:** AI must never decide whether a fire extinguisher operation or gas evacuation was correct.
- **NO Procedural Generation of Safety Steps:** AI must never alter the order of safety protocols mandated by industrial safety standards.
- **NO In-Simulation Chatbots:** Trainees must focus on hands-on spatial AR tasks, not conversing with an LLM prompt.
- **NO Real-Time Dynamic Scoring:** Scores must be calculated strictly from objective telemetry parameters (time, mistakes, help used).

---

## 3. Allowed AI Uses (Post-Simulation Analysis Only)

AI analysis operates **after** a training session is finished:

### 3.1 Input Payload
The AI analyzer receives only structured JSON telemetry:
- Target Module & Step completion times
- Failure frequency per step (e.g., failed extinguisher selection twice)
- Assistance escalation level reached

### 3.2 Generated Output
- **Strengths Summary:** (e.g., *"Excellent evacuation speed and alarm response"*).
- **Weak-Area Highlights:** (e.g., *"Struggled with identifying correct extinguisher for electrical fires"*).
- **Personalized Action Plan:** (e.g., *"Recommend re-running Module 1: Step 5 (Extinguisher Selection) in Learn Mode before Assessment"*).

---

## 4. Offline Deterministic Fallback
If AI cloud endpoints are unreachable or the app is offline:
- A local **Rule-Based Report Generator** evaluates the JSON using predefined conditional thresholds and outputs standardized feedback cards without requiring any cloud AI calls.
