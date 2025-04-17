# AI4SoilHealth Database

AI4SoilHealth database is a PostgreSQL database with PostGIS extension storing all relevant project data.<br/>
Client app [AI4SoilHealth app](https://maps.ai4soilhealth.eu/docs-app) communicates with the database usin Rest API calls in [AI4SoilHealth backend](https://maps.ai4soilhealth.eu/docs-backend).<br/>
<br/>
It is divided into following schemas:<br/>
<br/>
auth - Authorization related objects<br/>
data - Specialized AI4SoilHealth data related objects<br/>
general - General objects, reusable outside the AI4SoilHealth context<br/>
log - Logging objects<br/>
meta - Metadata related objects (application design)<br/>
utils - Utilities and auxiliary routines<br/>
zzglc - CRUF backoffice routines<br/>
zzgll - Lookup routines<br/>
zzhistory - History tables (recorded changes)
<br/>

## Related docs

<br/>

[AI4SoilHealth app](https://maps.ai4soilhealth.eu/docs-app)
<br/>

[AI4SoilHealth backend](https://maps.ai4soilhealth.eu/docs-backend)
<br/>
