// Pantalla "Nuevo pedido": botones +/−, campos según el tipo de pedido y total en vivo.
(function () {
    const formulario = document.getElementById('formPedido');
    if (!formulario) return;

    const porcentajeMesa = parseFloat(formulario.dataset.porcentajeMesa);
    const tarifaDomicilio = parseFloat(formulario.dataset.tarifaDomicilio);
    const minimoGratis = parseFloat(formulario.dataset.minimoGratis);

    const pesos = valor => '$ ' + Math.round(valor).toLocaleString('es-CO');

    function tipoSeleccionado() {
        const marcado = formulario.querySelector('input[name="Tipo"]:checked');
        return marcado ? marcado.value : 'Mesa';
    }

    function actualizarCampos() {
        const tipo = tipoSeleccionado();
        formulario.querySelectorAll('.solo-mesa').forEach(e => e.classList.toggle('d-none', tipo !== 'Mesa'));
        formulario.querySelectorAll('.solo-cliente').forEach(e => e.classList.toggle('d-none', tipo === 'Mesa'));
        formulario.querySelectorAll('.solo-domicilio').forEach(e => e.classList.toggle('d-none', tipo !== 'Domicilio'));
        recalcular();
    }

    function calcularCargo(tipo, subtotal) {
        if (tipo === 'Mesa') return { nombre: 'Servicio (10 %)', valor: Math.round(subtotal * porcentajeMesa) };
        if (tipo === 'Domicilio') return { nombre: 'Domicilio', valor: subtotal >= minimoGratis || subtotal === 0 ? 0 : tarifaDomicilio };
        return { nombre: 'Sin cargo', valor: 0 };
    }

    function recalcular() {
        let subtotal = 0, unidades = 0;
        const lista = document.getElementById('listaResumen');
        lista.innerHTML = '';

        formulario.querySelectorAll('.fila-producto').forEach(fila => {
            const campo = fila.querySelector('.campo-cantidad');
            const cantidad = Math.max(0, parseInt(campo.value || '0', 10) || 0);
            const precio = parseFloat(fila.dataset.precio);
            fila.classList.toggle('seleccionado', cantidad > 0);
            fila.querySelector('.campo-nota').classList.toggle('d-none', cantidad === 0);
            if (cantidad > 0) {
                subtotal += cantidad * precio;
                unidades += cantidad;
                const li = document.createElement('li');
                li.className = 'd-flex justify-content-between';
                li.innerHTML = `<span>${cantidad} × ${fila.dataset.nombre}</span><span>${pesos(cantidad * precio)}</span>`;
                lista.appendChild(li);
            }
        });

        if (unidades === 0) lista.innerHTML = '<li class="text-muted">Aún no hay productos.</li>';

        const cargo = calcularCargo(tipoSeleccionado(), subtotal);
        document.getElementById('txtSubtotal').textContent = pesos(subtotal);
        document.getElementById('txtCargoNombre').textContent = cargo.nombre;
        document.getElementById('txtCargo').textContent = pesos(cargo.valor);
        document.getElementById('txtTotal').textContent = pesos(subtotal + cargo.valor);
        document.getElementById('txtTotalMovil').textContent = pesos(subtotal + cargo.valor);
        document.getElementById('txtUnidades').textContent = unidades;
    }

    formulario.addEventListener('click', evento => {
        const boton = evento.target.closest('.btn-mas, .btn-menos');
        if (!boton) return;
        const campo = boton.closest('.fila-producto').querySelector('.campo-cantidad');
        const actual = parseInt(campo.value || '0', 10) || 0;
        const nuevo = boton.classList.contains('btn-mas') ? Math.min(50, actual + 1) : Math.max(0, actual - 1);
        campo.value = nuevo;
        recalcular();
    });

    formulario.addEventListener('input', evento => {
        if (evento.target.classList.contains('campo-cantidad')) recalcular();
    });

    formulario.querySelectorAll('input[name="Tipo"]').forEach(r => r.addEventListener('change', actualizarCampos));

    actualizarCampos();
})();
