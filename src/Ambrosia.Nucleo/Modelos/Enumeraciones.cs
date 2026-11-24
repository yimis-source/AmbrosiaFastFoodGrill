namespace Ambrosia.Nucleo.Modelos;

// Las enumeraciones limitan los valores posibles y evitan "cadenas mágicas".
// El texto amigable que ven los usuarios ("En preparación", "Para llevar"…) está
// en los métodos de extensión Texto() de Utilidades/Extensiones.cs.

/// <summary>Ciclo de vida de un pedido dentro del restaurante.</summary>
public enum EstadoPedido
{
    Pendiente,
    EnPreparacion,
    Listo,
    Entregado,
    Cancelado
}

/// <summary>Cómo recibe el cliente su pedido.</summary>
public enum TipoPedido
{
    Mesa,
    ParaLlevar,
    Domicilio
}

/// <summary>Medios de pago aceptados en Ambrosía.</summary>
public enum MetodoPago
{
    Efectivo,
    Tarjeta,
    Nequi,
    Daviplata
}

/// <summary>Unidad en la que se controla cada insumo del inventario.</summary>
public enum UnidadMedida
{
    Gramo,
    Mililitro,
    Unidad
}
