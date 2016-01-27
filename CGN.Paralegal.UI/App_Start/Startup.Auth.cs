﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OAuth;
using Owin;
using CGN.Paralegal.UI.Providers;

namespace CGN.Paralegal.UI
{
    public partial class Startup
    {
        static Startup()
        {
            PublicClientId = "self";

            UserManagerFactory = () => new UserManager<IdentityUser>(new UserStore<IdentityUser>());

            OAuthOptions = new OAuthAuthorizationServerOptions
            {
                TokenEndpointPath = new PathString("/Token"),
                Provider = new ApplicationOAuthProvider(PublicClientId, UserManagerFactory),
                AuthorizeEndpointPath = new PathString("/api/Account/ExternalLogin"),
                AccessTokenExpireTimeSpan = TimeSpan.FromDays(14),
                AllowInsecureHttp = true
            };
        }

        public static OAuthAuthorizationServerOptions OAuthOptions { get; private set; }

        public static Func<UserManager<IdentityUser>> UserManagerFactory { get; set; }

        public static string PublicClientId { get; private set; }

        // For more information on configuring authentication, please visit http://go.microsoft.com/fwlink/?LinkId=301864
        public void ConfigureAuth(IAppBuilder app)
        {
            // Enable the application to use a cookie to store information for the signed in user
            // and to use a cookie to temporarily store information about a user logging in with a third party login provider
            app.UseCookieAuthentication(new CookieAuthenticationOptions());
            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);

            // Enable the application to use bearer tokens to authenticate users
            app.UseOAuthBearerTokens(OAuthOptions);

            // Uncomment the following lines to enable logging in with third party login providers
            app.UseMicrosoftAccountAuthentication(
                clientId: "000000004C172782",
                clientSecret: "9rjrusF64Ws2Ksd67DVgyPgd0101i4jw");

            app.UseTwitterAuthentication(
                consumerKey: "QBAhakLsIl794uEEAX8csF3tP",
                consumerSecret: "ML44lD3gjDWfF1VR2SRmYa8Ic0VPUEk9FwraEBs0iP3awfvKuS");

            app.UseFacebookAuthentication(
                appId: "523635924470241",
                appSecret: "ef29066fd82cb20e746b52c00c7e34a7");

            app.UseGoogleAuthentication(new Microsoft.Owin.Security.Google.GoogleOAuth2AuthenticationOptions() {   ClientId = "122905730574.apps.googleusercontent.com", ClientSecret = "AjLf5P2WiGXIQrKqeTBRifAr" });
        }
    }
}