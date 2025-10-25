using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Microsoft.Extensions.Logging;

namespace WeaponPaints
{
  public class MongoDatabase : IDatabase
  {
    private readonly IMongoDatabase _database;
    private readonly MongoClient _client;

    public MongoDatabase(string connectionString, string databaseName)
    {
      _client = new MongoClient(connectionString);
      _database = _client.GetDatabase(databaseName);
    }

    public async Task<bool> TestConnectionAsync()
    {
      try
      {
        Console.WriteLine($"[WeaponPaints] Testing MongoDB connection to database: {_database.DatabaseNamespace.DatabaseName}");
        await _database.RunCommandAsync((Command<BsonDocument>)"{ping:1}");
        Console.WriteLine($"[WeaponPaints] MongoDB connection successful!");
        return true;
      }
      catch (MongoDB.Driver.MongoAuthenticationException authEx)
      {
        Console.WriteLine($"[WeaponPaints] MongoDB Authentication Error: {authEx.Message}");
        Console.WriteLine($"[WeaponPaints] Check your MongoDB credentials and authentication database.");
        Console.WriteLine($"[WeaponPaints] Common solutions:");
        Console.WriteLine($"[WeaponPaints] 1. Verify username and password are correct");
        Console.WriteLine($"[WeaponPaints] 2. Add '?authSource=admin' to your connection string");
        Console.WriteLine($"[WeaponPaints] 3. Use the correct authentication database");
        return false;
      }
      catch (MongoDB.Driver.MongoConnectionException connEx)
      {
        Console.WriteLine($"[WeaponPaints] MongoDB Connection Error: {connEx.Message}");
        Console.WriteLine($"[WeaponPaints] Check your MongoDB host and port configuration.");
        return false;
      }
      catch (Exception ex)
      {
        Console.WriteLine($"[WeaponPaints] MongoDB Error: {ex.GetType().Name}: {ex.Message}");
        if (ex.InnerException != null)
        {
          Console.WriteLine($"[WeaponPaints] Inner Exception: {ex.InnerException.Message}");
        }
        return false;
      }
    }

    public async Task<bool> CreateTablesAsync()
    {
      try
      {
        // MongoDB creates collections automatically when first document is inserted
        // We can create indexes here for better performance

        var knifeCollection = _database.GetCollection<PlayerKnife>("knives");
        await knifeCollection.Indexes.CreateOneAsync(new CreateIndexModel<PlayerKnife>(
            Builders<PlayerKnife>.IndexKeys.Ascending(x => x.SteamId)));

        var gloveCollection = _database.GetCollection<PlayerGlove>("gloves");
        await gloveCollection.Indexes.CreateOneAsync(new CreateIndexModel<PlayerGlove>(
            Builders<PlayerGlove>.IndexKeys.Ascending(x => x.SteamId)));

        var agentCollection = _database.GetCollection<PlayerAgent>("agents");
        await agentCollection.Indexes.CreateOneAsync(new CreateIndexModel<PlayerAgent>(
            Builders<PlayerAgent>.IndexKeys.Ascending(x => x.SteamId)));

        var musicCollection = _database.GetCollection<PlayerMusic>("music");
        await musicCollection.Indexes.CreateOneAsync(new CreateIndexModel<PlayerMusic>(
            Builders<PlayerMusic>.IndexKeys.Ascending(x => x.SteamId)));

        var skinsCollection = _database.GetCollection<PlayerWeaponSkin>("skins");
        await skinsCollection.Indexes.CreateOneAsync(new CreateIndexModel<PlayerWeaponSkin>(
            Builders<PlayerWeaponSkin>.IndexKeys.Ascending(x => x.SteamId)));

        var pinsCollection = _database.GetCollection<PlayerPin>("pins");
        await pinsCollection.Indexes.CreateOneAsync(new CreateIndexModel<PlayerPin>(
            Builders<PlayerPin>.IndexKeys.Ascending(x => x.SteamId)));

        Console.WriteLine($"[WeaponPaints] MongoDB collections and indexes created successfully!");
        return true;
      }
      catch (MongoDB.Driver.MongoAuthenticationException authEx)
      {
        Console.WriteLine($"[WeaponPaints] MongoDB Authentication Error during index creation: {authEx.Message}");
        Console.WriteLine($"[WeaponPaints] The connection worked but authentication failed during operations.");
        Console.WriteLine($"[WeaponPaints] This usually means the user doesn't have write permissions on the database.");
        return false;
      }
      catch (Exception ex)
      {
        Console.WriteLine($"[WeaponPaints] Error creating MongoDB collections/indexes: {ex.GetType().Name}: {ex.Message}");
        if (ex.InnerException != null)
        {
          Console.WriteLine($"[WeaponPaints] Inner Exception: {ex.InnerException.Message}");
        }
        return false;
      }
    }

