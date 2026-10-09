// Lógica de la pantalla "Nueva venta": arma las líneas y muestra un resumen.
// El servidor vuelve a validar precios, stock e impuesto al guardar.
(function () {
    var dataEl = document.getElementById('catalogo-data');
    var app = document.getElementById('venta-app');
    if (!dataEl || !app) return;

    var catalogo = JSON.parse(dataEl.textContent || '[]');
    var previas = JSON.parse(document.getElementById('lineas-data').textContent || '[]');
    var impuesto = parseFloat(app.dataset.impuesto) || 0;

    var select = document.getElementById('item-select');
    var cantidad = document.getElementById('item-cantidad');
    var btnAgregar = document.getElementById('btn-agregar');
    var tbody = document.querySelector('#tabla-lineas tbody');
    var hidden = document.getElementById('lineas-hidden');
    var form = document.getElementById('venta-form');
    var msg = document.getElementById('venta-msg');

    var lineas = [];

    function fmt(n) {
        return n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }
    function buscar(tipo, id) {
        return catalogo.find(function (c) { return c.tipo === tipo && c.id === id; });
    }
    function mostrarError(texto) {
        msg.textContent = texto;
        msg.classList.toggle('d-none', !texto);
    }

    // Opciones del selector agrupadas por tipo.
    ['Servicio', 'Producto'].forEach(function (tipo) {
        var items = catalogo.filter(function (c) { return c.tipo === tipo; });
        if (!items.length) return;
        var grupo = document.createElement('optgroup');
        grupo.label = tipo === 'Servicio' ? 'Servicios' : 'Productos';
        items.forEach(function (c) {
            var op = document.createElement('option');
            op.value = c.tipo + '|' + c.id;
            op.textContent = c.nombre + ' - RD$ ' + fmt(c.precio) + (c.stock != null ? ' (disp. ' + c.stock + ')' : '');
            grupo.appendChild(op);
        });
        select.appendChild(grupo);
    });

    function celda(texto, clase) {
        var td = document.createElement('td');
        td.textContent = texto;
        if (clase) td.className = clase;
        return td;
    }

    function pintar() {
        tbody.innerHTML = '';
        hidden.innerHTML = '';
        var subtotal = 0;

        lineas.forEach(function (l, i) {
            var item = buscar(l.tipo, l.itemId);
            if (!item) return;
            var sub = item.precio * l.cantidad;
            subtotal += sub;

            var tr = document.createElement('tr');
            tr.appendChild(celda(item.nombre));
            tr.appendChild(celda(l.tipo));
            tr.appendChild(celda(String(l.cantidad), 'text-end'));
            tr.appendChild(celda(fmt(item.precio), 'text-end'));
            tr.appendChild(celda(fmt(sub), 'text-end'));

            var tdBtn = document.createElement('td');
            tdBtn.className = 'text-end';
            var b = document.createElement('button');
            b.type = 'button';
            b.className = 'btn btn-sm btn-outline-danger';
            b.innerHTML = '<i class="bi bi-trash"></i>';
            b.title = 'Quitar';
            b.addEventListener('click', function () { lineas.splice(i, 1); pintar(); });
            tdBtn.appendChild(b);
            tr.appendChild(tdBtn);
            tbody.appendChild(tr);

            [['Tipo', l.tipo], ['ItemId', l.itemId], ['Cantidad', l.cantidad]].forEach(function (par) {
                var inp = document.createElement('input');
                inp.type = 'hidden';
                inp.name = 'Lineas[' + i + '].' + par[0];
                inp.value = par[1];
                hidden.appendChild(inp);
            });
        });

        if (!lineas.length) {
            var tr0 = document.createElement('tr');
            var td0 = celda('Aún no hay artículos en la venta.', 'text-center text-secondary py-4');
            td0.colSpan = 6;
            tr0.appendChild(td0);
            tbody.appendChild(tr0);
        }

        var imp = Math.round(subtotal * impuesto) / 100;
        document.getElementById('r-subtotal').textContent = fmt(subtotal);
        document.getElementById('r-impuesto').textContent = fmt(imp);
        document.getElementById('r-total').textContent = fmt(subtotal + imp);
    }

    function agregar(tipo, id, cant) {
        var item = buscar(tipo, id);
        if (!item) return;
        if (!Number.isInteger(cant) || cant < 1) { mostrarError('La cantidad debe ser un número entero mayor que cero.'); return; }

        var existente = lineas.find(function (l) { return l.tipo === tipo && l.itemId === id; });
        var total = (existente ? existente.cantidad : 0) + cant;
        if (item.stock != null && total > item.stock) {
            mostrarError('Stock insuficiente para "' + item.nombre + '": disponible ' + item.stock + '.');
            return;
        }
        mostrarError('');
        if (existente) existente.cantidad = total;
        else lineas.push({ tipo: tipo, itemId: id, cantidad: cant });
        pintar();
    }

    btnAgregar.addEventListener('click', function () {
        if (!select.value) { mostrarError('Seleccione un servicio o producto.'); return; }
        var partes = select.value.split('|');
        agregar(partes[0], parseInt(partes[1], 10), parseInt(cantidad.value, 10));
        cantidad.value = 1;
    });

    form.addEventListener('submit', function (e) {
        if (!lineas.length) {
            e.preventDefault();
            mostrarError('Agregue al menos un artículo a la venta.');
        }
    });

    // Restaura las líneas si el servidor devolvió el formulario con errores.
    previas.forEach(function (l) {
        var tipo = (l.tipo === 2 || l.tipo === 'Producto') ? 'Producto' : 'Servicio';
        agregar(tipo, l.itemId, l.cantidad);
    });
    pintar();
})();
