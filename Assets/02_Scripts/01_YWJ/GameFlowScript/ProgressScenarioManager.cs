using UnityEngine;
using YWJ.CutScene;
using YWJ.Player.Progress;
using YWJ.GameFlow.Content;


namespace YWJ.GameFlow
{
    public class ProgressScenarioManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private GameFlowManager _gameFlowManager;

        [SerializeField]
        private CutSceneManager _cutSceneManager;

        [SerializeField]
        private StageManager _stageManager;

        [SerializeField]
        private BossStageManager _bossStageManager;

        [SerializeField]
        private TutorialManager _tutorialManager;

        [Header("Opening")]
        [SerializeField]
        private CutSceneData _openingCutScene;

        [Header("Tutorial")]
        [SerializeField]
        private CutSceneData _tutorialIntroCutScene;

        [Header("Stage 01")]
        [SerializeField]
        private CutSceneData _stage01IntroCutScene;

        [SerializeField]
        private CutSceneData _stage01SuccessCutScene;

        [SerializeField]
        private CutSceneData _stage01FailCutScene;

        [SerializeField]
        private CutSceneData _bossStage01IntroCutScene;

        [Header("Stage 02")]
        [SerializeField]
        private CutSceneData _stage02IntroCutScene;

        [SerializeField]
        private CutSceneData _stage02SuccessCutScene;

        [SerializeField]
        private CutSceneData _stage02FailCutScene;

        [SerializeField]
        private CutSceneData _bossStage02IntroCutScene;

        [Header("Ending")]
        [SerializeField]
        private CutSceneData _endingCutScene;

        private PlayerProgress _currentProgress;
        private PlayerProgress _failedProgress;

        private ScenarioStep _currentStep =
            ScenarioStep.None;

        private bool _isScenarioRunning;


        private enum ScenarioStep
        {
            None,
            CutScene,
            SuccessCutScene,
            Tutorial,
            Stage,
            BossStage,
            FailureCutScene
        }

