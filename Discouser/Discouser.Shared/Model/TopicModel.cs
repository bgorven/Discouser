using SQLite;
using System;

namespace Discouser.Model
{
    [Table("Topics")]
    class Topic : Model
    {
        [PrimaryKey]
        public int Id { get; set; }

        [Indexed]
        public int? CategoryId { get; set; }

        public string Name { get; set; }

        [Indexed]
        public DateTime Activity { get; set; }

        /// <summary>
        /// Id of latest info packet received about this thread from messagebus
        /// </summary>
        public int LatestMessage { get; internal set; }
    }
}
