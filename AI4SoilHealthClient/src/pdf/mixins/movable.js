/**
 * @desc A mixin object containing methods for moving a component.
 * @module MovableMixin
 */

// const DEBUG = false;
const DEBUG = true;

export const MovableMixin = {
    data() {
        return {
            startX: 0,
            startY: 0,
            startLeft: 0,
            startTop: 0,
            startWidth: 0,
            startHeight: 0,
            // lastDifference: null,
            lastPosition: null,
            startClientRect: null,
        };
    },
    computed: {
        usingDragGrid() {
            const [gridX, gridY] = this.getAbsoluteDragGrid();
            return gridX > 0 || gridY > 0;
        },
    },
    methods: {
        getChangeToSnapToGrid(distanceToGrid, grid, roundMethod) {
            if (roundMethod === 'round') {
                return distanceToGrid > grid / 2
                    ? grid - distanceToGrid
                    : -distanceToGrid;
            }
            if (roundMethod === 'floor') {
                return -distanceToGrid;
            }
            if (roundMethod === 'ceil') {
                return grid - distanceToGrid;
            }
        },

        // getDifferenceOnGrid(
        //     differenceX,
        //     differenceY,
        //     { roundMethod = 'round' } = {}
        // ) {
        //     const [gridX, gridY] = this.getAbsoluteDragGrid();
        //     const containerClientRect = this.getContainerClientRect();
        //     if (gridX > 0) {
        //         const relativeToContainerX =
        //             this.startClientRect.left -
        //             containerClientRect.left +
        //             differenceX;
        //         const distanceToGridX = relativeToContainerX % gridX;
        //         const changeToSnapToGridX = this.getChangeToSnapToGrid(
        //             distanceToGridX,
        //             gridX,
        //             roundMethod
        //         );
        //         differenceX += changeToSnapToGridX;
        //     }
        //     if (gridY > 0) {
        //         const relativeToContainerY =
        //             this.startClientRect.top -
        //             containerClientRect.top +
        //             differenceY;
        //         const distanceToGridY = relativeToContainerY % gridY;
        //         const changeToSnapToGridY = this.getChangeToSnapToGrid(
        //             distanceToGridY,
        //             gridY,
        //             roundMethod
        //         );
        //         differenceY += changeToSnapToGridY;
        //     }
        //     return [differenceX, differenceY];
        // },

        getPositionOnGrid({ left, top }, { roundMethod = 'round' } = {}) {
            const [gridX, gridY] = this.getAbsoluteDragGrid();
            if (gridX > 0) {
                const distanceFromGridX = left % gridX;
                const changeToSnapToGridX = this.getChangeToSnapToGrid(
                    distanceFromGridX,
                    gridX,
                    roundMethod,
                );
                left += changeToSnapToGridX;
            }
            if (gridY > 0) {
                const distanceFromGridY = top % gridY;
                const changeToSnapToGridY = this.getChangeToSnapToGrid(
                    distanceFromGridY,
                    gridY,
                    roundMethod,
                );
                top += changeToSnapToGridY;
            }
            return { left, top };
        },

        // getDifferenceInsideContainer(differenceX, differenceY) {
        //     const containerClientRect = this.getContainerClientRect();
        //     differenceX = Math.min(
        //         differenceX,
        //         containerClientRect.right - this.startClientRect.right
        //     );
        //     differenceX = Math.max(
        //         differenceX,
        //         containerClientRect.left - this.startClientRect.left
        //     );
        //     differenceY = Math.min(
        //         differenceY,
        //         containerClientRect.bottom - this.startClientRect.bottom
        //     );
        //     differenceY = Math.max(
        //         differenceY,
        //         containerClientRect.top - this.startClientRect.top
        //     );
        //     return [differenceX, differenceY];
        // },

        getPositionInsideContainer({ left, top }) {
            const containerSize = this.getContainerSize();
            if (
                containerSize === null ||
                (containerSize.width === 0 && containerSize.height === 0)
            ) {
                return { left, top };
            }
            let maximumPosition = {
                left: containerSize.width - this.startWidth,
                top: containerSize.height - this.startHeight,
            };
            if (this.usingDragGrid) {
                maximumPosition = this.getPositionOnGrid(maximumPosition, {
                    roundMethod: 'floor',
                });
            }
            left = Math.max(left, 0);
            left = Math.min(left, maximumPosition.left);
            top = Math.max(top, 0);
            top = Math.min(top, maximumPosition.top);
            return { left, top };
        },

        // processDifference(
        //     differenceX,
        //     differenceY,
        //     // totalDifferenceX,
        //     // totalDifferenceY
        // ) {
        //     if (this.usingDragGrid) {
        //         [differenceX, differenceY] = this.getDifferenceOnGrid(
        //             differenceX,
        //             differenceY
        //         );
        //     }
        //     if (this.isForcedInsideContainer()) {
        //         [differenceX, differenceY] = this.getDifferenceInsideContainer(
        //             differenceX,
        //             differenceY
        //         );
        //     }
        //     const difference = {
        //         x: differenceX,
        //         y: differenceY,
        //         // totalX: totalDifferenceX + differenceX,
        //         // totalY: totalDifferenceY + differenceY,
        //     };
        //     return difference;
        // },

        getCheckedPosition(left, top) {
            let position = { left, top };
            if (this.usingDragGrid) {
                // [differenceX, differenceY] = this.getDifferenceOnGrid(
                //     differenceX,
                //     differenceY
                // );
                position = this.getPositionOnGrid(position);
            }
            if (this.isForcedInsideContainer()) {
                // [differenceX, differenceY] = this.getDifferenceInsideContainer(
                //     differenceX,
                //     differenceY
                // );
                position = this.getPositionInsideContainer(position);
            }
            return position;
        },

        getPositionFromDifference(differenceX, differenceY) {
            let left = this.startLeft + differenceX;
            let top = this.startTop + differenceY;
            const position = this.getCheckedPosition(left, top);
            return position;
        },

        // processMovableSetup(totalDifferenceX, totalDifferenceY) {
        processMovableSetup(left, top, width, height) {
            // if (this.isForcedInsideContainer() || this.usingDragGrid) {
            //     this.startClientRect = this.getElementClientRect();
            // }
            // const difference = this.processDifference(
            //     0,
            //     0,
            //     totalDifferenceX,
            //     totalDifferenceY
            // );
            this.startWidth = width;
            this.startHeight = height;
            const position = this.getCheckedPosition(left, top);
            return position;
        },

        initiateMove(clientX, clientY, left, top, width, height) {
            this.startX = clientX;
            this.startY = clientY;
            this.startLeft = left;
            this.startTop = top;
            this.startWidth = width;
            this.startHeight = height;
            // if (this.isForcedInsideContainer() || this.usingDragGrid) {
            //     this.startClientRect = this.getElementClientRect();
            // }
        },

        // processMove(clientX, clientY, totalDifferenceX, totalDifferenceY) {
        processMove(clientX, clientY) {
            let differenceX = clientX - this.startX;
            let differenceY = clientY - this.startY;
            // const difference = this.processDifference(
            //     differenceX,
            //     differenceY,
            // totalDifferenceX,
            // totalDifferenceY
            // );
            const position = this.getPositionFromDifference(
                differenceX,
                differenceY,
            );
            // this.lastDifference = difference;
            this.lastPosition = position;
            // return difference;
            return position;
        },

        completeMove() {
            // return this.lastDifference;
            return this.lastPosition;
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

        getAbsoluteDragGrid() {
            if (DEBUG) {
                console.warn(
                    'The method getAbsoluteDragGrid is not implemented.',
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
    },
};
