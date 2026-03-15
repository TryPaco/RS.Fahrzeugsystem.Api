using Microsoft.AspNetCore.Mvc;
using RS.Fahrzeugsystem.Api.Authorization;
using RS.Fahrzeugsystem.Api.Services;

namespace RS.Fahrzeugsystem.Api.Controllers;

[ApiController]
[Route("api/vin")]
public sealed class VinController(IVinDecoderService vinDecoderService) : ControllerBase
{
	public sealed class DecodeVinRequest
	{
		public string Vin { get; set; } = default!;
	}

	[HttpPost("decode")]
	[HasPermission("vehicles.view")]
	public ActionResult Decode([FromBody] DecodeVinRequest request)
	{
		if (string.IsNullOrWhiteSpace(request.Vin))
			return BadRequest("VIN/FIN ist erforderlich.");

		var result = vinDecoderService.Decode(request.Vin);
		return Ok(result);
	}
}
