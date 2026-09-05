using UnityEngine;
using YWJ.GameFlow;

namespace YWJ.CutScene
{
    public class CutSceneActor : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private CharacterData _characterData;
        [SerializeField] private string _actorIDOverride;

        [Header("Components")]
        [SerializeField] private Animator _animator;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        public string ActorID
        {
            get
            {
                if (_characterData != null &&
                    !string.IsNullOrWhiteSpace(_characterData.CharacterID))
                {
                    return _characterData.CharacterID;
                }

                return _actorIDOverride;
            }
        }

        public CharacterData CharacterData => _characterData;
        public Animator Animator => _animator;
        public SpriteRenderer SpriteRenderer => _spriteRenderer;

        private void Reset()
        {
            _animator = GetComponentInChildren<Animator>();
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        public void SetPosition(Vector3 position)
        {
            transform.position = position;
        }

        public void SetFacingRight(bool faceRight)
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.flipX = !faceRight;
            }
        }

        public void SetAnimationBool(string parameterName, bool value)
        {
            if (_animator == null || string.IsNullOrWhiteSpace(parameterName))
            {
                return;
            }

            _animator.SetBool(parameterName, value);
        }

        public void SetAnimationTrigger(string parameterName)
        {
            if (_animator == null || string.IsNullOrWhiteSpace(parameterName))
            {
                return;
            }

            _animator.SetTrigger(parameterName);
        }

        public void PlayAnimationState(string stateName, float normalizedTime = 0f)
        {
            if (_animator == null || string.IsNullOrWhiteSpace(stateName))
            {
                return;
            }

            _animator.Play(stateName, 0, normalizedTime);
        }
    }
}
