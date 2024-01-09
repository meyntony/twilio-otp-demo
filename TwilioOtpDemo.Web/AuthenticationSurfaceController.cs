using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Security;
using Umbraco.Cms.Web.Website.Controllers;

namespace TwilioOtpDemo.Web
{
	public sealed class AuthenticationSurfaceController : SurfaceController
	{
		IMemberService _memberService;
		IMemberManager _memberManager;
		IMemberSignInManager _memberSignInManager;
		IHttpContextAccessor _httpContextAccessor;

		public AuthenticationSurfaceController(
			IUmbracoContextAccessor umbracoContextAccessor,
			IUmbracoDatabaseFactory databaseFactory,
			ServiceContext services,
			AppCaches appCaches,
			IProfilingLogger profilingLogger,
			IPublishedUrlProvider publishedUrlProvider,

			IMemberService memberService,
			IMemberManager memberManager,
			IMemberSignInManager memberSignInManager,
			IHttpContextAccessor httpContextAccessor) : base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
		{
			_memberService = memberService;
			_memberManager = memberManager;
			_memberSignInManager = memberSignInManager;
			_httpContextAccessor = httpContextAccessor;
		}

		[HttpPost]
		public IActionResult Logout()
		{
			_memberSignInManager.SignOutAsync();
			_httpContextAccessor.HttpContext.Session.Clear();
			return RedirectToCurrentUmbracoPage();
		}
	}
}
