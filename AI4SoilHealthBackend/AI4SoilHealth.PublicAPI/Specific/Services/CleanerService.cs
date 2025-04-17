using Google.Protobuf;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Data;
using Dapper;

namespace PublicAPI.Common.Services {

/**
        * Cleaning related operations   
        * Periodically checks data.image_delete and removes deleted images from os
        @module CleanerService
*/

    public class CleanerService : BackgroundService {

        private readonly ILogger<CleanerService> _logger;
        private readonly IDistributedCache _cache;
        private readonly AppOptions _options;

        public CleanerService(ILogger<CleanerService> logger, IDistributedCache cache, IOptions<AppOptions> options) {
            _logger = logger;
            _cache = cache;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            IDbConnection connection = null;
            while (!stoppingToken.IsCancellationRequested) {
                try {
                    Console.WriteLine("CleanerService running at: " + DateTimeOffset.Now);

                    connection = new NpgsqlConnection(_options.ConnectionString);
                    connection.Open();
                    List<string> files = (await connection.QueryAsync<string>("select file from general.file_delete", null, commandType: CommandType.Text)).AsList();
                    foreach (string file in files) {
                        if (file.Contains(".zip")) {
                            if (File.Exists(GlobalConstants.fileDir + file)) {
                                File.Delete(GlobalConstants.fileDir + file);
                            }
                        } else {
                            if (File.Exists(GlobalConstants.imageDir + file)) {
                                File.Delete(GlobalConstants.imageDir + file);
                            }
                        }
                        await connection.ExecuteAsync("delete from general.file_delete where file=@file", new { file = file }, null, null, CommandType.Text);
                    }
                    connection.Close();
                } catch (Exception ex) {
                    _logger.LogError(ex.Message);
                    if (connection != null && connection.State == ConnectionState.Open) {
                        connection.Close();
                    }
                }
                await Task.Delay(600000, stoppingToken);
            }
        }
    }
}
