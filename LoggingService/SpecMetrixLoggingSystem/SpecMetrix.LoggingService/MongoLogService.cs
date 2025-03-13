using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.Options;
using LoggingService.Configuration;


namespace LoggingService
{
    public class MongoLogService
    {
        private readonly IMongoDatabase _database;
        private readonly MongoDbSettings _settings;

        public MongoLogService(IOptions<MongoDbSettings> settings)
        {
            _settings = settings.Value;
            var client = new MongoClient(_settings.ConnectionString);
            _database = client.GetDatabase(_settings.DatabaseName);

            EnsureDatabaseAndCollection();
        }

        /// <summary>
        /// Ensures that the MongoDB database and time-series collection exist and match the current configuration.
        /// </summary>
        public void EnsureDatabaseAndCollection()
        {
            var collections = _database.ListCollectionNames().ToList();

            if (collections.Contains(_settings.CollectionName))
            {
                if (HasCollectionChanged())
                {
                    Console.WriteLine($"Detected changes in MongoDB time-series settings for '{_settings.CollectionName}'");
                    _database.DropCollection(_settings.CollectionName);
                    CreateTimeSeriesCollection();
                }
            }
            else
            {
                CreateTimeSeriesCollection();
            }
        }

        /// <summary>
        /// Checks if the collection's settings have changed.
        /// </summary>
        private bool HasCollectionChanged()
        {
            var collectionInfo = _database.ListCollections(new ListCollectionsOptions { Filter = new BsonDocument("name", _settings.CollectionName) }).FirstOrDefault();
            if (collectionInfo == null) return false;

            var options = collectionInfo["options"].AsBsonDocument;
            var timeSeriesOptions = options.Contains("timeseries") ? options["timeseries"].AsBsonDocument : null;
            var expireAfter = options.Contains("expireAfterSeconds") ? options["expireAfterSeconds"].ToInt32() : 0;

            return timeSeriesOptions == null ||
                   timeSeriesOptions["granularity"].AsString != _settings.Granularity ||
                   timeSeriesOptions["timeField"].AsString != _settings.TimeField ||
                   expireAfter != _settings.ExpireAfterDays * 86400; // Convert days to seconds
        }

        /// <summary>
        /// Creates a MongoDB time-series collection with the configured settings.
        /// </summary>
        private void CreateTimeSeriesCollection()
        {
            var options = new CreateCollectionOptions
            {
                TimeSeriesOptions = new TimeSeriesOptions(
                    timeField: _settings.TimeField,
                    metaField: _settings.MetaField,
                    granularity: Enum.Parse<TimeSeriesGranularity>(_settings.Granularity, true)
                ),
                ExpireAfter = TimeSpan.FromDays(_settings.ExpireAfterDays)
            };

            _database.CreateCollection(_settings.CollectionName, options);
            Console.WriteLine($"MongoDB Time-Series Collection '{_settings.CollectionName}' Created Successfully!");
        }
    }
}
