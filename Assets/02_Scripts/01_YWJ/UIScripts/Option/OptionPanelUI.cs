using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YWJ.Player.Progress;

namespace YWJ.Settings
{
    public class OptionPanelUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private AudioSettingsManager _audioSettingsManager;

        [SerializeField]
        private PlayerProgressManager _playerProgressManager;

        [Header("Close")]
        [SerializeField]
        private Button _closeButton;

        [Header("Master Volume")]
        [SerializeField]
        private Slider _masterVolumeSlider;

        [SerializeField]
        private TMP_Text _masterVolumeValueText;

        [Header("Progress Reset")]
        [SerializeField]
        private Button _resetProgressButton;

        [SerializeField]
        private GameObject _resetConfirmPanel;

        [SerializeField]
        private Button _resetConfirmButton;

        [SerializeField]
        private Button _resetCancelButton;

        private void Awake()
        {
            FindReferences();
            InitializeUI();
        }

        private void OnEnable()
        {
            SubscribeEvents();

            /*
             * 외부 모듈에서 옵션 패널을 다시 활성화했을 때
             * 현재 저장된 값을 UI에 반영합니다.
             */
            RefreshUI();

            CloseResetConfirmPanel();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void FindReferences()
        {
            if (_audioSettingsManager == null)
            {
                _audioSettingsManager =
                    FindFirstObjectByType<AudioSettingsManager>();
            }

            if (_playerProgressManager == null)
            {
                _playerProgressManager =
                    FindFirstObjectByType<PlayerProgressManager>();
            }
        }

        private void InitializeUI()
        {
            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.minValue = 0f;
                _masterVolumeSlider.maxValue = 1f;
                _masterVolumeSlider.wholeNumbers = false;
            }

            CloseResetConfirmPanel();
        }

        private void SubscribeEvents()
        {
            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.onValueChanged.AddListener(
                    HandleMasterVolumeChanged);
            }

            if (_resetProgressButton != null)
            {
                _resetProgressButton.onClick.AddListener(
                    OpenResetConfirmPanel);
            }

            if (_resetConfirmButton != null)
            {
                _resetConfirmButton.onClick.AddListener(
                    ConfirmProgressReset);
            }

            if (_resetCancelButton != null)
            {
                _resetCancelButton.onClick.AddListener(
                    CloseResetConfirmPanel);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(
                    PressClose);
            }
        }

        private void UnsubscribeEvents()
        {
            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.onValueChanged.RemoveListener(
                    HandleMasterVolumeChanged);
            }

            if (_resetProgressButton != null)
            {
                _resetProgressButton.onClick.RemoveListener(
                    OpenResetConfirmPanel);
            }

            if (_resetConfirmButton != null)
            {
                _resetConfirmButton.onClick.RemoveListener(
                    ConfirmProgressReset);
            }

            if (_resetCancelButton != null)
            {
                _resetCancelButton.onClick.RemoveListener(
                    CloseResetConfirmPanel);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(PressClose);
            }
        }

        private void RefreshUI()
        {
            if (_audioSettingsManager == null)
            {
                Debug.LogWarning(
                    "[OptionPanelUI] " +
                    "AudioSettingsManager를 찾을 수 없습니다.",
                    this);

                return;
            }

            float masterVolume =
                _audioSettingsManager.MasterVolume;

            if (_masterVolumeSlider != null)
            {
                /*
                 * UI를 갱신하면서 볼륨 변경 이벤트가
                 * 다시 발생하지 않도록 합니다.
                 */
                _masterVolumeSlider.SetValueWithoutNotify(
                    masterVolume);
            }

            UpdateMasterVolumeText(
                masterVolume);
        }

        private void HandleMasterVolumeChanged(
            float value)
        {
            if (_audioSettingsManager == null)
                return;

            _audioSettingsManager.SetMasterVolume(
                value);

            UpdateMasterVolumeText(
                value);
        }

        private void UpdateMasterVolumeText(
            float value)
        {
            if (_masterVolumeValueText == null)
                return;

            int percentage =
                Mathf.RoundToInt(value * 100f);

            _masterVolumeValueText.text =
                $"{percentage}%";
        }

        private void OpenResetConfirmPanel()
        {
            if (_resetConfirmPanel != null)
            {
                _resetConfirmPanel.SetActive(true);
            }
        }

        private void CloseResetConfirmPanel()
        {
            if (_resetConfirmPanel != null)
            {
                _resetConfirmPanel.SetActive(false);
            }
        }

        private void ConfirmProgressReset()
        {
            if (_playerProgressManager == null)
            {
                Debug.LogError(
                    "[OptionPanelUI] " +
                    "PlayerProgressManager를 찾을 수 없습니다.",
                    this);

                return;
            }

            _playerProgressManager.ResetProgress();

            Debug.Log(
                "[OptionPanelUI] " +
                "게임 진행도를 초기화했습니다.",
                this);

            CloseResetConfirmPanel();
        }

        private void PressClose()
        {
            gameObject.SetActive(false);
        }
    }
}