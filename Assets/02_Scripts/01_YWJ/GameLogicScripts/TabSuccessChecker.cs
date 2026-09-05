using System;
using System.Collections.Generic;
using GrooveWarrior.ChartData;
using KKH.ChartData;
using KKH.Gameplay;
using UnityEngine;
using YWJ.GameLogic.Effect;
using YWJ.GameLogic.Judgement;
using YWJ.GameStateMachine;

namespace YWJ.GameLogic
{
    public class TabSuccessChecker : MonoBehaviour
    {
        [Header("Current Generation")]
        [SerializeField]
        private int _currentGeneration = 0;

        [SerializeField]
        private int _lastGeneration = 0;

        [Header("Music Conductor")]
        [SerializeField]
        private MusicConductor _musicConductor;

        [SerializeField]
        private SongChartData _songChartData;

        [SerializeField]
        private IReadOnlyList<NoteData> _tabNotes =
            new List<NoteData>();

        [Header("Current Note")]
        [SerializeField]
        private NoteData _currentNote;

        [SerializeField]
        private int _currentNoteIndex;

        [Header("Judgement Setting")]
        [SerializeField]
        private JudgementSetting _judgementSetting;

        /*
         * 점수 및 게임 진행 처리용 이벤트입니다.
         */
        public event Action TabSuccess;
        public event Action TabFail;

        /// <summary>
        /// 성공한 노트를 기준으로 탭 효과를 요청합니다.
        /// </summary>
        public event Action<TabEffectType>
            TabEffectRequested;

        /// <summary>
        /// 성공한 노트를 기준으로 플레이어 애니메이션을 요청합니다.
        /// </summary>
        public event Action<RhythmActorKind, TabEffectType>
            TabAnimationRequested;

        private double _inputTimingSongSeconds;
        private double _holdingTimingSongSeconds;

        private double _tickInterval;
        private double _nextTickTime;

        /*
         * 플레이어가 현재 Tab 입력을
         * 누르고 있는지 나타냅니다.
         */
        private bool _isHoldingTab;

        /*
         * 현재 HOLD가 정상적으로 시작되었고
         * 성공 상태를 유지하고 있는지 나타냅니다.
         */
        private bool _successHoldingTab;

        /*
         * 현재 HOLD 노트가 진행 중인지 나타냅니다.
         */
        private bool _isHoldTabActive;

        /*
         * 진행 중인 HOLD 노트의 인덱스입니다.
         */
        private int _activeHoldNoteIndex = -1;

        private TabJudgementSetting _tabJudgement;

        private HoldTabJudgementSetting _holdTabJudgement;

        private void Awake()
        {
            InitializeReferences();
            InitializeJudgementSettings();
        }

        private void OnEnable()
        {
            GameStateEventBus
                .OnMusicModeStateEntered +=
                InitTabNotes;
        }

        private void OnDisable()
        {
            GameStateEventBus
                .OnMusicModeStateEntered -=
                InitTabNotes;
        }

        private void Update()
        {
            if (!CanUpdateJudgement())
                return;

            /*
             * 진행 중인 HOLD가 외부 모듈에 의해
             * 제거되었는지 먼저 검사합니다.
             */
            if (UpdateExternallyIgnoredHold())
            {
                /*
                 * HOLD를 정리하면서 다음 노트로
                 * 이동했으므로 현재 노트 상태도 검사합니다.
                 */
                UpdateCurrentNoteState();
            }

            /*
             * Waiting, Judgeable, Ignored 상태에 따라
             * 현재 노트를 정리합니다.
             */
            UpdateCurrentNoteState();

            /*
             * HOLD의 Tick을 먼저 처리한 뒤
             * 종료 여부를 검사합니다.
             *
             * 현재 프레임에 마지막 유효 Tick이 있다면
             * 완료 전에 반영하기 위함입니다.
             */
            UpdateHoldTabByInterval();
            UpdateHoldTabExpiration();
        }

        // ========================================
        // Initialization
        // ========================================

