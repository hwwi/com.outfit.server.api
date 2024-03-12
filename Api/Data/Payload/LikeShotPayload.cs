namespace Api.Data.Payload
{
    public class LikeShotPayload
    {
        public long ShotId { get; set; }
        public int LikesCount { get; set; }
        public bool IsViewerLike { get; set; }
    }
}