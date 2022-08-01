using ATI.Services.Common.Behaviors;
using ATI.Services.Common.Initializers.Interfaces;
using ATI.Services.Common.Logging;
using Microsoft.Extensions.Options;
using MockService.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using NLog;
using Polly;
using HttpMethod = Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod;
using ILogger = NLog.ILogger;

namespace MockService.Repository
{
    public class MongoRepository : IInitializer
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly IOptions<MongoOptions> _mongoOptions;

        private IMongoDatabase _mongo;
        private IMongoCollection<Mock> _mock;

        private static readonly EventId Mongo = new EventId(113);

        public MongoRepository(IOptions<MongoOptions> mongoOptions)
        {
            _mongoOptions = mongoOptions;
        }

        public Task InitializeAsync()
        {
            var options = _mongoOptions.Value;

            var policy = Policy<IMongoDatabase>
                .Handle<Exception>()
                .WaitAndRetryForever(retryAttempt =>
                        retryAttempt > 4
                            ? TimeSpan.FromSeconds(10)
                            : TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (result, _) => _logger.ErrorWithObject(result.Exception, Mongo, nameof(InitializeAsync)));

            _mongo = policy.Execute(GetDatabaseAsync);
            _mock = _mongo.GetCollection<Mock>(options.MocksCollectionName);

            IMongoDatabase GetDatabaseAsync()
            {
                var mongoSettings = new MongoClientSettings
                {
                    Servers = options.Servers.Select(s => new MongoServerAddress(s)),
                    Credential = MongoCredential.CreateCredential("db_name", options.User, options.Password),
                    ReplicaSetName = options.ReplicaSetName
                };

                return new MongoClient(mongoSettings).GetDatabase(options.Database);
            }

            return Task.CompletedTask;
        }

        public string InitStartConsoleMessage()
        {
            return "Start MongoInitializer";
        }

        public string InitEndConsoleMessage()
        {
            return "End MongoInitializer";
        }

        public async Task<OperationResult<List<Mock>>> GetAllMocksAsync()
        {
            try
            {
                return new OperationResult<List<Mock>>(await _mock.Find(Builders<Mock>.Filter.Empty).ToListAsync());
            }
            catch (Exception e)
            {
                _logger.ErrorWithObject(e, "Error while getting all mocks");
                return new OperationResult<List<Mock>>(ActionStatus.InternalServerError);
            }
        }

        public async Task<OperationResult<List<Mock>>> GetMocksByFilterNameAsync(string serviceName)
        {
            try
            {
                return new OperationResult<List<Mock>>(
                    await _mock.Find(Builders<Mock>.Filter.Eq(i => i.FilterName, serviceName)).ToListAsync());
            }
            catch (Exception e)
            {
                _logger.ErrorWithObject(e, "Error while getting template by serviceName", new {ServiceName = serviceName});
                return new OperationResult<List<Mock>>(ActionStatus.InternalServerError);
            }
        }

        public async Task<OperationResult<Mock>> GetMocksByIdAsync(string id)
        {
            try
            {
                var filter = Builders<Mock>.Filter.Eq(i => i.Id, id);

                return new OperationResult<Mock>(await _mock.Find(filter).FirstOrDefaultAsync());
            }
            catch (Exception e)
            {
                _logger.ErrorWithObject(e, "Error while getting mock by id", new {Id = id});
                return new OperationResult<Mock>(ActionStatus.InternalServerError);
            }
        }

        public async Task<OperationResult<Mock>> GetActiveMockByPathAndMethodAsync(string path,
            HttpMethod httpMethod)
        {
            try
            {
                var filter =
                    Builders<Mock>.Filter.Where(m => m.IsActive && m.Path == path && m.Method == httpMethod);

                var findOperation = await _mock
                    .Find(filter).ToListAsync();

                return new OperationResult<Mock>(findOperation.FirstOrDefault());
            }
            catch (Exception e)
            {
                _logger.ErrorWithObject(e, "Error while getting active mock by path");
                return new OperationResult<Mock>(ActionStatus.InternalServerError);
            }
        }

