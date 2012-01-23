﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Prism.ViewModel;

using Torshify.Radio.Framework;
using Torshify.Radio.Framework.Commands;

namespace Torshify.Radio.Core.Views
{
    [Export(typeof(MainViewModel))]
    public class MainViewModel : NotificationObject, INavigationAware
    {
        #region Fields

        private ObservableCollection<string> _autoCompleteList;

        #endregion Fields

        #region Constructors

        public MainViewModel()
        {
            _autoCompleteList = new ObservableCollection<string>();

            NavigateBackCommand = new AutomaticCommand(ExecuteNavigateBack, CanExecuteNavigateBack);
            NavigateForwardCommand = new AutomaticCommand(ExecuteNavigateForward, CanExecuteNavigateForward);
            SearchCommand = new AutomaticCommand<string>(ExecuteSearch, CanExecuteSearch);
        }

        #endregion Constructors

        #region Properties

        public IEnumerable<string> AutoCompleteList
        {
            get
            {
                return _autoCompleteList;
            }
        }

        public AutomaticCommand NavigateBackCommand
        {
            get;
            private set;
        }

        public AutomaticCommand NavigateForwardCommand
        {
            get;
            private set;
        }

        [Import]
        public IRegionManager RegionManager
        {
            get;
            set;
        }

        [Import]
        public ISearchBarService SearchBarService
        {
            get;
            set;
        }

        public AutomaticCommand<string> SearchCommand
        {
            get;
            private set;
        }

        #endregion Properties

        #region Methods

        bool INavigationAware.IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        void INavigationAware.OnNavigatedFrom(NavigationContext navigationContext)
        {
            SearchBarService.CurrentChanged -= SearchBarServiceOnCurrentChanged;
        }

        void INavigationAware.OnNavigatedTo(NavigationContext navigationContext)
        {
            SearchBarService.CurrentChanged += SearchBarServiceOnCurrentChanged;
        }

        public void UpdateAutoCompleteList(string text)
        {
            var ui = TaskScheduler.FromCurrentSynchronizationContext();
            Task<IEnumerable<string>>
                .Factory
                .StartNew(() =>
                {
                    try
                    {
                        return SearchBarService.Current.Data.AutoCompleteProvider(text);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    return new string[0];
                })
                .ContinueWith(t =>
                {
                    _autoCompleteList.Clear();

                    foreach (var phrase in t.Result)
                    {
                        _autoCompleteList.Add(phrase);
                    }
                }, ui);
        }

        private bool CanExecuteNavigateBack()
        {
            return RegionManager.Regions[AppRegions.ViewRegion].NavigationService.Journal.CanGoBack;
        }

        private bool CanExecuteNavigateForward()
        {
            return RegionManager.Regions[AppRegions.ViewRegion].NavigationService.Journal.CanGoForward;
        }

        private bool CanExecuteSearch(string phrase)
        {
            return !string.IsNullOrEmpty(phrase);
        }

        private void ExecuteNavigateBack()
        {
            RegionManager.Regions[AppRegions.ViewRegion].NavigationService.Journal.GoBack();
        }

        private void ExecuteNavigateForward()
        {
            RegionManager.Regions[AppRegions.ViewRegion].NavigationService.Journal.GoForward();
        }

        private void ExecuteSearch(string phrase)
        {
            RegionManager.RequestNavigate(AppRegions.ViewRegion, SearchBarService.Current.NavigationUri);
        }

        private void SearchBarServiceOnCurrentChanged(object sender, EventArgs eventArgs)
        {
        }

        #endregion Methods
    }
}