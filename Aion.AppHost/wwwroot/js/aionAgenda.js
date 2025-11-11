// wwwroot/js/aionAgenda.js - Version améliorée style Google Agenda

let calendar;
let dotNetRef;

/**
 * Attend que FullCalendar soit chargé
 */
async function ensureFullCalendar() {
    if (window.FullCalendar) {
        return window.FullCalendar;
    }

    let attempts = 30;

    return new Promise((resolve, reject) => {
        const intervalId = setInterval(() => {
            if (window.FullCalendar) {
                clearInterval(intervalId);
                resolve(window.FullCalendar);
                return;
            }

            attempts--;
            if (attempts <= 0) {
                clearInterval(intervalId);
                reject(new Error('FullCalendar global not found after 30 attempts'));
            }
        }, 100);
    }).catch(err => {
        console.error('Failed to load FullCalendar:', err);
        return null;
    });
}

/**
 * Initialise le calendrier avec style Google
 */
export async function initCalendar(dotNetRefRef, selectedAgendaId, defaultView) {
    await disposeCalendar();

    dotNetRef = dotNetRefRef;

    const fullCalendar = await ensureFullCalendar();
    if (!fullCalendar) {
        console.error("FullCalendar n'est pas disponible. Vérifiez que le script CDN est correctement chargé.");
        return;
    }

    const { Calendar } = fullCalendar;

    if (!Calendar) {
        console.error("Le module Calendar de FullCalendar n'est pas disponible.");
        return;
    }

    const calendarEl = document.getElementById("aion-agenda-calendar");
    if (!calendarEl) {
        console.error("Élément #aion-agenda-calendar introuvable");
        return;
    }

    // Configuration style Google Agenda
    calendar = new Calendar(calendarEl, {
        initialView: defaultView || "timeGridWeek",
        locale: "fr",

        // Paramètres généraux
        selectable: true,
        editable: true,
        droppable: true,
        eventStartEditable: true,
        eventDurationEditable: true,

        // Affichage
        allDaySlot: true,
        nowIndicator: true,
        expandRows: true,
        height: "100%",
        aspectRatio: 1.8,

        // Horaires
        slotDuration: "00:15:00", // Intervalles de 15 minutes comme Google
        slotLabelInterval: "01:00:00", // Afficher les labels toutes les heures
        slotMinTime: "00:00:00",
        slotMaxTime: "24:00:00",
        scrollTime: "08:00:00", // Scroll initial à 8h
        slotLabelFormat: {
            hour: "2-digit",
            minute: "2-digit",
            hour12: false,
            omitZeroMinute: true
        },

        // Jour de début de semaine
        firstDay: 1, // Lundi

        // En-têtes
        headerToolbar: false, // Géré côté Blazor
        dayHeaderFormat: { weekday: 'short', day: 'numeric', month: 'numeric' },

        // Apparence des événements
        eventDisplay: 'block',
        eventTimeFormat: {
            hour: '2-digit',
            minute: '2-digit',
            hour12: false
        },

        // Nombre max d'événements dans la vue mois
        dayMaxEvents: 3,
        moreLinkText: (num) => `+${num} événements`,

        // Options de vue
        views: {
            dayGridMonth: {
                dayMaxEvents: 3,
                fixedWeekCount: false
            },
            timeGridWeek: {
                slotEventOverlap: false,
                allDaySlot: true
            },
            timeGridDay: {
                slotEventOverlap: false,
                allDaySlot: true
            }
        },

        // Style des boutons (même si toolbar est false)
        buttonText: {
            today: "Aujourd'hui",
            month: 'Mois',
            week: 'Semaine',
            day: 'Jour',
            list: 'Agenda'
        },

        // Événements vides par défaut
        events: [],

        // === Callbacks vers Blazor ===

        // Changement de dates visibles
        datesSet: function (info) {
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync("OnVisibleRangeChangedAsync", info.startStr, info.endStr);
            }
        },

        // Sélection de plage de dates (création d'événement)
        select: function (info) {
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync("OnEventCreatedAsync", info.startStr, info.endStr, info.allDay);
            }
            calendar.unselect();
        },

        // Clic sur un événement
        eventClick: function (info) {
            info.jsEvent.preventDefault();
            if (dotNetRef && info.event.id) {
                dotNetRef.invokeMethodAsync("OnEventClickAsync", info.event.id);
            }
        },

        // Drag & drop d'événement
        eventDrop: function (info) {
            persistEventDates(info.event);
        },

        // Redimensionnement d'événement
        eventResize: function (info) {
            persistEventDates(info.event);
        },

        // Hover sur événement (pour tooltip futur)
        eventMouseEnter: function (info) {
            info.el.style.cursor = 'pointer';
            info.el.style.transform = 'scale(1.02)';
        },

        eventMouseLeave: function (info) {
            info.el.style.transform = 'scale(1)';
        },

        // Rendu personnalisé des événements
        eventDidMount: function (info) {
            // Ajouter des classes personnalisées
            if (info.event.extendedProps.isPrivate) {
                info.el.classList.add('event-private');
            }

            // Tooltip simple (pourrait être amélioré avec Fluent UI)
            const title = info.event.title;
            const start = info.event.start ? formatDateTime(info.event.start) : '';
            const end = info.event.end ? formatDateTime(info.event.end) : '';

            info.el.title = `${title}\n${start}${end ? ' - ' + end : ''}`;
        },

        // Limite de sélection
        selectConstraint: {
            start: '1900-01-01',
            end: '2100-12-31'
        },

        // Permettre la sélection sur plusieurs jours
        selectMirror: true,

        // Overlay lors du drag
        selectOverlap: function (event) {
            return event.display !== 'background';
        },

        // Format pour les événements all-day
        allDayContent: 'Journée entière',

        // Week numbers (optionnel)
        weekNumbers: false,
        weekText: 'Sem.',

        // Navigation par clic sur les numéros de jours
        navLinks: true,
        navLinkDayClick: function (date, jsEvent) {
            calendar.changeView('timeGridDay', date);
        },

        // Réglages de langue supplémentaires
        locale: 'fr',
        timeZone: 'local',

        // Business hours (heures de travail)
        businessHours: {
            daysOfWeek: [1, 2, 3, 4, 5], // Lun-Ven
            startTime: '08:00',
            endTime: '18:00'
        },

        // Afficher les heures de travail
        selectConstraint: 'businessHours',

        // Style pour week-ends
        dayCellClassNames: function (arg) {
            if (arg.date.getDay() === 0 || arg.date.getDay() === 6) {
                return ['weekend-day'];
            }
            return [];
        }
    });

    calendar.render();

    // Rendre accessible globalement
    window.aionAgendaCalendar = calendar;

    console.log('✓ Calendrier initialisé avec succès');
}

