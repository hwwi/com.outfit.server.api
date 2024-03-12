using Api.Service;

namespace Api.Data.Payload
{
    public class PostRequestVerificationPayload
    {
        public long VerificationId { get; set; }
        public RouteVerificationPurpose Purpose { get; set; }
        public VerificationMethod Method { get; set; }
        public int CodeLength { get; set; }
    }
}