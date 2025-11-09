// wwwroot/js/aionAgenda.js

import { Calendar } from '@fullcalendar/core';
import dayGridPlugin from '@fullcalendar/daygrid';
import timeGridPlugin from '@fullcalendar/timegrid';
import interactionPlugin from '@fullcalendar/interaction';

window.aionAgenda = (function () {
    let calendar;
    let dotNetRef;

    function initCalendar(dotNet) {
        dotNetRef = dotNet;

        const calendarEl = document.getElementById('aion-agenda-calendar');

        calendar = new Calendar(calendarEl, {
            plugins: [dayGridPlugin, timeGridPlugin, interactionPlugin],
            initialView: 'timeGridWeek',          // comme Google Agenda par défaut
            headerToolbar: {
                left: 'prev,next today',
                center: 'title',
                right: 'dayGridMonth,timeGridWeek,timeGridDay'
            },
            height: 'auto',
            nowIndicator: true,
            selectable: true,
            editable: true,
            slotDuration: '00:30:00',
            slotMinTime: '07:00:00',
            slotMaxTime: '20:00:00',
            timeZone: 'Europe/Paris',
            firstDay: 1,                         // Lundi
            locale: 'fr',

            // Chargement des événements depuis l'API Aion
            events: async function (info, successCallback, failureCallback) {
                try {
                    const from = info.start.toISOString();
                    const to = info.end.toISOString();
                    const response = await fetch(`/api/agenda/events?from=${from}&to=${to}`);
                    if (!response.ok) throw new Error('Erreur API agenda');
                    const data = await response.json();
                    successCallback(data);
                } catch (e) {
                    console.error(e);
                    failureCallback(e);
                }
            },

            // Clic sur un évènement → édition
            eventClick: function (info) {
                info.jsEvent.preventDefault();
                if (dotNetRef) {
                    dotNetRef.invokeMethodAsync('OnEventClickAsync', info.event.id);
                }
            },

            // Sélection d’une plage horaire → création
            select: function (info) {
                if (dotNetRef) {
                    dotNetRef.invokeMethodAsync(
                        'OnEventCreatedAsync',
                        info.startStr,
                        info.endStr,
                        info.allDay
                    );
                }
            },

            // Drag & drop → mise à jour dates
            eventDrop: async function (info) {
                await updateEventDates(info.event);
            },

            // Resize → mise à jour dates
            eventResize: async function (info) {
                await updateEventDates(info.event);
            }
        });

        calendar.render();
    }

    async function updateEventDates(event) {
        // appel API PUT Aion pour mettre à jour l'évènement
        const payload = {
            id: event.id,
            start: event.start.toISOString(),
            end: event.end ? event.end.toISOString() : null,
            allDay: event.allDay
        };

        await fetch(`/api/agenda/events/${event.id}/dates`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
    }

    function changeView(viewName) {
        if (calendar) {
            calendar.changeView(viewName);
        }
    }

    function goToday() {
        if (calendar) {
            calendar.today();
        }
    }

    function reloadEvents() {
        if (calendar) {
            calendar.refetchEvents();
        }
    }

    return {
        initCalendar,
        changeView,
        goToday,
        reloadEvents
    };
})();
