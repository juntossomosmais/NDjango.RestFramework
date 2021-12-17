using JSM.PartialJsonObject;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using WebApplication2.DTO;
using WebApplication2.Filters;
using WebApplication2.Models;
using WebApplication2.Serializers;

namespace WebApplication2.Controllers
{


    //public class QuerySet<TDestination>
    //{
    //    Expression<Func<TDestination, bool>> filter
    //    public dynamic BuildQuery<TDestination>(HttpRequest request)
    //    {
    //        string queryString = request.QueryString.Value;
    //    }
    //}


    public abstract class BackendFilter<TContext> where TContext:DbContext
    {
        public TContext Context { get; set; }

        public BackendFilter(TContext context)
        {
            Context = context;
        }

        public virtual Dictionary<string, string> FilterQuerySet<TDestination>(HttpRequest request)
        {
            return null;
        }

        public virtual Dictionary<string, string> FilterQuerySet<TDestination>(HttpRequest httpRequest, List<string> allowedFields)
        {
            return null;
        }
    }

    public class ActionOptions
    {
        public bool AllowList { get; set; } = true;
        public bool AllowPost { get; set; } = true;
        public bool AllowPatch { get; set; } = true;
    }


    public class BaseController<TOrigin, TDestination, TContext> : ControllerBase where TOrigin : BaseDto
                                                                                  where TDestination : BaseEntity
                                                                                  where TContext : DbContext

    {
        private readonly ISerializer<TOrigin, TDestination, TContext> _serializer;
        private ActionOptions _actionOptions;
        public List<string> FilterFields { get; set; } = new List<string>();

        public List<Func<Dictionary<string, string>>> Filters { get; set; } = new List<Func<Dictionary<string, string>>>();


        #region .:: Constructors ::.
        public BaseController(ISerializer<TOrigin, TDestination, TContext> serializer, TContext context, ActionOptions actionOptions)
        {
            _serializer = serializer;
            _actionOptions = actionOptions == null ? new ActionOptions() : actionOptions;
        }

        public BaseController(ISerializer<TOrigin, TDestination, TContext> serializer)
        {
            _serializer = serializer;
            _actionOptions = new ActionOptions();
        }

        #endregion


        [ApiExplorerSettings(IgnoreApi = true)]
        public Dictionary<string, string> FilterQuerySet()
        {
            var request = HttpContext.Request;
            Dictionary<string, string> filters = new Dictionary<string, string>();

            foreach (var item in Filters)
            {
                var filterItems = item.Invoke();
                foreach (var dictEntry in filterItems)
                    filters.Add(dictEntry.Key, dictEntry.Value);
            }

            return filters;
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 5)
        {
            
            var responseBody = await _serializer.List(FilterQuerySet());
            return Ok(responseBody);
        }

        [HttpGet]
        [Route("Paged")]
        public async Task<IActionResult> ListPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 5)
        {
            var responseBody = await _serializer.List(page, pageSize);
            return Ok(responseBody);
        }

        [HttpPost]
        public async Task<IActionResult> Post(TOrigin entity)
        {
            await _serializer.Post(entity);
            await _serializer.Save();
            return Created("", new { });
        }

        [HttpPatch]
        public async Task<IActionResult> Patch([FromBody] PartialJsonObject<TOrigin> entity)
        {
            if (!_actionOptions.AllowPatch)
                return StatusCode(StatusCodes.Status405MethodNotAllowed);

            _serializer.Patch(entity);
            await _serializer.Save();
            return Ok();
        }
    }
}
