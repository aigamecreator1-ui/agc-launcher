using AGC.Server.Configuration;
using AGC.Server.Data;
using AGC.Server.Entities;
using AGC.Server.Services;
using AGC.Shared.Dtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AGC.Server.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    AppDbContext db,
    AppOptions options,
    PasswordHasher<User> passwordHasher,
    ITokenService tokenService,
    IEmailSender emailSender,
    OwnerCodeAttemptLimiter ownerAttemptLimiter,
    IHostEnvironment env,
    ILogger<AuthController> logger) : ControllerBase
{
    // Deliberately identical for every failure branch of the owner-code endpoint, so a
    // wrong code can never be distinguished from "this isn't the owner account."
    private const string OwnerLoginGenericError = "We couldn't sign you in with that information.";

    [HttpPost("signup")]
    public async Task<ActionResult<AuthResultDto>> SignUp(SignUpRequestDto request, CancellationToken ct)
    {
        var email = Normalize(request.Email);
        var username = request.Username.Trim();

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            return BadRequest(new ApiErrorDto("Enter a valid email address."));
        }

        if (username.Length is < 3 or > 24)
        {
            return BadRequest(new ApiErrorDto("Username must be between 3 and 24 characters."));
        }

        if (string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new ApiErrorDto("Password is required."));
        }

        // The owner email is never a normal DB account — treat it as already-taken so
        // sign-up can't be used to shadow/impersonate it, without revealing why.
        var emailTaken = string.Equals(email, Normalize(options.OwnerEmail), StringComparison.Ordinal)
            || await db.Users.AnyAsync(u => u.Email == email, ct);
        if (emailTaken)
        {
            return Conflict(new ApiErrorDto("That email is already registered."));
        }

        if (await db.Users.AnyAsync(u => u.Username == username, ct))
        {
            return Conflict(new ApiErrorDto("That username is taken."));
        }

        var user = new User { Email = email, Username = username, PasswordHash = "" };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        var token = tokenService.IssueToken(user, isOwner: false);
        return Ok(new AuthResultDto(token, ToDto(user, isOwner: false)));
    }

    [HttpPost("login/request")]
    public async Task<ActionResult<LoginRequestResponseDto>> RequestLogin(LoginRequestDto request, CancellationToken ct)
    {
        var email = Normalize(request.Email);

        if (string.Equals(email, Normalize(options.OwnerEmail), StringComparison.Ordinal))
        {
            return Ok(new LoginRequestResponseDto(LoginRequestStatus.OwnerCodeRequired));
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null)
        {
            return Ok(new LoginRequestResponseDto(LoginRequestStatus.AccountNotFound));
        }

        var code = VerificationCodeGenerator.GenerateSixDigitCode();
        db.VerificationCodes.Add(new VerificationCode
        {
            Email = email,
            CodeHash = VerificationCodeGenerator.Hash(code),
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
        });
        await db.SaveChangesAsync(ct);

        var devMode = string.IsNullOrEmpty(options.ResendApiKey);
        var sendFailed = false;
        try
        {
            await emailSender.SendVerificationCodeAsync(email, code, ct);
        }
        catch (Exception ex)
        {
            sendFailed = true;
            logger.LogError(ex, "Failed to send verification email to {Email}", email);

            // In Development, don't hard-block login over a real-provider send failure
            // (e.g. Resend's sandbox sender only delivers to the account's own email
            // until a domain is verified) — fall back to echoing the code like dev mode.
            if (!devMode && !env.IsDevelopment())
            {
                return StatusCode(502, new ApiErrorDto("Couldn't send the verification email. Try again shortly."));
            }
        }

        var echoCode = devMode || sendFailed;
        return Ok(new LoginRequestResponseDto(LoginRequestStatus.EmailCodeSent, echoCode ? code : null));
    }

    [HttpPost("login/verify")]
    public async Task<ActionResult<AuthResultDto>> VerifyLogin(VerifyLoginCodeRequestDto request, CancellationToken ct)
    {
        var email = Normalize(request.Email);
        const string invalidCodeError = "That code is invalid or has expired.";

        var pending = await db.VerificationCodes
            .Where(v => v.Email == email && !v.Consumed && v.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (pending is null || pending.FailedAttempts >= 5)
        {
            return BadRequest(new ApiErrorDto(invalidCodeError));
        }

        if (pending.CodeHash != VerificationCodeGenerator.Hash(request.Code.Trim()))
        {
            pending.FailedAttempts++;
            await db.SaveChangesAsync(ct);
            return BadRequest(new ApiErrorDto(invalidCodeError));
        }

        pending.Consumed = true;

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null)
        {
            return BadRequest(new ApiErrorDto(invalidCodeError));
        }

        await db.SaveChangesAsync(ct);

        var token = tokenService.IssueToken(user, isOwner: false);
        return Ok(new AuthResultDto(token, ToDto(user, isOwner: false)));
    }

    [HttpPost("owner/verify")]
    public async Task<ActionResult<AuthResultDto>> VerifyOwner(VerifyOwnerCodeRequestDto request, CancellationToken ct)
    {
        var rateLimitKey = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (ownerAttemptLimiter.IsBlocked(rateLimitKey))
        {
            return BadRequest(new ApiErrorDto(OwnerLoginGenericError));
        }

        var email = Normalize(request.Email);
        var isOwnerEmail = string.Equals(email, Normalize(options.OwnerEmail), StringComparison.Ordinal);
        var isCorrectCode = request.Code.Trim() == options.OwnerSecurityCode;

        if (!isOwnerEmail || !isCorrectCode)
        {
            ownerAttemptLimiter.RecordFailure(rateLimitKey);
            return BadRequest(new ApiErrorDto(OwnerLoginGenericError));
        }

        ownerAttemptLimiter.RecordSuccess(rateLimitKey);

        var owner = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (owner is null)
        {
            owner = new User
            {
                Email = email,
                Username = "Owner",
                PasswordHash = string.Empty,
            };
            db.Users.Add(owner);
            await db.SaveChangesAsync(ct);
        }

        var token = tokenService.IssueToken(owner, isOwner: true);
        return Ok(new AuthResultDto(token, ToDto(owner, isOwner: true)));
    }

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();

    private static UserDto ToDto(User user, bool isOwner) => new(user.Id, user.Email, user.Username, isOwner);
}
