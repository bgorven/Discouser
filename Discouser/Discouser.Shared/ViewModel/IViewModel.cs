using System;
using System.ComponentModel;
using System.Threading.Tasks;
namespace Discouser.ViewModel
{
    interface IViewModel : INotifyPropertyChanged
    {
        Task Initialize();
        bool Initialized { get; set; }
        void Loaded();
        Task OnLoad();
        void Unloaded();
        Task OnUnload();

        bool CanRefresh { get; set; }
        Task Refresh();
        Command RefreshCommand { get; }
    }
}
