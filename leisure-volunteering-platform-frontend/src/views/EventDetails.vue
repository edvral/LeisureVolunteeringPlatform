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
        v-if="userId === event.organizerId"
        color="primary"
        small
        outlined
        class="fancy-button-volunteers"
        @click="fetchVolunteers(dateObj.date)"
      > 
        <v-icon left>mdi-account-group</v-icon> Savanorių sąrašas
      </v-btn>

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

      <p v-if="userRole !== 'Volunteer' && userId !== event.organizerId" class="text-warning font-weight-bold mt-2">
      🔒 Prisijunkite kaip savanoris norint užsiregistruoti į veiklą
      </p>
    </v-col>
  </v-row>

  <v-divider class="my-3"></v-divider>
</div>
</div>

<div v-else class="text-center text-red font-weight-bold mt-4">
  📅 Patikslinkite datas !
</div>

    </v-card>

    <v-alert v-else type="error">Renginys nerastas.</v-alert>

  <v-dialog v-model="showVolunteersModal" max-width="1000">
  <v-card class="elevated-card">
    <v-card-title class="text-h5 d-flex align-center">
      <v-icon color="primary" class="mr-2">mdi-account-group</v-icon>
      Savanorių sąrašas
    </v-card-title>

    <v-divider></v-divider>

    <v-card-text>
      <v-container fluid>
        <v-row v-if="volunteers.length > 0">
          <v-col v-for="volunteer in volunteers" :key="volunteer.name" cols="12">
            <v-card class="volunteer-card pa-3">
              <v-row align="center">
                <v-col cols="3" class="d-flex justify-center">
                  <v-avatar color="blue" size="50" class="volunteer-avatar">
                    <v-icon color="white">mdi-account</v-icon>
                  </v-avatar>
                </v-col>
                <v-col cols="9">
                  <p class="font-weight-bold text-primary">👤 {{ volunteer.name }} {{ volunteer.surname }}</p>
                  <p class="text-caption">🎂 <strong>Amžius:</strong> {{ volunteer.age }}</p>
                  <p class="text-caption">📝 <strong>Papildoma informacija:</strong> {{ volunteer.comment || "-" }}</p>
                </v-col>
              </v-row>

             <v-divider class="my-2"></v-divider>

             <v-row>
            <v-col cols="12">
            <p v-if="volunteer.isApproved === true" class="text-success font-weight-bold">
            ✅ Patvirtinta
            </p>

            <p v-else-if="volunteer.isApproved === false" class="text-error font-weight-bold">
            ❌ Atmesta
            </p>

           <v-row v-if="userId === event.organizerId && volunteer.isApproved === null">
            <v-col cols="12">
              <v-row align="center">
                <v-col cols="8">
                <v-textarea
                  v-model="volunteer.feedback"
                  label="📝 Palikti atsiliepimą"
                  outlined
                  dense
                ></v-textarea>
              </v-col>
              <v-col cols="4" class="d-flex align-center justify-end">
                <v-btn color="success" small class="approve-btn" @click="approveVolunteer(volunteer, true)">
                  <v-icon left>mdi-check</v-icon> Patvirtinti
                </v-btn>
                <v-btn color="red darken-1" small class="ml-2 reject-btn" @click="approveVolunteer(volunteer, false)">
            <v-icon left>mdi-close</v-icon> Atmesti
          </v-btn>
        </v-col>
      </v-row>
  </v-col>
</v-row>

<p v-if="volunteer.feedback" class="text-caption text-grey feedback-text">
  📝 Atsiliepimas: <em>{{ volunteer.feedback }}</em>
</p>
      </v-col>
      </v-row>
            </v-card>
          </v-col>
        </v-row>

        <v-row v-else>
          <v-col class="text-center text-grey">
            <p>📭 Nėra savanorių užsiregistravusių šiai datai.</p>
          </v-col>
        </v-row>
      </v-container>
    </v-card-text>

    <v-divider></v-divider>

    <v-card-actions class="d-flex justify-end">
      <v-btn color="grey darken-1" class="cancel-button" @click="showVolunteersModal = false">
        <v-icon left>mdi-close</v-icon> Uždaryti
      </v-btn>
    </v-card-actions>
  </v-card>
