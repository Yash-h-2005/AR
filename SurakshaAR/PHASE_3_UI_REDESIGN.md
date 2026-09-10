# SurakshaAR — Phase 3 UI Redesign Specification & Report

**Project Location:** `D:\AR-mining\SurakshaAR`  
**Unity Version:** `6000.3.23f1` (Unity 6 LTS)  
**Target Platform:** Android (API Level 29+ ARM64 IL2CPP)  
**Package Identifier:** `com.surakshaar.training`  
**APK Output:** [`D:\AR-mining\SurakshaAR\Builds\SurakshaAR_Phase3_UI_Redesign.apk`](file:///D:/AR-mining/SurakshaAR/Builds/SurakshaAR_Phase3_UI_Redesign.apk) (**130,708,618 bytes / 130.7 MB**)

---

## 1. Visual Design System & Palette

- **Background:** Soft Light Off-White (`#F8FAFC`)
- **Primary Action Accent:** Safety Orange (`#F97316` / `#EA580C`)
- **Headings & Titles:** Dark Navy (`#0F172A`)
- **Secondary Text:** Slate Gray (`#64748B`)
- **Success & Badges:** Emerald Green (`#10B981`) & Warning Amber (`#F59E0B`)
- **Card Panels:** Rounded White Panels (`#FFFFFF`) with subtle border lines (`#E2E8F0`) and touch-friendly targets.

---

## 2. 4-Tab Bottom Navigation Bar

Persistent bottom navigation bar across main screens:
- 🏠 **HOME** (`ScreenState.Home`)
- 📚 **MODULES** (`ScreenState.TrainingModules`)
- 📜 **CERTIFICATE** (`ScreenState.CertificatePreview`)
- 👤 **PROFILE** (`ScreenState.Profile`)

Active tab is highlighted in **Safety Orange (`#F97316`)** with bold text.

---

## 3. Redesigned Screen Hierarchy (13 Scenes)

1. **Splash** ([`Splash.unity`](file:///D:/AR-mining/SurakshaAR/Assets/_Project/Scenes/Bootstrap/Splash.unity)): SurakshaAR industrial logo, subtitle, 2.5s auto transition.
2. **Worker Login** ([`Login.unity`](file:///D:/AR-mining/SurakshaAR/Assets/_Project/Scenes/MainMenu/Login.unity)): Worker ID & Name input, validation error banner, offline session storage.
3. **Language Selection** ([`LanguageSelection.unity`](file:///D:/AR-mining/SurakshaAR/Assets/_Project/Scenes/MainMenu/LanguageSelection.unity)): English, हिन्दी (Hindi), and Santali options.
4. **Home Dashboard** ([`Home.unity`](file:///D:/AR-mining/SurakshaAR/Assets/_Project/Scenes/MainMenu/Home.unity)):
   - "Good Morning, Worker 👋" greeting
   - Training Progress card ("2 / 6 Activities Completed" with Progress Bar)
   - Continue Training card ("🔥 Fire & Explosion Response", Next: Use Fire Extinguisher, `[ CONTINUE TRAINING → ]`)
   - Safety Performance summary card
5. **Training Modules** ([`TrainingModules.unity`](file:///D:/AR-mining/SurakshaAR/Assets/_Project/Scenes/MainMenu/TrainingModules.unity)):
   - Card 1: 🔥 Fire & Explosion Response (3 Activities, 0/3 Completed, Progress Bar 0%, `START TRAINING →`)
   - Card 2: ☣ Gas Leak & Confined Space (3 Activities, 0/3 Completed, Progress Bar 0%, `START TRAINING →`)
   - Card 3: ⚙ Machinery Safety (Coming Soon, 🔒 Locked tag)
6. **Fire Module Intro** ([`FireModuleIntro.unity`](file:///D:/AR-mining/SurakshaAR/Assets/_Project/Scenes/Fire/FireModuleIntro.unity)):
   - Short description
   - "What You'll Learn" 6-item checklist
   - `[ LEARN MODE → ]` (Primary Orange Action Button)
   - `[ SIMULATION → ]` (Secondary Action Button)
7. **AR Training UI (Learn)** ([`ARLearnPlaceholder.unity`](file:///D:/AR-mining/SurakshaAR/Assets/_Project/Scenes/Fire/ARLearnPlaceholder.unity)):
   - 85-90% live AR Camera view clear for physical interaction
   - Top HUD: Step 3 of 8, Progress Bar, `🔥 FIRE DETECTED` status, instruction ("Aim at the base of the fire")
   - Bottom Action: `[ SPRAY ]` (Primary Orange Button), `Need Help?` secondary option
   - Contextual Wrong-Action `FeedbackCard` overlay (`⚠ Try Again`, instruction tip, `TRY AGAIN` button)
8. **AR Simulation UI** ([`ARSimulationPlaceholder.unity`](file:///D:/AR-mining/SurakshaAR/Assets/_Project/Scenes/Fire/ARSimulationPlaceholder.unity)):
   - Minimal HUD overlay over live AR feed with `EXIT SIMULATION` button.
9. **Results** ([`Result.unity`](file:///D:/AR-mining/SurakshaAR/Assets/_Project/Scenes/MainMenu/Result.unity)):
   - "Training Completed 🎉", Score 85%, performance metrics (Correct: 7/8, Mistakes: 2, Retries: 1, Time: 03:42), Areas to Improve card, `PRACTICE AGAIN` and `CONTINUE →` buttons.
10. **Certificate Preview** ([`CertificatePreview.unity`](file:///D:/AR-mining/SurakshaAR/Assets/_Project/Scenes/MainMenu/CertificatePreview.unity)):
    - SURAKSHAAR Safety Training Certificate preview card, Worker Name/ID, Module, Score (85%), Completion Date, `VIEW CERTIFICATE` and `VERIFY QR` buttons.
11. **Worker Profile** ([`Profile.unity`](file:///D:/AR-mining/SurakshaAR/Assets/_Project/Scenes/MainMenu/Profile.unity)):
    - Worker Name ("Ramesh Kumar"), ID ("WRK001"), Completed Modules (2/6), Overall Performance (85%), Language picker (English, Hindi, Santali), Settings entry button.
12. **Settings** ([`Settings.unity`](file:///D:/AR-mining/SurakshaAR/Assets/_Project/Scenes/MainMenu/Settings.unity)):
    - Language switcher, Audio toggle, Haptic feedback toggle, Training assistance toggle, About SurakshaAR panel.
13. **AR Test Scene** ([`ARFoundationTest.unity`](file:///D:/AR-mining/SurakshaAR/Assets/_Project/Scenes/Bootstrap/ARFoundationTest.unity)):
    - Phase 2 AR Foundation plane detection, raycasting, placement & spatial anchoring system preserved 100%.

---

## 4. Localization Engine

- **Architecture:** Key-based [`LocalizationManager.cs`](file:///D:/AR-mining/SurakshaAR/Assets/_Project/Scripts/Localization/LocalizationManager.cs) and [`LocalizedText.cs`](file:///D:/AR-mining/SurakshaAR/Assets/_Project/Scripts/Localization/LocalizedText.cs).
- **English:** 100% complete across all redesigned screens.
- **Hindi (हिन्दी):** 100% complete across all redesigned screens.
- **Santali:** Key fallback structure intact.
