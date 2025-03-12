<template>
  <v-container class="reset-container">
    <v-row justify="center">
      <v-col cols="12" md="6">
        <v-card class="pa-5 elevation-4">
          <v-row justify="center">
            <h2 class="reset-title">Atkurti slaptažodį</h2>
          </v-row>

          <v-divider></v-divider>

          <v-form @submit.prevent="handleResetPassword">
            <v-text-field 
              v-model="newPassword" 
              label="Naujas slaptažodis" 
              prepend-inner-icon="mdi-lock"
              :type="showNewPassword ? 'text' : 'password'"
              required outlined dense
              :append-inner-icon="showNewPassword ? 'mdi-eye-off' : 'mdi-eye'"
              @click:append-inner="showNewPassword = !showNewPassword">
            </v-text-field>

            <v-text-field 
              v-model="confirmPassword" 
              label="Pakartokite slaptažodį" 
              prepend-inner-icon="mdi-lock"
              :type="showConfirmPassword ? 'text' : 'password'"
              required outlined dense
              :append-inner-icon="showConfirmPassword ? 'mdi-eye-off' : 'mdi-eye'"
              @click:append-inner="showConfirmPassword = !showConfirmPassword">
            </v-text-field>

            <v-btn type="submit" block color="primary" class="mt-4">Atkurti slaptažodį</v-btn>
          </v-form>

          <p v-if="message" class="reset-message" :class="{'success': success, 'error': !success}">
            {{ message }}
          </p>

        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script>
export default {
  name: "ResetPassword",
  data() {
    return {
      newPassword: "",
      confirmPassword: "",
      showNewPassword: false,
      showConfirmPassword: false,
      message: "",
      success: false,
      token: "",
    };
  },
  mounted() {
    const urlParams = new URLSearchParams(window.location.search);
    this.token = urlParams.get("token");

    if (!this.token) {
      this.message = "⚠️ Netinkama arba pasibaigusi slaptažodžio atkūrimo nuoroda.";
      this.success = false;
    }
  },
  methods: {
    async handleResetPassword() {
      if (!this.newPassword || !this.confirmPassword) {
        this.message = "❌ Visi laukai yra privalomi!";
        this.success = false;
        return;
      }

      if (this.newPassword !== this.confirmPassword) {
        this.message = "❌ Slaptažodžiai nesutampa!";
        this.success = false;
        return;
      }

      try {
        const response = await fetch("https://localhost:7177/api/auth/reset-password", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            token: this.token,
            newPassword: this.newPassword,
          }),
        });

        let result = await response.json();
        if (!response.ok) throw new Error(result?.message || "Slaptažodžio atkūrimas nepavyko!");

        this.message = "✅ Slaptažodis atkurtas sėkmingai! Galite prisijungti.";
        this.success = true;

        setTimeout(() => this.$router.push("/auth"), 3000);

      } catch (error) {
        console.error("Reset password error:", error);
        this.message = error.message || "⚠️ Klaida atkuriant slaptažodį.";
        this.success = false;
      }
    },
  },
};
</script>

<style scoped>
.reset-container {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
}

.reset-title {
  text-align: center;
  font-size: 24px;
  font-weight: bold;
  margin-bottom: 20px;
}

.reset-message {
  text-align: center;
  margin-top: 15px;
  font-weight: bold;
}

.success {
  color: green;
}

.error {
  color: red;
}
</style>
