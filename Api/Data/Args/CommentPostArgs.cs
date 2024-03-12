using System.ComponentModel.DataAnnotations;
using Api.Data.DataAnnotations;

namespace Api.Data.Args
{
    public class CommentPostArgs
    {
        [Required, CommentText]
        public string Text { get; set; }
    }
}