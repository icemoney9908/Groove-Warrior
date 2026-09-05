using UnityEngine;
using YWJ.GameLogic;

namespace YWJ.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class TabSoundPlayer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private TabSuccessChecker _tabSuccessChecker;

        [SerializeField]
        private AudioSource _audioSource;

        [Header("Sounds")]
        [SerializeField]
        private AudioClip _tabSuccessClip;

        [SerializeField]
        private AudioClip _tabFailClip;

        [Range(0f, 1f)]
        [SerializeField]
        private float _volume = 1f;

        private void Awake()
        {
            FindReferences();

            if (_audioSource != null)
            {
                _audioSource.playOnAwake = false;
                _audioSource.loop = false;
            }
        }

        private void OnEnable()
        {
            if (_tabSuccessChecker != null)
            {
                _tabSuccessChecker.TabSuccess +=
                    HandleTabSuccess;

                _tabSuccessChecker.TabFail +=
                    HandleTabFail;
            }
        }

        private void OnDisable()
        {
            if (_tabSuccessChecker != null)
            {
                _tabSuccessChecker.TabSuccess -=
                    HandleTabSuccess;

                _tabSuccessChecker.TabFail -=
                    HandleTabFail;
            }
        }

        private void FindReferences()
        {
            if (_tabSuccessChecker == null)
            {
                _tabSuccessChecker =
                    FindFirstObjectByType<TabSuccessChecker>();
            }

            if (_audioSource == null)
            {
                _audioSource =
                    GetComponent<AudioSource>();
            }
        }

        private void HandleTabSuccess()
        {
            if (_tabSuccessClip == null)
            {
                return;
            }

            PlaySound(_tabSuccessClip);
        }


        private void HandleTabFail()
        {
            if (_tabFailClip == null)
            {
                return;
            }

            PlaySound(_tabFailClip);
        }

        private void PlaySound(AudioClip clip)
        {
            if (_audioSource == null ||
                clip == null)
            {
                return;
            }

            _audioSource.PlayOneShot(
                clip,
                _volume);
        }
    }
}