/**
 * Définit les événements du calendrier
 */
export function setEvents(events) {
    if (!window.aionAgendaCalendar) {
        console.warn('Calendrier non initialisé');
        return;
    }

    const calendar = window.aionAgendaCalendar;

    // Supprimer tous les événements existants
    calendar.removeAllEvents();

    // Ajouter les nouveaux événements
    if (events && events.length > 0) {
        calendar.addEventSource(events);
        console.log(`✓ ${events.length} événement(s) chargé(s)`);
    }
}

/**
 * Navigation - Aller à aujourd'hui
 */
export function goToday() {
    if (window.aionAgendaCalendar) {
        window.aionAgendaCalendar.today();
    }
}

/**
 * Navigation - Période précédente
 */
export function goPrev() {
    if (window.aionAgendaCalendar) {
        window.aionAgendaCalendar.prev();
    }
}

/**
 * Navigation - Période suivante
 */
export function goNext() {
    if (window.aionAgendaCalendar) {
        window.aionAgendaCalendar.next();
    }
}

/**
 * Changer la vue (mois, semaine, jour)
 */
export function changeView(viewName) {
    if (window.aionAgendaCalendar) {
        window.aionAgendaCalendar.changeView(viewName);
        console.log(`✓ Vue changée vers: ${viewName}`);
    }
}

