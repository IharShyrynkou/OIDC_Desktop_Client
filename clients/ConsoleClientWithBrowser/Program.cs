using IdentityModel.OidcClient;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog.Sinks.SystemConsole.Themes;
using IdentityModel;
using System.Linq;

namespace ConsoleClientWithBrowser
{
    public class Program
    {
        static readonly string _api = "http://localhost:10001/device";
        static readonly string Authority = "http://192.168.100.246:62863";
        static readonly string ClientID = "client_secret_mvc";

        static OidcClient _oidcClient;
        static HttpClient _apiClient = new HttpClient { BaseAddress = new Uri(_api) };

        public static async Task Main()
        {
            Console.WriteLine("+-----------------------+");
            Console.WriteLine("|  Sign in with OIDC    |");
            Console.WriteLine("+-----------------------+");
            Console.WriteLine("");
            Console.WriteLine("Press any key to sign in...");
            Console.ReadKey();

            await SignIn();
        }

        private static async Task SignIn()
        {
            // create a redirect URI using an available port on the loopback address.
            // requires the OP to allow random ports on 127.0.0.1 - otherwise set a static port
            var browser = new SystemBrowser(port: 2001);

            string redirectUri = string.Format($"http://localhost:{browser.Port}/signin-oidc");

            var options = new OidcClientOptions
            {
                ClientSecret = "client_secret_mvc",

                Authority = Authority,
                ClientId = "client_id_mvc",
                RedirectUri = redirectUri,
                Scope = "openid profile offline_access",
                FilterClaims = false,
                Browser = browser,
            };

            var serilog = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message}{NewLine}{Exception}{NewLine}", theme: AnsiConsoleTheme.Code)
                .CreateLogger();

            options.LoggerFactory.AddSerilog(serilog);

            _oidcClient = new OidcClient(options);
            var result = await _oidcClient.LoginAsync(new LoginRequest());

            _apiClient = new HttpClient(result.RefreshTokenHandler)
            {
                BaseAddress = new Uri(_api)
            };

            ShowResult(result);
            await NextSteps(result);
        }

        private static void ShowResult(LoginResult result)
        {
            if (result == null) return;

            if (result.IsError)
            {
                Console.WriteLine("\n\nError:\n{0}", result.Error);
                return;
            }
            //email_verified
            var isActivated = result.User.HasClaim(JwtClaimTypes.EmailVerified, "False");
            //Console.WriteLine("\n\nClaims:");
            //foreach (var claim in result.User.Claims)
            //{
            //    Console.WriteLine("{0}: {1}", claim.Type, claim.Value);
            //}

            TryShowCLaim(result, JwtClaimTypes.Name, "Name");
            TryShowCLaim(result, JwtClaimTypes.EmailVerified, "IsActivated");
            TryShowCLaim(result, JwtClaimTypes.Role, "Role");

            //var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(result.TokenResponse.Raw);
            //var res = ValidateToken(result.AccessToken, ClientID);

            //Console.WriteLine($"token response...");
            //foreach (var item in values)
            //{
            //    Console.WriteLine($"{item.Key}: {item.Value}");
            //}
        }

        private static void TryShowCLaim(LoginResult result, string claimType, string description)
        {
            if (result.User.Claims.Any(x => x.Type.Equals(claimType)))
                Console.WriteLine($"\n {description}: {result.User.Claims.First(x => x.Type.Equals(claimType)).Value}");
        }

        private static async Task NextSteps(LoginResult result)
        {
            var menu = "  x...exit  c...call api   ";
            
            while (true)
            {
                Console.WriteLine("\n\n");

                Console.Write(menu);
                var key = Console.ReadKey();

                if (key.Key == ConsoleKey.X) return;
                if (key.Key == ConsoleKey.C) await CallApi();
            }
        }

        private static async Task CallApi()
        {
            var response = await _apiClient.GetAsync("");


            if (response.IsSuccessStatusCode)
            {
                var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                Console.WriteLine("\n\n");
                Console.WriteLine(json.RootElement);
            }
            else
            {
                Console.WriteLine($"Error: {response.ReasonPhrase}");

            }
        }
    }
}