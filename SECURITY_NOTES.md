# SurakshaAR — Security & Safety Guidelines

## 1. Safety Rules Integrity & Determinism

- **Fixed Safety Protocols:** Industrial safety protocols must be hard-coded or stored in signed, read-only ScriptableObjects/JSON within the application bundle.
- **No Dynamic External Rules:** Neither remote API calls nor AI models may alter safety-critical procedures (e.g., changing fire extinguisher selection rules).

---

## 2. Secrets & Credential Management

- **Client Secret Ban:** Admin API keys, Firebase Service Account private keys, and cloud database master keys must **NEVER** be compiled into the Android client APK.
- **Client Configuration:** Only public client identifiers (e.g., `google-services.json` client app IDs, web API keys scoped exclusively to Firebase Auth/Firestore client access) may exist in the mobile app.
- **Environment Exclusions:** Ensure `.env`, keystores, and credentials are listed in `.gitignore`.

---

## 3. Data Storage & Certificate Verification

- **Local Storage Security:** SQLite local databases containing worker scores should use local salt verification hashes to detect tampering.
- **Certificate Verification:** Certificates carry a SHA-256 cryptographic signature derived from `(workerId + moduleId + score + issueTimestamp + secretSalt)`. The web admin dashboard independently re-calculates this hash to verify validity before displaying a verified badge.

---

## 4. Physical Testing Safety Protocol

> **CRITICAL REAL-WORLD SAFETY DIRECTIVE:**  
> During software testing, development, and demonstration:
> - **NEVER** ignite real fires or release hazardous gases.
> - **NEVER** tamper with live industrial machinery or actual emergency alarms.
> - All testing must use purely virtual AR overlays in safe, controlled indoor/outdoor environments.
