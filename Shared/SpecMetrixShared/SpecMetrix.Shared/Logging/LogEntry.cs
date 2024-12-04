using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SpecMetrix.Interfaces;

namespace SpecMetrix.Shared.Logging;

/// <summary>
/// Log Entry for SpecMetrix systems
/// </summary>
[BsonIgnoreExtraElements]
public class LogEntry : ILogEntry
{
    [BsonId] // Maps to MongoDB's _id field
    public ObjectId Id { get; set; }
    public Guid LogId { get; set; }
    [BsonIgnore]
    public string Namespace {
        get
        {
            return Properties.Contains("Namespace") && Properties["Namespace"].IsString
                ? Properties["Namespace"].AsString
                : string.Empty; // Return an empty string if the field is not found or is not a string
        }
        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                Properties["Namespace"] = value; // Set the Namespace field in the Properties
            }
            else
            {
                Properties.Remove("Namespace"); // Remove the Namespace field if the value is null or empty
            }
        }
    }
    public DateTime Timestamp { get; set; }
    [BsonIgnore]
    public string? MachineName
    {
        get
        {
            return Properties.Contains("MachineName") && Properties["MachineName"].IsString
                ? Properties["MachineName"].AsString
                : null; // Return null if the field is not found or is not a string
        }
        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                Properties["MachineName"] = value; // Set the MachineName field in the Properties
            }
            else
            {
                Properties.Remove("MachineName"); // Remove the MachineName field if the value is null or empty
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
                : default; // Return default value (0) if the field is not found or not an int
        }
        set
        {
            Properties["Code"] = value; // Set the Code field in the Properties
        }
    }
    // Get and Set for Process
    [BsonIgnore]
    public string Process
    {
        get
        {
            return Properties.Contains("Process") && Properties["Process"].IsString
                ? Properties["Process"].AsString
                : default; // Return default value (null) if the field is not found or not a string
        }
        set
        {
            Properties["Process"] = value; // Set the Process field in the Properties
        }
    }

    [BsonIgnore]
    public string? ClassMethod {
        get
        {
            return Properties.Contains("ClassMethod") && Properties["ClassMethod"].IsString
                ? Properties["ClassMethod"].AsString
                : null; // Return null if the field is not found or is not a string
        }
        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                Properties["ClassMethod"] = value; // Set the ClassMethod field in the Properties
            }
            else
            {
                Properties.Remove("ClassMethod"); // Remove the ClassMethod field if the value is null or empty
            }
        }
    }
    public string Message { get; set; }
    public ulong Occurrences { get; set; }
    public IDictionary<string, string>? Metadata { get; set; }
    [BsonIgnore]
    public string? Source
    {
        get
        {
            return Properties.Contains("Source") && Properties["Source"].IsString
                ? Properties["Source"].AsString
                : null; // Return null if the field is not found or is not a string
        }
        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                Properties["Source"] = value; // Set the Source field in the Properties
            }
            else
            {
                Properties.Remove("Source"); // Remove the Source field if the value is null or empty
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
                return Enum.TryParse(Properties["Category"].AsString, out LogCategory category) ? category : (LogCategory?)null;
            }
            return null;
        }
        set
        {
            if (value.HasValue)
            {
                Properties["Category"] = value.Value.ToString(); // Set the Category field in the Properties
            }
            else
            {
                Properties.Remove("Category"); // Remove the Category field if the value is null
            }
        }
    }
    public string? ExceptionMessage { get; set; }
    public string? StackTrace { get; set; }
    [BsonElement("Properties")]
    public BsonDocument Properties { get; set; }

    /// <summary>
    /// Message Template for Serilog based logging
    /// </summary>
    public string MessageTemplate { get; set; } // Holds the message template, e.g., "User {UserId} logged in from {Location} at {LoginTime}"
    public IDictionary<string, object> TemplateValues { get; set; } // Holds the values for placeholders
    [BsonIgnore]
    public string? DeviceName
    {
        get
        {
            return Properties.Contains("DeviceName") && Properties["DeviceName"].IsString
                ? Properties["DeviceName"].AsString
                : null; // Return null if the field is not found or is not a string
        }
        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                Properties["DeviceName"] = value; // Set the DeviceName field in the Properties
            }
            else
            {
                Properties.Remove("DeviceName"); // Remove the DeviceName field if the value is null or empty
            }
        }
    }

    public string RenderedMessage { get; set; }
    public LogLevel Level { get; set; }

    public LogEntry()
    {

    }
}
