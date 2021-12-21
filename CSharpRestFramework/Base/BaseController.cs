﻿using CSharpRestFramework.Filters;
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
                                                    where TEntity : BaseEntity
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
    }


    public class BaseController<TOrigin, TDestination, TContext> : ControllerBase where TOrigin : BaseDto
                                                                                  where TDestination : BaseEntity
                                                                                  where TContext : DbContext

    {
        private readonly Serializer<TOrigin, TDestination, TContext> _serializer;
        private ActionOptions _actionOptions;
        public IQueryable<TDestination> Query { get; set; }
        public List<string> FilterFields { get; set; } = new List<string>();

        public List<Filter<TDestination>> Filters { get; set; } = new List<Filter<TDestination>>();
        private TContext _context;


        #region .:: Constructors ::.
        public BaseController(Serializer<TOrigin, TDestination, TContext> serializer, TContext context, ActionOptions actionOptions)
        {
            _serializer = serializer;
            _actionOptions = actionOptions == null ? new ActionOptions() : actionOptions;
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
        protected void RegisterFilters(HttpRequest request)
        {
            foreach (var filter in Filters)
                Query = filter.AddFilter(Query, request);
        }

        [NonAction]
        protected void AddSort(HttpRequest request, string[] allowedFilters)
        {
            Query = new SortFilter<TDestination>().Sort(Query, request, allowedFilters);
        }


        [HttpGet]
        public async Task<IActionResult> ListPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 5)
        {
            var responseBody = await _serializer.List(page, pageSize, Query);
            return Ok(new PagedBaseResponse<TDestination>()
            {
                Data = responseBody.Data,
                Pages = responseBody.Pages
            });
        }

        [HttpPost]
        public async Task<IActionResult> Post(TOrigin entity)
        {
            var isSaved = await _serializer.Save(entity, OperationType.Create);

            if (!isSaved)
                return BadRequest(_serializer.Errors);
           
            return Created("", new { });
        }

        [HttpPatch]
        public async Task<IActionResult> Patch([FromBody] PartialJsonObject<TOrigin> entity)
        {
            if (!_actionOptions.AllowPatch)
                return StatusCode(StatusCodes.Status405MethodNotAllowed);

            await _serializer.Patch(entity);
            return Ok();
        }
    }
}
