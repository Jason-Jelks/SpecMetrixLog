using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SpecMetrix.Interfaces;

namespace SpecMetrix.Shared.Logging
{
    /// <summary>
    /// Log Entry for SpecMetrix systems
    /// </summary>
    [BsonIgnoreExtraElements]
    public class MongoLogEntry : ILogEntry
    {
        [BsonId] // Maps to MongoDB's _id field
        public ObjectId Id { get; set; }

        public Guid LogId { get; set; }

        public required string Message { get; set; }

        [BsonElement("Properties")]
        public BsonDocument Properties { get; set; } = new BsonDocument();

        public required string MessageTemplate { get; set; }

        public required IDictionary<string, object> TemplateValues { get; set; }

        public required string RenderedMessage { get; set; }

        public DateTime Timestamp { get; set; }

        public string Namespace
        {
            get
            {
                if (Properties.Contains("Namespace") && Properties["Namespace"].IsString)
                {
                    return Properties["Namespace"].AsString;
                }
                return string.Empty;
            }
            set
            {
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

        public string MachineName
        {
            get
            {
                if (Properties.Contains("MachineName") && Properties["MachineName"].IsString)
                {
                    return Properties["MachineName"].AsString;
                }
                return string.Empty;
            }
            set
            {
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

        public int Code
        {
            get
            {
                if (Properties.Contains("Code") && Properties["Code"].IsInt32)
                {
                    return Properties["Code"].AsInt32;
                }
                return default;
            }
            set => Properties["Code"] = value;
        }

        public string Process
        {
            get
            {
                if (Properties.Contains("Process") && Properties["Process"].IsString)
                {
                    return Properties["Process"].AsString;
                }
                return string.Empty;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    Properties["Process"] = value;
                }
                else
                {
                    Properties.Remove("Process");
                }
            }
        }

        public string? ClassMethod
        {
            get
            {
                if (Properties.Contains("ClassMethod") && Properties["ClassMethod"].IsString)
                {
                    return Properties["ClassMethod"].AsString;
                }
                return null;
            }
            set
            {
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

        public string? Source
        {
            get
            {
                if (Properties.Contains("Source") && Properties["Source"].IsString)
                {
                    return Properties["Source"].AsString;
                }
                return null;
            }
            set
            {
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

        public LogCategory? Category
        {
            get
            {
                if (Properties.Contains("Category") && Properties["Category"].IsString)
                {
                    return Enum.TryParse(Properties["Category"].AsString, out LogCategory category) ? category : (LogCategory?)null;
                }
                return null;
            }
            set
            {
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

        public string? ExceptionMessage { get; set; }

        public string? StackTrace { get; set; }

        public string? DeviceName
        {
            get
            {
                if (Properties.Contains("DeviceName") && Properties["DeviceName"].IsString)
                {
                    return Properties["DeviceName"].AsString;
                }
                return null;
            }
            set
            {
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
    }
}
