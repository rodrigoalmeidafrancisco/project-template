using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WebApi.Configurations.Filters
{
    public class AuthorizeCustomAttribute(string policyName, string roles) : ActionFilterAttribute
    {
        private readonly string _policyName = policyName;
        private readonly List<string> _listInfoRoles = roles?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList() ?? [];

        //Executa antes de entrar na ação
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            // Verifica se o usuário está autenticado
            if (context.HttpContext.User?.Identity == null || !context.HttpContext.User.Identity.IsAuthenticated)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // Valida a Policy se especificada
            if (!string.IsNullOrWhiteSpace(_policyName))
            {
                var authorizationService = context.HttpContext.RequestServices.GetService<Microsoft.AspNetCore.Authorization.IAuthorizationService>();

                if (authorizationService != null)
                {
                    var authorizationResult = authorizationService.AuthorizeAsync(context.HttpContext.User, null, _policyName).GetAwaiter().GetResult();

                    if (!authorizationResult.Succeeded)
                    {
                        context.Result = new ForbidResult();
                        return;
                    }
                }
            }

            // Valida as Roles
            if (_listInfoRoles != null && _listInfoRoles.Any())
            {
                //Obtem as roles do token
                List<string> rolesToken = context.HttpContext.User?.Claims
                    .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToList() ?? [];

                //Verifica se o usuário possui alguma das roles informadas
                if (_listInfoRoles.Any(rolesToken.Contains) == false)
                {
                    context.Result = new ForbidResult();
                    return;
                }
            }

        }

    }
}
