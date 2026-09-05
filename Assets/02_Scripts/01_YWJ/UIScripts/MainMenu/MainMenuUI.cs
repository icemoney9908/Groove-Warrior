using UnityEngine;
using UnityEngine.UI;
using YWJ.GameFlow;



namespace YWJ.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button _gameStartButton;
        [SerializeField] private Button _optionButton;
        [SerializeField] private Button _exitButton;

        [Header("Option Panel")]
        [SerializeField] private GameObject _optionPanel;

        private GameFlowManager _gameFlowManager;

        private void Awake()
        {
            _gameFlowManager = FindFirstObjectByType<GameFlowManager>();

            _gameStartButton.onClick.AddListener(OnGameStartButtonClicked);
            _optionButton.onClick.AddListener(OnOptionButtonClicked);
            _exitButton.onClick.AddListener(OnExitButtonClicked);
        }

        private void OnGameStartButtonClicked()
        {
            Debug.Log("Game Start button clicked!");
            _gameFlowManager?.StartCurrentFlow();
        }

        private void OnOptionButtonClicked()
        {
            Debug.Log("Option button clicked!");
            _optionPanel.SetActive(true);
        }

        private void OnExitButtonClicked()
        {
            Debug.Log("Exit button clicked!");
            Application.Quit();
        }
    }
}
