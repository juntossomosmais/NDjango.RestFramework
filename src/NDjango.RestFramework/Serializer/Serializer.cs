using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NDjango.RestFramework.Base;
using NDjango.RestFramework.Helpers;

namespace NDjango.RestFramework.Serializer
{
    public class Serializer<TOrigin, TDestination, TPrimaryKey, TContext>
        where TDestination : BaseModel<TPrimaryKey>
        where TOrigin : BaseDto<TPrimaryKey>
        where TContext : DbContext
    {
        private readonly TContext _dbContext;

        public Serializer(TContext applicationDbContext)
        {
            _dbContext = applicationDbContext;
        }

        public virtual async Task<TDestination> PostAsync(TOrigin data)
        {
            var stringDeserialized = JsonConvert.SerializeObject(data);
            var destinationObject = JsonConvert.DeserializeObject<TDestination>(stringDeserialized);

            await _dbContext.Set<TDestination>().AddAsync(destinationObject);
            await _dbContext.SaveChangesAsync();

            return destinationObject;
        }

        public virtual async Task<TDestination> PatchAsync(PartialJsonObject<TOrigin> originObject, TPrimaryKey entityId)
        {
            TDestination destinationObject = await GetFromDB(entityId);

            if (destinationObject == null)
                return null;

            var destinationType = typeof(TDestination);

            foreach (var property in typeof(TOrigin).GetProperties())
            {
                if (property.PropertyType.IsEnum)
                    throw new NotImplementedException("Lists are not supported");

                if (originObject.IsSet(property.Name))
                {
                    var productProperty = destinationType.GetProperty(property.Name);
                    productProperty.SetValue(destinationObject, property.GetValue(originObject.Instance));
                }
            }

            await _dbContext.SaveChangesAsync();

            return destinationObject;
        }

        public virtual async Task<TDestination> PutAsync(TOrigin origin, TPrimaryKey entityId)
        {
            TDestination destinationObject = await GetFromDB(entityId);

            if (destinationObject == null)
                return null;

            var stringDeserialized = JsonConvert.SerializeObject(origin);

            dynamic stringDeserializedDynamic = JsonConvert.DeserializeObject<dynamic>(stringDeserialized);
            stringDeserializedDynamic.Id = entityId;

            JsonConvert.PopulateObject(stringDeserializedDynamic.ToString(), destinationObject);
            _dbContext.Update(destinationObject);
            await _dbContext.SaveChangesAsync();

            return destinationObject;
        }

        public virtual async Task<IList<TPrimaryKey>> PutManyAsync(TOrigin origin, IList<TPrimaryKey> entityIds)
        {
            IList<TDestination> destinationObjects = await GetManyFromDB(entityIds);

            var stringDeserialized = JsonConvert.SerializeObject(origin);
            dynamic stringDeserializedDynamic = JsonConvert.DeserializeObject<dynamic>(stringDeserialized);

            foreach (var obj in destinationObjects)
            {
                stringDeserializedDynamic.Id = obj.Id;
                JsonConvert.PopulateObject(stringDeserializedDynamic.ToString(), obj);
            }

            _dbContext.UpdateRange(destinationObjects);
            await _dbContext.SaveChangesAsync();

            return destinationObjects.Select(m => m.Id).ToList();
        }

        public virtual async Task<TDestination> DeleteAsync(TPrimaryKey entityId)
        {
            var data = await GetFromDB(entityId);

            if (data == null)
                return null;

            _dbContext.Remove(data);
            await _dbContext.SaveChangesAsync();

            return data;
        }

        public virtual async Task<IList<TPrimaryKey>> DeleteManyAsync(IList<TPrimaryKey> entityIds)
        {
            var deletedObjects = await GetManyFromDB(entityIds);

            _dbContext.RemoveRange(deletedObjects);
            await _dbContext.SaveChangesAsync();

            return deletedObjects.Select(m => m.Id).ToList();
        }

        public async Task<TDestination> GetFromDB(TPrimaryKey id, IQueryable<TDestination> query)
        {
            var key = id.ToString();
            var data = await query.Where(x => x.Id.ToString() == key).FirstOrDefaultAsync();

            return data;
        }

        protected async Task<TDestination> GetFromDB(TPrimaryKey id)
        {
            return await _dbContext.Set<TDestination>().FindAsync(id);
        }

        protected async Task<IList<TDestination>> GetManyFromDB(IList<TPrimaryKey> entityIds)
        {
            return await _dbContext.Set<TDestination>().Where(m => entityIds.Contains(m.Id)).ToListAsync();
        }
    }
}
