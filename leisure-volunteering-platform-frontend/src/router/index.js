import { createRouter, createWebHistory } from "vue-router";
import Home from "@/views/Home.vue";
import EventCreate from "@/views/EventCreate.vue";
import Events from "@/views/Events.vue";
import EventDetails from "@/views/EventDetails.vue";
import Auth from '@/views/Auth.vue';
import ResetPassword from "@/views/ResetPassword.vue";

const routes = [
  { path: "/", component: Home },
  { path: "/create-event", component: EventCreate },
  { path: "/events", component: Events },
  { path: "/event/:id", component: EventDetails, props: true },
  { path: '/auth', component: Auth },
  { path: "/reset-password", component: ResetPassword },
];

const router = createRouter({
  history: createWebHistory(),
  routes,
});

export default router;
