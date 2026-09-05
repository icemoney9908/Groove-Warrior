using UnityEngine;
using UnityEngine.Events;

namespace YWJ.CutScene
{
    public class CutSceneObject : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField]
        private string _objectID;

        [Header("Signal")]
        [SerializeField]
        private UnityEvent _onSignalInvoked;

        public string ObjectID => _objectID;

        public void SetActive(bool isActive)
        {
            gameObject.SetActive(isActive);
        }

        public void InvokeSignal()
        {
            _onSignalInvoked?.Invoke();
        }
    }
}