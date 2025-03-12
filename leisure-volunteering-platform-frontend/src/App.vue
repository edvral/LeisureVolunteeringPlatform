<template>
  <v-app>
    <v-app-bar app color="primary" dark>
      <v-container class="d-flex align-center">
        
        <v-app-bar-nav-icon v-if="isMobile" @click="drawer = !drawer"></v-app-bar-nav-icon>
        
        <v-icon class="mr-1">mdi-hand-heart</v-icon>
        <v-toolbar-title>Savanorystės Platforma</v-toolbar-title>

        <v-spacer></v-spacer>

        <v-tabs v-if="!isMobile" v-model="currentTab" class="d-none d-md-flex">
          <v-tab class="mx-1 nav-tab" @click="navigateTo('/')">Pagrindinis</v-tab>
          <v-tab class="mx-1 nav-tab" @click="navigateTo('/events')">Artimiausios savanoriškos veiklos</v-tab>
        </v-tabs>

        <div v-if="!isMobile" class="d-flex align-center ">
          <v-btn v-if="!isAuthenticated" color="white" class="login-button mx-3" elevation="4" large outlined @click="navigateTo('/auth')">
            <v-icon left>mdi-login</v-icon> Prisijungti
          </v-btn>

         <div v-else class="d-flex flex-column align-center ml-2">
          <v-btn color="red" class="logout-button mx-3" elevation="4" large outlined @click="handleLogout">
          <v-icon left>mdi-logout</v-icon> Atsijungti
        </v-btn>

        <small class="mt-1 text-center username-text">
         Prisijungęs kaip: <strong>{{ username }}</strong>
        </small>
        </div>
        
        </div>
      </v-container>
    </v-app-bar>

    <v-navigation-drawer v-model="drawer" app temporary class="d-md-none">
      <v-list>
        <v-list-item to="/" @click="drawer = false">Pagrindinis</v-list-item>
        <v-list-item to="/events" @click="drawer = false">Artimiausios savanoriškos veiklos</v-list-item>

        <v-list-item v-if="!isAuthenticated" to="/auth" @click="drawer = false">
          <v-icon left>mdi-login</v-icon> Prisijungti
        </v-list-item>
        <v-list-item v-else @click="handleLogout">
          <v-icon left>mdi-logout</v-icon> Atsijungti
        </v-list-item>
      </v-list>
    </v-navigation-drawer>

    <v-main class="main-content">
      <router-view @auth-updated="checkAuthStatus"></router-view>
    </v-main>

    <v-footer color="primary" dark class="footer">
      <v-container>
        <v-row align="center" justify="space-between">
          <v-col cols="12" sm="6" class="text-left">
            <p>© {{ new Date().getFullYear() }} Laisvalaikio Savanorystės Platforma</p>
          </v-col>
          <v-col cols="12" sm="6" class="footer-links text-right">
            <span @click="navigateTo('/about')">Apie mus</span> | 
            <span @click="navigateTo('/contact')">Kontaktai</span> | 
            <span @click="navigateTo('/faq')">DUK</span>
            <v-icon class="social-icon" @click="openLink('https://facebook.com')" size="20">mdi-facebook</v-icon>
            <v-icon class="social-icon" @click="openLink('https://instagram.com')" size="20">mdi-instagram</v-icon>
            <v-icon class="social-icon" @click="openLink('https://twitter.com')" size="20">mdi-twitter</v-icon>
          </v-col>
        </v-row>
      </v-container>
    </v-footer>
  </v-app>
</template>

