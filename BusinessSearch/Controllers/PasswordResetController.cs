using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using BusinessSearch.Models;
using Microsoft.AspNetCore.Authorization;
using System.Text.Encodings.Web;

namespace BusinessSearch.Controllers
{
    [AllowAnonymous]
    public class PasswordResetController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<PasswordResetController> _logger;
        private readonly IEmailSender _emailSender;

        public PasswordResetController(
            UserManager<ApplicationUser> userManager,
            ILogger<PasswordResetController> logger,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _logger = logger;
            _emailSender = emailSender;
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
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
                "PasswordReset",
                new { userId = user.Id, code = code },
                protocol: HttpContext.Request.Scheme);

            // Log the reset URL during development (remove in production)
            _logger.LogInformation($"Password reset URL: {callbackUrl}");

            try
            {
                await _emailSender.SendEmailAsync(
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
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
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
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }
        

        [HttpGet("TestSendGrid")]
        public async Task<IActionResult> TestSendGrid()
        {
            try
            {
                var apiKey = "YOUR_API_KEY"; // Replace with actual key
                var client = new SendGrid.SendGridClient(apiKey);

                var from = new SendGrid.Helpers.Mail.EmailAddress("chudler624@gmail.com", "Test Email");
                var to = new SendGrid.Helpers.Mail.EmailAddress("chudler624@gmail.com"); // Send to yourself
                var plainTextContent = "Testing SendGrid integration";
                var htmlContent = "<p>Testing SendGrid integration</p>";

                var msg = SendGrid.Helpers.Mail.MailHelper.CreateSingleEmail(
                    from, to, "Test Email", plainTextContent, htmlContent);

                var response = await client.SendEmailAsync(msg);

                var responseBody = await response.Body.ReadAsStringAsync();
                return Content($"Status: {response.StatusCode}\nBody: {responseBody}");
            }
            catch (Exception ex)
            {
                return Content($"Error: {ex.Message}\n\nStack trace: {ex.StackTrace}");
            }
        }

        [HttpGet("TestEmail")]
        public async Task<IActionResult> TestEmail()
        {
            try
            {
                _logger.LogInformation("Starting test email send...");

                await _emailSender.SendEmailAsync(
                    "chudler624@gmail.com", // Your verified email
                    "Test Email from Business Search",
                    "<p>This is a test email to verify SendGrid integration.</p>");

                _logger.LogInformation("Email sending completed");
                return Content("Test email attempt completed. Check your inbox and application logs.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test email");
                return Content($"Error: {ex.Message}\n\nStack trace: {ex.StackTrace}");
            }
        }
    }


}