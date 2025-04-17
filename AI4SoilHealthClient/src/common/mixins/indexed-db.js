// export const initTable = async (databaseName, storeName, attributes) => {
//     try {
//         const db = await new Promise((resolve, reject) => {
//             const request = indexedDB.open(databaseName);

//             request.onsuccess = (event) => {
//                 const db = event.target.result;

//                 // Ako ne postoji tablica, trebamo je kreirati s većom verzijom baze
//                 if (!db.objectStoreNames.contains(storeName)) {
//                     db.close();

//                     // Otvorite bazu podataka s povećanom verzijom
//                     const upgradeRequest = indexedDB.open(databaseName, db.version + 1);

//                     upgradeRequest.onupgradeneeded = (event) => {
//                         const upgradedDb = event.target.result;


//                         // Kreiraj novi ObjectStore za novi StoreName
//                         const objectStore = upgradedDb.createObjectStore(storeName, {
//                             keyPath: 'id',
//                             autoIncrement: true
//                         });

//                         // Dodaj atribute kao indekse
//                         for (let attribute of attributes) {
//                             objectStore.createIndex(attribute.name, attribute.name, {
//                                 unique: attribute.unique || false
//                             });
//                         }

                     
//                     };

//                     upgradeRequest.onsuccess = (event) => {
//                         console.log(`Object store "${storeName}" created successfully in database "${databaseName}".`);
//                         resolve(event.target.result);
//                     };

//                     upgradeRequest.onerror = (event) => {
//                         console.error('Error upgrading database:', event.target.errorCode);
//                         reject(event.target.error);
//                     };
//                 } else {
//                     resolve(db);
//                 }
//             };

//             request.onerror = (event) => {
//                 console.error('Error opening database:', event.target.errorCode);
//                 reject(event.target.error);
//             };
//         });

//         db.close();
//     } catch (error) {
//         console.error('Error initializing table:', error);
//     }
// };


// export const storeData = async (dbName, table, data, attributes = null) => {
//     try {
//         // Inicijalizacija tablice 
//         if (data && data.length > 0) {
//             //preprocessing for non-table data
//             if(!attributes){
//                 // attributes = Object.keys(data[0]);
//                 attributes = Object.keys(data[0]).map(key => {
//                     return { name: key };
//                 });
//                 data = data.map(r => Object.values(r));
//             }

//             await initTable(dbName, table, attributes);
//             //init changesStore
//             await initTable(dbName, 'changesStore',  [{ name: 'id', keyPath: 'id', unique: true },
//                 { name: 'type', keyPath: 'type', unique: false },
//                 { name: 'data', keyPath: 'data', unique: false }]);
//         } else {
//             return; // Ako nema podataka, nema što za pohraniti
//         }

//         // Otvorite bazu podataka
//         const db = await new Promise((resolve, reject) => {
//             const dbRequest = indexedDB.open(dbName);
//             dbRequest.onsuccess = (event) => resolve(event.target.result);
//             dbRequest.onerror = (event) => reject(event.target.error);
//         });

//         // Kreirajte transakciju za dodavanje podataka u tablicu
//         const transaction = db.transaction([table], 'readwrite');
//         const store = transaction.objectStore(table);

//         // // Pohrana podataka
//         // for (const row of data) {
//         //     let record = {};
//         //     Object.keys(row).forEach((key) => {
//         //         record[key] = row[key];
//         //     });
//         //     store.put(record); // Dodavanje ili ažuriranje zapisa u objectStore
//         // }
//         //         console.log(data);
        
//         // Spremamo rezultate ID-ova u niz
//         let ids = [];

//          // Pohrana podataka i prikupljanje ID-ova
//         await Promise.all(data.map(row => {
//             return new Promise((resolve, reject) => {
//                 let record = {};
//                 row.forEach((value, index) => {
//                     record[attributes[index].name] = value; // Pretpostavlja da redoslijed u `data` odgovara redoslijedu u `attributes`
                    
//                 });

//                 // Dodavanje ili ažuriranje zapisa u objectStore
//                 const request = store.put(record);

//                 request.onsuccess = (event) => {
//                     ids.push(event.target.result); // Pohranjujemo ID novog zapisa
//                     resolve();
//                 };

