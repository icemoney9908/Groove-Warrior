using System;
using UnityEngine;
using YWJ.UI.Conversation;

namespace YWJ.CutScene
{
    [Serializable]
    public class CutSceneAction
    {
        [SerializeField]
        private string _actionName = "New Action";

        [SerializeField]
        private CutSceneActionType _actionType;

        [SerializeField]
        private bool _waitForCompletion = true;

        [SerializeField]
        private bool _useUnscaledTime;

        [SerializeField]
        private string _actorID;

        [Min(0f)]
        [SerializeField]
        private float _duration = 1f;

        [SerializeField]
        private bool _useMarker = true;

        [SerializeField]
        private string _markerID;

        [SerializeField]
        private Vector3 _targetPosition;

        [SerializeField]
        private AnimationCurve _movementCurve =
            AnimationCurve.EaseInOut(
                0f,
                0f,
                1f,
                1f
            );

        [SerializeField]
        private bool _controlMoveAnimation = true;

        [SerializeField]
        private string _moveAnimationBool = "IsMove";

        [SerializeField]
        private bool _faceMovementDirection = true;

        [SerializeField]
        private string _animationParameter;

        [SerializeField]
        private bool _animationBoolValue;

        [Range(0f, 1f)]
        [SerializeField]
        private float _normalizedTime;

        [SerializeField]
        private ConversationData _conversationData;

        [SerializeField]
        private string _sceneObjectID;

        [SerializeField]
        private bool _activeValue = true;

        [SerializeField]
        private bool _monochromeEnabled;

        [Min(0.01f)]
        [SerializeField]
        private float _cameraOrthographicSize = 5f;

        public string ActionName => _actionName;
        public CutSceneActionType ActionType => _actionType;
        public bool WaitForCompletion => _waitForCompletion;
        public bool UseUnscaledTime => _useUnscaledTime;
        public string ActorID => _actorID;
        public float Duration => _duration;
        public bool UseMarker => _useMarker;
        public string MarkerID => _markerID;
        public Vector3 TargetPosition => _targetPosition;
        public AnimationCurve MovementCurve => _movementCurve;
        public bool ControlMoveAnimation => _controlMoveAnimation;
        public string MoveAnimationBool => _moveAnimationBool;
        public bool FaceMovementDirection => _faceMovementDirection;
        public string AnimationParameter => _animationParameter;
        public bool AnimationBoolValue => _animationBoolValue;
        public float NormalizedTime => _normalizedTime;
        public ConversationData ConversationData => _conversationData;
        public string SceneObjectID => _sceneObjectID;
        public bool ActiveValue => _activeValue;
        public bool MonochromeEnabled => _monochromeEnabled;
        public float CameraOrthographicSize => _cameraOrthographicSize;
    }
}