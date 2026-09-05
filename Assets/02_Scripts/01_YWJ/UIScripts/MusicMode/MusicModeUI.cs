using UnityEngine;
using UnityEngine.UI;
using TMPro;
using YWJ.GameStateMachine;
using YWJ.GameLogic;



namespace YWJ.UI
{
    public class MusicModeUI : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button _GameClearButton;
        [SerializeField] private Button _GameOverButton;

        [Header("Text")]
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _comboText;
        [SerializeField] private TMP_Text _multiplierText;
        [SerializeField] private TMP_Text _successPointText;

        private GameStateController _gameStateController;
        private GameScoreCalculator _gameScoreCalculator;

        private void Awake()
        {
            _gameStateController = FindFirstObjectByType<GameStateController>();
            _gameScoreCalculator = FindFirstObjectByType<GameScoreCalculator>();
        }

        private void Update()
        {
            UpdateScoreText();
            UpdateComboText();
            UpdateMultiplierText();
            UpdateSuccessPointText();
        }

        private void UpdateScoreText()
        {
            _scoreText.text = $"Score: {_gameScoreCalculator.Score:F0}";
        }

        private void UpdateComboText()
        {
            _comboText.text = $"COMBO: {_gameScoreCalculator.Combo}";
        }

        private void UpdateMultiplierText()
        {
            _multiplierText.text = $"× {_gameScoreCalculator.ScoreMultiplier:F2}";
        }

        private void UpdateSuccessPointText()
        {
            _successPointText.text = $"SP: {_gameScoreCalculator.SuccessPoint}";
        }
    }
}

