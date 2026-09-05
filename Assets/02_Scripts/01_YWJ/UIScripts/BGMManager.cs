using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using YWJ.GameStateMachine;

namespace YWJ.Audio
{
    /// <summary>
    /// MAIN_MENU와 STORY_MODE에서만
    /// 일반 BGM을 재생합니다.
    ///
    /// MUSIC_MODE와 BOSS_MODE에서는
    /// MusicConductor의 게임 음악과 겹치지 않도록
    /// 일반 BGM을 정지합니다.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class BGMManager : MonoBehaviour
    {
        [Header("Reference")]
        [SerializeField]
        private GameStateController _gameStateController;

        [SerializeField]
        private AudioSource _audioSource;

        [Header("BGM Clips")]
        [SerializeField]
        private AudioClip _mainMenuBGM;

        [SerializeField]
        private AudioClip _storyModeBGM;

        [Header("Audio")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _volume = 1f;

        [Tooltip(
            "AudioMixer를 사용한다면 BGM Mixer Group을 연결합니다."
        )]
        [SerializeField]
        private AudioMixerGroup _outputMixerGroup;

        [Header("Fade")]
        [SerializeField]
        private bool _useFade = true;

        [Min(0f)]
        [SerializeField]
        private float _fadeOutDuration = 0.3f;

        [Min(0f)]
        [SerializeField]
        private float _fadeInDuration = 0.3f;

        [Header("Settings")]
        [Tooltip(
            "메인 메뉴와 스토리 모드에 같은 음악을 " +
            "사용할 때 상태가 변경되어도 처음부터 다시 재생하지 않습니다."
        )]
        [SerializeField]
        private bool _keepPlayingSameClip = true;

        [Header("Debug")]
        [SerializeField]
        private bool _showDebugLog;

        private Coroutine _changeRoutine;
        private bool _isSubscribed;

        private void Awake()
        {
            InitializeReferences();
            InitializeAudioSource();
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void Start()
        {
            /*
             * BGMManager가 GameStateController보다 늦게 활성화되거나
             * 초기 상태 변경 이벤트를 놓친 경우를 대비하여
             * 현재 상태를 한 번 직접 반영합니다.
             */
            if (_gameStateController == null)
            {
                InitializeReferences();
                SubscribeEvents();
            }

            if (_gameStateController != null)
            {
                ApplyGameState(
                    _gameStateController.CurrentState,
                    immediate: true
                );
            }
        }

        private void OnDisable()
        {
            UnsubscribeEvents();

            StopChangeRoutine();
        }

        // ========================================
        // Initialization
        // ========================================

        private void InitializeReferences()
        {
            if (_audioSource == null)
            {
                _audioSource =
                    GetComponent<AudioSource>();
            }

            if (_gameStateController == null)
            {
                _gameStateController =
                    FindFirstObjectByType<
                        GameStateController>();
            }

            if (_gameStateController == null)
            {
                Debug.LogError(
                    "[BGMManager] " +
                    "GameStateController를 찾지 못했습니다.",
                    this
                );
            }
        }

        private void InitializeAudioSource()
        {
            if (_audioSource == null)
                return;

            _audioSource.playOnAwake = false;
            _audioSource.loop = true;
            _audioSource.spatialBlend = 0f;
            _audioSource.volume = _volume;

            if (_outputMixerGroup != null)
            {
                _audioSource.outputAudioMixerGroup =
                    _outputMixerGroup;
            }
        }

        // ========================================
        // Event Subscription
        // ========================================

        private void SubscribeEvents()
        {
            if (_isSubscribed ||
                _gameStateController == null)
            {
                return;
            }

            _gameStateController.GameStateChanged +=
                HandleGameStateChanged;

            _isSubscribed = true;
        }

        private void UnsubscribeEvents()
        {
            if (!_isSubscribed)
                return;

            if (_gameStateController != null)
            {
                _gameStateController.GameStateChanged -=
                    HandleGameStateChanged;
            }

            _isSubscribed = false;
        }

        // ========================================
        // State
        // ========================================

        private void HandleGameStateChanged(
            GameState newState)
        {
            ApplyGameState(
                newState,
                immediate: false
            );
        }

        private void ApplyGameState(
            GameState gameState,
            bool immediate)
        {
            AudioClip targetClip =
                GetBGMClip(gameState);

            if (_showDebugLog)
            {
                string clipName =
                    targetClip != null
                        ? targetClip.name
                        : "Stop";

                Debug.Log(
                    "[BGMManager] " +
                    $"상태 적용: {gameState}, " +
                    $"BGM={clipName}",
                    this
                );
            }

            if (immediate || !_useFade)
            {
                ApplyClipImmediately(
                    targetClip
                );

                return;
            }

            ChangeBGM(
                targetClip
            );
        }

        private AudioClip GetBGMClip(
            GameState gameState)
        {
            return gameState switch
            {
                GameState.MAIN_MENU =>
                    _mainMenuBGM,

                GameState.STORY_MODE =>
                    _storyModeBGM,

                /*
                 * 리듬 게임 음악과 겹치지 않도록
                 * 나머지 상태에서는 일반 BGM을 정지합니다.
                 */
                GameState.MUSIC_MODE => null,
                GameState.BOSS_MODE => null,
                GameState.GAME_CLEAR => null,

                _ => null
            };
        }

        // ========================================
        // Public Control
        // ========================================

        public void PlayMainMenuBGM()
        {
            ChangeBGM(
                _mainMenuBGM
            );
        }

        public void PlayStoryModeBGM()
        {
            ChangeBGM(
                _storyModeBGM
            );
        }

        public void StopBGM()
        {
            ChangeBGM(null);
        }

        public void SetVolume(
            float volume)
        {
            _volume =
                Mathf.Clamp01(
                    volume
                );

            if (_audioSource != null &&
                _changeRoutine == null)
            {
                _audioSource.volume =
                    _volume;
            }
        }

        // ========================================
        // BGM Playback
        // ========================================

        private void ChangeBGM(
            AudioClip targetClip)
        {
            if (_audioSource == null)
                return;

            bool isSameClip =
                _audioSource.clip == targetClip;

            if (_keepPlayingSameClip &&
                isSameClip &&
                _audioSource.isPlaying)
            {
                return;
            }

            StopChangeRoutine();

            _changeRoutine =
                StartCoroutine(
                    ChangeBGMRoutine(
                        targetClip
                    )
                );
        }

        private IEnumerator ChangeBGMRoutine(
            AudioClip targetClip)
        {
            if (_audioSource.isPlaying)
            {
                yield return FadeVolumeRoutine(
                    _audioSource.volume,
                    0f,
                    _fadeOutDuration
                );

                _audioSource.Stop();
            }

            _audioSource.clip = targetClip;

            if (targetClip == null)
            {
                _audioSource.volume =
                    _volume;

                _changeRoutine = null;
                yield break;
            }

            _audioSource.volume = 0f;
            _audioSource.Play();

            yield return FadeVolumeRoutine(
                0f,
                _volume,
                _fadeInDuration
            );

            _audioSource.volume =
                _volume;

            _changeRoutine = null;
        }

        private IEnumerator FadeVolumeRoutine(
            float startVolume,
            float targetVolume,
            float duration)
        {
            if (duration <= 0f)
            {
                _audioSource.volume =
                    targetVolume;

                yield break;
            }

            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime +=
                    Time.unscaledDeltaTime;

                float normalizedTime =
                    Mathf.Clamp01(
                        elapsedTime / duration
                    );

                _audioSource.volume =
                    Mathf.Lerp(
                        startVolume,
                        targetVolume,
                        normalizedTime
                    );

                yield return null;
            }

            _audioSource.volume =
                targetVolume;
        }

        private void ApplyClipImmediately(
            AudioClip targetClip)
        {
            StopChangeRoutine();

            if (_audioSource.isPlaying)
            {
                _audioSource.Stop();
            }

            _audioSource.clip =
                targetClip;

            _audioSource.volume =
                _volume;

            if (targetClip != null)
            {
                _audioSource.Play();
            }
        }

        private void StopChangeRoutine()
        {
            if (_changeRoutine == null)
                return;

            StopCoroutine(
                _changeRoutine
            );

            _changeRoutine = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _volume =
                Mathf.Clamp01(
                    _volume
                );

            _fadeOutDuration =
                Mathf.Max(
                    0f,
                    _fadeOutDuration
                );

            _fadeInDuration =
                Mathf.Max(
                    0f,
                    _fadeInDuration
                );

            if (_audioSource != null)
            {
                _audioSource.playOnAwake = false;
                _audioSource.loop = true;
                _audioSource.spatialBlend = 0f;

                if (!Application.isPlaying)
                {
                    _audioSource.volume =
                        _volume;
                }

                if (_outputMixerGroup != null)
                {
                    _audioSource.outputAudioMixerGroup =
                        _outputMixerGroup;
                }
            }
        }
#endif
    }
}