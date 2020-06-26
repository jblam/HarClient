using System;
using System.Collections.Generic;
using System.Linq;
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
        public IActionResult DoContent() => Ok(new SomeContent { Content = "Content", Collection = { 1, 2, } });

        [HttpPost, Route("accepted")]
        public IActionResult DoAccepted([FromQuery] string location = null) => Accepted(location);

        [Route("no-content")]
        public IActionResult DoNoContent() => NoContent();

        [Route("redirect")]
        public IActionResult DoRedirect() => Redirect("/api/behaviour/content");

        [Route("not-found")]
        public IActionResult DoNotFound() => NotFound();
    }

    public class SomeContent
    {
        public string Content { get; set; }
        public IList<int> Collection { get; } = new List<int>();
    }
}
