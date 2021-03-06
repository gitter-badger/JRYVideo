﻿using JryVideo.Data.DataSources;
using JryVideo.Model;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JryVideo.Data.MongoDb
{
    public class MongoSeriesDataSource : MongoJryEntitySet<JrySeries>, ISeriesSet
    {
        public MongoSeriesDataSource(JryVideoMongoDbDataEngine engine, IMongoCollection<JrySeries> collection)
            : base(engine, collection)
        {
        }

        public async Task<IEnumerable<JrySeries>> QueryAsync(SearchElement search, int skip, int take)
        {
            var builder = Builders<JrySeries>.Filter;
            FilterDefinition<JrySeries> filter;

            switch (search.Type)
            {
                case SearchElement.ElementType.Text:
                    filter = builder.Or(
                        builder.Regex(z => z.Names, new BsonRegularExpression(new Regex(Regex.Escape(search.Value), RegexOptions.IgnoreCase))),
                        builder.Regex("Videos.Names", new BsonRegularExpression(new Regex(Regex.Escape(search.Value), RegexOptions.IgnoreCase))));
                    break;

                case SearchElement.ElementType.SeriesId:
                    return (await this.FindAsync(search.Value)).IntoArray();

                case SearchElement.ElementType.VideoId:
                    filter = builder.Eq("Videos.Id", search.Value);
                    break;

                case SearchElement.ElementType.EntityId:
                    var it = await this.Engine.VideoCollection.FindAsync(Builders<Model.JryVideo>.Filter.Eq("Entities.Id", search.Value));
                    var en = (await it.ToListAsync()).FirstOrDefault();
                    if (en == null)
                    {
                        return Enumerable.Empty<JrySeries>();
                    }
                    else
                    {
                        filter = builder.Eq("Videos.Id", en.Id);
                    }
                    break;

                case SearchElement.ElementType.DoubanId:
                    filter = builder.Eq("Videos.DoubanId", search.Value);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            return await (await this.Collection.FindAsync(
                filter,
                options: new FindOptions<JrySeries, JrySeries>()
                {
                    Skip = skip,
                    Limit = take,
                    Sort = this.BuildDefaultSort()
                }))
                .ToListAsync();
        }

        protected override SortDefinition<JrySeries> BuildDefaultSort()
        {
            return Builders<JrySeries>.Sort.Descending("Videos.Created");
        }

        public async Task<IEnumerable<JrySeries>> ListTrackingAsync()
        {
            var builder = Builders<JrySeries>.Filter;

            var filter = builder.Eq("Videos.IsTracking", true);

            return await (await this.Collection.FindAsync(
                filter,
                options: new FindOptions<JrySeries, JrySeries>()
                {
                    Sort = this.BuildDefaultSort()
                }))
                .ToListAsync();
        }
    }
}