using SQLite;

namespace Discouser.Model
{
    [Table("Categories")]
    class Category : IModel
    {
        [PrimaryKey]
        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Color { get; set; }

        public string TextColor { get; set; }

        public int PagesFetched { get; set; }

        public bool Initialized { get; set; }

        public string Path { get; set; }
    }
}