        private void InitializeReferences()
        {
            if (_musicConductor == null)
            {
                _musicConductor =
                    FindFirstObjectByType<
                        MusicConductor>();
            }

            if (_musicConductor == null)
            {
                Debug.LogError(
                    "MusicConductor is not assigned " +
                    "in TabSuccessChecker.",
                    this
                );
            }
        }

        private void InitializeJudgementSettings()
        {
            if (_judgementSetting == null)
            {
                Debug.LogError(
                    "JudgementSetting is not assigned " +
                    "in TabSuccessChecker.",
                    this
                );

                return;
            }

            _tabJudgement =
                _judgementSetting.Tab;

            _holdTabJudgement =
                _judgementSetting.HoldTab;

            if (_tabJudgement == null)
            {
                Debug.LogError(
                    "TabJudgementSetting is null.",
                    this
                );
            }

            if (_holdTabJudgement == null)
            {
                Debug.LogError(
                    "HoldTabJudgementSetting is null.",
                    this
                );

                return;
            }

            _tickInterval =
                _holdTabJudgement.TickInterval;
        }

        private bool CanUpdateJudgement()
        {
            return _musicConductor != null
                   && _tabJudgement != null
                   && _holdTabJudgement != null;
        }

        // ========================================
        // TAP
        // ========================================

        public void CheckTabSuccess()
        {
            if (!CanUpdateJudgement())
                return;

            /*
             * Input System 콜백이 Update보다 먼저
             * 실행되는 경우를 대비합니다.
             *
             * 앞에 있는 Ignored 노트를 같은 프레임에
             * 모두 건너뜁니다.
             */
            UpdateCurrentNoteState();

            if (!CanProcessCurrentNote(
                    NoteType.TAP))
            {
                return;
            }

            _inputTimingSongSeconds =
                GetCurrentSongTime();

            double timingDifference =
                _inputTimingSongSeconds
                - _currentNote.HitTimeSeconds;

            /*
             * MissWindow보다 더 이른 입력은
             * 현재 노트를 소모하지 않습니다.
             */
            if (timingDifference <
                -_tabJudgement.MissWindow)
            {
                return;
            }

            /*
             * MissWindow 안에는 들어왔지만
             * PerfectWindow보다 이른 입력입니다.
             */
            if (timingDifference <
                -_tabJudgement.PerfectWindow)
            {
                InvokeTabFail(
                    TabEffectType.TapFail
                );

                ConsumeCurrentNote();
                UpdateCurrentNoteState();

                return;
            }

            bool isSuccess =
                _tabJudgement.Validate(
                    timingDifference
                );

            if (!isSuccess)
            {
                /*
                 * 늦은 입력은 여기서 바로 실패시키지 않고
                 * UpdateCurrentNoteState()의 만료 검사에서
                 * 처리합니다.
                 */
                return;
            }

            InvokeTabSuccess(
                TabEffectType.TapSuccess
            );

            ConsumeCurrentNote();
            UpdateCurrentNoteState();
        }

        // ========================================
        // HOLD Start
        // ========================================

        public void CheckStartHoldTabSuccess()
        {
            if (!CanUpdateJudgement())
                return;

            /*
             * 앞에 있는 Ignored 노트를 먼저 넘깁니다.
             */
            UpdateCurrentNoteState();

            if (!CanProcessCurrentNote(
                    NoteType.HOLD))
            {
                return;
            }

            if (_isHoldTabActive)
                return;

            double currentTime =
                GetCurrentSongTime();

            double timingDifference =
                currentTime
                - _currentNote.HitTimeSeconds;

            double startWindow =
                _holdTabJudgement.StartWindow;

            double missWindow =
                _holdTabJudgement.MissWindow;

            /*
             * 너무 이른 입력은 무시합니다.
             */
            if (timingDifference < -missWindow)
                return;

            /*
             * HOLD 시작 이른 Miss입니다.
             */
            if (timingDifference < -startWindow)
            {
                InvokeTabFail(
                    TabEffectType.HoldFail
                );

                ConsumeCurrentNote();
                UpdateCurrentNoteState();

                return;
            }

            bool isStartSuccess =
                _holdTabJudgement.ValidateStart(
                    currentTime,
                    _currentNote.HitTimeSeconds
                );

            if (!isStartSuccess)
            {
                /*
                 * 늦은 시작은 현재 노트 만료 처리에서
                 * 실패로 판정합니다.
                 */
                return;
            }

            StartCurrentHoldTab(
                currentTime
            );

            InvokeTabSuccess(
                TabEffectType.HoldStart
            );
        }

