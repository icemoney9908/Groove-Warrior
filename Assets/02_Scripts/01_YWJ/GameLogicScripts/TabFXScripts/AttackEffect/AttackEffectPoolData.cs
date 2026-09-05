using System;
using UnityEngine;

namespace YWJ.Effect
{
    [Serializable]
    public sealed class AttackEffectPoolData
    {
        [SerializeField]
        private string _effectId;

        [SerializeField]
        private AttackEffect _prefab;

        [Min(1)]
        [SerializeField]
        private int _initialSize = 3;

        public string EffectId =>
            _effectId;

        public AttackEffect Prefab =>
            _prefab;

        public int InitialSize =>
            Mathf.Max(
                1,
                _initialSize
            );
    }
}