<template>
  <v-container fluid class="profile">
    <h2 class="mb-4">👤 Mano profilis</h2>
    <v-card class="pa-5" elevation="3">
      <v-row>
     <v-col cols="12" md="6" class="level-info">
  <div class="info-item">
    <v-icon color="amber darken-3" class="mr-2">mdi-star-circle</v-icon>
    <span class="label">Taškai:</span> 
    <span class="value">{{ points }}</span>
  </div>

    <div class="info-item">
    <v-icon :color="getLevelColor(levelName)" class="mr-2">
    {{ currentLevelIcon }}
    </v-icon>
    <span class="label">Lygis:</span> 
    <span class="value level-text">{{ levelName }}</span>
    </div>
    </v-col>

    <v-col cols="12" md="6" class="d-flex justify-end align-center">
    <template v-if="!atMaxLevel">
    <v-tooltip location="top">
    <template v-slot:activator="{ props }">
      <v-progress-linear
        v-bind="props"
        :model-value="progress"
        color="success"
        height="25"
        rounded
        striped
        class="mb-2"
        style="max-width: 300px; width: 100%;"
      >
        <template v-slot:default>
          {{ points }} / {{ nextLevelPoints }} taškai
        </template>
      </v-progress-linear>
    </template>
    <span>🔥 {{ pointsToNextLevel }} taškų iki kito lygio</span>
  </v-tooltip>
</template>

<template v-else>
  <div class="max-level-message" :class="{ 'dark-max-level': isDark }">
    🏆 <strong>Pasiektas aukščiausias lygis!</strong>  
    <br>
    🎉 Sveikiname, esi tikra <span class="highlight">savanorystės legenda</span>!
  </div>
</template>

        </v-col>
      </v-row>
    </v-card>

    <v-card class="mt-4 pa-4" elevation="2">
      <h3 class="mb-2">🎯 Lygiai</h3>
      <v-list dense>
        <v-list-item 
          v-for="level in thresholds" 
          :key="level.id"
        >
          <v-icon left class="mr-2" :color="getLevelColor(level.levelName)">{{ level.icon }}</v-icon>
          <span :class="{ 'font-weight-bold': points >= level.minPoints && (points <= level.maxPoints || level.maxPoints === null) }">
            {{ level.levelName }} 
            ({{ level.minPoints }}{{ level.maxPoints === null ? '+' : ' – ' + level.maxPoints }} taškų)
          </span>
        </v-list-item>
      </v-list>
    </v-card>

    <v-card class="mt-4 pa-4" elevation="2">
  <h3 class="mb-2">🕓 Savanorystės istorija</h3>
  <v-table v-if="eventHistory.length">
    <thead>
      <tr>
        <th>Veikla</th>
        <th>Data</th>
        <th>Būsena</th>
        <th>Atsiliepimas</th> 
        <th>Taškai</th> 
      </tr>
    </thead>
    <tbody>
     <tr v-for="(entry, index) in sortedEventHistory" :key="index">
        <td><router-link v-if="entry.eventId" :to="`/event/${entry.eventId}`" class="event-link">
        {{ entry.eventName || 'Nežinoma veikla' }}
  </router-link>
  <span v-else>{{ entry.eventName || 'Nežinoma veikla' }}</span>
</td>
        <td>{{ formatDate(entry.eventDate) }}</td>
        <td>
  <v-chip
    :color="entry.isApproved === true ? 'green' : entry.isApproved === false ? 'red' : 'orange'"
    small
    dark
  >
    {{
      entry.isApproved === true
        ? 'Patvirtinta'
        : entry.isApproved === false
        ? 'Atmesta'
        : 'Laukiama atsakymo'
    }}
  </v-chip>
</td>
<td>{{ entry.isApproved === false ? '-' : (entry.finalFeedback?.trim() || '-') }}</td>
<td>{{ entry.isApproved === false ? '-' : `${entry.points ?? 0}/50` }}</td>
      </tr>
    </tbody>
  </v-table>
  <div v-else class="text-subtitle-1 text-center text-grey">Nėra savanorystės įrašų</div>
