/**
 * @namespace data

 * @description Specialized AI4SoilHealth data related objects
*/
/**
 * @namespace general

 * @description General objects, reusable outside the AI4SoilHealth context
*/
/**
 * @namespace zzglc

 * @description CRUD procedures
*/
/**
 * @namespace zzgll

 * @description Lookup procedures
*/
/**
 * @namespace auth

 * @description Authorization related objects
*/
/**
 * @namespace meta

 * @description Metadata related objects (application design)
*/
/**
 * @namespace log

 * @description Logging objects
*/
/**
 * @namespace zzhistory

 * @description History tables
*/
/**
 * @namespace osm

 * @description null
*/
/**
 * @namespace utils

 * @description General utilities

*/

/**
 * @class event
 * @memberof auth
 * @description Table of events
 * @property {boolean} is_active
 * @property {integer} tenant_id
 * @property {integer} event_type_id
 * @property {integer} app_id
 * @property {uuid} id primary key
 * @property {character_varying} name
 * @property {character_varying} description
 * @property {timestamp_without_time_zone} time_modified
*/
class event {}

/**
 * @class event_date
 * @memberof auth
 * @description Event date
 * @property {timestamp_without_time_zone} time_modified
 * @property {timestamp_without_time_zone} datetime
 * @property {uuid} event_id
 * @property {integer} period_id
 * @property {uuid} id
*/
class event_date {}

/**
 * @class log
 * @memberof auth
 * @description Log of user actions
 * @property {character_varying} ip
 * @property {timestamp_without_time_zone} time_modified
 * @property {integer} id
 * @property {integer} person_id foreign key to general.person.id
*/
class log {}

/**
 * @class person_role_public
 * @memberof auth
 * @description This table contains the roles that a person can have in the system.
 * @property {integer} person_id foreign key to general.person.id
 * @property {integer} id
 * @property {integer} role_public_id foreign key to auth.role_public.id
*/
class person_role_public {}

/**
 * @class role
 * @memberof auth
 * @description Role
 * @property {boolean} is_admin
 * @property {boolean} is_user
 * @property {timestamp_without_time_zone} time_modified
 * @property {integer} app_specific_type
 * @property {boolean} is_active
 * @property {boolean} sees_all
 * @property {character_varying} description
 * @property {integer} tenant_id
 * @property {uuid} id
 * @property {character_varying} name
*/
class role {}

/**
 * @class role_public
 * @memberof auth
 * @description Roles for public users.
 * @property {character_varying} name
 * @property {integer} id
*/
class role_public {}

/**
 * @class route_role_public
 * @memberof auth
 * @description Route role public
 * @property {integer} route_id foreign key to meta.route.id
 * @property {integer} id
 * @property {integer} role_public_id foreign key to auth.role_public.id
*/
class route_role_public {}

/**
 * @class user
 * @memberof auth
 * @description User
 * @property {character_varying} user_name
 * @property {uuid} id
 * @property {timestamp_without_time_zone} time_modified
 * @property {character} gender
 * @property {character_varying} personal_id
 * @property {character_varying} display_name
 * @property {character_varying} phone_number
 * @property {character_varying} email
 * @property {character_varying} last_name
 * @property {character_varying} first_name
*/
class user {}

/**
 * @class user_role
 * @memberof auth
 * @description User role
 * @property {uuid} role_id
 * @property {uuid} user_id
 * @property {uuid} id
 * @property {timestamp_without_time_zone} time_modified
*/
class user_role {}

