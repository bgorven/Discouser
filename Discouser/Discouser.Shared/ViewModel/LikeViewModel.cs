using Discouser.Api;
using SQLite;
using System.Threading.Tasks;

namespace Discouser.ViewModel
{
    class Like : ViewModelBase<Model.Like>
    {
        public Like(Model.Like model, SQLiteConnection db, ApiConnection api) : base(model, db, api) { }
        public Like(int id, SQLiteConnection db, ApiConnection api) : base(id, db, api) { }

        public async override Task Load()
        {
            await Task.FromResult(Changes = false);
        }

        public User User { get { return new User(_model.UserId, _db, _api); } }
    }
}
