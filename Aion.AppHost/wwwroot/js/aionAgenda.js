// wwwroot/js/aionAgenda.js
import { Calendar } from '/lib/fullcalendar/core/index.global.js';
import timeGridPlugin from '/lib/fullcalendar/timegrid/index.global.js';
import dayGridPlugin from '/lib/fullcalendar/daygrid/index.global.js';
import interactionPlugin from '/lib/fullcalendar/interaction/index.global.js';

// Optionnel : import des styles si tu veux les packager côté Blazor
import '/lib/fullcalendar/core/index.global.min.css';
import '/lib/fullcalendar/daygrid/index.global.min.css';
import '/lib/fullcalendar/timegrid/index.global.min.css';

let calendar;
let dotNetRef;

export function initCalendar(dotNetRefRef, selectedAgendaId, defaultView) {
    dotNetRef = dotNetRefRef;
    const calendarEl = document.getElementById("aion-agenda-calendar");
    if (!calendarEl) {
        console.error("Élément #aion-agenda-calendar introuvable");
        return;
    }

    // instanciation complète
    calendar = new Calendar(calendarEl, {
        plugins: [dayGridPlugin, timeGridPlugin, interactionPlugin],
        initialView: defaultView || "timeGridWeek",
        locale: "fr",
        selectable: true,
        editable: true,
        allDaySlot: true,
        nowIndicator: true,
        expandRows: true,
        height: "100%",
        slotDuration: "00:30:00",
        slotMinTime: "07:00:00",
        slotMaxTime: "20:00:00",
        firstDay: 1,
        headerToolbar: false, // toolbar Blazor gérée côté Razor

        // Rendu FluentUI
        eventColor: "var(--accent-fill-rest, #0078d4)",
        eventTextColor: "var(--neutral-foreground-on-accent-rest, #ffffff)",
        eventBorderColor: "var(--accent-stroke-control-rest, #0078d4)",
        dayMaxEvents: true,

        events: [],

        // Appels vers Blazor
        datesSet: (info) => {
            dotNetRef.invokeMethodAsync("OnVisibleRangeChangedAsync", info.startStr, info.endStr);
        },
        select: (info) => {
            dotNetRef.invokeMethodAsync("OnEventCreatedAsync", info.startStr, info.endStr, info.allDay);
        },
        eventClick: (info) => {
            dotNetRef.invokeMethodAsync("OnEventClickAsync", info.event.id);
        },
        eventDrop: (info) => persistEventDates(info.event),
        eventResize: (info) => persistEventDates(info.event)
    });

    calendar.render();
    window.aionAgendaCalendar = calendar;
}

export function setEvents(events) {
    if (!window.aionAgendaCalendar) return;
    const calendar = window.aionAgendaCalendar;
    calendar.removeAllEvents();
    calendar.addEventSource(events);
}

export function goToday() {
    window.aionAgendaCalendar?.today();
}

export function goPrev() {
    window.aionAgendaCalendar?.prev();
}

export function goNext() {
    window.aionAgendaCalendar?.next();
}

export function changeView(viewName) {
    window.aionAgendaCalendar?.changeView(viewName);
}

// Sauvegarde côté serveur des dates après drag/resizing
async function persistEventDates(event) {
    if (!event) return;
    const payload = {
        id: event.id,
        start: event.start ? event.start.toISOString() : null,
        end: event.end ? event.end.toISOString() : null,
        allDay: event.allDay ?? false
    };
    try {
        await fetch(`/api/agenda/events/${event.id}/dates`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
    } catch (err) {
        console.error('Failed to persist event dates', err);
    }
}
