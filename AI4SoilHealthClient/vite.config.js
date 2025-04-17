import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { quasar, transformAssetUrls } from '@quasar/vite-plugin'
import { VitePWA } from 'vite-plugin-pwa';
import mkcert from 'vite-plugin-mkcert';

export default defineConfig({
  optimizeDeps: {
    include: ['ol'],
  },
  resolve: {
    alias: {
      '@': '/src',
      'vue': 'vue/dist/vue.esm-bundler.js',
    },
  },
  plugins: [
    vue({ 
       template: { transformAssetUrls }
    }),
    quasar({
      sassVariables: '@/quasar-variables.sass'
    }),
    VitePWA({ 
      registerType: 'autoUpdate',
      // mode: 'development',
      devOptions: {
        // enabled: process.env.SW_DEV === 'true',
      /* when using generateSW the PWA plugin will switch to classic */
      // type: 'module',
      // navigateFallback: 'index.html',
      // suppressWarnings: true,
        // enabled: true // Omogućite PWA funkcionalnost u razvoju
        //  ,type: 'module' // Dodajte ovo ako koristite ES module
        // , navigateFallback: 'index.html'
      },
      workbox: {
        globPatterns: ['**/*.{js,css,html,ico,png,svg,otf,webmanifest,woff,woff2}'],
        runtimeCaching: [
          // {
          //   urlPattern: /\.(?:html|css|js|png|svg|ico|otf|woff|woff2|webmanifest)$/,
          //   handler: 'CacheFirst',
          //   options: {
          //     cacheName: 'static-resources',
          //     expiration: {
          //       maxEntries: 500,
          //       maxAgeSeconds: 30 * 24 * 60 * 60, // 30 days
          //     },
          //   },
          // },
          {
            urlPattern: /^https:\/\/tile\.openstreetmap\.org\/.*\.(png)$/,
            handler: 'NetworkFirst',
            options: {
              networkTimeoutSeconds: 1, 
              cacheName: 'openstreet-images',
              expiration: {
                maxEntries: 2500, 
                maxAgeSeconds: 30 * 24 * 60 * 60, // Cache for 30 days
              },
            },
          },
          {
            urlPattern: /^https:\/\/ecn\.t0\.tiles\.virtualearth\.net\/.*\.(jpeg)$/,
            handler: 'NetworkFirst',
            options: {
              networkTimeoutSeconds: 1, 
              cacheName: 'virtualearth-images',
              expiration: {
                maxEntries: 2500, 
                maxAgeSeconds: 30 * 24 * 60 * 60, // Cache for 30 days
              },
            },
          },
          // Add other runtime caching rules here if needed
        ],
      },
      manifest: {
        name: "AI4SoilHealth",
        short_name: "AI4SoilHealth",
        theme_color: "#ffffff",
        start_url: "/", 
        display: "standalone",
        background_color: "#ffffff",
        icons: [
          {
            src: "android-chrome-192x192.png",
            sizes: "192x192",
            type: "image/png",
          },
          {
            src: "android-chrome-512x512.png",
            sizes: "512x512",
            type: "image/png",
          },
        ],
      },
    }),
    //mkcert(),
  ],
  
  build: {
    //sourcemap: true,
    reportCompressedSize: true,
    target: 'esnext'
  },
  server: {
  }
})