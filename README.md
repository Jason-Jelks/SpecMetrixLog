# SpecMetrixLog
SpecMetrix logging system written in .NET 8 (original) that will accept event messages/logs from SpecMetrix and write to MongoDB

Solution Plans:

* MongoDB Data Service: Separate generic implementation to read and write structures to Mongo. The MongoDB will set to purge logs with a TTL of 7 days, without affecting other MongoDB files.
* SpecMetrixLog Serivce: This will be a separately running service that will accept incoming messages and deduplicate high speed events that will flood normal event managers. This service will employ cache for fast retrieval of most recent events without needing to perform DB read on entry.
* Blazor UI: will communicate with the SpecMetrixLog Service to retrieve the most recent events from cache. There will be pagination and advanced filtering to retreive and search for historical events.