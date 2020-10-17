using SQLBuilder.Attributes;

namespace SQLBuilder.UnitTest
{
    [Table("student")]
    public class MyStudent
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Subject { get; set; }
        public decimal? Score { get; set; }
        public bool? IsEffective { get; set; }
        public bool IsOnLine { get; set; }
    }
}
