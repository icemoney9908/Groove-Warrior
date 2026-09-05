using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace YWJ.UI.Conversation
{
    public class ConversationManager : MonoBehaviour
    {
        [Header("Conversation Root")]
        [SerializeField]
        private GameObject _conversationPanel;

        [Header("Speaker UI")]
        [SerializeField]
        private GameObject _speakerNamePanel;

        [SerializeField]
        private TMP_Text _speakerNameText;

        [SerializeField]
        private TMP_Text _dialogueText;

        [Header("Character Images")]
        [SerializeField]
        private Image _leftCharacterImage;

        [SerializeField]
        private Image _rightCharacterImage;

        [Header("Character Display")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _inactiveCharacterAlpha = 0.4f;

        [Range(0f, 1f)]
        [SerializeField]
        private float _activeCharacterAlpha = 1f;

        [SerializeField]
        private bool _hideUnusedCharacterImage = true;

        [Header("Typing Effect")]
        [SerializeField]
        private bool _useTypingEffect = true;

        [Min(0.001f)]
        [SerializeField]
        private float _typingInterval = 0.03f;

        [Header("Input")]
        [SerializeField]
        private Key _nextKey = Key.Space;

        [SerializeField]
        private bool _useMouseClick = true;

        [Header("Events")]
        [SerializeField]
        private UnityEvent _onConversationStarted;

        [SerializeField]
        private UnityEvent _onConversationEnded;

        private ConversationData _currentConversation;
        private Coroutine _typingCoroutine;

        private int _currentLineIndex;
        private bool _isTyping;
        private bool _isPlaying;

        private string _currentFullDialogue;

        public bool IsPlaying => _isPlaying;

        public bool IsTyping => _isTyping;

        public int CurrentLineIndex => _currentLineIndex;

        public ConversationData CurrentConversation =>
            _currentConversation;

        public event Action<ConversationData> ConversationStarted;

        public event Action<ConversationData> ConversationEnded;

        public event Action<ConversationLine, int> LineChanged;

        private void Awake()
        {
            SetConversationPanelActive(false);
            ClearCharacterImages();
        }

        private void Update()
        {
            if (!_isPlaying)
            {
                return;
            }

            if (!IsNextInputPressed())
            {
                return;
            }

            Proceed();
        }

        /// <summary>
        /// 지정한 대화 이벤트를 시작합니다.
        /// </summary>
        public void StartConversation(ConversationData conversation)
        {
            if (conversation == null)
            {
                Debug.LogWarning(
                    "[ConversationManager] ConversationData가 null입니다.",
                    this
                );

                return;
            }

            if (!conversation.HasLines)
            {
                Debug.LogWarning(
                    $"[ConversationManager] 대사가 없습니다. " +
                    $"Conversation: {conversation.name}",
                    conversation
                );

                return;
            }

            StopTypingCoroutine();

            _currentConversation = conversation;
            _currentLineIndex = 0;
            _isPlaying = true;

            ClearCharacterImages();
            SetConversationPanelActive(true);

            ConversationStarted?.Invoke(_currentConversation);
            _onConversationStarted?.Invoke();

            ShowCurrentLine();
        }

        /// <summary>
        /// 현재 대화를 진행합니다.
        /// 타이핑 중이면 전체 문장을 즉시 표시하고,
        /// 타이핑이 끝난 상태라면 다음 대사로 이동합니다.
        /// </summary>
        public void Proceed()
        {
            if (!_isPlaying)
            {
                return;
            }

            if (_isTyping)
            {
                CompleteTypingImmediately();
                return;
            }

            MoveNextLine();
        }

        /// <summary>
        /// 현재 대화를 강제로 종료합니다.
        /// </summary>
        public void StopConversation()
        {
            if (!_isPlaying)
            {
                return;
            }

            EndConversation();
        }

        private bool IsNextInputPressed()
        {
            if (Keyboard.current != null &&
                Keyboard.current[_nextKey].wasPressedThisFrame)
            {
                return true;
            }

            if (_useMouseClick &&
                Mouse.current != null &&
                Mouse.current.leftButton.wasPressedThisFrame)
            {
                return true;
            }

            return false;
        }

        private void MoveNextLine()
        {
            _currentLineIndex++;

            if (_currentConversation == null ||
                _currentLineIndex >= _currentConversation.Lines.Count)
            {
                EndConversation();
                return;
            }

            ShowCurrentLine();
        }

        private void ShowCurrentLine()
        {
            ConversationLine line =
                _currentConversation.GetLine(_currentLineIndex);

            if (line == null)
            {
                Debug.LogWarning(
                    $"[ConversationManager] 대사 정보를 찾을 수 없습니다. " +
                    $"Index: {_currentLineIndex}",
                    this
                );

                EndConversation();
                return;
            }

            UpdateSpeakerUI(line);
            UpdateCharacterUI(line);
            DisplayDialogue(line.Dialogue);

            LineChanged?.Invoke(line, _currentLineIndex);
        }

        private void UpdateSpeakerUI(ConversationLine line)
        {
            bool showSpeakerName =
                !line.IsNarration &&
                line.Speaker != null;

            if (_speakerNamePanel != null)
            {
                _speakerNamePanel.SetActive(showSpeakerName);
            }

            if (_speakerNameText != null)
            {
                _speakerNameText.text =
                    showSpeakerName
                        ? line.GetSpeakerName()
                        : string.Empty;
            }
        }

        private void UpdateCharacterUI(ConversationLine line)
        {
            if (line.IsNarration || line.Speaker == null)
            {
                SetCharacterActiveState(null);
                return;
            }

            Sprite characterImage = line.GetCharacterImage();

            switch (line.DisplaySide)
            {
                case CharacterDisplaySide.Left:
                    ShowCharacterImage(
                        _leftCharacterImage,
                        characterImage
                    );

                    SetCharacterActiveState(_leftCharacterImage);
                    break;

                case CharacterDisplaySide.Right:
                    ShowCharacterImage(
                        _rightCharacterImage,
                        characterImage
                    );

                    SetCharacterActiveState(_rightCharacterImage);
                    break;
            }
        }

        private void ShowCharacterImage(
            Image targetImage,
            Sprite characterSprite)
        {
            if (targetImage == null)
            {
                Debug.LogWarning(
                    "[ConversationManager] 대상 이미지가 없습니다.",
                    this
                );
                return;
            }

            targetImage.sprite = characterSprite;

            bool hasSprite = characterSprite != null;
            targetImage.gameObject.SetActive(hasSprite);
        }

        private void SetCharacterActiveState(Image activeImage)
        {
            SetImageAlpha(
                _leftCharacterImage,
                activeImage == _leftCharacterImage
                    ? _activeCharacterAlpha
                    : _inactiveCharacterAlpha
            );

            SetImageAlpha(
                _rightCharacterImage,
                activeImage == _rightCharacterImage
                    ? _activeCharacterAlpha
                    : _inactiveCharacterAlpha
            );
        }

        private void SetImageAlpha(
            Image targetImage,
            float alpha)
        {
            if (targetImage == null)
            {
                return;
            }

            if (!targetImage.gameObject.activeSelf)
            {
                return;
            }

            Color color = targetImage.color;
            color.a = alpha;
            targetImage.color = color;
        }

        private void DisplayDialogue(string dialogue)
        {
            StopTypingCoroutine();

            _currentFullDialogue = dialogue ?? string.Empty;

            if (_dialogueText == null)
            {
                return;
            }

            if (!_useTypingEffect)
            {
                _dialogueText.text = _currentFullDialogue;
                _isTyping = false;
                return;
            }

            _typingCoroutine = StartCoroutine(
                TypeDialogueCoroutine(_currentFullDialogue)
            );
        }

        private IEnumerator TypeDialogueCoroutine(string dialogue)
        {
            _isTyping = true;
            _dialogueText.text = string.Empty;

            foreach (char character in dialogue)
            {
                _dialogueText.text += character;

                yield return new WaitForSecondsRealtime(
                    _typingInterval
                );
            }

            _dialogueText.text = dialogue;
            _isTyping = false;
            _typingCoroutine = null;
        }

        private void CompleteTypingImmediately()
        {
            StopTypingCoroutine();

            if (_dialogueText != null)
            {
                _dialogueText.text = _currentFullDialogue;
            }

            _isTyping = false;
        }

        private void StopTypingCoroutine()
        {
            if (_typingCoroutine == null)
            {
                return;
            }

            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
            _isTyping = false;
        }

        private void EndConversation()
        {
            ConversationData endedConversation =
                _currentConversation;

            StopTypingCoroutine();

            _currentConversation = null;
            _currentLineIndex = 0;
            _currentFullDialogue = string.Empty;
            _isPlaying = false;

            if (_dialogueText != null)
            {
                _dialogueText.text = string.Empty;
            }

            if (_speakerNameText != null)
            {
                _speakerNameText.text = string.Empty;
            }

            ClearCharacterImages();
            SetConversationPanelActive(false);

            ConversationEnded?.Invoke(endedConversation);
            _onConversationEnded?.Invoke();
        }

        private void ClearCharacterImages()
        {
            ClearCharacterImage(_leftCharacterImage);
            ClearCharacterImage(_rightCharacterImage);
        }

        private void ClearCharacterImage(Image targetImage)
        {
            if (targetImage == null)
            {
                return;
            }

            targetImage.sprite = null;

            if (_hideUnusedCharacterImage)
            {
                targetImage.gameObject.SetActive(false);
            }
            else
            {
                SetImageAlpha(targetImage, 0f);
            }
        }

        private void SetConversationPanelActive(bool isActive)
        {
            if (_conversationPanel != null)
            {
                _conversationPanel.SetActive(isActive);
            }
        }
    }
}