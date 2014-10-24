﻿using SQLite;

namespace Discouser.Model
{
    [Table("RawText")]
    class LongText : IModel
    {
        [PrimaryKey]
        public int Id { get; set; }

        public int? Next { get; set; }

        [MaxLength(140)]
        public string Text { get; set; }
    }
}