        private void StartCurrentHoldTab(
            double currentTime)
        {
            _isHoldTabActive = true;
            _successHoldingTab = true;

            _activeHoldNoteIndex =
                _currentNoteIndex;

            _nextTickTime =
                currentTime + _tickInterval;
        }

        // ========================================
        // HOLD Tick
        // ========================================

        private void UpdateHoldTabByInterval()
        {
            if (!_isHoldTabActive)
                return;

            if (!_isHoldingTab)
                return;

            if (!_successHoldingTab)
                return;

            if (!CanProcessCurrentNote(
                    NoteType.HOLD))
            {
                return;
            }

            /*
             * 외부 모듈에서 화면 노트를 제거해
             * 현재 HOLD를 Ignored로 바꾼 경우에는
             * 더 이상 Tick을 발생시키지 않습니다.
             */
            if (!_currentNote.IsActive)
                return;

            if (_tickInterval <= 0.0)
                return;

            double currentTime =
                GetCurrentSongTime();

            /*
             * 프레임 드롭으로 여러 Tick이 지나간 경우에도
             * 유효한 Tick을 모두 처리합니다.
             */
            while (currentTime >= _nextTickTime)
            {
                /*
                 * HOLD의 EndTime 이후 Tick은
                 * 처리하지 않습니다.
                 */
                if (_nextTickTime >=
                    _currentNote.EndTimeSeconds)
                {
                    break;
                }

                /*
                 * 마지막 불완전 Tick은 처리하지 않습니다.
                 */
                if (_nextTickTime + _tickInterval >
                    _currentNote.EndTimeSeconds)
                {
                    break;
                }

                CheckKeepHoldTabSuccess(
                    _nextTickTime
                );

                _nextTickTime +=
                    _tickInterval;
            }
        }

        private void CheckKeepHoldTabSuccess(
            double tickTime)
        {
            if (!CanProcessCurrentNote(
                    NoteType.HOLD))
            {
                return;
            }

            if (!_currentNote.IsActive)
                return;

            bool isTickSuccess =
                _holdTabJudgement.ValidateTick(
                    _isHoldingTab,
                    _successHoldingTab,
                    tickTime,
                    _currentNote.EndTimeSeconds
                );

            if (!isTickSuccess)
                return;

            InvokeTabSuccess(
                TabEffectType.HoldTick
            );
        }

        // ========================================
        // HOLD Release
        // ========================================

        public void CheckCancelHoldTabSuccess()
        {
            if (!CanUpdateJudgement())
                return;

            _holdingTimingSongSeconds =
                GetCurrentSongTime();

            /*
             * 진행 중인 HOLD가 없다면
             * canceled 입력을 무시합니다.
             */
            if (!_isHoldTabActive)
                return;

            /*
             * 활성화 당시 노트와 현재 노트가 다르면
             * 잘못 남은 HOLD 상태만 정리합니다.
             */
            if (_activeHoldNoteIndex !=
                _currentNoteIndex)
            {
                ResetHoldTabState();
                return;
            }

            if (_currentNote == null)
            {
                ResetHoldTabState();
                return;
            }

            /*
             * 다른 모듈의 Miss로 화면 노트가 제거되어
             * Ignored가 된 경우에는 추가 실패 판정을
             * 발생시키지 않습니다.
             */
            if (_currentNote.JudgementState ==
                NoteJudgementState.Ignored)
            {
                CompleteIgnoredHoldTab();
                return;
            }

            if (!CanProcessCurrentNote(
                    NoteType.HOLD))
            {
                ResetHoldTabState();
                return;
            }

            bool isReleaseSuccess =
                _successHoldingTab
                && _holdTabJudgement
                    .ValidateRelease(
                        _holdingTimingSongSeconds,
                        _currentNote.EndTimeSeconds
                    );

            if (isReleaseSuccess)
            {
                InvokeTabSuccess(
                    TabEffectType.HoldComplete
                );
            }
            else
            {
                InvokeTabFail(
                    TabEffectType.HoldFail
                );
            }

            CompleteCurrentHoldTab();
        }

