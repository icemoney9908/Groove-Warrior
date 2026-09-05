using System;
using System.Collections.Generic;
using UnityEngine;

namespace YWJ.Background
{
    public class InfiniteGroundScroller : MonoBehaviour
    {
        [Serializable]
        private sealed class GroundStageSetting
        {
            [Tooltip("이 단계에서 사용할 바닥 프리팹")]
            [SerializeField]
            private SpriteRenderer _groundPrefab;

            [Tooltip("이 바닥 단계가 완전히 활성화되는 콤보")]
            [Min(0)]
            [SerializeField]
            private int _requiredCombo;

            public SpriteRenderer GroundPrefab =>
                _groundPrefab;

            public int RequiredCombo =>
                _requiredCombo;
        }

        private sealed class GroundTile
        {
            public readonly List<SpriteRenderer> Renderers =
                new();

            public Transform Transform
            {
                get
                {
                    for (int i = 0;
                         i < Renderers.Count;
                         i++)
                    {
                        if (Renderers[i] != null)
                        {
                            return Renderers[i].transform;
                        }
                    }

                    return null;
                }
            }

            public float PositionX
            {
                get
                {
                    Transform tileTransform =
                        Transform;

                    return tileTransform != null
                        ? tileTransform.position.x
                        : 0f;
                }
            }

            public void Move(
                Vector3 movement)
            {
                for (int i = 0;
                     i < Renderers.Count;
                     i++)
                {
                    SpriteRenderer renderer =
                        Renderers[i];

                    if (renderer == null)
                        continue;

                    renderer.transform.position +=
                        movement;
                }
            }

            public void SetPosition(
                Vector3 position)
            {
                for (int i = 0;
                     i < Renderers.Count;
                     i++)
                {
                    SpriteRenderer renderer =
                        Renderers[i];

                    if (renderer == null)
                        continue;

                    renderer.transform.position =
                        position;
                }
            }

            public void SetStageAlpha(
                int stageIndex,
                float alpha)
            {
                if (stageIndex < 0 ||
                    stageIndex >= Renderers.Count)
                {
                    return;
                }

                SpriteRenderer renderer =
                    Renderers[stageIndex];

                if (renderer == null)
                    return;

                Color color =
                    renderer.color;

                color.a =
                    Mathf.Clamp01(alpha);

                renderer.color =
                    color;
            }
        }

        [Header("State")]
        [SerializeField]
        private bool _isMoving;

        [Header("Ground Stages")]
        [Tooltip(
            "콤보 순서에 따라 나타날 바닥 단계를 등록합니다. " +
            "Required Combo는 오름차순으로 설정하세요."
        )]
        [SerializeField]
        private List<GroundStageSetting> _groundStages =
            new();

        [Tooltip(
            "뒤 단계일수록 앞에 표시되도록 더할 Sorting Order 간격"
        )]
        [Min(1)]
        [SerializeField]
        private int _sortingOrderStep = 1;

        [Header("Ground Count")]
        [Min(2)]
        [SerializeField]
        private int _groundCount = 3;

        [Min(0)]
        [SerializeField]
        private int _extraLeftGroundCount = 4;

        [Header("Movement")]
        [Min(0f)]
        [SerializeField]
        private float _moveSpeed = 5f;

        [SerializeField]
        private bool _useUnscaledTime;

        [Header("Transition")]
        [Tooltip("현재 Alpha가 목표값으로 변하는 속도")]
        [Min(0f)]
        [SerializeField]
        private float _alphaChangeSpeed = 2f;

        [Header("Position")]
        [SerializeField]
        private float _groundY;

        [SerializeField]
        private float _groundZ;

        [SerializeField]
        private float _spacingOffset;

        [Header("Camera")]
        [SerializeField]
        private Camera _targetCamera;

        private readonly List<GroundTile> _groundTiles =
            new();

        private readonly List<float> _currentStageAlphas =
            new();

        private readonly List<float> _targetStageAlphas =
            new();

        private float _groundWidth;
        private int _currentCombo;
        private bool _isInitialized;

        private void Awake()
        {
            Initialize();
        }

        private void Update()
        {
            if (!_isInitialized)
                return;

            if (_isMoving)
            {
                MoveGrounds();
                RecycleGrounds();
            }

            UpdateStageAlphas();
        }

        // ========================================
        // Initialization
        // ========================================

