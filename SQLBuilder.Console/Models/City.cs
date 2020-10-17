using SQLBuilder.Attributes;

namespace SQLBuilder
{
    [Table("Base_City")]
    public class City
    {
        public int Id { get; set; }
        public int CountryId { get; set; }
        public string CityName { get; set; }
    }
}