        // ========================================
        // Current Note State
        // ========================================

        private void UpdateCurrentNoteState()
        {
            /*
             * 진행 중인 HOLD는 일반 노트 처리 루프에서
             * 이동시키지 않습니다.
             */
            if (_isHoldTabActive)
                return;

            double currentTime =
                GetCurrentSongTime();

            /*
             * 같은 프레임에 Ignored 또는 만료 노트가
             * 여러 개 존재해도 모두 연속 처리합니다.
             */
            while (_currentNote != null)
            {
                switch (_currentNote.JudgementState)
                {
                    case NoteJudgementState.Waiting:
                        /*
                         * 아직 화면에 생성되지 않은 노트입니다.
                         *
                         * 다음 노트로 이동하지 않고
                         * 현재 노트를 유지합니다.
                         */
                        return;

                    case NoteJudgementState.Ignored:
                        /*
                         * 외부 모듈에서 제거되었거나
                         * 이미 소비된 노트입니다.
                         *
                         * 성공 또는 실패 판정 없이
                         * 다음 노트로 이동합니다.
                         */
                        MoveToNextNote();
                        continue;

                    case NoteJudgementState.Judgeable:
                        /*
                         * 정상적인 입력 및 만료 판정
                         * 대상입니다.
                         */
                        break;

                    default:
                        Debug.LogWarning(
                            $"Unsupported judgement state. " +
                            $"Index={_currentNoteIndex}, " +
                            $"State=" +
                            $"{_currentNote.JudgementState}",
                            this
                        );

                        return;
                }

                double expireTime =
                    GetCurrentNoteExpireTime();

                /*
                 * 지원하지 않는 NoteType은
                 * 추가 판정 없이 소비합니다.
                 */
                if (double.IsPositiveInfinity(
                        expireTime))
                {
                    Debug.LogWarning(
                        $"Unsupported note ignored. " +
                        $"Index={_currentNoteIndex}, " +
                        $"Type={_currentNote.NoteType}",
                        this
                    );

                    ConsumeCurrentNote();
                    continue;
                }

                /*
                 * 아직 판정 가능 시간이 남아 있습니다.
                 */
                if (currentTime <= expireTime)
                    return;

                /*
                 * 화면에 존재하는 Judgeable 노트가
                 * 입력 없이 만료된 경우입니다.
                 */
                InvokeCurrentNoteExpiredFail();

                ConsumeCurrentNote();
            }
        }

        private double GetCurrentNoteExpireTime()
        {
            if (_currentNote == null)
            {
                return double.PositiveInfinity;
            }

            switch (_currentNote.NoteType)
            {
                case NoteType.TAP:
                    return _currentNote.HitTimeSeconds
                           + _tabJudgement
                               .PerfectWindow;

                case NoteType.HOLD:
                    return _currentNote.HitTimeSeconds
                           + _holdTabJudgement
                               .StartWindow;

                default:
                    return double.PositiveInfinity;
            }
        }

        private void InvokeCurrentNoteExpiredFail()
        {
            if (_currentNote == null)
                return;

            switch (_currentNote.NoteType)
            {
                case NoteType.TAP:
                    InvokeTabFail(
                        TabEffectType.TapFail
                    );
                    break;

                case NoteType.HOLD:
                    InvokeTabFail(
                        TabEffectType.HoldFail
                    );
                    break;
            }
        }

        // ========================================
        // Externally Ignored HOLD
        // ========================================

