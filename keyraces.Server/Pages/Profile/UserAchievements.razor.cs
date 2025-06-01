using keyraces.Server.Dtos;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace keyraces.Server.Pages.Profile
{
    public partial class UserAchievements
    {

        private List<UserAchievementDisplayDto>? achievements;
        private bool isLoading = true;
        private string? errorMessage;
        private int? userProfileId;

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity != null && user.Identity.IsAuthenticated)
            {
                try
                {
                    var userProfileIdString = await LocalStorage.GetItemAsync<string>("userProfileId");
                    if (int.TryParse(userProfileIdString, out int idFromLocalStorage))
                    {
                        userProfileId = idFromLocalStorage;
                    }
                    else
                    {
                        Claim? idClaim = user.FindFirst("user_profile_id");

                        if (idClaim != null && int.TryParse(idClaim.Value, out int idFromCustomClaim))
                        {
                            userProfileId = idFromCustomClaim;
                        }
                        else
                        {
                            Claim? nameIdentifierClaim = user.FindFirst(ClaimTypes.NameIdentifier);
                            if (nameIdentifierClaim != null && int.TryParse(nameIdentifierClaim.Value, out int idFromNameIdentifier))
                            {
                                userProfileId = idFromNameIdentifier;
                            }
                            else
                            {
                                errorMessage = "Could not determine user profile ID from local storage or claims.";
                                isLoading = false;
                                return;
                            }
                        }
                    }

                    if (userProfileId.HasValue)
                    {
                        achievements = await Http.GetFromJsonAsync<List<UserAchievementDisplayDto>>("api/achievement/user");
                    }
                    else
                    {
                        errorMessage = "User profile ID could not be determined. Cannot load achievements.";
                    }
                }
                catch (System.Exception ex)
                {
                    errorMessage = $"Error loading achievements: {ex.Message}";
                }
            }
            else
            {
                errorMessage = "User not authenticated.";
            }
            isLoading = false;
        }
    }
}
