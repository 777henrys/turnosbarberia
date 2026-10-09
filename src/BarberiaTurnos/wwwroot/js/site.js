// Confirmación antes de operaciones destructivas: <form data-confirm="¿Seguro?">
document.addEventListener('submit', function (e) {
    var form = e.target;
    var mensaje = form.getAttribute('data-confirm');
    if (mensaje && !window.confirm(mensaje)) {
        e.preventDefault();
        return;
    }
    // Evita doble envío accidental.
    if (form.hasAttribute('data-once') && !e.defaultPrevented) {
        form.querySelectorAll('button[type=submit]').forEach(function (b) { b.disabled = true; });
    }
});
