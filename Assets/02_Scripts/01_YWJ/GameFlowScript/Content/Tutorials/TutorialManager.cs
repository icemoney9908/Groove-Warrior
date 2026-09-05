using KKH.Gameplay;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YWJ.GameLogic.Effect;
using YWJ.UI.Conversation;
using YWJ.GameLogic;
using YWJ.Player;
using YWJ.Background;

namespace YWJ.GameFlow.Content
{
    public class TutorialManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private MusicConductor _musicConductor;

        [SerializeField]
        private ConversationManager _conversationManager;

        [SerializeField]
        private TabSuccessChecker _tabSuccessChecker;

        [SerializeField]
        private PlayerAnimationHandler _playerAnimationHandler;

        [SerializeField]
        private InfiniteGroundScroller _infiniteGroundScroller;

        [SerializeField]
        private InfiniteBackgroundScroller _infiniteBackgroundScroller;

        [Header("Tutorial Data")]
        [SerializeField]
        private TutorialSequenceData _tutorialSequence;

        [Header("Debug")]
        [SerializeField]
        private bool _showDebugLog = true;

        private Coroutine _tutorialCoroutine;

        private bool _isTutorialRunning;
        private bool _isWaitingForInputSuccess;
        private bool _inputSucceeded;

        private int _currentStepIndex = -1;

        public bool IsTutorialRunning =>
            _isTutorialRunning;

        public int CurrentStepIndex =>
            _currentStepIndex;

        public event Action TutorialStarted;
        public event Action TutorialCompleted;
        public event Action TutorialFailed;

        public event Action<int, TutorialStep> TutorialStepStarted;
        public event Action<int, TutorialStep> TutorialStepCompleted;

        private void Awake()
        {
            if (_musicConductor == null)
            {
                _musicConductor =
                    FindFirstObjectByType<MusicConductor>();
            }

            if (_conversationManager == null)
            {
                _conversationManager =
                    FindFirstObjectByType<ConversationManager>();
            }

            if (_tabSuccessChecker == null)
            {
                _tabSuccessChecker =
                    FindFirstObjectByType<TabSuccessChecker>();
            }

            if (_playerAnimationHandler == null)
            {
                _playerAnimationHandler = 
                    FindFirstObjectByType<PlayerAnimationHandler>();
            }

            if (_infiniteGroundScroller == null)
            {
                _infiniteGroundScroller =
                    FindFirstObjectByType<InfiniteGroundScroller>();
            }

            if (_infiniteBackgroundScroller == null)
            {
                _infiniteBackgroundScroller =
                    FindFirstObjectByType<InfiniteBackgroundScroller>();
            }
        }

        private void OnEnable()
        {
            TutorialStarted += OnTutorialStarted;
            TutorialCompleted += OnTutorialFinished;
            TutorialFailed += OnTutorialFinished;
        }

        private void OnDisable()
        {
            TutorialStarted -= OnTutorialStarted;
            TutorialCompleted -= OnTutorialFinished;
            TutorialFailed -= OnTutorialFinished;

            StopTutorialCoroutine();

            _isTutorialRunning = false;
            _isWaitingForInputSuccess = false;
            _inputSucceeded = false;
        }

        private void OnTutorialStarted()
        {
            _playerAnimationHandler?.EnableAnimation();

            _tabSuccessChecker.TabEffectRequested += OnHandleTabSuccess;
        }

        private void OnTutorialFinished()
        {
            _playerAnimationHandler?.DisableAnimation();

            _tabSuccessChecker.TabEffectRequested -= OnHandleTabSuccess;
        }

        private void OnHandleTabSuccess(TabEffectType effectType)
        {
            if (effectType == TabEffectType.TapSuccess)
            {
                NotifyTabSuccess();
            }
            else if (effectType == TabEffectType.HoldStart)
            {
                NotifyHoldStartSuccess();
            }
            else if (effectType == TabEffectType.HoldComplete)
            {
                NotifyHoldCompleteSuccess();
            }
        }

        public void StartTutorial()
        {
            if (_isTutorialRunning)
            {
                Debug.LogWarning(
                    "[TutorialManager] 이미 튜토리얼이 실행 중입니다.",
                    this);

                return;
            }

            if (!ValidateReferences())
            {
                TutorialFailed?.Invoke();
                return;
            }

            StopTutorialCoroutine();

            _tutorialCoroutine =
                StartCoroutine(TutorialRoutine());
        }

