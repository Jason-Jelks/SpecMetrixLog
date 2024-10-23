using MongoDB.Driver;
using SpecMetrix.Interfaces;

namespace SpecMetrix.DataService
{
    public class MongoDataService : IDataService
    {
        private readonly IMongoCollection<ILogEntry> _logCollection;

        public MongoDataService(IMongoClient mongoClient)
        {
            var database = mongoClient.GetDatabase("LoggingDB");
            _logCollection = database.GetCollection<ILogEntry>("Logs");
        }

        public async Task WriteLogAsync(ILogEntry logEntry)
        {
            await _logCollection.InsertOneAsync(logEntry);
        }

        public async Task WriteLogsAsync(IEnumerable<ILogEntry> logEntry)
        {
            await _logCollection.InsertManyAsync(logEntry);
        }


        public async Task<IEnumerable<ILogEntry>> ReadLogsAsync(LogQueryOptions queryOptions)
        {
            var filterBuilder = Builders<ILogEntry>.Filter;
            var filter = filterBuilder.Empty; // Default to no filters

            // Apply filters based on query options (date range, log level, etc.)
            if (queryOptions.StartDate.HasValue)
                filter &= filterBuilder.Gte(log => log.Timestamp, queryOptions.StartDate.Value);

            if (queryOptions.EndDate.HasValue)
                filter &= filterBuilder.Lte(log => log.Timestamp, queryOptions.EndDate.Value);

            if (queryOptions.LogLevel.HasValue)
                filter &= filterBuilder.Eq(log => log.Level, queryOptions.LogLevel.Value);

            if (!string.IsNullOrEmpty(queryOptions.Process))
                filter &= filterBuilder.Eq(log => log.Process, queryOptions.Process);

            return await _logCollection.Find(filter).ToListAsync();
        }
    }
}
