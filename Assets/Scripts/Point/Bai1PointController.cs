using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using PDTwin.RuntimeUI;

namespace PDTwin.Point
{
    /// <summary>Runtime-only scorer. It does not edit the lesson scene, wires or PLC scripts.</summary>
    public sealed class Bai1PointController : MonoBehaviour
    {
        private const string LessonId = "BAI_1";
        private const int ExpectedWireCount = 15;
        private const int MaximumFailedAttempts = 3;
        private const float WireRefreshInterval = 0.25f;
        private const float TelemetryFreshSeconds = 3f;
        private const float TestMaximumScore = 2.5f;
        private const float DirectionScore = 0.75f;
        private const float RpmScore = 0.75f;
        private const float PositionScore = 1f;

        private enum TestState { Idle, Collecting, Failed, Passed, Exhausted }

        [Serializable]
        private sealed class TestEvaluation
        {
            public bool directionPassed;
            public bool rpmPassed;
            public bool positionPassed;
            public float measuredRpm;
            public float secondaryMeasuredRpm;
            public int phaseAPulses;
            public int phaseBPulses;
            public int finalNetPulses;
            public float directionMatchRatio;
            public float rpmErrorPercent;
            public int positionErrorPulses;
            public int runningSamples;
            public int stoppedSamples;
            public string error = string.Empty;

            public bool Passed => directionPassed && rpmPassed && positionPassed;
            public float Score => (directionPassed ? DirectionScore : 0f)
                                + (rpmPassed ? RpmScore : 0f)
                                + (positionPassed ? PositionScore : 0f);
        }

        private sealed class TestRecord
        {
            public TestState state = TestState.Idle;
            public int failedAttempts;
            public float bestScore;
            public TestEvaluation bestEvaluation;
            public TestEvaluation lastEvaluation;

            public bool Resolved => state == TestState.Passed || state == TestState.Exhausted;
            public int RemainingAttempts => Mathf.Max(0, MaximumFailedAttempts - failedAttempts);

            public void Reset()
            {
                state = TestState.Idle;
                failedAttempts = 0;
                bestScore = 0f;
                bestEvaluation = null;
                lastEvaluation = null;
            }
        }

        private sealed class TelemetryTestCollector
        {
            private const float RampUpSeconds = 2f;
            private const int MinimumRunningSamples = 4;
            private const int RequiredStoppedSamples = 3;
            private const float RpmTolerancePercent = 5f;
            private const int EncoderTolerancePulses = 5;

            private readonly List<float> rpmSamples = new List<float>();
            private readonly List<int> stoppedEncoderSamples = new List<int>();
            private int testIndex;
            private int phaseIndex;
            private int baselineEncoder;
            private int phaseAStopEncoder;
            private float runStartedAt;
            private int matchingDirectionSamples;
            private int directionSamples;
            private int totalRunningSamples;
            private int totalStoppedSamples;
            private bool runObserved;
            private bool waitingForStableStop;
            private float phaseARpm;
            private float phaseADirectionRatio;
            private int phaseARunningSamples;
            private int phaseAStoppedSamples;

            public bool IsCollecting { get; private set; }
            public string Status { get; private set; } = string.Empty;

            public void Begin(int selectedTest, int encoder)
            {
                testIndex = selectedTest;
                phaseIndex = 0;
                baselineEncoder = encoder;
                phaseAStopEncoder = encoder;
                phaseARpm = 0f;
                phaseADirectionRatio = 0f;
                phaseARunningSamples = 0;
                phaseAStoppedSamples = 0;
                totalRunningSamples = 0;
                totalStoppedSamples = 0;
                IsCollecting = true;
                ResetPhaseCapture();
                Status = selectedTest == 0
                    ? $"Đã khóa mốc E0 = {Signed(encoder)}; đang chờ chuỗi RUN → STOP của Test 1."
                    : $"Đã khóa mốc E0 = {Signed(encoder)}; đang chờ pha A chiều Thuận của Test 2.";
            }

            public void Cancel()
            {
                IsCollecting = false;
                Status = string.Empty;
            }

