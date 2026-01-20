using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SpecMetrix.Interfaces;

namespace SpecMetrix.Shared.Logging
{
    /// <summary>
    /// MongoDB persisted log entry for SpecMetrix systems.
    /// 
    /// IMPORTANT:
    /// - We store rich Serilog properties inside the "Properties" BsonDocument.
    /// - The interface fields (Namespace, MachineName, Process, etc.) are implemented as proxy
    ///   accessors into that document.
    /// - Those proxy properties are marked [BsonIgnore] so Mongo does NOT persist them twice
    ///   (top-level + inside Properties) which can cause clobbering during deserialize.
    /// </summary>
    [BsonIgnoreExtraElements]
    public sealed class MongoLogEntry : ILogEntry
    {
        public MongoLogEntry()
        {
            // Ensure non-null defaults to satisfy ILogEntry contract and tolerate partial inputs.
            Properties = new BsonDocument();
            Message = string.Empty;
            MessageTemplate = string.Empty;
            RenderedMessage = string.Empty;
            TemplateValues = new Dictionary<string, object>();
        }

        /// <summary>
        /// Stable event taxonomy identifier (e.g. CFG_PARSE_101).
        /// </summary>
        public string? EventId { get; set; }

        /// <summary>
        /// MongoDB internal identifier.
        /// </summary>
        [BsonId] // Maps to MongoDB "_id"
        public ObjectId Id { get; set; }

        /// <summary>
        /// Unique identifier for the log entry (SpecMetrix contract).
        /// </summary>
        public Guid LogId { get; set; }

        /// <summary>
        /// Timestamp the event occurred.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Logging level.
        /// </summary>
        public LogLevel Level { get; set; }

        /// <summary>
        /// Primary message (human readable).
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Number of repetitive occurrences without stopping.
        /// </summary>
        public ulong Occurrences { get; set; }

        /// <summary>
        /// Optional contextual data (string,string per current ILogEntry contract).
        /// </summary>
        public IDictionary<string, string>? Metadata { get; set; }

        /// <summary>
        /// Exception message (optional).
        /// </summary>
        public string? ExceptionMessage { get; set; }

        /// <summary>
        /// Stack trace (optional).
        /// </summary>
        public string? StackTrace { get; set; }

        /// <summary>
        /// Serilog message template (may be empty).
        /// </summary>
        public string MessageTemplate { get; set; } = string.Empty;

        /// <summary>
        /// Serilog template values (non-null per ILogEntry contract).
        /// </summary>
        public IDictionary<string, object> TemplateValues { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Rendered message (may be empty).
        /// </summary>
        public string RenderedMessage { get; set; } = string.Empty;

        /// <summary>
        /// Captured Serilog "Properties" (structured event payload).
        /// This is the authoritative store for fields like Namespace/MachineName/Process/etc.
        /// </summary>
        [BsonElement("Properties")]
        public BsonDocument Properties { get; set; } = new BsonDocument();

        // --------------------------
        // Proxy properties into Properties BsonDocument
        // These are part of ILogEntry but should NOT be persisted as top-level fields.
        // --------------------------

        [BsonIgnore]
        public string Namespace
        {
            get => GetString("Namespace") ?? string.Empty;
            set => SetStringOrRemove("Namespace", value);
        }

        [BsonIgnore]
        public string MachineName
        {
            get => GetString("MachineName") ?? string.Empty;
            set => SetStringOrRemove("MachineName", value);
        }

        [BsonIgnore]
        public int Code
        {
            get => GetInt32("Code");
            set => EnsureProperties()["Code"] = value;
        }

        [BsonIgnore]
        public string Process
        {
            get => GetString("Process") ?? string.Empty;
            set => SetStringOrRemove("Process", value);
        }

        [BsonIgnore]
        public string? ClassMethod
        {
            get => GetString("ClassMethod");
            set => SetStringOrRemove("ClassMethod", value);
        }

        [BsonIgnore]
        public string? Source
        {
            get => GetString("Source");
            set => SetStringOrRemove("Source", value);
        }

        [BsonIgnore]
        public LogCategory? Category
        {
            get
            {
                var s = GetString("Category");
                if (string.IsNullOrWhiteSpace(s)) return null;
                return Enum.TryParse(s, out LogCategory cat) ? cat : (LogCategory?)null;
            }
            set
            {
                if (value.HasValue)
                    EnsureProperties()["Category"] = value.Value.ToString();
                else
                    EnsureProperties().Remove("Category");
            }
        }

        [BsonIgnore]
        public string? DeviceName
        {
            get => GetString("DeviceName");
            set => SetStringOrRemove("DeviceName", value);
        }

        // --------------------------
        // Internal helpers
        // --------------------------

        private BsonDocument EnsureProperties()
        {
            Properties ??= new BsonDocument();
            return Properties;
        }

        private string? GetString(string key)
        {
            var doc = EnsureProperties();
            if (doc.Contains(key) && doc[key].IsString)
                return doc[key].AsString;

            return null;
        }

        private int GetInt32(string key)
        {
            var doc = EnsureProperties();
            if (doc.Contains(key))
            {
                var v = doc[key];
                if (v.IsInt32) return v.AsInt32;
                if (v.IsInt64) return checked((int)v.AsInt64);
                if (v.IsDouble) return checked((int)v.AsDouble);
            }
            return default;
        }

        private void SetStringOrRemove(string key, string? value)
        {
            var doc = EnsureProperties();
            if (!string.IsNullOrWhiteSpace(value))
                doc[key] = value!;
            else
                doc.Remove(key);
        }
    }
}
