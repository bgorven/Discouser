using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Discouser.Model
{
    [Table("Likes")]
    class Like : IModel
    {
        [Ignore]
        public int Id { get; set; }

        /// <summary>
        /// Enforce uniqueness.
        /// </summary>
        [PrimaryKey, MaxLength(23)]
        public string KeyString { get { return PostId + "/" + UserId; } set { } }

        [Indexed]
        public int PostId { get; set; }

        public int UserId { get; set; }
    }
}