</v-card>
  </v-container>
</template>

<script>
import { useTheme } from 'vuetify';
import { computed } from 'vue';
export default {
    setup() {
    const theme = useTheme();
    const isDark = computed(() => theme.global.name.value === 'dark');

    return { isDark };
  },
  data() {
    return {
      eventHistory: [],
      points: 0,
      levelName: '',
      nextLevelPoints: 0,
      pointsToNextLevel: 0,
      progress: 0,
      thresholds: [],
      atMaxLevel: false,
    };
  },
  computed: {
  currentLevelIcon() {
    const current = this.thresholds.find(
      l => l.levelName === this.levelName
    );
    return current?.icon || 'mdi-crown';
  },
   sortedEventHistory() {
    return [...this.eventHistory].sort((a, b) => new Date(a.eventDate) - new Date(b.eventDate));
  }
},
  methods: {
  getLevelColor(levelName) {
    switch (levelName) {
      case 'Naujas Lapas': return 'green lighten-2';
      case 'Pagalbos Riteris': return 'blue lighten-2';
      case 'Pokyčių Kūrėjas': return 'orange lighten-1';
      case 'Savanorystės Herojus': return 'purple lighten-2';
      case 'Savanorystės Legenda': return 'deep-purple accent-3';
      default: return 'grey';
    }
  },
  formatDate(dateStr) {
    const d = new Date(dateStr);
    return d.toLocaleDateString("lt-LT");
  }
},
  async mounted() {
  const token = localStorage.getItem('accessToken') || sessionStorage.getItem('accessToken');
  if (!token) return;

  const payload = JSON.parse(atob(token.split('.')[1]));
  const userId = payload?.userId;

  const response = await fetch(`https://localhost:7177/api/users/${userId}`, {
    headers: { Authorization: `Bearer ${token}` }
  });
  const user = await response.json();
  this.points = user.points || 0;
  this.levelName = user.level || "Naujas Lapas";

  const historyRes = await fetch(`https://localhost:7177/api/events/user/${userId}`, {
  headers: { Authorization: `Bearer ${token}` }
  });
  this.eventHistory = await historyRes.json();

  const levelRes = await fetch("https://localhost:7177/api/levels");
  this.thresholds = await levelRes.json();

  const currentLevel = this.thresholds.find(
    l => this.points >= l.minPoints && (this.points <= l.maxPoints || l.maxPoints === null)
  );

  const min = currentLevel?.minPoints || 0;
  const max = currentLevel?.maxPoints || this.points;
  this.atMaxLevel = currentLevel?.maxPoints === null;
  this.nextLevelPoints = max;
  if (!this.atMaxLevel) {
  this.nextLevelPoints = max;
  this.pointsToNextLevel = max - this.points;
  this.progress = Math.round(((this.points - min) / (max - min)) * 100);
}
 }
}
</script>

<style scoped>
.profile{
   min-height: 80vh; 
}
.max-level-message {
  background-color: #e8f5e9;
  border-left: 5px solid #4caf50;
  padding: 16px;
  border-radius: 8px;
  font-size: 18px;
  color: #2e7d32;
  animation: popIn 0.8s ease;
  text-align: center;
}

.dark-max-level {
  background-color: #1e1e1e;
  color: #a5d6a7;
  border-left-color: #81c784;
  box-shadow: 0 0 12px rgba(129, 199, 132, 0.4);
}

@keyframes popIn {
  0% {
    transform: scale(0.8);
    opacity: 0;
  }
  100% {
    transform: scale(1);
    opacity: 1;
  }
}

.level-info {
  display: flex;
  flex-direction: column;
  justify-content: center;
  gap: 12px;
  padding-left: 12px;
}

.info-item {
  display: flex;
  align-items: center;
  font-size: 18px;
}

.label {
  font-weight: 600;
  margin-right: 6px;
}

.event-link {
  color: #1976d2;
  font-weight: 500;
  text-decoration: none;
  transition: color 0.3s;
}

.event-link:hover {
  color: #1565c0;
  text-decoration: underline;
}
</style>
