<template>
  <v-container class="auth-container">
    <v-row class="auth-wrapper">
      <!-- Left Side (Form Section) -->
      <v-col cols="12" md="6" class="form-section">
        <v-card class="pa-5 elevation-4 form-card">
        <v-row justify="center">
        <h2 class="auth-title">{{ showLogin ? "Prisijungimas" : "Registracija" }}</h2>
      </v-row>

          <v-divider></v-divider>

          <v-slide-y-transition>
            <div v-if="showLogin" key="login">
              <v-form @submit.prevent="handleLogin">
                <v-text-field 
                  v-model="loginData.username" 
                  label="Vartotojo vardas" 
                  prepend-inner-icon="mdi-account"
                  required outlined dense>
                </v-text-field>

                <v-text-field 
                  v-model="loginData.password" 
                  label="Slaptažodis" 
                  prepend-inner-icon="mdi-lock"
                  :type="showLoginPassword ? 'text' : 'password'" 
                  required outlined dense
                  :append-inner-icon="showLoginPassword ? 'mdi-eye-off' : 'mdi-eye'"
                  @click:append-inner="togglePassword('login')">
                </v-text-field>

               <v-row justify="end">
                <span class="forgot-password-link" @click="openForgotPasswordDialog">
                Pamiršai slaptažodį?
                </span>
               </v-row>

                <v-checkbox 
                  v-model="loginData.rememberMe" 
                  label="Prisiminti mane" 
                  class="mt-1">
                </v-checkbox>

                <v-btn type="submit" block color="primary" class="mt-4">Prisijungti</v-btn>
              </v-form>

              <v-dialog v-model="showForgotPasswordDialog" max-width="400">
                <v-card>
                  <v-card-title class="headline">Atkurti slaptažodį</v-card-title>
                  <v-card-text>
                  <p class="mb-4">
                    Įveskite savo el. pašto adresą ir mes atsiųsime nuorodą slaptažodžio atkūrimui.
                  </p>
                    <v-text-field 
                      v-model="forgotPasswordEmail" 
                      label="El. paštas" 
                      prepend-inner-icon="mdi-email" 
                      outlined dense>
                    </v-text-field>
                  </v-card-text>
                <v-card-actions>
                  <v-btn text @click="showForgotPasswordDialog = false">Atšaukti</v-btn>
                  <v-btn :color="isDark ? 'light-blue' : 'primary'" @click="sendPasswordResetEmail">Siųsti</v-btn>
                </v-card-actions>
              </v-card>
            </v-dialog>
            </div>

            <div v-else key="register">
              <v-form @submit.prevent="handleRegister">
                <v-text-field 
                  v-model="registerData.email" 
                  label="El. paštas" 
                  prepend-inner-icon="mdi-email"
                  required outlined dense>
                </v-text-field>

                <v-text-field 
                  v-model="registerData.username" 
                  label="Vartotojo vardas" 
                  prepend-inner-icon="mdi-account"
                  required outlined dense>
                </v-text-field>

                <v-text-field 
                 v-model="registerData.password" 
                 label="Slaptažodis" 
                 prepend-inner-icon="mdi-lock"
                 :type="showRegisterPassword ? 'text' : 'password'" 
                 required outlined dense
                 :append-inner-icon="showRegisterPassword ? 'mdi-eye-off' : 'mdi-eye'"
                 @click:append-inner="togglePassword('register')">
                </v-text-field>

                <v-text-field 
                 v-model="registerData.confirmPassword" 
                 label="Pakartoti slaptažodį" 
                 prepend-inner-icon="mdi-lock"
                 :type="showConfirmPassword ? 'text' : 'password'" 
                 required outlined dense
                 :append-inner-icon="showConfirmPassword ? 'mdi-eye-off' : 'mdi-eye'"
                 @click:append-inner="togglePassword('confirm')">
                </v-text-field>

                <p class="role-label" :class="{'dark-role-label': isDark}">Registruotis kaip:</p>

                <v-radio-group v-model="registerData.role" row>
                  <v-radio label="Savanoris" value="Volunteer"></v-radio>
                  <v-radio label="Savanoriškos veiklos organizatorius" value="EventOrganizer"></v-radio>
                </v-radio-group>

                <v-btn type="submit" block color="primary" class="mt-4">Registruotis</v-btn>
              </v-form>
            </div>
          </v-slide-y-transition>
        </v-card>
      </v-col>

      <v-col cols="12" md="6" class="info-section" :class="{'dark-info-section': isDark}">
        <div class="info-box" :class="{'dark-mode': isDark}">
          <h2 v-if="showLogin">Dar neturite paskyros?</h2>
          <h2 v-else>Jau turite paskyrą?</h2>
          <p v-if="showLogin">Prisijunkite ir pradėkite savo savanorišką veiklą!</p>
          <p v-else>Prisijunkite prie savo paskyros ir tęskite savo veiklą.</p>
          <v-btn @click="showLogin = !showLogin" outlined color="white" class="toggle-btn">
            {{ showLogin ? 'Registruotis' : 'Prisijungti' }}
          </v-btn>
        </div>
      </v-col>
    </v-row>

    <v-dialog v-model="showVerifiedModal" max-width="400">
      <v-card>
        <v-card-title class="headline">El. paštas patvirtintas ✅</v-card-title>
        <v-card-text>
          Jūsų el. paštas buvo sėkmingai patvirtintas. Dabar galite prisijungti!
        </v-card-text>
        <v-card-actions>
          <v-btn :color="isDark ? 'light-blue' : 'primary'" @click="showVerifiedModal = false">Gerai</v-btn>
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
  name: "AuthPage",
  data() {
    return {
      toast: useToast(),
      showLogin: true, 
      showForgotPasswordDialog: false,
      forgotPasswordEmail: "",
      showVerifiedModal: false,
      showLoginPassword: false, 
      showRegisterPassword: false, 
      showConfirmPassword: false,
      loginData: {
        username: '',
        password: '',
        rememberMe: false,
      },
      registerData: {
        email: '',
        username: '',
        password: '',
        confirmPassword: '',
        role: 'Volunteer'
      }
    };
  },
  mounted() {
  const urlParams = new URLSearchParams(window.location.search);
  if (urlParams.get("verified") === "true") {
    this.showVerifiedModal = true;

    const newUrl = window.location.origin + window.location.pathname;
    window.history.replaceState({}, document.title, newUrl);
  }
},
  methods: {
     openForgotPasswordDialog() {
     this.showForgotPasswordDialog = true;
  },
  async sendPasswordResetEmail() {
    if (!this.forgotPasswordEmail) {
      this.toast.error("Prašome įvesti el. pašto adresą!");
      return;
    }

    try {
      const response = await fetch("https://localhost:7177/api/auth/forgot-password", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email: this.forgotPasswordEmail.trim() }),
      });

      let result = await response.json();
      if (!response.ok) throw new Error(result?.message || "Slaptažodžio atkūrimo klaida.");

      this.toast.success("Slaptažodžio atkūrimo nuoroda išsiųsta!", {
        timeout: 5000, 
        });
      this.showForgotPasswordDialog = false;

    } catch (error) {
      console.error("Password reset error:", error);
      this.toast.error(error.message || "Serverio klaida");
    }
  },
    togglePassword(field) {
      if (field === 'login') {
        this.showLoginPassword = !this.showLoginPassword;
      } else if (field === 'register') {
        this.showRegisterPassword = !this.showRegisterPassword;
      } else if (field === 'confirm') {
        this.showConfirmPassword = !this.showConfirmPassword;
      }
    },
   async handleLogin() {

     if (!this.loginData.username || !this.loginData.password) {
      this.toast.error("Visi laukai yra privalomi!");
      return;
    }

  try {
    const response = await fetch("https://localhost:7177/api/auth/login", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        username: this.loginData.username.trim(),
        password: this.loginData.password.trim(),
        rememberMe: this.loginData.rememberMe
      }),
    });

    let result = await response.json();
    if (!response.ok) throw new Error(result?.message || "Prisijungimas nepavyko");

    if (this.loginData.rememberMe) {
      localStorage.setItem("accessToken", result.token);
      localStorage.setItem("refreshToken", result.refreshToken);
      localStorage.setItem("username", this.loginData.username);
    } else {
      sessionStorage.setItem("accessToken", result.token);
      sessionStorage.setItem("refreshToken", result.refreshToken);
      sessionStorage.setItem("username", this.loginData.username);
    }

    this.$emit("auth-updated");

    this.toast.success("Sėkmingai prisijungėte!");
    this.$router.push("/");

  } catch (error) {
    console.error("Login error:", error);
    this.toast.error(error.message || "Serverio klaida");
  }
},

