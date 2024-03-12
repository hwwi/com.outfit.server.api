using Api.Data.DataAnnotations;

namespace Api.Data.Args
{
    public class AccountPatchNameArgs
    {
        [PersonName]
        public string Name { get; set; }
    }
}