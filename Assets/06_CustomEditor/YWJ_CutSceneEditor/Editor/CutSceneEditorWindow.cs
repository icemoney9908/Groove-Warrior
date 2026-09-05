#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace YWJ.CutScene.Editor
{
    public class CutSceneEditorWindow : EditorWindow
    {
        private CutSceneData _data;
        private SerializedObject _serializedData;
        private SerializedProperty _stepsProperty;

        private ReorderableList _stepList;
        private readonly Dictionary<string, ReorderableList> _actionLists = new();

        private Vector2 _scroll;

        private string[] _actorIDs = { "(Actor 없음)" };
        private string[] _markerIDs = { "(Marker 없음)" };
        private string[] _sceneObjectIDs = { "(Scene Object 없음)" };

        [MenuItem("YWJ/CutScene Editor V2")]
        public static void Open()
        {
            GetWindow<CutSceneEditorWindow>(
                "YWJ CutScene Editor"
            );
        }

        private void OnEnable()
        {
            RefreshSceneOptions();
        }

        private void OnHierarchyChange()
        {
            RefreshSceneOptions();
            Repaint();
        }

        private void OnSelectionChange()
        {
            if (Selection.activeObject is CutSceneData selected)
            {
                SetData(selected);
                Repaint();
            }
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (_data == null)
            {
                EditorGUILayout.HelpBox(
                    "CutSceneData를 선택하거나 New 버튼으로 생성하세요.",
                    MessageType.Info
                );
                return;
            }

            EnsureSerializedData();

            _serializedData.Update();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawHeader();
            EditorGUILayout.Space(8f);

            _stepList.DoLayoutList();

            EditorGUILayout.EndScrollView();

            if (_serializedData.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(_data);
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                CutSceneData selected = (CutSceneData)EditorGUILayout.ObjectField(
                    _data,
                    typeof(CutSceneData),
                    false,
                    GUILayout.MinWidth(180f)
                );

                if (selected != _data)
                {
                    SetData(selected);
                }

                if (GUILayout.Button(
                        "New",
                        EditorStyles.toolbarButton,
                        GUILayout.Width(45f)))
                {
                    CreateNewData();
                }

                if (GUILayout.Button(
                        "Refresh Scene",
                        EditorStyles.toolbarButton,
                        GUILayout.Width(95f)))
                {
                    RefreshSceneOptions();
                }

                GUILayout.FlexibleSpace();

                GUI.enabled = Application.isPlaying && _data != null;

                if (GUILayout.Button(
                        "Play All",
                        EditorStyles.toolbarButton,
                        GUILayout.Width(65f)))
                {
                    FindFirstObjectByType<CutSceneManager>()?.Play(_data);
                }

                GUI.enabled = true;
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField(
                "CutScene Information",
                EditorStyles.boldLabel
            );

            EditorGUILayout.PropertyField(
                _serializedData.FindProperty(
                    "_cutSceneID"
                )
            );

            EditorGUILayout.PropertyField(
                _serializedData.FindProperty(
                    "_displayName"
                )
            );

            EditorGUILayout.PropertyField(
                _serializedData.FindProperty(
                    "_description"
                )
            );

            EditorGUILayout.Space(4f);

            EditorGUILayout.PropertyField(
                _serializedData.FindProperty(
                    "_restoreActorPositionsOnFinish"
                ),
                new GUIContent(
                    "컷신 종료 시 Actor 위치 복구"
                )
            );
        }

        private void BuildStepList()
        {
            _stepList = new ReorderableList(
                _serializedData,
                _stepsProperty,
                true,
                true,
                true,
                true
            );

            _stepList.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(
                    rect,
                    $"Parallel Steps ({_stepsProperty.arraySize})",
                    EditorStyles.boldLabel
                );
            };

            _stepList.elementHeightCallback = index =>
            {
                SerializedProperty step =
                    _stepsProperty.GetArrayElementAtIndex(index);

                ReorderableList actions = GetActionList(step, index);

                return 58f +
                       actions.GetHeight() +
                       EditorGUIUtility.standardVerticalSpacing;
            };

            _stepList.drawElementCallback = (
                rect,
                index,
                active,
                focused) =>
            {
                SerializedProperty step =
                    _stepsProperty.GetArrayElementAtIndex(index);

                SerializedProperty stepName =
                    step.FindPropertyRelative("_stepName");

                Rect titleRect = new Rect(
                    rect.x,
                    rect.y + 2f,
                    rect.width - 165f,
                    EditorGUIUtility.singleLineHeight
                );

                EditorGUI.PropertyField(
                    titleRect,
                    stepName,
                    GUIContent.none
                );

                Rect duplicateRect = new Rect(
                    rect.xMax - 158f,
                    rect.y + 2f,
                    58f,
                    EditorGUIUtility.singleLineHeight
                );

                if (GUI.Button(duplicateRect, "Duplicate"))
                {
                    DuplicateArrayElement(_stepsProperty, index);
                    RebuildLists();
                    GUIUtility.ExitGUI();
                }

                Rect testRect = new Rect(
                    rect.xMax - 95f,
                    rect.y + 2f,
                    42f,
                    EditorGUIUtility.singleLineHeight
                );

                GUI.enabled = Application.isPlaying;

                if (GUI.Button(testRect, "Test"))
                {
                    FindFirstObjectByType<CutSceneManager>()?
                        .PlayStep(_data, index);
                }

                GUI.enabled = true;

                Rect selectRect = new Rect(
                    rect.xMax - 48f,
                    rect.y + 2f,
                    48f,
                    EditorGUIUtility.singleLineHeight
                );

                if (GUI.Button(selectRect, "Scene"))
                {
                    FocusStepSceneObjects(step);
                }

                Rect helpRect = new Rect(
                    rect.x,
                    rect.y + 24f,
                    rect.width,
                    28f
                );

                EditorGUI.HelpBox(
                    helpRect,
                    "이 Step 안의 Action들은 동시에 시작됩니다.",
                    MessageType.None
                );

                ReorderableList actionList =
                    GetActionList(step, index);

                Rect listRect = new Rect(
                    rect.x,
                    rect.y + 55f,
                    rect.width,
                    actionList.GetHeight()
                );

                actionList.DoList(listRect);
            };

            _stepList.onAddCallback = list =>
            {
                int index = _stepsProperty.arraySize;
                _stepsProperty.InsertArrayElementAtIndex(index);

                SerializedProperty step =
                    _stepsProperty.GetArrayElementAtIndex(index);

                step.FindPropertyRelative("_stepName").stringValue =
                    $"Step {index + 1}";

                step.FindPropertyRelative("_actions").ClearArray();

                RebuildLists();
            };

            _stepList.onRemoveCallback = list =>
            {
                if (EditorUtility.DisplayDialog(
                        "Delete Step",
                        "선택한 Step을 삭제할까요?",
                        "Delete",
                        "Cancel"))
                {
                    ReorderableList.defaultBehaviours
                        .DoRemoveButton(list);

                    RebuildLists();
                }
            };
        }

        private ReorderableList GetActionList(
            SerializedProperty step,
            int stepIndex)
        {
            string path = step.propertyPath;

            if (_actionLists.TryGetValue(
                    path,
                    out ReorderableList existing))
            {
                return existing;
            }

            SerializedProperty actions =
                step.FindPropertyRelative("_actions");

            ReorderableList list = new ReorderableList(
                _serializedData,
                actions,
                true,
                true,
                true,
                true
            );

            list.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(
                    rect,
                    $"Actions ({actions.arraySize})"
                );
            };

            list.elementHeightCallback = actionIndex =>
            {
                SerializedProperty action =
                    actions.GetArrayElementAtIndex(actionIndex);

                return GetActionHeight(action);
            };

            list.drawElementCallback = (
                rect,
                actionIndex,
                active,
                focused) =>
            {
                SerializedProperty action =
                    actions.GetArrayElementAtIndex(actionIndex);

                DrawAction(rect, action, actions, actionIndex);
            };

            list.onAddDropdownCallback = (rect, targetList) =>
            {
                GenericMenu menu = new GenericMenu();

                foreach (CutSceneActionType type in
                         Enum.GetValues(typeof(CutSceneActionType)))
                {
                    CutSceneActionType captured = type;

                    menu.AddItem(
                        new GUIContent(type.ToString()),
                        false,
                        () =>
                        {
                            AddAction(actions, captured);
                            RebuildLists();
                        }
                    );
                }

                menu.ShowAsContext();
            };

            list.onRemoveCallback = targetList =>
            {
                if (EditorUtility.DisplayDialog(
                        "Delete Action",
                        "선택한 Action을 삭제할까요?",
                        "Delete",
                        "Cancel"))
                {
                    ReorderableList.defaultBehaviours
                        .DoRemoveButton(targetList);

                    RebuildLists();
                }
            };

            _actionLists.Add(path, list);
            return list;
        }

        private void DrawAction(
            Rect rect,
            SerializedProperty action,
            SerializedProperty actions,
            int actionIndex)
        {
            float y = rect.y + 2f;
            float line = EditorGUIUtility.singleLineHeight;
            float gap = EditorGUIUtility.standardVerticalSpacing;

            SerializedProperty typeProperty =
                action.FindPropertyRelative("_actionType");

            Rect typeRect = new Rect(
                rect.x,
                y,
                rect.width * 0.38f,
                line
            );

            EditorGUI.PropertyField(
                typeRect,
                typeProperty,
                GUIContent.none
            );

            Rect nameRect = new Rect(
                typeRect.xMax + 4f,
                y,
                rect.width - typeRect.width - 72f,
                line
            );

            EditorGUI.PropertyField(
                nameRect,
                action.FindPropertyRelative("_actionName"),
                GUIContent.none
            );

            Rect duplicateRect = new Rect(
                rect.xMax - 64f,
                y,
                64f,
                line
            );

            if (GUI.Button(duplicateRect, "Duplicate"))
            {
                DuplicateArrayElement(actions, actionIndex);
                RebuildLists();
                GUIUtility.ExitGUI();
            }

            y += line + gap;

            Rect waitRect = new Rect(
                rect.x,
                y,
                rect.width,
                line
            );

            EditorGUI.PropertyField(
                waitRect,
                action.FindPropertyRelative("_waitForCompletion"),
                new GUIContent("완료까지 기다림")
            );

            y += line + gap;

            CutSceneActionType type =
                (CutSceneActionType)typeProperty.enumValueIndex;

            DrawActionSpecificFields(
                rect.x,
                ref y,
                rect.width,
                action,
                type
            );
        }

        private void DrawActionSpecificFields(
            float x,
            ref float y,
            float width,
            SerializedProperty action,
            CutSceneActionType type)
        {
            switch (type)
            {
                case CutSceneActionType.Wait:
                    DrawDuration(x, ref y, width, action);
                    break;

                case CutSceneActionType.MoveActor:
                    DrawActorPopup(x, ref y, width, action);
                    DrawDuration(x, ref y, width, action);

                    DrawProperty(
                        x, ref y, width,
                        action.FindPropertyRelative("_useMarker"),
                        "Marker 사용"
                    );

                    if (action.FindPropertyRelative("_useMarker").boolValue)
                    {
                        DrawMarkerPopup(x, ref y, width, action);
                    }
                    else
                    {
                        DrawProperty(
                            x, ref y, width,
                            action.FindPropertyRelative("_targetPosition"),
                            "Target Position"
                        );
                    }

                    DrawProperty(
                        x, ref y, width,
                        action.FindPropertyRelative("_movementCurve"),
                        "Movement Curve"
                    );

                    DrawProperty(
                        x, ref y, width,
                        action.FindPropertyRelative("_controlMoveAnimation"),
                        "이동 중 Animator Bool 자동 제어"
                    );

                    if (action.FindPropertyRelative(
                            "_controlMoveAnimation").boolValue)
                    {
                        DrawAnimatorParameterPopup(
                            x, ref y, width,
                            action,
                            "_moveAnimationBool",
                            UnityEngine.AnimatorControllerParameterType.Bool,
                            "Move Bool"
                        );
                    }

                    DrawProperty(
                        x, ref y, width,
                        action.FindPropertyRelative("_faceMovementDirection"),
                        "이동 방향 자동 바라보기"
                    );
                    break;

                case CutSceneActionType.TeleportActor:
                    DrawActorPopup(
                        x,
                        ref y,
                        width,
                        action
                    );

                    DrawProperty(
                        x,
                        ref y,
                        width,
                        action.FindPropertyRelative(
                            "_useMarker"
                        ),
                        "Marker 사용"
                    );

                    if (action.FindPropertyRelative(
                            "_useMarker").boolValue)
                    {
                        DrawMarkerPopup(
                            x,
                            ref y,
                            width,
                            action
                        );
                    }
                    else
                    {
                        DrawProperty(
                            x,
                            ref y,
                            width,
                            action.FindPropertyRelative(
                                "_targetPosition"
                            ),
                            "Target Position"
                        );
                    }

                    DrawProperty(
                        x,
                        ref y,
                        width,
                        action.FindPropertyRelative(
                            "_faceMovementDirection"
                        ),
                        "이동 방향 자동 바라보기"
                    );
                    break;

                case CutSceneActionType.RestoreActorPosition:
                    DrawActorPopup(
                        x,
                        ref y,
                        width,
                        action
                    );
                    break;

                case CutSceneActionType.SetAnimationBool:
                    DrawActorPopup(x, ref y, width, action);
                    DrawAnimatorParameterPopup(
                        x, ref y, width,
                        action,
                        "_animationParameter",
                        UnityEngine.AnimatorControllerParameterType.Bool,
                        "Bool Parameter"
                    );
                    DrawProperty(
                        x, ref y, width,
                        action.FindPropertyRelative("_animationBoolValue"),
                        "Value"
                    );
                    break;

                case CutSceneActionType.SetAnimationTrigger:
                    DrawActorPopup(x, ref y, width, action);
                    DrawAnimatorParameterPopup(
                        x, ref y,width,
                        action,
                        "_animationParameter",
                        UnityEngine.AnimatorControllerParameterType.Trigger,
                        "Trigger Parameter"
                    );

                    DrawDuration(x, ref y, width, action);
                    break;

                case CutSceneActionType.PlayAnimationState:
                    DrawActorPopup(x, ref y, width, action);
                    DrawProperty(
                        x, ref y, width,
                        action.FindPropertyRelative("_animationParameter"),
                        "State Name"
                    );
                    DrawProperty(
                        x, ref y, width,
                        action.FindPropertyRelative("_normalizedTime"),
                        "Normalized Time"
                    );
                    break;

                case CutSceneActionType.PlayConversation:
                    DrawProperty(
                        x, ref y, width,
                        action.FindPropertyRelative("_conversationData"),
                        "Conversation"
                    );
                    break;

                case CutSceneActionType.SetActorActive:
                    DrawActorPopup(x, ref y, width, action);
                    DrawProperty(
                        x, ref y, width,
                        action.FindPropertyRelative("_activeValue"),
                        "Active"
                    );
                    break;

                case CutSceneActionType.SetGameObjectActive:
                    DrawSceneObjectPopup(
                        x,
                        ref y,
                        width,
                        action
                    );

                    DrawProperty(
                        x,
                        ref y,
                        width,
                        action.FindPropertyRelative(
                            "_activeValue"
                        ),
                        "Active"
                    );
                    break;

                case CutSceneActionType.InvokeSignal:
                    DrawSceneObjectPopup(
                        x,
                        ref y,
                        width,
                        action
                    );
                    break;

                case CutSceneActionType.SetCameraMonochrome:
                    DrawProperty(
                        x, ref y, width,
                        action.FindPropertyRelative("_monochromeEnabled"),
                        "모노크롬 적용 여부"
                    );
                    DrawProperty(
                        x, ref y, width,
                        action.FindPropertyRelative("_duration"),
                        "변환 시간"
                    );
                    break;

                case CutSceneActionType.ShakeCamera:
                    DrawProperty(
                        x, ref y, width,
                        action.FindPropertyRelative("_duration"),
                        "흔들림 지속 시간"
                    );
                    break;

                case CutSceneActionType.SetCameraZoom:
                    DrawProperty(
                        x, ref y, width,
                        action.FindPropertyRelative("_cameraOrthographicSize"),
                        "Camera Orthographic Size"
                    );
                    DrawProperty(
                        x, ref y, width,
                        action.FindPropertyRelative("_duration"),
                        "변환 시간"
                    );
                    break;

                case CutSceneActionType.MoveCamera:
                    DrawMarkerPopup(
                        x,
                        ref y,
                        width,
                        action
                    );

                    DrawDuration(
                        x,
                        ref y,
                        width,
                        action
                    );

                    DrawProperty(
                        x,
                        ref y,
                        width,
                        action.FindPropertyRelative(
                            "_movementCurve"
                        ),
                        "Movement Curve"
                    );
                    break;
            }
        }

        private float GetActionHeight(SerializedProperty action)
        {
            CutSceneActionType type =
                (CutSceneActionType)action
                    .FindPropertyRelative("_actionType")
                    .enumValueIndex;

            int lines = 2;

            switch (type)
            {
                case CutSceneActionType.Wait:
                    lines += 2;
                    break;

                case CutSceneActionType.MoveActor:
                    lines += 8;

                    if (action.FindPropertyRelative(
                            "_controlMoveAnimation").boolValue)
                    {
                        lines += 1;
                    }
                    break;

                case CutSceneActionType.TeleportActor:
                    lines += 4;
                    break;

                case CutSceneActionType.RestoreActorPosition:
                    lines += 1;
                    break;

                case CutSceneActionType.SetAnimationBool:
                    lines += 3;
                    break;

                case CutSceneActionType.SetAnimationTrigger:
                    lines += 4;
                    break;

                case CutSceneActionType.PlayAnimationState:
                    lines += 3;
                    break;

                case CutSceneActionType.PlayConversation:
                    lines += 1;
                    break;

                case CutSceneActionType.SetActorActive:
                    lines += 2;
                    break;

                case CutSceneActionType.SetGameObjectActive:
                    lines += 2;
                    break;

                case CutSceneActionType.InvokeSignal:
                    lines += 3;
                    break;
                case CutSceneActionType.SetCameraMonochrome:
                    lines += 3;
                    break;
                case CutSceneActionType.ShakeCamera:
                    lines += 2;
                    break;
                case CutSceneActionType.SetCameraZoom:
                    lines += 3;
                    break;
                case CutSceneActionType.MoveCamera:
                    lines += 4;
                    break;
            }

            return lines *
                   (EditorGUIUtility.singleLineHeight +
                    EditorGUIUtility.standardVerticalSpacing) +
                   8f;
        }

        private void DrawActorPopup(
            float x,
            ref float y,
            float width,
            SerializedProperty action)
        {
            SerializedProperty actorID =
                action.FindPropertyRelative("_actorID");

            Rect rect = NextRect(x, ref y, width);

            // 씬에 등록된 Actor가 없으면
            // 직접 Actor ID를 입력할 수 있게 합니다.
            if (_actorIDs == null ||
                _actorIDs.Length == 0 ||
                (_actorIDs.Length == 1 &&
                 _actorIDs[0].StartsWith("(")))
            {
                EditorGUI.PropertyField(
                    rect,
                    actorID,
                    new GUIContent("Actor ID")
                );

                return;
            }

            int current = FindIndex(
                _actorIDs,
                actorID.stringValue
            );

            int selected = EditorGUI.Popup(
                rect,
                "Actor",
                current,
                _actorIDs
            );

            if (selected >= 0 &&
                selected < _actorIDs.Length)
            {
                actorID.stringValue =
                    _actorIDs[selected];
            }
        }

        private void DrawMarkerPopup(
            float x,
            ref float y,
            float width,
            SerializedProperty action)
        {
            SerializedProperty markerID =
                action.FindPropertyRelative("_markerID");

            Rect rect = NextRect(x, ref y, width);

            // 씬에 사용할 수 있는 Marker가 없으면
            // Popup을 그리지 않고 문자열 입력 필드만 표시합니다.
            if (_markerIDs == null ||
                _markerIDs.Length == 0 ||
                (_markerIDs.Length == 1 &&
                 _markerIDs[0].StartsWith("(")))
            {
                EditorGUI.PropertyField(
                    rect,
                    markerID,
                    new GUIContent("Marker ID")
                );

                return;
            }

            int current = FindIndex(
                _markerIDs,
                markerID.stringValue
            );

            int selected = EditorGUI.Popup(
                rect,
                "Destination Marker",
                current,
                _markerIDs
            );

            if (selected >= 0 &&
                selected < _markerIDs.Length)
            {
                markerID.stringValue =
                    _markerIDs[selected];
            }
        }

        private void DrawSceneObjectPopup(
    float x,
    ref float y,
    float width,
    SerializedProperty action)
        {
            SerializedProperty objectID =
                action.FindPropertyRelative(
                    "_sceneObjectID"
                );

            Rect rect =
                NextRect(
                    x,
                    ref y,
                    width
                );

            if (_sceneObjectIDs == null ||
                _sceneObjectIDs.Length == 0 ||
                (_sceneObjectIDs.Length == 1 &&
                 _sceneObjectIDs[0].StartsWith("(")))
            {
                EditorGUI.PropertyField(
                    rect,
                    objectID,
                    new GUIContent(
                        "Scene Object ID"
                    )
                );

                return;
            }

            int current =
                FindIndex(
                    _sceneObjectIDs,
                    objectID.stringValue
                );

            int selected =
                EditorGUI.Popup(
                    rect,
                    "Scene Object",
                    current,
                    _sceneObjectIDs
                );

            if (selected >= 0 &&
                selected < _sceneObjectIDs.Length)
            {
                objectID.stringValue =
                    _sceneObjectIDs[selected];
            }
        }

        private void DrawAnimatorParameterPopup(
            float x,
            ref float y,
            float width,
            SerializedProperty action,
            string propertyName,
            UnityEngine.AnimatorControllerParameterType parameterType,
            string label)
        {
            SerializedProperty actorID =
                action.FindPropertyRelative("_actorID");

            SerializedProperty parameter =
                action.FindPropertyRelative(propertyName);

            CutSceneActor actor = FindObjectsByType<CutSceneActor>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(item => item.ActorID == actorID.stringValue);

            string[] names = actor != null && actor.Animator != null
                ? actor.Animator.parameters
                    .Where(item => item.type == parameterType)
                    .Select(item => item.name)
                    .ToArray()
                : Array.Empty<string>();

            Rect rect = NextRect(x, ref y, width);

            if (names.Length == 0)
            {
                EditorGUI.PropertyField(
                    rect,
                    parameter,
                    new GUIContent(label)
                );
                return;
            }

            int current = FindIndex(names, parameter.stringValue);

            int selected = EditorGUI.Popup(
                rect,
                label,
                current,
                names
            );

            parameter.stringValue = names[selected];
        }

        private void DrawDuration(
            float x,
            ref float y,
            float width,
            SerializedProperty action)
        {
            DrawProperty(
                x, ref y, width,
                action.FindPropertyRelative("_duration"),
                "Duration"
            );

            DrawProperty(
                x, ref y, width,
                action.FindPropertyRelative("_useUnscaledTime"),
                "Use Unscaled Time"
            );
        }

        private void DrawProperty(
            float x,
            ref float y,
            float width,
            SerializedProperty property,
            string label)
        {
            EditorGUI.PropertyField(
                NextRect(x, ref y, width),
                property,
                new GUIContent(label),
                true
            );
        }

        private static Rect NextRect(
            float x,
            ref float y,
            float width)
        {
            Rect rect = new Rect(
                x,
                y,
                width,
                EditorGUIUtility.singleLineHeight
            );

            y += EditorGUIUtility.singleLineHeight +
                 EditorGUIUtility.standardVerticalSpacing;

            return rect;
        }

        private void AddAction(
            SerializedProperty actions,
            CutSceneActionType type)
        {
            int index = actions.arraySize;
            actions.InsertArrayElementAtIndex(index);

            SerializedProperty action =
                actions.GetArrayElementAtIndex(index);

            action.FindPropertyRelative("_actionName").stringValue =
                type.ToString();

            action.FindPropertyRelative("_actionType").enumValueIndex =
                (int)type;

            action.FindPropertyRelative("_waitForCompletion").boolValue =
                type == CutSceneActionType.Wait ||
                type == CutSceneActionType.MoveActor ||
                type == CutSceneActionType.PlayConversation ||
                type == CutSceneActionType.SetAnimationTrigger ||
                type == CutSceneActionType.MoveCamera;

            action.FindPropertyRelative("_duration").floatValue = 1f;
            action.FindPropertyRelative("_useMarker").boolValue = true;
            action.FindPropertyRelative("_controlMoveAnimation").boolValue = true;
            action.FindPropertyRelative("_moveAnimationBool").stringValue = "IsMove";
            action.FindPropertyRelative("_faceMovementDirection").boolValue = true;
            action.FindPropertyRelative("_activeValue").boolValue = true;
            action.FindPropertyRelative("_movementCurve").animationCurveValue =
                AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }

        private void FocusStepSceneObjects(SerializedProperty step)
        {
            SerializedProperty actions =
                step.FindPropertyRelative("_actions");

            List<GameObject> objects = new();

            for (int i = 0; i < actions.arraySize; i++)
            {
                SerializedProperty action =
                    actions.GetArrayElementAtIndex(i);

                string actorID =
                    action.FindPropertyRelative("_actorID").stringValue;

                string markerID =
                    action.FindPropertyRelative("_markerID").stringValue;

                string sceneObjectID =
                    action.FindPropertyRelative("_sceneObjectID").stringValue;


                CutSceneActor actor = FindObjectsByType<CutSceneActor>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(item => item.ActorID == actorID);

                CutSceneMarker marker = FindObjectsByType<CutSceneMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(item => item.MarkerID == markerID);

                CutSceneObject sceneObject = FindObjectsByType<CutSceneObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(item => item.ObjectID == sceneObjectID);

                if (actor != null)
                {
                    objects.Add(actor.gameObject);
                }

                if (marker != null)
                {
                    objects.Add(marker.gameObject);
                }

                if (sceneObject != null)
                {
                    objects.Add(sceneObject.gameObject);
                }
            }

            Selection.objects = objects
                .Distinct()
                .Cast<UnityEngine.Object>()
                .ToArray();

            if (Selection.activeGameObject != null)
            {
                SceneView.lastActiveSceneView?.FrameSelected();
            }
        }

        private static void DuplicateArrayElement(
            SerializedProperty array,
            int index)
        {
            array.InsertArrayElementAtIndex(index);
            array.MoveArrayElement(index, index + 1);
        }

        private void RefreshSceneOptions()
        {
            _actorIDs =
                FindObjectsByType<CutSceneActor>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None)
                    .Where(item =>
                        !string.IsNullOrWhiteSpace(
                            item.ActorID))
                    .Select(item => item.ActorID)
                    .Distinct()
                    .OrderBy(item => item)
                    .DefaultIfEmpty("(Actor 없음)")
                    .ToArray();

            _markerIDs =
                FindObjectsByType<CutSceneMarker>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None)
                    .Where(item =>
                        !string.IsNullOrWhiteSpace(
                            item.MarkerID))
                    .Select(item => item.MarkerID)
                    .Distinct()
                    .OrderBy(item => item)
                    .DefaultIfEmpty("(Marker 없음)")
                    .ToArray();

            _sceneObjectIDs =
                FindObjectsByType<CutSceneObject>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None)
                    .Where(item =>
                        !string.IsNullOrWhiteSpace(
                            item.ObjectID))
                    .Select(item => item.ObjectID)
                    .Distinct()
                    .OrderBy(item => item)
                    .DefaultIfEmpty(
                        "(Scene Object 없음)")
                    .ToArray();
        }

        private static int FindIndex(
            string[] values,
            string target)
        {
            if (values == null || values.Length == 0)
            {
                return 0;
            }

            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == target)
                {
                    return i;
                }
            }

            return 0;
        }

        private void SetData(CutSceneData data)
        {
            _data = data;
            _serializedData = null;
            _stepsProperty = null;
            _stepList = null;
            _actionLists.Clear();
        }

        private void EnsureSerializedData()
        {
            if (_serializedData != null &&
                _serializedData.targetObject == _data)
            {
                return;
            }

            _serializedData = new SerializedObject(_data);
            _stepsProperty = _serializedData.FindProperty("_steps");

            BuildStepList();
        }

        private void RebuildLists()
        {
            _serializedData.ApplyModifiedProperties();
            _actionLists.Clear();
            BuildStepList();
            Repaint();
        }

        private void CreateNewData()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create CutSceneData",
                "NewCutSceneData",
                "asset",
                "저장 위치를 선택하세요."
            );

            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            CutSceneData data =
                CreateInstance<CutSceneData>();

            AssetDatabase.CreateAsset(data, path);
            AssetDatabase.SaveAssets();

            Selection.activeObject = data;
            SetData(data);
        }
    }
}

#endif
