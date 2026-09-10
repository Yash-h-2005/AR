using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurakshaAR.Localization
{
    /// <summary>
    /// Supported UI Languages for SurakshaAR.
    /// </summary>
    public enum Language
    {
        English,
        Hindi,
        Santali
    }

    /// <summary>
    /// Centralized offline key-based Localization Manager.
    /// Supports English, Hindi (हिन्दी), and Santali (with English fallback).
    /// </summary>
    public class LocalizationManager : MonoBehaviour
    {
        private static LocalizationManager instance;
        public static LocalizationManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<LocalizationManager>();
                    if (instance == null)
                    {
                        var go = new GameObject("LocalizationManager");
                        instance = go.AddComponent<LocalizationManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        public event Action<Language> OnLanguageChanged;

        private Language currentLanguage = Language.English;
        public Language CurrentLanguage => currentLanguage;

        private readonly Dictionary<string, string> englishDictionary = new Dictionary<string, string>();
        private readonly Dictionary<string, string> hindiDictionary = new Dictionary<string, string>();
        private readonly Dictionary<string, string> santaliDictionary = new Dictionary<string, string>();

        private bool isInitialized = false;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureInitialized();
        }

        public void EnsureInitialized()
        {
            if (isInitialized) return;
            InitializeDictionaries();
            LoadSavedLanguage();
            isInitialized = true;
        }

        private void LoadSavedLanguage()
        {
            string savedLang = PlayerPrefs.GetString("SurakshaAR_Language", "English");
            if (Enum.TryParse(savedLang, out Language lang))
            {
                currentLanguage = lang;
            }
            else
            {
                currentLanguage = Language.English;
            }
        }

        public void SetLanguage(Language language)
        {
            EnsureInitialized();
            currentLanguage = language;
            PlayerPrefs.SetString("SurakshaAR_Language", language.ToString());
            PlayerPrefs.Save();

            Debug.Log($"[LocalizationManager] Language changed to: {language}");
            OnLanguageChanged?.Invoke(currentLanguage);
        }

        public string GetString(string key)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(key)) return string.Empty;

            Dictionary<string, string> targetDict = currentLanguage switch
            {
                Language.Hindi => hindiDictionary,
                Language.Santali => santaliDictionary,
                _ => englishDictionary
            };

            if (targetDict != null && targetDict.TryGetValue(key, out string value) && !string.IsNullOrEmpty(value))
            {
                return value;
            }

            // Fallback to English dictionary if key is missing in selected language
            if (englishDictionary.TryGetValue(key, out string fallbackValue))
            {
                return fallbackValue;
            }

            // Fallback to key itself
            return key;
        }

        private void InitializeDictionaries()
        {
            // ==================== ENGLISH ====================
            englishDictionary["APP_TITLE"] = "SurakshaAR";
            englishDictionary["APP_SUBTITLE"] = "Industrial Safety Training";

            // Login Screen
            englishDictionary["LOGIN"] = "LOGIN";
            englishDictionary["LOGIN_SUBTITLE"] = "Enter your Worker details";
            englishDictionary["WORKER_NAME"] = "Worker Name";
            englishDictionary["WORKER_ID"] = "Worker ID";
            englishDictionary["ENTER_WORKER_NAME"] = "Enter your name";
            englishDictionary["ENTER_WORKER_ID"] = "Enter Worker ID";
            englishDictionary["NAME_AND_ID_REQUIRED"] = "Worker Name and Worker ID are required!";
            englishDictionary["WORKER_ID_REQUIRED"] = "Worker ID is required!";
            englishDictionary["WORKER_NAME_REQUIRED"] = "Worker Name is required!";
            englishDictionary["NEED_HELP"] = "Need help? Contact Admin";

            // Language Selection
            englishDictionary["SELECT_LANGUAGE"] = "SELECT LANGUAGE";
            englishDictionary["CHOOSE_PREFERRED_LANG"] = "Choose your preferred training language";
            englishDictionary["ENGLISH"] = "ENGLISH";
            englishDictionary["HINDI"] = "HINDI";
            englishDictionary["SANTALI"] = "SANTALI";
            englishDictionary["CONTINUE"] = "CONTINUE";

            // Camera Access
            englishDictionary["CAMERA_ACCESS"] = "Camera Access";
            englishDictionary["CAMERA_ACCESS_DESC"] = "SURAKSHAAR uses your camera to provide AR-based training experiences.";
            englishDictionary["ALLOW_CAMERA"] = "ALLOW CAMERA ACCESS";
            englishDictionary["CAMERA_PERMISSION_REQUIRED"] = "Camera permission is required to proceed with AR safety training.";

            // Home Screen
            englishDictionary["HELLO_FORMAT"] = "Hello, {0} 👋";
            englishDictionary["WORKER_ID_FORMAT"] = "Worker ID: {0}";
            englishDictionary["TRAINING_PROGRESS"] = "TRAINING PROGRESS";
            englishDictionary["MODULES_COMPLETED_FORMAT"] = "{0} of {1} modules completed";
            englishDictionary["CURRENT_TRAINING"] = "Current Training";
            englishDictionary["FIRE_MODULE_TITLE"] = "Fire & Explosion Response";
            englishDictionary["GAS_MODULE_TITLE"] = "Gas Leak & Confined Space";
            englishDictionary["MACHINERY_MODULE_TITLE"] = "Machinery Safety";
            englishDictionary["COMPLETED"] = "Completed";
            englishDictionary["START_TRAINING"] = "Start";
            englishDictionary["CONTINUE_TRAINING"] = "Continue";
            englishDictionary["NOT_STARTED"] = "Not Started";
            englishDictionary["LOCKED"] = "Locked";
            englishDictionary["COMING_SOON"] = "Coming Soon";
            englishDictionary["MY_CERTIFICATES"] = "My Certificates";
            englishDictionary["NO_CERTIFICATES_YET"] = "No certificates yet";

            // Notifications Popup
            englishDictionary["NOTIFICATIONS"] = "Notifications";
            englishDictionary["NO_NEW_NOTIFICATIONS"] = "No new notifications";

            // Navigation & Drawer
            englishDictionary["NAV_HOME"] = "Home";
            englishDictionary["NAV_TRAINING"] = "Training";
            englishDictionary["NAV_CERTIFICATES"] = "Certificates";
            englishDictionary["NAV_PROFILE"] = "Profile";
            englishDictionary["NAV_SETTINGS"] = "Settings";
            englishDictionary["NAV_LOGOUT"] = "Logout";
            englishDictionary["OFFLINE_TRAINING"] = "Offline Training";
            englishDictionary["ACHIEVEMENTS"] = "Achievements";

            // Module Details & In-Training
            englishDictionary["OBJECTIVE"] = "OBJECTIVE";
            englishDictionary["EMERGENCY_MESSAGES"] = "Emergency Notice";
            englishDictionary["WHAT_YOULL_LEARN"] = "What You'll Learn";
            englishDictionary["LEARN_ITEM_1"] = "✓ Identify fire hazards";
            englishDictionary["LEARN_ITEM_2"] = "✓ Sound emergency alarm";
            englishDictionary["LEARN_ITEM_3"] = "✓ Select correct extinguisher";
            englishDictionary["LEARN_ITEM_4"] = "✓ Evacuate safely via Route A";
            englishDictionary["LEARN_ITEM_5"] = "✓ Reach emergency assembly point";
            englishDictionary["LEARN_MODE_ARROW"] = "LEARN MODE →";
            englishDictionary["SIMULATION_ARROW"] = "SIMULATION →";

            // Module & Sub-module Hierarchy & Status
            englishDictionary["IN_PROGRESS"] = "In Progress";
            englishDictionary["ASSESSMENT_AVAILABLE"] = "Assessment Available";
            englishDictionary["ASSESSMENT_SIMULATION"] = "Assessment Simulation";
            englishDictionary["ASSESSMENT_LOCKED_DESC"] = "Complete all 3 sub-modules to unlock assessment.";
            englishDictionary["ASSESSMENT_PASSED"] = "PASSED";
            englishDictionary["ASSESSMENT_FAILED"] = "FAILED";
            englishDictionary["RETRY_ASSESSMENT"] = "RETRY ASSESSMENT";
            englishDictionary["START_ASSESSMENT"] = "START ASSESSMENT";
            englishDictionary["SUBMODULE_1"] = "Sub-module 1";
            englishDictionary["SUBMODULE_2"] = "Sub-module 2";
            englishDictionary["SUBMODULE_3"] = "Sub-module 3";
            englishDictionary["NO_CERTIFICATES_EARNED"] = "No certificates earned yet.";
            englishDictionary["NO_CERTIFICATES_DESC"] = "Complete all 3 sub-modules and pass the assessment to earn your digital safety certificate.";
            englishDictionary["CERTIFICATE_TITLE_M1"] = "Fire & Explosion Response Training Certificate";
            englishDictionary["CERTIFICATE_TITLE_M2"] = "Gas Leak & Confined Space Protocol Training Certificate";
            englishDictionary["CERTIFICATE_TITLE_M3"] = "Machinery Safety Training Certificate";
            englishDictionary["CERTIFICATE_TITLE_TOTAL"] = "Total Industrial Safety Training Completion Certificate";
            englishDictionary["TOTAL_TRAINING_BADGE"] = "TOTAL TRAINING COMPLETED";
            englishDictionary["ALL_MODULES_PASSED"] = "All 3 Modules Completed";

            // Sub-module Titles
            englishDictionary["M1_S1_TITLE"] = "Fire & Extinguisher for electric fire";
            englishDictionary["M1_S2_TITLE"] = "Fire & mud for petroleum fire";
            englishDictionary["M1_S3_TITLE"] = "Explosion & collision";
            englishDictionary["M2_S1_TITLE"] = "Gas Detection & Atmospheric Testing";
            englishDictionary["M2_S2_TITLE"] = "PPE & Self-Contained Self-Rescuer (SCSR)";
            englishDictionary["M2_S3_TITLE"] = "Confined Space Isolation & Extraction";
            englishDictionary["M3_S1_TITLE"] = "Heavy Mobile Equipment Awareness";
            englishDictionary["M3_S2_TITLE"] = "Conveyor Belt & Pinch Point LOTO";
            englishDictionary["M3_S3_TITLE"] = "Haul Road & Machinery Inspection";

            // Results & Certificates
            englishDictionary["TRAINING_COMPLETED_HEADER"] = "Training Completed 🎉";
            englishDictionary["CERTIFICATE_HEADER"] = "SURAKSHAAR\nSafety Training Certificate";
            englishDictionary["VIEW_CERTIFICATE"] = "VIEW CERTIFICATE";
            englishDictionary["VERIFY_QR"] = "VERIFY QR";
            englishDictionary["COMPLETION_DATE"] = "Completion Date";
            englishDictionary["MODULE_SELECT_HINT"] = "Select a module to view training sub-modules & assessment.";
            englishDictionary["LOCKED_BADGE"] = "🔒 LOCKED";
            englishDictionary["SUBMODULE_LOCKED_DESC"] = "Complete previous sub-module to unlock.";
            englishDictionary["OPEN_MODULE"] = "OPEN MODULE →";
            englishDictionary["SUBMODULES_COUNT_FMT"] = "{0} of 3 Sub-modules Complete";

            // Module Title & Status Aliases
            englishDictionary["M1_TITLE"] = "Fire & Explosion Response";
            englishDictionary["M2_TITLE"] = "Gas Leak & Confined Space";
            englishDictionary["M3_TITLE"] = "Machinery Safety";
            englishDictionary["STATUS_COMPLETED"] = "Completed";
            englishDictionary["STATUS_IN_PROGRESS"] = "In Progress";
            englishDictionary["STATUS_NOT_STARTED"] = "Not Started";
            englishDictionary["STATUS_ASSESSMENT_AVAILABLE"] = "Assessment Available";
            englishDictionary["STATUS_LOCKED"] = "Locked";
            englishDictionary["REVIEW"] = "REVIEW";
            englishDictionary["MODULE_LOCKED_MSG"] = "This module is locked.";


            // ==================== HINDI (हिन्दी) ====================
            hindiDictionary["APP_TITLE"] = "सुरक्षाAR";
            hindiDictionary["APP_SUBTITLE"] = "औद्योगिक सुरक्षा प्रशिक्षण";

            // Login Screen
            hindiDictionary["LOGIN"] = "लॉगिन";
            hindiDictionary["LOGIN_SUBTITLE"] = "कर्मचारी विवरण दर्ज करें";
            hindiDictionary["WORKER_NAME"] = "कर्मचारी का नाम";
            hindiDictionary["WORKER_ID"] = "कर्मचारी आईडी";
            hindiDictionary["ENTER_WORKER_NAME"] = "अपना नाम दर्ज करें";
            hindiDictionary["ENTER_WORKER_ID"] = "कर्मचारी आईडी दर्ज करें";
            hindiDictionary["NAME_AND_ID_REQUIRED"] = "कर्मचारी का नाम और आईडी आवश्यक हैं!";
            hindiDictionary["WORKER_ID_REQUIRED"] = "कर्मचारी आईडी आवश्यक है!";
            hindiDictionary["WORKER_NAME_REQUIRED"] = "कर्मचारी का नाम आवश्यक है!";
            hindiDictionary["NEED_HELP"] = "सहायता चाहिए? एडमिन से संपर्क करें";

            // Language Selection
            hindiDictionary["SELECT_LANGUAGE"] = "भाषा चुनें";
            hindiDictionary["CHOOSE_PREFERRED_LANG"] = "प्रशिक्षण के लिए अपनी पसंदीदा भाषा चुनें";
            hindiDictionary["ENGLISH"] = "अंग्रेजी (ENGLISH)";
            hindiDictionary["HINDI"] = "हिन्दी";
            hindiDictionary["SANTALI"] = "संथाली (SANTALI)";
            hindiDictionary["CONTINUE"] = "जारी रखें";

            // Camera Access
            hindiDictionary["CAMERA_ACCESS"] = "कैमरा एक्सेस";
            hindiDictionary["CAMERA_ACCESS_DESC"] = "सुरक्षाAR संवर्धित वास्तविकता (AR) प्रशिक्षण के लिए कैमरे का उपयोग करता है।";
            hindiDictionary["ALLOW_CAMERA"] = "कैमरा एक्सेस की अनुमति दें";
            hindiDictionary["CAMERA_PERMISSION_REQUIRED"] = "AR सुरक्षा प्रशिक्षण जारी रखने के लिए कैमरा अनुमति आवश्यक है।";

            // Home Screen
            hindiDictionary["HELLO_FORMAT"] = "नमस्ते, {0} 👋";
            hindiDictionary["WORKER_ID_FORMAT"] = "कर्मचारी आईडी: {0}";
            hindiDictionary["TRAINING_PROGRESS"] = "प्रशिक्षण प्रगति";
            hindiDictionary["MODULES_COMPLETED_FORMAT"] = "{0} में से {1} मॉड्यूल पूर्ण";
            hindiDictionary["CURRENT_TRAINING"] = "वर्तमान प्रशिक्षण";
            hindiDictionary["FIRE_MODULE_TITLE"] = "आग और विस्फोट प्रतिक्रिया";
            hindiDictionary["GAS_MODULE_TITLE"] = "गैस रिसाव और सीमित स्थान";
            hindiDictionary["MACHINERY_MODULE_TITLE"] = "मशीनरी सुरक्षा";
            hindiDictionary["COMPLETED"] = "पूर्ण";
            hindiDictionary["START_TRAINING"] = "शुरू करें";
            hindiDictionary["CONTINUE_TRAINING"] = "जारी रखें";
            hindiDictionary["NOT_STARTED"] = "शुरू नहीं हुआ";
            hindiDictionary["LOCKED"] = "लॉक किया गया";
            hindiDictionary["COMING_SOON"] = "जल्द आ रहा है";
            hindiDictionary["MY_CERTIFICATES"] = "मेरे प्रमाणपत्र";
            hindiDictionary["NO_CERTIFICATES_YET"] = "अभी कोई प्रमाणपत्र नहीं";

            // Notifications Popup
            hindiDictionary["NOTIFICATIONS"] = "सूचनाएं";
            hindiDictionary["NO_NEW_NOTIFICATIONS"] = "कोई नई सूचना नहीं";

            // Navigation & Drawer
            hindiDictionary["NAV_HOME"] = "होम";
            hindiDictionary["NAV_TRAINING"] = "प्रशिक्षण";
            hindiDictionary["NAV_CERTIFICATES"] = "प्रमाणपत्र";
            hindiDictionary["NAV_PROFILE"] = "प्रोफ़ाइल";
            hindiDictionary["NAV_SETTINGS"] = "सेटिंग्स";
            hindiDictionary["NAV_LOGOUT"] = "लॉगआउट";
            hindiDictionary["OFFLINE_TRAINING"] = "ऑफ़लाइन प्रशिक्षण";
            hindiDictionary["ACHIEVEMENTS"] = "उपलब्धियां";

            // Module Details & In-Training
            hindiDictionary["OBJECTIVE"] = "उद्देश्य";
            hindiDictionary["EMERGENCY_MESSAGES"] = "आपातकालीन सूचना";
            hindiDictionary["WHAT_YOULL_LEARN"] = "आप क्या सीखेंगे";
            hindiDictionary["LEARN_ITEM_1"] = "✓ आग के खतरों की पहचान करें";
            hindiDictionary["LEARN_ITEM_2"] = "✓ आपातकालीन अलार्म सक्रिय करें";
            hindiDictionary["LEARN_ITEM_3"] = "✓ सही अग्निशामक का चयन करें";
            hindiDictionary["LEARN_ITEM_4"] = "✓ मार्ग A द्वारा सुरक्षित बाहर निकलें";
            hindiDictionary["LEARN_ITEM_5"] = "✓ आपातकालीन असेंबली पॉइंट तक पहुंचें";
            hindiDictionary["LEARN_MODE_ARROW"] = "सीखें मोड →";
            hindiDictionary["SIMULATION_ARROW"] = "सिमुलेशन →";

            // Module & Sub-module Hierarchy & Status
            hindiDictionary["IN_PROGRESS"] = "प्रगति पर है";
            hindiDictionary["ASSESSMENT_AVAILABLE"] = "मूल्यांकन उपलब्ध है";
            hindiDictionary["ASSESSMENT_SIMULATION"] = "मूल्यांकन सिमुलेशन";
            hindiDictionary["ASSESSMENT_LOCKED_DESC"] = "मूल्यांकन अनलॉक करने के लिए सभी 3 उप-मॉड्यूल पूरे करें।";
            hindiDictionary["ASSESSMENT_PASSED"] = "उत्तीर्ण";
            hindiDictionary["ASSESSMENT_FAILED"] = "असफल";
            hindiDictionary["RETRY_ASSESSMENT"] = "पुनः प्रयास करें";
            hindiDictionary["START_ASSESSMENT"] = "मूल्यांकन शुरू करें";
            hindiDictionary["SUBMODULE_1"] = "उप-मॉड्यूल 1";
            hindiDictionary["SUBMODULE_2"] = "उप-मॉड्यूल 2";
            hindiDictionary["SUBMODULE_3"] = "उप-मॉड्यूल 3";
            hindiDictionary["NO_CERTIFICATES_EARNED"] = "अभी तक कोई प्रमाणपत्र अर्जित नहीं किया गया है।";
            hindiDictionary["NO_CERTIFICATES_DESC"] = "अपना डिजिटल सुरक्षा प्रमाणपत्र प्राप्त करने के लिए सभी 3 उप-मॉड्यूल पूरे करें और मूल्यांकन उत्तीर्ण करें।";
            hindiDictionary["CERTIFICATE_TITLE_M1"] = "अग्नि और विस्फोट प्रतिक्रिया प्रशिक्षण प्रमाणपत्र";
            hindiDictionary["CERTIFICATE_TITLE_M2"] = "गैस रिसाव और सीमित स्थान प्रोटोकॉल प्रशिक्षण प्रमाणपत्र";
            hindiDictionary["CERTIFICATE_TITLE_M3"] = "मशीनरी सुरक्षा प्रशिक्षण प्रमाणपत्र";
            hindiDictionary["CERTIFICATE_TITLE_TOTAL"] = "पूर्ण औद्योगिक सुरक्षा प्रशिक्षण समापन प्रमाणपत्र";
            hindiDictionary["TOTAL_TRAINING_BADGE"] = "पूर्ण प्रशिक्षण संपन्न";
            hindiDictionary["ALL_MODULES_PASSED"] = "सभी 3 मॉड्यूल पूर्ण";

            // Sub-module Titles
            hindiDictionary["M1_S1_TITLE"] = "बिजली की आग के लिए आग और अग्निशामक";
            hindiDictionary["M1_S2_TITLE"] = "पेट्रोलियम आग के लिए आग और मिट्टी";
            hindiDictionary["M1_S3_TITLE"] = "विस्फोट और टक्कर";
            hindiDictionary["M2_S1_TITLE"] = "गैस का पता लगाना और वायुमंडलीय परीक्षण";
            hindiDictionary["M2_S2_TITLE"] = "सुरक्षा उपकरण और आत्म-बचावकर्ता (SCSR)";
            hindiDictionary["M2_S3_TITLE"] = "सीमित स्थान अलगाव और बचाव प्रोटोकॉल";
            hindiDictionary["M3_S1_TITLE"] = "भारी मोबाइल उपकरण जागरूकता";
            hindiDictionary["M3_S2_TITLE"] = "कन्वेयर बेल्ट और तालाबंदी-टैगिंग (LOTO)";
            hindiDictionary["M3_S3_TITLE"] = "ढुलाई सड़क और मशीनरी पूर्व-प्रारंभ निरीक्षण";

            // Results & Certificates
            hindiDictionary["TRAINING_COMPLETED_HEADER"] = "प्रशिक्षण पूर्ण 🎉";
            hindiDictionary["CERTIFICATE_HEADER"] = "सुरक्षाAR\nसुरक्षा प्रशिक्षण प्रमाणपत्र";
            hindiDictionary["VIEW_CERTIFICATE"] = "प्रमाणपत्र देखें";
            hindiDictionary["VERIFY_QR"] = "QR सत्यापित करें";
            hindiDictionary["COMPLETION_DATE"] = "पूर्णता तिथि";
            hindiDictionary["MODULE_SELECT_HINT"] = "प्रशिक्षण उप-मॉड्यूल और मूल्यांकन देखने के लिए एक मॉड्यूल चुनें।";
            hindiDictionary["LOCKED_BADGE"] = "🔒 लॉक";
            hindiDictionary["SUBMODULE_LOCKED_DESC"] = "अनलॉक करने के लिए पिछला उप-मॉड्यूल पूरा करें।";
            hindiDictionary["OPEN_MODULE"] = "मॉड्यूल खोलें →";
            hindiDictionary["SUBMODULES_COUNT_FMT"] = "3 में से {0} उप-मॉड्यूल पूर्ण";

            // Module Title & Status Aliases
            hindiDictionary["M1_TITLE"] = "अग्नि और विस्फोट प्रतिक्रिया";
            hindiDictionary["M2_TITLE"] = "गैस रिसाव और सीमित स्थान";
            hindiDictionary["M3_TITLE"] = "मशीनरी सुरक्षा";
            hindiDictionary["STATUS_COMPLETED"] = "पूर्ण";
            hindiDictionary["STATUS_IN_PROGRESS"] = "प्रगति पर";
            hindiDictionary["STATUS_NOT_STARTED"] = "प्रारंभ नहीं हुआ";
            hindiDictionary["STATUS_ASSESSMENT_AVAILABLE"] = "मूल्यांकन उपलब्ध";
            hindiDictionary["STATUS_LOCKED"] = "लॉक";
            hindiDictionary["REVIEW"] = "समीक्षा";
            hindiDictionary["MODULE_LOCKED_MSG"] = "यह मॉड्यूल लॉक है।";


            // ==================== SANTALI (Fallback) ====================
            santaliDictionary["APP_TITLE"] = "SurakshaAR";
            santaliDictionary["LOGIN"] = "LOHIN";
            santaliDictionary["WORKER_NAME"] = "Kamiya Nutum";
            santaliDictionary["WORKER_ID"] = "Kamiya ID";
            santaliDictionary["CONTINUE"] = "Laha Seno-me";
            santaliDictionary["SELECT_LANGUAGE"] = "Parsi Bachhao-me";
            santaliDictionary["NAV_HOME"] = "HOME";
            santaliDictionary["NAV_TRAINING"] = "TRAINING";
            santaliDictionary["NAV_CERTIFICATES"] = "CERTIFICATE";
            santaliDictionary["NAV_PROFILE"] = "PROFILE";
            santaliDictionary["NOTIFICATIONS"] = "NOTIFICATIONS";
            santaliDictionary["NO_NEW_NOTIFICATIONS"] = "No new notifications";
        }
    }
}
