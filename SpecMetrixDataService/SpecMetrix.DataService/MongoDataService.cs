using MongoDB.Driver;
using SpecMetrix.Interfaces;
using SpecMetrix.Shared.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpecMetrix.DataService
{
    public sealed class MongoDataService : IDataService<MongoLogEntry>
    {
        private const string DefaultDatabaseName = "Logging";
        private const string DefaultCollectionName = "Logs";

        private readonly IMongoCollection<MongoLogEntry> _mongoLogCollection;

        public MongoDataService(IMongoClient mongoClient)
        {
            if (mongoClient == null) throw new ArgumentNullException(nameof(mongoClient));

            var database = mongoClient.GetDatabase(DefaultDatabaseName);
            _mongoLogCollection = database.GetCollection<MongoLogEntry>(DefaultCollectionName);
        }

        public async Task WriteLogAsync(MongoLogEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            await _mongoLogCollection.InsertOneAsync(entry).ConfigureAwait(false);
        }

        public async Task WriteLogsAsync(IEnumerable<MongoLogEntry> entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));

            var list = entries as IList<MongoLogEntry> ?? entries.ToList();
            if (list.Count == 0) return;

            await _mongoLogCollection.InsertManyAsync(list).ConfigureAwait(false);
        }

        public async Task<IEnumerable<MongoLogEntry>> ReadLogsAsync(LogQueryOptions queryOptions)
        {
            if (queryOptions == null) throw new ArgumentNullException(nameof(queryOptions));

            try
            {
                var filterBuilder = Builders<MongoLogEntry>.Filter;
                var filter = filterBuilder.Empty;

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

                if (queryOptions.HowManyLogsToGet.HasValue)
                {
                    query = query.SortByDescending(log => log.Timestamp)
                                 .Limit(queryOptions.HowManyLogsToGet.Value);
                }

                return await query.ToListAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while reading logs: {ex.Message}");
                return Enumerable.Empty<MongoLogEntry>();
            }
        }
    }
}
