<template>
    <div
        class="interactable-element"
        :style="interactableElementStyle"
        @mousedown="onMouseDown"
    >
        <div
            ref="content"
            class="content"
            :style="[contentStyleInternal, contentStyle]"
        >
            <slot></slot>
        </div>
        <template v-if="resizable && isSelected">
            <div
                v-for="handlerName in resizeHandlers"
                :key="handlerName"
                :class="'resize-handler ' + handlerName"
                :style="resizeHandlerStyles[handlerName]"
                @mousedown.stop="resizeHandlerOnMouseDown($event, handlerName)"
            ></div>
        </template>
    </div>
</template>

<script>
import { MovableMixin } from '../mixins/movable.js';
import { ResizableMixin } from '../mixins/resizable.js';
import {
    PdfUtilsMixin,
    DefaultStyleRoundingPrecision,
    DefaultInternalRoundingPrecision,
    validateRoundingPrecision,
} from '../mixins/pdf.js';

const ResizeHandlerEnum = Object.freeze({
    TopLeft: 'tl',
    Top: 't',
    TopRight: 'tr',
    Right: 'r',
    BottomRight: 'br',
    Bottom: 'b',
    BottomLeft: 'bl',
    Left: 'l',
});

const validateSizeAndPositionProperties = (value) => {
    if (!Array.isArray(value)) {
        return Number.isFinite(value) && value >= 0;
    }
    if (!Number.isFinite(value[0]) || !(value[0] >= 0)) {
        return false;
    }
    if (typeof value[1] !== 'object') {
        return false;
    }
    const optionNames = Object.keys(value[1]);
    return (
        optionNames.every((key) =>
            ['useRelativeUnits', 'relativeTo'].includes(key),
        ) &&
        (!optionNames.includes('useRelativeUnits') ||
            typeof value[1].useRelativeUnits === 'boolean') &&
        (!optionNames.includes('relativeTo') ||
            (typeof value[1].relativeTo === 'string' &&
                ['width', 'height'].includes(value[1].relativeTo)))
    );
};

const validateGridProperties = (value) =>
    value.length === 2 &&
    value.every((item) => validateSizeAndPositionProperties(item));

const emitNames = [
    'drag-setup',
    'drag-start',
    'drag-update',
    'drag-end',
    'position-update',
    'resize-setup',
    'resize-start',
    'resize-update',
    'resize-end',
    'size-update',
    'select-start',
    'select-end',
    'update:selected',
    'layout-update',
];

