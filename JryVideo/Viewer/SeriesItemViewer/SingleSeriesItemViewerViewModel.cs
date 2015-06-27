using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using JryVideo.Common;
using JryVideo.Core;
using JryVideo.Model;

namespace JryVideo.Viewer.SeriesItemViewer
{
    public class SingleSeriesItemViewerViewModel : SeriesItemViewerViewModel
    {
        private JrySeries source;

        public SingleSeriesItemViewerViewModel(JrySeries series)
        {
            this.source = series;
        }

        public JrySeries Source
        {
            get { return this.source; }
            set { this.SetPropertyRef(ref this.source, value); }
        }

        public async override Task LoadAsync()
        {
            var series = this.Source;
            Debug.Assert(series != null);
            this.VideosObservableCollection.AddRange(await Task.Run(() => VideoViewModel.Create(series).ToArray()));
        }
    }
}