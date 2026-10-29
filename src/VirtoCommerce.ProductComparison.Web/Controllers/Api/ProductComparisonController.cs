using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Permissions = VirtoCommerce.ProductComparison.Core.ModuleConstants.Security.Permissions;

namespace VirtoCommerce.ProductComparison.Web.Controllers.Api;

[Authorize]
[Route("api/product-comparison")]
public class ProductComparisonController : Controller
{
    // GET: api/product-comparison
    /// <summary>
    /// Get message
    /// </summary>
    /// <remarks>Return "Hello world!" message</remarks>
    [HttpGet]
    [Route("")]
    [Authorize(Permissions.Read)]
    public ActionResult<string> Get()
    {
        return Ok(new { result = "Hello world!" });
    }
}
