using Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;

namespace CommonLogging.Poc.WebApi.Controllers
{
    public class DefaultController : ApiController
    {
        public DefaultController(ILog logger)
        {
            this.Logger = logger;
        }

        public ILog Logger { get; private set; }
        
        [Route("DoSomething")]
        [HttpGet]
        public IHttpActionResult DoSomething()
        {
            Logger.Debug("Entering DefaultController.DoSomething()");


            

            Logger.Debug("Completing DefaultController.DoSomething()");

            return Ok();

        }

        [Route("ThrowError")]
        [HttpGet]
        public IHttpActionResult ThrowError()
        {
            try
            {
                throw new InvalidOperationException("Something bad happened!!!");
            }
            catch(Exception exc)
            {
                Logger.Error("Error found", exc);

                return BadRequest(exc.Message);
            }

            

        }

        [ResponseType(typeof(string))]
        [HttpGet]
        [Route("Hello")]
        public IHttpActionResult Hello([FromUri] string name)
        {
            Logger.Debug("Entering DefaultController.Hello()");




            Logger.Debug("Completing DefaultController.Hello()");

            return Ok($"Hello {name}");

        }
    }
}