            public bool Accept(bool running, float speedRpm, int encoder, string direction, float now,
                out TestEvaluation completed)
            {
                completed = null;
                if (!IsCollecting)
                    return false;

                if (!runObserved)
                {
                    if (!running)
                        return false;
                    runObserved = true;
                    waitingForStableStop = false;
                    runStartedAt = now;
                    Status = testIndex == 1 && phaseIndex == 1
                        ? "Đang ghi nhận pha B của Test 2."
                        : $"Đang ghi nhận {(testIndex == 0 ? "Test 1" : "pha A của Test 2")}.";
                    return false;
                }

                if (running)
                {
                    if (waitingForStableStop)
                    {
                        stoppedEncoderSamples.Clear();
                        waitingForStableStop = false;
                    }
                    if (now - runStartedAt >= RampUpSeconds)
                    {
                        rpmSamples.Add(Mathf.Abs(speedRpm));
                        directionSamples++;
                        if (DirectionMatches(direction, phaseIndex == 0 ? "forward" : "reverse"))
                            matchingDirectionSamples++;
                        totalRunningSamples++;
                    }
                    return false;
                }

                if (rpmSamples.Count < MinimumRunningSamples)
                {
                    ResetPhaseCapture();
                    Status = "Chuỗi vừa nhận chưa đủ mẫu RUN ổn định; lượt chấm vẫn đang tiếp tục.";
                    return false;
                }

                waitingForStableStop = true;
                stoppedEncoderSamples.Add(encoder);
                totalStoppedSamples++;
                if (stoppedEncoderSamples.Count > RequiredStoppedSamples)
                    stoppedEncoderSamples.RemoveAt(0);
                if (stoppedEncoderSamples.Count < RequiredStoppedSamples || !StopIsStable())
                {
                    Status = "Đã thấy STOP; đang xác nhận vị trí encoder ổn định.";
                    return false;
                }

                int stableEncoder = PointMath.Median(stoppedEncoderSamples);
                float medianRpm = PointMath.Median(rpmSamples);
                float directionRatio = directionSamples > 0
                    ? matchingDirectionSamples / (float)directionSamples : 0f;

                if (testIndex == 1 && phaseIndex == 0)
                {
                    phaseAStopEncoder = stableEncoder;
                    phaseARpm = medianRpm;
                    phaseADirectionRatio = directionRatio;
                    phaseARunningSamples = rpmSamples.Count;
                    phaseAStoppedSamples = stoppedEncoderSamples.Count;
                    phaseIndex = 1;
                    ResetPhaseCapture();
                    Status = "Pha A đã ghi nhận; đang chờ pha B chiều Ngược để trở về vị trí đầu.";
                    return false;
                }

                completed = testIndex == 0
                    ? EvaluateTestOne(stableEncoder, medianRpm, directionRatio)
                    : EvaluateTestTwo(stableEncoder, medianRpm, directionRatio);
                IsCollecting = false;
                return true;
            }

            private TestEvaluation EvaluateTestOne(int finalEncoder, float medianRpm, float directionRatio)
            {
                int delta = finalEncoder - baselineEncoder;
                TestEvaluation result = new TestEvaluation
                {
                    directionPassed = directionRatio >= 0.8f,
                    rpmPassed = PercentError(medianRpm, 10f) <= RpmTolerancePercent,
                    positionPassed = Mathf.Abs(delta - 5000) <= EncoderTolerancePulses,
                    measuredRpm = medianRpm,
                    phaseAPulses = delta,
                    finalNetPulses = delta,
                    directionMatchRatio = directionRatio,
                    rpmErrorPercent = PercentError(medianRpm, 10f),
                    positionErrorPulses = Mathf.Abs(delta - 5000),
                    runningSamples = totalRunningSamples,
                    stoppedSamples = totalStoppedSamples
                };
                result.error = BuildTestOneResult(result);
                return result;
            }

            private TestEvaluation EvaluateTestTwo(int finalEncoder, float phaseBRpm, float phaseBDirectionRatio)
            {
                int deltaA = phaseAStopEncoder - baselineEncoder;
                int deltaB = finalEncoder - phaseAStopEncoder;
                int net = finalEncoder - baselineEncoder;
                float rpmErrorA = PercentError(phaseARpm, 5f);
                float rpmErrorB = PercentError(phaseBRpm, 5f);
                int positionError = Mathf.Max(Mathf.Abs(deltaA - 2500),
                    Mathf.Abs(deltaB + 2500), Mathf.Abs(net));
                TestEvaluation result = new TestEvaluation
                {
                    directionPassed = phaseADirectionRatio >= 0.8f && phaseBDirectionRatio >= 0.8f,
                    rpmPassed = rpmErrorA <= RpmTolerancePercent && rpmErrorB <= RpmTolerancePercent,
                    positionPassed = Mathf.Abs(deltaA - 2500) <= EncoderTolerancePulses
                                     && Mathf.Abs(deltaB + 2500) <= EncoderTolerancePulses
                                     && Mathf.Abs(net) <= EncoderTolerancePulses,
                    measuredRpm = phaseARpm,
                    secondaryMeasuredRpm = phaseBRpm,
                    phaseAPulses = deltaA,
                    phaseBPulses = deltaB,
                    finalNetPulses = net,
                    directionMatchRatio = Mathf.Min(phaseADirectionRatio, phaseBDirectionRatio),
                    rpmErrorPercent = Mathf.Max(rpmErrorA, rpmErrorB),
                    positionErrorPulses = positionError,
                    runningSamples = phaseARunningSamples + rpmSamples.Count,
                    stoppedSamples = phaseAStoppedSamples + stoppedEncoderSamples.Count
                };
                result.error = BuildTestTwoResult(result);
                return result;
            }

            private void ResetPhaseCapture()
            {
                rpmSamples.Clear();
                stoppedEncoderSamples.Clear();
                runObserved = false;
                waitingForStableStop = false;
                runStartedAt = 0f;
                matchingDirectionSamples = 0;
                directionSamples = 0;
            }

