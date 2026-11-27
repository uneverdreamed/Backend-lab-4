using Microsoft.AspNetCore.Mvc;
using Seismic.Workflows.Api.Earthquakes.Contract;
using Seismic.Workflows.Api.Earthquakes.Services;

namespace Seismic.Workflows.Api.Earthquakes.Controllers;

[ApiController]
[Route("earthquakes")]
public sealed class EarthquakesController(IEarthquakeProcessingService service) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<StartProcessingResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Start([FromBody] StartProcessingRequest request) =>
        Handle(async () =>
        {
            var response = await service.StartAsync(request);
            return AcceptedAtAction(nameof(GetStatus), new { earthquakeId = response.EarthquakeId }, response);
        });

    [HttpPost("demo-batch")]
    [ProducesResponseType<IReadOnlyList<StartProcessingResponse>>(StatusCodes.Status202Accepted)]
    public Task<IActionResult> StartDemoBatch([FromBody] DemoBatchRequest request) =>
        Handle(async () => (IActionResult)Accepted(await service.StartDemoBatchAsync(request)));

    [HttpPost("{earthquakeId:long}/confirmation")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Confirm(long earthquakeId, [FromBody] ConfirmRequest request) =>
        Handle(async () =>
        {
            await service.ConfirmAsync(earthquakeId, request);
            return (IActionResult)Accepted();
        });

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProcessListItem>>(StatusCodes.Status200OK)]
    public Task<IActionResult> List([FromQuery] int limit = 30) =>
        Handle(async () => (IActionResult)Ok(await service.ListAsync(Math.Clamp(limit, 1, 100))));

    [HttpGet("{earthquakeId:long}")]
    [ProducesResponseType<ProcessingStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetStatus(long earthquakeId) =>
        Handle(async () => (IActionResult)Ok(await service.GetStatusAsync(earthquakeId)));

    private async Task<IActionResult> Handle(Func<Task<IActionResult>> action)
    {
        try
        {
            return await action();
        }
        catch (ProcessingNotFoundException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status404NotFound, title: "Процесс не найден");
        }
        catch (ProcessingConflictException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status409Conflict, title: "Конфликт состояния");
        }
        catch (TemporalUnavailableException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable, title: "Temporal недоступен");
        }
    }
}
