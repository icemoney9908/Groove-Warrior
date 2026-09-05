using GrooveWarrior.Gameplay;
using System;
using UnityEngine;
using YWJ.GameStateMachine;
using YWJ.GameFlow.Content;
using YWJ.Background;
using YWJ.Effect;

namespace YWJ.GameLogic
{
    public class GameScoreCalculator : MonoBehaviour
    {
        [Header("Combo")]
        [SerializeField]
        private int _combo;

        [Header("Score")]
        [SerializeField]
        private float _score;

        [SerializeField]
        private float _baseTabScore = 100f;

        [Header("Multiplier")]
        [SerializeField]
        private float _scoreMultiplier = 1f;

        [SerializeField]
        private int _successPoint;

        [Header("Failure Score")]
        [Tooltip("한 번 실패했을 때 기본적으로 추가되는 실패 점수입니다.")]
        [Min(0)]
        [SerializeField]
        private int _baseFailureScore = 10;

        [Tooltip("한 번 성공했을 때 기본적으로 차감되는 실패 점수입니다.")]
        [Min(0)]
        [SerializeField]
        private int _decreaseFailureScore = 10;

        [Tooltip("연속 실패 1회가 늘어날 때마다 추가되는 실패 점수 배율입니다.")]
        [Min(0f)]
        [SerializeField]
        private float _failureMultiplierStep = 1f;

        [Tooltip("연속 실패로 증가할 수 있는 최대 실패 점수 배율입니다.")]
        [Min(1f)]
        [SerializeField]
        private float _maximumFailureMultiplier = 5f;

        [Tooltip("누적 실패 점수가 이 값 이상이면 게임오버 조건이 충족됩니다.")]
        [Min(0)]
        [SerializeField]
        private int _gameOverFailureScore = 150;

        [SerializeField]
        [Min(0)]
        private float _failureScore;

        [SerializeField]
        private int _consecutiveFailCount;

        [SerializeField]
        private float _currentFailureMultiplier = 1f;

        private bool _hasReachedGameOverFailureScore;

        [Header("Music Tier")]
        [Range(0, 5)]
        [SerializeField]
        private int _musicTier = 0;

        [Header("Tier Threshold")]
        [Min(0)]
        [SerializeField]
        private int _tier2Point = 20;

        [Min(0)]
        [SerializeField]
        private int _tier3Point = 60;

        [Min(0)]
        [SerializeField]
        private int _tier4Point = 120;

        [Min(0)]
        [SerializeField]
        private int _tier5Point = 200;

        [Min(0)]
        [SerializeField]
        private int _tier6Point = 320;

        [Header("Success Point Settings")]
        [Min(0)]
        [SerializeField]
        private int _tabSuccessPoint = 5;

        [Min(1)]
        [SerializeField]
        private int _maxSuccessPoint = 400;

        [Header("Note Count")]
        [SerializeField]
        private int _tabPerfectCount;

        [SerializeField]
        private int _tabMissCount;

        [Header("Success Checker")]
        [SerializeField]
        private TabSuccessChecker _tabSuccessChecker;

        [Header("Music Tier State")]
        [SerializeField]
        private MusicTierState _musicTierState;

        [Header("Stage Manager")]
        [SerializeField]
        private StageManager _stageManager;

        [Header("Ground & Background")]
        [SerializeField]
        private InfiniteGroundScroller _groundScroller;

        [SerializeField]
        private InfiniteBackgroundScroller _backgroundScroller;

        [Header("Particle Effect Manager")]
        [SerializeField]
        private ParticleEffectManager _particleEffectManager;

        /// <summary>
        /// 실제 티어가 변경됐을 때만 호출됩니다.
        /// </summary>
        public event Action<int> MusicTierChanged;

        public int Combo => _combo;
        public float Score => _score;
        public float ScoreMultiplier => _scoreMultiplier;
        public int SuccessPoint => _successPoint;
        public int MusicTier => _musicTier;

        public float FailureScore => _failureScore;
        public int ConsecutiveFailCount => _consecutiveFailCount;
        public float CurrentFailureMultiplier => _currentFailureMultiplier;
        public bool HasReachedGameOverFailureScore =>
            _hasReachedGameOverFailureScore;

