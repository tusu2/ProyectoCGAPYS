// /js/site.js

// --- INICIO DE CÓDIGO PARA DEPURACIÓN ---

// Seleccionamos todos los enlaces dentro del sidebar
const menuLinks = document.querySelectorAll('.sidebar .menu-link');
function handleLinkClick(event) {
    menuLinks.forEach(link => {
        link.classList.remove('active');
    });
    this.classList.add('active');
}
menuLinks.forEach(link => {
    link.addEventListener('click', handleLinkClick);
});

let mapa;
let todosLosProyectos = [];
let marcadoresEnMapa = [];

if (document.getElementById('mapa-principal')) {
    // Esta parte se ejecuta solo en la página de búsqueda.
    // inicializarLogicaIndex() se llama desde Index.cshtml
}

function inicializarMapa() {
    if (mapa) {
        mapa.remove();
    }
    const latitudInicial = 25.5428;
    const longitudInicial = -103.4068;
    mapa = L.map('mapa-principal').setView([latitudInicial, longitudInicial], 13);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
    }).addTo(mapa);
}

async function cargarYMostrarProyectos(listaProyectosUl) {
    try {
        const response = await fetch('/Proyecto/GetProyectos');
        if (!response.ok) throw new Error(`Error al obtener los proyectos: ${response.statusText}`);
        todosLosProyectos = await response.json();
        renderizarProyectos(todosLosProyectos, listaProyectosUl);
    } catch (error) {
        console.error("No se pudieron cargar los proyectos:", error);
        if (listaProyectosUl) {
            listaProyectosUl.innerHTML = '<li class="no-projects">Error al cargar los proyectos.</li>';
        }
    }
}

function renderizarProyectos(proyectos, listaProyectosUl) {
    if (!listaProyectosUl) return;
    const modal = document.getElementById('modal-proyecto');
    const modalDetalles = document.getElementById('modal-detalles');
    const modalNombreProyecto = document.getElementById('modal-nombre-proyecto');

    marcadoresEnMapa.forEach(marker => mapa.removeLayer(marker));
    marcadoresEnMapa = [];
    listaProyectosUl.innerHTML = '';

    if (proyectos.length === 0) {
        listaProyectosUl.innerHTML = '<li class="no-projects">No se encontraron proyectos que coincidan.</li>';
    } else {
        proyectos.forEach(proyecto => {
            crearMarcadorEnMapa(proyecto, modal, modalNombreProyecto, modalDetalles);
            crearElementoEnLista(proyecto, listaProyectosUl, modal, modalNombreProyecto, modalDetalles);
        });
    }
}

function crearMarcadorEnMapa(proyecto, modal, modalNombreProyecto, modalDetalles) {
    const lat = parseFloat(proyecto.latitud);
    const lon = parseFloat(proyecto.longitud);
    if (!isNaN(lat) && !isNaN(lon)) {
        const defaultIcon = new L.Icon.Default();
        const marker = L.marker([lat, lon], { icon: defaultIcon }).addTo(mapa);
        marker.on('click', () => mostrarModalConDetalles(proyecto, modal, modalNombreProyecto, modalDetalles));
        marcadoresEnMapa.push(marker);
    }
}

