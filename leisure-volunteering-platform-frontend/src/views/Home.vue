<template>
  <v-container fluid class="home">
    <v-row align="center" justify="center" class="content">
      
      <v-col cols="12" md="6" class="text-center">
       <div class="text-group">
        <h1 class="font-weight-bold mb-5">Laisvalaikio Savanorystės Platforma</h1>
        <p class="text-h5 mb-14">Prisijunkite ir keiskite pasaulį kartu!</p>
       </div>

        <v-row justify="center" class="mt-7 benefits">
          <v-col cols="12" sm="4" class="text-center">
            <v-icon size="50" color="primary">mdi-account-group</v-icon>
            <p>Prisijunkite prie bendruomenės</p>
          </v-col>
          <v-col cols="12" sm="4" class="text-center">
            <v-icon size="50" color="primary">mdi-hand-heart</v-icon>
            <p>Padėkite kitiems</p>
          </v-col>
          <v-col cols="12" sm="4" class="text-center">
            <v-icon size="50" color="primary">mdi-star-circle</v-icon>
            <p>Gaukite įvertinimus ir ženklelius</p>
          </v-col>
        </v-row>
      </v-col>

      <v-col cols="12" md="6" class="d-flex justify-end image-container">
        <v-img :src="imageSrc" class="homepage-image"></v-img>
      </v-col>

    </v-row>
  </v-container>
</template>

<script>
import homepageImage from "@/assets/istockphoto-1389138499-612x612.jpg";

export default {
  name: "AppHome",
  data() {
    return {
      imageSrc: homepageImage,
    };
  },
};
</script>

<style scoped>
.home {
  min-height: 80vh; 
  display: flex;
  flex-direction: column;
  justify-content: space-between; 
}

.content {
  flex-grow: 1;
  display: flex;
  flex-wrap: wrap; 
  align-items: center;
  justify-content: center;
}

.benefits {
  margin-top: auto !important;
  padding-bottom: 2rem;
  margin-left:10px;
}

.image-container {
  display: flex;
  justify-content: center;
}

.homepage-image {
  max-width: 80%;
  height: auto;
  margin-right:50px;
}

.text-group {
  margin-left: 45px; 
}

@media (max-width: 768px) {
  .content {
    flex-direction: column-reverse; 
  }

   .text-group {
    margin-left: 0; 
    text-align: center;
  }

  .homepage-image {
    max-width: 90%; 
    margin: 0 auto; 
  }
}
</style>
