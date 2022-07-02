using JSM.PartialJsonObject;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNetCore.RestFramework.Core.Base;

namespace AspNetCore.RestFramework.Core.Serializer
{
    public class Serializer<TOrigin, TDestination, TPrimaryKey, TContext> where TDestination : BaseModel<TPrimaryKey>
                                                             where TOrigin : BaseDto
                                                             where TContext : DbContext
    {
        private readonly TContext _applicationDbContext;

        public IEnumerable<string> Errors { get; private set; }
        public Serializer(TContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;
        }

        public async virtual Task<(int Pages, List<TDestination> Data)> List(int page, int pageSize, IQueryable<TDestination> query)
        {

            if (pageSize < 1)
                throw new Exception("pageSize should be greater than 0");

            if (page < 1)
                throw new Exception("page should be greater than 0");


            int totalRecords = query.Count();

            int skip = page - 1;
            query = query.Skip(skip * pageSize).Take(pageSize);

            var data = await query.ToListAsync();
            var pages = (int)Math.Ceiling((decimal)totalRecords / (decimal)pageSize);
            
            return (pages, data);
        }

        public async Task<bool> Save<TPrimaryKey>(TOrigin data, OperationType operationType, TPrimaryKey objectId)
        {
            Errors = Validate(data, operationType);

            if (Errors.Any())
                return false;

            await Put(data, objectId);

            return true;
        }

        public async Task<bool> Save(TOrigin data, OperationType operationType)
        {
            Errors = Validate(data, operationType);

            if (Errors.Any())
                return false;

            await Post(data);

            return true;
        }

        /// <summary>
        /// Used for Patch
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<bool> Save(PartialJsonObject<TOrigin> data, object entityId)
        {
            Errors = Validate(data);

            if (Errors.Any())
                return false;

            await Patch(data, entityId);

            return true;
        }

        public virtual async Task Post(TOrigin originObject)
        {
            var stringDeserialized = JsonConvert.SerializeObject(originObject);
            var destinationObject = JsonConvert.DeserializeObject<TDestination>(stringDeserialized);
            await _applicationDbContext.Set<TDestination>().AddAsync(destinationObject);
            await _applicationDbContext.SaveChangesAsync();
        }

        public virtual async Task Patch<TPrimaryKey>(PartialJsonObject<TOrigin> originObject, TPrimaryKey entityId)
        {
            TDestination destinationObject = await GetFromDB(entityId);

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

            await _applicationDbContext.SaveChangesAsync();
        }

        public virtual async Task Put<TPrimaryKey>(TOrigin origin, TPrimaryKey entityId)
        {
            TDestination destinationObject = await GetFromDB(entityId);
            var stringDeserialized = JsonConvert.SerializeObject(origin);
            
            dynamic stringDeserializedDynamic = JsonConvert.DeserializeObject<dynamic>(stringDeserialized);
            stringDeserializedDynamic.Id = entityId;
            
            JsonConvert.PopulateObject(stringDeserializedDynamic.ToString(), destinationObject);
            _applicationDbContext.Update(destinationObject);
            await _applicationDbContext.SaveChangesAsync();
        }

        public virtual async  Task Delete<TPrimaryKey>(TPrimaryKey entityId)
        {
            var data = await GetFromDB(entityId);
            if (data == null)
                throw new Exception("Entity not found");

            _applicationDbContext.Remove(data);
            await _applicationDbContext.SaveChangesAsync();

        }

       // public virtual async Task<string> GetSingle<TPrimaryKey>(TPrimaryKey entityId, IQueryable<TDestination> query)
       // {
       //     var data = await GetFromDB(entityId, query);
       //     if (data == null)
       //         throw new Exception("Entity not found");
       //     
       //     return data;
       // }
        
        public virtual async Task<TDestination> GetSingle(IQueryable<TDestination> query)
        {
            var data = await GetFromDB(query);
            if (data == null)
                throw new Exception("Entity not found");

            return data;
        }

        private async Task<TDestination> GetFromDB<TPrimaryKey>(TPrimaryKey guid)
        {
            return await _applicationDbContext.Set<TDestination>().FindAsync(guid);
        }
        
        public async Task<TDestination> GetFromDB<TPrimaryKey>(TPrimaryKey guid, IQueryable<TDestination> query)
        {
            var key = guid.ToString();
            var data = await query.Where(x => x.Id.ToString() == key).FirstOrDefaultAsync();

            return data;
        }


        public virtual IEnumerable<string> Validate(TOrigin data, OperationType operation)
        {
            return data.Validate();
        }

        public virtual IEnumerable<string> Validate(PartialJsonObject<TOrigin> data)
        {
            return new List<string>();
        }
    }

    public enum OperationType
    {
        Create,
        Update
    }
    
    
}
