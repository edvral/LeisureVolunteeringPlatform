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
      <v-icon left color="success" class="mr-2">mdi-calendar-clock</v-icon> Datos ir laikai
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
        v-if="userRole === 'Volunteer' && dateObj.hoursLeft > 24 && event.volunteersCountPerDate[dateObj.date] > 0 && !pendingApproval[dateObj.date]"
        color="success"
        small
        outlined
        class="fancy-button"
        @click="openRegistrationForm(dateObj.date)"
      > 
        <v-icon left>mdi-account-plus</v-icon> Registruotis šiai veiklai
      </v-btn>

      <p v-else-if="pendingApproval[dateObj.date]" class="text-warning font-weight-bold">
      ⏳ Laukiama atsakymo iš organizatoriaus...
      </p>

      <p v-else-if="userRole === 'Volunteer' && (event.volunteersCountPerDate[dateObj.date] <= 0 || dateObj.hoursLeft <= 24)" class="text-error font-weight-bold">
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

<div v-else class="text-center text-red font-weight-bold mt-4">
  📅 Patikslinkite datas ir laikus!
</div>

    </v-card>

    <v-alert v-else type="error">Renginys nerastas.</v-alert>
  </v-container>

<v-dialog v-model="showRegistrationForm" max-width="500">
  <v-card class="elevated-card">
    <v-card-title class="text-h5 d-flex align-center">
      <v-icon color="primary" class="mr-2">mdi-account-plus</v-icon>
      Registracija savanoriškai veiklai
    </v-card-title>

    <v-divider></v-divider>

    <v-card-text>
      <v-form ref="registrationForm">
        <v-text-field 
          v-model="registrationData.name" 
          label="Vardas" 
          required 
          outlined 
          dense
        ></v-text-field>

        <v-text-field 
          v-model="registrationData.surname" 
          label="Pavardė" 
          required 
          outlined 
          dense
        ></v-text-field>

        <v-text-field 
          v-model="registrationData.age" 
          label="Amžius" 
          type="number" 
          required 
          outlined 
          dense
          @input="validateForm"
        ></v-text-field>

        <v-textarea 
          v-model="registrationData.comment" 
          label="Papildoma informacija apie save (rekomenduojama)" 
          outlined 
          dense
        ></v-textarea>

        <p v-if="invalidAge" class="text-red font-weight-bold">⚠️ Amžius turi būti teigiamas skaičius!</p>
      </v-form>
    </v-card-text>

    <v-divider></v-divider>

    <v-card-actions class="d-flex justify-end">
      <v-btn color="grey darken-1" @click="showRegistrationForm = false">
        <v-icon left>mdi-close</v-icon> Atšaukti
      </v-btn>
      <v-btn 
        color="success" 
        :disabled="isSubmitDisabled" 
        @click="submitRegistration"
      >
        <v-icon left>mdi-check</v-icon> Registruotis į veiklą
      </v-btn>
    </v-card-actions>
  </v-card>
</v-dialog>
</template>

<script>
/* global google */
import { useToast } from "vue-toastification";
import loader from "@/utils/GoogleMapsLoader";

export default {
  name: "EventDetails",
  data() {
    return {
      toast: useToast(),
      event: null,
      userRole: null,
      showRegistrationForm: false,
      selectedDate: null,
      registrationData: {
      name: "",
      surname: "",
      age: "",
      comment: "",
    },
     invalidAge: false, 
     pendingApproval: {},
    };
  },
  computed: {
    isSubmitDisabled() {
    return (
      !this.registrationData.name.trim() ||
      !this.registrationData.surname.trim() ||
      !this.registrationData.age ||
      this.registrationData.age <= 0
    );
  },
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
    openRegistrationForm(date) {
    this.selectedDate = date;
    this.showRegistrationForm = true;
  },
  validateForm() {
    this.invalidAge = this.registrationData.age <= 0;
  },
async fetchEventDetails() {
  const eventId = this.$route.params.id;
  const token = localStorage.getItem("accessToken") || sessionStorage.getItem("accessToken");

  try {
    const response = await fetch(`https://localhost:7177/api/events/${eventId}`, {
      headers: token
        ? { "Authorization": `Bearer ${token}`, "Content-Type": "application/json" }
        : { "Content-Type": "application/json" }
    });
    if (!response.ok) throw new Error(`Failed to fetch event: ${response.statusText}`);

    const eventData = await response.json();
    if (!eventData || Object.keys(eventData).length === 0) {
      throw new Error("Event data is empty or invalid");
    }

    this.event = eventData;

    this.event.volunteersCountPerDate = eventData.volunteersCountPerDate || {};
    this.pendingApproval = eventData.pendingRegistrations || {};

     this.eventDates.forEach(dateObj => {
      if (this.event.volunteersCountPerDate[dateObj.date] === undefined) {
        this.event.volunteersCountPerDate[dateObj.date] = this.event.volunteersCount; 
      }
    });

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
 async submitRegistration() {
    if (!this.registrationData.name || !this.registrationData.surname || !this.registrationData.age) {
      this.toast.error("Užpildykite visus privalomus laukus!");
      return;
    }

    try {
      const token = localStorage.getItem("accessToken") || sessionStorage.getItem("accessToken");
      if (!token) {
        this.toast.error("Jūs turite būti prisijungęs!");
        return;
      }

      const userId = this.getUserIdFromToken();
      if (!userId) {
        this.toast.error("Nepavyko gauti vartotojo ID!");
        return;
      }

      const response = await fetch("https://localhost:7177/api/events/register", {
        method: "POST",
        headers: { 
          "Content-Type": "application/json",
          "Authorization": `Bearer ${token}`
        },
        body: JSON.stringify({
          userId: userId,
          eventId: this.event.id,
          eventDate: this.selectedDate,
          name: this.registrationData.name,
          surname: this.registrationData.surname,
          age: this.registrationData.age,
          comment: this.registrationData.comment,
        }),
      });

      let result = await response.json();
      if (!response.ok) throw new Error(result?.message || "Registracija į veiklą nepavyko!");

      this.pendingApproval[this.selectedDate] = true;

      this.toast.success("Registracija į veiklą pateikta!");

      this.showRegistrationForm = false;

      await this.fetchEventDetails();

    } catch (error) {
      console.error("Registracijos klaida:", error);
      this.toast.error(error.message || "Registracija nepavyko!");
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
