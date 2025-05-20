//Backbone.Core/Settings/JwtSettings.cs
namespace Backbone.Core.Settings
{
    public class JwtSettings
    {
        public string Secret { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public int ExpirationInMinutes { get; set; } = 15;
    }
}