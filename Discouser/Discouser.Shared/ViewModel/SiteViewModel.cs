using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Discouser.ViewModel
{
    class Site : ViewModelBase<Model.Site>
    {
        public override Task Load()
        {
            throw new NotImplementedException();
        }

        public string Url { get { return _model.Url; } }

        public string Name { get { return _model.Name; } }
    }
}
