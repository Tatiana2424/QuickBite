import axios from "axios";
import { apiBaseUrl } from "../config/env";

export const apiClient = axios.create({
  baseURL: apiBaseUrl,
  timeout: 10000
});
