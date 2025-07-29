using Google.Apis.Auth;

namespace SesliDil.API.Validators
{
    public static class GoogleTokenValidator
    {
        public static async Task<GoogleJsonWebSignature.Payload> ValidateAsync(string idToken, string clientId)
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings()
            {
                Audience = new[] { clientId }
            };

            return await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
        }
    }
}
