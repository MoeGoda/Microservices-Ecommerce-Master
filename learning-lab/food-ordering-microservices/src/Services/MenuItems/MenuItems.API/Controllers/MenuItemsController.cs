using MenuItems.API.Entities;
using MenuItems.API.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace MenuItems.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class MenuItemsController : ControllerBase
    {
        private readonly IMenuItemsRepository _repository;

        public MenuItemsController(IMenuItemsRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<MenuItem>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IEnumerable<MenuItem>>> GetMenuItems()
        {
            return Ok(await _repository.GetMenuItems());
        }

        [HttpGet("{id}", Name = "GetMenuItem")]
        [ProducesResponseType(typeof(MenuItem), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<MenuItem>> GetMenuItem(string id)
        {
            var menuItem = await _repository.GetMenuItem(id);
            if (menuItem == null)
            {
                return NotFound();
            }

            return Ok(menuItem);
        }

        [HttpGet("GetByCategory/{category}", Name = "GetMenuItemsByCategory")]
        [ProducesResponseType(typeof(IEnumerable<MenuItem>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IEnumerable<MenuItem>>> GetMenuItemsByCategory(string category)
        {
            return Ok(await _repository.GetMenuItemsByCategory(category));
        }

        [HttpPost]
        [ProducesResponseType(typeof(MenuItem), (int)HttpStatusCode.Created)]
        public async Task<ActionResult<MenuItem>> CreateMenuItem([FromBody] MenuItem menuItem)
        {
            await _repository.CreateMenuItem(menuItem);
            return CreatedAtRoute("GetMenuItem", new { id = menuItem.Id }, menuItem);
        }

        [HttpPut]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> UpdateMenuItem([FromBody] MenuItem menuItem)
        {
            await _repository.UpdateMenuItem(menuItem);
            return NoContent();
        }

        [HttpDelete("{id}", Name = "DeleteMenuItem")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> DeleteMenuItem(string id)
        {
            await _repository.DeleteMenuItem(id);
            return NoContent();
        }
    }
}
