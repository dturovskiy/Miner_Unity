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
            return ConsumePendingStartMode(out _, fallback);
        }

        public static GameStartMode ConsumePendingStartMode(out bool wasExplicitRequest, GameStartMode fallback = GameStartMode.Continue)
        {
            wasExplicitRequest = pendingStartMode != GameStartMode.Unspecified;

            GameStartMode result = wasExplicitRequest
                ? pendingStartMode
                : fallback;

            pendingStartMode = GameStartMode.Unspecified;
            return result;
        }
    }
}
