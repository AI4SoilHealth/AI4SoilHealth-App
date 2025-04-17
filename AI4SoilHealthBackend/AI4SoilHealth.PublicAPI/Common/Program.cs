
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using System.Net.WebSockets;
using AspNetCoreRateLimit;
using PublicAPI.Common.Filters;
using PublicAPI.Common.Services;
using PublicAPI.Specific.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Cryptography;

namespace PublicAPI.Common {
    public class Program {

        public static void Main(string[] args) {

            var builder = WebApplication.CreateBuilder(args);
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Limits.MaxRequestBodySize = 104857600; 
            });
            builder.Logging.ClearProviders();
            // Serilog.Debugging.SelfLog.Enable(msg => System.Diagnostics.Debug.WriteLine(msg));
            var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                                //.Enrich.WithDynamicProperty("person_id", () => { return "33"; })
                                .Enrich.FromLogContext()
                                //.Enrich.WithProperty("TenantId", builder.Configuration.GetValue<int>("TenantId"))
                                //.Enrich.WithProperty("AppId", builder.Configuration.GetValue<int>("AppId"))
                                //.Enrich.WithMachineName()
                                //.Enrich.WithEnrichedProperties()
                                .CreateLogger();
            builder.Logging.AddSerilog(logger);
            builder.Host.UseSerilog(logger);

            builder.Services.AddControllers(
                options => options.Filters.Add<AuthFilter>(1)
            );

            //builder.Services.AddMemoryCache();
            builder.Services.AddDistributedMemoryCache();
            builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
            //builder.Services.AddInMemoryRateLimiting();
            builder.Services.AddDistributedRateLimiting();

            builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

            builder.Services.AddTransient<Db>();

            ServiceAdder.AddSpecificServices(builder);

            builder.Services.AddTransient<Misc>();
            builder.Services.AddScoped<AuthService>();
            builder.Services.AddScoped<CurrentUserInfo>();
            builder.Services.AddHostedService<CacheExpirationService>();
            builder.Services.AddTransient<ChatHandler>();
            builder.Services.AddTransient<MailService>();

            builder.Services.Configure<AppOptions>(options =>
            {
                options.ConnectionString = builder.Configuration.GetConnectionString("Data");

                options.GoogleClientId = builder.Configuration["GoogleClientId"];
                options.SecretKey = builder.Configuration["SecretKey"];
                options.Issuer = builder.Configuration["Issuer"];
                options.Audience = builder.Configuration["Audience"];

                options.KeycloakAuthority = builder.Configuration["Keycloak:Authority"];
                options.KeycloakAudience = builder.Configuration["Keycloak:Audience"];

                options.GoogleAPI = builder.Configuration["GoogleAPI"];
            });

            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .WithExposedHeaders("Content-Disposition", "X-Suggested-Filename", "WWW-Authenticate", "Access-Control-Allow-Origin");
                });
            });
            builder.Services.AddResponseCaching();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });


            // where websocket authentication is needed
            var publicKey = builder.Configuration["Keycloak:PublicKey"];
            RsaSecurityKey issuerSigningKey = null;
            if (publicKey != null) {
                var rsa = RSA.Create();
                rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKey), out _);
                issuerSigningKey = new RsaSecurityKey(rsa);
            }

            JwtBearerOptions jwtBearerOptions = new JwtBearerOptions
            {
                Authority = builder.Configuration["Keycloak:Authority"],
                Audience = builder.Configuration["Keycloak:Audience"],
                TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["Keycloak:Authority"],
                    ValidateAudience = false,
                    ValidAudience = builder.Configuration["Keycloak:Audience"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = issuerSigningKey,
                    ValidateLifetime = true,
                    RequireExpirationTime = true
                },
                Events = new JwtBearerEvents()
                {
                    OnAuthenticationFailed = c =>
                    {
                        c.NoResult();
                        c.Response.StatusCode = 500;
                        c.Response.ContentType = "text/plain";
                        {
                            return c.Response.WriteAsync(c.Exception.ToString());
                        }
                    }
                }
            };
            builder.Services.AddSingleton(jwtBearerOptions);

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                // Configure JwtBearerOptions here
                options.Authority = jwtBearerOptions.Authority;
                options.Audience = jwtBearerOptions.Audience;
                options.TokenValidationParameters = jwtBearerOptions.TokenValidationParameters;
                options.Events = jwtBearerOptions.Events;
            });

            builder.Services.AddAuthorization(options => { });

            var app = builder.Build();
            ServiceAdder.AddSpecificThings(app);

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });
            app.UseCors();
            app.UseResponseCaching();

            app.UseDefaultFiles();
#if DEBUG
            app.UseHttpsRedirection();
#endif
            app.UseStaticFiles();

            app.UseSerilogRequestLogging();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseIpRateLimiting();

            app.UseMiddleware<HeaderMiddleware>();

            if (app.Environment.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.MapControllers();
            app.MapFallbackToFile("index.html");

            AppContext.SetSwitch("Npgsql.EnableStoredProcedureCompatMode", true);
            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

            app.UseWebSockets();

            app.MapGet("/ws/{taskId}", async (HttpContext context, string taskId) =>
            {
                if (context.WebSockets.IsWebSocketRequest) {
                    using WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    await WebSocketHandler.Handler(webSocket, taskId);
                } else {
                    Log.Error("Websocket error: {@Headers}", context.Request.Headers);
                    context.Response.StatusCode = 400;
                }
            });

            app.MapGet("/chat/{roomId}", async (HttpContext context, int roomId) =>
            {
                if (context.WebSockets.IsWebSocketRequest) {
                    using WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    using var scope = app.Services.CreateScope();
                    var chatHandler = scope.ServiceProvider.GetRequiredService<ChatHandler>();
                    await chatHandler.Handler(webSocket, roomId);
                } else {
                    Log.Error("Websocket error: {@Headers}", context.Request.Headers);
                    context.Response.StatusCode = 400;
                }
            });

            app.Run();

        }
    }
}
