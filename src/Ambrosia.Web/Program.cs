using System.Globalization;
using Ambrosia.Nucleo.Datos;
using Ambrosia.Nucleo.Servicios;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// Cultura invariante para leer números de los formularios siempre igual ("0.5" = medio),
// sin importar la configuración regional del computador. Los montos se muestran en formato
// colombiano con la extensión ComoPesos() de la biblioteca.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------- Base de datos (EF Core)
// Si appsettings.json no trae cadena, se usa el mismo archivo SQLite que el escritorio.
var cadena = ConfiguracionBaseDatos.Resolver(builder.Configuration.GetConnectionString("Ambrosia"));
builder.Services.AddDbContext<AmbrosiaDbContext>(opciones => opciones.UseSqlite(cadena));

// ---------------------------------------------------------------- ASP.NET Core Identity
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
    opciones.ExpireTimeSpan = TimeSpan.FromHours(10);   // un turno de trabajo
    opciones.SlidingExpiration = true;
});

// ---------------------------------------------------------------- Servicios de la biblioteca
// "Scoped": una instancia por petición HTTP, compartiendo el mismo DbContext.
builder.Services.AddScoped<ServicioInventario>();
builder.Services.AddScoped<ServicioPedidos>();
builder.Services.AddScoped<ServicioCatalogo>();
builder.Services.AddScoped<ServicioReportes>();

// ---------------------------------------------------------------- MVC (mensajes en español)
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

// ---------------------------------------------------------------- Crear BD y datos iniciales
using (var alcance = app.Services.CreateScope())
{
    var db = alcance.ServiceProvider.GetRequiredService<AmbrosiaDbContext>();
    await InicializadorBaseDatos.InicializarAsync(db);
}

// ---------------------------------------------------------------- Tubería HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();   // ¿quién es? (cookie de Identity)
app.UseAuthorization();    // ¿qué puede hacer? ([Authorize(Roles = ...)])

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
