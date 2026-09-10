# SurakshaAR — Phase 3 UI Architecture & Navigation Specification

**Project Location:** `D:\AR-mining\SurakshaAR`  
**Unity Version:** `6000.3.23f1` (Unity 6 LTS)  
**Target Platform:** Android (API Level 29+ ARM64 IL2CPP)  
**Package Identifier:** `com.surakshaar.training`  
**Design System Focus:** Industrial safety training software (Clean, High-contrast, Mobile-friendly)

---

## 1. Application Navigation Flow

```
SPLASH (Assets/_Project/Scenes/Bootstrap/Splash.unity)
   ↓ (Auto transition 2.5s)
WORKER LOGIN (Assets/_Project/Scenes/MainMenu/Login.unity)
   ↓ (Worker ID validation -> Store locally)
LANGUAGE SELECTION (Assets/_Project/Scenes/MainMenu/LanguageSelection.unity)
   ↓ (English / हिन्दी / Santali)
HOME DASHBOARD (Assets/_Project/Scenes/MainMenu/Home.unity)
   ↓
TRAINING MODULES (Assets/_Project/Scenes/MainMenu/TrainingModules.unity)
   ↓
FIRE MODULE INTRO (Assets/_Project/Scenes/Fire/FireModuleIntro.unity)
   ↓
MODE SELECTION (Learn Mode vs Simulation Mode)
   ├── LEARN MODE (Assets/_Project/Scenes/Fire/ARLearnPlaceholder.unity)
   └── SIMULATION MODE (Assets/_Project/Scenes/Fire/ARSimulationPlaceholder.unity)
   ↓
RESULT SCREEN (Assets/_Project/Scenes/MainMenu/Result.unity)
   ↓
PERFORMANCE SUMMARY (Assets/_Project/Scenes/MainMenu/PerformanceSummary.unity)
   ↓
CERTIFICATE PREVIEW (Assets/_Project/Scenes/MainMenu/CertificatePreview.unity)
```

---

## 2. Reusable UI Code Architecture

All UI logic is cleanly separated from training, AR, and backend code in `Assets/_Project/Scripts/UI/`:

```
Assets/_Project/Scripts/
├── Localization/
│   ├── LocalizationManager.cs    <- Key-based offline localization database (EN, HI, SAT)
│   └── LocalizedText.cs          <- Dynamic text updating component
└── UI/
    ├── Components/
    │   ├── BottomNavigation.cs   <- Mobile bottom bar with 5 persistent tabs
    │   ├── HeaderBar.cs          <- Top title bar with back button & worker badge
    │   └── ModuleCard.cs         <- Industrial module card with status badges
    ├── Data/
    │   └── UserSession.cs        <- In-memory & PlayerPrefs worker profile storage
    ├── Navigation/
    │   └── SceneNavigator.cs     <- Central screen stack manager & Android Back button handler
    └── Screens/
        ├── SplashScreenController.cs
        ├── LoginScreenController.cs
        ├── LanguageSelectionController.cs
        ├── HomeScreenController.cs
        ├── TrainingModulesController.cs
        ├── FireModuleIntroController.cs
        ├── LearnPlaceholderController.cs
        ├── SimulationPlaceholderController.cs
        ├── ResultScreenController.cs
        ├── PerformanceSummaryController.cs
        ├── CertificatePreviewController.cs
        └── SettingsController.cs
```

---

## 3. Registered Build Scenes

All 12 scenes registered in `BuildScript.cs`:
1. `Assets/_Project/Scenes/Bootstrap/Splash.unity`
2. `Assets/_Project/Scenes/MainMenu/Login.unity`
3. `Assets/_Project/Scenes/MainMenu/LanguageSelection.unity`
4. `Assets/_Project/Scenes/MainMenu/Home.unity`
5. `Assets/_Project/Scenes/MainMenu/TrainingModules.unity`
6. `Assets/_Project/Scenes/Fire/FireModuleIntro.unity`
7. `Assets/_Project/Scenes/Fire/ARLearnPlaceholder.unity`
8. `Assets/_Project/Scenes/Fire/ARSimulationPlaceholder.unity`
9. `Assets/_Project/Scenes/MainMenu/Result.unity`
10. `Assets/_Project/Scenes/MainMenu/PerformanceSummary.unity`
11. `Assets/_Project/Scenes/MainMenu/CertificatePreview.unity`
12. `Assets/_Project/Scenes/MainMenu/Settings.unity`
13. `Assets/_Project/Scenes/Bootstrap/ARFoundationTest.unity` (Phase 2 AR test scene)

---

## 4. Key Design Principles

- **Industrial Aesthetic:** Deep slate navy background (`#1F272E`), amber safety gold accents (`#F2A927`), emerald success badges (`#2ECC71`), cyan primary buttons (`#1F87E6`).
- **High Accessibility:** Large touch targets (minimum 80px height), high-contrast text, clear status badges.
- **Offline Resilience:** All session profiles, language preferences, and screen navigation run 100% offline without Firebase or network requirements.
- **Phase 2 AR Preservation:** AR Learn and AR Simulation placeholder screens render UI overlays directly on top of the live AR camera feed without breaking physical tracking.