        private void Initialize()
        {
            if (_groundStages == null ||
                _groundStages.Count == 0)
            {
                Debug.LogError(
                    "[InfiniteGroundScroller] " +
                    "Ground Stage가 설정되지 않았습니다.",
                    this
                );

                enabled = false;
                return;
            }

            if (_groundStages[0].GroundPrefab == null)
            {
                Debug.LogError(
                    "[InfiniteGroundScroller] " +
                    "첫 번째 Ground Prefab이 없습니다.",
                    this
                );

                enabled = false;
                return;
            }

            if (_targetCamera == null)
            {
                _targetCamera =
                    Camera.main;
            }

            if (_targetCamera == null)
            {
                Debug.LogError(
                    "[InfiniteGroundScroller] " +
                    "카메라를 찾을 수 없습니다.",
                    this
                );

                enabled = false;
                return;
            }

            SortGroundStages();

            _groundWidth =
                _groundStages[0]
                    .GroundPrefab
                    .bounds
                    .size
                    .x +
                _spacingOffset;

            if (_groundWidth <= 0f)
            {
                Debug.LogError(
                    "[InfiniteGroundScroller] " +
                    "바닥 너비가 올바르지 않습니다.",
                    this
                );

                enabled = false;
                return;
            }

            InitializeAlphaLists();
            CreateGrounds();

            SetComboImmediately(0);

            _isInitialized = true;
        }

        private void SortGroundStages()
        {
            _groundStages.Sort(
                (left, right) =>
                    left.RequiredCombo.CompareTo(
                        right.RequiredCombo
                    )
            );
        }

        private void InitializeAlphaLists()
        {
            _currentStageAlphas.Clear();
            _targetStageAlphas.Clear();

            for (int i = 0;
                 i < _groundStages.Count;
                 i++)
            {
                _currentStageAlphas.Add(0f);
                _targetStageAlphas.Add(0f);
            }
        }

        private void CreateGrounds()
        {
            float cameraLeft =
                GetCameraLeftPosition();

            float firstGroundX =
                cameraLeft +
                (_groundWidth * 0.5f) -
                (_groundWidth *
                 _extraLeftGroundCount);

            int totalGroundCount =
                _groundCount +
                _extraLeftGroundCount;

            for (int tileIndex = 0;
                 tileIndex < totalGroundCount;
                 tileIndex++)
            {
                float groundX =
                    firstGroundX +
                    (_groundWidth * tileIndex);

                Vector3 position =
                    new Vector3(
                        groundX,
                        _groundY,
                        _groundZ
                    );

                GroundTile groundTile =
                    CreateGroundTile(
                        tileIndex,
                        position
                    );

                _groundTiles.Add(
                    groundTile
                );
            }
        }

        private GroundTile CreateGroundTile(
            int tileIndex,
            Vector3 position)
        {
            GroundTile groundTile =
                new();

            for (int stageIndex = 0;
                 stageIndex < _groundStages.Count;
                 stageIndex++)
            {
                GroundStageSetting stage =
                    _groundStages[stageIndex];

                if (stage.GroundPrefab == null)
                {
                    Debug.LogError(
                        "[InfiniteGroundScroller] " +
                        $"Stage {stageIndex}의 프리팹이 없습니다.",
                        this
                    );

                    continue;
                }

                SpriteRenderer renderer =
                    Instantiate(
                        stage.GroundPrefab,
                        position,
                        Quaternion.identity,
                        transform
                    );

                renderer.name =
                    $"Ground_{tileIndex}_Stage_{stageIndex}";

                /*
                 * 뒤 단계일수록 앞쪽에 렌더링합니다.
                 */
                int baseSortingOrder =
                    _groundStages[0]
                        .GroundPrefab
                        .sortingOrder;

                renderer.sortingLayerID =
                    _groundStages[0]
                        .GroundPrefab
                        .sortingLayerID;

                renderer.sortingOrder =
                    baseSortingOrder +
                    (stageIndex *
                     _sortingOrderStep);

                SetRendererAlpha(
                    renderer,
                    stageIndex == 0
                        ? 1f
                        : 0f
                );

                groundTile.Renderers.Add(
                    renderer
                );
            }

            return groundTile;
        }

        private static void SetRendererAlpha(
            SpriteRenderer renderer,
            float alpha)
        {
            if (renderer == null)
                return;

            Color color =
                renderer.color;

            color.a =
                Mathf.Clamp01(alpha);

            renderer.color =
                color;
        }

        // ========================================
        // Movement
        // ========================================

        private void MoveGrounds()
        {
            float moveDistance =
                _moveSpeed *
                GetDeltaTime();

            Vector3 movement =
                Vector3.left *
                moveDistance;

            for (int i = 0;
                 i < _groundTiles.Count;
                 i++)
            {
                _groundTiles[i].Move(
                    movement
                );
            }
        }

        private void RecycleGrounds()
        {
            float cameraLeft =
                GetCameraLeftPosition();

            for (int i = 0;
                 i < _groundTiles.Count;
                 i++)
            {
                GroundTile tile =
                    _groundTiles[i];

                float groundRight =
                    tile.PositionX +
                    (_groundWidth * 0.5f);

                if (groundRight < cameraLeft)
                {
                    MoveGroundToRightEnd(
                        tile
                    );
                }
            }
        }

        private void MoveGroundToRightEnd(
            GroundTile tile)
        {
            float rightmostX =
                GetRightmostGroundPositionX(
                    tile
                );

            Vector3 position =
                tile.Transform.position;

            position.x =
                rightmostX +
                _groundWidth;

            tile.SetPosition(
                position
            );
        }