//                 request.onerror = (event) => {
//                     console.error('Error adding record:', event.target.error);
//                     reject(event.target.error);
//                 };
//             });
//         }));

//         // Završi transakciju i uhvati greške ako postoje
//         await new Promise((resolve, reject) => {
//             transaction.oncomplete = () => {
//                 console.log('Data has been stored successfully.');
//                     resolve();
//             };
//             transaction.onerror = (event) => {
//                 console.error('Transaction error:', event.target.error);
//                 reject(event.target.error);
//             };
//         });

//         // Zatvorite bazu podataka
//         db.close();

//         // Vraćamo ID-ove svih dodanih zapisa
//         return ids;
//     } catch (error) {
//         console.error('Error storing data:', error);
//     }
// };


// /**
//  * Fetch data from the specified object store in IndexedDB using async/await.
//  * @param {string} dbName - The name of the database.
//  * @param {string} storeName - The name of the object store.
//  * @param {IDBValidKey | IDBKeyRange | undefined} key - Optional key to fetch a specific record.
//  * @returns {Promise<any[]>} - Implicitly returns a promise resolving to an array of records or a single record.
//  */
// export async function fetchData(dbName, storeName, tableFormat = true, key = undefined, fkey = undefined) {
//     const dbRequest = indexedDB.open(dbName);

//     try {
//         const dbOpenResult = await new Promise((resolve, reject) => {
//             dbRequest.onsuccess = () => resolve(dbRequest.result);
//             dbRequest.onerror = () => reject(dbRequest.error);
//         });

//         const transaction = dbOpenResult.transaction([storeName], "readonly");
//         const store = transaction.objectStore(storeName);
//         let dataRequest = null;
//         if(fkey){
//             const index = store.index(fkey.name);
//             dataRequest = index.getAll(fkey.value);
//         } else {
//             dataRequest = key !== undefined ? store.get(key) : store.getAll();
//         }

//         return await new Promise((resolve, reject) => {
//             dataRequest.onsuccess = //() => resolve(dataRequest.result);
//             function() {
//                 let records = dataRequest.result;
//                 if(key){
//                     records = [records];
//                 }

//                 if (records.length > 0) {
//                     // Pretpostavljamo da su svi zapisi istog formata
//                     const attributes = Object.keys(records[0]).map(key => ({
//                         name: key,
//                         type: typeof records[0][key] === 'boolean' ? 'boolean' :
//                               typeof records[0][key] === 'number' ? (Number.isInteger(records[0][key]) ? 'integer' : 'float') :
//                               (typeof records[0][key] === 'string' && !isNaN(Date.parse(records[0][key]))) ? 'timestamp with time zone' :
//                               'character varying'
//                     }));

//                     const data = records.map(record =>
//                         attributes.map(attr => record[attr.name])
//                     );

//                     if(tableFormat){
//                         resolve({ attributes, data });
//                     } else {
//                         resolve(records);
//                     }
//                 } else {
//                     if(tableFormat){
//                         resolve({ attributes: [], data: [] });
//                     } else {
//                         resolve(records);
//                     }
//                 }
//             };
//             dataRequest.onerror = () => reject(dataRequest.error);
//         });
//     } catch (error) {
//         console.error("Database error:", error);
//         throw error; // Rethrowing the error to handle it further up in the call stack
//     }
// }

// export const deleteData = (dbName, table,  key = undefined, fkey = undefined) => {
//     const dbRequest = indexedDB.open(dbName);

//     dbRequest.onsuccess = function (event) {
//         let db = event.target.result;

//         const transaction = db.transaction([table, 'changesStore'], 'readwrite');
//         const store = transaction.objectStore(table);
//         const changesStore = transaction.objectStore('changesStore');

//         // Brisanje zapisa iz glavne tablice
//         // store.delete(id);
//         if(fkey){
//            const index = store.index(fkey.name);
//            //TODO...
//            index.openCursor(fkey.value).onsuccess = function(event) {
//             const cursor = event.target.result;
          
