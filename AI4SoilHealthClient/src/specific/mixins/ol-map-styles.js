/**
 * @desc A mixin object containing map styles for the OpenLayers map.
 * @module MapStylesMixin
 */
import { Style, Stroke, Fill, Circle, RegularShape, Text, Icon } from 'ol/style';
import { LineString, Point } from 'ol/geom';

const labelFont = '14px Calibri,sans-serif';

export const MapStylesMixin = {
    data() {
        return {

            shapeStyle: new Style({
                stroke: new Stroke({
                    color: '#3399CC',
                    width: 1,
                }),
                fill: new Fill({
                    color: 'rgba(255,255,255, 0.5)',
                }),
            }),

            selectStyle: new Style({
                zIndex: 1000,
                stroke: new Stroke({
                    color: '#3399CC',
                    width: 3,
                }),
                fill: new Fill({
                    color: 'rgba(255,255,255, 0.3)',
                }),
                image: new Circle({
                    fill: new Fill({
                        color: '#3399CC'
                    }),
                }),
            }),

            positionStyle: new Style({
                image: new Icon({
                    anchor: [0.5, 0.5],
                    anchorXUnits: 'fraction',
                    anchorYUnits: 'fraction',
                    src: 'my_location.svg'
                }),
            }),

            labelStyleYellow: new Style({
                zIndex: 2000,
                text: new Text({
                    font: labelFont,
                    fill: new Fill({
                        color: "yellow"
                    }),
                    textBaseline: 'middle',
                    offsetY: 0
                }),
            }),

            labelStyleBlack: new Style({
                zIndex: 2000,
                text: new Text({
                    font: labelFont,
                    fill: new Fill({
                        color: "black"
                    }),
                    textBaseline: 'middle',
                    offsetY: 0
                }),
            }),
        }
    },
    methods: {
         /**
         * Returns the style for a boundary feature.
         * @param {Feature} feature - The boundary feature.
         * @returns {Style} The style for the boundary feature.
         */
        getBoundaryStyle(feature) {           
            let color = feature.get('color_code') ?? '#ff0000';
            let style = this.styleCache["B" + color];
            if (!style) {
                style = new Style({
                    stroke: new Stroke({
                        color: color,
                    }),
                    fill: new Fill({
                        color: color + '3F',
                    }),
                });
                this.styleCache["B"+color] = style;
            }
            return style;
        },

        /**
         * Returns the style for a given point based on whether it is clustered or not.
         *
         * @param {Feature} feature - The feature for which to get the style.
         * @param {boolean} clustered - Indicates whether the feature is clustered or not.
         * @param {number} zoom - The current zoom level of the map.
         * @returns {Style} The style for the feature.
         */
        getPointStyle(feature, clustered, zoom) {
            let style;
            let radius = Math.round(zoom * 2 / 3);
            let size = 1;
            if (clustered) {
                let f = feature.get('features');
                if (f) {
                    size = f.length;
                    if (size == 1) feature = f[0];
                }
            }

            if (size == 1) {
                let color = feature.get('color_code') ?? '#f0f0f0';
                let shape = feature.get('data_source_order_no') % 3;
                style = this.styleCache["P"+shape+color+radius];
                if (!style) {
                    if (shape == 0) {
                        style = new Style({
                            image: new Circle({
                                radius: radius,
                                stroke: new Stroke({
                                    color: '#707070',
                                }),
                                fill: new Fill({
                                    color: color,
                                }),
                            }),
                        });
                    } else if (shape == 1) {
                        style = new Style({
                            image: new RegularShape({
                                fill: new Fill({
                                    color: color,
                                }),
                                stroke: new Stroke({
                                    color: '#707070',
                                }),
                                points: 3,
                                radius: radius,
                                angle: 0,
                            }),
                        });
                    } else {
                        style = new Style({
                            image: new RegularShape({
                                fill: new Fill({
                                    color: color,
                                }),
                                stroke: new Stroke({
                                    color: '#707070',
                                }),
                                points: 4,
                                radius: radius,
                                angle: Math.PI / 4,
                            }),
                        });
                    }
                    this.styleCache["P"+shape+color+radius] = style;
                }
            } else {
                //const size = feature.get('features').length;
                style = this.styleCache["C" + size];
                if (!style) {
                    style = new Style({
                        image: new Circle({
                            radius: 7,
                            stroke: new Stroke({
                                color: '#fff',
                            }),
                            fill: new Fill({
                                color: '#3399CC',
                            }),
                        }),
                        text: new Text({
                        text: size.toString(),
                            fill: new Fill({
                                color: '#fff',
                            }),
                        }),
                    });
                    this.styleCache["C" + size] = style;
                }
            }
            return style;
        },
        
        /**
         * Returns the style for a given observation point based on zoom.
         *
         * @param {Feature} feature - The feature for which to get the style.
         * @param {number} zoom - The current zoom level of the map.
         * @returns {Style} The style for the feature.
         */
        getObservationPointStyle(feature, zoom) {
            let style;
            //if (feature.get('extension') == '.jpg') {
            if (feature.get("image_id")) {  // it is an image
                let compass = feature.get('compass');
                if (compass == null) {
                    compass = 0;
                }
                style = this.styleCache[".jpg" + compass];
                let scale = this.$store.isWide ? 2 : 3;
                if (!style) {
                    style = new Style({
                        image: new Icon({
                            anchor: [0.5, 0.8],
                            anchorXUnits: 'fraction',
                            anchorYUnits: 'fraction',
                            src: 'viewpoint.svg',
                            scale: scale,
                            rotateWithView: true,
                            rotation: Math.PI * compass / 180,
                        }),
                    });
                    this.styleCache[".jpg" + compass] = style;
                }
            } else {
                let radius=Math.round(zoom * 2 / 3);
                if (!this.$store.isWide) radius *= 1.5;
                style = this.styleCache["Pred" + radius];
                if (!style) {
                    style = new Style({
                        image: new Circle({
                            radius: radius,
                            stroke: new Stroke({
                                color: '#707070',
                            }),
                            fill: new Fill({
                                color: "red",
                            }),
                        }),
                    });
                    this.styleCache["Pred" + radius] = style;
                }
            }
            return style;
        },

        /**
         * Generates an array of styles for a given feature.
         * @param {Feature} feature - The feature to generate styles for.
         * @returns {Array<Style>} - An array of styles for the feature.
         */
        selectedStyleFunction(feature) {
            let styles = [];
            styles.push(this.selectStyle);
            let geometry = feature.getGeometry();
            let type = geometry.getType();
            let point, label, line, style, text, segment, centerPoint, pointCoords;
            if (type == "Polygon") {
                line = new LineString(geometry.getCoordinates()[0]);
                centerPoint = geometry.getInteriorPoint();
                label = this.formatArea(geometry);
                style = this.selectedBaseLayer.value == 1 ? this.labelStyleBlack.clone() : this.labelStyleYellow.clone();
                if (line.getCoordinates().length > 3) style.getText().setText(label);
                style.setGeometry(centerPoint);
                styles.push(style);
            }
            if (type != "Point") {
                if (!line) line = geometry;
                if (line.forEachSegment) {
                    let self = this;
                    line.forEachSegment(function (a, b) {
                        segment = new LineString([a, b]);
                        label = self.formatLength(segment);
                        style = self.selectedBaseLayer.value == 1 ? self.labelStyleBlack.clone() : self.labelStyleYellow.clone();
                        pointCoords = segment.getCoordinateAt(0.5);
                        if (type == "Polygon") {
                            let pixel = self.map.getPixelFromCoordinate(pointCoords); 
                            let unitVectorAB = self.unitVector(centerPoint.getCoordinates(), pointCoords);
                            let wh = self.getTextDimensions(label, labelFont);
                            let pixelOffset = [unitVectorAB[0] * (wh[0] / 2 + 2), -unitVectorAB[1] * (wh[1] / 2 + 2)];
                            pixel[0] += pixelOffset[0];
                            pixel[1] += pixelOffset[1];
                            pointCoords = self.map.getCoordinateFromPixel(pixel);
                        }
                        point = new Point(pointCoords);
                        style.setGeometry(point);
                        style.getText().setText(label);
                        styles.push(style);
                    });
                }    
            }
            return styles;
        },

        /**
         * Get the dimensions of a given text string.
         * @param {*} text 
         * @returns [width, height]
         */
        getTextDimensions(text, font) {
            var span = document.createElement('span');
            span.style.font = font;
            span.textContent = text;
            document.body.appendChild(span);
            var width = span.offsetWidth;
            var height = span.offsetHeight;
            document.body.removeChild(span);
            return [width, height];
        },

        /**
         * Calculates the unit vector from point a to point b.
         * @param {number[]} a - The coordinates of point a.
         * @param {number[]} b - The coordinates of point b.
         * @returns {number[]} The unit vector from point a to point b.
         */
        unitVector(a, b) {
            // Calculate the vector from a to b
            var vectorAB = [b[0] - a[0], b[1] - a[1]];
            // Calculate the length of the vector
            var lengthAB = Math.sqrt(vectorAB[0] * vectorAB[0] + vectorAB[1] * vectorAB[1]);
            // Normalize the vector (make it a unit vector)
            if (lengthAB == 0) return [0, 0]; // Prevent division by zero (if a and b are the same point
            else return [vectorAB[0] / lengthAB, vectorAB[1] / lengthAB];         
        },

        /**
         * Extends a line segment from point a to point b by a specified distance.
         * @param {number[]} a - The coordinates of point a.
         * @param {number[]} b - The coordinates of point b.
         * @param {number} distance - The distance by which to extend the line segment.
         * @returns {number[]} The coordinates of the extended point c.
         */
        extendLine(a, b, distance) {
            var unitVectorAB = this.unitVector(a, b);
            var c = [b[0] + unitVectorAB[0] * distance, b[1] + unitVectorAB[1] * distance];
            return c;
        },
    }
}