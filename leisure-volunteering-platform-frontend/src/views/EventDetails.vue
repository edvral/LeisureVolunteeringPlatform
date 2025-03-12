<template>
  <v-container fluid class="eventDetails">
    <v-row class="mb-4">
      <v-col class="d-flex align-center">
        <v-btn color="primary" class="mr-2" @click="$router.push('/events')">
          <v-icon left>mdi-arrow-left</v-icon> Atgal
        </v-btn>
      </v-col>
    </v-row>

    <v-card v-if="event" class="pa-5">
      <v-card-title class="text-h5">{{ event.name }}</v-card-title>
   
      <v-divider class="my-3"></v-divider>

      <div class="description-box">
        <h3><v-icon left>mdi-information-outline</v-icon> Aprašymas</h3>
        <p class="description-text">{{ event.description }}</p>
      </div>

      <v-divider class="my-3"></v-divider>

      <v-card class="pa-3">
        <h3 class="mb-2">🗺️ Savanorystės vieta: {{ event.address || "Nėra adreso" }}</h3>
        <div id="map"></div>
      </v-card>

      <v-divider class="my-3"></v-divider>

      <v-card-title class="text-h5 d-flex align-center">
      <v-icon left color="success" class="mr-2">mdi-calendar-clock</v-icon> Renginio laikai
      </v-card-title>


      <v-divider class="my-2"></v-divider>

   <div v-if="eventDates.length > 0">
    <div v-for="dateObj in eventDates" :key="dateObj.date" class="event-entry">
    <v-row align="center" justify="space-between">
    <v-col cols="8">
      <p class="text-subtitle-1">📅 <strong>{{ dateObj.date }}</strong></p>
      <p class="text-subtitle-1">⏰ <strong>{{ event.startTime }} - {{ event.endTime }}</strong></p>
      <p class="text-subtitle-1">👥 <strong>Likusios vietos:</strong> {{ event.volunteersCountPerDate && event.volunteersCountPerDate[dateObj.date] !== undefined? event.volunteersCountPerDate[dateObj.date] : "?" }}</p>
    </v-col>

    <v-col cols="4" class="d-flex justify-end align-center">
      <v-btn 
        v-if="userRole === 'Volunteer' && dateObj.hoursLeft > 24"
        color="success"
        small
        outlined
        class="fancy-button"
        @click="registerForEvent(dateObj.date)"
      > 
        <v-icon left>mdi-account-plus</v-icon> Registruotis į veiklą
      </v-btn>

      <p v-else-if="userRole === 'Volunteer'" class="text-error font-weight-bold">
        🚫 Registracija šiai savanoriškai veiklai baigta.
      </p>

      <p v-if="userRole !== 'Volunteer'" class="text-warning font-weight-bold mt-2">
      🔒 Prisijunkite kaip savanoris norint užsiregistruoti į veiklą
      </p>
    </v-col>
  </v-row>

  <v-divider class="my-3"></v-divider>
</div>
</div>

<div v-else class="text-center text-grey font-weight-bold mt-4">
  📅 Renginio datos bus patikslintos greitu metu
</div>

    </v-card>

    <v-alert v-else type="error">Renginys nerastas.</v-alert>
  </v-container>
</template>

<script>
/* global google */
import loader from "@/utils/GoogleMapsLoader";

