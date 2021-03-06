using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using EchoNest;
using EchoNest.Playlist;

using Torshify.Radio.Framework;

namespace Torshify.Radio.EchoNest.Views.LoveHate
{
    public class LoveHateTrackStream : ITrackStream
    {
        #region Fields

        private readonly string _initialArtistName;
        private readonly IRadio _radio;
        private readonly IToastService _toastService;

        private IEnumerable<Track> _currentTracks;
        private string _sessionId;
        private bool? _likesCurrentTrack;
        private double? _currentTrackRating;

        #endregion Fields

        #region Constructors

        public LoveHateTrackStream(
            string initialArtistName,
            IRadio radio,
            IToastService toastService)
        {
            _currentTracks = new Track[0];
            _initialArtistName = initialArtistName;
            _radio = radio;
            _toastService = toastService;
        }

        #endregion Constructors

        #region Properties

        public IEnumerable<Track> Current
        {
            get
            {
                return _currentTracks;
            }
        }

        public bool SupportsTrackSkipping
        {
            get
            {
                return true;
            }
        }

        public string Description
        {
            get
            {
                return "Love/hate";
            }
        }

        public TrackStreamData Data
        {
            get
            {
                return new LoveHateTrackStreamData
                {
                    InitialArtist = _initialArtistName,
                    Name = _initialArtistName,
                    Description = "Love/hate radio",
                    Source = "Love/hate"
                };
            }
        }

        #endregion Properties

        #region Methods

        public void Dispose()
        {
        }

        public bool MoveNext(CancellationToken token)
        {
            if (string.IsNullOrEmpty(_sessionId))
            {
                using (var session = new EchoNestSession(EchoNestModule.ApiKey))
                {
                    var argument = new DynamicArgument();
                    argument.Type = "artist-radio";
                    argument.Artist.Add(_initialArtistName);
                    argument.Dmca = true;

                    var response = session.Query<Dynamic>().Execute(argument);

                    if (response.Status.Code == ResponseCode.Success)
                    {
                        _sessionId = response.SessionId;

                        var song = response
                            .Songs
                            .FirstOrDefault(s => s.ArtistName.Equals(_initialArtistName, StringComparison.InvariantCultureIgnoreCase));

                        if (song == null)
                        {
                            song = response.Songs.FirstOrDefault();
                        }

                        if (song != null)
                        {
                            if (token.IsCancellationRequested)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var queryResult = _radio.GetTracksByName(_initialArtistName + " " + song.Title);

                            if (!queryResult.Any())
                            {
                                queryResult = _radio.GetTracksByName(_initialArtistName);
                            }

                            _currentTracks =
                                queryResult
                                    .Where(s => s.Artist.Equals(_initialArtistName, StringComparison.InvariantCultureIgnoreCase))
                                    .Take(1)
                                    .ToArray();

                            if (!_currentTracks.Any())
                            {
                                _toastService.Show("Unable to find any tracks matching the query");
                                return false;
                            }

                            return true;
                        }
                    }
                    else
                    {
                        _toastService.Show(response.Status.Message);
                    }
                }
            }
            else
            {
                using (var session = new EchoNestSession(EchoNestModule.ApiKey))
                {
                    if (token.IsCancellationRequested)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    var argument = new DynamicArgument
                    {
                        SessionId = _sessionId
                    };

                    if (_likesCurrentTrack.HasValue)
                    {
                        if (!_likesCurrentTrack.Value)
                        {
                            argument.Ban = "artist";
                        }
                    }

                    if (_currentTrackRating.HasValue)
                    {
                        argument.Rating = Convert.ToInt32(_currentTrackRating.GetValueOrDefault(1));
                    }

                    var response = session.Query<Dynamic>().Execute(argument);

                    _likesCurrentTrack = null;
                    _currentTrackRating = null;

                    if (response.Status.Code == ResponseCode.Success)
                    {
                        var song = response.Songs.FirstOrDefault();

                        if (song != null)
                        {
                            var queryResult = _radio.GetTracksByName(song.ArtistName + " " + song.Title);

                            if (!queryResult.Any())
                            {
                                queryResult = _radio.GetTracksByName(song.ArtistName);
                            }

                            _currentTracks =
                                queryResult
                                    .Where(s => s.Artist.Equals(song.ArtistName, StringComparison.InvariantCultureIgnoreCase))
                                    .Take(1)
                                    .ToArray();

                            if (!_currentTracks.Any())
                            {
                                _toastService.Show("Unable to find any tracks matching the query");

                                if (response.Songs.Any())
                                {
                                    return MoveNext(token);
                                }

                                return false;
                            }

                            return true;
                        }
                    }
                    else
                    {
                        _toastService.Show(response.Status.Message);
                    }
                }
            }

            return false;
        }

        public void Reset()
        {
            _sessionId = null;
            _currentTracks = new Track[0];
        }

        public void DislikeCurrentTrack()
        {
            _likesCurrentTrack = false;
        }

        public void LikeCurrentTrack()
        {
            _likesCurrentTrack = true;
        }

        public void SetCurrentTrackRating(double rating)
        {
            _currentTrackRating = rating;
        }

        public bool TryGetSessionInfo(out SessionInfoResponse response)
        {
            if (string.IsNullOrEmpty(_sessionId))
            {
                response = null;
                return false;
            }

            using (var session = new EchoNestSession(EchoNestModule.ApiKey))
            {
                response = session.Query<SessionInfo>().Execute(new SessionInfoArgument
                {
                    SessionId = _sessionId
                });

                if (response.Status.Code == ResponseCode.Success)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion Methods
    }
}