//             if (cursor) {
//               cursor.delete(); // Brisanje trenutnog zapisa
//               console.log("Record deleted:", cursor.value);
//               cursor.continue(); // Nastavak iteracije kroz zapise sa istom vrednošću
//             } else {
//               console.log("No more matching records.");
//             }
//           };
//         } else {
//             store.delete(key);
//         }

//         // Kreirajte promjenu tipa `delete` za `changesStore`
//         const newChange = {
//             // id: id,
//             id: key,
//             type: 'delete',
//             table: table,
//             data: null,
//         };

//         changesStore.get(id).onsuccess = function (event) {
//             const existingChange = event.target.result;

//             if (existingChange) {
//                 if (existingChange.type === 'add') {
//                     // Ako je prethodno `add`, poništi obje promjene
//                     changesStore.delete(id);
//                 } else {
//                     // Inače, dodaj `delete`
//                     changesStore.put(newChange);
//                 }
//             } else {
//                 changesStore.put(newChange);
//             }
//         };

//         transaction.oncomplete = function () {
//             console.log("Delete operation and corresponding change recorded successfully.");
//         };

//         transaction.onerror = function (event) {
//             console.error("Transaction error:", event.target.error);
//         };
//     };

//     dbRequest.onerror = function (event) {
//         console.error("Database error: " + event.target.errorCode);
//     };
// };

// services/storage/indexed-db.js

/**
 * Klasa za rad s IndexedDB
 */
export class IndexedDbService {
    /**
     * Inicijalizira servis s imenom baze podataka
     * @param {string} dbName - Ime baze podataka
     */
    constructor(dbName = 'default-database') {
      this.dbName = dbName;
    }
  
    /**
     * Otvori bazu podataka
     * @returns {Promise<IDBDatabase>} - Promise koji vraća objekt baze
     */
    async openDatabase() {
      return new Promise((resolve, reject) => {
        const request = indexedDB.open(this.dbName);
        
        request.onsuccess = (event) => {
          resolve(event.target.result);
        };
        
        request.onerror = (event) => {
          reject(new Error(`Error opening database [${this.dbName}]: ${event.target.errorCode}`));
        };
      });
    }
  
    /**
     * Inicijalizira tablicu u bazi podataka
     * @param {string} storeName - Ime tablice
     * @param {Array} attributes - Atributi (stupci) tablice
     * @returns {Promise<void>}
     */
    async initTable(storeName, attributes) {
      if (!attributes || attributes.length === 0) {
        console.warn(`No attributes provided for table [${storeName}] initialization`);
        return;
      }
      
      try {
        // Otvori bazu da dobiješ trenutnu verziju
        const db = await this.openDatabase();
        
        // Provjeri postoji li tablica
        if (!db.objectStoreNames.contains(storeName)) {
          // Zatvori trenutnu vezu
          db.close();
          
          // Otvori s većom verzijom za kreiranje tablice
          await this.upgradeDatabase(db.version + 1, storeName, attributes);
          console.log(`Table [${storeName}] created in database [${this.dbName}]`);
        } else {
          db.close();
        }
      } catch (error) {
        console.error(`Error initializing table [${storeName}] in [${this.dbName}]:`, error);
        throw error;
      }
    }
  
    /**
     * Nadogradi bazu podataka
     * @param {number} version - Nova verzija baze
     * @param {string} storeName - Ime tablice za kreiranje
     * @param {Array} attributes - Atributi za tablicu
     * @returns {Promise<IDBDatabase>} - Promise koji vraća objekt baze
     */
    async upgradeDatabase(version, storeName, attributes) {
      return new Promise((resolve, reject) => {
        const request = indexedDB.open(this.dbName, version);
        
        request.onupgradeneeded = (event) => {
          const db = event.target.result;
          
          // Kreiraj novu tablicu ako ne postoji
          if (!db.objectStoreNames.contains(storeName)) {
            const objectStore = db.createObjectStore(storeName, {
              keyPath: 'id',
              autoIncrement: true
            });
            
            // Dodaj indekse
            for (const attribute of attributes) {
              if (attribute && attribute.name && attribute.name !== 'id') {
                objectStore.createIndex(attribute.name, attribute.name, {
                  unique: attribute.unique || false
                });
              }
            }
          }
        };
        
        request.onsuccess = (event) => {
          resolve(event.target.result);
        };
        
        request.onerror = (event) => {
          reject(new Error(`Error upgrading database [${this.dbName}]: ${event.target.errorCode}`));
        };
      });
    }
  
