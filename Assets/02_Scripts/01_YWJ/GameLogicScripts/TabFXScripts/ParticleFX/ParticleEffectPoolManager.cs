using System;
using System.Collections.Generic;
using UnityEngine;

namespace YWJ.Effect
{
    /// <summary>
    /// 파티클 이펙트의 생성, 풀링, 재생, 반환을 담당한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ParticleEffectPoolManager : MonoBehaviour
    {
        [Serializable]
        private sealed class ParticlePoolSetting
        {
            [Tooltip("코드에서 이 이펙트를 호출할 때 사용할 고유 ID")]
            [SerializeField]
            private string _effectId;

            [Tooltip("ParticleSystem이 포함된 파티클 프리팹")]
            [SerializeField]
            private PooledParticleEffect _prefab;

            [Tooltip("게임 시작 시 미리 생성할 개수")]
            [Min(0)]
            [SerializeField]
            private int _initialSize = 5;

            [Tooltip("동시에 생성할 수 있는 최대 개수")]
            [Min(1)]
            [SerializeField]
            private int _maxSize = 20;

            [Tooltip("풀이 비었을 때 최대 개수까지 확장할지 여부")]
            [SerializeField]
            private bool _canExpand = true;

            public string EffectId => _effectId;
            public PooledParticleEffect Prefab => _prefab;
            public int InitialSize => _initialSize;
            public int MaxSize => _maxSize;
            public bool CanExpand => _canExpand;
        }

        private sealed class RuntimePool
        {
            public ParticlePoolSetting Setting;

            public readonly Queue<PooledParticleEffect>
                InactiveEffects = new();

            public readonly HashSet<PooledParticleEffect>
                ActiveEffects = new();

            public int CreatedCount;
        }

        [Header("Pool Settings")]
        [SerializeField]
        private List<ParticlePoolSetting> _poolSettings = new();

        [Header("Hierarchy")]
        [Tooltip("반환된 파티클이 배치될 부모. 비워두면 자동 생성한다.")]
        [SerializeField]
        private Transform _poolRoot;

        [Header("Options")]
        [Tooltip("Awake에서 모든 풀을 미리 생성할지 여부")]
        [SerializeField]
        private bool _initializeOnAwake = true;

        private readonly Dictionary<string, RuntimePool>
            _pools = new(StringComparer.Ordinal);

        private bool _isInitialized;

        private void Awake()
        {
            if (_initializeOnAwake)
            {
                Initialize();
            }
        }

        /// <summary>
        /// 설정된 모든 파티클 풀을 초기화한다.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            CreatePoolRoot();

            _pools.Clear();

            for (int i = 0; i < _poolSettings.Count; i++)
            {
                ParticlePoolSetting setting =
                    _poolSettings[i];

                if (!ValidateSetting(setting))
                {
                    continue;
                }

                RuntimePool runtimePool = new()
                {
                    Setting = setting
                };

                _pools.Add(
                    setting.EffectId,
                    runtimePool
                );

                int createCount = Mathf.Min(
                    setting.InitialSize,
                    setting.MaxSize
                );

                for (int j = 0; j < createCount; j++)
                {
                    PooledParticleEffect effect =
                        CreateEffect(runtimePool);

                    if (effect == null)
                    {
                        break;
                    }

                    runtimePool.InactiveEffects.Enqueue(effect);
                }
            }

            _isInitialized = true;
        }

        /// <summary>
        /// 월드 위치에서 파티클을 재생한다.
        /// </summary>
        public PooledParticleEffect Play(
            string effectId,
            Vector3 position)
        {
            return Play(
                effectId,
                position,
                Quaternion.identity,
                null,
                true
            );
        }

        /// <summary>
        /// 월드 위치와 회전을 지정해 파티클을 재생한다.
        /// </summary>
        public PooledParticleEffect Play(
            string effectId,
            Vector3 position,
            Quaternion rotation)
        {
            return Play(
                effectId,
                position,
                rotation,
                null,
                true
            );
        }

        /// <summary>
        /// 파티클을 재생한다.
        /// </summary>
        /// <param name="effectId">등록한 이펙트 ID</param>
        /// <param name="position">생성 위치</param>
        /// <param name="rotation">생성 회전</param>
        /// <param name="parent">파티클이 따라갈 부모</param>
        /// <param name="autoReturn">
        /// 재생이 끝난 후 자동 반환할지 여부
        /// </param>
        public PooledParticleEffect Play(
            string effectId,
            Vector3 position,
            Quaternion rotation,
            Transform parent,
            bool autoReturn = true)
        {
            EnsureInitialized();

            PooledParticleEffect effect =
                Rent(effectId);

            if (effect == null)
            {
                return null;
            }

            effect.Play(
                position,
                rotation,
                parent,
                autoReturn
            );

            return effect;
        }

        /// <summary>
        /// 특정 Transform의 위치에서 파티클을 재생한다.
        /// </summary>
        public PooledParticleEffect PlayAt(
            string effectId,
            Transform target,
            bool followTarget = false,
            bool autoReturn = true)
        {
            if (target == null)
            {
                Debug.LogWarning(
                    $"[{nameof(ParticleEffectPoolManager)}] " +
                    "재생 대상 Transform이 없습니다.",
                    this
                );

                return null;
            }

            Transform parent =
                followTarget ? target : null;

            return Play(
                effectId,
                target.position,
                target.rotation,
                parent,
                autoReturn
            );
        }

