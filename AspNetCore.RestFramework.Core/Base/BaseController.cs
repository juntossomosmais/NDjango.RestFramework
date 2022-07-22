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

namespace AspNetCore.RestFramework.Core.Base
{
    [Produces("application/json")]
    public class BaseController<TOrigin, TDestination, TPrimaryKey, TContext> : ControllerBase
        where TOrigin : BaseDto<TPrimaryKey>
        where TDestination : BaseModel<TPrimaryKey>
        where TContext : DbContext

    {
        private readonly Serializer<TOrigin, TDestination, TPrimaryKey, TContext> _serializer;
        public IQueryable<TDestination> Query { get; set; }
        private TContext _context { get; set; }
        private ActionOptions _actionOptions { get; set; }
        public List<Filter<TDestination>> Filters { get; set; } = new List<Filter<TDestination>>();
        public string[] AllowedFields { get; set; } = Array.Empty<string>();
        
        private readonly ILogger _logger;

        #region .:: Constructors ::.

        public BaseController(Serializer<TOrigin, TDestination, TPrimaryKey, TContext> serializer, TContext context,
            ActionOptions actionOptions, ILogger<TDestination> logger)
        {
            _serializer = serializer;
            _actionOptions = actionOptions ?? new ActionOptions();
            _context = context;
            Query = new FilterBuilder<TContext, TDestination>(_context).DbSet;
            _logger = logger;
        }

        public BaseController(Serializer<TOrigin, TDestination, TPrimaryKey, TContext> serializer, TContext context, ILogger<TDestination> logger)
        {
            _serializer = serializer;
            _actionOptions = new ActionOptions();
            _context = context;
            Query = new FilterBuilder<TContext, TDestination>(_context).DbSet;
            _logger = logger;
        }

        #endregion


        [HttpGet]
        [Route("{Id}")]
        public virtual async Task<IActionResult> GetSingle(TPrimaryKey Id)
        {
            try
            {
                var listOfProps = GetFieldsFromModel();

                if (listOfProps == null || listOfProps.Length == 0)
                    return BadRequest(BaseMessages.ERROR_GET_FIELDS);

                var query = FilterQuery(GetQuerySet(), HttpContext.Request);

                var data = await _serializer.GetFromDB(Id, query);
                if (data == null)
                    return NotFound(BaseMessages.NOT_FOUND);

                string json = JsonConvert.SerializeObject(data,
                    new JsonSerializerSettings {ContractResolver = new JsonTransform(listOfProps)});

                var jObject = JObject.Parse(json);
                return Ok(jObject);
            }
            catch (Exception e)
            {
                _logger.LogError(e, BaseMessages.ERROR_MESSAGE);
                return BadRequest(BaseMessages.ERROR_MESSAGE);
            }
        }

        [HttpGet]
        public virtual async Task<IActionResult> ListPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 5)
        {
            try
            {
                var listOfProps = GetFieldsFromModel();

                if (listOfProps == null || listOfProps.Length == 0)
                    return BadRequest(BaseMessages.ERROR_GET_FIELDS);

                var query = FilterQuery(GetQuerySet(), HttpContext.Request);
                query = Sort(AllowedFields, query);
                var (pages, data) = await _serializer.List(page, pageSize, query);

                string json = JsonConvert.SerializeObject(data,
                    new JsonSerializerSettings {ContractResolver = new JsonTransform(listOfProps)});
                var jArray = JArray.Parse(json);

                var result = new PagedBaseResponse<JArray>()
                {
                    Data = jArray,
                    Pages = pages
                };

                return Ok(result);
            }
            catch (Exception e)
            {
                _logger.LogError(e, BaseMessages.ERROR_MESSAGE);
                return BadRequest(BaseMessages.ERROR_MESSAGE);
            }
        }

        [HttpPost]
        public virtual async Task<IActionResult> Post(TOrigin entity)
        {
            try
            {
                var isSaved = await _serializer.Save(entity, OperationType.Create);

                if (!isSaved)
                    return BadRequest(_serializer.Errors);
            }
            catch (Exception e)
            {
                _logger.LogError(e, BaseMessages.ERROR_MESSAGE);
                return BadRequest(BaseMessages.ERROR_MESSAGE);
            }

            return Created(BaseMessages.SUCESS, new { });
        }

        [HttpPatch]
        [Route("{Id}")]
        public virtual async Task<IActionResult> Patch([FromBody] PartialJsonObject<TOrigin> entity,
            [FromRoute] TPrimaryKey Id)
        {
            try
            {
                if (!_actionOptions.AllowPatch)
                    return StatusCode(StatusCodes.Status405MethodNotAllowed);

                await _serializer.Patch(entity, Id);
                return Ok(BaseMessages.SUCESS);
            }
            catch (Exception e)
            {
                _logger.LogError(e, BaseMessages.ERROR_MESSAGE);
                return BadRequest(BaseMessages.ERROR_MESSAGE);
            }
        }

        [HttpPut]
        [Route("{Id}")]
        public virtual async Task<IActionResult> Put([FromBody] TOrigin origin, [FromRoute] TPrimaryKey Id)
        {
            try
            {
                if (!_actionOptions.AllowPut)
                    return StatusCode(StatusCodes.Status405MethodNotAllowed);

                await _serializer.Save(origin, OperationType.Update, Id);
                return Ok(BaseMessages.SUCESS);
            }
            catch (Exception e)
            {
                _logger.LogError(e, BaseMessages.ERROR_MESSAGE);
                return BadRequest(BaseMessages.ERROR_MESSAGE);
            }
        }

        [HttpDelete]
        [Route("{Id}")]
        public virtual async Task<IActionResult> Delete([FromRoute] TPrimaryKey Id)
        {
            try
            {
                await _serializer.Delete(Id);
                return Ok(BaseMessages.SUCESS);
            }
            catch (Exception e)
            {
                _logger.LogError(e, BaseMessages.ERROR_MESSAGE);
                return BadRequest(BaseMessages.ERROR_MESSAGE);
            }
        }

        #region methods

        [NonAction]
        public string[] GetFieldsFromModel()
        {
            try
            {
                var instanceMethod = typeof(TDestination).GetMethod("GetFields");
                return (string[]) instanceMethod?.Invoke(Activator.CreateInstance(typeof(TDestination), null), null);
            }
            catch (Exception e)
            {
                return Array.Empty<string>();
            }
        }

        [NonAction]
        public IQueryable<TDestination> FilterQuery(IQueryable<TDestination> query, HttpRequest request)
        {
            foreach (var filter in Filters)
                query = filter.AddFilter(query, request);
            return query;
        }

        [NonAction]
        public IQueryable<TDestination> Sort(string[] allowedFilters, IQueryable<TDestination> query)
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