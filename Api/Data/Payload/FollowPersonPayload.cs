namespace Api.Data.Payload
{
    public class FollowPersonPayload
    {
        public long FollowerId { get; set; }
        public int FollowerFollowingsCount { get; set; }
        public long FollowedId { get; set; }
        public int FollowedFollowersCount { get; set; }
        public bool IsFollow { get; set; }
        
    }
}