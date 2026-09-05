#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace YWJ.CutScene.Editor
{
    [CustomEditor(typeof(CutSceneMarker))]
    public class CutSceneMarkerEditor : UnityEditor.Editor
    {
        private void OnSceneGUI()
        {
            CutSceneMarker marker = (CutSceneMarker)target;

            EditorGUI.BeginChangeCheck();

            Vector3 newPosition = Handles.PositionHandle(
                marker.transform.position,
                marker.transform.rotation
            );

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(
                    marker.transform,
                    "Move CutScene Marker"
                );

                marker.transform.position = newPosition;
            }

            Handles.Label(
                marker.transform.position + Vector3.up * 0.45f,
                marker.MarkerID
            );
        }
    }
}

#endif
