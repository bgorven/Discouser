using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Discouser.Model
{
    [Table("UserInfo")]
    class UserInfo
    {
        [PrimaryKey]
        public int Id { get; set; }

        public string BioText { get; set; }

        public string Site { get; set; }

        public string Location { get; set; }
    }
}
