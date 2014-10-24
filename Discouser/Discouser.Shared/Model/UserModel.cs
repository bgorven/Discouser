using SQLite;

namespace Discouser.Model
{
    [Table("Users")]
    class User : IModel
    {
        [PrimaryKey]
        public int Id { get; set; }

        [Unique]
        public string Username { get; set; }

        public string DisplayName { get; set; }

        public string Title { get; set; }

        public int AvatarId { get; set; }
    }
}