/**
 * @class asset
 * @memberof data
 * @description Assets from Open Environmental Data Cube
 * @property {character_varying} scale_factor
 * @property {integer} unit_id foreign key to data.unit.id
 * @property {character_varying} platform
 * @property {character_varying} instruments_list
 * @property {character_varying} doi
 * @property {integer} asset_version_id foreign key to data.asset_version.id
 * @property {character_varying} keywords_list
 * @property {integer} asset_theme_id foreign key to data.asset_theme.id
 * @property {character_varying} description
 * @property {character_varying} title
 * @property {integer} asset_catalog_id foreign key to data.asset_catalog.id
 * @property {boolean} ignore
 * @property {character_varying} orig_id
 * @property {integer} id
 * @property {character_varying} gsd
 * @property {date} start_date
 * @property {date} end_date
 * @property {integer} date_step
 * @property {character_varying} date_offset
 * @property {integer} date_unit_id foreign key to data.date_unit.id
 * @property {character_varying} constellation_list
 * @property {character_varying} main_sld_url
 * @property {integer} license_id foreign key to data.license.id
 * @property {character_varying} providers_list
 * @property {integer} person_id foreign key to general.person.id
 * @property {character_varying} depth_list
 * @property {character_varying} sld_1_url
 * @property {integer} indicator_id foreign key to data.indicator.id
 * @property {integer} decimals
 * @property {integer} no_data
 * @property {character_varying} color_map
 * @property {character_varying} main_url
 * @property {character_varying} main_qml_url
 * @property {integer} date_style_id foreign key to data.date_style.id
 * @property {boolean} ignore_29feb
*/
class asset {}

/**
 * @class asset_catalog
 * @memberof data
 * @description Asset catalog
 * @property {character_varying} name
 * @property {integer} id
*/
class asset_catalog {}

/**
 * @class asset_theme
 * @memberof data
 * @description Asset themes
 * @property {character_varying} name
 * @property {integer} id
*/
class asset_theme {}

/**
 * @class asset_version
 * @memberof data
 * @description Asset versions
 * @property {character_varying} name
 * @property {integer} id
 * @property {character_varying} srid
*/
class asset_version {}

/**
 * @class attribute
 * @memberof data
 * @description Values for non numerical attributes of data sets.
 * @property {date} date
 * @property {integer} data_source_id foreign key to data.data_source.id
 * @property {integer} depth_id foreign key to data.depth.id
 * @property {integer} indicator_id foreign key to data.indicator.id
 * @property {integer} point_id foreign key to data.point.id
 * @property {integer} id
 * @property {integer} attribute_description_id foreign key to data.attribute_description.id
*/
class attribute {}

/**
 * @class attribute_description
 * @memberof data
 * @description Catalog of non numerical attributes of data sets.
 * @property {integer} indicator_id foreign key to data.indicator.id
 * @property {character_varying} code
 * @property {integer} color_id foreign key to general.color.id
 * @property {character_varying} name
 * @property {integer} id
*/
class attribute_description {}

/**
 * @class custom_geometry
 * @memberof data
 * @description User points or polygons for soil health data collection.
 * @property {timestamp_with_time_zone} time_modified
 * @property {timestamp_with_time_zone} time_created
 * @property {boolean} public
 * @property {character_varying} name
 * @property {geometry} geometry
 * @property {integer} person_id foreign key to general.person.id
 * @property {integer} geometry_type_id foreign key to data.geometry_type.id
 * @property {integer} id
*/
class custom_geometry {}

/**
 * @class custom_geometry_file
 * @memberof data
 * @description Files attached to user points or polygons.
 * @property {double_precision} lat
 * @property {double_precision} lon
 * @property {geometry} geometry
 * @property {integer} id
 * @property {character_varying} extension
 * @property {character_varying} mime_type
 * @property {time_without_time_zone} time_modified
 * @property {character_varying} name
 * @property {integer} custom_geometry_id foreign key to data.custom_geometry.id
 * @property {integer} compass
*/
class custom_geometry_file {}

/**
 * @class custom_geometry_property
 * @memberof data
 * @description Properties of user points or polygons.
 * @property {character_varying} value
 * @property {integer} id
 * @property {integer} custom_geometry_time_id foreign key to data.custom_geometry_time.id
 * @property {integer} property_id foreign key to data.property.id
*/
class custom_geometry_property {}

/**
 * @class custom_geometry_time
 * @memberof data
 * @description Points in time when properties of user points or polygons are collected.
 * @property {integer} id
 * @property {integer} custom_geometry_id foreign key to data.custom_geometry.id
 * @property {timestamp_with_time_zone} observation_time
*/
class custom_geometry_time {}

/**
 * @class data_source
 * @memberof data
 * @description Catalog of data sources of measurements or attributes.
 * @property {character_varying} name
 * @property {integer} id
 * @property {integer} srid
*/
class data_source {}

/**
 * @class date_style
 * @memberof data
 * @description Catalog of date styles for asset name generation.
 * @property {integer} id
 * @property {character_varying} name
*/
class date_style {}

