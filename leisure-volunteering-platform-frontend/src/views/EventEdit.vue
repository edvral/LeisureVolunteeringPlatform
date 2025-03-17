<template>
  <v-container>
    <v-btn color="primary" outlined class="mb-3" @click="goBack">
      <v-icon left>mdi-arrow-left</v-icon> Atgal
    </v-btn>

    <h1 class="font-weight-bold mb-5 text-center">Redaguoti veiklą</h1>

    <v-card class="pa-5 mx-auto elevation-4 fancy-card">
      <v-form @submit.prevent="submitEvent">
        
        <v-text-field 
          v-model="event.name" 
          label="Pavadinimas" 
          prepend-inner-icon="mdi-pencil"
          outlined dense
          required>
        </v-text-field>

        <v-textarea 
          v-model="event.description" 
          label="Aprašymas" 
          prepend-inner-icon="mdi-text"
          outlined dense auto-grow
          rows="3"
          required>
        </v-textarea>

        <v-text-field 
          v-model="event.volunteersCount" 
          label="Savanorių Skaičius" 
          prepend-inner-icon="mdi-account-group"
          type="number" min="1" 
          outlined dense required>
        </v-text-field>

        <v-row dense> 
          <v-col cols="6">
            <v-menu v-model="startDateMenu" :close-on-content-click="false">
              <template v-slot:activator="{ props }">
                <v-text-field
                  v-model="formattedStartDate"
                  label="Pradžios data"
                  prepend-inner-icon="mdi-calendar"
                  readonly outlined dense
                  v-bind="props">
                </v-text-field>
              </template>
              <v-date-picker 
                v-model="event.startDate" 
                :min="new Date().toISOString().split('T')[0]" 
                @update:modelValue="updateFormattedStartDate">
              </v-date-picker>
            </v-menu>
          </v-col>

          <v-col cols="6">
            <v-menu v-model="endDateMenu" :close-on-content-click="false">
              <template v-slot:activator="{ props }">
                <v-text-field
                  v-model="formattedEndDate"
                  label="Pabaigos data"
                  prepend-inner-icon="mdi-calendar"
                  readonly outlined dense
                  v-bind="props">
                </v-text-field>
              </template>
              <v-date-picker 
                v-model="event.endDate" 
                :min="event.startDate" 
                @update:modelValue="updateFormattedEndDate">
              </v-date-picker>
            </v-menu>
          </v-col>
        </v-row>

        <v-row dense>
          <v-col cols="6">
            <v-menu v-model="startTimeMenu" :close-on-content-click="false">
              <template v-slot:activator="{ props }">
                <v-text-field
                  v-model="event.startTime"
                  label="Pradžios laikas"
                  prepend-inner-icon="mdi-clock-outline"
                  readonly
                  v-bind="props"
                  @click="startTimeMenu = true"
                ></v-text-field>
              </template>
              <v-time-picker
                v-model="event.startTime"
                format="24hr"
                @update:modelValue="startTimeMenu = false"
              ></v-time-picker>
            </v-menu>
          </v-col>

          <v-col cols="6">
            <v-menu v-model="endTimeMenu" :close-on-content-click="false">
              <template v-slot:activator="{ props }">
                <v-text-field
                  v-model="event.endTime"
                  label="Pabaigos laikas"
                  prepend-inner-icon="mdi-clock-outline"
                  readonly
                  v-bind="props"
                  @click="endTimeMenu = true"
                ></v-text-field>
              </template>
              <v-time-picker
                v-model="event.endTime"
                format="24hr"
                :min="event.startTime"
                @update:modelValue="endTimeMenu = false"
              ></v-time-picker>
            </v-menu>
          </v-col>
        </v-row>

        <v-text-field 
          id="autocomplete" 
          v-model="event.address" 
          label="Adresas" 
          prepend-inner-icon="mdi-map-marker"
          outlined dense required @input="updateAddressManually">
        </v-text-field>

        <div id="map"></div>
        <p><strong>📍 Pasirinkta vieta:</strong> {{ event.latitude }}, {{ event.longitude }}</p>

        <v-btn color="primary" block large type="submit" class="mt-4">
          <v-icon left>mdi-check</v-icon> Atnaujinti veiklą
        </v-btn>
      </v-form>
    </v-card>
  </v-container>
</template>

<script>
/* global google */
import loader from "@/utils/GoogleMapsLoader";
import { useToast } from "vue-toastification";

