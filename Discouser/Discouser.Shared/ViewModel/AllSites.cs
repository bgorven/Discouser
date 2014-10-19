using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Discouser.ViewModel
{
    class AllSites : IDisposable
    {
        public ObservableCollection<Site> Sites { get; private set; }

        private const string loginSettingsKey = "logins";
        private const string guidSettingsKey = "Guid";
        public List<Tuple<string,string>> logins;
        private Guid LocalGuid;

        public AllSites()
        {
            var roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings.Values;
            logins = roamingSettings[loginSettingsKey] as List<Tuple<string, string>> ?? new List<Tuple<string, string>>();

            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings.Values;
            if (!(localSettings[guidSettingsKey] is Guid))
            {
                localSettings[guidSettingsKey] = Guid.NewGuid();
            }
            LocalGuid = (Guid)localSettings[guidSettingsKey];

            Sites = new ObservableCollection<Site>(logins.Select(l => new Site(new DataContext(l.Item1, l.Item2, (Guid)LocalGuid))));
        }

        public void AddSite(string url, string username)
        {
            AddSite(Tuple.Create(url, username));
        }

        private void AddSite(Tuple<string, string> login)
        {
            if (!logins.Contains(login))
            {
                logins.Add(login);
                Sites.Add(new Site(new DataContext(login.Item1, login.Item2, LocalGuid)));
                Windows.Storage.ApplicationData.Current.RoamingSettings.Values[loginSettingsKey] = logins;
            }
        }

        /// <summary>
        /// Removes a site from the list of sites, and disposes its data context.
        /// </summary>
        /// <param name="siteToRemove"></param>
        public void RemoveSite(Site siteToRemove)
        {
            Sites.Remove(siteToRemove);
            logins.RemoveAll(login => login.Item1 == siteToRemove.Url && login.Item2 == siteToRemove.Username);
            siteToRemove.Context.Dispose();
        }

        public void Dispose()
        {
            foreach (var site in Sites)
            {
                site.Context.Dispose();
            }
        }
    }
}
