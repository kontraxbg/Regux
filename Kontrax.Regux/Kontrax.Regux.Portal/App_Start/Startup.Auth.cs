using System;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Owin;
using Kontrax.Regux.Portal.Identity;
using Kontrax.Regux.Shared.Portal.Identity;

namespace Kontrax.Regux.Portal
{
    public partial class Startup
    {
        public const string IdentityCookieName = "." + Model.ThisSystem.Name + ".Identity";

        // For more information on configuring authentication, please visit https://go.microsoft.com/fwlink/?LinkId=301864
        public void ConfigureAuth(IAppBuilder app)
        {
            // Configure the db context, user manager and signin manager to use a single instance per request
            app.CreatePerOwinContext(ApplicationDbContext.Create);
            app.CreatePerOwinContext<ApplicationUserManager>(ApplicationUserManager.Create);
            app.CreatePerOwinContext<ApplicationSignInManager>(ApplicationSignInManager.Create);

            // Enable the application to use a cookie to store information for the signed in user
            // and to use a cookie to temporarily store information about a user logging in with a third party login provider
            // Configure the sign in cookie
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                CookieName = IdentityCookieName,
                // Тест: Бързо изхвърляне от системата на първо ниво (парола).
                //ExpireTimeSpan = TimeSpan.FromSeconds(10),  // Това е sliding expiration.
                LoginPath = new PathString("/Account/Login"),
                Provider = new CookieAuthenticationProvider
                {
                    // Enables the application to validate the security stamp when the user logs in.
                    // This is a security feature which is used when you change a password or add an external login to your account.  
                    OnValidateIdentity = SecurityStampValidator.OnValidateIdentity<ApplicationUserManager, ApplicationUser>(
                        // Тест: Честа проверка дали е променен SecurityStamp-ът в базата.
                        //validateInterval: TimeSpan.FromSeconds(10),
                        validateInterval: TimeSpan.FromMinutes(30),
                        regenerateIdentity: (manager, user) => user.GenerateUserIdentityAsync(manager))
                }
            });

            // Тест: Бързо изтичане на второ ниво (еАвт или сертификат). Този timeout не изхвъля потребителя от системата.
            // Единственият видим ефект е, че понякога след еАвт процеса (който тече няколко секунди)
            // HasBeenVerified() е false, при което не може да се изпълни TwoFactorSignIn().
            //app.UseTwoFactorSignInCookie(DefaultAuthenticationTypes.TwoFactorCookie, TimeSpan.FromSeconds(10));
            app.UseTwoFactorSignInCookie(DefaultAuthenticationTypes.TwoFactorCookie, TimeSpan.FromMinutes(5));
            app.UseTwoFactorRememberBrowserCookie(DefaultAuthenticationTypes.TwoFactorRememberBrowserCookie);
        }
    }
}