/**
 * @class date_unit
 * @memberof data
 * @description Catalog of date units for asset name generation.
 * @property {integer} id
 * @property {character_varying} name
 * @property {character_varying} format
*/
class date_unit {}

/**
 * @class depth
 * @memberof data
 * @description Catalog of depths of measurements or attributes.
 * @property {integer} color_id foreign key to general.color.id
 * @property {integer} id
 * @property {character_varying} name
*/
class depth {}

/**
 * @class geometry_type
 * @memberof data
 * @description Catalog of types of custom geometries.
 * @property {character_varying} name
 * @property {integer} id
*/
class geometry_type {}

/**
 * @class geometry_type_property
 * @memberof data
 * @description Catalog of properties that are collected for custom geometries depending on type.
 * @property {integer} property_id foreign key to data.property.id
 * @property {integer} id
 * @property {integer} order_no
 * @property {boolean} required
 * @property {integer} geometry_type_id foreign key to data.geometry_type.id
*/
class geometry_type_property {}

/**
 * @class import_procedure
 * @memberof data
 * @description Utility table to enable different imports
 * @property {integer} data_source_id foreign key to data.data_source.id
 * @property {integer} id
 * @property {character_varying} proc_name
 * @property {character_varying} sheet
 * @property {character_varying} preprocess
 * @property {date} date
*/
class import_procedure {}

/**
 * @class indicator
 * @memberof data
 * @description Catalog of AI4SoilHealth data indicators.
 * @property {character_varying} lod
 * @property {character_varying} name_match
 * @property {integer} decimals_for_stats
 * @property {boolean} numerical
 * @property {real} value_from
 * @property {real} value_to
 * @property {integer} unit_id foreign key to data.unit.id
 * @property {character_varying} name
 * @property {integer} id
 * @property {integer} decimals_for_display
*/
class indicator {}

/**
 * @class indicator_color
 * @memberof data
 * @description Coloring scheme of AI4SoilHealth data indicators depending on value.
 * @property {real} value_to
 * @property {integer} id
 * @property {integer} indicator_id foreign key to data.indicator.id
 * @property {integer} color_id foreign key to general.color.id
*/
class indicator_color {}

/**
 * @class indicator_lang
 * @memberof data
 * @description Translations of AI4SoilHealth data indicators.
 * @property {character_varying} value
 * @property {integer} indicator_id foreign key to data.indicator.id
 * @property {integer} id
 * @property {integer} lang_id foreign key to meta.lang.id
*/
class indicator_lang {}

/**
 * @class license
 * @memberof data
 * @description Catalog of licenses that can be defined for assets.
 * @property {integer} id
 * @property {character_varying} name
*/
class license {}

/**
 * @class measurement
 * @memberof data
 * @description Values of numerical measurements.
 * @property {integer} depth_id foreign key to data.depth.id
 * @property {integer} id
 * @property {integer} indicator_id foreign key to data.indicator.id
 * @property {real} value
 * @property {integer} point_id foreign key to data.point.id
 * @property {date} date
 * @property {character} lod
 * @property {integer} data_source_id foreign key to data.data_source.id
*/
class measurement {}

/**
 * @class point
 * @memberof data
 * @description Points where measurements were taken orr attributes recorded.
 * @property {double_precision} y
 * @property {character_varying} name
 * @property {integer} elev
 * @property {integer} l3_id foreign key to general.nuts.id
 * @property {character_varying} notes
 * @property {integer} sample
 * @property {integer} data_source_id foreign key to data.data_source.id
 * @property {double_precision} lon
 * @property {character_varying} point_key
 * @property {integer} id
 * @property {double_precision} lat
 * @property {double_precision} x
*/
class point {}

/**
 * @class property
 * @memberof data
 * @description Catalog of properties that can be attached to a certain geometry type.
 * @property {character_varying} name
 * @property {character_varying} mask
 * @property {boolean} numerical
 * @property {integer} data_type_id
 * @property {integer} unit_id foreign key to data.unit.id
 * @property {integer} indicator_id foreign key to data.indicator.id
 * @property {integer} id
*/
class property {}

/**
 * @class shape
 * @memberof data
 * @description Shape file names.
 * @property {integer} id
 * @property {integer} data_source_id foreign key to data.data_source.id
 * @property {character_varying} name
*/
class shape {}

