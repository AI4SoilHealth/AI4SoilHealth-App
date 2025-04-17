/**
 * @desc A mixin object containing methods for resizing a component.
 * @module ResizableMixin
 */

// const DEBUG = false;
const DEBUG = true;

export const ResizableMixin = {
    data() {
        return {
            activeHandlerName: null,
            startX: 0,
            startY: 0,
            startLeft: 0,
            startTop: 0,
            startWidth: 0,
            startHeight: 0,
            // lastDifference: null,
            lastPosition: null,
            lastSize: null,
            startClientRect: null,
        };
    },
    computed: {
        // TODO: check if it is enough to use
        // these computed properties as data properties
        // props are updated from parent component
        usingResizeGrid() {
            const [gridX, gridY] = this.getAbsoluteResizeGrid();
            return gridX > 0 || gridY > 0;
        },
        minimumWidthOnGrid() {
            const gridX = this.getAbsoluteResizeGrid()[0];
            const minimumWidth = this.getMinimumWidth();
            if (gridX === 0) {
                return minimumWidth;
            }
            return Math.ceil(minimumWidth / gridX) * gridX;
        },
        minimumHeightOnGrid() {
            const gridY = this.getAbsoluteResizeGrid()[1];
            const minimumHeight = this.getMinimumHeight();
            if (gridY === 0) {
                return minimumHeight;
            }
            return Math.ceil(minimumHeight / gridY) * gridY;
        },
    },
    methods: {
        // getClientPositionInsideContainer(clientX, clientY) {
        //     const containerClientRect = this.getContainerClientRect();
        //     const clientXInsideContainer = Math.max(
        //         containerClientRect.left,
        //         Math.min(containerClientRect.right, clientX)
        //     );
        //     const clientYInsideContainer = Math.max(
        //         containerClientRect.top,
        //         Math.min(containerClientRect.bottom, clientY)
        //     );
        //     return [clientXInsideContainer, clientYInsideContainer];
        // },

        getSizeWithRatio({ width, height }, fitMethod = 'fill') {
            if (this.startWidth === 0 || this.startHeight === 0) {
                return { width, height };
            }
            const currentRation = width / height;
            const requiredRatio = this.startWidth / this.startHeight;
            if (fitMethod === 'fill') {
                if (currentRation > requiredRatio) {
                    height = width / requiredRatio;
                } else {
                    width = height * requiredRatio;
                }
            } else if (fitMethod === 'contain') {
                if (currentRation < requiredRatio) {
                    height = width / requiredRatio;
                } else {
                    width = height * requiredRatio;
                }
            }
            return { width, height };
        },

        getSizeComponentOnGrid(sizeComponent, grid, roundMethod) {
            const precision = this.getInternalRoundingPrecision();
            const quotient = this.roundTo(sizeComponent / grid, precision);
            if (roundMethod === 'round') {
                return Math.round(quotient) * grid;
            }
            if (roundMethod === 'floor') {
                return Math.floor(quotient) * grid;
            }
            if (roundMethod === 'ceil') {
                return Math.ceil(quotient) * grid;
            }
        },

        getSizeOnGrid({ width, height }, { roundMethod = 'round' } = {}) {
            const [gridX, gridY] = this.getAbsoluteResizeGrid();
            if (gridX > 0) {
                // width = Math.round(width / gridX) * gridX;
                width = this.getSizeComponentOnGrid(width, gridX, roundMethod);
            }
            if (gridY > 0) {
                // height = Math.round(height / gridY) * gridY;
                height = this.getSizeComponentOnGrid(
                    height,
                    gridY,
                    roundMethod,
                );
            }
            return { width, height };
        },

        getSizeInsideContainer({ width, height }) {
            const [gridX, gridY] = this.getAbsoluteResizeGrid();
            // const containerClientRect = this.getContainerClientRect();
            const containerSize = this.getContainerSize();
            if (
                containerSize === null ||
                (containerSize.width === 0 && containerSize.height === 0)
            ) {
                return { width, height };
            }
            // let heightMaximum = containerClientRect.height;
            let heightMaximum = containerSize.height;
            if (this.activeHandlerName === null) {
                heightMaximum = containerSize.height - this.startTop;
            } else if (this.activeHandlerName.startsWith('t')) {
                // heightMaximum =
                //     this.startClientRect.bottom - containerClientRect.top;
                heightMaximum = this.startTop + this.startHeight;
            } else if (this.activeHandlerName.startsWith('b')) {
                // heightMaximum =
                //     containerClientRect.bottom - this.startClientRect.top;
                heightMaximum = containerSize.height - this.startTop;
            }
            if (gridY > 0) {
                heightMaximum = Math.floor(heightMaximum / gridY) * gridY;
            }
            heightMaximum = Math.max(heightMaximum, this.minimumHeightOnGrid);
            height = Math.min(height, heightMaximum);
            // let widthMaximum = containerClientRect.width;
            let widthMaximum = containerSize.width;
            if (this.activeHandlerName === null) {
                widthMaximum = containerSize.width - this.startLeft;
            } else if (this.activeHandlerName.endsWith('l')) {
                // widthMaximum =
                //     this.startClientRect.right - containerClientRect.left;
                widthMaximum = this.startLeft + this.startWidth;
            } else if (this.activeHandlerName.endsWith('r')) {
                // widthMaximum =
                //     containerClientRect.right - this.startClientRect.left;
                widthMaximum = containerSize.width - this.startLeft;
            }
            if (gridX > 0) {
                widthMaximum = Math.floor(widthMaximum / gridX) * gridX;
            }
            widthMaximum = Math.max(widthMaximum, this.minimumWidthOnGrid);
            width = Math.min(width, widthMaximum);
            return { width, height };
        },

        getCheckedSize(
            width,
            height,
            { roundMethod = 'round', ignoreResizeGrid = false } = {},
        ) {
            width = Math.max(width, this.minimumWidthOnGrid);
            height = Math.max(height, this.minimumHeightOnGrid);
            let size = { width, height };
            if (this.getKeepRatio()) {
                size = this.getSizeWithRatio(size, 'fill');
            }
            if (this.usingResizeGrid && !ignoreResizeGrid) {
                size = this.getSizeOnGrid(size, { roundMethod });
            }
            if (this.isForcedInsideContainer()) {
                size = this.getSizeInsideContainer(size);
            }
            if (this.getKeepRatio()) {
                size = this.getSizeWithRatio(size, 'contain');
            }
            return size;
        },

        getSizeFromDifference(differenceX, differenceY) {
            let resizeDifferenceX, resizeDifferenceY;
            if (this.activeHandlerName.startsWith('t')) {
                resizeDifferenceY = -differenceY;
            } else if (this.activeHandlerName.startsWith('b')) {
                resizeDifferenceY = differenceY;
            } else {
                resizeDifferenceY = 0;
            }
            if (this.activeHandlerName.endsWith('l')) {
                resizeDifferenceX = -differenceX;
            } else if (this.activeHandlerName.endsWith('r')) {
                resizeDifferenceX = differenceX;
            } else {
                resizeDifferenceX = 0;
            }
            const width = this.startWidth + resizeDifferenceX;
            const height = this.startHeight + resizeDifferenceY;
            const size = this.getCheckedSize(width, height);
            return size;
        },

        // // getDifference(width, height, totalDifferenceX, totalDifferenceY) {
        // getDifference(width, height) {
        //     let moveDifferenceY = 0;
        //     if (this.activeHandlerName.startsWith('t')) {
        //         moveDifferenceY = this.startHeight - height;
        //     }
        //     let moveDifferenceX = 0;
        //     if (this.activeHandlerName.endsWith('l')) {
        //         moveDifferenceX = this.startWidth - width;
        //     }
        //     const difference = {
        //         x: moveDifferenceX,
        //         y: moveDifferenceY,
        //         // totalX: totalDifferenceX + moveDifferenceX,
        //         // totalY: totalDifferenceY + moveDifferenceY,
        //     };
        //     return difference;
        // },

        getPosition({ width, height }) {
            let moveDifferenceY = 0;
            if (this.activeHandlerName.startsWith('t')) {
                moveDifferenceY = this.startHeight - height;
            }
            let moveDifferenceX = 0;
            if (this.activeHandlerName.endsWith('l')) {
                moveDifferenceX = this.startWidth - width;
            }
            const left = this.startLeft + moveDifferenceX;
            const top = this.startTop + moveDifferenceY;
            const position = { left, top };
            return position;
        },

        processResizableSetup(
            width,
            height,
            { roundMethod = 'round', ignoreResizeGrid = false } = {},
        ) {
            const size = this.getCheckedSize(width, height, {
                roundMethod,
                ignoreResizeGrid,
            });
            return size;
        },

        initiateResize(
            handlerName,
            clientX,
            clientY,
            left,
            top,
            width,
            height,
        ) {
            this.activeHandlerName = handlerName;
            this.startX = clientX;
            this.startY = clientY;
            this.startLeft = left;
            this.startTop = top;
            this.startWidth = width;
            this.startHeight = height;
            // if (this.isForcedInsideContainer() || this.usingResizeGrid) {
            //     this.startClientRect = this.getElementClientRect();
            // }
        },

        // processResize(clientX, clientY, totalDifferenceX, totalDifferenceY) {
        processResize(clientX, clientY) {
            // if (this.isForcedInsideContainer()) {
            //     [clientX, clientY] = this.getClientPositionInsideContainer(
            //         clientX,
            //         clientY
            //     );
            // }
            const differenceX = clientX - this.startX;
            const differenceY = clientY - this.startY;
            let size = this.getSizeFromDifference(differenceX, differenceY);

            // const difference = this.getDifference(
            //     size.width,
            //     size.height,
            //     // totalDifferenceX,
            //     // totalDifferenceY
            // );
            const position = this.getPosition(size);
            // this.lastDifference = difference;
            this.lastPosition = position;
            this.lastSize = size;
            return [size, position];
        },

        completeResize() {
            // return this.lastDifference;
            this.activeHandlerName = null;
            return [this.lastPosition, this.lastSize];
        },

        // getContainerClientRect() {
        //     if (DEBUG) {
        //         console.warn(
        //             'The method getContainerClientRect is not implemented.'
        //         );
        //     }
        // },

        isForcedInsideContainer() {
            if (DEBUG) {
                console.warn(
                    'The method isForcedInsideContainer is not implemented.',
                );
            }
        },

        getAbsoluteResizeGrid() {
            if (DEBUG) {
                console.warn(
                    'The method getAbsoluteResizeGrid is not implemented.',
                );
            }
        },

        // getElementClientRect() {
        //     if (DEBUG) {
        //         console.warn(
        //             'The method getElementClientRect is not implemented.'
        //         );
        //     }
        // },

        getContainerSize() {
            if (DEBUG) {
                console.warn('The method getContainerSize is not implemented.');
            }
        },

        getKeepRatio() {
            if (DEBUG) {
                console.warn('The method getKeepRatio is not implemented.');
            }
        },

        getMinimumHeight() {
            if (DEBUG) {
                console.warn('The method getMinimumHeight is not implemented.');
            }
        },

        getMinimumWidth() {
            if (DEBUG) {
                console.warn('The method getMinimumWidth is not implemented.');
            }
        },

        getInternalRoundingPrecision() {
            if (DEBUG) {
                console.warn(
                    'The method getInternalRoundingPrecision ' +
                        'is not implemented.',
                );
            }
        },
    },
};
