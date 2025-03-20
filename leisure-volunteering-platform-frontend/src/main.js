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

const savedTheme = localStorage.getItem('theme') || 'light';

const vuetify = createVuetify({
  locale: {
    locale: 'lt', 
    messages: { lt }, 
  },
  theme: {
    defaultTheme: savedTheme, 
    themes: {
      light: {
        dark: false, 
        colors: {
          primary: '#1976D2',
          secondary: '#424242',
          accent: '#82B1FF',
          background: '#FFFFFF',
          surface: '#ffffff',
          text: '#000000',
        },
      },
      dark: {
        dark: true,
        colors: {
          primary: '#000000',
          secondary: '#121212',
          accent: '#BB86FC',
          background: '#1E1E1E',
          surface: '#242424',
          text: '#ffffff',
        },
      },
    },
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