export default {
  name: "EventDetails",
  data() {
    return {
      event: null,
      userRole: null,
    };
  },
  computed: {
  formattedDateRange() {
  if (!this.event?.startDate || !this.event?.endDate) return "Invalid Date";

  const start = new Date(this.event.startDate);
  const end = new Date(this.event.endDate);

  const formattedStart = new Intl.DateTimeFormat('lt-LT', {
    year: "numeric",
    month: "2-digit",
    day: "2-digit"
  }).format(start);

  const formattedEnd = new Intl.DateTimeFormat('lt-LT', {
    year: "numeric",
    month: "2-digit",
    day: "2-digit"
  }).format(end);

  return `${formattedStart} - ${formattedEnd}`;
  },
    formattedTime() {
      return this.event?.startTime && this.event?.endTime
        ? `${this.event.startTime} - ${this.event.endTime}`
        : "Invalid Time";
    },
  eventDates() {
  if (!this.event?.startDate || !this.event?.endDate) return [];

  const now = new Date();
  now.setSeconds(0, 0, 0); 

  const start = new Date(this.event.startDate);
  const end = new Date(this.event.endDate);
  let dates = [];

  while (start <= end) {
    const eventDate = new Date(start);
    eventDate.setHours(0, 0, 0, 0);

    const startTime = new Date(eventDate);
    const [startHour, startMinute] = this.event.startTime.split(":");
    startTime.setHours(startHour, startMinute, 0, 0);

    if (startTime > now) { 
      const formattedDate = new Intl.DateTimeFormat("lt-LT", {
        year: "numeric",
        month: "2-digit",
        day: "2-digit",
      }).format(eventDate);
      
      const timeDiff = startTime - now;
      const hoursLeft = timeDiff / (1000 * 60 * 60); 

      dates.push({ date: formattedDate, hoursLeft });
    }

    start.setDate(start.getDate() + 1);
  }

  return dates;
}
},
  methods: {
async fetchEventDetails() {
  const eventId = this.$route.params.id;
  try {

    const response = await fetch(`https://localhost:7177/api/events/${eventId}`);
    if (!response.ok) throw new Error(`Failed to fetch event: ${response.statusText}`);

    const eventData = await response.json();

    if (!eventData || Object.keys(eventData).length === 0) {
      throw new Error("Event data is empty or invalid");
    }

    if (!eventData.volunteersCount) {
      console.warn("Warning: VolunteersCount is missing in event data.");
    }

    const start = new Date(eventData.startDate);
    const end = new Date(eventData.endDate);
    eventData.volunteersCountPerDate = {}; 

    while (start <= end) {
      const formattedDate = new Intl.DateTimeFormat("lt-LT", {
        year: "numeric",
        month: "2-digit",
        day: "2-digit",
      }).format(start);

      eventData.volunteersCountPerDate[formattedDate] = eventData.volunteersCount; 
      start.setDate(start.getDate() + 1);
    }

    this.event = eventData;
    this.$nextTick(() => {
      this.initMap();
    });

  } catch (error) {
    console.error("Klaida gaunant renginio informaciją:", error);
  }
},
    initMap() {
      if (!this.event || !this.event.latitude || !this.event.longitude) return;

       loader.load().then(() => {
        const location = { lat: parseFloat(this.event.latitude), lng: parseFloat(this.event.longitude) };

        const map = new google.maps.Map(document.getElementById("map"), {
          center: location,
          zoom: 14,
        });

        new google.maps.Marker({
          position: location,
          map: map,
          title: this.event.name,
        });
      });
    },
    getUserRoleFromToken() {
    const token = localStorage.getItem("accessToken") || sessionStorage.getItem("accessToken");
    if (!token) {
      return null;
    }

    try {
      const payloadBase64 = token.split(".")[1]; 
      if (!payloadBase64) {
        return null;
      }

      const decodedPayload = JSON.parse(atob(payloadBase64));
      const role = decodedPayload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || decodedPayload.role || null;
      return role;

    } catch (error) {
      console.error("[ERROR] Failed to decode token:", error);
      return null;
    }
  },
 async registerForEvent(date) {
  try {
    if (this.event.volunteersCountPerDate[date] !== undefined) {
      this.event.volunteersCountPerDate[date] = Math.max(0, this.event.volunteersCountPerDate[date] - 1);
    }

    this.$toast.success(`Sėkmingai užsiregistravote į veiklą ${date}`);
  } catch (error) {
    console.error("Registracijos klaida:", error);
    this.$toast.error("Registracija nepavyko");
  }
}
},
  mounted() {
    this.fetchEventDetails();
    this.userRole = this.getUserRoleFromToken();
  },
}
</script>

<style scoped>
.eventDetails {
  min-height: 80vh;
}

#map {
  width: 100%;
  height: 300px;
  border-radius: 8px;
}

.description-box {
  background-color: #f5f5f5;
  padding: 16px;
  border-radius: 8px;
  margin: 12px 0;
}

.description-text {
  font-size: 16px;
  color: #424242;
  line-height: 1.5;
}

.fancy-button {
  font-weight: bold;
  text-transform: none;
  letter-spacing: 0.5px;
  border-radius: 20px; 
  transition: all 0.3s ease-in-out;
}

.fancy-button:hover {
  background-color: #2e7d32 !important;
  color: white !important;
}
</style>
