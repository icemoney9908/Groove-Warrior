using UnityEngine;


namespace YWJ.GameFlow
{
    [CreateAssetMenu(
        fileName = "CharacterData",
        menuName = "YWJ/GameFlow/Character Data"
    )]
    public class CharacterData : ScriptableObject
    {
        [Header("Character Name")]
        [SerializeField]
        private string _characterName;
        [SerializeField]
        private string _characterID;

        [Header("Character Image")]
        [SerializeField]
        private Sprite _characterImage;

        [Header("Character Prefab")]
        [SerializeField]
        private GameObject _characterPrefab;

        public string CharacterName => _characterName;

        public string CharacterID => _characterID;

        public Sprite CharacterImage => _characterImage;

        public GameObject CharacterPrefab => _characterPrefab;
    }
}