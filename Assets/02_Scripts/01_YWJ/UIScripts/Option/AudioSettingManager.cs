using UnityEngine;
using UnityEngine.Audio;

namespace YWJ.Settings
{
    public class AudioSettingsManager : MonoBehaviour
    {
        private const string MasterVolumeKey =
            "Settings.MasterVolume";

        private const float DefaultMasterVolume = 1f;
        private const float MinimumDecibel = -80f;

        [Header("Audio Mixer")]
        [SerializeField]
        private AudioMixer _audioMixer;

        [SerializeField]
        private string _masterVolumeParameter =
            "Master";

        public float MasterVolume { get; private set; } =
            DefaultMasterVolume;

        private void Awake()
        {
            LoadSettings();
        }

        /// <summary>
        /// 저장되어 있는 오디오 설정을 불러옵니다.
        /// </summary>
        private void LoadSettings()
        {
            float savedVolume = PlayerPrefs.GetFloat(
                MasterVolumeKey,
                DefaultMasterVolume);

            SetMasterVolume(
                savedVolume,
                false);
        }

        /// <summary>
        /// 마스터 볼륨을 변경합니다.
        /// 값의 범위는 0~1입니다.
        /// </summary>
        public void SetMasterVolume(float normalizedVolume)
        {
            SetMasterVolume(
                normalizedVolume,
                true);
        }

        private void SetMasterVolume(
            float normalizedVolume,
            bool save)
        {
            normalizedVolume =
                Mathf.Clamp01(normalizedVolume);

            MasterVolume = normalizedVolume;

            float decibel;

            if (normalizedVolume <= 0.0001f)
            {
                decibel = MinimumDecibel;
            }
            else
            {
                decibel =
                    Mathf.Log10(normalizedVolume) * 20f;
            }

            if (_audioMixer == null)
            {
                Debug.LogError(
                    "[AudioSettingsManager] " +
                    "AudioMixer가 연결되지 않았습니다.",
                    this);

                return;
            }

            bool result = _audioMixer.SetFloat(
                _masterVolumeParameter,
                decibel);

            if (!result)
            {
                Debug.LogError(
                    $"[AudioSettingsManager] " +
                    $"AudioMixer에서 파라미터를 찾지 못했습니다: " +
                    $"{_masterVolumeParameter}",
                    this);

                return;
            }

            if (save)
            {
                PlayerPrefs.SetFloat(
                    MasterVolumeKey,
                    normalizedVolume);

                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// 오디오 설정을 기본값으로 되돌립니다.
        /// </summary>
        public void ResetAudioSettings()
        {
            SetMasterVolume(
                DefaultMasterVolume,
                true);
        }
    }
}