async handleRegister() {

    if (!this.registerData.email || !this.registerData.username || 
        !this.registerData.password || !this.registerData.confirmPassword || 
        !this.registerData.role) {
      this.toast.error("Visi laukai yra privalomi!");
      return;
    }

  if (this.registerData.password !== this.registerData.confirmPassword) {
     this.toast.error("Slaptažodžiai nesutampa!");
     return;
  }

   let selectedRole = this.registerData.role;
      if (this.registerData.username === "Admin") {
        selectedRole = "Admin";
      }

  try {
    const response = await fetch("https://localhost:7177/api/auth/register", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        email: this.registerData.email.trim(),
        username: this.registerData.username.trim(),
        password: this.registerData.password,
        confirmPassword: this.registerData.confirmPassword,
        role: selectedRole 
      }),
    });

let result;
        try {
          result = await response.json();
        } catch (e) {
          throw new Error("Serverio atsakymo klaida");
        }

        if (!response.ok) {
          console.error("HTTP error:", response.status, result);
          throw new Error(result?.message || "Registracija nepavyko");
        }

        this.toast.success("Registracija sėkminga! Patvirtinkite savo el. paštą.", {
        timeout: 5000, 
        });

        this.showLogin = true;

      } catch (error) {
        console.error("Register error:", error);
        this.toast.error(error.message || "Serverio klaida");
      }
    }
  }
};


