using MongoDB.Bson;
using MongoDB.Driver;
using SpecMetrix.Interfaces;
using SpecMetrix.Shared.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpecMetrix.DataService
{
    public class MongoDataService : IDataService
    {
        private readonly IMongoCollection<LogEntry> _logCollection; // Change ILogEntry to LogEntry

        public MongoDataService(IMongoClient mongoClient)
        {
            var database = mongoClient.GetDatabase("Logging");
            _logCollection = database.GetCollection<LogEntry>("Logs"); // Change ILogEntry to LogEntry
        }

        public async Task WriteLogAsync(ILogEntry logEntry)
        {
            await _logCollection.InsertOneAsync((LogEntry)logEntry); // Cast ILogEntry to LogEntry
        }

        public async Task WriteLogsAsync(IEnumerable<ILogEntry> logEntries)
        {
            await _logCollection.InsertManyAsync((IEnumerable<LogEntry>)logEntries); // Cast IEnumerable<ILogEntry> to IEnumerable<LogEntry>
        }

        public async Task<IEnumerable<ILogEntry>> ReadLogsAsync(LogQueryOptions queryOptions)
        {
                var filterBuilder = Builders<LogEntry>.Filter;
                var filter = filterBuilder.Empty; // Default to no filters

                // Apply filters based on query options
                if (queryOptions.StartDate.HasValue)
                    filter &= filterBuilder.Gte(log => log.Timestamp, queryOptions.StartDate.Value);

                if (queryOptions.EndDate.HasValue)
                    filter &= filterBuilder.Lte(log => log.Timestamp, queryOptions.EndDate.Value);

                if (queryOptions.LogLevel.HasValue)
                    filter &= filterBuilder.Eq(log => log.Level, queryOptions.LogLevel.Value);

                if (!string.IsNullOrEmpty(queryOptions.Process))
                    filter &= filterBuilder.Eq(log => log.Process, queryOptions.Process);

                if (queryOptions.Category.HasValue)
                    filter &= filterBuilder.Eq(log => log.Category, queryOptions.Category.Value);

                if (!string.IsNullOrEmpty(queryOptions.Source))
                    filter &= filterBuilder.Eq(log => log.Source, queryOptions.Source);

                if (queryOptions.Code.HasValue)
                    filter &= filterBuilder.Eq(log => log.Code, queryOptions.Code.Value);

                if (!string.IsNullOrEmpty(queryOptions.ClassMethod))
                    filter &= filterBuilder.Eq(log => log.ClassMethod, queryOptions.ClassMethod);

                var logs = await _logCollection.Find(filter).ToListAsync();
                return logs.Cast<ILogEntry>(); // Cast LogEntry back to ILogEntry
        }

       
    }
}
