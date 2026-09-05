using System;
using System.Collections;
using UnityEngine;
using YWJ.Player.Progress;
using YWJ.GameStateMachine;


namespace YWJ.GameFlow
{
    public class GameFlowManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private PlayerProgressManager _playerProgressManager;

        [SerializeField]
        private GameStateController _gameStateController;

        private Coroutine _flowCoroutine;

        private PlayerProgress _currentContent;

        private bool _isFlowRunning;
        private bool _isContentCompleted;
        private bool _isContentSucceeded;

        public PlayerProgress CurrentContent =>
            _currentContent;

        public bool IsFlowRunning =>
            _isFlowRunning;

        /// <summary>
        /// GameState와 UI 준비가 끝난 뒤 발생합니다.
        /// 실제 콘텐츠 시스템이 이 이벤트를 구독합니다.
        /// </summary>
        public event Action<PlayerProgress> ContentStarted;

        /// <summary>
        /// 콘텐츠가 정상적으로 끝나고
        /// 다음 진행도로 이동하기 전에 발생합니다.
        /// </summary>
        public event Action<PlayerProgress> ContentCompleted;

        /// <summary>
        /// 콘텐츠가 실패했을 때 발생합니다.
        /// </summary>
        public event Action<PlayerProgress> ContentFailed;

        private void Awake()
        {
            FindReferences();
        }

        private void OnDestroy()
        {
            StopCurrentFlow();
        }

        private void FindReferences()
        {
            if (_playerProgressManager == null)
            {
                _playerProgressManager =
                    FindFirstObjectByType<PlayerProgressManager>();
            }

            if (_gameStateController == null)
            {
                _gameStateController =
                    FindFirstObjectByType<GameStateController>();
            }
        }

        /// <summary>
        /// 시작 화면의 게임 시작 버튼에서 호출합니다.
        /// 현재 저장된 PlayerProgress에 맞는 콘텐츠부터 실행합니다.
        /// </summary>
        public void StartCurrentFlow()
        {
            if (_isFlowRunning)
            {
                Debug.LogWarning(
                    $"[GameFlowManager] 이미 콘텐츠가 실행 중입니다: " +
                    $"{_currentContent}",
                    this);

                return;
            }

            if (!ValidateReferences())
            {
                return;
            }

            _flowCoroutine =
                StartCoroutine(RunFlowRoutine());
        }

        private IEnumerator RunFlowRoutine()
        {
            _isFlowRunning = true;

            while (_isFlowRunning)
            {
                _currentContent =
                    _playerProgressManager.CurrentProgress;

                GameState targetState =
                    GetGameState(_currentContent);

                Debug.Log(
                    $"[GameFlowManager] 콘텐츠 준비: " +
                    $"{_currentContent} / {targetState}",
                    this);

                /*
                 * GameStateController 내부에서:
                 *
                 * 1. 이전 GSM Exit
                 * 2. UI 변경
                 * 3. 한 프레임 대기
                 * 4. 새로운 GSM Enter
                 *
                 * 순서를 모두 마칠 때까지 기다립니다.
                 */
                yield return _gameStateController
                    .ChangeStateRoutine(targetState);

                _isContentCompleted = false;
                _isContentSucceeded = false;

                Debug.Log(
                    $"[GameFlowManager] 콘텐츠 시작: " +
                    $"{_currentContent}",
                    this);

                /*
                 * CutSceneManager, TutorialManager,
                 * StageManager, BossStageManager 등이
                 * 이 이벤트를 구독합니다.
                 */
                ContentStarted?.Invoke(_currentContent);

                if (_currentContent ==
                    PlayerProgress.GameCompleted)
                {
                    _isFlowRunning = false;
                    break;
                }

                /*
                 * 실제 콘텐츠에서 CompleteCurrentFlow()
                 * 또는 FailCurrentFlow()를 호출할 때까지 기다립니다.
                 */
                yield return new WaitUntil(
                    () => _isContentCompleted);

                PlayerProgress finishedContent =
                    _currentContent;

                if (!_isContentSucceeded)
                {
                    Debug.Log(
                        $"[GameFlowManager] 콘텐츠 실패: " +
                        $"{finishedContent}",
                        this);

                    ContentFailed?.Invoke(finishedContent);

                    _isFlowRunning = false;
                    break;
                }

                PlayerProgress nextProgress =
                    GetNextProgress(finishedContent);

                Debug.Log(
                    $"[GameFlowManager] 콘텐츠 완료: " +
                    $"{finishedContent} → {nextProgress}",
                    this);

                _playerProgressManager.SetProgress(
                    nextProgress);

                ContentCompleted?.Invoke(
                    finishedContent);

                /*
                 * 종료 이벤트 처리와 오브젝트 정리가 끝날 수 있도록
                 * 다음 프레임까지 기다린 뒤 다음 콘텐츠를 실행합니다.
                 */
                yield return null;
            }

            _currentContent =
                _playerProgressManager.CurrentProgress;

            _flowCoroutine = null;
        }

