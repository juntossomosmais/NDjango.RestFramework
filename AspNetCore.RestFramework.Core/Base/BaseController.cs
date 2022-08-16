using JSM.PartialJsonObject;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNetCore.RestFramework.Core.Filters;
using AspNetCore.RestFramework.Core.Serializer;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AspNetCore.RestFramework.Core.Errors;

namespace AspNetCore.RestFramework.Core.Base
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

        public IQueryable<TDestination> Query { get; set; }
        public List<Filter<TDestination>> Filters { get; set; } = new List<Filter<TDestination>>();
        public string[] AllowedFields { get; set; } = Array.Empty<string>();

        #region .:: Constructors ::.

        protected BaseController(
            Serializer<TOrigin, TDestination, TPrimaryKey, TContext> serializer,
            TContext context,
            ActionOptions actionOptions,
            ILogger<TDestination> logger)
        {
            _serializer = serializer;
            _context = context;
            _actionOptions = actionOptions ?? new ActionOptions();
            _logger = logger;
            Query = new FilterBuilder<TContext, TDestination>(_context).DbSet;
        }

        protected BaseController(
            Serializer<TOrigin, TDestination, TPrimaryKey, TContext> serializer,
            TContext context,
            ILogger<TDestination> logger)
            : this(serializer, context, new ActionOptions(), logger)
        { }

        #endregion

        [HttpGet]
        [Route("{id}")]
        public virtual async Task<IActionResult> GetSingle([FromRoute] TPrimaryKey id)
        {
            try
            {
                if (!TryGetFieldsFromModel(out string[] listOfProps))
                    return BadRequest(new UnexpectedError(BaseMessages.ERROR_GET_FIELDS));

                var query = FilterQuery(GetQuerySet(), HttpContext.Request);

                var data = await _serializer.GetFromDB(id, query);
                if (data == null)
                    return NotFound();

                string json = JsonConvert.SerializeObject(
                    data,
                    new JsonSerializerSettings { ContractResolver = new JsonTransform(listOfProps) }
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
        public virtual async Task<IActionResult> ListPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 5)
        {
            try
            {
                if (!TryGetFieldsFromModel(out string[] listOfProps))
                    return BadRequest(new UnexpectedError(BaseMessages.ERROR_GET_FIELDS));

                var query = FilterQuery(GetQuerySet(), HttpContext.Request);
                query = SortQuery(AllowedFields, query);
                var (total, data) = await _serializer.ListAsync(page, pageSize, query);

                string json = JsonConvert.SerializeObject(
                    data,
                    new JsonSerializerSettings { ContractResolver = new JsonTransform(listOfProps) }
                );

                var jArray = JArray.Parse(json);

                var result = new PagedBaseResponse<JArray>()
                {
                    Data = jArray,
                    Total = total
                };

                return Ok(result);
            }
            catch (Exception e)
            {
                _logger.LogError(e, BaseMessages.ERROR_MESSAGE);
                return BadRequest(new UnexpectedError(BaseMessages.ERROR_MESSAGE));
            }
        }

        [HttpPost]
        public virtual async Task<IActionResult> Post([FromBody] TOrigin entity)
        {
            try
            {
                if (!TryGetFieldsFromModel(out string[] listOfProps))
                    return BadRequest(new UnexpectedError(BaseMessages.ERROR_GET_FIELDS));

                var data = await _serializer.PostAsync(entity);

                string json = JsonConvert.SerializeObject(
                    data,
                    new JsonSerializerSettings { ContractResolver = new JsonTransform(listOfProps) }
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

                if (!TryGetFieldsFromModel(out string[] listOfProps))
                    return BadRequest(new UnexpectedError(BaseMessages.ERROR_GET_FIELDS));

                var data = await _serializer.PatchAsync(entity, id);
                
                if (data == null)
                    return NotFound();

                string json = JsonConvert.SerializeObject(
                    data,
                    new JsonSerializerSettings { ContractResolver = new JsonTransform(listOfProps) }
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

                if (!TryGetFieldsFromModel(out string[] listOfProps))
                    return BadRequest(new UnexpectedError(BaseMessages.ERROR_GET_FIELDS));

                var data = await _serializer.PutAsync(origin, id);

                if (data == null)
                    return NotFound();

                string json = JsonConvert.SerializeObject(
                    data,
                    new JsonSerializerSettings { ContractResolver = new JsonTransform(listOfProps) }
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

        [HttpDelete]
        [Route("{id}")]
        public virtual async Task<IActionResult> Delete([FromRoute] TPrimaryKey id)
        {
            try
            {
                if (!TryGetFieldsFromModel(out string[] listOfProps))
                    return BadRequest(new UnexpectedError(BaseMessages.ERROR_GET_FIELDS));

                var data = await _serializer.DeleteAsync(id);

                if (data == null)
                    return NotFound();

                string json = JsonConvert.SerializeObject(
                    data,
                    new JsonSerializerSettings { ContractResolver = new JsonTransform(listOfProps) }
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

        #region Methods

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