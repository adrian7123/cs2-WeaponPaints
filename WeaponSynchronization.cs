using System.Collections.Concurrent;
using CounterStrikeSharp.API.Modules.Utils;
using System.Globalization;

namespace WeaponPaints;

internal class WeaponSynchronization
{
  private readonly WeaponPaintsConfig _config;
  private readonly IDatabase _database;

  internal WeaponSynchronization(IDatabase database, WeaponPaintsConfig config)
  {
    _database = database;
    _config = config;
  }

  internal async Task GetPlayerData(PlayerInfo? player)
  {
    try
    {
      if (_config.Additional.KnifeEnabled)
        await GetKnifeFromDatabase(player);
      if (_config.Additional.GloveEnabled)
        await GetGloveFromDatabase(player);
      if (_config.Additional.AgentEnabled)
        await GetAgentFromDatabase(player);
      if (_config.Additional.MusicEnabled)
        await GetMusicFromDatabase(player);
      if (_config.Additional.SkinEnabled)
        await GetWeaponPaintsFromDatabase(player);
      if (_config.Additional.PinsEnabled)
        await GetPinsFromDatabase(player);
    }
    catch (Exception ex)
    {
      // Log the exception or handle it appropriately
      Console.WriteLine($"An error occurred: {ex.Message}");
    }
  }

  internal async Task GetKnifeFromDatabase(PlayerInfo? player)
  {
    try
    {
      if (!_config.Additional.KnifeEnabled || string.IsNullOrEmpty(player?.SteamId))
        return;

      var rows = await _database.GetPlayerKnivesAsync(player.SteamId);

      foreach (var row in rows)
      {
        // Check if knife is null or empty
        if (!row.TryGetValue("knife", out var knifeObj) || string.IsNullOrEmpty(knifeObj?.ToString())) continue;

        var knife = knifeObj.ToString();
        if (!row.TryGetValue("weapon_team", out var teamObj)) continue;

        // Determine the weapon team based on the query result
        CsTeam weaponTeam = Convert.ToInt32(teamObj) switch
        {
          2 => CsTeam.Terrorist,
          3 => CsTeam.CounterTerrorist,
          _ => CsTeam.None,
        };

        // Get or create entries for the player's slot
        var playerKnives = WeaponPaints.GPlayersKnife.GetOrAdd(player.Slot, _ => new ConcurrentDictionary<CsTeam, string>());

        if (weaponTeam == CsTeam.None)
        {
          // Assign knife to both teams if weaponTeam is None
          playerKnives[CsTeam.Terrorist] = knife!;
          playerKnives[CsTeam.CounterTerrorist] = knife!;
        }
        else
        {
          // Assign knife to the specific team
          playerKnives[weaponTeam] = knife!;
        }
      }
    }
    catch (Exception ex)
    {
      Utility.Log($"An error occurred in GetKnifeFromDatabase: {ex.Message}");
    }
  }

  internal async Task GetGloveFromDatabase(PlayerInfo? player)
  {
    try
    {
      if (!_config.Additional.GloveEnabled || string.IsNullOrEmpty(player?.SteamId))
        return;

      var rows = await _database.GetPlayerGlovesAsync(player.SteamId);

      foreach (var row in rows)
      {
        // Check if weapon_defindex is null
        if (!row.TryGetValue("weapon_defindex", out var defindexObj)) continue;
        if (!row.TryGetValue("weapon_team", out var teamObj)) continue;

        // Determine the weapon team based on the query result
        var playerGloves = WeaponPaints.GPlayersGlove.GetOrAdd(player.Slot, _ => new ConcurrentDictionary<CsTeam, ushort>());
        CsTeam weaponTeam = Convert.ToInt32(teamObj) switch
        {
          2 => CsTeam.Terrorist,
          3 => CsTeam.CounterTerrorist,
          _ => CsTeam.None,
        };

        var weaponDefindex = Convert.ToUInt16(defindexObj);

        if (weaponTeam == CsTeam.None)
        {
          // Assign glove ID to both teams if weaponTeam is None
          playerGloves[CsTeam.Terrorist] = weaponDefindex;
          playerGloves[CsTeam.CounterTerrorist] = weaponDefindex;
        }
        else
        {
          // Assign glove ID to the specific team
          playerGloves[weaponTeam] = weaponDefindex;
        }
      }
    }
    catch (Exception ex)
    {
      Utility.Log($"An error occurred in GetGloveFromDatabase: {ex.Message}");
    }
  }

