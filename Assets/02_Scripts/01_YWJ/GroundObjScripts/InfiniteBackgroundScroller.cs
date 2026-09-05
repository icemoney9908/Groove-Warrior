using System;
using System.Collections.Generic;
using UnityEngine;

namespace YWJ.Background
{
    /// <summary>
    /// 여러 단계의 배경을 같은 위치에 겹쳐 생성합니다.
    ///
    /// 게임 중에는 자동으로 왼쪽 스크롤하고,
    /// 카메라 이동 시에는 카메라 이동량에 따라 패럴랙스를 적용합니다.
    ///
    /// 콤보에 따라 다음 배경 단계의 Alpha가 증가하여
    /// Background 1 → Background 2 → Background 3 순서로 전환됩니다.
    /// </summary>
    public class InfiniteBackgroundScroller : MonoBehaviour
    {
        [Serializable]
        private sealed class BackgroundStageSetting
        {
            [Tooltip("이 단계에서 사용할 배경 프리팹")]
            [SerializeField]
            private SpriteRenderer _backgroundPrefab;

            [Tooltip("이 단계의 배경이 완전히 표시되는 콤보")]
            [Min(0)]
            [SerializeField]
            private int _requiredCombo;

            public SpriteRenderer BackgroundPrefab =>
                _backgroundPrefab;

            public int RequiredCombo =>
                _requiredCombo;
        }

        private sealed class BackgroundTile
        {
            public readonly List<SpriteRenderer> Renderers = new();

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
                    Transform tileTransform = Transform;

                    return tileTransform != null
                        ? tileTransform.position.x
                        : 0f;
                }
            }

            public void Move(Vector3 movement)
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

            public void SetPosition(Vector3 position)
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

                Color color = renderer.color;

