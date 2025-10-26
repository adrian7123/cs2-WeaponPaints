using System.Collections.Generic;
using System.Threading.Tasks;

namespace WeaponPaints
{
  public interface IDatabase
  {
    Task<bool> TestConnectionAsync();
    Task<bool> CreateTablesAsync();

    // Player knife operations
    Task<Dictionary<string, object>[]> GetPlayerKnivesAsync(string steamId);
    Task SavePlayerKnifeAsync(string steamId, string knife, int weaponTeam);

    // Player glove operations
    Task<Dictionary<string, object>[]> GetPlayerGlovesAsync(string steamId);
    Task SavePlayerGloveAsync(string steamId, int weaponDefindex, int weaponTeam);

    // Player agent operations
    Task<Dictionary<string, object>[]> GetPlayerAgentsAsync(string steamId);
    Task SavePlayerAgentAsync(string steamId, string agent, int weaponTeam);

    // Player music operations
    Task<Dictionary<string, object>[]> GetPlayerMusicAsync(string steamId);
    Task SavePlayerMusicAsync(string steamId, int musicId);

    // Player weapon skins operations
    Task<Dictionary<string, object>[]> GetPlayerWeaponSkinsAsync(string steamId);
    Task SavePlayerWeaponSkinAsync(string steamId, int weaponDefindex, int weaponPaintId, float weaponWear, int weaponSeed, string weaponNametag, int weaponStattrak);
    Task UpdatePlayerWeaponStatTrakAsync(string steamId, int weaponDefindex, int statTrakCount);

    // Player pins operations
    Task<Dictionary<string, object>[]> GetPlayerPinsAsync(string steamId);
    Task SavePlayerPinAsync(string steamId, int pinId);

    // Utility methods
    Task<bool> PlayerExistsAsync(string steamId);
    Task CreatePlayerAsync(string steamId, string playerName);
  }
}
