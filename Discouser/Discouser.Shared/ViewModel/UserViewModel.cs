using System;
using System.Threading.Tasks;
using SQLite;
using Discouser.Data;
using Discouser.Model;

namespace Discouser.ViewModel
{
    class User : ViewModelBase<Model.User>
    {
        private UserInfo _info;

        public User(int id, DataContext context) : base(id, context)
        {
            _info = LoadInfo();
        }

        private UserInfo LoadInfo()
        {
            return _context.PersistentDbConnection.Get<UserInfo>(_model.Id);
        }

        public string Username { get { return _model.Username; } }
        public string DisplayName { get { return _model.DisplayName; } }
        public string Title { get { return _model.Title; } }
        public int AvatarId { get { return _model.AvatarId; } }
        public string Location { get { return _info.Location; } }
        public string Site {  get { return _info.Site; } }
        public string Bio { get { return _info.BioText; } }

        public override void NotifyChanges(Model.User model)
        {
            model = model ?? LoadModel();
            var info = LoadInfo();

            _changedProperties = new String[] {
                _model.Username == model.Username ? "" : "Username",
                _model.DisplayName == model.DisplayName ? "" : "DisplayName",
                _model.Title == model.Title ? "" : "Title",
                _model.AvatarId == model.AvatarId ? "" : "AvatarId",
                _info.BioText == info.BioText ? "" : "Bio",
                _info.Location == info.Location ? "" : "Location",
                _info.Site == info.Site ? "" : "Site"
            };

            _model = model;
            _info = info;
        }
    }
}
