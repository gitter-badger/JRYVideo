﻿using Jasily.ComponentModel;
using JryVideo.Core.Managers;
using JryVideo.Model;
using System;
using System.Diagnostics;
using System.Enums;
using System.Threading.Tasks;

namespace JryVideo.Common
{
    public abstract class EditorItemViewModel<T> : JasilyEditableViewModel<T>
        where T : JryObject, new()
    {
        public event EventHandler<RequestActionEventArgs<T>> Creating;
        public event EventHandler<T> Created;
        public event EventHandler<T> Updated;

        public EditorItemViewModel()
            : base(null)
        {
        }

        public virtual void CreateMode()
        {
            this.Source = new T();
            this.Action = ObjectChangedAction.Create;
        }

        public virtual void ModifyMode(T source)
        {
            this.Source = source.ThrowIfNull("source");
            this.Action = ObjectChangedAction.Modify;

            this.ReadFromObject(source);
        }

        public virtual void CloneMode(T source)
        {
            this.Source = new T();
            this.Action = ObjectChangedAction.Create;

            this.ReadFromObject(source);
        }

        public virtual T GetCommitObject()
        {
            return this.Action == ObjectChangedAction.Create ? new T() : this.Source;
        }

        public ObjectChangedAction Action { get; private set; }

        /// <summary>
        /// please sure call obj.BuildMetaData() before call this.
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        protected async Task<T> CommitAsync(IObjectEditProvider<T> provider, T obj)
        {
            if (obj.HasError())
            {
                if (Debugger.IsAttached) Debugger.Break();
                return null;
            }

            switch (this.Action)
            {
                case ObjectChangedAction.Create:
                    var arg = new RequestActionEventArgs<T>(obj) { IsAccept = true };
                    this.Creating.Fire(this, arg);
                    if (arg.IsAccept && await provider.InsertAsync(obj))
                    {
                        this.Clear();
                        this.Created.BeginFire(this, obj);
                        return obj;
                    }
                    else
                    {
                        return null;
                    }

                case ObjectChangedAction.Modify:
                    if (await this.OnUpdateAsync(provider, obj))
                    {
                        this.Updated.BeginFire(this, obj);
                        return obj;
                    }
                    else
                    {
                        return null;
                    }

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected virtual async Task<bool> OnUpdateAsync(IObjectEditProvider<T> provider, T obj)
            => await provider.UpdateAsync(obj);

        public virtual void Clear()
        {

        }
    }
}