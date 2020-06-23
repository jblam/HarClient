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

        public IActionResult Get()
        {
            return Ok(new { Content = "Content", Collection = new[] { 1, 2 } });
        }
    }
}
