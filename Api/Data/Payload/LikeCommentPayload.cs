namespace Api.Data.Payload
{
    public class LikeCommentPayload
    {
        public long CommentId { get; set; }
        public int LikesCount { get; set; }
        public bool IsViewerLike { get; set; }
    }
}