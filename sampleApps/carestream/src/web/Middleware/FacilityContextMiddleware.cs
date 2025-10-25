using carestream.core.infrastructure;
using carestream.core.interfaces.services;
using carestream.core.interfaces.repositories;
using System.Security.Claims;
using carestream.core.dtos.facility;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace carestream.web.Middleware
{
    public class FacilityContextMiddleware
    {
        private readonly RequestDelegate _next;

        public FacilityContextMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context,
                                      ICurrentFacilityContext facilityContext,
                                      IFacilitySelectionService facilitySelectionService,
                                      IUserRepository userRepository,
                                      ILogger<FacilityContextMiddleware> logger)
        {
            // Only attempt to set context if the user is authenticated
            if (context.User.Identity != null && context.User.Identity.IsAuthenticated)
            {
                // Initialize variables from claims or default
                int internalUserId = 0;
                List<FacilityDto> userAccessibleFacilities = new List<FacilityDto>();
                int currentFacilityId = 0;
                string currentFacilityName = "Unknown Facility"; // Default name for display
                bool claimsDataComplete = false;

                // --- Attempt to retrieve data from Claims (fast path after initial login/re-sign) ---
                var carestreamUserIdClaim = context.User.FindFirst("carestream_user_id");
                var userFacilitiesJsonClaim = context.User.FindFirst("carestream_user_facilities");
                var currentFacilityIdClaim = context.User.FindFirst("carestream_current_facility_id");

                if (carestreamUserIdClaim != null && int.TryParse(carestreamUserIdClaim.Value, out internalUserId) &&
                    userFacilitiesJsonClaim != null &&
                    currentFacilityIdClaim != null && int.TryParse(currentFacilityIdClaim.Value, out currentFacilityId))
                {
                    try
                    {
                        userAccessibleFacilities = JsonSerializer.Deserialize<List<FacilityDto>>(userFacilitiesJsonClaim.Value) ?? new List<FacilityDto>();
                        var facilityFromClaims = userAccessibleFacilities.FirstOrDefault(f => f.FacilityId == currentFacilityId);
                        if (facilityFromClaims != null)
                        {
                            currentFacilityName = facilityFromClaims.Name;
                            claimsDataComplete = true; // All necessary data found and valid in claims
                        }
                    }
                    catch (JsonException ex)
                    {
                        logger.LogError(ex, "Middleware: Failed to deserialize facilities from claims for user {UserId}. Forcing DB fallback.", internalUserId);
                        claimsDataComplete = false; // Force DB lookup
                    }
                }

                // --- If claims are missing or invalid, hit the Database (slow path for first request or invalid claims) ---
                if (!claimsDataComplete)
                {
                    logger.LogDebug("Middleware: Claims data incomplete/invalid for user {UserId}. Falling back to database for full setup.", internalUserId);
                    var sub = context.User.FindFirstValue("sub");
                    if (string.IsNullOrEmpty(sub))
                    {
                        logger.LogWarning("Middleware: Authenticated user has no 'sub' claim. Skipping facility context setup.");
                        await _next(context); // Proceed without setting context
                        return;
                    }

                    // Get internal user ID from DB if not already determined from claims
                    if (internalUserId == 0)
                    {
                        var idFromDb = await userRepository.GetUserIdByLogtoSubAsync(sub);
                        if (!idFromDb.HasValue)
                        {
                            logger.LogWarning("Middleware: Authenticated user sub '{Sub}' not found in internal users. Skipping facility context setup.", sub);
                            await _next(context); // Proceed without setting context
                            return;
                        }
                        internalUserId = idFromDb.Value; // Update internalUserId from DB lookup
                    }

                    // Get all facilities the user has access to from the DB
                    userAccessibleFacilities = (await facilitySelectionService.GetFacilitiesForUserAsync(internalUserId)).ToList();
                    if (!userAccessibleFacilities.Any())
                    {
                        logger.LogWarning("Middleware: User {InternalUserId} has no accessible facilities from DB. Skipping context setup.", internalUserId);
                        await _next(context); // Proceed without setting context
                        return;
                    }

                    // Determine current facility: prioritize cookie, then default facility logic
                    int selectedFacilityIdFromCookie = 0;
                    string? cookieValue = context.Request.Cookies["_CareStreamFacilityId"];
                    if (!string.IsNullOrEmpty(cookieValue) && int.TryParse(cookieValue, out int parsedFacilityId))
                    {
                        if (userAccessibleFacilities.Any(f => f.FacilityId == parsedFacilityId))
                        {
                            selectedFacilityIdFromCookie = parsedFacilityId;
                        }
                        else
                        {
                            logger.LogWarning("Middleware: User {InternalUserId} cookie facility ID {CookieId} is not accessible. Ignoring cookie.", internalUserId, parsedFacilityId);
                        }
                    }

                    if (selectedFacilityIdFromCookie != 0) // Cookie provided a valid accessible facility
                    {
                        currentFacilityId = selectedFacilityIdFromCookie;
                    }
                    else // No valid cookie or cookie facility not accessible, use default logic
                    {
                        var defaultFacilityDto = await facilitySelectionService.GetDefaultFacilityForUserAsync(internalUserId);
                        if (defaultFacilityDto != null)
                        {
                            currentFacilityId = defaultFacilityDto.FacilityId;
                        }
                    }

                    if (currentFacilityId == 0) // Still no facility could be determined as current
                    {
                        logger.LogError("Middleware: Could not determine valid current facility for user {InternalUserId} from DB fallback. Skipping context setup.", internalUserId);
                        await _next(context); // Proceed without setting context
                        return;
                    }

                    // Get the name for the determined current facility
                    var finalCurrentFacilityDto = userAccessibleFacilities.FirstOrDefault(f => f.FacilityId == currentFacilityId);
                    if (finalCurrentFacilityDto == null) // This should ideally not happen if logic above is correct
                    {
                        logger.LogError("Middleware: Determined current facility ID {CurrentId} not found in accessible list. This indicates a logic error. Skipping context.", currentFacilityId);
                        await _next(context);
                        return;
                    }
                    currentFacilityName = finalCurrentFacilityDto.Name;


                    // Re-sign the authentication cookie with fresh claims.
                    // This updates the claims on the client-side for subsequent requests and fast path.
                    var identity = context.User.Identity as ClaimsIdentity;
                    if (identity != null)
                    {
                        // Remove old carestream claims before adding new ones to prevent duplicates/stale data
                        // NOTE: Ensure these claims are unique and don't clash with OIDC defaults or other claims.
                        // "sub", "name", "roles" are typically OIDC, "carestream_..." are custom.
                        identity.TryRemoveClaim(identity.FindFirst("carestream_user_id"));
                        identity.TryRemoveClaim(identity.FindFirst("carestream_user_facilities"));
                        identity.TryRemoveClaim(identity.FindFirst("carestream_current_facility_id"));

                        identity.AddClaim(new Claim("carestream_user_id", internalUserId.ToString(), ClaimValueTypes.Integer32));
                        identity.AddClaim(new Claim("carestream_user_facilities", JsonSerializer.Serialize(userAccessibleFacilities), ClaimValueTypes.String));
                        identity.AddClaim(new Claim("carestream_current_facility_id", currentFacilityId.ToString(), ClaimValueTypes.Integer32));

                        await context.SignInAsync(context.User);
                        logger.LogInformation("Middleware: Re-signed authentication cookie with fresh facility claims for user {InternalUserId}.", internalUserId);
                    }

                    // Update the persistent cookie for the selected facility (or determined default)
                    // only if it needs to be changed.
                    if (cookieValue != currentFacilityId.ToString())
                    {
                        context.Response.Cookies.Append(
                            "_CareStreamFacilityId",
                            currentFacilityId.ToString(),
                            new CookieOptions
                            {
                                Expires = DateTimeOffset.UtcNow.AddYears(1), // Persistent cookie
                                HttpOnly = true,
                                IsEssential = true, // Essential for authentication flow
                                SameSite = SameSiteMode.Lax,
                                Secure = context.Request.IsHttps // Only send over HTTPS if in production
                            }
                        );
                        logger.LogInformation("Middleware: Set/Updated _CareStreamFacilityId cookie to: {FacilityId}.", currentFacilityId);
                    }
                }

                // --- Final step: Populate the Scoped ICurrentFacilityContext ---
                // This instance is now guaranteed to be available for dependency injection.
                if (facilityContext.IsFacilityContextSet == false) // Only set if not already populated (e.g., by fast path)
                {
                    facilityContext.SetCurrentFacility(
                        currentFacilityId,
                        currentFacilityName,
                        userAccessibleFacilities
                    );
                    logger.LogInformation("Middleware: ICurrentFacilityContext set for user {InternalUserId} to {FacilityName} (ID: {FacilityId}).", internalUserId, currentFacilityName, currentFacilityId);
                }
            }
            else // User is not authenticated, clear context and proceed
            {
                facilityContext.ClearContext();
                logger.LogDebug("Middleware: User not authenticated. Facility context cleared.");
            }

            await _next(context); // Continue down the pipeline
        }
    }
}