<script>
import { useToast } from "vue-toastification";
export default {
  name: "App",
  data() {
    return {
      toast: useToast(),
      currentTab: 0,
      drawer: false,
      isMobile: window.innerWidth < 960, 
      isAuthenticated: false,
      username: "",
    };
  },
  methods: {
     openLink(url) {
    window.open(url, "_blank"); 
  },
    navigateTo(route) {
      this.$router.push(route);
    },
    checkScreenSize() {
      this.isMobile = window.innerWidth < 960; 
    },
    checkAuthStatus() {
    const accessToken = localStorage.getItem("accessToken") || sessionStorage.getItem("accessToken");
    const storedUsername = localStorage.getItem("username") || sessionStorage.getItem("username");

    this.isAuthenticated = !!accessToken; 
    this.username = storedUsername || "Vartotojas";
    },
    async handleLogout() {
    const accessToken = localStorage.getItem("accessToken") || sessionStorage.getItem("accessToken");
    const refreshToken = localStorage.getItem("refreshToken") || sessionStorage.getItem("refreshToken");

  if (!refreshToken) {
    this.clearAuthData();
    return;
  }

  try {
    const response = await fetch("https://localhost:7177/api/auth/logout", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "Authorization": `Bearer ${accessToken}`
      },
      body: JSON.stringify(refreshToken), 
    });

    let result = response.status === 204 ? {} : await response.json();

    if (!response.ok) throw new Error(result?.message || "Atsijungimas nepavyko.");

    this.clearAuthData();
    this.toast.success("Sėkmingai atsijungėte!");

  } catch (error) {
    console.error("Logout error:", error);
    this.clearAuthData();
  }
},
    clearAuthData() {
      localStorage.removeItem("accessToken");
      localStorage.removeItem("refreshToken");
      localStorage.removeItem("username");
      sessionStorage.removeItem("accessToken");
      sessionStorage.removeItem("refreshToken");
      sessionStorage.removeItem("username");

      this.checkAuthStatus(); 
      this.$router.push("/");
    },
     getTokenExpiration(token) {
      if (!token) return null;
      const payloadBase64 = token.split(".")[1]; 
      if (!payloadBase64) return null;

      try {
        const decodedPayload = JSON.parse(atob(payloadBase64)); 
        return decodedPayload.exp * 1000; 
      } catch (error) {
        console.error("Error decoding token:", error);
        return null;
      }
    },
    async checkAndRefreshToken() {
      const accessToken = localStorage.getItem("accessToken") || sessionStorage.getItem("accessToken");
      const refreshToken = localStorage.getItem("refreshToken") || sessionStorage.getItem("refreshToken");

      if (!accessToken || !refreshToken) return;

      const expirationTime = this.getTokenExpiration(accessToken);
      if (!expirationTime) return;

      const currentTime = Date.now();
      const timeLeft = expirationTime - currentTime;

      if (timeLeft < 5 * 60 * 1000) {
        await this.refreshAccessToken();
      }
    },
    async refreshAccessToken() {
      const refreshToken = localStorage.getItem("refreshToken") || sessionStorage.getItem("refreshToken");
      if (!refreshToken) return;

      try {
        const response = await fetch("https://localhost:7177/api/auth/refresh", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(refreshToken),
        });

        let result = await response.json();
        if (!response.ok) throw new Error(result?.message || "Žetono atnaujinimas nepavyko!");

        if (localStorage.getItem("refreshToken")) {
          localStorage.setItem("accessToken", result.token);
          localStorage.setItem("refreshToken", result.refreshToken);
        } else {
          sessionStorage.setItem("accessToken", result.token);
          sessionStorage.setItem("refreshToken", result.refreshToken);
        }

        console.log("✅ Access token refreshed successfully.");
      } catch (error) {
        console.error("Token refresh error:", error);
      }
    },
  },
  mounted() {
    this.checkAuthStatus();
    window.addEventListener("resize", this.checkScreenSize); 
    this.checkAndRefreshToken();
    setInterval(this.checkAndRefreshToken, 60 * 1000);
  },
  beforeUnmount() {
    window.removeEventListener("resize", this.checkScreenSize); 
  },
  watch: {
    isAuthenticated() {
      this.checkAuthStatus();
    },
  },
};
</script>

<style>
.v-application {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
}

.v-main {
  flex-grow: 1;
}

.footer {
  position: relative;
  bottom: 0;
  width: 100%;
}

.footer-links span {
  cursor: pointer;
  margin: 0 5px;
  color: white;
}

.footer-links span:hover {
  text-decoration: underline;
}

.nav-tabs {
  display: flex;
  gap: 20px; 
}

.nav-tab {
  transition: all 0.3s ease-in-out;
}

.nav-tab:hover {
  background-color: rgba(255, 255, 255, 0.2); 
  border-radius: 8px;
}

.social-icon {
  margin-left: 15px;
  cursor: pointer;
  color: white;
  transition: color 0.3s ease-in-out;
}

.social-icon:hover {
  color: #fbc02d;
}

.login-button {
  font-size: 18px;
  font-weight: bold;
  text-transform: uppercase;
  padding: 10px 20px;
  border: 2px solid white;
  color: white !important;
  background-color: rgba(255, 255, 255, 0.1);
  transition: all 0.3s ease-in-out;
}

.login-button:hover {
  background-color: white !important;
  color: #1565C0 !important;
}

.logout-button {
  font-size: 18px;
  font-weight: bold !important;
  text-transform: uppercase;
  padding: 10px 20px;
  border: 2px solid red;
  color: white !important;
  background-color: rgba(255, 0, 0, 0.1);
  transition: all 0.3s ease-in-out;
}

.logout-button:hover {
  background-color: red !important;
  color: white !important;
}
</style>
