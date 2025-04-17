/**
 * @desc A mixin object containing common map methods
 * @module MapMixin
 */

import { View } from "ol";
import { transform } from "ol/proj";
import { get as getProjection } from 'ol/proj';
//import * as Extent from 'ol/extent';
import { getCenter } from "ol/extent";
import { StatisticsMixin } from "@/common/mixins/statistics"; 
import { date } from "quasar";

export const MapMixin = {
    mixins: [StatisticsMixin],
    data() { 
        return {
            styleCache: {},
        }
    },
    methods: {

        /**
         * Returns the coordinates of the map based on the client coordinates.
         * @returns {Array<number>} The coordinates of the map.
         */
        coordinates() {
            return this.map.getCoordinateFromPixel([this.$store.clientCoordinates[0]/this.pixelRatio, this.$store.clientCoordinates[1]/this.pixelRatio]);
        },
            
        /**
         * Reprojects the given features from the old projection to the new projection.
         * @param {ol.Feature[]} features - The features to be reprojected.
         * @param {ol.proj.ProjectionLike} oldProjection - The old projection of the features.
         * @param {ol.proj.ProjectionLike} newProjection - The new projection to reproject the features to.
         */
        reprojectFeatures(features, oldProjection, newProjection) {
            this.$store.working = true;
            for (let feature of features) {
                feature.getGeometry().transform(oldProjection, newProjection);
            }
            this.$store.working = false;
        },

        /**
         * Sets the SRID (Spatial Reference System Identifier) for the map.
         * @param {number} val - The SRID value to set.
         */
        setSrid(val) {
            const currentView = this.map.getView();
            const currentCenter = currentView.getCenter();
            const newProjection = getProjection("EPSG:" + val);
            const newCenter = transform(currentCenter, this.projection, newProjection);
            const newView = new View({
                projection: newProjection,
                center: newCenter,
                zoom: this.zoom,
            });
            this.map.setView(newView);
            this.geolocation.setProjection(newProjection);
            this.reprojectFeatures(this.boundariesSource.getFeatures(), this.projection, newProjection);
    
            this.reprojectFeatures(this.pointSource.getFeatures(), this.projection, newProjection);           
            this.reprojectFeatures(this.editableSource.getFeatures(), this.projection, newProjection);
           //this.positionFeature.getGeometry().setCoordinates(this.geolocation.getPosition());
            this.projection = newProjection;
        },

        /**
         * Show history of selected feature
         * @param {*} options 
         */
        async showHistory() {
            let f = this.selectedFeature;
            if (!f.get("point_id")) {   // show history for selected point on tiff layer
                let coords = this.selectedFeature.getGeometry().getCoordinates();
                let pixel = this.map.getPixelFromCoordinate(coords);
                let r = this.getActiveTiffLayer(pixel);
                let o = {
                    x: coords[0],
                    y: coords[1],
                    srid: this.srid,
                    scaleFactor: r.selectedTitle.scale_factor,
                    decimalsForStats: r.selectedTitle.decimals,
                    sources: r.selectedTitle.sources.map(x => ({ file: x.source, label: x.label, value: null })).sort((a, b) => a.label.localeCompare(b.label))                    
                };
                let res = await this.post("Gdal/GetHistory", o);
                if (res) {
                    console.log(res);
                    this.initPopup({component : "chart-popup", maximized: true,
                        chartType: "line",
                        data: res,
                        labelField: "label",
                        valueField: "value",
                        title: r.selectedTitle.label,
                        unit: r.selectedTitle.unit,
                        xScale: 'category',
                        trendLine: true,
                        pointRadius: 3
                    });
                }
            } else {  // show history for selected point
                if (f.getGeometry().getType() == "Point") {
                    let res = await this.get("User/GetPointHistory", { pointId: f.get("point_id"), indicatorId : this.selectedIndicator.value });
                    if (res) {
                         this.initPopup({component : "chart-popup", maximized: true,
                            chartType: "line",
                            data: res,
                            labelField: "date",
                            valueField: "value",
                            seriesField: "depth",
                            title: this.selectedIndicator.label + " - " + f.get("name"),
                            unit: this.selectedIndicator.unit,
                            xScale: 'time',
                            trendLine: true,
                            pointRadius: 3
                        });
                    }
                }
            }
        },

        /**
         * Show properties of selected feature in a popup
         * @param {*} options 
         */
        async showProps(options) {
            this.initPopup({component : "ol-map-popup", title : this.selectedFeature.get("name") ?? this.selectedFeature.get("Name") ?? "Feature"});   
        },

        /**
         * Prepares a frequency chart based on the given points.
         *
         * @param {Array} points - The array of points.
         * @param {boolean} numerical - Indicates whether the points are numerical or not.
         * @param {number} decimalsForStats - The number of decimals to round the statistics to.
         * @returns {Object} - The prepared frequency chart object.
         */
        prepareFrequencyChart(points, numerical, decimalsForStats) {
            let v;
            if (numerical) {
                v = points.map(p => this.roundTo(p.get("value"), decimalsForStats)); //.toFixed(1));
            } else {
                v = points.map(p => p.get("description"));
            }
            let f = {};
            let stat = {};
            v.forEach(v => {
                f[v] = (f[v] || 0) + 1;
            });
            if (numerical) {
                stat = this.statistics(v);
            }
            let fs = Object.keys(f).map(k => ({ label: k, value: f[k] }));
            fs.sort((a, b) => a.label - b.label);
            return {
                stat: stat,
                x: fs.map(f => f.label),
                y: fs.map(f => f.value)
            };
        },

        /**
         * Displays the frequencies of the selected indicator.
         *
         * @param {Object} options - The options for displaying the frequencies.
         * @returns {Promise<void>} - A promise that resolves when the frequencies are displayed.
         */
        async showFrequencies(options) {
            if (this.selectedIndicator) {
                let data = this.prepareFrequencyChart(this.pointFeatures, this.selectedIndicator.numerical, this.selectedIndicator.decimals_for_stats);
                this.initPopup({component : "chart-popup", maximized: true,
                    chartType: "bar",
                    data: data,
                    stat: data.stat,
                    title: this.$t("Frequency") + " - " + this.selectedIndicator.label,
                    xyChart: true,
                    annotationAxis: "x",
                    xScale: "category",
                    feature: this.selectedFeature,
                    numerical: this.selectedIndicator.numerical,
                    unit: this.selectedIndicator.unit
                });
            }
        },

        /**
         * Retrieves feature statistics based on the selected feature.
         * @param {Object} options - The options for retrieving feature statistics.
         * @returns {Promise<void>} - A promise that resolves when the feature statistics are retrieved.
         */
        async showFeatureStats(compare) {
            this.$store.working = true; 
            let gj = this.format.writeGeometry(this.selectedFeature.getGeometry());
            gj = JSON.parse(gj);
            gj.crs = {
                type: "name",
                properties: {
                    name: "EPSG:" + this.srid.toString()
                }
            };

            let data = await this.post("Gdal/CalcStatisticsForGeometry", {
                file: this.selectedCatalog.source,
                geometry: JSON.stringify(gj),
                srid: this.srid.toString(),
                scale_factor: this.selectedTitle.scale_factor,
                no_data: this.selectedTitle.no_data.toString(),
            });

            if (data) {
                let name = this.selectedFeature.get("name");
                if (!name && this.selectedFeature.get("props")) {
                    let nameProp = this.selectedFeature.get("props").find(p => p.name == "Name");
                    if (nameProp) name = nameProp.value;

                }
                if (!name) name = this.selectedFeature.get("Name");
                if (compare) {

                    let id = this.selectedFeature.get("key");
                    let geometry_type_id = this.selectedFeature.get("geometry_type_id") ?? 3;
                
                    let thisData = { id: id, indicator_id : this.selectedCatalog.indicator_id, name: name, label: name, ...data.stat, x: data.x, y: data.y };
                    thisData.parent_id = id;
                    thisData.geometry_type_id = geometry_type_id;

                    this.$store.globalValues.indicator_id_val = this.selectedTitle.label;

                    let otherData = await this.get("Table/GetTable", {
                        dbFunction: "data.get_intersecting_geometries",
                        json: true,
                        pars: JSON.stringify({
                            id: id,
                            geometry_type_id: geometry_type_id,
                            indicator_id : this.selectedCatalog.indicator_id,
                        })
                    });
                    otherData = [thisData, ...otherData];

                    let action = this.getProps("/Compare");
                    action.path = "/Compare";  // see that user has permission to this path
                    action.data = otherData;
                    action.title = "Comparison of " + name + " with intersecting geometries for " + this.selectedTitle.label;

                    this.initPopup(action);

                } else {
                
                    this.initPopup({
                        component: "chart-popup", maximized: true,
                        chartType: "bar",
                        data: data,
                        stat: data.stat,
                        title: this.selectedTitle.label + " " + (name ?? "") + " (" + data.stat.n + " " + this.$t("points") + ")",
                        xyChart: true,
                        seriesName: this.$t("Frequency"),
                        annotationAxis: "x",
                        xScale: "category",
                        feature: this.selectedFeature,
                        numerical: true,
                        unit: this.selectedTitle.unit,
                        labelField: this.selectedTitle.label,
                    });
                }
            }
            this.$store.working = false;
        },

        /**
         * Shows the spectrum for the selected feature.
         * @async
         * @function showSpectrum
         * @returns {Promise<void>}
         */
        async showSpectrum() {
            let data = await this.get(`User/GetSpectrum/${this.selectedDataSource[0].value}/${this.selectedFeature.get("point_id")}/${date.formatDate(this.selectedFeature.get("date"), "YYYY-MM-DD")}`);
            this.initPopup({component : "chart-popup", maximized: true,
                chartType: "line",
                data: data,
                labelField: "wavelength",
                valueField: "value",
                //seriesField: "date",
                title: this.selectedIndicator.label + " - " + this.selectedFeature.get("name"),
                unit: "nm"
            });
        },

        /**
         * Rescales the given value based on the selected title's scale factor.
         * If the scale factor is a number, the value is multiplied by the scale factor.
         * If the scale factor is a formula, it is evaluated and applied to the value.
         * If the formula fails to evaluate, the original value is returned.
         * 
         * @param {number} value - The value to be rescaled.
         * @returns {number} The rescaled value.
         */
        rescale(value, scale_factor) {
            if (scale_factor.indexOf('x') == -1) {
                value = (value / scale_factor);
            } else {
                // this is a formula, so apply it to the value
                try {
                    let x = value;
                    value = eval(scale_factor);
                } catch (e) {
                    // if the formula fails, just return the value
                }
            }
            return value;
        },

        updateColorMap(selectedTitle, tiffLegend) {
            let color_map = "";
            if (selectedTitle.numerical)  {
                color_map = selectedTitle.color_map;
                // color_map = '["interpolate", ["linear"], ["band", 1]';
                // color_map += ", 0, [255, 255, 255, 0]";
                // let prev = 0;
                // for (let i = 0; i < tiffLegend.length; i++) {
                //     if (tiffLegend[i].active) {
                //         color_map += ", " + tiffLegend[i].original + ", [" + this.hexToRgbaValues(tiffLegend[i].color_code) + "]";
                //     } else
                // }
                // color_map += ", 9999, [0, 0, 0, 0]]"; 
            } else {
                color_map = '["match", ["band", 1]';
                for (let i = 0; i < tiffLegend.length; i++) {
                    if (tiffLegend[i].active) {
                        color_map += ", " + tiffLegend[i].from + ", [" + this.hexToRgbaValues(tiffLegend[i].color_code) + "]";
                    }
                };
                if (color_map == '["match", ["band", 1]') {
                    color_map += ", 0, [255, 255, 255, 0]";
                }
                color_map += ", [255, 255, 255, 0]]";
            }
            selectedTitle.tiffLayer.setStyle({
                color: JSON.parse(color_map)
            }); 

            console.log("colorMap", color_map);
        },

        /**
         * Creates a legend based on the provided color map.
         * @param {string} color_map - The color map in JSON format.
         */
        createLegend(selectedTitle, tiffLegend) {
            selectedTitle.allActive = true;
            if (selectedTitle.description) {
                tiffLegend.length = 0;
                let a = selectedTitle.description;
                Object.keys(a).forEach( key => {
                    tiffLegend.push({ color_code: a[key].color_code, text: a[key].name, from: key, to: key, active: true });
                });
                tiffLegend.sort((a, b) => a.text - b.text);
                console.log("tiffLegend", tiffLegend);
            } else if (selectedTitle.color_map > "") {
                let a = JSON.parse(selectedTitle.color_map);
                tiffLegend.length = 0;
                // for (let i = 5; i < a.length; i = i + 2) {
                //     a[i] = this.rescale(a[i]).toFixed(1);
                // }
                // for (let i = 5; i < a.length; i = i + 2) {
                //     let text, from, to;
                //     if (i == 5) {
                //         text = "< " + a[i];
                //         from = 0;
                //         to = a[i];
                //     } else {
                //         text = a[i - 2] + " - " + a[i];
                //         from = +a[i - 2];
                //         to = +a[i];
                //     }
                //     this.tiffLegend.push({ color_code : this.rgbaToHex(a[i+1]), text : text, from :from, to : to });
                // }
                let start = 5;
                if (a[0] == "match") start = 4;
                let end = a.length - 2;
                if (start == 4) end = a.length - 3;
                for (let i = start; i < end; i = i + 2) {
                    let original = a[i];
                    a[i] = this.rescale(a[i], selectedTitle.scale_factor).toFixed(2);
                    let text = a[i];
                    let from = +a[i];
                    let to = null;
                    tiffLegend.push({ color_code: this.rgbaToHex(a[i+1]), text: text, from: from, to: to, active: true, original: original });
                }
            }
            this.updateColorMap(selectedTitle, tiffLegend);
        },

        /**
         * Animates the map view to the extent of a given source.
         *
         * @param {Source} source - The source to animate to.
         */
        animateToExtent(source) {
            if (source == null) {
                if (this.pointSource && this.pointSource.getFeatures().length > 0) {
                    source = this.pointSource;
                } else if (this.shapeSource && this.shapeSource.getFeatures().length > 0) {
                    source = this.shapeSource;
                } else {
                    source = this.editableSource;
                    let count = 0;         
                    for (let f of this.editableSource.getFeatures()) {
                        //if (f.getId().startsWith('g')) {
                            count++;
                        //}
                    }
                    if (count > 0) {
                        let f = source.getFeatures()[0];
                        if (count == 1 && f.getGeometry().getType() === 'Point') {
                            this.map.getView().setCenter(f.getGeometry().getCoordinates());
                            return;
                        }
                    }
                }
            }

            let view = this.map.getView();
            let extent = source.getExtent();
            let zoom = view.getZoomForResolution(view.getResolutionForExtent(extent, this.map.getSize()) * 1.20);
            if (zoom > 16) zoom = 16;
            view.animate({
                zoom: zoom,
                center: getCenter(extent),
                duration: 1000,
            });
        },
    },
}