    /**
     * Pripremi podatke za spremanje
     * @param {Array} data - Podaci za spremiti
     * @param {Array} attributes - Atributi (stupci)
     * @returns {Object} - Pripremljeni podaci i atributi
     */
    prepareData(data, attributes) {
      // Za podatke koji nisu u tabličnom formatu
      if (!attributes) {
        return {
          attributes: Object.keys(data[0]).map(key => ({ name: key })),
          data: data
        };
      }
      
      // Podaci su u tabličnom formatu, potrebno preoblikovanje
      const preparedData = data.map(row => {
        const record = {};
        row.forEach((value, index) => {
          record[attributes[index].name] = value;
        });
        return record;
      });
      
      return { attributes, data: preparedData };
    }
  
    /**
     * Spremi podatke u tablicu
     * @param {string} tableName - Ime tablice
     * @param {Array} data - Podaci za spremiti
     * @param {Array} attributes - Atributi (stupci)
     * @returns {Promise<Array>} - ID-ovi spremljenih zapisa
     */
    async storeData(tableName, data, attributes = null) {
      if (!data || data.length === 0) {
        console.warn(`No data provided to store in [${tableName}]`);
        return [];
      }
      
      try {
        // Pripremi podatke
        const prepared = this.prepareData(data, attributes);
        
        // Inicijaliziraj tablice
        await this.initTable(tableName, prepared.attributes);
        await this.initTable('changesStore', [
          { name: 'id', unique: true },
          { name: 'type', unique: false },
          { name: 'data', unique: false },
          { name: 'table', unique: false }
        ]);
        
        // Otvori bazu i započni transakciju
        const db = await this.openDatabase();
        const transaction = db.transaction([tableName], 'readwrite');
        const store = transaction.objectStore(tableName);
        
        // Spremi podatke i prikupi ID-ove
        const ids = await Promise.all(
          prepared.data.map(record => 
            new Promise((resolve, reject) => {
              const request = store.put(record);
              request.onsuccess = () => resolve(request.result);
              request.onerror = () => reject(request.error);
            })
          )
        );
        
        // Čekaj završetak transakcije
        await new Promise((resolve, reject) => {
          transaction.oncomplete = resolve;
          transaction.onerror = () => reject(transaction.error);
        });
        
        // Zatvori bazu
        db.close();
        
        console.log(`${ids.length} records stored in [${tableName}]`);
        return ids;
      } catch (error) {
        console.error(`Error storing data in [${tableName}]:`, error);
        return [];
      }
    }
  
    /**
     * Dohvati podatke iz tablice
     * @param {string} tableName - Ime tablice
     * @param {boolean} tableFormat - Treba li vratiti u tabličnom formatu
     * @param {*} key - Ključ za dohvat (ID)
     * @param {Object} fkey - Foreign key objekt {name, value}
     * @returns {Promise<Array|Object>} - Vraća podatke
     */
    async fetchData(tableName, tableFormat = true, key = undefined, fkey = undefined) {
      try {
        // Otvori bazu
        const db = await this.openDatabase();
        
        // Započni transakciju
        const transaction = db.transaction([tableName], 'readonly');
        const store = transaction.objectStore(tableName);
        
        // Dohvati podatke ovisno o parametrima
        let dataRequest;
        
        if (fkey) {
          // Dohvat po foreign key-u
          const index = store.index(fkey.name);
          dataRequest = index.getAll(fkey.value);
        } else {
          // Dohvat po primary key-u ili svi zapisi
          dataRequest = key !== undefined ? store.get(key) : store.getAll();
        }
        
        // Čekaj rezultat zahtjeva
        const result = await new Promise((resolve, reject) => {
          dataRequest.onsuccess = () => resolve(dataRequest.result);
          dataRequest.onerror = () => reject(dataRequest.error);
        });
        
        // Zatvori bazu
        db.close();
        
        // Pripremi rezultat
        let records = Array.isArray(result) ? result : (result ? [result] : []);
        
        // Ako je rezultat prazan, vrati prazni objekt ili niz
        if (records.length === 0) {
          return tableFormat ? { attributes: [], data: [] } : [];
        }
        
        // Za tablični format, preoblikuj podatke
        if (tableFormat) {
          // Pretpostavljamo da su svi zapisi istog formata
          const attributes = Object.keys(records[0]).map(key => ({
            name: key,
            type: typeof records[0][key] === 'boolean' ? 'boolean' :
                  typeof records[0][key] === 'number' ? (Number.isInteger(records[0][key]) ? 'integer' : 'float') :
                  (typeof records[0][key] === 'string' && !isNaN(Date.parse(records[0][key]))) ? 'timestamp with time zone' :
                  'character varying'
          }));
          
          const data = records.map(record =>
            attributes.map(attr => record[attr.name])
          );
          
          return { attributes, data };
        }
        
        return records;
      } catch (error) {
        console.error(`Error fetching data from [${tableName}]:`, error);
        return tableFormat ? { attributes: [], data: [] } : [];
      }
    }
  
