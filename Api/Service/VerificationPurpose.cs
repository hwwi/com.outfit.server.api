namespace Api.Service
{
    public enum VerificationPurpose
    {
        SignUp,
        ChangeEmail,
        ResetPassword,
    }

    public enum RouteAnonymousVerificationPurpose
    {
        SignUp = VerificationPurpose.SignUp,
        ResetPassword = VerificationPurpose.ResetPassword,
    }

    public enum RouteVerificationPurpose
    {
        ChangeEmail = VerificationPurpose.ChangeEmail
    }
}