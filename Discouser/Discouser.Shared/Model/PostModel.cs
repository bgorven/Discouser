using SQLite;
using System;

namespace Discouser.Model
{
    [Table("Posts")]
    class Post : Model
    {
        [PrimaryKey]
        public int Id { get; set; }

        [Indexed]
        public int TopicId { get; set; }

        [Indexed]
        public int PostNumberInTopic { get; set; }

        public int UserId { get; set; }

        [NotNull]
        public DateTime Created { get; set; }

        public int Text { get; set; }

        public int Html { get; set; }

        public bool Deleted { get; set; }

        [Ignore]
        public string HtmlCache { get; set; }

        [Ignore]
        public string TextCache { get; set; }
    }
}
