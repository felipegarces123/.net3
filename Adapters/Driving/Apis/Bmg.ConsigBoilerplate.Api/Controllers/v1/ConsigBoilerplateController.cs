using Bmg.ConsigBoilerplate.Api.AppServices.v1.Interfaces;
using Bmg.Logging.Internal;
using Bmg.Logging.Internal.Attributes;
using Bmg.Project.Utils.Base;
using Bmg.Project.Utils.Extensions;
using Bmg.Project.Utils.Interfaces;
using Bmg.Project.Utils.Notifications;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Bmg.ConsigBoilerplate.Api.Dtos.v1.ConsigBoilerplate;
using Bmg.Project.Utils.Types;

namespace Bmg.ConsigBoilerplate.Api.Controllers.v1
{
    /// <summary>
    /// 
    /// </summary>
    /// <response code="400">Field validation messages</response>
    /// <response code="422">Business messages</response>
    /// <response code="500">Coding and server errors</response>
    [ApiController]
    [ApiVersion("1")]
    [Route("v{version:apiVersion}/[controller]")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<BmgNotification>), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public class ConsigBoilerplateController : BmgControllerBase<IConsigBoilerplateAppService>
    {
        /// <summary>
        /// Get all weathers
        /// </summary>
        /// <returns></returns>
        /// <response code="200">List with items</response>
        /// <response code="204">Empty list</response>
        [HttpGet(Name = nameof(GetAsync))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult<IEnumerable<WeatherResponse>>> GetAsync(CancellationToken cancellationToken)
        {
            var result = await AppService.GetAsync(cancellationToken);

            if (HasNotifications())
                return Notifications();

            if (result.Any())
                return Ok(result);
            else
                return NoContent();
        }

        /// <summary>
        /// Get specific weather by id
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Item</response>
        /// <response code="204">Empty item</response>
        [HttpGet("{id}", Name = nameof(GetAsync))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult<WeatherResponse>> GetAsync(long id, CancellationToken cancellationToken)
        {
            var result = await AppService.GetAsync(id, cancellationToken);

            if (HasNotifications())
                return Notifications();

            if (result != null)
                return Ok(result);
            else
                return NoContent();
        }

        /// <summary>
        /// Get all weathers paginated
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Item</response>
        /// <response code="204">Empty item</response>
        [HttpGet("{pageSize}/{currentPage}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult<IEnumerable<WeatherResponse>>> GetPaginatedAsync(int pageSize, int currentPage, CancellationToken cancellationToken)
        {
            var result = await AppService.GetPaginatedAsync(pageSize, currentPage, cancellationToken);

            if (HasNotifications())
                return Notifications();

            return OkPaginated(result);
        }

        /// <summary>
        /// Create weather
        /// </summary>
        /// <returns></returns>
        /// <response code="201">Item created with success</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<ActionResult<WeatherResponse>> PostAsync(WeatherRequest weather, CancellationToken cancellationToken)
        {
            var result = await AppService.PostAsync(weather, cancellationToken);

            if (HasNotifications())
                return Notifications();

            return CreatedAtRoute(string.Empty, new { id = result.Id }, result);
        }

        /// <summary>
        /// Update weather specific field
        /// </summary>
        /// <returns></returns>
        /// <response code="204">Item field updated with success</response>
        /// <response code="400">Item not found</response>
        [HttpPatch("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> PatchAsync(long id, JsonPatchDocument<WeatherRequest> weather, CancellationToken cancellationToken)
        {
            var result = await AppService.PatchAsync(id, weather, cancellationToken);

            if (HasNotifications())
                return Notifications();

            if (result)
                return NoContent();
            else
                return BadRequest();
        }

        /// <summary>
        /// Update a complete weather
        /// </summary>
        /// <returns></returns>
        /// <response code="204">Item updated with success</response>
        /// <response code="400">Item not found</response>
        [HttpPut]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> PutAsync(WeatherRequest weather, CancellationToken cancellationToken)
        {
            var result = await AppService.PutAsync(weather, cancellationToken);

            if (HasNotifications())
                return Notifications();

            if (result)
                return NoContent();
            else
                return BadRequest();
        }

        /// <summary>
        /// Delete weather by id
        /// </summary>
        /// <returns></returns>
        /// <response code="204">Item deleted success</response>
        /// <response code="400">Item not found</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteAsync(long id, CancellationToken cancellationToken)
        {
            var result = await AppService.DeleteAsync(id, cancellationToken);

            if (HasNotifications())
                return Notifications();

            if (result)
                return NoContent();
            else
                return BadRequest();
        }
    }
}