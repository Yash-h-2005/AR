#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine.XR.Management;
using UnityEditor.XR.ARCore;

namespace SurakshaAR.Editor
{
    public static class BuildScript
    {
        public static void BuildAndroidAPK()
        {
            string outputDirectory = "D:/AR-mining/SurakshaAR/Builds";
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            string apkPath = Path.Combine(outputDirectory, "SurakshaAR_Phase3_UI_Redesign.apk");

            // Build all 13 Phase 3 Redesigned UI Scenes programmatically first
            SceneBuilderScript.BuildAllScenes();

            // Configure Player Settings for Android & ARCore
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            
            PlayerSettings.applicationIdentifier = "com.surakshaar.training";
            PlayerSettings.productName = "SurakshaAR";
            PlayerSettings.companyName = "Suraksha";

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.stripEngineCode = false;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            string[] scenes = new string[]
            {
                "Assets/_Project/Scenes/Bootstrap/Splash.unity",
                "Assets/_Project/Scenes/MainMenu/Login.unity",
                "Assets/_Project/Scenes/MainMenu/LanguageSelection.unity",
                "Assets/_Project/Scenes/MainMenu/CameraAccess.unity",
                "Assets/_Project/Scenes/MainMenu/TrainingNotice.unity",
                "Assets/_Project/Scenes/MainMenu/Home.unity",
                "Assets/_Project/Scenes/MainMenu/TrainingModules.unity",
                "Assets/_Project/Scenes/Fire/FireModuleIntro.unity",
                "Assets/_Project/Scenes/Gas/GasModuleIntro.unity",
                "Assets/_Project/Scenes/Fire/ARLearnPlaceholder.unity",
                "Assets/_Project/Scenes/Gas/GasARLearnPlaceholder.unity",
                "Assets/_Project/Scenes/Fire/ARSimulationPlaceholder.unity",
                "Assets/_Project/Scenes/MainMenu/Result.unity",
                "Assets/_Project/Scenes/MainMenu/PerformanceSummary.unity",
                "Assets/_Project/Scenes/MainMenu/CertificatePreview.unity",
                "Assets/_Project/Scenes/MainMenu/QRVerification.unity",
                "Assets/_Project/Scenes/MainMenu/Profile.unity",
                "Assets/_Project/Scenes/MainMenu/Settings.unity"
            };

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            };