            private bool StopIsStable()
            {
                int minimum = stoppedEncoderSamples[0];
                int maximum = minimum;
                for (int i = 1; i < stoppedEncoderSamples.Count; i++)
                {
                    minimum = Mathf.Min(minimum, stoppedEncoderSamples[i]);
                    maximum = Mathf.Max(maximum, stoppedEncoderSamples[i]);
                }
                return maximum - minimum <= EncoderTolerancePulses;
            }

            private static bool DirectionMatches(string actual, string expected)
            {
                return string.Equals(PointMath.NormalizeDirection(actual), expected,
                    StringComparison.OrdinalIgnoreCase);
            }

            private static float PercentError(float measured, float target)
            {
                return target <= 0f ? 0f : Mathf.Abs(measured - target) * 100f / target;
            }

            private static string BuildTestOneResult(TestEvaluation result)
            {
                if (!result.directionPassed)
                    return "Sai chiều quay: telemetry không ghi nhận chiều Thuận theo yêu cầu.";
                if (!result.rpmPassed)
                    return $"Sai số tốc độ: đo {result.measuredRpm:F2} RPM, mục tiêu 10 RPM, lệch {result.rpmErrorPercent:F1}%.";
                if (!result.positionPassed)
                    return $"Sai số vị trí cuối: ΔEncoder {Signed(result.phaseAPulses)} / +5000, lệch {result.positionErrorPulses} xung.";
                return "Test 1 đạt đủ chiều quay, tốc độ và vị trí encoder.";
            }

            private static string BuildTestTwoResult(TestEvaluation result)
            {
                if (!result.directionPassed)
                    return "Sai trình tự chiều quay: telemetry chưa xác nhận đủ pha Thuận rồi pha Ngược.";
                if (!result.rpmPassed)
                    return $"Sai số tốc độ: pha A {result.measuredRpm:F2} RPM, pha B {result.secondaryMeasuredRpm:F2} RPM; mục tiêu 5 RPM.";
                if (!result.positionPassed)
                    return $"Sai số vị trí: pha A {Signed(result.phaseAPulses)}, pha B {Signed(result.phaseBPulses)}, vị trí cuối {Signed(result.finalNetPulses)} xung.";
                return "Test 2 đạt đủ hai chiều, tốc độ và trở về đúng vị trí đầu.";
            }

            private static string Signed(int value) => value >= 0 ? "+" + value : value.ToString();
        }

        [Serializable]
        private sealed class TestSubmissionPayload
        {
            public string name;
            public string state;
            public int failedAttempts;
            public int remainingAttempts;
            public float bestScore;
            public bool passed;
            public float measuredRpm;
            public float secondaryMeasuredRpm;
            public int phaseAPulses;
            public int phaseBPulses;
            public int finalNetPulses;
            public bool directionPassed;
            public bool rpmPassed;
            public bool positionPassed;
            public string lastResult;
        }

        [Serializable]
        private sealed class SubmissionPayload
        {
            public int schemaVersion = 2;
            public string lesson = LessonId;
            public string submitReason;
            public string timestampUtc;
            public float totalScore;
            public int correctWires;
            public int expectedWires = ExpectedWireCount;
            public int detectedWires;
            public float wiringScore;
            public bool telemetryOnline;
            public bool telemetryFresh;
            public string timeoutPolicy = "BEST_COMPLETED_RESULTS;IN_PROGRESS_DISCARDED";
            public TestSubmissionPayload test1;
            public TestSubmissionPayload test2;
        }

        private readonly List<WireBody> lessonWires = new List<WireBody>();
        private readonly TestRecord[] tests = { new TestRecord(), new TestRecord() };
        private readonly TelemetryTestCollector collector = new TelemetryTestCollector();
        private CircuitManager circuitManager;
        private PLCController_v2 plc;
        private PointRuntimePanel panel;
        private float nextWireRefresh;
        private float lastTelemetryAt = float.NegativeInfinity;
        private float lastValidTelemetryAt = float.NegativeInfinity;
        private int correctWires;
        private int totalWires;
        private int currentTest;
        private int reviewedTest = -1;
        private float currentScore;
        private bool submittedLocally;
        private string infrastructureMessage = string.Empty;

        private IEnumerator Start()
        {
            panel = PointRuntimePanel.Create("BÀI THỰC HÀNH 1 • CHI TIẾT ĐIỂM");
            panel.transform.SetParent(transform, false);
            panel.PrimaryRequested += HandlePrimaryAction;
            panel.SubmitRequested += RequestSubmit;
            panel.ReviewTestRequested += ReviewTest;
            float deadline = Time.realtimeSinceStartup + 12f;
            while (circuitManager == null && Time.realtimeSinceStartup < deadline)
            {
                circuitManager = CircuitManager.Instance != null ? CircuitManager.Instance
                    : FindFirstObjectByType<CircuitManager>(FindObjectsInactive.Include);
                if (circuitManager == null)
                    yield return null;
            }
            BindPlcIfAvailable();
            RefreshWiring(true);
            RefreshPanel();
            if (circuitManager == null)
            {
                infrastructureMessage = "Không có dữ liệu CircuitManager; hệ thống chưa thể xác nhận phần đấu dây.";
                RefreshPanel();
            }
        }

