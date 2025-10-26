using Dapper;
using System.Collections.Concurrent;
using CounterStrikeSharp.API.Modules.Utils;
using System.Globalization;
using WeaponPaints.Services;
using System.Threading.Tasks;
using WeaponPaints.Models;

namespace WeaponPaints;

internal class WeaponSynchronization
{
  private readonly WeaponPaintsConfig _config;
  private readonly WeaponPaintsApiClient _api;

  internal WeaponSynchronization(WeaponPaintsApiClient api, WeaponPaintsConfig config)
  {
    _api = api;
    _config = config;
  }

  internal async Task GetPlayerData(PlayerInfo? player)
  {
    try
    {
      if (_config.Additional.KnifeEnabled)
        await GetKnives(player);
      if (_config.Additional.GloveEnabled)
        await GetGloves(player);
      if (_config.Additional.AgentEnabled)
        await GetAgents(player);
      if (_config.Additional.MusicEnabled)
        await GetMusic(player);
      if (_config.Additional.SkinEnabled)
        await GetWeaponPaints(player);
      if (_config.Additional.PinsEnabled)
        await GetPins(player);
    }
    catch (Exception ex)
    {
      // Log the exception or handle it appropriately
      Console.WriteLine($"An error occurred: {ex.Message}");
    }
  }

  private async Task GetKnives(PlayerInfo? player)
  {
    try
    {
      if (!_config.Additional.KnifeEnabled || string.IsNullOrEmpty(player?.SteamId))
        return;

      var res = await _api.GetAsync<IEnumerable<Knife>>("/skin/knife/?steamid=" + player.SteamId);

      foreach (var row in res.Data ?? [])
      {
        // Check if knife is null or empty
        if (string.IsNullOrEmpty(row.knife)) continue;

        // Determine the weapon team based on the query result
        CsTeam weaponTeam = row.weapon_team switch
        {
          2 => CsTeam.Terrorist,
          3 => CsTeam.CounterTerrorist,
          _ => CsTeam.None,
        };

        // Get or create entries for the player’s slot
        var playerKnives = WeaponPaints.GPlayersKnife.GetOrAdd(player.Slot, _ => new ConcurrentDictionary<CsTeam, string>());

        if (weaponTeam == CsTeam.None)
        {
          // Assign knife to both teams if weaponTeam is None
          playerKnives[CsTeam.Terrorist] = row.knife;
          playerKnives[CsTeam.CounterTerrorist] = row.knife;
        }
        else
        {
          // Assign knife to the specific team
          playerKnives[weaponTeam] = row.knife;
        }
      }
    }
    catch (Exception ex)
    {
      Utility.Log($"An error occurred in GetKnife: {ex.Message}");
    }
  }

  private async Task GetGloves(PlayerInfo? player)
  {
    try
    {
      if (!_config.Additional.GloveEnabled || string.IsNullOrEmpty(player?.SteamId))
        return;

      var res = await _api.GetAsync<IEnumerable<Knife>>("/skin/knife/?steamid=" + player.SteamId);

      foreach (var row in res.Data ?? [])
      {
        // Check if weapon_defindex is null
        if (row.weapon_defindex == null) continue;

        // Get or create entries for the player's slot
        var playerGloves = WeaponPaints.GPlayersGlove.GetOrAdd(player.Slot, _ => new ConcurrentDictionary<CsTeam, ushort>());

        // Determine the weapon team based on the query result
        CsTeam weaponTeam = row.weapon_team switch
        {
          2 => CsTeam.Terrorist,
          3 => CsTeam.CounterTerrorist,
          _ => CsTeam.None,
        };

        // Get or create entries for the player’s slot

        if (weaponTeam == CsTeam.None)
        {
          // Assign glove ID to both teams if weaponTeam is None
          playerGloves[CsTeam.Terrorist] = (ushort)row.weapon_defindex;
          playerGloves[CsTeam.CounterTerrorist] = (ushort)row.weapon_defindex;
        }
        else
        {
          // Assign glove ID to the specific team
          playerGloves[weaponTeam] = (ushort)row.weapon_defindex;
        }
      }
    }
    catch (Exception ex)
    {
      Utility.Log($"An error occurred in GetGloves: {ex.Message}");
    }
  }

  private async Task GetAgents(PlayerInfo? player)
  {
    try
    {
      if (!_config.Additional.AgentEnabled || string.IsNullOrEmpty(player?.SteamId))
        return;

      var res = await _api.GetAsync<IEnumerable<Agent>>("/skin/agent/?steamid=" + player.SteamId);

      if (res.Data?.Count() <= 0)
        return;

      var agents = res.Data!.ElementAt(0);
      var agentCT = agents.agent_ct;
      var agentT = agents.agent_t;

      if (!string.IsNullOrEmpty(agentCT) || !string.IsNullOrEmpty(agentT))
      {
        WeaponPaints.GPlayersAgent[player.Slot] = (
          agentCT,
          agentT
        );
      }
    }
    catch (Exception ex)
    {
      Utility.Log($"An error occurred in GetAgent: {ex.Message}");
    }
  }