        /// <summary>
        /// 컷신 종료, 튜토리얼 완료,
        /// 일반 스테이지 클리어, 보스 클리어 시 호출합니다.
        /// </summary>
        public void CompleteCurrentFlow()
        {
            if (!_isFlowRunning)
            {
                Debug.LogWarning(
                    "[GameFlowManager] 실행 중인 Flow가 없습니다.",
                    this);

                return;
            }

            if (_isContentCompleted)
            {
                Debug.LogWarning(
                    "[GameFlowManager] 현재 콘텐츠는 이미 종료 처리됐습니다.",
                    this);

                return;
            }

            if (_currentContent ==
                PlayerProgress.GameCompleted)
            {
                Debug.LogWarning(
                    "[GameFlowManager] GameCompleted 이후에는 " +
                    "다음 진행도가 없습니다.",
                    this);

                return;
            }

            _isContentSucceeded = true;
            _isContentCompleted = true;
        }

        /// <summary>
        /// 일반 스테이지나 보스 스테이지 실패 시 호출합니다.
        /// PlayerProgress는 변경하지 않습니다.
        /// </summary>
        public void FailCurrentFlow()
        {
            if (!_isFlowRunning)
            {
                Debug.LogWarning(
                    "[GameFlowManager] 실행 중인 Flow가 없습니다.",
                    this);

                return;
            }

            if (_isContentCompleted)
            {
                return;
            }

            _isContentSucceeded = false;
            _isContentCompleted = true;
        }

        /// <summary>
        /// 현재 PlayerProgress를 변경하지 않고 다시 실행합니다.
        /// </summary>
        public void RestartCurrentFlow()
        {
            StopCurrentFlow();
            StartCurrentFlow();
        }

        /// <summary>
        /// 현재 실행 중인 Flow를 중단합니다.
        /// PlayerProgress는 변경하지 않습니다.
        /// </summary>
        public void StopCurrentFlow()
        {
            if (_flowCoroutine != null)
            {
                StopCoroutine(_flowCoroutine);
                _flowCoroutine = null;
            }

            _isFlowRunning = false;
            _isContentCompleted = false;
            _isContentSucceeded = false;
        }

        private bool ValidateReferences()
        {
            bool isValid = true;

            if (_playerProgressManager == null)
            {
                Debug.LogError(
                    "[GameFlowManager] " +
                    "PlayerProgressManager가 없습니다.",
                    this);

                isValid = false;
            }

            if (_gameStateController == null)
            {
                Debug.LogError(
                    "[GameFlowManager] " +
                    "GameStateController가 없습니다.",
                    this);

                isValid = false;
            }

            return isValid;
        }

        private static GameState GetGameState(
            PlayerProgress progress)
        {
            return progress switch
            {
                PlayerProgress.OpeningCutScene =>
                    GameState.STORY_MODE,

                PlayerProgress.BasicTutorial =>
                    GameState.MUSIC_MODE,

                PlayerProgress.Stage01 =>
                    GameState.MUSIC_MODE,

                PlayerProgress.CutScene01 =>
                    GameState.STORY_MODE,

                PlayerProgress.BossStage01 =>
                    GameState.BOSS_MODE,

                PlayerProgress.Stage02 =>
                    GameState.MUSIC_MODE,

                PlayerProgress.CutScene02 =>
                    GameState.STORY_MODE,

                PlayerProgress.BossStage02 =>
                    GameState.BOSS_MODE,

                PlayerProgress.EndingCutScene =>
                    GameState.STORY_MODE,

                PlayerProgress.GameCompleted =>
                    GameState.GAME_CLEAR,

                _ => throw new ArgumentOutOfRangeException(
                    nameof(progress),
                    progress,
                    null)
            };
        }

        private static PlayerProgress GetNextProgress(
            PlayerProgress progress)
        {
            return progress switch
            {
                PlayerProgress.OpeningCutScene =>
                    PlayerProgress.BasicTutorial,

                PlayerProgress.BasicTutorial =>
                    PlayerProgress.Stage01,

                PlayerProgress.Stage01 =>
                    PlayerProgress.CutScene01,

                PlayerProgress.CutScene01 =>
                    PlayerProgress.BossStage01,

                PlayerProgress.BossStage01 =>
                    PlayerProgress.Stage02,

                PlayerProgress.Stage02 =>
                    PlayerProgress.CutScene02,

                PlayerProgress.CutScene02 =>
                    PlayerProgress.BossStage02,

                PlayerProgress.BossStage02 =>
                    PlayerProgress.EndingCutScene,

                PlayerProgress.EndingCutScene =>
                    PlayerProgress.GameCompleted,

                PlayerProgress.GameCompleted =>
                    PlayerProgress.GameCompleted,

                _ => throw new ArgumentOutOfRangeException(
                    nameof(progress),
                    progress,
                    null)
            };
        }

        public void ReturnToMainMenu(
            PlayerProgress nextProgress)
        {
            _playerProgressManager.SetProgress(
                nextProgress);

            StopCurrentFlow();

            StartCoroutine(
                ReturnToMainMenuRoutine());
        }

        private IEnumerator ReturnToMainMenuRoutine()
        {
            Debug.Log(
                "[GameFlowManager] 메인 화면으로 돌아갑니다.",
                this);


            yield return _gameStateController.ChangeStateRoutine(
                GameState.MAIN_MENU);
        }

#if UNITY_EDITOR
        [ContextMenu("Start Current Flow")]
        private void StartCurrentFlowInEditor()
        {
            StartCurrentFlow();
        }

        [ContextMenu("Complete Current Flow")]
        private void CompleteCurrentFlowInEditor()
        {
            CompleteCurrentFlow();
        }

        [ContextMenu("Fail Current Flow")]
        private void FailCurrentFlowInEditor()
        {
            FailCurrentFlow();
        }
#endif
    }
}