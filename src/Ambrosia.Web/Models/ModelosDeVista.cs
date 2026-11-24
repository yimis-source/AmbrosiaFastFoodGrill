using System.ComponentModel.DataAnnotations;
using Ambrosia.Nucleo.Estructuras;
using Ambrosia.Nucleo.Modelos;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Ambrosia.Web.Models;

// MODELOS DE VISTA (ViewModels): llevan a cada vista exactamente lo que necesita.
// [BindNever]/[ValidateNever] marcan datos que solo se muestran y no vienen del formulario.

public class LoginViewModel
{
    [Required(ErrorMessage = "Escriba su usuario.")]
    [Display(Name = "Usuario")]
    public string Usuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escriba su contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Clave { get; set; } = string.Empty;

    [Display(Name = "Mantener la sesión iniciada")]
    public bool Recordarme { get; set; }

    public string? ReturnUrl { get; set; }
}

public class CartaViewModel
{
    public List<GrupoMenu> Grupos { get; set; } = new();

    /// <summary>Productos sin inventario suficiente (HashSet para preguntar Contains en O(1)).</summary>
    public HashSet<int> Agotados { get; set; } = new();
}

public class CrearPedidoViewModel
{
    [BindNever, ValidateNever]
    public List<GrupoMenu> Grupos { get; set; } = new();

    [BindNever, ValidateNever]
    public HashSet<int> Agotados { get; set; } = new();

    [Display(Name = "Tipo de pedido")]
    public TipoPedido Tipo { get; set; } = TipoPedido.Mesa;

    [Range(1, 30, ErrorMessage = "La mesa debe estar entre 1 y 30.")]
    public int? Mesa { get; set; }

    [Display(Name = "Nombre del cliente")]
    [StringLength(80, ErrorMessage = "Máximo 80 caracteres.")]
    public string? NombreCliente { get; set; }

    [Display(Name = "Dirección")]
    [StringLength(150, ErrorMessage = "Máximo 150 caracteres.")]
    public string? Direccion { get; set; }

    [Display(Name = "Teléfono")]
    [StringLength(20, ErrorMessage = "Máximo 20 caracteres.")]
    public string? Telefono { get; set; }

    [Display(Name = "Método de pago")]
    public MetodoPago MetodoPago { get; set; } = MetodoPago.Efectivo;

    [StringLength(250, ErrorMessage = "Máximo 250 caracteres.")]
    public string? Observaciones { get; set; }

    /// <summary>Cantidad por producto. En el HTML: name="Cantidades[IdProducto]".</summary>
    public Dictionary<int, int> Cantidades { get; set; } = new();

    /// <summary>Nota por producto ("sin cebolla"). En el HTML: name="Notas[IdProducto]".</summary>
    public Dictionary<int, string?> Notas { get; set; } = new();

    /// <summary>Convierte el formulario en la solicitud que entiende la biblioteca.</summary>
    public SolicitudPedido ComoSolicitud() => new()
    {
        Tipo = Tipo,
        MetodoPago = MetodoPago,
        Mesa = Mesa,
        NombreCliente = NombreCliente,
        Direccion = Direccion,
        Telefono = Telefono,
        Observaciones = Observaciones,
        Items = Cantidades
            .Where(par => par.Value > 0)
            .Select(par => new ItemSolicitado(par.Key, par.Value, Notas.GetValueOrDefault(par.Key)))
            .ToList()
    };
}

public class PedidosIndexViewModel
{
    public List<Pedido> Pedidos { get; set; } = new();
    public EstadoPedido? Estado { get; set; }
    public DateOnly Fecha { get; set; }
    public decimal TotalVendido => Pedidos.Where(p => p.Estado != EstadoPedido.Cancelado).Sum(p => p.Total);
}

public class CocinaViewModel
{
    public ColaCocina Cola { get; set; } = new ColaCocina(new List<Pedido>());
    public List<Pedido> EnPreparacion { get; set; } = new();
    public List<Pedido> Listos { get; set; } = new();
    public DateTime Ahora { get; set; } = DateTime.Now;
}

public class ProductoDetalleViewModel
{
    public Producto Producto { get; set; } = new();
    public List<Insumo> Insumos { get; set; } = new();
}

public class IngredienteFormulario
{
    public int ProductoId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un insumo.")]
    public int InsumoId { get; set; }

    [Range(typeof(decimal), "0.01", "100000", ErrorMessage = "La cantidad debe ser mayor que cero.",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal Cantidad { get; set; }
}

public class InsumosIndexViewModel
{
    public List<Insumo> Insumos { get; set; } = new();
    public List<Insumo> Alertas { get; set; } = new();
    public decimal ValorInventario => Insumos.Sum(i => i.ValorEnBodega);
}

public class UsuarioFilaViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public string? Email { get; set; }
    public IList<string> Roles { get; set; } = new List<string>();
    public bool Bloqueado { get; set; }
}

public class CrearUsuarioViewModel
{
    [Required(ErrorMessage = "El usuario es obligatorio.")]
    [RegularExpression("^[a-zA-Z0-9._-]{3,30}$", ErrorMessage = "Entre 3 y 30 caracteres: letras, números, punto, guion.")]
    public string Usuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Escriba un correo válido.")]
    [Display(Name = "Correo")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(50, MinimumLength = 6, ErrorMessage = "Mínimo 6 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Clave { get; set; } = string.Empty;

    [Compare(nameof(Clave), ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar contraseña")]
    public string ConfirmarClave { get; set; } = string.Empty;

    [Required(ErrorMessage = "Seleccione un rol.")]
    public string Rol { get; set; } = string.Empty;
}

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
