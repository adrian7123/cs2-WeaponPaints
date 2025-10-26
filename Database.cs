using MySqlConnector;
using Dapper;

namespace WeaponPaints
{
  public class MySqlDatabase : IDatabase
  {
    private readonly string _connectionString;

    public MySqlDatabase(string connectionString)
    {
      _connectionString = connectionString;
    }

    public async Task<MySqlConnection> GetConnectionAsync()
    {
      try
      {
        var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
      }
      catch (Exception ex)
      {
        Console.WriteLine($"[WeaponPaints] Unable to connect to MySQL database: {ex.Message}");
        throw;
      }
    }

    public async Task<bool> TestConnectionAsync()
    {
      try
      {
        await using var connection = await GetConnectionAsync();
        return connection.State == System.Data.ConnectionState.Open;
      }
      catch
      {
        return false;
      }
    }

    public async Task<bool> CreateTablesAsync()
    {
      try
      {
        await using var connection = await GetConnectionAsync();

        // Create tables as needed by the application
        var createTables = new[]
        {
          @"CREATE TABLE IF NOT EXISTS `wp_player_knife` (
						`steamid` varchar(32) NOT NULL,
						`knife` varchar(128) DEFAULT NULL,
						`weapon_team` int(11) DEFAULT NULL,
						PRIMARY KEY (`steamid`, `weapon_team`)
					) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

          @"CREATE TABLE IF NOT EXISTS `wp_player_gloves` (
						`steamid` varchar(32) NOT NULL,
						`weapon_defindex` int(11) DEFAULT NULL,
						`weapon_team` int(11) DEFAULT NULL,
						PRIMARY KEY (`steamid`, `weapon_team`)
					) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

          @"CREATE TABLE IF NOT EXISTS `wp_player_agents` (
						`steamid` varchar(32) NOT NULL,
						`agent` varchar(128) DEFAULT NULL,
						`weapon_team` int(11) DEFAULT NULL,
						PRIMARY KEY (`steamid`, `weapon_team`)
					) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

          @"CREATE TABLE IF NOT EXISTS `wp_player_music` (
						`steamid` varchar(32) NOT NULL,
						`music_id` int(11) DEFAULT NULL,
						PRIMARY KEY (`steamid`)
					) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

