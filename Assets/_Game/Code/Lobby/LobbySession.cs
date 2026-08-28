namespace ROS.Game.Lobby
{
    public static class LobbySession
    {
        private static bool _launchRequested;

        public static LobbyMatchMode SelectedMode { get; private set; } =
            LobbyMatchMode.Solo;

        public static string SelectedMap { get; private set; } =
            "Ghillie Island";

        public static void RequestMatch(
            LobbyMatchMode mode,
            string mapName
        )
        {
            SelectedMode = mode;
            SelectedMap = string.IsNullOrWhiteSpace(mapName)
                ? "Ghillie Island"
                : mapName;
            _launchRequested = true;
        }

        public static bool ConsumeLaunchRequest()
        {
            if (!_launchRequested)
            {
                return false;
            }

            _launchRequested = false;
            return true;
        }

        public static void CancelLaunchRequest()
        {
            _launchRequested = false;
        }
    }
}