/**
 * @class shape_geometry
 * @memberof data
 * @description Geometries of shapes.
 * @property {integer} id
 * @property {json} properties
 * @property {integer} shape_id foreign key to data.shape.id
 * @property {geometry} geom
*/
class shape_geometry {}

/**
 * @class spectrum
 * @memberof data
 * @description Spectrum
 * @property {date} date
 * @property {integer} data_source_id foreign key to data.data_source.id
 * @property {integer} point_id foreign key to data.point.id
 * @property {integer} id
 * @property {real[]} values
 * @property {smallint[]} wavelengths
*/
class spectrum {}

/**
 * @class unit
 * @memberof data
 * @description Catalog of units of measurements.
 * @property {character_varying} name
 * @property {integer} id
*/
class unit {}

/**
 * @class vw_asset
 * @memberof data
 * @description Vw asset
 * @property {character_varying} title
 * @property {character_varying} orig_id
 * @property {integer} title_id
 * @property {text} time_span
 * @property {character_varying} main_sld_url
 * @property {text} url
 * @property {text} depth
*/
class vw_asset {}

/**
 * @class vw_avg_indicator_values_by_nuts
 * @memberof data
 * @description Average indicator values by nuts.
 * @property {character_varying} nuts_id
 * @property {real} value_max
 * @property {real} value_min
 * @property {double_precision} value_avg
 * @property {integer} indicator_id
 * @property {integer} levl_code
 * @property {integer} data_source_id
*/
class vw_avg_indicator_values_by_nuts {}

/**
 * @class vw_data_source_indicator
 * @memberof data
 * @description Indicators by data sources.
 * @property {integer} data_source_id
 * @property {integer} indicator_id
*/
class vw_data_source_indicator {}

/**
 * @class vw_indicator_color
 * @memberof data
 * @description Colors for indicators by value intervals.
 * @property {real} value_from
 * @property {character_varying} color_name
 * @property {character_varying} color_code
 * @property {real} value_to
 * @property {integer} indicator_id
*/
class vw_indicator_color {}

/**
 * @class ai_model
 * @memberof general
 * @description Catalog of AI models for chat.
 * @property {integer} id
 * @property {character_varying} name
*/
class ai_model {}

/**
 * @class boundary
 * @memberof general
 * @description OSM area boundaries.
 * @property {integer} admin_level
 * @property {character_varying} name
 * @property {character_varying} local_name
 * @property {integer} id
 * @property {integer} osm_id
 * @property {integer} level
 * @property {geometry} geom
*/
class boundary {}

/**
 * @class boundary_hierarchy
 * @memberof general
 * @description Hierarchy of OSM boundaries.
 * @property {integer} parent_distance
 * @property {integer} id
 * @property {integer} parent_id
 * @property {integer} child_id
*/
class boundary_hierarchy {}

/**
 * @class color
 * @memberof general
 * @description Catalog of colors.
 * @property {character_varying} code
 * @property {character_varying} name
 * @property {integer} id
 * @property {integer} order_no
*/
class color {}

/**
 * @class country
 * @memberof general
 * @description Catalog of countries.
 * @property {character} code
 * @property {integer} region_id foreign key to general.region.id
 * @property {integer} sub_region_id foreign key to general.sub_region.id
 * @property {integer} osm_id
 * @property {character} lucas_code
 * @property {character_varying} name
 * @property {smallint} id
*/
class country {}

/**
 * @class message
 * @memberof general
 * @description Chat messages.
 * @property {timestamp_without_time_zone} time_modified
 * @property {text} message
 * @property {integer} person_id foreign key to general.person.id
 * @property {integer} id
 * @property {integer} parent_id
*/
class message {}

/**
 * @class nuts
 * @memberof general
 * @description NUTS regions.
 * @property {character_varying} fid
 * @property {integer} id
 * @property {character_varying} nuts_id
 * @property {integer} levl_code
 * @property {integer} coast_type
 * @property {integer} urbn_type
 * @property {integer} mount_type
 * @property {character_varying} name_latn
 * @property {character_varying} nuts_name
 * @property {character} cntr_code
 * @property {geometry} geom
*/
class nuts {}

