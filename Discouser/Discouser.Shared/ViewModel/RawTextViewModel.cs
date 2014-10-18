using Discouser.Api;
using SQLite;
using System.Threading.Tasks;

namespace Discouser.ViewModel
{
    class RawText : ViewModelBase<Model.RawText>
    {
        public RawText(Model.RawText model, SQLiteConnection db, ApiConnection api) : base(model, db, api) { }
        public RawText(int id, SQLiteConnection db, ApiConnection api) : base(id, db, api) { }

        private string _text = "";

        /// <summary>
        /// Not Implemented
        /// </summary>
        /// <returns>NotImplementedException</returns>
        public async override Task Load()
        {
            await Task.FromResult(Changes = false);
        }

        public string Text
        {
            get
            {
                if (string.IsNullOrEmpty(_text) && !string.IsNullOrEmpty(_model.Text))
                {
                    var text = _model.Text;
                    var raw = _model;
                    while (raw.Next.HasValue)
                    {
                        raw = _db.Get<Model.RawText>(raw.Next.Value);
                        text += raw.Text;
                    }
                    _text = text;
                }
                return _text;
            }
            //get
            //{
            //    if (string.IsNullOrEmpty(_text) && !string.IsNullOrEmpty(_rawText.Text))
            //    {
            //        var result = _db.QueryAsync<RawTextModel>("WITH RECURSIVE ids(n) AS(" + 
            //            "VALUES(" + _rawText.Next + ") UNION ALL" +
            //            "SELECT next FROM rawtext, ids" +
            //            "WHERE (rawtext.id = ids.n AND ids.n NOT NULL) )" +
            //            "SELECT text FROM rawtext WHERE rawtext.id IN ids").GetAwaiter().GetResult();
            //        _text = result.Aggregate("", (str, raw) => str + raw.Text);
            //    }
            //    return _text;
            //}
        }
    }
}
