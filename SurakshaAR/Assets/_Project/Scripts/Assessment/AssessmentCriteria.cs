using System;
using UnityEngine;

namespace SurakshaAR.Assessment
{
    [Serializable]
    public class AssessmentCriteria
    {
        [Header("Scoring Weights (Total = 100 Points)")]
        public float correctActionsWeight = 40f;
        public float sequenceAccuracyWeight = 20f;
        public float independentPerformanceWeight = 20f;
        public float responseCompletionWeight = 10f;
        public float hazardRecognitionWeight = 10f;

        [Header("Deduction Penalties")]
        public float incorrectActionPenalty = 5f;
        public float sequenceViolationPenalty = 10f;
        public float excessiveRetryPenalty = 3f;

        [Header("Certification Pass Thresholds")]
        public float minimumPassingScore = 70f;
        public float minimumSequenceAccuracyPercent = 70f;
        public int maximumCriticalViolationsAllowed = 0;

        public static AssessmentCriteria Default => new AssessmentCriteria();
    }
}
