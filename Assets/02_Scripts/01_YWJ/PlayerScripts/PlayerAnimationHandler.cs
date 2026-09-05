using GrooveWarrior.ChartData;
using UnityEngine;
using YWJ.GameLogic;
using YWJ.GameLogic.Effect;
using YWJ.Player.Animation;

namespace YWJ.Player
{
    public class PlayerAnimationHandler : MonoBehaviour
    {
        [Header("Animate Able")]
        [SerializeField]
        private bool _isAnimateAble;

        [Header("Success Checker")]
        [SerializeField]
        private TabSuccessChecker _tabSuccessChecker;

        [Header("Anim Controller")]
        [SerializeField]
        private TabAnimController _tabAnimController;

        private void Awake()
        {
            if (_tabSuccessChecker == null)
            {
                _tabSuccessChecker =
                    FindFirstObjectByType<TabSuccessChecker>();
            }

            if (_tabSuccessChecker == null)
            {
                Debug.LogError(
                    "[PlayerAnimationHandler] " +
                    "TabSuccessChecker를 찾을 수 없습니다.",
                    this
                );
            }

            if (_tabAnimController == null)
            {
                _tabAnimController =
                    GetComponent<TabAnimController>();
            }

            if (_tabAnimController == null)
            {
                Debug.LogError(
                    "[PlayerAnimationHandler] " +
                    "TabAnimController를 찾을 수 없습니다.",
                    this
                );
            }
        }

        private void OnEnable()
        {
            if (_tabSuccessChecker == null)
                return;

            _tabSuccessChecker.TabAnimationRequested +=
                OnTabAnimationRequested;

            _tabSuccessChecker.TabFail +=
                OnTabFail;
        }

        private void OnDisable()
        {
            if (_tabSuccessChecker == null)
                return;

            _tabSuccessChecker.TabAnimationRequested -=
                OnTabAnimationRequested;

            _tabSuccessChecker.TabFail -=
                OnTabFail;
        }

        private void OnTabAnimationRequested(
            RhythmActorKind actorKind,
            TabEffectType effectType)
        {
            if (!_isAnimateAble)
                return;

            if (_tabAnimController == null)
                return;

            _tabAnimController.PlayTabSuccessAnimation(
                actorKind
            );
        }

        private void OnTabFail()
        {
            if (!_isAnimateAble)
                return;

            if (_tabAnimController == null)
                return;

            _tabAnimController.PlayTabFailAnimation();
        }

        public void EnableAnimation()
        {
            _isAnimateAble = true;
            
            if (_tabAnimController != null)
            {
                _tabAnimController.SetPlayerRun(true);
            }
        }

        public void DisableAnimation()
        {
            _isAnimateAble = false;

            if (_tabAnimController != null)
            {
                _tabAnimController.SetPlayerRun(false);
            }
        }
    }
}