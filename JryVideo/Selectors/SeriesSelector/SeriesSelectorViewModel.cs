﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Jasily.Diagnostics;
using JryVideo.Common;
using JryVideo.Core;
using JryVideo.Selectors.Common;

namespace JryVideo.Selectors.SeriesSelector
{
    public sealed class SeriesSelectorViewModel : BaseSelectorViewModel<SeriesViewModel>
    {
        protected override bool OnFilter(SeriesViewModel obj)
        {
            if (String.IsNullOrWhiteSpace(this.FilterText)) return true;

            if (obj == null) return true;

            var keyword = this.FilterText.Trim().ToLower();

            return
                obj.Source.Names.Any(z => z.ToLower().Contains(keyword)) ||
                obj.Source.Videos.Any(z => z.DoubanId == keyword);
        }

        public async Task LoadAsync()
        {
            var seriesManager = JryVideoCore.Current.CurrentDataCenter.SeriesManager;

            JasilyDebug.Pointer();
            this.Items.Collection.AddRange(
                await Task.Run(async () =>
                    (await seriesManager.LoadAsync())
                    .Select(z => new SeriesViewModel(z))));
            JasilyDebug.Pointer();
        }
    }
}