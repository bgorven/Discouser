using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Discouser.Model
{
    [Table("Sites")]
    class Site : Model
    {
        [PrimaryKey]
        public int Id { get; set; }

        public string Name { get; set; }

        public string Url { get; set; }
    }
}
