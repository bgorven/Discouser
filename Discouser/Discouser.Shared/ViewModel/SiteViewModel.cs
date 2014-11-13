using Discouser.Data;
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

        public override async Task Initialize()
        {
            await _context.Initialize();
        }

        public override async Task NotifyChanges(Model.Site model)
        {
            if (_categories == null)
            {
                _categories = new ObservableCollection<Category>((await _context.AllCategories()).Select(category => new Category(category, _context)));
                _changedProperties = new string[] { "Categories" };
                CanRefresh = true;
            }
        }

        private ObservableCollection<Category> _categories;
        public ObservableCollection<Category> Categories
        {
            get
            {
                if (_categories == null)
                {
                    var task = Refresh();
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
