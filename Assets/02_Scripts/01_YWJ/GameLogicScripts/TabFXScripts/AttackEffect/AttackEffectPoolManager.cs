using System.Collections.Generic;
using UnityEngine;

namespace YWJ.Effect
{
    public sealed class AttackEffectPoolManager :
        MonoBehaviour
    {
        private sealed class EffectPool
        {
            public AttackEffect Prefab;
            public readonly Queue<AttackEffect>
                InactiveEffects = new();
        }

        [Header("Pool")]
        [SerializeField]
        private Transform _poolRoot;

        [SerializeField]
        private List<AttackEffectPoolData>
            _poolSettings = new();

        [Header("Debug")]
        [SerializeField]
        private bool _showDebugLog;

        private readonly Dictionary<
            string,
            EffectPool
        > _pools = new();

        private void Awake()
        {
            InitializePoolRoot();
            InitializePools();
        }

        private void InitializePoolRoot()
        {
            if (_poolRoot != null)
                return;

            GameObject rootObject =
                new GameObject(
                    "AttackSpriteEffectPoolRoot"
                );

            rootObject.transform.SetParent(
                transform,
                false
            );

            _poolRoot =
                rootObject.transform;
        }

        private void InitializePools()
        {
            _pools.Clear();

            for (int i = 0;
                 i < _poolSettings.Count;
                 i++)
            {
                AttackEffectPoolData setting =
                    _poolSettings[i];

                if (setting == null)
                    continue;

                if (string.IsNullOrWhiteSpace(
                        setting.EffectId))
                {
                    Debug.LogError(
                        "[AttackSpriteEffectPoolManager] " +
                        "Effect ID가 비어 있습니다.",
                        this
                    );

                    continue;
                }

                if (setting.Prefab == null)
                {
                    Debug.LogError(
                        "[AttackSpriteEffectPoolManager] " +
                        $"프리팹이 없습니다: " +
                        $"{setting.EffectId}",
                        this
                    );

                    continue;
                }

                if (_pools.ContainsKey(
                        setting.EffectId))
                {
                    Debug.LogError(
                        "[AttackSpriteEffectPoolManager] " +
                        $"중복된 Effect ID입니다: " +
                        $"{setting.EffectId}",
                        this
                    );

                    continue;
                }

                EffectPool pool =
                    new EffectPool
                    {
                        Prefab =
                            setting.Prefab
                    };

                _pools.Add(
                    setting.EffectId,
                    pool
                );

                for (int count = 0;
                     count < setting.InitialSize;
                     count++)
                {
                    CreateEffect(
                        setting.EffectId,
                        pool
                    );
                }
            }
        }

        public AttackEffect Play(
            string effectId,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            Transform parent = null,
            bool flipX = false,
            float duration = -1f)
        {
            AttackEffect effect =
                Rent(effectId);

            if (effect == null)
                return null;

            effect.Play(
                position,
                rotation,
                scale,
                parent,
                flipX,
                duration
            );

            return effect;
        }

        public AttackEffect Play(
            string effectId,
            Transform spawnPoint,
            Vector3 scale,
            bool flipX = false,
            float duration = -1f)
        {
            if (spawnPoint == null)
            {
                Debug.LogError(
                    "[AttackSpriteEffectPoolManager] " +
                    "SpawnPoint가 null입니다.",
                    this
                );

                return null;
            }

            return Play(
                effectId,
                spawnPoint.position,
                spawnPoint.rotation,
                scale,
                parent: null,
                flipX,
                duration
            );
        }

        private AttackEffect Rent(
            string effectId)
        {
            if (!_pools.TryGetValue(
                    effectId,
                    out EffectPool pool))
            {
                Debug.LogError(
                    "[AttackSpriteEffectPoolManager] " +
                    $"등록되지 않은 Effect ID입니다: " +
                    $"{effectId}",
                    this
                );

                return null;
            }

            if (pool.InactiveEffects.Count == 0)
            {
                CreateEffect(
                    effectId,
                    pool
                );
            }

            if (pool.InactiveEffects.Count == 0)
                return null;

            return pool.InactiveEffects.Dequeue();
        }

        private AttackEffect CreateEffect(
            string effectId,
            EffectPool pool)
        {
            AttackEffect effect =
                Instantiate(
                    pool.Prefab,
                    _poolRoot
                );

            effect.Initialize(
                effectId,
                this
            );

            effect.OnReturnedToPool(
                _poolRoot
            );

            pool.InactiveEffects.Enqueue(
                effect
            );

            if (_showDebugLog)
            {
                Debug.Log(
                    "[AttackSpriteEffectPoolManager] " +
                    $"Effect 생성: {effectId}",
                    this
                );
            }

            return effect;
        }

        public void Return(
            AttackEffect effect)
        {
            if (effect == null)
                return;

            if (string.IsNullOrWhiteSpace(
                    effect.EffectId))
            {
                Destroy(
                    effect.gameObject
                );

                return;
            }

            if (!_pools.TryGetValue(
                    effect.EffectId,
                    out EffectPool pool))
            {
                Destroy(
                    effect.gameObject
                );

                return;
            }

            effect.OnReturnedToPool(
                _poolRoot
            );

            pool.InactiveEffects.Enqueue(
                effect
            );
        }
    }
}