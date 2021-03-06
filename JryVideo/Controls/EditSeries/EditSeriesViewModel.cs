﻿using JryVideo.Common;
using JryVideo.Core;
using JryVideo.Core.Douban;
using JryVideo.Core.Managers;
using JryVideo.Model;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace JryVideo.Controls.EditSeries
{
    public class EditSeriesViewModel : EditorItemViewModel<JrySeries>
    {
        private string names;
        private string doubanId;

        public EditSeriesViewModel()
        {
            this.UpdateNames();
        }

        public string Names
        {
            get { return this.names; }
            set { this.SetPropertyRef(ref this.names, value); }
        }

        public override void CreateMode()
        {
            base.CreateMode();
            this.UpdateNames();
        }

        public override void ModifyMode(JrySeries source)
        {
            base.ModifyMode(source);
            this.UpdateNames();
        }

        public void UpdateNames()
        {
            this.Names = this.Source == null ? "" : this.Source.Names.AsLines();
        }

        public string DoubanId
        {
            get { return this.doubanId; }
            set { this.SetPropertyRef(ref this.doubanId, value); }
        }

        public async Task LoadDoubanAsync()
        {
            if (String.IsNullOrWhiteSpace(this.DoubanId)) return;

            var movie = await DoubanHelper.TryGetMovieInfoAsync(this.DoubanId);

            if (movie != null)
            {
                var doubanName = DoubanMovieParser.Parse(movie).SeriesNames.AsLines();

                this.Names = String.IsNullOrWhiteSpace(this.Names)
                    ? doubanName
                    : String.Join("\r\n", this.Names, doubanName);
            }
        }

        public async Task<JrySeries> CommitAsync()
        {
            var series = this.GetCommitObject().ThrowIfNull("series");

            series.Names.Clear();

            if (!String.IsNullOrWhiteSpace(this.Names))
            {
                series.Names.AddRange(
                    this.Names.AsLines()
                        .Select(z => z.Trim())
                        .Where(z => !String.IsNullOrWhiteSpace(z)));
                series.Names = series.Names.Distinct().ToList();
            }

            SeriesManager.BuildSeriesMetaData(series);

            var seriesManager = JryVideoCore.Current.CurrentDataCenter.SeriesManager;

            return await this.CommitAsync(seriesManager, series);
        }

        public override void Clear()
        {
            this.Names = "";
            this.DoubanId = "";
        }
    }
}