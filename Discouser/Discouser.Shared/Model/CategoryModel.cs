﻿using SQLite;

namespace Discouser.Model
{
    [Table("Categories")]
    class Category : Model
    {
        [PrimaryKey]
        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }
    }
}