  internal async Task GetAgentFromDatabase(PlayerInfo? player)
  {
    try
    {
      if (!_config.Additional.AgentEnabled || string.IsNullOrEmpty(player?.SteamId))
        return;

      var rows = await _database.GetPlayerAgentsAsync(player.SteamId);

      foreach (var row in rows)
      {
        // Check if agent is null or empty
        if (!row.TryGetValue("agent", out var agentObj) || string.IsNullOrEmpty(agentObj?.ToString())) continue;

        var agent = agentObj.ToString();
        if (!row.TryGetValue("weapon_team", out var teamObj)) continue;

        // Determine the weapon team based on the query result
        CsTeam weaponTeam = Convert.ToInt32(teamObj) switch
        {
          2 => CsTeam.Terrorist,
          3 => CsTeam.CounterTerrorist,
          _ => CsTeam.None,
        };

        // Get or create entries for the player's slot - using tuple structure
        var currentAgents = WeaponPaints.GPlayersAgent.GetOrAdd(player.Slot, _ => (null, null));

        if (weaponTeam == CsTeam.CounterTerrorist)
        {
          WeaponPaints.GPlayersAgent[player.Slot] = (agent, currentAgents.T);
        }
        else if (weaponTeam == CsTeam.Terrorist)
        {
          WeaponPaints.GPlayersAgent[player.Slot] = (currentAgents.CT, agent);
        }
        else // Both teams
        {
          WeaponPaints.GPlayersAgent[player.Slot] = (agent, agent);
        }
      }
    }
    catch (Exception ex)
    {
      Utility.Log($"An error occurred in GetAgentFromDatabase: {ex.Message}");
    }
  }

  internal async Task GetMusicFromDatabase(PlayerInfo? player)
  {
    try
    {
      if (!_config.Additional.MusicEnabled || string.IsNullOrEmpty(player?.SteamId))
        return;

      var rows = await _database.GetPlayerMusicAsync(player.SteamId);

      foreach (var row in rows)
      {
        if (!row.TryGetValue("music_id", out var musicIdObj)) continue;

        var musicId = Convert.ToUInt16(musicIdObj);
        var playerMusic = WeaponPaints.GPlayersMusic.GetOrAdd(player.Slot, _ => new ConcurrentDictionary<CsTeam, ushort>());
        playerMusic[CsTeam.None] = musicId; // Music is not team-specific
        break; // Only one music kit per player
      }
    }
    catch (Exception ex)
    {
      Utility.Log($"An error occurred in GetMusicFromDatabase: {ex.Message}");
    }
  }