  private async Task GetWeaponPaints(PlayerInfo? player)
  {
    try
    {
      if (!_config.Additional.SkinEnabled || player == null || string.IsNullOrEmpty(player.SteamId))
        return;

      var playerWeapons = WeaponPaints.GPlayerWeaponsInfo.GetOrAdd(player.Slot,
        _ => new ConcurrentDictionary<CsTeam, ConcurrentDictionary<int, WeaponInfo>>());

      var res = await _api.GetAsync<IEnumerable<Skin>>("/skin/skin/?steamid=" + player.SteamId);

      foreach (var row in res.Data ?? [])
      {
        int weaponDefIndex = row.weapon_defindex;
        int weaponPaintId = row.weapon_paint_id;
        float weaponWear = row.weapon_wear;
        int weaponSeed = row.weapon_seed;
        string weaponNameTag = row.weapon_nametag ?? "";
        bool weaponStatTrak = row.weapon_stattrak;
        int weaponStatTrakCount = row.weapon_stattrak_count;

        CsTeam weaponTeam = row.weapon_team switch
        {
          2 => CsTeam.Terrorist,
          3 => CsTeam.CounterTerrorist,
          _ => CsTeam.None,
        };

        string[]? keyChainParts = row.weapon_keychain?.Split(';');

        KeyChainInfo keyChainInfo = new KeyChainInfo();

        if (keyChainParts!.Length == 5 &&
            uint.TryParse(keyChainParts[0], out uint keyChainId) &&
            float.TryParse(keyChainParts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float keyChainOffsetX) &&
            float.TryParse(keyChainParts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float keyChainOffsetY) &&
            float.TryParse(keyChainParts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float keyChainOffsetZ) &&
            uint.TryParse(keyChainParts[4], out uint keyChainSeed))
        {
          // Successfully parsed the values
          keyChainInfo.Id = keyChainId;
          keyChainInfo.OffsetX = keyChainOffsetX;
          keyChainInfo.OffsetY = keyChainOffsetY;
          keyChainInfo.OffsetZ = keyChainOffsetZ;
          keyChainInfo.Seed = keyChainSeed;
        }
        else
        {
          // Failed to parse the values, default to 0
          keyChainInfo.Id = 0;
          keyChainInfo.OffsetX = 0f;
          keyChainInfo.OffsetY = 0f;
          keyChainInfo.OffsetZ = 0f;
          keyChainInfo.Seed = 0;
        }

        // Create the WeaponInfo object
        WeaponInfo weaponInfo = new WeaponInfo
        {
          Paint = weaponPaintId,
          Seed = weaponSeed,
          Wear = weaponWear,
          Nametag = weaponNameTag,
          KeyChain = keyChainInfo,
          StatTrak = weaponStatTrak,
          StatTrakCount = weaponStatTrakCount,
        };

        // Retrieve and parse sticker data (up to 5 slots)
        string?[] stickerData = [row.weapon_sticker_0, row.weapon_sticker_1, row.weapon_sticker_2, row.weapon_sticker_3, row.weapon_sticker_4];

        for (int i = 0; i <= 4; i++)
        {
          if (string.IsNullOrEmpty(stickerData[i])) continue;

          var parts = stickerData[i]!.Split(';');

          //"id;schema;x;y;wear;scale;rotation"
          if (parts.Length != 7 ||
              !uint.TryParse(parts[0], out uint stickerId) ||
              !uint.TryParse(parts[1], out uint stickerSchema) ||
              !float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float stickerOffsetX) ||
              !float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float stickerOffsetY) ||
              !float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float stickerWear) ||
              !float.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out float stickerScale) ||
              !float.TryParse(parts[6], NumberStyles.Float, CultureInfo.InvariantCulture, out float stickerRotation)) continue;

          StickerInfo stickerInfo = new StickerInfo
          {
            Id = stickerId,
            Schema = stickerSchema,
            OffsetX = stickerOffsetX,
            OffsetY = stickerOffsetY,
            Wear = stickerWear,
            Scale = stickerScale,
            Rotation = stickerRotation
          };