/**
 * @class partner
 * @memberof general
 * @description Catalog of project partners
 * @property {timestamp_without_time_zone} time_modified
 * @property {character_varying} name
 * @property {character_varying} short_name
 * @property {integer} id
 * @property {integer} user_modified foreign key to general.person.id
*/
class partner {}

/**
 * @class person
 * @memberof general
 * @description Catalog of persons related to the project (including users).
 * @property {character_varying} email
 * @property {timestamp_with_time_zone} agreed
 * @property {character_varying} user_email
 * @property {character_varying} name1
 * @property {character_varying} name
 * @property {integer} partner_id foreign key to general.partner.id
 * @property {character_varying} last_name
 * @property {character_varying} first_name
 * @property {integer} id
*/
class person {}

/**
 * @class region
 * @memberof general
 * @description Catalog of OSM regions.
 * @property {smallint} id
 * @property {character_varying} name
*/
class region {}

/**
 * @class sub_region
 * @memberof general
 * @description Catalog of OSM subregions.
 * @property {character_varying} name
 * @property {smallint} id
*/
class sub_region {}

/**
 * @class errors
 * @memberof log
 * @description Error log.
 * @property {integer} app_id
 * @property {timestamp_without_time_zone} time_modified
 * @property {integer} id
 * @property {character_varying} message
 * @property {character_varying} level
 * @property {timestamp_without_time_zone} time_stamp
 * @property {character_varying} exception
 * @property {character_varying} log_event
 * @property {integer} tenant_id
 * @property {uuid} user_id
*/
class errors {}

/**
 * @class app
 * @memberof meta
 * @description App
 * @property {text} agreement
 * @property {text} terms_of_service
 * @property {text} privacy_policy
 * @property {integer} id
*/
class app {}

/**
 * @class data_type
 * @memberof meta
 * @description Catalog of data types.
 * @property {character_varying} name
 * @property {integer} id
 * @property {boolean} default_type
 * @property {integer} default_format_id
 * @property {timestamp_without_time_zone} time_modified
 * @property {character_varying} js_data_type
 * @property {character_varying} server_data_type
 * @property {integer} default_col_editor_id
 * @property {integer} default_width
*/
class data_type {}

/**
 * @class error
 * @memberof meta
 * @description Errors of auxiliary admin app.
 * @property {integer} lang_id
 * @property {integer} code
 * @property {uuid} id
 * @property {timestamp_without_time_zone} time_modified
 * @property {character_varying} message
*/
class error {}

/**
 * @class help
 * @memberof meta
 * @description Catalog of help items.
 * @property {character_varying} name
 * @property {integer} id
*/
class help {}

/**
 * @class help_lang
 * @memberof meta
 * @description Translations of help texts.
 * @property {integer} id
 * @property {integer} lang_id foreign key to meta.lang.id
 * @property {integer} help_id foreign key to meta.help.id
 * @property {text} value
*/
class help_lang {}

/**
 * @class i18n
 * @memberof meta
 * @description Catalog of i18n text items in app.
 * @property {character_varying} key
 * @property {integer} id
*/
class i18n {}

/**
 * @class i18n_lang
 * @memberof meta
 * @description Translations of i18n text items in app.
 * @property {integer} i18n_id foreign key to meta.i18n.id
 * @property {integer} id
 * @property {integer} lang_id foreign key to meta.lang.id
 * @property {character_varying} value
*/
class i18n_lang {}

/**
 * @class import_detail
 * @memberof meta
 * @description Column descriptions for import from csv/excel.
 * @property {character_varying} joins
 * @property {boolean} split
 * @property {boolean} ignore
 * @property {character_varying} name_match
 * @property {integer} order_no
 * @property {boolean} ignore_in_master
 * @property {character_varying} constant_value
 * @property {integer} id
 * @property {integer} import_master_id foreign key to meta.import_master.id
 * @property {character_varying} col_name
 * @property {character_varying} target_schema
 * @property {character_varying} target_table
 * @property {character_varying} target_column
 * @property {character_varying} col_type
 * @property {boolean} is_key
 * @property {boolean} is_fk
 * @property {character_varying} name
 * @property {boolean} as_row
 * @property {character_varying} target_column_list
 * @property {character_varying} source_column_list
*/
class import_detail {}

