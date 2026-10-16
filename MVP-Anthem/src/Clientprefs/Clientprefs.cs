using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using Clientprefs.API;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API;
namespace MVPAnthem;

public partial class MVPAnthem
{
    public int MVPCookie = -1;
    public int VolumeCookie = -1;

    public Dictionary<CCSPlayerController, string> playerMVPCookies { get; set; } = new();
    public Dictionary<CCSPlayerController, string> playerVolumeCookies { get; set; } = new();

    public readonly PluginCapability<IClientprefsApi> cookieCapabilty = new("Clientprefs");
    public IClientprefsApi? CLIENT_PREFS_API;

    void LoadClientPrefs(bool hotReload)
    {
        try
        {
            CLIENT_PREFS_API = cookieCapabilty.Get() ?? throw new Exception("Clientprefs api not found");
            CLIENT_PREFS_API.OnDatabaseLoaded += OnClientprefDatabaseReady;
            CLIENT_PREFS_API.OnPlayerCookiesCached += OnPlayerCookiesCached;
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to load CleintprefsApi! | {ex.Message}");
            return;
        }

        // OnDatabaseLoaded already fired before a hot reload, so register the cookies directly.
        if (hotReload)
            RegisterCookies();
    }
    void UnloadClientprefis()
    {
        if (CLIENT_PREFS_API == null)
            return;

        CLIENT_PREFS_API.OnDatabaseLoaded -= OnClientprefDatabaseReady;
        CLIENT_PREFS_API.OnPlayerCookiesCached -= OnPlayerCookiesCached;
    }
    void ReloadClientprefs()
    {
        if (CLIENT_PREFS_API == null || MVPCookie == -1 || VolumeCookie == -1) return;

        foreach (CCSPlayerController player in Utilities.GetPlayers().Where(p => !p.IsBot))
        {
            LoadPlayerCookies(player);
        }
    }
    void OnClientprefDatabaseReady()
    {
        RegisterCookies();
    }
    void RegisterCookies()
    {
        IClientprefsApi? api = CLIENT_PREFS_API;
        if (api == null) return;

        Task.Run(async () =>
        {
            int mvpCookie = await api.RegPlayerCookieAsync("MVP", "Player selected MVP", CookieAccess.CookieAccess_Public);
            int volumeCookie = await api.RegPlayerCookieAsync("Volume", "Player volume preference", CookieAccess.CookieAccess_Public);

            Server.NextFrame(() =>
            {
                MVPCookie = mvpCookie;
                VolumeCookie = volumeCookie;

                if (MVPCookie == -1)
                    Logger.LogError("Failed to register MVP cookie");

                if (VolumeCookie == -1)
                    Logger.LogError("Failed to register Volume cookie");

                // Players whose cookies were cached before the cookie ids were ready.
                ReloadClientprefs();
            });
        });
    }

    void OnPlayerCookiesCached(CCSPlayerController player)
    {
        if (CLIENT_PREFS_API == null || MVPCookie == -1 || VolumeCookie == -1) return;

        LoadPlayerCookies(player);
    }
    void LoadPlayerCookies(CCSPlayerController player)
    {
        if (CLIENT_PREFS_API == null) return;

        // GetPlayerCookie throws if the player's cookies are not loaded yet.
        try
        {
            string mvpValue = CLIENT_PREFS_API.GetPlayerCookie(player, MVPCookie);
            if (!string.IsNullOrEmpty(mvpValue))
                playerMVPCookies[player] = mvpValue;

            string volValue = CLIENT_PREFS_API.GetPlayerCookie(player, VolumeCookie);
            if (!string.IsNullOrEmpty(volValue))
                playerVolumeCookies[player] = volValue;
        }
        catch (Exception ex)
        {
            Logger.LogDebug($"Skipped loading cookies for {player.PlayerName}: {ex.Message}");
        }
    }
}
