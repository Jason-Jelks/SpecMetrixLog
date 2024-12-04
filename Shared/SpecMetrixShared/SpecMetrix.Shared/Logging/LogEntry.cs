using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SpecMetrix.Interfaces;

namespace SpecMetrix.Shared.Logging
{
    [BsonIgnoreExtraElements]
    public class LogEntry : ILogEntry
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public Guid LogId { get; set; }

        public required string Message { get; set; }

        [BsonElement("Properties")]
        public required BsonDocument Properties { get; set; } = new BsonDocument(); // Default initialization

        public required string MessageTemplate { get; set; }

        public required IDictionary<string, object> TemplateValues { get; set; } = new Dictionary<string, object>();
       
        public required string RenderedMessage { get; set; }

        [BsonIgnore]
        public string Namespace
        {
            get
            {
                return Properties.Contains("Namespace") && Properties["Namespace"].IsString
                    ? Properties["Namespace"].AsString
                    : string.Empty;
            }
            set
            {
                EnsurePropertiesInitialized(); // Ensure Properties is not null
                if (!string.IsNullOrEmpty(value))
                {
                    Properties["Namespace"] = value;
                }
                else
                {
                    Properties.Remove("Namespace");
                }
            }
        }

        public DateTime Timestamp { get; set; }

        [BsonIgnore]
        public string MachineName
        {
            get
            {
                return Properties.Contains("MachineName") && Properties["MachineName"].IsString
                    ? Properties["MachineName"].AsString
                    : string.Empty;
            }
            set
            {
                EnsurePropertiesInitialized();
                if (!string.IsNullOrEmpty(value))
                {
                    Properties["MachineName"] = value;
                }
                else
                {
                    Properties.Remove("MachineName");
                }
            }
        }

        [BsonIgnore]
        public int Code
        {
            get
            {
                return Properties.Contains("Code") && Properties["Code"].IsInt32
                    ? Properties["Code"].AsInt32
                    : default;
            }
            set
            {
                EnsurePropertiesInitialized();
                Properties["Code"] = value;
            }
        }

        [BsonIgnore]
        public string Process
        {
            get
            {
                return Properties.Contains("Process") && Properties["Process"].IsString
                    ? Properties["Process"].AsString
                    : string.Empty;
            }
            set
            {
                EnsurePropertiesInitialized();
                Properties["Process"] = value;
            }
        }

        [BsonIgnore]
        public string? ClassMethod
        {
            get
            {
                return Properties.Contains("ClassMethod") && Properties["ClassMethod"].IsString
                    ? Properties["ClassMethod"].AsString
                    : null;
            }
            set
            {
                EnsurePropertiesInitialized();
                if (!string.IsNullOrEmpty(value))
                {
                    Properties["ClassMethod"] = value;
                }
                else
                {
                    Properties.Remove("ClassMethod");
                }
            }
        }

        public ulong Occurrences { get; set; }

        public IDictionary<string, string>? Metadata { get; set; }

        [BsonIgnore]
        public string? Source
        {
            get
            {
                return Properties.Contains("Source") && Properties["Source"].IsString
                    ? Properties["Source"].AsString
                    : null;
            }
            set
            {
                EnsurePropertiesInitialized();
                if (!string.IsNullOrEmpty(value))
                {
                    Properties["Source"] = value;
                }
                else
                {
                    Properties.Remove("Source");
                }
            }
        }

        [BsonIgnore]
        public LogCategory? Category
        {
            get
            {
                if (Properties.Contains("Category") && Properties["Category"].IsString)
                {
                    return Enum.TryParse(Properties["Category"].AsString, out LogCategory category)
                        ? category
                        : (LogCategory?)null;
                }
                return null;
            }
            set
            {
                EnsurePropertiesInitialized();
                if (value.HasValue)
                {
                    Properties["Category"] = value.Value.ToString();
                }
                else
                {
                    Properties.Remove("Category");
                }
            }
        }

        [BsonIgnore]
        public string? ExceptionMessage { get; set; }

        [BsonIgnore]
        public string? StackTrace { get; set; }

        [BsonIgnore]
        public string? DeviceName
        {
            get
            {
                return Properties.Contains("DeviceName") && Properties["DeviceName"].IsString
                    ? Properties["DeviceName"].AsString
                    : null;
            }
            set
            {
                EnsurePropertiesInitialized();
                if (!string.IsNullOrEmpty(value))
                {
                    Properties["DeviceName"] = value;
                }
                else
                {
                    Properties.Remove("DeviceName");
                }
            }
        }

        public LogLevel Level { get; set; }

        private void EnsurePropertiesInitialized()
        {
            if (Properties == null)
            {
                Properties = new BsonDocument();
            }
        }
    }
}
