<template>
  <v-container fluid class="events">
    <v-row class="mb-3" align="center">
      <v-col cols="8">
        <h1 class="font-weight-bold">Artimiausios savanoriškos veiklos</h1>
      </v-col>
      <v-col cols="4" class="text-right">
         <v-btn v-if="userRole === 'EventOrganizer'" color="primary" @click="goToCreateEvent">
          <v-icon left>mdi-plus</v-icon> Pridėti veiklą
        </v-btn>
      </v-col>
    </v-row>

    <v-row>
      <v-col 
        v-for="event in filteredEvents" 
        :key="event.id" 
        cols="12" sm="6" md="4" lg="3"
      >
        <v-card class="event-card" :class="{'organizer-event': event.organizerId === userId, 'dark-event-card': isDark}">
        <v-card-title class="font-weight-bold">
          <span class="flex-grow-1">{{ event.name }}</span>

          <v-icon
    v-if="volunteerResponses[event.id] === true"
    class="alert-icon ml-2"
  >
    mdi-alert-circle
  </v-icon>

        </v-card-title>
        <v-card-text>
          <p class="text-truncate description">{{ event.description }}</p>
    
          <v-divider class="my-3"></v-divider> 

          <p class="text-subtitle-2">
            👥 <strong v-if="event.nextEventDate">Likusios vietos: {{ event.nextEventSpots }}</strong>
          </p>

          <v-divider class="my-3"></v-divider> 
         
          <p class="text-subtitle-2">
            📅 <strong v-if="event.nextEventDate">{{ event.nextEventDate }}</strong>
            <strong v-else class="text-red font-weight-bold">Patikslinkite datą!</strong>
          </p>
    
          <p class="text-subtitle-2">
            🕒 {{ event.startTime }} - {{ event.endTime }}
          </p>
        </v-card-text>

        <v-card-actions>
            <v-btn v-if="event.organizerId === userId" icon @click="editEvent(event.id)">
              <v-icon :color="isDark ? 'white' : 'primary'">mdi-pencil</v-icon>
            </v-btn>

            <v-btn v-if="event.organizerId === userId" icon @click="confirmDelete(event.id)">
              <v-icon color="red">mdi-delete</v-icon>
            </v-btn>

            <v-spacer></v-spacer>

          <v-btn :color="isDark ? 'white' : 'primary'" text @click="goToEventDetails(event.id)">+Daugiau</v-btn>
        </v-card-actions>
        </v-card>
      </v-col>
    </v-row>

    <v-dialog v-model="deleteDialog" max-width="400">
      <v-card>
        <v-card-title class="text-h5">Patvirtinti ištrynimą</v-card-title>
        <v-card-text>
          Ar tikrai norite ištrinti šią veiklą?
        </v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn color="grey" @click="deleteDialog = false">Atšaukti</v-btn>
          <v-btn color="red" @click="deleteEvent">Ištrinti</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-container>
</template>