    /**
     * Brisanje podataka iz tablice
     * @param {string} tableName - Ime tablice
     * @param {*} key - Ključ (ID) za brisanje
     * @param {Object} fkey - Foreign key objekt {name, value}
     * @returns {Promise<boolean>} - Rezultat operacije
     */
    async deleteData(tableName, key = undefined, fkey = undefined) {
      try {
        const db = await this.openDatabase();
        
        // Kreiraj transakciju
        const transaction = db.transaction([tableName, 'changesStore'], 'readwrite');
        const store = transaction.objectStore(tableName);
        const changesStore = transaction.objectStore('changesStore');
        
        // Brisanje ovisno o parametrima
        if (fkey) {
          // Brisanje po foreign key-u
          const index = store.index(fkey.name);
          const cursorRequest = index.openCursor(fkey.value);
          
          await new Promise((resolve, reject) => {
            cursorRequest.onsuccess = (event) => {
              const cursor = event.target.result;
              if (cursor) {
                // Bilježi promjenu u changesStore
                const newChange = {
                  id: cursor.value.id,
                  type: 'delete',
                  table: tableName,
                  data: null
                };
                changesStore.put(newChange);
                
                // Obriši zapis
                cursor.delete();
                cursor.continue();
              } else {
                resolve();
              }
            };
            cursorRequest.onerror = () => reject(cursorRequest.error);
          });
        } else if (key !== undefined) {
          // Brisanje po primary key-u
          store.delete(key);
          
          // Bilježi promjenu u changesStore
          const newChange = {
            id: key,
            type: 'delete',
            table: tableName,
            data: null
          };
          changesStore.put(newChange);
        } else {
          // Obriši sve zapise (oprezno koristiti!)
          store.clear();
        }
        
        // Čekaj završetak transakcije
        await new Promise((resolve, reject) => {
          transaction.oncomplete = () => {
            console.log(`Successfully deleted data from [${tableName}]`);
            resolve(true);
          };
          transaction.onerror = () => reject(transaction.error);
        });
        
        // Zatvori bazu
        db.close();
        
        return true;
      } catch (error) {
        console.error(`Error deleting data from [${tableName}]:`, error);
        return false;
      }
    }
  }
  
  // Stvaramo instancu s default imenom baze za lakši import
  export const db = new IndexedDbService(import.meta.env.VITE_INDEXED_DB_NAME || 'default-database');
  
  // Wrappers za funkcije radi kompatibilnosti s postojećim kodom
  export const initTable = (tableName, attributes) => {
    return db.initTable(tableName, attributes);
  };
  
  export const storeData = (tableName, data, attributes = null) => {
    return db.storeData(tableName, data, attributes);
  };
  
  export const fetchData = (tableName, tableFormat = true, key = undefined, fkey = undefined) => {
    return db.fetchData(tableName, tableFormat, key, fkey);
  };
  
  export const deleteData = (tableName, key = undefined, fkey = undefined) => {
    return db.deleteData(tableName, key, fkey);
  };