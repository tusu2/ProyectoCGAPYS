

    // Seleccionamos todos los enlaces dentro del sidebar
    const menuLinks = document.querySelectorAll('.sidebar .menu-link');

    // Función que maneja el clic
    function handleLinkClick(event) {
        // Primero, quitamos la clase 'active' de todos los demás enlaces
        menuLinks.forEach(link => {
            link.classList.remove('active');
        });

        // Luego, añadimos la clase 'active' al enlace que fue clickeado
        // 'this' se refiere al elemento que disparó el evento (el enlace)
        this.classList.add('active');
    }

    // Agregamos un 'escuchador' de eventos de clic a cada enlace
    menuLinks.forEach(link => {
        link.addEventListener('click', handleLinkClick);
    });
    // --- REFERENCIAS AL DOM ---
    // ... (las referencias a modal, lista, etc. no cambian)
    const listaProyectosUl = document.getElementById('lista-proyectos');
    const modal = document.getElementById('modal-proyecto');
    const modalNombreProyecto = document.getElementById('modal-nombre-proyecto');
    const modalDetalles = document.getElementById('modal-detalles');
    const cerrarModalBtn = document.querySelector('.cerrar-modal');

    // Referencias a los elementos del formulario de filtros
    const filtroNombre = document.getElementById('filtro-nombre');
    const filtroDependencia = document.getElementById('filtro-dependencia');
    const filtroFechaInicio = document.getElementById('filtro-fecha-inicio');
    const filtroFechaFin = document.getElementById('filtro-fecha-fin');
    const aplicarFiltrosBtn = document.getElementById('aplicar-filtros-btn');
    const limpiarFiltrosBtn = document.getElementById('limpiar-filtros-btn');

    // --- VARIABLES GLOBALES ---
    let mapa;
    let todosLosProyectos = [];
    let marcadoresEnMapa = [];

    // ... (El resto del código: inicializarMapa, cargarYMostrarProyectos, renderizarProyectos, etc. es exactamente el mismo)
    // ... (Lo omito aquí para ser breve, pero no ha cambiado)


    // --- ASIGNACIÓN DE EVENTOS ---
    // El código para el modal no cambia
    if (cerrarModalBtn) {
        cerrarModalBtn.addEventListener('click', cerrarModal);
    }
    if (modal) {
        modal.addEventListener('click', (event) => {
            if (event.target === modal) {
                cerrarModal();
            }
        });
    }

    // *** AQUÍ ESTÁ LA MEJORA ***
    // Comprobamos que los botones de filtro existen en la página actual antes de asignarles eventos.
    if (aplicarFiltrosBtn && limpiarFiltrosBtn) {
        aplicarFiltrosBtn.addEventListener('click', aplicarFiltros);
        limpiarFiltrosBtn.addEventListener('click', limpiarFiltros);
    }

    // --- EJECUCIÓN INICIAL ---
    // Comprobamos si el contenedor del mapa existe para decidir si ejecutar el script.
    // Esto asegura que todo este código SÓLO se ejecute en la página de búsqueda.
    if (document.getElementById('mapa-principal')) {
        inicializarMapa();
        cargarYMostrarProyectos();
    }

    // --- DEFINICIONES DE FUNCIONES (sin cambios) ---
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
        // Le pasamos la referencia de la lista a la siguiente función.
        renderizarProyectos(todosLosProyectos, listaProyectosUl);
    } catch (error) {
        console.error("No se pudieron cargar los proyectos:", error);
        if (listaProyectosUl) {
            listaProyectosUl.innerHTML = '<li class="no-projects">Error al cargar los proyectos.</li>';
        }
    }
    }