        private IEnumerator TutorialRoutine()
        {
            _isTutorialRunning = true;
            _currentStepIndex = -1;

            _isWaitingForInputSuccess = false;
            _inputSucceeded = false;

            TutorialStarted?.Invoke();

            bool musicStarted =
                _musicConductor.Play(
                    _tutorialSequence.TutorialChartData);

            if (_infiniteGroundScroller != null)
            {
                _infiniteGroundScroller.SetIsMoving(true);
            }

            if (_infiniteBackgroundScroller != null)
            {
                _infiniteBackgroundScroller.StartScrolling();
            }

            if (!musicStarted)
            {
                Debug.LogError(
                    "[TutorialManager] 튜토리얼 음원 재생에 실패했습니다.",
                    this);

                FailTutorial();
                yield break;
            }

            _tabSuccessChecker.InitTabNotes_ForChartChange();

            IReadOnlyList<TutorialStep> steps =
                _tutorialSequence.Steps;

            for (int i = 0; i < steps.Count; i++)
            {
                if (!_isTutorialRunning)
                {
                    yield break;
                }

                _currentStepIndex = i;

                TutorialStep step = steps[i];

                if (step == null)
                {
                    continue;
                }

                Log(
                    $"단계 시작: {i}, " +
                    $"{step.StepName}, " +
                    $"{step.StepType}");

                TutorialStepStarted?.Invoke(i, step);

                yield return ExecuteStep(step);

                TutorialStepCompleted?.Invoke(i, step);

                Log(
                    $"단계 완료: {i}, " +
                    $"{step.StepName}");
            }

            CompleteTutorial();
        }

        private IEnumerator ExecuteStep(
            TutorialStep step)
        {
            switch (step.StepType)
            {
                case TutorialStepType.WaitForSongTime:
                    yield return WaitForSongTime(
                        step.SongTime);
                    break;

                case TutorialStepType.PlayConversation:
                    yield return PlayConversation(step);
                    break;

                case TutorialStepType.WaitForTabSuccess:
                case TutorialStepType.WaitForHoldStartSuccess:
                case TutorialStepType.WaitForHoldCompleteSuccess:
                    yield return WaitForInputSuccess();
                    break;

                case TutorialStepType.PauseMusic:
                    _musicConductor.Pause();
                    if (_infiniteGroundScroller != null)
                    {
                        _infiniteGroundScroller.SetIsMoving(false);
                    }

                    if (_infiniteBackgroundScroller != null)
                    {
                        _infiniteBackgroundScroller.StopScrolling();
                    }
                    break;

                case TutorialStepType.ResumeMusic:
                    _musicConductor.Resume();
                    if (_infiniteGroundScroller != null)
                    {
                        _infiniteGroundScroller.SetIsMoving(true);
                    }

                    if (_infiniteBackgroundScroller != null)
                    {
                        _infiniteBackgroundScroller.StartScrolling();
                    }
                    break;

                case TutorialStepType.CompleteTutorial:
                    CompleteTutorial();
                    break;
            }
        }

        private IEnumerator WaitForSongTime(
            float targetSongTime)
        {
            while (_isTutorialRunning)
            {
                if (_musicConductor.HasEnded)
                {
                    Debug.LogWarning(
                        "[TutorialManager] 목표 시간 전에 음원이 종료되었습니다.",
                        this);

                    yield break;
                }

                if (_musicConductor.CurrentSongTimeSecondsExact >=
                    targetSongTime)
                {
                    yield break;
                }

                yield return null;
            }
        }

        private IEnumerator PlayConversation(
            TutorialStep step)
        {
            if (step.PauseMusicBeforeConversation &&
                _musicConductor.IsPlaying)
            {
                _musicConductor.Pause();
            }

            if (step.ConversationData == null)
            {
                Debug.LogWarning(
                    $"[TutorialManager] 대화 데이터가 없습니다. " +
                    $"Step={step.StepName}",
                    this);

                yield break;
            }

            _conversationManager.StartConversation(
                step.ConversationData);

            while (_conversationManager.IsPlaying)
            {
                yield return null;
            }

            if (step.ResumeMusicAfterConversation &&
                _musicConductor.IsPaused)
            {
                _musicConductor.Resume();
            }
        }

        private IEnumerator WaitForInputSuccess()
        {
            _isWaitingForInputSuccess = true;
            _inputSucceeded = false;

            while (_isTutorialRunning &&
                   !_inputSucceeded)
            {
                yield return null;
            }

            _isWaitingForInputSuccess = false;
            _inputSucceeded = false;
        }

        /// <summary>
        /// TabSuccessChecker의 성공 이벤트에서 호출합니다.
        /// </summary>
        public void NotifyTabSuccess()
        {
            TutorialStepType? currentStepType =
                GetCurrentStepType();

            if (currentStepType !=
                TutorialStepType.WaitForTabSuccess)
            {
                return;
            }

            ResolveInputSuccess("Tab");
        }

