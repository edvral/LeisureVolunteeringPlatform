import { createApp } from 'vue';
import App from './App.vue';
import router from './router';
import { createVuetify } from 'vuetify';
import 'vuetify/styles';
import '@mdi/font/css/materialdesignicons.css';
import { VTimePicker } from 'vuetify/labs/VTimePicker'
import Toast from "vue-toastification";
import "vue-toastification/dist/index.css";
import { lt } from 'vuetify/locale';

import * as components from 'vuetify/components';
import * as directives from 'vuetify/directives';

const vuetify = createVuetify({
  locale: {
    locale: 'lt', 
    messages: { lt }, 
  },
  components: {
    ...components, 
    VTimePicker,    
  },
  directives,
});

const toastOptions = {
  position: "top-center",
  timeout: 2500,         
  closeOnClick: true,
  pauseOnFocusLoss: true,
  pauseOnHover: true,
  draggable: true,
  draggablePercent: 0.6,
  showCloseButtonOnHover: false,
  hideProgressBar: false,
  icon: true,
  rtl: false,
};

createApp(App).use(router).use(vuetify).use(Toast, toastOptions).mount('#app');
