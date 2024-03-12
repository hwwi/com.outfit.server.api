using Api.Data.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController, Route("tags"), Authorize]
    public class TagsController : AbstractController
    {
        [HttpGet("validate/item"), AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public IActionResult GetValidateItem(
            [FromQuery, BrandCode] string brandCode,
            [FromQuery, ProductCode] string? productCode
        )
        {
            return NoContent();
        }
    }
}