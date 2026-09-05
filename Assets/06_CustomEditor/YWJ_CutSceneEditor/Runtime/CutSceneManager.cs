using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using YWJ.CameraSystem;
using YWJ.UI.Conversation;

namespace YWJ.CutScene
{
    public class CutSceneManager : MonoBehaviour
    {
        [Header("Managers")]
        [SerializeField] private ConversationManager _conversationManager;

        [SerializeField] private CameraController _cameraController;

        [Header("Control Lock")]
        [SerializeField] private Behaviour[] _behavioursToDisable;

        [Header("Events")]
        [SerializeField] private UnityEvent _onCutSceneStarted;
        [SerializeField] private UnityEvent _onCutSceneFinished;

        private readonly Dictionary<string, CutSceneActor> _actors = new();
        private readonly Dictionary<string, CutSceneMarker> _markers = new();
        private readonly Dictionary<string, CutSceneObject> _sceneObjects = new();
        private readonly Dictionary<string, Vector3> _actorStartPositions = new();

        private Coroutine _playCoroutine;

        public bool IsPlaying { get; private set; }
        public CutSceneData CurrentCutScene { get; private set; }

        public event Action CutSceneStarted;
        public event Action CutSceneFinished;

        private void Awake()
        {
            RefreshSceneReferences();
        }

        [ContextMenu("Refresh Scene References")]
        public void RefreshSceneReferences()
        {
            _actors.Clear();
            _markers.Clear();
            _sceneObjects.Clear();

            foreach (CutSceneActor actor in
                     FindObjectsByType<CutSceneActor>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (actor == null ||
                    string.IsNullOrWhiteSpace(actor.ActorID))
                {
                    continue;
                }

                if (_actors.ContainsKey(actor.ActorID))
                {
                    Debug.LogWarning(
                        $"[CutSceneManager] 중복 Actor ID: " +
                        $"{actor.ActorID}",
                        actor
                    );

                    continue;
                }

                _actors.Add(
                    actor.ActorID,
                    actor
                );
            }

            foreach (CutSceneMarker marker in
                     FindObjectsByType<CutSceneMarker>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (marker == null ||
                    string.IsNullOrWhiteSpace(marker.MarkerID))
                {
                    continue;
                }

                if (_markers.ContainsKey(marker.MarkerID))
                {
                    Debug.LogWarning(
                        $"[CutSceneManager] 중복 Marker ID: " +
                        $"{marker.MarkerID}",
                        marker
                    );

                    continue;
                }

                _markers.Add(
                    marker.MarkerID,
                    marker
                );
            }

            foreach (CutSceneObject sceneObject in
                     FindObjectsByType<CutSceneObject>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (sceneObject == null ||
                    string.IsNullOrWhiteSpace(sceneObject.ObjectID))
                {
                    continue;
                }

                if (_sceneObjects.ContainsKey(
                        sceneObject.ObjectID))
                {
                    Debug.LogWarning(
                        $"[CutSceneManager] 중복 Scene Object ID: " +
                        $"{sceneObject.ObjectID}",
                        sceneObject
                    );

                    continue;
                }

                _sceneObjects.Add(
                    sceneObject.ObjectID,
                    sceneObject
                );
            }
        }

        public void Play(CutSceneData cutSceneData)
        {
            Debug.Log(
            $"[CutSceneManager] Play 호출: {cutSceneData?.name}, " +
            $"IsPlaying={IsPlaying}, " +
            $"Current={CurrentCutScene?.name}",
            this);


            if (cutSceneData == null)
            {
                Debug.LogWarning("[CutSceneManager] CutSceneData가 없습니다.", this);
                return;
            }

            Stop();

            RefreshSceneReferences();
            _playCoroutine = StartCoroutine(PlayCoroutine(cutSceneData));
        }

        public void PlayStep(CutSceneData cutSceneData, int stepIndex)
        {
            if (cutSceneData == null ||
                stepIndex < 0 ||
                stepIndex >= cutSceneData.Steps.Count)
            {
                return;
            }

            Stop();
            RefreshSceneReferences();

            _playCoroutine = StartCoroutine(
                PlaySingleStepCoroutine(cutSceneData, stepIndex)
            );
        }

        public void Stop()
        {
            if (_playCoroutine != null)
            {
                StopCoroutine(_playCoroutine);
                _playCoroutine = null;
            }

            if (IsPlaying)
            {
                FinishCutScene();
            }
        }

