using UnityEngine;
using UnityEngine.UI;
using YWJ.GameStateMachine;



namespace YWJ.UI
{
    public class BossModeUI : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button _mainMenuButton;

        private GameStateController _gameStateController;

        private void Awake()
        {
            _gameStateController = FindFirstObjectByType<GameStateController>();

        }

    }
}

