using System;
using System.Threading.Tasks;
using SQLite;
using Discouser.Api;
using Discouser.Model;

namespace Discouser.ViewModel
{
    class User : ViewModelBase<Model.User>
    {
        private UserInfo _info;

        public User(Model.User model, SQLiteConnection db, ApiConnection api) : base(model, db, api)
        {
            _info = db.Get<UserInfo>(_model.Id);
        }

        public User(int id, SQLiteConnection db, ApiConnection api) : base(id, db, api)
        {
            _info = db.Get<UserInfo>(_model.Id);
        }

        public string Username { get { return _model.Username; } }

        public string DisplayName { get { return _model.DisplayName; } }

        public string Title { get { return _model.Title; } }

        public int AvatarId { get { return _model.AvatarId; } }

        public string Location { get { return _info.Location; } }

        public string Site {  get { return _info.Site; } }

        public string Bio { get { return new RawText(_db.Get<Model.RawText>(_info.Bio), _db, _api).Text; } }

        public override async Task Load()
        {
            var model = _db.Get<Model.User>(_model.Id);
            var info = _db.Get<UserInfo>(_model.Id);

            RaisePropertyChanged(new String[] {
                _model.Username == model.Username ? "" : "Username",
                _model.DisplayName == model.DisplayName ? "" : "DisplayName",
                _model.Title == model.Title ? "" : "Title",
                _model.AvatarId == model.AvatarId ? "" : "AvatarId",
                _info.Bio == info.Bio ? "" : "Bio",
                _info.Location == info.Location ? "" : "Location",
                _info.Site == info.Site ? "" : "Site"
            });

            _model = model;

            await Task.FromResult(Changes = false);
        }
    }
}
