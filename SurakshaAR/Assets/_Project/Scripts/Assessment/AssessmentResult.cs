using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurakshaAR.Assessment
{
    public enum AssessmentStatus
    {
        PASSED = 1,
        FAILED = 2,
        INCOMPLETE = 3
    }

    [Serializable]
    public class AssessmentResult
    {
        public string assessmentId;
        public string workerId;
        public string workerName;
        public string moduleName;
        public string timestamp;
        
        public float totalScore; // Clamped 0 - 100
        public AssessmentStatus status;
        public bool isCertified;

        [Header("Detailed Score Breakdown")]
        public float correctActionsScore;
        public float sequenceAccuracyScore;
        public float independentPerformanceScore;
        public float responseTimeScore;
        public float hazardRecognitionScore;

        [Header("Accuracy & Error Metrics")]
        public float independentAccuracyPercent;
        public float assistedAccuracyPercent;
        public float sequenceAccuracyPercent;
        public int totalMistakes;
        public int totalRetries;
        public int sequenceErrors;
        public int criticalViolationsCount;
        public float durationSeconds;

        public List<string> mistakeCategories = new List<string>();
        public List<string> skillsToImprove = new List<string>();
        public string recommendedAction;
    }
}
