using UnityEngine;


namespace YWJ.GameStateMachine
{
    public abstract class Root_GSM
    {
        protected abstract GameState GSMState { get; }

        public abstract void EnterState();

        public abstract void UpdateState();

        public abstract void ExitState();
    }
}