        private void Update()
        {
            if (PDTwinBridge.IsSubmitted || submittedLocally)
                return;
            BindPlcIfAvailable();
            if (Time.unscaledTime >= nextWireRefresh)
            {
                nextWireRefresh = Time.unscaledTime + WireRefreshInterval;
                RefreshWiring(false);
                RefreshPanel();
            }
            if (collector.IsCollecting && Time.unscaledTime - lastValidTelemetryAt > TelemetryFreshSeconds)
                CancelForInfrastructure("Không đủ dữ liệu chấm: telemetry vật lý bị gián đoạn. Lượt hiện tại không bị trừ.");
        }

        private void OnDestroy()
        {
            if (plc != null)
                plc.OnTelemetryUpdated -= OnTelemetryUpdated;
            if (panel != null)
            {
                panel.PrimaryRequested -= HandlePrimaryAction;
                panel.SubmitRequested -= RequestSubmit;
                panel.ReviewTestRequested -= ReviewTest;
            }
        }

        private void BindPlcIfAvailable()
        {
            if (plc != null)
                return;
            plc = PLCController_v2.Instance != null ? PLCController_v2.Instance
                : FindFirstObjectByType<PLCController_v2>(FindObjectsInactive.Include);
            if (plc != null)
            {
                plc.OnTelemetryUpdated += OnTelemetryUpdated;
                if (plc.IsPiOnline && plc.LatestTelemetry != null && plc.LatestTelemetry.backendSynced)
                    lastValidTelemetryAt = Time.unscaledTime;
            }
        }

        private void RefreshWiring(bool forceProgress)
        {
            if (circuitManager == null)
                return;
            int previousCorrect = correctWires;
            int previousTotal = totalWires;
            lessonWires.Clear();
            HashSet<WireBody> unique = new HashSet<WireBody>();
            if (circuitManager.stepRoots != null)
            {
                foreach (GameObject root in circuitManager.stepRoots)
                {
                    if (root == null) continue;
                    foreach (WireBody wire in root.GetComponentsInChildren<WireBody>(true))
                        if (wire != null && unique.Add(wire)) lessonWires.Add(wire);
                }
            }
            totalWires = lessonWires.Count;
            correctWires = 0;
            foreach (WireBody wire in lessonWires)
                if (wire.isFullyConnected && wire.isCorrect) correctWires++;
            bool changed = previousCorrect != correctWires || previousTotal != totalWires;
            if (changed && correctWires < ExpectedWireCount && HasAnyTestResult())
            {
                collector.Cancel();
                ResetTests();
                infrastructureMessage = "Kết quả vận hành đã được xóa vì trạng thái đấu dây thay đổi sau khi chấm.";
            }
            RecalculateScore();
            if (changed || forceProgress) PDTwinBridge.ReportProgress(currentScore);
        }

        private void HandlePrimaryAction()
        {
            if (PDTwinBridge.IsSubmitted || submittedLocally) return;
            if (reviewedTest >= 0)
            {
                reviewedTest = -1;
                RefreshPanel();
                return;
            }
            TestRecord record = tests[currentTest];
            if (record.state == TestState.Passed || record.state == TestState.Exhausted)
            {
                if (currentTest == 0) currentTest = 1;
                RefreshPanel();
                return;
            }
            BeginAttempt();
        }

        private void BeginAttempt()
        {
            if (correctWires < ExpectedWireCount)
            {
                infrastructureMessage = $"Chưa đủ điều kiện chấm: đấu dây hiện tại {correctWires}/15; phát hiện {totalWires} dây.";
                RefreshPanel();
                return;
            }
            TestRecord record = tests[currentTest];
            if (record.RemainingAttempts <= 0)
            {
                record.state = TestState.Exhausted;
                RefreshPanel();
                return;
            }
            BindPlcIfAvailable();
            bool telemetryReady = plc != null && plc.IsPiOnline && plc.LatestTelemetry != null
                                  && plc.LatestTelemetry.backendSynced
                                  && Time.unscaledTime - lastValidTelemetryAt <= TelemetryFreshSeconds;
            if (!telemetryReady)
            {
                infrastructureMessage = "Không đủ dữ liệu chấm: PLC hoặc telemetry vật lý chưa sẵn sàng. Lượt hiện tại không bị trừ.";
                RefreshPanel();
                return;
            }
            if (plc.LatestTelemetry.running)
            {
                infrastructureMessage = "Chưa thể bắt đầu chấm: motor đang RUN. Mốc E0 chưa được ghi và lượt hiện tại không bị trừ.";
                RefreshPanel();
                return;
            }
            infrastructureMessage = string.Empty;
            reviewedTest = -1;
            record.state = TestState.Collecting;
            collector.Begin(currentTest, ReadEncoder(plc.LatestTelemetry));
            RefreshPanel();
        }

