using SQLBuilder.Attributes;

namespace SQLBuilder
{
    [Table("student")]
    public class MyStudent
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Subject { get; set; }
        public decimal? Score { get; set; }
    }
}
