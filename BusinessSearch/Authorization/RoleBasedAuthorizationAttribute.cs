using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using BusinessSearch.Models.Organization;
using BusinessSearch.Models;
using Microsoft.AspNetCore.Identity;
using BusinessSearch.Services;

namespace BusinessSearch.Authorization
{
    public class RoleBasedAuthorizationAttribute : ActionFilterAttribute
    {
        private readonly OrganizationRole[] _allowedRoles;
        private readonly bool _requiresListAccess;

        public RoleBasedAuthorizationAttribute(params OrganizationRole[] allowedRoles)
        {
            _allowedRoles = allowedRoles;
            _requiresListAccess = false;
        }

        public RoleBasedAuthorizationAttribute(bool requiresListAccess, params OrganizationRole[] allowedRoles)
        {
            _allowedRoles = allowedRoles;
            _requiresListAccess = requiresListAccess;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var userManager = context.HttpContext.RequestServices.GetService<UserManager<ApplicationUser>>();
            var orgFilterService = context.HttpContext.RequestServices.GetService<IOrganizationFilterService>();

            if (userManager == null || orgFilterService == null)
            {
                context.Result = new ForbidResult();
                return;
            }

            var user = await userManager.GetUserAsync(context.HttpContext.User);
            if (user?.OrganizationRole == null)
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
                return;
            }

            // Check if user's role is in allowed roles
            if (!_allowedRoles.Contains(user.OrganizationRole.Value))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
                return;
            }

            // Additional check for Callers accessing specific lists
            if (_requiresListAccess && user.OrganizationRole == OrganizationRole.Caller)
            {
                var listId = GetListIdFromContext(context);
                if (listId.HasValue)
                {
                    var hasAccess = await CheckCallerListAccess(user.Id, listId.Value, context.HttpContext);
                    if (!hasAccess)
                    {
                        context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
                        return;
                    }
                }
            }

            await next();
        }

        private int? GetListIdFromContext(ActionExecutingContext context)
        {
            // Check route parameters
            if (context.RouteData.Values.ContainsKey("id") &&
                int.TryParse(context.RouteData.Values["id"]?.ToString(), out int id))
            {
                return id;
            }

            // Check action parameters
            if (context.ActionArguments.ContainsKey("id") &&
                int.TryParse(context.ActionArguments["id"]?.ToString(), out int actionId))
            {
                return actionId;
            }

            return null;
        }

        private async Task<bool> CheckCallerListAccess(string userId, int listId, HttpContext context)
        {
            var crmService = context.RequestServices.GetService<CrmService>();
            if (crmService == null) return false;

            var list = await crmService.GetListById(listId);
            return list?.AssignedToId == userId;
        }
    }
}