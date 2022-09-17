using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WebServer;
using WebServer.Models.WebServerDB;
using WebServer.Services;

// Program.cs �u�|����@��


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Set ConnectionString
builder.Services.AddDbContext<WebServerDBContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("WebServerDB"));
});

// Set Session (Server)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Set Cookie (Client)
builder.Services
    .AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        //�s���Q���������
        options.AccessDeniedPath = new PathString("/Account/Signin");
        //�n�J��
        options.LoginPath = new PathString("/Account/Signin");
        //�n�X��
        options.LogoutPath = new PathString("/Account/Signout");
    }).AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // SignalR doesn't appear to have the bearer header
                // The JWT is added as a query string when using the JS token factory on the SignalR JS Api
                // JS API: new HubConnectionBuilder().withUrl(connectionUrl, {accessTokenFactory: () => getMyJwtToken()})
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken)) {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            },
            /** The following hooks are very handy for debugging */
            //OnChallenge = context => Task.CompletedTask,
            //OnAuthenticationFailed = context => Task.CompletedTask,
            //OnForbidden = context => Task.CompletedTask,
            //OnTokenValidated = context => Task.CompletedTask
        };

        options.TokenValidationParameters = new TokenValidationParameters
        {
            //https://github.com/IdentityServer/IdentityServer3/issues/1251 �w�]�O5����, �S�]�w�p��5���������L��
            // �ɶ����� (�ݭn�]�p��5�����ɭn�]�w)
            ClockSkew = TimeSpan.Zero,
            // �z�L�o���ŧi�A�N�i�H�q "sub" ���Ȩó]�w�� User.Identity.Name
            NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
            // �z�L�o���ŧi�A�N�i�H�q "roles" ���ȡA�åi�� [Authorize] �P�_����
            RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",

            // ñ�o��
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration.GetValue<string>("JwtSettings:Issuer"),

            // ������
            ValidateAudience = true,
            ValidAudience = builder.Configuration.GetValue<string>("JwtSettings:Audience"),

            // �@��ڭ̳��|���� Token �����Ĵ���
            ValidateLifetime = true,

            //ñ��
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(builder.Configuration.GetValue<string>("JwtSettings:SignKey"))),
        };
    });

// Set �h��y�t
builder.Services.AddLocalization();
builder.Services.AddControllersWithViews()
    //�b cshtml ���ϥΦh��y��
    .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
    //�b Model ���ϥΦh��y��
    .AddDataAnnotationsLocalization(
    options =>
    {
        options.DataAnnotationLocalizerProvider = (type, factory) =>
        factory.Create(typeof(Resource));
    });

// Set Service
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<SiteService>();
builder.Services.AddScoped<ValidatorService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<IViewRenderService, ViewRenderService>();
builder.Services.AddScoped<WebServer.Filters.AuthorizeFilter>();
builder.Services.AddScoped<JWTService>();

// �n�`�J���A�ȭn�g�b�����e
var app = builder.Build();

// �n��DI�w�`�J���A�ȡA�ҥH�g�b.Build()��
ServiceActivator.Configure(app.Services);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// �ҥΦh��y�t (��b UseStaticFiles �ҥ��R�A�������U)
using (var serviceScope = ServiceActivator.GetScope()) {
    // �q DI Container ���o Servic
    SiteService siteService = (SiteService)serviceScope.ServiceProvider.GetService(typeof(SiteService));
    var cultures = siteService?.GetCultures();

    var localizationOptions = new RequestLocalizationOptions()
        .SetDefaultCulture(cultures[0])//�w�]��
        .AddSupportedCultures(cultures)
        .AddSupportedUICultures(cultures);
    localizationOptions.RequestCultureProviders = new List<IRequestCultureProvider>
        {
            new QueryStringRequestCultureProvider(),
            new CookieRequestCultureProvider(),
            new AcceptLanguageHeaderRequestCultureProvider(),
        };
    app.UseRequestLocalization(localizationOptions);
}

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

// add Session (Server)
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();