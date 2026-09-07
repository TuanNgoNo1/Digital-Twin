using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace PDTwin.Point
{
    public enum PointAttemptState
    {
        Idle,
        Collecting,
        ProvisionalReady,
        Submitted
    }

    [Serializable]
    public sealed class PointTarget
    {
        public string direction = "forward";
        public float speedRpm = 10f;
        public int encoderPulses = 5000;
        public float rpmTolerancePercent = 5f;
        public int encoderTolerancePulses = 5;
        public float rampUpSeconds = 2f;
        public int minimumRunningSamples = 4;
        public int requiredStoppedSamples = 3;
        public string source = "PROJECT_DEFAULT";
    }

    [Serializable]
    public sealed class PointMeasuredPayload
    {
        public string direction = "unknown";
        public float speedRpm;
        public int encoderPulses;
    }

    [Serializable]
    public sealed class PointBreakdownPayload
    {
        public float wiringScore;
        public float directionScore;
        public float rpmScore;
        public float encoderScore;
    }

    [Serializable]
    public sealed class PointValidationPayload
    {
        public bool telemetryOnline;
        public bool telemetryFresh;
        public bool telemetryOnly;
        public bool motorRunObserved;
        public bool motorStopObserved;
        public int runningSamples;
        public int stoppedSamples;
        public float directionMatchRatio;
        public float rpmErrorPercent;
        public int encoderErrorPulses;
        public string note = string.Empty;
    }

    [Serializable]
    public sealed class PointWiringPayload
    {
        public int correctWires;
        public int totalWires;
    }

    [Serializable]
    public sealed class PointSubmissionDetails
    {
        public int schemaVersion = 1;
        public string lesson = string.Empty;
        public string submitReason = string.Empty;
        public string timestampUtc = string.Empty;
        public float totalScore;
        public PointTarget target = new PointTarget();
        public PointMeasuredPayload measured = new PointMeasuredPayload();
        public PointBreakdownPayload breakdown = new PointBreakdownPayload();
        public PointValidationPayload validation = new PointValidationPayload();
        public PointWiringPayload wiring = new PointWiringPayload();
    }

    public sealed class PointOperationResult
    {
        public string measuredDirection = "unknown";
        public float measuredRpm;
        public int measuredEncoder;
        public float directionMatchRatio;
        public float rpmErrorPercent;
        public int encoderErrorPulses;
        public int runningSamples;
        public int stoppedSamples;
        public bool directionPassed;
        public bool rpmPassed;
        public bool encoderPassed;
    }

    public static class PointMath
    {
        public static float RoundScore(float score)
        {
            if (float.IsNaN(score) || float.IsInfinity(score))
                return 0f;

            return (float)Math.Round(Mathf.Clamp(score, 0f, 10f), 2, MidpointRounding.AwayFromZero);
        }

        public static float WiringScore(int correctWires, int totalWires)
        {
            if (totalWires <= 0)
                return 0f;

            return RoundScore(Mathf.Clamp(correctWires, 0, totalWires) * 5f / totalWires);
        }

        public static string NormalizeDirection(string value)
        {
            string normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
            if (normalized == "reverse" || normalized == "backward" || normalized == "rev" || normalized == "nghich")
                return "reverse";
            if (normalized == "forward" || normalized == "fwd" || normalized == "thuan")
                return "forward";
            return "unknown";
        }

        public static float Median(IList<float> values)
        {
            if (values == null || values.Count == 0)
                return 0f;

            float[] copy = new float[values.Count];
            for (int i = 0; i < values.Count; i++)
                copy[i] = values[i];
            Array.Sort(copy);

            int middle = copy.Length / 2;
            return copy.Length % 2 == 0 ? (copy[middle - 1] + copy[middle]) * 0.5f : copy[middle];
        }

        public static int Median(IList<int> values)
        {
            if (values == null || values.Count == 0)
                return 0;

            int[] copy = new int[values.Count];
            for (int i = 0; i < values.Count; i++)
                copy[i] = values[i];
            Array.Sort(copy);

            int middle = copy.Length / 2;
            return copy.Length % 2 == 0
                ? (int)Math.Round((copy[middle - 1] + copy[middle]) * 0.5, MidpointRounding.AwayFromZero)
                : copy[middle];
        }
    }

    public static class PointTargetProvider
    {
        public static PointTarget Load(string lesson)
        {
            bool isBai1 = string.Equals(lesson, "BAI_1", StringComparison.OrdinalIgnoreCase);
            PointTarget target = new PointTarget
            {
                direction = "forward",
                speedRpm = isBai1 ? 50f : 10f,
                encoderPulses = 5000,
                rpmTolerancePercent = 5f,
                encoderTolerancePulses = 5,
                rampUpSeconds = 2f,
                minimumRunningSamples = 4,
                requiredStoppedSamples = 3,
                source = "PROJECT_DEFAULT"
            };

            Dictionary<string, string> query = ReadQuery();
            bool overridden = false;
            overridden |= ApplyString(query, new[] { "pointDirection", "targetDirection" }, value => target.direction = PointMath.NormalizeDirection(value));
            overridden |= ApplyFloat(query, new[] { "pointRpm", "targetRpm" }, value => target.speedRpm = Mathf.Max(0.01f, value));
            overridden |= ApplyInt(query, new[] { "pointEncoder", "targetEncoder", "targetPulses" }, value => target.encoderPulses = value);
            overridden |= ApplyFloat(query, new[] { "pointRpmTolerance", "rpmTolerance" }, value => target.rpmTolerancePercent = Mathf.Clamp(value, 0f, 100f));
            overridden |= ApplyInt(query, new[] { "pointEncoderTolerance", "encoderTolerance" }, value => target.encoderTolerancePulses = Mathf.Max(0, value));

            target.direction = PointMath.NormalizeDirection(target.direction);
            if (target.direction == "unknown")
                target.direction = "forward";
            target.source = overridden ? "URL_QUERY" : "PROJECT_DEFAULT";
            return target;
        }

        private static Dictionary<string, string> ReadQuery()
        {
            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string url = Application.absoluteURL;
            if (string.IsNullOrWhiteSpace(url))
                return result;

            int queryIndex = url.IndexOf('?');
            if (queryIndex < 0 || queryIndex >= url.Length - 1)
                return result;

            string query = url.Substring(queryIndex + 1);
            int fragmentIndex = query.IndexOf('#');
            if (fragmentIndex >= 0)
                query = query.Substring(0, fragmentIndex);

            string[] pairs = query.Split('&');
            foreach (string pair in pairs)
            {
                if (string.IsNullOrWhiteSpace(pair))
                    continue;

                string[] parts = pair.Split(new[] { '=' }, 2);
                string key = Uri.UnescapeDataString(parts[0].Replace('+', ' '));
                string value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1].Replace('+', ' ')) : string.Empty;
                if (!string.IsNullOrWhiteSpace(key))
                    result[key] = value;
            }

            return result;
        }

        private static bool ApplyString(Dictionary<string, string> query, IEnumerable<string> keys, Action<string> setter)
        {
            foreach (string key in keys)
            {
                if (!query.TryGetValue(key, out string value) || string.IsNullOrWhiteSpace(value))
                    continue;
                setter(value);
                return true;
            }
            return false;
        }

        private static bool ApplyFloat(Dictionary<string, string> query, IEnumerable<string> keys, Action<float> setter)
        {
            foreach (string key in keys)
            {
                if (!query.TryGetValue(key, out string value))
                    continue;
                if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
                    continue;
                setter(parsed);
                return true;
            }
            return false;
        }

        private static bool ApplyInt(Dictionary<string, string> query, IEnumerable<string> keys, Action<int> setter)
        {
            foreach (string key in keys)
            {
                if (!query.TryGetValue(key, out string value))
                    continue;
                if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
                    continue;
                setter(parsed);
                return true;
            }
            return false;
        }
    }

    public sealed class PointOperationCollector
    {
        private readonly List<float> rpmSamples = new List<float>();
        private readonly List<string> directionSamples = new List<string>();
        private readonly List<int> stoppedEncoderSamples = new List<int>();
        private PointTarget target;
        private float runStartedAt = -1f;

        public PointAttemptState State { get; private set; } = PointAttemptState.Idle;
        public bool HasSeenMotorRun { get; private set; }
        public string Status { get; private set; } = "Chua bat dau cham.";
        public int RunningSampleCount => rpmSamples.Count;
        public int StoppedSampleCount => stoppedEncoderSamples.Count;

        public void Begin(PointTarget gradingTarget)
        {
            target = gradingTarget ?? throw new ArgumentNullException(nameof(gradingTarget));
            rpmSamples.Clear();
            directionSamples.Clear();
            stoppedEncoderSamples.Clear();
            runStartedAt = -1f;
            HasSeenMotorRun = false;
            State = PointAttemptState.Collecting;
            Status = "Dang cho motor chay...";
        }

        public void Cancel(string reason)
        {
            State = PointAttemptState.Idle;
            Status = string.IsNullOrWhiteSpace(reason) ? "Da huy luot cham." : reason;
        }

        public bool Accept(bool valid, bool running, float speedRpm, int encoderCount, string direction, float now)
        {
            if (State != PointAttemptState.Collecting || target == null)
                return false;

            if (!valid || float.IsNaN(speedRpm) || float.IsInfinity(speedRpm))
            {
                Status = "Dang cho telemetry hop le...";
                return false;
            }

            if (running)
            {
                if (!HasSeenMotorRun)
                {
                    HasSeenMotorRun = true;
                    runStartedAt = now;
                    rpmSamples.Clear();
                    directionSamples.Clear();
                    stoppedEncoderSamples.Clear();
                }

                if (now - runStartedAt < Mathf.Max(0f, target.rampUpSeconds))
                {
                    Status = "Motor dang tang toc, chua lay mau...";
                    return false;
                }

                rpmSamples.Add(Mathf.Abs(speedRpm));
                directionSamples.Add(PointMath.NormalizeDirection(direction));
                stoppedEncoderSamples.Clear();
                Status = $"Dang do chieu va RPM ({rpmSamples.Count}/{target.minimumRunningSamples})...";
                return false;
            }

            if (!HasSeenMotorRun)
            {
                Status = "Dang cho motor chay...";
                return false;
            }

            if (rpmSamples.Count < Mathf.Max(1, target.minimumRunningSamples))
            {
                HasSeenMotorRun = false;
                runStartedAt = -1f;
                rpmSamples.Clear();
                directionSamples.Clear();
                stoppedEncoderSamples.Clear();
                Status = "Motor dung qua som; hay chay lai de du mau RPM.";
                return false;
            }

            stoppedEncoderSamples.Add(encoderCount);
            int required = Mathf.Max(1, target.requiredStoppedSamples);
            while (stoppedEncoderSamples.Count > required)
                stoppedEncoderSamples.RemoveAt(0);

            Status = $"Dang xac nhan encoder dung ({stoppedEncoderSamples.Count}/{required})...";
            if (stoppedEncoderSamples.Count < required)
                return false;

            int min = stoppedEncoderSamples[0];
            int max = stoppedEncoderSamples[0];
            foreach (int value in stoppedEncoderSamples)
            {
                min = Math.Min(min, value);
                max = Math.Max(max, value);
            }

            if (max - min > Mathf.Max(1, target.encoderTolerancePulses))
            {
                Status = "Encoder chua on dinh sau khi dung...";
                return false;
            }

            State = PointAttemptState.ProvisionalReady;
            Status = "Da du du lieu cham diem.";
            return true;
        }

        public PointOperationResult Evaluate()
        {
            if (State != PointAttemptState.ProvisionalReady || target == null)
                return null;

            string normalizedTarget = PointMath.NormalizeDirection(target.direction);
            int matchingDirections = 0;
            foreach (string sample in directionSamples)
            {
                if (PointMath.NormalizeDirection(sample) == normalizedTarget)
                    matchingDirections++;
            }

            float directionRatio = directionSamples.Count > 0 ? matchingDirections / (float)directionSamples.Count : 0f;
            float medianRpm = PointMath.Median(rpmSamples);
            int medianEncoder = PointMath.Median(stoppedEncoderSamples);
            float rpmError = target.speedRpm > 0f
                ? Mathf.Abs(medianRpm - target.speedRpm) / target.speedRpm * 100f
                : float.PositiveInfinity;
            int encoderError = Math.Abs(medianEncoder - target.encoderPulses);

            string measuredDirection = "unknown";
            if (directionSamples.Count > 0)
            {
                int forward = 0;
                int reverse = 0;
                foreach (string sample in directionSamples)
                {
                    if (PointMath.NormalizeDirection(sample) == "forward") forward++;
                    if (PointMath.NormalizeDirection(sample) == "reverse") reverse++;
                }
                measuredDirection = forward >= reverse ? "forward" : "reverse";
            }

            return new PointOperationResult
            {
                measuredDirection = measuredDirection,
                measuredRpm = medianRpm,
                measuredEncoder = medianEncoder,
                directionMatchRatio = directionRatio,
                rpmErrorPercent = rpmError,
                encoderErrorPulses = encoderError,
                runningSamples = rpmSamples.Count,
                stoppedSamples = stoppedEncoderSamples.Count,
                directionPassed = directionRatio >= 0.8f,
                rpmPassed = rpmError <= target.rpmTolerancePercent,
                encoderPassed = encoderError <= target.encoderTolerancePulses
            };
        }
    }
}
