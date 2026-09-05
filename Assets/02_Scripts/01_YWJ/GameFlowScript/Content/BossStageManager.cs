using System;
using KKH.Gameplay;
using UnityEngine;

namespace YWJ.GameFlow.Content
{
    public class BossStageManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private MusicConductor _musicConductor;

        private int _currentBossStageIndex;
        private bool _isBossStageRunning;

        public int CurrentBossStageIndex =>
            _currentBossStageIndex;

        public bool IsBossStageRunning =>
            _isBossStageRunning;

        public event Action<int> BossStageStarted;
        public event Action BossStageCleared;
        public event Action BossStageFailed;

        private void Awake()
        {
            if (_musicConductor == null)
            {
                _musicConductor =
                    FindFirstObjectByType<MusicConductor>();
            }
        }

        /// <summary>
        /// 지정한 보스 스테이지를 시작합니다.
        /// 현재는 MusicConductor.Play()만 실행합니다.
        /// </summary>
        public void StartBossStage(int bossStageIndex)
        {
            if (_isBossStageRunning)
            {
                Debug.LogWarning(
                    $"[BossStageManager] 이미 보스 스테이지가 " +
                    $"실행 중입니다: {_currentBossStageIndex}",
                    this);

                return;
            }

            if (_musicConductor == null)
            {
                Debug.LogError(
                    "[BossStageManager] MusicConductor가 없습니다.",
                    this);

                BossStageFailed?.Invoke();
                return;
            }

            if (bossStageIndex <= 0)
            {
                Debug.LogError(
                    $"[BossStageManager] 올바르지 않은 " +
                    $"보스 스테이지 번호입니다: {bossStageIndex}",
                    this);

                BossStageFailed?.Invoke();
                return;
            }

            _currentBossStageIndex = bossStageIndex;
            _isBossStageRunning = true;

            Debug.Log(
                $"[BossStageManager] Boss Stage " +
                $"{bossStageIndex} 시작",
                this);

            BossStageStarted?.Invoke(
                bossStageIndex);

            _musicConductor.Play();
        }

        /// <summary>
        /// 현재 보스 스테이지를 클리어 처리합니다.
        /// </summary>
        public void ClearCurrentBossStage()
        {
            if (!_isBossStageRunning)
            {
                Debug.LogWarning(
                    "[BossStageManager] 실행 중인 " +
                    "보스 스테이지가 없습니다.",
                    this);

                return;
            }

            int clearedBossStageIndex =
                _currentBossStageIndex;

            ClearCurrentBossStageState();

            Debug.Log(
                $"[BossStageManager] Boss Stage " +
                $"{clearedBossStageIndex} 클리어",
                this);

            BossStageCleared?.Invoke();
        }

        /// <summary>
        /// 현재 보스 스테이지를 실패 처리합니다.
        /// </summary>
        public void FailCurrentBossStage()
        {
            if (!_isBossStageRunning)
            {
                Debug.LogWarning(
                    "[BossStageManager] 실행 중인 " +
                    "보스 스테이지가 없습니다.",
                    this);

                return;
            }

            int failedBossStageIndex =
                _currentBossStageIndex;

            ClearCurrentBossStageState();

            Debug.LogWarning(
                $"[BossStageManager] Boss Stage " +
                $"{failedBossStageIndex} 실패",
                this);

            BossStageFailed?.Invoke();
        }

        /// <summary>
        /// 현재 보스 스테이지를 중단합니다.
        /// 클리어/실패 이벤트는 발생하지 않습니다.
        /// </summary>
        public void StopCurrentBossStage()
        {
            if (!_isBossStageRunning)
            {
                return;
            }

            Debug.Log(
                $"[BossStageManager] Boss Stage " +
                $"{_currentBossStageIndex} 중단",
                this);

            /*
             * MusicConductor에 Stop()이 추가되면 호출합니다.
             *
             * _musicConductor.Stop();
             */

            ClearCurrentBossStageState();
        }

        /// <summary>
        /// 현재 보스 스테이지를 처음부터 다시 시작합니다.
        /// </summary>
        public void RestartCurrentBossStage()
        {
            if (!_isBossStageRunning)
            {
                Debug.LogWarning(
                    "[BossStageManager] 재시작할 " +
                    "보스 스테이지가 없습니다.",
                    this);

                return;
            }

            int bossStageIndex =
                _currentBossStageIndex;

            StopCurrentBossStage();
            StartBossStage(bossStageIndex);
        }

        private void ClearCurrentBossStageState()
        {
            _isBossStageRunning = false;
            _currentBossStageIndex = 0;
        }

#if UNITY_EDITOR
        [ContextMenu("Test Start Boss Stage 01")]
        private void TestStartBossStage01()
        {
            StartBossStage(1);
        }

        [ContextMenu("Test Start Boss Stage 02")]
        private void TestStartBossStage02()
        {
            StartBossStage(2);
        }

        [ContextMenu("Test Clear Current Boss Stage")]
        private void TestClearCurrentBossStage()
        {
            _musicConductor.Stop();
            ClearCurrentBossStage();
        }

        [ContextMenu("Test Fail Current Boss Stage")]
        private void TestFailCurrentBossStage()
        {
            _musicConductor.Stop();
            FailCurrentBossStage();
        }
#endif
    }
}