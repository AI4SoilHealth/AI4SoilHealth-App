/**
 * finds all the $t('key') in the project and sends them to the API 
 */
import fs from 'fs';
import path from 'path';
import fetch from 'node-fetch';

const projectDirectory = '.'; path
const apiUrl = 'http://localhost:5012/api/Dev/AddI18NKeys'; 
let allTranslations = [];

/**
 * Recursively processes files in a directory, skipping 'node_modules' and processing JavaScript files.
 * @param {string} dir - The directory path to process.
 */
function processFiles(dir) {
  const files = fs.readdirSync(dir);
  for (const file of files) {
    const filePath = path.join(dir, file);
    const stats = fs.statSync(filePath);
    // skip node_modules
    if (filePath.includes('node_modules')) {
      continue;
    }
    if (stats.isDirectory()) {
      processFiles(filePath); // Recursive call for subdirectories
    } else if (stats.isFile() && (file.endsWith('.js') || file.endsWith('.vue'))) {
      processFile(filePath); // Process JavaScript files
    }
  }
}

/**
 * Processes a single file and extracts all the translation keys.
 * @param {string} filePath - The file path to process.
 */
function processFile(filePath) {
  const fileContents = fs.readFileSync(filePath, 'utf-8');
  const regex = /\$t\(['"]([^'"]+)['"]\)/g;
  //const regex = /\$t\((.*?)\)/g;
  const matches = [...fileContents.matchAll(regex)];
  const translations = matches.map(match => match[1]);
  allTranslations = allTranslations.concat(translations);
}

// Start processing the project directory
processFiles(projectDirectory);

if (allTranslations.length > 0) {
  // Send translations to the REST API
  let object = { keys: JSON.stringify(allTranslations) };

  fetch(apiUrl, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(object)
  })
  .then(response => {
    if (response.ok) {
      console.log(`Translations sent to the API successfully.`);
    } else {
      console.error(`Failed to send translations to the API: ${response.status} ${response.statusText}`);
    }
  })
  .catch(error => {
    console.error(`An error occurred: `, error);
  });
}
