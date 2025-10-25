namespace carestream.core.dtos.user
{
    public class UserVerificationCodeInfo // Simple class for return tuple items
    {
        public string? HashedVerificationCode { get; set; }
        public string? VerificationCodeSalt { get; set; }
    }
}
