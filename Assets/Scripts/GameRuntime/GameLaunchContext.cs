namespace MinerUnity.Runtime
{
    public enum GameStartMode
    {
        Unspecified = 0,
        Continue = 1,
        NewGame = 2
    }

    /// <summary>
    /// Carries the next scene boot mode across menu-to-gameplay navigation
    /// without letting UI buttons mutate persistence directly.
    /// </summary>
    public static class GameLaunchContext
    {
        private static GameStartMode pendingStartMode = GameStartMode.Unspecified;

        public static void RequestNewGame()
        {
            pendingStartMode = GameStartMode.NewGame;
        }

        public static void RequestContinue()
        {
            pendingStartMode = GameStartMode.Continue;
        }

        public static GameStartMode ConsumePendingStartMode(GameStartMode fallback = GameStartMode.Continue)
        {
            GameStartMode result = pendingStartMode == GameStartMode.Unspecified
                ? fallback
                : pendingStartMode;

            pendingStartMode = GameStartMode.Unspecified;
            return result;
        }
    }
}
