using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Discouser.ViewModel
{
    class Site : ViewModelBase<Model.Site>
    {
        public Site(DataContext context)
        {
            _context = context;
        }

        public override void NotifyChanges(Model.Site model)
        {
            throw new NotImplementedException();
        }

        public override Task LoadChanges()
        {
            throw new NotImplementedException();
        }

        public DataContext Context { get { return _context; } }

        public string Url { get { return _context.SiteUrl; } }

        public string Username { get { return _context.Username; } }

        public string Name { get { return _context.SiteName ?? _context.SiteUrl; } }
    }
}
