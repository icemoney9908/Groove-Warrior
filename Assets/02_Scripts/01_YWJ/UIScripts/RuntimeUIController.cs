using UnityEngine;
using UnityEngine.UI;
using YWJ.GameStateMachine;



namespace YWJ.UI
{
    public class RuntimeUIController : MonoBehaviour
    {
        [Header("Runtime UI")]
        [SerializeField] private GameObject _mainMenuUI;
        [SerializeField] private GameObject _storyModeUI;
        [SerializeField] private GameObject _musicModeUI;
        [SerializeField] private GameObject _gameClearUI;
        [SerializeField] private GameObject _bossModeUI;

        private GameObject _currentRuntimeUI;


        public void SetRuntimeUI(GameState newState)
        {
            if (_currentRuntimeUI != null)
            {
                _currentRuntimeUI.SetActive(false);
            }

            _currentRuntimeUI = newState switch
            {
                GameState.MAIN_MENU => _mainMenuUI,
                GameState.STORY_MODE => _storyModeUI,
                GameState.MUSIC_MODE => _musicModeUI,
                GameState.BOSS_MODE => _bossModeUI,
                GameState.GAME_CLEAR => _gameClearUI,
                _ => null
            };

            if (_currentRuntimeUI != null)
            {
                _currentRuntimeUI.SetActive(true);
            }
        }

    }
}
