using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SurakshaAR.UI.Navigation
{
    public enum ScreenState
    {
        Splash,
        Login,
        LanguageSelection,
        CameraAccess,
        TrainingNotice,
        Home,
        TrainingModules,
        FireModuleIntro,
        ARPhase1FloorScan,       // Phase 1 — Real camera floor detection & anchor placement
        FireMineSimulator,       // Fire Module Phase 1 — Spatial-tracking first-person mine movement prototype
        PetroleumFireSimulator,  // Fire Module Sub-module 2 — Petroleum/fuel fire scenario
        ExplosionMineSimulator,  // Fire Module Sub-module 3 — Explosion & collision scenario
        FireAssessmentSimulator, // Fire Module Final Assessment — Electric Fire + Explosion & Route B Evacuation
        GasModuleIntro,
        ARLearnPlaceholder,
        GasARLearnPlaceholder,
        ARSimulationPlaceholder,
        Result,
        PerformanceSummary,
        CertificatePreview,
        QRVerification,
        Profile,
        Settings
    }

    /// <summary>
    /// Centralized navigation manager supporting screen transitions and Android hardware back button.
    /// </summary>
    public class SceneNavigator : MonoBehaviour
    {
        public static SceneNavigator Instance { get; private set; }

        public ScreenState CurrentScreen { get; private set; } = ScreenState.Splash;

        private readonly Stack<ScreenState> navigationStack = new Stack<ScreenState>();

        public event Action<ScreenState> OnScreenChanged;

        private static readonly Dictionary<ScreenState, string> SceneNameMap = new Dictionary<ScreenState, string>()
        {
            { ScreenState.Splash, "Splash" },
            { ScreenState.Login, "Login" },
            { ScreenState.LanguageSelection, "LanguageSelection" },
            { ScreenState.CameraAccess, "CameraAccess" },
            { ScreenState.TrainingNotice, "TrainingNotice" },
            { ScreenState.Home, "Home" },
            { ScreenState.TrainingModules, "TrainingModules" },
            { ScreenState.FireModuleIntro, "FireModuleIntro" },
            { ScreenState.ARPhase1FloorScan, "ARPhase1_FloorScan" },
            { ScreenState.FireMineSimulator, "FireMineSimulator" },
            { ScreenState.PetroleumFireSimulator, "PetroleumFireSimulator" },
            { ScreenState.ExplosionMineSimulator, "ExplosionMineSimulator" },
            { ScreenState.FireAssessmentSimulator, "FireAssessmentSimulator" },
            { ScreenState.GasModuleIntro, "GasModuleIntro" },
            { ScreenState.ARLearnPlaceholder, "ARLearnPlaceholder" },
            { ScreenState.GasARLearnPlaceholder, "GasARLearnPlaceholder" },
            { ScreenState.ARSimulationPlaceholder, "ARSimulationPlaceholder" },
            { ScreenState.Result, "Result" },
            { ScreenState.PerformanceSummary, "PerformanceSummary" },
            { ScreenState.CertificatePreview, "CertificatePreview" },
            { ScreenState.QRVerification, "QRVerification" },
            { ScreenState.Profile, "Profile" },
            { ScreenState.Settings, "Settings" }
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            // Intercept Android hardware Back button (Keycode.Escape on Android)
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                HandleHardwareBack();
            }
        }

        public void NavigateTo(ScreenState targetScreen, bool addToStack = true)
        {
            if (addToStack && CurrentScreen != ScreenState.Splash && CurrentScreen != targetScreen)
            {
                navigationStack.Push(CurrentScreen);
            }

            CurrentScreen = targetScreen;
            Debug.Log($"[SceneNavigator] Navigating to: {targetScreen}");

            if (SceneNameMap.TryGetValue(targetScreen, out string sceneName))
            {
                SceneManager.LoadScene(sceneName);
                OnScreenChanged?.Invoke(CurrentScreen);
            }
            else
            {
                Debug.LogError($"[SceneNavigator] Unmapped screen state: {targetScreen}");
            }
        }

        public void GoBack()
        {
            if (navigationStack.Count > 0)
            {
                ScreenState previousScreen = navigationStack.Pop();
                NavigateTo(previousScreen, addToStack: false);
            }
            else
            {
                // Default fallback if stack empty
                if (CurrentScreen != ScreenState.Home && CurrentScreen != ScreenState.Login)
                {
                    NavigateTo(ScreenState.Home, addToStack: false);
                }
            }
        }

        public void HandleHardwareBack()
        {
            Debug.Log($"[SceneNavigator] Hardware back button pressed on screen: {CurrentScreen}");

            if (CurrentScreen == ScreenState.Home || CurrentScreen == ScreenState.Login)
            {
                // Don't exit app unexpectedly; confirm or stay on home
                Debug.Log("[SceneNavigator] On Root screen (Home/Login); ignoring back button to prevent accidental app exit.");
            }
            else
            {
                GoBack();
            }
        }
    }
}