        private bool UpdateExternallyIgnoredHold()
        {
            if (!_isHoldTabActive)
                return false;

            if (_currentNote == null)
            {
                ResetHoldTabState();
                return false;
            }

            if (_activeHoldNoteIndex !=
                _currentNoteIndex)
            {
                ResetHoldTabState();
                return false;
            }

            if (_currentNote.JudgementState !=
                NoteJudgementState.Ignored)
            {
                return false;
            }

            /*
             * 다른 모듈의 Miss로 화면의 노트가
             * 모두 사라진 경우입니다.
             *
             * 이미 외부 모듈에서 실패를 처리했으므로
             * TabFail을 중복 호출하지 않습니다.
             */
            CompleteIgnoredHoldTab();

            return true;
        }

        private void CompleteIgnoredHoldTab()
        {
            ResetHoldTabState();
            MoveToNextNote();
        }

        // ========================================
        // HOLD Expiration
        // ========================================

        private void UpdateHoldTabExpiration()
        {
            if (!_isHoldTabActive)
                return;

            if (_activeHoldNoteIndex !=
                _currentNoteIndex)
            {
                ResetHoldTabState();
                return;
            }

            if (!CanProcessCurrentNote(
                    NoteType.HOLD))
            {
                ResetHoldTabState();
                return;
            }

            /*
             * 다른 모듈에 의해 제거된 HOLD라면
             * 여기서는 성공 또는 실패를 처리하지 않습니다.
             */
            if (!_currentNote.IsActive)
                return;

            double currentTime =
                GetCurrentSongTime();

            if (currentTime <
                _currentNote.EndTimeSeconds)
            {
                return;
            }

            /*
             * EndTime까지 정상적으로 누르고 있었다면
             * HOLD 완료 성공입니다.
             */
            bool isCompleteSuccess =
                _isHoldingTab
                && _successHoldingTab;

            if (isCompleteSuccess)
            {
                InvokeTabSuccess(
                    TabEffectType.HoldComplete
                );
            }
            else
            {
                InvokeTabFail(
                    TabEffectType.HoldFail
                );
            }

            CompleteCurrentHoldTab();
        }

        // ========================================
        // Event
        // ========================================

        private void InvokeTabSuccess(
            TabEffectType effectType)
        {
            TabSuccess?.Invoke();

            TabEffectRequested?.Invoke(
                effectType
            );

            RequestTabAnimation(effectType);
        }


        private void InvokeTabFail(
            TabEffectType effectType)
        {
            string noteInformation =
                _currentNote != null
                    ? $"Index={_currentNoteIndex}, " +
                      $"Type={_currentNote.NoteType}, " +
                      $"State=" +
                      $"{_currentNote.JudgementState}, " +
                      $"HitTime=" +
                      $"{_currentNote.HitTimeSeconds:F3}"
                    : "CurrentNote=null";

            Debug.LogWarning(
                $"Tab Fail: {effectType}, " +
                noteInformation,
                this
            );

            TabFail?.Invoke();

            TabEffectRequested?.Invoke(
                effectType
            );

            RequestTabAnimation(effectType);
        }


        private void RequestTabAnimation(
            TabEffectType effectType)
        {
            if (_currentNote == null)
                return;

            /*
             * HOLD Tick과 완료 시에는 새로운 입력 애니메이션을
             * 실행하지 않습니다.
             *
             * TAP 입력 성공과 HOLD 시작 입력 성공에만
             * 노트에 지정된 ActorKind를 전달합니다.
             */


            TabAnimationRequested?.Invoke(
                _currentNote.ActorKind,
                effectType
            );
        }


        // ========================================
        // Current Note
        // ========================================

        private bool CanProcessCurrentNote(
            NoteType expectedNoteType)
        {
            if (_currentNote == null)
                return false;

            /*
             * Judgeable 상태인 노트만 입력 판정합니다.
             *
             * Waiting 상태는 아직 생성 전이고,
             * Ignored 상태는 제거된 노트입니다.
             */
            if (!_currentNote.IsActive)
                return false;

            return _currentNote.NoteType ==
                   expectedNoteType;
        }