    public async Task<Dictionary<string, object>[]> GetPlayerKnivesAsync(string steamId)
    {
      var collection = _database.GetCollection<PlayerKnife>("knives");
      var filter = Builders<PlayerKnife>.Filter.Eq(x => x.SteamId, steamId);
      var knives = await collection.Find(filter).ToListAsync();

      return knives.Select(k => new Dictionary<string, object>
            {
                { "knife", k.Knife ?? string.Empty },
                { "weapon_team", k.WeaponTeam }
            }).ToArray();
    }

    public async Task SavePlayerKnifeAsync(string steamId, string knife, int weaponTeam)
    {
      var collection = _database.GetCollection<PlayerKnife>("knives");
      var filter = Builders<PlayerKnife>.Filter.And(
          Builders<PlayerKnife>.Filter.Eq(x => x.SteamId, steamId),
          Builders<PlayerKnife>.Filter.Eq(x => x.WeaponTeam, weaponTeam)
      );

      var update = Builders<PlayerKnife>.Update
          .Set(x => x.Knife, knife)
          .Set(x => x.SteamId, steamId)
          .Set(x => x.WeaponTeam, weaponTeam);

      await collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
    }

    public async Task<Dictionary<string, object>[]> GetPlayerGlovesAsync(string steamId)
    {
      var collection = _database.GetCollection<PlayerGlove>("gloves");
      var filter = Builders<PlayerGlove>.Filter.Eq(x => x.SteamId, steamId);
      var gloves = await collection.Find(filter).ToListAsync();

      return gloves.Select(g => new Dictionary<string, object>
            {
                { "weapon_defindex", g.WeaponDefindex },
                { "weapon_team", g.WeaponTeam }
            }).ToArray();
    }

    public async Task SavePlayerGloveAsync(string steamId, int weaponDefindex, int weaponTeam)
    {
      var collection = _database.GetCollection<PlayerGlove>("gloves");
      var filter = Builders<PlayerGlove>.Filter.And(
          Builders<PlayerGlove>.Filter.Eq(x => x.SteamId, steamId),
          Builders<PlayerGlove>.Filter.Eq(x => x.WeaponTeam, weaponTeam)
      );

      var update = Builders<PlayerGlove>.Update
          .Set(x => x.WeaponDefindex, weaponDefindex)
          .Set(x => x.SteamId, steamId)
          .Set(x => x.WeaponTeam, weaponTeam);

      await collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
    }

    public async Task<Dictionary<string, object>[]> GetPlayerAgentsAsync(string steamId)
    {
      var collection = _database.GetCollection<PlayerAgent>("agents");
      var filter = Builders<PlayerAgent>.Filter.Eq(x => x.SteamId, steamId);
      var agents = await collection.Find(filter).ToListAsync();

      return agents.Select(a => new Dictionary<string, object>
            {
                { "agent", a.Agent ?? string.Empty },
                { "weapon_team", a.WeaponTeam }
            }).ToArray();
    }

    public async Task SavePlayerAgentAsync(string steamId, string agent, int weaponTeam)
    {
      var collection = _database.GetCollection<PlayerAgent>("agents");
      var filter = Builders<PlayerAgent>.Filter.And(
          Builders<PlayerAgent>.Filter.Eq(x => x.SteamId, steamId),
          Builders<PlayerAgent>.Filter.Eq(x => x.WeaponTeam, weaponTeam)
      );

      var update = Builders<PlayerAgent>.Update
          .Set(x => x.Agent, agent)
          .Set(x => x.SteamId, steamId)
          .Set(x => x.WeaponTeam, weaponTeam);

      await collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
    }

    public async Task<Dictionary<string, object>[]> GetPlayerMusicAsync(string steamId)
    {
      var collection = _database.GetCollection<PlayerMusic>("music");
      var filter = Builders<PlayerMusic>.Filter.Eq(x => x.SteamId, steamId);
      var music = await collection.Find(filter).ToListAsync();

      return music.Select(m => new Dictionary<string, object>
            {
                { "music_id", m.MusicId }
            }).ToArray();
    }

    public async Task SavePlayerMusicAsync(string steamId, int musicId)
    {
      var collection = _database.GetCollection<PlayerMusic>("music");
      var filter = Builders<PlayerMusic>.Filter.Eq(x => x.SteamId, steamId);

      var update = Builders<PlayerMusic>.Update
          .Set(x => x.MusicId, musicId)
          .Set(x => x.SteamId, steamId);

      await collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
    }

    public async Task<Dictionary<string, object>[]> GetPlayerWeaponSkinsAsync(string steamId)
    {
      var collection = _database.GetCollection<PlayerWeaponSkin>("skins");
      var filter = Builders<PlayerWeaponSkin>.Filter.Eq(x => x.SteamId, steamId);
      var skins = await collection.Find(filter).ToListAsync();

      return skins.Select(s => new Dictionary<string, object>
            {
                { "weapon_defindex", s.WeaponDefindex ?? string.Empty },
                { "weapon_paint_id", s.WeaponPaintId },
                { "weapon_wear", s.WeaponWear },
                { "weapon_seed", s.WeaponSeed },
                { "weapon_nametag", s.WeaponNametag ?? string.Empty },
                { "weapon_stattrak", s.WeaponStattrak }
            }).ToArray();
    }

