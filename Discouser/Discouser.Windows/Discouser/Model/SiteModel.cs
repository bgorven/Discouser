using SQLite;

namespace Discouser.Model
{
    [Table("Sites")]
    internal class Site : Model
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Unique]
        public string Url { get; set; }

        public string Name { get; set; }
    }
}