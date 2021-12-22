using CSharpRestFramework.Base;
using JSM.PartialJsonObject;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CSharpRestFramework.Serializer
{
    public class Serializer<TOrigin, TDestination, TContext> where TDestination : class
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

        public async Task<bool> Save(TOrigin data, OperationType operationType)
        {
            Errors = Validate(data, operationType);

            if (Errors.Any())
                return false;

            if (operationType == OperationType.Create)
                await Post(data);

            else if (operationType == OperationType.Update)
                await Put(data);

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

        public virtual void Update(TOrigin originObject, object entityId)
        {
            TDestination destinationObject = GetFromDB(entityId);
            var stringDeserialized = JsonConvert.SerializeObject(originObject);
            JsonConvert.PopulateObject(stringDeserialized, destinationObject);
            _applicationDbContext.Update(destinationObject);
        }

        public virtual async Task Post(TOrigin originObject)
        {
            var stringDeserialized = JsonConvert.SerializeObject(originObject);
            var destinationObject = JsonConvert.DeserializeObject<TDestination>(stringDeserialized);
            await _applicationDbContext.Set<TDestination>().AddAsync(destinationObject);
            await _applicationDbContext.SaveChangesAsync();
        }

        public async virtual Task Patch(PartialJsonObject<TOrigin> originObject, object entityId)
        {
            TDestination destinationObject = GetFromDB(originObject.Instance.Id);

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

        public async virtual Task Put(TOrigin origin)
        {
            var stringDeserialized = JsonConvert.SerializeObject(origin);
            var destinationObject = JsonConvert.DeserializeObject<TDestination>(stringDeserialized);
            _applicationDbContext.Set<TDestination>().Update(destinationObject);
            await _applicationDbContext.SaveChangesAsync();
        }

        private TDestination GetFromDB(object guid)
        {
            return _applicationDbContext.Set<TDestination>().Find(guid);
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
