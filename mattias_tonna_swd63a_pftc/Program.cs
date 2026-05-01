using System.Security.Claims;
using mattias_tonna_swd63a_pftc.DataAccess;
using mattias_tonna_swd63a_pftc.interfaces;
using mattias_tonna_swd63a_pftc.services;
using Microsoft.AspNetCore.Authentication.Cookies; 
using Microsoft.AspNetCore.Authentication.Google; 

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

Environment.SetEnvironmentVariable(
    "GOOGLE_APPLICATION_CREDENTIALS",
    builder.Configuration["Authentication:Google:CredentialsPath"]
);

var secretManager = new GoogleSecretManagerService(builder.Configuration["Authentication:Google:ProjectId"],
    builder.Services.BuildServiceProvider().GetRequiredService<ILogger<GoogleSecretManagerService>>());
await secretManager.LoadSecretsIntoConfigurationAsync(builder.Configuration);


builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
}).AddCookie().AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.Scope.Add("profile");
    options.Events.OnCreatingTicket = (context) =>
    {
        var email = context.User.GetProperty("email").GetString();
        var picture = context.User.GetProperty("picture").GetString();
        if (!string.IsNullOrEmpty(email))
            context.Identity?.AddClaim(new Claim("email", email));

        if (!string.IsNullOrEmpty(picture))
            context.Identity?.AddClaim(new Claim("picture", picture));

        return Task.CompletedTask;
    };
    options.Events.OnRemoteFailure = context =>
    {
        context.HandleResponse();
        context.Response.Redirect("/?loginCancelled=true");
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization();

builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient();
builder.Services.AddScoped<MenuParsingService>();
builder.Services.AddScoped<VisionOcrService>();
builder.Services.AddScoped<MenuRepository>();
builder.Services.AddScoped<PubSubService>();
builder.Services.AddScoped<IBucketStorageService, BucketStorageService>();

var app = builder.Build();



// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();