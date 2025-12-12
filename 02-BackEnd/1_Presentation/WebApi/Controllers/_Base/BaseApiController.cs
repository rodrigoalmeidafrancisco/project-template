using Domain.Commands._Base;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers._Base
{
    public class BaseApiController : ControllerBase
    {
        public BaseApiController()
        {

        }

        protected IActionResult RetornoBaseApi<T>(CommandResult<T> result)
        {
            return result.StatusCod switch
            {
                200 => Ok(result),
                201 => Created("", result),
                400 => BadRequest(result),
                _ => StatusCode(StatusCodes.Status500InternalServerError, result),
            };
        }

    }
}
