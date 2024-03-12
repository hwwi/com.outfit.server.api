using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Api.Data.Models;
using AutoMapper;

namespace Api.Data.Dto
{
    public class ImageDto 
    {
        [Required]
        public Uri Url { get; set; }

        [Required]
        public string ContentType { get; set; }

        [Required]
        public int Width { get; set; }

        [Required]
        public int Height { get; set; }

        [Required]
        public long Length { get; set; }
        [Required]
        public List<ItemTagDto> ItemTags { get; set; }
    }
}