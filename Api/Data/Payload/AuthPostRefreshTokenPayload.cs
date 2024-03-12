namespace Api.Data.Payload
{
    public class AuthPostRefreshTokenPayload
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
}