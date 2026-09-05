using System;
using UnityEngine;
using YWJ.GameFlow;

namespace YWJ.UI.Conversation
{
    public enum CharacterDisplaySide
    {
        Left,
        Right
    }

    [Serializable]
    public class ConversationLine
    {
        [Header("Speaker")]
        [SerializeField]
        private CharacterData _speaker;

        [Header("Dialogue")]
        [TextArea(2, 6)]
        [SerializeField]
        private string _dialogue;

        [Header("Character Display")]
        [SerializeField]
        private CharacterDisplaySide _displaySide = CharacterDisplaySide.Left;

        [Tooltip("설정하면 CharacterData의 기본 이미지 대신 이 이미지를 사용합니다.")]
        [SerializeField]
        private Sprite _overrideCharacterImage;

        [Header("Display Options")]
        [Tooltip("화자가 없는 내레이션 대사로 표시합니다.")]
        [SerializeField]
        private bool _isNarration;

        public CharacterData Speaker => _speaker;

        public string Dialogue => _dialogue;

        public CharacterDisplaySide DisplaySide => _displaySide;

        public Sprite OverrideCharacterImage => _overrideCharacterImage;

        public bool IsNarration => _isNarration;

        public Sprite GetCharacterImage()
        {
            if (_overrideCharacterImage != null)
            {
                return _overrideCharacterImage;
            }

            return _speaker != null
                ? _speaker.CharacterImage
                : null;
        }

        public string GetSpeakerName()
        {
            if (_isNarration)
            {
                return string.Empty;
            }

            return _speaker != null
                ? _speaker.CharacterName
                : string.Empty;
        }
    }
}