using ControlBalanza.Data;
using ControlBalanza.Models;
using ControlBalanza.Services;
using ControlBalanza.Services.Cpe;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- MVC: toda la app requiere login salvo lo marcado con [AllowAnonymous] (login de Identity, página de error) ---
builder.Services.AddControllersWithViews(options =>
{
    // Mensajes de validación de binding en español
    var m = options.ModelBindingMessageProvider;
    m.SetValueMustBeANumberAccessor(campo => $"El campo {campo} debe ser un número.");
    m.SetValueMustNotBeNullAccessor(valor => $"El valor '{valor}' no es válido.");
    m.SetAttemptedValueIsInvalidAccessor((valor, campo) => $"El valor '{valor}' no es válido para {campo}.");
    m.SetValueIsInvalidAccessor(valor => $"El valor '{valor}' no es válido.");
    m.SetMissingBindRequiredValueAccessor(campo => $"Falta el valor de {campo}.");
    m.SetMissingKeyOrValueAccessor(() => "Falta un valor obligatorio.");
    m.SetUnknownValueIsInvalidAccessor(campo => $"El valor ingresado no es válido para {campo}.");
    m.SetNonPropertyAttemptedValueIsInvalidAccessor(valor => $"El valor '{valor}' no es válido.");
    m.SetNonPropertyUnknownValueIsInvalidAccessor(() => "El valor ingresado no es válido.");
    m.SetNonPropertyValueMustBeANumberAccessor(() => "Debe ser un número.");
});
builder.Services.AddRazorPages();
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
});

// Angular manda el token antiforgery en este header (lo lee de un <meta> que renderiza la vista Playon)
builder.Services.AddAntiforgery(options => options.HeaderName = "X-XSRF-TOKEN");

// --- Base de datos ---
builder.Services.AddDbContext<AgroDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DbConnection")));

// --- Identity ---
builder.Services.AddDefaultIdentity<Usuario>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<AgroDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true; // Renueva la cookie si el usuario sigue activo
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";

    // Para la API (Angular) se devuelve 401/403 en lugar de redirigir al login
    options.Events.OnRedirectToLogin = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api")) ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        else ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api")) ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        else ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
});

// --- Servicios de negocio ---
builder.Services.Configure<BalanzaOptions>(builder.Configuration.GetSection(BalanzaOptions.Seccion));
builder.Services.Configure<AzureOpenAIOptions>(builder.Configuration.GetSection(AzureOpenAIOptions.Seccion));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IPesajeWorkflowService, PesajeWorkflowService>();
builder.Services.AddScoped<CpeIngresoService>();
builder.Services.AddScoped<ICpeExtractorIA, CpeExtractorIA>();
builder.Services.AddScoped<ReportesService>();

var app = builder.Build();

// --- Migraciones y datos iniciales ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AgroDbContext>();
        var userManager = services.GetRequiredService<UserManager<Usuario>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        await DbSeeder.Seed(context, userManager, roleManager, app.Environment.IsDevelopment());
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error aplicando migraciones / datos iniciales.");
        throw; // Sin base no tiene sentido levantar la app: mejor ver el error en la consola
    }
}

// --- Pipeline HTTP ---

// Formato argentino para fechas y números (45.120 kg, miércoles 28/10/2026)
var cultura = new System.Globalization.CultureInfo("es-AR");
app.UseRequestLocalization(new Microsoft.AspNetCore.Builder.RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(cultura),
    SupportedCultures = new[] { cultura },
    SupportedUICultures = new[] { cultura }
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Sin registro público: los usuarios los crea un administrador (Administración → Usuarios)
app.Use(async (ctx, next) =>
{
    var path = ctx.Request.Path;
    if (path.StartsWithSegments("/Identity/Account/Register") ||
        path.StartsWithSegments("/Identity/Account/RegisterConfirmation") ||
        path.StartsWithSegments("/Identity/Account/ExternalLogin"))
    {
        ctx.Response.Redirect("/Identity/Account/Login");
        return;
    }
    await next();
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Los archivos estáticos (css/js) no requieren login: la pantalla de ingreso los necesita
app.MapStaticAssets().AllowAnonymous();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages();
app.Run();