        private IEnumerator PlayCoroutine(CutSceneData cutSceneData)
        {
            BeginCutScene(cutSceneData);

            foreach (CutSceneStep step in cutSceneData.Steps)
            {
                if (step != null)
                {
                    yield return ExecuteParallelStep(step);
                }
            }

            _playCoroutine = null;
            FinishCutScene();
        }

        private IEnumerator PlaySingleStepCoroutine(
            CutSceneData cutSceneData,
            int stepIndex)
        {
            BeginCutScene(cutSceneData);

            yield return ExecuteParallelStep(
                cutSceneData.Steps[stepIndex]
            );

            _playCoroutine = null;
            FinishCutScene();
        }

        private void BeginCutScene(
            CutSceneData cutSceneData)
        {
            CurrentCutScene = cutSceneData;
            IsPlaying = true;

            CaptureActorStartPositions();

            SetLockedBehavioursEnabled(false);

            CutSceneStarted?.Invoke();
            _onCutSceneStarted?.Invoke();
        }

        private void CaptureActorStartPositions()
        {
            _actorStartPositions.Clear();

            foreach (KeyValuePair<string, CutSceneActor> pair
                     in _actors)
            {
                CutSceneActor actor = pair.Value;

                if (actor == null)
                {
                    continue;
                }

                _actorStartPositions[pair.Key] =
                    actor.transform.position;
            }
        }

        private IEnumerator ExecuteParallelStep(CutSceneStep step)
        {
            int blockingCount = 0;
            int completedBlockingCount = 0;

            foreach (CutSceneAction action in step.Actions)
            {
                if (action == null)
                {
                    continue;
                }

                if (action.WaitForCompletion)
                {
                    blockingCount++;
                    StartCoroutine(
                        ExecuteTrackedAction(
                            action,
                            () => completedBlockingCount++
                        )
                    );
                }
                else
                {
                    StartCoroutine(ExecuteAction(action));
                }
            }

            while (completedBlockingCount < blockingCount)
            {
                yield return null;
            }
        }

        private IEnumerator ExecuteTrackedAction(
            CutSceneAction action,
            Action onCompleted)
        {
            yield return ExecuteAction(action);
            onCompleted?.Invoke();
        }

        private IEnumerator ExecuteAction(CutSceneAction action)
        {
            switch (action.ActionType)
            {
                case CutSceneActionType.Wait:
                    yield return Wait(action);
                    break;

                case CutSceneActionType.MoveActor:
                    yield return MoveActor(action);
                    break;

                case CutSceneActionType.TeleportActor:
                    TeleportActor(action);
                    break;

                case CutSceneActionType.RestoreActorPosition:
                    RestoreActorPosition(action);
                    break;

                case CutSceneActionType.SetAnimationBool:
                    GetActor(action.ActorID)?.SetAnimationBool(
                        action.AnimationParameter,
                        action.AnimationBoolValue
                    );
                    break;

                case CutSceneActionType.SetAnimationTrigger:
                    yield return PlayAnimationTrigger(action);
                    break;

                case CutSceneActionType.PlayAnimationState:
                    GetActor(action.ActorID)?.PlayAnimationState(
                        action.AnimationParameter,
                        action.NormalizedTime
                    );
                    break;

                case CutSceneActionType.PlayConversation:
                    yield return PlayConversation(action);
                    break;

                case CutSceneActionType.SetActorActive:
                    SetActorActive(action);
                    break;

                case CutSceneActionType.SetGameObjectActive:
                    SetSceneObjectActive(action);
                    break;

                case CutSceneActionType.InvokeSignal:
                    InvokeSceneObjectSignal(action);
                    break;

                case CutSceneActionType.SetCameraMonochrome:
                    yield return ExecuteCameraMonochrome(action);
                    break;

                case CutSceneActionType.ShakeCamera:
                    yield return ExecuteCameraShake(action);
                    break;

                case CutSceneActionType.SetCameraZoom:
                    yield return ExecuteCameraZoom(action);
                    break;
                case CutSceneActionType.MoveCamera:
                    yield return ExecuteMoveCamera(action);
                    break;
            }
        }

        private IEnumerator Wait(CutSceneAction action)
        {
            float elapsed = 0f;

            while (elapsed < action.Duration)
            {
                elapsed += action.UseUnscaledTime
                    ? Time.unscaledDeltaTime
                    : Time.deltaTime;

                yield return null;
            }
        }

