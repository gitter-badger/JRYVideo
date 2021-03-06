﻿using Jasily.ComponentModel;
using JryVideo.Common;
using JryVideo.Core;
using JryVideo.Model;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Enums;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace JryVideo.Editors.EntityEditor
{
    public class EntityEditorViewModel : EditorItemViewModel<JryEntity>
    {
        private string resolution;
        private string filmSource;
        private string audioSource;
        private string extension;
        private bool isWildcardChecked;
        private bool isRegexChecked;
        private string format;

        public EntityEditorViewModel()
        {
            this.Tags = new ObservableCollection<string>();
            this.Fansubs = new ObservableCollection<string>();
            this.SubTitleLanguages = new ObservableCollection<string>();
            this.TrackLanguages = new ObservableCollection<string>();
            this.isWildcardChecked = true;

            this.Resolutions = new List<string>();
            this.FilmSources = new List<string>();
            this.AudioSources = new List<string>();
            this.Extensions = new List<string>();
        }

        public Model.JryVideo Video { get; private set; }

        public void Initialize(Model.JryVideo video)
        {
            this.Video = video;
        }

        public ObservableCollection<string> this[JryFlagType flagType]
        {
            get
            {
                switch (flagType)
                {
                    case JryFlagType.EntityFansub:
                        return this.Fansubs;

                    case JryFlagType.EntitySubTitleLanguage:
                        return this.SubTitleLanguages;

                    case JryFlagType.EntityTrackLanguage:
                        return this.TrackLanguages;

                    case JryFlagType.EntityTag:
                        return this.Tags;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(flagType), flagType, null);
                }
            }
        }

        public ObservableCollection<string> Tags { get; private set; }

        public ObservableCollection<string> Fansubs { get; private set; }

        public ObservableCollection<string> SubTitleLanguages { get; private set; }

        public ObservableCollection<string> TrackLanguages { get; private set; }

        public List<string> Resolutions { get; private set; }

        public List<string> FilmSources { get; private set; }

        public List<string> AudioSources { get; private set; }

        public List<string> Extensions { get; private set; }

        public async Task LoadAsync()
        {
            var manager = JryVideoCore.Current.CurrentDataCenter.FlagManager;

            this.Resolutions.AddRange((await manager.LoadAsync(JryFlagType.EntityResolution)).Select(z => z.Value));
            this.FilmSources.AddRange((await manager.LoadAsync(JryFlagType.EntityFilmSource)).Select(z => z.Value));
            this.AudioSources.AddRange((await manager.LoadAsync(JryFlagType.EntityAudioSource)).Select(z => z.Value));
            this.Extensions.AddRange((await manager.LoadAsync(JryFlagType.EntityExtension)).Select(z => z.Value));
        }

        [EditableField]
        public string Resolution
        {
            get { return this.resolution; }
            set { this.SetPropertyRef(ref this.resolution, value.IsNullOrWhiteSpace() ? null : value.Trim()); }
        }

        [EditableField]
        public string FilmSource
        {
            get { return this.filmSource; }
            set { this.SetPropertyRef(ref this.filmSource, value.IsNullOrWhiteSpace() ? null : value.Trim()); }
        }

        [EditableField]
        public string AudioSource
        {
            get { return this.audioSource; }
            set { this.SetPropertyRef(ref this.audioSource, value.IsNullOrWhiteSpace() ? null : value.Trim()); }
        }

        [EditableField]
        public string Extension
        {
            get { return this.extension; }
            set { this.SetPropertyRef(ref this.extension, value.IsNullOrWhiteSpace() ? null : value.Trim()); }
        }

        public bool IsWildcardChecked
        {
            get { return this.isWildcardChecked; }
            set { this.SetPropertyRef(ref this.isWildcardChecked, value); }
        }

        public bool IsRegexChecked
        {
            get { return this.isRegexChecked; }
            set { this.SetPropertyRef(ref this.isRegexChecked, value); }
        }

        public string Format
        {
            get { return this.format; }
            set
            {
                this.SetPropertyRef(ref this.format, value);
                this.TryParseFromFormatString();
            }
        }

        public override void ReadFromObject(JryEntity obj)
        {
            base.ReadFromObject(obj);

            this.Tags.Clear();
            this.Tags.AddRange(obj.Tags);

            this.Fansubs.Clear();
            this.Fansubs.AddRange(obj.Fansubs);

            this.SubTitleLanguages.Clear();
            this.SubTitleLanguages.AddRange(obj.SubTitleLanguages);

            this.TrackLanguages.Clear();
            this.TrackLanguages.AddRange(obj.TrackLanguages);

            if (obj.Format != null)
            {
                switch (obj.Format.Type)
                {
                    case JryFormatType.Regex:
                        this.IsRegexChecked = true;
                        break;

                    case JryFormatType.Wildcard:
                        this.IsWildcardChecked = true;
                        break;
                }

                Debug.Assert(this.IsRegexChecked != this.IsWildcardChecked);

                this.Format = obj.Format.Value;
            }
        }

        public override void WriteToObject(JryEntity obj)
        {
            base.WriteToObject(obj);

            obj.Tags = this.Tags.Distinct().OrderBy(z => z).ToList();
            obj.Fansubs = this.Fansubs.Distinct().OrderBy(z => z).ToList();
            obj.SubTitleLanguages = this.SubTitleLanguages.Distinct().OrderBy(z => z).ToList();
            obj.TrackLanguages = this.TrackLanguages.Distinct().OrderBy(z => z).ToList();

            if (!this.Format.IsNullOrWhiteSpace())
            {
                Debug.Assert(this.IsRegexChecked != this.IsWildcardChecked);

                obj.Format = new JryFormat()
                {
                    Type = this.IsRegexChecked ? JryFormatType.Regex : JryFormatType.Wildcard,
                    Value = this.Format
                };
            }
        }

        public async Task<JryEntity> CommitAsync(MetroWindow window)
        {
            if (JryEntity.IsExtensionInvalid(this.Extension))
            {
                await window.ShowMessageAsync("error", "invalid extension.");
                return null;
            }

            if (JryEntity.IsResolutionInvalid(this.Resolution))
            {
                await window.ShowMessageAsync("error", "invalid resolution.");
                return null;
            }

            var entity = this.GetCommitObject();

            this.WriteToObject(entity);

            var provider = JryVideoCore.Current.CurrentDataCenter.VideoManager.GetEntityManager(this.Video);

            if (this.Action == ObjectChangedAction.Create)
            {
                entity.BuildMetaData();

                if (provider.IsExists(entity))
                {
                    await window.ShowMessageAsync("error", "had same entity.");
                    return null;
                }
            }

            return await base.CommitAsync(provider, entity);
        }

        public async void ParseFiles(string[] files)
        {
            if (files == null) throw new ArgumentNullException(nameof(files));

            files = files.Where(File.Exists).Select(Path.GetFileName).ToArray();
            if (files.Length == 0) return;
            this.Format = files.Length == 1 ? files[0] : await Task.Run(() => ParseCommonFileName(files));
        }

        private async void TryParseFromFormatString()
        {
            var format = this.Format;
            if (format.IsNullOrWhiteSpace() || this.Action != ObjectChangedAction.Create) return;

            var str = format.ToLower();
            if (this.Resolution.IsNullOrWhiteSpace())
            {
                this.Resolution = this.Resolutions.FirstOrDefault(z => str.Contains(z.Replace("P", "").ToLower())) ?? string.Empty;
            }
            if (this.FilmSource.IsNullOrWhiteSpace())
            {
                this.FilmSource = this.FilmSources.FirstOrDefault(z => str.Contains(z.ToLower())) ?? string.Empty;
            }
            if (this.AudioSource.IsNullOrWhiteSpace())
            {
                this.AudioSource = this.AudioSources.FirstOrDefault(z => str.Contains(z.ToLower())) ?? string.Empty;
            }
            if (this.Extension.IsNullOrWhiteSpace())
            {
                this.Extension = this.Extensions.FirstOrDefault(z => str.Contains(z.ToLower())) ?? string.Empty;
            }

            var mapper = ((App)Application.Current).UserConfig?.Mapper;
            if (mapper != null)
            {
                var filter = new Func<string, bool>(z => !z.IsNullOrWhiteSpace());
                this.Fansubs.Reset(this.Fansubs.ToArray()
                    .Concat(await mapper.TryFireAsync(JryFlagType.EntityFansub, format, filter))
                    .Distinct());
                this.SubTitleLanguages.Reset(this.SubTitleLanguages.ToArray()
                    .Concat(await mapper.TryFireAsync(JryFlagType.EntitySubTitleLanguage, format, filter))
                    .Distinct());
                this.TrackLanguages.Reset(this.TrackLanguages.ToArray()
                    .Concat(await mapper.TryFireAsync(JryFlagType.EntityTrackLanguage, format, filter))
                    .Distinct());
                this.Tags.Reset(this.Tags.ToArray()
                    .Concat(await mapper.TryFireAsync(JryFlagType.EntityTag, format, filter))
                    .Distinct());
            }
        }

        private static readonly Regex Crc32 = new Regex(@"(.*)(\[[a-f0-9]{7,8}\]|\([a-f0-9]{7,8}\))$", RegexOptions.IgnoreCase);

        private string ParseCommonFileName(string[] source)
        {
            if (source.Length == 0 || source.Length == 1) throw new ArgumentOutOfRangeException();

            var configExtensions = ((App)Application.Current).UserConfig?.SubTitleExtensions;
            if (configExtensions != null)
            {
                var es = configExtensions
                    .Select(z => (z.StartsWith(".") ? z : "." + z).ToLower())
                    .ToArray();
                var subTitles = new List<string>();
                source = source.Where(z =>
                {
                    if (es.Any(x => z.ToLower().EndsWith(x)))
                    {
                        subTitles.Add(Path.GetFileNameWithoutExtension(z));
                        return false;
                    }
                    return true;
                }).ToArray();

                var mapper = ((App)Application.Current).UserConfig?.Mapper;
                if (mapper != null && subTitles.Count > 0)
                {
                    var filter = new Func<string, bool>(z => !z.IsNullOrWhiteSpace());
                    var sttags = subTitles.Select(Path.GetExtension).Distinct().ToArray();
                    var langs = sttags
                        .SelectMany(z => mapper.TryFire(mapper.ExtendSubTitleLanguages, z, filter))
                        .Distinct()
                        .ToArray();
                    var fansubs = sttags
                        .SelectMany(z => mapper.TryFire(mapper.Fansubs, z, filter))
                        .Distinct()
                        .ToArray();
                    JasilyDispatcher.GetUIDispatcher().BeginInvoke(() =>
                    {
                        this.Fansubs.Reset(this.Fansubs.ToArray()
                           .Concat(fansubs)
                           .Distinct());
                        this.SubTitleLanguages.Reset(this.SubTitleLanguages.ToArray()
                            .Concat(langs)
                            .Distinct());
                    });
                }
            }

            var exts = source.Select(Path.GetExtension).ToArray();
            var ext = exts[0];
            if (exts.Skip(1).All(z => z == ext))
            {
                var names = source.Select(Path.GetFileNameWithoutExtension).ToArray();
                var matchs = names.Select(z => Crc32.Match(z)).ToArray();
                if (matchs.All(z => z.Success))
                {
                    var left = matchs[0].Groups[2].Value[0];
                    if (matchs.All(z => z.Groups[2].Value[0] == left))
                    {
                        var strsL = matchs.Select(z => z.Groups[1].Value).ToArray();
                        return ParseCommonString(strsL) +
                            matchs[0].Groups[2].Value[0] +
                            "*" +
                            matchs[0].Groups[2].Value.Last() +
                            ext;
                    }
                }
            }

            return ParseCommonString(source);
        }

        private static string ParseCommonString(string[] source)
            => source.CommonStart() + "*" + source.CommonEnd();
    }
}