/**
 * @class import_master
 * @memberof meta
 * @description Items for import from csv/excel.
 * @property {character_varying} data_date
 * @property {character_varying} schema_name
 * @property {character_varying} table_name
 * @property {character_varying} file_name
 * @property {integer} id
 * @property {character_varying} sheet
 * @property {character_varying} name
 * @property {character_varying} import_name
 * @property {character_varying} constant_value
*/
class import_master {}

/**
 * @class lang
 * @memberof meta
 * @description Catalog of app languages.
 * @property {integer} id
 * @property {character_varying} name
 * @property {character_varying} tag
 * @property {character} short_tag
 * @property {timestamp_without_time_zone} time_modified
*/
class lang {}

/**
 * @class news
 * @memberof meta
 * @description News items for display in app home page.
 * @property {character_varying} name
 * @property {timestamp_with_time_zone} time_created
 * @property {integer} user_modified foreign key to general.person.id
 * @property {timestamp_with_time_zone} time_modified
 * @property {boolean} active
 * @property {integer} id
*/
class news {}

/**
 * @class news_lang
 * @memberof meta
 * @description Translations of news items for display in app home page.
 * @property {timestamp_with_time_zone} time_created
 * @property {integer} news_id foreign key to meta.news.id
 * @property {character_varying} title
 * @property {boolean} active
 * @property {integer} user_modified foreign key to general.person.id
 * @property {timestamp_with_time_zone} time_modified
 * @property {character_varying} extended_text
 * @property {character_varying} text
 * @property {integer} lang_id foreign key to meta.lang.id
 * @property {integer} id
*/
class news_lang {}

/**
 * @class route
 * @memberof meta
 * @description Routes (menu items) with attributes for app.
 * @property {boolean} offline
 * @property {boolean} public
 * @property {json} props
 * @property {character_varying} parent
 * @property {character_varying} icon
 * @property {character_varying} component_name
 * @property {character_varying} path
 * @property {character_varying} name
 * @property {integer} id
 * @property {boolean} active
 * @property {integer} order_no
*/
class route {}

/**
 * @class safe_function
 * @memberof meta
 * @description List of database functions that can be safely executed.
 * @property {character_varying} name
 * @property {integer} id
*/
class safe_function {}

/**
 * @class table_ref_col
 * @memberof meta
 * @description Column to be shown as label in dropdown (in not second column of a table).
 * @property {character_varying} schema_name
 * @property {character_varying} table_name
 * @property {character_varying} ref_col
 * @property {integer} id
*/
class table_ref_col {}

/**
 * @class trace
 * @memberof meta
 * @description Table to store manual debugging data.
 * @property {character_varying} text
 * @property {integer} id
 * @property {timestamp_without_time_zone} time_modified
*/
class trace {}

/**
 * @class vw_ai4soilhealth_hierarchy
 * @memberof meta
 * @description Menu item hierarchy of auxiliary admin app.
 * @property {character_varying} child
 * @property {character_varying} proc
 * @property {integer} order_no
 * @property {text} parent
*/
class vw_ai4soilhealth_hierarchy {}

/**
 * @class auth_person_role_public
 * @memberof zzhistory
 * @description Auth person role public
 * @property {integer} role_public_id
 * @property {uuid} modifieduserid
 * @property {integer} modifiedpersonid
 * @property {character_varying} modifiedlogin
 * @property {timestamp_without_time_zone} modifiedtime
 * @property {character} modifiedaction
 * @property {integer} id
 * @property {integer} person_id
*/
class auth_person_role_public {}

/**
 * @class auth_role_public
 * @memberof zzhistory
 * @description Auth role public
 * @property {integer} id
 * @property {character} modifiedaction
 * @property {character_varying} modifiedlogin
 * @property {timestamp_without_time_zone} modifiedtime
 * @property {integer} modifiedpersonid
 * @property {uuid} modifieduserid
 * @property {character_varying} name
*/
class auth_role_public {}

/**
 * @class auth_route_role_public
 * @memberof zzhistory
 * @description Auth route role public
 * @property {integer} role_public_id
 * @property {integer} route_id
 * @property {integer} id
 * @property {character} modifiedaction
 * @property {character_varying} modifiedlogin
 * @property {timestamp_without_time_zone} modifiedtime
 * @property {integer} modifiedpersonid
 * @property {uuid} modifieduserid
*/
class auth_route_role_public {}
