// =================================================================
// Archivo: Crear.js (Versión Final con Pestañas Bloqueadas y Zoom de Mapa)
// =================================================================

document.addEventListener('DOMContentLoaded', function () {

    // --- CAMBIO: Coordenadas y zoom actualizados según tu petición ---
    const LATITUD_INICIAL = 25.528995205680427;
    const LONGITUD_INICIAL = -103.33246842026712;
    const ZOOM_INICIAL = 17; // Un zoom más cercano para ver detalles
    let mapa;
    let marker;

    const mapaContainer = document.getElementById('mapa');
    if (mapaContainer) {
        // Usamos las nuevas constantes para inicializar el mapa
        mapa = L.map(mapaContainer).setView([LATITUD_INICIAL, LONGITUD_INICIAL], ZOOM_INICIAL);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '© OpenStreetMap contributors'
        }).addTo(mapa);

        mapa.on('click', e => {
            const { lat, lng } = e.latlng;
            if (marker) {
                marker.setLatLng(e.latlng);
            } else {
                marker = L.marker(e.latlng).addTo(mapa);
            }
            document.getElementById('Latitud').value = lat;
            document.getElementById('Longitud').value = lng;
        });
    }

    // --- CAMBIO: Bloqueamos las pestañas 2 y 3 al cargar la página ---
    document.getElementById('responsable-tab').classList.add('disabled');
    document.getElementById('estatus-tab').classList.add('disabled');


    // --------------------------------------
    // FUNCIONES PARA VALIDACIÓN Y NAVEGACIÓN
    // --------------------------------------
    function validarTabActual(tabId) {
        const tab = document.querySelector(tabId);
        if (!tab) return false;

        let isTabValid = true;
        const inputs = tab.querySelectorAll('input:not([readonly]), select, textarea');

        for (const input of inputs) {
            let isFieldValid = true;
            let errorMessage = '';

            const isRequired = input.hasAttribute('data-val-required');
            if (isRequired && !input.value) {
                isFieldValid = false;
                errorMessage = input.getAttribute('data-val-required');
            } else {
                if (!input.checkValidity()) {
                    isFieldValid = false;
                    errorMessage = input.validationMessage;
                }
            }

            if (isFieldValid) {
                if (input.type === 'date' && new Date(input.value).getFullYear() <= 1) {
                    isFieldValid = false;
                    errorMessage = 'Por favor, selecciona una fecha válida.';
                }
                if (input.name === 'Presupuesto' && parseFloat(input.value) <= 0) {
                    isFieldValid = false;
                    errorMessage = input.getAttribute('data-val-range') || 'El valor debe ser mayor a 0.';
                }
            }

            const errorSpan = document.querySelector(`span[data-valmsg-for="${input.name}"]`);
            if (isFieldValid) {
                input.classList.remove('is-invalid');
                input.classList.add('is-valid');
                if (errorSpan) errorSpan.textContent = '';
            } else {
                input.classList.remove('is-valid');
                input.classList.add('is-invalid');
                if (errorSpan) errorSpan.textContent = errorMessage;
                isTabValid = false;
            }
        }
        return isTabValid;
    }

    function goToTab(tabId) {
        const tabTriggerEl = document.querySelector(tabId + '-tab');
        if (tabTriggerEl) {
            new bootstrap.Tab(tabTriggerEl).show();
        }

        const tabs = ['#proyecto', '#responsable', '#estatus'];
        const index = tabs.indexOf(tabId);
        const porcentaje = [33, 66, 100][index];
        const label = `Paso ${index + 1} de 3`;
        const barra = document.getElementById('barraProgreso');

        if (barra) {
            barra.style.width = porcentaje + '%';
            barra.textContent = label;
        }
    }


    // --------------------------------------
    // MANEJO DE EVENTOS (LISTENERS)
    // --------------------------------------
    const form = document.getElementById('formularioProyecto');
    if (form) {
        form.addEventListener('submit', handleFormSubmit);
    }

    document.getElementById('btnSiguiente')?.addEventListener('click', () => {
        if (validarTabActual('#proyecto')) {
            // --- CAMBIO: Desbloqueamos la siguiente pestaña antes de movernos ---
            document.getElementById('responsable-tab').classList.remove('disabled');
            goToTab('#responsable');
        }
    });

    document.getElementById('btnAnterior')?.addEventListener('click', () => {
        goToTab('#proyecto');
    });

    document.getElementById('btnSiguienteEstatus')?.addEventListener('click', () => {
        if (validarTabActual('#responsable')) {
            // --- CAMBIO: Desbloqueamos la siguiente pestaña antes de movernos ---
            document.getElementById('estatus-tab').classList.remove('disabled');
            goToTab('#estatus');
        }
    });

    document.getElementById('btnAnteriorR')?.addEventListener('click', () => {
        goToTab('#responsable');
    });

    const estatusTab = document.getElementById('estatus-tab');
    if (estatusTab) {
        estatusTab.addEventListener('shown.bs.tab', function () {
            setTimeout(() => mapa?.invalidateSize(), 10);
        });
    }

    // --------------------------------------
    // FUNCIÓN PRINCIPAL DE ENVÍO
    // --------------------------------------
    function handleFormSubmit(event) {
        event.preventDefault();

        const esProyectoValido = validarTabActual('#proyecto');
        const esResponsableValido = validarTabActual('#responsable');
        const esEstatusValido = validarTabActual('#estatus');

        if (!esProyectoValido || !esResponsableValido || !esEstatusValido) {
            Swal.fire("Formulario Incompleto", "Por favor, revisa todas las pestañas y corrige los campos marcados en rojo.", "error");
            return;
        }

        const formData = new FormData(form);

        fetch('/Registro/Crear', {
            method: 'POST',
            body: formData,
        })
            .then(response => {
                if (!response.ok) {
                    return response.json().then(err => { throw new Error(err.message || 'Error del servidor') });
                }
                return response.json();
            })
            .then(data => {
                if (data.success) {
                    Swal.fire({
                        title: '¡Proyecto Guardado!',
                        text: data.message,
                        icon: 'success'
                    }).then(() => {
                        form.reset();
                        form.querySelectorAll('.is-valid, .is-invalid').forEach(el => el.classList.remove('is-valid', 'is-invalid'));
                        form.querySelectorAll('span[data-valmsg-for]').forEach(span => span.textContent = '');
                        if (marker) mapa.removeLayer(marker);

                        // --- CAMBIO: Volvemos a bloquear las pestañas después de guardar ---
                        document.getElementById('responsable-tab').classList.add('disabled');
                        document.getElementById('estatus-tab').classList.add('disabled');

                        goToTab('#proyecto');
                    });
                } else {
                    Swal.fire('Error de Validación', data.message, 'error');
                }
            })
            .catch(error => {
                console.error('Error en fetch:', error);
                Swal.fire('Error de Conexión', `No se pudo comunicar con el servidor. ${error.message}`, 'error');
            });
    }
});