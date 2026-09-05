using UnityEngine;
using UnityEngine.UI;
using YWJ.GameStateMachine;



namespace YWJ.UI
{
    public class GameClearUI : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button _continueButton;

        private GameStateController _gameStateController;

        private void Awake()
        {
            _gameStateController = FindFirstObjectByType<GameStateController>();

            _continueButton.onClick.AddListener(OnContinueButtonClicked);
        }

        private void OnContinueButtonClicked()
        {
            Debug.Log("Continue button clicked!");
        }
    }
}