        private void ConsumeCurrentNote()
        {
            if (_currentNote == null)
                return;

            if (_currentGeneration != _lastGeneration)
            {
                _lastGeneration = _currentGeneration; // 세대가 바뀌었으므로 세대를 동기화하고, 1번 노트를 소비하지 않습니다.
                return;
            }

            /*
             * 성공, 실패 또는 만료로 소비된 노트는
             * 더 이상 판정되지 않도록 Ignored로 변경합니다.
             */
            _currentNote.SetJudgementState(
                NoteJudgementState.Ignored
            );

            MoveToNextNote();
        }

        private void MoveToNextNote()
        {
            _currentNoteIndex++;

            _currentNote =
                _currentNoteIndex < _tabNotes.Count
                    ? _tabNotes[_currentNoteIndex]
                    : null;
        }

        // ========================================
        // HOLD State
        // ========================================

        private void CompleteCurrentHoldTab()
        {
            /*
             * Reset 전에 현재 HOLD 노트를
             * 소비 처리합니다.
             */
            if (_currentNote != null)
            {
                _currentNote.SetJudgementState(
                    NoteJudgementState.Ignored
                );
            }

            ResetHoldTabState();
            MoveToNextNote();

            /*
             * 같은 프레임 안에 뒤따르는 Ignored 노트도
             * 즉시 정리합니다.
             */
            UpdateCurrentNoteState();
        }

        private void ResetHoldTabState()
        {
            _isHoldTabActive = false;
            _successHoldingTab = false;

            _activeHoldNoteIndex = -1;

            _nextTickTime = 0.0;
        }

        public void EnableHoldingTab()
        {
            _isHoldingTab = true;
        }

        public void DisableHoldingTab()
        {
            _isHoldingTab = false;
        }

        // ========================================
        // Chart Initialization
        // ========================================

        private void InitTabNotes()
        {
            if (_musicConductor == null)
            {
                Debug.LogError(
                    "MusicConductor is not assigned " +
                    "in TabSuccessChecker.",
                    this
                );

                return;
            }

            _songChartData =
                _musicConductor.CurrentChartData;

            if (_songChartData == null)
            {
                Debug.LogError(
                    "Current SongChartData is null " +
                    "in TabSuccessChecker.",
                    this
                );

                return;
            }

            _tabNotes =
                _songChartData.Notes
                ?? Array.Empty<NoteData>();

            /*
             * 이전 플레이의 런타임 상태가 남는 상황을
             * 방지하기 위해 모든 노트를 Waiting으로
             * 초기화합니다.
             *
             * 이후 시각 노트 생성 모듈이 화면에 생성한
             * 노트만 Judgeable로 변경해야 합니다.
             */
            for (int i = 0;
                 i < _tabNotes.Count;
                 i++)
            {
                NoteData note =
                    _tabNotes[i];

                note?.SetJudgementState(
                    NoteJudgementState.Waiting
                );
            }

            _currentNoteIndex = 0;

            _currentNote =
                _tabNotes.Count > 0
                    ? _tabNotes[0]
                    : null;

            _isHoldingTab = false;

            ResetHoldTabState();

            Debug.Log(
                $"Tab notes initialized. " +
                $"Count={_tabNotes.Count}" +
                $"Generation={_currentGeneration}",
                this
            );
        }

        public void InitTabNotes_OnRestart()
        {
            Debug.Log(
                $"Tab notes re-initialized on restart. "
            );

            _currentGeneration++; // 재시작 시 세대 증가

            InitTabNotes();
        }

        public void InitTabNotes_ForChartChange()
        {
            InitTabNotes();
        }

        // ========================================
        // Utility
        // ========================================

        private double GetCurrentSongTime()
        {
            if (_musicConductor == null)
            {
                return Time.unscaledTimeAsDouble;
            }

            return _musicConductor
                .CurrentSongTimeSecondsExact;
        }
    }
}