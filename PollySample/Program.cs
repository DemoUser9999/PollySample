using System;
using System.Data;
using System.Data.SQLite;
using System.Threading;
using System.Threading.Tasks;
using Polly;

namespace PollySample
{
    class Program
    {
        // Useful links:
        // https://sqlitebrowser.org/
        // https://www.connectionstrings.com/sqlite/
        // https://github.com/App-vNext/Polly
        // https://github.com/App-vNext/Polly/wiki/PolicyWrap

        static void Main(string[] args)
        {
            Task.Run(async () =>
            {
                var retryPolicy = Policy
                  .Handle<SQLiteException>()
                  .RetryAsync(3, (Exception ex, int retryCount) =>
                  {
                      Console.Error.WriteLine($"Caught {ex.GetType()}. Retry count: {retryCount}.");
                  });

                var fallbackPolicy = Policy
                    .Handle<SQLiteException>()
                    .FallbackAsync(async (CancellationToken ct) => Console.WriteLine("Unable to list users at this time."));

                //await retryPolicy.ExecuteAsync(() => PrintUsersAsync());
                //await fallbackPolicy.ExecuteAsync(() => PrintUsersAsync());

                var wrappedPolicy = Policy
                    .WrapAsync(fallbackPolicy, retryPolicy);

                await wrappedPolicy.ExecuteAsync(() => PrintUsersAsync());
            }).Wait();

            Console.WriteLine("Done");
            Console.ReadLine();
        }

        static async Task PrintUsersAsync()
        {
            var connectionString = "Data Source=./PollySample.db;Version=3;";
            var conn = new SQLiteConnection(connectionString);
            var cmd = new SQLiteCommand("SELECT * FROM `Users2`")
            {
                Connection = conn
            };

            conn.Open();
            var reader = await cmd.ExecuteReaderAsync();

            while (reader.Read())
            {
                var row = (IDataRecord)reader;
                Console.WriteLine($"User name: {row.GetString(1)}");
            }
        }
    }
}