export default {
  name: "EditEvent",
  data() {
    return {
      toast: useToast(),
      event: {
        name: "",
        description: "",
        volunteersCount: 1,
        startDate: null,
        endDate: null,
        startTime: "", 
        endTime: "", 
        latitude: null,
        longitude: null,
        address: "",
      },
      startDateMenu: false,
      endDateMenu: false,
      startTimeMenu: false,
      endTimeMenu: false,
    };
  },
  computed: {
  formattedStartDate() {
    return this.event.startDate instanceof Date 
      ? new Date(this.event.startDate).toLocaleDateString("lt-LT", { year: "numeric", month: "2-digit", day: "2-digit" })
      : "";
  },
  formattedEndDate() {
    return this.event.endDate instanceof Date 
      ? new Date(this.event.endDate).toLocaleDateString("lt-LT", { year: "numeric", month: "2-digit", day: "2-digit" })
      : "";
  }
  },
  methods: {
    updateFormattedStartDate() {
    this.startDateMenu = false;
  },
  updateFormattedEndDate() {
    this.endDateMenu = false;
  },
    async loadMap() {
  try {
    await loader.load(); 

    const map = new google.maps.Map(document.getElementById("map"), {
      center: { lat: this.event.latitude || 54.6872, lng: this.event.longitude || 25.2797 },
      zoom: 10,
    });

    const marker = new google.maps.Marker({
      position: { lat: this.event.latitude || 54.6872, lng: this.event.longitude || 25.2797 },
      map: map,
      draggable: true,
    });

    const geocoder = new google.maps.Geocoder();
    const autocomplete = new google.maps.places.Autocomplete(
      document.getElementById("autocomplete")
    );

    const updateAddressFromLatLng = (latLng) => {
      geocoder.geocode({ location: latLng }, (results, status) => {
        if (status === "OK" && results[0]) {
          this.event.address = results[0].formatted_address;

          this.$nextTick(() => {
            document.getElementById("autocomplete").value = this.event.address;
          });
        } else {
          console.error("Geocoder failed due to:", status);
        }
      });
    };

    autocomplete.addListener("place_changed", () => {
      const place = autocomplete.getPlace();
      if (place.geometry) {
        const latLng = place.geometry.location;
        map.setCenter(latLng);
        marker.setPosition(latLng);

        this.event.latitude = latLng.lat();
        this.event.longitude = latLng.lng();
        this.event.address = place.formatted_address;

        document.getElementById("autocomplete").value = this.event.address;
      }
    });

    google.maps.event.addListener(marker, "dragend", (event) => {
      const latLng = event.latLng;
      this.event.latitude = latLng.lat();
      this.event.longitude = latLng.lng();
      updateAddressFromLatLng(latLng);
    });

    google.maps.event.addListener(map, "click", (event) => {
      const latLng = event.latLng;
      marker.setPosition(latLng);
      this.event.latitude = latLng.lat();
      this.event.longitude = latLng.lng();
      updateAddressFromLatLng(latLng);
    });

    if (this.event.latitude && this.event.longitude) {
      const initialLocation = new google.maps.LatLng(this.event.latitude, this.event.longitude);
      updateAddressFromLatLng(initialLocation);
    }
  } catch (error) {
    console.error("❌ Error loading Google Maps:", error);
  }
},
    async loadEventData() {
      const eventId = this.$route.params.id;
      try {
        const response = await fetch(`https://localhost:7177/api/events/${eventId}`);
        if (!response.ok) throw new Error("Nepavyko gauti veiklos duomenų");

         const data = await response.json();

         const startDateObject = data.startDate ? new Date(data.startDate) : null;
         const endDateObject = data.endDate ? new Date(data.endDate) : null;

        this.event = {
        ...data,
        startDate: startDateObject,  
        endDate: endDateObject       
        };

      } catch (error) {
        console.error("❌ Klaida:", error);
        this.toast.error("Klaida gaunant veiklos duomenis");
      }
    },
    async submitEvent() {
  const eventId = this.$route.params.id;
  try {

    const startDateObject = this.event.startDate instanceof Date ? this.event.startDate : new Date(this.event.startDate);
    const endDateObject = this.event.endDate instanceof Date ? this.event.endDate : new Date(this.event.endDate);
  
   const formattedStartDate = startDateObject.toLocaleDateString('lt-LT', {
      year: "numeric",
      month: "2-digit",
      day: "2-digit"
    }).split('.').reverse().join('-'); 

    const formattedEndDate = endDateObject.toLocaleDateString('lt-LT', {
      year: "numeric",
      month: "2-digit",
      day: "2-digit"
    }).split('.').reverse().join('-'); 

    const eventData = {
      name: this.event.name,
      description: this.event.description || "",
      volunteersCount: this.event.volunteersCount,
      startDate: formattedStartDate,
      endDate: formattedEndDate,
      startTime: String(this.event.startTime),
      endTime: String(this.event.endTime),
      latitude: this.event.latitude,
      longitude: this.event.longitude,
      address: this.event.address || "",
    };

    const token = localStorage.getItem("accessToken") || sessionStorage.getItem("accessToken");
    const response = await fetch(`https://localhost:7177/api/events/${eventId}`, {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
        "Authorization": `Bearer ${token}`
      },
      body: JSON.stringify(eventData),
    });

    if (!response.ok) {
      const errorData = await response.json();
      throw new Error(errorData.message || "Klaida atnaujinant veiklą");
    }

    this.toast.success("Veikla sėkmingai atnaujinta!");
    this.$router.push("/events");
  } catch (error) {
    console.error("❌ Klaida:", error);
    this.toast.error(error.message);
  }
},
    goBack() {
      this.$router.push("/events");
    }
  },
  mounted() {
    this.loadEventData();
    this.loadEventData().then(() => {
    this.loadMap(); 
  });
  }
};
</script>

<style scoped>
#map {
  width: 100%;
  height: 400px;
  margin-top: 10px;
}
</style>
