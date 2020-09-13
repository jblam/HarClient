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
        [Route("content")]
        public IActionResult DoContent() => Ok(new SomeContent { Content = "Content 🚀", Collection = { 1, 2, } });

        [HttpPost, Route("accepted")]
        public IActionResult DoAccepted([FromQuery] string location = null) => Accepted(location);

        [Route("no-content")]
        public IActionResult DoNoContent() => NoContent();

        [Route("redirect")]
        [Route("redirect-found")]
        public IActionResult DoRedirect() => LocalRedirect("/api/behaviour/content");
        [Route("redirect-moved-permanent")]
        public IActionResult DoRedirectPermanent() => RedirectPermanent("/api/behaviour/content");
        [Route("redirect-see-other")]
        public IActionResult DoRedirectSeeOther()
        {
            var output = new StatusCodeResult(303);
            Response.Headers.Add("Location", "/api/behaviour/content");
            return output;
        }
        [Route("redirect-temporary")]
        public IActionResult DoRedirectTemporary() => RedirectPreserveMethod("/api/behaviour/content");
        [Route("redirect-permanent")]
        public IActionResult DoRedirect308() => RedirectPermanentPreserveMethod("/api/behaviour/content");
        [Route("redirect-echo")]
        public IActionResult DoRedirectEcho([FromQuery] int? status = null, [FromQuery] string location = "/api/behaviour/content")
        {
            if (!status.HasValue || status < 300 || status >= 400 || location == null)
                return BadRequest();
            switch (status)
            {
                case 301:
                    return RedirectPermanent(location);
                case 302:
                    return LocalRedirect(location);
                case 307:
                    return RedirectPreserveMethod(location);
                case 308:
                    return RedirectPermanentPreserveMethod(location);
                default:
                    Response.Headers.Add("Location", location);
                    return new StatusCodeResult(status.Value);
            }
        }

        [Route("redirect/infinite/{i}")]
        public IActionResult DoInfiniteRedirect(int i) => Redirect($"/api/behaviour/redirect/infinite/{i + 1}");

        [Route("redirect/circular/{i}")]
        public IActionResult DoCircularRedirect(int i) => Redirect($"/api/behaviour/redirect/circular/{(i == 0 ? 1 : 0)}");

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