        private void Awake()
        {
            FindReferences();
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void FindReferences()
        {
            if (_gameFlowManager == null)
            {
                _gameFlowManager =
                    FindFirstObjectByType<GameFlowManager>();
            }

            if (_cutSceneManager == null)
            {
                _cutSceneManager =
                    FindFirstObjectByType<CutSceneManager>();
            }

            if (_stageManager == null)
            {
                _stageManager =
                    FindFirstObjectByType<StageManager>();
            }

            if (_bossStageManager == null)
            {
                _bossStageManager =
                    FindFirstObjectByType<BossStageManager>();
            }

            if (_tutorialManager == null)
            {
                _tutorialManager =
                    FindFirstObjectByType<TutorialManager>();
            }
        }

        private void SubscribeEvents()
        {
            if (_gameFlowManager != null)
            {
                _gameFlowManager.ContentStarted +=
                    HandleContentStarted;

                _gameFlowManager.ContentFailed +=
                    HandleContentFailed;
            }

            if (_cutSceneManager != null)
            {
                _cutSceneManager.CutSceneFinished +=
                    HandleCutSceneFinished;
            }

            if (_tutorialManager != null)
            {
                _tutorialManager.TutorialCompleted +=
                    HandleTutorialCompleted;
            }

            if (_stageManager != null)
            {
                _stageManager.StageCleared +=
                    HandleStageCleared;

                _stageManager.StageFailed +=
                    HandleScenarioFailed;
            }

            if (_bossStageManager != null)
            {
                _bossStageManager.BossStageCleared +=
                    HandleBossStageCleared;

                _bossStageManager.BossStageFailed +=
                    HandleScenarioFailed;
            }

        }

        private void UnsubscribeEvents()
        {
            if (_gameFlowManager != null)
            {
                _gameFlowManager.ContentStarted -=
                    HandleContentStarted;

                _gameFlowManager.ContentFailed -=
                    HandleContentFailed;
            }

            if (_cutSceneManager != null)
            {
                _cutSceneManager.CutSceneFinished -=
                    HandleCutSceneFinished;
            }

            if (_tutorialManager != null)
            {
                _tutorialManager.TutorialCompleted -=
                    HandleTutorialCompleted;
            }

            if (_stageManager != null)
            {
                _stageManager.StageCleared -=
                    HandleStageCleared;

                _stageManager.StageFailed -=
                    HandleScenarioFailed;
            }

            if (_bossStageManager != null)
            {
                _bossStageManager.BossStageCleared -=
                    HandleBossStageCleared;

                _bossStageManager.BossStageFailed -=
                    HandleScenarioFailed;
            }
        }

        private void HandleContentStarted(
            PlayerProgress progress)
        {
            Debug.Log(
                $"[ProgressScenarioManager] ContentStarted 수신: {progress}",
                this);

            if (_isScenarioRunning)
            {
                Debug.LogWarning(
                    "[ProgressScenarioManager] " +
                    "이미 시나리오가 실행 중입니다.",
                    this);

                return;
            }

            _currentProgress = progress;
            _isScenarioRunning = true;

            StartScenario(progress);
        }

        private void StartScenario(
            PlayerProgress progress)
        {
            switch (progress)
            {
                case PlayerProgress.OpeningCutScene:
                    PlayCutScene(_openingCutScene);
                    break;

                case PlayerProgress.BasicTutorial:
                    StartBasicTutorialScenario();
                    break;

                case PlayerProgress.Stage01:
                    StartStage01Scenario();
                    break;

                case PlayerProgress.CutScene01:
                    PlaySuccessCutScene(
                        _stage01SuccessCutScene);
                    break;

                case PlayerProgress.BossStage01:
                    StartBossStage01Scenario();
                    break;

                case PlayerProgress.Stage02:
                    StartStage02Scenario();
                    break;

                case PlayerProgress.CutScene02:
                    PlaySuccessCutScene(
                        _stage02SuccessCutScene);
                    break;

                case PlayerProgress.BossStage02:
                    StartBossStage02Scenario();
                    break;

                case PlayerProgress.EndingCutScene:
                    PlayCutScene(_endingCutScene);
                    break;

                case PlayerProgress.GameCompleted:
                    CompleteScenario();
                    break;
            }
        }

        private void StartBasicTutorialScenario()
        {
            Debug.Log(
                $"[ProgressScenarioManager] BasicTutorial 시나리오 시작, " +
                $"컷신 존재 여부: {_tutorialIntroCutScene != null}",
                this);

            if (_tutorialIntroCutScene != null)
            {
                PlayCutScene(_tutorialIntroCutScene);
                return;
            }

            StartTutorial();
        }

        private void StartStage01Scenario()
        {
            if (_stage01IntroCutScene != null)
            {
                PlayCutScene(_stage01IntroCutScene);
                return;
            }

            StartStage(1);
        }

        private void StartStage02Scenario()
        {
            if (_stage02IntroCutScene != null)
            {
                PlayCutScene(_stage02IntroCutScene);
                return;
            }

            StartStage(2);
        }

        private void StartBossStage01Scenario()
        {
            if (_bossStage01IntroCutScene != null)
            {
                PlayCutScene(_bossStage01IntroCutScene);
                return;
            }

            StartBossStage(1);
        }

        private void StartBossStage02Scenario()
        {
            if (_bossStage02IntroCutScene != null)
            {
                PlayCutScene(_bossStage02IntroCutScene);
                return;
            }

            StartBossStage(2);
        }

        private void PlayCutScene(
            CutSceneData cutSceneData)
        {
            if (cutSceneData == null)
            {
                Debug.LogError(
                    $"[ProgressScenarioManager] " +
                    $"{_currentProgress}의 컷신 데이터가 없습니다.",
                    this);

                FailScenario();
                return;
            }

            _currentStep = ScenarioStep.CutScene;

            _cutSceneManager.Play(cutSceneData);
        }

        private void PlaySuccessCutScene(
            CutSceneData cutSceneData)
        {
            if (cutSceneData == null)
            {
                Debug.LogError(
                    $"[ProgressScenarioManager] " +
                    $"{_currentProgress}의 성공 컷씬 데이터가 없습니다.",
                    this);

                FailScenario();
                return;
            }

            _currentStep =
                ScenarioStep.SuccessCutScene;

            _cutSceneManager.Play(cutSceneData);
        }

        private void StartTutorial()
        {
            _currentStep = ScenarioStep.Tutorial;

            _tutorialManager.StartTutorial();
        }

        private void StartStage(int stageIndex)
        {
            _currentStep = ScenarioStep.Stage;

            _stageManager.StartStage(stageIndex);
        }

        private void StartBossStage(int bossStageIndex)
        {
            _currentStep = ScenarioStep.BossStage;

            _bossStageManager.StartBossStage(
                bossStageIndex);
        }

        private void HandleCutSceneFinished()
        {
            if (!_isScenarioRunning)
                return;

            /*
             * 실패 컷씬 종료
             */
            if (_currentStep ==
                ScenarioStep.FailureCutScene)
            {
                RestartFailedScenario();
                return;
            }

            /*
             * 성공 컷씬 종료
             */
            if (_currentStep ==
                ScenarioStep.SuccessCutScene)
            {
                HandleSuccessCutSceneFinished();
                return;
            }

            if (_currentStep !=
                ScenarioStep.CutScene)
            {
                return;
            }

            switch (_currentProgress)
            {
                case PlayerProgress.OpeningCutScene:
                case PlayerProgress.EndingCutScene:
                    CompleteScenario();
                    break;

                case PlayerProgress.BasicTutorial:
                    StartTutorial();
                    break;

                case PlayerProgress.Stage01:
                    StartStage(1);
                    break;

                case PlayerProgress.Stage02:
                    StartStage(2);
                    break;

                case PlayerProgress.BossStage01:
                    StartBossStage(1);
                    break;

                case PlayerProgress.BossStage02:
                    StartBossStage(2);
                    break;
            }
        }

        private void HandleSuccessCutSceneFinished()
        {
            if (!_isScenarioRunning ||
                _currentStep !=
                ScenarioStep.SuccessCutScene)
            {
                return;
            }

            PlayerProgress finishedProgress =
                _currentProgress;

            PlayerProgress nextProgress =
                GetProgressAfterSuccessCutScene(
                finishedProgress);

            ClearScenario();

            Debug.Log(
                $"[ProgressScenarioManager] " +
                $"성공 컷씬 종료: {finishedProgress}. " +
                $"진행도를 변경하지 않고 메인 화면으로 돌아갑니다.",
                this);

            _gameFlowManager.ReturnToMainMenu(
                nextProgress);
        }

        private static PlayerProgress GetProgressAfterSuccessCutScene(
            PlayerProgress progress)
        {
            return progress switch
            {
                PlayerProgress.CutScene01 =>
                    PlayerProgress.Stage01,

                PlayerProgress.CutScene02 =>
                    PlayerProgress.Stage02,

                _ => throw new
                    System.ArgumentOutOfRangeException(
                        nameof(progress),
                        progress,
                        "성공 컷씬에 연결된 다음 진행도가 없습니다.")
            };
        }


        private void HandleTutorialCompleted()
        {
            if (!_isScenarioRunning ||
                _currentStep != ScenarioStep.Tutorial)
            {
                return;
            }

            CompleteScenario();
        }

        private void HandleStageCleared()
        {
            if (!_isScenarioRunning ||
                _currentStep != ScenarioStep.Stage)
            {
                return;
            }

            CompleteScenario();
        }

        private void HandleBossStageCleared()
        {
            if (!_isScenarioRunning ||
                _currentStep != ScenarioStep.BossStage)
            {
                return;
            }

            CompleteScenario();
        }

        private void HandleScenarioFailed()
        {
            FailScenario();
        }

        private void HandleContentFailed(
            PlayerProgress failedProgress)
        {
            /*
             * GameFlow는 실패로 종료됐지만,
             * 실패 컷씬은 ProgressScenarioManager가
             * 별도로 실행합니다.
             */
            CutSceneData failCutScene =
                GetFailureCutScene(
                    failedProgress
                );

            if (failCutScene == null)
            {
                Debug.LogWarning(
                    $"[ProgressScenarioManager] " +
                    $"{failedProgress}의 실패 컷씬이 없습니다. " +
                    $"즉시 현재 Flow를 재시작합니다.",
                    this
                );

                RestartFailedScenario();
                return;
            }

            _failedProgress =
                failedProgress;

            _isScenarioRunning = true;
            _currentProgress = failedProgress;
            _currentStep =
                ScenarioStep.FailureCutScene;

            Debug.Log(
                $"[ProgressScenarioManager] " +
                $"실패 컷씬 시작: {failedProgress}",
                this
            );

            _cutSceneManager.Play(
                failCutScene
            );
        }

        private void RestartFailedScenario()
        {
            PlayerProgress retryProgress =
                _failedProgress;

            ClearScenario();

            _failedProgress = default;

            Debug.Log(
                $"[ProgressScenarioManager] " +
                $"실패 컷씬 종료. " +
                $"{retryProgress}를 처음부터 다시 시작합니다.",
                this
            );

            /*
             * PlayerProgress는 실패 시 변경되지 않았으므로
             * 동일한 Stage01 또는 Stage02가 다시 시작됩니다.
             *
             * ProgressScenarioManager의 StartScenario()가
             * 다시 호출되므로 스테이지 시작 컷씬부터 재생됩니다.
             */
            _gameFlowManager.RestartCurrentFlow();
        }

        private CutSceneData GetFailureCutScene(
            PlayerProgress progress)
        {
            return progress switch
            {
                PlayerProgress.Stage01 =>
                    _stage01FailCutScene,

                PlayerProgress.Stage02 =>
                    _stage02FailCutScene,

                PlayerProgress.BossStage01 =>
                    _stage01FailCutScene,

                PlayerProgress.BossStage02 =>
                    _stage02FailCutScene,

                _ => null
            };
        }

        private void CompleteScenario()
        {
            if (!_isScenarioRunning)
            {
                return;
            }

            PlayerProgress completedProgress =
                _currentProgress;

            ClearScenario();

            Debug.Log(
                $"[ProgressScenarioManager] " +
                $"시나리오 완료: {completedProgress}",
                this);

            _gameFlowManager.CompleteCurrentFlow();
        }

        private void FailScenario()
        {
            if (!_isScenarioRunning)
                return;

            _failedProgress =
                _currentProgress;

            PlayerProgress failedProgress =
                _currentProgress;

            ClearScenario();

            Debug.LogWarning(
                $"[ProgressScenarioManager] " +
                $"시나리오 실패: {failedProgress}",
                this
            );

            /*
             * PlayerProgress는 변경되지 않고
             * 현재 GameFlow만 종료됩니다.
             *
             * 이후 ContentFailed 이벤트에서
             * 실패 컷씬을 재생합니다.
             */
            _gameFlowManager.FailCurrentFlow();
        }

        private void ClearScenario()
        {
            _isScenarioRunning = false;
            _currentProgress = default;
            _currentStep = ScenarioStep.None;
        }
    }
}