        private IEnumerator ExecuteMoveCamera(
            CutSceneAction action)
        {
            if (_cameraController == null)
            {
                Debug.LogWarning(
                    "[CutSceneManager] " +
                    "CameraController가 없습니다.",
                    this
                );

                yield break;
            }

            if (string.IsNullOrWhiteSpace(
                    action.MarkerID))
            {
                Debug.LogWarning(
                    "[CutSceneManager] " +
                    "MoveCamera의 Marker ID가 비어 있습니다.",
                    this
                );

                yield break;
            }

            if (!_markers.TryGetValue(
                    action.MarkerID,
                    out CutSceneMarker marker))
            {
                Debug.LogWarning(
                    $"[CutSceneManager] " +
                    $"카메라 Marker를 찾지 못했습니다. " +
                    $"MarkerID={action.MarkerID}",
                    this
                );

                yield break;
            }

            _cameraController.FixAtPosition(
                marker.transform.position,
                action.Duration,
                action.MovementCurve
            );

            if (action.Duration > 0f)
            {
                yield return Wait(action);
            }
        }

        private IEnumerator MoveActor(CutSceneAction action)
        {
            CutSceneActor actor = GetActor(action.ActorID);

            if (actor == null)
            {
                yield break;
            }

            Vector3 start = actor.transform.position;
            Vector3 target = ResolveTargetPosition(action);

            if (action.FaceMovementDirection)
            {
                float deltaX = target.x - start.x;

                if (!Mathf.Approximately(deltaX, 0f))
                {
                    actor.SetFacingRight(deltaX > 0f);
                }
            }

            if (action.ControlMoveAnimation)
            {
                actor.SetAnimationBool(action.MoveAnimationBool, true);
            }

            if (action.Duration <= 0f)
            {
                actor.SetPosition(target);

                if (action.ControlMoveAnimation)
                {
                    actor.SetAnimationBool(action.MoveAnimationBool, false);
                }

                yield break;
            }

            float elapsed = 0f;

            while (elapsed < action.Duration)
            {
                elapsed += action.UseUnscaledTime
                    ? Time.unscaledDeltaTime
                    : Time.deltaTime;

                float t = Mathf.Clamp01(elapsed / action.Duration);
                float evaluated = action.MovementCurve != null
                    ? action.MovementCurve.Evaluate(t)
                    : t;

                actor.SetPosition(
                    Vector3.LerpUnclamped(start, target, evaluated)
                );

                yield return null;
            }

            actor.SetPosition(target);

            if (action.ControlMoveAnimation)
            {
                actor.SetAnimationBool(action.MoveAnimationBool, false);
            }
        }

        private void TeleportActor(
            CutSceneAction action)
        {
            CutSceneActor actor =
                GetActor(action.ActorID);

            if (actor == null)
            {
                return;
            }

            Vector3 targetPosition =
                ResolveTargetPosition(action);

            if (action.FaceMovementDirection)
            {
                float deltaX =
                    targetPosition.x -
                    actor.transform.position.x;

                if (!Mathf.Approximately(deltaX, 0f))
                {
                    actor.SetFacingRight(deltaX > 0f);
                }
            }

            actor.SetPosition(targetPosition);
        }

        private void RestoreActorPosition(
            CutSceneAction action)
        {
            CutSceneActor actor =
                GetActor(action.ActorID);

            if (actor == null)
            {
                return;
            }

            if (!_actorStartPositions.TryGetValue(
                    action.ActorID,
                    out Vector3 startPosition))
            {
                Debug.LogWarning(
                    $"[CutSceneManager] 시작 위치가 저장되지 않았습니다. " +
                    $"ActorID={action.ActorID}",
                    actor
                );

                return;
            }

            actor.SetPosition(startPosition);
        }

        private IEnumerator PlayAnimationTrigger(
            CutSceneAction action)
        {
            CutSceneActor actor = GetActor(action.ActorID);

            if (actor == null)
            {
                yield break;
            }

            actor.SetAnimationTrigger(
                action.AnimationParameter
            );

            if (action.Duration > 0f)
            {
                yield return Wait(action);
            }
        }

        private IEnumerator PlayConversation(CutSceneAction action)
        {
            if (_conversationManager == null ||
                action.ConversationData == null)
            {
                yield break;
            }

            _conversationManager.StartConversation(
                action.ConversationData
            );

            while (_conversationManager.IsPlaying)
            {
                yield return null;
            }
        }

        private void SetActorActive(CutSceneAction action)
        {
            CutSceneActor actor = GetActor(action.ActorID);

            if (actor != null)
            {
                actor.gameObject.SetActive(action.ActiveValue);
            }
        }

        private Vector3 ResolveTargetPosition(CutSceneAction action)
        {
            if (!action.UseMarker)
            {
                return action.TargetPosition;
            }

            if (_markers.TryGetValue(
                    action.MarkerID,
                    out CutSceneMarker marker))
            {
                return marker.transform.position;
            }

            Debug.LogWarning(
                $"[CutSceneManager] Marker를 찾지 못했습니다: {action.MarkerID}",
                this
            );

            return action.TargetPosition;
        }

