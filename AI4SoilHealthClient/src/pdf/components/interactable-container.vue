<!-- mouseup and mousemove events are setup in mounted() method
    because listeners are added to the window object -->
<!-- @mouseup="onMouseUp" @mousemove="onMouseMove" -->
<template>
    <div class="interactable-container" @selectstart.prevent>
        <slot />
    </div>
</template>

<script>
export default {
    name: 'InteractableContainer',
    // provide() {
    //     return {
    //         interactableManager: this.interactableManager,
    //     };
    // },
    data() {
        // const self = this;
        // const interactableManager = {
        //     interactableElements: new Map(),
        //     sentEventsNumber: 0,
        //     addElement(interactableElement) {
        //         this.interactableElements.set(interactableElement, {
        //             isInteractive: false,
        //         });
        //     },
        //     removeElement(interactableElement) {
        //         this.interactableElements.delete(interactableElement);
        //     },
        //     setElementInteractivity(interactableElement, interactivity) {
        //         this.interactableElements.get(
        //             interactableElement
        //         ).isInteractive = interactivity;
        //     },
        //     setOneElementInteractable(interactableElement) {
        //         this.interactableElements.forEach((state, element) => {
        //             element.setInteractability(element === interactableElement);
        //         });
        //     },
        //     setAllElementsInteractable() {
        //         this.interactableElements.forEach((state, element) => {
        //             element.setInteractability(true);
        //         });
        //     },
        //     callMethodOnAllElements(method, ...args) {
        //         this.interactableElements.forEach((state, element) => {
        //             element[method](...args);
        //         });
        //     },
        //     async asyncCallMethodOnAllElements(method, ...args) {
        //         await Promise.all(
        //             Array.from(this.interactableElements.entries()).map(
        //                 ([element, _state]) => element[method](...args)
        //             )
        //         );
        //     },
        //     sendEventToInteractiveElements(method, event) {
        //         this.interactableElements.forEach((state, element) => {
        //             if (state.isInteractive) {
        //                 element[method](event);
        //                 this.sentEventsNumber++;
        //             }
        //         });
        //     },
        //     getContainerClientRect() {
        //         // It won't work if CSS transformations are applied
        //         // to the container element. In that case it is need
        //         // to calculate the bounding offset rect.
        //         // https://gist.github.com/andreiglingeanu/aa3aef6c8dffb2105736148b2cab3617
        //         return self.$el.getBoundingClientRect();
        //     },
        //     setContainerCursor(cursor) {
        //         self.cursor = cursor;
        //     },
        //     getContentMinimumSizeValue(elementSizeGetter) {
        //         const elementSizes = Array.from(
        //             this.interactableElements.keys(),
        //             elementSizeGetter
        //         );
        //         const contentMinimumSizeValue = Math.max(0, ...elementSizes);
        //         return contentMinimumSizeValue;
        //     },
        //     getContentMinimumSize(refresh = false) {
        //         const contentMinimumWidth = this.getContentMinimumSizeValue(
        //             (element) => element.getWidth(refresh)
        //         );
        //         const contentMinimumHeight = this.getContentMinimumSizeValue(
        //             (element) => element.getHeight(refresh)
        //         );
        //         return [contentMinimumWidth, contentMinimumHeight];
        //     },
        // };
        return {
            // interactableManager: interactableManager,
            cursor: '',
            contentMinimumSize: [0, 0],
            // lastSentEventsNumber: interactableManager.sentEventsNumber - 1,
            // styleChangedNumber: 0,
            // lastStyleChangedNumber: 0,
        };
    },
    watch: {
        cursor(value) {
            this.$el.style.cursor = value;
        },
    },
    // mounted() {
    //     window.addEventListener('mousemove', this.onMouseMove);
    //     window.addEventListener('mouseup', this.onMouseUp);
    // },
    // unmounted() {
    //     window.removeEventListener('mousemove', this.onMouseMove);
    //     window.removeEventListener('mouseup', this.onMouseUp);
    // },
    expose: [
        // 'forceElementsInside',
        // 'styleChanged',
        // 'getContentMinimumSize'
    ],
    methods: {
        /**
         * Handles the mouse move event.
         *
         * @param {MouseEvent} event - The mouse event.
         */
        // onMouseMove(event) {
        //     // console.log('container onMouseMove', event.clientX, event.clientY);
        //     this.interactableManager.sendEventToInteractiveElements(
        //         'onMouseMove',
        //         event
        //     );
        // },

        /**
         * Handles the mouse up event.
         *
         * @param {MouseEvent} event - The mouse object.
         */
        // onMouseUp(event) {
        //     // console.log('container onMouseUp');
        //     this.interactableManager.sendEventToInteractiveElements(
        //         'onMouseUp',
        //         event
        //     );
        // },

        // exposed methods

        // forceElementsInside({
            // saveRelativePosition = false,
            // useSavedRelativePosition = true,
        // } = {}) {
            // this.interactableManager.callMethodOnAllElements('setUpMovable', {
            //     setUpInitial: false,
            //     saveRelativePosition,
            //     useSavedRelativePosition,
            // });
        // },

        // async styleChanged(forceRefresh = false) {
        //     this.styleChangedNumber++;
        //     await this.interactableManager.asyncCallMethodOnAllElements(
        //         'styleChanged',
        //         forceRefresh
        //     );
        // },

        // getContentMinimumSize() {
        //     const styleChanged =
        //         this.styleChangedNumber > this.lastStyleChangedNumber;
        //     if (
        //         this.interactableManager.sentEventsNumber >
        //             this.lastSentEventsNumber ||
        //         styleChanged
        //     ) {
        //         this.contentMinimumSize =
        //             this.interactableManager.getContentMinimumSize(
        //                 styleChanged
        //             );
        //         this.lastSentEventsNumber =
        //             this.interactableManager.sentEventsNumber;
        //         this.lastStyleChangedNumber = this.styleChangedNumber;
        //     }
        //     return this.contentMinimumSize;
        // },
    },
};
</script>

<style scoped>
.interactable-container {
    position: relative;
}
</style>