<script>
import { useToast } from "vue-toastification";
import { useTheme } from 'vuetify';
import { computed } from 'vue';
export default {
  setup() {
    const theme = useTheme();
    const isDark = computed(() => theme.global.name.value === 'dark');

    return { isDark };
  },
  name: "EventsPage",
  data() {
    return {
      volunteerResponses: {},
      toast: useToast(),
      events: [], 
      userRole: null,
      userId: null,
      deleteDialog: false,
      selectedEventId: null
    };
  },
  computed: {
filteredEvents() {
    const now = new Date();
    now.setSeconds(0, 0);

    let myUpcomingEvents = [];
    let myOutdatedEvents = [];
    let otherEvents = [];

    this.events.forEach(event => {
      const isOutdated = !event.nextEventDate; 

      const enrichedEvent = { ...event, isOutdated };

      if (this.userId && event.organizerId === this.userId) {
        if (isOutdated) {
          myOutdatedEvents.push(enrichedEvent);
        } else {
          myUpcomingEvents.push(enrichedEvent);
        }
      } else {
        if (!isOutdated) {
          otherEvents.push(enrichedEvent);
        }
      }
    });

    myUpcomingEvents.sort((a, b) => new Date(a.startDate) - new Date(b.startDate)); 
    myOutdatedEvents.sort((a, b) => new Date(b.startDate) - new Date(a.startDate)); 
    otherEvents.sort((a, b) => new Date(a.startDate) - new Date(b.startDate)); 

    return [...myUpcomingEvents, ...myOutdatedEvents, ...otherEvents];
  }
},
  methods: {
    editEvent(eventId) {
      this.$router.push(`/edit-event/${eventId}`);
    },
    confirmDelete(eventId) {
      this.selectedEventId = eventId;
      this.deleteDialog = true;
    },
    async deleteEvent() {
      try {
        const token = localStorage.getItem("accessToken") || sessionStorage.getItem("accessToken");
        const response = await fetch(`https://localhost:7177/api/events/${this.selectedEventId}`, {
          method: "DELETE",
          headers: { "Authorization": `Bearer ${token}` }
        });

        if (!response.ok) throw new Error("Nepavyko pašalinti veiklos");

        this.events = this.events.filter(event => event.id !== this.selectedEventId);
        this.deleteDialog = false;
        this.toast.success("Veikla sėkmingai pašalinta!")
      } catch (error) {
        console.error("Klaida trinant veiklą:", error);
      }
    },
  getUserIdFromToken() {
  const token = localStorage.getItem("accessToken") || sessionStorage.getItem("accessToken");
  if (!token) return null;

  try {
    const payloadBase64 = token.split(".")[1]; 
    if (!payloadBase64) return null;

    const decodedPayload = JSON.parse(atob(payloadBase64));
    return decodedPayload["userId"] || null;

  } catch (error) {
    console.error("[ERROR] Failed to decode token:", error);
    return null;
  }
},
    async fetchEvents() {
  try {
    const response = await fetch("https://localhost:7177/api/events");
    if (!response.ok) throw new Error("Failed to fetch events");

    const data = await response.json();

    const now = new Date(); 
    now.setSeconds(0, 0); 

const volunteerResponses = {};

const enrichedEvents = await Promise.all(
  data.map(async (event) => {
    let nextDate = null;
    let availableSpots = event.volunteersCount;

    const eventDetailsResponse = await fetch(`https://localhost:7177/api/events/${event.id}`);
    const eventDetails = eventDetailsResponse.ok ? await eventDetailsResponse.json() : {};

    if (this.userRole === 'Volunteer') {
      const seen = localStorage.getItem(`seen-response-${event.id}-user-${this.userId}`);
      if (seen === "false") {
        volunteerResponses[event.id] = true;
        console.log("Should show icon for event:", event.id);
        console.log(`Seen for event ${event.id}:`, seen);

      }
    }

    const start = new Date(event.startDate);
    const end = new Date(event.endDate);
    const now = new Date();
    now.setSeconds(0, 0);
    while (start <= end) {
      const eventDate = new Date(start);
      const eventStartTime = new Date(eventDate);
      const [startHour, startMinute] = event.startTime.split(":");
      eventStartTime.setHours(startHour, startMinute, 0, 0);

      if (eventStartTime > now) {
        nextDate = new Intl.DateTimeFormat("lt-LT").format(eventDate);
        if (eventDetails.volunteersCountPerDate?.[nextDate] !== undefined) {
          availableSpots = Math.max(0, eventDetails.volunteersCountPerDate[nextDate]);
        }
        break;
      }
      start.setDate(start.getDate() + 1);
    }

    return {
      ...event,
      nextEventDate: nextDate,
      nextEventSpots: availableSpots
    };
  })
);

this.events = enrichedEvents;
this.volunteerResponses = volunteerResponses;
  } catch (error) {
    console.error("Klaida gaunant veiklas:", error);
  }
},
   goToEventDetails(eventId) {
  if (this.volunteerResponses[eventId]) {
     localStorage.setItem(`seen-response-${eventId}-user-${this.userId}`, "true");
     this.volunteerResponses[eventId] = false;
  }
  this.$router.push(`/event/${eventId}`);
},
    goToCreateEvent() {
      this.$router.push("/create-event"); 
    },
    getUserRoleFromToken() {
      const token = localStorage.getItem("accessToken") || sessionStorage.getItem("accessToken");
      if (!token) return null;

      try {
        const payloadBase64 = token.split(".")[1]; 
        if (!payloadBase64) return null;

        const decodedPayload = JSON.parse(atob(payloadBase64));
        const role = decodedPayload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || null;
        return role;
        
      } catch (error) {
        console.error("Error decoding token:", error);
        return null;
      }
    }
  },
  mounted() {
    this.userRole = this.getUserRoleFromToken();
    this.userId = Number(this.getUserIdFromToken());
    this.fetchEvents(); 
  },
};
</script>

<style scoped>
.events{
   min-height: 80vh; 
}

.v-container {
  max-width: 1200px;
}

.text-truncate {
  overflow: hidden;
  text-overflow: ellipsis;
  display: -webkit-box;
  -webkit-line-clamp: 3;
  -webkit-box-orient: vertical;
}

.description {
  margin-bottom: 12px;
}

.v-divider {
  margin: 8px 0;
}

.v-card-actions {
  justify-content: flex-end;
}

.text-right {
  text-align: right;
}

.event-card {
  height: 100%;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  padding: 16px;
  background-color: var(--v-theme-surface);
  border-radius: 8px;
  border: 2px solid transparent;
  box-shadow: 0px 4px 8px rgba(0, 0, 0, 0.1);
  transition: transform 0.2s ease-in-out, box-shadow 0.2s ease-in-out, border-color 0.2s ease-in-out;
}

.event-card:hover {
  transform: translateY(-3px);
  box-shadow: 0px 6px 15px rgba(0, 0, 0, 0.15);
  border-color: #1976d2;
}

.dark-event-card {
  border-color: black;
  background-color: #1e1e1e !important;
  color: #E0E0E0 !important; 
}

.dark-event-card:hover {
  border-color: #1976d2;
}

.organizer-event {
  border: 2px solid #1976d2 !important;
  background-color: var(--v-theme-surface-lighten-1) !important;
  box-shadow: 0px 4px 10px rgba(25, 118, 210, 0.2);
}

.organizer-event:hover {
  transform: translateY(-3px);
  box-shadow: 0px 6px 15px rgba(25, 118, 210, 0.3);
  border-color: #1565c0;
}

.alert-icon {
  position: absolute;
  left: 25px;
  top: 83%;
  transform: translateY(-50%);
  color: #FFD600;
  border-radius: 50%;
  padding: 2px;
  font-size: 18px;
  animation: pulse 1.5s infinite;
}

@keyframes pulse {
  0% {
    transform: scale(1);
    opacity: 1;
  }
  50% {
    transform: scale(1.2);
    opacity: 0.7;
  }
  100% {
    transform: scale(1);
    opacity: 1;
  }
}
</style>
