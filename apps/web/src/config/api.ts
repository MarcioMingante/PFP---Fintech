export function getApiUrl() {
  const url = new URL(process.env.API_URL ?? "http://localhost:5168");
  if (!["http:", "https:"].includes(url.protocol)) {
    throw new Error("API_URL deve usar HTTP ou HTTPS.");
  }
  return url;
}
