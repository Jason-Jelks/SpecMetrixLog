using MongoDB.Bson;
using MongoDB.Driver;
using SpecMetrix.Interfaces;
using SpecMetrix.Shared.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpecMetrix.DataService
{
    /*
    * 12.05.2024 jj - Changed all MongoDataService features to use MongoLogEntry (SpecMetrix.Shared 0.1.10 required)
    *                 This is to remove conflicts for Serilog writes to MongoDB using LogEntry class.
    *                 Note However that as of this release (0.1.10) the returning interface is still based on ILogEntry
    *                 we may need to add a IMongoLogEntry for the consumers to manage correct implementation of all data
    */

    public class MongoDataService : IDataService
    {

        private readonly IMongoCollection<MongoLogEntry> _mongoLogCollection; // used for read processing

        public MongoDataService(IMongoClient mongoClient)
        {
            var database = mongoClient.GetDatabase("Logging");
            _mongoLogCollection = database.GetCollection<MongoLogEntry>("Logs"); // added _mongoLogCollection
        }

        public async Task WriteLogAsync(ILogEntry MongoLogEntry)
        {
            await _mongoLogCollection.InsertOneAsync((MongoLogEntry)MongoLogEntry); // Cast IMongoLogEntry to MongoLogEntry
        }

        public async Task WriteLogsAsync(IEnumerable<ILogEntry> logEntries)
        {
            await _mongoLogCollection.InsertManyAsync((IEnumerable<MongoLogEntry>)logEntries); // Cast IEnumerable<IMongoLogEntry> to IEnumerable<MongoLogEntry>
        }

        public async Task<IEnumerable<ILogEntry>> ReadLogsAsync(LogQueryOptions queryOptions)
        {
            try
            {
                var filterBuilder = Builders<MongoLogEntry>.Filter;
                var filter = filterBuilder.Empty; // Default to no filters

                // Apply filters based on query options
                if (queryOptions.StartDate.HasValue)
                    filter &= filterBuilder.Gte(log => log.Timestamp, queryOptions.StartDate.Value);

                if (queryOptions.EndDate.HasValue)
                    filter &= filterBuilder.Lte(log => log.Timestamp, queryOptions.EndDate.Value);

                if (queryOptions.LogLevel.HasValue)
                    filter &= filterBuilder.Eq(log => log.Level, queryOptions.LogLevel.Value);

                if (!string.IsNullOrEmpty(queryOptions.Process))
                    filter &= filterBuilder.Eq("Properties.Process", queryOptions.Process);

                if (queryOptions.Category.HasValue)
                    filter &= filterBuilder.Eq("Properties.Category", queryOptions.Category.Value.ToString());

                if (!string.IsNullOrEmpty(queryOptions.Source))
                    filter &= filterBuilder.Eq("Properties.Source", queryOptions.Source);

                if (queryOptions.Code.HasValue)
                    filter &= filterBuilder.Eq("Properties.Code", queryOptions.Code.Value);

                if (!string.IsNullOrEmpty(queryOptions.ClassMethod))
                    filter &= filterBuilder.Eq("Properties.ClassMethod", queryOptions.ClassMethod);

                var query = _mongoLogCollection.Find(filter);

                // Apply sorting and limiting if specified
                if (queryOptions.HowManyLogsToGet.HasValue)
                {
                    query = query.SortByDescending(log => log.Timestamp)
                                 .Limit(queryOptions.HowManyLogsToGet.Value);
                }

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                Console.WriteLine($"Error while reading logs: {ex.Message}");
                return []; // returns empty IEnumerable<IMongoLogEntry>
            }
        }

    }
}
