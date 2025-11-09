// wwwroot/js/aionAgenda.js

let calendar;
let dotNetRef;
export function initCalendar(dotNetRef, selectedAgendaId, defaultView) {
    const calendarEl = document.getElementById("aion-agenda-calendar");
    if (!calendarEl) {
        console.error("Élément #aion-agenda-calendar introuvable");
        return;
    }

    if (!window.FullCalendar || !window.FullCalendar.Calendar) {
        console.error("FullCalendar global n'est pas chargé");
        return;
    }

    const calendar = new FullCalendar.Calendar(calendarEl, {
        initialView: defaultView || "timeGridWeek",
        locale: "fr",
        selectable: true,
        editable: false,

        // 👉 pour occuper toute la hauteur de son conteneur
        height: "100%", 
        width: "100%",
        expandRows: true,

        events: [],
        datesSet: (info) => {
            dotNetRef.invokeMethodAsync(
                "OnVisibleRangeChangedAsync",
                info.startStr,
                info.endStr
            );
        },
        select: (info) => {
            dotNetRef.invokeMethodAsync(
                "OnEventCreatedAsync",
                info.startStr,
                info.endStr,
                info.allDay
            );
        },
        eventClick: (info) => {
            dotNetRef.invokeMethodAsync(
                "OnEventClickAsync",
                info.event.id
            );
        }
    });


    calendar.render();
    window.aionAgendaCalendar = calendar;
}

export function setEvents(events) {
    const calendar = window.aionAgendaCalendar;
    if (!calendar) return;

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

async function persistEventDates(event) {
    if (!event) {
        return;
    }

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
