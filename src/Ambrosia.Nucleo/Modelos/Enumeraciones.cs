// Enumeraciones del dominio: estado y tipo de pedido, método de pago y unidad de medida.
namespace Ambrosia.Nucleo.Modelos;

public enum EstadoPedido
{
    Pendiente,
    EnPreparacion,
    Listo,
    Entregado,
    Cancelado
}

public enum TipoPedido
{
    Mesa,
    ParaLlevar,
    Domicilio
}

public enum MetodoPago
{
    Efectivo,
    Tarjeta,
    Nequi,
    Daviplata
}

public enum UnidadMedida
{
    Gramo,
    Mililitro,
    Unidad
}
