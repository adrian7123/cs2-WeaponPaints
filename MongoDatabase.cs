using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

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
      Console.WriteLine($"[WeaponPaints] MongoDB initialized with BsonIgnoreExtraElements - unknown fields like '__v' will be ignored");
    }

    public async Task<long> GetSkinsCollectionCountAsync()
    {
      try
      {
        var collection = _database.GetCollection<PlayerWeaponSkin>("skins");
        var count = await collection.CountDocumentsAsync(FilterDefinition<PlayerWeaponSkin>.Empty);
        Console.WriteLine($"[WeaponPaints] MongoDB Diagnostic: Found {count} total documents in 'skins' collection");
        return count;
      }
      catch (Exception ex)
      {
        Console.WriteLine($"[WeaponPaints] Error counting skins collection: {ex.Message}");
        return -1;
      }
    }

    public async Task<BsonDocument[]> GetRawSkinsAsync(string steamId)
    {
      try
      {
        var collection = _database.GetCollection<BsonDocument>("skins");
        var filter = Builders<BsonDocument>.Filter.Eq("steamid", steamId);
        var rawDocs = await collection.Find(filter).ToListAsync();

        Console.WriteLine($"[WeaponPaints] Raw MongoDB documents for {steamId}:");
        foreach (var doc in rawDocs)
        {
          Console.WriteLine($"[WeaponPaints] Raw doc: {doc.ToJson()}");
        }

        return rawDocs.ToArray();
      }
      catch (Exception ex)
      {
        Console.WriteLine($"[WeaponPaints] Error getting raw skins: {ex.Message}");
        return Array.Empty<BsonDocument>();
      }
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
      Console.WriteLine($"[WeaponPaints] MongoDB Query: Getting knives for player {steamId}");
      var collection = _database.GetCollection<PlayerKnife>("knives");
      var filter = Builders<PlayerKnife>.Filter.Eq(x => x.SteamId, steamId);
      var knives = await collection.Find(filter).ToListAsync();

      var result = knives.Select(k => new Dictionary<string, object>
            {
                { "knife", k.Knife ?? string.Empty },
                { "weapon_team", k.WeaponTeam }
            }).ToArray();

      Console.WriteLine($"[WeaponPaints] MongoDB Result: Found {result.Length} knives for player {steamId}");
      foreach (var knife in result)
      {
        Console.WriteLine($"[WeaponPaints] Knife: {knife["knife"]}, Team: {knife["weapon_team"]}");
      }

      return result;
    }

    public async Task SavePlayerKnifeAsync(string steamId, string knife, int weaponTeam)
    {
      Console.WriteLine($"[WeaponPaints] MongoDB Save: Saving knife '{knife}' for player {steamId}, team {weaponTeam}");
      var collection = _database.GetCollection<PlayerKnife>("knives");
      var filter = Builders<PlayerKnife>.Filter.And(
          Builders<PlayerKnife>.Filter.Eq(x => x.SteamId, steamId),
          Builders<PlayerKnife>.Filter.Eq(x => x.WeaponTeam, weaponTeam)
      );

      var update = Builders<PlayerKnife>.Update
          .Set(x => x.Knife, knife)
          .Set(x => x.SteamId, steamId)
          .Set(x => x.WeaponTeam, weaponTeam);

      var result = await collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
      Console.WriteLine($"[WeaponPaints] MongoDB Save Result: Modified {result.ModifiedCount}, Upserted {result.UpsertedId != null}");
    }

    public async Task<Dictionary<string, object>[]> GetPlayerGlovesAsync(string steamId)
    {
      Console.WriteLine($"[WeaponPaints] MongoDB Query: Getting gloves for player {steamId}");
      var collection = _database.GetCollection<PlayerGlove>("gloves");
      var filter = Builders<PlayerGlove>.Filter.Eq(x => x.SteamId, steamId);
      var gloves = await collection.Find(filter).ToListAsync();

      var result = gloves.Select(g => new Dictionary<string, object>
            {
                { "weapon_defindex", g.WeaponDefindex },
                { "weapon_team", g.WeaponTeam }
            }).ToArray();

      Console.WriteLine($"[WeaponPaints] MongoDB Result: Found {result.Length} gloves for player {steamId}");
      foreach (var glove in result)
      {
        Console.WriteLine($"[WeaponPaints] Glove DefIndex: {glove["weapon_defindex"]}, Team: {glove["weapon_team"]}");
      }

      return result;
    }

    public async Task SavePlayerGloveAsync(string steamId, int weaponDefindex, int weaponTeam)
    {
      Console.WriteLine($"[WeaponPaints] MongoDB Save: Saving glove defindex {weaponDefindex} for player {steamId}, team {weaponTeam}");
      var collection = _database.GetCollection<PlayerGlove>("gloves");
      var filter = Builders<PlayerGlove>.Filter.And(
          Builders<PlayerGlove>.Filter.Eq(x => x.SteamId, steamId),
          Builders<PlayerGlove>.Filter.Eq(x => x.WeaponTeam, weaponTeam)
      );

      var update = Builders<PlayerGlove>.Update
          .Set(x => x.WeaponDefindex, weaponDefindex)
          .Set(x => x.SteamId, steamId)
          .Set(x => x.WeaponTeam, weaponTeam);

      var result = await collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
      Console.WriteLine($"[WeaponPaints] MongoDB Save Result: Modified {result.ModifiedCount}, Upserted {result.UpsertedId != null}");
    }

    public async Task<Dictionary<string, object>[]> GetPlayerAgentsAsync(string steamId)
    {
      Console.WriteLine($"[WeaponPaints] MongoDB Query: Getting agents for player {steamId}");
      var collection = _database.GetCollection<PlayerAgent>("agents");
      var filter = Builders<PlayerAgent>.Filter.Eq(x => x.SteamId, steamId);
      var agents = await collection.Find(filter).ToListAsync();

      var result = agents.Select(a => new Dictionary<string, object>
            {
                { "agent", a.Agent ?? string.Empty },
                { "weapon_team", a.WeaponTeam }
            }).ToArray();

      Console.WriteLine($"[WeaponPaints] MongoDB Result: Found {result.Length} agents for player {steamId}");
      foreach (var agent in result)
      {
        Console.WriteLine($"[WeaponPaints] Agent: {agent["agent"]}, Team: {agent["weapon_team"]}");
      }

      return result;
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
      Console.WriteLine($"[WeaponPaints] MongoDB Query: Getting music for player {steamId}");
      var collection = _database.GetCollection<PlayerMusic>("music");
      var filter = Builders<PlayerMusic>.Filter.Eq(x => x.SteamId, steamId);
      var music = await collection.Find(filter).ToListAsync();

      var result = music.Select(m => new Dictionary<string, object>
            {
                { "music_id", m.MusicId }
            }).ToArray();

      Console.WriteLine($"[WeaponPaints] MongoDB Result: Found {result.Length} music items for player {steamId}");
      foreach (var musicItem in result)
      {
        Console.WriteLine($"[WeaponPaints] Music ID: {musicItem["music_id"]}");
      }

      return result;
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
      Console.WriteLine($"[WeaponPaints] MongoDB Query: Getting weapon skins for player {steamId}");
      var collection = _database.GetCollection<PlayerWeaponSkin>("skins");
      var filter = Builders<PlayerWeaponSkin>.Filter.Eq(x => x.SteamId, steamId);
      var skins = await collection.Find(filter).ToListAsync();

      var result = skins.Select(s => new Dictionary<string, object>
            {
                { "weapon_defindex", s.WeaponDefindex.ToString() },
                { "weapon_paint_id", s.WeaponPaintId },
                { "weapon_wear", s.WeaponWear },
                { "weapon_seed", s.WeaponSeed },
                { "weapon_nametag", s.WeaponNametag ?? string.Empty },
                { "weapon_stattrak", s.WeaponStattrak }
            }).ToArray();

      Console.WriteLine($"[WeaponPaints] MongoDB Result: Found {result.Length} weapon skins for player {steamId}");
      foreach (var skin in result)
      {
        Console.WriteLine($"[WeaponPaints] Skin - DefIndex: {skin["weapon_defindex"]}, PaintID: {skin["weapon_paint_id"]}, Wear: {skin["weapon_wear"]}, Seed: {skin["weapon_seed"]}, Nametag: {skin["weapon_nametag"]}, StatTrak: {skin["weapon_stattrak"]}");
      }

      return result;
    }

    public async Task SavePlayerWeaponSkinAsync(string steamId, int weaponDefindex, int weaponPaintId, float weaponWear, int weaponSeed, string weaponNametag, int weaponStattrak)
    {
      Console.WriteLine($"[WeaponPaints] MongoDB Save: Saving weapon skin for player {steamId}");
      Console.WriteLine($"[WeaponPaints] Weapon Details - DefIndex: {weaponDefindex}, PaintID: {weaponPaintId}, Wear: {weaponWear}, Seed: {weaponSeed}, Nametag: '{weaponNametag}', StatTrak: {weaponStattrak}");

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
          .Set(x => x.WeaponDefindex, weaponDefindex); var result = await collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
      Console.WriteLine($"[WeaponPaints] MongoDB Save Result: Modified {result.ModifiedCount}, Upserted {result.UpsertedId != null}");
    }

    public async Task UpdatePlayerWeaponStatTrakAsync(string steamId, int weaponDefindex, int statTrakCount)
    {
      Console.WriteLine($"[WeaponPaints] MongoDB Update: Updating StatTrak count to {statTrakCount} for player {steamId}, weapon {weaponDefindex}");
      var collection = _database.GetCollection<PlayerWeaponSkin>("skins");
      var filter = Builders<PlayerWeaponSkin>.Filter.And(
          Builders<PlayerWeaponSkin>.Filter.Eq(x => x.SteamId, steamId),
          Builders<PlayerWeaponSkin>.Filter.Eq(x => x.WeaponDefindex, weaponDefindex)
      );

      var update = Builders<PlayerWeaponSkin>.Update.Set(x => x.WeaponStattrak, statTrakCount);
      var result = await collection.UpdateOneAsync(filter, update);
      Console.WriteLine($"[WeaponPaints] MongoDB Update Result: Modified {result.ModifiedCount} documents");
    }

    public async Task<Dictionary<string, object>[]> GetPlayerPinsAsync(string steamId)
    {
      Console.WriteLine($"[WeaponPaints] MongoDB Query: Getting pins for player {steamId}");
      var collection = _database.GetCollection<PlayerPin>("pins");
      var filter = Builders<PlayerPin>.Filter.Eq(x => x.SteamId, steamId);
      var pins = await collection.Find(filter).ToListAsync();

      var result = pins.Select(p => new Dictionary<string, object>
            {
                { "pin_id", p.PinId }
            }).ToArray();

      Console.WriteLine($"[WeaponPaints] MongoDB Result: Found {result.Length} pins for player {steamId}");
      foreach (var pin in result)
      {
        Console.WriteLine($"[WeaponPaints] Pin ID: {pin["pin_id"]}");
      }

      return result;
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
      Console.WriteLine($"[WeaponPaints] MongoDB Query: Checking if player {steamId} exists");
      var collection = _database.GetCollection<PlayerKnife>("knives");
      var filter = Builders<PlayerKnife>.Filter.Eq(x => x.SteamId, steamId);
      var count = await collection.CountDocumentsAsync(filter);
      bool exists = count > 0;
      Console.WriteLine($"[WeaponPaints] MongoDB Result: Player {steamId} exists: {exists} (found {count} documents)");
      return exists;
    }

    public async Task CreatePlayerAsync(string steamId, string playerName)
    {
      // MongoDB doesn't require explicit player creation
      // Documents are created when first data is saved
      await Task.CompletedTask;
    }
  }

  // MongoDB document models
  [BsonIgnoreExtraElements]
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

  [BsonIgnoreExtraElements]
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

  [BsonIgnoreExtraElements]
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

  [BsonIgnoreExtraElements]
  public class PlayerMusic
  {
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("steamid")]
    public string SteamId { get; set; } = string.Empty;

    [BsonElement("music_id")]
    public int MusicId { get; set; }
  }

  [BsonIgnoreExtraElements]
  public class PlayerWeaponSkin
  {
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("steamid")]
    public string SteamId { get; set; } = string.Empty;

    [BsonElement("weapon_defindex")]
    public int WeaponDefindex { get; set; }

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

  [BsonIgnoreExtraElements]
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
