﻿using Jasily.ComponentModel;
using JryVideo.Core;
using JryVideo.Editors.VideoEditor;
using JryVideo.Model;
using JryVideo.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using static System.String;

namespace JryVideo.Common
{
    public sealed class VideoInfoViewModel : HasCoverViewModel<JryVideoInfo>
    {
        private bool isTrackButtonEnable;
        private bool isUntrackButtonEnable;
        private string dayOfWeek;
        private bool isDone;
        private ViewModelCompareMode compareMode;
        private Playing todayPlaying;

        public VideoInfoViewModel(JrySeries series, JryVideoInfo source)
            : base(source)
        {
            this.SeriesView = new SeriesViewModel(series);
            this.RefreshProperties();
        }

        public SeriesViewModel SeriesView { get; }

        [NotifyPropertyChanged]
        public string YearWithIndex => $"({this.Source.Year}) {this.Source.Index}";

        [NotifyPropertyChanged]
        public string VideoNames => this.Source.Names.FirstOrDefault() ?? Empty;

        [NotifyPropertyChanged]
        public string VideoFullNames => this.Source.Names.Count == 0 ? null : this.Source.Names.AsLines();

        [NotifyPropertyChanged]
        public Playing TodayPlaying => this.todayPlaying;

        [NotifyPropertyChanged]
        public bool IsTodayPlayAndNotEnd => this.TodayPlaying?.Episode != null && !this.TodayPlaying.IsWatched;

        public string GroupTitle
        {
            get { return this.dayOfWeek; }
            private set { this.SetPropertyRef(ref this.dayOfWeek, value); }
        }

        public override void RefreshProperties()
        {
            this.IsTrackButtonEnable = !(this.IsUntrackButtonEnable = this.Source.IsTracking);
            this.UpdateGroup();
            base.RefreshProperties();
        }

        private async void UpdateGroup()
        {
            // only tracking need build group info.
            if (!this.Source.IsTracking) return;

            if (!this.Source.StartDate.HasValue)
            {
                this.SetCompareMode(ViewModelCompareMode.Unknown, $"{Resources.DayOfWeek_Unknown} (unknown start)");
                return;
            }

            this.todayPlaying = null;
            var today = DateTime.Now.Date;
            var sunday = today.AddDays(-(int)today.DayOfWeek); // sunday
            var startDate = this.Source.StartDate.Value.ToLocalTime();

            if (startDate < today)
            {
                if (this.Source.DayOfWeek == today.DayOfWeek)
                {
                    this.compareMode = ViewModelCompareMode.Today;
                    this.GroupTitle = $"{this.Source.DayOfWeek.GetLocalizeString()} ({Resources.DayOfWeek_Today})";
                    var episode = this.Source.GetTodayEpisode(today) + (this.Source.EpisodeOffset ?? 0);
                    this.isDone = episode > this.Source.EpisodesCount;
                    var playing = this.todayPlaying = new Playing(this.isDone ? (int?)null : episode);
                    if (playing.Episode.HasValue)
                    {
                        var isWatched = await Task.Run(async () =>
                        {
                            var manager = JryVideoCore.Current.CurrentDataCenter.VideoManager;
                            var video = await manager.FindAsync(this.Source.Id);
                            return video?.Watcheds?.Contains(playing.Episode.Value);
                        });

                        if (isWatched != null && playing.IsWatched != isWatched.Value)
                        {
                            playing.IsWatched = !playing.IsWatched;
                            //this.NotifyPropertyChanged(nameof(this.TodayPlaying));
                            this.NotifyPropertyChanged(nameof(this.IsTodayPlayAndNotEnd));
                        }
                    }
                }
                else
                {
                    this.SetCompareMode(ViewModelCompareMode.DayOfWeek,
                        this.Source.DayOfWeek.GetLocalizeString());
                }
            }
            else
            {
                if ((startDate - sunday).Days < 7) // this week
                {
                    this.SetCompareMode(ViewModelCompareMode.DayOfWeek,
                        this.Source.DayOfWeek.GetLocalizeString());
                }
                else
                {
                    var compared = startDate.DayOfYear - today.DayOfYear;
                    if (compared <= 7) // next week
                    {
                        this.SetCompareMode(ViewModelCompareMode.NextWeek,
                            string.Format(Resources.DateTime_Next, startDate.DayOfWeek.GetLocalizeString()));
                    }
                    else if (startDate.Month == today.Month) // in one month
                    {
                        var week = (compared / 7) + (compared % 7 == 0 ? 0 : 1);
                        this.SetCompareMode(ViewModelCompareMode.FewWeek,
                            string.Format(Resources.DateTime_AfterWeek, week));
                    }
                    else
                    {
                        if (startDate.Year == today.Year)
                        {
                            this.SetCompareMode(ViewModelCompareMode.FewMonth,
                                string.Format(Resources.DateTime_AfterMonth, startDate.Month - today.Month));
                        }
                        else
                        {
                            this.SetCompareMode(ViewModelCompareMode.Future, Resources.DateTime_Future);
                        }
                    }
                }
            }
        }