        private void OnTelemetryUpdated(PLCController_v2.MotorTelemetry telemetry)
        {
            if (telemetry == null) return;
            lastTelemetryAt = Time.unscaledTime;
            bool physicalTelemetryValid = plc != null && plc.IsPiOnline && telemetry.backendSynced;
            if (physicalTelemetryValid) lastValidTelemetryAt = Time.unscaledTime;
            if (!collector.IsCollecting || !physicalTelemetryValid) return;
            if (correctWires < ExpectedWireCount)
            {
                CancelForInfrastructure("Trạng thái đấu dây thay đổi trong lúc chấm. Lượt hiện tại không bị trừ.");
                return;
            }
            if (!collector.Accept(telemetry.running, telemetry.speedRpm, ReadEncoder(telemetry),
                    telemetry.direction, Time.unscaledTime, out TestEvaluation evaluation))
            {
                RefreshPanel();
                return;
            }
            CompleteAttempt(evaluation);
        }

        private void CompleteAttempt(TestEvaluation evaluation)
        {
            TestRecord record = tests[currentTest];
            record.lastEvaluation = evaluation;
            if (record.bestEvaluation == null || evaluation.Score > record.bestScore)
            {
                record.bestScore = evaluation.Score;
                record.bestEvaluation = evaluation;
            }
            if (evaluation.Passed)
            {
                record.state = TestState.Passed;
                record.bestScore = TestMaximumScore;
                record.bestEvaluation = evaluation;
            }
            else
            {
                record.failedAttempts++;
                record.state = record.failedAttempts >= MaximumFailedAttempts
                    ? TestState.Exhausted : TestState.Failed;
            }
            infrastructureMessage = string.Empty;
            RecalculateScore();
            PDTwinBridge.ReportProgress(currentScore);
            RefreshPanel();
        }

        private void CancelForInfrastructure(string message)
        {
            collector.Cancel();
            TestRecord record = tests[currentTest];
            if (record.state == TestState.Collecting)
                record.state = record.lastEvaluation == null ? TestState.Idle : TestState.Failed;
            infrastructureMessage = message;
            RefreshPanel();
        }

        private void ReviewTest(int index)
        {
            if (collector.IsCollecting || index < 0 || index >= tests.Length) return;
            bool submitted = PDTwinBridge.IsSubmitted || submittedLocally;
            if (!submitted && index >= currentTest) return;
            if (submitted && tests[index].bestEvaluation == null && !tests[index].Resolved) return;
            reviewedTest = index;
            RefreshPanel();
        }

        private void RequestSubmit()
        {
            if (PDTwinBridge.IsSubmitted || submittedLocally || !tests[1].Resolved) return;
            SubmitFinal("MANUAL");
        }

        public void FinalizeOnTimeout()
        {
            if (PDTwinBridge.IsSubmitted || submittedLocally) return;
            if (collector.IsCollecting)
            {
                collector.Cancel();
                TestRecord record = tests[currentTest];
                record.state = record.lastEvaluation == null ? TestState.Idle : TestState.Failed;
            }
            SubmitFinal("TIMEOUT");
        }

        private void SubmitFinal(string reason)
        {
            RecalculateScore();
            SubmissionPayload payload = BuildSubmissionPayload(reason);
            submittedLocally = true;
            PDTwinBridge.Submit(currentScore, JsonUtility.ToJson(payload));
            reviewedTest = currentTest;
            RefreshPanel();
            panel.LockAfterSubmit(reason == "TIMEOUT");
        }

        private void RecalculateScore()
        {
            float wiringScore = PointMath.WiringScore(correctWires, ExpectedWireCount);
            float operationScore = correctWires >= ExpectedWireCount
                ? tests[0].bestScore + tests[1].bestScore : 0f;
            currentScore = PointMath.RoundScore(wiringScore + operationScore);
        }

        private void RefreshPanel()
        {
            if (panel == null) return;
            RecalculateScore();
            bool submitted = PDTwinBridge.IsSubmitted || submittedLocally;
            panel.SetScore(currentScore, submitted);
            panel.SetOverview(BuildOverview());
            int displayTest = reviewedTest >= 0 ? reviewedTest : currentTest;
            TestRecord displayRecord = tests[displayTest];
            ConfigureTestCard(displayTest, displayRecord);
            ConfigureEvidence(displayTest, displayRecord);
            ConfigureStatus(displayTest, displayRecord);
            ConfigureActions(displayRecord, submitted);
        }

        private PointCriterionView[] BuildOverview()
        {
            float wiringScore = PointMath.WiringScore(correctWires, ExpectedWireCount);
            int selectedTest = reviewedTest >= 0 ? reviewedTest : currentTest;
            return new[]
            {
                new PointCriterionView
                {
                    title = "Đấu nối đúng sơ đồ", evidence = $"{correctWires}/15 dây đúng • phát hiện {totalWires} dây",
                    state = correctWires >= ExpectedWireCount ? "ĐẠT" : "ĐANG LÀM", earned = wiringScore,
                    maximum = 5f, health = correctWires >= ExpectedWireCount ? RuntimeHealth.Online : RuntimeHealth.Warning,
                    selected = false, interactive = false, testIndex = -1
                },
                BuildTestOverview(0, selectedTest), BuildTestOverview(1, selectedTest)
            };
        }

