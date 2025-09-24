// ============== wwwroot/js/panelDeFases.js (VERSIÓN FINAL COMPLETA) ==============

document.addEventListener('DOMContentLoaded', () => {
    const board = document.querySelector('.kanban-board');
    if (!board) return;
    const confirmBar = document.getElementById('confirm-bar');
    const btnConfirmarCambios = document.getElementById('btn-confirmar-cambios');
    let cambiosPendientes = []; // Aquí guardaremos los movimientos

    // --- 1. CONFIGURAMOS LAS COLUMNAS PARA SER "ARRASTRABLES" ---
    const columnas = board.querySelectorAll('.kanban-cards-container');
    columnas.forEach(columna => {
        new Sortable(columna, {
            group: 'proyectos', // Permite arrastrar tarjetas entre columnas
            animation: 150,
            onEnd: handleDragEnd // Llamamos a nuestra función de lógica cuando se suelta una tarjeta
        });
    });

    // --- 2. LÓGICA QUE SE EJECUTA CUANDO SE ARRASTRA Y SUELTA UNA TARJETA ---
    function handleDragEnd(event) {
        const tarjeta = event.item;
        const columnaDestino = event.to;

        const proyectoId = tarjeta.dataset.proyectoId;
        const faseOrigenId = parseInt(event.from.parentElement.id.split('-').pop());
        const faseDestinoId = parseInt(columnaDestino.parentElement.id.split('-').pop());

        // --- VALIDACIÓN: SOLO A LA FASE SIGUIENTE ---
        if (faseDestinoId !== (faseOrigenId + 1)) {
            // Si el movimiento es inválido, lo devolvemos a su columna original
            event.from.appendChild(tarjeta);
            Swal.fire({
                icon: 'error',
                title: 'Movimiento Inválido',
                text: 'Un proyecto solo puede ser arrastrado a la fase inmediatamente siguiente.',
            });
            return;
        }

        // Si el movimiento es válido, lo registramos como un cambio pendiente
        console.log(`Proyecto ${proyectoId} movido a fase ${faseDestinoId}`);
        // Evitamos duplicados
        cambiosPendientes = cambiosPendientes.filter(p => p.proyectoId !== proyectoId);
        cambiosPendientes.push({
            proyectoId: proyectoId,
            nuevaFaseId: faseDestinoId
        });

        // Mostramos la barra de confirmación
        confirmBar.style.display = 'flex';
        confirmBar.querySelector('span').textContent = `Tienes ${cambiosPendientes.length} cambio(s) sin guardar.`
    }

    // --- 3. LÓGICA PARA EL BOTÓN DE CONFIRMAR CAMBIOS ---
    btnConfirmarCambios.addEventListener('click', () => {
        if (cambiosPendientes.length === 0) return;

        Swal.fire({
            title: '¿Confirmar Cambios?',
            text: `Se guardarán los ${cambiosPendientes.length} movimientos de fase que has realizado.`,
            icon: 'info',
            showCancelButton: true,
            confirmButtonText: 'Sí, ¡guardar todo!'
        }).then(result => {
            if (result.isConfirmed) {
                fetch('/PanelDeFases/GuardarCambiosDeFase', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(cambiosPendientes)
                })
                    .then(response => response.json())
                    .then(data => {
                        if (data.success) {
                            Swal.fire('¡Guardado!', data.message, 'success');
                            cambiosPendientes = []; // Limpiamos los cambios pendientes
                            confirmBar.style.display = 'none'; // Ocultamos la barra
                        } else {
                            Swal.fire('Error', data.message, 'error');
                        }
                    });
            }
        });
    });

    board.addEventListener('click', function (event) {
        const targetButton = event.target.closest('button');
        // Si no se hizo clic en un botón, o si el clic fue en la manija de resize, no hacemos nada.
        if (!targetButton || event.target.classList.contains('resize-handle')) return;

        const card = targetButton.closest('.kanban-card');
        if (!card) return;

        const proyectoId = card.dataset.proyectoId;

        // Lógica para avanzar fase
        if (targetButton.classList.contains('btn-avanzar-fase')) {
            Swal.fire({ title: '¿Avanzar proyecto?', text: "El proyecto se moverá a la siguiente fase.", icon: 'question', showCancelButton: true, confirmButtonText: 'Sí, ¡avanzar!' })
                .then(result => {
                    if (result.isConfirmed) {
                        fetch(`/PanelDeFases/AvanzarFaseProyecto?proyectoId=${proyectoId}`, { method: 'POST' })
                            .then(response => response.json())
                            .then(data => handleServerResponse(data, card));
                    }
                });
        }

        // Lógica para rechazar fase
        if (targetButton.classList.contains('btn-rechazar-fase')) {
            Swal.fire({ title: 'Rechazar Avance', input: 'textarea', inputLabel: 'Motivo del rechazo', showCancelButton: true, confirmButtonText: 'Registrar Rechazo' })
                .then(result => {
                    if (result.isConfirmed && result.value) {
                        const comentario = encodeURIComponent(result.value);
                        fetch(`/PanelDeFases/RechazarFase?proyectoId=${proyectoId}&comentario=${comentario}`, { method: 'POST' })
                            .then(response => response.json())
                            .then(data => handleServerResponse(data, card));
                    } else if (result.isConfirmed) {
                        Swal.fire('Error', 'El comentario no puede estar vacío.', 'warning');
                    }
                });
        }
    });

    // --- PARTE 2: LÓGICA PARA REDIMENSIONAR COLUMNAS ---
    let isResizing = false;
    let activeColumn = null;
    let startX = 0;
    let startWidth = 0;

    board.addEventListener('mousedown', (e) => {
        // Solo activamos la redimensión si el clic inicial es en una "manija"
        if (e.target.classList.contains('resize-handle')) {
            isResizing = true;
            activeColumn = e.target.parentElement;
            startX = e.pageX;
            startWidth = activeColumn.offsetWidth;
            // La siguiente línea evita que se seleccione texto mientras arrastramos. ¡Esta sí la dejamos!
            document.body.style.userSelect = 'none';
        }
    });

    document.addEventListener('mousemove', (e) => {
        if (!isResizing) return;

        // Prevenimos el comportamiento por defecto para un arrastre más fluido
        e.preventDefault();

        const newWidth = startWidth + (e.pageX - startX);
        const minWidth = 340;
        const maxWidth = 1500;

        if (newWidth >= minWidth && newWidth <= maxWidth) {
            // Esta línea es la que hace la magia en tiempo real
            activeColumn.style.flexBasis = `${newWidth}px`;
        }
    });

    document.addEventListener('mouseup', () => {
        if (isResizing) {
            isResizing = false;
            // Restauramos la selección de texto
            document.body.style.userSelect = '';

            // ¡IMPORTANTE! Ya NO limpiamos el ancho de la columna activa.
            // Esto hace que el nuevo tamaño se quede guardado.
        }
    });

    new Sortable(board, {
        animation: 150,      // Duración de la animación en ms
        handle: '.drag-handle', // Especifica que solo se puede arrastrar desde el título
        ghostClass: 'blue-background-class' // Opcional: una clase para el "fantasma" mientras arrastras
    });
});


// --- FUNCIÓN UTILITARIA PARA MANEJAR RESPUESTA DEL SERVIDOR ---
function handleServerResponse(data, cardElement) {
    if (data.success) {
        Swal.fire('¡Éxito!', data.message, 'success');
        const nuevaColumna = document.querySelector(`#fase-col-${data.nuevaFaseId} .kanban-cards-container`);
        if (nuevaColumna) {
            nuevaColumna.appendChild(cardElement);
        }
    } else {
        Swal.fire('Error', data.message, 'error');
    }
}