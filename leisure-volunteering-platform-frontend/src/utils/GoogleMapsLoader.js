import { Loader } from "@googlemaps/js-api-loader";

const loader = new Loader({
  apiKey: "AIzaSyBMvaTEdXX55yRDHAOieTdyl2hc1cmnuVY", 
  version: "weekly",
  libraries: ["places"],
  language: "lt",
});

export default loader;
