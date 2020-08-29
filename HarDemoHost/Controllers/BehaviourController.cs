using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HarDemoHost.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BehaviourController : ControllerBase
    {
        [HttpGet, HttpPost, Route("content")]
        public IActionResult DoContent() => Ok(new SomeContent { Content = "Content 🚀", Collection = { 1, 2, } });

        [HttpPost, Route("accepted")]
        public IActionResult DoAccepted([FromQuery] string location = null) => Accepted(location);

        [Route("no-content")]
        public IActionResult DoNoContent() => NoContent();

        [Route("redirect")]
        public IActionResult DoRedirect() => Redirect("/api/behaviour/content");

        [Route("redirect/infinite/{i}")]
        public IActionResult DoInfiniteRedirect(int i) => Redirect($"/api/behaviour/redirect/{i + 1}");

        [Route("redirect/circular/{i}")]
        public IActionResult DoCircularRedirect(int i) => Redirect($"/api/behaviour/redirect/{(i == 0 ? 1 : 0)}");

        [Route("not-found")]
        public IActionResult DoNotFound() => NotFound();

        [Route("delay")]
        public async Task<IActionResult> DelayNoContent(CancellationToken cancellationToken, [FromQuery] int delay_ms = 0)
        {
            await Task.Delay(delay_ms, cancellationToken);
            return NoContent();
        }

        static readonly CookieOptions DemoCookieOptions =
            new CookieOptions { Path = "/api", SameSite = SameSiteMode.Strict };

        [HttpPost, Route("set-cookie")]
        public IActionResult SetCookie()
        {
            Response.Cookies.Append("key", "value", DemoCookieOptions);
            return NoContent();
        }

        [HttpGet("check-cookie")]
        public IActionResult CheckCookie()
        {
            if (Request.Cookies.Any())
            {
                Response.Cookies.Delete("key", DemoCookieOptions);
                return Ok(Request.Cookies.Select(kv => $"{kv.Key}={kv.Value}"));
            }
            else
            {
                return BadRequest("No cookies 🙁");
            }
        }

    }

    /// <summary>
    /// Some arbitrary structured data for serialisation
    /// </summary>
    /// <remarks>
    /// The XML serialisation requires this to be a public, non-nested class.
    /// </remarks>
    public class SomeContent
    {
        public string Content { get; set; }
        public IList<int> Collection { get; } = new List<int>();
    }
}
