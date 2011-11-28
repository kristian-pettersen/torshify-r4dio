﻿using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using EchoNest;
using EchoNest.Artist;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.ViewModel;
using Torshify.Radio.Framework;

namespace Torshify.Radio.EchoNest.Style
{
    public class StyleRadioStationViewModel : NotificationObject
    {
        #region Fields

        private readonly IRadioStationContext _context;
        private readonly IRadio _radio;

        private IEnumerable<TermModel> _currentTermList;

        #endregion Fields

        #region Constructors

        public StyleRadioStationViewModel(IRadio radio, IRadioStationContext context)
        {
            AvailableTerms = new ObservableCollection<TermModel>();
            CreatePlaylistCommand = new DelegateCommand<IEnumerable>(ExecuteCreatePlaylist);

            _radio = radio;
            _context = context;

            var ui = TaskScheduler.FromCurrentSynchronizationContext();
            Task<IEnumerable<TermModel>>.Factory
                .StartNew(GetAvailableStyles)
                .ContinueWith(t =>
                {
                    AvailableTerms.Clear();

                    foreach (var termModel in t.Result)
                    {
                        AvailableTerms.Add(termModel);
                    }
                }, ui);
        }

        #endregion Constructors

        #region Properties

        public ObservableCollection<TermModel> AvailableTerms
        {
            get;
            private set;
        }

        public ICommand CreatePlaylistCommand
        {
            get;
            private set;
        }

        #endregion Properties

        #region Methods

        private void ExecuteCreatePlaylist(IEnumerable moods)
        {
            _currentTermList = moods.Cast<TermModel>();

            var termEnumerator = new StylesToArtistEnumerator();
            termEnumerator.Initialize(_currentTermList, _radio);

            _context
                .SetTrackProvider(termEnumerator.DoIt).
                ContinueWith(t => _context.GoToTracks(), TaskScheduler.FromCurrentSynchronizationContext());
        }

        private IEnumerable<TermModel> GetAvailableStyles()
        {
            using (EchoNestSession session = new EchoNestSession(EchoNestConstants.ApiKey))
            {
                var response = session
                    .Query<ListTerms>()
                    .Execute();

                if (response.Status.Code == ResponseCode.Success)
                {
                    return response.Terms.Select(t => new TermModel {Name = t.Name});
                }
            }

            return new TermModel[0];
        }

        #endregion Methods
    }
}