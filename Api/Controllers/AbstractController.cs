using System;
using System.Globalization;
using Api.Errors;
using Api.Properties;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;

namespace Api.Controllers
{
    public abstract class AbstractController : ControllerBase
    {
        protected string RequestIpAddress {
            get {
                if (Request.Headers.ContainsKey("X-Forwarded-For"))
                    return Request.Headers["X-Forwarded-For"];
                return HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
            }
        }

        [NonAction]
        protected long? CurrentPersonIdOrNull()
        {
            var name = User.Identity.Name;
            if (name == null)
                return null;

            try
            {
                return long.Parse(name, CultureInfo.InvariantCulture);
            }
            catch (Exception e)
            {
                return null;
            }
        }

        [NonAction]
        protected long CurrentPersonId()
        {
            return CurrentPersonIdOrNull() ?? throw new Exception("");
        }

        [NonAction]
        public ActionResult ValidationProblem(Action<ModelStateDictionary> initializeModelState)
        {
            var modelState = new ModelStateDictionary();
            initializeModelState?.Invoke(modelState);
            return ValidationProblem(modelState);
        }
    }
}