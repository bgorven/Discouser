using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discouser.ViewModel
{
    class Site : ViewModelBase<Model.Site>
    {
        public Site() : this(new DataContext()) { }

        public Site(DataContext context)
        {
            _context = context;
        }

        public override void NotifyChanges(Model.Site model) { }

        public override async Task LoadData()
        {
            await _context.Initialize();
            _categories = new ObservableCollection<Category>((await _context.AllCategories()).Select(model => new Category(model, _context)));
            RaisePropertyChanged("Categories");
        }

        private ObservableCollection<Category> _categories;
        public ObservableCollection<Category> Categories
        {
            get
            {
                if (_categories == null)
                {
                    var task = LoadData();
                }
                return _categories;
            }
            set
            {
                _categories = value;
            }
        }

        public DataContext Context { get { return _context; } }

        public string Url { get { return _context.SiteUrl; } }

        public string Username { get { return _context.Username; } }

        public string Logo { get { return _context.FolderName + "Logo.png"; } }

        public string SiteName { get { return _context.SiteName ?? _context.SiteUrl; } }
    }
}
