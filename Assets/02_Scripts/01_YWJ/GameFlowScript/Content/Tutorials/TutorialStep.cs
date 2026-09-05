using System;
using UnityEngine;
using YWJ.UI.Conversation;

namespace YWJ.GameFlow.Content
{
    public enum TutorialStepType
    {
        WaitForSongTime,
        PlayConversation,
        WaitForTabSuccess,
        WaitForHoldStartSuccess,
        WaitForHoldCompleteSuccess,
        ResumeMusic,
        PauseMusic,
        CompleteTutorial
    }

    [Serializable]
    public class TutorialStep
    {
        [SerializeField]
        private string _stepName;

        [SerializeField]
        private TutorialStepType _stepType;

        [Min(0f)]
        [SerializeField]
        private float _songTime;

        [SerializeField]
        private ConversationData _conversationData;

        [SerializeField]
        private bool _pauseMusicBeforeConversation = true;

        [SerializeField]
        private bool _resumeMusicAfterConversation;

        public string StepName => _stepName;

        public TutorialStepType StepType =>
            _stepType;

        public float SongTime =>
            _songTime;

        public ConversationData ConversationData =>
            _conversationData;

        public bool PauseMusicBeforeConversation =>
            _pauseMusicBeforeConversation;

        public bool ResumeMusicAfterConversation =>
            _resumeMusicAfterConversation;
    }
}