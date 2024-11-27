using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        string connectionString = "mongodb+srv://clone47:NlNP9xAJOqX7wg2v@poccluster.mhbvh.mongodb.net/?retryWrites=true&w=majority&appName=POCCluster";
        var settings = MongoClientSettings.FromConnectionString(connectionString);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(30); // Increase timeout if needed
        var client = new MongoClient(settings);
        var database = client.GetDatabase("POC_DB");
        var collection = database.GetCollection<BsonDocument>("POC_Data");

        // Paths for log files
        string logFilePath = "POC_Logs.txt";
        string resourceUsagePath = "ResourceUsage.txt";
        string dbStatsPath = "DB_Stats.txt";

        // Ensure logs are clean at the start
        File.WriteAllText(logFilePath, string.Empty);
        File.WriteAllText(resourceUsagePath, string.Empty);
        File.WriteAllText(dbStatsPath, string.Empty);

        // Define filter as BsonDocument
        var filter = new BsonDocument("ProcessState", "INITIATED");

        // Aggregation pipeline for Atlas Search
        var atlasPipeline = new[]
        {
            new BsonDocument("$search", new BsonDocument
            {
                { "text", new BsonDocument
                    {
                        { "query", "INITIATED" },
                        { "path", "ProcessState" }
                    }
                }
            }),
            new BsonDocument("$match", filter)
        };

        // Number of concurrent users to simulate
        int concurrentUsers = 2;

        var tasks = Enumerable.Range(0, concurrentUsers).Select(async userId =>
        {
            var stopwatch = Stopwatch.StartNew();

            // Measure normal MongoDB query execution time
            stopwatch.Restart();
            Console.WriteLine("Starting normal query...");
            var normalResults = await collection.Find(filter).ToListAsync();
            Console.WriteLine("Normal query completed!");
            stopwatch.Stop();
            long normalQueryTime = stopwatch.ElapsedMilliseconds;

            // Measure Atlas Search query execution time
            stopwatch.Restart();
            Console.WriteLine("Starting atlas query...");
            var atlasCursor = await collection.AggregateAsync<BsonDocument>(atlasPipeline);
            var atlasResults = atlasCursor.ToList();
            Console.WriteLine("Atlas query completed!");
            stopwatch.Stop();
            long atlasQueryTime = stopwatch.ElapsedMilliseconds;

            // Log query times and performance difference
            var log = $"User {userId + 1}:\n" +
                      $"Normal Query Time: {normalQueryTime} ms\n" +
                      $"Atlas Search Query Time: {atlasQueryTime} ms\n" +
                      $"Performance Difference: {normalQueryTime - atlasQueryTime} ms\n\n";
            File.AppendAllText(logFilePath, log);

            // Fetch database statistics after each query execution
            var dbStats = database.RunCommand<BsonDocument>(new BsonDocument { { "dbStats", 1 } });
            File.AppendAllText(dbStatsPath, $"User {userId + 1} DB Stats:\n{dbStats}\n");

            // Simulate logging of resource usage (could be integrated with external monitoring tools)
            var simulatedResourceUsage = $"User {userId + 1} Resource Usage: Simulated CPU and Memory usage data.\n";
            File.AppendAllText(resourceUsagePath, simulatedResourceUsage);
        });

        // Wait for all tasks to complete
        await Task.WhenAll(tasks);

        Console.WriteLine("POC Completed! Logs saved locally.");
    }
}
