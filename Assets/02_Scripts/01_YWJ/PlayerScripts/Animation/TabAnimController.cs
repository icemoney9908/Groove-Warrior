using GrooveWarrior.ChartData;
using UnityEngine;

namespace YWJ.Player.Animation
{
    public class TabAnimController : MonoBehaviour
    {
        [Header("Animator")]
        [SerializeField]
        private Animator _tabAnimator;

        private static readonly int Attack1Hash =
            Animator.StringToHash("Attack1");

        private static readonly int Attack2Hash =
            Animator.StringToHash("Attack2");

        private static readonly int Attack3Hash =
            Animator.StringToHash("Attack3");

        private static readonly int Attack4Hash =
            Animator.StringToHash("Attack4");

        private static readonly int DodgeHash =
            Animator.StringToHash("Dodge");

        private static readonly int SlideHash =
            Animator.StringToHash("Slide");

        private static readonly int TumblingHash =
            Animator.StringToHash("Tumbling");

        private readonly int[] _attackAnimationHashes =
        {
            Attack1Hash,
            Attack2Hash,
            Attack3Hash,
            Attack4Hash
        };

        private readonly int[] _obstacleAnimationHashes =
        {
            DodgeHash,
            SlideHash
        };

        /*
         * 그룹별 직전 애니메이션을 따로 저장합니다.
         *
         * 공격 이후 회피가 실행되었다고 해서
         * 이전 공격 기록이 사라지지 않습니다.
         */
        private int _previousAttackIndex = -1;
        private int _previousObstacleIndex = -1;

        private void Awake()
        {
            if (_tabAnimator == null)
            {
                _tabAnimator =
                    GetComponent<Animator>();
            }

            if (_tabAnimator == null)
            {
                Debug.LogError(
                    "[TabAnimController] " +
                    "Animator를 찾을 수 없습니다.",
                    this
                );

                enabled = false;
            }
        }

        public void PlayTabSuccessAnimation(
            RhythmActorKind actorKind)
        {
            if (_tabAnimator == null)
                return;

            ResetAllTriggers();

            switch (actorKind)
            {
                case RhythmActorKind.MONSTER:
                    PlayRandomAttack();
                    break;

                case RhythmActorKind.OBSTACLE:
                    PlayRandomObstacleAction();
                    break;

                case RhythmActorKind.NONE:
                    /*
                     * 연출 대상이 없는 일반 노트라면
                     * 애니메이션을 실행하지 않습니다.
                     */
                    break;

                default:
                    Debug.LogWarning(
                        $"[TabAnimController] " +
                        $"지원하지 않는 ActorKind입니다. " +
                        $"ActorKind={actorKind}",
                        this
                    );
                    break;
            }
        }

        public void PlayTabFailAnimation()
        {
            Debug.Log(
                "[TabAnimController] Tab Fail",
                this
            );

            _tabAnimator.SetTrigger(
                TumblingHash
            );
        }

        private void PlayRandomAttack()
        {
            int selectedIndex =
                GetRandomIndexExceptPrevious(
                    _attackAnimationHashes.Length,
                    _previousAttackIndex
                );

            _tabAnimator.SetTrigger(
                _attackAnimationHashes[selectedIndex]
            );

            _previousAttackIndex =
                selectedIndex;
        }

        private void PlayRandomObstacleAction()
        {
            int selectedIndex =
                GetRandomIndexExceptPrevious(
                    _obstacleAnimationHashes.Length,
                    _previousObstacleIndex
                );

            _tabAnimator.SetTrigger(
                _obstacleAnimationHashes[selectedIndex]
            );

            _previousObstacleIndex =
                selectedIndex;
        }

        private int GetRandomIndexExceptPrevious(
            int animationCount,
            int previousIndex)
        {
            if (animationCount <= 0)
                return -1;

            if (animationCount == 1)
                return 0;

            /*
             * 이전 인덱스를 제외한 0 ~ Count - 2 중
             * 하나를 먼저 선택합니다.
             *
             * 선택된 값이 이전 인덱스 이상이면 1을 더해
             * 이전 인덱스를 건너뜁니다.
             */
            int selectedIndex =
                Random.Range(
                    0,
                    animationCount - 1
                );

            if (selectedIndex >= previousIndex)
            {
                selectedIndex++;
            }

            return selectedIndex;
        }

        private void ResetAllTriggers()
        {
            ResetTriggers(
                _attackAnimationHashes
            );

            ResetTriggers(
                _obstacleAnimationHashes
            );
        }

        private void ResetTriggers(
            int[] triggerHashes)
        {
            for (int i = 0;
                 i < triggerHashes.Length;
                 i++)
            {
                _tabAnimator.ResetTrigger(
                    triggerHashes[i]
                );
            }
        }

        public void SetPlayerRun(bool isRunning)
        {
            if (_tabAnimator == null)
                return;

            _tabAnimator.SetBool("Run", isRunning);
        }
    }
}