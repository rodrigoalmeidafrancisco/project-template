using Domain.Commands._Base;
using Microsoft.AspNetCore.Mvc;
using Shared.Settings;

namespace WebApi.Controllers._Base
{
    public class BaseApiController : ControllerBase
    {
        public BaseApiController()
        {

        }

        protected IActionResult RetornoBaseApi<T>(CommandResult<T> result, string link201)
        {
            return result.StatusCod switch
            {
                200 => Ok(result),
                201 => Created($"{SettingApp.Aplication.WebUri}{link201}", result),
                400 => BadRequest(result),
                _ => StatusCode(StatusCodes.Status500InternalServerError, result),
            };
        }

    }
}
