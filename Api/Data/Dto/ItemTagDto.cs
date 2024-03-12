using System.ComponentModel.DataAnnotations;
using Api.Data.DataAnnotations;
using Api.Data.Models;
using AutoMapper;

namespace Api.Data.Dto
{
    public class ItemTagDto 
    {
        [Required, BrandCode]
        public string Brand { get; set; }

        [Required, ProductCode]
        public string Product { get; set; }

        [Required, Coordinate]
        public float X { get; set; }

        [Required, Coordinate]
        public float Y { get; set; }
    }
}