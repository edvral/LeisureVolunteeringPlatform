import { createRouter, createWebHistory } from "vue-router";
import Home from "@/views/Home.vue";
import EventCreate from "@/views/EventCreate.vue";
import EventEdit from "@/views/EventEdit.vue";
import Events from "@/views/Events.vue";
import EventDetails from "@/views/EventDetails.vue";
import Auth from '@/views/Auth.vue';
import ResetPassword from "@/views/ResetPassword.vue";
import VolunteerProfile from "@/views/VolunteerProfile.vue"

const routes = [
  { path: "/", component: Home },
  { path: "/create-event", component: EventCreate },
  { path: "/events", component: Events },
  { path: "/event/:id", component: EventDetails, props: true },
  { path: '/auth', component: Auth },
  { path: "/reset-password", component: ResetPassword },
  { path: "/edit-event/:id", component: EventEdit, props : true },
  { path: '/profile', component: VolunteerProfile}
];

const router = createRouter({
  history: createWebHistory(),
  routes,
});

export default router;