export default {
    name: 'InteractableElement',
    mixins: [MovableMixin, ResizableMixin, PdfUtilsMixin],
    props: {
        draggable: { type: Boolean },
        emitChanges: {
            type: [Boolean, Array],
            default: false,
            validator: (value) =>
                !Array.isArray(value) ||
                value.every((option) => emitNames.includes(option)),
        },
        /* eslint-disable-next-line vue/no-boolean-default */
        forceInsideContainer: { type: Boolean, default: true },
        grid: {
            type: Array,
            default: () => [0, 0],
            validator: validateGridProperties,
        },
        dragGrid: {
            type: Array,
            default: null,
            validator: validateGridProperties,
        },
        resizeGrid: {
            type: Array,
            default: null,
            validator: validateGridProperties,
        },
        resizable: { type: Boolean },
        resizeHandlerColor: { type: String, default: 'blue' },
        resizeHandlerSize: {
            type: Number,
            default: 6,
            validator: (value) => Number.isFinite(value),
        },
        resizeHandlers: {
            type: Array,
            default: () => ['tl', 'tr', 'bl', 'br'],
            validator(value) {
                const uniqueOptions = [...new Set(value)];
                if (uniqueOptions.length !== value.length) {
                    return false;
                }
                return uniqueOptions.every((option) =>
                    Object.values(ResizeHandlerEnum).includes(option),
                );
            },
        },
        left: {
            type: [Number, Array],
            default: 0,
            validator: validateSizeAndPositionProperties,
        },
        top: {
            type: [Number, Array],
            default: 0,
            validator: validateSizeAndPositionProperties,
        },
        minimumWidth: {
            type: Number,
            default: 10,
            validator: (value) => value >= 0,
        },
        minimumHeight: {
            type: Number,
            default: 10,
            validator: (value) => value >= 0,
        },
        width: {
            type: [Number, Array, String],
            default: (properties) => properties.minimumWidth,
            validator: (value) =>
                validateSizeAndPositionProperties(value) || value === 'auto',
        },
        height: {
            type: [Number, Array, String],
            default: (properties) => properties.minimumHeight,
            validator: (value) =>
                validateSizeAndPositionProperties(value) || value === 'auto',
        },
        containerSize: {
            type: Object,
            default: () => ({ width: 0, height: 0 }),
            validator: (value) =>
                Number.isFinite(value.width) &&
                value.width >= 0 &&
                Number.isFinite(value.height) &&
                value.height >= 0,
        },
        fitContent: { type: Boolean },
        waitContent: { type: Boolean },
        selected: { type: Boolean },
        showImmediately: { type: Boolean },
        editingToolbarSelector: { type: String, default: '' },
        keepRatio: { type: [Boolean, Function], default: false },
        justifyContent: {
            type: String,
            default: 'center',
            validator: (value) => ['left', 'center', 'right'].includes(value),
        },
        contentStyle: { type: Object, default: () => ({}) },
        styleRoundingPrecision: {
            type: Number,
            default: DefaultStyleRoundingPrecision,
            validator: validateRoundingPrecision,
        },
        internalRoundingPrecision: {
            type: Number,
            default: DefaultInternalRoundingPrecision,
            validator: validateRoundingPrecision,
        },
        recalculateLayoutNumber: {
            type: Number,
            default: 0,
            validator: (value) => value >= 0,
        },
    },
    emits: [
        ...emitNames,
        'update:left',
        'update:top',
        'update:width',
        'update:height',
    ],
    data() {
        return {
            absoluteLeft: null,
            absoluteTop: null,
            absoluteWidth: null,
            absoluteHeight: null,
            isMoving: false,
            isResizing: false,
            isSelected: false,
            cursor: '',
            isLayoutSetUp: false,
            watchingProperties: false,
        };
    },
    computed: {
        // Check when (how many times) are computed properties called
        interactableElementStyle() {
            const style = {
                cursor:
                    this.draggable && (this.isMoving || this.isResizing)
                        ? 'move'
                        : '',
            };
            if (!this.isLayoutSetUp) {
                style.visibility = this.showImmediately ? 'visible' : 'hidden';
            }
            return style;
        },
        contentStyleInternal() {
            // TODO: check what style has to be when fitContent is true
            // if (!this.fitContent) {
            //     return { width: '100%', height: '100%' };
            // }
            return {
                width: '100%',
                height: '100%',
                justifyContent: this.justifyContent,
            };
            // return {};
        },
        resizeHandlerStyles() {
            const resizeHandlerStyles = {};
            const halfSize = this.roundTo(
                this.resizeHandlerSize / 2,
                this.styleRoundingPrecision,
            );
            const positionStyles = new Map([
                ['tl', { top: `-${halfSize}px`, left: `-${halfSize}px` }],
                [
                    't',
                    {
                        top: `-${halfSize}px`,
                        left: `calc(50% - ${halfSize}px)`,
                    },
                ],
                ['tr', { top: `-${halfSize}px`, right: `-${halfSize}px` }],
                [
                    'r',
                    {
                        top: `calc(50% - ${halfSize}px)`,
                        right: `-${halfSize}px`,
                    },
                ],
                ['br', { bottom: `-${halfSize}px`, right: `-${halfSize}px` }],
                [
                    'b',
                    {
                        bottom: `-${halfSize}px`,
                        left: `calc(50% - ${halfSize}px)`,
                    },
                ],
                ['bl', { bottom: `-${halfSize}px`, left: `-${halfSize}px` }],
                [
                    'l',
                    {
                        top: `calc(50% - ${halfSize}px)`,
                        left: `-${halfSize}px`,
                    },
                ],
            ]);
            const sizeCssPixelValue = this.getCSSPixelValue(
                this.resizeHandlerSize,
            );
            for (const handlerName of this.resizeHandlers) {
                resizeHandlerStyles[handlerName] = {
                    width: sizeCssPixelValue,
                    height: sizeCssPixelValue,
                    ...positionStyles.get(handlerName),
                    backgroundColor: this.resizeHandlerColor,
                };
            }
            return resizeHandlerStyles;
        },
        absoluteDragGrid() {
            if (!this.draggable) {
                return [0, 0];
            }
            const [gridWidth, gridHeight] = this.dragGrid ?? this.grid;
            return [
                this.getAbsoluteSizeOrPosition(gridWidth, 'width'),
                this.getAbsoluteSizeOrPosition(gridHeight, 'height'),
            ];
        },
        absoluteResizeGrid() {
            if (!this.resizable) {
                return [0, 0];
            }
            const [gridWidth, gridHeight] = this.resizeGrid ?? this.grid;
            return [
                this.getAbsoluteSizeOrPosition(gridWidth, 'width'),
                this.getAbsoluteSizeOrPosition(gridHeight, 'height'),
            ];
        },
        emitEvents() {
            if (Array.isArray(this.emitChanges)) {
                return this.emitChanges;
            }
            return this.emitChanges === true ? emitNames : [];
        },
    },
    watch: {
        left(newValue, oldValue) {
            this.onLayoutPropertyChange(
                'left',
                newValue,
                oldValue,
                'width',
                this.draggable,
            );
        },
        top(newValue, oldValue) {
            this.onLayoutPropertyChange(
                'top',
                newValue,
                oldValue,
                'height',
                this.draggable,
            );
        },
        width(newValue, oldValue) {
            this.onLayoutPropertyChange(
                'width',
                newValue,
                oldValue,
                'width',
                this.resizable,
            );
        },
        height(newValue, oldValue) {
            this.onLayoutPropertyChange(
                'height',
                newValue,
                oldValue,
                'height',
                this.resizable,
            );
        },
        absoluteLeft(value) {
            this.updateLayoutProperty('left', value, this.draggable);
        },
        absoluteTop(value) {
            this.updateLayoutProperty('top', value, this.draggable);
        },
        absoluteWidth(value) {
            this.updateLayoutProperty('width', value, this.resizable);
        },
        absoluteHeight(value) {
            this.updateLayoutProperty('height', value, this.resizable);
        },
        recalculateLayoutNumber(value) {
            if (value < this.recalculateLayoutNumber) {
                return;
            }
            this.checkAndUpdateLayout();
        },
        draggable(value) {
            if (value) {
                this.setUpMovable();
            }
            // else {
            //     this.$el.style.left = '';
            //     this.$el.style.top = '';
            // }
        },
        resizable(value) {
            if (value) {
                this.setUpResizable();
            }
            // else {
            //     this.$el.style.width = '';
            //     this.$el.style.height = '';
            // }
        },
        containerSize(newValue, oldValue) {
            if (
                newValue?.width === oldValue?.width &&
                newValue?.height === oldValue?.height
            ) {
                return;
            }
            this.checkAndUpdateLayout({ increaseSizeOnlyToFitContent: true });
        },
        selected(value) {
            this.isSelected = value;
        },
        isSelected(value) {
            if (value) {
                this.checkAndEmit('select-start');
                window.addEventListener('mousedown', this.windowOnMouseDown);
            } else {
                this.checkAndEmit('select-end');
                window.removeEventListener('mousedown', this.windowOnMouseDown);
            }
            this.$emit('update:selected', value);
        },
        cursor(value) {
            this.$el.style.cursor = value;
        },
    },
    async mounted() {
        // this.setUpMovable();
        // this.setUpResizable();
        const sizeAndPositionProperties = [
            'top',
            'left',
            'width',
            'height',
            'containerSize',
        ];
        if (
            sizeAndPositionProperties.every(
                (property) => this[property] !== null,
            ) &&
            !this.waitContent
        ) {
            this.setUpLayout();
        } else {
            this.setWatchersForLayoutSetUp();
        }
    },
    beforeUnmount() {
        window.removeEventListener('mousemove', this.onMouseMove);
        window.removeEventListener('mouseup', this.onMouseUp);
        window.removeEventListener('mousedown', this.windowOnMouseDown);
    },
    expose: [],
    methods: {
        setWatchersForLayoutSetUp() {
            if (this.watchingProperties) {
                return;
            }
            const sizeAndPositionProperties = [
                'top',
                'left',
                'width',
                'height',
                'containerSize',
            ];
            // Consider watching only properties that are not null
            const propertiesToWatch = [
                ...sizeAndPositionProperties,
                'waitContent',
            ];
            const unwatchProperties = new Map(
                propertiesToWatch.map((property) => [
                    property,
                    this.$watch(property, () => {
                        for (const [
                            property,
                            unwatch,
                        ] of unwatchProperties.entries()) {
                            if (
                                (property !== 'waitContent' &&
                                    this[property] !== null) ||
                                (property === 'waitContent' &&
                                    !this.waitContent)
                            ) {
                                unwatch();
                                unwatchProperties.delete(property);
                            }
                        }
                        if (unwatchProperties.size > 0) {
                            return;
                        }
                        if (
                            sizeAndPositionProperties.every(
                                (property) => this[property] !== null,
                            ) &&
                            !this.waitContent
                        ) {
                            this.setUpLayout();
                            this.watchingProperties = false;
                        } else {
                            this.setWatchersForLayoutSetUp();
                        }
                    }),
                ]),
            );
            this.watchingProperties = true;
        },
        getContentBoundingClientRect() {
            // TODO: Check if this is fixed.
            // It won't work if CSS transformations are applied to the element.
            // In that case it is need to calculate the bounding offset rect.
            // https://gist.github.com/andreiglingeanu/aa3aef6c8dffb2105736148b2cab3617
            return this.$refs.content.getBoundingClientRect();
        },
        async checkAndUpdateLayout({
            emit = true,
            ignoreResizeGrid = false,
            increaseSizeOnlyToFitContent = false,
        } = {}) {
            if (!this.isLayoutSetUp) {
                return;
            }
            if (this.width === 'auto' || this.height === 'auto') {
                await this.$nextTick();
            }
            let width = this.absoluteWidth;
            let height = this.absoluteHeight;
            const contentSize = { width: 0, height: 0 };
            if (
                !this.resizable ||
                this.fitContent ||
                this.width === 'auto' ||
                this.height === 'auto'
            ) {
                ({ width: contentSize.width, height: contentSize.height } =
                    this.getContentBoundingClientRect());
                width = Math.max(width, contentSize.width);
                height = Math.max(height, contentSize.height);
            }
            const size = this.processResizableSetup(width, height, {
                roundMethod:
                    this.fitContent && !increaseSizeOnlyToFitContent
                        ? 'ceil'
                        : 'round',
                ignoreResizeGrid: ignoreResizeGrid,
            });
            if (this.fitContent && increaseSizeOnlyToFitContent) {
                width = Math.max(width, contentSize.width);
                height = Math.max(height, contentSize.height);
            }
            this.updateSize(size, emit);

            const position = this.processMovableSetup(
                this.absoluteLeft,
                this.absoluteTop,
                this.absoluteWidth,
                this.absoluteHeight,
            );
            // this.updateMove(difference, false);
            this.updatePosition(position, emit);
        },

        async setUpLayout() {
            //
            this.absoluteLeft = this.getAbsoluteSizeOrPosition(
                this.left,
                'width',
            );
            this.absoluteTop = this.getAbsoluteSizeOrPosition(
                this.top,
                'height',
            );
            this.absoluteWidth = this.getAbsoluteSizeOrPosition(
                this.width,
                'width',
            );
            this.absoluteHeight = this.getAbsoluteSizeOrPosition(
                this.height,
                'height',
            );
            this.isLayoutSetUp = true;
            await this.checkAndUpdateLayout();
            // TODO: check why is style layout set even if not interactable
            if (!this.draggable) {
                this.$el.style.left = this.getCSSPixelValue(this.absoluteLeft);
                this.$el.style.top = this.getCSSPixelValue(this.absoluteTop);
            }
            if (!this.resizable) {
                this.$el.style.width = this.getCSSPixelValue(
                    this.absoluteWidth,
                );
                this.$el.style.height = this.getCSSPixelValue(
                    this.absoluteHeight,
                );
            }
            this.checkAndEmit('drag-setup', {
                args: [this.getAbsolutePosition()],
            });
            this.checkAndEmit('resize-setup', {
                args: [this.getAbsoluteSize()],
            });
        },

        setUpMovable() {
            // TODO: check if it can be removed or called from setUpLayout()
            this.$el.style.left = this.getCSSPixelValue(this.absoluteLeft);
            this.$el.style.top = this.getCSSPixelValue(this.absoluteTop);
            this.checkAndEmit('drag-setup');
        },

        setUpResizable() {
            this.$el.style.width = this.getCSSPixelValue(this.absoluteWidth);
            this.$el.style.height = this.getCSSPixelValue(this.absoluteHeight);
            this.checkAndEmit('resize-setup', {
                args: [this.getAbsoluteSize()],
            });
        },

        /**
         * Handles the mouse down event.
         *
         * @param {Event} event - The mouse event.
         */
        onMouseDown(event) {
            // console.log('onMouseDown', event.clientX, event.clientY);
            if (!this.draggable && !this.resizable) {
                return;
            }
            if (event.button !== 0) {
                return;
            }
            // Exclude interactable element if more specific
            // interactable element (child element) is clicked.
            // Used instead of stopPropagation() to allow
            // other elements (i.e. window) to handle the event.
            if (event.target.closest('.interactable-element') !== this.$el) {
                return;
            }
            if (!this.isSelected) {
                this.isSelected = true;
            }
            if (!this.draggable) {
                return;
            }

            this.isMoving = true;
            this.initiateMove(
                event.clientX,
                event.clientY,
                this.absoluteLeft,
                this.absoluteTop,
                this.absoluteWidth,
                this.absoluteHeight,
            );
            this.checkAndEmit('drag-start');
            window.addEventListener('mousemove', this.onMouseMove);
            window.addEventListener('mouseup', this.onMouseUp);
        },

        /**
         * Handles the mouse down event on resize handlers.
         *
         * @param {Event} event - The mouse event.
         */
        resizeHandlerOnMouseDown(event, handlerName) {
            if (!this.resizable) {
                return;
            }
            if (event.button !== 0) {
                return;
            }

            this.cursor = 'crosshair';

            this.isResizing = true;
            this.initiateResize(
                handlerName,
                event.clientX,
                event.clientY,
                this.absoluteLeft,
                this.absoluteTop,
                this.absoluteWidth,
                this.absoluteHeight,
            );
            this.checkAndEmit('resize-start');
            window.addEventListener('mousemove', this.onMouseMove);
            window.addEventListener('mouseup', this.onMouseUp);
        },

        /**
         * Handles the mouse move event.
         *
         * @param {MouseEvent} event - The mouse event.
         */
        onMouseMove(event) {
            if (!this.draggable && !this.resizable) {
                return;
            }
            // console.log('element onMouseMove', event.clientX, event.clientY);
            // console.log(this.$el);
            if (this.isMoving) {
                const position = this.processMove(event.clientX, event.clientY);
                this.updatePosition(position);
                this.checkAndEmit('drag-update');
            }
            if (this.isResizing) {
                const [size, position] = this.processResize(
                    event.clientX,
                    event.clientY,
                );
                this.updateSize(size);
                this.updatePosition(position);
                this.checkAndEmit('resize-update', {
                    args: [this.getAbsoluteSize()],
                });
                this.checkAndEmit('drag-update');
            }
        },

        /**
         * Handles the mouse up event.
         *
         * @param {MouseEvent} event - The mouse object.
         */
        onMouseUp(event) {
            if (!this.draggable && !this.resizable) {
                return;
            }
            if (event.button !== 0) {
                return;
            }
            // console.log(
            //     `element onMouseUp, isMoving: ${this.isMoving}, isResizing: ${this.isResizing}`
            // );
            if (this.isMoving) {
                const position = this.completeMove();
                // this.updateMove(position);
                this.isMoving = false;
                this.checkAndEmit('drag-end', {
                    args: [this.getRoundedPositionOrSize(position, precision)],
                });
            }
            if (this.isResizing) {
                const [position, size] = this.completeResize();
                // this.updateMove(position);
                this.cursor = '';
                this.isResizing = false;
                const precision = this.internalRoundingPrecision;
                this.checkAndEmit('resize-end', {
                    args: [
                        this.getRoundedPositionOrSize(position, precision),
                        this.getRoundedPositionOrSize(size, precision),
                    ],
                });
            }
            this.checkAndUpdateLayout({ ignoreResizeGrid: !this.isResizing });
            window.removeEventListener('mousemove', this.onMouseMove);
            window.removeEventListener('mouseup', this.onMouseUp);
        },

        /**
         * Handles the mouse down event for whole window.
         * Used to detect when the mouse is clicked outside the element
         * (or on another interactable element).
         *
         * @param {MouseEvent} event - The mouse object.
         */
        windowOnMouseDown(event) {
            // Check that current interactable element is not selected
            if (event.target.closest('.interactable-element') === this.$el) {
                return;
            }
            // Check that element editing toolbar is not clicked
            if (
                this.editingToolbarSelector !== '' &&
                event.target.closest(this.editingToolbarSelector) !== null
            ) {
                return;
            }
            this.isSelected = false;
        },

        /**
         * Updates element (styling, position) based on difference object.
         *
         * @param {Object} difference - The difference object.
         * @param {number} difference.x - The horizontal difference from mouse down event position.
         * @param {number} difference.y - The vertical difference from mouse down event position.
         * @param {number} difference.totalX - The horizontal difference from initial element position.
         * @param {number} difference.totalY - The vertical difference from initial element position.
         */
        updatePosition(position, emit = true) {
            // console.log('displayMove', difference);
            const precision = this.internalRoundingPrecision;
            // this.left = this.roundTo(difference.totalX, precision);
            // this.top = this.roundTo(difference.totalY, precision);
            // const oldPosition = {
            //     left: this.absoluteLeft,
            //     top: this.absoluteTop,
            // };
            const oldPosition = this.getAbsolutePosition();
            this.absoluteLeft = this.roundTo(position.left, precision);
            this.absoluteTop = this.roundTo(position.top, precision);
            // this.checkAndEmit('drag-update', { emit });
            const newPosition = this.getAbsolutePosition();
            this.checkAndEmit('position-update', { emit, args: [newPosition] });
            const size = this.getAbsoluteSize();
            const oldLayout = { ...oldPosition, ...size };
            const layout = { ...newPosition, ...size };
            this.checkAndEmit('layout-update', {
                emit,
                args: [layout, oldLayout],
            });
        },

        /**
         * Updates element (styling, position) based on size object.
         *
         * @param {Object} size - The size object.
         * @param {number} size.width
         * @param {number} size.height
         */
        updateSize(size, emit = true) {
            // console.log('displayResize', size);
            const precision = this.internalRoundingPrecision;
            const oldSize = this.getAbsoluteSize();
            this.absoluteWidth = this.roundTo(size.width, precision);
            this.absoluteHeight = this.roundTo(size.height, precision);
            // this.checkAndEmit('resize-update', { emit });
            const newSize = this.getAbsoluteSize();
            this.checkAndEmit('size-update', {
                emit,
                args: [newSize],
            });    
            const position = this.getAbsolutePosition();       
            const oldLayout = { ...position, ...oldSize };
            const layout = { ...position, ...newSize };
            this.checkAndEmit('layout-update', {
                emit,
                args: [layout, oldLayout],
            });
        },

        // methods from mixins

        isForcedInsideContainer() {
            return this.forceInsideContainer;
        },

        getAbsoluteDragGrid() {
            return this.absoluteDragGrid;
        },

        getAbsoluteResizeGrid() {
            return this.absoluteResizeGrid;
        },

        getContainerSize() {
            return this.containerSize;
        },

        getKeepRatio() {
            return this.keepRatio;
        },

        getMinimumHeight() {
            return this.minimumHeight;
        },

        getMinimumWidth() {
            return this.minimumWidth;
        },

        getInternalRoundingPrecision() {
            return this.internalRoundingPrecision;
        },

        //
        // helper methods

        checkIfSizeOrPositionChanged(newValue, oldValue) {
            if (!Array.isArray(newValue) || !Array.isArray(oldValue)) {
                return newValue !== oldValue;
            }
            return (
                newValue[0] !== oldValue[0] ||
                newValue[1]?.useRelativeUnits !==
                    oldValue[1]?.useRelativeUnits ||
                newValue[1]?.relativeTo !== oldValue[1]?.relativeTo
            );
        },

        emitLayoutPropertyChangeIfChanged(propertyName, absolutePropertyName) {
            if (this[propertyName] === this[absolutePropertyName]) {
                return;
            }
            this.$emit(`update:${propertyName}`, this[absolutePropertyName]);
        },

        getUnroundedAbsoluteSizeOrPosition(value, relativeTo) {
            if (typeof value === 'number') {
                return value;
            }
            if (value === null) {
                // TODO: remove if unnecessary
                console.log(value);
                return null;
            }
            if (value === 'auto') {
                return null;
            }
            const [numberValue, options] = value;
            if (!(options.useRelativeUnits ?? false)) {
                return numberValue;
            }
            if (this.containerSize === null) {
                return null;
            }
            const containerValue =
                this.containerSize[options.relativeTo ?? relativeTo];
            // return (numberValue / 100) * containerValue;
            return numberValue * containerValue;
        },

        getAbsoluteSizeOrPosition(value, relativeTo) {
            const absoluteValue = this.getUnroundedAbsoluteSizeOrPosition(
                value,
                relativeTo,
            );
            if (absoluteValue === null) {
                return null;
            }
            return this.roundTo(absoluteValue, this.internalRoundingPrecision);
        },

        checkAndEmit(eventName, { emit = true, args: arguments_ = [] } = {}) {
            if (emit && this.emitEvents.includes(eventName)) {
                this.$nextTick(() => this.$emit(eventName, ...arguments_));
            }
        },

        onLayoutPropertyChange(
            propertyName,
            newValue,
            oldValue,
            relativeTo,
            interactableCondition,
        ) {
            if (newValue === null) {
                return;
            }
            const capitalizedPropertyName =
                propertyName.charAt(0).toUpperCase() + propertyName.slice(1);
            const absolutePropertyName = `absolute${capitalizedPropertyName}`;
            // TODO: Consider using $data instead of this directly
            // check other places where this[prop] is used
            if (newValue === this[absolutePropertyName]) {
                // If property uses v-model then property is updated
                // every time its corresponding absolute property is updated
                return;
            }
            if (!this.checkIfSizeOrPositionChanged(newValue, oldValue)) {
                this.emitLayoutPropertyChangeIfChanged(
                    propertyName,
                    absolutePropertyName,
                );
                return;
            }
            const newAbsoluteValue = this.getAbsoluteSizeOrPosition(
                newValue,
                relativeTo,
            );
            if (newAbsoluteValue == this[absolutePropertyName]) {
                this.emitLayoutPropertyChangeIfChanged(
                    propertyName,
                    absolutePropertyName,
                );
                return;
            }
            this[absolutePropertyName] = newAbsoluteValue;
            if (!interactableCondition) {
                this.$el.style[propertyName] = this.getCSSPixelValue(
                    this[absolutePropertyName],
                );
            }
        },

        updateLayoutProperty(propertyName, value, interactableCondition) {
            if (!interactableCondition) {
                return;
            }
            this.$el.style[propertyName] = this.getCSSPixelValue(value);
            this.$emit(`update:${propertyName}`, value);
        },

        getAbsolutePosition() {
            return {
                left: this.absoluteLeft,
                top: this.absoluteTop,
            };
        },

        getAbsoluteSize() {
            return {
                width: this.absoluteWidth,
                height: this.absoluteHeight,
            };
        },

        getRoundedPositionOrSize(positionOrSize, precision) {
            return Object.fromEntries(
                Object.entries(positionOrSize).map(([key, value]) => [
                    key,
                    this.roundTo(value, precision),
                ]),
            );
        },

        //
        // methods from mixins

        getStyleRoundingPrecision() {
            return this.styleRoundingPrecision;
        },
    },
};
</script>

<style scoped>
.interactable-element {
    width: max-content;
    position: absolute;
}

.interactable-element > .resize-handler {
    position: absolute;
}

.interactable-element > .resize-handler.tl {
    cursor: nw-resize;
}

.interactable-element > .resize-handler.t {
    cursor: n-resize;
}

.interactable-element > .resize-handler.tr {
    cursor: ne-resize;
}

.interactable-element > .resize-handler.r {
    cursor: e-resize;
}

.interactable-element > .resize-handler.br {
    cursor: se-resize;
}

.interactable-element > .resize-handler.b {
    cursor: s-resize;
}

.interactable-element > .resize-handler.bl {
    cursor: sw-resize;
}

.interactable-element > .resize-handler.l {
    cursor: w-resize;
}

.content {
    display: flex;
    min-height: 100%;
    min-width: 100%;
}
</style>
