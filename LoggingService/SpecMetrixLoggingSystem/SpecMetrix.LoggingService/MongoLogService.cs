using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.Options;
using LoggingService.Configuration;
using LoggingService.Health;

namespace LoggingService
{
    public class MongoLogService : IMongoWriteVerifier
    {
        private readonly IMongoDatabase _database;
        private readonly DatabaseConfig _config;
        private readonly string _activeDatabaseName;

        // Health-check collection and per-instance document id (idempotent upsert target)
        private readonly string _healthCollectionName = "_health";
        private readonly string _instanceId = $"{System.Environment.MachineName}-{System.Environment.ProcessId}";

        public MongoLogService(
            IOptions<Dictionary<string, DatabaseConfig>> dbConfigs,
            IOptions<RepositoryProfile> repoProfile)
        {
            var profile = repoProfile.Value;
            var databases = dbConfigs.Value;

            _config = TryConnect(profile.Primary, databases)
                   ?? TryConnect(profile.Mode == "Failover" ? profile.Secondary : null, databases)
                   ?? throw new InvalidOperationException("Unable to connect to primary or secondary MongoDB instance.");

            var client = new MongoClient(_config.ConnectionString);
            _database = client.GetDatabase(_config.DatabaseName);
            _activeDatabaseName = _config.DatabaseName;

            EnsureDatabaseAndCollection();
            EnsureHealthCollection();
        }

        private DatabaseConfig? TryConnect(string? dbKey, Dictionary<string, DatabaseConfig> configs)
        {
            if (string.IsNullOrWhiteSpace(dbKey) || !configs.TryGetValue(dbKey, out var config))
                return null;

            if (!string.Equals(config.Type, "MongoDb", StringComparison.OrdinalIgnoreCase))
                return null;

            try
            {
                var client = new MongoClient(config.ConnectionString);
                var pingResult = client.GetDatabase(config.DatabaseName)
                    .RunCommand<BsonDocument>(new BsonDocument("ping", 1));
                return pingResult != null ? config : null;
            }
            catch
            {
                Console.WriteLine($"Failed to connect to MongoDB database '{dbKey}'");
                return null;
            }
        }

        public void EnsureDatabaseAndCollection()
        {
            var collections = _database.ListCollectionNames().ToList();

            if (collections.Contains(_config.CollectionName))
            {
                if (HasCollectionChanged())
                {
                    Console.WriteLine($"Detected changes in MongoDB time-series settings for '{_config.CollectionName}'");
                    _database.DropCollection(_config.CollectionName);
                    CreateTimeSeriesCollection();
                }
            }
            else
            {
                CreateTimeSeriesCollection();
            }
        }

        private bool HasCollectionChanged()
        {
            var collectionInfo = _database
                .ListCollections(new ListCollectionsOptions
                {
                    Filter = new BsonDocument("name", _config.CollectionName)
                })
                .FirstOrDefault();

            if (collectionInfo == null) return false;

            var options = collectionInfo["options"].AsBsonDocument;
            var timeSeriesOptions = options.Contains("timeseries") ? options["timeseries"].AsBsonDocument : null;
            var expireAfter = options.Contains("expireAfterSeconds") ? options["expireAfterSeconds"].ToInt32() : 0;

            return timeSeriesOptions == null
                   || timeSeriesOptions["granularity"].AsString != (_config.Granularity ?? "minutes")
                   || timeSeriesOptions["timeField"].AsString != (_config.TimeField ?? "Timestamp")
                   || expireAfter != (_config.ExpireAfterDays) * 86400;
        }

        private void CreateTimeSeriesCollection()
        {
            var options = new CreateCollectionOptions
            {
                TimeSeriesOptions = new TimeSeriesOptions(
                    timeField: _config.TimeField ?? "Timestamp",
                    metaField: _config.MetaField ?? "metadata",
                    granularity: Enum.Parse<TimeSeriesGranularity>(_config.Granularity ?? "minutes", true)
                ),
                ExpireAfter = TimeSpan.FromDays(_config.ExpireAfterDays)
            };

            _database.CreateCollection(_config.CollectionName, options);
            Console.WriteLine($"MongoDB Time-Series Collection '{_config.CollectionName}' Created Successfully in '{_activeDatabaseName}'");
        }

        private void EnsureHealthCollection()
        {
            var collections = _database.ListCollectionNames().ToList();
            if (!collections.Contains(_healthCollectionName))
            {
                _database.CreateCollection(_healthCollectionName);
                Console.WriteLine($"MongoDB Health Collection '{_healthCollectionName}' Created Successfully in '{_activeDatabaseName}'");
            }
        }

        // Implements IMongoWriteVerifier for health checks
        // Uses idempotent upsert into a dedicated _health collection (no delete, minimal churn)
        public async Task<(bool ok, string error)> VerifyWriteAsync(CancellationToken ct)
        {
            try
            {
                // 1) Connectivity check
                await _database.RunCommandAsync<BsonDocument>(
                    new BsonDocument("ping", 1),
                    cancellationToken: ct);

                // 2) Acknowledged single-document upsert proves write-path health
                var coll = _database
                    .GetCollection<BsonDocument>(_healthCollectionName)
                    .WithWriteConcern(WriteConcern.W1);

                var filter = Builders<BsonDocument>.Filter.Eq("_id", _instanceId);
                var update = Builders<BsonDocument>.Update
                    .SetOnInsert("createdUtc", DateTime.UtcNow)
                    .CurrentDate("lastUtc")
                    .Inc("seq", 1);

                var options = new UpdateOptions { IsUpsert = true };

                var result = await coll.UpdateOneAsync(filter, update, options, ct);

                var acknowledged = result.IsAcknowledged && (result.MatchedCount == 1 || result.UpsertedId != null);
                return acknowledged ? (true, string.Empty) : (false, "Write not acknowledged");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}
