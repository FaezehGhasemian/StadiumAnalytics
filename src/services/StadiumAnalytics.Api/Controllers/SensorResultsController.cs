using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using StadiumAnalytics.Application.SensorResults.Dtos;
using StadiumAnalytics.Application.SensorResults.Queries;

namespace StadiumAnalytics.Api.Controllers;

[ApiController]
[Route("api/sensor-results")]
[Produces("application/json")]
public sealed class SensorResultsController : ControllerBase
{
    private readonly ISensorResultsQueryService _queryService;
    private readonly IValidator<GetSensorResultsQuery> _validator;

    public SensorResultsController(
        ISensorResultsQueryService queryService,
        IValidator<GetSensorResultsQuery> validator)
    {
        _queryService = queryService;
        _validator = validator;
    }

    [HttpPost("query")]
    [ProducesResponseType(typeof(IReadOnlyList<SensorResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<SensorResultDto>>> Query(
        [FromBody] GetSensorResultsRequest? request,
        CancellationToken cancellationToken)
    {
        var query = (request ?? new GetSensorResultsRequest()).ToQuery();

        var validation = await _validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => new { error = e.ErrorMessage }));

        var results = await _queryService.GetGroupedAsync(query, cancellationToken);
        return Ok(results);
    }
}
