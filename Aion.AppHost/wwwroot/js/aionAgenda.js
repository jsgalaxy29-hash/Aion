import { Calendar } from '@fullcalendar/core';
import dayGridPlugin from '@fullcalendar/daygrid';
import timeGridPlugin from '@fullcalendar/timegrid';
import interactionPlugin from '@fullcalendar/interaction';

let calendar;
let dotNetRef;

export function initCalendar(dotNet, selectedAgendaId, initialView) {
    dotNetRef = dotNet;

    const calendarEl = document.getElementById('aion-agenda-calendar');
    if (!calendarEl) {
        console.warn('Aion agenda calendar element not found.');
        return;
    }

    calendar = new Calendar(calendarEl, {
        plugins: [dayGridPlugin, timeGridPlugin, interactionPlugin],
        initialView: initialView || 'timeGridWeek',
        height: 'auto',
        nowIndicator: true,
        selectable: true,
        editable: true,
        slotDuration: '00:30:00',
        slotMinTime: '06:00:00',
        slotMaxTime: '20:00:00',
        firstDay: 1,
        locale: 'fr',
        headerToolbar: false,
        eventClick: (info) => {
            info.jsEvent?.preventDefault();
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync('OnEventClickAsync', info.event.id);
            }
        },
        select: (info) => {
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync(
                    'OnEventCreatedAsync',
                    info.startStr,
                    info.endStr ?? info.startStr,
                    info.allDay ?? false
                );
            }
        },
        eventDrop: async (info) => {
            await persistEventDates(info.event);
        },
        eventResize: async (info) => {
            await persistEventDates(info.event);
        }
    });

    calendar.render();
}

export function setEvents(events) {
    if (!calendar) {
        return;
    }

    calendar.removeAllEvents();
    calendar.addEventSource(events ?? []);
}

export function changeView(viewName) {
    if (calendar) {
        calendar.changeView(viewName);
    }
}

export function goToday() {
    if (calendar) {
        calendar.today();
    }
}

export function goPrev() {
    if (calendar) {
        calendar.prev();
    }
}

export function goNext() {
    if (calendar) {
        calendar.next();
    }
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