                renderer.color = new Color(
                    color.r,
                    color.g,
                    color.b,
                    Mathf.Clamp01(alpha)
                );
            }
        }

        [Header("Scroll State")]
        [SerializeField]
        private bool _isScrolling;

        [Header("Background Stages")]
        [Tooltip(
            "콤보 순서대로 표시할 배경을 등록합니다.\n" +
            "Required Combo는 오름차순으로 설정하세요."
        )]
        [SerializeField]
        private List<BackgroundStageSetting> _backgroundStages = new();

        [Tooltip(
            "다음 단계의 배경이 이전 단계보다 앞에 표시되도록 " +
            "Sorting Order에 더할 간격"
        )]
        [Min(1)]
        [SerializeField]
        private int _sortingOrderStep = 1;

        [Header("Tile")]
        [Min(2)]
        [SerializeField]
        private int _backgroundCount = 3;

        [Tooltip("카메라 좌우에 추가로 생성할 타일 개수")]
        [Min(1)]
        [SerializeField]
        private int _extraBackgroundCountEachSide = 4;

        [Header("Gameplay Scroll")]
        [Tooltip("게임 플레이 중 배경이 왼쪽으로 이동하는 속도")]
        [Min(0f)]
        [SerializeField]
        private float _scrollSpeed = 1.5f;

        [Header("Camera Parallax")]
        [Tooltip(
            "카메라 이동량 중 배경이 따라가는 비율입니다.\n" +
            "0: 월드에 완전히 고정\n" +
            "1: 카메라와 동일하게 이동"
        )]
        [Range(0f, 1f)]
        [SerializeField]
        private float _cameraFollowRatio = 0.8f;

        [Tooltip("카메라의 세로 이동에도 패럴랙스를 적용합니다.")]
        [SerializeField]
        private bool _applyVerticalParallax;

        [Header("Transition")]
        [Tooltip("배경 Alpha가 목표값까지 변하는 속도")]
        [Min(0f)]
        [SerializeField]
        private float _alphaChangeSpeed = 1.5f;

        [Header("Position")]
        [SerializeField]
        private float _backgroundY;

        [SerializeField]
        private float _backgroundZ;

        [Tooltip("배경 타일 사이 간격 보정값")]
        [SerializeField]
        private float _spacingOffset;

        [Header("Camera")]
        [SerializeField]
        private Camera _targetCamera;

        [Header("Time")]
        [SerializeField]
        private bool _useUnscaledTime;

        [Header("Debug")]
        [SerializeField]
        private bool _showDebugLog;

        private readonly List<BackgroundTile> _backgroundTiles = new();
        private readonly List<float> _currentStageAlphas = new();
        private readonly List<float> _targetStageAlphas = new();

        private float _backgroundWidth;
        private Vector3 _previousCameraPosition;
        private int _currentCombo;
        private bool _isInitialized;

        public bool IsScrolling =>
            _isScrolling;

        public int CurrentCombo =>
            _currentCombo;

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            CacheCameraPosition();
        }

        private void LateUpdate()
        {
            if (!_isInitialized)
                return;

            /*
             * 카메라 패럴랙스는 항상 검사합니다.
             * 게임 중 카메라가 고정되어 있다면 이동량은 0입니다.
             */
            ApplyCameraParallax();

            if (_isScrolling)
            {
                ApplyGameplayScroll();
            }

            UpdateStageAlphas();
            RecycleBackgrounds();
        }

        // ========================================
        // Initialization
        // ========================================

        private void Initialize()
        {
            if (_backgroundStages == null ||
                _backgroundStages.Count == 0)
            {
                Debug.LogError(
                    "[InfiniteBackgroundScroller] " +
                    "Background Stage가 설정되지 않았습니다.",
                    this
                );

                enabled = false;
                return;
            }

            RemoveInvalidStages();

            if (_backgroundStages.Count == 0)
            {
                Debug.LogError(
                    "[InfiniteBackgroundScroller] " +
                    "유효한 Background Prefab이 없습니다.",
                    this
                );

                enabled = false;
                return;
            }

            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }

            if (_targetCamera == null)
            {
                Debug.LogError(
                    "[InfiniteBackgroundScroller] " +
                    "사용할 카메라를 찾을 수 없습니다.",
                    this
                );

                enabled = false;
                return;
            }

            SortBackgroundStages();

            _backgroundWidth =
                _backgroundStages[0]
                    .BackgroundPrefab
                    .bounds
                    .size
                    .x +
                _spacingOffset;

            if (_backgroundWidth <= 0f)
            {
                Debug.LogError(
                    "[InfiniteBackgroundScroller] " +
                    "배경 스프라이트의 너비가 올바르지 않습니다.",
                    this
                );

                enabled = false;
                return;
            }

            ValidateBackgroundStageSizes();
            InitializeAlphaLists();
            CreateBackgrounds();

            CacheCameraPosition();

            _isInitialized = true;

            SetComboImmediately(0);
        }

        private void RemoveInvalidStages()
        {
            _backgroundStages.RemoveAll(
                stage =>
                    stage == null ||
                    stage.BackgroundPrefab == null
            );
        }

        private void SortBackgroundStages()
        {
            _backgroundStages.Sort(
                (left, right) =>
                    left.RequiredCombo.CompareTo(
                        right.RequiredCombo
                    )
            );
        }

        private void ValidateBackgroundStageSizes()
        {
            for (int i = 1;
                 i < _backgroundStages.Count;
                 i++)
            {
                float stageWidth =
                    _backgroundStages[i]
                        .BackgroundPrefab
                        .bounds
                        .size
                        .x +
                    _spacingOffset;

                if (Mathf.Approximately(
                        _backgroundWidth,
                        stageWidth))
                {
                    continue;
                }

                Debug.LogWarning(
                    "[InfiniteBackgroundScroller] " +
                    $"Stage {i} 배경의 너비가 첫 번째 배경과 다릅니다. " +
                    "Sprite 크기, Pixels Per Unit, Scale을 확인하세요. " +
                    $"Base={_backgroundWidth:F3}, " +
                    $"Stage{i}={stageWidth:F3}",
                    this
                );
            }
        }

        private void InitializeAlphaLists()
        {
            _currentStageAlphas.Clear();
            _targetStageAlphas.Clear();

            for (int i = 0;
                 i < _backgroundStages.Count;
                 i++)
            {
                _currentStageAlphas.Add(0f);
                _targetStageAlphas.Add(0f);
            }
        }

        // ========================================
        // Tile Creation
        // ========================================

        private void CreateBackgrounds()
        {
            float cameraLeft =
                GetCameraLeftPosition();

            float firstBackgroundX =
                cameraLeft +
                (_backgroundWidth * 0.5f) -
                (_backgroundWidth *
                 _extraBackgroundCountEachSide);

            int totalBackgroundCount =
                _backgroundCount +
                (_extraBackgroundCountEachSide * 2);

            for (int tileIndex = 0;
                 tileIndex < totalBackgroundCount;
                 tileIndex++)
            {
                float backgroundX =
                    firstBackgroundX +
                    (_backgroundWidth * tileIndex);

                Vector3 position = new Vector3(
                    backgroundX,
                    _backgroundY,
                    _backgroundZ
                );

                BackgroundTile tile =
                    CreateBackgroundTile(
                        tileIndex,
                        position
                    );

                _backgroundTiles.Add(tile);
            }
        }

        private BackgroundTile CreateBackgroundTile(
            int tileIndex,
            Vector3 position)
        {
            BackgroundTile tile = new();

            SpriteRenderer firstPrefab =
                _backgroundStages[0]
                    .BackgroundPrefab;

            int baseSortingLayerID =
                firstPrefab.sortingLayerID;

            int baseSortingOrder =
                firstPrefab.sortingOrder;

            for (int stageIndex = 0;
                 stageIndex < _backgroundStages.Count;
                 stageIndex++)
            {
                BackgroundStageSetting stage =
                    _backgroundStages[stageIndex];

                SpriteRenderer renderer =
                    Instantiate(
                        stage.BackgroundPrefab,
                        position,
                        Quaternion.identity,
                        transform
                    );

                renderer.name =
                    $"Background_{tileIndex}_Stage_{stageIndex}";

                /*
                 * 모든 단계가 같은 Sorting Layer를 사용하며,
                 * 높은 단계일수록 앞에 렌더링됩니다.
                 */
                renderer.sortingLayerID =
                    baseSortingLayerID;

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

                tile.Renderers.Add(renderer);
            }

            return tile;
        }

        private static void SetRendererAlpha(
            SpriteRenderer renderer,
            float alpha)
        {
            if (renderer == null)
                return;

            Color color = renderer.color;

            renderer.color = new Color(
                color.r,
                color.g,
                color.b,
                Mathf.Clamp01(alpha)
            );
        }

        // ========================================
        // Camera Parallax
        // ========================================

        private void ApplyCameraParallax()
        {
            Vector3 currentCameraPosition =
                _targetCamera.transform.position;

            Vector3 cameraDelta =
                currentCameraPosition -
                _previousCameraPosition;

            Vector3 backgroundMovement = new Vector3(
                cameraDelta.x *
                _cameraFollowRatio,

                _applyVerticalParallax
                    ? cameraDelta.y *
                      _cameraFollowRatio
                    : 0f,

                0f
            );

            MoveAllBackgrounds(
                backgroundMovement
            );

            _previousCameraPosition =
                currentCameraPosition;
        }

        // ========================================
        // Gameplay Scroll
        // ========================================

        private void ApplyGameplayScroll()
        {
            float moveDistance =
                _scrollSpeed *
                GetDeltaTime();

            if (moveDistance <= 0f)
                return;

            MoveAllBackgrounds(
                Vector3.left *
                moveDistance
            );
        }

        private void MoveAllBackgrounds(
            Vector3 movement)
        {
            if (movement.sqrMagnitude <=
                Mathf.Epsilon)
            {
                return;
            }

            for (int i = 0;
                 i < _backgroundTiles.Count;
                 i++)
            {
                _backgroundTiles[i]?.Move(
                    movement
                );
            }
        }

        // ========================================
        // Recycling
        // ========================================

        private void RecycleBackgrounds()
        {
            float cameraLeft =
                GetCameraLeftPosition();

            float cameraRight =
                GetCameraRightPosition();

            /*
             * 왼쪽으로 벗어난 타일을 오른쪽 끝으로 보냅니다.
             */
            for (int i = 0;
                 i < _backgroundTiles.Count;
                 i++)
            {
                BackgroundTile tile =
                    _backgroundTiles[i];

                if (tile == null ||
                    tile.Transform == null)
                {
                    continue;
                }

                float tileRight =
                    tile.PositionX +
                    (_backgroundWidth * 0.5f);

                if (tileRight < cameraLeft)
                {
                    MoveBackgroundToRightEnd(
                        tile
                    );
                }
            }

            /*
             * 컷씬에서 카메라가 왼쪽으로 이동할 경우,
             * 오른쪽으로 벗어난 타일을 왼쪽 끝으로 보냅니다.
             */
            for (int i = 0;
                 i < _backgroundTiles.Count;
                 i++)
            {
                BackgroundTile tile =
                    _backgroundTiles[i];

                if (tile == null ||
                    tile.Transform == null)
                {
                    continue;
                }

                float tileLeft =
                    tile.PositionX -
                    (_backgroundWidth * 0.5f);

                if (tileLeft > cameraRight)
                {
                    MoveBackgroundToLeftEnd(
                        tile
                    );
                }
            }
        }

        private void MoveBackgroundToRightEnd(
            BackgroundTile tile)
        {
            float rightmostX =
                GetRightmostBackgroundPositionX(
                    tile
                );

            Vector3 position =
                tile.Transform.position;

            position.x =
                rightmostX +
                _backgroundWidth;

            tile.SetPosition(position);
        }

        private void MoveBackgroundToLeftEnd(
            BackgroundTile tile)
        {
            float leftmostX =
                GetLeftmostBackgroundPositionX(
                    tile
                );

            Vector3 position =
                tile.Transform.position;

            position.x =
                leftmostX -
                _backgroundWidth;

            tile.SetPosition(position);
        }

        private float GetRightmostBackgroundPositionX(
            BackgroundTile excludedTile)
        {
            float rightmostX =
                float.MinValue;

            for (int i = 0;
                 i < _backgroundTiles.Count;
                 i++)
            {
                BackgroundTile tile =
                    _backgroundTiles[i];

                if (tile == null ||
                    tile == excludedTile ||
                    tile.Transform == null)
                {
                    continue;
                }

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

        private float GetLeftmostBackgroundPositionX(
            BackgroundTile excludedTile)
        {
            float leftmostX =
                float.MaxValue;

            for (int i = 0;
                 i < _backgroundTiles.Count;
                 i++)
            {
                BackgroundTile tile =
                    _backgroundTiles[i];

                if (tile == null ||
                    tile == excludedTile ||
                    tile.Transform == null)
                {
                    continue;
                }

                leftmostX =
                    Mathf.Min(
                        leftmostX,
                        tile.PositionX
                    );
            }

            return leftmostX ==
                   float.MaxValue
                ? excludedTile.PositionX
                : leftmostX;
        }

        // ========================================
        // Combo Transition
        // ========================================

        public void SetCombo(int combo)
        {
            _currentCombo =
                Mathf.Max(
                    0,
                    combo
                );

            CalculateTargetStageAlphas(
                _currentCombo
            );

            if (_showDebugLog)
            {
                Debug.Log(
                    "[InfiniteBackgroundScroller] " +
                    $"Combo={_currentCombo}",
                    this
                );
            }
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

            if (_backgroundStages.Count == 1)
            {
                _targetStageAlphas[0] = 1f;
                return;
            }

            /*
             * 첫 번째 배경의 Required Combo보다도 낮은 경우
             * 첫 번째 배경을 표시합니다.
             */
            if (combo <
                _backgroundStages[0].RequiredCombo)
            {
                _targetStageAlphas[0] = 1f;
                return;
            }

            int lowerStageIndex = 0;

            for (int i = 0;
                 i < _backgroundStages.Count;
                 i++)
            {
                if (combo >=
                    _backgroundStages[i]
                        .RequiredCombo)
                {
                    lowerStageIndex = i;
                }
                else
                {
                    break;
                }
            }

            /*
             * 마지막 배경 단계에 도달한 경우
             */
            if (lowerStageIndex >=
                _backgroundStages.Count - 1)
            {
                _targetStageAlphas[
                    _backgroundStages.Count - 1
                ] = 1f;

                return;
            }

            int upperStageIndex =
                lowerStageIndex + 1;

            int lowerCombo =
                _backgroundStages[
                    lowerStageIndex
                ].RequiredCombo;

            int upperCombo =
                _backgroundStages[
                    upperStageIndex
                ].RequiredCombo;

            float transition =
                Mathf.InverseLerp(
                    lowerCombo,
                    upperCombo,
                    combo
                );

            /*
             * 현재 단계는 완전히 표시하고,
             * 다음 단계의 Alpha를 콤보 비율만큼 증가시킵니다.
             *
             * 다음 단계는 Sorting Order가 높으므로
             * 현재 단계 위로 자연스럽게 나타납니다.
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
                float previousAlpha =
                    _currentStageAlphas[
                        stageIndex
                    ];

                float targetAlpha =
                    _targetStageAlphas[
                        stageIndex
                    ];

                float currentAlpha =
                    Mathf.MoveTowards(
                        previousAlpha,
                        targetAlpha,
                        _alphaChangeSpeed *
                        deltaTime
                    );

                _currentStageAlphas[
                    stageIndex
                ] = currentAlpha;

                if (Mathf.Approximately(
                        previousAlpha,
                        currentAlpha))
                {
                    continue;
                }

                SetStageAlphaToAllTiles(
                    stageIndex,
                    currentAlpha
                );
            }
        }

        private void SetStageAlphaToAllTiles(
            int stageIndex,
            float alpha)
        {
            for (int tileIndex = 0;
                 tileIndex <
                 _backgroundTiles.Count;
                 tileIndex++)
            {
                _backgroundTiles[
                    tileIndex
                ]?.SetStageAlpha(
                    stageIndex,
                    alpha
                );
            }
        }

        /// <summary>
        /// 전환 시간 없이 해당 콤보 상태를 즉시 적용합니다.
        /// </summary>
        public void SetComboImmediately(int combo)
        {
            _currentCombo =
                Mathf.Max(
                    0,
                    combo
                );

            CalculateTargetStageAlphas(
                _currentCombo
            );

            for (int stageIndex = 0;
                 stageIndex <
                 _currentStageAlphas.Count;
                 stageIndex++)
            {
                float alpha =
                    _targetStageAlphas[
                        stageIndex
                    ];

                _currentStageAlphas[
                    stageIndex
                ] = alpha;

                SetStageAlphaToAllTiles(
                    stageIndex,
                    alpha
                );
            }
        }

        public void ResetBackgroundStage(
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
        // Camera Utility
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

        private float GetCameraRightPosition()
        {
            float cameraHalfWidth =
                _targetCamera.orthographicSize *
                _targetCamera.aspect;

            return
                _targetCamera.transform.position.x +
                cameraHalfWidth;
        }

        private void CacheCameraPosition()
        {
            if (_targetCamera == null)
                return;

            _previousCameraPosition =
                _targetCamera.transform.position;
        }

        private float GetDeltaTime()
        {
            return _useUnscaledTime
                ? Time.unscaledDeltaTime
                : Time.deltaTime;
        }

        // ========================================
        // Public Movement Control
        // ========================================

        public void SetIsScrolling(
            bool isScrolling)
        {
            _isScrolling =
                isScrolling;
        }

        public void StartScrolling()
        {
            _isScrolling = true;
        }

        public void StopScrolling()
        {
            _isScrolling = false;
        }

        public void SetScrollSpeed(
            float scrollSpeed)
        {
            _scrollSpeed =
                Mathf.Max(
                    0f,
                    scrollSpeed
                );
        }

        public void SetCameraFollowRatio(
            float followRatio)
        {
            _cameraFollowRatio =
                Mathf.Clamp01(
                    followRatio
                );
        }

        /// <summary>
        /// 카메라가 순간 이동한 직후 호출하면
        /// 순간 이동량이 패럴랙스로 적용되는 것을 막습니다.
        /// </summary>
        public void ResetCameraTracking()
        {
            CacheCameraPosition();
        }

        // ========================================
        // Debug
        // ========================================

        [ContextMenu("Test Background Stage 1")]
        private void TestBackgroundStage1()
        {
            if (_backgroundStages.Count < 1)
                return;

            SetComboImmediately(
                _backgroundStages[0]
                    .RequiredCombo
            );
        }

        [ContextMenu("Test Background Stage 2")]
        private void TestBackgroundStage2()
        {
            if (_backgroundStages.Count < 2)
                return;

            SetComboImmediately(
                _backgroundStages[1]
                    .RequiredCombo
            );
        }

        [ContextMenu("Test Background Stage 3")]
        private void TestBackgroundStage3()
        {
            if (_backgroundStages.Count < 3)
                return;

            SetComboImmediately(
                _backgroundStages[2]
                    .RequiredCombo
            );
        }
    }
}