        /// <summary>
        /// 대여된 이펙트를 풀에 반환한다.
        /// PooledParticleEffect 내부에서 호출한다.
        /// </summary>
        public void Return(PooledParticleEffect effect)
        {
            if (effect == null)
            {
                return;
            }

            if (!_pools.TryGetValue(
                    effect.EffectId,
                    out RuntimePool runtimePool))
            {
                Debug.LogWarning(
                    $"[{nameof(ParticleEffectPoolManager)}] " +
                    $"등록되지 않은 이펙트가 반환되었습니다. " +
                    $"ID: {effect.EffectId}",
                    effect
                );

                effect.gameObject.SetActive(false);
                return;
            }

            if (!runtimePool.ActiveEffects.Remove(effect))
            {
                return;
            }

            effect.OnReturnedToPool(_poolRoot);

            runtimePool.InactiveEffects.Enqueue(effect);
        }

        /// <summary>
        /// 특정 ID의 현재 활성화된 모든 파티클을 정지한다.
        /// </summary>
        public void StopAll(
            string effectId,
            bool immediate = true)
        {
            EnsureInitialized();

            if (!_pools.TryGetValue(
                    effectId,
                    out RuntimePool runtimePool))
            {
                return;
            }

            PooledParticleEffect[] activeEffects =
                new PooledParticleEffect[
                    runtimePool.ActiveEffects.Count
                ];

            runtimePool.ActiveEffects.CopyTo(
                activeEffects
            );

            for (int i = 0; i < activeEffects.Length; i++)
            {
                PooledParticleEffect effect =
                    activeEffects[i];

                if (effect == null)
                {
                    continue;
                }

                effect.Stop(immediate);
            }
        }

        /// <summary>
        /// 현재 활성화된 모든 파티클을 정지하고 반환한다.
        /// </summary>
        public void StopAll(bool immediate = true)
        {
            EnsureInitialized();

            foreach (RuntimePool runtimePool in _pools.Values)
            {
                PooledParticleEffect[] activeEffects =
                    new PooledParticleEffect[
                        runtimePool.ActiveEffects.Count
                    ];

                runtimePool.ActiveEffects.CopyTo(
                    activeEffects
                );

                for (int i = 0; i < activeEffects.Length; i++)
                {
                    PooledParticleEffect effect =
                        activeEffects[i];

                    if (effect == null)
                    {
                        continue;
                    }

                    effect.Stop(immediate);
                }
            }
        }

        /// <summary>
        /// 풀에서 이펙트 하나를 대여한다.
        /// </summary>
        private PooledParticleEffect Rent(string effectId)
        {
            if (string.IsNullOrWhiteSpace(effectId))
            {
                Debug.LogWarning(
                    $"[{nameof(ParticleEffectPoolManager)}] " +
                    "Effect ID가 비어 있습니다.",
                    this
                );

                return null;
            }

            if (!_pools.TryGetValue(
                    effectId,
                    out RuntimePool runtimePool))
            {
                Debug.LogWarning(
                    $"[{nameof(ParticleEffectPoolManager)}] " +
                    $"등록되지 않은 이펙트입니다. ID: {effectId}",
                    this
                );

                return null;
            }

            PooledParticleEffect effect = null;

            while (runtimePool.InactiveEffects.Count > 0)
            {
                effect =
                    runtimePool.InactiveEffects.Dequeue();

                if (effect != null)
                {
                    break;
                }
            }

            if (effect == null)
            {
                bool canCreate =
                    runtimePool.Setting.CanExpand &&
                    runtimePool.CreatedCount <
                    runtimePool.Setting.MaxSize;

                if (!canCreate)
                {
                    Debug.LogWarning(
                        $"[{nameof(ParticleEffectPoolManager)}] " +
                        $"파티클 풀이 모두 사용 중입니다. " +
                        $"ID: {effectId}",
                        this
                    );

                    return null;
                }

                effect = CreateEffect(runtimePool);
            }

            if (effect == null)
            {
                return null;
            }

            runtimePool.ActiveEffects.Add(effect);

            return effect;
        }

        private PooledParticleEffect CreateEffect(
            RuntimePool runtimePool)
        {
            if (runtimePool.CreatedCount >=
                runtimePool.Setting.MaxSize)
            {
                return null;
            }

            PooledParticleEffect instance =
                Instantiate(
                    runtimePool.Setting.Prefab,
                    _poolRoot
                );

            instance.name =
                $"{runtimePool.Setting.EffectId}_" +
                $"{runtimePool.CreatedCount:00}";

            instance.Initialize(
                this,
                runtimePool.Setting.EffectId
            );

            instance.gameObject.SetActive(false);

            runtimePool.CreatedCount++;

            return instance;
        }

        private bool ValidateSetting(
            ParticlePoolSetting setting)
        {
            if (setting == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(
                    setting.EffectId))
            {
                Debug.LogWarning(
                    $"[{nameof(ParticleEffectPoolManager)}] " +
                    "Effect ID가 비어 있는 설정이 있습니다.",
                    this
                );

                return false;
            }

            if (setting.Prefab == null)
            {
                Debug.LogWarning(
                    $"[{nameof(ParticleEffectPoolManager)}] " +
                    $"파티클 프리팹이 없습니다. " +
                    $"ID: {setting.EffectId}",
                    this
                );

                return false;
            }

            if (_pools.ContainsKey(setting.EffectId))
            {
                Debug.LogWarning(
                    $"[{nameof(ParticleEffectPoolManager)}] " +
                    $"중복된 Effect ID입니다. " +
                    $"ID: {setting.EffectId}",
                    this
                );

                return false;
            }

            return true;
        }

        private void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                Initialize();
            }
        }

        private void CreatePoolRoot()
        {
            if (_poolRoot != null)
            {
                return;
            }

            GameObject rootObject =
                new("ParticleEffectPool");

            rootObject.transform.SetParent(
                transform,
                false
            );

            _poolRoot = rootObject.transform;
        }
    }
}