          weaponInfo.Stickers.Add(stickerInfo);
        }

        if (weaponTeam == CsTeam.None)
        {
          // Get or create entries for both teams
          var terroristWeapons = playerWeapons.GetOrAdd(CsTeam.Terrorist, _ => new ConcurrentDictionary<int, WeaponInfo>());
          var counterTerroristWeapons = playerWeapons.GetOrAdd(CsTeam.CounterTerrorist, _ => new ConcurrentDictionary<int, WeaponInfo>());

          // Add weaponInfo to both team weapon dictionaries
          terroristWeapons[weaponDefIndex] = weaponInfo;
          counterTerroristWeapons[weaponDefIndex] = weaponInfo;
        }
        else
        {
          // Add to the specific team
          var teamWeapons = playerWeapons.GetOrAdd(weaponTeam, _ => new ConcurrentDictionary<int, WeaponInfo>());
          teamWeapons[weaponDefIndex] = weaponInfo;
        }

        // weaponInfos[weaponDefIndex] = weaponInfo;
      }

      // WeaponPaints.GPlayerWeaponsInfo[player.Slot][weaponTeam] = weaponInfos;
    }
    catch (Exception ex)
    {
      Utility.Log($"An error occurred in GetWeaponPaints: {ex.Message}");
    }
  }

  private async Task GetMusic(PlayerInfo? player)
  {
    try
    {
      if (!_config.Additional.MusicEnabled || string.IsNullOrEmpty(player?.SteamId))
        return;

      var res = await _api.GetAsync<IEnumerable<Music>>("/skin/music/?steamid=" + player.SteamId);

      foreach (var row in res.Data ?? [])
      {
        // Determine the weapon team based on the query result
        CsTeam weaponTeam = row.weapon_team switch
        {
          2 => CsTeam.Terrorist,
          3 => CsTeam.CounterTerrorist,
          _ => CsTeam.None,
        };

        // Get or create entries for the player’s slot
        var playerMusic = WeaponPaints.GPlayersMusic.GetOrAdd(player.Slot, _ => new ConcurrentDictionary<CsTeam, ushort>());

        if (weaponTeam == CsTeam.None)
        {
          // Assign music ID to both teams if weaponTeam is None
          playerMusic[CsTeam.Terrorist] = (ushort)row.music_id;
          playerMusic[CsTeam.CounterTerrorist] = (ushort)row.music_id;
        }
        else
        {
          // Assign music ID to the specific team
          playerMusic[weaponTeam] = (ushort)row.music_id;
        }
      }
    }
    catch (Exception ex)
    {
      Utility.Log($"An error occurred in GetMusic: {ex.Message}");
    }
  }

  private async Task GetPins(PlayerInfo? player)
  {
    try
    {
      if (string.IsNullOrEmpty(player?.SteamId))
        return;

      var res = await _api.GetAsync<IEnumerable<Pin>>("/skin/pin/?steamid=" + player.SteamId);

      foreach (var row in res.Data ?? [])
      {
        // Determine the weapon team based on the query result
        CsTeam weaponTeam = row.weapon_team switch
        {
          2 => CsTeam.Terrorist,
          3 => CsTeam.CounterTerrorist,
          _ => CsTeam.None,
        };

        // Get or create entries for the player’s slot
        var playerPins = WeaponPaints.GPlayersPin.GetOrAdd(player.Slot, _ => new ConcurrentDictionary<CsTeam, ushort>());

        if (weaponTeam == CsTeam.None)
        {
          // Assign pin ID to both teams if weaponTeam is None
          playerPins[CsTeam.Terrorist] = (ushort)row.id;
          playerPins[CsTeam.CounterTerrorist] = (ushort)row.id;
        }
        else
        {
          // Assign pin ID to the specific team
          playerPins[weaponTeam] = (ushort)row.id;
        }
      }
    }
    catch (Exception ex)
    {
      Utility.Log($"An error occurred in GetPins: {ex.Message}");
    }
  }

  internal async Task SyncKnife(PlayerInfo player, string knife, CsTeam[] teams)
  {
    if (!_config.Additional.KnifeEnabled || string.IsNullOrEmpty(player.SteamId) || string.IsNullOrEmpty(knife) || teams.Length == 0) return;

    try
    {
      // Loop through each team and insert/update accordingly
      foreach (var team in teams)
      {
        await _api.PostAsync<object>("/skin/knife", new
        {
          steamid = player.SteamId,
          weapon_team = team,
          knife
        });
      }
    }
    catch (Exception e)
    {
      Utility.Log($"Error syncing knife to database: {e.Message}");
    }
  }

  internal async Task SyncGlove(PlayerInfo player, ushort gloveDefIndex, CsTeam[] teams)
  {
    if (!_config.Additional.GloveEnabled || string.IsNullOrEmpty(player.SteamId) || teams.Length == 0)
      return;

    try
    {
      foreach (var team in teams)
      {
        await _api.PostAsync<object>("/skin/knife", new
        {
          steamid = player.SteamId,
          weapon_team = team,
          weapon_defindex = gloveDefIndex
        });
      }
    }
    catch (Exception e)
    {
      Utility.Log($"Error syncing glove to database: {e.Message}");
    }
  }

  internal async Task SyncAgent(PlayerInfo player)
  {
    if (!_config.Additional.AgentEnabled || string.IsNullOrEmpty(player.SteamId)) return;

    try
    {
      await _api.PostAsync<object>("/skin/knife", new
      {
        steamid = player.SteamId,
        agent_ct = WeaponPaints.GPlayersAgent[player.Slot].CT,
        agent_t = WeaponPaints.GPlayersAgent[player.Slot].T
      });
    }
    catch (Exception e)
    {
      Utility.Log($"Error syncing agents to database: {e.Message}");
    }
  }

  internal async Task SyncWeaponPaints(PlayerInfo player)
  {
    if (string.IsNullOrEmpty(player.SteamId) || !WeaponPaints.GPlayerWeaponsInfo.TryGetValue(player.Slot, out var teamWeaponInfos))
      return;

    try
    {
      // Loop through each team (Terrorist and CounterTerrorist)
      foreach (var (teamId, weaponsInfo) in teamWeaponInfos)
      {
        foreach (var (weaponDefIndex, weaponInfo) in weaponsInfo)
        {
          await _api.PostAsync<object>("/skin/skin",
             new
             {
               steamid = player.SteamId,
               weapon_defindex = weaponDefIndex,
               weapon_team = (int)teamId,
               weapon_paint_id = weaponInfo.Paint,
               weapon_wear = weaponInfo.Wear,
               weapon_seed = weaponInfo.Seed
             }
             );
        }
      }
    }
    catch (Exception e)
    {
      Utility.Log($"Error syncing weapon paints to database: {e.Message}");
    }
  }

  internal async Task SyncMusic(PlayerInfo player, ushort music, CsTeam[] teams)
  {
    if (!_config.Additional.MusicEnabled || string.IsNullOrEmpty(player.SteamId)) return;

    try
    {
      // Loop through each team and insert/update accordingly
      foreach (var team in teams)
      {
        await _api.PostAsync<object>("/skin/music", new
        {
          steamid = player.SteamId,
          weapon_team = team,
          music_id = music
        }
        );
      }
    }
    catch (Exception e)
    {
      Utility.Log($"Error syncing music kit to database: {e.Message}");
    }
  }

  internal async Task SyncPin(PlayerInfo player, ushort pin, CsTeam[] teams)
  {
    if (!_config.Additional.PinsEnabled || string.IsNullOrEmpty(player.SteamId)) return;

    try
    {
      // Loop through each team and insert/update accordingly
      foreach (var team in teams)
      {
        await _api.PostAsync<object>("/skin/pin", new
        {
          steamid = player.SteamId,
          weapon_team = team,
          id = pin
        });
      }
    }
    catch (Exception e)
    {
      Utility.Log($"Error syncing pin to database: {e.Message}");
    }
  }

  internal async Task SyncStatTrak(PlayerInfo player)
  {
    if (WeaponPaints.WeaponSync == null || WeaponPaints.GPlayerWeaponsInfo.IsEmpty) return;
    if (string.IsNullOrEmpty(player.SteamId))
      return;

    try
    {
      // Check if player's slot exists in GPlayerWeaponsInfo
      if (!WeaponPaints.GPlayerWeaponsInfo.TryGetValue(player.Slot, out var teamWeaponsInfo))
        return;

      // Iterate through each team in the player's weapon info
      foreach (var teamInfo in teamWeaponsInfo)
      {
        // Retrieve weaponInfos for the current team
        var weaponInfos = teamInfo.Value;

        // Get StatTrak weapons for the current team
        var statTrakWeapons = weaponInfos
          .ToDictionary(
            w => w.Key,
            w => (w.Value.StatTrak, w.Value.StatTrakCount) // Store both StatTrak and StatTrakCount in a tuple
          );

        // Check if there are StatTrak weapons to sync
        if (statTrakWeapons.Count == 0) continue;

        // Get the current team ID
        int weaponTeam = (int)teamInfo.Key;

        // Sync StatTrak values for the current team
        foreach (var (defindex, (statTrak, statTrakCount)) in statTrakWeapons)
        {
          await _api.PostAsync<object>("/skin/skin", new
          {
            steamid = player.SteamId,
            weapon_defindex = defindex,
            weapon_stattrak = statTrak,
            weapon_stattrak_count = statTrakCount,
            weapon_team = weaponTeam
          });
        }
      }
    }
    catch (Exception e)
    {
      Utility.Log($"Error syncing stattrak to database: {e.Message}");
    }
  }
}
