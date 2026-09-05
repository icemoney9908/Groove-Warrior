using UnityEngine;

namespace YWJ.CutScene
{
    public class CutSceneMarker : MonoBehaviour
    {
        [SerializeField] private string _markerID;

        public string MarkerID => _markerID;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(transform.position, 0.2f);
            Gizmos.DrawLine(
                transform.position + Vector3.left * 0.35f,
                transform.position + Vector3.right * 0.35f
            );
            Gizmos.DrawLine(
                transform.position + Vector3.down * 0.35f,
                transform.position + Vector3.up * 0.35f
            );
        }
#endif
    }
}
