using Discouser.Data;
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
                .Select(login => new Site(new DataContext(username: login.Item1, url: login.Item2, guid: LocalGuid))));

            NewSiteCommand = new Command(CanAddNewSite, AddNewSite);
            NewSiteCancelAddCommand = new Command(CancelAddNewSite);
        }

        private bool CanAddNewSite()
        {
            return !string.IsNullOrEmpty(NewSiteUrl)
                && Uri.IsWellFormedUriString(NewSiteUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? NewSiteUrl : "http://" + NewSiteUrl, UriKind.Absolute)
                && !string.IsNullOrEmpty(NewSiteUsername)
                && !string.IsNullOrEmpty(NewSitePassword);
        }

        private async Task CancelAddNewSite()
        {
            await Task.FromResult(NewSiteLoading = false);
        }

        private async Task AddNewSite()
        {
            NewSiteFailedToAuthorize = false;
            NewSiteLoading = true;

            try {
                var url = NewSiteUrl;
                var username = NewSiteUsername;
                var password = NewSitePassword;
                var siteToAdd = new Site(new DataContext(url, username, LocalGuid));

                if (!NewSiteLoading) return;
                var loggedInUser = await siteToAdd.Context.Authorize(username, password);


                if (!NewSiteLoading) return;
                NewSiteLoading = false;
                if (loggedInUser == username)
                {
                    await siteToAdd.Context.Initialize();
                    await siteToAdd.LoadData();
                    AddSite(siteToAdd);
                    NewSiteUrl = "";
                    NewSiteUsername = "";
                    NewSitePassword = "";
                    NewSiteViewVisible = false;
                }
                else
                {
                    NewSiteFailedToAuthorize = true;
                }
            } catch (Exception é)
            {
                é.Message.ToList();
                NewSiteFailedToAuthorize = true;
            }
        }

        private void AddSite(Site siteToAdd)
        {
            foreach (var siteToRemove in Sites
                .Where(site => site.Url == siteToAdd.Url && site.Username == siteToAdd.Username)
                .ToList())
            {
                RemoveSite(siteToRemove);
            }
            Logins.Add(EncodeLogin(siteToAdd.Username, siteToAdd.Url), "");
            Sites.Add(siteToAdd);
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

        private string EncodeLogin(string username, string url)
        {
            return username + "@" + url;
        }

        /// <summary>
        /// Username in item1, Url in item2
        /// </summary>
        private Tuple<string, string> DecodeLogin(string login)
        {
            var split = login.Split('@');
            return Tuple.Create(split[0], split[1]);
        }

        public void Dispose()
        {
            foreach (var site in Sites)
            {
                site.Context.Dispose();
            }
        }

        public override Task NotifyChanges(Model.Site model)
        {
            throw new NotImplementedException();
        }

        public Command NewSiteCommand { get; private set; }
        public Command NewSiteCancelAddCommand { get; private set; }

        private volatile string _newSiteUsername;
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
        private volatile string _newSitePassword;
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
        private volatile string _newSiteUrl;
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

        private volatile bool _newSiteFailedToAuthorize;
        public bool NewSiteFailedToAuthorize
        {
            get { return _newSiteFailedToAuthorize; }
            set
            {
                _newSiteFailedToAuthorize = value;
                RaisePropertyChanged("NewSiteFailedToAuthorize");
            }
        }


        private volatile bool _newSiteLoading;
        public bool NewSiteLoading
        {
            get { return _newSiteLoading; }
            set
            {
                _newSiteLoading = value;
                RaisePropertyChanged("NewSiteLoading");
            }
        }

        private volatile bool _newSiteViewVisible;
        public bool NewSiteViewVisible
        {
            get { return _newSiteViewVisible; }
            set
            {
                _newSiteViewVisible = value;
                RaisePropertyChanged("NewSiteViewVisible");
            }
        }
    }
}