function aplicarFiltros(listaProyectosUl) {
    console.log("PASO 3: ✅ Función aplicarFiltros() EJECUTADA. Verificando elementos...");

    // Modo detective: Verificamos cada elemento uno por uno.
    const filtroNombreEl = document.getElementById('filtro-nombre');
    const filtroCampusEl = document.getElementById('filtro-campus');
    const filtroFechaInicioEl = document.getElementById('filtro-fecha-inicio');
    const filtroFechaFinEl = document.getElementById('filtro-fecha-fin');

    // Reporte del detective en la consola
    console.log(`- Elemento 'filtro-nombre':`, filtroNombreEl ? '✔️ Encontrado' : '❌ NO ENCONTRADO');
    console.log(`- Elemento 'filtro-dependencia':`, filtroCampusEl ? '✔️ Encontrado' : '❌ NO ENCONTRADO');
    console.log(`- Elemento 'filtro-fecha-inicio':`, filtroFechaInicioEl ? '✔️ Encontrado' : '❌ NO ENCONTRADO');
    console.log(`- Elemento 'filtro-fecha-fin':`, filtroFechaFinEl ? '✔️ Encontrado' : '❌ NO ENCONTRADO');

    if (!filtroNombreEl || !filtroCampusEl || !filtroFechaInicioEl || !filtroFechaFinEl) {
        console.log("🛑 Saliendo de la función porque falta un elemento.");
        return;
    }

    const nombreValor = filtroNombreEl.value;
    const campusIdValor = filtroCampusEl.value;
    const fechaInicioValor = filtroFechaInicioEl.value;
    const fechaFinValor = filtroFechaFinEl.value;
    const nombreNormalizado = normalizarTexto(nombreValor);

    console.log(`PASO 4: 🕵️‍♀️ Filtrando con el texto: "${nombreNormalizado}". Proyectos totales: ${todosLosProyectos.length}`);

    const proyectosFiltrados = todosLosProyectos.filter(proyecto => {
        const matchNombre = normalizarTexto(proyecto.nombreProyecto).includes(nombreNormalizado);
        const matchCampus = !campusIdValor || proyecto.idCampusFk == campusIdValor; 
        const fechaProyecto = new Date(proyecto.fechaSolicitud);
        const matchFechaInicio = !fechaInicioValor || fechaProyecto >= new Date(fechaInicioValor);
        const matchFechaFin = !fechaFinValor || fechaProyecto <= new Date(fechaFinValor);

        return matchNombre && matchCampus && matchFechaInicio && matchFechaFin;
    });

    console.log(`PASO 5: 🏁 Filtro terminado. Proyectos encontrados: ${proyectosFiltrados.length}`);
    renderizarProyectos(proyectosFiltrados, listaProyectosUl);
}

function limpiarFiltros(listaProyectosUl) {
    const filtroNombreEl = document.getElementById('filtro-nombre');
    const filtroDependenciaEl = document.getElementById('filtro-dependencia');
    const filtroFechaInicioEl = document.getElementById('filtro-fecha-inicio');
    const filtroFechaFinEl = document.getElementById('filtro-fecha-fin');

    if (filtroNombreEl) filtroNombreEl.value = '';
    if (filtroDependenciaEl) filtroDependenciaEl.value = '';
    if (filtroFechaInicioEl) filtroFechaInicioEl.value = '';
    if (filtroFechaFinEl) filtroFechaFinEl.value = '';

    renderizarProyectos(todosLosProyectos, listaProyectosUl);
}

function crearElementoEnLista(proyecto, listaProyectosUl, modal, modalNombreProyecto, modalDetalles) {
    const listItem = document.createElement('li');
    listItem.innerHTML = `
        <div class="project-info">
            <div class="project-name">${proyecto.nombreProyecto}</div>
            <div class="project-id">Folio: ${proyecto.folio || 'N/D'}</div>
        </div>
        <span class="view-details-icon">➔</span>
    `;
    listItem.addEventListener('click', () => {
        const lat = parseFloat(proyecto.latitud);
        const lon = parseFloat(proyecto.longitud);
        if (!isNaN(lat) && !isNaN(lon)) {
            mapa.setView([lat, lon], 17);
        }
        mostrarModalConDetalles(proyecto, modal, modalNombreProyecto, modalDetalles);
    });
    listaProyectosUl.appendChild(listItem);
}

// En js/site.js

