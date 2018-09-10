using SQLBuilder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SQLBuilder.UnitTest
{
    [Table("Base_Country")]
    public class Country
    {
        [Column("Country_Id")]
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
