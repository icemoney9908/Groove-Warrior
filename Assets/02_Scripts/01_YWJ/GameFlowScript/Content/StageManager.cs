using System;
using KKH.ChartData;
using KKH.Gameplay;
using UnityEngine;
using YWJ.Background;
using YWJ.GameLogic;
using YWJ.Player;

namespace YWJ.GameFlow.Content
{
    public class StageManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private MusicConductor _musicConductor;

        [SerializeField]
        private InfiniteGroundScroller _infiniteGroundScroller;

        [SerializeField]
        private InfiniteBackgroundScroller _infiniteBackgroundScroller;

        [SerializeField]
        private PlayerAnimationHandler _playerAnimationHandler;

        [SerializeField]
        private TabSuccessChecker _tabSuccessChecker;

        [Header("Note Hit Lane")]
        [SerializeField] GameObject _noteHitLane;

        [Header("Stage Chart Data")]
        [SerializeField] private SongChartData[] _stageChartDataArray;

        private int _currentStageIndex;
        private bool _isStageRunning;

        public int CurrentStageIndex =>
            _currentStageIndex;

        public bool IsStageRunning =>
            _isStageRunning;

        public event Action<int> StageStarted;
        public event Action StageCleared;
        public event Action StageFailed;

        private void Awake()
        {
            if (_musicConductor == null)
            {
                _musicConductor =
                    FindFirstObjectByType<MusicConductor>();
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

            if (_playerAnimationHandler == null)
            {
                Debug.LogWarning(
                    "[StageManager] PlayerAnimationHandler가 설정되지 않았습니다. " +
                    "애니메이션 관련 기능이 작동하지 않을 수 있습니다.",
                    this);
            }

            if ( _tabSuccessChecker == null)
            {
                _tabSuccessChecker = FindFirstObjectByType<TabSuccessChecker>();
            }
        }

        private void OnEnable()
        {
            if (_musicConductor != null)
            {
                _musicConductor.PlaybackEnded += HandlePlaybackEnded;
            }
        }

        private void OnDisable()
        {
            if (_musicConductor != null)
            {
                _musicConductor.PlaybackEnded -= HandlePlaybackEnded;
            }
        }

        /// <summary>
        /// 지정한 일반 스테이지를 시작합니다.
        /// 현재는 MusicConductor.Play()만 실행합니다.
        /// </summary>
        public void StartStage(int stageIndex)
        {
            if (_isStageRunning)
            {
                Debug.LogWarning(
                    $"[StageManager] 이미 스테이지가 실행 중입니다: " +
                    $"{_currentStageIndex}",
                    this);

                return;
            }

            if (_musicConductor == null)
            {
                Debug.LogError(
                    "[StageManager] MusicConductor가 없습니다.",
                    this);

                StageFailed?.Invoke();
                return;
            }

            if (stageIndex <= 0)
            {
                Debug.LogError(
                    $"[StageManager] 올바르지 않은 스테이지 번호입니다: " +
                    $"{stageIndex}",
                    this);

                StageFailed?.Invoke();
                return;
            }

            _currentStageIndex = stageIndex;
            _isStageRunning = true;

            Debug.Log(
                $"[StageManager] Stage {stageIndex} 시작",
                this);

            StageStarted?.Invoke(stageIndex);

            _noteHitLane.SetActive(true);
            _infiniteGroundScroller.SetIsMoving(true);
            _infiniteBackgroundScroller.StartScrolling();
            _playerAnimationHandler.EnableAnimation();
            _musicConductor.Play(_stageChartDataArray[stageIndex - 1]);
            _tabSuccessChecker?.InitTabNotes_ForChartChange();
        }

        /// <summary>
        /// 현재 스테이지를 정상 클리어 처리합니다.
        /// 음악 종료나 판정 완료 지점에서 호출합니다.
        /// </summary>
        public void ClearCurrentStage()
        {
            if (!_isStageRunning)
            {
                Debug.LogWarning(
                    "[StageManager] 실행 중인 스테이지가 없습니다.",
                    this);

                return;
            }

            int clearedStageIndex =
                _currentStageIndex;

            ClearCurrentStageState();

            Debug.Log(
                $"[StageManager] Stage {clearedStageIndex} 클리어",
                this);

            StageCleared?.Invoke();
        }


        private void HandlePlaybackEnded()
        {
            if (!_isStageRunning)
                return;

            ClearCurrentStage();
        }


        /// <summary>
        /// 현재 스테이지를 실패 처리합니다.
        /// </summary>
        public void FailCurrentStage()
        {
            if (!_isStageRunning)
            {
                Debug.LogWarning(
                    "[StageManager] 실행 중인 스테이지가 없습니다.",
                    this
                );

                return;
            }

            int failedStageIndex =
                _currentStageIndex;

            /*
             * 실패 컷씬으로 넘어가기 전에
             * 현재 음악과 노트 진행을 정지합니다.
             */
            if (_musicConductor != null)
            {
                _musicConductor.Stop();
            }

            ClearCurrentStageState();

            Debug.LogWarning(
                $"[StageManager] " +
                $"Stage {failedStageIndex} 실패",
                this
            );

            StageFailed?.Invoke();
        }

        /// <summary>
        /// 현재 스테이지를 중단합니다.
        /// 클리어/실패 이벤트는 발생하지 않습니다.
        /// </summary>
        public void StopCurrentStage()
        {
            if (!_isStageRunning)
            {
                return;
            }

            Debug.Log(
                $"[StageManager] Stage {_currentStageIndex} 중단",
                this);

            /*
             * MusicConductor에 Stop()이 만들어지면 호출합니다.
             *
             * _musicConductor.Stop();
             */

            ClearCurrentStageState();
        }

        /// <summary>
        /// 현재 스테이지를 처음부터 다시 시작합니다.
        /// </summary>
        public void RestartCurrentStage()
        {
            if (!_isStageRunning)
            {
                Debug.LogWarning(
                    "[StageManager] 재시작할 스테이지가 없습니다.",
                    this);

                return;
            }

            int stageIndex =
                _currentStageIndex;

            StopCurrentStage();
            StartStage(stageIndex);
        }

        private void ClearCurrentStageState()
        {
            _noteHitLane.SetActive(false);
            _infiniteGroundScroller.SetIsMoving(false);
            _infiniteBackgroundScroller.StopScrolling();
            _playerAnimationHandler.DisableAnimation();
            _isStageRunning = false;
            _currentStageIndex = 0;
        }

#if UNITY_EDITOR
        [ContextMenu("Test Start Stage 01")]
        private void TestStartStage01()
        {
            StartStage(1);
        }

        [ContextMenu("Test Start Stage 02")]
        private void TestStartStage02()
        {
            StartStage(2);
        }

        [ContextMenu("Test Clear Current Stage")]
        private void TestClearCurrentStage()
        {
            _musicConductor.Stop();
            ClearCurrentStage();
        }

        [ContextMenu("Test Fail Current Stage")]
        private void TestFailCurrentStage()
        {
            _musicConductor.Stop();
            FailCurrentStage();
        }
#endif
    }
}