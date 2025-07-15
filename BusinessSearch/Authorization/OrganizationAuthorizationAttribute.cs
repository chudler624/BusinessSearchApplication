using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using BusinessSearch.Services;

namespace BusinessSearch.Authorization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireOrganizationAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        private readonly bool _allowNoOrganization;

        public RequireOrganizationAttribute(bool allowNoOrganization = false)
        {
            _allowNoOrganization = allowNoOrganization;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var orgFilterService = context.HttpContext.RequestServices
                .GetService<IOrganizationFilterService>();

            if (orgFilterService == null)
            {
                context.Result = new StatusCodeResult(500);
                return;
            }

            var organizationId = orgFilterService.GetCurrentOrganizationId().Result;

            if (organizationId == -1 && !_allowNoOrganization)
            {
                context.Result = new RedirectToActionResult(
                    "NoOrganization",
                    "Account",
                    new { returnUrl = context.HttpContext.Request.Path });
                return;
            }
        }
    }
}
