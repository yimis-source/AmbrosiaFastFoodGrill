// Arranque de la web: base de datos, Identity, servicios del Núcleo y rutas MVC.
using System.Globalization;
using Ambrosia.Nucleo.Datos;
using Ambrosia.Nucleo.Servicios;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

var cadena = ConfiguracionBaseDatos.Resolver(builder.Configuration.GetConnectionString("Ambrosia"));
builder.Services.AddDbContext<AmbrosiaDbContext>(opciones => opciones.UseSqlite(cadena));

builder.Services
    .AddIdentity<IdentityUser, IdentityRole>(opciones =>
    {
        opciones.Password.RequiredLength = 6;
        opciones.User.RequireUniqueEmail = true;
        opciones.Lockout.MaxFailedAccessAttempts = 5;
        opciones.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        opciones.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<AmbrosiaDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(opciones =>
{
    opciones.LoginPath = "/Cuenta/Login";
    opciones.LogoutPath = "/Cuenta/Salir";
    opciones.AccessDeniedPath = "/Cuenta/AccesoDenegado";
    opciones.ExpireTimeSpan = TimeSpan.FromHours(10);
    opciones.SlidingExpiration = true;
});

builder.Services.AddScoped<ServicioInventario>();
builder.Services.AddScoped<ServicioPedidos>();
builder.Services.AddScoped<ServicioCatalogo>();
builder.Services.AddScoped<ServicioReportes>();

builder.Services.AddControllersWithViews(opciones =>
{
    var mensajes = opciones.ModelBindingMessageProvider;
    mensajes.SetValueMustNotBeNullAccessor(_ => "Este campo es obligatorio.");
    mensajes.SetMissingBindRequiredValueAccessor(campo => $"Falta el valor de «{campo}».");
    mensajes.SetAttemptedValueIsInvalidAccessor((valor, campo) => $"«{valor}» no es un valor válido para «{campo}».");
    mensajes.SetNonPropertyAttemptedValueIsInvalidAccessor(valor => $"«{valor}» no es un valor válido.");
    mensajes.SetUnknownValueIsInvalidAccessor(campo => $"El valor no es válido para «{campo}».");
    mensajes.SetValueIsInvalidAccessor(valor => $"«{valor}» no es válido.");
    mensajes.SetValueMustBeANumberAccessor(campo => $"«{campo}» debe ser un número.");
});

var app = builder.Build();

using (var alcance = app.Services.CreateScope())
{
    var db = alcance.ServiceProvider.GetRequiredService<AmbrosiaDbContext>();
    await InicializadorBaseDatos.InicializarAsync(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
