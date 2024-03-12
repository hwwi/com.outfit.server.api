using System;
using System.ComponentModel.DataAnnotations;
using Api.Data.DataAnnotations;
using Api.Data.Models;
using AutoMapper;

namespace Api.Data.Dto
{
    public class PersonDto: IIdentifiability
    {
        public long Id { get; set; }

        [PersonName]
        public string Name { get; set; }

        public Uri? ProfileImageUrl { get; set; }
    }
}