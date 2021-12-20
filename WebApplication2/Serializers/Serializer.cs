using JSM.PartialJsonObject;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Threading.Tasks;
using WebApplication2.Base;
using WebApplication2.Context;
using WebApplication2.DTO;
using WebApplication2.Models;

namespace WebApplication2.Serializers
{
    public interface ISerializer<TOrigin, TDestination, TContext> where TDestination : BaseEntity
                                                        where TOrigin : BaseDto
                                                        where TContext : DbContext
                
    {
        Task<List<TDestination>> List();
        //Task<List<TDestination>> List(Dictionary<string, string> keyPairValue);
        Task<List<TDestination>> List(IQueryable<TDestination> query);


        Task<PagedBaseResponse<TDestination>> List(int page, int pageSize, Expression<Func<TDestination, bool>> filter = null);
        Task<List<TResult>> List<TResult>(int page, int pageSize, Expression<Func<TDestination, TResult>> selector, Expression<Func<TDestination, bool>> filter = null);
        Task Save();
        Task Post(TOrigin origin);
        void Patch(PartialJsonObject<TOrigin> originObject);
        void Update(TOrigin originObject);
    }

    public class Serializer<TOrigin, TDestination, TContext> : ISerializer<TOrigin, TDestination, TContext> where TDestination : BaseEntity
                                                                                        where TOrigin : BaseDto
                                                                                        where TContext : DbContext
    {

        private readonly TContext _applicationDbContext;
        public Serializer(TContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;
        }

        public async Task Save()
        {
            await _applicationDbContext.SaveChangesAsync();
        }

        public void Patch(PartialJsonObject<TOrigin> originObject)
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
        }
                    
        public async Task<List<TDestination>> List()
        {
            return await _applicationDbContext.Set<TDestination>().ToListAsync();
        }

        public async Task<PagedBaseResponse<TDestination>> List(int page, int pageSize, Expression<Func<TDestination, bool>> filter = null)
        {
            if (pageSize < 1)
                throw new Exception("pageSize should be greater than 0");

            if (page < 1)
                throw new Exception("page should be greater than 0");

            var response = new PagedBaseResponse<TDestination>();
            var dbSet = _applicationDbContext.Set<TDestination>();
            var query = dbSet.AsQueryable();

            int totalRecords;

            if (filter != null)
                query = query.Where(filter).AsQueryable();

            totalRecords = filter != null ? dbSet.Count(filter) : dbSet.Count();

            var skip = page == 1 ? 0 : (page - 1) * pageSize;

            response.Data = await query.Skip(skip).Take(pageSize).ToListAsync();
            response.Pages = (int)Math.Ceiling((decimal)totalRecords / (decimal)pageSize);

            return response;
        }

        public async Task<List<TResult>> List<TResult>(int page, int pageSize, Expression<Func<TDestination, TResult>> selector, Expression<Func<TDestination, bool>> filter = null)
        {
            if (filter == null)
            {
                var query = _applicationDbContext.Set<TDestination>().Where(filter).Select(selector);

                if (page == 1)
                    return await query.Take(pageSize).ToListAsync();

                return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            }
            else
            {
                var query = _applicationDbContext.Set<TDestination>().Select(selector);

                if (page == 1)
                    return await query.Take(pageSize).ToListAsync();

                return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            }
        }

        private TDestination GetFromDB(Guid guid)
        {
            return _applicationDbContext.Set<TDestination>().FirstOrDefault(x => x.Id == guid);
        }

        public void Update(TOrigin originObject)
        {
            TDestination destinationObject = GetFromDB(originObject.Id);
            var stringDeserialized = JsonConvert.SerializeObject(originObject);
            JsonConvert.PopulateObject(stringDeserialized, destinationObject);
            _applicationDbContext.Update(destinationObject);
        }

        public async Task Post(TOrigin originObject)
        {
            var stringDeserialized = JsonConvert.SerializeObject(originObject);
            var destinationObject = JsonConvert.DeserializeObject<TDestination>(stringDeserialized);
            await _applicationDbContext.Set<TDestination>().AddAsync(destinationObject);
        }

        

        public async Task<List<TDestination>> List(Dictionary<string, string> keyPairValue)
        {
            var query = _applicationDbContext.Set<TDestination>().AsQueryable();

            var counter = 0;
            foreach(var dictEntry in keyPairValue)
            {
                query = query.Where($"{dictEntry.Key} = @{counter}", dictEntry.Value);
            }

            return await query.ToListAsync();
        }

        public async Task<List<TDestination>> List(IQueryable<TDestination> query)
        {
            return await query.ToListAsync();
        }
    }
}
