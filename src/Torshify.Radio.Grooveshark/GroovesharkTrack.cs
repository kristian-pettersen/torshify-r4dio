﻿using System.Globalization;
using Torshify.Radio.Framework;

namespace Torshify.Radio.Grooveshark
{
    public class GroovesharkTrack : Track
    {
        #region Constructors

        public GroovesharkTrack(int songID, int artistID)
        {
            SongID = songID;
            ArtistID = artistID;
        }

        #endregion Constructors

        #region Properties

        public int SongID
        {
            get;
            private set;
        }

        public int ArtistID
        {
            get;
            private set;
        }

        #endregion Properties

        #region Methods

        public override TrackLink ToLink()
        {
            TrackLink link = new TrackLink("grooveshark");
            link["SongID"] = SongID.ToString(CultureInfo.InvariantCulture);
            link["ArtistID"] = ArtistID.ToString(CultureInfo.InvariantCulture);
            return link;
        }

        #endregion Methods
    }
}