        private void SetCompareMode(ViewModelCompareMode mode, string groupTitle)
        {
            this.compareMode = mode;
            this.GroupTitle = groupTitle;
        }

        private enum ViewModelCompareMode
        {
            Today,

            DayOfWeek,

            NextWeek,

            FewWeek,

            FewMonth,

            FewYear,

            Future,

            Unknown
        }

        internal sealed class DayOfWeekComparer : Comparer<VideoInfoViewModel>
        {
            private readonly DayOfWeek DayOfWeek = DateTime.Now.DayOfWeek;

            public override int Compare(VideoInfoViewModel x, VideoInfoViewModel y)
            {
                Debug.Assert(x != null, "x != null");
                Debug.Assert(y != null, "y != null");

                if (x.compareMode != y.compareMode)
                    return x.compareMode.CompareTo(y.compareMode);

                var ret = this.CompareOnMode(x, y);
                return ret == 0 ? y.Source.Created.CompareTo(x.Source.Created) : ret;
            }

            private int CompareOnMode(VideoInfoViewModel x, VideoInfoViewModel y)
            {
                switch (x.compareMode)
                {
                    case ViewModelCompareMode.Today:
                        Debug.Assert(x.TodayPlaying != null, "x.TodayPlaying != null");
                        Debug.Assert(y.TodayPlaying != null, "y.TodayPlaying != null");
                        return x.TodayPlaying.CompareTo(y.TodayPlaying);

                    case ViewModelCompareMode.DayOfWeek:
                        if (x.Source.DayOfWeek == y.Source.DayOfWeek) return 0;
                        if (x.Source.DayOfWeek == null) return -1;
                        if (y.Source.DayOfWeek == null) return 1;

                        var sub1 = ((int)x.Source.DayOfWeek) - ((int)this.DayOfWeek);
                        var sub2 = ((int)y.Source.DayOfWeek) - ((int)this.DayOfWeek);

                        return sub1 * sub2 > 0 ? sub1 - sub2 : sub2 - sub1;

                    case ViewModelCompareMode.NextWeek:
                    case ViewModelCompareMode.FewWeek:
                    case ViewModelCompareMode.FewMonth:
                    case ViewModelCompareMode.FewYear:
                        Debug.Assert(x.Source.StartDate != null, "x.Source.StartDate != null");
                        Debug.Assert(y.Source.StartDate != null, "y.Source.StartDate != null");
                        if (y.Source.StartDate.Value == x.Source.StartDate.Value) return 0;
                        return DateTime.Compare(x.Source.StartDate.Value, y.Source.StartDate.Value);

                    case ViewModelCompareMode.Future:
                        if (x.Source.StartDate == y.Source.StartDate) return 0;
                        if (x.Source.StartDate == null) return -1;
                        if (y.Source.StartDate == null) return 1;
                        return x.Source.StartDate.Value.CompareTo(y.Source.StartDate.Value);
                }

                return 0;
            }
        }

        internal sealed class DefaultComparer : Comparer<VideoInfoViewModel>
        {
            public override int Compare(VideoInfoViewModel x, VideoInfoViewModel y)
            {
                Debug.Assert(x != null, "x != null");
                Debug.Assert(y != null, "y != null");

                if (x.SeriesView.DisplayNameFirstLine == "火星救援")
                {
                }

                if (x.SeriesView.Source.Id != y.SeriesView.Source.Id)
                {
                    return y.SeriesView.Source.Created.CompareTo(x.SeriesView.Source.Created);
                }

                // in same video: 1. date 1. year 1. type 1. index
                if (x.Source.StartDate.HasValue && y.Source.StartDate.HasValue)
                {
                    return y.Source.StartDate.Value.CompareTo(x.Source.StartDate.Value);
                }

                if (x.Source.Year != y.Source.Year)
                {
                    return y.Source.Year - x.Source.Year;
                }

                if (x.Source.Type != y.Source.Type)
                {
                    return CompareOrdinal(x.Source.Type, y.Source.Type);
                }

                if (x.Source.Index != y.Source.Index)
                {
                    return x.Source.Index - y.Source.Index;
                }

                return y.Source.Created.CompareTo(x.Source.Created);
            }
        }

