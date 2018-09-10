using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLBuilder;

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