        private PointCriterionView BuildTestOverview(int index, int selectedTest)
        {
            TestRecord record = tests[index];
            bool future = index > currentTest;
            bool submitted = PDTwinBridge.IsSubmitted || submittedLocally;
            string state;
            RuntimeHealth health;
            switch (record.state)
            {
                case TestState.Collecting: state = "ĐANG CHẤM"; health = RuntimeHealth.Warning; break;
                case TestState.Passed: state = "ĐẠT"; health = RuntimeHealth.Online; break;
                case TestState.Failed: state = "CHƯA ĐẠT"; health = RuntimeHealth.Offline; break;
                case TestState.Exhausted: state = "HẾT LƯỢT"; health = RuntimeHealth.Offline; break;
                default: state = future ? "CHƯA MỞ" : "SẴN SÀNG";
                    health = future ? RuntimeHealth.Unknown : RuntimeHealth.Warning; break;
            }
            return new PointCriterionView
            {
                title = index == 0 ? "Test 1 • Chế độ số vòng" : "Test 2 • Chế độ góc",
                evidence = BuildOverviewEvidence(index, record), state = state, earned = record.bestScore,
                maximum = TestMaximumScore, health = health, selected = index == selectedTest,
                interactive = !collector.IsCollecting && (submitted
                    ? record.bestEvaluation != null || record.Resolved
                    : index < currentTest),
                testIndex = index
            };
        }

        private static string BuildOverviewEvidence(int index, TestRecord record)
        {
            if (record.bestEvaluation != null)
                return index == 0
                    ? $"10 RPM • ΔEncoder {Signed(record.bestEvaluation.phaseAPulses)} • {record.failedAttempts} lượt chưa đạt"
                    : $"A {Signed(record.bestEvaluation.phaseAPulses)} • B {Signed(record.bestEvaluation.phaseBPulses)} • cuối {Signed(record.bestEvaluation.finalNetPulses)}";
            return index == 0 ? "Thuận • 10 RPM • 1 vòng"
                : "Thuận +180° • Ngược −180° • về vị trí đầu";
        }

        private void ConfigureTestCard(int index, TestRecord record)
        {
            panel.SetTestHeader($"TEST {index + 1} • {(index == 0 ? "CHẾ ĐỘ SỐ VÒNG" : "CHẾ ĐỘ GÓC")}",
                index == 0 ? "Quay thuận 1 vòng ở 10 RPM" : "Quay +180° rồi −180° để về vị trí đầu",
                reviewedTest >= 0 ? "XEM LẠI" : StateLabel(record.state, index > currentTest),
                reviewedTest >= 0 ? RuntimeHealth.Unknown : StateHealth(record.state));
            if (index == 0)
                panel.SetTargetChips(new[] { "CHIỀU", "TỐC ĐỘ", "QUÃNG ĐƯỜNG", "ΔENCODER" },
                    new[] { "Thuận", "10 RPM", "1 vòng", "+5.000" });
            else
                panel.SetTargetChips(new[] { "PHA A", "PHA B", "TỐC ĐỘ", "KẾT QUẢ" },
                    new[] { "+180° Thuận", "−180° Ngược", "5 RPM", "Về vị trí đầu" });
        }

        private void ConfigureStatus(int index, TestRecord record)
        {
            if (reviewedTest >= 0)
            {
                panel.SetStatus(BuildReadOnlyResult(index, record), StateHealth(record.state));
                panel.SetAttemptSummary("Đang xem lại kết quả đã lưu • Test đã đạt không thể chạy lại");
                return;
            }
            if (!string.IsNullOrWhiteSpace(infrastructureMessage))
                panel.SetStatus(infrastructureMessage, RuntimeHealth.Offline);
            else if (collector.IsCollecting)
                panel.SetStatus(collector.Status, RuntimeHealth.Warning);
            else if (correctWires < ExpectedWireCount)
                panel.SetStatus($"Chưa đủ điều kiện chấm: đấu dây hiện tại {correctWires}/15; phát hiện {totalWires} dây.", RuntimeHealth.Warning);
            else if (record.lastEvaluation != null)
                panel.SetStatus(record.lastEvaluation.error,
                    record.lastEvaluation.Passed ? RuntimeHealth.Online : RuntimeHealth.Offline);
            else
                panel.SetStatus("Test đã sẵn sàng nhận telemetry vật lý.", RuntimeHealth.Online);

            if (record.state == TestState.Passed)
                panel.SetAttemptSummary("Đã đạt • không trừ lượt • Kết quả tốt nhất đã lưu");
            else if (record.state == TestState.Exhausted)
                panel.SetAttemptSummary("Đã dùng hết 3 lượt chưa đạt • Hệ thống giữ điểm cao nhất");
            else if (record.failedAttempts > 0)
                panel.SetAttemptSummary($"Bạn còn {record.RemainingAttempts} lần chấm lại • Hệ thống giữ điểm cao nhất");
            else
                panel.SetAttemptSummary("Có tối đa 3 lượt nếu chưa đạt • Lỗi hạ tầng không bị trừ lượt");
        }

