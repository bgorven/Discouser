using SQLite;
using System;

namespace Discouser.Model
{
    [Table("Posts")]
    class Post : IModel
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

        public string RawText { get; set; }

        public string HtmlText { get; set; }

        public bool Deleted { get; set; }
    }
}
