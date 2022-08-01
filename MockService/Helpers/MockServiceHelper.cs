using System.Net;
using ATI.Services.Common.Behaviors;
using Microsoft.Extensions.Options;
using MockService.Models;
using MockService.Repository;
using MongoDB.Bson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using HttpMethod = Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod;

namespace MockService.Helpers
{
    public class MockServiceHelper
    {
        private readonly MongoRepository _mongoRepository;
        private readonly MockServiceOptions _mockServiceOptions;

        public MockServiceHelper(MongoRepository mongoRepository,
            IOptions<MockServiceOptions> options)
        {
            _mockServiceOptions = options.Value;
            _mongoRepository = mongoRepository;
        }

        public async Task<OperationResult<(JContainer json, HttpStatusCode statusCode, bool isPdf)>>
            GetMockResponseAsync(string rowPath, string method)
        {
            // удаляем прокси адрес
            var path = rowPath.Replace("/mockservice", "");

            // находим запрашиваемый мок
            if (!Enum.TryParse(method, true, out HttpMethod httpMethod))
                return new OperationResult<(JContainer, HttpStatusCode, bool)>(ActionStatus.BadRequest);
            
            var findOperation = await _mongoRepository.GetActiveMockByPathAndMethodAsync(path, httpMethod);
            if (!findOperation.Success)
            {
                return new OperationResult<(JContainer, HttpStatusCode, bool)>(findOperation.ActionStatus);
            }

            // В приоритете пдф
            if (findOperation.Value.IsPdf)
            {
                return new OperationResult<(JContainer, HttpStatusCode, bool)>
                    ((new JObject(), findOperation.Value.StatusCode, true));
            }

            // json
            if (findOperation.Value.BodyForMongo != null)
            {
                var response = (JContainer) JToken.Parse(findOperation.Value.BodyForMongo.ToJson());
                return new OperationResult<(JContainer, HttpStatusCode, bool)>
                    ((response, findOperation.Value.StatusCode, false));
            }

            // статус код
            return new OperationResult<(JContainer, HttpStatusCode, bool)>
                ((null, findOperation.Value.StatusCode, false));
        }

        public async Task<OperationResult> CreateMockAsync(Mock mock)
        {
            // Очистка path от query запроса 
            mock.Path = ModificationPath(mock.Path);

            mock.IsActive = true;
            mock.CreateDate = DateTime.Now;

            //Выключаем все моки на данном урле
            var disableOperation = await _mongoRepository.DisableAllMockByUrlAsync(mock.Path, mock.Method);
            if (!disableOperation.Success)
                return new OperationResult(disableOperation);

            // создаем мок
            return await _mongoRepository.CreateMockAsync(mock);
        }

        public async Task<OperationResult> EditMockAsync(Mock mock)
        {
            // Очистка path от query запроса 
            mock.Path = ModificationPath(mock.Path);

            mock.IsActive = true;
            mock.CreateDate = DateTime.Now;

            //Выключаем все моки на данном урле
            var disableOperation = await _mongoRepository.DisableAllMockByUrlAsync(mock.Path, mock.Method);
            if (!disableOperation.Success)
                return new OperationResult(disableOperation);

            // редактируем мок
            return await _mongoRepository.EditMockAsync(mock);
        }

        public async Task<OperationResult> DeleteMockAsync(string id)
        {
            return await _mongoRepository.DeleteMockAsync(id);
        }

        /// <summary>
        /// Отдаем данные отсортированные по дате последнего добавления
        /// </summary>
        /// <returns></returns>
        public async Task<OperationResult<List<GetFiltersResponse>>> GetFiltersAsync()
        {
            var mongoOperation = await _mongoRepository.GetAllMocksAsync();
            if (!mongoOperation.Success)
                return new OperationResult<List<GetFiltersResponse>>(mongoOperation);
            var result = mongoOperation.Value.GroupBy(m => m.FilterName)
                .Select(p =>
                    new GetFiltersResponse
                    {
                        FilterName = p.Key,
                        Count = p.Count(),
                        LastCreateDate = p.Max(d => d.CreateDate)
                    }).OrderByDescending(g => g!.LastCreateDate).ToList();
            return new OperationResult<List<GetFiltersResponse>>(result);
        }

        public async Task<OperationResult<List<MockDto>>> GetMockByFilterAsync(string serviceName)
        {
            var mongoOperation = await _mongoRepository.GetMocksByFilterNameAsync(serviceName);
            if (!mongoOperation.Success)
                return new OperationResult<List<MockDto>>(mongoOperation);

            return new OperationResult<List<MockDto>>(mongoOperation.Value.Select(i => new MockDto
            {
                Id = i.Id,
                Body = i.BodyForMongo is null
                    ? null
                    : JsonConvert.DeserializeObject(i.BodyForMongo.ToJson()),
                StatusCode = i.StatusCode,
                Description = i.Description,
                IsActive = i.IsActive,
                IsPdf = i.IsPdf,
                Method = i.Method,
                Path = i.Path,
                Domain = _mockServiceOptions.DomainMockPath,
                FilterName = i.FilterName
            }).ToList());
        }

        public async Task<OperationResult> EnableMockAsync(string id)
        {
            // берем мок по id
            var mongoOperation = await _mongoRepository.GetMocksByIdAsync(id);
            if (!mongoOperation.Success)
                return new OperationResult<List<Mock>>(mongoOperation);

            // Выключаем все моки на данном урле
            var disableOperation = await _mongoRepository.DisableAllMockByUrlAsync(
                mongoOperation.Value.Path, mongoOperation.Value.Method);
            if (!disableOperation.Success)
                return new OperationResult(disableOperation);

            // включаем переданный мок
            return await _mongoRepository.EnableMockAsync(id);
        }

        private static string ModificationPath(string path)
        {
            path = path.Trim();

            if (path.Contains('?'))
            {
                var temp = path.Split('?');
                path = temp[0];
            }

            // Проверка чтобы первый символ был "/"
            if (path[0] != '/')
                path = "/" + path;

            return path;
        }
    }
}