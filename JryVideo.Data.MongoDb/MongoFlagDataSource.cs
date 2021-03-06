﻿using JryVideo.Data.DataSources;
using JryVideo.Model;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace JryVideo.Data.MongoDb
{
    public class MongoFlagDataSource : MongoJryEntitySet<JryFlag>, IFlagSet
    {
        public MongoFlagDataSource(JryVideoMongoDbDataEngine engine, IMongoCollection<JryFlag> collection)
            : base(engine, collection)
        {
        }

        public async Task<IEnumerable<JryFlag>> QueryAsync(JryFlagType type)
        {
            var filter = Builders<JryFlag>.Filter;

            return (await (await this.Collection.FindAsync(
                filter.Eq(t => t.Type, type)))
                .ToListAsync())
                .OrderByDescending(z => z.Count);
        }

        public async Task<bool> RefMathAsync(JryFlagType type, string value, int count)
        {
            if (count == 0) return true;

            var flagName = String.Format("<{0}>[{1}]", type, value);
            this.Log(JasilyLogger.LoggerMode.Debug, String.Format("calc flag {0} => {1}", flagName, count));

            var id = JryFlag.BuildCounterId(type, value);
            var filter = Builders<JryFlag>.Filter;
            var update = Builders<JryFlag>.Update;

            var flag = await this.Collection.FindOneAndUpdateAsync(
                filter.Eq(z => z.Id, id),
                update.Inc(z => z.Count, count),
                new FindOneAndUpdateOptions<JryFlag, JryFlag>() { IsUpsert = true });

            if (flag == null)
            {
                this.Log(JasilyLogger.LoggerMode.Debug, String.Format("new flag {0} was inserted.", flagName));
                if (count < 1)
                {
                    this.Log(JasilyLogger.LoggerMode.Debug, String.Format("flag {0} was less than 0, ignore.", flagName));
                    return true;
                }

                flag = await this.FindAsync(id);
                flag.Type = type;
                flag.Value = value;
                flag.Count = count;
                flag.BuildMetaData(true);
                this.Log(JasilyLogger.LoggerMode.Release, String.Format("flag {0} was update to {1}, ignore.", flagName, flag.Count));
                return await this.UpdateAsync(flag);
            }
            else
            {
                this.Log(JasilyLogger.LoggerMode.Debug, String.Format("flag {0} was exists.", flagName));

                if (flag.Count + count < 0)
                {
                    this.Log(JasilyLogger.LoggerMode.Debug, String.Format("flag {0} count less than 0, db error.", flagName));
                    await this.Collection.FindOneAndDeleteAsync(filter.Lt(z => z.Count, 0));
                }
                else if (flag.Count + count == 0)
                {
                    this.Log(JasilyLogger.LoggerMode.Debug, String.Format("flag {0} count was 0, removed.", flagName));
                    await this.Collection.FindOneAndDeleteAsync(filter.Lt(z => z.Count, 0));
                }
            }

            return true;
        }
    }
}