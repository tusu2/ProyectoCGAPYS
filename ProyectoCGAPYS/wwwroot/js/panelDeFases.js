// ============== wwwroot/js/panelDeFases.js (VERSIÓN FINAL COMPLETA) ==============

document.addEventListener('DOMContentLoaded', () => {
    const board = document.querySelector('.kanban-board');
    if (!board) return;
    const confirmBar = document.getElementById('confirm-bar');
    const btnConfirmarCambios = document.getElementById('btn-confirmar-cambios');
    const cardContainers = board.querySelectorAll('.kanban-cards-container');
    let cambiosPendientes = []; // Aquí guardaremos los movimientos
   
    // --- 1. CONFIGURAMOS LAS COLUMNAS PARA SER "ARRASTRABLES" ---
  
    const columnas = board.querySelectorAll('.kanban-cards-container');
    columnas.forEach(columna => {
        const faseColumnaId = parseInt(columna.parentElement.id.split('-').pop());

        new Sortable(columna, {
            group: {
                name: 'proyectos',
                // ¡LA MAGIA! Esta función decide si se puede sacar una tarjeta de ESTA columna.
                pull: function () {
                    // Se permite si el usuario es Jefa (permiso -1)
                    // O si el permiso del usuario coincide con el ID de esta columna.
                    return permisoFaseUsuario === -1 || permisoFaseUsuario === faseColumnaId;
                }
            },
            animation: 150,
            onEnd: handleDragEnd,
        });
    });
    // --- 2. LÓGICA QUE SE EJECUTA CUANDO SE ARRASTRA Y SUELTA UNA TARJETA ---
    async function handleDragEnd(event) {
        const tarjeta = event.item;
        const columnaDestino = event.to;
        const columnaOrigen = event.from;

        const proyectoId = tarjeta.dataset.proyectoId;
        const faseOrigenId = parseInt(columnaOrigen.parentElement.id.split('-').pop());
        const faseDestinoId = parseInt(columnaDestino.parentElement.id.split('-').pop());
        // Validamos el movimiento (solo a fases contiguas)
        if (Math.abs(faseDestinoId - faseOrigenId) !== 1) {
            event.from.appendChild(tarjeta);
            Swal.fire('Movimiento Inválido', 'Un proyecto solo puede moverse a la fase contigua.', 'error');
            return;
        }

        // Si el movimiento es válido, actualizamos la visibilidad de los botones INMEDIATAMENTE
    
        // CASO 1: Es un AVANCE (movimiento hacia adelante)
        if (faseDestinoId === faseOrigenId + 1) {
            registrarCambioPendiente(proyectoId, faseDestinoId, "Avance", "Movimiento de fase en el panel Kanban.");
            actualizarVisibilidadBotones(tarjeta, faseDestinoId);
            actualizarEnlaceDetalles(tarjeta, faseDestinoId);
        }
        // CASO 2: Es un RETROCESO (movimiento hacia atrás)
        else if (faseDestinoId === faseOrigenId - 1) {
            // Primero, devolvemos la tarjeta a su sitio original mientras pedimos el comentario.
            columnaOrigen.appendChild(tarjeta);

            // Pedimos el motivo del rechazo con SweetAlert
            const { value: comentario } = await Swal.fire({
                title: 'Devolver a Fase Anterior',
                input: 'textarea',
                inputLabel: 'Motivo de la devolución',
                inputPlaceholder: 'Escribe aquí por qué se devuelve el proyecto...',
                showCancelButton: true,
                confirmButtonText: 'Registrar y Devolver',
                cancelButtonText: 'Cancelar'
            });

            // Si el usuario escribió un comentario y confirmó...
            if (comentario) {
                // ...movemos la tarjeta programáticamente a la columna anterior...
                columnaDestino.appendChild(tarjeta);
                // ...y registramos el cambio con el comentario.
                registrarCambioPendiente(proyectoId, faseDestinoId, "Retroceso", comentario);
            }
        }
        // CASO 3: Es un MOVIMIENTO INVÁLIDO
        else {
            columnaOrigen.appendChild(tarjeta);
            Swal.fire('Movimiento Inválido', 'Un proyecto solo puede moverse a la fase contigua.', 'error');
        }
    }

   
    function registrarCambioPendiente(proyectoId, nuevaFaseId, tipo, comentario) {
        const confirmBar = document.getElementById('confirm-bar');

        // Evitamos duplicados y actualizamos si ya existía
        cambiosPendientes = cambiosPendientes.filter(p => p.proyectoId !== proyectoId);
        cambiosPendientes.push({
            proyectoId: proyectoId,
            nuevaFaseId: nuevaFaseId,
            tipo: tipo,
            comentario: comentario
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
                    // ¡LA CORRECCIÓN ESTÁ AQUÍ! Nos aseguramos de recibir el parámetro 'data'.
                    .then(data => {
                        if (data.success) {
                            Swal.fire('¡Guardado!', data.message, 'success');

                            // Actualizamos la "memoria" de cada tarjeta que se movió.
                            cambiosPendientes.forEach(cambio => {
                                const tarjetaMovida = document.getElementById(`proyecto-${cambio.proyectoId}`);
                                if (tarjetaMovida) {
                                    tarjetaMovida.dataset.faseOrigenId = cambio.nuevaFaseId;
                                }
                            });

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

    function actualizarVisibilidadBotones(tarjeta, faseId) {
        const actionsDiv = tarjeta.querySelector('.card-actions');
        if (!actionsDiv) return;

        let mostrarBotones = false;
        // La misma lógica de permisos que teníamos en Razor, pero ahora en JavaScript
        if (userRoles.includes('Jefa')) {
            mostrarBotones = true;
        } else if (userRoles.includes('Empleado1') && faseId === 1) {
            mostrarBotones = true;
        } else if (userRoles.includes('Empleado2') && faseId === 2) {
            mostrarBotones = true;
        } else if (userRoles.includes('Empleado3') && faseId === 3) {
            mostrarBotones = true;
        }

        // Mostramos u ocultamos el div de los botones
        actionsDiv.style.display = mostrarBotones ? 'flex' : 'none';
    }
    function actualizarEnlaceDetalles(tarjeta, faseId) {
        const link = tarjeta.querySelector('.card-link');
        if (!link) return;

        let permitirClick = false;
        if (userRoles.includes('Jefa')) {
            permitirClick = true;
        } else if (userRoles.includes('Empleado1') && faseId === 1) {
            permitirClick = true;
        } else if (userRoles.includes('Empleado2') && faseId === 2) {
            permitirClick = true;
        } else if (userRoles.includes('Empleado3') && faseId === 3) {
            permitirClick = true;
        }

        if (permitirClick) {
            link.classList.remove('link-disabled');
        } else {
            link.classList.add('link-disabled');
        }
    }
});


// --- FUNCIÓN UTILITARIA PARA MANEJAR RESPUESTA DEL SERVIDOR ---
// También la llamamos después de guardar los cambios
function handleServerResponse(data, cardElement) {
    if (data.success) {
        Swal.fire('¡Éxito!', data.message, 'success');
        const nuevaColumna = document.querySelector(`#fase-col-${data.nuevaFaseId} .kanban-cards-container`);
        if (nuevaColumna) {
            nuevaColumna.appendChild(cardElement);
            actualizarVisibilidadBotones(cardElement, data.nuevaFaseId);
            actualizarEnlaceDetalles(cardElement, data.nuevaFaseId); // <-- ¡NUEVA LLAMADA!
        }
    } else {
        Swal.fire('Error', data.message, 'error');
    }
}