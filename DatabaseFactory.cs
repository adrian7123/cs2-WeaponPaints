using MySqlConnector;
using Microsoft.Extensions.Logging;

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
                                connectionString = $"mongodb://{config.DatabaseUser}:{config.DatabasePassword}@{hostPort}/{config.DatabaseName}";
                            }
                            else
                            {
                                connectionString = $"mongodb://{hostPort}/{config.DatabaseName}";
                            }
                        }
                        
                        WeaponPaints.Instance.Logger.LogInformation($"Using MongoDB database: {config.DatabaseName}");
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

                        WeaponPaints.Instance.Logger.LogInformation($"Using MySQL database: {config.DatabaseName}");
                        return new MySqlDatabase(builder.ConnectionString);
                    }
            }
        }
    }
}