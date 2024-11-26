﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BusinessSearch.Models;
using BusinessSearch.Models.ViewModels;
using BusinessSearch.Data;

namespace BusinessSearch.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountController> logger,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (_signInManager.IsSignedIn(User))
                return RedirectToAction("Index", "Home");

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
                .FirstOrDefaultAsync(u => u.Id == user.Id);

            var model = new ProfileViewModel
            {
                Id = user.Id,
                Name = user.TeamMember?.Name ?? "",
                Email = user.Email ?? "",
                CreatedAt = user.CreatedAt,
                TeamMemberId = user.TeamMemberId
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
    }
}