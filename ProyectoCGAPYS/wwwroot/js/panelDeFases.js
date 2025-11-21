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
    // --- 2. LÓGICA QUE SE EJECUTA CUANDO SE ARRASTRA Y SUELTA UNA TARJETA ---
    async function handleDragEnd(event) {
        const tarjeta = event.item;
        const columnaDestino = event.to;
        const columnaOrigen = event.from;

        const proyectoId = tarjeta.dataset.proyectoId;
        const faseOrigenId = parseInt(columnaOrigen.parentElement.id.split('-').pop());
        const faseDestinoId = parseInt(columnaDestino.parentElement.id.split('-').pop());

        if (Math.abs(faseDestinoId - faseOrigenId) !== 1) {
            event.from.appendChild(tarjeta);
            Swal.fire('Movimiento Inválido', 'Un proyecto solo puede moverse a la fase contigua.', 'error');
            return;
        }

        if (faseDestinoId === faseOrigenId + 1) {
            registrarCambioPendiente(proyectoId, faseDestinoId, "Avance", "Movimiento de fase en el panel Kanban.");
            actualizarVisibilidadBotones(tarjeta, faseDestinoId);
            actualizarEnlaceDetalles(tarjeta, faseDestinoId);
            // ----> LÍNEA NUEVA: Activamos la animación
            tarjeta.classList.add('card-pending-changes');
        }
        else if (faseDestinoId === faseOrigenId - 1) {
            const { value: comentario, isConfirmed } = await Swal.fire({
                title: 'Devolver a Fase Anterior',
                input: 'textarea',
                inputLabel: 'Motivo de la devolución',
                inputPlaceholder: 'Escribe aquí por qué se devuelve el proyecto...',
                showCancelButton: true,
                confirmButtonText: 'Registrar y Devolver',
                cancelButtonText: 'Cancelar',
                allowOutsideClick: false,
                inputValidator: (value) => {
                    if (!value || value.trim() === '') {
                        return '¡Necesitas escribir un motivo para la devolución!'
                    }
                }
            });

            if (isConfirmed && comentario) {
                registrarCambioPendiente(proyectoId, faseDestinoId, "Retroceso", comentario);
                actualizarVisibilidadBotones(tarjeta, faseDestinoId);
                actualizarEnlaceDetalles(tarjeta, faseDestinoId);
                // ----> LÍNEA NUEVA: Activamos la animación
                tarjeta.classList.add('card-pending-changes');
            } else {
                columnaOrigen.appendChild(tarjeta);
            }
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

                            cambiosPendientes.forEach(cambio => {
                                const tarjetaMovida = document.getElementById(`proyecto-${cambio.proyectoId}`);
                                if (tarjetaMovida) {
                                    // ----> AÑADE ESTA LÍNEA <----
                                    tarjetaMovida.classList.remove('card-pending-changes');
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

    board.addEventListener('click', async function (event) {
        const targetButton = event.target.closest('button');
        if (!targetButton || event.target.classList.contains('resize-handle')) return;

        const card = targetButton.closest('.kanban-card');
        if (!card) return;

        const proyectoId = card.dataset.proyectoId;
        const columnaOrigenEl = card.parentElement;
        const faseColumnaOrigenEl = columnaOrigenEl.parentElement;
        const faseOrigenId = parseInt(faseColumnaOrigenEl.id.split('-').pop());

        // Lógica para avanzar fase
        if (targetButton.classList.contains('btn-avanzar-fase')) {
            const faseDestinoId = faseOrigenId + 1;
            const columnaDestinoEl = document.querySelector(`#fase-col-${faseDestinoId} .kanban-cards-container`);

            if (columnaDestinoEl) {
                // Mover visualmente la tarjeta
                columnaDestinoEl.appendChild(card);
                // Registrar el cambio pendiente
                registrarCambioPendiente(proyectoId, faseDestinoId, "Avance", "Avance de fase mediante botón.");
                // Actualizar UI de la tarjeta en su nueva posición
                actualizarVisibilidadBotones(card, faseDestinoId);
                actualizarEnlaceDetalles(card, faseDestinoId);
                // Añadir el resaltado persistente
                card.classList.add('card-pending-changes');
            }
        }

        // Lógica para rechazar fase
        if (targetButton.classList.contains('btn-rechazar-fase')) {
            const faseDestinoId = faseOrigenId - 1;
            const columnaDestinoEl = document.querySelector(`#fase-col-${faseDestinoId} .kanban-cards-container`);

            if (columnaDestinoEl) {
                // Pedir el motivo del rechazo ANTES de moverla
                const { value: comentario, isConfirmed } = await Swal.fire({
                    title: 'Rechazar Avance',
                    input: 'textarea',
                    inputLabel: 'Motivo del rechazo',
                    showCancelButton: true,
                    confirmButtonText: 'Registrar Rechazo',
                    inputValidator: (value) => !value && 'El comentario no puede estar vacío.'
                });

                if (isConfirmed && comentario) {
                    // Si el usuario confirma, ahora sí la movemos
                    columnaDestinoEl.appendChild(card);
                    registrarCambioPendiente(proyectoId, faseDestinoId, "Retroceso", comentario);
                    actualizarVisibilidadBotones(card, faseDestinoId);
                    actualizarEnlaceDetalles(card, faseDestinoId);
                    card.classList.add('card-pending-changes');
                }
            }
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
