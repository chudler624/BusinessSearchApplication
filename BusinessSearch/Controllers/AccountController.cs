using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BusinessSearch.Models;
using BusinessSearch.Models.ViewModels;
using BusinessSearch.Data;
using BusinessSearch.Services;
using Microsoft.AspNetCore.Authorization;
using System.Text.Encodings.Web;

namespace BusinessSearch.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IOrganizationService _organizationService;
        private readonly IEmailService _emailService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountController> logger,
            ApplicationDbContext context,
            IOrganizationService organizationService,
            IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _context = context;
            _organizationService = organizationService;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult NoOrganization(string returnUrl = null)
        {
            var viewModel = new NoOrganizationViewModel
            {
                ReturnUrl = returnUrl
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrganization(CreateOrganizationViewModel model)
        {
            _logger.LogInformation("CreateOrganization POST action started");
            try
            {
                if (model == null)
                {
                    _logger.LogError("Model is null");
                    return Json(new { success = false, message = "Invalid request data." });
                }

                _logger.LogInformation($"Received organization name: {model.OrganizationName}");

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("ModelState is invalid");
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage);
                    _logger.LogWarning($"Validation errors: {string.Join(", ", errors)}");
                    return Json(new { success = false, message = "Please check the form values." });
                }

                var user = await _userManager.GetUserAsync(User);
                _logger.LogInformation($"Retrieved user: {user?.Id ?? "null"}");

                if (user == null)
                {
                    _logger.LogError("User not found or not authenticated");
                    return Json(new { success = false, message = "User not found or not authenticated." });
                }

                try
                {
                    _logger.LogInformation("Calling CreateOrganizationAsync");
                    var organization = await _organizationService.CreateOrganizationAsync(
                        model.OrganizationName,
                        user);

                    _logger.LogInformation($"Organization created successfully. ID: {organization.Id}");
                    return Json(new { success = true, organizationId = organization.Id });
                }
                catch (Exception innerEx)
                {
                    _logger.LogError($"Error in CreateOrganizationAsync: {innerEx.Message}");
                    _logger.LogError($"Inner exception: {innerEx.InnerException?.Message}");
                    _logger.LogError($"Stack trace: {innerEx.StackTrace}");
                    return Json(new { success = false, message = $"Error creating organization: {innerEx.Message}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unhandled error in CreateOrganization action: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = "An unexpected error occurred. Please try again." });
            }
        }

        [HttpGet]
        public IActionResult CreateOrganization(string returnUrl = null)
        {
            var viewModel = new CreateOrganizationViewModel
            {
                ReturnUrl = returnUrl
            };
            return View(viewModel);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (_signInManager.IsSignedIn(User))
            {
                // Only redirect if there's a valid return URL
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                // Otherwise, go to home
                return RedirectToAction("Index", "Home");
            }

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password,
                        model.RememberMe, lockoutOnFailure: true);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User logged in: {Email}", model.Email);

                        // Update LastLoginAt
                        var user = await _userManager.FindByEmailAsync(model.Email);
                        if (user != null)
                        {
                            user.LastLoginAt = DateTime.UtcNow;
                            await _userManager.UpdateAsync(user);
                        }

                        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                        {
                            return Redirect(model.ReturnUrl);
                        }
                        return RedirectToAction("Index", "Home");
                    }
                    if (result.IsLockedOut)
                    {
                        _logger.LogWarning("User account locked out: {Email}", model.Email);
                        return RedirectToAction(nameof(Lockout));
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                        return View(model);
                    }
                }

                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        _logger.LogError($"Model validation error during login: {error.ErrorMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login attempt");
                ModelState.AddModelError(string.Empty, "An error occurred during login. Please try again.");
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (_signInManager.IsSignedIn(User))
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            try
            {
                _logger.LogInformation("Starting registration process...");

                if (ModelState.IsValid)
                {
                    // Check if email already exists
                    var existingUser = await _userManager.FindByEmailAsync(model.Email);
                    if (existingUser != null)
                    {
                        ModelState.AddModelError(string.Empty, "Email already registered.");
                        return View(model);
                    }

                    // Use the correct execution strategy
                    var strategy = _context.Database.CreateExecutionStrategy();
                    await strategy.ExecuteAsync(async () =>
                    {
                        using var transaction = await _context.Database.BeginTransactionAsync();
                        try
                        {
                            // Create TeamMember first
                            var teamMember = new TeamMember
                            {
                                Name = model.Name,
                                Email = model.Email,
                                DateAdded = DateTime.UtcNow
                            };

                            _logger.LogInformation("Adding TeamMember to context...");
                            _context.TeamMembers.Add(teamMember);
                            await _context.SaveChangesAsync();
                            _logger.LogInformation($"TeamMember created with ID: {teamMember.Id}");

                            // Create ApplicationUser
                            var user = new ApplicationUser
                            {
                                UserName = model.Email,
                                Email = model.Email,
                                CreatedAt = DateTime.UtcNow,
                                TeamMemberId = teamMember.Id,
                                EmailConfirmed = true // Set to true for now, implement email confirmation later
                            };

                            _logger.LogInformation("Creating ApplicationUser...");
                            var result = await _userManager.CreateAsync(user, model.Password);

                            if (result.Succeeded)
                            {
                                _logger.LogInformation("User created successfully. Committing transaction...");
                                await transaction.CommitAsync();

                                _logger.LogInformation("Transaction committed. Signing in user...");
                                await _signInManager.SignInAsync(user, isPersistent: false);

                                TempData["Success"] = "Registration successful!";
                                return;
                            }

                            foreach (var error in result.Errors)
                            {
                                _logger.LogError($"Identity error: {error.Code} - {error.Description}");
                                ModelState.AddModelError(string.Empty, error.Description);
                            }

                            await transaction.RollbackAsync();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error during registration transaction");
                            await transaction.RollbackAsync();
                            throw;
                        }
                    });

                    if (!ModelState.IsValid)
                    {
                        return View(model);
                    }

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    foreach (var modelState in ModelState.Values)
                    {
                        foreach (var error in modelState.Errors)
                        {
                            _logger.LogError($"Model validation error: {error.ErrorMessage}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error during registration");
                ModelState.AddModelError(string.Empty, $"Registration failed: {ex.Message}");
            }

            // If we got this far, something failed
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Lockout()
        {
            return View();
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            user = await _userManager.Users
                .Include(u => u.TeamMember)
                .Include(u => u.Organization)
                .FirstOrDefaultAsync(u => u.Id == user.Id);

            var model = new ProfileViewModel
            {
                Id = user.Id,
                Name = user.TeamMember?.Name ?? "",
                Email = user.Email ?? "",
                CreatedAt = user.CreatedAt,
                TeamMemberId = user.TeamMemberId,
                OrganizationId = user.OrganizationId,
                OrganizationName = user.Organization?.Name
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Update email if changed
                if (user.Email != model.Email)
                {
                    var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);
                    if (!setEmailResult.Succeeded)
                    {
                        foreach (var error in setEmailResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        return View(model);
                    }
                    user.UserName = model.Email; // Update username to match email
                    await _userManager.UpdateAsync(user);
                }

                // Update password if provided
                if (!string.IsNullOrEmpty(model.NewPassword))
                {
                    if (string.IsNullOrEmpty(model.CurrentPassword))
                    {
                        ModelState.AddModelError(string.Empty, "Current password is required to set a new password.");
                        return View(model);
                    }

                    var changePasswordResult = await _userManager.ChangePasswordAsync(user,
                        model.CurrentPassword, model.NewPassword);
                    if (!changePasswordResult.Succeeded)
                    {
                        foreach (var error in changePasswordResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        return View(model);
                    }
                }

                // Update TeamMember
                if (user.TeamMemberId.HasValue)
                {
                    var teamMember = await _context.TeamMembers.FindAsync(user.TeamMemberId);
                    if (teamMember != null)
                    {
                        teamMember.Name = model.Name;
                        teamMember.Email = model.Email;
                        await _context.SaveChangesAsync();
                    }
                }

                await transaction.CommitAsync();
                TempData["Success"] = "Your profile has been updated successfully.";
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating profile");
                ModelState.AddModelError(string.Empty, "An error occurred while updating your profile. Please try again.");
                return View(model);
            }
        }

        // Password Recovery Methods
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError(string.Empty, "Email is required");
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                // Don't reveal that the user does not exist or is not confirmed
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            // Generate password reset token
            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Action(
                "ResetPassword",
                "Account",
                new { userId = user.Id, code },
                protocol: HttpContext.Request.Scheme);

            // Log the reset URL during development (remove in production)
            _logger.LogInformation($"Password reset URL: {callbackUrl}");

            try
            {
                await _emailService.SendEmailAsync(
                    email,
                    "Reset Your Password",
                    $"Please reset your password by clicking here: <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>Reset Password</a>");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset email");
                // Still redirect to confirmation page to avoid revealing user existence
            }

            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string code = null, string userId = null)
        {
            if (code == null || userId == null)
            {
                return BadRequest("A code and user ID must be supplied for password reset.");
            }

            ViewData["Code"] = code;
            ViewData["UserId"] = userId;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string email, string password, string confirmPassword, string code, string userId)
        {
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(userId))
            {
                return BadRequest("Invalid password reset attempt.");
            }

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
            {
                ModelState.AddModelError(string.Empty, "All fields are required");
                ViewData["Code"] = code;
                ViewData["UserId"] = userId;
                return View();
            }

            if (password != confirmPassword)
            {
                ModelState.AddModelError(string.Empty, "The password and confirmation password do not match.");
                ViewData["Code"] = code;
                ViewData["UserId"] = userId;
                return View();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.Email != email)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            var result = await _userManager.ResetPasswordAsync(user, code, password);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            ViewData["Code"] = code;
            ViewData["UserId"] = userId;
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }
    }
}