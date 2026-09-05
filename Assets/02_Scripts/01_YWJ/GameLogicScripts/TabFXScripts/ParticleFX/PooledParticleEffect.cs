using System.Collections;
using UnityEngine;

namespace YWJ.Effect
{
    /// <summary>
    /// 풀링되는 개별 파티클 이펙트의 재생과 반환을 담당한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PooledParticleEffect : MonoBehaviour
    {
        private ParticleEffectPoolManager _owner;
        private ParticleSystem[] _particleSystems;

        private Coroutine _returnRoutine;

        private string _effectId;
        private bool _isRented;
        private bool _autoReturn;

        public string EffectId => _effectId;
        public bool IsRented => _isRented;

        private void Awake()
        {
            CacheParticleSystems();
        }

        private void OnDisable()
        {
            StopReturnRoutine();
        }

        /// <summary>
        /// 풀 매니저에서 처음 생성할 때 호출한다.
        /// </summary>
        public void Initialize(
            ParticleEffectPoolManager owner,
            string effectId)
        {
            _owner = owner;
            _effectId = effectId;

            CacheParticleSystems();

            SetParticleStopAction();
            ClearParticles();
        }

        /// <summary>
        /// 풀에서 대여하여 파티클을 재생한다.
        /// </summary>
        public void Play(
            Vector3 position,
            Quaternion rotation,
            Transform parent,
            bool autoReturn)
        {
            StopReturnRoutine();

            _isRented = true;
            _autoReturn = autoReturn;

            transform.SetParent(parent, false);
            transform.SetPositionAndRotation(position, rotation);

            gameObject.SetActive(true);

            ClearParticles();

            for (int i = 0; i < _particleSystems.Length; i++)
            {
                ParticleSystem particle = _particleSystems[i];

                if (particle == null)
                {
                    continue;
                }

                particle.Play(true);
            }

            if (_autoReturn)
            {
                _returnRoutine = StartCoroutine(
                    WaitForParticleEndRoutine()
                );
            }
        }

        /// <summary>
        /// 파티클을 정지한다.
        /// </summary>
        /// <param name="immediate">
        /// true면 즉시 제거하고 반환한다.
        /// false면 이미 생성된 파티클이 사라진 뒤 반환한다.
        /// </param>
        public void Stop(bool immediate)
        {
            if (!_isRented)
            {
                return;
            }

            StopReturnRoutine();

            ParticleSystemStopBehavior stopBehavior =
                immediate
                    ? ParticleSystemStopBehavior
                        .StopEmittingAndClear
                    : ParticleSystemStopBehavior
                        .StopEmitting;

            for (int i = 0; i < _particleSystems.Length; i++)
            {
                ParticleSystem particle = _particleSystems[i];

                if (particle == null)
                {
                    continue;
                }

                particle.Stop(
                    true,
                    stopBehavior
                );
            }

            if (immediate)
            {
                ReturnToPool();
                return;
            }

            _returnRoutine = StartCoroutine(
                WaitForParticleEndRoutine()
            );
        }

        /// <summary>
        /// 파티클을 즉시 초기화하고 풀로 반환한다.
        /// </summary>
        public void ReturnImmediately()
        {
            if (!_isRented)
            {
                return;
            }

            StopReturnRoutine();
            ClearParticles();
            ReturnToPool();
        }

        /// <summary>
        /// 풀에 들어갈 때 호출된다.
        /// </summary>
        public void OnReturnedToPool(Transform poolRoot)
        {
            StopReturnRoutine();

            _isRented = false;
            _autoReturn = false;

            ClearParticles();

            transform.SetParent(poolRoot, false);
            gameObject.SetActive(false);
        }

        private IEnumerator WaitForParticleEndRoutine()
        {
            yield return null;

            while (IsAnyParticleAlive())
            {
                yield return null;
            }

            _returnRoutine = null;
            ReturnToPool();
        }

        private bool IsAnyParticleAlive()
        {
            for (int i = 0; i < _particleSystems.Length; i++)
            {
                ParticleSystem particle = _particleSystems[i];

                if (particle == null)
                {
                    continue;
                }

                if (particle.IsAlive(true))
                {
                    return true;
                }
            }

            return false;
        }

        private void ReturnToPool()
        {
            if (!_isRented)
            {
                return;
            }

            if (_owner == null)
            {
                Debug.LogWarning(
                    $"[{nameof(PooledParticleEffect)}] " +
                    $"PoolManager가 없어 오브젝트를 비활성화합니다.",
                    this
                );

                _isRented = false;
                gameObject.SetActive(false);
                return;
            }

            _owner.Return(this);
        }

        private void CacheParticleSystems()
        {
            _particleSystems =
                GetComponentsInChildren<ParticleSystem>(true);
        }

        private void ClearParticles()
        {
            if (_particleSystems == null)
            {
                return;
            }

            for (int i = 0; i < _particleSystems.Length; i++)
            {
                ParticleSystem particle = _particleSystems[i];

                if (particle == null)
                {
                    continue;
                }

                particle.Stop(
                    true,
                    ParticleSystemStopBehavior
                        .StopEmittingAndClear
                );

                particle.Clear(true);
            }
        }

        private void SetParticleStopAction()
        {
            if (_particleSystems == null)
            {
                return;
            }

            for (int i = 0; i < _particleSystems.Length; i++)
            {
                ParticleSystem particle = _particleSystems[i];

                if (particle == null)
                {
                    continue;
                }

                ParticleSystem.MainModule main =
                    particle.main;

                // 반환은 이 컴포넌트가 직접 감지하므로
                // Unity의 Destroy/Disable 동작을 사용하지 않는다.
                main.stopAction =
                    ParticleSystemStopAction.None;
            }
        }

        private void StopReturnRoutine()
        {
            if (_returnRoutine == null)
            {
                return;
            }

            StopCoroutine(_returnRoutine);
            _returnRoutine = null;
        }
    }
}