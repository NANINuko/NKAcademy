using BepInEx;
using BepInEx.Configuration;

namespace NANINuko
{
    public static class NANINukoConfig
    {
        private static ConfigFile _config;

        public static ConfigEntry<string> TeamName { get; private set; }
        public static ConfigEntry<string> Honorific { get; private set; }

        public static void Init(ConfigFile config)
        {
            if (_config != null)
                return;

            _config = config;

            TeamName = config.Bind(
                "General",
                "TeamName",
                "チーム名未設定",
                "ドラマ中で #teamname に置換される名前"
            );

            Honorific = config.Bind(
                "General",
                "Honorific",
                "さん",
                "ドラマ中で #honorific に置換される敬称"
            );
        }

        public static void Save()
        {
            _config?.Save();
        }
    }
}
