namespace Api.Data
{
    public enum NotificationType
    {
        // 1:팔로워 라 db에는 저장 안함
        ShotPosted,
        
        ShotIncludePersonTag,
        ShotLiked,
        Commented,
        CommentIncludePersonTag,
        CommentLiked,
        Followed,
    }
}