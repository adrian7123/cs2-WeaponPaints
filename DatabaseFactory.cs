using MySqlConnector;

namespace WeaponPaints
{
  public static class DatabaseFactory
  {
    public static IDatabase CreateDatabase(WeaponPaintsConfig config)
    {
      var databaseType = config.DatabaseType.ToLowerInvariant();

      switch (databaseType)
      {
        case "mongodb":
        case "mongo":
          {
            string connectionString;

            // Use MongoConnectionString if provided, otherwise build from individual parameters
            if (!string.IsNullOrEmpty(config.MongoConnectionString))
            {
              connectionString = config.MongoConnectionString;
            }
            else
            {
              // Build MongoDB connection string from individual parameters
              var hostPort = $"{config.DatabaseHost}:{config.DatabasePort}";

              if (!string.IsNullOrEmpty(config.DatabaseUser) && !string.IsNullOrEmpty(config.DatabasePassword))
              {
                // Add authSource=admin for admin users, or use the database name for regular users
                var authSource = config.DatabaseUser.ToLowerInvariant() == "admin" ? "admin" : config.DatabaseName;
                connectionString = $"mongodb://{config.DatabaseUser}:{config.DatabasePassword}@{hostPort}/{config.DatabaseName}?authSource={authSource}";
                Console.WriteLine($"[WeaponPaints] MongoDB connection string (credentials hidden): mongodb://{config.DatabaseUser}:***@{hostPort}/{config.DatabaseName}?authSource={authSource}");
              }
              else
              {
                connectionString = $"mongodb://{hostPort}/{config.DatabaseName}";
                Console.WriteLine($"[WeaponPaints] MongoDB connection string: {connectionString}");
              }
            }

            Console.WriteLine($"[WeaponPaints] Creating MongoDB database connection for: {config.DatabaseName}");
            return new MongoDatabase(connectionString, config.DatabaseName);
          }

        case "mysql":
        default:
          {
            var builder = new MySqlConnectionStringBuilder
            {
              Server = config.DatabaseHost,
              UserID = config.DatabaseUser,
              Password = config.DatabasePassword,
              Database = config.DatabaseName,
              Port = (uint)config.DatabasePort,
              Pooling = true,
              MaximumPoolSize = 640,
            };

            return new MySqlDatabase(builder.ConnectionString);
          }
      }
    }
  }
}
