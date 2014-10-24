using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Discouser.Model
{
    [Table("Replies")]
    class Reply : IModel
    {
        [PrimaryKey]
        public int Id { get; set; }

        [Indexed]
        public int OriginalPostId { get; set; }

        [Indexed]
        public int ReplyPostId { get; set; }
    }
}