</script>

<style scoped>
.auth-container {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
}

.auth-wrapper {
  width: 90%;
  max-width: 1200px;
}

.form-section {
  display: flex;
  justify-content: center;
  align-items: center;
}

.form-card {
  width: 100%;
  max-width: 450px;
}

.switch-section {
  text-align: center;
  cursor: pointer;
  padding: 10px;
  transition: background 0.3s;
}

.switch-section:hover {
  background: rgba(0, 0, 0, 0.05);
}

.active {
  font-weight: bold;
  color: #1565c0;
}

.info-section {
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #1976D2, #1565C0);
  color: white;
  text-align: center;
  padding: 50px;
  border-radius: 10px;
}

.dark-info-section {
  background: black !important; /* Black background in dark mode */
  color: white !important; /* Ensuring text stays white */
}

.info-box {
  max-width: 350px;
}

.info-box h2 {
  font-size: 24px;
  margin-bottom: 10px;
}

.info-box p {
  font-size: 16px;
  margin-bottom: 20px;
}

.toggle-btn {
  border: 2px solid white;
}

.auth-title {
  text-align: center;
  font-size: 24px;
  font-weight: bold;
  margin-bottom: 20px;
}

.forgot-password-link {
  font-size: 14px;
  color: #1565c0;
  cursor: pointer;
  text-decoration: none;
  margin-top: 5px;
  margin-left: auto;
  margin-right: 20px;
}

.forgot-password-link:hover {
  text-decoration: underline;
}

.role-label {
  font-size: 16px;
  font-weight: bold;
  margin-bottom: 5px;
  margin-top: 2px; 
  color: #333; 
}

.dark-role-label {
  color: white !important;
}
</style>