        private float GetRightmostGroundPositionX(
            GroundTile excludedTile)
        {
            float rightmostX =
                float.MinValue;

            for (int i = 0;
                 i < _groundTiles.Count;
                 i++)
            {
                GroundTile tile =
                    _groundTiles[i];

                if (tile == excludedTile)
                    continue;

                rightmostX =
                    Mathf.Max(
                        rightmostX,
                        tile.PositionX
                    );
            }

            return rightmostX ==
                   float.MinValue
                ? excludedTile.PositionX
                : rightmostX;
        }

        // ========================================
        // Combo Transition
        // ========================================

        public void SetCombo(
            int combo)
        {
            _currentCombo =
                Mathf.Max(
                    0,
                    combo
                );

            CalculateTargetStageAlphas(
                _currentCombo
            );
        }

        private void CalculateTargetStageAlphas(
            int combo)
        {
            for (int i = 0;
                 i < _targetStageAlphas.Count;
                 i++)
            {
                _targetStageAlphas[i] = 0f;
            }

            if (_groundStages.Count == 1)
            {
                _targetStageAlphas[0] = 1f;
                return;
            }

            int lowerStageIndex = 0;

            for (int i = 0;
                 i < _groundStages.Count;
                 i++)
            {
                if (combo >=
                    _groundStages[i].RequiredCombo)
                {
                    lowerStageIndex = i;
                }
                else
                {
                    break;
                }
            }

            /*
             * 마지막 단계에 도달한 경우
             */
            if (lowerStageIndex >=
                _groundStages.Count - 1)
            {
                _targetStageAlphas[
                    _groundStages.Count - 1
                ] = 1f;

                return;
            }

            int upperStageIndex =
                lowerStageIndex + 1;

            int lowerCombo =
                _groundStages[
                    lowerStageIndex
                ].RequiredCombo;

            int upperCombo =
                _groundStages[
                    upperStageIndex
                ].RequiredCombo;

            float transition =
                Mathf.InverseLerp(
                    lowerCombo,
                    upperCombo,
                    combo
                );

            /*
             * 아래 단계는 항상 유지하고,
             * 위 단계의 Alpha만 증가시킵니다.
             *
             * Stage 2 Alpha가 1이 되면
             * Stage 1은 아래에 가려지므로 보이지 않습니다.
             */
            _targetStageAlphas[
                lowerStageIndex
            ] = 1f;

            _targetStageAlphas[
                upperStageIndex
            ] = transition;
        }

        private void UpdateStageAlphas()
        {
            float deltaTime =
                GetDeltaTime();

            for (int stageIndex = 0;
                 stageIndex <
                 _currentStageAlphas.Count;
                 stageIndex++)
            {
                _currentStageAlphas[
                    stageIndex
                ] =
                    Mathf.MoveTowards(
                        _currentStageAlphas[
                            stageIndex
                        ],
                        _targetStageAlphas[
                            stageIndex
                        ],
                        _alphaChangeSpeed *
                        deltaTime
                    );

                SetStageAlphaToAllTiles(
                    stageIndex,
                    _currentStageAlphas[
                        stageIndex
                    ]
                );
            }
        }

        private void SetStageAlphaToAllTiles(
            int stageIndex,
            float alpha)
        {
            for (int tileIndex = 0;
                 tileIndex < _groundTiles.Count;
                 tileIndex++)
            {
                _groundTiles[
                    tileIndex
                ].SetStageAlpha(
                    stageIndex,
                    alpha
                );
            }
        }

        public void SetComboImmediately(
            int combo)
        {
            _currentCombo =
                Mathf.Max(
                    0,
                    combo
                );

            CalculateTargetStageAlphas(
                _currentCombo
            );

            for (int i = 0;
                 i < _currentStageAlphas.Count;
                 i++)
            {
                _currentStageAlphas[i] =
                    _targetStageAlphas[i];

                SetStageAlphaToAllTiles(
                    i,
                    _currentStageAlphas[i]
                );
            }
        }

        public void ResetGroundStage(
            bool immediately = true)
        {
            if (immediately)
            {
                SetComboImmediately(0);
            }
            else
            {
                SetCombo(0);
            }
        }

        // ========================================
        // Utility
        // ========================================

        private float GetCameraLeftPosition()
        {
            float cameraHalfWidth =
                _targetCamera.orthographicSize *
                _targetCamera.aspect;

            return
                _targetCamera.transform.position.x -
                cameraHalfWidth;
        }

        private float GetDeltaTime()
        {
            return _useUnscaledTime
                ? Time.unscaledDeltaTime
                : Time.deltaTime;
        }

        // ========================================
        // Public Movement
        // ========================================

        public void SetIsMoving(
            bool isMoving)
        {
            _isMoving =
                isMoving;
        }

        public void StartMoving()
        {
            _isMoving = true;
        }

        public void StopMoving()
        {
            _isMoving = false;
        }

        public void SetMoveSpeed(
            float moveSpeed)
        {
            _moveSpeed =
                Mathf.Max(
                    0f,
                    moveSpeed
                );
        }
    }
}