        internal void NavigateToDouban()
        {
            var doubanId = this.Source.DoubanId;
            Task.Run(() =>
            {
                if (!doubanId.IsNullOrWhiteSpace())
                {
                    using (Process.Start("http://movie.douban.com/subject/" + doubanId))
                    {
                    }
                }
            });
        }

        internal void NavigateToImdb()
        {
            var imdnId = this.Source.ImdbId;
            Task.Run(() =>
            {
                if (!imdnId.IsNullOrWhiteSpace())
                {
                    using (Process.Start("http://www.imdb.com/title/" + imdnId))
                    {
                    }
                }
            });
        }

        protected override async Task<bool> TryAutoAddCoverAsync()
        {
            if (this.Source.DoubanId == null) return false;

            var coverManager = JryVideoCore.Current.CurrentDataCenter.CoverManager;

            var guid = await coverManager.GetCoverFromDoubanIdAsync(JryCoverType.Video, this.Source.DoubanId);

            if (guid == null) return false;

            this.Source.CoverId = guid;

            var manager = JryVideoCore.Current.CurrentDataCenter.SeriesManager.GetVideoInfoManager(this.SeriesView.Source);
            return await manager.UpdateAsync(this.Source);
        }

        public static IEnumerable<VideoInfoViewModel> Create(JrySeries series)
        {
            return series.Videos.Select(jryVideo => new VideoInfoViewModel(series, jryVideo));
        }

        public async Task<bool> TrackAsync()
        {
            this.IsTrackButtonEnable = this.IsUntrackButtonEnable = false;
            var manager = JryVideoCore.Current.CurrentDataCenter.SeriesManager.GetVideoInfoManager(this.SeriesView.Source);
            this.Source.IsTracking = true;

            if (await manager.UpdateAsync(this.Source))
            {
                this.IsUntrackButtonEnable = true;
                return true;
            }

            return false;
        }

        public async Task<bool> UntrackAsync()
        {
            this.IsTrackButtonEnable = this.IsUntrackButtonEnable = false;
            var manager = JryVideoCore.Current.CurrentDataCenter.SeriesManager.GetVideoInfoManager(this.SeriesView.Source);
            this.Source.IsTracking = false;

            if (await manager.UpdateAsync(this.Source))
            {
                this.IsTrackButtonEnable = true;
                return true;
            }

            return false;
        }

        public bool IsTrackButtonEnable
        {
            get { return this.isTrackButtonEnable; }
            set { this.SetPropertyRef(ref this.isTrackButtonEnable, value); }
        }

        public bool IsUntrackButtonEnable
        {
            get { return this.isUntrackButtonEnable; }
            set { this.SetPropertyRef(ref this.isUntrackButtonEnable, value); }
        }

        public sealed class Playing : NotifyPropertyChangedObject, IComparable<Playing>
        {
            private bool isWatched;
            public int? Episode { get; }

            public Playing(int? episode)
            {
                this.Episode = episode;
            }

            public string Text => this.Episode.HasValue ? $"today play {this.Episode.Value}" : "done!";

            public string WatchedText => $"watched ep {this.Episode}";

            public bool IsWatched
            {
                get { return this.isWatched; }
                set { this.SetPropertyRef(ref this.isWatched, value); }
            }

            public int CompareTo(Playing other)
                => this.GetOrderHash().CompareTo(other.GetOrderHash());

            private int GetOrderHash() => this.Episode.HasValue ? (this.IsWatched ? 2 : 0) : 1;
        }

        public async void Watch()
        {
            var ep = this.TodayPlaying?.Episode;
            if (ep == null) return;
            var manager = JryVideoCore.Current.CurrentDataCenter.VideoManager;
            var video = await manager.FindAsync(this.Source.Id);
            if (video == null) return;
            if (video.Watcheds == null) video.Watcheds = new List<int>();
            if (video.Watcheds.Contains(ep.Value)) return;
            video.Watcheds.Add(ep.Value);
            await manager.UpdateAsync(video);
            this.TodayPlaying.IsWatched = true;
            this.NotifyPropertyChanged(nameof(this.IsTodayPlayAndNotEnd));
        }

        public bool OpenEditorWindows(Window parent)
        {
            var dlg = new VideoEditorWindow(this.SeriesView.Source, this.Source)
            {
                Owner = parent
            };
            if (dlg.ShowDialog() == true)
            {
                this.RefreshProperties();
                return true;
            }
            return false;
        }
    }
}