  internal async Task GetWeaponPaintsFromDatabase(PlayerInfo? player)
  {
    try
    {
      if (!_config.Additional.SkinEnabled || string.IsNullOrEmpty(player?.SteamId))
        return;

      var rows = await _database.GetPlayerWeaponSkinsAsync(player.SteamId);

      foreach (var row in rows)
      {
        if (!row.TryGetValue("weapon_defindex", out var defindexObj) || string.IsNullOrEmpty(defindexObj?.ToString())) continue;
        if (!row.TryGetValue("weapon_paint_id", out var paintIdObj)) continue;

        var weaponDefindexStr = defindexObj.ToString()!;
        var weaponDefindex = Convert.ToInt32(weaponDefindexStr);
        var weaponPaintId = Convert.ToInt32(paintIdObj);
        var weaponWear = row.TryGetValue("weapon_wear", out var wearObj) ? Convert.ToSingle(wearObj) : 0.0f;
        var weaponSeed = row.TryGetValue("weapon_seed", out var seedObj) ? Convert.ToInt32(seedObj) : 0;
        var weaponNametag = row.TryGetValue("weapon_nametag", out var nametagObj) ? nametagObj?.ToString() ?? string.Empty : string.Empty;
        var weaponStattrak = row.TryGetValue("weapon_stattrak", out var stattrakObj) ? Convert.ToInt32(stattrakObj) : -1;

        // Get or create entries for the player's slot - structure is nested: [slot][team][defindex]
        var playerWeapons = WeaponPaints.GPlayerWeaponsInfo.GetOrAdd(player.Slot, _ => new ConcurrentDictionary<CsTeam, ConcurrentDictionary<int, WeaponInfo>>());

        // For skins, we typically use CsTeam.None since skins aren't team-specific
        var teamWeapons = playerWeapons.GetOrAdd(CsTeam.None, _ => new ConcurrentDictionary<int, WeaponInfo>());

        teamWeapons[weaponDefindex] = new WeaponInfo
        {
          Paint = weaponPaintId,
          Wear = weaponWear,
          Seed = weaponSeed,
          Nametag = weaponNametag,
          StatTrak = weaponStattrak > 0,
          StatTrakCount = weaponStattrak > 0 ? weaponStattrak : 0
        };
      }
    }
    catch (Exception ex)
    {
      Utility.Log($"An error occurred in GetWeaponPaintsFromDatabase: {ex.Message}");
    }
  }

  internal async Task GetPinsFromDatabase(PlayerInfo? player)
  {
    try
    {
      if (!_config.Additional.PinsEnabled || string.IsNullOrEmpty(player?.SteamId))
        return;

      var rows = await _database.GetPlayerPinsAsync(player.SteamId);

      foreach (var row in rows)
      {
        if (!row.TryGetValue("pin_id", out var pinIdObj)) continue;

        var pinId = Convert.ToUInt16(pinIdObj);
        var playerPins = WeaponPaints.GPlayersPin.GetOrAdd(player.Slot, _ => new ConcurrentDictionary<CsTeam, ushort>());
        playerPins[CsTeam.None] = pinId; // Pins are not team-specific
        break; // Only one pin per player
      }
    }
    catch (Exception ex)
    {
      Utility.Log($"An error occurred in GetPinsFromDatabase: {ex.Message}");
    }
  }

  // Save methods

  internal static async Task SyncKnifeToDatabase(PlayerInfo? player, CsTeam team, string knife)
  {
    try
    {
      if (WeaponPaints.Database == null || string.IsNullOrEmpty(player?.SteamId)) return;

      int weaponTeam = team switch
      {
        CsTeam.Terrorist => 2,
        CsTeam.CounterTerrorist => 3,
        _ => 0
      };

      await WeaponPaints.Database.SavePlayerKnifeAsync(player.SteamId, knife, weaponTeam);
    }
    catch (Exception ex)
    {
      Utility.Log($"An error occurred in SyncKnifeToDatabase: {ex.Message}");
    }
  }

  internal static async Task SyncGloveToDatabase(PlayerInfo? player, CsTeam team, ushort defindex)
  {
    try
    {
      if (WeaponPaints.Database == null || string.IsNullOrEmpty(player?.SteamId)) return;

      int weaponTeam = team switch
      {
        CsTeam.Terrorist => 2,
        CsTeam.CounterTerrorist => 3,
        _ => 0
      };

      await WeaponPaints.Database.SavePlayerGloveAsync(player.SteamId, defindex, weaponTeam);
    }
    catch (Exception ex)
    {
      Utility.Log($"An error occurred in SyncGloveToDatabase: {ex.Message}");
    }
  }

