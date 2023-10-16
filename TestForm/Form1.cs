using IdentityModel.OidcClient;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog.Sinks.SystemConsole.Themes;
using IdentityModel.OidcClient.DPoP;
using IdentityModel;
using Microsoft.AspNetCore.DataProtection;
using System.Net;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using ConsoleClientWithBrowserAndDPoP;
using IdentityModel.OidcClient.Results;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using IdentityModel.Client;
using System.Diagnostics;

namespace TestForm
{
    public partial class Form1 : Form
    {
        private bool isLoggedIn;
        public Form1()
        {
            InitializeComponent();
            isLoggedIn = true;
            //LoginButton.BackColor = Color.DeepSkyBlue;

            //CheckAuth();

            Thread thread1 = new Thread(SetValues);
            thread1.IsBackground = true;
            thread1.Start();
        }

        private void LoginButton_Click(object sender, EventArgs e)
        {
            //CheckAuth();


        }

        private void SetValues()
        {
            while (true)
            {
                CheckAuth();
                Thread.Sleep(3);
            }
        }


        private void CheckAuth()
        {


            if (!Check())
            {
                SignIn();
                isLoggedIn = false;
                LoginButton.Invoke(()=>(LoginButton.Text = "Unauthorized"));
                LoginButton.Invoke(() => (LoginButton.BackColor = Color.Crimson));
            }
            else
            {
                isLoggedIn = true;
                LoginButton.Invoke(() => (LoginButton.Text = "Authorized"));
                LoginButton.Invoke(() => (LoginButton.BackColor = Color.LawnGreen));
            }
        }

        static readonly string Api = "https://192.168.100.246:10001/api/api";
        static readonly string Authority = "https://192.168.100.246:10001";
        static readonly string ClientID = "client_id_mvc";
        static readonly string ClientSecret = "client_secret_mvc";
        static readonly string Scopes = "openid profile offline_access";

        private static OidcClient _oidcClient;
        private static HttpClient _apiClient = new HttpClient { BaseAddress = new Uri(Api) };
        private static RefreshTokenResult _refreshResult;


        private static void SignIn()
        {
            var browser = new SystemBrowser(port: 2001);
            string redirectUri = string.Format($"http://localhost:{browser.Port}/signin-oidc");

            // create or retrieve stored proof key
            var proofKey = GetProofKey();

            var options = new OidcClientOptions
            {
                ClientSecret = ClientSecret,

                Authority = Authority,
                ClientId = ClientID,
                RedirectUri = redirectUri,
                Scope = Scopes,
                FilterClaims = false,
                Browser = browser,
                Policy = new Policy()
                {
                    Discovery = new DiscoveryPolicy() { RequireHttps = false }
                }
            };

            options.ConfigureDPoP(proofKey);

            var serilog = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message}{NewLine}{Exception}{NewLine}", theme: AnsiConsoleTheme.Code)
                .CreateLogger();

            options.LoggerFactory.AddSerilog(serilog);

            _oidcClient = new OidcClient(options);

            LoginResult result = null;
            if (File.Exists("refresh_token"))
            {
                UseStoredToken();
            }
            else
            {
                StoreNewToken();
            }

            NextSteps();
        }

        private static async Task StoreNewToken()
        {
            LoginResult result;
            result = await _oidcClient.LoginAsync(new LoginRequest());
            _refreshResult = await _oidcClient.RefreshTokenAsync(result.RefreshToken);
            Console.WriteLine("store refresh token");
            File.WriteAllText("refresh_token", _refreshResult.RefreshToken);

            _apiClient = new HttpClient(result.RefreshTokenHandler)
            {
                BaseAddress = new Uri(Api)
            };
        }

        private static async Task RefreshToken()
        {
            _refreshResult = await _oidcClient.RefreshTokenAsync(_refreshResult.RefreshToken);
            File.WriteAllText("refresh_token", _refreshResult.RefreshToken);
        }

        private static void UseStoredToken()
        {
            Console.WriteLine("using stored refresh token");

            var refreshToken = File.ReadAllText("refresh_token");
            _refreshResult = _oidcClient.RefreshTokenAsync(refreshToken).GetAwaiter().GetResult();

            if (_refreshResult.IsError)
                StoreNewToken().GetAwaiter().GetResult();

            var handler = _oidcClient.CreateDPoPHandler(GetProofKey(), _refreshResult.RefreshToken);

            _apiClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(Api)
            };


        }

        private static string GetProofKey()
        {
            if (File.Exists("proofkey"))
            {
                Console.WriteLine("using stored proof key");
                return File.ReadAllText("proofkey");
            }

            Console.WriteLine("creating and storing proof key");
            var proofKey = JsonWebKeys.CreateRsaJson();
            File.WriteAllText("proofkey", proofKey);
            return proofKey;
        }

        private static string TryGetClaim(UserInfoResult result, string claimType)
        { 
            return result.Claims.FirstOrDefault(x => x.Type.Equals(claimType))!.Value;
        }

        private static async Task NextSteps()
        {
            //var menu = "  x...exit  c...call api   ";

            //while (true)
            //{
            //    Console.WriteLine("\n\n");

            //    Console.Write(menu);
            //    var key = Console.ReadKey();

            //    if (key.Key == ConsoleKey.X) return;
            //    if (key.Key == ConsoleKey.C) await Check(_refreshResult.RefreshToken);
            //}
        }

        private static bool Check()
        {
            if (_refreshResult != null)
                RefreshToken().GetAwaiter().GetResult();
            else
            {
                SignIn();
            }

            var userInfo = _oidcClient.GetUserInfoAsync(_refreshResult.AccessToken).GetAwaiter().GetResult();

            if (userInfo.IsError)
                if (userInfo.Error.Contains("Unauthorized"))
                    SignIn();

            TryGetClaim(userInfo, JwtClaimTypes.Name);
            return bool.Parse(TryGetClaim(userInfo, JwtClaimTypes.EmailVerified));
        }
    }
}