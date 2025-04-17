import { initTable, storeData, fetchData, deleteData } from '@/common/mixins/indexed-db';
import { storeImageInCache, getImageFromCache } from '@/common/mixins/cache-api';

export const OfflineApiMixin = {
    methods: {
    // Helper functions
        async parseTableName(path, options) {
            if (path[1] === 'GetTable') {
                const tableName = options.params.dbFunction.replace(/\./g, '_');
                return tableName === 'data_get_locations' ? 'custom_geometry' : tableName;
            }
            return path[1];
        },

        async handleOfflineRequest(path, options) {
            if (path[0] === 'Table' && path[1] !== 'GetLookup') {
            const table = await this.parseTableName(path, options);
            return await fetchData(table);
            } 
            
            if (path[0] === 'User' && path[1] !== 'GetCustomGeometry') {
            return await this.handleOfflineUserRequests(path, options);
            }
            
            // Default: try localStorage
            return this.$q.localStorage.getItem(url);
        },

        async handleOfflineUserRequests(path, options) {
            const action = path[1];
            const id = parseInt(path[2]);
            
            switch (action) {
            case 'DeleteSimpleObservation':
                await deleteData('single_observation', id);
                await deleteData('image', null, {name: 'simple_observation_id', value: id});
                return 'success';
                
            case 'DeleteImage':
                await deleteData('image', id);
                //delete also image bytes from cache storage?
                return 'success';
                
            case 'GetImage':
                const imageRecords = await fetchData('image', false, id);
                if (imageRecords?.length > 0) {
                const img = imageRecords[0];
                return await getImageFromCache(img.fileName + '_' + img.simple_observation_id);
                }
                return null;
                
            case 'UploadImage':
                return await this.handleOfflineImageUpload(path, options);
                
            case 'DeleteCustomGeometry':
                await deleteData('custom_geometry', id);
                //trebalo bi obrisati i single_observationse!
                return 'success'; //test?
                
            case 'SetSingleGeometry':
                return await this.handleOfflineSetSingleGeometry(options);
                
            case 'GetSingleGeometry':
                return await this.handleOfflineGetSingleGeometry(options);
                
            default:
                return null;
            }
        },

        async handleOfflineImageUpload(path, options) {
            let file, attributes, fileName;
            const observationId = parseInt(path[2]);
            
            // Extract file and attributes from form data
            for (let [key, value] of options.entries()) {
            if (key === 'file') {
                file = options.get(key);
            } else {
                attributes = JSON.parse(options.get(key));
                fileName = key;
            }
            }
            
            // Create and store record to indexedDB
            const record = { 
            ...attributes, 
            name: fileName, 
            simple_observation_id: observationId 
            };
            
            const ids = await storeData('image', [record]);
            const id = ids?.length > 0 ? ids[0] : null;
            
            // Cache the image
            await storeImageInCache(file, fileName + '_' + record.simple_observation_id);
            
            const extension = fileName.substring(fileName.lastIndexOf('.'));
            
            return [{
            ...attributes,
            id,
            simple_observation_id: observationId,
            name: fileName,
            extension,
            mime_type: file.type
            }];
        },

        async handleOfflineSetSingleGeometry(options) {
            let { custom_geometry, simple_observation } = options;
            simple_observation = JSON.parse(JSON.stringify(simple_observation));
            //custom geometry - not necessary to store
            // await storeData(dbName, 'custom_geometry', [custom_geometry]);
            
            // Prepare simple observation
            simple_observation.custom_geometry_id = custom_geometry.id;
            delete simple_observation.id;
            delete simple_observation.simple_observation_type_id;
            delete simple_observation.images;
            
            const ids = await storeData('single_observation', [simple_observation]);
            
            if (ids?.length > 0) {
            return { 
                key: custom_geometry.id, 
                simple_observation_id: ids[0] 
            };
            }
            
            return null;
        },
        
        async handleOfflineGetSingleGeometry(options) {
            const id = parseInt(options.params.id);
            
            // Get custom geometry
            const cg = await fetchData('custom_geometry', false, id);
            const custom_geometry = cg?.length > 0 ? cg[0] : null;
            
            // Get simple observations
            const simple_observations = await fetchData(
            'single_observation', 
            false, 
            null, 
            {name: 'custom_geometry_id', value: id}
            );
            
            // Get images for each observation
            for (let so of simple_observations) {
            const images = await fetchData(
                'image', 
                false, 
                null, 
                {name: 'simple_observation_id', value: so.id}
            );
            so.images = images || [];
            }
            
            return {
            ...custom_geometry,
            simple_observations
            };
        },
        
        async cacheOnlineResponse(url, path, options, data, axiosMethod) {
            if (path[0] === 'Table' && path[1] !== 'GetLookup') {
            const table = await this.parseTableName(path, options);
            
            if (axiosMethod === this.axios.API.get) {
                await storeData(table, data.data, data.attributes);
            }
            } 
            else if (path[0] === 'User' && path[1] !== 'GetCustomGeometry') {
            if (path[1] === 'GetImage') {
                await this.cacheImageResponse(path, data);
            } 
            else if (path[1] === 'GetSingleGeometry') {
                await this.cacheSingleGeometryResponse(data);
            }
            } 
            else {
            this.$q.localStorage.set(url, data);
            }
        },
        async cacheImageResponse(path, data) {
            const id = parseInt(path[2]);
            const records = await fetchData('image', false, id);
            
            if (records?.length > 0) {
            const img = records[0];
            await storeImageInCache(data, img.name + '_' + img.simple_observation_id);
            }
        },
        
        async cacheSingleGeometryResponse(data) {
            // Extract and clone data
            const { simple_observations, ...custom_geometry } = data;
            const cloned_observations = JSON.parse(JSON.stringify(simple_observations));
            
            // Store custom geometry
            await storeData('custom_geometry', [custom_geometry]);
            
            // Extract images from observations
            const arrImages = [];
            if (cloned_observations?.length > 0) {
            for (const observation of cloned_observations) {
                const images = observation.images;
                delete observation.images;
                if (images?.length > 0) {
                arrImages.push(...images);
                }
            }
            }
            
            // Store images and observations
            await storeData('image', arrImages);
            await storeData('single_observation', cloned_observations);
        }
    }
};