  internal static async Task SyncAgentToDatabase(PlayerInfo? player, CsTeam team, string agent)
  {
    try
    {
      if (WeaponPaints.Database == null || string.IsNullOrEmpty(player?.SteamId)) return;

      int weaponTeam = team switch
      {
        CsTeam.Terrorist => 2,
        CsTeam.CounterTerrorist => 3,
        _ => 0
      };

      await WeaponPaints.Database.SavePlayerAgentAsync(player.SteamId, agent, weaponTeam);
    }
    catch (Exception ex)
    {
      Utility.Log($"An error occurred in SyncAgentToDatabase: {ex.Message}");
    }
  }

  internal static async Task SyncMusicToDatabase(PlayerInfo? player, int musicId)
  {
    try
    {
      if (WeaponPaints.Database == null || string.IsNullOrEmpty(player?.SteamId)) return;

      await WeaponPaints.Database.SavePlayerMusicAsync(player.SteamId, musicId);
    }
    catch (Exception ex)
    {
      Utility.Log($"An error occurred in SyncMusicToDatabase: {ex.Message}");
    }
  }

  internal static async Task SyncWeaponPaintToDatabase(PlayerInfo? player, int weaponDefindex, int weaponPaintId, float weaponWear, int weaponSeed, string weaponNametag, int weaponStattrak)
  {
    try
    {
      if (WeaponPaints.Database == null || string.IsNullOrEmpty(player?.SteamId)) return;

      await WeaponPaints.Database.SavePlayerWeaponSkinAsync(player.SteamId, weaponDefindex, weaponPaintId, weaponWear, weaponSeed, weaponNametag, weaponStattrak);
    }
    catch (Exception ex)
    {
      Utility.Log($"An error occurred in SyncWeaponPaintToDatabase: {ex.Message}");
    }
  }

  internal static async Task SyncStatTrakToDatabase(PlayerInfo? player)
  {
    try
    {
      if (WeaponPaints.Database == null || string.IsNullOrEmpty(player?.SteamId)) return;
      if (!WeaponPaints.GPlayerWeaponsInfo.TryGetValue(player.Slot, out var teamWeapons)) return;

      foreach (var team in teamWeapons)
      {
        foreach (var weapon in team.Value)
        {
          if (weapon.Value.StatTrak && weapon.Value.StatTrakCount >= 0)
          {
            await WeaponPaints.Database.UpdatePlayerWeaponStatTrakAsync(player.SteamId, weapon.Key, weapon.Value.StatTrakCount);
          }
        }
      }
    }
    catch (Exception ex)
    {
      Utility.Log($"An error occurred in SyncStatTrakToDatabase: {ex.Message}");
    }
  }

  internal static async Task SyncPinToDatabase(PlayerInfo? player, int pinId)
  {
    try
    {
      if (WeaponPaints.Database == null || string.IsNullOrEmpty(player?.SteamId)) return;

      await WeaponPaints.Database.SavePlayerPinAsync(player.SteamId, pinId);
    }
    catch (Exception ex)
    {
      Utility.Log($"An error occurred in SyncPinToDatabase: {ex.Message}");
    }
  }

  internal async Task SyncWeaponPaintsToDatabase(PlayerInfo? player)
  {
    try
    {
      if (WeaponPaints.Database == null || string.IsNullOrEmpty(player?.SteamId)) return;
      if (!WeaponPaints.GPlayerWeaponsInfo.TryGetValue(player.Slot, out var teamWeapons)) return;

      foreach (var team in teamWeapons)
      {
        foreach (var weapon in team.Value)
        {
          var weaponInfo = weapon.Value;
          await WeaponPaints.Database.SavePlayerWeaponSkinAsync(
            player.SteamId,
            weapon.Key,
            weaponInfo.Paint,
            weaponInfo.Wear,
            weaponInfo.Seed,
            weaponInfo.Nametag,
            weaponInfo.StatTrak ? weaponInfo.StatTrakCount : -1
          );
        }
      }
    }
    catch (Exception ex)
    {
      Utility.Log($"An error occurred in SyncWeaponPaintsToDatabase: {ex.Message}");
    }
  }
}