/**
 * Aller à une date spécifique
 */
export function gotoDate(dateStr) {
    if (window.aionAgendaCalendar) {
        window.aionAgendaCalendar.gotoDate(dateStr);
    }
}

/**
 * Obtenir la vue actuelle
 */
export function getCurrentView() {
    if (window.aionAgendaCalendar) {
        return window.aionAgendaCalendar.view.type;
    }
    return null;
}

/**
 * Rafraîchir le calendrier
 */
export function refetchEvents() {
    if (window.aionAgendaCalendar) {
        window.aionAgendaCalendar.refetchEvents();
    }
}

/**
 * Sauvegarder les dates d'un événement après drag/resize
 */
async function persistEventDates(event) {
    if (!event || !event.id) {
        console.warn('Événement invalide pour persistEventDates');
        return;
    }

    const payload = {
        id: event.id,
        start: event.start ? event.start.toISOString() : null,
        end: event.end ? event.end.toISOString() : null,
        allDay: event.allDay ?? false
    };

    try {
        const response = await fetch(`/api/agenda/events/${event.id}/dates`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            },
            body: JSON.stringify(payload)
        });

        if (!response.ok) {
            console.error('Échec de la sauvegarde des dates:', response.statusText);
            // Optionnel: Revert les changements
            event.revert();
        } else {
            console.log('✓ Dates de l\'événement sauvegardées');
        }
    } catch (err) {
        console.error('Erreur lors de la sauvegarde des dates:', err);
        // Optionnel: Revert les changements
        if (event.revert) {
            event.revert();
        }
    }
}

/**
 * Formater une date pour l'affichage
 */
function formatDateTime(date) {
    if (!date) return '';

    return new Intl.DateTimeFormat('fr-FR', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit'
    }).format(date);
}

/**
 * Ajouter un événement programmatiquement
 */
export function addEvent(event) {
    if (window.aionAgendaCalendar) {
        window.aionAgendaCalendar.addEvent(event);
    }
}

/**
 * Supprimer un événement par son ID
 */
export function removeEvent(eventId) {
    if (window.aionAgendaCalendar) {
        const event = window.aionAgendaCalendar.getEventById(eventId);
        if (event) {
            event.remove();
        }
    }
}

/**
 * Mettre à jour un événement
 */
export function updateEvent(eventId, updates) {
    if (window.aionAgendaCalendar) {
        const event = window.aionAgendaCalendar.getEventById(eventId);
        if (event) {
            event.setProp('title', updates.title);
            if (updates.start) event.setStart(updates.start);
            if (updates.end) event.setEnd(updates.end);
            if (updates.backgroundColor) event.setProp('backgroundColor', updates.backgroundColor);
        }
    }
}

/**
 * Obtenir tous les événements
 */
export function getAllEvents() {
    if (window.aionAgendaCalendar) {
        return window.aionAgendaCalendar.getEvents();
    }
    return [];
}

/**
 * Nettoyer et détruire le calendrier
 */
export async function disposeCalendar() {
    if (window.aionAgendaCalendar) {
        try {
            window.aionAgendaCalendar.destroy();
            console.log('✓ Calendrier détruit');
        } catch (err) {
            console.error('Erreur lors de la destruction du calendrier:', err);
        }
        window.aionAgendaCalendar = null;
    }

    calendar = undefined;
    dotNetRef = null;
}

/**
 * Redimensionner le calendrier (utile après changement de layout)
 */
export function resizeCalendar() {
    if (window.aionAgendaCalendar) {
        window.aionAgendaCalendar.updateSize();
    }
}

/**
 * Toggle menu mobile (pour responsive)
 */
export function toggleMobileSidebar() {
    const sidebar = document.querySelector('.agenda-sidebar');
    if (sidebar) {
        sidebar.classList.toggle('open');
    }
}

// Export pour debug
if (typeof window !== 'undefined') {
    window.aionAgendaDebug = {
        getCalendar: () => window.aionAgendaCalendar,
        getView: getCurrentView,
        getEvents: getAllEvents
    };
}