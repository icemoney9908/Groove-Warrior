using System;
using System.Collections;
using KKH.Gameplay;
using UnityEngine;
using YWJ.UI;

namespace YWJ.GameStateMachine
{
    public class GameStateController : MonoBehaviour
    {
        [Header("Current GameState")]
        [SerializeField]
        private GameState _currentState = GameState.MAIN_MENU;

        [Header("References")]
        [SerializeField]
        private MusicConductor _musicConductor;

        [SerializeField]
        private RuntimeUIController _runtimeUIController;

        private Root_GSM _currentGSM;

        private MainMenu_GSM _mainMenuGSM;
        private StoryMode_GSM _storyModeGSM;
        private MusicMode_GSM _musicModeGSM;
        private BossMode_GSM _bossModeGSM;
        private GameClear_GSM _gameClearGSM;

        private bool _isChangingState;

        public GameState CurrentState => _currentState;
        public bool IsChangingState => _isChangingState;

        public event Action<GameState> GameStateChanged;

        private void Awake()
        {
            FindReferences();
            InitGSM();
        }

        private IEnumerator Start()
        {
            yield return ChangeStateRoutine(
                _currentState,
                forceChange: true);
        }

        private void FindReferences()
        {
            if (_runtimeUIController == null)
            {
                _runtimeUIController =
                    FindFirstObjectByType<RuntimeUIController>();
            }

            if (_musicConductor == null)
            {
                _musicConductor =
                    FindFirstObjectByType<MusicConductor>();
            }
        }

        public IEnumerator ChangeStateRoutine(
            GameState nextState,
            bool forceChange = false)
        {
            if (_isChangingState)
            {
                Debug.LogWarning(
                    "[GameStateController] 이미 상태 전환 중입니다.",
                    this);

                yield break;
            }

            if (!forceChange && _currentState == nextState)
            {
                yield break;
            }

            _isChangingState = true;

            GameState previousState = _currentState;

            Debug.Log(
                $"[GameStateController] 상태 전환 시작: " +
                $"{previousState} → {nextState}",
                this);

            // 1. 기존 상태 종료
            _currentGSM?.ExitState();

            // 종료 과정에서 비활성화된 오브젝트와 이벤트가
            // 반영될 수 있도록 한 프레임 기다립니다.
            yield return null;

            // 2. 새 상태와 GSM 지정
            _currentState = nextState;
            _currentGSM = GetGSM(nextState);

            if (_currentGSM == null)
            {
                Debug.LogError(
                    $"[GameStateController] 대응하는 GSM이 없습니다: " +
                    $"{nextState}",
                    this);

                _isChangingState = false;
                yield break;
            }

            // 3. UI 변경
            if (_runtimeUIController != null)
            {
                _runtimeUIController.SetRuntimeUI(nextState);
            }

            // UI OnEnable과 레이아웃 변경 반영
            yield return null;

            Canvas.ForceUpdateCanvases();

            // 4. UI 준비 후 새 상태 진입
            _currentGSM.EnterState();

            _isChangingState = false;

            GameStateChanged?.Invoke(nextState);

            Debug.Log(
                $"[GameStateController] 상태 전환 완료: {nextState}",
                this);
        }

        private Root_GSM GetGSM(GameState state)
        {
            return state switch
            {
                GameState.MAIN_MENU => _mainMenuGSM,
                GameState.STORY_MODE => _storyModeGSM,
                GameState.MUSIC_MODE => _musicModeGSM,
                GameState.BOSS_MODE => _bossModeGSM,
                GameState.GAME_CLEAR => _gameClearGSM,
                _ => null
            };
        }

        private void InitGSM()
        {
            _mainMenuGSM = new MainMenu_GSM();
            _storyModeGSM = new StoryMode_GSM();
            _musicModeGSM = new MusicMode_GSM();
            _bossModeGSM = new BossMode_GSM();
            _gameClearGSM = new GameClear_GSM();
        }
    }
}