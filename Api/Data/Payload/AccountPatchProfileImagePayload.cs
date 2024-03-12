using System;
using Api.Data.Dto;

namespace Api.Data.Payload
{
    public class AccountPatchProfileImagePayload
    {
        public long PersonId { get; set; }
        public Uri ProfileImageUrl { get; set; }
    }
}