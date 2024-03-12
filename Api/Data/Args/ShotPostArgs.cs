using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Api.Data.DataAnnotations;
using Api.Data.Dto;

namespace Api.Data.Args
{
    public class ShotPostArgs
    {
        [Required(AllowEmptyStrings = true), ShotCaption]
        public string Caption { get; set; }

        [Required]
        public List<TagListAndFileIndex> TagListAndFileIndexList { get; set; }
    }

    public class TagListAndFileIndex
    {
        [Required]
        public int FileIndex { get; set; }

        [Required]
        public List<ItemTagArgs> ItemTags { get; set; }
    }

    public class ItemTagArgs
    {
        [Required, BrandCode]
        public string Brand { get; set; } = default!;

        [Required(AllowEmptyStrings = true), ProductCode]
        public string Product { get; set; } = default!;

        [Required, Coordinate]
        public float X { get; set; }

        [Required, Coordinate]
        public float Y { get; set; }
    }
}