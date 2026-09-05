using System;



namespace YWJ.GameStateMachine
{
    public static class GameStateEventBus
    {
        // MAIN_MENU State Events
        public static event Action OnMainMenuStateEntered;
        public static event Action OnMainMenuStateExited;

        public static void PublishMainMenuStateEntered()
        {
            OnMainMenuStateEntered?.Invoke();
        }

        public static void PublishMainMenuStateExited()
        {
            OnMainMenuStateExited?.Invoke();
        }

        // STORY_MODE State Events
        public static event Action OnStoryModeStateEntered;
        public static event Action OnStoryModeStateExited;

        public static void PublishStoryModeStateEntered()
        {
            OnStoryModeStateEntered?.Invoke();
        }

        public static void PublishStoryModeStateExited()
        {
            OnStoryModeStateExited?.Invoke();
        }

        // MUSIC_MODE State Events
        public static event Action OnMusicModeStateEntered;
        public static event Action OnMusicModeStateExited;

        public static void PublishMusicModeStateEntered()
        {
            OnMusicModeStateEntered?.Invoke();
        }

        public static void PublishMusicModeStateExited()
        {
            OnMusicModeStateExited?.Invoke();
        }

        // GAME_CLEAR State Events
        public static event Action OnGameClearStateEntered;
        public static event Action OnGameClearStateExited;

        public static void PublishGameClearStateEntered()
        {
            OnGameClearStateEntered?.Invoke();
        }

        public static void PublishGameClearStateExited()
        {
            OnGameClearStateExited?.Invoke();
        }

        // BOSS_MODE State Events
        public static event Action OnBossModeStateEntered;
        public static event Action OnBossModeStateExited;

        public static void PublishBossModeStateEntered()
        {
            OnBossModeStateEntered?.Invoke();
        }

        public static void PublishBossModeStateExited()
        {
            OnBossModeStateExited?.Invoke();
        }
    }
}