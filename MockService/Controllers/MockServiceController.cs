using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using ATI.Services.Common.Behaviors.OperationBuilder.Extensions;
using ATI.Services.Common.Swagger;
using Microsoft.AspNetCore.Mvc;
using MockService.Helpers;
using MockService.Models;

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
        /// Метод, возвращающий подготовленный ответ
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
        /// Добавления мока
        /// </summary>
        /// <param name="mock"></param>
        /// <returns></returns>
        [HttpPost("v1/mock")]
        public Task<IActionResult> CreateMock([FromBody] Mock mock)
        {
            return _mockServiceHelper.CreateMockAsync(mock).AsActionResultAsync();
        }

        /// <summary>
        /// Редактирование мока
        /// </summary>
        /// <param name="mock"></param>
        /// <returns></returns>
        [HttpPut("v1/mock")]
        public Task<IActionResult> EditMock([FromBody] Mock mock)
        {
            return _mockServiceHelper.EditMockAsync(mock).AsActionResultAsync();
        }

        /// <summary>
        /// Удаление мока
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("v1/mock")]
        public Task<IActionResult> DeleteMock([Required] [FromQuery] string id)
        {
            return _mockServiceHelper.DeleteMockAsync(id).AsActionResultAsync();
        }


        /// <summary>
        /// Получение существующих фильтров (групп моков)
        /// </summary>
        /// <returns></returns>
        [HttpGet("v1/mock/filters")]
        public Task<IActionResult> GetFilters()
        {
            return _mockServiceHelper.GetFiltersAsync().AsActionResultAsync();
        }


        /// <summary>
        /// Получение всех шаблонов для группы моков
        /// </summary>
        /// <returns></returns>
        [HttpGet("v1/mocks")]
        public Task<IActionResult> GetMockByFilterName([Required] [FromQuery] string name)
        {
            return _mockServiceHelper.GetMockByFilterAsync(name).AsActionResultAsync();
        }

        /// <summary>
        /// Метод включения мока
        /// </summary>
        /// <returns></returns>
        [HttpPatch("v1/mock")]
        public Task<IActionResult> EnableMock([Required] [FromQuery] string id)
        {
            return _mockServiceHelper.EnableMockAsync(id).AsActionResultAsync();
        }
    }
}