        private void Awake()
        {
            InitializeReferences();
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void InitializeReferences()
        {
            if (_tabSuccessChecker == null)
            {
                _tabSuccessChecker =
                    FindFirstObjectByType<TabSuccessChecker>();
            }

            if (_musicTierState == null)
            {
                _musicTierState =
                    FindFirstObjectByType<MusicTierState>();
            }

            if (_stageManager == null)
            {
                _stageManager =
                    FindFirstObjectByType<StageManager>();
            }   

            if (_groundScroller == null)
            {
                _groundScroller =
                    FindFirstObjectByType<InfiniteGroundScroller>();
            }

            if (_backgroundScroller == null)
            {
                _backgroundScroller =
                    FindFirstObjectByType<InfiniteBackgroundScroller>();
            }

            if (_particleEffectManager == null)
            {
                _particleEffectManager =
                    FindFirstObjectByType<ParticleEffectManager>();
            }
        }

        private void SubscribeEvents()
        {
            if (_tabSuccessChecker != null)
            {
                _tabSuccessChecker.TabSuccess +=
                    OnTabSuccess;

                _tabSuccessChecker.TabFail +=
                    OnTabFail;
            }

            if (_musicTierState != null)
            {
                MusicTierChanged +=
                    _musicTierState.SetTier;
            }

            GameStateEventBus.OnMusicModeStateEntered +=
                ResetCalculator;

            GameStateEventBus.OnMusicModeStateExited +=
                SaveScore;

            _stageManager.StageStarted += ResetCalculator_ForStageStart;
        }

        private void UnsubscribeEvents()
        {
            if (_tabSuccessChecker != null)
            {
                _tabSuccessChecker.TabSuccess -=
                    OnTabSuccess;

                _tabSuccessChecker.TabFail -=
                    OnTabFail;
            }

            if (_musicTierState != null)
            {
                MusicTierChanged -=
                    _musicTierState.SetTier;
            }

            GameStateEventBus.OnMusicModeStateEntered -=
                ResetCalculator;

            GameStateEventBus.OnMusicModeStateExited -=
                SaveScore;

            _stageManager.StageStarted -= ResetCalculator_ForStageStart;
        }

        private void OnTabSuccess()
        {
            _combo++;
            _tabPerfectCount++;

            _groundScroller.SetCombo(_combo);
            _backgroundScroller.SetCombo(_combo);
            _particleEffectManager.SetCombo(_combo);

            ResetConsecutiveFail();

            AddSuccessPoint(
                _tabSuccessPoint
            );

            SubtractFailurePoint(
                _decreaseFailureScore
            );

            _score +=
                _baseTabScore *
                _scoreMultiplier;
        }

        private void OnTabFail()
        {
            _combo = 0;
            _tabMissCount++;

            _groundScroller.SetCombo(_combo);
            _backgroundScroller.SetCombo(_combo);
            _particleEffectManager.SetCombo(_combo);

            AddFailureScore();

            ResetMultiplierAndTier();
        }

        private void AddFailureScore()
        {
            /*
             * 첫 실패의 연속 실패 횟수는 1입니다.
             *
             * 배율 계산:
             * 1회 연속 실패 = 1배
             * 2회 연속 실패 = 1 + Step
             * 3회 연속 실패 = 1 + Step * 2
             */
            _consecutiveFailCount++;

            _currentFailureMultiplier =
                CalculateFailureMultiplier(
                    _consecutiveFailCount
                );

            float addedFailureScore =
                _baseFailureScore *
                _currentFailureMultiplier;

            _failureScore +=
                addedFailureScore;

            Debug.LogWarning(
                $"Tab Miss: " +
                $"Consecutive Fail={_consecutiveFailCount}, " +
                $"Failure Multiplier=x{_currentFailureMultiplier:0.##}, " +
                $"Added Failure Score={addedFailureScore:0.##}, " +
                $"Total Failure Score=" +
                $"{_failureScore:0.##}/" +
                $"{_gameOverFailureScore:0.##}",
                this
            );

            CheckGameOverFailureScore();
        }

        private float CalculateFailureMultiplier(
            int consecutiveFailCount)
        {
            int additionalFailCount =
                Mathf.Max(
                    0,
                    consecutiveFailCount - 1
                );

            float multiplier =
                1f +
                additionalFailCount *
                _failureMultiplierStep;

            return Mathf.Clamp(
                multiplier,
                1f,
                _maximumFailureMultiplier
            );
        }

        private void ResetConsecutiveFail()
        {
            if (_consecutiveFailCount <= 0 &&
                Mathf.Approximately(
                    _currentFailureMultiplier,
                    1f))
            {
                return;
            }

            _consecutiveFailCount = 0;
            _currentFailureMultiplier = 1f;
        }

        private void CheckGameOverFailureScore()
        {
            if (_hasReachedGameOverFailureScore)
                return;

            if (_failureScore <
                _gameOverFailureScore)
            {
                return;
            }

            _hasReachedGameOverFailureScore = true;

            _stageManager.FailCurrentStage();

            Debug.LogError(
                $"GAME OVER CONDITION REACHED: " +
                $"Failure Score={_failureScore:0.##}, " +
                $"Threshold={_gameOverFailureScore:0.##}, " +
                $"Total Miss Count={_tabMissCount}",
                this
            );

            ResetCalculator(); // 게임오버 조건에 도달하면 점수 계산기를 초기화합니다.
            _tabSuccessChecker.InitTabNotes_OnRestart(); // 재시작 시 탭 노트들을 초기화합니다.
        }

        private void AddSuccessPoint(int amount)
        {
            if (amount <= 0)
                return;

            _successPoint = Mathf.Clamp(
                _successPoint + amount,
                0,
                _maxSuccessPoint
            );

            UpdateMultiplierAndTier();
        }

        private void SubtractFailurePoint(int amount)
        {
            if (amount <= 0)
                return;

            _failureScore = Mathf.Clamp(
                _failureScore - amount,
                0,
                _gameOverFailureScore * 2
            );
        }

        private void UpdateMultiplierAndTier()
        {
            int newTier;
            float newMultiplier;

            if (_successPoint >= _tier6Point)
            {
                newTier = 5;
                newMultiplier = 4f;
            }
            else if (_successPoint >= _tier5Point)
            {
                newTier = 4;
                newMultiplier = 3f;
            }
            else if (_successPoint >= _tier4Point)
            {
                newTier = 3;
                newMultiplier = 2f;
            }
            else if (_successPoint >= _tier3Point)
            {
                newTier = 2;
                newMultiplier = 1.5f;
            }
            else if (_successPoint >= _tier2Point)
            {
                newTier = 1;
                newMultiplier = 1.2f;
            }
            else
            {
                newTier = 0;
                newMultiplier = 1f;
            }

            _scoreMultiplier = newMultiplier;

            //SetMusicTier(newTier);
        }

        private void SetMusicTier(int newTier)
        {
            newTier = Mathf.Clamp(
                newTier,
                0,
                5
            );

            // 같은 티어라면 이벤트를 반복 호출하지 않습니다.
            if (_musicTier == newTier)
                return;

            _musicTier = newTier;

            MusicTierChanged?.Invoke(
                _musicTier
            );

            Debug.Log(
                $"Music tier changed: {_musicTier}",
                this
            );
        }

        private void ResetMultiplierAndTier()
        {
            _successPoint = 0;
            _scoreMultiplier = 1f;

            //SetMusicTier(0);
        }

        private void ResetCalculator()
        {
            _score = 0f;
            _combo = 0;

            _successPoint = 0;
            _scoreMultiplier = 1f;

            _failureScore = 0f;
            _consecutiveFailCount = 0;
            _currentFailureMultiplier = 1f;
            _hasReachedGameOverFailureScore = false;
            _groundScroller?.ResetGroundStage();
            _backgroundScroller?.ResetBackgroundStage();
            _particleEffectManager?.ResetEffects();

            _tabPerfectCount = 0;
            _tabMissCount = 0;

            /*
             * 현재 기획에서는 음악 티어를
             * 최고 티어인 5로 고정합니다.
             */
            SetMusicTier(5);

            Debug.Log(
                "GameScoreCalculator reset.",
                this
            );
        }

        private void ResetCalculator_ForStageStart(int num)
        {
            ResetCalculator();
        }

        private void SaveScore()
        {
            Debug.Log(
                $"Final Score: {_score}, " +
                $"Combo: {_combo}, " +
                $"Success Points: {_successPoint}, " +
                $"Multiplier: {_scoreMultiplier}, " +
                $"Music Tier: {_musicTier}, " +
                $"Failure Score: {_failureScore}, " +
                $"Consecutive Fail: {_consecutiveFailCount}, " +
                $"Failure Multiplier: " +
                $"x{_currentFailureMultiplier:0.##}, " +
                $"Game Over Condition: " +
                $"{_hasReachedGameOverFailureScore}",
                this
            );
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _tier2Point =
                Mathf.Max(
                    0,
                    _tier2Point
                );

            _tier3Point =
                Mathf.Max(
                    _tier2Point,
                    _tier3Point
                );

            _tier4Point =
                Mathf.Max(
                    _tier3Point,
                    _tier4Point
                );

            _tier5Point =
                Mathf.Max(
                    _tier4Point,
                    _tier5Point
                );

            _tier6Point =
                Mathf.Max(
                    _tier5Point,
                    _tier6Point
                );

            // 최고 티어에 도달할 수 있도록 보정합니다.
            _maxSuccessPoint =
                Mathf.Max(
                    _maxSuccessPoint,
                    _tier6Point
                );

            _baseFailureScore =
                Mathf.Max(
                    0,
                    _baseFailureScore
                );

            _failureMultiplierStep =
                Mathf.Max(
                    0f,
                    _failureMultiplierStep
                );

            _maximumFailureMultiplier =
                Mathf.Max(
                    1f,
                    _maximumFailureMultiplier
                );

            _gameOverFailureScore =
                Mathf.Max(
                    0,
                    _gameOverFailureScore
                );
        }
#endif
    }
}