using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Discouser.ViewModel
{
    class Site : ViewModelBase<Model.Site>
    {
        public Site(string url, string username)
        {
            Url = url;
            _context = new DataContext(url, username);
        }

        public override void NotifyChanges(Model.Site model)
        {
            throw new NotImplementedException();
        }

        public override Task LoadChanges()
        {
            throw new NotImplementedException();
        }

        public string Url { get; private set; }

        public string Name { get; private set; }
    }
}