        private CutSceneActor GetActor(string actorID)
        {
            if (_actors.TryGetValue(actorID, out CutSceneActor actor))
            {
                return actor;
            }

            Debug.LogWarning(
                $"[CutSceneManager] Actor를 찾지 못했습니다: {actorID}",
                this
            );

            return null;
        }

        private void FinishCutScene()
        {
            CutSceneData finished =
                CurrentCutScene;

            if (finished != null &&
                finished.RestoreActorPositionsOnFinish)
            {
                RestoreAllActorPositions();
            }

            CurrentCutScene = null;
            IsPlaying = false;

            _actorStartPositions.Clear();

            SetLockedBehavioursEnabled(true);

            CutSceneFinished?.Invoke();
            _onCutSceneFinished?.Invoke();
        }

        private void RestoreAllActorPositions()
        {
            foreach (KeyValuePair<string, Vector3> pair
                     in _actorStartPositions)
            {
                if (!_actors.TryGetValue(
                        pair.Key,
                        out CutSceneActor actor))
                {
                    continue;
                }

                if (actor == null)
                {
                    continue;
                }

                actor.SetPosition(pair.Value);
            }
        }

        private void SetLockedBehavioursEnabled(bool enabled)
        {
            if (_behavioursToDisable == null)
            {
                return;
            }

            foreach (Behaviour behaviour in _behavioursToDisable)
            {
                if (behaviour != null)
                {
                    behaviour.enabled = enabled;
                }
            }
        }

        private void SetSceneObjectActive(
    CutSceneAction action)
        {
            CutSceneObject sceneObject =
                GetSceneObject(
                    action.SceneObjectID
                );

            if (sceneObject == null)
                return;

            sceneObject.SetActive(
                action.ActiveValue
            );
        }

        private void InvokeSceneObjectSignal(
            CutSceneAction action)
        {
            CutSceneObject sceneObject =
                GetSceneObject(
                    action.SceneObjectID
                );

            if (sceneObject == null)
                return;

            sceneObject.InvokeSignal();
        }

        private CutSceneObject GetSceneObject(
            string objectID)
        {
            if (string.IsNullOrWhiteSpace(objectID))
            {
                Debug.LogWarning(
                    "[CutSceneManager] " +
                    "Scene Object ID가 비어 있습니다.",
                    this
                );

                return null;
            }

            if (_sceneObjects.TryGetValue(
                    objectID,
                    out CutSceneObject sceneObject))
            {
                return sceneObject;
            }

            Debug.LogWarning(
                $"[CutSceneManager] Scene Object를 찾지 못했습니다. " +
                $"ObjectID={objectID}",
                this
            );

            return null;
        }

        private IEnumerator ExecuteCameraMonochrome(
    CutSceneAction action)
        {
            if (_cameraController == null)
            {
                Debug.LogWarning(
                    "[CutSceneManager] CameraController가 없습니다.",
                    this
                );

                yield break;
            }

            _cameraController.SetMonochrome(
                action.MonochromeEnabled,
                action.Duration
            );

            yield return WaitForActionDuration(action);
        }

        private IEnumerator ExecuteCameraShake(
            CutSceneAction action)
        {
            if (_cameraController == null)
            {
                Debug.LogWarning(
                    "[CutSceneManager] CameraController가 없습니다.",
                    this
                );

                yield break;
            }

            _cameraController.Shake(
            action.Duration
            );

            if (!action.WaitForCompletion)
            {
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < action.Duration)
            {
                elapsed += action.UseUnscaledTime
                    ? Time.unscaledDeltaTime
                    : Time.deltaTime;

                yield return null;
            }
        }

        private IEnumerator ExecuteCameraZoom(
            CutSceneAction action)
        {
            if (_cameraController == null)
            {
                Debug.LogWarning(
                    "[CutSceneManager] CameraController가 없습니다.",
                    this
                );

                yield break;
            }

            _cameraController.Zoom(
                action.CameraOrthographicSize,
                action.Duration
            );

            yield return WaitForActionDuration(action);
        }

        private IEnumerator WaitForActionDuration(
            CutSceneAction action)
        {
            if (action.Duration <= 0f)
            {
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < action.Duration)
            {
                elapsed += action.UseUnscaledTime
                    ? Time.unscaledDeltaTime
                    : Time.deltaTime;

                yield return null;
            }
        }
    }
}
