using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Foundation.Collections;

namespace Discouser.ViewModel
{
    class AllSites : ViewModelBase<Model.Site>, IDisposable
    {
        public ObservableCollection<Site> Sites { get; private set; }

        private const string loginSettingsKey = "logins";
        private const string guidSettingsKey = "Guid";
        private Guid LocalGuid;
        private IPropertySet Logins;

        public AllSites()
        {
            var roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            Logins = roamingSettings.CreateContainer(loginSettingsKey, Windows.Storage.ApplicationDataCreateDisposition.Always).Values;

            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings.Values;
            if (!(localSettings[guidSettingsKey] is Guid))
            {
                localSettings[guidSettingsKey] = Guid.NewGuid();
            }
            LocalGuid = (Guid)localSettings[guidSettingsKey];

            Sites = new ObservableCollection<Site>(Logins
                .Where(item => item.Key.Contains('@'))
                .Select(item => DecodeLogin(item.Key))
                .Select(login => new Site(new DataContext(login.Item1, login.Item2, LocalGuid))));

            NewSiteCommand = new Command(CanAddNewSite, AddNewSite);
        }

        private bool CanAddNewSite()
        {
            return !string.IsNullOrEmpty(NewSiteUrl)
                && Uri.IsWellFormedUriString(NewSiteUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? NewSiteUrl : "http://" + NewSiteUrl, UriKind.Absolute)
                && !string.IsNullOrEmpty(NewSiteUsername)
                && !string.IsNullOrEmpty(NewSitePassword);
        }

        private async Task AddNewSite()
        {
            var url = NewSiteUrl;
            var username = NewSiteUsername;
            var password = NewSitePassword;

            NewSiteFailedToAuthorize = false;
            NewSiteLoading = true;
            var siteToAdd = new Site(new DataContext(url, username, LocalGuid));

            var loggedInUser = await siteToAdd.Context.Authorize(username, password);
            NewSiteLoading = false;
            if (loggedInUser == username)
            {
                AddSite(siteToAdd);
                url = "";
                username = "";
                password = "";
            }
            else
            {
                NewSiteFailedToAuthorize = true;
            }
        }

        private void AddSite(Site siteToAdd)
        {
            var login = EncodeLogin(siteToAdd.Username, siteToAdd.Url);
            if (!Logins.ContainsKey(login))
            {
                Logins.Add(login, null);
            }

            Sites.Remove(siteToAdd);
            Sites.Add(siteToAdd);
        }

        private string EncodeLogin(string username, string url)
        {
            return username + "@" + url;
        }

        private Tuple<string, string> DecodeLogin(string login)
        {
            var split = login.Split('@');
            return Tuple.Create(split[0], split[1]);
        }


        /// <summary>
        /// Removes a site from the list of sites, and disposes its data context.
        /// </summary>
        public void RemoveSite(Site siteToRemove)
        {
            Sites.Remove(siteToRemove);
            Logins.Remove(EncodeLogin(siteToRemove.Username, siteToRemove.Url));
            siteToRemove.Context.Dispose();
        }

        public void Dispose()
        {
            foreach (var site in Sites)
            {
                site.Context.Dispose();
            }
        }

        public override void NotifyChanges(Model.Site model)
        {
            throw new NotImplementedException();
        }

        public Command NewSiteCommand { get; private set; }

        private string _newSiteUsername;
        public string NewSiteUsername
        {
            get { return _newSiteUsername; }
            set
            {
                _newSiteUsername = value;
                NewSiteCommand.Notify();
                RaisePropertyChanged("NewSiteUsername");
            }
        }
        private string _newSitePassword;
        public string NewSitePassword
        {
            get { return _newSitePassword; }
            set
            {
                _newSitePassword = value;
                NewSiteCommand.Notify();
                RaisePropertyChanged("NewSitePassword");
            }
        }
        private string _newSiteUrl;
        public string NewSiteUrl
        {
            get { return _newSiteUrl; }
            set
            {
                _newSiteUrl = value;
                NewSiteCommand.Notify();
                RaisePropertyChanged("NewSiteUrl");
            }
        }

        private bool _newSiteFailedToAuthorize;
        public bool NewSiteFailedToAuthorize
        {
            get { return _newSiteFailedToAuthorize; }
            set
            {
                _newSiteFailedToAuthorize = value;
                RaisePropertyChanged("NewSiteFailedToAuthorize");
            }
        }


        private bool _newSiteLoading;
        public bool NewSiteLoading
        {
            get { return _newSiteLoading; }
            set
            {
                _newSiteLoading = value;
                RaisePropertyChanged("NewSiteLoading");
            }
        }
    }
}