function renderizarProyectos(proyectos, listaProyectosUl) {
    if (!listaProyectosUl) return; // Salimos si la lista no existe.

    marcadoresEnMapa.forEach(marker => mapa.removeLayer(marker));
    marcadoresEnMapa = [];
    listaProyectosUl.innerHTML = '';

    if (proyectos.length === 0) {
        listaProyectosUl.innerHTML = '<li class="no-projects">No se encontraron proyectos.</li>';
    } else {
        proyectos.forEach(proyecto => {
            const modal = document.getElementById('modal-proyecto'); // Buscamos modal y detalles aquí
            const modalDetalles = document.getElementById('modal-detalles');
            const modalNombreProyecto = document.getElementById('modal-nombre-proyecto');

            crearMarcadorEnMapa(proyecto, modal, modalNombreProyecto, modalDetalles);
            crearElementoEnLista(proyecto, listaProyectosUl, modal, modalNombreProyecto, modalDetalles);
        });
    }
}

    function crearMarcadorEnMapa(proyecto) {
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
    const filtroNombre = document.getElementById('filtro-nombre').value.toLowerCase();
    const filtroDependencia = document.getElementById('filtro-dependencia').value;
        const nombre = filtroNombre.value.toLowerCase();
        const dependencia = filtroDependencia.value;
        const fechaInicio = filtroFechaInicio.value;
        const fechaFin = filtroFechaFin.value;
        const proyectosFiltrados = todosLosProyectos.filter(proyecto => {
            const matchNombre = proyecto.nombreProyecto.toLowerCase().includes(nombre);
            const matchDependencia = !dependencia || proyecto.dependencia === dependencia;
            const fechaProyecto = new Date(proyecto.fechaSolicitud);
            const matchFechaInicio = !fechaInicio || fechaProyecto >= new Date(fechaInicio);
            const matchFechaFin = !fechaFin || fechaProyecto <= new Date(fechaFin);
            return matchNombre && matchDependencia && matchFechaInicio && matchFechaFin;
        });
        renderizarProyectos(proyectosFiltrados);
    }

function limpiarFiltros(listaProyectosUl) {
    document.getElementById('filtro-nombre').value = '';
    document.getElementById('filtro-dependencia').value = '';
        filtroNombre.value = '';
        filtroDependencia.value = '';
        filtroFechaInicio.value = '';
        filtroFechaFin.value = '';
        renderizarProyectos(todosLosProyectos);
    }

    function mostrarMensajeSinProyectos() {
        listaProyectosUl.innerHTML = '<li class="no-projects">No se encontraron proyectos que coincidan con los filtros.</li>';
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

function mostrarModalConDetalles(proyecto) {
    if (!modal) return;
        modalNombreProyecto.textContent = proyecto.nombreProyecto;
        modalDetalles.innerHTML = `
            <p><strong>Folio:</strong> ${proyecto.folio || 'No disponible'}</p>
            <p><strong>Dependencia:</strong> ${proyecto.dependencia}</p>
            <p><strong>Estatus:</strong> ${proyecto.estatus}</p>
            <p><strong>Responsable:</strong> ${proyecto.nombreResponsable}</p>
            <p><strong>Tipo de Proyecto:</strong> ${proyecto.tipoProyecto}</p>
            <p><strong>Tipo de Fondo:</strong> ${proyecto.tipoFondo}</p>
            <p><strong>Presupuesto:</strong> $${Number(proyecto.presupuesto).toLocaleString('es-MX', { minimumFractionDigits: 2 })}</p>
            <p><strong>Fecha Solicitud:</strong> ${proyecto.fechaSolicitud ? new Date(proyecto.fechaSolicitud).toLocaleDateString() : 'N/D'}</p>
            <p><strong>Descripción:</strong> ${proyecto.descripcion}</p>
        `;
        modal.classList.add('show');
    }

    function cerrarModal() {
        if (modal) modal.classList.remove('show');
    }

function inicializarLogicaIndex() {

    // 1. "Toma una foto" nueva de TODOS los elementos del HTML necesarios.
    const listaProyectosUl = document.getElementById('lista-proyectos');
    const modal = document.getElementById('modal-proyecto');
    const modalNombreProyecto = document.getElementById('modal-nombre-proyecto');
    const modalDetalles = document.getElementById('modal-detalles');
    const cerrarModalBtn = document.querySelector('.cerrar-modal');
    const aplicarFiltrosBtn = document.getElementById('aplicar-filtros-btn');
    const limpiarFiltrosBtn = document.getElementById('limpiar-filtros-btn');

    // 2. Llama a las funciones que necesitan estas referencias.
    inicializarMapa();
    cargarYMostrarProyectos(listaProyectosUl);

    // 3. Asigna TODOS los eventos a los elementos que acabamos de encontrar.
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
}
