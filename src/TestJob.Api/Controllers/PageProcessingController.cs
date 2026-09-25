using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TestJob.Api.Models;
using TestJob.Api.Services;

namespace TestJob.Api.Controllers;

[ApiController]
[Route("api/process-page")]
public sealed class PageProcessingController(
    IValidator<ProcessPageRequest> validator,
    IPageProcessingService processingService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ProcessPageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProcessPageResponse>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProcessPageResponse>> ProcessAsync(
        [FromBody] ProcessPageRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var message = string.Join(" ", validationResult.Errors.Select(error => error.ErrorMessage));
            return BadRequest(ProcessPageResponse.Error(ErrorCodes.ValidationError, message));
        }

        var response = await processingService.ProcessAsync(request, cancellationToken);
        if (response.ErrorCode == ErrorCodes.UnexpectedError)
            return StatusCode(StatusCodes.Status500InternalServerError, response);
        return response.IsError == 0 ? Ok(response) : BadRequest(response);
    }
}