        /// <summary>
        /// 홀드 시작 성공 이벤트에서 호출합니다.
        /// </summary>
        public void NotifyHoldStartSuccess()
        {
            TutorialStepType? currentStepType =
                GetCurrentStepType();

            if (currentStepType !=
                TutorialStepType.WaitForHoldStartSuccess)
            {
                return;
            }

            ResolveInputSuccess("HoldStart");
        }

        /// <summary>
        /// 홀드 완료 성공 이벤트에서 호출합니다.
        /// </summary>
        public void NotifyHoldCompleteSuccess()
        {
            TutorialStepType? currentStepType =
                GetCurrentStepType();

            if (currentStepType !=
                TutorialStepType.WaitForHoldCompleteSuccess)
            {
                return;
            }

            ResolveInputSuccess("HoldComplete");
        }

        private void ResolveInputSuccess(
            string inputName)
        {
            if (!_isTutorialRunning ||
                !_isWaitingForInputSuccess)
            {
                return;
            }

            Log($"입력 성공 수신: {inputName}");

            _inputSucceeded = true;
        }

        private TutorialStepType? GetCurrentStepType()
        {
            if (_tutorialSequence == null ||
                _currentStepIndex < 0 ||
                _currentStepIndex >=
                _tutorialSequence.Steps.Count)
            {
                return null;
            }

            TutorialStep currentStep =
                _tutorialSequence.Steps[
                    _currentStepIndex];

            return currentStep?.StepType;
        }

        public void CompleteTutorial()
        {
            if (!_isTutorialRunning)
            {
                return;
            }

            StopTutorialCoroutine();

            _isTutorialRunning = false;
            _isWaitingForInputSuccess = false;
            _inputSucceeded = false;
            _currentStepIndex = -1;

            if (_conversationManager != null &&
                _conversationManager.IsPlaying)
            {
                _conversationManager.StopConversation();
            }

            if (_musicConductor != null)
            {
                _musicConductor.Stop();
            }

            if (_infiniteGroundScroller != null)
            {
                _infiniteGroundScroller.SetIsMoving(false);
            }

            if (_infiniteBackgroundScroller != null)
            {
                _infiniteBackgroundScroller.StopScrolling();
            }

            Log("튜토리얼 완료");

            TutorialCompleted?.Invoke();
        }

        public void FailTutorial()
        {
            if (!_isTutorialRunning)
            {
                return;
            }

            StopTutorialCoroutine();

            _isTutorialRunning = false;
            _isWaitingForInputSuccess = false;
            _inputSucceeded = false;
            _currentStepIndex = -1;

            if (_conversationManager != null &&
                _conversationManager.IsPlaying)
            {
                _conversationManager.StopConversation();
            }

            if (_musicConductor != null)
            {
                _musicConductor.Stop();
            }

            Debug.LogWarning(
                "[TutorialManager] 튜토리얼 실패",
                this);

            TutorialFailed?.Invoke();
        }

        public void StopTutorial()
        {
            if (!_isTutorialRunning)
            {
                return;
            }

            StopTutorialCoroutine();

            _isTutorialRunning = false;
            _isWaitingForInputSuccess = false;
            _inputSucceeded = false;
            _currentStepIndex = -1;

            if (_conversationManager != null &&
                _conversationManager.IsPlaying)
            {
                _conversationManager.StopConversation();
            }

            if (_musicConductor != null)
            {
                _musicConductor.Stop();
            }

            Log("튜토리얼 중단");
        }

        public void RestartTutorial()
        {
            StopTutorial();
            StartTutorial();
        }

        private bool ValidateReferences()
        {
            if (_musicConductor == null)
            {
                Debug.LogError(
                    "[TutorialManager] MusicConductor가 없습니다.",
                    this);

                return false;
            }

            if (_conversationManager == null)
            {
                Debug.LogError(
                    "[TutorialManager] ConversationManager가 없습니다.",
                    this);

                return false;
            }

            if (_tutorialSequence == null)
            {
                Debug.LogError(
                    "[TutorialManager] TutorialSequenceData가 없습니다.",
                    this);

                return false;
            }

            if (_tutorialSequence.TutorialChartData == null)
            {
                Debug.LogError(
                    "[TutorialManager] 튜토리얼 ChartData가 없습니다.",
                    this);

                return false;
            }

            return true;
        }

        private void StopTutorialCoroutine()
        {
            if (_tutorialCoroutine == null)
            {
                return;
            }

            StopCoroutine(_tutorialCoroutine);
            _tutorialCoroutine = null;
        }

        private void Log(string message)
        {
            if (!_showDebugLog)
            {
                return;
            }

            Debug.Log(
                $"[TutorialManager] {message}",
                this);
        }
    }
}