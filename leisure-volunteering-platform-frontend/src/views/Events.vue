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
        <v-card class="event-card">
          <v-card-title class="font-weight-bold">{{ event.name }}</v-card-title>
          
          <v-card-text>
            <p class="text-truncate description">{{ event.description }}</p>
            
            <v-divider class="my-3"></v-divider> 

            <p class="text-subtitle-2">
            👥 <strong v-if="event.nextEventDate">Likusios vietos: {{ event.nextEventSpots }}</strong>
            </p>

            <v-divider class="my-3"></v-divider> 
                 
            <p class="text-subtitle-2">
            📅 <strong v-if="event.nextEventDate">{{ event.nextEventDate }}</strong>
            <strong v-else class="text-red font-weight-bold">Patikslinkite datą !</strong>
            </p>
            
            <p class="text-subtitle-2">
              🕒 {{ event.startTime }} - {{ event.endTime }}
            </p>
          </v-card-text>

          <v-card-actions>
            <v-btn color="primary" text @click="goToEventDetails(event.id)">+Daugiau</v-btn>
          </v-card-actions>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script>
export default {
  name: "EventsPage",
  data() {
    return {
      events: [], 
      userRole: null,
    };
  },
  computed: {
  filteredEvents() {
    if (this.userRole === "EventOrganizer") {
      return this.events;
    }
    return this.events.filter(event => event.nextEventDate);
  }
},
  methods: {
    async fetchEvents() {
  try {
    const response = await fetch("https://localhost:7177/api/events");
    if (!response.ok) throw new Error("Failed to fetch events");

    const data = await response.json();
    console.log("🔍 Full API Response:", JSON.stringify(data, null, 2)); // <-- Debug API response

    const now = new Date(); 
    now.setSeconds(0, 0); 

    const enrichedEvents = await Promise.all(
      data.map(async (event) => {
        const start = new Date(event.startDate);
        const end = new Date(event.endDate);
        let nextDate = null;
        let availableSpots = event.volunteersCount; // Default to max spots

        while (start <= end) {
          const eventDate = new Date(start);
          eventDate.setHours(0, 0, 0, 0);

          const eventStartTime = new Date(eventDate);
          const [startHour, startMinute] = event.startTime.split(":");
          eventStartTime.setHours(startHour, startMinute, 0, 0);

          if (eventStartTime > now) {
            nextDate = new Intl.DateTimeFormat("lt-LT", {
              year: "numeric",
              month: "2-digit",
              day: "2-digit",
            }).format(eventDate);

            // ✅ Fetch detailed event data to get `volunteersCountPerDate`
            const eventDetailsResponse = await fetch(`https://localhost:7177/api/events/${event.id}`);
            if (eventDetailsResponse.ok) {
              const eventDetails = await eventDetailsResponse.json();

              if (eventDetails.volunteersCountPerDate && eventDetails.volunteersCountPerDate[nextDate] !== undefined) {
                availableSpots = Math.max(0, eventDetails.volunteersCountPerDate[nextDate]);
              }
            }

            console.log(`✅ Event: ${event.name}, Date: ${nextDate}, Available Spots: ${availableSpots}`);
            break;
          }

          start.setDate(start.getDate() + 1);
        }

        return {
          ...event,
          nextEventDate: nextDate,
          nextEventSpots: availableSpots,
        };
      })
    );

    this.events = enrichedEvents;
  } catch (error) {
    console.error("Klaida gaunant renginius:", error);
  }
},
    goToEventDetails(eventId) {
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
    this.fetchEvents();
    this.userRole = this.getUserRoleFromToken();
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

.event-card {
  height: 100%;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  padding: 16px;
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
</style>
