// Cierra automáticamente los mensajes de éxito después de unos segundos.
document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('.alert-success.alert-dismissible').forEach(alerta => {
        setTimeout(() => {
            if (window.bootstrap) bootstrap.Alert.getOrCreateInstance(alerta).close();
        }, 6000);
    });
});
