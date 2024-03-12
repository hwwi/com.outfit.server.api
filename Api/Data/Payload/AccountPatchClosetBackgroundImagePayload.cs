using System;
using Api.Data.Dto;

namespace Api.Data.Payload
{
    public class AccountPatchClosetBackgroundImagePayload
    {
        public long PersonId { get; set; }
        public Uri ClosetBackgroundImageUrl { get; set; }
    }
}