        private void ConfigureEvidence(int index, TestRecord record)
        {
            bool telemetryReady = plc != null && plc.IsPiOnline && plc.LatestTelemetry != null
                                  && plc.LatestTelemetry.backendSynced
                                  && Time.unscaledTime - lastValidTelemetryAt <= TelemetryFreshSeconds;
            float age = Mathf.Max(0f, Time.unscaledTime - lastValidTelemetryAt);
            TestEvaluation evaluation = reviewedTest >= 0 || PDTwinBridge.IsSubmitted || submittedLocally
                ? record.bestEvaluation
                : record.lastEvaluation ?? record.bestEvaluation;

            string gatewayValue = telemetryReady
                ? $"Dữ liệu mới {age:F1} giây • Sẵn sàng"
                : plc != null && plc.IsPiOnline ? "Telemetry vật lý chưa đồng bộ" : "Không có dữ liệu vật lý";
            RuntimeHealth gatewayHealth = telemetryReady ? RuntimeHealth.Online : RuntimeHealth.Offline;

            string sequenceValue;
            RuntimeHealth sequenceHealth;
            if (collector.IsCollecting && reviewedTest < 0)
            {
                sequenceValue = index == 0 ? "Đang thu RUN → STOP" : "Đang thu hai pha vận hành";
                sequenceHealth = RuntimeHealth.Warning;
            }
            else if (evaluation != null)
            {
                sequenceValue = index == 0 ? "Đã ghi nhận RUN → STOP" : "Đã ghi nhận Thuận → Ngược → STOP";
                sequenceHealth = RuntimeHealth.Online;
            }
            else
            {
                sequenceValue = "Chờ RUN • Chưa bắt đầu";
                sequenceHealth = RuntimeHealth.Unknown;
            }

            string rpmValue;
            RuntimeHealth rpmHealth;
            if (evaluation != null)
            {
                rpmValue = index == 0
                    ? $"{evaluation.measuredRpm:F2} RPM • {(evaluation.rpmPassed ? "Đạt" : "Chưa đạt")}"
                    : $"A {evaluation.measuredRpm:F2} • B {evaluation.secondaryMeasuredRpm:F2} RPM";
                rpmHealth = evaluation.rpmPassed ? RuntimeHealth.Online : RuntimeHealth.Offline;
            }
            else
            {
                rpmValue = collector.IsCollecting ? "Đang đo..." : "Chưa đo • —";
                rpmHealth = collector.IsCollecting ? RuntimeHealth.Warning : RuntimeHealth.Unknown;
            }

            string encoderValue;
            RuntimeHealth encoderHealth;
            if (evaluation != null)
            {
                encoderValue = index == 0
                    ? $"Δ {Signed(evaluation.phaseAPulses)} • {(evaluation.positionPassed ? "Đạt" : "Chưa đạt")}"
                    : $"A {Signed(evaluation.phaseAPulses)} • B {Signed(evaluation.phaseBPulses)} • cuối {Signed(evaluation.finalNetPulses)}";
                encoderHealth = evaluation.positionPassed ? RuntimeHealth.Online : RuntimeHealth.Offline;
            }
            else
            {
                encoderValue = collector.IsCollecting ? "Chờ điểm dừng" : "Chưa đo • —";
                encoderHealth = collector.IsCollecting ? RuntimeHealth.Warning : RuntimeHealth.Unknown;
            }

            panel.SetEvidenceRows(
                new[] { "Gateway & telemetry", "Chuỗi vận hành", "RPM thực", "Encoder khi dừng" },
                new[] { gatewayValue, sequenceValue, rpmValue, encoderValue },
                new[] { gatewayHealth, sequenceHealth, rpmHealth, encoderHealth });
        }

        private void ConfigureActions(TestRecord record, bool submitted)
        {
            if (submitted) { panel.SetActions("ĐÃ KHÓA BÀI", false, false); return; }
            if (reviewedTest >= 0) { panel.SetActions("QUAY LẠI TEST 2", true, tests[1].Resolved); return; }
            bool wiringReady = correctWires >= ExpectedWireCount;
            bool submitReady = currentTest == 1 && tests[1].Resolved && !collector.IsCollecting;
            if (collector.IsCollecting) { panel.SetActions("ĐANG CHẤM...", false, false); return; }
            if (!wiringReady) { panel.SetActions("CHƯA ĐỦ 15/15 DÂY", false, false); return; }
            if (record.state == TestState.Passed || record.state == TestState.Exhausted)
            {
                panel.SetActions(currentTest == 0 ? "SANG TEST 2" : "ĐÃ HOÀN THÀNH TEST 2",
                    currentTest == 0, submitReady);
                return;
            }
            string label = record.failedAttempts > 0
                ? $"CHẤM LẠI TEST {currentTest + 1} • CÒN {record.RemainingAttempts} LƯỢT"
                : $"BẮT ĐẦU CHẤM TEST {currentTest + 1}";
            panel.SetActions(label, record.RemainingAttempts > 0, submitReady);
        }

