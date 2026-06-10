import axios from "axios";

const BASE_URL = "http://localhost:8080";

export const endpoints = {
  "certificate-info": "/certificate/info",
};

export const authApi = () => {
  return axios.create({
    baseURL: BASE_URL,
    headers: {
      Authorization: `Bearer local-token`,
    },
  });
};

export default axios.create({
  baseURL: BASE_URL,
});