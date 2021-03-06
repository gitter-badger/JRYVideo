using JryVideo.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace JryVideo.Core.Managers
{
    public class JryObjectManager<T, TProvider> : IObjectEditProvider<T>
        where T : JryObject
        where TProvider : IJasilyEntitySetProvider<T, string>
    {
        protected JryObjectManager(TProvider source)
        {
            this.Source = source;
        }

        protected TProvider Source { get; private set; }

        public async Task<IEnumerable<T>> LoadAsync()
        {
            return await this.Source.ListAsync(0, Int32.MaxValue);
        }

        public async Task<IEnumerable<T>> LoadAsync(int skip, int take)
        {
            return await this.Source.ListAsync(skip, take);
        }

        public async Task<T> FindAsync(string id)
        {
            return await this.Source.FindAsync(id);
        }

        public virtual async Task<bool> InsertAsync(T obj)
        {
            if (obj.HasError()) return false;

            return await this.Source.InsertAsync(obj);
        }

        protected virtual async Task<bool> InsertAsync(IEnumerable<T> objs)
        {
            if (objs.Any(obj => obj.HasError())) return false;

            return await this.Source.InsertAsync(objs);
        }

        public virtual async Task<bool> UpdateAsync(T obj)
        {
            if (obj.HasError()) return false;

            return await this.Source.UpdateAsync(obj);
        }

        public async Task<bool> RemoveAsync(string id)
        {
            return await this.Source.RemoveAsync(id);
        }
    }
}