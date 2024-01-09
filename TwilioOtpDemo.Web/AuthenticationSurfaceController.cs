using Microsoft.AspNetCore.Mvc;
using Twilio;
using Twilio.Rest.Verify.V2.Service;
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
		private readonly IConfiguration _configuration;
		private readonly string? _serviceSid;

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
			IHttpContextAccessor httpContextAccessor,
			IConfiguration configuration) : base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
		{
			_memberService = memberService;
			_memberManager = memberManager;
			_memberSignInManager = memberSignInManager;
			_httpContextAccessor = httpContextAccessor;
			_configuration = configuration;
			
			var configPath = "SmsDataService:Twilio:";

			var accountSid = _configuration[$"{configPath}AccountSid"];
			var authToken = _configuration[$"{configPath}AuthToken"];
			TwilioClient.Init(accountSid, authToken);

			_serviceSid = _configuration[$"{configPath}ServiceSid"];
		}

		public const string UserNameKey = "usernameKey";


		[HttpPost]
		public IActionResult Login(long member_danish_number, string otp, string redirect_url = "/")
		{
			string username = "+45";
			if (member_danish_number.ToString().Length == 8)
			{
				username += member_danish_number;
				if (string.IsNullOrWhiteSpace(otp))
				{
					// send otp
					VerificationResource.Create(
						to: username,
						channel: "sms",
						locale: "en",
						pathServiceSid: _serviceSid
					);
					_httpContextAccessor.HttpContext.Session.SetString(UserNameKey, member_danish_number.ToString());
				}
				else
				{
					// validate otp
					var verification = VerificationCheckResource.Create(
						to: username,
						code: otp,
						pathServiceSid: _serviceSid
					);
					bool verified = verification?.Valid ?? false;
					if (verified)
					{
						var member = _memberService.GetByUsername(username);
						if (member == null)
						{
							var memberIdentityUser = MemberIdentityUser.CreateNew(
								username: username,
								email: $"{member_danish_number}@45.dk",
								memberTypeAlias: "Member",
								isApproved: true,
								name: member_danish_number.ToString());

							if (_memberManager.CreateAsync(
								user: memberIdentityUser,
								password: username).Result.Succeeded)
							{
								_memberManager.AddToRolesAsync(
								user: memberIdentityUser,
								roles: new List<string>() { "Everyone" }).Wait();
							}
							member = _memberService.GetByUsername(username);
						}
						if (_memberManager.ValidateCredentialsAsync(username: member.Username, password: member.Username).Result)
						{
							// Validate member credentials
							var memberIdentityUser = _memberManager.FindByNameAsync(member.Username).Result;
							_memberSignInManager.SignInAsync(user: memberIdentityUser, isPersistent: true).Wait();
						}
					}
				}
			}
			
			return Redirect(redirect_url);
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