</v-dialog>

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
          prepend-inner-icon="mdi-account" 
          outlined 
          dense
        ></v-text-field>

        <v-text-field 
          v-model="registrationData.surname" 
          label="Pavardė" 
          prepend-inner-icon="mdi-account" 
          outlined 
          dense
        ></v-text-field>

        <v-text-field 
          v-model="registrationData.age" 
          label="Amžius" 
          type="number" 
          prepend-inner-icon="mdi-cake" 
          outlined 
          dense
          @input="validateForm"
        ></v-text-field>

        <v-textarea 
          v-model="registrationData.comment" 
          label="Papildoma informacija apie save (rekomenduojama)" 
          prepend-inner-icon="mdi-comment-text-outline" 
          outlined 
          dense
        ></v-textarea>

        <p v-if="invalidAge" class="text-red font-weight-bold">⚠️ Amžius turi būti teigiamas skaičius!</p>
      </v-form>
    </v-card-text>

    <v-divider></v-divider>

    <v-card-actions class="d-flex justify-end">
      <v-btn color="grey darken-1" class="cancel-button" @click="showRegistrationForm = false">
        <v-icon left>mdi-close</v-icon> Atšaukti
      </v-btn>
      <v-btn 
        color="success" 
        class="submit-button"
        :disabled="isSubmitDisabled" 
        @click="submitRegistration"
      >
        <v-icon left>mdi-check</v-icon> Registruotis
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
      showVolunteersModal: false,
      volunteers: [],
      toast: useToast(),
      event: null,
      userRole: null,
      userId: null,
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
    async approveVolunteer(volunteer, isApproved) {
    try {
      const token = localStorage.getItem("accessToken") || sessionStorage.getItem("accessToken");

      if (!volunteer.registrationId) {
        console.error("Volunteer registrationId is missing:", volunteer);
        this.toast.error("Registracijos ID nerastas!");
        return;
      }

      console.log("Approving Volunteer:", volunteer.registrationId, isApproved);

      const response = await fetch(`https://localhost:7177/api/events/${this.event.id}/volunteers/${volunteer.registrationId}/approve`, {
        method: "POST",
        headers: { 
          "Content-Type": "application/json",
          "Authorization": `Bearer ${token}`
        },
        body: JSON.stringify({
          isApproved: isApproved,
          feedback: volunteer.feedback || ""
        }),
      });

      console.log("API Response Status:", response.status);

      const result = await response.json();
      console.log("API Response Body:", result);

      if (!response.ok) throw new Error(result?.message || "Nepavyko atnaujinti registracijos būsenos!");

      this.toast.success(isApproved ? "Savanoris patvirtintas!" : "Savanoris atmestas!");

      volunteer.isApproved = isApproved;
      volunteer.feedback = volunteer.feedback || "";

      await this.fetchVolunteers(this.selectedDate);
    } catch (error) {
      console.error("Klaida patvirtinant savanorį:", error);
      this.toast.error("Nepavyko atnaujinti registracijos būsenos.");
    }
},
    async fetchVolunteers(date) {
    try {
      const token = localStorage.getItem("accessToken") || sessionStorage.getItem("accessToken");
      const formattedDate = date.split('.').reverse().join('-');

      const response = await fetch(`https://localhost:7177/api/events/${this.event.id}/volunteers/${formattedDate}`, {
        headers: { Authorization: `Bearer ${token}` }
      });
      if (!response.ok) throw new Error("Failed to fetch volunteers");

      const volunteersData = await response.json();
      
      this.volunteers = volunteersData.map(v => ({
        ...v,
        registrationId: v.registrationId  
      }));

      this.showVolunteersModal = true;
    } catch (error) {
      console.error("Error fetching volunteers:", error);
    }
  },
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
    this.userId = Number(this.getUserIdFromToken());
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

.fancy-button-volunteers {
  font-weight: bold;
  text-transform: none;
  letter-spacing: 0.5px;
  border-radius: 20px; 
  transition: all 0.3s ease-in-out;
}

.fancy-button-volunteers:hover {
  background-color: darken !important;
  color: white !important;
}

.submit-button {
  font-weight: bold;
  text-transform: none;
  border-radius: 20px; 
  transition: all 0.3s ease-in-out;
}

.submit-button:hover {
  background-color:rgb(46, 63, 125) !important;
  color: white !important;
}

.cancel-button {
  font-weight: bold;
  text-transform: none;
  border-radius: 20px;
  transition: all 0.3s ease-in-out;
}

.volunteer-card {
  background-color: white;
  border-radius: 12px;
  transition: background-color 0.3s ease-in-out, transform 0.3s ease-in-out, box-shadow 0.3s ease-in-out;
  box-shadow: 0px 2px 5px rgba(0, 0, 0, 0.1);
}

.volunteer-card:hover:not(:focus-within) {
  background-color: #e3f2fd;
  transform: scale(1.02);
}

.volunteer-card:focus-within {
  transform: none !important;
  box-shadow: 0px 6px 15px rgba(0, 0, 0, 0.15);
}

.volunteer-avatar {
  transition: 0.3s;
}

.volunteer-avatar:hover {
  transform: scale(1.1);
}

.cancel-button:hover {
  background-color: #b0bec5 !important;
  color: white !important;
}

.elevated-card {
  border-radius: 12px;
  box-shadow: 0px 4px 10px rgba(0, 0, 0, 0.1);
  transition: transform 0.2s ease-in-out, box-shadow 0.2s ease-in-out;
}

.elevated-card:hover:not(:focus-within) {
  transform: scale(1.02);
  box-shadow: 0px 6px 15px rgba(0, 0, 0, 0.15);
}

.elevated-card:focus-within {
  transform: none !important;
  box-shadow: 0px 6px 15px rgba(0, 0, 0, 0.15);
}
</style>
