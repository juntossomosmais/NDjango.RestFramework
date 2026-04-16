using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NDjango.RestFramework.Base;
using NDjango.RestFramework.Helpers;
using Newtonsoft.Json;

namespace NDjango.RestFramework.Serializer
{
    public class Serializer<TOrigin, TDestination, TPrimaryKey, TContext>
        where TDestination : BaseModel<TPrimaryKey>
        where TOrigin : BaseDto<TPrimaryKey>
        where TContext : DbContext
    {
        /// <summary>
        /// The database context, available to subclasses for custom queries
        /// (e.g., uniqueness checks inside <see cref="ValidateAsync(TOrigin, IDictionary{string, List{string}})"/>).
        /// </summary>
        /// <remarks>
        /// Usage guidelines when called from <c>ValidateAsync</c>:
        /// <list type="bullet">
        /// <item>Always use <c>.AsNoTracking()</c> on read queries — validation should never track entities.</item>
        /// <item>Do NOT call <c>SaveChangesAsync()</c> inside <c>ValidateAsync</c>. The base CRUD method
        /// (<see cref="CreateAsync"/>, <see cref="UpdateAsync"/>, etc.) will save after validation succeeds.
        /// Calling <c>SaveChangesAsync()</c> during validation interacts badly with the change tracker
        /// and may persist entities prematurely.</item>
        /// <item>Do NOT attach, add, or remove entities during validation — only read.</item>
        /// </list>
        /// </remarks>
        protected readonly TContext _dbContext;

        public Serializer(TContext applicationDbContext)
        {
            _dbContext = applicationDbContext;
        }

        /// <summary>
        /// Validates and optionally mutates the incoming DTO before a create operation (POST) or
        /// a bulk update operation (PutMany). Override this method to add business rules that
        /// require async I/O (e.g., database uniqueness checks) or to normalize field values.
        /// Populate <paramref name="errors"/> to signal validation failures; the controller will
        /// short-circuit to 400 when the dictionary is non-empty.
        /// </summary>
        /// <remarks>
        /// This overload is invoked by <b>both POST and PutMany</b>. PutMany does not pass entity IDs
        /// because the same payload applies to many rows — so entity-specific checks (e.g., "uniqueness
        /// excluding all target entities") cannot be expressed here. If your PutMany needs per-entity
        /// validation context, override <see cref="UpdateManyAsync"/> and perform the checks there
        /// before the bulk update. For single-entity PUT, prefer the
        /// <see cref="ValidateAsync(TOrigin, TPrimaryKey, IDictionary{string, List{string}})"/> overload
        /// which receives the entity id.
        /// </remarks>
        /// <param name="data">The DTO to validate. Mutate properties directly to normalize values.</param>
        /// <param name="errors">
        /// Per-field error collector. Populate via <c>errors.GetOrAdd("Field").Add("message")</c>.
        /// <b>Not thread-safe</b> — write from the <c>ValidateAsync</c> method body only; do not share
        /// across parallel subtasks (e.g., <c>Task.WhenAll</c>) without external synchronization.
        /// </param>
        /// <returns>The (possibly mutated) DTO that will be forwarded to the CRUD operation.</returns>
        public virtual Task<TOrigin> ValidateAsync(
            TOrigin data,
            IDictionary<string, List<string>> errors)
            => Task.FromResult(data);

        /// <summary>
        /// Validates and optionally mutates the incoming DTO before a full update operation (PUT).
        /// The <paramref name="entityId"/> allows querying the existing entity for skip-self
        /// uniqueness checks (the DRF <c>self.instance</c> equivalent). By default delegates to
        /// the non-ID overload.
        /// </summary>
        /// <param name="data">The DTO to validate.</param>
        /// <param name="entityId">The primary key of the entity being updated.</param>
        /// <param name="errors">
        /// Per-field error collector. Populate via <c>errors.GetOrAdd("Field").Add("message")</c>.
        /// <b>Not thread-safe</b> — write from the <c>ValidateAsync</c> method body only; do not share
        /// across parallel subtasks (e.g., <c>Task.WhenAll</c>) without external synchronization.
        /// </param>
        /// <returns>The (possibly mutated) DTO.</returns>
        public virtual Task<TOrigin> ValidateAsync(
            TOrigin data,
            TPrimaryKey entityId,
            IDictionary<string, List<string>> errors)
            => ValidateAsync(data, errors);

        /// <summary>
        /// Validates and optionally mutates the incoming partial DTO before a partial update
        /// operation (PATCH). Use <c>partialData.IsSet(d =&gt; d.Field)</c> to check which fields
        /// were sent and <c>partialData.SetValue(d =&gt; d.Field, value)</c> to normalize values.
        /// </summary>
        /// <param name="partialData">The partial DTO wrapper.</param>
        /// <param name="entityId">The primary key of the entity being patched.</param>
        /// <param name="errors">
        /// Per-field error collector. Populate via <c>errors.GetOrAdd("Field").Add("message")</c>.
        /// <b>Not thread-safe</b> — write from the <c>ValidateAsync</c> method body only; do not share
        /// across parallel subtasks (e.g., <c>Task.WhenAll</c>) without external synchronization.
        /// </param>
        /// <returns>The (possibly mutated) partial DTO.</returns>
        public virtual Task<PartialJsonObject<TOrigin>> ValidateAsync(
            PartialJsonObject<TOrigin> partialData,
            TPrimaryKey entityId,
            IDictionary<string, List<string>> errors)
            => Task.FromResult(partialData);

        public virtual async Task<TDestination> CreateAsync(TOrigin data)
        {
            var stringDeserialized = JsonConvert.SerializeObject(data);
            var destinationObject = JsonConvert.DeserializeObject<TDestination>(stringDeserialized);

            await _dbContext.Set<TDestination>().AddAsync(destinationObject);
            await _dbContext.SaveChangesAsync();

            return destinationObject;
        }

        public virtual async Task<TDestination> PartialUpdateAsync(PartialJsonObject<TOrigin> originObject, TPrimaryKey entityId)
        {
            var destinationObject = await GetFromDB(entityId);

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

        public virtual async Task<TDestination> UpdateAsync(TOrigin origin, TPrimaryKey entityId)
        {
            var destinationObject = await GetFromDB(entityId);

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

        public virtual async Task<IList<TPrimaryKey>> UpdateManyAsync(TOrigin origin, IList<TPrimaryKey> entityIds)
        {
            var destinationObjects = await GetManyFromDB(entityIds);

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

        public virtual async Task<TDestination> DestroyAsync(TPrimaryKey entityId)
        {
            var data = await GetFromDB(entityId);

            if (data == null)
                return null;

            _dbContext.Remove(data);
            await _dbContext.SaveChangesAsync();

            return data;
        }

        public virtual async Task<IList<TPrimaryKey>> DestroyManyAsync(IList<TPrimaryKey> entityIds)
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
