using System;
using System.Collections.Generic;

namespace Torshify.Radio.Framework
{
    public interface ITileService
    {
        #region Properties

        IEnumerable<Tile> Tiles
        {
            get;
        }

        #endregion Properties

        #region Methods

        void Create<T>(TileData tileData, params Tuple<string, string>[] parameters);

        #endregion Methods
    }
}