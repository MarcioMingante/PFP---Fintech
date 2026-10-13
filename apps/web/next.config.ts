import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  output: "standalone",
  distDir: process.env.FINTRACK_E2E === "1" ? ".next-e2e" : ".next",
};

export default nextConfig;
