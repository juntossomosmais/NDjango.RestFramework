using CSharpRestFramework.Filters;
using CSharpRestFramework.Serializer;
using JSM.PartialJsonObject;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CSharpRestFramework.Base
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
        public ActionOptions(bool allowList = true,
            bool allowPost = true,
            bool allowPatch = true,
            bool allowPut = true,
            bool allowDelete = true,
            bool allowGetSingle = true)
        {
            AllowList = allowList;
            AllowPost = allowPost;
            AllowPatch = allowPatch;
            AllowPut = allowPut;
            AllowDelete = allowDelete;
            AllowGetSingle = allowGetSingle;
        }

        public bool AllowList { get; } 
        public bool AllowPost { get; } 
        public bool AllowPatch { get;  }
        public bool AllowPut { get; }
        public bool AllowDelete { get; }
        public bool AllowGetSingle { get; }
    }


    public class BaseController<TOrigin, TDestination, TPrimaryKey, TContext> : ControllerBase where TOrigin : BaseDto
                                                                                  where TDestination : class
                                                                                  where TContext : DbContext

    {
        private readonly Serializer<TOrigin, TDestination, TContext> _serializer;
        public IQueryable<TDestination> Query { get; set; }
        private TContext _context { get; set; }
        private ActionOptions _actionOptions { get; set; }
        public List<Filter<TDestination>> Filters { get; set; } = new List<Filter<TDestination>>();
        public string[] AllowedFields { get; set; } = Array.Empty<string>();


        #region .:: Constructors ::.
        public BaseController(Serializer<TOrigin, TDestination, TContext> serializer, TContext context, ActionOptions actionOptions = null)
        {
            _serializer = serializer;
            _actionOptions = actionOptions ?? new ActionOptions();
            _context = context;
            Query = new FilterBuilder<TContext, TDestination>(_context).DbSet;
        }

        public BaseController(Serializer<TOrigin, TDestination, TContext> serializer, TContext context)
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
        [Route("{entityId}")]
        public virtual async Task<IActionResult> GetSingle(TPrimaryKey entityId)
        {
            if (!_actionOptions.AllowGetSingle)
                return StatusCode(StatusCodes.Status405MethodNotAllowed);
            
            return Ok(await _serializer.GetSingle(entityId));
        }

        [HttpGet]
        public virtual async Task<IActionResult> ListPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 5)
        {
            if (!_actionOptions.AllowList)
                return StatusCode(StatusCodes.Status405MethodNotAllowed);
            
            var query = FilterQuery(GetQuerySet(), HttpContext.Request);

            query = Sort(AllowedFields, query);

            var (pages, data) = await _serializer.List(page, pageSize, query);
            return Ok(new PagedBaseResponse<TDestination>()
            {
                Data = data,
                Pages = pages
            });
        }

        [HttpPost]
        
        public virtual async Task<IActionResult> Post(TOrigin entity)
        {
            if (!_actionOptions.AllowPost)
                return StatusCode(StatusCodes.Status405MethodNotAllowed);
            
            var isSaved = await _serializer.Save(entity, OperationType.Create);

            if (!isSaved)
                return BadRequest(_serializer.Errors);

            return Created("", new { });
        }

        [HttpPatch]
        [Route("{entityId}")]
        public virtual async Task<IActionResult> Patch([FromBody] PartialJsonObject<TOrigin> entity, [FromRoute] TPrimaryKey entityId)
        {
            if (!_actionOptions.AllowPatch)
                return StatusCode(StatusCodes.Status405MethodNotAllowed);

            await _serializer.Patch(entity, entityId);
            return Ok();
        }

        [HttpPut]
        [Route("{entityId}")]
        public virtual async Task<IActionResult> Put([FromBody] TOrigin origin, [FromRoute] TPrimaryKey entityId)
        {
            if (!_actionOptions.AllowPut)
                return StatusCode(StatusCodes.Status405MethodNotAllowed);

            await _serializer.Save(origin, OperationType.Update, entityId);
            return Ok();
        }

        [HttpDelete]
        [Route("{entityId}")]
        public virtual async Task<IActionResult> Delete([FromRoute] TPrimaryKey entityId)
        {
            if (!_actionOptions.AllowDelete)
                return StatusCode(StatusCodes.Status405MethodNotAllowed);
            
            await _serializer.Delete(entityId);
            return Ok();
        }
    }
}
