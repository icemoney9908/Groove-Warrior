using System.Collections.Generic;
using UnityEngine;

namespace YWJ.UI.Conversation
{
    [CreateAssetMenu(
        fileName = "ConversationData",
        menuName = "YWJ/GameFlow/Conversation Data"
    )]
    public class ConversationData : ScriptableObject
    {
        [Header("Conversation Info")]
        [SerializeField]
        private string _conversationID;

        [SerializeField]
        private string _conversationTitle;

        [Header("Conversation Lines")]
        [SerializeField]
        private List<ConversationLine> _lines = new();

        public string ConversationID => _conversationID;

        public string ConversationTitle => _conversationTitle;

        public IReadOnlyList<ConversationLine> Lines => _lines;

        public bool HasLines =>
            _lines != null &&
            _lines.Count > 0;

        public ConversationLine GetLine(int index)
        {
            if (_lines == null ||
                index < 0 ||
                index >= _lines.Count)
            {
                return null;
            }

            return _lines[index];
        }
    }
}