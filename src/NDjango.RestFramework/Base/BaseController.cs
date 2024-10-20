using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NDjango.RestFramework.Errors;
using NDjango.RestFramework.Filters;
using NDjango.RestFramework.Helpers;
using NDjango.RestFramework.Paginations;
using NDjango.RestFramework.Serializer;

namespace NDjango.RestFramework.Base
{
    [Produces("application/json")]
    public abstract class BaseController<TOrigin, TDestination, TPrimaryKey, TContext> : ControllerBase
        where TOrigin : BaseDto<TPrimaryKey>
        where TDestination : BaseModel<TPrimaryKey>
        where TContext : DbContext
    {
        private readonly Serializer<TOrigin, TDestination, TPrimaryKey, TContext> _serializer;
        private readonly ILogger _logger;
        private readonly TContext _context;
        private readonly ActionOptions _actionOptions;
        private readonly IPagination<TDestination> _pagination;

        public IQueryable<TDestination> Query { get; set; }
        public List<Filter<TDestination>> Filters { get; set; } = new();
        public string[] AllowedFields { get; set; } = [];

        #region Constructors

        protected BaseController(
            Serializer<TOrigin, TDestination, TPrimaryKey, TContext> serializer,
            TContext context,
            ActionOptions actionOptions,
            ILogger<TDestination> logger,
            IPagination<TDestination>? pagination = null)
        {
            _serializer = serializer;
            _context = context;
            _actionOptions = actionOptions ?? new ActionOptions();
            _logger = logger;
            _pagination = pagination ?? new PageNumberPagination<TDestination>();
            Query = new FilterBuilder<TContext, TDestination>(_context).DbSet;
        }

        protected BaseController(
            Serializer<TOrigin, TDestination, TPrimaryKey, TContext> serializer,
            TContext context,
            ILogger<TDestination> logger,
            IPagination<TDestination>? pagination = null)
            : this(serializer, context, new ActionOptions(), logger, pagination)
        { }

        #endregion

        #region Actions

        /// <summary>
        /// Retrieves a single entity by its primary key.
        /// </summary>
        /// <param name="id">The primary key of the entity.</param>
        /// <returns>The entity if found, otherwise a NotFound result.</returns>
        [HttpGet]
        [Route("{id}")]
        public virtual async Task<IActionResult> GetSingle([FromRoute] TPrimaryKey id)
        {
            try
            {
                if (!TryGetFieldsFromModel(out string[] fieldsToBeRendered))
                    return BadRequest(new UnexpectedError(BaseMessages.ERROR_GET_FIELDS));

                var query = FilterQuery(GetQuerySet(), HttpContext.Request);

                var data = await _serializer.GetFromDB(id, query);
                if (data == null)
                    return NotFound();

                string json = JsonConvert.SerializeObject(
                    data,
                    new JsonSerializerSettings { ContractResolver = new JsonTransform(fieldsToBeRendered) }
                );

                var jObject = JObject.Parse(json);
                return Ok(jObject);
            }
            catch (Exception e)
            {
                _logger.LogError(e, BaseMessages.ERROR_MESSAGE);
                return BadRequest(new UnexpectedError(BaseMessages.ERROR_MESSAGE));
            }
        }

        [HttpGet]
        public async Task<IActionResult> ListPaged()
        {
            try
            {
                if (!TryGetFieldsFromModel(out string[] fieldsToBeRendered))
                    return BadRequest(new UnexpectedError(BaseMessages.ERROR_GET_FIELDS));

                var query = FilterQuery(GetQuerySet(), HttpContext.Request);
                query = SortQuery(AllowedFields, query);
                var paginated = await _pagination.PaginateAsync(query, HttpContext.Request);
                if (paginated == null)
                    return NotFound();

                var paginatedResultsAsJson = JsonConvert.SerializeObject(
                    paginated.Results,
                    new JsonSerializerSettings { ContractResolver = new JsonTransform(fieldsToBeRendered) }
                );

                var results = JArray.Parse(paginatedResultsAsJson);
                var paginatedToBeReturned =
                    new PaginatedResponse<JArray>(paginated.Count, paginated.Next, paginated.Previous, results);

                return Ok(paginatedToBeReturned);
            }
            catch (Exception e)
            {
                _logger.LogError(e, BaseMessages.ERROR_MESSAGE);
                return BadRequest(new UnexpectedError(BaseMessages.ERROR_MESSAGE));
            }
        }

        /// <summary>
        /// Creates a new entity.
        /// </summary>
        /// <param name="entity">The entity to create.</param>
        /// <returns>The created entity.</returns>
        [HttpPost]
        public virtual async Task<IActionResult> Post([FromBody] TOrigin entity)
        {
            try
            {
                if (!TryGetFieldsFromModel(out string[] fieldsToBeRendered))
                    return BadRequest(new UnexpectedError(BaseMessages.ERROR_GET_FIELDS));

                var data = await _serializer.PostAsync(entity);

                string json = JsonConvert.SerializeObject(
                    data,
                    new JsonSerializerSettings { ContractResolver = new JsonTransform(fieldsToBeRendered) }
                );

                var jObject = JObject.Parse(json);
                return CreatedAtAction(nameof(GetSingle), new { id = data.Id }, jObject);
            }
            catch (Exception e)
            {
                _logger.LogError(e, BaseMessages.ERROR_MESSAGE);
                return BadRequest(new UnexpectedError(BaseMessages.ERROR_MESSAGE));
            }
        }

        /// <summary>
        /// Partially updates an existing entity.
        /// </summary>
        /// <param name="entity">The partial entity data to update.</param>
        /// <param name="id">The primary key of the entity to update.</param>
        /// <returns>The updated entity if found, otherwise a NotFound result.</returns>
        [HttpPatch]
        [Route("{id}")]
        public virtual async Task<IActionResult> Patch(
            [FromBody] PartialJsonObject<TOrigin> entity,
            [FromRoute] TPrimaryKey id)
        {
            try
            {
                if (!_actionOptions.AllowPatch)
                    return StatusCode(StatusCodes.Status405MethodNotAllowed);

                if (!TryGetFieldsFromModel(out string[] fieldsToBeRendered))
                    return BadRequest(new UnexpectedError(BaseMessages.ERROR_GET_FIELDS));

                var data = await _serializer.PatchAsync(entity, id);

                if (data == null)
                    return NotFound();

                string json = JsonConvert.SerializeObject(
                    data,
                    new JsonSerializerSettings { ContractResolver = new JsonTransform(fieldsToBeRendered) }
                );

                var jObject = JObject.Parse(json);
                return Ok(jObject);
            }
            catch (Exception e)
            {
                _logger.LogError(e, BaseMessages.ERROR_MESSAGE);
                return BadRequest(new UnexpectedError(BaseMessages.ERROR_MESSAGE));
            }
        }

        /// <summary>
        /// Fully updates an existing entity.
        /// </summary>
        /// <param name="origin">The entity data to update.</param>
        /// <param name="id">The primary key of the entity to update.</param>
        /// <returns>The updated entity if found, otherwise a NotFound result.</returns>
        [HttpPut]
        [Route("{id}")]
        public virtual async Task<IActionResult> Put(
            [FromBody] TOrigin origin,
            [FromRoute] TPrimaryKey id)
        {
            try
            {
                if (!_actionOptions.AllowPut)
                    return StatusCode(StatusCodes.Status405MethodNotAllowed);

                if (!TryGetFieldsFromModel(out string[] fieldsToBeRendered))
                    return BadRequest(new UnexpectedError(BaseMessages.ERROR_GET_FIELDS));

                var data = await _serializer.PutAsync(origin, id);

                if (data == null)
                    return NotFound();

                string json = JsonConvert.SerializeObject(
                    data,
                    new JsonSerializerSettings { ContractResolver = new JsonTransform(fieldsToBeRendered) }
                );

                var jObject = JObject.Parse(json);
                return Ok(jObject);
            }
            catch (Exception e)
            {
                _logger.LogError(e, BaseMessages.ERROR_MESSAGE);
                return BadRequest(new UnexpectedError(BaseMessages.ERROR_MESSAGE));
            }
        }

        /// <summary>
        /// Fully updates multiple entities.
        /// </summary>
        /// <param name="origin">The entity data to update.</param>
        /// <param name="ids">The primary keys of the entities to update.</param>
        /// <returns>The list of updated entity IDs.</returns>
        [HttpPut]
        public virtual async Task<IActionResult> PutMany(
            [FromBody] TOrigin origin,
            [FromQuery] IList<TPrimaryKey> ids)
        {
            try
            {
                if (!_actionOptions.AllowPut)
                    return StatusCode(StatusCodes.Status405MethodNotAllowed);

                var updatedIds = await _serializer.PutManyAsync(origin, ids);

                return Ok(updatedIds);
            }
            catch (Exception e)
            {
                _logger.LogError(e, BaseMessages.ERROR_MESSAGE);
                return BadRequest(new UnexpectedError(BaseMessages.ERROR_MESSAGE));
            }
        }

        /// <summary>
        /// Deletes an entity by its primary key.
        /// </summary>
        /// <param name="id">The primary key of the entity to delete.</param>
        /// <returns>The deleted entity if found, otherwise a NotFound result.</returns>
        [HttpDelete]
        [Route("{id}")]
        public virtual async Task<IActionResult> Delete([FromRoute] TPrimaryKey id)
        {
            try
            {
                if (!TryGetFieldsFromModel(out string[] fieldsToBeRendered))
                    return BadRequest(new UnexpectedError(BaseMessages.ERROR_GET_FIELDS));

                var data = await _serializer.DeleteAsync(id);

                if (data == null)
                    return NotFound();

                string json = JsonConvert.SerializeObject(
                    data,
                    new JsonSerializerSettings { ContractResolver = new JsonTransform(fieldsToBeRendered) }
                );

                var jObject = JObject.Parse(json);
                return Ok(jObject);
            }
            catch (Exception e)
            {
                _logger.LogError(e, BaseMessages.ERROR_MESSAGE);
                return BadRequest(new UnexpectedError(BaseMessages.ERROR_MESSAGE));
            }
        }

        /// <summary>
        /// Deletes multiple entities by their primary keys.
        /// </summary>
        /// <param name="ids">The primary keys of the entities to delete.</param>
        /// <returns>The list of deleted entity IDs.</returns>
        [HttpDelete]
        public virtual async Task<IActionResult> DeleteMany([FromQuery] IList<TPrimaryKey> ids)
        {
            try
            {
                var deletedIds = await _serializer.DeleteManyAsync(ids);

                return Ok(deletedIds);
            }
            catch (Exception e)
            {
                _logger.LogError(e, BaseMessages.ERROR_MESSAGE);
                return BadRequest(new UnexpectedError(BaseMessages.ERROR_MESSAGE));
            }
        }

        #endregion

        #region Utils

        [NonAction]
        private static bool TryGetFieldsFromModel(out string[] fields)
        {
            try
            {
                var instanceMethod = typeof(TDestination).GetMethod("GetFields");
                fields = (string[])instanceMethod?.Invoke(Activator.CreateInstance(typeof(TDestination), null), null);
                return true;
            }
            catch
            {
                fields = null;
                return false;
            }
        }

        [NonAction]
        public virtual IQueryable<TDestination> FilterQuery(IQueryable<TDestination> query, HttpRequest request)
        {
            foreach (var filter in Filters)
                query = filter.AddFilter(query, request);
            return query;
        }

        [NonAction]
        public virtual IQueryable<TDestination> SortQuery(string[] allowedFilters, IQueryable<TDestination> query)
        {
            return new SortFilter<TDestination>().Sort(query, HttpContext.Request, allowedFilters);
        }

        [NonAction]
        public virtual IQueryable<TDestination> GetQuerySet()
        {
            return Query ?? new FilterBuilder<TContext, TDestination>(_context).DbSet;
        }

        #endregion
    }
}
