using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using ATI.Services.Common.Behaviors.OperationBuilder.Extensions;
using ATI.Services.Common.Swagger;
using Microsoft.AspNetCore.Mvc;
using MockService.Helpers;
using MockService.Models;
using MockService.Models.Mongo;

namespace MockService.Controllers
{
    public class MockServiceController : ControllerWithOpenApi
    {
        private readonly MockServiceHelper _mockServiceHelper;

        public MockServiceController(MockServiceHelper mockServiceHelper)
        {
            _mockServiceHelper = mockServiceHelper;
        }

        /// <summary>
        /// Метод отдающий замоканный ответ
        /// </summary>
        /// <returns></returns>
        [Route("v1/mockservice/{*catchall}")]
        public async Task<IActionResult> GetMockResponse()
        {
            var operation = await _mockServiceHelper.GetMockResponseAsync(HttpContext.Request.Path.ToString(),
                HttpContext.Request.Method);

            if (operation.Value.isPdf)
                return File("~/mock.pdf", MediaTypeNames.Application.Pdf, "mock.pdf");

            return new JsonResult(operation.Value.json)
            {
                StatusCode = (int) operation.Value.statusCode
            };
        }

        /// <summary>
        /// Создание мока
        /// </summary>
        /// <param name="mock"></param>
        /// <returns></returns>
        [ProducesResponseType(200)]
        [HttpPost("v1/mock")]
        [ValidateModelState]
        public Task<IActionResult> CreateMock([FromBody] Mock mock)
        {
            return _mockServiceHelper.CreateMockAsync(mock).AsActionResultAsync();
        }
        
        /// <summary>
        /// Редактирование мока
        /// </summary>
        /// <param name="mock"></param>
        /// <returns></returns>
        [ProducesResponseType(200)]
        [HttpPut("v1/mock")]
        [ValidateModelState]
        public Task<IActionResult> EditMock([FromBody] Mock mock)
        {
            return _mockServiceHelper.EditMockAsync(mock).AsActionResultAsync();
        }

        /// <summary>
        /// Удаление мока
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [ProducesResponseType(200)]
        [HttpDelete("v1/mock")]
        public Task<IActionResult> DeleteMock([Required] [FromQuery] string id)
        {
            return _mockServiceHelper.DeleteMockAsync(id).AsActionResultAsync();
        }


        /// <summary>
        /// Получение summary по существующим фильтрам
        /// </summary>
        /// <returns></returns>
        [ProducesResponseType(200)]
        [HttpGet("v1/mock/filters")]
        public Task<IActionResult> GetFilters()
        {
            return _mockServiceHelper.GetFiltersAsync().AsActionResultAsync();
        }


        /// <summary>
        /// Получение всех шаблонов для группы моков
        /// </summary>
        /// <returns></returns>
        [ProducesResponseType(200)]
        [HttpGet("v1/mocks")]
        public Task<IActionResult> GetMockByFilterName([Required] [FromQuery] string name)
        {
            return _mockServiceHelper.GetMockByFilterAsync(name).AsActionResultAsync();
        }

        /// <summary>
        /// Метод переключения мока
        /// </summary>
        /// <returns></returns>
        [ProducesResponseType(200)]
        [HttpPatch("v1/mock")]
        public Task<IActionResult> EnableMock([Required] [FromQuery] string id)
        {
            return _mockServiceHelper.EnableMockAsync(id).AsActionResultAsync();
        }
    }
}