function mostrarModalConDetalles(proyecto) {
    // --- INICIA SECCIÓN DE DEPURACIÓN ---
    console.log("1. Intentando abrir modal para:", proyecto.nombreProyecto);

    const modal = document.getElementById('modal-proyecto');
    console.log("2. Buscando #modal-proyecto:", modal);

    const modalNombreProyecto = document.getElementById('modal-nombre-proyecto');
    console.log("3. Buscando #modal-nombre-proyecto:", modalNombreProyecto);

    const modalDetalles = document.getElementById('modal-detalles');
    console.log("4. Buscando #modal-detalles:", modalDetalles);

    if (!modal || !modalNombreProyecto || !modalDetalles) {
        console.error("¡ERROR! Uno o más elementos del modal no se encontraron en el HTML. La función se detuvo.");
        return; // Detenemos la ejecución aquí
    }
    console.log("5. ¡Éxito! Todos los elementos del modal fueron encontrados. Mostrando...");
    // --- TERMINA SECCIÓN DE DEPURACIÓN ---


    // --- Lógica para determinar el estado de la prioridad (sin cambios) ---
    let prioridadClass = 'priority-ninguna';
    let prioridadTexto = 'Sin Asignar';

    if (proyecto.prioridad === 'verde') {
        prioridadClass = 'priority-verde';
        prioridadTexto = 'Normal';
    } else if (proyecto.prioridad === 'amarillo') {
        prioridadClass = 'priority-amarillo';
        prioridadTexto = 'Media';
    } else if (proyecto.prioridad === 'rojo') {
        prioridadClass = 'priority-rojo';
        prioridadTexto = 'Urgente';
    }

    // Llenamos el contenido del modal (sin cambios)
    modalNombreProyecto.textContent = proyecto.nombreProyecto;
    modalDetalles.innerHTML = `
        <div class="priority-display">
            <div class="priority-indicator ${prioridadClass}"></div>
            <p>${prioridadTexto}</p>
        </div>
        <p><strong>Folio:</strong> ${proyecto.folio || 'No disponible'}</p>
        <p><strong>Campus:</strong> ${proyecto.campus}</p>
        <p><strong>Estatus:</strong> ${proyecto.estatus}</p>
        <p><strong>Responsable:</strong> ${proyecto.nombreResponsable}</p>
        <hr>
        <p><strong>Descripción:</strong> ${proyecto.descripcion}</p>
    `;

    // Mostramos el modal
    modal.classList.add('show');
}

function cerrarModal(modal) {
    if (modal) modal.classList.remove('show');
}

function inicializarLogicaIndex() {
    const listaProyectosUl = document.getElementById('lista-proyectos');
    const modal = document.getElementById('modal-proyecto');
    const modalNombreProyecto = document.getElementById('modal-nombre-proyecto');
    const modalDetalles = document.getElementById('modal-detalles');
    const cerrarModalBtn = document.querySelector('.cerrar-modal');
    const aplicarFiltrosBtn = document.getElementById('aplicar-filtros-btn');
    const limpiarFiltrosBtn = document.getElementById('limpiar-filtros-btn');
    const filtroNombreInput = document.getElementById('filtro-nombre');

    if (!listaProyectosUl) return; // Si no hay lista, no hacemos nada de esto.

    inicializarMapa();
    cargarYMostrarProyectos(listaProyectosUl);
    const filtrosDebounced = debounce(() => aplicarFiltros(listaProyectosUl), 400);

    if (cerrarModalBtn) {
        cerrarModalBtn.addEventListener('click', () => cerrarModal(modal));
    }
    if (modal) {
        modal.addEventListener('click', (event) => {
            if (event.target === modal) {
                cerrarModal(modal);
            }
        });
    }
    if (aplicarFiltrosBtn) {
        aplicarFiltrosBtn.addEventListener('click', () => aplicarFiltros(listaProyectosUl));
    }
    if (limpiarFiltrosBtn) {
        limpiarFiltrosBtn.addEventListener('click', () => limpiarFiltros(listaProyectosUl));
    }
    if (filtroNombreInput) {
        filtroNombreInput.addEventListener('input', () => {
            console.log("PASO 1: ⌨️ Evento 'input' detectado.");
            const texto = filtroNombreInput.value;
            if (texto.length >= 3 || texto.length === 0) {
                console.log("PASO 2: 👍 Condición cumplida. Llamando al filtro debounced...");
                filtrosDebounced();
            }
        });
    }
}

function normalizarTexto(texto) {
    if (!texto) return '';
    return texto
        .normalize("NFD")
        .replace(/[\u0300-\u036f]/g, "")
        .toLowerCase();
}

function debounce(func, delay) {
    let timeout;
    return function (...args) {
        clearTimeout(timeout);
        timeout = setTimeout(() => func.apply(this, args), delay);
    };
}