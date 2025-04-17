/**
 * @desc Module for adding additional components to the application.
 * @module additionalComponents
 */
import { addComponents } from '../common/component-loader.js';
function additionalComponents() {
    addComponents(import.meta.glob('../pdf/components/*.vue'), 'pdf/components');
}
export { additionalComponents };