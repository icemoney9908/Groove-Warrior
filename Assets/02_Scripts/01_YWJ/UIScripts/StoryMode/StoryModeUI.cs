using UnityEngine;
using UnityEngine.UI;
using YWJ.CutScene;
using YWJ.GameStateMachine;



namespace YWJ.UI
{
    public class StoryModeUI : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button _musicStartButton;
        [SerializeField] private Button _cutSceneStartButton;

        [Header("Player")]
        [SerializeField] private GameObject _player;

        [Header("Hit Lane")]
        [SerializeField] private GameObject _hitLine;

        [Header("CutScene")]
        [SerializeField] private CutSceneManager _cutSceneManager;
        [SerializeField] private CutSceneData _cutSceneData;

        private GameStateController _gameStateController;


        private void Awake()
        {
            _gameStateController = FindFirstObjectByType<GameStateController>();
            _cutSceneManager = FindFirstObjectByType<CutSceneManager>();
        }
    }
}

