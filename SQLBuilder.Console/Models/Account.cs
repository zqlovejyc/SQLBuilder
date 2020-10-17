using SQLBuilder.Attributes;

namespace SQLBuilder
{
    [Table("Base_Account", Schema = "")]
    public class Account
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }
    }
}