    public async Task SavePlayerWeaponSkinAsync(string steamId, string weaponDefindex, int weaponPaintId, float weaponWear, int weaponSeed, string weaponNametag, int weaponStattrak)
    {
      var collection = _database.GetCollection<PlayerWeaponSkin>("skins");
      var filter = Builders<PlayerWeaponSkin>.Filter.And(
          Builders<PlayerWeaponSkin>.Filter.Eq(x => x.SteamId, steamId),
          Builders<PlayerWeaponSkin>.Filter.Eq(x => x.WeaponDefindex, weaponDefindex)
      );

      var update = Builders<PlayerWeaponSkin>.Update
          .Set(x => x.WeaponPaintId, weaponPaintId)
          .Set(x => x.WeaponWear, weaponWear)
          .Set(x => x.WeaponSeed, weaponSeed)
          .Set(x => x.WeaponNametag, weaponNametag)
          .Set(x => x.WeaponStattrak, weaponStattrak)
          .Set(x => x.SteamId, steamId)
          .Set(x => x.WeaponDefindex, weaponDefindex);

      await collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
    }

    public async Task UpdatePlayerWeaponStatTrakAsync(string steamId, string weaponDefindex, int statTrakCount)
    {
      var collection = _database.GetCollection<PlayerWeaponSkin>("skins");
      var filter = Builders<PlayerWeaponSkin>.Filter.And(
          Builders<PlayerWeaponSkin>.Filter.Eq(x => x.SteamId, steamId),
          Builders<PlayerWeaponSkin>.Filter.Eq(x => x.WeaponDefindex, weaponDefindex)
      );

      var update = Builders<PlayerWeaponSkin>.Update.Set(x => x.WeaponStattrak, statTrakCount);
      await collection.UpdateOneAsync(filter, update);
    }

    public async Task<Dictionary<string, object>[]> GetPlayerPinsAsync(string steamId)
    {
      var collection = _database.GetCollection<PlayerPin>("pins");
      var filter = Builders<PlayerPin>.Filter.Eq(x => x.SteamId, steamId);
      var pins = await collection.Find(filter).ToListAsync();

      return pins.Select(p => new Dictionary<string, object>
            {
                { "pin_id", p.PinId }
            }).ToArray();
    }

    public async Task SavePlayerPinAsync(string steamId, int pinId)
    {
      var collection = _database.GetCollection<PlayerPin>("pins");
      var filter = Builders<PlayerPin>.Filter.Eq(x => x.SteamId, steamId);

      var update = Builders<PlayerPin>.Update
          .Set(x => x.PinId, pinId)
          .Set(x => x.SteamId, steamId);

      await collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
    }

    public async Task<bool> PlayerExistsAsync(string steamId)
    {
      var collection = _database.GetCollection<PlayerKnife>("knives");
      var filter = Builders<PlayerKnife>.Filter.Eq(x => x.SteamId, steamId);
      var count = await collection.CountDocumentsAsync(filter);
      return count > 0;
    }

    public async Task CreatePlayerAsync(string steamId, string playerName)
    {
      // MongoDB doesn't require explicit player creation
      // Documents are created when first data is saved
      await Task.CompletedTask;
    }
  }

  // MongoDB document models
  public class PlayerKnife
  {
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("steamid")]
    public string SteamId { get; set; } = string.Empty;

    [BsonElement("knife")]
    public string? Knife { get; set; }

    [BsonElement("weapon_team")]
    public int WeaponTeam { get; set; }
  }

  public class PlayerGlove
  {
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("steamid")]
    public string SteamId { get; set; } = string.Empty;

    [BsonElement("weapon_defindex")]
    public int WeaponDefindex { get; set; }

    [BsonElement("weapon_team")]
    public int WeaponTeam { get; set; }
  }

  public class PlayerAgent
  {
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("steamid")]
    public string SteamId { get; set; } = string.Empty;

    [BsonElement("agent")]
    public string? Agent { get; set; }

    [BsonElement("weapon_team")]
    public int WeaponTeam { get; set; }
  }

  public class PlayerMusic
  {
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("steamid")]
    public string SteamId { get; set; } = string.Empty;

    [BsonElement("music_id")]
    public int MusicId { get; set; }
  }

  public class PlayerWeaponSkin
  {
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("steamid")]
    public string SteamId { get; set; } = string.Empty;

    [BsonElement("weapon_defindex")]
    public string? WeaponDefindex { get; set; }

    [BsonElement("weapon_paint_id")]
    public int WeaponPaintId { get; set; }

    [BsonElement("weapon_wear")]
    public float WeaponWear { get; set; }

    [BsonElement("weapon_seed")]
    public int WeaponSeed { get; set; }

    [BsonElement("weapon_nametag")]
    public string? WeaponNametag { get; set; }

    [BsonElement("weapon_stattrak")]
    public int WeaponStattrak { get; set; }
  }

  public class PlayerPin
  {
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("steamid")]
    public string SteamId { get; set; } = string.Empty;

    [BsonElement("pin_id")]
    public int PinId { get; set; }
  }
}
