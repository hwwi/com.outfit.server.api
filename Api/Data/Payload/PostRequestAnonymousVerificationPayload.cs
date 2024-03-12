using Api.Service;

namespace Api.Data.Payload
{
    public class PostRequestAnonymousVerificationPayload
    {
        public long VerificationId { get; set; }
        public RouteAnonymousVerificationPurpose Purpose { get; set; }
        public VerificationMethod Method { get; set; }
        public string To { get; set; }
        public int CodeLength { get; set; }
    }
}