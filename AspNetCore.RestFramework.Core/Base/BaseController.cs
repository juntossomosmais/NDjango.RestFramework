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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AspNetCore.RestFramework.Core.Base
{
    public class BaseFilter<TContext, TEntity> where TContext : DbContext
        where TEntity : class
    {
        private TContext _context;
        public IQueryable<TEntity> DbSet { get; set; }

        public BaseFilter(TContext context)
        {
            _context = context;
            DbSet = context.Set<TEntity>();
        }
    }

    public class ActionOptions
    {
        public bool AllowList { get; set; } = true;
        public bool AllowPost { get; set; } = true;
        public bool AllowPatch { get; set; } = true;
        public bool AllowPut { get; set; } = true;
    }


    [Produces("application/json")]
    public class BaseController<TOrigin, TDestination, TPrimaryKey, TContext> : ControllerBase
        where TOrigin : BaseDto
        where TDestination : BaseModel<TPrimaryKey>
        where TContext : DbContext

    {
        private readonly Serializer<TOrigin, TDestination, TPrimaryKey, TContext> _serializer;
        public IQueryable<TDestination> Query { get; set; }
        private TContext _context { get; set; }
        private ActionOptions _actionOptions { get; set; }
        public List<Filter<TDestination>> Filters { get; set; } = new List<Filter<TDestination>>();
        public string[] AllowedFields { get; set; } = Array.Empty<string>();


        #region .:: Constructors ::.

        public BaseController(Serializer<TOrigin, TDestination, TPrimaryKey, TContext> serializer, TContext context,
            ActionOptions actionOptions)
        {
            _serializer = serializer;
            _actionOptions = actionOptions ?? new ActionOptions();
            _context = context;
            Query = new FilterBuilder<TContext, TDestination>(_context).DbSet;
        }

        public BaseController(Serializer<TOrigin, TDestination, TPrimaryKey, TContext> serializer, TContext context)
        {
            _serializer = serializer;
            _actionOptions = new ActionOptions();
            _context = context;
            Query = new FilterBuilder<TContext, TDestination>(_context).DbSet;
        }

        #endregion


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

        [HttpGet]
        [Route("{Id}")]
        public virtual async Task<IActionResult> GetSingle(TPrimaryKey Id)
        {
            try
            {
                var instanceMethod = typeof(TDestination).GetMethod("GetFields");
                var classInstance = Activator.CreateInstance(typeof(TDestination), null);
                var listOfProps = instanceMethod?.Invoke(classInstance, null);

                if (listOfProps == null || ((string[]) listOfProps).Length == 0)
                    return BadRequest("It is necessary to implement the GetFields method inside the entity.");
                
                var query = FilterQuery(GetQuerySet(), HttpContext.Request);

                var data = await _serializer.GetFromDB(Id, query);
                if (data == null)
                    return NotFound("Entity not found");

                string json = JsonConvert.SerializeObject(data,
                    new JsonSerializerSettings {ContractResolver = new JsonTransform((string[]) listOfProps)});

                var jObject = JObject.Parse(json);
                return Ok(jObject);
            }
            catch (Exception e)
            {
                return BadRequest("An error occurred while performing the operation.");
            }
        }

        [HttpGet]
        public virtual async Task<IActionResult> ListPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 5)
        {
            try
            {
                var instanceMethod = typeof(TDestination).GetMethod("GetFields");
                var classInstance = Activator.CreateInstance(typeof(TDestination), null);
                var listOfProps = instanceMethod?.Invoke(classInstance, null);

                if (listOfProps == null || ((string[]) listOfProps).Length == 0)
                    return BadRequest("It is necessary to implement the GetFields method inside the entity.");
                
                var query = FilterQuery(GetQuerySet(), HttpContext.Request);
                query = Sort(AllowedFields, query);
                var (pages, data) = await _serializer.List(page, pageSize, query);

                string json = JsonConvert.SerializeObject(data,
                    new JsonSerializerSettings {ContractResolver = new JsonTransform((string[]) listOfProps)});
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
                return BadRequest("An error occurred while performing the operation.");
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
                Console.WriteLine(e);
                throw;
            }

            return Created("", new { });
        }

        [HttpPatch]
        [Route("{Id}")]
        public virtual async Task<IActionResult> Patch([FromBody] PartialJsonObject<TOrigin> entity,
            [FromRoute] TPrimaryKey Id)
        {
            if (!_actionOptions.AllowPatch)
                return StatusCode(StatusCodes.Status405MethodNotAllowed);

            await _serializer.Patch(entity, Id);
            return Ok();
        }

        [HttpPut]
        [Route("{Id}")]
        public virtual async Task<IActionResult> Put([FromBody] TOrigin origin, [FromRoute] TPrimaryKey Id)
        {
            if (!_actionOptions.AllowPut)
                return StatusCode(StatusCodes.Status405MethodNotAllowed);

            await _serializer.Save(origin, OperationType.Update, Id);
            return Ok();
        }

        [HttpDelete]
        [Route("{Id}")]
        public virtual async Task<IActionResult> Delete([FromRoute] TPrimaryKey Id)
        {
            await _serializer.Delete(Id);
            return Ok();
        }
    }
}