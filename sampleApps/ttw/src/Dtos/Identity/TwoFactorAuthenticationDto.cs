namespace ttw.Dtos.Identity
{
    public class TwoFactorAuthenticationDto
    {
        public bool HasAuthenticator { get; set; }
        public int RecoveryCodesLeft { get; set; }
        public bool Is2faEnabled { get; set; }
        public bool IsMachineRemembered { get; set; }
    }
}