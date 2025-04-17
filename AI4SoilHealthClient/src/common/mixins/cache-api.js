// export const storeImageInCache = async function(blob, imageKey) {
//     if ('caches' in window) {
//       try {
//         // Otvaramo (ili kreiramo) cache pod nazivom 'my-image-cache'
//         const cache = await caches.open('ai4soilhealth-image-cache');
  
//         // Kreiramo URL koristeći Object URL API (služi samo kao ključ, ne mora biti stvarni URL)
//         const fakeUrl = new URL(imageKey, window.location.href);
  
//         // Pohranjujemo Blob kao Response objekt u cache
//         const response = new Response(blob, {
//           headers: { 'Content-Type': blob.type, 'Content-Length': blob.size }
//         });
  
//         await cache.put(fakeUrl, response);
//         console.log('Image stored in cache successfully!');
//       } catch (error) {
//         console.error('Error storing image in cache:', error);
//       }
//     }
//   }
  

// export const getImageFromCache = async function (imageKey) {
//     if ('caches' in window) {
//       try {
//         // Otvaramo cache
//         const cache = await caches.open('ai4soilhealth-image-cache');
  
//         // Kreiramo URL koristeći isti ključ kao kod pohrane
//         const fakeUrl = new URL(imageKey, window.location.href);
  
//         // Dohvaćamo response iz cache-a
//         const cachedResponse = await cache.match(fakeUrl);
  
//         if (cachedResponse) {
//           // Pretvaramo response natrag u Blob
//           const blob = await cachedResponse.blob();
  
//           // Vraćamo Blob ili ga koristimo za kreiranje Object URL-a za prikaz
//           return blob;
//         } else {
//           console.log('Image not found in cache.');
//         }
//       } catch (error) {
//         console.error('Error fetching image from cache:', error);
//       }
//     }
//   }

// services/storage/cache-api.js

/**
 * Klasa za rad s Cache API
 */
export class CacheApiService {
  /**
   * Inicijalizira servis s imenom cache-a
   * @param {string} cacheName - Ime cache-a koje će se koristiti
   */
  constructor(cacheName = 'default-image-cache') {
    this.cacheName = cacheName;
  }

  /**
   * Pomoćna funkcija za generiranje URL-a za cache
   * @param {string} key - Ključ za cache
   * @returns {URL} - URL objekt
   */
  createCacheUrl(key) {
    return new URL(key, window.location.href);
  }

  /**
   * Spremi podatak u cache
   * @param {Blob} blob - Blob podatak za spremiti
   * @param {string} key - Ključ za pohranu
   * @returns {Promise<void>}
   */
  async store(blob, key) {
    if (!('caches' in window)) {
      console.warn('Cache API is not supported in this browser');
      return;
    }
    
    try {
      // Otvori ili kreiraj cache
      const cache = await caches.open(this.cacheName);
      
      // Kreiraj URL za cache ključ
      const fakeUrl = this.createCacheUrl(key);
      
      // Kreiraj Response objekt iz blob-a
      const response = new Response(blob, {
        headers: { 
          'Content-Type': blob.type, 
          'Content-Length': blob.size 
        }
      });
      
      // Spremi u cache
      await cache.put(fakeUrl, response);
      console.log(`Data stored in cache [${this.cacheName}] with key: ${key}`);
    } catch (error) {
      console.error(`Error storing data in cache [${this.cacheName}]:`, error);
      throw error;
    }
  }

  /**
   * Dohvati podatak iz cache-a
   * @param {string} key - Ključ podatka
   * @returns {Promise<Blob|null>} - Blob podatak ili null
   */
  async get(key) {
    if (!('caches' in window)) {
      console.warn('Cache API is not supported in this browser');
      return null;
    }
    
    try {
      // Otvori cache
      const cache = await caches.open(this.cacheName);
      
      // Kreiraj URL za cache ključ
      const fakeUrl = this.createCacheUrl(key);
      
      // Dohvati response iz cache-a
      const cachedResponse = await cache.match(fakeUrl);
      
      if (cachedResponse) {
        // Pretvori response u Blob
        return await cachedResponse.blob();
      } 
      
      console.log(`Item not found in cache [${this.cacheName}]: ${key}`);
      return null;
    } catch (error) {
      console.error(`Error fetching from cache [${this.cacheName}]:`, error);
      return null;
    }
  }

  /**
   * Brisanje iz cache-a
   * @param {string} key - Ključ za brisanje
   * @returns {Promise<boolean>} - Rezultat brisanja
   */
  async delete(key) {
    if (!('caches' in window)) {
      return false;
    }
    
    try {
      const cache = await caches.open(this.cacheName);
      const fakeUrl = this.createCacheUrl(key);
      const result = await cache.delete(fakeUrl);
      
      if (result) {
        console.log(`Item deleted from cache [${this.cacheName}]: ${key}`);
      } else {
        console.log(`Item not found in cache [${this.cacheName}] for deletion: ${key}`);
      }
      
      return result;
    } catch (error) {
      console.error(`Error deleting from cache [${this.cacheName}]:`, error);
      return false;
    }
  }

  /**
   * Provjeri postoji li podatak u cache-u
   * @param {string} key - Ključ podatka
   * @returns {Promise<boolean>} - true ako postoji, false inače
   */
  async exists(key) {
    if (!('caches' in window)) {
      return false;
    }
    
    try {
      const cache = await caches.open(this.cacheName);
      const fakeUrl = this.createCacheUrl(key);
      const cachedResponse = await cache.match(fakeUrl);
      
      return cachedResponse !== undefined;
    } catch (error) {
      console.error(`Error checking item in cache [${this.cacheName}]:`, error);
      return false;
    }
  }

  /**
   * Obriši sve iz cache-a
   * @returns {Promise<boolean>} - Rezultat operacije
   */
  async clear() {
    if (!('caches' in window)) {
      return false;
    }
    
    try {
      const result = await caches.delete(this.cacheName);
      if (result) {
        console.log(`Cache [${this.cacheName}] cleared successfully`);
      }
      return result;
    } catch (error) {
      console.error(`Error clearing cache [${this.cacheName}]:`, error);
      return false;
    }
  }
}

// Stvaramo instancu s default cache imenom za lakši import
export const imageCache = new CacheApiService(import.meta.env.VITE_IMAGE_CACHE_NAME || 'default-image-cache');

// Wrappers za funkcije radi kompatibilnosti s postojećim kodom
export const storeImageInCache = (blob, key) => imageCache.store(blob, key);
export const getImageFromCache = (key) => imageCache.get(key);
export const deleteImageFromCache = (key) => imageCache.delete(key);
export const checkImageInCache = (key) => imageCache.exists(key);