        private SubmissionPayload BuildSubmissionPayload(string reason)
        {
            return new SubmissionPayload
            {
                submitReason = reason, timestampUtc = DateTime.UtcNow.ToString("o"), totalScore = currentScore,
                correctWires = correctWires, detectedWires = totalWires,
                wiringScore = PointMath.WiringScore(correctWires, ExpectedWireCount),
                telemetryOnline = plc != null && plc.IsPiOnline,
                telemetryFresh = Time.unscaledTime - lastValidTelemetryAt <= TelemetryFreshSeconds,
                test1 = BuildTestPayload(0), test2 = BuildTestPayload(1)
            };
        }

        private TestSubmissionPayload BuildTestPayload(int index)
        {
            TestRecord record = tests[index];
            TestEvaluation evaluation = record.bestEvaluation;
            return new TestSubmissionPayload
            {
                name = index == 0 ? "NUMBER_OF_ROTATIONS" : "ANGLE_RETURN_HOME",
                state = record.state.ToString().ToUpperInvariant(), failedAttempts = record.failedAttempts,
                remainingAttempts = record.RemainingAttempts, bestScore = record.bestScore,
                passed = record.state == TestState.Passed, measuredRpm = evaluation?.measuredRpm ?? 0f,
                secondaryMeasuredRpm = evaluation?.secondaryMeasuredRpm ?? 0f,
                phaseAPulses = evaluation?.phaseAPulses ?? 0, phaseBPulses = evaluation?.phaseBPulses ?? 0,
                finalNetPulses = evaluation?.finalNetPulses ?? 0,
                directionPassed = evaluation != null && evaluation.directionPassed,
                rpmPassed = evaluation != null && evaluation.rpmPassed,
                positionPassed = evaluation != null && evaluation.positionPassed,
                lastResult = record.lastEvaluation?.error ?? "NO_COMPLETED_RESULT"
            };
        }

        private string BuildReadOnlyResult(int index, TestRecord record)
        {
            TestEvaluation evaluation = record.bestEvaluation;
            if (evaluation == null) return $"Test {index + 1} chưa có kết quả hoàn chỉnh được lưu.";
            return index == 0
                ? $"Kết quả đã lưu: {evaluation.measuredRpm:F2} RPM • ΔEncoder {Signed(evaluation.phaseAPulses)} • {record.bestScore:F2}/2.50 điểm."
                : $"Kết quả đã lưu: A {Signed(evaluation.phaseAPulses)} • B {Signed(evaluation.phaseBPulses)} • cuối {Signed(evaluation.finalNetPulses)} • {record.bestScore:F2}/2.50 điểm.";
        }

        private bool HasAnyTestResult()
        {
            return collector.IsCollecting || tests[0].state != TestState.Idle || tests[1].state != TestState.Idle;
        }

        private void ResetTests()
        {
            tests[0].Reset(); tests[1].Reset(); currentTest = 0; reviewedTest = -1;
        }

        private static int ReadEncoder(PLCController_v2.MotorTelemetry telemetry)
        {
            if (telemetry == null) return 0;
            return telemetry.encoderCount != 0 ? telemetry.encoderCount : telemetry.count;
        }

        private static RuntimeHealth StateHealth(TestState state)
        {
            switch (state)
            {
                case TestState.Passed: return RuntimeHealth.Online;
                case TestState.Failed:
                case TestState.Exhausted: return RuntimeHealth.Offline;
                default: return RuntimeHealth.Warning;
            }
        }

        private static string StateLabel(TestState state, bool future)
        {
            switch (state)
            {
                case TestState.Passed: return "ĐẠT";
                case TestState.Failed: return "CHƯA ĐẠT";
                case TestState.Exhausted: return "HẾT LƯỢT";
                case TestState.Collecting: return "ĐANG CHẤM";
                default: return future ? "CHƯA MỞ" : "SẴN SÀNG";
            }
        }

        private static string Signed(int value) => value >= 0 ? "+" + value : value.ToString();
    }

    internal static class Bai1PointRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallAfterInitialLoad() => Install(SceneManager.GetActiveScene());
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Install(scene);

        private static void Install(Scene scene)
        {
            if (!scene.IsValid() || !string.Equals(scene.name, "Sy_scene", StringComparison.OrdinalIgnoreCase)) return;
            if (UnityEngine.Object.FindFirstObjectByType<Bai1PointController>(FindObjectsInactive.Include) != null) return;
            GameObject root = new GameObject("Bai1PointRuntime");
            root.AddComponent<Bai1PointController>();
        }
    }
}
