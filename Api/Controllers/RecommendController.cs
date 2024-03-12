using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Data.Dto;
using Api.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController, Route("recommend")]
    public class RecommendController : AbstractController
    {
        private readonly SearchRepository _searchRepository;

        public RecommendController(SearchRepository searchRepository)
        {
            _searchRepository = searchRepository ?? throw new ArgumentNullException(nameof(searchRepository));
        }

        // [HttpGet]
        // [ProducesResponseType(StatusCodes.Status200OK)]
        // public async Task<ActionResult<List<SuggestionDto>>> getRecommend(string? text)
        // {
        //     
        //     List<SuggestionDto> suggestionItems = await _searchRepository.FindSuggestions(text);
        //     return Ok(suggestionItems);
        // }
    }
}