          @"CREATE TABLE IF NOT EXISTS `wp_player_skins` (
						`steamid` varchar(32) NOT NULL,
						`weapon_defindex` varchar(128) NOT NULL,
						`weapon_paint_id` int(11) DEFAULT NULL,
						`weapon_wear` float DEFAULT NULL,
						`weapon_seed` int(11) DEFAULT NULL,
						`weapon_nametag` varchar(128) DEFAULT NULL,
						`weapon_stattrak` int(11) DEFAULT NULL,
						PRIMARY KEY (`steamid`, `weapon_defindex`)
					) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

          @"CREATE TABLE IF NOT EXISTS `wp_player_pins` (
						`steamid` varchar(32) NOT NULL,
						`pin_id` int(11) DEFAULT NULL,
						PRIMARY KEY (`steamid`)
					) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;"
        };

        foreach (var sql in createTables)
        {
          await connection.ExecuteAsync(sql);
        }

        return true;
      }
      catch (Exception ex)
      {
        Console.WriteLine($"[WeaponPaints] Error creating MySQL tables: {ex.Message}");
        return false;
      }
    }

    public async Task<Dictionary<string, object>[]> GetPlayerKnivesAsync(string steamId)
    {
      await using var connection = await GetConnectionAsync();
      const string query = "SELECT `knife`, `weapon_team` FROM `wp_player_knife` WHERE `steamid` = @steamid ORDER BY `weapon_team` ASC";
      var result = await connection.QueryAsync(query, new { steamid = steamId });
      return result.Select(r => (Dictionary<string, object>)r).ToArray();
    }

    public async Task SavePlayerKnifeAsync(string steamId, string knife, int weaponTeam)
    {
      await using var connection = await GetConnectionAsync();
      const string query = "INSERT INTO `wp_player_knife` (`steamid`, `knife`, `weapon_team`) VALUES (@steamid, @knife, @weaponTeam) ON DUPLICATE KEY UPDATE `knife` = @knife";
      await connection.ExecuteAsync(query, new { steamid = steamId, knife, weaponTeam });
    }

    public async Task<Dictionary<string, object>[]> GetPlayerGlovesAsync(string steamId)
    {
      await using var connection = await GetConnectionAsync();
      const string query = "SELECT `weapon_defindex`, `weapon_team` FROM `wp_player_gloves` WHERE `steamid` = @steamid ORDER BY `weapon_team` ASC";
      var result = await connection.QueryAsync(query, new { steamid = steamId });
      return result.Select(r => (Dictionary<string, object>)r).ToArray();
    }

    public async Task SavePlayerGloveAsync(string steamId, int weaponDefindex, int weaponTeam)
    {
      await using var connection = await GetConnectionAsync();
      const string query = "INSERT INTO `wp_player_gloves` (`steamid`, `weapon_defindex`, `weapon_team`) VALUES (@steamid, @weaponDefindex, @weaponTeam) ON DUPLICATE KEY UPDATE `weapon_defindex` = @weaponDefindex";
      await connection.ExecuteAsync(query, new { steamid = steamId, weaponDefindex, weaponTeam });
    }

    public async Task<Dictionary<string, object>[]> GetPlayerAgentsAsync(string steamId)
    {
      await using var connection = await GetConnectionAsync();
      const string query = "SELECT `agent`, `weapon_team` FROM `wp_player_agents` WHERE `steamid` = @steamid ORDER BY `weapon_team` ASC";
      var result = await connection.QueryAsync(query, new { steamid = steamId });
      return result.Select(r => (Dictionary<string, object>)r).ToArray();
    }

    public async Task SavePlayerAgentAsync(string steamId, string agent, int weaponTeam)
    {
      await using var connection = await GetConnectionAsync();
      const string query = "INSERT INTO `wp_player_agents` (`steamid`, `agent`, `weapon_team`) VALUES (@steamid, @agent, @weaponTeam) ON DUPLICATE KEY UPDATE `agent` = @agent";
      await connection.ExecuteAsync(query, new { steamid = steamId, agent, weaponTeam });
    }

    public async Task<Dictionary<string, object>[]> GetPlayerMusicAsync(string steamId)
    {
      await using var connection = await GetConnectionAsync();
      const string query = "SELECT `music_id` FROM `wp_player_music` WHERE `steamid` = @steamid";
      var result = await connection.QueryAsync(query, new { steamid = steamId });
      return result.Select(r => (Dictionary<string, object>)r).ToArray();
    }

    public async Task SavePlayerMusicAsync(string steamId, int musicId)
    {
      await using var connection = await GetConnectionAsync();
      const string query = "INSERT INTO `wp_player_music` (`steamid`, `music_id`) VALUES (@steamid, @musicId) ON DUPLICATE KEY UPDATE `music_id` = @musicId";
      await connection.ExecuteAsync(query, new { steamid = steamId, musicId });
    }

    public async Task<Dictionary<string, object>[]> GetPlayerWeaponSkinsAsync(string steamId)
    {
      await using var connection = await GetConnectionAsync();
      const string query = "SELECT `weapon_defindex`, `weapon_paint_id`, `weapon_wear`, `weapon_seed`, `weapon_nametag`, `weapon_stattrak` FROM `wp_player_skins` WHERE `steamid` = @steamid";
      var result = await connection.QueryAsync(query, new { steamid = steamId });
      return result.Select(r => (Dictionary<string, object>)r).ToArray();
    }

    public async Task SavePlayerWeaponSkinAsync(string steamId, string weaponDefindex, int weaponPaintId, float weaponWear, int weaponSeed, string weaponNametag, int weaponStattrak)
    {
      await using var connection = await GetConnectionAsync();
      const string query = @"INSERT INTO `wp_player_skins` (`steamid`, `weapon_defindex`, `weapon_paint_id`, `weapon_wear`, `weapon_seed`, `weapon_nametag`, `weapon_stattrak`)
				VALUES (@steamid, @weaponDefindex, @weaponPaintId, @weaponWear, @weaponSeed, @weaponNametag, @weaponStattrak)
				ON DUPLICATE KEY UPDATE `weapon_paint_id` = @weaponPaintId, `weapon_wear` = @weaponWear, `weapon_seed` = @weaponSeed, `weapon_nametag` = @weaponNametag, `weapon_stattrak` = @weaponStattrak";
      await connection.ExecuteAsync(query, new { steamid = steamId, weaponDefindex, weaponPaintId, weaponWear, weaponSeed, weaponNametag, weaponStattrak });
    }

    public async Task UpdatePlayerWeaponStatTrakAsync(string steamId, string weaponDefindex, int statTrakCount)
    {
      await using var connection = await GetConnectionAsync();
      const string query = "UPDATE `wp_player_skins` SET `weapon_stattrak` = @statTrakCount WHERE `steamid` = @steamid AND `weapon_defindex` = @weaponDefindex";
      await connection.ExecuteAsync(query, new { steamid = steamId, weaponDefindex, statTrakCount });
    }

    public async Task<Dictionary<string, object>[]> GetPlayerPinsAsync(string steamId)
    {
      await using var connection = await GetConnectionAsync();
      const string query = "SELECT `pin_id` FROM `wp_player_pins` WHERE `steamid` = @steamid";
      var result = await connection.QueryAsync(query, new { steamid = steamId });
      return result.Select(r => (Dictionary<string, object>)r).ToArray();
    }

    public async Task SavePlayerPinAsync(string steamId, int pinId)
    {
      await using var connection = await GetConnectionAsync();
      const string query = "INSERT INTO `wp_player_pins` (`steamid`, `pin_id`) VALUES (@steamid, @pinId) ON DUPLICATE KEY UPDATE `pin_id` = @pinId";
      await connection.ExecuteAsync(query, new { steamid = steamId, pinId });
    }

    public async Task<bool> PlayerExistsAsync(string steamId)
    {
      await using var connection = await GetConnectionAsync();
      const string query = "SELECT COUNT(*) FROM `wp_player_knife` WHERE `steamid` = @steamid LIMIT 1";
      var count = await connection.QuerySingleAsync<int>(query, new { steamid = steamId });
      return count > 0;
    }

    public async Task CreatePlayerAsync(string steamId, string playerName)
    {
      // For MySQL, we don't need to explicitly create a player record
      // Records are created when first skin/knife/etc is saved
      await Task.CompletedTask;
    }
  }

  // Keep the old Database class for backward compatibility
  public class Database : MySqlDatabase
  {
    public Database(string connectionString) : base(connectionString) { }
  }
}