            Debug.Log($"Starting SurakshaAR Phase 3 UI Redesign Android APK Build at: {apkPath}");

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"SurakshaAR Phase 3 UI Redesign Build SUCCEEDED: {summary.totalSize} bytes. Output: {apkPath}");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"SurakshaAR Phase 3 UI Redesign Build FAILED with {summary.totalErrors} errors.");
                EditorApplication.Exit(1);
            }
        }

        public static void BuildAndroidAPK_Phase4_FireLearn()
        {
            string outputDirectory = "D:/AR-mining/SurakshaAR/Builds";
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            string apkPath = Path.Combine(outputDirectory, "SurakshaAR_Phase4_FireLearn.apk");

            // Build all scenes programmatically first
            SceneBuilderScript.BuildAllScenes();

            // Configure Player Settings for Android & ARCore
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            
            PlayerSettings.applicationIdentifier = "com.surakshaar.training";
            PlayerSettings.productName = "SurakshaAR";
            PlayerSettings.companyName = "Suraksha";

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.stripEngineCode = false;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            string[] scenes = new string[]
            {
                "Assets/_Project/Scenes/Bootstrap/Splash.unity",
                "Assets/_Project/Scenes/MainMenu/Login.unity",
                "Assets/_Project/Scenes/MainMenu/LanguageSelection.unity",
                "Assets/_Project/Scenes/MainMenu/Home.unity",
                "Assets/_Project/Scenes/MainMenu/TrainingModules.unity",
                "Assets/_Project/Scenes/Fire/FireModuleIntro.unity",
                "Assets/_Project/Scenes/Fire/ARLearnPlaceholder.unity",
                "Assets/_Project/Scenes/Fire/ARSimulationPlaceholder.unity",
                "Assets/_Project/Scenes/MainMenu/Result.unity",
                "Assets/_Project/Scenes/MainMenu/PerformanceSummary.unity",
                "Assets/_Project/Scenes/MainMenu/CertificatePreview.unity",
                "Assets/_Project/Scenes/MainMenu/Profile.unity",
                "Assets/_Project/Scenes/MainMenu/Settings.unity",
                "Assets/_Project/Scenes/Bootstrap/ARFoundationTest.unity"
            };

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            };

            Debug.Log($"Starting SurakshaAR Phase 4 Fire Learn Mode Android APK Build at: {apkPath}");

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"SurakshaAR Phase 4 Fire Learn Build SUCCEEDED: {summary.totalSize} bytes. Output: {apkPath}");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"SurakshaAR Phase 4 Fire Learn Build FAILED with {summary.totalErrors} errors.");
                EditorApplication.Exit(1);
            }
        }

        public static void BuildAndroidAPK_Phase4_TrueAR_FireLearn()
        {
            string outputDirectory = "D:/AR-mining/SurakshaAR/Builds";
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            string apkPath = Path.Combine(outputDirectory, "SurakshaAR_Phase4_TrueAR_FireLearn.apk");

            // Build all scenes programmatically first
            SceneBuilderScript.BuildAllScenes();

            // Configure Player Settings for Android & ARCore
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            
            PlayerSettings.applicationIdentifier = "com.surakshaar.training";
            PlayerSettings.productName = "SurakshaAR";
            PlayerSettings.companyName = "Suraksha";

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.stripEngineCode = false;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            string[] scenes = new string[]
            {
                "Assets/_Project/Scenes/Bootstrap/Splash.unity",
                "Assets/_Project/Scenes/MainMenu/Login.unity",
                "Assets/_Project/Scenes/MainMenu/LanguageSelection.unity",
                "Assets/_Project/Scenes/MainMenu/Home.unity",
                "Assets/_Project/Scenes/MainMenu/TrainingModules.unity",
                "Assets/_Project/Scenes/Fire/FireModuleIntro.unity",
                "Assets/_Project/Scenes/Fire/ARLearnPlaceholder.unity",
                "Assets/_Project/Scenes/Fire/ARSimulationPlaceholder.unity",
                "Assets/_Project/Scenes/MainMenu/Result.unity",
                "Assets/_Project/Scenes/MainMenu/PerformanceSummary.unity",
                "Assets/_Project/Scenes/MainMenu/CertificatePreview.unity",
                "Assets/_Project/Scenes/MainMenu/Profile.unity",
                "Assets/_Project/Scenes/MainMenu/Settings.unity",
                "Assets/_Project/Scenes/Bootstrap/ARFoundationTest.unity"
            };

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            };

            Debug.Log($"Starting SurakshaAR Phase 4 True AR Fire Learn Mode Android APK Build at: {apkPath}");

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"SurakshaAR Phase 4 True AR Fire Learn Build SUCCEEDED: {summary.totalSize} bytes. Output: {apkPath}");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"SurakshaAR Phase 4 True AR Fire Learn Build FAILED with {summary.totalErrors} errors.");
                EditorApplication.Exit(1);
            }
        }

        public static void BuildAndroidAPK_Phase5_AdaptiveAR()
        {
            string outputDirectory = "D:/AR-mining/SurakshaAR/Builds";
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            string apkPath = Path.Combine(outputDirectory, "SurakshaAR_Phase5_AdaptiveAR.apk");

            // Build all scenes programmatically first
            SceneBuilderScript.BuildAllScenes();

            // Configure Player Settings for Android & ARCore
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            
            PlayerSettings.applicationIdentifier = "com.surakshaar.training";
            PlayerSettings.productName = "SurakshaAR";
            PlayerSettings.companyName = "Suraksha";

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.stripEngineCode = false;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            string[] scenes = new string[]
            {
                "Assets/_Project/Scenes/Bootstrap/Splash.unity",
                "Assets/_Project/Scenes/MainMenu/Login.unity",
                "Assets/_Project/Scenes/MainMenu/LanguageSelection.unity",
                "Assets/_Project/Scenes/MainMenu/Home.unity",
                "Assets/_Project/Scenes/MainMenu/TrainingModules.unity",
                "Assets/_Project/Scenes/Fire/FireModuleIntro.unity",
                "Assets/_Project/Scenes/Fire/ARLearnPlaceholder.unity",
                "Assets/_Project/Scenes/Fire/ARSimulationPlaceholder.unity",
                "Assets/_Project/Scenes/MainMenu/Result.unity",
                "Assets/_Project/Scenes/MainMenu/PerformanceSummary.unity",
                "Assets/_Project/Scenes/MainMenu/CertificatePreview.unity",
                "Assets/_Project/Scenes/MainMenu/Profile.unity",
                "Assets/_Project/Scenes/MainMenu/Settings.unity",
                "Assets/_Project/Scenes/Bootstrap/ARFoundationTest.unity"
            };

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            };

            Debug.Log($"Starting SurakshaAR Phase 5 AI-Adaptive Training Android APK Build at: {apkPath}");

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"SurakshaAR Phase 5 AI-Adaptive Training Build SUCCEEDED: {summary.totalSize} bytes. Output: {apkPath}");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"SurakshaAR Phase 5 AI-Adaptive Training Build FAILED with {summary.totalErrors} errors.");
                EditorApplication.Exit(1);
            }
        }

        public static void BuildAndroidAPK_Phase6_GasConfinedSpace()
        {
            string outputDirectory = "D:/AR-mining/SurakshaAR/Builds";
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            string apkPath = Path.Combine(outputDirectory, "SurakshaAR_Phase6_GasConfinedSpace.apk");

            // Build all 15 scenes programmatically first
            SceneBuilderScript.BuildAllScenes();

            // Configure Player Settings for Android & ARCore
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            
            PlayerSettings.applicationIdentifier = "com.surakshaar.training";
            PlayerSettings.productName = "SurakshaAR";
            PlayerSettings.companyName = "Suraksha";

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.stripEngineCode = false;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            string[] scenes = new string[]
            {
                "Assets/_Project/Scenes/Bootstrap/Splash.unity",
                "Assets/_Project/Scenes/MainMenu/Login.unity",
                "Assets/_Project/Scenes/MainMenu/LanguageSelection.unity",
                "Assets/_Project/Scenes/MainMenu/Home.unity",
                "Assets/_Project/Scenes/MainMenu/TrainingModules.unity",
                "Assets/_Project/Scenes/Fire/FireModuleIntro.unity",
                "Assets/_Project/Scenes/Gas/GasModuleIntro.unity",
                "Assets/_Project/Scenes/Fire/ARLearnPlaceholder.unity",
                "Assets/_Project/Scenes/Gas/GasARLearnPlaceholder.unity",
                "Assets/_Project/Scenes/Fire/ARSimulationPlaceholder.unity",
                "Assets/_Project/Scenes/MainMenu/Result.unity",
                "Assets/_Project/Scenes/MainMenu/PerformanceSummary.unity",
                "Assets/_Project/Scenes/MainMenu/CertificatePreview.unity",
                "Assets/_Project/Scenes/MainMenu/Profile.unity",
                "Assets/_Project/Scenes/MainMenu/Settings.unity",
                "Assets/_Project/Scenes/Bootstrap/ARFoundationTest.unity"
            };

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            };

            Debug.Log($"Starting SurakshaAR Phase 6 Gas Leak & Confined Space Android APK Build at: {apkPath}");

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"SurakshaAR Phase 6 Gas Leak & Confined Space Build SUCCEEDED: {summary.totalSize} bytes. Output: {apkPath}");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"SurakshaAR Phase 6 Gas Leak & Confined Space Build FAILED with {summary.totalErrors} errors.");
                EditorApplication.Exit(1);
            }
        }

        public static void BuildAndroidAPK_Phase7_Certification()
        {
            string outputDirectory = "D:/AR-mining/SurakshaAR/Builds";
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            string apkPath = Path.Combine(outputDirectory, "SurakshaAR_Phase7_Certification.apk");

            // Build all scenes programmatically first
            SceneBuilderScript.BuildAllScenes();

            // Configure Player Settings for Android & ARCore
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            
            PlayerSettings.applicationIdentifier = "com.surakshaar.training";
            PlayerSettings.productName = "SurakshaAR";
            PlayerSettings.companyName = "Suraksha";

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.stripEngineCode = false;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            string[] scenes = new string[]
            {
                "Assets/_Project/Scenes/Bootstrap/Splash.unity",
                "Assets/_Project/Scenes/MainMenu/Login.unity",
                "Assets/_Project/Scenes/MainMenu/LanguageSelection.unity",
                "Assets/_Project/Scenes/MainMenu/Home.unity",
                "Assets/_Project/Scenes/MainMenu/TrainingModules.unity",
                "Assets/_Project/Scenes/Fire/FireModuleIntro.unity",
                "Assets/_Project/Scenes/Gas/GasModuleIntro.unity",
                "Assets/_Project/Scenes/Fire/ARLearnPlaceholder.unity",
                "Assets/_Project/Scenes/Gas/GasARLearnPlaceholder.unity",
                "Assets/_Project/Scenes/Fire/ARSimulationPlaceholder.unity",
                "Assets/_Project/Scenes/MainMenu/Result.unity",
                "Assets/_Project/Scenes/MainMenu/PerformanceSummary.unity",
                "Assets/_Project/Scenes/MainMenu/CertificatePreview.unity",
                "Assets/_Project/Scenes/MainMenu/QRVerification.unity",
                "Assets/_Project/Scenes/MainMenu/Profile.unity",
                "Assets/_Project/Scenes/MainMenu/Settings.unity",
                "Assets/_Project/Scenes/Bootstrap/ARFoundationTest.unity"
            };

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            };

            Debug.Log($"Starting SurakshaAR Phase 7 Assessment & Certification Android APK Build at: {apkPath}");

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"SurakshaAR Phase 7 Assessment & Certification Build SUCCEEDED: {summary.totalSize} bytes. Output: {apkPath}");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"SurakshaAR Phase 7 Assessment & Certification Build FAILED with {summary.totalErrors} errors.");
                EditorApplication.Exit(1);
            }
        }

        public static void BuildAndroidAPK_FrontendUICorrection()
        {
            string outputDirectory = "D:/AR-mining/SurakshaAR/Builds";
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            string apkPath = Path.Combine(outputDirectory, "SurakshaAR_FrontendUICorrection.apk");

            // Build all 17 scenes programmatically first
            SceneBuilderScript.BuildAllScenes();

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            
            PlayerSettings.applicationIdentifier = "com.surakshaar.training";
            PlayerSettings.productName = "SurakshaAR";
            PlayerSettings.companyName = "Suraksha";

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.stripEngineCode = false;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            string[] scenes = new string[]
            {
                "Assets/_Project/Scenes/Bootstrap/Splash.unity",
                "Assets/_Project/Scenes/MainMenu/Login.unity",
                "Assets/_Project/Scenes/MainMenu/LanguageSelection.unity",
                "Assets/_Project/Scenes/MainMenu/CameraAccess.unity",
                "Assets/_Project/Scenes/MainMenu/TrainingNotice.unity",
                "Assets/_Project/Scenes/MainMenu/Home.unity",
                "Assets/_Project/Scenes/MainMenu/TrainingModules.unity",
                "Assets/_Project/Scenes/Fire/FireModuleIntro.unity",
                "Assets/_Project/Scenes/Gas/GasModuleIntro.unity",
                "Assets/_Project/Scenes/Fire/ARLearnPlaceholder.unity",
                "Assets/_Project/Scenes/Gas/GasARLearnPlaceholder.unity",
                "Assets/_Project/Scenes/Fire/ARSimulationPlaceholder.unity",
                "Assets/_Project/Scenes/MainMenu/Result.unity",
                "Assets/_Project/Scenes/MainMenu/PerformanceSummary.unity",
                "Assets/_Project/Scenes/MainMenu/CertificatePreview.unity",
                "Assets/_Project/Scenes/MainMenu/QRVerification.unity",
                "Assets/_Project/Scenes/MainMenu/Profile.unity",
                "Assets/_Project/Scenes/MainMenu/Settings.unity"
            };

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            };

            Debug.Log($"Starting SurakshaAR Frontend UI Correction Android APK Build at: {apkPath}");

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"SurakshaAR Frontend UI Correction Build SUCCEEDED: {summary.totalSize} bytes. Output: {apkPath}");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"SurakshaAR Frontend UI Correction Build FAILED with {summary.totalErrors} errors.");
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Fire Module Phase 1 — Spatial-Tracking Virtual Mine Simulator.
        /// Builds a full APK including the FireMineSimulator scene.
        /// Tests physical phone movement → virtual first-person camera.
        /// </summary>
        public static void BuildAndroidAPK_FireMineSimulator()
        {
            string outputDirectory = "D:/AR-mining/SurakshaAR/Builds";
            if (!Directory.Exists(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            string apkPath = System.IO.Path.Combine(outputDirectory, "SurakshaAR_FireMine_Phase2C.apk");

            // Build all scenes programmatically
            SceneBuilderScript.BuildAllScenes();
            SceneBuilderScript.CreateFireMineSimulatorScene();
            SceneBuilderScript.CreatePetroleumFireSimulatorScene();
            SceneBuilderScript.CreateExplosionMineSimulatorScene();
            SceneBuilderScript.CreateFireAssessmentSimulatorScene();

            // Configure Player Settings for Android & ARCore
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            PlayerSettings.applicationIdentifier = "com.surakshaar.training";
            PlayerSettings.productName = "SurakshaAR";
            PlayerSettings.companyName = "Suraksha";
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.stripEngineCode = false;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            // Ensure ARCore is enabled
            EnsureXRSettingsConfigured();

            string[] scenes = new string[]
            {
                "Assets/_Project/Scenes/Bootstrap/Splash.unity",
                "Assets/_Project/Scenes/MainMenu/Login.unity",
                "Assets/_Project/Scenes/MainMenu/LanguageSelection.unity",
                "Assets/_Project/Scenes/MainMenu/CameraAccess.unity",
                "Assets/_Project/Scenes/MainMenu/TrainingNotice.unity",
                "Assets/_Project/Scenes/MainMenu/Home.unity",
                "Assets/_Project/Scenes/MainMenu/TrainingModules.unity",
                "Assets/_Project/Scenes/Fire/FireModuleIntro.unity",
                "Assets/_Project/Scenes/Fire/FireMineSimulator.unity",
                "Assets/_Project/Scenes/Fire/PetroleumFireSimulator.unity",
                "Assets/_Project/Scenes/Fire/ExplosionMineSimulator.unity",
                "Assets/_Project/Scenes/Fire/FireAssessmentSimulator.unity",
                "Assets/_Project/Scenes/Fire/ARPhase1_FloorScan.unity",
                "Assets/_Project/Scenes/Gas/GasModuleIntro.unity",
                "Assets/_Project/Scenes/Fire/ARLearnPlaceholder.unity",
                "Assets/_Project/Scenes/Gas/GasARLearnPlaceholder.unity",
                "Assets/_Project/Scenes/Fire/ARSimulationPlaceholder.unity",
                "Assets/_Project/Scenes/MainMenu/Result.unity",
                "Assets/_Project/Scenes/MainMenu/PerformanceSummary.unity",
                "Assets/_Project/Scenes/MainMenu/CertificatePreview.unity",
                "Assets/_Project/Scenes/MainMenu/QRVerification.unity",
                "Assets/_Project/Scenes/MainMenu/Profile.unity",
                "Assets/_Project/Scenes/MainMenu/Settings.unity"
            };

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            };

            Debug.Log($"[Build] Starting Fire Mine Simulator Phase 1 build at: {apkPath}");

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[Build] FireMine Phase 1 SUCCEEDED: {summary.totalSize} bytes → {apkPath}");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"[Build] FireMine Phase 1 FAILED with {summary.totalErrors} errors.");
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// PHASE 1 — Real Camera AR Foundation.
        /// Minimal build: Splash → Login → Home → FireModuleIntro → ARPhase1_FloorScan.
        /// Used to verify real camera, floor detection, and anchor placement on device.
        /// </summary>
        public static void BuildAndroidAPK_Phase1_ARFoundation()
        {
            string outputDirectory = "D:/AR-mining/SurakshaAR/Builds";
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            string apkPath = System.IO.Path.Combine(outputDirectory, "SurakshaAR_Phase1_ARFoundation.apk");

            // Ensure Google ARCore XR Plug-in is enabled for Android
            EnsureXRSettingsConfigured();

            // Build only the Phase 1 scene (standalone test)
            SceneBuilderScript.CreateARPhase1Scene();

            // Configure Player Settings for Android & ARCore
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            PlayerSettings.applicationIdentifier = "com.surakshaar.training";
            PlayerSettings.productName = "SurakshaAR";
            PlayerSettings.companyName = "Suraksha";

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.stripEngineCode = false;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            // Phase 1 minimal scene set: includes all entry screens + Phase1 floor scan
            string[] scenes = new string[]
            {
                "Assets/_Project/Scenes/Bootstrap/Splash.unity",
                "Assets/_Project/Scenes/MainMenu/Login.unity",
                "Assets/_Project/Scenes/MainMenu/LanguageSelection.unity",
                "Assets/_Project/Scenes/MainMenu/CameraAccess.unity",
                "Assets/_Project/Scenes/MainMenu/TrainingNotice.unity",
                "Assets/_Project/Scenes/MainMenu/Home.unity",
                "Assets/_Project/Scenes/MainMenu/TrainingModules.unity",
                "Assets/_Project/Scenes/Fire/FireModuleIntro.unity",
                "Assets/_Project/Scenes/Fire/ARPhase1_FloorScan.unity",
                "Assets/_Project/Scenes/Gas/GasModuleIntro.unity",
                "Assets/_Project/Scenes/Fire/ARLearnPlaceholder.unity",
                "Assets/_Project/Scenes/Gas/GasARLearnPlaceholder.unity",
                "Assets/_Project/Scenes/Fire/ARSimulationPlaceholder.unity",
                "Assets/_Project/Scenes/MainMenu/Result.unity",
                "Assets/_Project/Scenes/MainMenu/PerformanceSummary.unity",
                "Assets/_Project/Scenes/MainMenu/CertificatePreview.unity",
                "Assets/_Project/Scenes/MainMenu/QRVerification.unity",
                "Assets/_Project/Scenes/MainMenu/Profile.unity",
                "Assets/_Project/Scenes/MainMenu/Settings.unity"
            };

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            };

            Debug.Log($"Starting SurakshaAR Phase 1 AR Foundation Build at: {apkPath}");

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"SurakshaAR Phase 1 Build SUCCEEDED: {summary.totalSize} bytes. Output: {apkPath}");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"SurakshaAR Phase 1 Build FAILED with {summary.totalErrors} errors.");
                EditorApplication.Exit(1);
            }
        }

        public static void EnsureXRSettingsConfigured()
        {
            if (!AssetDatabase.IsValidFolder("Assets/XR"))
            {
                AssetDatabase.CreateFolder("Assets", "XR");
            }

            XRGeneralSettingsPerBuildTarget buildTargetSettings = null;
            EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out UnityEngine.Object configObj);
            buildTargetSettings = configObj as XRGeneralSettingsPerBuildTarget;

            if (buildTargetSettings == null)
            {
                string path = "Assets/XR/XRGeneralSettingsPerBuildTarget.asset";
                buildTargetSettings = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(path);
                if (buildTargetSettings == null)
                {
                    buildTargetSettings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                    AssetDatabase.CreateAsset(buildTargetSettings, path);
                }
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, buildTargetSettings, true);
            }

            XRGeneralSettings androidSettings = buildTargetSettings.SettingsForBuildTarget(BuildTargetGroup.Android);
            if (androidSettings == null)
            {
                string path = "Assets/XR/Android_XRGeneralSettings.asset";
                androidSettings = AssetDatabase.LoadAssetAtPath<XRGeneralSettings>(path);
                if (androidSettings == null)
                {
                    androidSettings = ScriptableObject.CreateInstance<XRGeneralSettings>();
                    AssetDatabase.CreateAsset(androidSettings, path);
                }
                buildTargetSettings.SetSettingsForBuildTarget(BuildTargetGroup.Android, androidSettings);
            }

            if (androidSettings.Manager == null)
            {
                string path = "Assets/XR/Android_XRManagerSettings.asset";
                XRManagerSettings managerSettings = AssetDatabase.LoadAssetAtPath<XRManagerSettings>(path);
                if (managerSettings == null)
                {
                    managerSettings = ScriptableObject.CreateInstance<XRManagerSettings>();
                    AssetDatabase.CreateAsset(managerSettings, path);
                }
                androidSettings.Manager = managerSettings;
            }

            // Assign ARCoreLoader for Android
            XRPackageMetadataStore.AssignLoader(androidSettings.Manager, "UnityEngine.XR.ARCore.ARCoreLoader", BuildTargetGroup.Android);

            // Configure ARCore settings
            var arcoreSettings = ARCoreSettings.GetOrCreateSettings();
            arcoreSettings.requirement = ARCoreSettings.Requirement.Optional;

            EditorUtility.SetDirty(androidSettings.Manager);
            EditorUtility.SetDirty(androidSettings);
            EditorUtility.SetDirty(buildTargetSettings);
            AssetDatabase.SaveAssets();

            Debug.Log("[BuildScript] XRGeneralSettings persisted to disk with ARCoreLoader assigned.");
        }
    }
}
#endif
