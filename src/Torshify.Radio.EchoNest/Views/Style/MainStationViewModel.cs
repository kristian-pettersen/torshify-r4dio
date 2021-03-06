﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Timers;

using EchoNest;
using EchoNest.Artist;
using EchoNest.Playlist;

using Microsoft.Practices.Prism.Logging;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Prism.ViewModel;

using Raven.Client;

using Torshify.Radio.EchoNest.Views.Style.Models;
using Torshify.Radio.Framework;
using Torshify.Radio.Framework.Commands;
using Torshify.Radio.Framework.Common;

namespace Torshify.Radio.EchoNest.Views.Style
{
    [Export]
    [RegionMemberLifetime(KeepAlive = false)]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class MainStationViewModel : NotificationObject, INavigationAware
    {
        #region Fields

        private readonly IDocumentStore _documentStore;
        private readonly ILoadingIndicatorService _loadingIndicatorService;
        private readonly ILoggerFacade _logger;
        private readonly IRadio _radio;
        private readonly IToastService _toastService;

        private Range _artistFamiliarity;
        private Range _artistHotness;
        private Range _danceability;
        private Range _energy;
        private Timer _fetchPreviewTimer;
        private Range _loudness;
        private string _metricsText;
        private ObservableCollection<TermModel> _moods;
        private List<ArtistBucketItem> _previewArtistList;
        private TaskScheduler _scheduler;
        private ObservableCollection<TermModel> _selectedMoods;
        private ObservableCollection<TermModel> _selectedStyles;
        private Range _songHotness;
        private ObservableCollection<TermModel> _styles;
        private Range _tempo;

        #endregion Fields

        #region Constructors

        [ImportingConstructor]
        public MainStationViewModel(
            ILoadingIndicatorService loadingIndicatorService,
            IRadio radio,
            IToastService toastService,
            ILoggerFacade logger,
            IDocumentStore documentStore)
        {
            _loadingIndicatorService = loadingIndicatorService;
            _radio = radio;
            _toastService = toastService;
            _logger = logger;
            _documentStore = documentStore;
            _fetchPreviewTimer = new Timer(1000);
            _fetchPreviewTimer.Elapsed += FetchPreviewTimerTick;
            _scheduler = TaskScheduler.FromCurrentSynchronizationContext();
            _styles = new ObservableCollection<TermModel>();
            _moods = new ObservableCollection<TermModel>();
            _selectedMoods = new ObservableCollection<TermModel>();
            _selectedMoods.CollectionChanged += (sender, args) =>
            {
                RaisePropertyChanged("SelectedMoodsText");
                _fetchPreviewTimer.Stop();
                _fetchPreviewTimer.Start();
            };

            _selectedStyles = new ObservableCollection<TermModel>();
            _selectedStyles.CollectionChanged += (sender, args) =>
            {
                RaisePropertyChanged("SelectedStylesText");
                _fetchPreviewTimer.Stop();
                _fetchPreviewTimer.Start();
            };

            ToggleStyleCommand = new StaticCommand<TermModel>(ExecuteToggleStyle);
            ToggleMoodCommand = new StaticCommand<TermModel>(ExecuteToggleMood);
            StartRadioCommand = new AutomaticCommand(ExecuteStartRadio, CanExecuteStartRadio);
            IncreaseBoostCommand = new StaticCommand<TermModel>(ExecuteIncreaseBoost);
            DecreaseBoostCommand = new StaticCommand<TermModel>(ExecuteDecreaseBoost);
            RequireTermCommand = new StaticCommand<TermModel>(ExecuteRequireTerm);
            BanTermCommand = new StaticCommand<TermModel>(ExecuteBanTerm);
            Tempo = new Range();
            Tempo.Rounding = MidpointRounding.ToEven;
            Tempo.RangeChanged += MetricChanged;
            Loudness = new Range();
            Loudness.RangeChanged += MetricChanged;
            Energy = new Range();
            Energy.RangeChanged += MetricChanged;
            ArtistFamiliarity = new Range();
            ArtistFamiliarity.RangeChanged += MetricChanged;
            ArtistHotness = new Range();
            ArtistHotness.RangeChanged += MetricChanged;
            SongHotness = new Range();
            SongHotness.RangeChanged += MetricChanged;
            Danceability = new Range();
            Danceability.RangeChanged += MetricChanged;
        }

        #endregion Constructors

        #region Properties

        public StaticCommand<TermModel> BanTermCommand
        {
            get; private set;
        }

        public StaticCommand<TermModel> RequireTermCommand
        {
            get; private set;
        }

        public AutomaticCommand StartRadioCommand
        {
            get;
            private set;
        }

        public StaticCommand<TermModel> ToggleStyleCommand
        {
            get;
            private set;
        }

        public StaticCommand<TermModel> ToggleMoodCommand
        {
            get;
            private set;
        }

        public StaticCommand<TermModel> IncreaseBoostCommand
        {
            get; private set;
        }

        public StaticCommand<TermModel> DecreaseBoostCommand
        {
            get; private set;
        }

        public IEnumerable<TermModel> Moods
        {
            get
            {
                return _moods;
            }
        }

        public IEnumerable<TermModel> Styles
        {
            get
            {
                return _styles;
            }
        }

        public IEnumerable<TermModel> SelectedMoods
        {
            get
            {
                return _selectedMoods;
            }
        }

        public IEnumerable<TermModel> SelectedStyles
        {
            get
            {
                return _selectedStyles;
            }
        }

        public string SelectedStylesText
        {
            get
            {
                if (_selectedStyles.Count > 0)
                {
                    return "(" + string.Join(", ", EnumerableExtensions.Select(_selectedStyles, s => s.Name)) + ")";
                }

                return string.Empty;
            }
        }

        public string SelectedMoodsText
        {
            get
            {
                if (_selectedMoods.Count > 0)
                {
                    return "(" + string.Join(", ", EnumerableExtensions.Select(_selectedMoods, s => s.Name)) + ")";
                }

                return string.Empty;
            }
        }

        public Range Tempo
        {
            get
            {
                return _tempo;
            }
            set
            {
                if (_tempo != value)
                {
                    _tempo = value;
                    RaisePropertyChanged("Tempo");
                }
            }
        }

        public Range Loudness
        {
            get
            {
                return _loudness;
            }
            set
            {
                if (_loudness != value)
                {
                    _loudness = value;
                    RaisePropertyChanged("Loudness");
                }
            }
        }

        public Range Danceability
        {
            get
            {
                return _danceability;
            }
            set
            {
                if (_danceability != value)
                {
                    _danceability = value;
                    RaisePropertyChanged("Danceability");
                }
            }
        }

        public Range Energy
        {
            get
            {
                return _energy;
            }
            set
            {
                if (_energy != value)
                {
                    _energy = value;
                    RaisePropertyChanged("Energy");
                }
            }
        }

        public Range ArtistFamiliarity
        {
            get
            {
                return _artistFamiliarity;
            }
            set
            {
                if (_artistFamiliarity != value)
                {
                    _artistFamiliarity = value;
                    RaisePropertyChanged("ArtistFamiliarity");
                }
            }
        }

        public Range ArtistHotness
        {
            get
            {
                return _artistHotness;
            }
            set
            {
                if (_artistHotness != value)
                {
                    _artistHotness = value;
                    RaisePropertyChanged("ArtistHotness");
                }
            }
        }

        public Range SongHotness
        {
            get
            {
                return _songHotness;
            }
            set
            {
                if (_songHotness != value)
                {
                    _songHotness = value;
                    RaisePropertyChanged("SongHotness");
                }
            }
        }

        public string MetricsText
        {
            get
            {
                return _metricsText;
            }
            set
            {
                if (_metricsText != value)
                {
                    _metricsText = value;
                    RaisePropertyChanged("MetricsText");
                }
            }
        }

        public List<ArtistBucketItem> PreviewArtistList
        {
            get
            {
                return _previewArtistList;
            }
            set
            {
                _previewArtistList = value;
                RaisePropertyChanged("PreviewArtistList");
            }
        }

        #endregion Properties

        #region Methods

        void INavigationAware.OnNavigatedTo(NavigationContext navigationContext)
        {
            InitializeStyles()
                .ContinueWith(task =>
                {
                    string styleString = navigationContext.Parameters["Styles"];

                    if (!string.IsNullOrEmpty(styleString))
                    {
                        string[] styles = styleString.Split(new[] { ',' });

                        foreach (var style in styles)
                        {
                            var term = Styles.FirstOrDefault(s => s.Name.Equals(WebUtility.HtmlDecode(style), StringComparison.InvariantCultureIgnoreCase));

                            if (term != null)
                            {
                                ToggleStyleCommand.Execute(term);
                            }
                        }
                    }
                });

            InitializeMoods()
                .ContinueWith(task =>
                {
                    string moodString = navigationContext.Parameters["Moods"];

                    if (!string.IsNullOrEmpty(moodString))
                    {
                        string[] moods = moodString.Split(new[] { ',' });

                        foreach (var mood in moods)
                        {
                            var term = Moods.FirstOrDefault(s => s.Name.Equals(WebUtility.HtmlDecode(mood), StringComparison.InvariantCultureIgnoreCase));

                            if (term != null)
                            {
                                ToggleMoodCommand.Execute(term);
                            }
                        }
                    }
                });

            Tempo.Minimum = GetParameterValue("Tempo", navigationContext.Parameters);
            Loudness.Minimum = GetParameterValue("Loudness", navigationContext.Parameters);
            Danceability.Minimum = GetParameterValue("Danceability", navigationContext.Parameters);
            Energy.Minimum = GetParameterValue("Energy", navigationContext.Parameters);
            ArtistHotness.Minimum = GetParameterValue("ArtistHotness", navigationContext.Parameters);
            ArtistFamiliarity.Minimum = GetParameterValue("ArtistFamiliarity", navigationContext.Parameters);
            SongHotness.Minimum = GetParameterValue("SongHotness", navigationContext.Parameters);
        }

        bool INavigationAware.IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        void INavigationAware.OnNavigatedFrom(NavigationContext navigationContext)
        {
            Microsoft.Practices.Prism.UriQuery q = new Microsoft.Practices.Prism.UriQuery();

            var selectedMoods = SelectedMoods.Select(t => WebUtility.HtmlEncode(t.Name)).ToArray();
            if (selectedMoods.Any())
            {
                q.Add("Moods", string.Join(",", selectedMoods));
            }

            var selectedStyles = SelectedStyles.Select(t => WebUtility.HtmlEncode(t.Name)).ToArray();
            if (selectedStyles.Any())
            {
                q.Add("Styles", string.Join(",", selectedStyles));
            }

            SetParameterValue("Tempo", q, Tempo.Minimum);
            SetParameterValue("Loudness", q, Loudness.Minimum);
            SetParameterValue("Danceability", q, Danceability.Minimum);
            SetParameterValue("Energy", q, Energy.Minimum);
            SetParameterValue("ArtistHotness", q, ArtistHotness.Minimum);
            SetParameterValue("ArtistFamiliarity", q, ArtistFamiliarity.Minimum);
            SetParameterValue("SongHotness", q, SongHotness.Minimum);

            var newUri = typeof(MainStationView).FullName + q;
            navigationContext.NavigationService.Journal.CurrentEntry.Uri = new Uri(newUri, UriKind.RelativeOrAbsolute);
        }

        private void ExecuteBanTerm(TermModel model)
        {
            model.Require = false;
            model.Ban = true;
            model.Boost = null;
        }

        private void ExecuteRequireTerm(TermModel model)
        {
            model.Require = true;
            model.Ban = false;
            model.Boost = null;
        }

        private void ExecuteDecreaseBoost(TermModel model)
        {
            if (!model.Boost.HasValue)
            {
                model.Boost = 1.0;
            }

            model.Ban = false;
            model.Require = false;

            if (DoubleUtilities.GreaterThan(model.Boost.GetValueOrDefault(), 0.0))
            {
                model.Boost -= 0.5;
            }
        }

        private void ExecuteIncreaseBoost(TermModel model)
        {
            if (!model.Boost.HasValue)
            {
                model.Boost = 1.0;
            }

            model.Ban = false;
            model.Require = false;

            if (DoubleUtilities.LessThan(model.Boost.GetValueOrDefault(), 5.0))
            {
                model.Boost += 0.5;
            }
        }

        private double? GetParameterValue(string parameterName, Microsoft.Practices.Prism.UriQuery query)
        {
            if (!string.IsNullOrEmpty(query[parameterName]))
            {
                double value;
                if (double.TryParse(query[parameterName], NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                {
                    return value;
                }
            }

            return null;
        }

        private void SetParameterValue(string parameterName, Microsoft.Practices.Prism.UriQuery query, double? value)
        {
            if (value.HasValue)
            {
                query.Add(parameterName, value.Value.ToString(CultureInfo.InvariantCulture));
            }
        }

        private void MetricChanged(object sender, EventArgs e)
        {
            string text = string.Empty;

            if (Tempo.Minimum.HasValue)
            {
                text += "tempo (" + (int)Tempo.Minimum.Value + "BPM) ";
            }

            if (Loudness.Minimum.HasValue)
            {
                text += "loudness (" + (int)Loudness.Minimum.Value + "dB) ";
            }

            if (Energy.Minimum.HasValue)
            {
                text += string.Format("energy ({0:0.0}) ", Energy.Minimum.Value);
            }

            if (Danceability.Minimum.HasValue)
            {
                text += string.Format("danceability ({0:0.0}) ", Danceability.Minimum.Value);
            }

            if (ArtistFamiliarity.Minimum.HasValue)
            {
                text += string.Format("familiarity ({0:0.0}) ", ArtistFamiliarity.Minimum.Value);
            }

            if (ArtistHotness.Minimum.HasValue)
            {
                text += string.Format("artist hotness ({0:0.0}) ", ArtistHotness.Minimum.Value);
            }

            if (SongHotness.Minimum.HasValue)
            {
                text += string.Format("song hotness ({0:0.0}) ", SongHotness.Minimum.Value);
            }

            MetricsText = text;

            _fetchPreviewTimer.Stop();
            _fetchPreviewTimer.Start();
        }

        private bool CanExecuteStartRadio()
        {
            return true;
        }

        private void ExecuteStartRadio()
        {
            Task.Factory.StartNew(() =>
            {
                SelectedMoods.ForEach(s => s.Count = s.Count + 1);
                SelectedStyles.ForEach(s => s.Count = s.Count + 1);

                using (var session = _documentStore.OpenSession())
                {
                    foreach (var selectedStyle in SelectedStyles)
                    {
                        var styleTerm = session.Load<StyleTerm>(selectedStyle.ID);

                        if (styleTerm != null)
                        {
                            styleTerm.Count = selectedStyle.Count;
                            session.Store(styleTerm);
                        }
                    }

                    foreach (var selectedMood in SelectedMoods)
                    {
                        var styleTerm = session.Load<MoodTerm>(selectedMood.ID);

                        if (styleTerm != null)
                        {
                            styleTerm.Count = selectedMood.Count;
                            session.Store(styleTerm);
                        }
                    }

                    session.SaveChanges();
                }
            })
            .ContinueWith(task =>
            {
                if (task.Exception != null)
                {
                    _logger.Log(task.Exception.ToString(), Category.Exception, Priority.Medium);
                }
            });

            Task.Factory.StartNew(() =>
            {
                using (_loadingIndicatorService.EnterLoadingBlock())
                {
                    using (var session = new EchoNestSession(EchoNestModule.ApiKey))
                    {
                        SearchArgument arg = new SearchArgument();
                        FillTermList(SelectedMoods, arg.Moods);
                        FillTermList(SelectedStyles, arg.Styles);
                        arg.MinFamiliarity = ArtistFamiliarity.Minimum;
                        arg.MinHotttnesss = ArtistHotness.Minimum;

                        var response = session.Query<Search>().Execute(arg);

                        if (response == null)
                        {
                            _toastService.Show("Unable to generate playlist");
                            return;
                        }

                        if (response.Status.Code == ResponseCode.Success && response.Artists.Count > 0)
                        {
                            StaticArgument arg2 = new StaticArgument();
                            arg2.Results = 75;
                            FillTermList(SelectedMoods, arg2.Moods);
                            FillTermList(SelectedStyles, arg2.Styles);
                            arg2.Type = "artist-radio";
                            arg2.Artist.Add(response.Artists[0].Name);
                            arg2.MinTempo = Tempo.Minimum;
                            arg2.MinLoudness = Loudness.Minimum;
                            arg2.MinDanceability = Danceability.Minimum;
                            arg2.MinEnergy = Energy.Minimum;
                            arg2.ArtistMinFamiliarity = ArtistFamiliarity.Minimum;
                            arg2.ArtistMinHotttnesss = ArtistHotness.Minimum;
                            arg2.SongMinHotttnesss = SongHotness.Minimum;

                            _radio.Play(new StyleTrackStream(arg2, _radio, _toastService));
                        }
                        else
                        {
                            if (response.Artists != null && response.Artists.Count == 0)
                            {
                                // TODO : Localize
                                _toastService.Show("Unable to find songs matching the current criterias");
                            }
                            else
                            {
                                _toastService.Show("EchoNest : " + response.Status.Message);
                            }
                        }
                    }
                }
            })
            .ContinueWith(task =>
            {
                if (task.Exception != null)
                {
                    _logger.Log(task.Exception.ToString(), Category.Exception, Priority.Medium);
                }
            });
        }

        private void ExecuteToggleMood(TermModel term)
        {
            if (_selectedMoods.Contains(term))
            {
                term.IsSelected = false;
                _selectedMoods.Remove(term);
            }
            else
            {
                term.IsSelected = true;
                _selectedMoods.Add(term);
            }
        }

        private void ExecuteToggleStyle(TermModel term)
        {
            if (_selectedStyles.Contains(term))
            {
                term.IsSelected = false;
                _selectedStyles.Remove(term);
            }
            else
            {
                term.IsSelected = true;
                _selectedStyles.Add(term);
            }
        }

        private Task InitializeMoods()
        {
            return Task.Factory
                .StartNew(() =>
                {
                    var moods = new List<MoodTerm>();
                    IEnumerable<MoodTerm> result;
                    using (var session = _documentStore.OpenSession())
                    {
                        result = session
                            .Query<MoodTerm>()
                            .ToArray();
                    }

                    if (!result.Any())
                    {
                        var allMoods = GetItems(ListTermsType.Mood);
                        using (var session = _documentStore.OpenSession())
                        {
                            int i = 0;
                            foreach (var listTermsItem in allMoods)
                            {
                                MoodTerm term = new MoodTerm();
                                term.Name = listTermsItem.Name;
                                term.Index = i;
                                moods.Add(term);
                                session.Store(term);

                                i++;
                            }

                            session.SaveChanges();
                        }
                    }
                    else
                    {
                        moods.AddRange(result.OrderBy(m => m.Id));
                    }

                    return moods;
                })
                .ContinueWith(task =>
                {
                    if (task.Exception != null)
                    {
                        return new TermModel[0];
                    }

                    // Load stuff from db, such as number of times its been used etc
                    return EnumerableExtensions.Select(task.Result, t => new TermModel(t.Id, WebUtility.HtmlDecode(t.Name), ListTermsType.Mood)
                    {
                        Count = t.Count
                    });
                })
                .ContinueWith(task =>
                {
                    foreach (var termModel in task.Result)
                    {
                        termModel.PropertyChanged += TermModelOnPropertyChanged;
                        _moods.Add(termModel);
                    }
                }, _scheduler);
        }

        private Task InitializeStyles()
        {
            return Task.Factory
                .StartNew(() =>
                {
                    var styles = new List<StyleTerm>();
                    IEnumerable<StyleTerm> result;
                    using (var session = _documentStore.OpenSession())
                    {
                        result = session
                            .Query<StyleTerm>()
                            .ToArray();
                    }

                    if (!result.Any())
                    {
                        var allStyles = GetItems(ListTermsType.Style);
                        using (var session = _documentStore.OpenSession())
                        {
                            int i = 0;
                            foreach (var listTermsItem in allStyles)
                            {
                                StyleTerm term = new StyleTerm();
                                term.Name = listTermsItem.Name;
                                term.Index = i;
                                styles.Add(term);
                                session.Store(term);

                                i++;
                            }

                            string[] additionalStyles = new[]
                            {
                                "dubstep",
                                "british metal",
                                "doom metal",
                                "industrial metal",
                                "metalcore",
                                "post-rock",
                                "post-metal"
                            };

                            foreach (var additionalStyle in additionalStyles)
                            {
                                var term = new StyleTerm
                                {
                                    Name = additionalStyle,
                                    Index = i
                                };

                                styles.Add(term);
                                session.Store(term);

                                i++;
                            }

                            session.SaveChanges();
                        }
                    }
                    else
                    {
                        styles.AddRange(result.OrderBy(s => s.Index));
                    }

                    return styles.OrderBy(s => s.Name);
                })
                .ContinueWith(task =>
                {
                    if (task.Exception != null)
                    {
                        return new TermModel[0];
                    }

                    // Load stuff from db, such as number of times its been used etc
                    return EnumerableExtensions.Select(task.Result, t => new TermModel(t.Id, WebUtility.HtmlDecode(t.Name), ListTermsType.Style)
                    {
                        Count = t.Count
                    });
                })
                .ContinueWith(task =>
                {
                    foreach (var termModel in task.Result)
                    {
                        termModel.PropertyChanged += TermModelOnPropertyChanged;
                        _styles.Add(termModel);
                    }

                }, _scheduler);
        }

        private IEnumerable<ListTermsItem> GetItems(ListTermsType type)
        {
            using (_loadingIndicatorService.EnterLoadingBlock())
            {
                using (var session = new EchoNestSession(EchoNestModule.ApiKey))
                {
                    var response = session.Query<ListTerms>().Execute(type);

                    if (response.Status.Code == ResponseCode.Success)
                    {
                        return response.Terms;
                    }
                }

                return new ListTermsItem[0];
            }
        }

        private void TermModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Boost" 
                || e.PropertyName == "Ban"
                || e.PropertyName == "Require")
            {
                _fetchPreviewTimer.Stop();
                _fetchPreviewTimer.Start();
            }
        }

        private void FetchPreviewTimerTick(object sender, ElapsedEventArgs e)
        {
            _fetchPreviewTimer.Stop();

            Task.Factory
                .StartNew(FetchPreview)
                .ContinueWith(task =>
                {
                    if (task.Exception != null)
                    {
                        _logger.Log(task.Exception.ToString(), Category.Exception, Priority.Medium);
                    }
                });
        }

        private void FetchPreview()
        {
            using (var session = new EchoNestSession(EchoNestModule.ApiKey))
            {
                SearchArgument arg = new SearchArgument();
                FillTermList(SelectedMoods, arg.Moods);
                FillTermList(SelectedStyles, arg.Styles);
                arg.MinFamiliarity = ArtistFamiliarity.Minimum;
                arg.MinHotttnesss = ArtistHotness.Minimum;

                SearchResponse response = session.Query<Search>().Execute(arg);

                if (response != null && response.Status.Code == ResponseCode.Success)
                {
                    PreviewArtistList = response.Artists;
                }
                else
                {
                    PreviewArtistList = null;
                }
            }
        }

        private void FillTermList(IEnumerable<TermModel> source, TermList target)
        {
            source.ForEach(s =>
            {
                var term = target.Add(s.Name);

                if (s.Boost.HasValue)
                {
                    term.Boost(s.Boost.Value);
                }

                if (s.Require)
                {
                    term.Require();
                }

                if (s.Ban)
                {
                    term.Ban();
                }
            });
        }

        #endregion Methods
    }
}