        public async Task<OperationResult> EnableMockAsync(string id)
        {
            try
            {
                var updates = Builders<Mock>.Update
                    .Set(s => s.IsActive, true);

                var filter = Builders<Mock>.Filter.Where(m => m.Id == id);

                await _mock.UpdateOneAsync(filter, updates);
                return OperationResult.Ok;
            }
            catch (Exception e)
            {
                _logger.ErrorWithObject(e, "Error while enable mock", new {id});
                return new OperationResult(ActionStatus.InternalServerError);
            }
        }

        public async Task<OperationResult> DisableAllMockByUrlAsync(string url, HttpMethod method)
        {
            try
            {
                var updates = Builders<Mock>.Update
                    .Set(s => s.IsActive, false);

                var filter = Builders<Mock>.Filter.Where(m => m.Path == url && m.Method == method);
                await _mock.UpdateManyAsync(filter, updates);
                return OperationResult.Ok;
            }
            catch (Exception e)
            {
                _logger.ErrorWithObject(e, "Error while disabling all mocks", new {url, method});
                return new OperationResult(ActionStatus.InternalServerError);
            }
        }

        public async Task<OperationResult> CreateMockAsync(Mock mock)
        {
            try
            {
                switch (mock.Body?.Type)
                {
                    case JTokenType.Object:
                    {
                        mock.BodyForMongo = BsonDocument.Parse(mock.Body.ToString());
                        break;
                    }
                    case JTokenType.Array:
                    {
                        mock.BodyForMongo = BsonSerializer.Deserialize<BsonArray>(mock.Body.ToString());
                        break;
                    }
                    default:
                    {
                        mock.BodyForMongo = null;
                        break;
                    }
                }

                mock.Id = Guid.NewGuid().ToString();
                await _mock
                    .InsertOneAsync(mock);
                return new OperationResult<Mock>(mock);
            }
            catch (Exception e)
            {
                _logger.ErrorWithObject(e, "Error while inserting mock", mock);
                return new OperationResult(ActionStatus.InternalServerError);
            }
        }

        public async Task<OperationResult> EditMockAsync(Mock mock)
        {
            try
            {
                switch (mock.Body?.Type)
                {
                    case JTokenType.Object:
                    {
                        mock.BodyForMongo = BsonDocument.Parse(mock.Body.ToString());
                        break;
                    }
                    case JTokenType.Array:
                    {
                        mock.BodyForMongo = BsonSerializer.Deserialize<BsonArray>(mock.Body.ToString());
                        break;
                    }
                    default:
                    {
                        mock.BodyForMongo = null;
                        break;
                    }
                }

                var updates = Builders<Mock>.Update
                    .Set(m => m.FilterName, mock.FilterName)
                    .Set(m => m.Description, mock.Description)
                    .Set(m => m.Path, mock.Path)
                    .Set(m => m.Method, mock.Method)
                    .Set(m => m.StatusCode, mock.StatusCode)
                    .Set(m => m.BodyForMongo, mock.BodyForMongo)
                    .Set(m => m.IsPdf, mock.IsPdf)
                    .Set(m => m.IsActive, mock.IsActive)
                    .Set(m => m.CreateDate, mock.CreateDate);

                await _mock
                    .UpdateOneAsync(Builders<Mock>.Filter.Eq(m => m.Id, mock.Id), updates);
                return new OperationResult<Mock>(mock);
            }
            catch (Exception e)
            {
                _logger.ErrorWithObject(e, "Error while inserting mock", mock);
                return new OperationResult(ActionStatus.InternalServerError);
            }
        }

        public async Task<OperationResult> DeleteMockAsync(string id)
        {
            try
            {
                return new OperationResult<Mock>(await _mock
                    .FindOneAndDeleteAsync(Builders<Mock>.Filter.Eq(m => m.Id, id)));
            }
            catch (Exception e)
            {
                _logger.ErrorWithObject(e, "Error while getting mock by port", new {Id = id});
                return new OperationResult<Mock>(ActionStatus.InternalServerError);
            }
        }
    }
}