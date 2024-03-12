using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Data.Dto;
using Api.Data.Payload;
using Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController, Route("search"), Authorize]
    public class SearchController : AbstractController
    {
        private readonly SearchRepository _searchRepository;

        public SearchController(SearchRepository searchRepository)
        {
            _searchRepository = searchRepository ?? throw new ArgumentNullException(nameof(searchRepository));
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<SearchGetPayload>> getSearch([FromQuery] string query)
        {
            SearchGetPayload payload = await _searchRepository.FindSearch(query);
            return Ok(payload);
        }
    }
}