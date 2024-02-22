using AutoMapper;
using Club12.Entities.DivisionEntity;
using Club12.Services.Divisions;
using Club12.Viewmodels.Division;
using Microsoft.AspNetCore.Mvc;

namespace Club12.Controllers;

/// <summary>
/// Controller for managing divisions.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class DivisionController : ControllerBase
{
    private readonly IDivisionService _divisionService;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="DivisionController"/> class.
    /// </summary>
    /// <param name="divisionService">The division service.</param>
    /// <param name="mapper">The AutoMapper instance.</param>
    public DivisionController(
        IDivisionService divisionService,
        IMapper mapper
    )
    {
        _divisionService = divisionService;
        _mapper = mapper;
    }

    /// <summary>
    /// Creates a new division.
    /// </summary>
    /// <param name="divisionRequest">The division request.</param>
    /// <returns>The created division response.
    /// <para>Returns 200 (Ok) with the division response if the creation was succesful.</para>
    /// </returns>
    [HttpPost("division")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(DivisionResponse))]
    public IActionResult CreateDivision(DivisionRequest divisionRequest)
    {
        Division mappedDivision = _mapper.Map<Division>(divisionRequest);
        Division createdDivision = _divisionService.CreateDivision(mappedDivision);
        DivisionResponse divisionResponse = _mapper.Map<DivisionResponse>(createdDivision);

        return new ObjectResult(divisionResponse) { StatusCode = StatusCodes.Status201Created };
    }

    /// <summary>
    /// Retrieves a division by its id.
    /// </summary>
    /// <param name="divisionId">The id of the division to retrieve.</param>
    /// <returns>The division with the specified id.
    /// <para>Returns 200 (Ok) with the division response if it was found.</para>
    /// <para>Returns 400 (BadRequest) if the division with the provided id was not found.</para>
    /// </returns>
    [HttpGet("division/{divisionId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DivisionResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<DivisionResponse> GetDivisionById(Guid divisionId)
    {
        Division? division = _divisionService.GetDivisionById(divisionId);

        if (division is null)
        {
            return BadRequest($"Division with id {divisionId} not found.");
        }

        DivisionResponse divisionResponse = _mapper.Map<DivisionResponse>(division);
        return Ok(divisionResponse);
    }
}
